# Implementation plan

This plan defines the recommended implementation order for Lokrain.Atlas.

The plan protects the managed Runtime boundary and delays native execution infrastructure until runnable metadata, workspace ownership, and scheduler ownership are stable.

## Status

Current managed Runtime stabilization is complete.

Managed field metadata and managed execution-profile identity are implemented.

The last verified pre-Phase 5 PlayMode test status was:

```text
870/870 PlayMode tests pass
```

Phase 5 is the managed runnable-plan bridge. Its code and documentation must be verified before any workspace, scheduler, native-storage, job, ECS, artifact, or runtime diagnostic implementation begins.

Do not update this plan to claim Phase 5 tests pass until Unity test execution confirms that result.

## Current implemented boundary after Phase 5

After Phase 5 is applied and verified, Runtime includes:

```text
Core values
Generation schemas
Resource definitions
Stage definitions
Stage contracts
Stage route definitions
Stage route step definitions
Operation definitions
Operation contracts
Operation implementation definitions
Generation recipes
Generation catalogs
Generation request descriptors
Generation request resolution
Generation requests
Generation plan compilation
Generation plans
Field value kinds
Field shapes
Field definitions
Field definition sets
Execution profiles
Execution profile sets
Built-in execution profile metadata
Landmass field metadata
Field indices
Stage indices
Operation indices
Field plan roles
Field capture policies
Resource field bindings
Runnable operations
Runnable stages
Runnable plans
Runnable plan compilation results and errors
Runnable plan compiler
```

The current managed semantic planning endpoint remains:

```text
GenerationPlan
```

The current managed executable metadata endpoint is:

```text
RunnablePlan
```

`RunnablePlan` is not runtime execution. It does not own field handles, native storage, scheduler state, jobs, ECS bindings, artifact capture execution, or runtime diagnostics.

## Future boundary

The following concepts remain future architecture after Phase 5:

```text
GenerationWorkspace
FieldHandle
WorkspaceAllocation
OperationScheduler
OperationScratch
Burst-compatible jobs
Native storage ownership
Artifact capture execution
Runtime diagnostic capture
ECS execution integration
```

Future concepts must not leak into current semantic Runtime objects, managed field metadata, execution profile metadata, or runnable metadata rows.

## Implementation principles

Implement one boundary at a time.

Do not mix semantic architecture with execution architecture.

Do not add execution state to managed semantic objects.

Do not make `GenerationCatalog` own field metadata or execution profiles.

Do not introduce native storage before runnable metadata and workspace ownership exist.

Do not introduce jobs before scheduler ownership and workspace data access are defined.

Do not introduce field lifetime or liveness metadata before workspace allocation and scheduler analysis exist.

Dictionaries and hash sets are lookup or membership structures only. Public order, generation order, diagnostic order, artifact order, and serialized order must come from lists, arrays, domain order, or canonical symbol ordering.

## Current Runtime validation checklist

Verify these names do not appear in current Runtime code except in documentation, decisions, or plans:

```text
LandmassResourceSymbols
RequiredInputSymbols
ProducedOutputSymbols
FieldLifetimeScope
FieldLifecyclePolicy
FieldBinding
SchedulerBinding
```

Verify current contracts use:

```text
IReadOnlyList<ResourceDefinition> RequiredInputs
IReadOnlyList<ResourceDefinition> ProducedOutputs
```

Verify current managed semantic planning objects do not contain:

```text
RunnablePlan
FieldHandle
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
Entity
UnityEngine.Object
```

Verify field metadata and execution profile metadata follow these rules:

```text
FieldDefinition maps one ResourceDefinition to managed field metadata.
FieldDefinitionSet owns field metadata lookup and canonical field-symbol ordering.
ExecutionProfile identifies managed execution-profile metadata.
ExecutionProfileSet owns profile lookup and canonical profile-symbol ordering.
GenerationCatalog does not own FieldDefinition.
GenerationCatalog does not own ExecutionProfile.
Built-in provider All lists preserve declared order.
Accepted set types expose deterministic canonical order.
```

Verify runnable metadata follows these rules:

```text
RunnablePlanCompiler consumes GenerationPlan, FieldDefinitionSet, and ExecutionProfile.
RunnablePlanCompiler is stateless.
Compile returns structured errors for deterministic validation failures.
CompileOrThrow throws for failed compilation.
RunnablePlanCompilationResult never contains a partial runnable plan.
ResourceFieldBinding validates reference-exact ResourceDefinition ownership.
FieldBindings are ordered by used FieldDefinition.Symbol.
Stages follow GenerationPlan.StagePlanNodes order.
Operations follow stage order, then operation order within each stage.
FieldIndex, StageIndex, and OperationIndex are dense zero-based table positions.
FieldPlanRole does not describe storage lifetime.
FieldCapturePolicy does not perform artifact or diagnostic capture.
```

## Phase 1: Documentation consistency

Status: complete for the current semantic and field metadata boundary. Phase 5 documentation updates must be applied after the runnable-plan bridge code overlay.

Goal: make architecture documentation match current Runtime and planned execution boundaries.

Completion criteria:

```text
Current documents present ResourceDefinition as semantic resource identity.
Current documents present FieldDefinition and FieldDefinitionSet as managed metadata.
Current documents present ExecutionProfile and ExecutionProfileSet as managed metadata.
Current documents present RunnablePlanCompiler and RunnablePlan as managed runnable metadata after Phase 5 is applied.
Current documents do not present GenerationWorkspace as implemented.
Current documents do not present OperationScheduler as implemented.
Current documents do not present jobs or ECS execution as implemented.
Current documents do not present artifact capture execution or runtime diagnostic capture as implemented.
Glossary defines terms only.
Naming guidelines discuss names only.
Architecture rules define governing rules only.
Future documents clearly state planned status.
Decision documents record rationale and rejected options.
```

## Phase 2: Current Runtime static consistency

Status: complete for semantic Runtime, managed field metadata, and execution-profile metadata. Pending verification after Phase 5 overlay.

Goal: verify current Runtime code matches documented architecture.

Completed work:

```text
ResourceDefinition is current semantic resource identity.
StageContract uses ResourceDefinition inputs and outputs.
OperationContract uses ResourceDefinition inputs and outputs.
GenerationCatalog owns ResourceDefinitions.
GenerationCatalog validates exact catalog ownership.
Generation modules expose ResourceDefinition groups.
Request descriptors remain symbolic.
Accepted requests contain accepted choices.
GenerationPlan contains no native execution state.
FieldDefinitionSet is independent from GenerationCatalog.
ExecutionProfileSet is independent from GenerationCatalog.
```

Phase 5 static verification:

```text
FieldLifetimeScope is absent.
FieldIndex, StageIndex, and OperationIndex use the same value-object pattern.
FieldPlanRole and FieldCapturePolicy are closed non-flags classifications.
ResourceFieldBinding contains no lifetime, scheduler, workspace, native, ECS, artifact execution, or diagnostic capture state.
RunnableOperation, RunnableStage, and RunnablePlan contain immutable managed metadata only.
RunnablePlanCompiler does not allocate, schedule, execute, capture, or bind ECS data.
```

## Phase 3: Current Runtime test gate

Status: complete before Phase 5. Pending rerun after Phase 5 overlay.

Goal: lock Runtime behavior before adding execution infrastructure.

Covered test areas before Phase 5:

```text
Core value object invariants.
Definition constructor invariants.
Symbol-based definition equality.
Contract resource-flow invariants.
Catalog ownership and graph validation.
Generation module built-in definition wiring.
Recipe route and implementation-choice invariants.
Request descriptor validation.
Request resolver success and failure results.
Accepted request validation.
Plan compiler ordering and operation-node creation.
Generation plan invariants.
Field metadata invariants.
Execution-profile metadata invariants.
```

Phase 5 test areas:

```text
FieldIndex, StageIndex, and OperationIndex invariants.
FieldPlanRole and FieldCapturePolicy stable values.
ResourceFieldBinding reference-exact ownership.
RunnablePlanCompilationErrorCode stable values.
RunnablePlanCompilationError invariants.
RunnablePlanCompilationResult success and failure invariants.
RunnableOperation invariants.
RunnableStage invariants.
RunnablePlan invariants.
RunnablePlanCompiler deterministic success and failure behavior.
```

## Phase 4: Managed field metadata and execution profiles

Status: complete.

Goal: add managed representation metadata without introducing executable runtime behavior.

Implemented concepts:

```text
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

Boundary rules:

```text
GenerationCatalog does not own FieldDefinition.
GenerationCatalog does not own ExecutionProfile.
FieldDefinitionSet owns field metadata lookup and canonical ordering.
ExecutionProfileSet owns execution-profile lookup and canonical ordering.
Landmass content owns field metadata but does not own execution policy.
```

## Phase 5: Managed runnable-plan bridge

Status: implementation overlay prepared; verification pending.

Goal: add deterministic managed executable metadata without introducing runtime execution infrastructure.

Implemented concepts after Phase 5:

```text
FieldIndex
StageIndex
OperationIndex
FieldPlanRole
FieldCapturePolicy
ResourceFieldBinding
RunnablePlanCompilationErrorCode
RunnablePlanCompilationError
RunnablePlanCompilationResult
RunnableOperation
RunnableStage
RunnablePlan
RunnablePlanCompiler
```

Explicitly excluded from Phase 5:

```text
FieldLifetimeScope
FieldBinding
SchedulerBinding
GenerationWorkspace
FieldHandle
WorkspaceAllocation
OperationScheduler
OperationScratch
NativeArray<T>
NativeList<T>
NativeHashMap<TKey,TValue>
JobHandle
Burst function pointers
ECS Entity
Artifact capture execution
Runtime diagnostic capture
Operation kernels
Scratch allocation
DAG edge execution
Kernel registry references
```

Completion criteria:

```text
All Phase 5 tests pass.
RunnablePlanCompiler creates deterministic field, stage, and operation tables.
Compilation errors are deterministic and structured.
No partial plans are returned on failure.
No runtime execution infrastructure is introduced.
Documentation reflects RunnablePlanCompiler and RunnablePlan as current managed metadata.
Workspace, scheduler, native storage, jobs, ECS, artifacts, and diagnostics remain future-only.
```

## Phase 6: Workspace ownership

Status: future.

Goal: define workspace-owned executable storage after runnable metadata is stable.

Candidate concepts:

```text
GenerationWorkspace
FieldHandle
WorkspaceAllocation
Field liveness analysis
Storage retention policy
```

## Phase 7: Scheduler ownership

Status: future.

Goal: define operation execution control flow after workspace ownership is stable.

Candidate concepts:

```text
OperationScheduler
OperationScratch
Dependency analysis
Job scheduling policy
Execution capability routing
```

## Phase 8: Jobs and native kernels

Status: future.

Goal: implement Burst-compatible executable operation code after scheduler ownership is stable.

Candidate concepts:

```text
Burst-compatible job structs
Native container access rules
Unmanaged operation parameters
Kernel registration
Deterministic scheduling constraints
```

## Phase 9: Artifacts, diagnostics, and Unity integration

Status: future.

Goal: add capture and Unity integration after core execution is deterministic and owned.

Candidate concepts:

```text
Artifact capture execution
Runtime diagnostic capture
ECS execution integration
GameObject or editor adapters
```
