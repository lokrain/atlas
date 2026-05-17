# Glossary

Package: `com.lokrain.atlas`  
Namespace root: `Lokrain.Atlas`

This glossary defines canonical Lokrain.Atlas architecture terminology.

Terms marked **Future** describe planned architecture and are not implemented Runtime behavior unless the corresponding code exists.

## Architecture object terms

### Accepted object

An object whose constructor or factory has validated its invariants.

Accepted objects can be used directly by downstream systems without repeating structural validation. They may still be rejected by a higher-level owner when object ownership, graph consistency, or catalog membership is invalid.

Examples:

```text
Symbol
DisplayName
Grid
Seed
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
OperationDefinition
GenerationCatalog
GenerationRecipeDefinition
GenerationRequest
GenerationPlan
````

### Descriptor

A valid symbolic input object that describes intent before catalog resolution.

A descriptor validates its own structure, but its symbols may be unsatisfied by a specific catalog.

Examples:

```text
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
```

### Result object

An object that represents the outcome of a boundary where failure is expected.

Result objects are used when callers should inspect structured errors instead of handling exceptions for normal negative outcomes.

Example:

```text
GenerationRequestResolutionResult
```

### Definition

Accepted package inventory that describes a reusable domain concept.

Definitions are catalog-owned metadata. They do not represent one generation run and do not own native storage or execution state.

Examples:

```text
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
StageRouteDefinition
OperationDefinition
OperationImplementationDefinition
GenerationRecipeDefinition
```

## Core terms

### Symbol

A stable machine-facing token.

A symbol is identity text for lookup, catalog membership, descriptor resolution, and artifact compatibility.

A symbol is not a display name, runtime numeric ID, native handle, scheduler binding, or storage address.

### DisplayName

Validated user-facing text used for editor UI, diagnostics, reports, and documentation.

A display name is metadata. It is not identity, not a lookup key, not a symbol, and not part of deterministic generation semantics.

### Grid

The horizontal map grid for one generation run.

A grid has:

```text
Width
Depth
CellCount
LastIndexValue
```

`Width` is the horizontal X dimension. `Depth` is the horizontal Z dimension.

Atlas does not use `Height` for horizontal map dimensions. Height means elevation.

### Cell

A map cell coordinate with `X` and `Z` components.

A cell represents a coordinate inside a grid only after it has been validated by that grid.

### CellIndex

A row-major flattened map cell index.

A cell index represents an index inside a grid only after it has been validated by that grid.

### Seed

The deterministic root seed for one generation run.

A zero seed is valid. Seed values are run settings, not request identity and not display metadata.

## Schema terms

### GenerationSchemaDefinition

A catalog-owned definition for a generation family.

A schema provides semantic context for resources, stages, operations, recipes, and generation modules.

Current built-in schema:

```text
BuiltInGenerationSchemas.World
```

## Resource terms

### ResourceDefinition

A catalog-owned semantic definition of a generated value.

Stage and operation contracts use resource definitions to declare required inputs and produced outputs.

A resource definition identifies what value is required or produced. It is not a field definition, not native storage, not a job dependency, and not an execution handle.

### Resource contract

A contract over resource definitions.

Resource contracts are managed planning metadata. They describe semantic data flow between stages and operations.

Resource contracts do not allocate storage and do not define native memory layout.

## Stage terms

### StageKind

A semantic category of generation stage.

Example:

```text
lokrain.atlas.landmass.stage_kind.continental_landmass
```

### StageDefinition

A catalog-owned definition of a generation stage.

A stage definition belongs to a schema and has a stage kind, symbol, and display name.

A stage is a semantic generation phase. It is not a route, scheduler, job, or native execution unit.

### StageRouteDefinition

A catalog-owned ordered route for satisfying a stage.

A route owns an ordered list of route-step definitions.

### StageRouteStepDefinition

A catalog-owned operation occurrence inside a stage route.

A route step has its own stable symbol so the same operation definition can appear multiple times without losing per-occurrence identity.

### StageContract

A resource-definition-based input/output contract for a stage.

A stage contract is planning metadata. It is not a field definition, storage definition, scheduler binding, or executable step.

## Operation terms

### OperationKind

A semantic category of operation.

Example:

```text
lokrain.atlas.landmass.operation_kind.main_continent_extraction
```

### OperationDefinition

A catalog-owned definition of an operation.

An operation definition belongs to a schema and an operation kind.

An operation is semantic generation work. It is not an implementation, scheduler, job, delegate, or Burst function pointer.

### OperationImplementationDefinition

A catalog-owned selectable implementation option for an operation definition.

An implementation definition identifies an implementation choice. It is metadata, not executable code.

### OperationContract

A resource-definition-based input/output contract for an operation.

An operation contract is planning metadata. It is not a field definition, storage definition, scheduler binding, or executable job description.

## Catalog terms

### GenerationCatalog

An immutable accepted inventory of schemas, resources, stages, routes, route steps, operations, implementations, recipes, and contracts.

The catalog validates definition ownership and graph consistency.

The catalog provides lookup and discovery. It does not resolve request descriptors, compile plans, allocate native storage, or execute jobs.

### GenerationCatalogBuilder

A mutable assembly surface used to build an accepted `GenerationCatalog`.

The builder collects candidate definitions and produces a catalog only after catalog-level validation succeeds.

### Catalog ownership

The rule that accepted definitions exposed by a catalog belong to that catalog.

Objects inside catalog-owned graphs must reference definitions owned by the same catalog. Cross-catalog object reuse is invalid.

## Recipe terms

### StageRouteChoice

An accepted recipe choice binding a stage to the route selected for that stage.

A stage route choice contains:

```text
StageDefinition
StageRouteDefinition
StageContract
```

### StageRouteStepImplementationChoice

An accepted choice binding a route-step occurrence to the selected implementation for that occurrence.

A route-step implementation choice contains:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

### GenerationRecipeDefinition

A reusable accepted generation template.

A recipe has a symbol, display name, schema, selected stage routes, and default route-step implementation choices.

A recipe does not contain grid, seed, per-run settings values, native storage, scheduler bindings, or job state.

## Request terms

### GenerationRunSettings

Generation-wide settings for one run.

Current settings contain:

```text
Grid
Seed
```

Run settings are per-run invocation data. They are not global process state and not ordinary recipe metadata.

### OperationImplementationOverrideDescriptor

A symbolic descriptor that overrides the selected implementation for one recipe route step.

It contains:

```text
StageRouteStepDefinitionSymbol
OperationImplementationDefinitionSymbol
```

### GenerationRequestDescriptor

A valid symbolic descriptor for generation intent.

It contains:

```text
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

A request descriptor is not an accepted request. It must be resolved through a catalog.

### GenerationRequestResolver

The boundary that converts symbolic generation intent into accepted generation intent.

The resolver combines:

```text
GenerationCatalog
GenerationRequestDescriptor
```

and produces:

```text
GenerationRequestResolutionResult
```

### GenerationRequestResolutionResult

A result object containing either an accepted `GenerationRequest` or one or more `GenerationRequestResolutionError` values.

### GenerationRequestResolutionError

A structured error describing why a request descriptor cannot be satisfied by a catalog.

### GenerationRequest

Accepted resolved generation intent for one run.

A request contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
final StageRouteStepImplementationChoice list
```

A request contains accepted definitions and final implementation choices. It does not contain unresolved symbols, native containers, jobs, scheduler bindings, or execution metadata.

## Plan terms

### GenerationPlanCompiler

A managed compiler that transforms an accepted request into a managed generation plan.

Conceptual flow:

```text
GenerationRequest -> GenerationPlan
```

The plan compiler does not resolve raw symbols and does not use a catalog for normal compilation.

### GenerationPlan

An accepted managed semantic plan for one generation run.

A plan contains run settings, the selected recipe, and ordered stage plan nodes.

A generation plan does not contain native storage, field handles, scheduler bindings, job handles, dependency handles, or executable job data.

### StagePlanNode

A compiler-created stage node inside a `GenerationPlan`.

A stage plan node represents one selected stage and its ordered operation plan nodes.

### OperationPlanNode

A compiler-created operation node inside a `StagePlanNode`.

An operation plan node represents one selected route-step operation and its selected implementation metadata.

## Generation module terms

### Generation module

A package area that owns built-in definitions, contracts, recipes, and descriptor factories for a domain.

Current module:

```text
Lokrain.Atlas.Generation.Landmass
```

A generation module may expose convenience factories, but it must not bypass catalog, resolver, accepted request, or plan compiler boundaries.

### Landmass

The current built-in generation module for continental landmass planning definitions.

Landmass currently defines schema-connected resources, stages, operations, routes, recipes, and request helpers for the primary continental landmass flow.

### LandmassResourceDefinitions

The landmass module surface that exposes built-in landmass resource definitions.

## Future storage terms

### FieldDefinition

**Future.**

A planned storage-facing definition for a resource.

A field definition describes how a semantic resource is represented for execution. It may define lifetime, shape, value kind, capture behavior, or execution profile metadata.

A field definition is not native storage and does not allocate memory.

### FieldDefinitionSet

**Future.**

A planned accepted collection of field definitions used by runnable plan compilation.

### Execution profile

**Future.**

A planned named configuration for selecting storage representation, capture behavior, scheduler binding, or implementation-specific execution policy.

Execution profiles are not part of the current managed plan model.

### Field shape

**Future.**

The planned spatial or structural shape of a field.

Examples may include cell-grid fields, scalar fields, sparse fields, or payload-specific shapes.

### Value kind

**Future.**

The planned value representation category for a field.

Examples may include integer, floating-point, byte, mask, vector, or structured value categories.

### Native storage

**Future.**

Allocated native data for one generation run.

Examples may include:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
```

Native storage is owned by the execution workspace, not by catalog, recipe, request, or managed plan objects.

### Field handle

**Future.**

A planned execution-time handle used to address workspace-owned field storage.

A field handle is not a symbol and not a resource definition.

## Future runnable execution terms

### RunnablePlanCompiler

**Future.**

A planned compiler that transforms a managed `GenerationPlan` into executable metadata.

A runnable plan compiler resolves semantic resources to field definitions and selected implementations to scheduler bindings.

### RunnablePlan

**Future.**

A planned execution-ready representation of one generation plan.

A runnable plan is metadata for execution. It is not the native workspace and not a running job graph.

### RunnableStage

**Future.**

A planned execution-ready stage entry inside a runnable plan.

### RunnableOperation

**Future.**

A planned execution-ready operation entry inside a runnable stage.

A runnable operation binds a planned operation to resource-field bindings, scheduler binding, and execution metadata.

### Scheduler binding

**Future.**

A planned binding from an operation implementation choice to the scheduler that can execute it.

Scheduler bindings are execution metadata. They are not operation definitions and not jobs.

### GenerationWorkspace

**Future.**

The planned native storage owner for one generation run.

A workspace owns allocation, access, and disposal of generated fields and execution-owned temporary storage.

### OperationScheduler

**Future.**

The planned execution controller for one runnable operation.

An operation scheduler owns execution control flow for its operation, including dependency wiring, scratch allocation, job scheduling, repeated chains, termination policy, and failure policy.

A scheduler is not a job.

### Job

**Future.**

A deterministic Burst-compatible transform over already-resolved native data.

Jobs receive native containers and unmanaged values only.

Jobs do not inspect symbols, catalogs, schemas, recipes, requests, plans, resources, field definitions, workspaces, or schedulers.

### Operation scratch

**Future.**

Private scheduler-owned native temporary storage.

Operation scratch is not a field, not a resource, not symbol-addressable, and not part of default artifact capture.

## Future field lifetime terms

### Canonical field

**Future.**

A durable generated field that represents authoritative generated map truth for a resource.

### Stage-transient field

**Future.**

A workspace field shared across operations within a stage or bounded stage group.

Stage-transient fields are not default artifact output.

### Diagnostic field

**Future.**

A validation, debug, or tooling field.

Diagnostic field capture is profile-dependent.

### Payload field

**Future.**

A derived consumer-facing field.

Payload fields are not canonical generation truth.

### External field

**Future.**

Caller-provided, importer-provided, or tooling-provided field data bound into a generation run.

External fields are planned to be bound explicitly through future descriptors or execution input contracts.

## Unity and package terms

### Runtime

Player-safe package code under `Runtime/`.

Current managed domain and planning Runtime code does not reference `UnityEngine` or `UnityEditor`.

### Editor

Unity Editor tooling under `Editor/`.

Editor code may use `UnityEditor`, inspectors, windows, importers, validation tools, and authoring workflows.

### Authoring adapter

A Unity-facing asset, editor object, importer, or tool that translates user-authored data into descriptors or catalog definitions.

ScriptableObjects can be authoring adapters. They are not canonical runtime state.

### Assembly boundary

A package compilation boundary defined by Unity assembly definition files.

Runtime assembly boundaries control which package areas can reference Unity APIs, editor APIs, Burst, Jobs, Collections, ECS/DOTS, tests, samples, and generation modules.

```