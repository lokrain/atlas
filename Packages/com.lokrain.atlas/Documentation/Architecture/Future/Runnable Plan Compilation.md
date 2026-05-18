# Runnable plan compilation

This document describes planned architecture.

`RunnablePlanCompiler`, `RunnablePlan`, runnable stages, runnable operations, field bindings, and scheduler bindings are not current Runtime behavior unless corresponding Runtime code exists.

Current Runtime architecture ends at `GenerationPlan`.

## Purpose

Runnable plan compilation is the planned bridge between managed semantic planning and executable generation metadata.

The planned flow is:

```text
GenerationPlan
  + FieldDefinitionSet
  + ExecutionProfile
    -> RunnablePlanCompiler
    -> RunnablePlan
```

The runnable plan compiler prepares execution metadata. It does not allocate native storage and does not schedule jobs.

## Current boundary

Current Runtime contains:

```text
GenerationPlanCompiler
GenerationPlan
StagePlanNode
OperationPlanNode
```

Current Runtime does not contain:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldHandle
SchedulerBinding
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
```

Do not add runnable metadata to current managed plan objects.

## Inputs

The planned runnable compiler uses:

```text
GenerationPlan
FieldDefinitionSet       current managed metadata
ExecutionProfile         current managed metadata
```

### GenerationPlan

`GenerationPlan` provides managed semantic planning output.

It contains:

```text
GenerationRecipeDefinition
GenerationSchemaDefinition
GenerationRunSettings
StagePlanNode list
```

It does not contain executable metadata.

### FieldDefinitionSet

`FieldDefinitionSet` provides planned storage-facing metadata for resources used by the plan.

It may define:

```text
field shape
field value kind
storage role
capture policy
external binding policy
diagnostic role
```

### ExecutionProfile

`ExecutionProfile` provides planned execution policy.

It may define:

```text
storage representation choices
capture policy
diagnostic policy
temporary retention policy
scheduler policy
artifact policy
validation strictness
```

Execution profiles must not change semantic resource identity.

## Output

The planned output is `RunnablePlan`.

A runnable plan is immutable executable metadata.

A runnable plan may contain:

```text
runnable stages
runnable operations
resource-to-field bindings
field allocation requirements
scheduler bindings
implementation execution metadata
capture declarations
diagnostic declarations
external binding requirements
```

A runnable plan does not own native storage lifetime.

Native storage lifetime belongs to planned `GenerationWorkspace`.

## RunnablePlanCompiler

`RunnablePlanCompiler` owns executable metadata compilation.

It may validate:

```text
each plan resource has a compatible field definition
each operation input can bind to a field
each operation output can bind to a writable field
selected implementations support required field shapes
temporary fields have storage policy
external fields have binding policy
diagnostic fields follow profile policy
scheduler bindings are available
capture declarations are valid
```

It must not:

```text
resolve descriptor symbols
build catalogs
select recipes
change the managed plan order
allocate native storage
create NativeArray<T>
schedule jobs
own JobHandle dependencies
capture artifacts
```

## RunnablePlan

`RunnablePlan` is planned executable metadata.

It is derived from a valid `GenerationPlan`.

It may contain execution-facing representations of:

```text
stages
operations
field bindings
storage requirements
scheduler bindings
capture policy
diagnostic policy
```

It must not replace the managed semantic plan.

A runnable plan is not a catalog.

A runnable plan is not a request.

A runnable plan is not a workspace.

A runnable plan is not a scheduler.

## RunnableStage

A `RunnableStage` is planned executable metadata for one stage plan node.

It may reference:

```text
source StagePlanNode identity
runnable operations
stage-level field requirements
stage-level capture declarations
stage-level scheduler policy
```

It must not own native field storage.

It must not schedule jobs directly.

## RunnableOperation

A `RunnableOperation` is planned executable metadata for one operation plan node.

It may reference:

```text
source OperationPlanNode identity
operation input field bindings
operation output field bindings
implementation execution metadata
scheduler binding
scratch requirements
diagnostic declarations
```

It must not own native storage lifetime.

It must not execute work directly.

## Resource-to-field binding

Runnable compilation binds semantic resources to planned fields.

Example:

```text
ResourceDefinition BaseElevation
  -> FieldDefinition BaseElevationField
  -> Runnable field binding
```

The resource remains the semantic identity.

The field definition describes execution representation.

The field binding records how the runnable operation accesses execution storage.

## Field allocation requirements

A runnable plan may describe what storage the workspace must allocate.

Examples:

```text
canonical field allocation
temporary field allocation
diagnostic field allocation
external field binding requirement
scratch requirement
```

The runnable plan records requirements.

The workspace performs allocation.

Correct:

```text
RunnablePlan describes allocation requirements.
GenerationWorkspace allocates native storage.
```

Incorrect:

```text
RunnablePlan owns NativeArray<T>.
GenerationPlan owns FieldHandle.
FieldDefinition owns native memory.
```

## Scheduler binding

A scheduler binding is planned metadata connecting a runnable operation to the scheduler that executes it.

It may describe:

```text
scheduler type
operation execution policy
dependency requirements
scratch requirements
iteration policy
termination policy
failure policy
```

A scheduler binding is metadata.

`OperationScheduler` owns actual scheduling.

## Implementation metadata

Operation implementations are current semantic selectable definitions.

Runnable compilation may map an `OperationImplementationDefinition` to implementation-specific execution metadata.

Correct planned flow:

```text
OperationImplementationDefinition
  -> RunnablePlanCompiler
  -> RunnableOperation execution metadata
  -> OperationScheduler
  -> jobs
```

Incorrect:

```text
OperationImplementationDefinition owns JobHandle.
OperationImplementationDefinition owns NativeArray<T>.
OperationImplementationDefinition schedules itself.
```

## Ordering

Runnable compilation must preserve managed plan order.

Stage order comes from `GenerationPlan.StagePlanNodes`.

Operation order comes from each `StagePlanNode.OperationPlanNodes`.

Do not reorder runnable stages or operations unless a future execution policy explicitly defines a deterministic transformation.

Do not depend on:

```text
dictionary enumeration order
hash set enumeration order
thread timing
managed object allocation order
Unity scene order
asset import order
```

## Validation failures

Runnable compilation may fail when execution metadata cannot be produced.

Expected planned failures include:

```text
missing field definition for required resource
incompatible field shape
incompatible field value kind
missing external binding declaration
unsupported implementation/profile combination
missing scheduler binding
invalid capture policy
```

The exact error mechanism is planned.

Use structured result objects for expected compilation failures.

Use exceptions for invalid API usage.

## Determinism

Runnable compilation must be deterministic for the same accepted inputs.

Inputs that may affect output must be explicit:

```text
GenerationPlan
FieldDefinitionSet       current managed metadata
ExecutionProfile         current managed metadata
implementation metadata
scheduler metadata
capture policy
```

Do not use hidden global state, Unity object identity, current time, global random state, or unordered collection enumeration.

## Workspace boundary

Runnable compilation does not allocate workspace storage.

The boundary is:

```text
RunnablePlanCompiler
  -> RunnablePlan
  -> GenerationWorkspace
```

The workspace owns:

```text
native allocation
field handle creation
storage lifetime
disposal
external field binding
scratch storage
```

The runnable plan owns metadata only.

## Scheduler boundary

Runnable compilation does not schedule jobs.

The boundary is:

```text
RunnablePlan
  -> OperationScheduler
  -> jobs
```

The scheduler owns:

```text
dependency wiring
job scheduling
scratch allocation
iteration
termination
failure policy
```

The runnable plan describes what must be scheduled.

## Job boundary

Jobs must receive native containers and unmanaged values only.

Jobs must not receive:

```text
GenerationPlan
RunnablePlan
ResourceDefinition
FieldDefinition
GenerationWorkspace
OperationScheduler
Symbol
DisplayName
UnityEngine.Object
```

Runnable compilation and scheduling resolve metadata before jobs are created.

## Artifact boundary

Runnable compilation may declare artifact capture requirements.

It must not capture artifacts.

Artifact capture belongs to planned execution after workspace data exists.

Correct planned flow:

```text
RunnablePlan declares capture requirements.
GenerationWorkspace stores data.
Artifact capture reads execution data.
```

Incorrect:

```text
RunnablePlan stores artifact buffers.
RunnablePlanCompiler writes artifact files.
```

## Diagnostics boundary

Runnable compilation may declare diagnostic fields or validation diagnostics.

Execution diagnostics belong to planned execution.

Do not store scheduler timings, job metrics, or native memory diagnostics in current managed plans.

## Invalid placements

Do not add these to current managed Runtime objects:

```text
GenerationPlan.RunnableStages
GenerationPlan.FieldBindings
GenerationPlan.SchedulerBindings
StagePlanNode.RunnableStage
OperationPlanNode.RunnableOperation
OperationPlanNode.FieldHandle
OperationPlanNode.JobHandle
GenerationRequest.RunnablePlan
GenerationRecipeDefinition.SchedulerBinding
ResourceDefinition.FieldDefinition
```

These belong to planned execution.

## Testing expectations

When implemented, tests should verify:

```text
RunnablePlanCompiler rejects null required inputs.
RunnablePlanCompiler preserves stage order.
RunnablePlanCompiler preserves operation order.
RunnablePlanCompiler rejects missing field definitions.
RunnablePlanCompiler rejects incompatible field shapes.
RunnablePlanCompiler rejects incompatible field value kinds.
RunnablePlanCompiler records external binding requirements.
RunnablePlanCompiler records capture declarations.
RunnablePlanCompiler does not allocate native storage.
RunnablePlanCompiler does not schedule jobs.
RunnablePlan exposes read-only runnable metadata.
RunnablePlan snapshots input metadata.
```

## Implementation order

Do not implement runnable compilation before the required current-runtime and field-definition foundations are stable.

Recommended order:

```text
1. Keep current managed Runtime stable through GenerationPlan.
2. Add FieldDefinition and FieldDefinitionSet.
3. Add ExecutionProfile.
4. Add RunnablePlanCompiler.
5. Add RunnablePlan, RunnableStage, and RunnableOperation.
6. Add GenerationWorkspace.
7. Add OperationScheduler.
8. Add jobs and execution diagnostics.
```

## Summary

Runnable plan compilation is planned architecture.

It converts a managed semantic `GenerationPlan` plus field metadata and execution policy into immutable executable metadata.

It does not allocate native storage.

It does not schedule jobs.

It is the planned bridge between current Runtime planning and future execution.