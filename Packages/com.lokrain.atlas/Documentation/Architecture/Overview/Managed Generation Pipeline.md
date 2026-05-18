# Managed generation pipeline

This article explains the current managed generation pipeline in Lokrain.Atlas.

The managed pipeline starts with reusable generation inventory and ends at `GenerationPlan`.

Execution after `GenerationPlan` is planned architecture.

## Pipeline summary

The current pipeline is:

```text
GenerationCatalog
  + GenerationRequestDescriptor
    -> GenerationRequestResolver
    -> GenerationRequestResolutionResult
    -> GenerationRequest
    -> GenerationPlanCompiler
    -> GenerationPlan
```

This pipeline is managed and semantic.

It does not allocate native storage, bind field handles, schedule jobs, execute Burst jobs, capture artifacts, or integrate with ECS execution.

## Pipeline stages

| Stage | Input | Output | Responsibility |
| --- | --- | --- | --- |
| Catalog construction | Candidate definitions | `GenerationCatalog` | Validate reusable definition inventory. |
| Descriptor creation | Caller intent | `GenerationRequestDescriptor` | Represent symbolic run intent. |
| Request resolution | Catalog and descriptor | `GenerationRequestResolutionResult` | Resolve symbols into accepted definitions or errors. |
| Accepted request | Successful resolution | `GenerationRequest` | Represent one accepted resolved run. |
| Plan compilation | Accepted request | `GenerationPlan` | Create managed semantic stage and operation order. |
| Planned execution | Managed plan | Planned runnable execution | Compile executable metadata, allocate storage, and schedule jobs. |

## Catalog construction

A `GenerationCatalog` contains accepted reusable inventory.

Catalog construction validates:

```text
definition uniqueness
schema ownership
resource ownership
stage ownership
route ownership
operation ownership
implementation compatibility
contract resource ownership
recipe graph consistency
cross-catalog reference rejection
```

Catalog construction failure is invalid API usage and throws.

Catalog construction does not return a request-resolution result.

Correct:

```text
GenerationCatalog catalog =
  LandmassGenerationCatalog.CreateCatalog();
```

Correct builder flow:

```text
GenerationCatalog catalog =
  LandmassGenerationCatalog
    .AddTo(new GenerationCatalogBuilder()
      .AddGenerationSchemaDefinition(BuiltInGenerationSchemas.World))
    .Build();
```

A catalog is immutable after construction.

## Catalog inventory

A catalog may contain:

```text
GenerationSchemaDefinition
ResourceDefinition
StageKind
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract
OperationKind
OperationDefinition
OperationContract
OperationImplementationDefinition
GenerationRecipeDefinition
```

A catalog must not contain:

```text
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
UnityEngine.Object
```

## Descriptor creation

A `GenerationRequestDescriptor` represents symbolic caller intent.

It contains:

```text
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

A descriptor can be valid even if a specific catalog cannot satisfy it.

Correct:

```text
GenerationRequestDescriptor descriptor =
  LandmassGenerationRequests.CreatePrimaryContinentalLandmass(grid, seed);
```

The descriptor does not contain accepted catalog definitions.

## Run settings

`GenerationRunSettings` contains run-specific input.

Current run settings contain:

```text
Grid
Seed
```

Run settings belong to per-run objects.

Correct:

```text
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
```

Incorrect:

```text
GenerationRecipeDefinition
StageDefinition
OperationDefinition
ResourceDefinition
```

Definitions are reusable inventory and must not contain per-run settings.

## Implementation overrides

`OperationImplementationOverrideDescriptor` represents a symbolic implementation override.

It contains:

```text
StageRouteStepDefinitionSymbol
OperationImplementationDefinitionSymbol
```

The override target is a route-step symbol.

The override value is an implementation symbol.

Overrides target route-step occurrences, not operation definitions.

This allows different occurrences of the same operation definition to choose different implementations.

## Request resolution

`GenerationRequestResolver` converts symbolic intent into accepted intent.

It uses:

```text
GenerationCatalog
GenerationRequestDescriptor
```

It returns:

```text
GenerationRequestResolutionResult
```

The resolver checks:

```text
the recipe symbol exists in the catalog
each override route step is selected by the recipe
each override implementation exists in the catalog
each override implementation belongs to the operation required by the route step
the final implementation choices satisfy the selected recipe
```

The resolver does not compile a plan.

The resolver does not allocate storage.

The resolver does not schedule work.

## Resolution success

On success, `GenerationRequestResolutionResult` contains an accepted `GenerationRequest`.

Success state:

```text
Succeeded == true
Failed == false
GenerationRequest != null
Errors is empty
```

The accepted request contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
final StageRouteStepImplementationChoice list
```

The accepted request contains no unresolved symbols.

## Resolution failure

On failure, `GenerationRequestResolutionResult` contains structured errors.

Failure state:

```text
Succeeded == false
Failed == true
GenerationRequest == null
Errors is not empty
```

Expected resolver failures include:

```text
unknown recipe symbol
override route step not selected by recipe
unknown implementation symbol
implementation operation mismatch
```

These are normal boundary failures and should not throw.

## Error ordering

Resolution errors should be returned in stable order.

For override errors, descriptor override order is the expected order.

Correct:

```text
override[0] error
override[1] error
```

Incorrect:

```text
unordered hash-map error output
randomized error order
```

Stable error order makes diagnostics deterministic and tests reliable.

## Accepted request

`GenerationRequest` represents one accepted resolved run.

It contains accepted definitions and final implementation choices.

It does not contain:

```text
recipe symbols
implementation override descriptors
unresolved operation symbols
native containers
field handles
job handles
```

The request is still managed semantic data.

It does not execute work.

## Default implementation choices

A recipe provides default route-step implementation choices.

A request uses the recipe defaults unless the descriptor provides valid overrides.

Resolution applies overrides and produces the final accepted choice list.

The final choice list belongs to the request.

## Plan compilation

`GenerationPlanCompiler` converts an accepted request into a managed plan.

It creates:

```text
GenerationPlan
StagePlanNode
OperationPlanNode
```

The compiler owns:

```text
stage dependency ordering
stage plan node creation
operation plan node creation
route-step order preservation
managed plan validation
```

The compiler does not own:

```text
symbol lookup
descriptor resolution
catalog construction
native allocation
field binding
job scheduling
artifact capture
```

## Stage ordering

Stage ordering is semantic.

A stage that produces a resource required by another selected stage must appear before the consumer stage.

When stages are independent, recipe order is the stable tie-breaker.

Correct:

```text
ProducerStage -> ConsumerStage
```

Correct independent ordering:

```text
recipe order is preserved
```

The compiler must not depend on dictionary enumeration order, hash set enumeration order, object allocation order, Unity scene order, asset import order, or thread timing.

## Operation ordering

Operation ordering follows the selected stage route.

A stage route owns an ordered list of route steps.

A stage plan node contains operation plan nodes in route-step order.

Correct:

```text
StageRouteDefinition
  step 0: EvaluateContinentSuitability
  step 1: FormContinentCandidate
  step 2: ExtractMainContinent
```

The generated `StagePlanNode` must preserve that operation order.

## GenerationPlan

`GenerationPlan` is the current managed pipeline output.

It contains:

```text
GenerationRecipeDefinition
GenerationSchemaDefinition
GenerationRunSettings
StagePlanNode list
```

A plan contains accepted definitions and semantic ordering.

A plan contains no unresolved symbols.

A plan contains no native execution state.

## StagePlanNode

`StagePlanNode` represents one selected stage in a managed plan.

It contains:

```text
StageDefinition
StageRouteDefinition
StageContract
OperationPlanNode list
```

A stage plan node does not execute work.

## OperationPlanNode

`OperationPlanNode` represents one selected route-step operation occurrence.

It contains:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

An operation plan node does not execute work.

## Current endpoint

The current managed pipeline ends at:

```text
GenerationPlan
```

`GenerationPlan` is not a runnable plan.

It must not contain:

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
UnityEngine.Object
```

## Planned execution boundary

Planned execution starts after `GenerationPlan`.

Planned execution flow:

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

Planned execution owns:

```text
storage-facing field metadata
runnable metadata
native storage allocation
field handles
scheduler bindings
job dependency wiring
scratch allocation
Burst job scheduling
artifact capture
execution diagnostics
ECS integration
```

These responsibilities must not be added to the current managed pipeline.

## Example flow

Example managed landmass flow:

```text
Grid grid = new(256, 256);
Seed seed = new(123UL);

GenerationCatalog catalog =
  LandmassGenerationCatalog.CreateCatalog();

GenerationRequestDescriptor descriptor =
  LandmassGenerationRequests.CreatePrimaryContinentalLandmass(grid, seed);

GenerationRequestResolutionResult result =
  resolver.Resolve(catalog, descriptor);

GenerationRequest request =
  result.GenerationRequest;

GenerationPlan plan =
  compiler.Compile(request);
```

The result should be inspected before accessing the request.

Conceptual safe flow:

```text
if result succeeds
  compile result.GenerationRequest
else
  report result.Errors
```

## Failure boundaries

| Failure | Boundary |
| --- | --- |
| Invalid catalog inventory | Exception during catalog construction. |
| Invalid descriptor structure | Exception during descriptor construction. |
| Unknown recipe symbol | Failed resolution result. |
| Unknown implementation override symbol | Failed resolution result. |
| Implementation override for wrong operation | Failed resolution result. |
| Invalid final request choices | Exception during request construction. |
| Impossible managed plan state | Exception during plan compilation. |

## Determinism

The managed pipeline must produce deterministic output for the same accepted input.

Deterministic inputs include:

```text
catalog-owned definitions
recipe choices
descriptor symbols
run settings
override descriptors
accepted request choices
route-step order
stage dependency graph
```

Do not use nondeterministic sources for ordering or identity:

```text
hash-map enumeration order
thread timing
Unity scene hierarchy order
asset import order
current time
global random state
managed object allocation order
```

## Summary

The managed generation pipeline resolves symbolic run intent into accepted run intent and compiles that intent into a managed semantic plan.

The pipeline validates the semantic model.

It does not execute generation.

Execution is planned after `GenerationPlan`.