# Field definition and execution profiles

This document describes planned architecture.

`FieldDefinition`, `FieldDefinitionSet`, and execution profiles are not current Runtime behavior unless corresponding Runtime code exists.

Current Runtime architecture ends at `GenerationPlan`.

## Purpose

Field definitions provide planned storage-facing metadata for generated resources.

Execution profiles provide planned policy for storage, capture, diagnostics, and implementation selection.

Together, they help convert a managed semantic `GenerationPlan` into executable metadata.

```text
GenerationPlan
  + FieldDefinitionSet
  + ExecutionProfile
    -> RunnablePlanCompiler
    -> RunnablePlan
```

## Current boundary

Current Runtime contains `ResourceDefinition`.

Current Runtime does not contain:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlan
FieldHandle
GenerationWorkspace
OperationScheduler
native storage allocation
job scheduling
```

Do not add field metadata to current contracts, resources, requests, or plans.

## ResourceDefinition versus FieldDefinition

`ResourceDefinition` is semantic.

`FieldDefinition` is planned storage-facing metadata.

| Concept | Status | Answers |
| --- | --- | --- |
| `ResourceDefinition` | Current | What generated value is this? |
| `FieldDefinition` | Planned | How is this value represented for execution? |

Correct current model:

```text
OperationContract
  RequiredInputs: ResourceDefinition
  ProducedOutputs: ResourceDefinition
```

Incorrect current model:

```text
OperationContract
  RequiredInputs: FieldDefinition
  ProducedOutputs: FieldHandle
```

## FieldDefinition

A planned `FieldDefinition` describes execution-facing representation for a resource.

A field definition may include:

```text
field symbol
resource definition
field shape
field value kind
storage role
default capture policy
external binding policy
diagnostic role
execution profile compatibility
```

A field definition must not allocate storage.

A field definition must not schedule jobs.

A field definition must not replace resource identity.

## Field identity

A field definition should have stable identity.

A field symbol identifies storage-facing metadata.

A resource symbol identifies semantic generated value identity.

These identities are related but not interchangeable.

Example planned relationship:

```text
ResourceDefinition
  Symbol: lokrain.atlas.landmass.resource.base_elevation

FieldDefinition
  Symbol: lokrain.atlas.landmass.field.base_elevation
  ResourceDefinition: BaseElevation
```

## Field shape

Field shape describes the structural layout category of a planned field.

Possible planned shapes include:

```text
cell grid
scalar
sparse set
indexed list
graph
region set
payload-specific shape
```

Dense raster terrain data should use row-major one-dimensional storage.

A shape describes layout requirements. It does not allocate the layout.

## Field value kind

Field value kind describes the stored value category.

Possible planned value kinds include:

```text
boolean mask
integer index
floating scalar
vector
range
classification
bitfield
payload-specific value
```

The value kind should be explicit enough for runnable compilation and workspace allocation.

It should not encode algorithm names or temporary implementation details.

## Storage role

A field may have a planned storage role.

Examples:

```text
canonical
temporary
external
diagnostic
scratch-derived
```

### Canonical field

A canonical field stores authoritative generated output for a semantic resource.

Example:

```text
BaseElevation canonical cell-grid field
```

### Temporary field

A temporary field stores intermediate execution data.

Temporary fields support operation chains but are not necessarily outputs.

### External field

An external field is provided by the caller, importer, editor tool, or another system.

External field binding must happen after semantic planning.

### Diagnostic field

A diagnostic field stores debug, validation, or tooling output.

Diagnostic capture depends on execution profile policy.

## FieldDefinitionSet

A planned `FieldDefinitionSet` is an accepted collection of field definitions.

It may validate:

```text
field symbol uniqueness
resource mapping consistency
required resource coverage
shape compatibility
value-kind compatibility
execution profile compatibility
external binding requirements
diagnostic capture declarations
```

A field definition set must not replace `GenerationCatalog`.

Catalogs validate semantic definition graphs.

Field definition sets validate storage-facing metadata for runnable compilation.

## ExecutionProfile

An execution profile is planned policy input.

An execution profile may define:

```text
storage representation choices
capture policy
diagnostic policy
temporary retention policy
implementation preference
scheduler policy
artifact policy
validation strictness
```

Execution profiles must not change semantic resource identity.

Correct:

```text
BaseElevation remains BaseElevation in debug and release profiles.
```

Incorrect:

```text
BaseElevation means one resource in release and another resource in debug.
```

## Profile examples

Possible planned profiles:

```text
Default
Debug
Preview
DeterministicValidation
ArtifactCapture
LowMemory
HighQuality
```

These names are examples only.

Profile names should describe policy, not implementation convenience.

## Capture policy

Capture policy defines which fields are preserved after execution.

Examples:

```text
capture canonical fields
capture selected diagnostics
discard temporary fields
capture artifacts for editor preview
```

Capture policy must not be stored on current `GenerationPlan`.

Capture policy belongs to planned execution metadata.

## Diagnostic policy

Diagnostic policy defines what validation and tooling data is generated.

Examples:

```text
capture continent candidate masks
capture region labels
capture scheduler timings
capture operation summaries
```

Diagnostic policy must not alter semantic output identity.

## External binding policy

External binding policy defines which fields can or must be provided by a caller.

Examples:

```text
required external climate input
optional external mask
editor-provided preview input
```

External native data must not be placed in current descriptors or requests.

Correct planned boundary:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> external binding validation
  -> GenerationWorkspace
```

Incorrect current boundary:

```text
GenerationRequestDescriptor contains NativeArray<T>.
ResourceDefinition stores external data pointer.
```

## Implementation selection policy

Execution profiles may influence planned implementation selection only through explicit supported rules.

They must not silently reinterpret recipes.

Correct planned model:

```text
Recipe selects semantic operation path.
Profile selects compatible execution implementation where allowed.
```

Incorrect:

```text
Profile changes selected recipe stages without a recipe or request-level decision.
```

## Runnable compilation

The planned `RunnablePlanCompiler` uses:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
```

It produces:

```text
RunnablePlan
```

During compilation, it may validate:

```text
every required resource has a field definition
operation input fields are available
operation output fields are writable
temporary fields have storage policy
external fields have binding policy
diagnostic fields follow profile policy
selected implementations support required field shapes
```

The runnable compiler must not mutate the managed plan.

The runnable compiler must not allocate workspace storage.

## Workspace allocation

Workspace allocation is planned after runnable compilation.

A field definition may describe allocation requirements.

The workspace owns actual native allocation.

Correct planned flow:

```text
FieldDefinition describes required representation.
RunnablePlan records allocation requirements.
GenerationWorkspace allocates native storage.
```

Incorrect:

```text
FieldDefinition owns NativeArray<T>.
RunnablePlan owns native memory lifetime.
GenerationPlan owns field storage.
```

## Data structure selection

Data structures are selected by data shape.

Dense raster data uses row-major one-dimensional storage.

Fixed-degree topology uses direct computation or fixed-stride arrays.

Variable one-to-many topology uses frozen compressed sparse layouts.

Mutable topology builders may use streams, unsafe lists, or native multi-hash maps during construction, but must freeze into deterministic arrays before becoming canonical.

Connectivity labeling uses union-find, deterministic flood-fill frontiers, or another explicitly ordered labeling algorithm.

Priority propagation uses deterministic heaps, bucket queues, radix-style queues, or another priority structure with explicit tie-breaking.

Spatial lookup uses chunk buckets before trees.

Canonical graph state uses arrays of nodes, edges, offsets, counts, and values.

Atlas does not use managed object graphs as canonical generation data.

## Determinism

Field definitions and execution profiles must preserve deterministic output.

They must not depend on:

```text
Unity object instance ID
asset import order
scene hierarchy order
current time
global random state
hash-map enumeration order
thread timing
managed object allocation order
```

Ordering that affects output must be explicit.

## Invalid placements

Do not add field definitions or profiles to current semantic objects.

Invalid current placements:

```text
ResourceDefinition.FieldDefinition
StageContract.FieldDefinition
OperationContract.FieldDefinition
GenerationRecipeDefinition.ExecutionProfile
GenerationRequestDescriptor.NativeFieldBinding
GenerationRequest.FieldHandle
GenerationPlan.FieldHandle
StagePlanNode.RunnableStage
OperationPlanNode.RunnableOperation
```

These cross the current Runtime boundary too early.

## Testing expectations

When implemented, tests should verify:

```text
FieldDefinition rejects null resource definitions.
FieldDefinition validates shape and value kind.
FieldDefinitionSet rejects duplicate field symbols.
FieldDefinitionSet rejects missing required resources.
FieldDefinitionSet rejects conflicting resource mappings.
ExecutionProfile validates policy names and options.
RunnablePlanCompiler rejects missing field definitions.
RunnablePlanCompiler preserves managed plan order.
RunnablePlanCompiler does not allocate native storage.
```

## Documentation rule

Documentation must clearly mark field definitions and profiles as planned until implemented.

Correct:

```text
FieldDefinition is planned storage-facing metadata.
```

Incorrect:

```text
FieldDefinition stores BaseElevation today.
```

## Checklist

Before implementing field or profile code, verify:

```text
ResourceDefinition remains semantic identity.
Contracts still use ResourceDefinition.
FieldDefinition does not allocate storage.
FieldDefinitionSet does not replace GenerationCatalog.
ExecutionProfile does not change semantic identity.
RunnablePlanCompiler owns binding validation.
GenerationWorkspace owns native allocation.
Current managed plans do not contain field handles.
Jobs do not receive field definitions.
```

## Summary

`ResourceDefinition` is current semantic metadata.

`FieldDefinition` is planned storage-facing metadata.

`FieldDefinitionSet` is planned accepted field metadata.

Execution profiles are planned policy inputs.

Runnable compilation is the planned bridge from managed semantic planning to executable metadata.