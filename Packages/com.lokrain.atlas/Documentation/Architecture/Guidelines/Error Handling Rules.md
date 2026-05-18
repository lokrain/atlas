# Error handling rules

This document defines error handling rules for Lokrain.Atlas.

Lokrain.Atlas uses exceptions for invalid API usage and result objects for expected boundary failures.

## Core rule

Use exceptions when the caller violates the API contract.

Use result objects when failure is an expected outcome of a boundary operation.

Correct:

```text
Symbol.Create("Invalid.Symbol") throws ArgumentException.
new Grid(0, 256) throws ArgumentOutOfRangeException.
GenerationRequestResolver.Resolve(...) returns a failed GenerationRequestResolutionResult for an unknown recipe symbol.
```

Incorrect:

```text
Symbol.Create("Invalid.Symbol") returns null.
new Grid(0, 256) creates an invalid grid.
GenerationRequestResolver.Resolve(...) throws for an unknown recipe symbol.
```

## Exception boundary

Throw exceptions for programmer errors and invalid local state.

Use exceptions for:

```text
null required arguments
out-of-range primitive values
invalid primitive text in Create or Parse methods
duplicate entries in accepted object constructors
invalid local resource flow
invalid final request choices
invalid catalog inventory
invalid managed plan construction
```

Examples:

```text
ArgumentNullException
ArgumentException
ArgumentOutOfRangeException
InvalidOperationException
```

Do not use custom exceptions unless a caller needs to catch a package-specific exception type.

## Result-object boundary

Use result objects when failure is normal for the operation.

Current result-object boundary:

```text
GenerationRequestResolver.Resolve(...)
```

Request resolution may fail because symbolic caller intent cannot be satisfied by a catalog.

Expected resolver failures include:

```text
unknown recipe symbol
override route step not selected by recipe
unknown implementation symbol
implementation operation mismatch
```

These failures should produce `GenerationRequestResolutionResult` with structured errors.

They should not throw.

## Descriptor resolution rule

Descriptors are symbolic input.

A descriptor can be structurally valid while still being unsatisfied by a catalog.

Correct:

```text
GenerationRequestDescriptor contains a valid recipe symbol.
GenerationRequestResolver returns recipe-not-found error when the catalog does not contain that symbol.
```

Incorrect:

```text
GenerationRequestDescriptor constructor requires a catalog.
GenerationRequestDescriptor constructor throws because the recipe symbol is unknown to one catalog.
```

Catalog-dependent failures belong to the resolver.

## Catalog construction rule

Catalog construction is an accepted-inventory boundary.

Invalid catalog inventory should throw.

Catalog construction failures are not request-resolution failures.

Throw for:

```text
duplicate definitions
definitions referencing schemas not owned by the catalog
contracts referencing resources not owned by the catalog
routes referencing missing operations
recipes referencing definitions not owned by the catalog
schema-inconsistent graphs
unsatisfied recipe graph dependencies
```

Correct:

```text
GenerationCatalogBuilder.Build() throws for invalid inventory.
```

Incorrect:

```text
GenerationCatalogBuilder.Build() returns GenerationRequestResolutionResult.
```

## Constructor rule

Constructors for accepted objects must reject invalid local state.

Use:

```text
ArgumentNullException
ArgumentException
ArgumentOutOfRangeException
```

Correct:

```text
new ResourceDefinition(null, displayName, schema)
  -> ArgumentNullException

new StageContract(stage, duplicatedInputs, outputs)
  -> ArgumentException

new Grid(128, 256)
  -> ArgumentOutOfRangeException
```

Incorrect:

```text
constructor stores invalid state
constructor returns null
constructor logs and continues
```

## Factory rule

`Create` and `Parse` methods throw when input is invalid.

`TryCreate` and `TryParse` methods return `false` when input is invalid.

Correct:

```text
Symbol.Create(value)
Symbol.TryCreate(value, out Symbol? symbol)

Seed.Parse(value)
Seed.TryParse(value, out Seed seed)
```

Incorrect:

```text
TryCreate throws for ordinary invalid input.
Create returns null for invalid input.
Parse returns default for invalid input.
```

## Null argument rule

Throw `ArgumentNullException` for required null arguments.

The parameter name must match the public API parameter.

Correct:

```csharp
throw new ArgumentNullException(nameof(value));
```

Incorrect:

```csharp
throw new ArgumentException("Value cannot be null.");
throw new ArgumentNullException("input");
```

Use nullable annotations so nullability is visible in the API.

## Invalid value rule

Throw `ArgumentException` when a value has invalid shape or violates a non-range invariant.

Use this for:

```text
invalid symbol text
invalid display-name text
duplicate collection entries
null entries inside required collections
invalid local graph shape
```

Correct:

```csharp
throw new ArgumentException("Symbol is invalid.", nameof(value));
```

Do not use `InvalidOperationException` for invalid constructor arguments.

## Out-of-range rule

Throw `ArgumentOutOfRangeException` when a numeric value is outside an allowed range.

Use this for:

```text
grid width
grid depth
cell coordinates
flattened cell index
```

Correct:

```csharp
throw new ArgumentOutOfRangeException(
    nameof(width),
    width,
    "Grid width must be between 256 and 4096.");
```

## Invalid operation rule

Use `InvalidOperationException` when the object is valid but the requested operation cannot be completed in the current state.

Examples:

```text
managed plan compilation detects an impossible dependency state
a required internal mapping is missing after prior validation should have established it
```

Do not use `InvalidOperationException` for null arguments or invalid primitive input.

## Error object rule

A structured error object must contain enough information for callers and tools to understand the failure.

`GenerationRequestResolutionError` contains:

```text
Code
Message
SubjectSymbol
```

The code is a stable symbol.

The message is human-readable diagnostic text.

The subject symbol identifies the recipe, route step, implementation, or other symbol involved when available.

## Error code rule

Error codes must be stable symbols.

Use lowercase dot-separated package symbols.

Correct:

```text
lokrain.atlas.planning.recipe_not_found
lokrain.atlas.planning.route_step_not_selected_by_recipe
lokrain.atlas.planning.implementation_not_found
lokrain.atlas.planning.implementation_operation_mismatch
```

Incorrect:

```text
RecipeNotFound
404
MissingImplementation
implementation mismatch
```

Do not localize error codes.

## Error message rule

Error messages are diagnostics.

They should be clear, specific, and short.

They may include the relevant symbol text.

They must not be used as programmatic identity.

Correct:

```text
Generation recipe was not found in the catalog: lokrain.atlas.tests.recipe.unknown.
```

Incorrect:

```text
Bad.
Something went wrong.
Recipe not found!!! Please fix.
```

## Result object rule

A result object must make success and failure explicit.

Correct:

```text
Succeeded
Failed
GenerationRequest
Errors
```

On success:

```text
Succeeded == true
Failed == false
GenerationRequest != null
Errors is empty
```

On failure:

```text
Succeeded == false
Failed == true
GenerationRequest == null
Errors is not empty
```

Avoid ambiguous result states.

## Multiple error rule

When a boundary can discover multiple independent expected errors, return all useful errors in stable order.

For request resolution, override errors should follow descriptor override order.

Correct:

```text
override[0] -> route step not selected
override[1] -> implementation not found
```

Incorrect:

```text
unordered error output
first error only when later independent errors are cheap and safe to report
random hash-map order
```

Do not continue after an error when later checks depend on missing accepted data.

## No logging as control flow

Do not use logs as the primary error mechanism.

Correct:

```text
throw exception
return failed result object
```

Incorrect:

```text
Debug.LogError(...)
return default
continue with invalid state
```

Logging may supplement an error at Unity adapter or tooling boundaries. It must not replace exceptions or result objects in Runtime domain logic.

## No null-as-error rule

Do not use `null` as the only failure signal.

Correct:

```text
TryGetResourceDefinition(symbol, out ResourceDefinition? definition)
GenerationRequestResolutionResult.Failed
```

Incorrect:

```text
GetResourceDefinition(symbol) returns null when missing.
Resolve(...) returns null when resolution fails.
```

Use `TryGet` for non-throwing lookup.

Use `Get` for throwing lookup.

Use result objects for boundary operations with structured failure.

## Lookup rules

Use standard lookup semantics.

`Contains...` returns `true` or `false`.

`TryGet...` returns `true` or `false` and sets an out value.

`Get...` returns the value or throws when missing.

Correct:

```text
ContainsResourceDefinition(symbol)
TryGetResourceDefinition(symbol, out ResourceDefinition? resourceDefinition)
GetResourceDefinition(symbol)
```

Incorrect:

```text
FindResourceDefinition(symbol) returns null
MaybeResource(symbol)
GetResourceDefinition(symbol, bool throwIfMissing)
```

## Unity boundary rule

Runtime domain code must not depend on Unity logging, Unity dialogs, editor UI, or console behavior for error handling.

Correct Runtime behavior:

```text
throw ArgumentException
return GenerationRequestResolutionResult
```

Correct adapter behavior:

```text
catch exception or inspect result
display message in editor UI
log diagnostic if useful
```

Incorrect Runtime behavior:

```text
EditorUtility.DisplayDialog(...)
Debug.LogError(...)
```

## Future execution rule

Future execution should keep the same boundary model.

Expected execution failures should use structured result objects or diagnostics.

Invalid API usage should throw.

Planned examples:

```text
RunnablePlanCompiler returns structured failure if field definitions cannot satisfy a plan.
GenerationWorkspace throws for invalid allocation requests.
OperationScheduler returns structured execution diagnostics for expected execution failure.
```

Do not mix scheduler diagnostics into current managed planning objects.

## Exception message rule

Exception messages should state the violated rule.

They should not include excessive implementation detail.

Correct:

```text
Grid width must be between 256 and 4096.
Display name cannot contain whitespace characters other than ordinary spaces.
Generation request must contain exactly one implementation choice for each selected route step.
```

Incorrect:

```text
bad width
invalid
System failed in internal method ValidateThing at index 3
```

## Testing rule

Tests must assert the error boundary.

Use exception assertions for invalid API usage.

Use result assertions for expected resolver failure.

Correct:

```text
Assert.Throws<ArgumentNullException>(...)
Assert.That(result.Succeeded, Is.False)
Assert.That(result.Errors[0].Code, Is.EqualTo(...))
```

Incorrect:

```text
expect resolver to throw for unknown recipe
expect constructor to return failed result for null argument
```

## Checklist

Before adding error handling, verify:

```text
Is this invalid API usage?
If yes, throw a precise exception.

Is this an expected boundary failure?
If yes, return a result object.

Is the input symbolic and catalog-dependent?
If yes, resolver failure should be a structured result.

Is the inventory graph invalid?
If yes, catalog construction should throw.

Is this a TryCreate, TryParse, or TryGet method?
If yes, ordinary invalid input should return false.

Does the result object have unambiguous success and failure states?
Does each structured error have a stable code?
Are errors returned in stable order?
Is null avoided as the only failure signal?
Is logging avoided as control flow?
```