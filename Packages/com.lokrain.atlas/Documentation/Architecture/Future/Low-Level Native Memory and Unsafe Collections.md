# Low-level native memory and unsafe collections

Package: `com.lokrain.atlas`  
Status: Future architecture policy  
Applies to: future execution, workspace, memory, topology, artifact, diagnostics, interop, hashing, and test infrastructure

This document defines when Atlas may use Unity low-level native functionality and unsafe collections.

These rules apply to future execution infrastructure. They do not change the current managed architecture, which ends at `GenerationPlan`.

## Purpose

Atlas uses standard Unity native containers by default.

Use low-level native functionality only when standard `Native*` containers cannot express a package-level requirement clearly enough. Valid requirements include memory layout, lifetime ownership, sparse topology construction, controlled aliasing, bulk memory operations, interop, artifact layout, or a measured workload-specific performance need.

Unsafe code is an implementation detail. It must not become an authoring model, resource model, stage-definition model, operation-definition model, recipe model, request model, or managed-plan model.

## Architecture boundary

The current architecture is semantic and managed:

```text
ResourceDefinition
  -> StageContract / OperationContract
  -> GenerationRequest
  -> GenerationPlan
````

Future execution introduces storage and scheduling boundaries:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> RunnablePlan
  -> GenerationWorkspace
  -> OperationScheduler
  -> Jobs
```

Low-level native functionality belongs only below future runnable compilation.

The required boundary is:

```text
Resource contracts define semantic meaning.
Runnable compilation resolves meaning into executable bindings.
The workspace owns memory.
Schedulers own execution control flow.
Jobs operate on resolved native views and unmanaged values.
Artifacts own durable output.
```

`StageContract` and `OperationContract` must remain resource-definition-based semantic contracts. They must not own pointers, allocators, field handles, safety handles, native containers, or unsafe memory.

## Baseline

Atlas targets Unity 6000.4.x for this policy.

The expected Collections baseline is:

```text
com.unity.collections@6.4.0
```

This baseline is not a permanent constraint. If the package version changes through Unity or Package Manager, this document must be reviewed against the updated Unity documentation and release notes.

## Scope

This policy covers the following APIs and patterns:

```text
Unity.Collections.LowLevel.Unsafe
UnsafeUtility
NativeArrayUnsafeUtility
UnsafeList<T>
UnsafeStream
UnsafeAppendBuffer
UnsafeBitArray
AllocatorManager
RewindableAllocator
custom native containers
raw pointer access
raw unmanaged allocation
direct memory copy, clear, move, compare, and hash operations
safety-disabling attributes
```

This policy also covers safe native APIs when they are part of low-level execution infrastructure decisions.

Example:

```text
NativeStream
```

`NativeStream` is a safe native container, but it commonly participates in stream, emission, merge, and deterministic materialization workflows.

## Non-goals

Atlas does not use low-level native functionality to:

```text
hide unclear ownership
bypass invalid field contracts
bypass runnable compiler validation
avoid deterministic merge or sort steps
make every dense field a raw pointer
make stage code responsible for memory layout
store managed data in Burst jobs
expose allocators or pointers through authoring APIs
make canonical output depend on job scheduling order
replace standard containers without a correctness or measured performance reason
```

Low-level native functionality must make the architecture safer, clearer, or measurably better for the target workload.

## Risk tiers

Use the lowest tier that correctly expresses the required ownership and access pattern.

| Tier | Category                                                 | Examples                                                                                                  | Policy                                                                                          |
| ---: | -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
|    0 | Standard safe native containers                          | `NativeArray<T>`, `NativeList<T>`, `NativeBitArray`, `NativeStream`, `NativeParallelHashMap<TKey,TValue>` | Preferred default for normal execution code.                                                    |
|    1 | Safe containers with unsafe access inside infrastructure | `GetUnsafePtr`, `MemCpy`, `MemClear`, pointer-based hashing over a validated native range                 | Allowed in memory, artifact, hashing, diagnostics, and interop infrastructure.                  |
|    2 | Unsafe collections                                       | `UnsafeList<T>`, `UnsafeStream`, `UnsafeAppendBuffer`, `UnsafeBitArray`                                   | Allowed for builders, topology construction, diagnostics, and tightly owned infrastructure.     |
|    3 | Raw unmanaged memory                                     | `UnsafeUtility.Malloc`, `UnsafeUtility.Free`, manually aligned blocks                                     | Prohibited except inside allocator, container, artifact-memory, or external-memory owner types. |
|    4 | Custom native containers                                 | `[NativeContainer]`, `AtomicSafetyHandle`, custom safety and disposal policy                              | Prohibited until a repeated Atlas invariant cannot be enforced with lower tiers.                |

## Decision rule

A low-level native implementation must satisfy at least one condition:

```text
Standard native containers cannot represent the required layout.
Standard native containers cannot represent the required lifetime.
The data shape requires nested or variable-length topology construction.
The operation is a memory-range operation, not an element algorithm.
Atlas must control aliasing beyond what Unity safety can infer.
Atlas must bridge externally owned or Unity-owned memory without copying.
A stable package invariant must be encoded in an infrastructure API.
A benchmark shows material improvement in the target workload.
```

If none of these conditions apply, use standard native containers.

## Measurement rule

Do not replace a standard native container with an unsafe collection for speculative performance.

A replacement requires one of:

```text
a correctness reason
a lifecycle reason
an interop reason
a layout reason
a benchmark demonstrating material improvement
```

Benchmarks must use the relevant:

```text
Unity version
Collections package version
Burst configuration
safety-check configuration
data-size range
target platform when platform behavior matters
```

A benchmark-only justification is not sufficient when the unsafe design weakens maintainability, ownership, determinism, or testability.

## Default container policy

Use the highest-level Unity native container that correctly expresses ownership and access.

| Data shape                 | Default choice                                 | Escalate only when                                                                 |
| -------------------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------- |
| Dense per-cell field       | `NativeArray<T>` or future typed field storage | Multiple logical fields share a pool and require compiler-resolved address access. |
| Packed mask                | `NativeBitArray`                               | Custom bit addressing, pooled bit ranges, or packed artifact layout is required.   |
| Variable-size records      | `NativeList<T>`                                | Records must be nested, arena-owned, or frozen after custom append phases.         |
| Parallel variable emission | `NativeStream`                                 | Safe stream behavior is insufficient and infrastructure owns the full lifecycle.   |
| Temporary scratch arrays   | `NativeArray<T>` with execution allocator      | Many allocations share a run, phase, or operation-group lifetime.                  |
| Graph adjacency            | Frozen `NativeArray<T>` layout                 | Mutable construction requires nested or variable-length builders.                  |
| Diagnostics                | Typed native lists or streams                  | Heterogeneous event payloads are required.                                         |
| Artifact data              | Typed arrays and validated field views         | Direct memory-range copy, hash, or internal binary writing is required.            |

## Data structure selection

Atlas chooses data structures by data shape, ownership, lifetime, and determinism requirements.

Dense raster data uses row-major one-dimensional storage. Public APIs and infrastructure APIs may expose coordinate helpers, but canonical dense storage is one-dimensional.

Fixed-degree topology uses direct computation or fixed-stride arrays.

Variable one-to-many topology uses frozen compressed sparse layouts.

Mutable topology builders may use streams, unsafe lists, or native multi-hash maps during construction. They must freeze into deterministic arrays before becoming canonical state.

Connectivity labeling uses union-find, deterministic flood-fill frontiers, or another explicitly ordered labeling algorithm.

Priority propagation uses deterministic heaps, bucket queues, radix-style queues, or another priority structure with explicit tie-breaking.

Spatial lookup uses chunk buckets before tree structures. Trees are reserved for editor tooling, rendering, LOD, sparse spatial acceleration, or cases where chunk buckets are proven insufficient.

Canonical graph state is represented as arrays of nodes, edges, offsets, counts, and values.

Atlas does not use managed object graphs as canonical generation data.

## Placement rule

Low-level native functionality belongs in future infrastructure areas.

Recommended locations:

```text
Runtime/Memory/
Runtime/Execution/
Runtime/Topology/
Runtime/Diagnostics/
Runtime/Artifacts/
Runtime/Interop/
Runtime/Hashing/
```

Avoid low-level native functionality in current semantic model areas:

```text
Runtime/Resources/
Runtime/Stages/
Runtime/Operations/
Runtime/Catalog/
Runtime/Recipes/
Runtime/Requests/
Runtime/Plans/
Runtime/Generation/
```

These paths express ownership boundaries. They are not mandatory final folder names.

## Canonical data policy

Canonical Atlas state must be:

```text
ordered
validated
hashable
serializable
deterministic
owned by a clear lifetime boundary
```

Allowed canonical storage patterns include:

```text
dense typed field pools
NativeArray<T> fields
NativeBitArray masks when directly owned
frozen topology arrays
explicit record arrays
artifact-owned immutable payloads
```

Disallowed canonical storage patterns include:

```text
raw stream append order
mutable nested unsafe lists
schema-less byte buffers
externally owned memory with unclear lifetime
scratch-allocator memory
unvalidated unmanaged memory
```

Intermediate low-level structures must be normalized before they become canonical state.

## Field pool aliasing

### Policy

Future workspace storage may store multiple logical fields inside shared typed pools.

Example:

```text
FinalElevation      -> Int32 pool, offset A
HydrologyElevation  -> Int32 pool, offset B
Temperature         -> Int32 pool, offset C
Curvature           -> Int32 pool, offset D
```

Unity job safety understands native containers. It does not understand Atlas field roles, compiler-resolved bindings, operation write exclusivity, or field-address ranges.

Use an Atlas-owned field address model only when pooled layout creates a real aliasing or layout requirement.

Candidate infrastructure names include:

```text
AtlasFieldAddress
AtlasFieldRange
AtlasFieldPool
AtlasFieldPoolViews
AtlasFieldView<T>
AtlasBitFieldView
```

These are candidate names, not committed API.

### Required flow

Future pooled field access must follow this boundary:

```text
ResourceDefinition
  -> FieldDefinition
  -> RunnablePlanCompiler field binding
  -> workspace layout entry
  -> resolved field address or field range
  -> validated pool view
  -> job read/write
```

Jobs must not resolve field IDs, symbols, resources, field definitions, or workspace entries.

Jobs receive resolved numeric addresses, typed native views, and unmanaged parameters only.

### Allowed use

Use low-level or safety-restricted pool access when:

```text
multiple logical fields share a native pool
runnable compiler validation has proven read/write contracts
Unity safety is too coarse for the Atlas layout
access can be validated against concrete address and range metadata
unsafe access is hidden inside package-owned field-view infrastructure
```

### Disallowed use

Do not use low-level field access when:

```text
a single NativeArray<T> clearly owns the data
there is no aliasing problem
the data is operation-local
standard containers are equally clear and efficient
```

## Safety-disabling attributes

Safety-disabling attributes weaken Unity safety checks and are high-risk.

Examples:

```text
NativeDisableParallelForRestriction
NativeDisableContainerSafetyRestriction
NativeDisableUnsafePtrRestriction
```

### Policy

Use safety-disabling attributes only inside package-owned infrastructure views or explicitly approved exceptions.

Allowed by default only in future infrastructure areas:

```text
Runtime/Memory/
Runtime/Execution/
Runtime/Topology/
Runtime/Artifacts/
Runtime/Diagnostics/
Runtime/Interop/
```

Disallowed by default in semantic and operation-definition areas:

```text
Runtime/Resources/
Runtime/Stages/
Runtime/Operations/
Runtime/Catalog/
Runtime/Recipes/
Runtime/Requests/
Runtime/Plans/
```

A stage or operation job must not directly add a safety-disabling attribute unless an architecture note or decision record defines:

```text
why standard safety cannot model the access
which invariant replaces the disabled safety check
how write ranges are proven unique or synchronized
which tests cover the exception
why the exception cannot be moved into infrastructure
```

## Scratch allocation

### Policy

Use execution-owned scratch allocation for shared temporary lifetimes.

Generation can require many temporary buffers:

```text
candidate lists
prefix-sum buffers
sort buffers
temporary masks
graph construction buffers
basin, ridge, and drainage staging data
validation scratch
diagnostic staging
```

Many of these buffers share one lifecycle: run, phase, operation group, or scheduler chain.

Candidate infrastructure names include:

```text
AtlasGenerationMemoryScope
AtlasScratchAllocator
AtlasExecutionScratch
AtlasPhaseScratchScope
AtlasExecutionScope
```

These are candidate names, not committed API.

### Required ownership

Scratch allocation belongs to future execution infrastructure.

Expected ownership:

```text
execution scope
  owns scratch allocator
    allocates temporary containers
  completes dependent jobs
  rewinds or disposes scratch at the scope boundary
```

Schedulers may request scratch through execution-owned infrastructure.

Jobs must not own scratch disposal.

### Allowed use

Use rewindable or custom scratch allocation when:

```text
many temporary allocations share a common lifetime
allocation and disposal code becomes noisy or error-prone
temporary data does not escape the owning scope
execution can guarantee all dependent jobs complete before rewind or dispose
```

### Disallowed use

Do not allocate the following from execution scratch:

```text
canonical output fields
durable artifacts
caller-returned data
editor preview objects that outlive generation
managed-object-captured data
data whose lifetime is not strictly bounded by the owning scope
```

## Sparse topology

### Policy

Use unsafe or stream-based builders only during topology construction.

Canonical topology must be frozen before canonical execution, hashing, serialization, or artifact writing.

Sparse and variable-length shapes include:

```text
Region -> many cells
Basin  -> many outlets
Node   -> many edges
River  -> many segments
Lake   -> many rim cells
```

Examples include:

```text
ridge graphs
drainage graphs
basin adjacency
coastline segment membership
lake rim cells
river segment chains
region member lists
portal and corridor candidate graphs
```

### Preferred final representations

Prefer frozen layouts such as:

```text
Offsets: NativeArray<int>
Counts:  NativeArray<int>
Values:  NativeArray<T>
```

Other valid frozen layouts include:

```text
fixed-stride arrays for bounded-degree topology
direct grid-neighbor computation for D4/D8 neighbors
structure-of-arrays for multi-attribute edges
dense fields for per-cell labels and receivers
sorted record arrays for small immutable topology
```

### Required flow

```text
emit or build mutable topology
-> sort deterministically
-> compact
-> freeze into explicit arrays
-> use frozen layout in canonical jobs
```

### Allowed use

Use `UnsafeList<T>`, `UnsafeStream`, or builder-owned unsafe memory when:

```text
the number of outputs per source item is unknown
construction needs nested variable-length lists
exact preallocation would be wasteful or complex
the structure will be frozen before canonical use
deterministic ordering is restored before hashing or artifact writing
```

### Disallowed use

Do not keep mutable unsafe topology as canonical state.

Canonical topology must be ordered, validated, hashable, and serializable.

## Parallel variable emission

### Policy

Use stream-based emission for intermediate variable output. Normalize after emission.

Prefer `NativeStream` before `UnsafeStream`.

Use `UnsafeStream` only when a proven infrastructure constraint requires the unsafe variant.

Examples of variable emission:

```text
coastline edge extraction
river candidate extraction
basin boundary crossing detection
validation event collection
contour generation
diagnostic sample emission
mesh primitive staging
```

### Required flow

```text
parallel emit
-> merge
-> sort by canonical key
-> compact
-> write stable output
```

Canonical keys must be explicit.

Examples:

```text
cell index
source region id
target region id
edge key
segment key
water body id
river id
operation-local sequence key
```

### Allowed use

Use stream-based emission when:

```text
each worker emits a variable amount of data
exact output counts are expensive or awkward to precompute
emitted output is intermediate
deterministic merge, sort, or freeze is part of the operation contract
```

### Disallowed use

Do not use raw parallel append order as:

```text
artifact order
hash order
canonical record order
gameplay-visible order
test-expected order
```

Parallel emission order is not a domain contract.

## Bulk memory operations

### Policy

Use direct memory operations only in infrastructure utilities.

Examples:

```text
clear an entire field
copy a field into an artifact
copy a field into a diagnostic snapshot
compare two field buffers
hash a contiguous field range
initialize a buffer with a repeated byte pattern
move a memory range inside a staging buffer
```

Candidate infrastructure names include:

```text
AtlasMemoryUtility
AtlasFieldClearUtility
AtlasFieldCopyUtility
AtlasFieldCompareUtility
AtlasFieldHashUtility
AtlasArtifactFieldWriter
```

These are candidate names, not committed API.

### Allowed operations

```text
clear
copy
move
compare
hash
zero-fill
byte-pattern fill
```

The infrastructure owner must validate:

```text
pointer validity
byte length
element count
storage format
field range
source and destination ownership
overlap behavior
job dependencies
initialization requirements
```

Use `MemMove` semantics, not `MemCpy` semantics, when ranges may overlap.

### Allowed use

Use memory-range operations when:

```text
the operation is format-independent
no per-element domain logic is required
the memory range is contiguous
source and destination ownership is clear
the operation belongs to workspace reset, artifact writing, hashing, diagnostics, or interop
```

### Disallowed use

Do not use raw memory operations when:

```text
values need conversion
values need clamping
values need semantic validation
element semantics matter
layout is not trivially copyable
byte order or public schema encoding matters
```

## Artifact writing and hashing

### Policy

Artifacts are durable. Their layout and hashes must be based on stable metadata and stable memory ranges.

Use compiled artifact layout metadata to write and hash artifacts.

Candidate infrastructure names include:

```text
AtlasArtifactBuilder
AtlasArtifactFieldWriter
AtlasArtifactHashBuilder
AtlasArtifactLayout
AtlasArtifactFieldRecord
```

These are candidate names, not committed API.

### Required flow

```text
compiled artifact field list
-> validate field address
-> copy or encode field data
-> update hash in stable field order
-> write artifact metadata
-> seal artifact
```

### Raw memory artifact rule

Raw memory copies are allowed for internal artifact sections only when the artifact schema defines:

```text
field format
element size
byte order
alignment assumptions
version
section length
hash order
migration policy
```

Public, cross-version, cross-platform, or presentation payloads must use explicit encoding.

### Allowed use

Use low-level memory access for artifacts when:

```text
writing dense final fields into internal sections
writing frozen topology arrays
hashing stable memory ranges
copying final records with fixed schema
serializing binary payload sections whose schema explicitly allows byte-copy encoding
```

### Disallowed use

Do not write raw memory directly when:

```text
the artifact requires version migration
canonical and presentation formats differ
byte order is unspecified
the section has a public schema requiring explicit encoding
the payload must be stable across platforms that may represent data differently
```

## Diagnostics and operation traces

### Policy

Diagnostics are optional, variable, and often heterogeneous. They do not belong in canonical fields unless explicitly normalized and promoted.

Examples:

```text
validation events
rejected candidates
sampled field values
operation timing
stage trace events
topology debug output
```

Use typed diagnostic streams or append buffers behind typed writer APIs.

Candidate infrastructure names include:

```text
AtlasDiagnosticEvent
AtlasDiagnosticEventStream
AtlasOperationTraceBuffer
AtlasValidationEventBuffer
AtlasDiagnosticPayloadWriter
```

These are candidate names, not committed API.

### Required API shape

Do not expose raw append buffers to stage or operation code.

Expose typed methods such as:

```text
WriteValidationError(...)
WriteRejectedCandidate(...)
WriteStageMetric(...)
WriteSample(...)
```

### Allowed use

Use append buffers or diagnostic streams when:

```text
diagnostic payloads are variable-size
event volume is high
events are collected from jobs
diagnostics are excluded from canonical output
a typed writer preserves schema discipline
```

### Disallowed use

Do not let diagnostics become a schema-less byte dump.

If a diagnostic event matters long-term, define its schema.

## External memory and Unity API bridges

### Policy

Use external-memory wrappers only at package boundaries.

Some APIs expose or require memory Atlas does not own directly:

```text
native plugin memory
generated artifact blob views
mesh data upload paths
image or texture transfer paths
editor inspection tools over native buffers
```

Candidate infrastructure names include:

```text
AtlasNativeArrayAdapter
AtlasExternalMemoryView
AtlasArtifactMemoryView
AtlasMeshDataBridge
```

These are candidate names, not committed API.

### Required ownership

External memory wrappers must state ownership explicitly:

```text
Atlas-owned
Unity-owned
native-plugin-owned
caller-owned
external-owned
```

### Allowed use

Use external-memory wrappers when:

```text
memory is already allocated by another system
copying is expensive and unnecessary
lifetime can be proven
safety handles or access rules can be assigned correctly
the wrapper is short-lived and boundary-local
```

### Disallowed use

Do not make external memory the default Atlas field storage model.

If most Atlas fields require external-memory wrapping, the workspace ownership model is wrong.

## Raw unmanaged allocation

### Policy

Direct `UnsafeUtility.Malloc` and `UnsafeUtility.Free` are prohibited except inside reviewed owner types.

Prefer this order:

```text
standard native containers
standard native containers allocated through an execution-owned allocator
RewindableAllocator for run-scoped or phase-scoped scratch
unsafe collections owned by builders or infrastructure
raw unmanaged allocation inside a reviewed owner type
```

### Required owner behavior

Any type that performs raw unmanaged allocation must define:

```text
allocator used
byte size
alignment
initialization policy
disposal policy
ownership transfer policy
job dependency policy
leak detection policy
debug validation policy
```

Raw unmanaged memory is not zero-initialized unless the owner explicitly initializes it.

## Custom native containers

### Policy

Custom native containers are prohibited until a repeated Atlas invariant cannot be enforced with lower tiers.

Before creating a custom native container, prefer:

```text
standard native containers
plain unmanaged views
field addresses
validated infrastructure APIs
compiler-validated binding
execution-scope lifecycle
tests
```

Candidate future containers include:

```text
AtlasNativeFieldView<T>
AtlasNativeBitFieldView
AtlasNativeRecordArena<T>
AtlasNativeArtifactBuilder
AtlasNativeSparseAdjacency<T>
```

These are candidates, not a plan.

### Approval requirements

A custom native container requires an architecture decision record or equivalent architecture note that defines:

```text
the invariant being encoded
why existing native containers are insufficient
ownership and lifetime
safety-handle behavior
disposal behavior
aliasing behavior
Burst and job support
required tests
migration path from existing infrastructure
```

## Required invariants

Every low-level native type in Atlas must document and enforce these invariants.

### Ownership

The type must state who owns memory.

Valid ownership examples:

```text
workspace-owned
execution-scratch-owned
artifact-owned
Unity-owned
native-plugin-owned
caller-owned
external-owned
```

### Lifetime

The type must state when memory is valid.

Valid lifetime examples:

```text
valid until workspace dispose
valid until execution scope rewind
valid until artifact builder seal
valid until dependent job completes
valid only during adapter scope
```

### Access mode

The type must state its access mode.

Valid access modes:

```text
read-only
write-only
read-write
append-only
freeze-then-read
copy-only
hash-only
```

### Ordering

The type must state whether order is stable.

Valid ordering examples:

```text
stable by field index
stable by cell index
stable by canonical key
stable after sort
stable after freeze
not stable; diagnostics only
```

### Safety boundary

The type must state where unsafe access is allowed.

Valid safety boundaries:

```text
Runtime/Memory only
Runtime/Execution only
Runtime/Topology builder only
Runtime/Artifacts only
Runtime/Diagnostics only
Runtime/Interop only
```

Unsafe access must not spread across stage, operation, catalog, recipe, request, or managed-plan code.

## Required tests

Low-level native infrastructure must be covered by tests before production generation code uses it.

Required test categories:

```text
allocation and disposal tests
double-dispose behavior tests when applicable
invalid range tests
invalid format tests
overlapping copy tests
initialization and default-value tests
deterministic order tests
deterministic hash tests
Burst-compiled job tests
safety-check-enabled editor tests
leak detection tests
dispose-after-job-dependency tests
rewind-after-complete tests
use-after-rewind negative tests where practical
artifact round-trip tests where artifact memory is involved
external-memory ownership tests where external wrapping is involved
safety-disabled aliasing proof tests where NativeDisable* attributes are used
```

Performance-motivated unsafe code also requires benchmarks for:

```text
target data size
target allocator mode
Burst enabled
safety checks enabled and disabled when relevant
editor and player when behavior differs materially
target platform when platform behavior matters
```

## Review checklist

Use this checklist before adding low-level native functionality.

### Design review

```text
Does this solve a real layout, lifetime, topology, bulk-memory, aliasing, artifact, or interop problem?
Could a standard Native* container express the same thing clearly?
Is unsafe code isolated in infrastructure?
Is ownership explicit?
Is lifetime explicit?
Is access mode explicit?
Is deterministic order explicit?
Is there a validated API above the unsafe implementation?
Can the unsafe implementation be replaced without changing authoring contracts or semantic definitions?
```

### Job safety review

```text
Are jobs given resolved numeric views?
Are owning workspaces excluded from job structs?
Are managed objects excluded from job structs?
Are pointer ranges validated before scheduling?
Are write ranges unique or synchronized?
Are dependencies chained correctly?
Is disposal or rewind delayed until dependent jobs complete?
Are safety-disabling attributes confined to approved infrastructure?
```

### Determinism review

```text
Does output order depend on job scheduling?
Is there a deterministic merge, sort, or freeze step?
Is artifact order compiler-defined?
Is hash order stable?
Are diagnostics excluded from canonical output unless explicitly normalized?
Does the design avoid current time, frame count, thread timing, and unmanaged memory addresses as generation identity?
```

### Memory review

```text
Is allocation size checked?
Is alignment correct?
Is initialization explicit?
Is dispose or rewind guaranteed?
Is overlapping copy handled correctly?
Are external ownership rules explicit?
Are debug checks enabled in editor and development builds?
```

### API review

```text
Is the public API semantic rather than pointer-based?
Are raw pointers hidden from stage and operation code?
Are contracts free of memory ownership?
Are operation definitions free of allocator details?
Are jobs free of managed domain metadata?
Are unsafe details hidden behind package-owned infrastructure?
```

## Anti-patterns

### Raw pointer stages

Bad:

```text
Stage receives void* elevation and byte offsets.
```

Good:

```text
Future scheduler prepares validated field views and schedules jobs with resolved native data.
```

### Unsafe as performance decoration

Bad:

```text
Use UnsafeList<T> because it sounds faster.
```

Good:

```text
Use UnsafeList<T> during topology construction because nested variable-length construction is required, then freeze to deterministic arrays.
```

### Parallel append as canonical order

Bad:

```text
Record order follows ParallelWriter append order.
```

Good:

```text
Parallel emit -> deterministic sort -> compact -> canonical record order.
```

### Scratch data escaping scope

Bad:

```text
Return a NativeArray<T> allocated from generation scratch.
```

Good:

```text
Copy durable results into artifact-owned or caller-owned memory before execution scope disposal.
```

### Schema-less diagnostics

Bad:

```text
Write arbitrary bytes into an append buffer from stage code.
```

Good:

```text
Expose typed diagnostic writer methods with stable event schemas.
```

### Direct NativeDisable in operation jobs

Bad:

```text
An operation job disables safety checks directly because the scheduler currently works.
```

Good:

```text
The infrastructure view carries the safety exception, documents the invariant, and is covered by aliasing and job-safety tests.
```

### Custom container too early

Bad:

```text
Create AtlasNativeEverything before field bindings, workspace layout, and scheduler ownership are stable.
```

Good:

```text
Use standard containers, field addresses, and validated infrastructure first. Introduce custom containers only when a stable repeated invariant requires them.
```

## Documentation rules

Current architecture documents may mention low-level native functionality only as future execution infrastructure.

Do not document unsafe collections as current resource, stage, operation, catalog, recipe, request, or plan behavior.

Do not describe `ResourceDefinition` as storage.

Do not describe `GenerationPlan` as executable job data.

Do not describe stage or operation contracts as field or memory contracts.

Implementation order belongs in `Plans/Implementation Plan.md`, not in this policy document.

## Summary

Atlas is safe by default and low-level only by necessity.

Use standard Unity native containers for ordinary execution code.

Use unsafe collections and low-level memory APIs only inside infrastructure that owns memory layout, lifetime, aliasing, topology construction, artifact writing, diagnostics, hashing, or interop.

Semantic contracts stay resource-definition-based.

Runnable compilation resolves semantic meaning into executable bindings.

The workspace owns memory.

Schedulers own orchestration.

Jobs receive resolved native views and unmanaged values only.

Unsafe code must make ownership clearer, determinism stronger, or infrastructure measurably better. It must not leak into authoring, definitions, requests, plans, or ordinary generation logic.

```