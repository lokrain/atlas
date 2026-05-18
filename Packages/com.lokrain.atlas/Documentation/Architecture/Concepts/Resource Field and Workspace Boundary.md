# Resource, field, and workspace boundary

This article explains the boundary between semantic resources, planned storage-facing fields, and planned execution workspace ownership.

Current Runtime architecture implements `ResourceDefinition`.

`FieldDefinition`, `RunnablePlan`, `GenerationWorkspace`, `OperationScheduler`, native storage, jobs, artifacts, and ECS execution integration are planned architecture.

## Boundary summary

The boundary is:

```text
ResourceDefinition
  -> FieldDefinition          planned
  -> RunnablePlanCompiler     planned
  -> RunnablePlan             planned
  -> GenerationWorkspace      planned
  -> OperationScheduler       planned
  -> Jobs                     planned
```

Current Runtime stops before `FieldDefinition`.

## Responsibility summary

| Concept | Status | Responsibility |
| --- | --- | --- |
| `ResourceDefinition` | Current | Semantic identity of a generated value. |
| `FieldDefinition` | Planned | Storage-facing metadata for representing a resource during execution. |
| `RunnablePlanCompiler` | Planned | Binds managed plan metadata to execution metadata. |
| `RunnablePlan` | Planned | Immutable executable metadata. |
| `GenerationWorkspace` | Planned | Native storage allocation, access, lifetime, and disposal for one run. |
| `OperationScheduler` | Planned | Execution control flow, dependency wiring, scratch allocation, and job scheduling. |
| Jobs | Planned | Deterministic transforms over native containers and unmanaged values. |

## ResourceDefinition

`ResourceDefinition` describes the semantic identity of a generated value.

A resource answers:

```text
What generated value is required or produced?
```

Examples from the landmass module:

```text
ContinentSuitability
ContinentCandidate
MainContinent
ContinentalLandmassArea
BaseElevation
```

A resource definition is managed metadata.

A resource definition belongs to a generation schema.

A resource definition is owned by a catalog when that exact instance is registered in the catalog.

A resource definition is not storage.

## Resource identity

A resource definition has stable symbol identity.

The symbol identifies the semantic value for:

```text
catalog lookup
contract resource flow
recipe validation
request validation
plan metadata
artifact compatibility
tooling
```

The display name is user-facing metadata.

Do not use display names as identity.

## Resource flow

Stage and operation contracts use `ResourceDefinition`.

Correct:

```text
OperationContract
  RequiredInputs
    ContinentSuitability
  ProducedOutputs
    ContinentCandidate
```

Incorrect:

```text
OperationContract
  RequiredInputSymbols
    lokrain.atlas.landmass.resource.continent_suitability
  ProducedOutputSymbols
    lokrain.atlas.landmass.resource.continent_candidate
```

Contracts use accepted resource definitions, not raw symbol lists.

Symbols are used for lookup and descriptor resolution. Resource flow uses accepted definitions.

## ResourceDefinition does not own storage

`ResourceDefinition` must not contain:

```text
FieldDefinition
FieldHandle
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
BlobAssetReference<T>
JobHandle
Entity
scheduler binding
storage layout
allocation policy
artifact buffer
```

A resource definition is reusable semantic inventory.

It must not contain execution state for one run.

## FieldDefinition

`FieldDefinition` is planned storage-facing metadata.

A field definition will describe how a semantic resource is represented for execution.

A field definition answers:

```text
How is this generated value represented for execution?
```

A field definition may describe planned metadata such as:

```text
resource identity
field shape
field value kind
execution profile
capture policy
storage requirements
external binding policy
diagnostic role
```

A field definition must not allocate native storage.

A field definition must not schedule jobs.

A field definition must not replace `ResourceDefinition`.

## Resource versus field

A resource is semantic.

A field is storage-facing.

Examples:

| Resource question | Field question |
| --- | --- |
| What generated value is this? | How is it represented for execution? |
| Is this `BaseElevation`? | Is this a dense cell-grid `float` field? |
| Which contracts require it? | Which workspace allocation stores it? |
| Which artifact name should identify it? | Which native container layout is used? |

Correct planned relationship:

```text
ResourceDefinition BaseElevation
  -> FieldDefinition BaseElevationField
  -> workspace allocation
```

Incorrect current relationship:

```text
ResourceDefinition BaseElevation
  -> NativeArray<float>
```

## FieldDefinitionSet

`FieldDefinitionSet` is planned accepted field metadata.

It will group field definitions used by runnable plan compilation.

A field definition set may validate:

```text
each field references a known resource
each required plan resource has a field definition
field symbols are unique
field resource mappings are unique when required
execution profile compatibility
storage representation compatibility
```

A field definition set must not replace catalog validation.

Catalogs own semantic definition graphs.

Field definition sets own planned storage-facing metadata.

## Execution profiles

Execution profiles are planned policy inputs.

An execution profile may influence:

```text
which fields are captured
which fields are temporary
which fields are diagnostic
which storage representation is selected
which implementation variant is selected
which scheduler policy is used
```

Execution profiles must not change semantic resource identity.

Correct:

```text
BaseElevation remains BaseElevation under all profiles.
```

Incorrect:

```text
BaseElevation means a different resource under a debug profile.
```

## RunnablePlanCompiler

`RunnablePlanCompiler` is planned architecture.

It is the bridge from managed semantic planning to executable metadata.

It uses:

```text
GenerationPlan
FieldDefinitionSet
execution profiles
```

It produces:

```text
RunnablePlan
```

The runnable compiler may bind:

```text
resources to field definitions
operations to runnable operation metadata
stage plan nodes to runnable stage metadata
operation plan nodes to scheduler bindings
execution profiles to capture and storage policy
```

It must not mutate the `GenerationPlan`.

It must not allocate workspace storage.

It must not schedule jobs.

## RunnablePlan

`RunnablePlan` is planned immutable executable metadata.

A runnable plan may contain:

```text
runnable stages
runnable operations
field bindings
scheduler bindings
workspace allocation requirements
execution profile decisions
artifact capture declarations
diagnostic capture declarations
```

A runnable plan must not own native storage lifetime.

Storage lifetime belongs to `GenerationWorkspace`.

## GenerationWorkspace

`GenerationWorkspace` is planned native storage ownership for one generation run.

A workspace may own:

```text
canonical field storage
temporary field storage
external field bindings
diagnostic field storage
operation scratch storage
native allocation lifetime
disposal
```

A workspace must not own:

```text
semantic resource identity
catalog validation
recipe selection
request descriptor resolution
managed plan compilation
runnable plan compilation
operation implementation selection
```

The workspace allocates and exposes execution storage. It does not decide what the generation run means.

## Field handles

Field handles are planned execution-time references to workspace-owned storage.

A field handle should be valid only within the workspace and runnable plan context that created it.

Field handles must not appear in current Runtime domain objects.

Do not add field handles to:

```text
ResourceDefinition
StageContract
OperationContract
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
StagePlanNode
OperationPlanNode
```

## OperationScheduler

`OperationScheduler` is planned execution control flow.

A scheduler may own:

```text
workspace access
dependency wiring
job scheduling
operation scratch allocation
iteration policy
termination policy
execution failure policy
```

A scheduler must not own:

```text
catalog lookup
symbol resolution
recipe selection
request resolution
managed plan compilation
semantic resource identity
```

A scheduler receives resolved executable metadata and workspace access. It does not reinterpret semantic planning data.

## Jobs

Jobs are planned deterministic transforms over native data.

Jobs receive native containers and unmanaged values.

Correct planned job inputs:

```text
NativeArray<float> baseElevation
NativeArray<byte> landMask
int width
int depth
uint seed
```

Incorrect planned job inputs:

```text
ResourceDefinition resource
GenerationCatalog catalog
GenerationPlan plan
GenerationWorkspace workspace
OperationScheduler scheduler
ScriptableObject settings
```

Jobs must not know symbols, catalogs, recipes, requests, plans, resources, field definitions, workspaces, or schedulers.

## Artifacts

Artifacts are planned captured outputs.

Artifacts may be produced from workspace-owned data according to execution profile and capture policy.

Artifacts must not be owned by `ResourceDefinition`, contracts, recipes, requests, or managed plans.

Correct planned flow:

```text
workspace field data
  -> artifact capture policy
  -> artifact output
```

Incorrect current flow:

```text
ResourceDefinition owns artifact data.
GenerationPlan owns artifact buffers.
OperationContract writes artifact files.
```

## Diagnostics

Current Runtime diagnostics are managed validation failures.

Examples:

```text
exceptions
GenerationRequestResolutionError
```

Planned execution diagnostics may include:

```text
captured diagnostic fields
scheduler timings
job execution diagnostics
artifact summaries
workspace allocation diagnostics
```

Execution diagnostics must not leak into current semantic Runtime objects.

## External fields

External fields are planned caller-provided or tooling-provided inputs.

An external field may bind external data to a field definition during runnable execution.

External field binding must happen after semantic request and plan validation.

Correct planned boundary:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> external field binding validation
  -> GenerationWorkspace
```

Incorrect current boundary:

```text
GenerationRequestDescriptor contains NativeArray<T>.
ResourceDefinition stores external data pointer.
```

## Temporary fields

Temporary fields are planned intermediate execution storage.

Temporary fields may support operation chains, staging, and diagnostics.

Temporary fields are not semantic resources unless explicitly represented by a `ResourceDefinition`.

Temporary fields must not be introduced into current contracts as raw storage concepts.

## Canonical fields

Canonical fields are planned authoritative generated outputs for semantic resources.

A canonical field may be captured as an artifact or consumed by later operations.

The semantic identity remains the `ResourceDefinition`.

The storage representation is the planned field definition and workspace allocation.

## Current valid model

Current Runtime model:

```text
ResourceDefinition
  used by StageContract
  used by OperationContract
  owned by GenerationCatalog
  selected through GenerationRecipeDefinition
  carried through GenerationRequest
  carried through GenerationPlan
```

This model is managed and semantic.

It does not allocate storage.

## Planned execution model

Planned execution model:

```text
GenerationPlan
  + FieldDefinitionSet
  + ExecutionProfile
    -> RunnablePlanCompiler
    -> RunnablePlan
    -> GenerationWorkspace
    -> OperationScheduler
    -> Jobs
```

This model is execution-facing.

It owns storage, bindings, scheduling, and job execution.

## Invalid boundary crossings

Do not implement these:

```text
ResourceDefinition.FieldDefinition
ResourceDefinition.NativeArray
StageContract.FieldHandle
OperationContract.NativeArray<T>
GenerationRecipeDefinition.GenerationWorkspace
GenerationRequestDescriptor.ExternalNativeArray
GenerationRequest.FieldHandle
GenerationPlan.RunnableOperation
StagePlanNode.JobHandle
OperationPlanNode.Schedule()
Job receives ResourceDefinition
Job receives GenerationPlan
```

Each item crosses the semantic/execution boundary too early.

## Data structure selection

Data structures are selected by data shape, ownership, and determinism requirements.

Dense raster data uses row-major one-dimensional storage.

Fixed-degree topology uses direct computation or fixed-stride arrays.

Variable one-to-many topology uses frozen compressed sparse layouts.

Mutable topology builders may use streams, unsafe lists, or native multi-hash maps during construction, but must freeze into deterministic arrays before becoming canonical.

Connectivity labeling uses union-find, deterministic flood-fill frontiers, or another explicitly ordered labeling algorithm.

Priority propagation uses deterministic heaps, bucket queues, radix-style queues, or another priority structure with explicit tie-breaking.

Spatial lookup uses chunk buckets before trees. Trees are reserved for editor tooling, rendering, LOD, sparse spatial acceleration, or cases where chunk buckets are proven insufficient.

Canonical graph state uses arrays of nodes, edges, offsets, counts, and values.

Atlas does not use managed object graphs as canonical generation data.

## Checklist

Before adding resource, field, or workspace code, verify:

```text
Is this semantic identity?
Use ResourceDefinition.

Is this storage-facing metadata?
Use planned FieldDefinition.

Is this executable metadata?
Use planned RunnablePlan.

Is this native storage lifetime?
Use planned GenerationWorkspace.

Is this operation control flow or job scheduling?
Use planned OperationScheduler.

Is this deterministic transform code?
Use planned jobs.

Does current Runtime object contain future execution state?
Move it after GenerationPlan.

Does a contract describe storage?
Move storage metadata to planned field definitions.

Does a job know semantic metadata?
Resolve metadata before scheduling the job.
```

## Summary

`ResourceDefinition` is current semantic metadata.

`FieldDefinition` is planned storage-facing metadata.

`RunnablePlanCompiler` is the planned bridge from managed plans to executable metadata.

`GenerationWorkspace` is planned native storage ownership.

`OperationScheduler` is planned execution control flow.

Jobs are planned deterministic native transforms.

Keep semantic planning and execution ownership separate.