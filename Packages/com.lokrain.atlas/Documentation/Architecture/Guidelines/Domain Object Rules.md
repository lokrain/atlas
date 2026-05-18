# Domain object rules

This document defines rules for Lokrain.Atlas domain objects.

Domain objects include core values, definitions, contracts, descriptors, result objects, requests, plans, and plan nodes.

## Domain object model

Lokrain.Atlas uses accepted domain objects.

An accepted domain object validates the invariants owned by its own type before it can be observed by callers.

Accepted objects are ordinary managed C# objects. They are not Unity objects, native storage owners, job data, or ECS components.

## Object categories

| Category | Purpose |
| --- | --- |
| Core value | Represents validated primitive domain data. |
| Definition | Represents reusable package inventory. |
| Contract | Represents semantic resource flow. |
| Descriptor | Represents symbolic caller intent. |
| Result object | Represents expected boundary success or failure. |
| Accepted request | Represents resolved generation intent for one run. |
| Managed plan | Represents semantic execution order before runnable compilation. |
| Plan node | Represents one selected stage or operation inside a managed plan. |

## Accepted object rules

An accepted object must be valid after construction.

Constructors and factories must reject invalid local state.

Accepted objects must not expose partially valid instances.

Accepted objects must not rely on later mutation to become valid.

Correct:

```text
new Grid(width, depth)
Symbol.Create(value)
DisplayName.Create(value)
new ResourceDefinition(symbol, displayName, schema)
new GenerationRequest(recipe, runSettings, choices)
```

Incorrect:

```text
new ResourceDefinition()
resource.Symbol = symbol
resource.Schema = schema
resource.ValidateLater()
```

## Constructor rules

A constructor should be public when all required state is already expressed as accepted objects.

A constructor should throw for invalid local state.

A constructor should not perform catalog lookup, descriptor resolution, native allocation, Unity object lookup, file I/O, or job scheduling.

Correct:

```text
ResourceDefinition constructor validates null arguments.
StageContract constructor validates local resource-flow invariants.
GenerationRequest constructor validates selected implementation choices.
```

Incorrect:

```text
ResourceDefinition constructor searches a catalog.
GenerationRecipeDefinition constructor allocates native fields.
GenerationPlan constructor schedules jobs.
```

## Factory rules

Use factories when inputs are not already accepted objects.

Factories may parse, normalize, or validate primitive caller input.

Correct:

```text
Symbol.Create(string?)
DisplayName.Create(string?)
OperationKind.Create(string?)
GenerationRequestDescriptor.Create(string?, GenerationRunSettings)
```

Incorrect:

```text
CreateFromSceneObject(GameObject)
CreateAndRegisterInGlobalCatalog(...)
CreateAndSchedule(...)
```

Unity authoring adapters may provide Unity-facing factory helpers, but core Runtime domain objects must remain Unity-independent.

## Try factory rules

Use `TryCreate` or `TryParse` when callers commonly need non-throwing validation.

`TryCreate` and `TryParse` return `false` for invalid input and set the out value to `null` or `default`.

They must not throw for ordinary invalid caller input.

They may still throw for programmer errors only when a type explicitly documents that behavior.

Correct:

```text
Symbol.TryCreate(value, out Symbol? symbol)
Seed.TryParse(value, out Seed seed)
```

Incorrect:

```text
TryCreate throws for empty user text.
TryParse throws for invalid numeric input.
```

## Validation scope

Each type validates only the invariants it owns.

Do not move graph validation into low-level objects.

Do not move catalog ownership validation into definitions.

Do not move descriptor satisfiability into descriptors.

| Invariant | Owner |
| --- | --- |
| Symbol syntax | `Symbol` |
| Display-name syntax | `DisplayName` |
| Grid dimensions | `Grid` |
| Local resource validity | `ResourceDefinition` |
| Local contract validity | `StageContract`, `OperationContract` |
| Catalog graph validity | `GenerationCatalog` |
| Descriptor satisfiability | `GenerationRequestResolver` |
| Final implementation choices | `GenerationRequest` |
| Managed plan ordering | `GenerationPlanCompiler` |

## Identity rules

Symbols provide stable machine-facing identity.

Display names are metadata.

Do not use display names for equality, lookup, deterministic identity, or artifact compatibility.

Definitions use symbol-based equality unless the type explicitly owns stronger identity rules.

Correct:

```text
ResourceDefinition equality is based on Symbol.
StageDefinition equality is based on Symbol.
GenerationRecipeDefinition equality is based on Symbol.
```

Incorrect:

```text
ResourceDefinition equality uses DisplayName.
StageDefinition equality uses object reference.
GenerationRecipeDefinition equality uses stage-route collection contents.
```

Catalog ownership is separate from equality.

A symbol-equivalent definition is not catalog-owned unless it is the exact instance owned by the catalog.

## Definition rules

Definitions describe reusable package inventory.

Definitions do not describe one generation run.

Definitions must not contain run-specific state.

Definitions may reference other accepted definitions when that relationship is part of reusable inventory.

Correct:

```text
StageDefinition references GenerationSchemaDefinition.
OperationDefinition references GenerationSchemaDefinition and OperationKind.
StageRouteDefinition references StageDefinition.
OperationImplementationDefinition references OperationDefinition.
GenerationRecipeDefinition references selected route and implementation choices.
```

Incorrect:

```text
StageDefinition references GenerationRunSettings.
OperationDefinition references NativeArray<T>.
GenerationRecipeDefinition references Seed.
ResourceDefinition references FieldHandle.
```

## Contract rules

Contracts describe semantic resource flow.

Contracts use `ResourceDefinition` for inputs and outputs.

Contracts do not define storage layout, field handles, native containers, job dependencies, scheduler bindings, or artifact capture.

Correct:

```text
OperationContract
  RequiredInputs: ContinentSuitability
  ProducedOutputs: ContinentCandidate
```

Incorrect:

```text
OperationContract
  RequiredInputSymbols
  ProducedOutputSymbols
  NativeArray<float>
  FieldHandle
```

## Descriptor rules

Descriptors represent symbolic caller intent.

Descriptors are allowed to contain unresolved symbols.

Descriptors are not accepted requests.

Descriptors should validate their own structure but must not require a catalog.

Correct:

```text
GenerationRequestDescriptor
  GenerationRecipeDefinitionSymbol
  GenerationRunSettings
  OperationImplementationOverrideDescriptor list
```

Incorrect:

```text
GenerationRequestDescriptor
  GenerationRecipeDefinition
  OperationImplementationDefinition
  StageRouteStepDefinition
```

## Result object rules

Result objects represent expected boundary outcomes.

Use result objects when failure is a normal outcome for the boundary.

A result object must make success and failure explicit.

Correct:

```text
GenerationRequestResolutionResult
  Succeeded
  Failed
  GenerationRequest
  Errors
```

Incorrect:

```text
null means failure
empty request means failure
exception for unknown recipe symbol during descriptor resolution
```

## Accepted request rules

A request represents one resolved generation run.

A request contains accepted definitions, run settings, and final implementation choices.

A request must not contain unresolved symbols or override descriptors.

Correct:

```text
GenerationRequest
  GenerationRecipeDefinition
  GenerationRunSettings
  StageRouteStepImplementationChoice list
```

Incorrect:

```text
GenerationRequest
  GenerationRecipeDefinitionSymbol
  OperationImplementationOverrideDescriptor list
  unresolved route-step symbols
```

## Managed plan rules

A managed plan represents semantic execution order.

A managed plan is not executable job data.

A managed plan must not own native storage, field handles, job handles, scheduler bindings, ECS entities, or Unity objects.

Correct:

```text
GenerationPlan
  GenerationRecipeDefinition
  GenerationSchemaDefinition
  GenerationRunSettings
  StagePlanNode list
```

Incorrect:

```text
GenerationPlan
  NativeArray<T>
  FieldHandle
  JobHandle
  Entity
  ScriptableObject
```

## Plan node rules

Plan nodes are compiler-created accepted objects.

A stage plan node represents one selected stage route and its operation plan nodes.

An operation plan node represents one selected route step, operation definition, contract, and implementation definition.

Plan nodes must not perform execution.

Correct:

```text
StagePlanNode
  StageDefinition
  StageRouteDefinition
  StageContract
  OperationPlanNode list
```

```text
OperationPlanNode
  StageRouteStepDefinition
  OperationDefinition
  OperationContract
  OperationImplementationDefinition
```

Incorrect:

```text
StagePlanNode.Schedule()
OperationPlanNode.Execute()
OperationPlanNode.NativeInputBuffer
StagePlanNode.JobHandle
```

## Immutability rules

Accepted domain objects should be immutable.

A mutable type must have explicit mutable ownership.

Correct mutable object:

```text
GenerationCatalogBuilder
```

Correct immutable objects:

```text
GenerationCatalog
GenerationRecipeDefinition
GenerationRequest
GenerationPlan
StagePlanNode
OperationPlanNode
```

Incorrect:

```text
GenerationCatalog exposes mutable definition lists.
GenerationRequest allows choices to be appended after construction.
GenerationPlan allows stage nodes to be reordered after construction.
```

## Collection rules

Constructors and factories that accept collection input must snapshot the input.

They must reject null collections when the collection is required.

They must reject null entries.

They must reject duplicates when duplicates violate the type invariant.

They must expose read-only views.

Correct sequence:

```text
copy input
validate copied values
reject null entries
reject duplicates when required
store private collection
expose IReadOnlyList<T>
```

Incorrect:

```text
store caller-owned List<T>
trust lazy enumerable input
expose mutable List<T>
allow caller mutation after construction
```

## Equality rules

Equality must match the type’s identity model.

For definition objects, equality is normally symbol-based.

For run-specific objects, equality includes the accepted state that identifies the run object.

For result objects, equality includes success/failure state and contained value or errors.

Examples:

```text
ResourceDefinition -> Symbol
StageDefinition -> Symbol
OperationDefinition -> Symbol
GenerationRecipeDefinition -> Symbol
GenerationRunSettings -> Grid and Seed
GenerationRequestDescriptor -> recipe symbol, run settings, override descriptors
GenerationRequest -> recipe, run settings, final choices
GenerationPlan -> recipe, run settings, stage plan nodes
```

Do not include display names in identity unless the type explicitly defines display text as identity.

## ToString rules

`ToString()` should return a compact diagnostic summary.

Use stable domain names and stable identity values.

Do not include large collections, transient state, memory addresses, native handles, or Unity instance IDs.

Correct:

```text
GenerationRunSettings(Grid: Grid(Width: 256, Depth: 256), Seed: 123)
```

Incorrect:

```text
GenerationRunSettings@7426182
GenerationRunSettings(Grid object reference, random Unity object ID)
```

## Nullability rules

Use nullable annotations consistently.

Public APIs must communicate nullability through signatures.

Reject null required arguments with `ArgumentNullException`.

Use nullable return values only when null is a meaningful result.

Correct:

```text
public static bool TryCreate(string? value, out Symbol? symbol)
public GenerationRequest? GenerationRequest { get; }
```

Incorrect:

```text
public Symbol Create(string value) // accepts null at runtime
public object? Value // no domain meaning
```

## Runtime independence rules

Core domain objects must not depend on Unity object identity.

Do not use these as domain identity:

```text
GameObject.name
UnityEngine.Object.GetInstanceID()
asset path
scene hierarchy index
editor selection state
ScriptableObject reference identity
```

Unity-facing code may adapt Unity-authored data into domain objects.

The resulting domain identity must be expressed with package-owned values such as `Symbol`.

## Future execution exclusion

Current domain objects must not contain planned execution state.

Do not add these to current domain objects:

```text
FieldDefinition
FieldHandle
RunnablePlan
GenerationWorkspace
OperationScheduler
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
JobHandle
Entity
```

Future execution state belongs to future execution-specific types.

## Checklist

Before adding or changing a domain object, verify:

```text
The type owns one responsibility.
The constructor rejects invalid local state.
Primitive input uses a factory when validation or normalization is required.
Try factories do not throw for ordinary invalid input.
The type does not validate higher-level graph ownership.
The type snapshots collection input.
The type exposes read-only state.
The equality model matches the identity model.
DisplayName is not used as identity.
Definitions do not contain run-specific state.
Descriptors do not contain accepted definitions.
Requests do not contain unresolved symbols.
Plans do not contain native execution state.
Unity object identity is not part of domain identity.
```