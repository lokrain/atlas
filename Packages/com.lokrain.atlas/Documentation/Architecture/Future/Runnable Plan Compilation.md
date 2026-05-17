# Runnable plan compilation

This document defines planned architecture.

`RunnablePlanCompiler`, `RunnablePlan`, `RunnableStage`, `RunnableOperation`, scheduler bindings, field bindings, and runnable compilation errors are future execution concepts. They are not current Runtime behavior unless corresponding code exists.

## Purpose

The current managed pipeline ends at `GenerationPlan`.

A `GenerationPlan` describes semantic managed generation work. It does not contain executable metadata, native storage, field handles, dependency handles, scheduler bindings, or job data.

Future runnable plan compilation converts a managed semantic plan into execution-ready metadata.

Planned flow:

```text
GenerationPlan
        +
FieldDefinitionSet
        +
ExecutionProfile
        +
SchedulerBindingCatalog
        |
        v
RunnablePlanCompiler
        |
        v
RunnablePlan
````

The runnable plan compiler answers:

```text
Which fields represent the plan resources?
Which scheduler executes each selected implementation?
Which runnable operations exist?
Which executable dependencies are implied by semantic resource flow and operation order?
What metadata does the workspace need before execution?
```

It does not allocate native storage and does not schedule jobs.

## Non-goals

Runnable plan compilation must not replace managed plan compilation.

Runnable plan compilation must not resolve request descriptor symbols.

Runnable plan compilation must not mutate the catalog, recipe, request, or managed plan.

Runnable plan compilation must not allocate native containers.

Runnable plan compilation must not create or complete `JobHandle` dependencies.

Runnable plan compilation must not execute operations.

Runnable plan compilation must not read Unity scene state, editor state, or global service locators.

## Current versus future boundary

Current implemented boundary:

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

Future execution boundary:

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

The runnable compiler starts after `GenerationPlan`.

It consumes accepted managed plan data. It must not consume unresolved descriptors.

## Inputs

A future `RunnablePlanCompiler` should consume accepted execution metadata.

Expected input categories:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
SchedulerBindingCatalog
external input bindings if designed
artifact capture policy if designed
```

The exact API shape is not defined here. The architectural requirement is that inputs are explicit and accepted before compilation.

## GenerationPlan input

`GenerationPlan` is the semantic source.

It provides:

```text
GenerationRecipeDefinition
GenerationRunSettings
ordered StagePlanNode list
ordered OperationPlanNode list
ResourceDefinition references through contracts
OperationImplementationDefinition selections
```

The runnable compiler must preserve semantic ordering from the managed plan unless it derives a deterministic execution order that is explicitly validated.

The runnable compiler must not reinterpret the request descriptor or choose a different recipe.

## FieldDefinitionSet input

`FieldDefinitionSet` is future field metadata inventory.

It provides storage-facing metadata for semantic resources.

The runnable compiler uses it to bind:

```text
ResourceDefinition -> FieldDefinition
```

The runnable compiler should validate that every required and produced resource in the managed plan has a compatible field definition for the selected execution profile.

The runnable compiler must not allocate storage for fields.

## ExecutionProfile input

`ExecutionProfile` is future representation and policy selection.

It may affect:

```text
field representation
scheduler binding
artifact capture
diagnostic fields
precision
memory policy
external input binding
```

The runnable compiler may use the profile to select compatible field definitions and scheduler bindings.

The profile must not change resource identity.

## SchedulerBindingCatalog input

`SchedulerBindingCatalog` is planned execution metadata.

It maps selected operation implementation definitions to schedulers or scheduler factories.

Expected binding:

```text
OperationImplementationDefinition -> SchedulerBinding
```

A scheduler binding is not a job and not a running scheduler instance.

It is metadata that identifies how a runnable operation can be executed.

## Output

`RunnablePlan` is future executable metadata.

It should contain enough accepted metadata for workspace allocation and scheduler execution.

A runnable plan may contain:

```text
GenerationRunSettings
ExecutionProfile
RunnableStage list
RunnableOperation list
resource-to-field bindings
operation-to-scheduler bindings
field access metadata
dependency metadata
artifact capture metadata
external input metadata
```

A runnable plan must not contain:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
allocated workspace storage
running scheduler instances
JobHandle
completed job results
Unity scene objects
Unity editor objects
```

A runnable plan is not the workspace and not execution.

## RunnablePlan

`RunnablePlan` is planned accepted executable metadata for one generation run.

It is produced from a `GenerationPlan`.

It is consumed by future workspace and scheduler systems.

It should be immutable after compilation.

It should preserve stable deterministic ordering.

It should expose only accepted metadata.

It should not expose mutable compiler internals.

## RunnableStage

`RunnableStage` is a planned executable stage entry inside a runnable plan.

It may contain:

```text
StageDefinition
StageRouteDefinition
StageContract
ordered RunnableOperation list
stage-level field requirements
stage-level dependency metadata
stage-level capture metadata
```

A runnable stage is still metadata.

It does not allocate storage and does not schedule jobs.

## RunnableOperation

`RunnableOperation` is a planned executable operation entry inside a runnable stage.

It may contain:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
resource-to-field bindings
scheduler binding
input field access descriptors
output field access descriptors
scratch requirements
dependency metadata
profile-specific execution metadata
```

A runnable operation binds semantic plan data to execution metadata.

It is not a scheduler instance and not a job.

## Field binding

A field binding connects a semantic resource to a storage-facing field definition.

Planned binding:

```text
ResourceDefinition -> FieldDefinition
```

The runnable compiler should produce deterministic field bindings for all resources used by the plan.

Field binding validation should include:

```text
required resource has a compatible field definition
produced resource has a compatible field definition
field definition belongs to the active field definition set
field definition is compatible with the execution profile
field lifetime is valid for the operation/stage usage
field shape is compatible with operation requirements
value kind is compatible with scheduler binding requirements
```

Field binding does not allocate native storage.

## Access binding

An access binding describes how an operation accesses a field.

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

Access binding may be used to validate dependency ordering and scheduler compatibility.

Access binding is metadata. Native safety and actual dependency handles are future scheduler/workspace responsibilities.

## Scheduler binding

A scheduler binding connects an operation implementation choice to execution control flow.

Planned binding:

```text
OperationImplementationDefinition -> SchedulerBinding
```

The scheduler binding may identify:

```text
scheduler type
scheduler factory
supported field shapes
supported value kinds
supported execution profiles
scratch requirements
job graph strategy
failure policy category
```

The scheduler binding must not execute jobs during runnable compilation.

## Dependency metadata

Runnable compilation may derive metadata for execution dependencies.

Dependency metadata may include:

```text
operation order constraints
resource read/write relationships
stage boundaries
field producer/consumer relationships
external input availability
artifact capture points
```

Dependency metadata is not a `JobHandle`.

It is input used later by schedulers to create actual job dependencies.

## Semantic order and execution order

Managed plan order is semantic.

Execution order may need additional dependency analysis.

The runnable compiler may preserve managed order directly or derive a compatible execution order.

Any derived order must be deterministic and must preserve semantic correctness.

Do not derive order from:

```text
dictionary enumeration order
hash set enumeration order
managed object reference order
thread timing
Unity scene order
native memory addresses
```

Use explicit inputs such as:

```text
stage plan node order
operation plan node order
resource producer/consumer relationships
field definition symbol order
scheduler binding order
profile-defined ordering
```

## Resource producer validation

The runnable compiler should validate that required resources are produced or externally provided before use.

Validation may include:

```text
stage required inputs are available
operation required inputs are available
operation produced outputs have compatible fields
resource producer order is deterministic
multiple producers are allowed only when explicitly modeled
external resources are explicitly bound
```

The exact validation model depends on future resource-flow semantics.

The compiler must not silently create placeholder resources or fields.

## Multiple producers

Multiple producers for the same resource require explicit semantics.

Possible future policies:

```text
disallow multiple producers
allow overwrite with explicit order
allow reduction/merge with explicit operation
allow profile-specific accumulation
```

The runnable compiler must not pick an arbitrary producer.

If multiple producers are unsupported, compilation should fail with a structured error.

## Missing fields

If a plan references a resource without a compatible field definition, runnable compilation should fail.

Missing field failure is not a request-resolution error.

It belongs to future runnable compilation.

Example:

```text
GenerationPlan requires ResourceDefinition.Height
FieldDefinitionSet contains no compatible Height field
RunnablePlanCompiler returns missing-field error
```

Do not hide this failure by creating implicit fields unless the field generation policy is explicit and deterministic.

## Incompatible fields

If a field definition exists but is incompatible with the selected profile, scheduler binding, shape, or value kind, runnable compilation should fail.

Examples:

```text
operation requires cell-grid field but field is scalar
scheduler supports byte mask but field value kind is float
profile disables diagnostic field required by selected implementation
external input shape does not match run grid
```

Failure should be structured and owned by runnable compilation.

## Missing scheduler binding

If a selected operation implementation has no compatible scheduler binding, runnable compilation should fail.

Example:

```text
OperationImplementationDefinition.DefaultMainContinentExtraction
  has no scheduler binding for ProductionDeterministic profile
```

This is not a catalog error and not a request-resolution error.

The managed plan is valid. The executable metadata is incomplete for the chosen execution configuration.

## Incompatible scheduler binding

A scheduler binding may exist but be incompatible with selected field definitions or profile.

Examples:

```text
scheduler requires Height as Float32 but profile selected Float16
scheduler requires writable Land field but field is external read-only
scheduler requires cell-grid shape but field is sparse
scheduler is not supported for selected execution profile
```

The runnable compiler owns this validation.

## External input binding

External inputs are planned architecture.

If a resource is required but not produced inside the plan, it may be provided externally only through explicit binding.

External binding metadata should include:

```text
resource or field identity
source identity
shape expectation
value kind expectation
profile compatibility
lifetime and access mode
validation policy
```

The runnable compiler must not discover external inputs through global state or Unity scene search.

## Artifact capture metadata

Artifact capture is planned architecture.

Runnable compilation may compute capture metadata from:

```text
resource definitions
field definitions
execution profile
capture policy
operation/stage boundaries
diagnostic settings
payload requirements
```

Artifact capture metadata must not force current managed plan objects to know storage details.

Capture policy should be explicit and deterministic.

## Scratch metadata

Operation scratch is future scheduler-owned temporary storage.

Runnable operations may include scratch requirements.

Scratch metadata may describe:

```text
required temporary buffers
expected value kind
expected shape
lifetime scope
allocator preference
maximum size policy
```

Scratch metadata is not a field unless intentionally modeled as one.

Scratch storage is not a semantic resource and is not default artifact output.

## Workspace preparation

A future workspace may use a runnable plan to allocate fields.

Workspace preparation may consume:

```text
runnable field bindings
field lifetimes
field shapes
value kinds
storage policies
external input bindings
scratch requirements
capture metadata
```

The runnable compiler prepares metadata. The workspace owns allocation.

## Scheduler preparation

A future scheduler may use runnable operations to schedule jobs.

Scheduler preparation may consume:

```text
scheduler binding
input field access descriptors
output field access descriptors
scratch descriptors
dependency metadata
execution profile
run settings
```

The runnable compiler prepares metadata. The scheduler owns actual job scheduling.

## Job preparation

Schedulers convert runnable metadata into job-safe data.

Jobs receive:

```text
native containers
unmanaged parameters
numeric settings
deterministic seeds
grid dimensions or derived primitive values
```

Jobs must not receive:

```text
RunnablePlan
RunnableOperation
ResourceDefinition
FieldDefinition
SchedulerBinding
GenerationPlan
GenerationCatalog
Symbol
DisplayName
```

## Error model

Runnable compilation should have its own future result/error model.

Expected compilation failures may include:

```text
MissingFieldDefinition
IncompatibleFieldDefinition
MissingSchedulerBinding
IncompatibleSchedulerBinding
MissingExternalInput
InvalidResourceProducerOrder
DuplicateResourceProducer
UnsupportedExecutionProfile
InvalidArtifactCapturePolicy
InvalidScratchRequirement
```

These are examples, not final API names.

Invalid API usage should still throw exceptions.

Examples:

```text
null generation plan
null field definition set
null execution profile
null scheduler binding catalog
invalid compiler configuration
```

## Error ownership

Future runnable compilation errors are not request-resolution errors.

Boundary ownership:

| Failure                                               | Owner                                       |
| ----------------------------------------------------- | ------------------------------------------- |
| Missing recipe symbol                                 | `GenerationRequestResolver`                 |
| Invalid catalog graph                                 | `GenerationCatalog`                         |
| Invalid managed plan structure                        | `GenerationPlanCompiler` / `GenerationPlan` |
| Missing field definition for plan resource            | Future `RunnablePlanCompiler`               |
| Missing scheduler binding for selected implementation | Future `RunnablePlanCompiler`               |
| Native allocation failure                             | Future `GenerationWorkspace`                |
| Job scheduling failure                                | Future `OperationScheduler`                 |

Do not report execution metadata failure from current managed request resolution.

## Determinism rules

Runnable compilation must be deterministic.

Same accepted inputs must produce equivalent runnable plans.

Deterministic inputs include:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
SchedulerBindingCatalog
external input bindings
artifact capture policy
```

The compiler must not depend on:

```text
current time
global random state
managed object allocation order
dictionary enumeration order
hash set enumeration order
thread timing
native memory addresses
Unity object instance IDs
Unity asset import order
```

When output order is required, ordering must be explicit.

## Immutability rules

`RunnablePlan` should be immutable after compilation.

`RunnableStage` and `RunnableOperation` should expose read-only metadata.

Compiler-internal mutable state must not leak into accepted runnable plan objects.

Correct:

```text
compiler builds temporary mutable graph
compiler validates graph
compiler creates immutable RunnablePlan
```

Incorrect:

```text
RunnablePlan exposes mutable compiler lists
workspace mutates RunnableOperation metadata during allocation
scheduler rewrites field bindings inside the runnable plan
```

Execution state belongs to workspace and schedulers, not to the runnable plan.

## Catalog independence

Runnable compilation should not use the catalog for normal operation.

By the time a `GenerationPlan` exists, catalog resolution has already happened.

Correct:

```text
GenerationPlan -> RunnablePlanCompiler
```

Incorrect:

```text
GenerationPlan + GenerationCatalog -> RunnablePlanCompiler performs symbol lookup again
```

A future compiler may use catalog-like execution inventories only for execution metadata, such as scheduler binding catalogs or field definition sets.

It must not repeat request resolution.

## Request independence

Runnable compilation should not use `GenerationRequestDescriptor`.

Descriptors are symbolic input.

Runnable compilation consumes accepted managed plans.

Invalid flow:

```text
GenerationRequestDescriptor -> RunnablePlanCompiler
```

Correct flow:

```text
GenerationRequestDescriptor
  -> GenerationRequestResolver
  -> GenerationRequest
  -> GenerationPlanCompiler
  -> GenerationPlan
  -> RunnablePlanCompiler
```

## Unity boundary

Runnable compilation should remain independent from Unity scene and editor state.

Do not use:

```text
GameObject lookup
MonoBehaviour state
ScriptableObject instance identity
AssetDatabase
Resources.Load
current scene
current editor selection
```

Unity-facing adapters may create accepted field definitions, execution profiles, scheduler binding catalogs, or external input bindings.

The runnable compiler consumes accepted inputs.

## ECS boundary

Future ECS integration should be an adapter or execution layer.

Runnable compilation should not require an ECS `World`, `Entity`, or `SystemHandle`.

A future ECS execution adapter may consume `RunnablePlan` and workspace metadata.

The runnable plan itself should remain package execution metadata, not ECS world state.

## Burst boundary

Runnable compilation is managed metadata compilation.

It is not Burst job execution.

Burst-compatible jobs belong behind scheduler bindings and scheduler implementations.

The runnable compiler may validate that a selected scheduler binding supports Burst execution for a profile, but it must not run Burst code.

## Correct future flow

```text
GenerationPlan
  has semantic resources and selected implementations

FieldDefinitionSet
  defines storage-facing representations

ExecutionProfile
  selects representation and policy

SchedulerBindingCatalog
  maps implementation metadata to scheduler metadata

RunnablePlanCompiler
  validates and binds metadata

RunnablePlan
  is immutable executable metadata

GenerationWorkspace
  allocates native storage from runnable metadata

OperationScheduler
  schedules jobs using workspace storage
```

## Incorrect future flow

```text
RunnablePlanCompiler resolves recipe symbols.
RunnablePlanCompiler creates NativeArray<T>.
RunnablePlanCompiler schedules jobs.
RunnablePlan stores JobHandle.
GenerationPlan stores FieldHandle.
OperationContract references SchedulerBinding.
Job reads ResourceDefinition.
Workspace chooses recipe route.
Scheduler resolves descriptor overrides.
```

These flows violate ownership boundaries.

## Code placement guidance

Future runnable compilation code should live in explicit execution compilation areas.

Possible folders:

```text
Runtime/Execution/Compilation/
Runtime/Execution/RunnablePlans/
Runtime/Execution/Bindings/
```

Do not place runnable plan compilation inside:

```text
Runtime/Catalog/
Runtime/Recipes/
Runtime/Requests/
Runtime/Plans/
Runtime/Resources/
```

except for references required by accepted input types.

## Public API guidance

Use precise future names:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
SchedulerBinding
SchedulerBindingCatalog
FieldBinding
FieldAccess
RunnablePlanCompilationResult
RunnablePlanCompilationError
RunnablePlanCompilationErrorCode
```

Avoid vague names:

```text
ExecutionManager
PlanProcessor
RunContext
OperationData
BindingInfo
JobPlanThing
```

Use `Runnable` to distinguish execution-ready metadata from current managed semantic plans.

## Test guidance

Future runnable compilation tests should verify:

```text
valid plan compiles to runnable plan
stage and operation order is deterministic
resources bind to compatible fields
missing fields produce structured errors
incompatible fields produce structured errors
selected implementations bind to schedulers
missing scheduler bindings produce structured errors
scheduler/profile incompatibility produces structured errors
external inputs must be explicit
artifact capture metadata is deterministic
runnable plan exposes immutable metadata
compiler does not allocate native containers
compiler does not schedule jobs
jobs do not receive managed metadata
```

Tests should not require current managed planning objects to contain future execution state.

## Documentation guidance

Current architecture docs may mention runnable compilation only as future architecture.

Future runnable docs must clearly state that runnable concepts are not current implemented Runtime behavior.

Do not describe runnable plans as current `GenerationPlan`.

Do not describe scheduler bindings as current operation implementation definitions.

Do not describe field handles as current resource definitions.

## Review checklist

Before accepting runnable compilation architecture, verify:

```text
GenerationPlan remains managed semantic data.
RunnablePlanCompiler consumes accepted inputs only.
RunnablePlanCompiler does not resolve request descriptors.
RunnablePlanCompiler does not allocate native storage.
RunnablePlanCompiler does not schedule jobs.
ResourceDefinition binds to FieldDefinition through explicit metadata.
OperationImplementationDefinition binds to SchedulerBinding through explicit metadata.
Missing fields are structured runnable compilation errors.
Missing scheduler bindings are structured runnable compilation errors.
RunnablePlan is immutable executable metadata.
GenerationWorkspace owns native allocation.
OperationScheduler owns job scheduling.
Jobs receive native containers and unmanaged values only.
Output ordering is deterministic.
Unity scene/editor state is not used.
```

## Summary

Runnable plan compilation is the future bridge between managed semantic planning and executable metadata.

`GenerationPlan` says what semantic generation work should happen.

`FieldDefinitionSet` says how semantic resources can be represented.

`ExecutionProfile` selects execution policy.

`SchedulerBindingCatalog` says how selected implementations can execute.

`RunnablePlanCompiler` validates and binds these inputs.

`RunnablePlan` is immutable execution metadata.

The workspace allocates native storage later.

Schedulers schedule jobs later.

Jobs execute deterministic transforms over native data only.

```