# Implementation plan

This plan defines the recommended implementation order for Lokrain.Atlas.

The plan protects the current managed Runtime boundary and delays execution infrastructure until semantic planning is stable.

Current Runtime architecture ends at `GenerationPlan`.

## Status

Current managed Runtime stabilization is the active gate.

Future execution work starts only after the current Runtime gate is stable.

## Implementation principles

Implement one boundary at a time.

Do not mix semantic architecture with execution architecture.

Do not add future execution state to current managed Runtime objects.

Do not introduce native storage before runnable metadata and workspace ownership exist.

Do not introduce jobs before scheduler ownership and workspace data access are defined.

## Current Runtime gate

The current Runtime gate includes:

```text
Core values
Generation schemas
Resource definitions
Stage and operation definitions
Stage and operation contracts
Generation recipes
Generation catalogs
Generation request descriptors
Generation request resolution
Generation requests
Generation plan compilation
Generation plans
```

The gate is complete when:

```text
Runtime APIs match architecture documentation.
Tests cover local invariants and graph validation.
Catalog ownership is reference-exact.
Contracts use ResourceDefinition inputs and outputs.
Requests contain accepted definitions and no unresolved symbols.
Plans contain managed semantic data only.
Documentation does not describe future concepts as current behavior.
```

## Current Runtime validation checklist

Verify these names do not appear in current Runtime code except in future documentation, decisions, or plans:

```text
LandmassResourceSymbols
RequiredInputSymbols
ProducedOutputSymbols
```

Verify current contracts use:

```text
IReadOnlyList<ResourceDefinition> RequiredInputs
IReadOnlyList<ResourceDefinition> ProducedOutputs
```

Verify current managed planning objects do not contain:

```text
FieldDefinition
RunnablePlan
FieldHandle
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
Entity
UnityEngine.Object
```

## Phase 1: Documentation consistency

Goal: make architecture documentation match current Runtime and planned execution boundaries.

Tasks:

```text
Update README navigation and documentation-set contract.
Keep glossary term-only.
Keep architecture rules concise and normative.
Split detailed rules into focused guideline documents.
Mark future execution concepts as planned.
Move rationale to decision records.
Move work order to this plan.
Remove migration-history wording from current architecture documents.
Remove stale resource-symbol terminology.
```

Completion criteria:

```text
No current document presents FieldDefinition as implemented.
No current document presents RunnablePlan as implemented.
No current document presents GenerationWorkspace as implemented.
No current document presents OperationScheduler as implemented.
No current document presents jobs or ECS execution as implemented.
Glossary defines terms only.
Naming guidelines discuss names only.
Architecture rules define governing rules only.
Future documents clearly state planned status.
Decision documents record rationale and rejected options.
```

## Phase 2: Current Runtime static consistency

Goal: verify current Runtime code matches documented architecture.

Tasks:

```text
Search Runtime and Tests for stale symbol-list terminology.
Verify ResourceDefinition is current semantic resource identity.
Verify StageContract uses ResourceDefinition inputs and outputs.
Verify OperationContract uses ResourceDefinition inputs and outputs.
Verify GenerationCatalog owns ResourceDefinitions.
Verify catalog validation rejects symbol-equivalent external definitions.
Verify generation modules expose ResourceDefinition groups.
Verify request descriptors remain symbolic.
Verify accepted requests contain accepted choices.
Verify plans contain no native execution state.
```

Completion criteria:

```text
No stale resource-symbol API remains.
No contract exposes raw resource symbol lists.
No current managed plan object exposes future execution state.
Catalog validation owns graph consistency.
Request resolution owns descriptor satisfiability.
Plan compilation owns managed semantic ordering.
```

## Phase 3: Current Runtime test gate

Goal: lock current Runtime behavior before adding future execution architecture.

Required test areas:

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
```

Completion criteria:

```text
Full PlayMode test suite passes.
Tests cover ResourceDefinition exact ownership.
Tests cover symbol-equivalent but non-owned definition rejection.
Tests cover request-resolution errors as result objects.
Tests cover managed plan ordering.
Tests do not assert implementation details that should remain private.
```

## Phase 4: Field metadata

Goal: add storage-facing metadata without adding execution storage.

Add planned types:

```text
FieldDefinition
FieldDefinitionSet
FieldShape
FieldValueKind
ExecutionProfile
```

Responsibilities:

```text
FieldDefinition maps a ResourceDefinition to storage-facing metadata.
FieldDefinitionSet validates field metadata consistency.
FieldShape describes planned structural shape.
FieldValueKind describes planned stored value category.
ExecutionProfile describes planned execution policy.
```

Do not add:

```text
NativeArray<T>
FieldHandle
GenerationWorkspace
OperationScheduler
JobHandle
Burst jobs
```

Completion criteria:

```text
FieldDefinition does not allocate storage.
FieldDefinitionSet does not replace GenerationCatalog.
ExecutionProfile does not change semantic resource identity.
Current contracts still use ResourceDefinition.
GenerationPlan still contains no field handles.
Tests cover field metadata invariants.
Documentation marks implemented field metadata accurately after code exists.
```

## Phase 5: Runnable plan compilation

Goal: add the bridge from managed semantic plans to executable metadata.

Add planned types:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldBinding
SchedulerBinding
```

Responsibilities:

```text
RunnablePlanCompiler validates resource-to-field binding.
RunnablePlan records immutable executable metadata.
RunnableStage records executable stage metadata.
RunnableOperation records executable operation metadata.
FieldBinding connects semantic resources to planned execution fields.
SchedulerBinding connects runnable operations to scheduler metadata.
```

Do not add:

```text
native storage allocation
job scheduling
workspace lifetime ownership
artifact writing
ECS integration
```

Completion criteria:

```text
Runnable compilation preserves GenerationPlan stage order.
Runnable compilation preserves route operation order.
Runnable compilation rejects missing field definitions.
Runnable compilation rejects incompatible field metadata.
RunnablePlan exposes read-only metadata.
RunnablePlan does not own native storage.
Tests cover deterministic runnable metadata creation.
```

## Phase 6: Workspace ownership

Goal: add native storage ownership for one generation run.

Add planned types:

```text
GenerationWorkspace
FieldHandle
WorkspaceAllocation
WorkspaceFieldStorage
ExternalFieldBinding
DiagnosticFieldStorage
```

Responsibilities:

```text
GenerationWorkspace owns native allocation lifetime.
FieldHandle identifies workspace-owned field storage during execution.
WorkspaceAllocation records allocation metadata.
ExternalFieldBinding records borrowed or caller-owned data.
DiagnosticFieldStorage owns diagnostic capture storage.
```

Do not add scheduler policy to workspace.

Do not make workspace resolve symbols or choose recipes.

Completion criteria:

```text
Workspace owns allocation and disposal.
Field handles are valid only for the owning workspace.
External binding ownership is explicit.
Disposed workspace access fails predictably.
No semantic Runtime object stores field handles.
Tests cover allocation, disposal, invalid handle use, and wrong-workspace use.
```

## Phase 7: Scheduler ownership

Goal: add operation execution control flow.

Add planned types:

```text
OperationScheduler
OperationScratch
SchedulerExecutionContext
OperationExecutionResult
OperationExecutionDiagnostic
```

Responsibilities:

```text
OperationScheduler owns dependency wiring.
OperationScheduler schedules jobs.
OperationScheduler owns operation scratch policy.
OperationScheduler owns iteration and termination policy.
OperationScheduler owns execution failure policy.
```

Do not make schedulers own semantic identity.

Do not make definitions schedule themselves.

Completion criteria:

```text
Schedulers receive runnable metadata and workspace access.
Schedulers do not perform catalog lookup.
Schedulers do not resolve descriptors.
Schedulers do not compile managed plans.
Schedulers schedule deterministic work.
Tests cover dependency ordering and scratch lifetime.
```

## Phase 8: Job implementation

Goal: implement deterministic Burst-compatible generation jobs.

Add jobs only after runnable metadata, workspace storage, and scheduler ownership exist.

Job rules:

```text
Jobs receive native containers and unmanaged values only.
Jobs do not receive Symbol.
Jobs do not receive ResourceDefinition.
Jobs do not receive GenerationPlan.
Jobs do not receive GenerationWorkspace.
Jobs do not receive OperationScheduler.
Jobs do not access UnityEngine.Object or UnityEditor.
```

Completion criteria:

```text
Jobs are Burst-compatible where required.
Jobs use explicit dimensions and seeds.
Jobs avoid managed allocations.
Jobs avoid nondeterministic ordering.
Jobs are scheduled by OperationScheduler.
Tests cover deterministic repeated output.
```

## Phase 9: Artifacts and diagnostics

Goal: add captured output and execution diagnostics.

Add planned areas:

```text
Artifact capture
Artifact metadata
Diagnostic field capture
Workspace allocation diagnostics
Scheduler timing diagnostics
Operation execution diagnostics
```

Responsibilities:

```text
Artifacts capture selected execution outputs.
Diagnostics explain execution behavior and validation results.
Capture policy comes from execution profile and runnable metadata.
```

Do not store artifacts in semantic definitions, requests, or managed plans.

Completion criteria:

```text
Artifact capture reads workspace data through explicit policy.
Diagnostics do not alter semantic output.
Diagnostics do not leak into current managed Runtime objects.
Tests cover capture policy and diagnostic selection.
```

## Phase 10: Unity and ECS integration

Goal: adapt execution results into Unity-facing workflows.

Potential integration areas:

```text
ScriptableObject authoring adapters
Editor preview tools
Import/export tools
ECS output adapters
ECS execution integration
```

Rules:

```text
Unity adapters translate data into or out of Atlas domain objects.
Unity object identity does not become domain identity.
Runtime semantic objects do not depend on UnityEditor.
ECS integration stays outside current managed planning objects.
```

Completion criteria:

```text
Authoring adapters do not replace Runtime domain objects.
Editor tooling does not leak into Runtime assemblies.
ECS integration does not change semantic resource identity.
Tests or validation tools cover adapter behavior.
```

## Dependency order

Recommended implementation order:

```text
1. Documentation consistency.
2. Current Runtime static consistency.
3. Current Runtime test gate.
4. FieldDefinition and FieldDefinitionSet.
5. ExecutionProfile.
6. RunnablePlanCompiler and RunnablePlan.
7. GenerationWorkspace and FieldHandle.
8. OperationScheduler and OperationScratch.
9. Burst-compatible jobs.
10. Artifacts and execution diagnostics.
11. Unity and ECS integration.
```

Do not skip from `GenerationPlan` directly to jobs.

Do not add native storage before workspace ownership exists.

Do not add scheduler behavior before runnable metadata exists.

## Review gates

Before starting a new phase, verify the previous phase is stable.

A phase is stable when:

```text
public API names match the architecture vocabulary
tests cover owned invariants
documentation matches implemented status
future concepts are not described as current behavior
dependency direction is preserved
```

## Stop conditions

Stop implementation and return to architecture review when:

```text
a type gains more than one responsibility
a lower layer depends on a higher layer
a current managed object receives execution state
a descriptor receives accepted definitions
a request receives unresolved symbols
a plan receives native storage or job state
a job receives semantic metadata
Unity object identity becomes domain identity
```

## Current next action

After the current documentation pass, perform a static consistency scan for:

```text
LandmassResourceSymbols
RequiredInputSymbols
ProducedOutputSymbols
FieldDefinition as current behavior
RunnablePlan as current behavior
GenerationWorkspace as current behavior
OperationScheduler as current behavior
```

Then run the full PlayMode test suite.

If tests pass and documentation is consistent, begin Phase 4 with managed field metadata.

## Summary

Stabilize current managed Runtime first.

Keep current architecture semantic through `GenerationPlan`.

Introduce field metadata before runnable metadata.

Introduce runnable metadata before workspace storage.

Introduce workspace storage before schedulers.

Introduce schedulers before jobs.

Introduce artifacts, diagnostics, Unity adapters, and ECS integration last.