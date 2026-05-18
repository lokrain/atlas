# Implementation plan

This plan defines the recommended implementation order for Lokrain.Atlas.

The plan protects the current managed Runtime boundary and delays native execution infrastructure until runnable metadata, workspace ownership, and scheduler ownership are stable.

## Status

Current managed Runtime stabilization is complete.

Managed field metadata and managed execution profile identity are implemented.

Verified PlayMode test status:

```text
870/870 PlayMode tests pass
````

The next implementation gate is Phase 5: managed runnable plan compilation.

## Current implemented boundary

The current Runtime includes:

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
```

The current Runtime endpoint remains:

```text
GenerationPlan
```

`GenerationPlan` is still managed semantic planning output. It is not executable metadata, does not own field handles, and does not allocate execution storage.

## Current future boundary

The following concepts remain future architecture:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldBinding
SchedulerBinding
GenerationWorkspace
FieldHandle
WorkspaceAllocation
OperationScheduler
OperationScratch
Burst-compatible jobs
Native storage ownership
Artifact capture
Execution diagnostics
ECS execution integration
```

Future concepts must not leak into current semantic Runtime objects.

## Implementation principles

Implement one boundary at a time.

Do not mix semantic architecture with execution architecture.

Do not add execution state to current managed semantic objects.

Do not make `GenerationCatalog` own field metadata or execution profiles.

Do not introduce native storage before runnable metadata and workspace ownership exist.

Do not introduce jobs before scheduler ownership and workspace data access are defined.

Dictionaries and hash sets are lookup or membership structures only. Public order, generation order, diagnostic order, artifact order, and serialized order must come from lists, arrays, domain order, or canonical symbol ordering.

## Current Runtime validation checklist

Verify these names do not appear in current Runtime code except in documentation, decisions, or plans:

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
ExecutionProfile identifies managed execution-policy metadata.
ExecutionProfileSet owns profile lookup and canonical profile-symbol ordering.
GenerationCatalog does not own FieldDefinition.
GenerationCatalog does not own ExecutionProfile.
Built-in provider All lists preserve declared order.
Accepted set types expose deterministic canonical order.
```

## Phase 1: Documentation consistency

Status: complete.

Goal: make architecture documentation match current Runtime and planned execution boundaries.

Completed work:

```text
README defines the architecture documentation set.
Glossary is term-only.
Architecture rules are concise and normative.
Detailed rules are split into focused guideline documents.
Current managed field metadata is documented as implemented.
Current execution profile identity metadata is documented as implemented.
Future execution concepts are marked planned.
Decision records contain rationale and rejected options.
Implementation order lives in this plan.
Current architecture documents avoid migration-history wording.
Stale resource-symbol terminology is removed from current Runtime documentation.
```

Completion criteria:

```text
Current documents present ResourceDefinition as current semantic resource identity.
Current documents present FieldDefinition and FieldDefinitionSet as current managed metadata.
Current documents present ExecutionProfile and ExecutionProfileSet as current managed metadata.
Current documents do not present RunnablePlan as implemented.
Current documents do not present GenerationWorkspace as implemented.
Current documents do not present OperationScheduler as implemented.
Current documents do not present jobs or ECS execution as implemented.
Glossary defines terms only.
Naming guidelines discuss names only.
Architecture rules define governing rules only.
Future documents clearly state planned status.
Decision documents record rationale and rejected options.
```

## Phase 2: Current Runtime static consistency

Status: complete.

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

Completion criteria:

```text
No stale resource-symbol API remains.
No contract exposes raw resource symbol lists.
No current managed plan object exposes future execution state.
Catalog validation owns semantic graph consistency.
Request resolution owns descriptor satisfiability.
Plan compilation owns managed semantic ordering.
Field metadata does not replace catalog ownership.
Execution profile metadata does not alter semantic resource identity.
```

## Phase 3: Current Runtime test gate

Status: complete.

Goal: lock current Runtime behavior before adding execution architecture.

Covered test areas:

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

Verified status:

```text
870/870 PlayMode tests pass
```

## Phase 4: Field metadata and execution profile identity

Status: complete.

Goal: add managed field metadata and managed execution profile identity without adding execution storage.

Implemented types:

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

Responsibilities:

```text
FieldValueKind describes current managed scalar value categories.
FieldShape describes current managed logical field shapes.
FieldDefinition maps a ResourceDefinition to managed field metadata.
FieldDefinitionSet validates field metadata and exposes deterministic canonical field-symbol order.
ExecutionProfile identifies current managed execution-policy metadata.
ExecutionProfileSet validates execution profile metadata and exposes deterministic canonical profile-symbol order.
Built-in execution profile metadata provides package-owned profile identity.
Landmass field metadata maps built-in landmass resources to managed field metadata.
```

Determinism rules:

```text
FieldDefinitionSet public order is canonical by field Symbol.
ExecutionProfileSet public order is canonical by profile Symbol.
Private dictionaries are lookup indexes only.
Private hash sets are membership guards only.
Built-in provider All lists preserve declared order.
Accepted set types define canonical order.
```

Not added in this phase:

```text
NativeArray<T>
FieldHandle
GenerationWorkspace
OperationScheduler
JobHandle
Burst jobs
RunnablePlan
RunnablePlanCompiler
```

Completion criteria:

```text
FieldDefinition does not allocate storage.
FieldDefinitionSet does not replace GenerationCatalog.
ExecutionProfile does not change semantic resource identity.
ExecutionProfileSet does not replace catalog ownership.
Current contracts still use ResourceDefinition.
GenerationPlan still contains no field handles.
Tests cover field metadata invariants.
Tests cover execution profile invariants.
Tests cover deterministic set ordering.
Documentation marks implemented managed metadata accurately.
```

Verified status:

```text
870/870 PlayMode tests pass
```

## Phase 5: Runnable plan compilation

Status: next.

Goal: add the managed bridge from semantic plans to executable metadata.

Add planned types:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldBinding
SchedulerBinding
```

Inputs:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
```

Responsibilities:

```text
RunnablePlanCompiler validates resource-to-field binding.
RunnablePlanCompiler validates profile availability and managed execution metadata compatibility.
RunnablePlan records immutable executable metadata.
RunnableStage records executable stage metadata.
RunnableOperation records executable operation metadata.
FieldBinding connects semantic resources to planned execution fields.
SchedulerBinding records managed scheduler-facing metadata without scheduling jobs.
```

Do not add:

```text
native storage allocation
job scheduling
workspace lifetime ownership
artifact writing
diagnostic capture
ECS integration
Unity object references
```

Completion criteria:

```text
Runnable compilation preserves GenerationPlan stage order.
Runnable compilation preserves route operation order.
Runnable compilation rejects missing field definitions.
Runnable compilation rejects duplicate or incompatible field metadata.
Runnable compilation rejects missing execution profile metadata.
RunnablePlan exposes read-only managed metadata.
RunnablePlan does not own native storage.
RunnablePlan does not contain NativeArray<T>, JobHandle, Entity, or UnityEngine.Object.
Tests cover deterministic runnable metadata creation.
Tests cover missing field-definition failures.
Tests cover execution-profile selection.
```

Recommended implementation order:

```text
1. Runtime/Execution/FieldBinding.cs
2. Tests/Runtime/Execution/FieldBindingTests.cs
3. Runtime/Execution/SchedulerBinding.cs
4. Tests/Runtime/Execution/SchedulerBindingTests.cs
5. Runtime/Execution/RunnableOperation.cs
6. Tests/Runtime/Execution/RunnableOperationTests.cs
7. Runtime/Execution/RunnableStage.cs
8. Tests/Runtime/Execution/RunnableStageTests.cs
9. Runtime/Execution/RunnablePlan.cs
10. Tests/Runtime/Execution/RunnablePlanTests.cs
11. Runtime/Execution/RunnablePlanCompiler.cs
12. Tests/Runtime/Execution/RunnablePlanCompilerTests.cs
```

Do not start with native storage, scheduler execution, or jobs.

## Phase 6: Workspace ownership

Status: future.

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

Status: future.

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

Status: future.

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

Status: future.

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

Status: future.

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
4. Field metadata and execution profile identity.
5. Runnable plan compilation.
6. Workspace ownership.
7. Scheduler ownership.
8. Burst-compatible jobs.
9. Artifacts and execution diagnostics.
10. Unity and ECS integration.
```

Do not skip from `GenerationPlan` directly to jobs.

Do not add native storage before workspace ownership exists.

Do not add scheduler behavior before runnable metadata exists.

Do not make `GenerationCatalog` own field definitions or execution profiles.

## Review gates

Before starting a new phase, verify the previous phase is stable.

A phase is stable when:

```text
public API names match the architecture vocabulary
tests cover owned invariants
documentation matches implemented status
future concepts are not described as current behavior
dependency direction is preserved
deterministic public order is explicit
lookup collections do not define public order
```

## Stop conditions

Stop implementation and return to architecture review when:

```text
a type gains more than one responsibility
a lower layer depends on a higher layer
a current managed object receives execution state
GenerationCatalog starts owning field definitions
GenerationCatalog starts owning execution profiles
a descriptor receives accepted definitions
a request receives unresolved symbols
a plan receives native storage or job state
a runnable plan receives native storage ownership
a job receives semantic metadata
Unity object identity becomes domain identity
public order depends on Dictionary or HashSet enumeration
```

## Current next action

Start Phase 5 with the smallest managed bridge type:

```text
Runtime/Execution/FieldBinding.cs
```

`FieldBinding` must connect a `ResourceDefinition` to a `FieldDefinition` without allocating storage, creating handles, scheduling jobs, or depending on workspace infrastructure.

After each Phase 5 file:

```text
run the full PlayMode test suite
keep documentation synchronized
verify current managed Runtime remains free of native execution state
```

``` 