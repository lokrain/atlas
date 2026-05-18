# Low-level native memory and unsafe collections

This document describes planned architecture.

Low-level native memory, unsafe collections, custom allocators, unmanaged topology builders, and unsafe execution infrastructure are not current Runtime behavior unless corresponding Runtime code exists.

Current Runtime architecture ends at `GenerationPlan`.

## Purpose

Low-level native memory exists to support deterministic execution workloads that cannot be represented efficiently or safely enough with higher-level managed objects.

Use standard Unity native containers by default.

Use unsafe or low-level APIs only when the data shape, ownership model, interop requirement, or measured target workload requires them.

## Current boundary

Current Runtime must not use unsafe execution infrastructure in semantic architecture layers.

Do not introduce unsafe memory or native containers into:

```text
Runtime/Core
Runtime/Schemas
Runtime/Resources
Runtime/Stages
Runtime/Operations
Runtime/Catalog
Runtime/Recipes
Runtime/Planning
Runtime/Generation module definitions
```

Do not add these to current managed objects:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
UnsafeList<T>
UnsafeHashMap<TKey, TValue>
UnsafePtrList<T>
BlobAssetReference<T>
void*
byte*
AllocatorManager.AllocatorHandle
JobHandle
```

Current managed Runtime objects are semantic and stop at `GenerationPlan`.

## Planned ownership areas

Unsafe and low-level native APIs may be valid in future infrastructure areas such as:

```text
Runtime/Memory
Runtime/Execution
Runtime/Topology
Runtime/Diagnostics
Runtime/Artifacts
Runtime/Interop
Runtime/Hashing
```

These areas must remain below semantic Runtime layers.

Semantic definitions, catalogs, recipes, requests, and plans must not expose unsafe implementation details.

## Default container rule

Prefer standard Unity native containers first.

Use these before unsafe equivalents:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
NativeParallelHashMap<TKey, TValue>
NativeParallelMultiHashMap<TKey, TValue>
NativeQueue<T>
NativeStream
```

Use unsafe containers only when standard containers do not satisfy a documented requirement.

## Valid reasons for unsafe code

Unsafe code may be justified for:

```text
specific memory layout
explicit allocation lifetime
high-volume topology construction
controlled aliasing
memory-range operations
custom compact graph layouts
interop with external native data
artifact serialization layout
measured target workload performance
Burst-compatible low-level algorithms
```

Unsafe code is not justified by preference, style, or speculative optimization.

## Required justification

Every unsafe infrastructure type must have a clear reason to exist.

Document:

```text
owner
lifetime
allocation strategy
disposal strategy
thread access model
aliasing rules
deterministic ordering rules
safe public surface
test coverage
reason standard native containers are insufficient
```

Do not merge unsafe code without this ownership model.

## Isolation rule

Unsafe details must be isolated behind infrastructure-owned APIs.

Correct planned boundary:

```text
OperationScheduler
  -> execution infrastructure
    -> unsafe topology builder
      -> frozen deterministic arrays
```

Incorrect boundary:

```text
ResourceDefinition exposes UnsafeList<T>.
GenerationPlan stores void*.
OperationContract stores NativeArray<T>.
GenerationRecipeDefinition owns unsafe buffers.
```

Unsafe APIs must not leak into semantic domain objects.

## Safety-disabling attributes

Use safety-disabling attributes only in infrastructure code with explicit ownership.

Examples include:

```text
NativeDisableContainerSafetyRestriction
NativeDisableParallelForRestriction
NativeDisableUnsafePtrRestriction
```

These attributes require justification.

They must not appear in semantic Runtime layers.

They must not be used to silence design issues.

## Allocation rule

Every native allocation must have one clear owner.

Allowed planned owners include:

```text
GenerationWorkspace
OperationScheduler
specialized execution allocator
topology builder
artifact writer
diagnostic capture owner
```

Disallowed owners include:

```text
ResourceDefinition
StageContract
OperationContract
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
StagePlanNode
OperationPlanNode
```

Allocation and disposal ownership must be paired.

## Disposal rule

The allocation owner disposes the allocation.

Borrowed data must document that it is borrowed.

Transferred data must document the transfer.

Do not rely on finalizers, editor domain reload, scene unload, or process exit for disposal.

Correct:

```text
GenerationWorkspace allocates field storage.
GenerationWorkspace disposes field storage.
```

Incorrect:

```text
Operation job allocates persistent memory and no owner disposes it.
ResourceDefinition owns a NativeArray<T>.
```

## Lifetime rule

Native memory lifetime must be explicit.

Common planned lifetimes:

```text
operation scratch lifetime
stage lifetime
generation run lifetime
artifact export lifetime
external borrowed lifetime
diagnostic capture lifetime
```

Do not store native pointers or containers beyond their declared lifetime.

Do not store execution lifetime objects in reusable definitions.

## Borrowing rule

Borrowed native data must not be disposed by the borrower.

A borrowed binding must define:

```text
owner
valid lifetime
read/write permissions
thread access permissions
required alignment
required length
shape assumptions
```

External fields are planned borrowed or caller-owned data unless explicitly transferred.

## Aliasing rule

Aliasing must be explicit.

If two views can reference overlapping memory, document:

```text
whether reads overlap writes
whether writes overlap writes
which jobs may run in parallel
which dependencies prevent hazards
which container restrictions are disabled
```

Do not disable safety checks without proving the aliasing model.

## Thread access rule

Thread access must be explicit and deterministic.

Define:

```text
single-writer or multi-writer policy
read-only views
parallel write partitioning
dependency requirements
atomic requirements
merge order
tie-breaking
```

Parallel algorithms must not depend on nondeterministic race order.

## Deterministic ordering rule

Unsafe and native algorithms must preserve deterministic output.

Do not depend on:

```text
hash-map enumeration order
thread scheduling order
pointer address order
allocation order
uninitialized memory
current time
global random state
Unity object instance ID
```

When output order matters, define explicit ordering or a deterministic freeze step.

## Freeze rule

Mutable low-level builders must freeze into deterministic canonical data before becoming accepted output.

Correct:

```text
mutable topology builder
  -> deterministic sort
  -> compact offsets/counts/values arrays
  -> canonical topology
```

Incorrect:

```text
mutable hash map enumeration becomes canonical output
```

Canonical output must be ordered, validated, hashable, serializable, and lifetime-owned.

## Canonical data rule

Canonical generation data must be:

```text
deterministically ordered
validated
lifetime-owned
hashable when needed
serializable when needed
free of transient builder state
free of unmanaged pointer identity
```

Canonical graph state uses arrays of:

```text
nodes
edges
offsets
counts
values
```

Do not use managed object graphs as canonical generation data.

## Dense raster data

Dense raster data uses row-major one-dimensional storage.

Correct:

```text
index = z * width + x
```

Use one-dimensional storage as the canonical layout.

Coordinate helpers may exist at API boundaries, but storage remains one-dimensional.

Avoid jagged arrays, multidimensional managed arrays, and per-cell managed objects for canonical runtime data.

## Fixed-degree topology

Fixed-degree topology should use direct computation or fixed-stride arrays.

Examples:

```text
4-neighbor grid
8-neighbor grid
fixed corner list
fixed edge list
```

Do not allocate variable lists when the degree is known and fixed.

## Variable topology

Variable one-to-many topology uses compressed sparse layouts after construction.

Canonical representation:

```text
offsets
counts
values
```

Mutable builders may use streams, lists, or multi-hash maps during construction.

They must freeze into deterministic arrays before canonical use.

## Connectivity labeling

Connectivity labeling must use deterministic algorithms.

Allowed approaches include:

```text
union-find with deterministic tie-breaking
deterministic flood-fill frontiers
explicitly ordered region-growing
```

Do not allow thread timing or unordered containers to determine label identity.

## Priority propagation

Priority propagation must use deterministic priority structures.

Allowed approaches include:

```text
deterministic binary heaps
bucket queues
radix-style queues
explicitly ordered frontiers
```

Tie-breaking must be explicit.

Do not rely on insertion order unless insertion order is explicitly deterministic and tested.

## Spatial lookup

Use chunk buckets before trees.

Trees are reserved for:

```text
editor tooling
rendering
LOD
sparse spatial acceleration
cases where chunk buckets are proven insufficient
```

Do not introduce tree structures for dense grid workloads without evidence.

## Managed object graph rule

Do not use managed object graphs as canonical generation data.

Incorrect canonical data:

```text
class Region
{
    List<Region> Neighbors;
    List<Cell> Cells;
}
```

Correct canonical data:

```text
NativeArray<int> regionNeighborOffsets
NativeArray<int> regionNeighborCounts
NativeArray<int> regionNeighborValues
NativeArray<int> regionCellOffsets
NativeArray<int> regionCellCounts
NativeArray<int> regionCellValues
```

Managed object graphs may be useful for editor visualization or tests, but not as canonical runtime generation state.

## Burst compatibility

Future execution data structures should be Burst-compatible unless there is a documented reason.

Prefer:

```text
unmanaged structs
native containers
explicit lengths
explicit allocators
math types from Unity.Mathematics
```

Avoid:

```text
managed references
virtual dispatch
delegates
LINQ
exceptions inside jobs
managed collections
string operations
UnityEngine.Object references
```

Burst-compatible code must remain deterministic.

## Job input rule

Jobs receive native containers and unmanaged values only.

Correct planned job inputs:

```text
NativeArray<float> elevation
NativeArray<byte> mask
int width
int depth
uint seed
```

Incorrect planned job inputs:

```text
GenerationPlan plan
ResourceDefinition resource
FieldDefinition field
GenerationWorkspace workspace
OperationScheduler scheduler
Symbol symbol
DisplayName displayName
```

Resolve metadata before scheduling jobs.

## Interop rule

Interop code must be isolated.

Interop APIs must document:

```text
ownership
pinning
alignment
endianness
lifetime
copy versus borrow behavior
thread access
error handling
```

Interop pointers must not leak into semantic domain objects.

## Artifact layout rule

Artifact serialization may require explicit binary layouts.

Artifact layout code must be separate from semantic definitions.

Correct planned boundary:

```text
workspace data
  -> artifact writer
  -> serialized artifact
```

Incorrect boundary:

```text
ResourceDefinition writes binary artifact bytes.
GenerationPlan owns artifact buffer.
```

## Diagnostics rule

Diagnostics may capture native execution details.

Diagnostics must not force semantic objects to depend on execution infrastructure.

Correct planned diagnostics:

```text
workspace allocation summary
scheduler timing summary
captured diagnostic field
operation validation report
```

Incorrect current diagnostics:

```text
GenerationPlan stores scheduler timings.
OperationContract stores debug NativeArray<T>.
```

## Error handling rule

Unsafe infrastructure must fail predictably.

Invalid API usage should throw before scheduling jobs.

Expected execution failures should use planned structured execution results or diagnostics.

Examples of invalid API usage:

```text
disposed workspace access
field handle from wrong workspace
invalid allocation length
unsupported field layout
null required metadata
```

Examples of expected execution failures:

```text
external field missing
operation convergence failure
allocation rejected by policy
diagnostic validation failure
```

## Testing rule

Unsafe and low-level infrastructure requires tests for:

```text
allocation and disposal
ownership transfer
borrowed data lifetime
deterministic ordering
freeze output ordering
parallel safety assumptions
aliasing assumptions
invalid handle rejection
wrong workspace rejection
disposed access rejection
boundary sizes
zero-length cases where allowed
large workload cases
```

Tests must verify deterministic output across repeated runs.

## Documentation rule

Future unsafe infrastructure must remain documented as planned until implemented.

Correct:

```text
Unsafe topology builders are planned execution infrastructure.
```

Incorrect:

```text
Atlas currently stores topology in UnsafeList<T>.
```

Do not document unsafe internals as current Runtime behavior before code exists.

## Invalid placements

Do not implement these:

```text
ResourceDefinition.NativeArray
ResourceDefinition.FieldHandle
StageContract.NativeInput
OperationContract.NativeOutput
GenerationCatalog.NativeStorage
GenerationRecipeDefinition.UnsafeBuffers
GenerationRequestDescriptor.NativeArray
GenerationRequest.Workspace
GenerationPlan.JobHandle
StagePlanNode.ExecuteUnsafe()
OperationPlanNode.UnsafePtr
```

These violate semantic Runtime boundaries.

## Review checklist

Before adding unsafe or low-level native code, verify:

```text
Standard native containers are insufficient.
The unsafe owner is an execution infrastructure type.
The allocation owner is explicit.
The disposal owner is explicit.
Borrowed versus owned data is documented.
Thread access is documented.
Aliasing is documented.
Safety-disabling attributes are justified.
Output ordering is deterministic.
Mutable builders freeze into canonical arrays.
Canonical data is validated.
Jobs receive only native containers and unmanaged values.
Semantic Runtime objects do not expose unsafe details.
Tests cover lifetime, determinism, aliasing, and invalid access.
```

## Summary

Use managed accepted objects for semantic architecture.

Use standard native containers by default for execution.

Use unsafe and low-level memory only in planned execution infrastructure when there is a documented structural or measured performance reason.

Keep unsafe implementation details out of resources, stages, operations, catalogs, recipes, requests, and managed plans.