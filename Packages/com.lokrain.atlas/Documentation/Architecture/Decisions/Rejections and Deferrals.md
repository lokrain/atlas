# Rejections and deferrals

This decision record lists rejected and deferred architecture options for Lokrain.Atlas.

Rejected items must not be implemented without a new decision record.

Deferred items are not current Runtime behavior. They require explicit implementation work before documentation may describe them as implemented.

## Status

Accepted.

This document is a decision record, not an implementation plan.

## Current Runtime boundary

Current semantic planning ends at `GenerationPlan`. Current Runtime also includes managed field metadata and managed execution profile identity.

The following are current Runtime concepts:

```text
ResourceDefinition
StageContract
OperationContract
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequestResolver
GenerationRequest
GenerationPlanCompiler
GenerationPlan
FieldValueKind
FieldShape
FieldDefinition
FieldDefinitionSet
ExecutionProfile
ExecutionProfileSet
BuiltInExecutionProfiles
BuiltInExecutionProfileSet
LandmassFieldDefinitions
LandmassFieldDefinitionSet
```

The following are planned or deferred concepts:

```text
FieldBinding
SchedulerBinding
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
GenerationWorkspace
FieldHandle
WorkspaceAllocation
OperationScheduler
OperationScratch
native storage
Burst jobs
artifact capture
execution diagnostics
ECS execution integration
unsafe execution infrastructure
```

## Rejected: raw resource symbol lists in contracts

Raw resource symbol lists are rejected for stage and operation contracts.

Rejected names:

```text
RequiredInputSymbols
ProducedOutputSymbols
LandmassResourceSymbols
```

Use `ResourceDefinition` instead.

Correct:

```text
StageContract.RequiredInputs -> IReadOnlyList<ResourceDefinition>
StageContract.ProducedOutputs -> IReadOnlyList<ResourceDefinition>
OperationContract.RequiredInputs -> IReadOnlyList<ResourceDefinition>
OperationContract.ProducedOutputs -> IReadOnlyList<ResourceDefinition>
```

Reason:

Raw symbol lists do not carry accepted definition ownership. They weaken validation and delay semantic consistency checks.

## Rejected: symbol-equivalent catalog ownership

Symbol-equivalent definitions are not catalog-owned unless they are the exact instance owned by the catalog.

Rejected model:

```text
catalog owns ResourceDefinition A
contract references ResourceDefinition B
A.Symbol == B.Symbol
therefore B is accepted
```

Correct model:

```text
catalog owns ResourceDefinition A
contract references ResourceDefinition A
```

Reason:

Catalog ownership must be reference-exact to prevent mixed graphs, hidden replacement, and ambiguous ownership.

## Rejected: ResourceDefinition as storage

`ResourceDefinition` must not describe storage layout.

Rejected members:

```text
ResourceDefinition.FieldDefinition
ResourceDefinition.FieldHandle
ResourceDefinition.NativeArray
ResourceDefinition.StorageKind
ResourceDefinition.AllocationPolicy
```

Reason:

A resource is semantic identity. Managed representation metadata belongs to field definitions. Storage lifetime belongs to planned workspace allocation.

## Rejected: FieldDefinition in current contracts

Current `StageContract` and `OperationContract` must not use `FieldDefinition`.

Rejected model:

```text
OperationContract.RequiredInputs -> IReadOnlyList<FieldDefinition>
OperationContract.ProducedOutputs -> IReadOnlyList<FieldDefinition>
```

Correct current model:

```text
OperationContract.RequiredInputs -> IReadOnlyList<ResourceDefinition>
OperationContract.ProducedOutputs -> IReadOnlyList<ResourceDefinition>
```

Reason:

Contracts describe semantic resource flow. `FieldDefinition` is current managed representation metadata and belongs beside runnable compilation, not inside semantic contracts.

## Rejected: GenerationPlan as executable plan

`GenerationPlan` is not a runnable plan.

Rejected members:

```text
GenerationPlan.RunnableStages
GenerationPlan.RunnableOperations
GenerationPlan.FieldBindings
GenerationPlan.FieldHandles
GenerationPlan.SchedulerBindings
GenerationPlan.JobHandles
```

Reason:

`GenerationPlan` is managed semantic output. Runnable execution metadata belongs to planned `RunnablePlan`.

## Rejected: native storage in managed planning objects

Native containers must not be stored in current managed planning objects.

Rejected placements:

```text
GenerationCatalog.NativeStorage
GenerationRecipeDefinition.NativeArray
GenerationRequest.NativeList
GenerationPlan.NativeHashMap
StagePlanNode.NativeArray
OperationPlanNode.NativeArray
OperationContract.NativeArray
ResourceDefinition.NativeArray
FieldDefinition.NativeArray
ExecutionProfile.NativeArray
```

Reason:

Current Runtime planning and metadata objects are managed semantic or managed metadata objects. Native storage belongs to planned workspace execution.

## Rejected: scheduler behavior in definitions

Definitions must not schedule work or own scheduler behavior.

Rejected placements:

```text
StageDefinition.Scheduler
OperationDefinition.Scheduler
OperationImplementationDefinition.JobHandle
GenerationRecipeDefinition.OperationScheduler
StageRouteDefinition.ExecutionPolicy
FieldDefinition.SchedulerBinding
ExecutionProfile.OperationScheduler
```

Reason:

Definitions are reusable inventory or managed metadata. Scheduler ownership belongs to planned execution architecture.

## Rejected: jobs receiving semantic objects

Jobs must not receive semantic domain objects or managed metadata objects.

Rejected job inputs:

```text
GenerationCatalog
GenerationRecipeDefinition
GenerationRequest
GenerationPlan
RunnablePlan
ResourceDefinition
FieldDefinition
ExecutionProfile
GenerationWorkspace
OperationScheduler
Symbol
DisplayName
UnityEngine.Object
```

Correct planned job inputs:

```text
NativeArray<T>
NativeList<T>
unmanaged values
explicit dimensions
explicit seeds
explicit configuration values
```

Reason:

Jobs should execute deterministic native transforms. Metadata must be resolved before scheduling.

## Rejected: Unity object identity as domain identity

Unity object identity must not define Atlas domain identity.

Rejected identity sources:

```text
GameObject.name
UnityEngine.Object.GetInstanceID()
ScriptableObject reference identity
asset path
scene hierarchy order
editor selection state
```

Correct identity source:

```text
Symbol
```

Reason:

Unity object identity is not stable enough for deterministic generation, catalog lookup, artifact compatibility, or package-domain identity.

## Rejected: ScriptableObject as canonical recipe

A `ScriptableObject` must not be the canonical recipe model.

Rejected model:

```text
ScriptableObject recipe asset is the authoritative GenerationRecipeDefinition.
```

Correct model:

```text
ScriptableObject authoring asset adapts authored data into accepted Runtime definitions or descriptors.
```

Reason:

Unity authoring assets are adapters. Canonical Runtime domain state must remain package-owned and Unity-independent.

## Rejected: descriptor containing accepted definitions

Descriptors must not contain accepted catalog definitions.

Rejected model:

```text
GenerationRequestDescriptor
  GenerationRecipeDefinition
  OperationImplementationDefinition
  StageRouteStepDefinition
```

Correct model:

```text
GenerationRequestDescriptor
  GenerationRecipeDefinitionSymbol
  OperationImplementationOverrideDescriptor list
```

Reason:

Descriptors are symbolic caller intent. Catalog-dependent resolution belongs to `GenerationRequestResolver`.

## Rejected: request containing unresolved symbols

Accepted requests must not contain unresolved symbols.

Rejected model:

```text
GenerationRequest
  GenerationRecipeDefinitionSymbol
  OperationImplementationOverrideDescriptor list
```

Correct model:

```text
GenerationRequest
  GenerationRecipeDefinition
  GenerationRunSettings
  final StageRouteStepImplementationChoice list
```

Reason:

A request is accepted resolved generation intent. Symbolic intent belongs to descriptors.

## Rejected: catalog resolving request descriptors

`GenerationCatalog` must not resolve `GenerationRequestDescriptor`.

Rejected model:

```text
catalog.Resolve(descriptor)
```

Correct model:

```text
GenerationRequestResolver.Resolve(catalog, descriptor)
```

Reason:

The catalog owns accepted inventory and graph validation. The resolver owns descriptor satisfiability.

## Rejected: recipe containing run settings

Recipes must not contain run-specific input.

Rejected members:

```text
GenerationRecipeDefinition.Grid
GenerationRecipeDefinition.Seed
GenerationRecipeDefinition.GenerationRunSettings
```

Reason:

Recipes are reusable templates. Run settings belong to descriptors, accepted requests, and plans.

## Rejected: operation definitions containing implementation execution state

`OperationDefinition` and `OperationImplementationDefinition` must not own native execution state.

Rejected members:

```text
OperationDefinition.NativeKernel
OperationImplementationDefinition.JobHandle
OperationImplementationDefinition.NativeArray
OperationImplementationDefinition.Execute()
```

Reason:

Operation definitions and implementation definitions are selectable metadata. Execution belongs to planned runnable metadata, scheduler infrastructure, and jobs.

## Rejected: managed object graphs as canonical generation data

Managed object graphs must not be canonical generation data.

Rejected canonical model:

```text
class Region
{
    List<Region> Neighbors;
    List<Cell> Cells;
}
```

Preferred canonical model:

```text
nodes
edges
offsets
counts
values
```

Reason:

Canonical generation data must be deterministic, compact, ordered, serializable, and compatible with Burst-oriented execution.

Managed object graphs may still be useful for editor visualization or tests.

## Rejected: unordered collection enumeration as deterministic order

Do not use unordered collection enumeration as generation order.

Rejected sources:

```text
Dictionary<TKey, TValue> enumeration
HashSet<T> enumeration
NativeHashMap<TKey, TValue> enumeration
thread completion order
object allocation order
```

Reason:

Generation-relevant ordering must be explicit and deterministic.

## Current: FieldDefinition

`FieldDefinition` is current managed field metadata.

It maps one `ResourceDefinition` to representation metadata such as field symbol, display name, field shape, and field value kind.

It must not:

```text
allocate native storage
own native memory
create field handles
schedule jobs
bind ECS data
capture artifacts
describe executable operation data
```

Required foundations:

```text
ResourceDefinition
resource-definition-based contracts
GenerationCatalog ownership validation
GenerationPlan
```

## Current: FieldDefinitionSet

`FieldDefinitionSet` is current managed field-metadata acceptance.

It groups accepted field definitions used by future runnable plan compilation.

It validates field metadata consistency and exposes deterministic canonical order by field symbol.

It must not replace `GenerationCatalog`.

Catalogs validate semantic definition graphs.

Field definition sets validate managed representation metadata.

## Current: ExecutionProfile

`ExecutionProfile` is current managed execution-profile identity metadata.

It identifies a reusable execution-profile variant by symbol and display name.

It does not currently store executable policy switches.

It must not:

```text
change semantic resource identity
select jobs
allocate storage
schedule work
capture artifacts
bind ECS data
```

Concrete execution behavior belongs to future runnable compilation, workspace, scheduler, and job infrastructure.

## Current: ExecutionProfileSet

`ExecutionProfileSet` is current managed execution-profile acceptance.

It validates accepted execution profiles and exposes deterministic canonical order by profile symbol.

It must not replace `GenerationCatalog` and must not define semantic resource ownership.

## Deferred: FieldBinding

`FieldBinding` is deferred until Phase 5 runnable-plan bridge implementation.

It should connect a semantic `ResourceDefinition` to a matching `FieldDefinition` without allocating storage or creating handles.

It should validate that the field definition represents the same resource as the resource side of the binding.

## Deferred: SchedulerBinding

`SchedulerBinding` is deferred until it is justified by runnable metadata.

It may record managed scheduler-facing metadata, but it must not schedule jobs, own dependencies, or allocate scratch.

If no immediate invariant requires this type during Phase 5, it should remain deferred.

## Deferred: RunnablePlanCompiler

`RunnablePlanCompiler` is deferred.

It will compile:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
```

into:

```text
RunnablePlan
```

It must not allocate native storage or schedule jobs.

## Deferred: RunnablePlan

`RunnablePlan` is deferred.

It will contain executable metadata such as:

```text
runnable stages
runnable operations
field bindings
scheduler bindings
storage requirements
capture declarations
diagnostic declarations
```

It must not own native storage lifetime.

## Deferred: GenerationWorkspace

`GenerationWorkspace` is deferred.

It will own native storage for one generation run.

Planned responsibilities:

```text
canonical field storage
temporary field storage
external field bindings
diagnostic field storage
scratch storage
native allocation lifetime
disposal
```

It must not own catalog validation, request resolution, or managed plan compilation.

## Deferred: OperationScheduler

`OperationScheduler` is deferred.

It will own execution control flow.

Planned responsibilities:

```text
workspace access
dependency wiring
job scheduling
scratch allocation
iteration policy
termination policy
failure policy
diagnostics
```

It must not own semantic resource identity, catalog lookup, request resolution, or managed plan compilation.

## Deferred: Burst jobs

Burst job execution is deferred.

Jobs must receive native containers and unmanaged values only.

Jobs must not receive semantic Runtime objects or managed metadata objects.

## Deferred: artifacts

Artifact capture is deferred.

Artifacts will be produced from workspace-owned execution data according to capture policy.

Artifacts must not be owned by resources, contracts, recipes, requests, field definitions, execution profiles, or managed plans.

## Deferred: execution diagnostics

Execution diagnostics are deferred.

Planned diagnostics may include:

```text
workspace allocation summaries
scheduler timings
captured diagnostic fields
operation validation summaries
artifact metadata
```

Execution diagnostics must not be stored in current semantic definitions, requests, field definitions, execution profiles, or plans.

## Deferred: ECS execution integration

ECS execution integration is deferred.

Current managed Runtime objects must not depend on:

```text
Entity
IComponentData
IBufferElementData
ISystem
SystemBase
EntityQuery
EntityCommandBuffer
World
```

ECS integration should remain an adapter or execution integration layer.

## Deferred: unsafe execution infrastructure

Unsafe memory infrastructure is deferred.

Unsafe code may be introduced only in isolated execution infrastructure when standard native containers are insufficient.

Potential future areas:

```text
Runtime/Memory
Runtime/Execution
Runtime/Topology
Runtime/Diagnostics
Runtime/Artifacts
Runtime/Interop
Runtime/Hashing
```

Unsafe code must not leak into current semantic Runtime layers.

## Rejected: GenerationCatalog owning field definitions or execution profiles

`GenerationCatalog` must not own `FieldDefinition`, `FieldDefinitionSet`, `ExecutionProfile`, or `ExecutionProfileSet`.

The catalog owns semantic generation inventory. Field metadata and execution profile metadata are separate managed inputs to future runnable compilation.

Rejected model:

```text
GenerationCatalog.FieldDefinitions
GenerationCatalog.ExecutionProfiles
GenerationCatalogBuilder.AddFieldDefinition(...)
GenerationCatalogBuilder.AddExecutionProfile(...)
```

Correct model:

```text
GenerationCatalog owns semantic inventory.
FieldDefinitionSet owns field metadata lookup and canonical ordering.
ExecutionProfileSet owns execution-profile lookup and canonical ordering.
RunnablePlanCompiler consumes GenerationPlan + FieldDefinitionSet + ExecutionProfile.
```

## Rejected: LandmassExecutionProfiles

Landmass-specific execution profile providers are rejected unless landmass owns a real execution-policy invariant.

Built-in execution profiles are package-level execution metadata under `Runtime/Execution`.

Landmass content may own resource definitions and field definitions. It must not own package-level execution policy by default.

## Reconsideration rule

A rejected option may be reconsidered only with a new decision record.

The new decision record must explain:

```text
what changed
why the original rejection no longer applies
which boundaries are affected
which tests protect the new model
which documentation must be updated
```

A deferred option may be implemented only when its prerequisite architecture is stable.

## Checklist

Before implementing a rejected or deferred concept, verify:

```text
Is this currently rejected?
If yes, create a new decision record before implementation.

Is this deferred?
If yes, confirm prerequisite current Runtime foundations are stable.

Does this cross the current GenerationPlan boundary?
If yes, place it in planned execution architecture.

Does this add storage to semantic objects or managed metadata objects?
If yes, reject or move it to workspace/field architecture.

Does this add scheduler behavior to definitions?
If yes, reject or move it to planned scheduler architecture.

Does this make Unity object identity domain identity?
If yes, reject.

Does this document future behavior as current behavior?
If yes, rewrite the documentation.
```

## Summary

Rejected options protect the managed Runtime boundary.

Current managed field metadata and current managed execution profile identity are implemented, but they are not execution runtime.

Deferred execution architecture starts after `GenerationPlan` and after the managed metadata inputs required for runnable compilation.
