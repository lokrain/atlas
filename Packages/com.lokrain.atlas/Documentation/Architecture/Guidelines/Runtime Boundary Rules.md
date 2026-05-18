# Runtime boundary rules

This document defines the boundary between current managed Runtime architecture and planned execution architecture.

Current Runtime ends at `GenerationPlan`.

Execution architecture after `GenerationPlan` is planned unless corresponding Runtime code exists.

## Current Runtime scope

Current Runtime includes managed domain and planning objects:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed

GenerationSchemaDefinition
ResourceDefinition

StageKind
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract

OperationKind
OperationDefinition
OperationContract
OperationImplementationDefinition

GenerationCatalog
GenerationCatalogBuilder

GenerationRecipeDefinition
StageRouteChoice
StageRouteStepImplementationChoice

GenerationRunSettings
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
GenerationRequestResolver
GenerationRequestResolutionResult
GenerationRequestResolutionError
GenerationRequest

GenerationPlanCompiler
GenerationPlan
StagePlanNode
OperationPlanNode
```

Current Runtime does not allocate native storage, bind field handles, schedule jobs, execute Burst jobs, capture artifacts, or integrate with ECS execution.

## Planned execution scope

The following concepts are planned execution architecture:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldHandle
SchedulerBinding
GenerationWorkspace
OperationScheduler
OperationScratch
Burst job execution
artifact capture
execution diagnostics
ECS execution integration
```

Do not describe these concepts as implemented Runtime behavior until corresponding Runtime code exists.

## Boundary map

```text
GenerationRequestDescriptor
  -> GenerationRequestResolver
  -> GenerationRequest
  -> GenerationPlanCompiler
  -> GenerationPlan
  -> RunnablePlanCompiler       planned
  -> RunnablePlan               planned
  -> GenerationWorkspace        planned
  -> OperationScheduler         planned
  -> Burst jobs                 planned
```

`GenerationPlan` is the last current Runtime execution-preparation object.

`RunnablePlanCompiler` is the planned bridge into executable metadata.

## Managed Runtime rule

Current managed Runtime objects must remain semantic.

They may own:

```text
validated values
accepted definitions
semantic contracts
catalog inventory
symbolic descriptors
resolution results
accepted requests
managed plan nodes
explicit managed ordering
```

They must not own:

```text
native containers
field handles
scheduler bindings
job handles
Burst job structs
ECS entities
UnityEngine.Object references
UnityEditor references
workspace allocations
scratch allocations
artifact buffers
```

## Definition boundary

Definitions are reusable managed inventory.

Definitions must not contain run-specific execution state.

Correct:

```text
ResourceDefinition describes semantic resource identity.
StageDefinition describes reusable stage identity.
OperationDefinition describes reusable operation identity.
GenerationRecipeDefinition describes reusable generation template.
```

Incorrect:

```text
ResourceDefinition stores NativeArray<T>.
StageDefinition stores current Grid.
OperationDefinition stores JobHandle.
GenerationRecipeDefinition stores Seed.
```

## Resource and field boundary

`ResourceDefinition` is current Runtime semantic metadata.

`FieldDefinition` is planned storage-facing metadata.

A resource answers:

```text
What generated value is this?
```

A field definition will answer:

```text
How is this generated value represented for execution?
```

Current contracts use resources.

Future runnable compilation may bind resources to fields.

Correct current model:

```text
StageContract -> ResourceDefinition
OperationContract -> ResourceDefinition
```

Correct planned model:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> FieldDefinition
  -> FieldHandle
  -> GenerationWorkspace allocation
```

Incorrect current model:

```text
StageContract -> FieldDefinition
OperationContract -> NativeArray<T>
ResourceDefinition -> FieldHandle
GenerationPlan -> FieldHandle
```

## Contract boundary

Contracts describe semantic resource flow.

Contracts must not define storage, allocation, scheduling, or job data.

Correct:

```text
OperationContract
  requires ContinentSuitability
  produces ContinentCandidate
```

Incorrect:

```text
OperationContract
  reads NativeArray<float>
  writes FieldHandle
  schedules JobHandle
```

Storage and execution details belong after runnable compilation.

## Request boundary

Descriptors are unresolved symbolic intent.

Requests are accepted resolved intent.

Neither descriptors nor requests execute work.

Correct:

```text
GenerationRequestDescriptor
  recipe symbol
  run settings
  override descriptors
```

```text
GenerationRequest
  recipe definition
  run settings
  final implementation choices
```

Incorrect:

```text
GenerationRequestDescriptor contains OperationImplementationDefinition.
GenerationRequest contains unresolved implementation symbols.
GenerationRequest schedules jobs.
GenerationRequest allocates native fields.
```

## Plan boundary

`GenerationPlan` is managed semantic data.

It contains the selected recipe, schema, run settings, and ordered stage plan nodes.

It must not contain execution-owned state.

Correct:

```text
GenerationPlan
  GenerationRecipeDefinition
  GenerationSchemaDefinition
  GenerationRunSettings
  StagePlanNode list
```

Incorrect:

```text
GenerationPlan
  NativeArray<T>
  NativeList<T>
  FieldHandle
  JobHandle
  Entity
  SchedulerBinding
```

A generation plan is not a runnable plan.

## Runnable plan boundary

`RunnablePlan` is planned executable metadata.

A runnable plan may contain planned execution metadata such as:

```text
runnable stages
runnable operations
field bindings
scheduler bindings
execution profiles
workspace allocation requirements
```

A runnable plan must be compiled from a managed `GenerationPlan`.

Do not bypass the managed plan boundary.

Correct planned flow:

```text
GenerationPlan -> RunnablePlanCompiler -> RunnablePlan
```

Incorrect planned flow:

```text
GenerationRequestDescriptor -> RunnablePlan
GenerationCatalog -> RunnablePlan
GenerationRecipeDefinition -> native job graph
```

## Workspace boundary

`GenerationWorkspace` is planned native storage ownership.

A workspace may own:

```text
canonical field storage
temporary field storage
external field bindings
diagnostic field storage
scratch storage
native allocation lifetime
disposal
```

A workspace must not own:

```text
symbol resolution
catalog validation
recipe selection
request resolution
managed plan compilation
semantic resource identity
```

Correct planned responsibility:

```text
GenerationWorkspace allocates and owns native storage for one run.
```

Incorrect responsibility:

```text
GenerationWorkspace chooses a recipe.
GenerationWorkspace resolves symbols.
GenerationWorkspace validates catalog graph ownership.
```

## Scheduler boundary

`OperationScheduler` is planned execution control.

A scheduler may own:

```text
dependency wiring
job scheduling
workspace access
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

Do not put scheduler behavior into definitions, recipes, requests, or managed plans.

## Job boundary

Jobs are planned deterministic transforms over native data.

Jobs receive native containers and unmanaged values only.

Jobs must not reference:

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
GenerationWorkspace
OperationScheduler
UnityEngine.Object
UnityEditor
```

If a job needs data, the scheduler resolves that data before scheduling and passes it as native containers or unmanaged parameters.

Correct planned job input:

```text
NativeArray<float> elevations
NativeArray<byte> mask
int width
int depth
uint seed
```

Incorrect planned job input:

```text
GenerationPlan plan
ResourceDefinition resource
GenerationCatalog catalog
ScriptableObject settings
```

## Unity boundary

Managed Runtime domain and planning objects are not Unity object wrappers.

Runtime domain objects must not use Unity object identity as package-domain identity.

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

Unity-facing adapters may translate Unity-authored data into Atlas descriptors or accepted definitions.

Correct:

```text
ScriptableObject authoring asset -> GenerationRequestDescriptor
Editor window -> displays GenerationCatalog
Importer -> creates accepted definitions
```

Incorrect:

```text
ScriptableObject is the canonical recipe.
GameObject name is the Symbol.
MonoBehaviour owns GenerationCatalog identity.
```

## ECS boundary

ECS integration is planned execution integration.

Current managed Runtime planning objects must not depend on ECS worlds, systems, entities, components, bakers, or entity queries.

Correct planned boundary:

```text
Runnable execution output -> ECS integration adapter
```

Incorrect current boundary:

```text
GenerationPlan stores Entity.
GenerationRecipeDefinition depends on ISystem.
OperationContract references IComponentData.
```

ECS concepts may appear in future execution or integration layers, not in current semantic planning objects.

## Native container boundary

Current managed Runtime objects must not own native containers.

Do not add native containers to:

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

Native containers belong to planned execution ownership.

Correct planned owners:

```text
GenerationWorkspace
OperationScheduler
low-level execution infrastructure
```

Incorrect owners:

```text
GenerationRecipeDefinition
GenerationPlan
ResourceDefinition
OperationContract
```

## Unsafe code boundary

Unsafe code is future low-level infrastructure.

Unsafe code must not leak into current semantic Runtime layers.

Do not use unsafe code in:

```text
Runtime/Resources
Runtime/Stages
Runtime/Operations
Runtime/Catalog
Runtime/Recipes
Runtime/Planning
Runtime/Generation module definitions
```

Potential future unsafe infrastructure areas include:

```text
Runtime/Memory
Runtime/Execution
Runtime/Topology
Runtime/Diagnostics
Runtime/Artifacts
Runtime/Interop
Runtime/Hashing
```

Unsafe code requires explicit ownership, lifetime rules, deterministic ordering, and tests.

## Artifact boundary

Artifacts are planned captured outputs.

Current managed plans do not produce artifacts.

Artifact capture belongs after execution policy and workspace storage exist.

Correct planned flow:

```text
Runnable execution
  -> workspace data
  -> artifact capture policy
  -> artifact output
```

Incorrect current model:

```text
GenerationPlan produces artifact buffers.
GenerationRecipeDefinition stores artifact payloads.
ResourceDefinition owns artifact files.
```

## Diagnostics boundary

Diagnostics exist in two forms:

```text
managed validation diagnostics
planned execution diagnostics
```

Current managed validation diagnostics may use exceptions or result errors.

Planned execution diagnostics may capture field data, scheduler state, timings, or artifacts.

Do not mix execution diagnostics into current managed domain objects.

## Boundary checklist

Before adding Runtime code, verify:

```text
Does this belong before or after GenerationPlan?
Is this current managed semantic state or planned execution state?
Does this type allocate native storage?
Does this type schedule jobs?
Does this type hold a field handle?
Does this type depend on UnityEngine or UnityEditor?
Does this type depend on ECS?
Does this type use unsafe code?
Does this type contain run-specific state when it is a reusable definition?
Does this type contain unresolved symbols when it is an accepted request?
Does this type contain native execution state when it is a managed plan?
```

If the answer crosses the current Runtime boundary, move the responsibility into planned execution architecture.

## Summary

Current Runtime is managed semantic architecture.

It resolves symbolic intent, validates accepted objects, and compiles managed generation plans.

Future execution architecture compiles runnable metadata, owns native storage, schedules jobs, captures artifacts, and integrates with ECS.

Keep those boundaries separate.