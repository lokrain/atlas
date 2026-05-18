# Atlas architecture overview

Lokrain.Atlas is a managed Unity package for deterministic world-generation architecture.

The current Runtime model defines validated domain objects, reusable generation inventory, catalog validation, request resolution, and managed plan compilation.

Current Runtime architecture ends at `GenerationPlan`.

Execution after `GenerationPlan` is planned architecture.

## Architecture summary

Lokrain.Atlas separates generation into these boundaries:

```text
Reusable definitions
  -> Catalog validation
  -> Symbolic request descriptors
  -> Request resolution
  -> Accepted generation requests
  -> Managed plan compilation
  -> Managed generation plans
  -> Planned runnable execution
```

Each boundary owns a different responsibility.

| Boundary | Responsibility |
| --- | --- |
| Definitions | Describe reusable generation inventory. |
| Catalog | Own and validate accepted definition graphs. |
| Descriptor | Represent symbolic caller intent. |
| Resolver | Convert symbolic intent into accepted run intent. |
| Request | Represent one accepted resolved generation run. |
| Plan compiler | Compile accepted run intent into a managed semantic plan. |
| Plan | Represent current managed semantic generation order. |
| Future execution | Compile runnable metadata, allocate storage, schedule jobs, and capture outputs. |

## Current Runtime scope

Current Runtime includes:

```text
Core values
Generation schemas
Semantic resource definitions
Stage kinds and operation kinds
Stage definitions, routes, route steps, and contracts
Operation definitions, contracts, and implementation definitions
Generation recipes
Generation catalogs
Generation run settings
Generation request descriptors
Operation implementation override descriptors
Generation request resolution
Generation requests
Generation plan compilation
Generation plans
Stage plan nodes
Operation plan nodes
```

Current Runtime does not include:

```text
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
native storage allocation
job scheduling
Burst execution
artifact capture
ECS execution integration
```

## Primary flow

The current managed flow is:

```text
GenerationCatalog
  + GenerationRequestDescriptor
    -> GenerationRequestResolver
    -> GenerationRequestResolutionResult
    -> GenerationRequest
    -> GenerationPlanCompiler
    -> GenerationPlan
```

The catalog provides accepted reusable inventory.

The descriptor provides symbolic caller intent.

The resolver produces either an accepted request or structured resolution errors.

The compiler produces a managed semantic plan.

The plan is the current Runtime endpoint.

## Core values

Core values represent validated primitive domain data.

Current core values include:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed
```

A core value owns its own local invariants.

Examples:

```text
Symbol validates stable machine-facing identity text.
DisplayName validates user-facing metadata text.
Grid validates width, depth, cell count, and coordinate/index conversion.
Seed represents deterministic generation input.
```

Core values do not know catalogs, recipes, requests, plans, Unity objects, native storage, or jobs.

## Definitions

Definitions describe reusable package inventory.

Current definition types include:

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

Definitions do not represent one generation run.

Definitions must not contain run-specific settings, native storage, job handles, scheduler state, or Unity object identity.

## Resources

`ResourceDefinition` describes the semantic identity of a generated value.

Resources are used by stage and operation contracts to describe semantic resource flow.

Examples:

```text
ContinentSuitability
ContinentCandidate
MainContinent
ContinentalLandmassArea
BaseElevation
```

A resource is not storage.

A resource definition does not describe field shape, native container layout, allocation, scheduling, or artifact capture.

Storage-facing metadata is planned as `FieldDefinition`.

## Contracts

Contracts describe semantic input/output flow.

Current contract types are:

```text
StageContract
OperationContract
```

Contracts use `ResourceDefinition` inputs and outputs.

Correct:

```text
OperationContract
  requires ContinentSuitability
  produces ContinentCandidate
```

Incorrect:

```text
OperationContract
  requires NativeArray<float>
  produces FieldHandle
```

Contracts are managed planning metadata, not storage or execution metadata.

## Stages and operations

A stage is a coarse generation phase.

An operation is a semantic work unit inside a stage route.

A stage route defines ordered operation occurrences for satisfying a stage.

A route step is an operation occurrence and has its own symbol.

This allows the same operation definition to appear multiple times in a route while still allowing per-occurrence implementation choices.

```text
StageDefinition
  -> StageRouteDefinition
    -> StageRouteStepDefinition
      -> OperationDefinition symbol
```

The route step stores an operation-definition symbol. The accepted operation binding is established later through recipe choices, catalog validation, request resolution, and planning.

## Implementations

`OperationImplementationDefinition` describes a selectable implementation for an operation definition.

An implementation definition identifies an implementation option. It does not execute work by itself.

Execution is planned after managed plan compilation.

Correct:

```text
OperationImplementationDefinition
  OperationDefinition: ExtractMainContinent
  Symbol: lokrain.atlas.landmass.implementation.extract_main_continent.default
```

Incorrect:

```text
OperationImplementationDefinition owns NativeArray<T>.
OperationImplementationDefinition schedules JobHandle.
```

## Catalogs

`GenerationCatalog` is immutable accepted inventory.

It owns accepted definition instances and validates graph consistency.

Catalog ownership is reference-exact.

A definition belongs to a catalog only when that exact object instance is owned by the catalog.

Symbol-equivalent definitions are not interchangeable.

The catalog validates:

```text
definition uniqueness
schema consistency
resource ownership
stage ownership
route ownership
route-step consistency
operation ownership
implementation compatibility
contract resource ownership
recipe graph consistency
cross-catalog reference rejection
```

The catalog does not represent one generation run.

## Recipes

`GenerationRecipeDefinition` is a reusable generation template.

A recipe selects:

```text
generation schema
stage route choices
default route-step implementation choices
```

A recipe does not contain:

```text
Grid
Seed
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native storage
job scheduling
```

The same recipe can be used for many runs.

## Descriptors

Descriptors represent symbolic caller intent.

Current descriptor types include:

```text
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
```

A request descriptor contains:

```text
recipe symbol
run settings
implementation override descriptors
```

Descriptors may contain symbols that are valid text but unresolved for a specific catalog.

A descriptor is not an accepted request.

## Request resolution

`GenerationRequestResolver` converts symbolic caller intent into accepted run intent.

It uses:

```text
GenerationCatalog
GenerationRequestDescriptor
```

It returns:

```text
GenerationRequestResolutionResult
```

Resolution can fail when the descriptor cannot be satisfied by the catalog.

Expected resolution failures are returned as structured errors.

Examples:

```text
unknown recipe symbol
route step override not selected by recipe
unknown implementation symbol
implementation operation mismatch
```

Resolver failures are not catalog construction failures.

## Requests

`GenerationRequest` is accepted resolved generation intent for one run.

A request contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
final StageRouteStepImplementationChoice list
```

A request contains accepted definitions.

A request contains no unresolved symbols.

A request does not allocate storage or execute work.

## Managed plans

`GenerationPlanCompiler` compiles an accepted `GenerationRequest` into a `GenerationPlan`.

A generation plan contains:

```text
GenerationRecipeDefinition
GenerationSchemaDefinition
GenerationRunSettings
StagePlanNode list
```

A stage plan node contains:

```text
StageDefinition
StageRouteDefinition
StageContract
OperationPlanNode list
```

An operation plan node contains:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

A generation plan is managed semantic data.

It is not executable job data.

## Planned execution architecture

Execution architecture starts after `GenerationPlan`.

Planned execution concepts include:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldHandle
GenerationWorkspace
OperationScheduler
OperationScratch
native storage
Burst jobs
artifacts
execution diagnostics
ECS integration
```

The planned flow is:

```text
GenerationPlan
  + FieldDefinitionSet
  + ExecutionProfile
    -> RunnablePlanCompiler
    -> RunnablePlan
    -> GenerationWorkspace
    -> OperationScheduler
    -> Jobs
```

Future execution concepts must not be modeled as current Runtime behavior until corresponding Runtime code exists.

## Unity boundary

Current managed Runtime domain and planning objects are not Unity object wrappers.

They must not depend on:

```text
UnityEngine.Object
ScriptableObject
MonoBehaviour
GameObject
UnityEditor
ECS World
ECS System
Entity
NativeArray<T>
JobHandle
```

Unity-facing adapters may translate Unity-authored data into Atlas descriptors or accepted definitions.

Unity object identity must not define package-domain identity.

Correct:

```text
ScriptableObject authoring asset -> GenerationRequestDescriptor
Editor window -> displays GenerationCatalog
Importer -> creates accepted definitions
```

Incorrect:

```text
ScriptableObject is the canonical recipe.
GameObject name is the Symbol.
MonoBehaviour owns GenerationCatalog identity.
```

## Determinism

Deterministic generation depends on stable accepted inputs.

Stable inputs include:

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

Do not use these as deterministic identity:

```text
DisplayName
Unity object instance ID
Unity asset path
managed object reference identity
process-local string hash code
current time
global random state
editor selection state
```

Symbols are identity.

Display names are metadata.

## Error boundaries

Lokrain.Atlas uses exceptions for invalid API usage and result objects for expected boundary failures.

Examples:

| Scenario | Boundary |
| --- | --- |
| Null required argument | Exception |
| Invalid symbol text | Exception |
| Invalid grid dimension | Exception |
| Invalid catalog inventory | Exception |
| Unknown recipe symbol during request resolution | Failed result |
| Unknown implementation override symbol | Failed result |

Catalog construction failures throw.

Descriptor resolution failures return `GenerationRequestResolutionResult`.

## Built-in landmass module

The current built-in generation module is landmass.

It provides built-in definitions for:

```text
world generation schema
landmass resources
landmass stage kind
landmass stage definition
landmass stage route
landmass route steps
landmass stage contract
landmass operation kinds
landmass operation definitions
landmass operation contracts
landmass operation implementations
landmass generation recipe
landmass request factory
landmass catalog factory
```

The landmass module follows the same Runtime boundaries as custom generation modules.

## Architecture principles

Use accepted domain objects instead of primitive bags.

Use symbols for stable identity.

Use display names only as user-facing metadata.

Use resource definitions for semantic resource flow.

Use catalogs for accepted inventory and graph validation.

Use descriptors for symbolic caller intent.

Use resolvers for catalog-dependent symbolic resolution.

Use requests for accepted resolved run intent.

Use plan compilers for managed semantic plans.

Keep current managed Runtime separate from planned execution.

Keep Unity adapters outside canonical Runtime domain identity.

## Summary

Lokrain.Atlas current Runtime architecture is managed and semantic.

It defines accepted values, definitions, catalogs, descriptors, requests, and managed plans.

It does not execute generation jobs.

Execution is planned after `GenerationPlan` through runnable compilation, workspace ownership, scheduler control, and jobs.