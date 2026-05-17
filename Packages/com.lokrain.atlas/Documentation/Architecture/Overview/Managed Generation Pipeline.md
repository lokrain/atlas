# Managed generation pipeline

The managed generation pipeline converts symbolic generation intent into an accepted managed generation plan.

The pipeline is implemented in Runtime and ends at `GenerationPlan`.

It does not allocate native storage, compile runnable execution metadata, create field handles, schedule jobs, or execute generation work.

## Pipeline summary

```text
GenerationRequestDescriptor
        +
GenerationCatalog
        |
        v
GenerationRequestResolver
        |
        v
GenerationRequestResolutionResult
        |
        v
GenerationRequest
        |
        v
GenerationPlanCompiler
        |
        v
GenerationPlan
````

Each boundary has one responsibility:

| Boundary                            | Responsibility                                                      |
| ----------------------------------- | ------------------------------------------------------------------- |
| `GenerationRequestDescriptor`       | Captures symbolic caller intent.                                    |
| `GenerationCatalog`                 | Provides accepted package inventory and symbol lookup.              |
| `GenerationRequestResolver`         | Resolves descriptor symbols through the catalog.                    |
| `GenerationRequestResolutionResult` | Reports either an accepted request or structured resolution errors. |
| `GenerationRequest`                 | Represents accepted resolved intent for one run.                    |
| `GenerationPlanCompiler`            | Converts the accepted request into an ordered managed plan.         |
| `GenerationPlan`                    | Represents semantic managed generation work for one run.            |

## Inputs

The pipeline starts with two inputs:

```text
GenerationCatalog
GenerationRequestDescriptor
```

The catalog is accepted package inventory.

The descriptor is symbolic run intent.

They are intentionally separate. The descriptor can be valid by itself while still being unsatisfied by a particular catalog.

## Generation catalog

`GenerationCatalog` is the immutable inventory used during request resolution.

It owns accepted definitions for:

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

The catalog validates graph consistency and exact ownership of definitions referenced by catalog-owned objects.

A catalog provides lookup. It does not represent a generation run.

The catalog must not own:

```text
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native containers
scheduler state
job handles
```

## Request descriptor

`GenerationRequestDescriptor` is caller-provided symbolic intent.

It contains:

```text
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

The descriptor validates its own structure. It does not validate that referenced symbols exist in a catalog.

A request descriptor is not an accepted request.

The descriptor must not contain:

```text
GenerationRecipeDefinition
StageRouteStepDefinition
OperationImplementationDefinition
GenerationPlan
native containers
field handles
job handles
```

## Run settings

`GenerationRunSettings` contains per-run deterministic settings.

Current settings include:

```text
Grid
Seed
```

Run settings belong to the request, not to the recipe and not to the catalog.

The same recipe can be used with different run settings.

## Implementation overrides

`OperationImplementationOverrideDescriptor` selects a non-default implementation for a specific route-step occurrence.

It contains symbolic references to:

```text
StageRouteStepDefinitionSymbol
OperationImplementationDefinitionSymbol
```

The override targets the route-step occurrence, not only the operation definition.

This preserves correctness when the same operation definition appears more than once in a route.

## Request resolution

`GenerationRequestResolver` resolves descriptor symbols through a catalog.

Conceptual flow:

```text
GenerationCatalog
        +
GenerationRequestDescriptor
        |
        v
GenerationRequestResolver
        |
        v
GenerationRequestResolutionResult
```

The resolver validates:

```text
the requested recipe exists
the recipe belongs to the catalog
each override target exists in the selected recipe route
each override implementation exists
each override implementation belongs to the operation used by the targeted route step
the final implementation choice set is complete
```

The resolver produces a result object because unsatisfied symbols are expected domain outcomes.

## Resolution result

`GenerationRequestResolutionResult` contains either:

```text
GenerationRequest
```

or:

```text
GenerationRequestResolutionError list
```

Resolution failure does not throw for normal catalog-satisfiability errors.

Expected resolution errors include:

```text
missing recipe symbol
missing route-step override target
missing implementation symbol
implementation incompatible with the targeted route-step operation
duplicate override target
```

Invalid API usage still throws exceptions.

Examples:

```text
null catalog
null descriptor
malformed constructor input
```

## Accepted request

`GenerationRequest` is accepted resolved generation intent for one run.

It contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
StageRouteStepImplementationChoice list
```

The accepted request has no unresolved symbols.

The request owns the final implementation choices for the run. The plan compiler should not reinterpret override descriptors or repeat catalog lookup during normal compilation.

A generation request is not executable data.

It must not contain:

```text
native containers
field definitions
field handles
scheduler bindings
job handles
dependency handles
Burst function pointers
```

## Plan compilation

`GenerationPlanCompiler` converts an accepted request into a managed semantic plan.

Conceptual flow:

```text
GenerationRequest
        |
        v
GenerationPlanCompiler
        |
        v
GenerationPlan
```

The compiler orders the selected recipe stages and route-step operations.

The compiler consumes accepted objects. It must not resolve raw symbols through the catalog during normal compilation.

The compiler may validate request consistency defensively, but request resolution is the boundary that owns symbol satisfaction.

## Generation plan

`GenerationPlan` is accepted managed semantic data for one run.

It contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
StagePlanNode list
```

Each `StagePlanNode` contains:

```text
StageDefinition
StageRouteDefinition
StageContract
OperationPlanNode list
```

Each `OperationPlanNode` contains:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

The plan expresses what semantic generation work should occur and in which managed order.

The plan does not say how native memory is allocated or how jobs are scheduled.

## Resource flow

Stage and operation contracts describe semantic data flow by using `ResourceDefinition`.

A stage contract declares the resources required and produced by a stage.

An operation contract declares the resources required and produced by an operation.

Contracts must use resource definitions, not raw symbol lists.

A resource definition identifies a generated value semantically. It is not storage, not a native container, and not a job dependency.

## Managed plan boundary

The managed pipeline stops at `GenerationPlan`.

The following are outside the current implemented managed pipeline:

```text
FieldDefinition
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
SchedulerBinding
GenerationWorkspace
FieldHandle
OperationScheduler
JobHandle
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
Burst jobs
```

These belong to planned execution architecture.

## Correct flow

```text
descriptor = symbolic intent
catalog = accepted inventory

resolutionResult = resolver.Resolve(catalog, descriptor)

if resolutionResult succeeds:
    request = resolutionResult.Request
    plan = planCompiler.Compile(request)
```

The plan compiler receives an accepted request, not a descriptor.

## Incorrect flow

```text
descriptor -> plan compiler
descriptor -> jobs
catalog -> jobs
recipe symbol -> plan node
operation implementation symbol -> scheduler
resource symbol -> native storage
```

These flows bypass accepted boundaries and make ownership unclear.

## Validation responsibilities

| Validation                                | Owner                                       |
| ----------------------------------------- | ------------------------------------------- |
| Symbol syntax                             | `Symbol`                                    |
| Display name syntax                       | `DisplayName`                               |
| Grid dimensions and cell/index bounds     | `Grid`                                      |
| Definition constructor invariants         | Definition type                             |
| Catalog graph consistency                 | `GenerationCatalog`                         |
| Descriptor structure                      | Descriptor type                             |
| Descriptor satisfiability against catalog | `GenerationRequestResolver`                 |
| Final request construction invariants     | `GenerationRequest`                         |
| Managed plan structure                    | `GenerationPlanCompiler` / `GenerationPlan` |
| Native storage validity                   | Future workspace                            |
| Job dependency validity                   | Future scheduler                            |

## Determinism responsibilities

The managed pipeline must preserve deterministic inputs and deterministic ordering.

Stable deterministic inputs include:

```text
Symbol
Grid
Seed
GenerationRunSettings
GenerationRecipeDefinition
StageRouteChoice
StageRouteStepImplementationChoice
GenerationRequest
GenerationPlan
```

The managed pipeline must not depend on:

```text
Unity object instance IDs
Unity asset paths as generation identity
display names
dictionary enumeration order
process-local hash codes
managed object allocation order
editor selection state
scene hierarchy order
```

When ordering is required, order must be explicit in accepted objects.

## Error model

The managed pipeline separates invalid API usage from expected resolution failure.

Throw exceptions for invalid API usage:

```text
null arguments
invalid constructor input
duplicate accepted objects where uniqueness is required
invalid grid dimensions
invalid symbols
invalid display names
```

Return result errors for expected request-resolution failure:

```text
requested recipe symbol not found
override route-step symbol not found in selected recipe
implementation symbol not found
implementation not compatible with target operation
```

Do not use exceptions as normal control flow for catalog-satisfiability failure.

## Current boundary checklist

A change belongs inside the current managed pipeline when it affects:

```text
core value object invariants
definition invariants
catalog ownership and lookup
recipe structure
request descriptor structure
request resolution
accepted request construction
managed plan compilation
managed plan node structure
resource-definition-based contracts
```

A change belongs outside the current managed pipeline when it affects:

```text
native memory allocation
field definitions
field handles
scheduler bindings
execution profiles
job scheduling
dependency handles
Burst job structs
ECS system integration
artifact capture
runtime workspace disposal
```

Those topics belong to future execution architecture documents.

```