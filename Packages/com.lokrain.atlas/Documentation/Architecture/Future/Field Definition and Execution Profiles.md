# Field definition and execution profiles

This document defines planned architecture.

`FieldDefinition`, `FieldDefinitionSet`, and execution profiles are future execution concepts. They are not current Runtime behavior unless corresponding code exists.

## Purpose

The current managed architecture uses `ResourceDefinition` to describe semantic generated values.

Future execution needs storage-facing metadata that describes how those resources are represented during execution.

The planned boundary is:

```text
ResourceDefinition
        |
        v
FieldDefinition
        |
        v
GenerationWorkspace allocation
````

A resource defines what the generated value means.

A field definition defines how that value is represented for execution.

A workspace allocation owns the native memory for one run.

## Non-goals

Field definitions must not replace resource definitions.

Field definitions must not be added to current stage or operation contracts.

Field definitions must not allocate native memory.

Field definitions must not schedule jobs.

Field definitions must not own operation control flow.

Field definitions must not become artifact data by themselves.

Field definitions are metadata for future runnable plan compilation and workspace allocation.

## Resource versus field

A `ResourceDefinition` is semantic identity.

A `FieldDefinition` is storage-facing execution metadata.

Example:

```text
ResourceDefinition
  Height

FieldDefinition
  Height as cell-grid float field

GenerationWorkspace
  NativeArray<float> allocated for Height in this run
```

The same resource can have different field definitions under different execution profiles.

Examples:

```text
Height -> cell-grid float field
Height -> cell-grid half-precision field
Height -> diagnostic-captured float field
Land   -> byte mask field
Land   -> bit-packed mask field
```

The semantic resource remains stable. The field representation can vary.

## FieldDefinition

`FieldDefinition` is a planned accepted object.

A field definition describes how a resource is represented for execution.

A field definition may contain:

```text
Symbol
DisplayName
ResourceDefinition
FieldLifetime
FieldShape
ValueKind
ExecutionProfileSymbol or profile binding
CapturePolicy
StoragePolicy
```

Exact API shape is not defined here. The architecture requirement is the boundary, not a specific constructor design.

## Field identity

A field definition should have stable identity.

The identity must not be native memory address, field handle value, scheduler instance, job type, or Unity object ID.

Valid identity candidates:

```text
Symbol
ResourceDefinition + execution profile + representation metadata
```

The selected identity model must support deterministic lookup, diagnostics, testing, and artifact compatibility.

## Resource binding

A field definition must bind to a `ResourceDefinition`.

The binding answers:

```text
Which semantic generated value does this execution field represent?
```

A field definition without a resource binding is not part of semantic resource execution.

Scratch memory is not a field definition unless it intentionally represents a modeled field.

## FieldDefinitionSet

`FieldDefinitionSet` is a planned accepted collection of field definitions.

It should validate:

```text
field identity uniqueness
resource binding consistency
profile compatibility
field shape validity
value kind validity
lifetime consistency
capture policy consistency
storage policy consistency
```

It must not allocate native memory.

It must not schedule jobs.

It must not resolve request descriptors.

It must not compile managed plans.

It is input to future runnable plan compilation.

## ExecutionProfile

An execution profile is planned configuration that selects execution representation and policy.

An execution profile may influence:

```text
field representation
precision
memory layout
capture behavior
debug diagnostics
scheduler binding
implementation selection constraints
external input binding
artifact output policy
```

An execution profile must not change semantic resource identity.

A profile may change how a resource is stored or captured. It must not change what the resource means.

## Profile examples

Possible future profiles:

```text
ProductionDeterministic
AuthoringDebug
LowMemory
HighPrecision
ArtifactCapture
ServerBatch
EditorPreview
```

These are examples only. They are not required current API names.

## Profile ownership

Execution profiles belong to future execution architecture.

They should be consumed by:

```text
RunnablePlanCompiler
FieldDefinitionSet
SchedulerBindingCatalog
GenerationWorkspace allocation policy
artifact capture policy
```

They should not be owned by:

```text
ResourceDefinition
StageContract
OperationContract
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
```

Current managed planning must stay profile-independent unless the architecture explicitly introduces profile-aware request input.

## Field lifetime

Field lifetime describes why a field exists and how long it should be retained.

Planned lifetime categories:

```text
Canonical
StageTransient
Diagnostic
Payload
External
```

The names may be refined during implementation. The boundary must remain stable.

## Canonical field

A canonical field represents authoritative generated truth for a resource.

Examples:

```text
Height field
Land mask field
Ocean mask field
Coast mask field
Continental region field
```

Canonical fields are candidates for downstream consumption and artifact capture.

A canonical field should have stable interpretation across execution profiles, even if the concrete representation changes.

## Stage-transient field

A stage-transient field exists to support operations inside a stage or bounded stage group.

It is execution-owned intermediate state.

It is not default artifact output.

It should not be modeled as a semantic resource unless downstream systems need to reason about it as generated truth.

## Diagnostic field

A diagnostic field exists for validation, debugging, visualization, or tooling.

Diagnostic field capture should be controlled by execution profile or artifact policy.

Diagnostic fields must not become required semantic inputs unless promoted to a resource intentionally.

## Payload field

A payload field exists for downstream consumer output.

It may be derived from canonical fields.

It is not automatically authoritative generation truth.

Examples:

```text
mesh payload data
texture payload data
region summary payload
streaming chunk payload
```

Payload fields should be explicit because consumers may require stable contracts.

## External field

An external field is caller-provided, importer-provided, or tooling-provided input.

External fields must be bound explicitly.

They must not be discovered implicitly through:

```text
Unity scene lookup
global service locators
asset paths
current editor selection
static mutable state
scheduler-side guessing
```

External field binding belongs to future request, runnable compilation, or execution input design.

## Field shape

Field shape describes the structure of the data.

Possible future field shapes:

```text
CellGrid
Scalar
SparseCellSet
RegionGraph
EdgeGraph
ChunkGrid
Payload
```

Field shape is metadata.

It is not native allocation.

It is not a job schedule.

It is not resource identity.

## Cell-grid fields

A cell-grid field has one value per grid cell.

It is the likely representation for common map resources.

Examples:

```text
Height as CellGrid<float>
Land as CellGrid<byte>
Ocean as CellGrid<byte>
Coast as CellGrid<byte>
ContinentalRegion as CellGrid<int>
```

The field definition describes that representation. The workspace owns the actual native container.

## Scalar fields

A scalar field has one value for the run or for a bounded scope.

Examples:

```text
sea level
global land ratio
normalization threshold
```

Scalar fields should be used only when the value is genuinely scalar and not hidden per-cell data.

## Sparse fields

Sparse fields represent data for a subset of cells or entities.

Examples:

```text
changed cells
coastline candidates
region seed cells
```

Sparse fields need explicit ownership and deterministic ordering rules.

Sparse field ordering must not depend on hash map enumeration order or thread timing.

## Graph fields

Graph fields represent topology.

Examples:

```text
region adjacency
river graph
coastline edge graph
```

Graph field definitions should specify deterministic node and edge identity rules.

Graph fields must not rely on object reference identity.

## Value kind

Value kind describes the value category stored by a field.

Possible future value kinds:

```text
Float32
Float16
Int32
UInt32
Byte
BoolMask
RegionId
Vector2
Vector3
Struct
```

Value kind is not necessarily the exact native container type.

Example:

```text
ValueKind.BoolMask
```

may be represented by:

```text
NativeArray<byte>
bit-packed native storage
NativeBitArray
```

depending on execution profile and storage policy.

## Storage policy

Storage policy describes allocation and layout requirements.

Possible future storage policy concerns:

```text
contiguous array
bit-packed storage
temporary allocator choice
persistent allocator choice
read/write access pattern
alignment
padding
chunking
capture compatibility
```

Storage policy must not allocate memory directly.

The workspace uses storage policy to allocate and manage native memory.

## Capture policy

Capture policy describes whether and how a field can be captured as an artifact.

Possible future capture categories:

```text
NeverCapture
CaptureOnRequest
CaptureForDiagnostics
CaptureByDefault
CaptureForPayload
```

Capture policy must not be stored directly on `ResourceDefinition` unless it is invariant semantic truth.

Most capture behavior should be profile-dependent.

## Access policy

Access policy may describe intended read/write behavior.

Examples:

```text
ReadOnlyInput
WriteOnlyOutput
ReadWrite
AppendOnly
TemporaryScratch
```

Access policy can help future runnable compilation validate dependencies and scheduler bindings.

It does not replace job safety, native container safety, or dependency scheduling.

## Field handles

A field handle is planned execution-time addressing.

A field handle refers to workspace-owned storage for one run.

A field handle is not:

```text
Symbol
ResourceDefinition
FieldDefinition
NativeArray<T>
catalog identity
artifact identity
```

Field definitions may be used to create field handles during workspace setup.

Handles belong to future execution state, not current managed planning.

## Runnable plan compiler boundary

Future runnable plan compilation uses field definitions to bind semantic resources to executable fields.

Input:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
Scheduler bindings
```

Output:

```text
RunnablePlan
```

The runnable compiler may validate:

```text
every required resource has a field definition
every produced resource has a field definition
field definitions are compatible with selected execution profile
operation implementations have scheduler bindings
resource-field bindings are deterministic
```

The runnable compiler must not allocate native memory or schedule jobs.

## Workspace boundary

`GenerationWorkspace` owns native storage.

It may allocate:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
NativeReference<T>
custom native containers
```

The workspace uses runnable metadata and field definitions to allocate storage for one generation run.

The workspace must not perform catalog lookup, descriptor resolution, recipe selection, or managed plan compilation.

## Scheduler boundary

Schedulers consume runnable operation metadata and workspace access.

Schedulers may:

```text
resolve field handles to native containers
allocate operation scratch
schedule jobs
combine dependencies
apply iteration policies
report execution failures
```

Schedulers must not define field identity.

Schedulers must not change resource semantics.

Schedulers must not resolve descriptor symbols.

## Job boundary

Jobs receive native containers and unmanaged values.

Jobs must not receive:

```text
ResourceDefinition
FieldDefinition
ExecutionProfile
GenerationWorkspace
OperationScheduler
GenerationPlan
GenerationCatalog
Symbol
DisplayName
```

The scheduler must convert executable metadata into job-safe data before scheduling.

## External input binding

External inputs require explicit binding.

A future external input descriptor may bind:

```text
ResourceDefinition or FieldDefinition identity
external data source
shape and value-kind expectations
profile compatibility
validation policy
```

External input binding must be deterministic.

Do not bind external inputs by:

```text
current Unity selection
scene search
asset path guessing
global mutable registry
field name string matching without symbol identity
```

## Settings binding

Run settings and execution profiles are separate concepts.

Current run settings include:

```text
Grid
Seed
```

Future execution settings may include:

```text
ExecutionProfile
artifact capture options
debug capture options
memory budget
external input bindings
```

Do not overload `GenerationRunSettings` with execution-specific settings until the boundary is designed.

A settings value belongs in run settings only when it is part of generation intent for one run.

A value belongs in execution profile when it changes representation, scheduling, capture, or storage policy.

## Determinism rules

Field definition and profile selection must be deterministic.

Do not use:

```text
dictionary enumeration order
hash set enumeration order
native memory address
thread timing
Unity object instance ID
asset import order
current time
global random state
```

Field order, handle assignment, and runnable metadata order must be stable.

Preferred ordering sources:

```text
managed plan order
resource symbol order
field definition symbol order
explicit profile order
route-step order
```

The chosen ordering rule must be owned by the future runnable compiler or workspace.

## Validation ownership

Validation must remain boundary-owned.

| Validation                           | Owner                                                |
| ------------------------------------ | ---------------------------------------------------- |
| Resource local invariants            | `ResourceDefinition`                                 |
| Contract local resource lists        | `StageContract` / `OperationContract`                |
| Catalog resource ownership           | `GenerationCatalog`                                  |
| Field metadata local invariants      | Future `FieldDefinition`                             |
| Field set uniqueness and consistency | Future `FieldDefinitionSet`                          |
| Profile compatibility                | Future execution profile model and runnable compiler |
| Resource-to-field binding            | Future `RunnablePlanCompiler`                        |
| Native allocation                    | Future `GenerationWorkspace`                         |
| Job dependency correctness           | Future scheduler                                     |

Do not make `ResourceDefinition` validate field layout.

Do not make `FieldDefinition` allocate storage.

Do not make the workspace resolve symbols.

## Error handling

Invalid field metadata should throw during field definition or field set construction.

Expected profile incompatibility during runnable compilation should use the future runnable compilation error model.

Native allocation failure belongs to workspace execution.

Job dependency failure belongs to scheduler execution.

Do not add future execution errors to current request-resolution errors.

Request resolution is about symbolic request satisfiability against a catalog, not storage or scheduling feasibility.

## Correct future flow

```text
GenerationPlan
  contains semantic resources and selected operation implementations

FieldDefinitionSet
  describes storage-facing metadata for resources

ExecutionProfile
  selects representation and policy

RunnablePlanCompiler
  binds resources to fields and implementations to schedulers

RunnablePlan
  contains executable metadata

GenerationWorkspace
  allocates native storage

OperationScheduler
  schedules jobs over workspace storage
```

## Incorrect flow

```text
ResourceDefinition allocates NativeArray<T>
StageContract references FieldDefinition directly in current Runtime
OperationContract references JobHandle
GenerationPlan stores FieldHandle
FieldDefinition schedules jobs
ExecutionProfile changes resource symbols
Workspace resolves request descriptors
Job reads ResourceDefinition
```

These flows collapse semantic, storage, and execution ownership.

## Public API guidance

When implemented, public field/profile APIs should preserve precise naming.

Use:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
FieldLifetime
FieldShape
ValueKind
StoragePolicy
CapturePolicy
```

Avoid:

```text
ResourceData
FieldData
StorageInfo
ExecutionContext
ProfileManager
NativeFieldThing
```

Do not use `Field` alone for public domain API unless the type has a precise, documented role.

## Code placement guidance

Future field and execution profile code should live in explicit execution-oriented areas.

Possible folders:

```text
Runtime/Fields/
Runtime/Execution/
Runtime/Execution/Profiles/
Runtime/Execution/Compilation/
```

Do not place field definitions inside:

```text
Runtime/Resources/
Runtime/Stages/
Runtime/Operations/
Runtime/Recipes/
Runtime/Requests/
Runtime/Plans/
```

unless the file is only referencing the concept in documentation or tests.

## Test guidance

Future tests should verify:

```text
field definition local validation
field identity stability
field set uniqueness
resource binding consistency
profile compatibility
deterministic field ordering
runnable compiler missing-field errors
runnable compiler incompatible-profile errors
workspace allocation from field metadata
scheduler receives resolved field handles
jobs do not receive managed metadata
```

Tests must not assert behavior that belongs to current managed planning unless the implementation exists.

## Documentation guidance

Current architecture documents may mention fields only as future execution concepts.

Future documents must explicitly mark field definitions and execution profiles as planned architecture.

Do not describe field definitions as current contract members.

Do not describe execution profiles as current request settings unless the code exists.

Do not use field terminology when the current concept is a semantic resource.

## Review checklist

Before accepting field/profile architecture, verify:

```text
ResourceDefinition remains semantic identity.
FieldDefinition is storage-facing metadata only.
FieldDefinition does not allocate native memory.
FieldDefinition does not schedule jobs.
ExecutionProfile does not change resource identity.
StageContract and OperationContract remain resource-definition-based.
GenerationPlan remains managed semantic data.
RunnablePlanCompiler owns resource-to-field binding.
GenerationWorkspace owns native allocation and disposal.
Schedulers own dependency wiring and job scheduling.
Jobs receive native containers and unmanaged values only.
External inputs are bound explicitly and deterministically.
Capture policy is profile-aware where appropriate.
Ordering is deterministic.
```

## Summary

`ResourceDefinition` is current semantic identity.

`FieldDefinition` is future storage-facing metadata.

`ExecutionProfile` is future representation and policy selection.

`FieldDefinitionSet` is future accepted field metadata inventory.

`RunnablePlanCompiler` will bind resources to fields.

`GenerationWorkspace` will allocate native storage.

Schedulers will execute operations over workspace storage.

Jobs will receive only native containers and unmanaged values.

Do not move storage, profile, workspace, scheduler, or job responsibilities into current managed resource, contract, request, or plan objects.

```