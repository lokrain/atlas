# Runnable plan compilation

Runnable plan compilation is the current managed bridge from semantic planning to managed executable metadata.

It does not execute work. It does not allocate native storage. It does not schedule jobs. It prepares deterministic table-oriented metadata that future workspace and scheduler infrastructure can consume.

## Inputs

`RunnablePlanCompiler` consumes:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
```

`GenerationPlan` supplies accepted semantic stage and operation order.

`FieldDefinitionSet` supplies managed field metadata for resources used by the plan.

`ExecutionProfile` supplies managed execution-profile identity and policy metadata.

The compiler is stateless. It must not read mutable global state, Unity scene state, editor state, current time, random state, or unordered collection enumeration for output ordering.

## Output

`RunnablePlanCompiler.Compile` returns `RunnablePlanCompilationResult`.

A successful result contains:

```text
Succeeded == true
RunnablePlan != null
Errors.Count == 0
```

A failed result contains:

```text
Succeeded == false
RunnablePlan == null
Errors.Count > 0
```

Compilation must not return partial runnable plans.

`CompileOrThrow` returns the runnable plan on success and throws when compilation fails.

## RunnablePlan

`RunnablePlan` is immutable managed executable metadata.

It contains:

```text
GenerationPlan
ExecutionProfile
IReadOnlyList<ResourceFieldBinding> FieldBindings
IReadOnlyList<RunnableStage> Stages
IReadOnlyList<RunnableOperation> Operations
```

`RunnablePlan` is not a workspace, scheduler, native storage container, artifact store, diagnostic store, or ECS binding.

## ResourceFieldBinding

`ResourceFieldBinding` is one field-binding table row.

It contains:

```text
FieldIndex
ResourceDefinition
FieldDefinition
FieldPlanRole
FieldCapturePolicy
```

The binding invariant is reference-exact:

```text
ReferenceEquals(fieldDefinition.ResourceDefinition, resourceDefinition)
```

`FieldPlanRole` is a closed classification derived from resource flow:

```text
RequiredInput
ProducedOutput
RequiredInputAndProducedOutput
```

`FieldPlanRole` is not a flags enum and does not describe storage lifetime.

`FieldCapturePolicy` records future capture intent only.

## RunnableStage

`RunnableStage` is one stage table row.

It contains:

```text
StageIndex
StagePlanNode
IReadOnlyList<FieldIndex> RequiredInputFieldIndices
IReadOnlyList<FieldIndex> ProducedOutputFieldIndices
IReadOnlyList<OperationIndex> OperationIndices
```

Stage field-index lists follow `StageContract` order.

Stage operation-index lists follow operation order within the source stage plan node.

## RunnableOperation

`RunnableOperation` is one operation occurrence table row.

It contains:

```text
OperationIndex
StageIndex
OperationPlanNode
IReadOnlyList<FieldIndex> RequiredInputFieldIndices
IReadOnlyList<FieldIndex> ProducedOutputFieldIndices
```

Operation field-index lists follow `OperationContract` order.

The same `OperationDefinition` may appear more than once in a runnable plan. Operation occurrence identity is represented by `OperationPlanNode` plus plan-local `OperationIndex`.

## Dense table invariants

Runnable metadata uses dense zero-based indices.

```text
FieldBindings[i].FieldIndex.Value == i
Stages[i].StageIndex.Value == i
Operations[i].OperationIndex.Value == i
```

The value `0` is valid. Atlas does not reserve sentinel index values.

`FieldIndex`, `StageIndex`, and `OperationIndex` are plan-local positions, not durable identities.

## Deterministic ordering

Compilation output order is deterministic:

```text
RunnablePlan.FieldBindings -> used FieldDefinition.Symbol ordinal order
RunnablePlan.Stages        -> GenerationPlan.StagePlanNodes order
RunnablePlan.Operations    -> stage order, then operation order within each stage
RunnableStage inputs       -> StageContract.RequiredInputs order
RunnableStage outputs      -> StageContract.ProducedOutputs order
RunnableOperation inputs   -> OperationContract.RequiredInputs order
RunnableOperation outputs  -> OperationContract.ProducedOutputs order
Compilation errors         -> deterministic error order
```

Private dictionaries may be used for lookup only. They must not define public output order.

## Compilation errors

`RunnablePlanCompilationErrorCode` is the stable machine-readable error contract.

Human-readable messages are diagnostic text and must not be treated as the API contract.

Current error codes cover reachable managed metadata validation failures:

```text
MissingFieldDefinition
ResourceFieldOwnershipMismatch
```

Additional codes should be added only when the compiler has a concrete reachable validation path for them.

## Boundary exclusions

Runnable plan compilation must not:

```text
resolve descriptor symbols
build catalogs
select recipes
change semantic plan order
allocate native storage
create NativeArray<T>
create FieldHandle
schedule jobs
own JobHandle dependencies
capture artifacts
capture runtime diagnostics
bind ECS entities
execute operation kernels
```

Those responsibilities belong to future execution infrastructure.
