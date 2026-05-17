# Catalog, recipe, request, and plan model

The managed generation model separates reusable package inventory from one generation run.

The model has four main layers:

```text
GenerationCatalog
        |
        v
GenerationRecipeDefinition
        |
        v
GenerationRequestDescriptor
        |
        v
GenerationRequest
        |
        v
GenerationPlan
````

Each layer has a different job.

| Layer              | Purpose                                          |
| ------------------ | ------------------------------------------------ |
| Catalog            | Accepted package inventory and lookup.           |
| Recipe             | Reusable generation template.                    |
| Request descriptor | Symbolic caller intent for one run.              |
| Request            | Accepted resolved generation intent for one run. |
| Plan               | Managed semantic operation order for one run.    |

Do not collapse these layers.

## Catalog

`GenerationCatalog` is the immutable accepted inventory of available generation definitions.

It owns accepted definitions for:

```text id="vexyew"
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

The catalog provides symbol lookup and validates graph consistency.

The catalog is not a request, not a recipe, not a plan, and not an execution context.

## Catalog ownership

A catalog owns the accepted definition instances it exposes.

Catalog-owned objects must reference definitions owned by the same catalog. Cross-catalog object reuse is invalid.

Correct model:

```text id="h9nydj"
catalog A
  schema A
  resource A
  stage A
  operation A
  recipe A
```

Invalid model:

```text id="4y7uoq"
catalog A
  recipe A
    references stage from catalog B
    references resource from catalog B
    references operation implementation from catalog B
```

Catalog ownership is stricter than symbol equality.

Two definitions with the same symbol are not interchangeable when they belong to different catalog instances.

## Catalog validation

The catalog validates consistency that cannot be owned by individual definitions.

Catalog-level validation includes:

```text id="yak742"
unique symbols within each definition category
schema ownership consistency
resource ownership consistency
stage ownership consistency
route and route-step consistency
operation and implementation compatibility
contract resource ownership
recipe stage-route consistency
recipe route-step implementation consistency
cross-catalog object reuse
```

Definition constructors validate local invariants.

The catalog validates object graph invariants.

## Catalog builder

`GenerationCatalogBuilder` is the mutable assembly surface for building a catalog.

The builder may collect definitions over multiple calls.

The accepted catalog is created only after catalog-level validation succeeds.

Correct responsibility split:

```text id="xup29t"
GenerationCatalogBuilder -> collect candidate definitions
GenerationCatalog        -> immutable accepted inventory
```

The builder must not be treated as the catalog.

The catalog must not expose builder-owned mutable state.

## Schema

`GenerationSchemaDefinition` defines a generation family.

A schema provides semantic context for resources, stages, operations, implementations, and recipes.

Definitions that belong to a generation family reference a schema.

A schema is not a recipe and not a run.

## Resource

`ResourceDefinition` defines the semantic identity of a generated value.

Resources are used by stage and operation contracts.

A resource answers:

```text id="hsohx8"
what generated value is required or produced
```

A resource does not answer:

```text id="nbrb6s"
how the value is stored
which native container owns it
which scheduler writes it
which job reads it
which artifact captures it
```

Storage and execution details belong to future field/workspace architecture.

## Stage

`StageDefinition` defines a semantic generation phase.

A stage belongs to a schema and has a `StageKind`.

A stage is not a route, not an operation, not a scheduler, and not a job.

A stage can have one or more routes.

## Stage route

`StageRouteDefinition` defines one ordered way to satisfy a stage.

A route owns ordered route steps.

A route belongs to one stage.

A route is selected by a recipe.

## Stage route step

`StageRouteStepDefinition` defines one operation occurrence inside a route.

A route step references an `OperationDefinition`.

The route step has its own symbol because occurrence identity matters.

This allows the same operation definition to appear multiple times in one route while preserving independent implementation choices.

Correct model:

```text id="gnmxbn"
route
  step A -> operation X
  step B -> operation X
```

`step A` and `step B` are different route-step occurrences even when they use the same operation definition.

## Operation

`OperationDefinition` defines semantic generation work.

An operation belongs to a schema and has an `OperationKind`.

An operation is not an implementation and not executable code.

An operation can have one or more implementation definitions.

## Operation implementation

`OperationImplementationDefinition` defines a selectable implementation option for an operation.

It identifies a choice, not executable job code.

Current implementation definitions are managed metadata used by recipe selection, request override resolution, and plan compilation.

Future execution architecture may bind an implementation definition to scheduler bindings and runnable operations.

## Contracts

`StageContract` and `OperationContract` declare semantic resource flow.

They use `ResourceDefinition` inputs and outputs.

Correct model:

```text id="kwldpz"
StageContract
  RequiredInputs  -> ResourceDefinition list
  ProducedOutputs -> ResourceDefinition list

OperationContract
  RequiredInputs  -> ResourceDefinition list
  ProducedOutputs -> ResourceDefinition list
```

Contracts must not use raw symbol lists for resource flow.

Contracts must not own field definitions, native containers, scheduler bindings, or job dependencies.

## Recipe

`GenerationRecipeDefinition` is a reusable generation template.

A recipe contains:

```text id="evch9i"
Symbol
DisplayName
GenerationSchemaDefinition
StageRouteChoice list
StageRouteStepImplementationChoice list
```

A recipe selects which route satisfies each stage and which default implementation satisfies each selected route step.

A recipe does not represent one generation run.

A recipe must not contain:

```text id="uqmsoi"
Grid
Seed
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native storage
scheduler state
job handles
```

The same recipe can be used by many requests with different run settings and implementation overrides.

## Stage route choice

`StageRouteChoice` binds a stage to the route selected for that stage.

It contains:

```text id="tzf9pi"
StageDefinition
StageRouteDefinition
StageContract
```

The selected route must belong to the selected stage.

The stage contract describes the semantic resources required and produced by that stage.

## Route-step implementation choice

`StageRouteStepImplementationChoice` binds a route-step occurrence to the selected implementation for that occurrence.

It contains:

```text id="iivxir"
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

The selected implementation must belong to the operation used by the route step.

The operation contract describes the semantic resources required and produced by that operation.

## Recipe defaults

A recipe owns default implementation choices for its selected route steps.

A request may override those defaults through `OperationImplementationOverrideDescriptor`.

Overrides are per route-step occurrence, not per operation definition.

Correct override target:

```text id="udibzx"
StageRouteStepDefinition.Symbol
```

Incorrect override target:

```text id="yzqv21"
OperationDefinition.Symbol
OperationKind.Symbol
OperationImplementationDefinition.Symbol only
```

Targeting the route-step occurrence is required because the same operation definition may appear multiple times in one route.

## Request descriptor

`GenerationRequestDescriptor` is symbolic caller intent for one generation run.

It contains:

```text id="z73m2g"
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

The descriptor validates its own structure.

The descriptor does not prove that the requested recipe or override symbols exist in a catalog.

A descriptor is not an accepted request.

## Operation implementation override descriptor

`OperationImplementationOverrideDescriptor` is a symbolic override.

It contains:

```text id="3itsdn"
StageRouteStepDefinitionSymbol
OperationImplementationDefinitionSymbol
```

The override says:

```text id="l0ms18"
for this selected route-step occurrence, use this implementation
```

It does not directly contain accepted route-step or implementation objects.

Resolution converts the symbolic override into an accepted `StageRouteStepImplementationChoice`.

## Run settings

`GenerationRunSettings` contains per-run deterministic settings.

Current settings include:

```text id="y66y87"
Grid
Seed
```

Run settings belong to the request descriptor and accepted request.

Run settings do not belong to the catalog or recipe.

## Request resolution

`GenerationRequestResolver` converts a descriptor into an accepted request by using a catalog.

Input:

```text id="8byzue"
GenerationCatalog
GenerationRequestDescriptor
```

Output:

```text id="1112kn"
GenerationRequestResolutionResult
```

The resolver validates that:

```text id="6asgly"
the requested recipe exists in the catalog
the selected recipe is catalog-owned
override route-step symbols exist in the selected recipe
override implementation symbols exist in the catalog
override implementations belong to the operation used by the targeted route step
the final implementation choice set is complete
```

The resolver owns catalog satisfiability.

The plan compiler should not repeat normal descriptor resolution.

## Resolution result

`GenerationRequestResolutionResult` represents either success or expected resolution failure.

Success contains:

```text id="iaq0cv"
GenerationRequest
```

Failure contains:

```text id="len019"
GenerationRequestResolutionError list
```

Expected resolution failures use structured errors instead of exceptions.

Examples:

```text id="skijhh"
missing recipe symbol
missing override target route-step symbol
missing implementation symbol
implementation incompatible with targeted route-step operation
duplicate override target
```

Invalid API usage still throws.

Examples:

```text id="p70x0y"
null catalog
null descriptor
invalid constructor arguments
```

## Accepted request

`GenerationRequest` is resolved generation intent for one run.

It contains:

```text id="c7c4rj"
GenerationRecipeDefinition
GenerationRunSettings
StageRouteStepImplementationChoice list
```

An accepted request has no unresolved symbols.

It contains the final implementation choices after applying recipe defaults and descriptor overrides.

A request is not a plan. It does not define ordered execution nodes beyond the selected recipe and final choices.

A request must not contain:

```text id="e0ldf6"
native containers
field definitions
field handles
scheduler bindings
job handles
dependency handles
Burst job data
```

## Plan compiler

`GenerationPlanCompiler` converts an accepted request into a managed plan.

Input:

```text id="4uzx3f"
GenerationRequest
```

Output:

```text id="h2dl6v"
GenerationPlan
```

The compiler expands the accepted recipe choices into ordered stage and operation plan nodes.

The compiler consumes accepted objects. It does not resolve descriptor symbols through the catalog during normal compilation.

## Generation plan

`GenerationPlan` is managed semantic generation work for one run.

It contains:

```text id="vkx5px"
GenerationRecipeDefinition
GenerationRunSettings
StagePlanNode list
```

The plan preserves explicit ordering.

A plan is still managed metadata. It is not a runnable plan and not executable job data.

## Stage plan node

`StagePlanNode` represents one selected stage in the managed plan.

It contains:

```text id="cwmtw0"
StageDefinition
StageRouteDefinition
StageContract
OperationPlanNode list
```

The stage plan node preserves the selected route and ordered operations for the stage.

## Operation plan node

`OperationPlanNode` represents one selected route-step operation in the managed plan.

It contains:

```text id="li7cdr"
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

The operation plan node preserves the route-step occurrence, operation, contract, and selected implementation.

An operation plan node is not a scheduler and not a job.

## Full managed flow

```text id="ocyzm9"
GenerationCatalog
  owns definitions and recipes

GenerationRecipeDefinition
  selects stage routes and default route-step implementations

GenerationRequestDescriptor
  names recipe symbol and override symbols

GenerationRequestResolver
  resolves descriptor through catalog

GenerationRequest
  contains accepted recipe, settings, and final implementation choices

GenerationPlanCompiler
  expands request into ordered managed plan nodes

GenerationPlan
  contains semantic managed work for one run
```

## Current execution boundary

The managed model ends at `GenerationPlan`.

The following concepts are outside the current implemented managed model:

```text id="xfzrxh"
FieldDefinition
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableOperation
SchedulerBinding
GenerationWorkspace
FieldHandle
OperationScheduler
NativeArray<T>
JobHandle
Burst jobs
```

Do not add these concepts to catalog, recipe, request, or plan objects as current state.

## Correct dependencies

Correct dependency direction:

```text id="9xa7p8"
core values
  -> definitions
  -> catalog
  -> request descriptor
  -> resolver
  -> request
  -> plan compiler
  -> plan
```

Definitions may reference core values.

Catalogs may reference definitions.

Descriptors may reference symbols and run settings.

Resolvers may reference catalogs and descriptors.

Requests may reference accepted recipe choices and run settings.

Plans may reference accepted request data.

Lower layers must not reference higher layers.

## Incorrect dependencies

Do not introduce these dependencies:

```text id="2jbp7y"
ResourceDefinition -> GenerationCatalog
StageContract -> FieldDefinition
OperationContract -> NativeArray<T>
GenerationRecipeDefinition -> GenerationRunSettings
GenerationCatalog -> GenerationRequest
GenerationRequestDescriptor -> GenerationCatalog
GenerationRequest -> GenerationPlan
GenerationPlan -> RunnablePlan
GenerationPlan -> JobHandle
OperationPlanNode -> NativeContainer
Job -> Symbol
Job -> GenerationCatalog
```

Each dependency crosses an ownership boundary incorrectly.

## Object identity summary

| Object                              | Identity                         |
| ----------------------------------- | -------------------------------- |
| `Symbol`                            | Text value.                      |
| `DisplayName`                       | Text value, presentation only.   |
| `GenerationSchemaDefinition`        | Symbol.                          |
| `ResourceDefinition`                | Symbol.                          |
| `StageDefinition`                   | Symbol.                          |
| `StageRouteDefinition`              | Symbol.                          |
| `StageRouteStepDefinition`          | Symbol.                          |
| `OperationDefinition`               | Symbol.                          |
| `OperationImplementationDefinition` | Symbol.                          |
| `GenerationRecipeDefinition`        | Symbol.                          |
| `GenerationRequestDescriptor`       | Structural symbolic intent.      |
| `GenerationRequest`                 | Accepted run intent.             |
| `GenerationPlan`                    | Accepted managed plan structure. |

Display names must not define identity.

Unity object identity must not define package-domain identity.

## Ordering summary

Ordering is explicit where order affects generation semantics.

Ordered objects include:

```text id="4wme71"
route steps
stage route choices
route-step implementation choices
stage plan nodes
operation plan nodes
resolution errors
```

Unordered lookup data must not leak nondeterministic enumeration order into plans.

When an unordered collection is transformed into ordered output, the ordering rule must be explicit and stable.

## Resource-flow summary

Resource flow is semantic in the current model.

```text id="xas4dy"
ResourceDefinition
  -> StageContract
  -> OperationContract
  -> GenerationPlan
```

The current model says which semantic values are required or produced.

The future execution model will decide how those resources map to storage-facing field definitions, native containers, scheduler bindings, and jobs.

## Design checklist

Use this checklist when adding or changing catalog, recipe, request, or plan code.

The change belongs to `GenerationCatalog` when it affects:

```text id="iv2p0f"
accepted inventory
definition lookup
symbol uniqueness
catalog ownership
definition graph consistency
```

The change belongs to `GenerationRecipeDefinition` when it affects:

```text id="mgk8p7"
reusable generation templates
selected stage routes
default route-step implementation choices
recipe metadata
```

The change belongs to `GenerationRequestDescriptor` when it affects:

```text id="0co2s9"
symbolic caller intent
selected recipe symbol
per-run settings
implementation override symbols
```

The change belongs to `GenerationRequestResolver` when it affects:

```text id="r93q0z"
descriptor satisfiability
catalog symbol lookup
override target resolution
implementation compatibility
resolution errors
```

The change belongs to `GenerationRequest` when it affects:

```text id="zq1h89"
accepted resolved run intent
final implementation choices
run settings after resolution
```

The change belongs to `GenerationPlanCompiler` or `GenerationPlan` when it affects:

```text id="0x8hw4"
managed semantic ordering
stage plan nodes
operation plan nodes
plan construction from accepted requests
```

The change does not belong to this model when it affects:

```text id="kz99zn"
native allocation
field handles
scheduler bindings
job dependencies
Burst job structs
ECS execution systems
artifact capture
workspace disposal
```

Those belong to future execution architecture.

```