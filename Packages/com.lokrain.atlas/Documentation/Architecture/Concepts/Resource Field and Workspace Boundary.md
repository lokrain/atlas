# Resource, field, and workspace boundary

Atlas separates semantic resources, managed field metadata, managed runnable field bindings, and future workspace storage.

Each layer owns a different decision:

```text
ResourceDefinition      -> what generated value means
FieldDefinition         -> how the value is represented as managed field metadata
ResourceFieldBinding    -> which resource field participates in one runnable plan
GenerationWorkspace     -> where executable field storage lives in future execution infrastructure
```

## ResourceDefinition

`ResourceDefinition` is current semantic resource identity.

It belongs to a `GenerationSchemaDefinition` and is used by `StageContract` and `OperationContract` to describe semantic resource flow.

A resource definition answers:

```text
Which generated value is this?
Which generation schema owns its meaning?
Which symbol identifies it?
Which display name describes it to users?
```

A resource definition does not answer:

```text
What field shape stores it?
What value kind stores it?
Where is storage allocated?
How long does storage live?
Which job writes it?
Which ECS component receives it?
Should it be captured as an artifact?
```

## FieldDefinition

`FieldDefinition` is current managed field representation metadata.

It maps one `ResourceDefinition` to:

```text
FieldShape
FieldValueKind
field Symbol
field DisplayName
```

A field definition does not allocate storage, own native memory, create handles, schedule jobs, bind ECS data, capture artifacts, or capture diagnostics.

`FieldDefinitionSet` owns lookup and deterministic canonical ordering for field definitions.

`GenerationCatalog` must not own field definitions.

## ResourceFieldBinding

`ResourceFieldBinding` is current managed runnable metadata.

It connects one semantic resource to one field definition inside one runnable plan.

It contains:

```text
FieldIndex
ResourceDefinition
FieldDefinition
FieldPlanRole
FieldCapturePolicy
```

`ResourceFieldBinding` uses reference-exact ownership between `ResourceDefinition` and `FieldDefinition.ResourceDefinition`. Symbol-equivalent resource instances are not accepted.

`FieldPlanRole` describes whether the field is required before plan execution, produced by plan execution, or both.

`FieldCapturePolicy` records whether the binding is marked for future capture. It does not perform capture.

A resource field binding does not contain:

```text
FieldLifetimeScope
FieldHandle
WorkspaceAllocation
NativeArray<T>
NativeList<T>
NativeHashMap<TKey,TValue>
JobHandle
Entity
scheduler dependencies
scratch allocation
artifact buffers
runtime diagnostics
```

## RunnablePlan

`RunnablePlan` is current managed executable metadata.

It is compiled from:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
```

It contains deterministic tables of:

```text
ResourceFieldBinding
RunnableStage
RunnableOperation
```

A runnable plan does not own native storage lifetime.

## GenerationWorkspace

`GenerationWorkspace` is future execution infrastructure.

It will own executable storage and storage lifetime. It is the correct layer for native containers, field handles, allocation state, and storage-retention decisions.

Workspace design must be derived from runnable metadata and execution requirements. It must not be guessed from semantic definitions alone.

## Lifetime and liveness

Storage lifetime is not a property of `ResourceFieldBinding`.

Storage lifetime requires execution analysis such as:

```text
first use
last use
stage and operation execution boundaries
scheduler dependencies
capture retention
scratch reuse
workspace allocation policy
```

These concepts belong to future workspace and scheduler infrastructure.

Atlas must not add `RunPersistent`, `StageTransient`, or `OperationTransient` style metadata to field bindings until liveness and allocation ownership are designed.

## Data structure selection

Atlas chooses data structures by data shape, not by implementation convenience.

Dense raster data uses row-major one-dimensional storage.

Fixed-degree topology uses direct computation or fixed-stride arrays.

Variable one-to-many topology uses frozen compressed sparse layouts.

Mutable topology builders may use streams, unsafe lists, or native multi-hash maps during construction, but must freeze into deterministic arrays before becoming canonical.

Connectivity labeling uses union-find, deterministic flood-fill frontiers, or another explicitly ordered labeling algorithm.

Priority propagation uses deterministic heaps, bucket queues, radix-style queues, or another priority structure with explicit tie-breaking.

Spatial lookup uses chunk buckets before trees. Trees are reserved for editor, rendering, LOD, sparse spatial acceleration, or cases where chunk buckets are proven insufficient.

Canonical graph state is represented as arrays of nodes, edges, offsets, counts, and values.

Atlas does not use managed object graphs as canonical generation data.
