# Error handling rules

This document defines error-handling rules for Lokrain.Atlas Runtime architecture.

Atlas separates invalid API usage from expected domain failure.

Use exceptions for invalid API usage.

Use result objects for expected failure at a domain boundary.

## Primary rule

Throw for invalid API usage.

Return a result object for expected domain failure.

Correct:

```text
Symbol.Create(invalidText) -> throws
Grid.GetCell(outOfRangeIndex) -> throws
GenerationRequestResolver.Resolve(validDescriptorWithMissingRecipe) -> failed result
````

Incorrect:

```text
Symbol.Create(invalidText) -> failed result
GenerationRequestResolver.Resolve(validDescriptorWithMissingRecipe) -> throws
GenerationPlanCompiler.Compile(descriptor) -> tries to resolve symbols
```

## Exception boundary

Exceptions represent caller misuse, invalid constructor input, violated preconditions, or impossible object state.

Throw exceptions when the caller passes data that the API contract does not accept.

Examples:

```text
null required argument
invalid symbol text
invalid display name text
invalid grid dimensions
out-of-range grid coordinate
null collection entry
duplicate constructor input where uniqueness is required
attempting to create a success result without a value
attempting to create a failure result without errors
```

Exceptions should be precise and standard where possible.

Preferred exception types:

```text
ArgumentNullException
ArgumentException
ArgumentOutOfRangeException
InvalidOperationException
```

Do not create custom exceptions unless the package needs a stable catchable exception contract.

## Result boundary

Result objects represent expected negative outcomes where the caller supplied valid symbolic input but the current domain state cannot satisfy it.

Current result boundary:

```text
GenerationRequestResolver.Resolve(...)
```

Expected request-resolution failures include:

```text
requested recipe symbol not found
override route-step symbol not found in selected recipe
implementation symbol not found
implementation incompatible with targeted route-step operation
duplicate override target
descriptor cannot be satisfied by catalog
```

These are not exceptional. They are normal outcomes of resolving symbolic input against a catalog.

## Boundary ownership

The object that owns a boundary owns its errors.

| Failure                                   | Owner                                       |
| ----------------------------------------- | ------------------------------------------- |
| Invalid symbol text                       | `Symbol`                                    |
| Invalid display name text                 | `DisplayName`                               |
| Invalid grid dimensions                   | `Grid`                                      |
| Invalid cell coordinate for a grid        | `Grid`                                      |
| Invalid resource constructor input        | `ResourceDefinition`                        |
| Invalid stage contract input              | `StageContract`                             |
| Invalid operation contract input          | `OperationContract`                         |
| Duplicate or cross-catalog definitions    | `GenerationCatalog`                         |
| Descriptor cannot be satisfied by catalog | `GenerationRequestResolver`                 |
| Invalid accepted request construction     | `GenerationRequest`                         |
| Invalid managed plan construction         | `GenerationPlanCompiler` / `GenerationPlan` |
| Native allocation failure                 | Future workspace                            |
| Job dependency failure                    | Future scheduler                            |

Do not make unrelated layers report or repair failures they do not own.

## Constructor and factory errors

Constructors and factories must validate local invariants.

They should throw immediately when local invariants are invalid.

Correct:

```text
ResourceDefinition(null, displayName, schema) -> throws
StageContract(requiredInputsWithNullEntry, producedOutputs) -> throws
GenerationRunSettings(null, seed) -> throws
```

Incorrect:

```text
ResourceDefinition stores null and waits for catalog validation.
StageContract stores null resources and waits for plan compilation.
GenerationRunSettings accepts null grid and waits for execution.
```

Accepted objects must not expose partially valid state.

## Collection input errors

APIs that accept collections must snapshot and validate them.

They should reject:

```text
null collection argument
null collection entry
duplicate entries when uniqueness is required
entries that violate the local type contract
```

Correct behavior:

```text
copy enumerable input
validate copied values
store read-only snapshot
throw precise exception for invalid local input
```

Incorrect behavior:

```text
store caller-owned mutable list
allow null entries
defer local validation to catalog or compiler
depend on lazy enumerable evaluation after construction
```

## Catalog errors

Catalog construction errors are invalid accepted-inventory errors.

`GenerationCatalog` should throw when candidate inventory violates catalog invariants.

Catalog construction errors include:

```text
duplicate definition symbols
schema ownership mismatch
resource ownership mismatch
stage ownership mismatch
route-step graph inconsistency
operation implementation incompatibility
contract resource ownership mismatch
recipe graph inconsistency
cross-catalog object reuse
```

These are not request-resolution failures. They mean the package inventory is invalid.

## Descriptor errors

Descriptor construction errors are invalid API usage.

A descriptor should throw when its own local shape is invalid.

Examples:

```text
null recipe symbol
null run settings
null override descriptor
duplicate override target when descriptor owns uniqueness
```

A descriptor should not throw because a catalog does not contain its symbols.

That failure belongs to request resolution.

## Resolution errors

`GenerationRequestResolver` owns symbolic satisfiability.

It should return `GenerationRequestResolutionResult` failure when a valid descriptor cannot be satisfied by a catalog.

Resolution errors should be structured.

A resolution error should expose:

```text
Code
Message
SubjectSymbol
```

The code is stable machine-facing identity.

The message is human-facing diagnostic text.

The subject symbol identifies the relevant descriptor or catalog symbol when available.

## Resolution error codes

Resolution error codes must be stable and specific.

Good examples:

```text
RecipeNotFound
OverrideTargetNotFound
ImplementationNotFound
ImplementationNotCompatible
DuplicateOverrideTarget
```

Avoid vague codes:

```text
Error
Invalid
Failed
BadRequest
UnknownProblem
```

Use `Unknown` only when the architecture explicitly needs a fallback for externally sourced errors.

## Diagnostic messages

Diagnostic messages should be clear and human-facing.

They may include:

```text
the failing symbol
the expected category
the selected recipe or route-step context
the incompatible implementation and operation when relevant
```

Messages must not be used as machine-facing identity.

Tests and tooling should assert error codes and subject symbols, not full message text, except where message formatting itself is intentionally under test.

## Subject symbols

Use `SubjectSymbol` for the primary symbol related to an error.

Examples:

| Error                       | Subject symbol                                                                              |
| --------------------------- | ------------------------------------------------------------------------------------------- |
| Missing recipe              | Requested recipe symbol.                                                                    |
| Missing override target     | Route-step symbol from the override descriptor.                                             |
| Missing implementation      | Implementation symbol from the override descriptor.                                         |
| Incompatible implementation | Implementation symbol or targeted route-step symbol, depending on which is more actionable. |
| Duplicate override target   | Duplicated route-step symbol.                                                               |

When no useful subject exists, the subject may be absent according to the result model.

Do not use display names as diagnostic identity.

## Success and failure result shape

A result object must have exactly one state: success or failure.

A successful result must contain the accepted value.

A failed result must contain one or more errors.

Invalid states are not allowed:

```text
success without value
failure without errors
success with errors
failure with accepted value
```

The result type must make invalid states impossible or reject them at construction.

## Plan compiler errors

`GenerationPlanCompiler` consumes an accepted `GenerationRequest`.

The compiler should throw when given invalid API input.

Examples:

```text
null request
request with invalid impossible internal state
duplicate plan nodes where the compiler requires uniqueness
```

The compiler should not return request-resolution errors.

The compiler should not resolve descriptor symbols.

If the compiler receives an accepted request, normal missing-symbol errors should already be impossible.

## Runtime execution errors

Execution error policy is future architecture.

Future workspace and scheduler errors may include:

```text
native allocation failure
field binding failure
scheduler binding failure
job dependency failure
operation timeout or termination failure
artifact capture failure
```

These errors must be owned by future workspace, runnable compiler, scheduler, or artifact systems.

Do not add execution failure policy to current catalog, recipe, request, or managed plan objects.

## Throwing versus Try methods

Use throwing APIs when invalid input is caller misuse.

Examples:

```text
Symbol.Create(...)
DisplayName.Create(...)
Seed.Parse(...)
Grid.GetCell(...)
Grid.GetIndex(...)
```

Use `Try` APIs when invalid or missing input is common and non-exceptional.

Examples:

```text
Symbol.TryCreate(...)
DisplayName.TryCreate(...)
Seed.TryParse(...)
Grid.TryGetCell(...)
Grid.TryGetIndex(...)
GenerationCatalog.TryGetResourceDefinition(...)
```

A `Try` method should follow the .NET pattern:

```text
bool TryGetX(..., out X value)
```

Do not use `Try` for methods that still throw for normal negative outcomes.

## Parse versus Create

Use `Parse` / `TryParse` for text conversion.

Examples:

```text
Seed.Parse(string value)
Seed.TryParse(string value, out Seed seed)
```

Use `Create` / `TryCreate` for validated domain creation.

Examples:

```text
Symbol.Create(string value)
Symbol.TryCreate(string value, out Symbol symbol)
DisplayName.Create(string value)
DisplayName.TryCreate(string value, out DisplayName displayName)
```

`Parse` implies conversion from a textual representation.

`Create` implies validated construction from input.

## Null handling

Required reference arguments must be rejected.

Use `ArgumentNullException` when the argument itself is null.

Use `ArgumentException` when the argument exists but contains invalid data.

Examples:

```text
null collection -> ArgumentNullException
collection with null entry -> ArgumentException
null required value object -> ArgumentNullException
empty symbol text -> ArgumentException
```

Do not silently replace null with defaults unless the API explicitly defines that behavior.

## Range handling

Use `ArgumentOutOfRangeException` for numeric values outside the accepted range.

Examples:

```text
grid width below minimum
grid depth above maximum
cell X outside grid
cell Z outside grid
flattened index outside grid
```

Range error messages should include the valid range when practical.

## Duplicate handling

Reject duplicates at the boundary that owns uniqueness.

Examples:

| Duplicate                                                            | Owner                                                          |
| -------------------------------------------------------------------- | -------------------------------------------------------------- |
| Duplicate resource symbols in catalog                                | `GenerationCatalog`                                            |
| Duplicate required resources in a contract if uniqueness is required | `StageContract` / `OperationContract`                          |
| Duplicate route-step override target                                 | Descriptor or resolver, depending on where uniqueness is owned |
| Duplicate stage plan node when impossible                            | `GenerationPlanCompiler` / `GenerationPlan`                    |

Do not allow duplicates and rely on later systems to pick an arbitrary winner.

## Defensive validation

Defensive validation is allowed at trust boundaries.

It must not duplicate architecture ownership unnecessarily.

Correct defensive validation:

```text
public constructor rejects null
public method rejects null request
catalog rejects cross-catalog object reuse
compiler rejects impossible accepted request state
```

Incorrect defensive validation:

```text
every operation plan node re-resolves symbols through the catalog
jobs validate resource symbols
workspace validates recipe choices
ResourceDefinition validates catalog membership
```

Defensive validation must not reverse dependency direction.

## Determinism and errors

Error handling must not introduce nondeterminism.

Do not depend on:

```text
dictionary enumeration order
hash set enumeration order
managed object allocation order
thread timing
current time
Unity scene order
Unity asset import order
```

When returning multiple errors, order must be stable.

Preferred ordering should follow descriptor order, recipe order, route-step order, or sorted symbol order, depending on the boundary.

The owner of the boundary must define the ordering.

## Logging

Domain-layer APIs should not log as their primary error mechanism.

They should return result errors or throw exceptions.

Logging may be added by adapters, tooling, tests, or execution systems that have a clear output surface.

Incorrect:

```text
resolver logs missing recipe and returns success with fallback
catalog logs duplicate symbol and keeps first entry
plan compiler logs invalid route and skips it
```

Correct:

```text
resolver returns failed result
catalog throws
plan compiler throws
```

## Fallbacks

Do not silently fall back across architecture boundaries.

Invalid:

```text
missing recipe -> use first recipe
missing implementation -> use first implementation in catalog
invalid symbol -> normalize into different identity
invalid grid -> clamp to valid range
missing resource -> create placeholder resource
```

Fallbacks are allowed only when the API explicitly defines them as domain behavior.

When fallback behavior exists, it must be deterministic and documented.

## Message style

Exception and diagnostic messages should be specific.

Good:

```text
"Grid width must be between 256 and 4096."
"Display name cannot contain whitespace characters other than ordinary spaces."
"Recipe symbol was not found in the catalog."
```

Bad:

```text
"Invalid."
"Bad data."
"Something went wrong."
"Failed."
```

Messages should describe the violated rule, not internal implementation details.

## Public API consistency

Equivalent APIs should use equivalent failure behavior.

If one constructor throws for null input, similar constructors should also throw for null input.

If one catalog lookup uses `TryGet`, similar optional lookups should also use `TryGet`.

If one resolver boundary returns structured errors, related expected satisfiability failures should use the same result model.

Do not mix exceptions and result objects for the same category of failure.

## Test rules

Tests should verify both success and failure behavior.

For exception tests, verify:

```text
exception type
parameter name when meaningful
accepted valid boundary cases
rejected invalid boundary cases
```

For result tests, verify:

```text
IsSuccess / IsFailure
accepted value presence on success
error count on failure
error code
subject symbol
stable error ordering when multiple errors exist
```

Avoid tests that depend on full diagnostic message text unless the message text is an explicit public contract.

## Documentation rules

Documentation must describe the active error model.

Do not document obsolete failure behavior.

Do not describe result failures as exceptions.

Do not describe invalid API usage as expected result failure.

Future execution error policy must be marked as future architecture until implemented.

## Review checklist

Before accepting error-handling code, verify:

```text
Invalid API usage throws.
Expected symbolic satisfiability failure returns a result object.
The error is reported by the boundary that owns it.
Constructors reject invalid local state.
Catalog construction rejects invalid inventory.
Request resolution does not throw for missing valid symbols.
Plan compilation does not resolve descriptors.
Error codes are stable and specific.
Diagnostic messages are human-facing only.
Tests do not parse diagnostic messages as identity.
Multiple errors are returned in stable order.
No domain object logs instead of throwing or returning errors.
No silent fallback hides invalid input.
Future execution errors are not added to current planning objects.
```

## Summary

Use exceptions when the caller violates an API contract.

Use result objects when valid symbolic input cannot be satisfied by the current catalog.

Keep errors owned by the boundary that detects and understands them.

Keep error codes stable, messages diagnostic, and ordering deterministic.

Do not use logging, fallback, or higher-layer validation to hide invalid architecture boundaries.

```