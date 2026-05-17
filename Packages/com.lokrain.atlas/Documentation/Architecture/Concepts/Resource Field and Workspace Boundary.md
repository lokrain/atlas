# Resource, field, and workspace boundary

Lokrain.Atlas separates semantic generated values from future storage and execution ownership.

The boundary is:

```text
ResourceDefinition -> FieldDefinition -> GenerationWorkspace
     current             future               future
````

`ResourceDefinition` is implemented Runtime architecture.

`FieldDefinition` and `GenerationWorkspace` are planned execution architecture and are not implemented Runtime behavior unless corresponding code exists.

## Boundary summary

| Concept               | Status  | Owns                                                                  | Does not own                                                               |
| --------------------- | ------- | --------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `ResourceDefinition`  | Current | Semantic identity of a generated value.                               | Storage layout, native allocation, field handles, scheduler binding, jobs. |
| `FieldDefinition`     | Future  | Storage-facing metadata for representing a resource during execution. | Native allocation, job scheduling, generated data.                         |
| `GenerationWorkspace` | Future  | Native storage allocation, access, and disposal for one run.          | Semantic planning, catalog lookup, recipe selection.                       |

## ResourceDefinition

`ResourceDefinition` defines the semantic identity of a generated value.

A resource answers:

```text
What generated value is required or produced?
```

Examples from the landmass module:

```text
Height
Land
Ocean
Coast
ContinentalRegion
```

A resource definition belongs to a schema and is owned by a catalog.

A resource definition is managed metadata. It is not executable data.

## Resource identity

A resource definition has stable symbol identity.

The symbol identifies the semantic value across catalog lookup, contracts, plans, diagnostics, tests, and future artifact compatibility.

A resource symbol must not be replaced by:

```text id="5485vq"
display name
Unity object name
Unity asset path
native container handle
field handle
runtime numeric ID
scheduler binding ID
```

Display names may describe a resource for humans. They must not define resource identity.

## Resource ownership

A catalog owns the resource definitions it exposes.

Contracts and recipes in a catalog-owned graph must reference resource definitions owned by the same catalog.

Symbol equality is not enough to satisfy ownership. A resource definition from one catalog must not be reused inside another catalog-owned object graph.

Correct model:

```text id="rl4gl7"
catalog A
  resource A.Height
  operation contract A
    required input: resource A.Height
```

Invalid model:

```text id="m8o17d"
catalog A
  operation contract A
    required input: resource B.Height
```

Cross-catalog resource reuse is invalid even when symbols match.

## Resource contracts

Stage and operation contracts use `ResourceDefinition`.

Current contract model:

```text id="3u97oh"
StageContract
  RequiredInputs  -> IReadOnlyList<ResourceDefinition>
  ProducedOutputs -> IReadOnlyList<ResourceDefinition>

OperationContract
  RequiredInputs  -> IReadOnlyList<ResourceDefinition>
  ProducedOutputs -> IReadOnlyList<ResourceDefinition>
```

Contracts describe semantic data flow.

They do not describe storage, memory layout, field lifetime, scheduler binding, or job dependencies.

## Stage resource flow

A stage contract describes resources required and produced by a stage.

A stage contract answers:

```text id="90g2js"
What semantic resources must exist before this stage?
What semantic resources does this stage produce?
```

A stage contract does not answer:

```text id="hmtqdq"
Which operation writes the native container?
Which field lifetime is used?
Which scheduler executes the work?
Which jobs are scheduled?
```

## Operation resource flow

An operation contract describes resources required and produced by an operation.

An operation contract answers:

```text id="x86ndv"
What semantic resources must this operation read?
What semantic resources does this operation write?
```

An operation contract does not answer:

```text id="k37u58"
Which NativeArray<T> stores the data?
Which field handle addresses the data?
Which scheduler implementation executes the operation?
Which JobHandle represents dependencies?
```

## Resource flow in managed plans

`GenerationPlan` carries semantic resource flow through stage and operation contracts.

A plan can say:

```text id="51otri"
operation A requires Height
operation A produces Land
operation B requires Land
operation B produces Coast
```

A plan cannot say:

```text id="arveox"
Height is stored in NativeArray<float>
Land is stored in NativeArray<byte>
operation A uses Scheduler X
operation B depends on JobHandle Y
Coast has workspace field handle Z
```

Those are future execution concerns.

## FieldDefinition

`FieldDefinition` is planned architecture.

A field definition describes how a semantic resource is represented for execution.

A field definition may define:

```text id="i9ejlk"
resource binding
field lifetime
field shape
value kind
execution profile
capture behavior
storage requirements
```

A field definition still does not allocate memory.

It is metadata used by future runnable plan compilation and workspace allocation.

## Resource versus field

A resource is semantic.

A field is storage-facing metadata.

A workspace allocation is concrete native storage.

Example:

```text id="dsfq70"
ResourceDefinition
  Height

FieldDefinition
  Height as cell-grid float field

GenerationWorkspace
  NativeArray<float> allocated for Height field in this run
```

These are separate concepts because the same semantic resource may need different storage representation under different execution profiles.

## Why resources do not own storage metadata

`ResourceDefinition` must stay independent from storage metadata because resource identity is stable semantic architecture.

Storage can vary by:

```text id="vz491c"
execution profile
target platform
debug versus release mode
artifact capture requirements
memory budget
scheduler implementation
precision policy
ECS integration strategy
```

Changing a storage representation must not change the semantic identity of the generated value.

## Field lifetime

Field lifetime is planned execution metadata.

Expected future lifetime categories include:

```text id="6w94jr"
canonical field
stage-transient field
diagnostic field
payload field
external field
```

These categories are not current Runtime behavior.

They belong to future field definition and workspace execution design.

## Canonical field

A canonical field is planned architecture.

It represents authoritative generated map truth for a semantic resource.

Example:

```text id="nrmqqg"
Height -> canonical height field
Land   -> canonical land mask field
Ocean  -> canonical ocean mask field
```

Canonical fields are future execution/storage concepts.

The current Runtime expresses these values only as semantic resources and contracts.

## Stage-transient field

A stage-transient field is planned architecture.

It represents workspace data shared across operations inside a bounded stage or stage group.

A stage-transient field is not a public semantic resource unless it is explicitly modeled as one.

It is not default artifact output.

## Diagnostic field

A diagnostic field is planned architecture.

It represents validation, debugging, visualization, or tooling data.

Diagnostic capture should be controlled by execution profile or artifact policy.

Diagnostic data must not become semantic generation truth unless promoted to a resource intentionally.

## Payload field

A payload field is planned architecture.

It represents consumer-facing derived data.

A payload field may be exported or consumed by downstream systems. It is not necessarily canonical generation truth.

## External field

An external field is planned architecture.

It represents caller-provided, importer-provided, or tooling-provided field data bound into a generation run.

External field binding must be explicit.

External data must not appear implicitly through global state, Unity scene lookup, asset paths, or scheduler-side discovery.

## Field shape

Field shape is planned architecture.

It describes the structural shape of a future field.

Possible examples:

```text id="ohgu43"
cell-grid field
scalar field
sparse field
region field
edge field
payload-specific field
```

A field shape is not a resource symbol and not native storage by itself.

## Value kind

Value kind is planned architecture.

It describes the stored value category of a future field.

Possible examples:

```text id="p30dk1"
float
int
byte
bool mask
region id
vector
structured value
```

A value kind is not a C# container allocation by itself.

The future workspace decides concrete native storage.

## Execution profile

Execution profile is planned architecture.

An execution profile may select storage representation, precision, capture behavior, scheduler binding, or implementation-specific execution options.

Examples:

```text id="wq6o32"
authoring debug profile
deterministic production profile
low-memory profile
high-detail profile
artifact-capture profile
```

Execution profiles must not change resource identity.

They may change how a resource is represented or captured during execution.

## FieldDefinitionSet

`FieldDefinitionSet` is planned architecture.

It is the accepted collection of field definitions available to future runnable plan compilation.

It should validate:

```text id="twggz3"
unique field identities
resource binding consistency
profile compatibility
shape and value-kind consistency
capture policy consistency
```

It must not allocate native memory or schedule jobs.

## Runnable plan compilation boundary

Future runnable plan compilation maps managed semantic plans to executable metadata.

Conceptual future flow:

```text id="m9wm9j"
GenerationPlan
        +
FieldDefinitionSet
        +
Scheduler bindings
        |
        v
RunnablePlanCompiler
        |
        v
RunnablePlan
```

The runnable compiler may bind:

```text id="qi3pyi"
ResourceDefinition -> FieldDefinition
OperationImplementationDefinition -> SchedulerBinding
OperationPlanNode -> RunnableOperation
```

It must not allocate workspace storage or schedule jobs.

## Workspace boundary

`GenerationWorkspace` is planned architecture.

The workspace owns native storage for one generation run.

The workspace may allocate and expose:

```text id="90t1cu"
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
NativeReference<T>
field handles
scratch allocations
```

The workspace owns disposal.

The workspace must not own semantic planning.

## Resource versus workspace

A resource is stable semantic identity.

A workspace allocation is per-run native storage.

Example boundary:

```text id="dgse20"
ResourceDefinition.Height
  -> semantic generated value

FieldDefinition.HeightFloatGrid
  -> future storage-facing metadata

GenerationWorkspace allocation
  -> NativeArray<float> for this run
```

The resource can exist without a workspace.

The workspace allocation cannot be interpreted correctly without accepted execution metadata.

## Scheduler boundary

An operation scheduler is planned architecture.

A scheduler owns execution control flow for one runnable operation.

A scheduler may:

```text id="d2b54c"
request workspace field access
allocate operation scratch
schedule jobs
combine JobHandle dependencies
apply iteration or termination policy
report execution failure
```

A scheduler must not:

```text id="rkk8lm"
resolve symbols
inspect recipes
modify catalogs
reinterpret request descriptors
decide semantic resource identity
own resource definitions
```

## Job boundary

Jobs are planned execution architecture.

A job receives native containers and unmanaged values.

A job must not know:

```text id="oavf6a"
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
```

A job should only execute a deterministic transform over already-resolved data.

## Artifact capture boundary

Artifact capture is future architecture.

Resources define semantic values.

Field definitions may define capture eligibility.

Execution profiles may define capture policy.

Workspace storage provides the data.

Schedulers and artifact systems coordinate when data can be captured.

Do not put artifact capture policy into `ResourceDefinition` unless the policy is semantic and invariant across all execution profiles.

## Correct current model

Current managed architecture should model resource flow like this:

```text id="mtu79w"
ResourceDefinition
  -> StageContract
  -> OperationContract
  -> GenerationRecipeDefinition
  -> GenerationRequest
  -> GenerationPlan
```

This is sufficient for semantic planning.

## Incorrect current model

Do not add these dependencies to current managed planning objects:

```text id="4vx9xq"
ResourceDefinition -> FieldDefinition
ResourceDefinition -> NativeArray<T>
StageContract -> FieldHandle
OperationContract -> SchedulerBinding
GenerationPlan -> GenerationWorkspace
OperationPlanNode -> JobHandle
OperationImplementationDefinition -> Burst job struct
GenerationCatalog -> native storage allocation
GenerationRecipeDefinition -> execution profile storage policy
```

These dependencies collapse planning and execution boundaries.

## Code placement

Current resource-definition code belongs in Runtime managed architecture.

Expected current areas:

```text id="7sois8"
Runtime/Resources/
Runtime/Stages/
Runtime/Operations/
Runtime/Catalog/
Runtime/Recipes/
Runtime/Requests/
Runtime/Plans/
Runtime/Generation/
```

Future field/workspace/scheduler/job code should be placed behind explicit execution-oriented boundaries when implemented.

It should not be introduced into catalog, recipe, request, or plan types as hidden state.

## Naming boundary

Use `ResourceDefinition` for semantic generated values.

Use `FieldDefinition` only for future storage-facing metadata.

Use `GenerationWorkspace` only for future per-run native storage ownership.

Use `OperationScheduler` only for future execution control flow.

Do not use these names interchangeably:

```text id="6ifhxk"
resource
field
workspace
scheduler
job
artifact
payload
```

Each name represents a different architecture boundary.

## Validation boundary

Validation is split by ownership.

| Validation                                | Owner                                                              |
| ----------------------------------------- | ------------------------------------------------------------------ |
| Resource symbol and display name validity | `ResourceDefinition` constructor/factory                           |
| Resource schema reference validity        | `ResourceDefinition` local invariants and catalog graph validation |
| Resource uniqueness                       | `GenerationCatalog`                                                |
| Contract resource null/duplicate rules    | `StageContract` / `OperationContract`                              |
| Contract resource catalog ownership       | `GenerationCatalog`                                                |
| Resource-to-field binding                 | Future runnable plan compiler                                      |
| Field storage metadata validity           | Future field definition model                                      |
| Native allocation validity                | Future workspace                                                   |
| Job dependency validity                   | Future scheduler                                                   |

Do not make a low-level object validate a boundary it does not own.

## Determinism boundary

Resource identity participates in deterministic planning through stable symbols and accepted object ordering.

Future field allocation must not introduce nondeterminism into semantic generation.

Do not base resource or field identity on:

```text id="35eyk7"
managed object allocation order
dictionary enumeration order
Unity object instance ID
Unity asset path
display name
native memory address
scheduler execution order
thread timing
```

When ordering is required, it must be explicit in accepted metadata or deterministic compiler output.

## Design checklist

Use this checklist when deciding where a concept belongs.

It belongs to `ResourceDefinition` when it answers:

```text id="gn6y4r"
What generated value is this?
Which schema owns the value?
What stable symbol identifies it?
What human-readable display name describes it?
```

It belongs to future `FieldDefinition` when it answers:

```text id="sq2sfy"
How should this resource be represented for execution?
What shape does the data have?
What value kind does it store?
What lifetime category does it use?
What profile-specific storage metadata applies?
```

It belongs to future `GenerationWorkspace` when it answers:

```text id="uw3kx3"
Which native container stores this field for this run?
Who allocates it?
Who disposes it?
How is storage accessed safely?
What temporary native memory exists for execution?
```

It belongs to a future scheduler when it answers:

```text id="bjp5ny"
Which jobs run?
In what dependency order?
What scratch memory is needed?
How are repeated job chains controlled?
How are execution failures reported?
```

It belongs to a future job when it answers:

```text id="wl9u4k"
How does this native input transform into this native output?
What unmanaged parameters does the transform need?
What deterministic per-element or per-region algorithm runs?
```

## Summary

`ResourceDefinition` is the current semantic contract boundary for generated values.

`FieldDefinition` is future storage-facing metadata.

`GenerationWorkspace` is future native storage ownership.

Schedulers are future execution control flow.

Jobs are future deterministic transforms over native data.

Current catalog, recipe, request, and plan code must stay on the semantic side of this boundary.

```
