# Accepted domain object model

Lokrain.Atlas uses accepted domain objects to keep validation, ownership, and generation semantics explicit.

An accepted object has already validated the invariants it owns. Downstream code may rely on those invariants without repeating the same checks.

Accepted objects are not necessarily globally valid in every context. A catalog can still reject an individually valid definition when it violates catalog ownership, uniqueness, schema membership, or graph consistency.

## Object categories

Atlas Runtime uses these domain object categories:

| Category | Purpose |
| --- | --- |
| Core value object | Validated reusable value with package-owned semantics. |
| Definition | Accepted reusable package inventory. |
| Descriptor | Symbolic caller intent before catalog resolution. |
| Result object | Structured success/failure result for an expected boundary failure. |
| Request | Accepted resolved intent for one generation run. |
| Plan | Accepted managed semantic plan for one generation run. |
| Builder | Mutable assembly surface that creates an accepted immutable object. |
| Module surface | Static package surface that exposes built-in definitions and factories. |

These categories are intentionally separate. Do not collapse them into one generic model type.

## Accepted object rule

A non-null accepted reference object must be valid for the invariants owned by its type.

Examples:

```text
Symbol
DisplayName
Grid
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
StageContract
OperationDefinition
OperationContract
GenerationRecipeDefinition
GenerationRunSettings
GenerationRequest
GenerationPlan
````

The object’s constructor or factory owns this guarantee.

A caller should not need to ask whether a constructed `ResourceDefinition` has a null symbol, whether a constructed `GenerationRunSettings` has a null grid, or whether a constructed `StageContract` contains null resources.

Those checks belong at construction.

## Struct default rule

Value-type defaults must be intentionally valid or intentionally inaccessible.

Current map value structs use valid defaults:

```text
default(Cell)      -> Cell(0, 0)
default(CellIndex) -> CellIndex(0)
default(Seed)      -> Seed(0)
```

`Cell` and `CellIndex` values are grid-relative. They are valid inside a specific grid only after grid validation.

A `Cell` is not proof that the coordinate belongs to every possible grid. A `CellIndex` is not proof that the index belongs to every possible grid.

Grid membership is owned by `Grid`.

## Core value objects

Core value objects define package-level primitives and their local invariants.

Examples:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed
```

Core value objects must not depend on catalog, recipes, request resolution, planning, Unity objects, native containers, schedulers, or jobs.

Core value objects should be small and semantically precise. They should exist when the package owns meaning, validation, formatting, deterministic behavior, or conversion rules.

Do not wrap primitive values only to rename them.

## Symbol

`Symbol` is stable machine-facing identity.

A symbol is used for lookup, catalog identity, descriptor resolution, tests, tooling, logs, and artifact compatibility.

A symbol is not a display name, Unity object name, file path, runtime numeric ID, native handle, or scheduler binding.

Objects with symbol-based identity should compare by symbol.

## DisplayName

`DisplayName` is validated user-facing text.

A display name is metadata for humans. It is not identity and must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.

Objects may expose both `Symbol` and `DisplayName`. The symbol owns identity. The display name owns presentation.

## Grid-owned coordinates and indices

`Grid` owns coordinate and flattened-index validation.

`Cell` and `CellIndex` are created by `Grid` after bounds validation.

Correct ownership:

```text
Grid -> validates coordinates
Grid -> creates Cell
Grid -> validates flattened index
Grid -> creates CellIndex
```

Incorrect ownership:

```text
Cell -> validates against all grids
CellIndex -> knows a grid
operation -> hand-rolls grid bounds
job -> receives unresolved grid coordinates from a descriptor
```

Grid conversion and bounds logic belongs in `Grid`.

## Definitions

Definitions describe accepted reusable package inventory.

Examples:

```text
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract
OperationDefinition
OperationContract
OperationImplementationDefinition
GenerationRecipeDefinition
```

Definitions are managed metadata. They are not one generation run and not execution state.

Definitions must not own:

```text
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
NativeArray<T>
NativeList<T>
JobHandle
scheduler state
workspace allocation
```

A definition can be individually valid while still not accepted into a catalog. Catalog acceptance is a higher-level boundary.

## Definition identity

Definitions that expose a symbol use that symbol as their stable identity.

Examples:

```text
GenerationSchemaDefinition.Symbol
ResourceDefinition.Symbol
StageDefinition.Symbol
StageRouteDefinition.Symbol
StageRouteStepDefinition.Symbol
OperationDefinition.Symbol
OperationImplementationDefinition.Symbol
GenerationRecipeDefinition.Symbol
```

Kind objects also expose symbol identity:

```text
StageKind.Symbol
OperationKind.Symbol
```

Identity must not be based on display name, object allocation order, collection position, or Unity asset identity.

## Catalog-dependent validity

Some validity cannot be proven by a definition constructor.

Catalog-dependent validity includes:

```text
definition symbol uniqueness within the catalog
schema ownership consistency
route ownership consistency
route-step membership
operation implementation compatibility
contract resource ownership
recipe graph consistency
cross-catalog object reuse
```

`GenerationCatalog` owns these checks.

Do not push catalog graph validation into low-level definitions.

## Descriptor objects

Descriptors are valid symbolic input objects.

Examples:

```text
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
```

A descriptor validates its own structure. It does not prove that its symbols exist in a catalog.

A descriptor may contain:

```text
Symbol
GenerationRunSettings
other descriptor objects
```

A descriptor must not contain:

```text
GenerationRecipeDefinition
StageRouteStepDefinition
OperationImplementationDefinition
GenerationPlan
native containers
field handles
job handles
```

Descriptors are the correct boundary for user-authored symbolic intent, editor tooling input, serialized request intent, and tests that exercise resolution failure.

## Descriptor validity

Descriptor validity means:

```text
required descriptor values are present
symbols are syntactically valid
settings are accepted
descriptor collections contain no null entries
descriptor collections do not violate local uniqueness rules
```

Descriptor validity does not mean:

```text
the recipe symbol exists
the override target exists in the selected recipe
the implementation symbol exists
the implementation can execute the target operation
the catalog can satisfy the request
```

Catalog satisfiability belongs to request resolution.

## Result objects

Result objects represent expected failure at a domain boundary.

Current example:

```text
GenerationRequestResolutionResult
```

A result object contains either a successful accepted value or structured errors.

Resolution result success contains:

```text
GenerationRequest
```

Resolution result failure contains:

```text
GenerationRequestResolutionError list
```

A result object must not contain both success data and errors.

A failed result must contain at least one error.

## Structured errors

Structured errors describe expected domain failure.

Current example:

```text
GenerationRequestResolutionError
```

A resolution error contains:

```text
Code
Message
SubjectSymbol
```

The code is stable machine-facing identity.

The message is human-facing diagnostic text.

The subject symbol identifies the primary related descriptor or catalog symbol when one exists.

Do not parse diagnostic messages in tests or tooling. Use error codes and subject symbols.

## Request objects

`GenerationRequest` is accepted resolved intent for one generation run.

A request contains accepted definitions and final choices:

```text
GenerationRecipeDefinition
GenerationRunSettings
StageRouteStepImplementationChoice list
```

A request contains no unresolved symbols.

A request is not a descriptor and not a plan.

A request must not contain native storage, field definitions, scheduler bindings, job handles, dependency handles, or Burst job data.

## Plan objects

`GenerationPlan` is accepted managed semantic data for one generation run.

A plan contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
StagePlanNode list
```

Stage and operation plan nodes contain accepted definitions and contracts selected for the run.

A plan is managed planning output. It is not executable runtime state.

A plan must not contain:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
FieldDefinition
FieldHandle
SchedulerBinding
JobHandle
ECS entity references
Burst function pointers
```

## Builder objects

A builder is mutable by design.

Current example:

```text
GenerationCatalogBuilder
```

A builder collects candidate objects and creates an accepted immutable object.

The builder is not the accepted object. Its internal mutable state must not leak as catalog state.

Correct ownership:

```text
GenerationCatalogBuilder -> mutable assembly
GenerationCatalog        -> accepted immutable inventory
```

A builder may accept repeated calls in authoring order, but final catalog construction must validate uniqueness, ownership, and consistency.

## Module surfaces

A generation module surface exposes built-in definitions and request helpers.

Current example:

```text
Lokrain.Atlas.Generation.Landmass
```

Module surfaces may expose:

```text
built-in schemas
resource definitions
stage kinds
stage definitions
stage contracts
operation kinds
operation definitions
operation contracts
operation implementations
routes
route steps
recipes
catalog factories
request descriptor factories
```

A module surface must not bypass the accepted object model.

A helper may reduce boilerplate, but it must still produce descriptors, definitions, catalogs, requests, or plans through the same domain boundaries as handwritten code.

## Construction rules

Constructors and factories must validate all invariants owned by the constructed type.

They should:

```text
reject null required values
copy enumerable input before storing it
reject null entries in copied collections
reject duplicates when uniqueness is required
normalize values when the type owns normalization
store read-only collection views
throw precise argument exceptions for invalid API usage
```

They should not:

```text
store caller-owned mutable collections directly
defer local invariant validation to later systems
perform catalog lookup unless they are catalog/resolver APIs
allocate native containers
schedule jobs
read Unity scene state
use display names as identity
```

## Collection ownership

Accepted objects that expose collections must own their collection snapshots.

Correct pattern:

```text
copy input
validate copied values
store private array or read-only collection
expose IReadOnlyList<T>
```

Incorrect pattern:

```text
store incoming List<T>
expose mutable List<T>
trust caller-owned arrays
allow null collection entries
allow duplicate semantic entries when uniqueness is required
```

Collection order is part of the object contract only when the domain says order matters.

Examples where order matters:

```text
route steps
stage choices
operation plan nodes
stage plan nodes
resolution errors
```

Examples where lookup uniqueness matters:

```text
catalog definitions by symbol
resource definitions by symbol
operation implementations by symbol
override descriptors by target route-step symbol
```

## Equality rules

Equality must match domain identity.

Use symbol equality for symbol-identified definitions.

Use structural equality for small value objects and plan/request objects where all owned values define the identity.

Use sequence equality when ordered collections are part of identity.

Do not use reference equality as public semantic equality unless object identity is the intended domain meaning.

Do not use display name, Unity object ID, asset path, or allocation order as equality identity.

## Exception boundary

Throw exceptions for invalid API usage and violated local invariants.

Examples:

```text
null required argument
invalid symbol text
invalid display name text
invalid grid dimensions
out-of-range grid coordinate
duplicate constructor entries where uniqueness is required
null entry in constructor collection
attempting to create an impossible success/failure result shape
```

Exceptions are correct when the caller passed invalid data to an API that requires accepted data.

## Result boundary

Return result objects for expected domain failure at a satisfiability boundary.

Current example:

```text
GenerationRequestResolver.Resolve(...)
```

Expected resolution failures include:

```text
requested recipe symbol is not found
override route-step symbol is not found in the selected recipe
implementation symbol is not found
implementation does not belong to the operation used by the targeted route step
descriptor cannot be satisfied by the selected catalog
```

These are not invalid API usage. They are normal negative outcomes for symbolic input.

## Ownership boundary

Every accepted object has a clear owner.

| Object            | Owner                                                                 |
| ----------------- | --------------------------------------------------------------------- |
| Core value object | The value itself owns local validation.                               |
| Definition        | Its constructor owns local invariants; catalog owns graph acceptance. |
| Descriptor        | Descriptor constructor or factory owns local symbolic shape.          |
| Resolution result | Resolver owns creation of success/failure results.                    |
| Request           | Resolver owns normal creation from catalog-satisfied input.           |
| Plan              | Plan compiler owns normal creation from accepted request.             |
| Native storage    | Future workspace.                                                     |
| Job scheduling    | Future scheduler.                                                     |

Do not make lower-level objects reach upward into owners.

Examples:

```text
ResourceDefinition must not inspect GenerationCatalog.
StageContract must not allocate field storage.
GenerationRequestDescriptor must not resolve recipe symbols.
GenerationPlan must not schedule jobs.
Job must not inspect GenerationPlan.
```

## Accepted graph rule

Accepted object graphs should flow in one direction:

```text
core values
    -> definitions
    -> catalog
    -> descriptor resolution
    -> request
    -> plan
    -> future runnable metadata
    -> future workspace and scheduler execution
```

A lower layer must not depend on a higher layer.

A higher layer may contain accepted lower-layer objects when that is its purpose.

## Resource contract objects

`StageContract` and `OperationContract` use `ResourceDefinition` objects.

They represent semantic resource requirements and production.

They do not represent storage layout, field allocation, scheduler binding, or job dependencies.

Correct contract model:

```text
StageContract.RequiredInputs  -> ResourceDefinition list
StageContract.ProducedOutputs -> ResourceDefinition list

OperationContract.RequiredInputs  -> ResourceDefinition list
OperationContract.ProducedOutputs -> ResourceDefinition list
```

Incorrect contract model:

```text
contract owns raw resource symbol lists
contract owns field definitions
contract owns NativeArray<T>
contract owns scheduler bindings
contract owns job dependency handles
```

## Unity boundary

Accepted domain objects are package domain objects.

They should not become Unity object wrappers.

Current Runtime managed domain objects must remain independent from:

```text
UnityEngine.Object
ScriptableObject
MonoBehaviour
GameObject
UnityEditor
ECS World
ECS System
NativeContainer allocation
Job scheduling
```

Unity-facing APIs may adapt data into accepted domain objects. They must not replace the accepted model.

## Determinism boundary

Accepted domain objects may participate in deterministic generation semantics only through stable domain values.

Stable deterministic values include:

```text
Symbol
Grid
Seed
GenerationRunSettings
accepted recipe choices
accepted implementation choices
GenerationRequest
GenerationPlan ordering
```

Do not base deterministic behavior on:

```text
DisplayName
Unity object instance ID
Unity asset path
managed object allocation order
dictionary enumeration order
process-local string hash codes
current time
editor selection state
scene hierarchy order
```

## Design checklist

A new type should be an accepted domain object when all of these are true:

```text
the package owns its meaning
the package owns its invariants
downstream systems should trust constructed instances
the type improves correctness more than it adds ceremony
the type has stable naming and clear ownership
```

A new type should not be added when it only:

```text
renames a primitive without adding domain meaning
wraps a Unity concept without owning an invariant
duplicates an existing accepted boundary
mixes descriptor and accepted object responsibilities
mixes planning and execution responsibilities
```

## Summary

Accepted domain objects keep Atlas boundaries explicit.

Descriptors express symbolic intent.

Definitions describe accepted reusable inventory.

Catalogs accept and own definition graphs.

Resolvers convert symbolic descriptors into accepted requests.

Plans describe managed semantic work.

Future execution systems own runnable metadata, native storage, scheduling, and jobs.

Each object should validate what it owns, expose only accepted state, and avoid reaching across boundaries it does not own.

```