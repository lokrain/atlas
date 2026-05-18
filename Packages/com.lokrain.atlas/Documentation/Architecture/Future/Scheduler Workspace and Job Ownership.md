# Scheduler, workspace, and job ownership

This document describes planned architecture.

`GenerationWorkspace`, `OperationScheduler`, operation scratch, runnable execution, native storage, jobs, artifacts, diagnostics, and ECS execution integration are not current Runtime behavior unless corresponding Runtime code exists.

Current Runtime architecture ends at `GenerationPlan`.

## Purpose

Scheduler and workspace architecture separates executable metadata, native storage ownership, execution control flow, and deterministic job code.

The planned execution flow is:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> RunnablePlan
  -> GenerationWorkspace
  -> OperationScheduler
  -> Jobs
```

Each layer owns a different responsibility.

## Ownership summary

| Concept | Status | Owns |
| --- | --- | --- |
| `GenerationPlan` | Current | Managed semantic plan. |
| `RunnablePlan` | Planned | Immutable executable metadata. |
| `GenerationWorkspace` | Planned | Native storage allocation, access, lifetime, and disposal. |
| `OperationScheduler` | Planned | Execution control flow, dependency wiring, scratch, and job scheduling. |
| Jobs | Planned | Deterministic transforms over native containers and unmanaged values. |
| Artifacts | Planned | Captured outputs from execution data. |
| Diagnostics | Planned | Validation, execution, and tooling data. |

## Current boundary

Current Runtime does not include:

```text
GenerationWorkspace
OperationScheduler
OperationScratch
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
JobHandle
Burst job structs
field handles
scheduler bindings
artifact buffers
execution diagnostics
ECS entities
```

Do not add these to current managed semantic objects.

Invalid current placements:

```text
GenerationPlan.JobHandle
GenerationPlan.FieldHandle
StagePlanNode.OperationScheduler
OperationPlanNode.NativeArray<T>
OperationContract.FieldHandle
ResourceDefinition.NativeArray<T>
GenerationRecipeDefinition.GenerationWorkspace
```

## RunnablePlan boundary

`RunnablePlan` is planned immutable executable metadata.

A runnable plan may contain:

```text
runnable stages
runnable operations
field bindings
scheduler bindings
storage requirements
capture declarations
diagnostic declarations
external binding requirements
```

A runnable plan must not own native storage lifetime.

A runnable plan must not schedule jobs.

Correct:

```text
RunnablePlan describes what execution requires.
GenerationWorkspace allocates where execution stores data.
OperationScheduler controls how execution runs.
```

Incorrect:

```text
RunnablePlan owns NativeArray<T>.
RunnablePlan schedules JobHandle.
RunnablePlan disposes workspace memory.
```

## GenerationWorkspace

`GenerationWorkspace` is planned native storage ownership for one generation run.

A workspace owns:

```text
canonical field storage
temporary field storage
external field bindings
diagnostic field storage
operation scratch storage when scratch is workspace-scoped
native allocation lifetime
field handle validity
disposal
```

A workspace must not own:

```text
symbol resolution
catalog validation
recipe selection
request resolution
managed plan compilation
runnable plan compilation
semantic resource identity
operation implementation selection
```

A workspace is execution storage. It is not semantic inventory.

## Workspace lifetime

A workspace lifetime is tied to one generation run or one explicit execution session.

The workspace must define:

```text
allocation ownership
read/write access rules
disposal ownership
handle validity lifetime
external binding lifetime
diagnostic storage lifetime
temporary storage lifetime
```

Workspace-owned storage must not outlive the workspace unless exported as an artifact or copied into another explicit owner.

## Field handles

Field handles are planned execution-time references to workspace-owned field storage.

A field handle must be valid only within the workspace context that created it.

A field handle should not be stable package identity.

Use resource and field symbols for identity.

Use field handles for execution access.

Do not store field handles in current managed objects.

## Storage categories

A workspace may allocate or bind several storage categories.

### Canonical storage

Canonical storage is authoritative generated data for a semantic resource.

Example:

```text
BaseElevation cell-grid field
```

### Temporary storage

Temporary storage is intermediate execution data.

Temporary storage may be discarded after the consuming operation or stage.

### External storage

External storage is provided by a caller, importer, editor tool, or integration layer.

The workspace validates and binds external storage according to runnable metadata.

### Diagnostic storage

Diagnostic storage supports debugging, validation, visualization, or tooling.

Diagnostic capture depends on execution profile policy.

### Scratch storage

Scratch storage supports scheduler or operation-local computation.

Scratch storage must not be treated as canonical generated output.

## OperationScheduler

`OperationScheduler` is planned execution control flow.

A scheduler owns:

```text
operation execution lifecycle
workspace access for one runnable operation
dependency wiring
job scheduling
scratch allocation
iteration policy
termination policy
failure policy
diagnostic emission
```

A scheduler must not own:

```text
catalog lookup
symbol resolution
recipe selection
request resolution
managed plan compilation
runnable plan compilation
semantic resource identity
workspace lifetime outside its assigned scope
```

The scheduler receives resolved executable metadata. It does not reinterpret the domain model.

## Scheduler binding

A scheduler binding is metadata in the runnable plan.

It tells execution which scheduler is responsible for one runnable operation and what policy applies.

A scheduler binding may describe:

```text
scheduler type
input and output field bindings
scratch requirements
dependency requirements
iteration policy
termination policy
failure policy
diagnostic policy
```

The binding is metadata.

The scheduler performs execution.

## Operation scratch

Operation scratch is temporary execution storage used by a scheduler or operation.

Scratch may be used for:

```text
frontiers
work queues
temporary labels
temporary region lists
priority queues
intermediate reductions
```

Scratch ownership must be explicit.

Allowed owners:

```text
OperationScheduler
GenerationWorkspace
specialized execution allocator
```

Disallowed owners:

```text
ResourceDefinition
OperationContract
GenerationRecipeDefinition
GenerationRequest
GenerationPlan
OperationPlanNode
Job struct beyond its scheduled execution lifetime
```

Scratch must not become canonical output unless explicitly copied into canonical storage.

## Jobs

Jobs are planned deterministic transforms over native data.

Jobs receive:

```text
native containers
unmanaged values
explicit dimensions
explicit seeds
explicit configuration values
```

Jobs must not receive:

```text
Symbol
DisplayName
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
ResourceDefinition
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
UnityEngine.Object
UnityEditor object
```

If a job needs data, the scheduler resolves that data before scheduling and passes it as native containers or unmanaged parameters.

## Job responsibility

A job owns computation for one scheduled unit of work.

A job does not own:

```text
resource identity
field identity
workspace lifetime
scheduler policy
catalog lookup
request resolution
plan compilation
artifact capture policy
```

Correct planned job responsibility:

```text
Read input arrays.
Write output arrays.
Use explicit dimensions.
Use explicit seed or deterministic parameters.
Return through native output data.
```

Incorrect job responsibility:

```text
Look up ResourceDefinition by Symbol.
Open a catalog.
Choose an implementation.
Allocate persistent workspace memory.
Write artifact files.
```

## Dependency wiring

The scheduler owns job dependency wiring.

Dependencies must be explicit and deterministic.

Do not rely on:

```text
thread timing
implicit Unity scheduling order
hash-map enumeration order
object allocation order
scene hierarchy order
```

Correct:

```text
scheduler schedules job B after job A's JobHandle
```

Incorrect:

```text
job B assumes job A finished because it was scheduled earlier from another system
```

## Iteration and termination

Some operations may require iterative execution.

The scheduler owns iteration policy.

Iteration policy may define:

```text
maximum iteration count
convergence condition
frontier exhaustion
fixed pass count
failure behavior
diagnostic capture
```

Jobs perform individual deterministic steps.

Schedulers decide whether another step is needed.

## Failure policy

Execution failure policy is planned.

Expected execution failures should be represented with structured execution results or diagnostics.

Invalid API usage should throw.

Examples of expected execution failures:

```text
external field missing
workspace allocation failed according to policy
operation did not converge
diagnostic validation failed
scheduler policy rejected execution
```

Examples of invalid API usage:

```text
null runnable plan
disposed workspace
field handle used with wrong workspace
scheduler invoked with incompatible runnable operation
```

## Artifact ownership

Artifacts are planned captured outputs.

Artifacts may be created from workspace data after or during execution according to policy.

Artifact ownership must be explicit.

Possible owners:

```text
artifact capture service
execution result
caller-provided artifact sink
editor tooling adapter
```

Invalid owners:

```text
ResourceDefinition
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
OperationContract
```

## Diagnostics ownership

Diagnostics may be managed validation diagnostics or execution diagnostics.

Current managed diagnostics:

```text
exceptions
GenerationRequestResolutionError
```

Planned execution diagnostics:

```text
scheduler timings
workspace allocation summaries
captured diagnostic fields
job validation summaries
artifact metadata
operation failure diagnostics
```

Execution diagnostics must not be stored in current semantic definitions, requests, or plans.

## ECS integration

ECS integration is planned and must remain an adapter or execution integration boundary.

Current managed Runtime objects must not depend on ECS:

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

Correct planned boundary:

```text
Runnable execution output
  -> ECS integration adapter
  -> ECS world data
```

Incorrect current boundary:

```text
GenerationPlan stores Entity.
OperationContract references IComponentData.
ResourceDefinition references EntityQuery.
GenerationRecipeDefinition depends on ISystem.
```

## Unity object boundary

Execution code may run inside Unity, but Unity object identity must not become domain identity.

Do not use:

```text
GameObject.name
UnityEngine.Object.GetInstanceID()
asset path
scene hierarchy index
ScriptableObject reference identity
editor selection state
```

as deterministic generation identity.

Use package-owned symbols and accepted definitions for identity.

## Native container policy

Use standard Unity native containers by default.

Use lower-level unsafe APIs only when the data shape or performance requirement justifies them.

Valid reasons may include:

```text
specific memory layout
explicit lifetime control
topology construction
controlled aliasing
memory-range operations
interop
artifact layout
measured target workload performance
```

Unsafe code must be isolated in execution infrastructure.

Unsafe code must not leak into current semantic Runtime layers.

## Data structure policy

Choose data structures by data shape.

Dense raster data uses row-major one-dimensional storage.

Fixed-degree topology uses direct computation or fixed-stride arrays.

Variable one-to-many topology uses frozen compressed sparse layouts.

Mutable topology builders may use streams, unsafe lists, or native multi-hash maps during construction, but must freeze into deterministic arrays before canonical use.

Connectivity labeling uses union-find, deterministic flood-fill frontiers, or another explicitly ordered labeling algorithm.

Priority propagation uses deterministic heaps, bucket queues, radix-style queues, or another priority structure with explicit tie-breaking.

Spatial lookup uses chunk buckets before trees.

Canonical graph state uses arrays of nodes, edges, offsets, counts, and values.

Atlas does not use managed object graphs as canonical generation data.

## Determinism

Execution must be deterministic for the same accepted input and execution profile.

Deterministic execution requires:

```text
explicit ordering
explicit tie-breaking
stable seeds
stable dimensions
stable data layout
stable dependency wiring
stable capture policy
```

Do not use:

```text
current time
global random state
thread timing
hash-map enumeration order
Unity object instance ID
asset import order
scene hierarchy order
managed object allocation order
```

for deterministic output.

## Disposal

Workspace-owned native storage must be disposed by the workspace or by an explicit owner declared by the workspace.

Schedulers must not leak native allocations.

Jobs must not retain references beyond their valid scheduled lifetime.

External bindings must document whether the workspace owns disposal or only borrows access.

## Testing expectations

When implemented, tests should verify:

```text
workspace rejects invalid allocation requests
workspace owns and disposes native storage
field handles cannot be used with the wrong workspace
runnable plan does not allocate storage
scheduler rejects incompatible runnable operations
scheduler wires dependencies deterministically
jobs receive only native containers and unmanaged values
scratch storage is not exposed as canonical output
artifact capture follows policy
diagnostics follow policy
disposed workspace access fails predictably
```

## Invalid boundary crossings

Do not implement these:

```text
GenerationPlan.Schedule()
GenerationPlan.JobHandle
StagePlanNode.NativeBuffer
OperationPlanNode.Execute()
OperationPlanNode.FieldHandle
OperationContract.NativeArray<T>
ResourceDefinition.FieldHandle
GenerationRecipeDefinition.OperationScheduler
GenerationRequest.GenerationWorkspace
Job receives GenerationPlan
Job receives ResourceDefinition
Job reads Symbol
```

Each item mixes semantic planning with execution ownership.

## Checklist

Before implementing scheduler, workspace, or job code, verify:

```text
Does the runnable plan own only metadata?
Does the workspace own native storage lifetime?
Does the scheduler own dependency wiring and job scheduling?
Do jobs receive only native containers and unmanaged values?
Is scratch ownership explicit?
Is canonical storage separate from scratch and temporary storage?
Are artifacts captured by an explicit artifact owner?
Are diagnostics separated from current semantic objects?
Does ECS integration stay outside current Runtime planning objects?
Is disposal ownership explicit?
Is deterministic ordering explicit?
Are unsafe APIs isolated in execution infrastructure?
```

## Summary

`GenerationWorkspace` is planned native storage ownership.

`OperationScheduler` is planned execution control flow.

Jobs are planned deterministic native transforms.

Runnable plans describe execution metadata.

Workspaces allocate storage.

Schedulers schedule work.

Jobs transform data.

Current managed Runtime objects remain semantic and stop at `GenerationPlan`.