# Scheduler, workspace, and job ownership

This document defines planned architecture.

`GenerationWorkspace`, `OperationScheduler`, operation scratch, field handles, job dependencies, and Burst jobs are future execution concepts. They are not current Runtime behavior unless corresponding code exists.

## Purpose

The current managed architecture ends at `GenerationPlan`.

Future execution requires a strict ownership split between executable metadata, native storage, orchestration, and jobs.

The planned boundary is:

```text
RunnablePlan
    -> GenerationWorkspace
    -> OperationScheduler
    -> Jobs
````

Each layer owns one responsibility:

| Layer                 | Owns                                                                         |
| --------------------- | ---------------------------------------------------------------------------- |
| `RunnablePlan`        | Immutable executable metadata.                                               |
| `GenerationWorkspace` | Native storage allocation, access, lifetime, and disposal.                   |
| `OperationScheduler`  | Operation execution control flow, dependencies, scratch, and job scheduling. |
| Jobs                  | Deterministic transforms over native containers and unmanaged values.        |

## Non-goals

Workspace code must not resolve request descriptors.

Workspace code must not choose recipes, routes, or operation implementations.

Workspace code must not compile managed plans.

Scheduler code must not perform catalog lookup.

Scheduler code must not reinterpret resource identity.

Scheduler code must not mutate the catalog, request, managed plan, or runnable plan.

Jobs must not know managed domain metadata.

Jobs must not inspect symbols, catalogs, recipes, requests, plans, resources, field definitions, workspaces, or schedulers.

## Current versus future boundary

Current implemented flow:

```text
GenerationRequestDescriptor
        +
GenerationCatalog
        |
        v
GenerationRequestResolver
        |
        v
GenerationRequest
        |
        v
GenerationPlanCompiler
        |
        v
GenerationPlan
```

Future execution flow:

```text
GenerationPlan
        |
        v
RunnablePlanCompiler
        |
        v
RunnablePlan
        |
        v
GenerationWorkspace
        |
        v
OperationScheduler
        |
        v
Jobs
```

Future execution starts after a runnable plan exists.

## RunnablePlan boundary

`RunnablePlan` is future immutable executable metadata.

It may contain:

```text
runnable stages
runnable operations
resource-to-field bindings
operation-to-scheduler bindings
field access metadata
dependency metadata
scratch metadata
artifact capture metadata
external input metadata
execution profile metadata
```

It must not contain:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
allocated field storage
operation scratch storage
running scheduler instances
JobHandle state
completed job results
Unity scene objects
Unity editor objects
```

The runnable plan describes execution. It does not execute.

## GenerationWorkspace

`GenerationWorkspace` is future per-run native storage ownership.

A workspace owns:

```text
field storage allocation
field storage access
field handle mapping
external field binding
operation scratch allocation when delegated by scheduler policy
native container disposal
workspace lifetime
storage safety validation
```

A workspace does not own:

```text
catalog lookup
request descriptor resolution
recipe selection
managed plan compilation
runnable plan compilation
operation semantic meaning
scheduler policy
job algorithm logic
```

The workspace is storage ownership, not generation intelligence.

## Workspace lifetime

A workspace belongs to one generation execution scope.

Expected lifecycle:

```text
create workspace from runnable plan
bind external inputs
allocate required fields
execute scheduler sequence
capture requested artifacts
dispose workspace storage
```

The workspace must make disposal explicit.

Native storage must not leak into catalog, request, plan, or module static state.

## Workspace allocation

The workspace allocates native storage from runnable metadata.

Allocation may use:

```text
field shape
value kind
storage policy
field lifetime
execution profile
grid dimensions
external input bindings
scratch requirements
capture requirements
```

The workspace must not infer semantic meaning by parsing symbols.

The runnable plan compiler should already have resolved semantic resources to field metadata.

## Field handles

A field handle is future execution-time addressing for workspace-owned storage.

A field handle is valid only within the workspace that created it.

A field handle is not:

```text
Symbol
ResourceDefinition
FieldDefinition
NativeArray<T>
catalog identity
artifact identity
global ID
```

A field handle must not be persisted as semantic identity.

Field handles may be stable within a run if the workspace defines stable assignment rules.

## Field access

Workspace access APIs should expose storage through explicit access intent.

Possible future access modes:

```text
Read
Write
ReadWrite
Append
ExternalRead
DiagnosticWrite
PayloadWrite
```

Access intent should be derived from runnable operation metadata.

The workspace should not allow arbitrary mutation without access validation when safety policy requires validation.

## External storage binding

External field data must be bound explicitly.

External binding may include:

```text
field identity
source identity
native container ownership policy
shape validation
value-kind validation
lifetime policy
disposal ownership
read/write access policy
```

The workspace must not discover external data through:

```text
Unity scene search
current editor selection
asset path guessing
global mutable registry
static current context
scheduler-side symbol lookup
```

External storage ownership must be explicit.

## Workspace and artifacts

Artifact capture is future architecture.

The workspace may provide native data for artifact capture.

The workspace does not decide semantic capture policy by itself.

Capture policy may come from:

```text
execution profile
field definition metadata
runnable plan capture metadata
artifact system request
```

The workspace owns safe access to data. Artifact systems own artifact creation.

## OperationScheduler

`OperationScheduler` is future execution orchestration for one runnable operation or operation family.

A scheduler owns:

```text
operation execution control flow
dependency wiring
workspace field access requests
operation scratch allocation policy
job scheduling
job chain composition
iteration policy
termination policy
execution failure policy
profiling hooks when designed
```

A scheduler does not own:

```text
symbol resolution
catalog lookup
recipe selection
request resolution
managed plan compilation
resource identity
field definition identity
workspace lifetime
artifact semantic policy
```

A scheduler translates runnable operation metadata into scheduled work.

## Scheduler binding

A scheduler binding maps selected implementation metadata to a scheduler.

Planned binding:

```text
OperationImplementationDefinition -> SchedulerBinding
```

A scheduler binding may identify:

```text
scheduler type
scheduler factory
supported execution profiles
supported field shapes
supported value kinds
scratch requirements
dependency behavior
```

The binding is metadata.

The scheduler instance or schedule call owns execution.

## Scheduler input

A scheduler should receive accepted execution inputs.

Possible future scheduler input:

```text
RunnableOperation
GenerationWorkspace
input dependencies
execution profile
run settings
scheduler-local options
allocator policy
```

A scheduler must not receive:

```text
GenerationRequestDescriptor
GenerationCatalog
GenerationRecipeDefinition as lookup source
unresolved symbols
Unity scene state
Editor state
```

The runnable operation should already contain resolved executable metadata.

## Scheduler output

A scheduler may return execution state.

Possible future output:

```text
JobHandle
operation execution result
produced dependency handle
diagnostic metadata
scheduled artifact capture marker
failure result
```

The exact output model is future design.

The scheduler output should not mutate managed semantic objects.

## Job dependencies

Job dependency handles belong to scheduler/workspace execution.

They must not appear in:

```text
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
StagePlanNode
OperationPlanNode
ResourceDefinition
StageContract
OperationContract
```

A managed plan can imply semantic order. It must not carry actual `JobHandle` state.

## Dependency wiring

Schedulers own actual dependency wiring.

Dependency wiring may consider:

```text
runnable operation order
field read/write access
producer/consumer relationships
stage boundaries
external input availability
scratch dependencies
artifact capture points
```

Dependency wiring must be deterministic.

Schedulers must not derive dependencies from nondeterministic collection enumeration, thread timing, or job completion race order.

## Operation scratch

Operation scratch is future scheduler-owned temporary native storage.

Scratch exists to support execution of one operation or bounded job chain.

Scratch is not:

```text
ResourceDefinition
FieldDefinition
canonical field
payload field
artifact output
catalog definition
request setting
managed plan node
```

Scratch memory should be private to the scheduler unless explicitly promoted to a modeled field.

## Scratch lifetime

Scratch lifetime should be explicit.

Possible future scratch lifetimes:

```text
single job
single operation
operation iteration
stage execution
scheduler chain
```

Scratch should be disposed or released by the owner that allocates it.

If the scheduler requests workspace-managed scratch, ownership and disposal must be explicit.

## Repeated job chains

Some operations may require repeated scheduling.

Examples:

```text
flood fill
region growth
constraint relaxation
erosion iterations
normalization passes
```

The scheduler owns repeated job-chain control.

Jobs execute one deterministic step or batch.

Jobs must not schedule additional jobs.

Jobs must not control high-level termination unless that control is explicitly modeled as unmanaged data passed by the scheduler.

## Termination policy

Schedulers own termination policy for repeated or conditional execution.

Termination policy may use:

```text
fixed iteration count
convergence threshold
maximum pass count
work queue exhaustion
profile-defined limit
operation-specific deterministic condition
```

Termination must be deterministic for the same accepted inputs.

Do not use wall-clock time, frame count, thread timing, or nondeterministic completion order as generation semantics.

## Failure policy

Schedulers own operation execution failure policy.

Failure categories may include:

```text
workspace access failure
missing field handle
scratch allocation failure
job scheduling failure
job validation failure
iteration limit exceeded
unsupported profile
unsupported field shape
artifact capture dependency failure
```

The exact error model is future design.

Execution failures should not be reported as request-resolution failures.

## Job boundary

Jobs are deterministic transforms over native data.

A job may receive:

```text
NativeArray<T>
NativeSlice<T>
NativeList<T>
NativeHashMap<TKey, TValue>
NativeReference<T>
unmanaged structs
primitive values
grid dimensions as primitive values
seed values as primitive values
```

A job must not receive:

```text
Symbol
DisplayName
GenerationCatalog
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
OperationDefinition
OperationImplementationDefinition
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
RunnablePlan
RunnableOperation
FieldDefinition
GenerationWorkspace
OperationScheduler
UnityEngine.Object
UnityEditor types
managed collections
managed delegates
```

The scheduler converts metadata into job-safe inputs before scheduling.

## Job ownership

A job owns only its algorithmic transform.

A job can define:

```text
input native data
output native data
unmanaged parameters
per-element execution logic
parallel execution strategy
deterministic math
```

A job cannot define:

```text
which recipe is selected
which implementation is selected
which resource is semantic truth
which field lifetime exists
which artifact is captured
which operation runs next
how the workspace is disposed
```

Those decisions belong to earlier or higher execution layers.

## Burst compatibility

Future jobs should be Burst-compatible unless a specific scheduler intentionally supports managed execution.

Burst-compatible job code should avoid:

```text
managed allocations
managed object references
virtual dispatch
exceptions as normal control flow
string usage
LINQ
UnityEngine.Object access
static mutable state
nondeterministic global random state
```

Burst compatibility is an execution concern. It must not affect current managed catalog/request/plan object design.

## Native container ownership

Native containers must have one clear owner.

Possible owners:

```text
GenerationWorkspace for field storage
OperationScheduler for operation scratch
external caller for externally bound input when policy says caller-owned
artifact system for captured output buffers
```

Jobs borrow native containers. They do not own disposal.

Catalogs, recipes, requests, and managed plans must not own native containers.

## Disposal ownership

The owner that allocates native memory owns disposal unless an explicit transfer policy exists.

Correct:

```text
workspace allocates field storage
workspace disposes field storage

scheduler allocates private scratch
scheduler disposes private scratch

caller provides external native input as caller-owned
caller disposes after agreed lifetime
```

Incorrect:

```text
job disposes workspace field storage
managed plan disposes NativeArray<T>
catalog holds persistent native container
scheduler disposes caller-owned external input without policy
```

## Workspace and scheduler interaction

The scheduler should request field access from the workspace using resolved runnable metadata.

Correct future interaction:

```text
scheduler receives RunnableOperation
scheduler requests input/output field access from workspace
workspace returns native container access or typed handles
scheduler schedules jobs with native containers and unmanaged values
scheduler returns dependency/output execution state
```

Incorrect interaction:

```text
scheduler parses resource symbols
scheduler searches catalog
scheduler creates fields not described by runnable metadata
workspace selects operation implementation
job asks workspace for a field by symbol
```

## Typed access

Future APIs should avoid unsafe untyped access when practical.

Good direction:

```text
workspace resolves typed field access before job scheduling
scheduler validates expected value kind and shape
job receives typed native containers
```

Avoid:

```text
job casts opaque field storage
scheduler guesses value type from string
workspace exposes object-typed native storage to jobs
```

Typed access should be compatible with Burst and Unity safety restrictions.

## ECS boundary

Future ECS integration should be an adapter or execution backend.

The core workspace/scheduler model should not require an ECS `World` for semantic correctness.

Possible ECS integration:

```text
ECS system drives execution of a runnable plan
ECS components reference execution handles
ECS graphics consumes payload artifacts
```

Invalid ECS coupling:

```text
ResourceDefinition stores Entity
GenerationPlan stores SystemHandle
Job reads GenerationCatalog through ECS singleton
Catalog identity depends on ECS World
```

ECS is an integration surface, not the catalog/request/plan domain model.

## Unity boundary

Workspace and scheduler code may use Unity Collections, Jobs, Burst, and Mathematics when they are part of execution.

Workspace and scheduler code must not use Unity scene/editor state as domain truth.

Do not use:

```text
GameObject.Find
Object.FindObjectsByType
Resources.Load for runtime domain lookup
AssetDatabase
current selection
scene hierarchy order
MonoBehaviour names as symbols
```

Unity-facing adapters may prepare accepted execution inputs. Execution systems consume them explicitly.

## Thread safety

Future workspace and scheduler APIs must define thread-safety expectations.

Workspace mutation during scheduling must be controlled.

Schedulers must respect Unity job safety rules.

Native containers must not be accessed concurrently in conflicting ways without dependencies or safety mechanisms.

Thread safety must not depend on undocumented call order or editor-only behavior.

## Determinism

Execution must be deterministic for the same accepted inputs and execution profile.

Do not use:

```text
current time
frame count
wall-clock duration
thread completion race order
native memory address
hash map enumeration order
unordered parallel writes
global random state
Unity object instance ID
```

Use explicit deterministic inputs:

```text
Grid
Seed
GenerationRunSettings
RunnablePlan order
field binding order
scheduler-defined deterministic iteration order
stable unmanaged random streams derived from Seed
```

Parallel jobs must avoid nondeterministic write conflicts.

## Randomness

Randomness must be derived from accepted seed data.

A scheduler may derive per-operation or per-job seeds from:

```text
run seed
operation identity
route-step identity
iteration index
stable worker partition index
```

Do not use:

```text
UnityEngine.Random global state
System.Random shared mutable instance
current time
thread ID as generation identity
job execution order
```

Seed derivation must be stable and documented by the owner.

## Ordering

Execution ordering must be explicit.

Possible ordering sources:

```text
runnable stage order
runnable operation order
field dependency graph
resource producer/consumer graph
scheduler-local deterministic partition order
profile-defined ordering
```

Avoid ordering from:

```text
dictionary enumeration
hash set enumeration
native hash map enumeration
parallel completion order
Unity scene order
asset import order
```

When a scheduler introduces parallelism, the final output must not depend on nondeterministic completion order.

## Error handling

Invalid API usage should throw.

Examples:

```text
null workspace
null runnable operation
invalid scheduler configuration
requesting field access with incompatible value kind
using a disposed workspace
```

Expected execution failure should use the future execution result/error model.

Examples:

```text
missing field handle
unsupported scheduler binding
scratch allocation failure
job scheduling failure
iteration limit exceeded
external input unavailable
artifact capture failed
```

Execution failure is not request-resolution failure.

## Logging

Workspace and scheduler code should not rely on logging as the primary failure mechanism.

Correct:

```text
scheduler returns execution failure
workspace throws for invalid API usage
artifact system returns capture failure
```

Incorrect:

```text
scheduler logs missing field and continues with default
workspace logs allocation failure and returns invalid handle
job silently skips invalid data
```

Logs may supplement structured errors. They must not replace them.

## Artifact capture

Artifact capture is future architecture.

A scheduler may coordinate capture timing.

The workspace may provide data access.

The artifact system owns artifact construction.

Capture must not require jobs to know artifact policy.

Correct:

```text
runnable metadata defines capture point
scheduler ensures dependencies are complete
workspace exposes data safely
artifact system captures output
```

Incorrect:

```text
job writes editor artifact directly
ResourceDefinition owns capture file path
GenerationPlan stores texture output
scheduler guesses capture from display name
```

## Profiling and diagnostics

Profiling and diagnostics are execution concerns.

Schedulers may expose profiling hooks when designed.

Diagnostics should not affect deterministic generation output unless explicitly modeled.

Diagnostic fields should be profile-controlled.

Do not make diagnostic behavior part of resource identity unless it is semantic generation truth.

## Memory budgeting

Memory budgeting belongs to future execution configuration.

Workspace allocation may use:

```text
execution profile
field definitions
storage policies
run grid size
artifact capture policy
external input policy
```

Memory budgeting must not mutate semantic resource identity or managed plan structure.

If a memory budget cannot satisfy a runnable plan, execution preparation should fail through the future execution error model.

## Implementation placement

Future workspace/scheduler/job code should live in explicit execution-oriented folders.

Possible folders:

```text
Runtime/Execution/
Runtime/Execution/Workspaces/
Runtime/Execution/Schedulers/
Runtime/Execution/Jobs/
Runtime/Execution/Scratch/
Runtime/Execution/Artifacts/
```

Do not place workspace, scheduler, or job ownership code inside:

```text
Runtime/Catalog/
Runtime/Recipes/
Runtime/Requests/
Runtime/Plans/
Runtime/Resources/
Runtime/Stages/
Runtime/Operations/
```

except for references required by accepted metadata.

## Public API naming

Use precise execution names.

Recommended future names:

```text
GenerationWorkspace
FieldHandle
FieldAccess
OperationScheduler
SchedulerBinding
OperationScratch
ExecutionResult
ExecutionError
ExecutionErrorCode
```

Use `Job` only for actual job structs or job-like execution units.

Avoid vague names:

```text
ExecutionManager
WorkspaceContext
JobProcessor
DataRunner
NativeStuff
OperationHandler
```

A name must reveal ownership.

## Testing guidance

Future workspace tests should verify:

```text
field allocation from runnable metadata
field handle validity
external input binding validation
native disposal
disposed workspace rejection
deterministic handle assignment
shape and value-kind access validation
```

Future scheduler tests should verify:

```text
dependency wiring
workspace access requests
scratch allocation and disposal
job scheduling inputs
deterministic ordering
failure handling
no catalog/request/plan mutation
```

Future job tests should verify:

```text
deterministic native transforms
Burst compatibility where required
no managed metadata dependencies
no allocations in hot paths
safe parallel writes
stable seed derivation
```

## Documentation guidance

Current architecture documents may mention workspace, scheduler, and jobs only as future concepts.

Future documents must clearly mark them as not implemented until code exists.

Do not describe jobs as current operation implementations.

Do not describe `GenerationPlan` as executable job data.

Do not describe resource definitions as native storage.

Do not describe schedulers as catalog or resolver components.

## Review checklist

Before accepting future execution architecture, verify:

```text
RunnablePlan remains immutable executable metadata.
GenerationWorkspace owns native allocation, access, and disposal.
OperationScheduler owns execution control flow and dependency wiring.
Jobs receive only native containers and unmanaged values.
Jobs do not know symbols, catalogs, resources, requests, plans, fields, workspaces, or schedulers.
Native containers are not stored in catalog, recipe, request, or managed plan objects.
JobHandle state is not stored in managed semantic plans.
External inputs are explicit.
Scratch memory is not modeled as a resource unless intentionally promoted.
Disposal ownership is explicit.
Ordering is deterministic.
Parallel writes are safe and deterministic.
Execution errors are not request-resolution errors.
Unity scene/editor state is not domain truth.
```

## Summary

`RunnablePlan` describes executable metadata.

`GenerationWorkspace` owns native storage.

`OperationScheduler` owns orchestration, dependencies, scratch, and job scheduling.

Jobs own only deterministic transforms over native containers and unmanaged values.

Native memory, job handles, schedulers, and execution failure policy belong after managed plan compilation.

Catalog, recipe, request, and managed plan objects must stay free of execution state.

```