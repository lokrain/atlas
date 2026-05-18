# Accepted domain object model

This article explains the accepted domain object model used by Lokrain.Atlas.

An accepted domain object is an object that has validated the invariants owned by its own type before callers can observe it.

## Model summary

Lokrain.Atlas uses explicit domain objects instead of loose dictionaries, primitive strings, mutable bags, or Unity object identity.

The model separates:

```text
primitive caller input
  -> accepted values
  -> accepted definitions
  -> accepted catalogs
  -> symbolic descriptors
  -> accepted requests
  -> managed plans
```

Each step has a specific owner and validation boundary.

## Accepted object

An accepted object is valid for the rules owned by its own type.

Examples:

```text
Symbol
DisplayName
Grid
Seed
ResourceDefinition
StageContract
OperationContract
GenerationRecipeDefinition
GenerationRequest
GenerationPlan
```

A non-null accepted object should not require callers to ask whether the object is locally valid.

Correct:

```text
Symbol symbol = Symbol.Create("lokrain.atlas.world");
Grid grid = new(256, 256);
GenerationRunSettings settings = new(grid, new Seed(123UL));
```

Incorrect:

```text
Symbol symbol = new();
symbol.Value = "lokrain.atlas.world";
symbol.ValidateLater();
```

## Local validation

Each type validates only the invariants it owns.

Examples:

| Type | Owns |
| --- | --- |
| `Symbol` | Symbol syntax and normalized identity text. |
| `DisplayName` | User-facing text validity. |
| `Grid` | Width, depth, cell count, and index boundaries. |
| `Seed` | Stable seed value and seed parsing. |
| `ResourceDefinition` | Resource symbol, display name, and schema references are locally valid. |
| `StageContract` | Stage contract inputs and outputs are locally valid. |
| `OperationContract` | Operation contract inputs and outputs are locally valid. |
| `GenerationRequest` | Final selected implementation choices are accepted and complete. |
| `GenerationPlan` | Managed plan nodes are accepted semantic plan data. |

Higher-level invariants stay with higher-level owners.

Examples:

| Invariant | Owner |
| --- | --- |
| Catalog ownership | `GenerationCatalog` |
| Cross-definition graph consistency | `GenerationCatalog` |
| Descriptor satisfiability | `GenerationRequestResolver` |
| Managed stage ordering | `GenerationPlanCompiler` |
| Native allocation | Planned `GenerationWorkspace` |
| Job scheduling | Planned `OperationScheduler` |

## Constructors and factories

Use constructors when all required inputs are already accepted objects or simple values with direct range validation.

Use factories when primitive input requires parsing, normalization, or validation.

Examples:

```text
new Grid(width, depth)
new ResourceDefinition(symbol, displayName, schema)
Symbol.Create(value)
DisplayName.Create(value)
Seed.Parse(value)
```

Use `TryCreate` or `TryParse` for non-throwing validation paths.

Examples:

```text
Symbol.TryCreate(value, out Symbol? symbol)
DisplayName.TryCreate(value, out DisplayName? displayName)
Seed.TryParse(value, out Seed seed)
```

## Definitions

Definitions describe reusable package inventory.

Definitions do not represent one generation run.

Current definition objects include:

```text
GenerationSchemaDefinition
ResourceDefinition
StageKind
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
OperationKind
OperationDefinition
OperationImplementationDefinition
GenerationRecipeDefinition
```

Definitions may reference other accepted definitions when the relationship is part of reusable inventory.

Examples:

```text
StageDefinition references GenerationSchemaDefinition.
OperationDefinition references GenerationSchemaDefinition and OperationKind.
StageRouteDefinition references StageDefinition.
OperationImplementationDefinition references OperationDefinition.
GenerationRecipeDefinition references selected route and implementation choices.
```

Definitions must not contain run-specific state.

Incorrect:

```text
StageDefinition contains Grid.
OperationDefinition contains Seed.
GenerationRecipeDefinition contains GenerationRunSettings.
ResourceDefinition contains FieldHandle.
```

## Contracts

Contracts describe semantic resource flow.

Current contract objects are:

```text
StageContract
OperationContract
```

Contracts use `ResourceDefinition` for required inputs and produced outputs.

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

Contracts do not define storage layout.

## Catalogs

`GenerationCatalog` is accepted immutable inventory.

It validates definition ownership and graph consistency across the definitions it exposes.

Catalog ownership is reference-exact.

A symbol-equivalent definition is not catalog-owned unless it is the exact instance owned by the catalog.

Correct:

```text
Catalog owns ResourceDefinition instance A.
OperationContract references ResourceDefinition instance A.
```

Incorrect:

```text
Catalog owns ResourceDefinition instance A.
OperationContract references ResourceDefinition instance B.
A.Symbol == B.Symbol.
```

Instance B is not catalog-owned.

## Descriptors

Descriptors represent symbolic caller intent before catalog resolution.

Current descriptor objects include:

```text
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
```

Descriptors may contain unresolved symbols.

Descriptors validate their own structure, not whether their symbols exist in a specific catalog.

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
  StageRouteStepDefinition
  OperationImplementationDefinition
```

A descriptor is not an accepted request.

## Result objects

Result objects represent expected boundary outcomes.

Current result objects include:

```text
GenerationRequestResolutionResult
GenerationRequestResolutionError
```

Request resolution can fail because symbolic caller intent cannot be satisfied by a catalog.

Those failures are expected boundary outcomes and are represented as result errors.

Examples:

```text
unknown recipe symbol
override route step not selected by recipe
unknown implementation symbol
implementation operation mismatch
```

## Accepted requests

`GenerationRequest` is accepted resolved generation intent for one run.

A request contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
StageRouteStepImplementationChoice list
```

A request contains accepted definitions and final implementation choices.

A request contains no unresolved symbols.

A request does not execute work.

## Managed plans

`GenerationPlan` is accepted managed semantic planning output.

A plan contains:

```text
GenerationRecipeDefinition
GenerationSchemaDefinition
GenerationRunSettings
StagePlanNode list
```

A plan is not executable job data.

A plan does not contain:

```text
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
Entity
UnityEngine.Object
```

Current Runtime architecture ends at `GenerationPlan`.

## Plan nodes

Plan nodes are compiler-created accepted domain objects.

`StagePlanNode` represents one selected stage in a managed plan.

`OperationPlanNode` represents one selected operation occurrence in a managed plan.

Plan nodes describe semantic order and selected metadata. They do not execute work.

## Identity

Symbols are stable machine-facing identity.

Display names are user-facing metadata.

Definitions normally use symbol-based equality.

Examples:

```text
ResourceDefinition equality uses Symbol.
StageDefinition equality uses Symbol.
OperationDefinition equality uses Symbol.
GenerationRecipeDefinition equality uses Symbol.
```

Do not use display names for equality, lookup, deterministic identity, or artifact compatibility.

## Immutability

Accepted domain objects should be immutable.

Constructors and factories that accept collections must snapshot the input.

Accepted objects should expose read-only views.

Correct:

```text
copy input collection
validate copied values
store private collection
expose IReadOnlyList<T>
```

Incorrect:

```text
store caller-owned List<T>
expose mutable List<T>
allow caller mutation after construction
```

Mutable objects must have explicit mutable ownership.

Current example:

```text
GenerationCatalogBuilder
```

## Error boundaries

Invalid API usage throws exceptions.

Expected descriptor-resolution failure returns a result object.

Examples:

| Scenario | Boundary |
| --- | --- |
| Null required argument | Exception |
| Invalid symbol text in `Create` | Exception |
| Invalid grid dimension | Exception |
| Invalid catalog inventory | Exception |
| Unknown recipe symbol during resolution | Failed result |
| Unknown override implementation symbol | Failed result |

## Current and future boundary

Accepted domain objects in current Runtime are managed semantic objects.

They do not allocate native storage, schedule jobs, own field handles, or integrate with ECS execution.

Future execution concepts include:

```text
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
native storage
Burst jobs
artifacts
ECS integration
```

These concepts start after the current managed plan boundary.

## Summary

Accepted domain objects make invalid local state unrepresentable after construction.

Definitions describe reusable inventory.

Catalogs validate accepted definition graphs.

Descriptors express symbolic caller intent.

Resolvers produce accepted requests.

Plan compilers produce accepted managed plans.

Execution architecture is planned after `GenerationPlan`.