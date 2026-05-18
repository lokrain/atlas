# Atlas architecture overview

Lokrain.Atlas is organized around deterministic, accepted managed metadata. Runtime code separates semantic generation meaning, managed field representation metadata, managed runnable metadata, and future runtime execution infrastructure.

The current architecture has three implemented managed layers:

```text
Semantic generation model
Managed field and execution-profile metadata
Managed runnable-plan metadata
```

Runtime execution infrastructure remains planned:

```text
GenerationWorkspace
OperationScheduler
Native storage ownership
Burst-compatible jobs
Artifact capture execution
Runtime diagnostic capture
ECS execution integration
```

## Current managed semantic model

The semantic model describes generation meaning and accepted caller intent.

It includes:

```text
Symbol and DisplayName
Grid, Cell, CellIndex, and Seed
GenerationSchemaDefinition
ResourceDefinition
StageDefinition and StageContract
StageRouteDefinition and StageRouteStepDefinition
OperationDefinition and OperationContract
OperationImplementationDefinition
GenerationRecipeDefinition
GenerationCatalog
GenerationRequestDescriptor
GenerationRequestResolver
GenerationRequest
GenerationPlanCompiler
GenerationPlan
```

The semantic model uses `ResourceDefinition` for resource flow. Stage and operation contracts must not use raw resource-symbol lists.

`GenerationCatalog` owns semantic inventory only. It does not own `FieldDefinition`, `FieldDefinitionSet`, `ExecutionProfile`, or `ExecutionProfileSet`.

`GenerationPlan` is the endpoint of semantic planning. It is deterministic managed order, not executable storage or scheduled work.

## Current managed field metadata

Managed field metadata maps semantic resources to managed representation metadata.

It includes:

```text
FieldValueKind
FieldShape
FieldDefinition
FieldDefinitionSet
LandmassFieldDefinitions
LandmassFieldDefinitionSet
```

`FieldDefinition` maps exactly one `ResourceDefinition` to a managed representation. It does not allocate storage, create handles, bind ECS data, capture artifacts, schedule jobs, or execute operations.

`FieldDefinitionSet` owns field metadata lookup and deterministic canonical ordering by field `Symbol`.

## Current managed execution-profile metadata

Managed execution-profile metadata identifies execution policy identity before executable runtime behavior exists.

It includes:

```text
ExecutionProfile
ExecutionProfileSet
BuiltInExecutionProfiles
BuiltInExecutionProfileSet
```

`ExecutionProfile` is identity and policy metadata only. It does not allocate storage, select native containers, schedule work, capture artifacts, or execute operations.

`ExecutionProfileSet` owns profile lookup and deterministic canonical ordering by profile `Symbol`.

## Current managed runnable metadata

Managed runnable metadata converts semantic plan nodes into dense table-oriented metadata that later execution infrastructure can consume.

It includes:

```text
FieldIndex
StageIndex
OperationIndex
FieldPlanRole
FieldCapturePolicy
ResourceFieldBinding
RunnableOperation
RunnableStage
RunnablePlan
RunnablePlanCompiler
RunnablePlanCompilationResult
RunnablePlanCompilationError
RunnablePlanCompilationErrorCode
```

`RunnablePlanCompiler` consumes:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
```

and returns either:

```text
RunnablePlan
```

or deterministic structured compilation errors.

`RunnablePlan` is managed executable metadata. It does not allocate native storage, create field handles, schedule jobs, own `JobHandle` dependencies, bind ECS entities, capture artifacts, or capture runtime diagnostics.

## Planned runtime execution infrastructure

Runtime execution infrastructure begins after `RunnablePlan`.

Planned execution infrastructure owns:

```text
Workspace storage allocation
Native container lifetime
Field handles
Operation scheduling
Scratch memory
Job dependencies
Artifact capture execution
Runtime diagnostic capture
ECS integration
```

These responsibilities must not be added to semantic definitions, contracts, catalogs, requests, plans, field definitions, execution profiles, or runnable metadata rows.

## Deterministic ordering rules

Atlas uses explicit order and canonical order only.

```text
Built-in provider All lists preserve declared order.
FieldDefinitionSet exposes canonical order by field Symbol.
ExecutionProfileSet exposes canonical order by profile Symbol.
RunnablePlan.FieldBindings are ordered by used FieldDefinition.Symbol.
RunnablePlan.Stages follow GenerationPlan.StagePlanNodes.
RunnablePlan.Operations follow stage order, then operation order within each stage.
RunnableStage field index lists follow StageContract order.
RunnableOperation field index lists follow OperationContract order.
```

Private dictionaries and hash sets may be used for lookup and membership only. They must never define public order, generation order, diagnostic order, artifact order, or serialized order.
