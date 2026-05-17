# Implementation plan

This document defines the ordered implementation work for Lokrain.Atlas.

It does not redefine architecture. Architecture rules, concepts, terminology, and future execution boundaries are defined by the other architecture documents.

## Scope

This plan covers work after the current managed architecture baseline.

The current implemented architecture ends at `GenerationPlan`.

Future execution work starts only after the managed model, catalog ownership, resource contracts, request resolution, plan compilation, tests, and documentation are stable.

## Implementation principles

Work proceeds in dependency order.

Do not implement execution infrastructure before the semantic model it depends on is stable.

Do not introduce native memory ownership into current catalog, recipe, request, or plan objects.

Do not introduce field definitions into current stage or operation contracts.

Do not introduce schedulers, jobs, field handles, or native containers into managed plans.

Do not add unsafe infrastructure until its owner, lifetime, access mode, deterministic ordering, and tests are defined.

## Phase 1: Documentation structure

Create the final architecture documentation structure:

```text
Documentation/Architecture/
  README.md

  Overview/
    Atlas Architecture Overview.md
    Managed Generation Pipeline.md

  Concepts/
    Accepted Domain Object Model.md
    Catalog Recipe Request and Plan Model.md
    Resource Field and Workspace Boundary.md

  Guidelines/
    Architecture Rules.md
    Naming Guidelines.md
    Dependency Rules.md
    Error Handling Rules.md

  Reference/
    Glossary.md

  Future/
    Field Definition and Execution Profiles.md
    Runnable Plan Compilation.md
    Scheduler Workspace and Job Ownership.md
    Low-Level Native Memory and Unsafe Collections.md

  Decisions/
    ResourceDefinition Before FieldDefinition.md
    Rejections and Deferrals.md

  Plans/
    Implementation Plan.md
````

Acceptance criteria:

```text
README describes the final structure.
Glossary contains current and future terms with clear status.
Current docs do not describe future execution as implemented behavior.
Future docs are explicitly marked as planned architecture.
Decision docs contain rationale and rejected options.
Plan docs contain work order only.
```

## Phase 2: Managed architecture lock

Verify and lock the current managed Runtime architecture.

Required state:

```text
ResourceDefinition exists.
StageContract uses ResourceDefinition inputs and outputs.
OperationContract uses ResourceDefinition inputs and outputs.
GenerationCatalog owns ResourceDefinitions.
GenerationCatalog validates exact resource ownership.
GenerationCatalogBuilder supports AddResourceDefinition.
GenerationCatalogBuilder supports AddResourceDefinitions.
LandmassResourceDefinitions exists.
LandmassResourceSymbols is absent.
RequiredInputSymbols is absent from Runtime and Tests.
ProducedOutputSymbols is absent from Runtime and Tests.
ResourceDefinitionTests exist.
```

Acceptance criteria:

```text
All Runtime tests pass.
All ResourceDefinition tests pass.
Catalog ownership tests reject cross-catalog resource references.
Stage and operation contract tests assert ResourceDefinition-based contracts.
Landmass tests use LandmassResourceDefinitions.
No Runtime/Test C# file references obsolete resource-symbol contract names.
```

## Phase 3: Managed API consistency

Review current public Runtime API for naming, ownership, and constructor consistency.

Areas:

```text
Core
Schemas
Resources
Stages
Operations
Catalog
Recipes
Requests
Plans
Generation/Landmass
```

Required checks:

```text
Accepted objects validate local invariants.
Constructors reject null required values.
Enumerable inputs are copied before storage.
Collections expose read-only views.
Duplicate handling is owned by the correct boundary.
Symbol identity is used for machine identity.
DisplayName is presentation only.
Grid uses Width and Depth.
Height means elevation only.
Try methods follow .NET Try pattern.
Parse/Create naming is consistent.
```

Acceptance criteria:

```text
Public API names match Naming Guidelines.
Invalid API usage throws precise standard exceptions.
Expected request-resolution failure returns result errors.
No accepted object exposes mutable caller-owned collections.
No managed planning type contains native execution state.
```

## Phase 4: Catalog and request-resolution hardening

Harden catalog validation and request-resolution behavior.

Catalog validation must cover:

```text
unique symbols
schema consistency
resource ownership
stage ownership
route ownership
route-step consistency
operation implementation compatibility
contract resource ownership
recipe graph consistency
cross-catalog object reuse
```

Request resolution must cover:

```text
missing recipe symbol
missing override target route-step symbol
missing implementation symbol
implementation incompatible with targeted route-step operation
duplicate override target
stable error ordering
```

Acceptance criteria:

```text
Catalog rejects invalid inventory during construction.
Resolver returns structured errors for expected satisfiability failures.
Resolver does not throw for valid descriptors that the catalog cannot satisfy.
Resolver does not compile plans.
Plan compiler does not resolve descriptor symbols.
Tests assert error codes and subject symbols, not diagnostic message text.
```

## Phase 5: Managed plan verification

Verify `GenerationPlanCompiler` and managed plan node behavior.

Required checks:

```text
Plan compiler consumes GenerationRequest only.
Plan compiler preserves accepted recipe stage order.
Plan compiler preserves route-step operation order.
StagePlanNode contains accepted stage, route, contract, and operation nodes.
OperationPlanNode contains accepted route step, operation, contract, and implementation.
Plan does not contain field definitions, field handles, native containers, schedulers, jobs, or dependency handles.
```

Acceptance criteria:

```text
Valid request compiles into deterministic plan structure.
Plan node order is explicit and stable.
Plan tests cover repeated operation definitions in different route-step occurrences.
Plan tests cover contract resource preservation.
No plan type references future execution concepts.
```

## Phase 6: Landmass module stabilization

Stabilize the built-in landmass module as the reference generation module.

Required surfaces:

```text
LandmassResourceDefinitions
LandmassStageDefinitions
LandmassOperationDefinitions
LandmassOperationImplementationDefinitions
LandmassRecipeDefinitions
LandmassCatalogs
LandmassRequestDescriptors
```

Acceptance criteria:

```text
Landmass module builds a valid catalog.
Landmass recipes use ResourceDefinition-based contracts.
Landmass request helpers produce descriptors, not accepted requests.
Landmass helpers do not bypass resolver or plan compiler.
Landmass names follow Naming Guidelines.
Landmass tests cover catalog, request resolution, and plan compilation.
```

## Phase 7: Future field model design

Start future execution design with field metadata only.

Implement only after current managed architecture is stable.

Planned concepts:

```text
FieldDefinition
FieldDefinitionSet
FieldLifetime
FieldShape
ValueKind
ExecutionProfile
StoragePolicy
CapturePolicy
```

Required boundaries:

```text
FieldDefinition binds to ResourceDefinition.
FieldDefinition is storage-facing metadata.
FieldDefinition does not allocate native memory.
FieldDefinition does not schedule jobs.
StageContract and OperationContract remain ResourceDefinition-based.
GenerationPlan remains managed semantic data.
```

Acceptance criteria:

```text
Field definitions validate local invariants.
Field definition sets validate uniqueness and compatibility.
Field definitions do not appear in current contracts.
Field definitions do not appear in current managed plan nodes.
Tests cover resource-to-field metadata consistency.
```

## Phase 8: Runnable plan compilation design

Introduce executable metadata compilation after field metadata exists.

Planned concepts:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldBinding
FieldAccess
SchedulerBinding
SchedulerBindingCatalog
RunnablePlanCompilationResult
RunnablePlanCompilationError
RunnablePlanCompilationErrorCode
```

Required boundaries:

```text
RunnablePlanCompiler consumes GenerationPlan.
RunnablePlanCompiler consumes FieldDefinitionSet.
RunnablePlanCompiler consumes ExecutionProfile.
RunnablePlanCompiler consumes SchedulerBindingCatalog.
RunnablePlanCompiler does not resolve request descriptors.
RunnablePlanCompiler does not allocate native storage.
RunnablePlanCompiler does not schedule jobs.
RunnablePlan is immutable executable metadata.
```

Acceptance criteria:

```text
Valid managed plan compiles to runnable metadata.
Missing field definition produces structured runnable compilation error.
Incompatible field definition produces structured runnable compilation error.
Missing scheduler binding produces structured runnable compilation error.
Output ordering is deterministic.
Runnable plan contains no native containers or JobHandles.
```

## Phase 9: Workspace ownership design

Introduce native storage ownership after runnable metadata exists.

Planned concepts:

```text
GenerationWorkspace
FieldHandle
FieldAccess
WorkspaceLayout
ExternalFieldBinding
WorkspaceAllocationResult
```

Required boundaries:

```text
Workspace owns native allocation.
Workspace owns field handle mapping.
Workspace owns disposal.
Workspace consumes RunnablePlan metadata.
Workspace does not resolve symbols.
Workspace does not choose recipes.
Workspace does not compile plans.
Workspace does not schedule jobs.
```

Acceptance criteria:

```text
Workspace allocates storage from runnable metadata.
Field handles are valid only inside their owning workspace.
Disposal ownership is explicit.
Disposed workspace access is rejected.
External field ownership is explicit.
Workspace tests cover allocation, access, disposal, invalid handles, and external binding.
```

## Phase 10: Scheduler ownership design

Introduce execution orchestration after workspace access is stable.

Planned concepts:

```text
OperationScheduler
SchedulerBinding
OperationScratch
ExecutionResult
ExecutionError
ExecutionErrorCode
```

Required boundaries:

```text
Scheduler owns dependency wiring.
Scheduler owns job scheduling.
Scheduler owns operation scratch policy.
Scheduler owns repeated job-chain control.
Scheduler owns execution failure policy.
Scheduler consumes RunnableOperation and workspace access.
Scheduler does not resolve symbols.
Scheduler does not inspect request descriptors.
Scheduler does not mutate catalog, request, managed plan, or runnable plan.
```

Acceptance criteria:

```text
Scheduler receives accepted executable metadata.
Scheduler requests workspace access explicitly.
Scheduler returns execution state or structured execution failure.
Scheduler tests cover dependency wiring and deterministic order.
Scheduler tests cover scratch allocation and disposal.
No scheduler code mutates managed semantic objects.
```

## Phase 11: Job implementation rules

Introduce Burst-compatible jobs only behind scheduler-owned execution.

Required job boundaries:

```text
Jobs receive native containers.
Jobs receive unmanaged parameters.
Jobs receive primitive grid and seed data when needed.
Jobs do not receive Symbol.
Jobs do not receive DisplayName.
Jobs do not receive GenerationCatalog.
Jobs do not receive GenerationRecipeDefinition.
Jobs do not receive GenerationRequestDescriptor.
Jobs do not receive GenerationRequest.
Jobs do not receive GenerationPlan.
Jobs do not receive ResourceDefinition.
Jobs do not receive FieldDefinition.
Jobs do not receive GenerationWorkspace.
Jobs do not receive OperationScheduler.
```

Acceptance criteria:

```text
Jobs are Burst-compatible where required.
Jobs allocate no managed memory.
Jobs do not use managed metadata.
Jobs avoid nondeterministic writes.
Job tests verify deterministic output.
Job tests verify scheduler-prepared native input.
```

## Phase 12: Low-level native infrastructure

Introduce low-level native infrastructure only after field, runnable, workspace, and scheduler ownership boundaries are stable.

Required order:

```text
scratch allocator
field pool views and addresses
memory utility layer
topology builders
emission streams
diagnostic buffers
custom native containers
```

### Scratch allocator

Purpose:

```text
Provide run-owned and phase-owned temporary allocation.
Guarantee rewind or dispose only after dependent jobs complete.
```

Acceptance criteria:

```text
Allocation and disposal tests pass.
Rewind-after-complete tests pass.
Use-after-rewind negative tests exist where practical.
Dispose-after-job-dependency tests pass.
Scratch data cannot escape owning scope.
```

### Field pool views and addresses

Purpose:

```text
Allow jobs to access compiler-resolved field ranges without owning workspace memory.
Contain aliasing and pool access rules.
```

Acceptance criteria:

```text
Field address ranges are validated.
Typed views validate shape and value kind.
Write ranges are unique or synchronized.
Safety-disabled aliasing proof tests exist where required.
Jobs receive resolved numeric/native views only.
```

### Memory utility layer

Purpose:

```text
Centralize clear, copy, compare, move, and hash operations.
Prevent ad-hoc pointer manipulation.
```

Acceptance criteria:

```text
Invalid range tests pass.
Invalid format tests pass.
Overlapping copy tests pass.
Initialization tests pass.
Deterministic hash tests pass.
Unsafe pointer use is isolated.
```

### Topology builders

Purpose:

```text
Build sparse or variable-length topology.
Freeze into deterministic arrays.
```

Acceptance criteria:

```text
Mutable topology does not become canonical.
Frozen topology is ordered, validated, hashable, and serializable.
Build, sort, compact, and freeze tests pass.
Canonical graph state uses arrays, not managed object graphs.
```

### Emission streams

Purpose:

```text
Support parallel variable output without making append order canonical.
```

Acceptance criteria:

```text
Raw append order is not canonical.
Merge and sort steps are deterministic.
Canonical keys are explicit.
Tests cover stable output order.
```

### Diagnostic buffers

Purpose:

```text
Capture optional variable diagnostics without polluting canonical fields.
```

Acceptance criteria:

```text
Diagnostics use typed writer APIs.
Schema-less byte dumps are not exposed.
Diagnostic streams are excluded from canonical output unless normalized.
High-volume diagnostic tests pass.
```

### Custom native containers

Purpose:

```text
Encode repeated Atlas invariants that standard containers cannot represent.
```

Acceptance criteria:

```text
An architecture decision record exists.
The invariant is stable and repeated.
Lower tiers are insufficient.
Safety-handle behavior is defined.
Disposal behavior is defined.
Burst and Jobs support is tested.
Migration path is defined.
```

## Phase 13: Data structure selection

Apply data structures by data shape.

Required policies:

```text
Dense raster data uses row-major one-dimensional storage.
Fixed-degree topology uses direct computation or fixed-stride arrays.
Variable one-to-many topology uses frozen compressed sparse layouts.
Mutable topology builders freeze before canonical use.
Connectivity labeling uses union-find or deterministic flood-fill frontiers.
Priority propagation uses deterministic priority structures with explicit tie-breaking.
Spatial lookup uses chunk buckets before trees.
Canonical graph state uses arrays of nodes, edges, offsets, counts, and values.
Managed object graphs are not canonical generation data.
```

Acceptance criteria:

```text
Dense field storage is one-dimensional.
Coordinate helpers do not change canonical storage shape.
Topology builders freeze into deterministic arrays.
Priority queues define tie-breaking.
Spatial acceleration choice is justified by data shape and workload.
Canonical generation data is hashable and serializable.
```

## Phase 14: Artifact and hashing infrastructure

Introduce artifact and hashing infrastructure after workspace storage and deterministic layout rules are stable.

Planned concepts:

```text
ArtifactLayout
ArtifactBuilder
ArtifactFieldWriter
ArtifactHashBuilder
ArtifactFieldRecord
```

Required boundaries:

```text
Artifact layout is based on compiled metadata.
Artifact field order is stable.
Hash order is stable.
Raw memory copies are allowed only for explicitly defined internal binary sections.
Public, cross-version, or cross-platform payloads use explicit encoding.
```

Acceptance criteria:

```text
Artifact round-trip tests pass.
Byte order is defined.
Version is defined.
Section length is defined.
Hash order is defined.
Migration policy is defined where required.
Raw memory writes are rejected when schema requires explicit encoding.
```

## Phase 15: Diagnostics and tracing

Introduce diagnostics after core execution is stable.

Planned concepts:

```text
DiagnosticEvent
DiagnosticEventStream
OperationTraceBuffer
ValidationEventBuffer
DiagnosticPayloadWriter
```

Required boundaries:

```text
Diagnostics are optional.
Diagnostics are typed.
Diagnostics do not become canonical output unless explicitly normalized.
Diagnostics do not change deterministic generation output.
```

Acceptance criteria:

```text
Typed diagnostic writer APIs exist.
Diagnostic payload schemas are defined.
Diagnostic streams do not leak into canonical fields.
High-volume diagnostic tests pass.
Diagnostics can be disabled without changing generation output.
```

## Phase 16: Unity and ECS integration

Introduce Unity-facing integration after Runtime domain and execution boundaries are stable.

Allowed adapters:

```text
ScriptableObject authoring assets
Editor validation windows
importers
MonoBehaviour integration shims
ECS execution adapters
ECS Graphics payload consumers
```

Required boundaries:

```text
Unity adapters translate into Atlas descriptors, definitions, profiles, or execution inputs.
Unity object identity is not package-domain identity.
Asset paths are not deterministic generation identity.
ECS World is not catalog ownership.
GameObjects are not canonical generation state.
```

Acceptance criteria:

```text
Runtime domain code does not depend on UnityEditor.
Current managed planning does not depend on Unity scene state.
Unity adapters do not bypass catalog validation.
Unity adapters do not bypass request resolution.
Unity adapters do not bypass plan compilation.
ECS integration consumes accepted execution metadata.
```

## Phase 17: Performance and benchmark gates

Performance work must be measured.

Benchmark categories:

```text
managed request resolution
managed plan compilation
field binding compilation
workspace allocation
scratch allocation
field clear/copy/hash
topology build/freeze
parallel emission merge/sort
scheduler overhead
job execution
artifact writing
```

Acceptance criteria:

```text
Benchmarks use target Unity version.
Benchmarks use target Collections version.
Benchmarks specify Burst configuration.
Benchmarks specify safety-check configuration.
Benchmarks use representative data sizes.
Performance-motivated unsafe code has benchmark evidence.
Benchmark-only unsafe changes still satisfy maintainability and safety requirements.
```

## Phase 18: Final architecture gates

Before execution architecture is considered stable, verify:

```text
Current managed architecture remains clean through GenerationPlan.
ResourceDefinition remains semantic identity.
FieldDefinition remains storage-facing metadata.
RunnablePlan remains immutable executable metadata.
GenerationWorkspace owns native storage.
OperationScheduler owns orchestration.
Jobs receive native containers and unmanaged values only.
Unsafe code is isolated in infrastructure.
Canonical data is deterministic, ordered, hashable, and serializable.
Artifacts have stable layout and hashing.
Diagnostics do not pollute canonical output.
Unity adapters do not become domain state.
```

## Work that must not be started early

Do not implement these before their owning boundaries exist:

```text
custom native containers
unsafe field views
scheduler job graphs
artifact binary writers
ECS execution adapters
external memory bridges
global execution services
native workspace pooling
```

Early implementation of these items creates pressure to move execution state into managed planning objects.

## Summary

The implementation order is:

```text
documentation structure
managed architecture lock
managed API consistency
catalog and resolver hardening
managed plan verification
landmass module stabilization
future field model
runnable plan compilation
workspace ownership
scheduler ownership
jobs
low-level native infrastructure
data structure selection enforcement
artifact and hashing infrastructure
diagnostics and tracing
Unity and ECS integration
performance gates
final architecture gates
```

The current architecture must remain semantic and managed through `GenerationPlan`.

Future execution must add storage, scheduling, jobs, unsafe memory, artifacts, and diagnostics only behind explicit ownership boundaries.

```