# Managed field metadata and execution profiles

This article explains the current managed metadata added after managed plan compilation stabilized.

`FieldDefinition`, `FieldDefinitionSet`, `FieldShape`, `FieldValueKind`, `ExecutionProfile`, and `ExecutionProfileSet` are current Runtime metadata.

They are not execution runtime, storage ownership, scheduler infrastructure, Burst jobs, artifacts, diagnostics, or ECS integration.

## Responsibility summary

| Concept | Status | Responsibility |
| --- | --- | --- |
| `FieldValueKind` | Current | Names the managed scalar value kind described by a field. |
| `FieldShape` | Current | Names the logical addressing shape described by a field. |
| `FieldDefinition` | Current | Maps one semantic resource to managed field metadata. |
| `FieldDefinitionSet` | Current | Validates and indexes accepted field definitions. |
| `ExecutionProfile` | Current | Identifies a reusable execution-policy variant. |
| `ExecutionProfileSet` | Current | Validates and indexes accepted execution profiles. |
| `RunnablePlanCompiler` | Future | Binds plans, fields, profiles, and implementation metadata into executable metadata. |
| `GenerationWorkspace` | Future | Owns native storage allocation and lifetime. |
| `OperationScheduler` | Future | Owns execution control flow and job scheduling. |

## Field definitions

A `FieldDefinition` maps a semantic `ResourceDefinition` to managed representation metadata:

```text
ResourceDefinition
Field symbol
Display name
FieldShape
FieldValueKind
```

A field definition describes the kind of field required for a resource. It does not allocate storage, own native memory, bind a field handle, schedule work, capture artifacts, or define executable operation data.

`FieldDefinition` validates reference invariants only. It does not reject enum defaults because it is a structural metadata object. The accepted set validates whether field shapes and value kinds are usable in a field metadata set.

## Field definition sets

`FieldDefinitionSet` is the acceptance boundary for managed field metadata.

It validates:

```text
no null field definitions
unique field symbols
unique represented resource definition symbols
no Unknown field shapes
no unsupported field shapes
no Unknown field value kinds
no unsupported field value kinds
```

A field definition set exposes field definitions in deterministic ordinal field-symbol order.

Private dictionaries are lookup indexes only. Dictionary and hash-set enumeration must not define public order, diagnostic order, serialized order, or generation order.

## Execution profiles

`ExecutionProfile` identifies a reusable execution-policy variant by symbol and display name.

An execution profile is identity metadata. It does not store policy switches yet and does not select jobs, allocate storage, or bind ECS data by itself.

Concrete policy behavior belongs to future runnable compilation and execution infrastructure.

## Execution profile sets

`ExecutionProfileSet` is the acceptance boundary for managed execution profile identity metadata.

It validates:

```text
no null execution profiles
unique execution profile symbols
```

An execution profile set exposes profiles in deterministic ordinal profile-symbol order.

Private dictionaries are lookup indexes only.

## Catalog boundary

`GenerationCatalog` owns semantic generation inventory.

It does not own `FieldDefinition`, `FieldDefinitionSet`, `ExecutionProfile`, or `ExecutionProfileSet`.

Field metadata and execution profile metadata are passed to future runnable compilation beside the managed plan.

Correct future bridge shape:

```text
GenerationPlan
FieldDefinitionSet
ExecutionProfile
  -> RunnablePlanCompiler
  -> RunnablePlan
```

Incorrect model:

```text
GenerationCatalog owns FieldDefinition
GenerationCatalog owns ExecutionProfile
GenerationCatalogBuilder adds execution profiles
```

## Built-in metadata

Built-in landmass field definitions live in the landmass generation module because they map landmass resources to landmass field metadata.

Built-in execution profiles live under `Runtime/Execution` because execution profile identity is package-level execution metadata, not landmass content.

Do not create landmass-specific execution profiles unless landmass owns a real execution-policy invariant that cannot be expressed through package-level profiles.
