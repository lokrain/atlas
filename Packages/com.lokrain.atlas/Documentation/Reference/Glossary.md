Use this as:

```text
Documentation/Architecture/Reference/Glossary.md
```

Move the current root-level `Documentation/Architecture/Glossary.md` content into this target path, then remove the old root glossary file.

```markdown
# Glossary

This glossary defines Lokrain.Atlas architecture terms.

Terms marked **Future** describe planned architecture. They are not implemented Runtime behavior unless the corresponding Runtime code exists.

## Current terms

### Accepted object

An object that has validated its own construction invariants.

Accepted objects may still be rejected by a higher-level owner when ownership, graph consistency, or catalog membership is invalid.

### Assembly boundary

A Unity assembly definition boundary.

Assembly boundaries control which package areas can reference Runtime code, Editor code, tests, samples, Unity APIs, Burst, Jobs, Collections, ECS/DOTS, and generation modules.

### Authoring adapter

A Unity-facing asset, editor object, importer, or tool that translates authored data into descriptors or accepted definitions.

Authoring adapters are not canonical Runtime state.

### Catalog ownership

The rule that accepted definitions exposed by a `GenerationCatalog` belong to that catalog.

Catalog-owned graphs must reference definitions owned by the same catalog.

### Cell

A map coordinate with `X` and `Z` components.

A cell is inside a grid only after it has been validated by that grid.

### CellIndex

A row-major flattened map cell index.

A cell index is inside a grid only after it has been validated by that grid.

### Definition

Accepted package inventory that describes a reusable domain concept.

Definitions are catalog-owned metadata. They do not represent one generation run.

### Descriptor

A valid symbolic input object that describes intent before catalog resolution.

A descriptor validates its own structure. Its symbols may still be unresolved for a specific catalog.

### DisplayName

Validated user-facing text used for editor UI, diagnostics, reports, and documentation.

A display name is not identity and is not used for lookup.

### Editor

Unity Editor tooling under `Editor/`.

Editor code may use `UnityEditor`, inspectors, windows, importers, validation tools, and authoring workflows.

### GenerationCatalog

An immutable accepted inventory of schemas, resources, stages, routes, route steps, operations, implementations, recipes, and contracts.

The catalog provides lookup and discovery for accepted definitions.

### GenerationCatalogBuilder

A mutable assembly surface used to build a `GenerationCatalog`.

The builder collects candidate definitions and produces an accepted catalog after catalog-level validation succeeds.

### Generation module

A package area that owns built-in definitions, contracts, recipes, and descriptor factories for a generation domain.

The current generation module is `Lokrain.Atlas.Generation.Landmass`.

### GenerationPlan

An accepted managed semantic plan for one generation run.

A generation plan contains run settings, the selected recipe, and ordered stage plan nodes.

### GenerationPlanCompiler

A managed compiler that transforms an accepted `GenerationRequest` into a `GenerationPlan`.

### GenerationRecipeDefinition

A reusable accepted generation template.

A recipe has a symbol, display name, schema, selected stage routes, and default route-step implementation choices.

### GenerationRequest

Accepted resolved generation intent for one run.

A request contains accepted definitions, run settings, and final implementation choices.

### GenerationRequestDescriptor

A symbolic descriptor for generation intent.

A request descriptor contains a recipe symbol, run settings, and optional symbolic implementation overrides.

### GenerationRequestResolutionError

A structured error describing why a request descriptor cannot be satisfied by a catalog.

### GenerationRequestResolutionResult

A result object containing either an accepted `GenerationRequest` or one or more `GenerationRequestResolutionError` values.

### GenerationRequestResolver

The boundary that converts symbolic generation intent into accepted generation intent.

The resolver uses a `GenerationCatalog` and a `GenerationRequestDescriptor`.

### GenerationRunSettings

Generation-wide settings for one run.

Current run settings contain a `Grid` and a `Seed`.

### GenerationSchemaDefinition

A catalog-owned definition for a generation family.

A schema provides semantic context for resources, stages, operations, recipes, and generation modules.

### Grid

The horizontal map grid for one generation run.

A grid has `Width`, `Depth`, `CellCount`, and `LastIndexValue`.

`Width` is the horizontal X dimension. `Depth` is the horizontal Z dimension.

### Landmass

The current built-in generation module for continental landmass planning definitions.

### LandmassResourceDefinitions

The landmass module surface that exposes built-in landmass resource definitions.

### OperationContract

A resource-definition-based input/output contract for an operation.

An operation contract is managed planning metadata.

### OperationDefinition

A catalog-owned definition of semantic generation work.

An operation definition belongs to a schema and an operation kind.

### OperationImplementationDefinition

A catalog-owned selectable implementation option for an operation definition.

An implementation definition identifies an implementation choice.

### OperationImplementationOverrideDescriptor

A symbolic descriptor that overrides the selected implementation for one recipe route step.

### OperationKind

A semantic category of operation.

### OperationPlanNode

A compiler-created operation node inside a `StagePlanNode`.

An operation plan node represents one selected route-step operation and its selected implementation metadata.

### Resource contract

A managed planning contract over resource definitions.

Resource contracts describe semantic data flow between stages and operations.

### ResourceDefinition

A catalog-owned semantic definition of a generated value.

Stage and operation contracts use resource definitions to declare required inputs and produced outputs.

### Result object

An object that represents the outcome of a boundary where failure is expected.

Result objects expose structured errors for normal negative outcomes.

### Runtime

Player-safe package code under `Runtime/`.

Current managed domain and planning Runtime code does not reference `UnityEngine` or `UnityEditor`.

### Seed

The deterministic root seed for one generation run.

A zero seed is valid.

### StageContract

A resource-definition-based input/output contract for a stage.

A stage contract is managed planning metadata.

### StageDefinition

A catalog-owned definition of a generation stage.

A stage definition belongs to a schema and has a stage kind, symbol, and display name.

### StageKind

A semantic category of generation stage.

### StagePlanNode

A compiler-created stage node inside a `GenerationPlan`.

A stage plan node represents one selected stage and its ordered operation plan nodes.

### StageRouteChoice

An accepted recipe choice binding a stage to the route selected for that stage.

### StageRouteDefinition

A catalog-owned ordered route for satisfying a stage.

A route owns an ordered list of route-step definitions.

### StageRouteStepDefinition

A catalog-owned operation occurrence inside a stage route.

A route step has its own stable symbol so the same operation definition can appear multiple times with distinct per-occurrence identity.

### StageRouteStepImplementationChoice

An accepted choice binding a route-step occurrence to the selected implementation for that occurrence.

### Symbol

A stable machine-facing token.

A symbol is identity text for lookup, catalog membership, descriptor resolution, and artifact compatibility.

## Future terms

### Artifact

**Future.**

A captured generation output intended for tooling, diagnostics, persistence, preview, export, or consumer use.

### Canonical field

**Future.**

A durable generated field that represents authoritative generated map truth for a resource.

### Diagnostic field

**Future.**

A validation, debug, or tooling field.

Diagnostic field capture depends on execution profile policy.

### Execution profile

**Future.**

A named configuration for selecting storage representation, capture behavior, scheduler binding, or implementation-specific execution policy.

### External field

**Future.**

Caller-provided, importer-provided, or tooling-provided field data bound into a generation run.

### FieldDefinition

**Future.**

A storage-facing definition for a resource.

A field definition describes how a semantic resource is represented for execution.

### FieldDefinitionSet

**Future.**

An accepted collection of field definitions used by runnable plan compilation.

### Field handle

**Future.**

An execution-time handle used to address workspace-owned field storage.

### Field shape

**Future.**

The spatial or structural shape of a field.

Examples include cell-grid fields, scalar fields, sparse fields, and payload-specific shapes.

### GenerationWorkspace

**Future.**

The native storage owner for one generation run.

A workspace owns allocation, access, and disposal of generated fields and execution-owned temporary storage.

### Job

**Future.**

A deterministic Burst-compatible transform over resolved native data.

Jobs receive native containers and unmanaged values.

### Native storage

**Future.**

Allocated native data for one generation run.

Native storage is owned by the execution workspace.

### Operation scratch

**Future.**

Private scheduler-owned native temporary storage.

### OperationScheduler

**Future.**

The execution controller for one runnable operation.

An operation scheduler owns operation execution control flow, dependency wiring, scratch allocation, job scheduling, repeated chains, termination policy, and failure policy.

### Payload field

**Future.**

A derived consumer-facing field.

Payload fields are not canonical generation truth.

### RunnableOperation

**Future.**

An execution-ready operation entry inside a runnable stage.

A runnable operation binds a planned operation to resource-field bindings, scheduler binding, and execution metadata.

### RunnablePlan

**Future.**

An execution-ready metadata representation of one generation plan.

A runnable plan is not the native workspace and is not a running job graph.

### RunnablePlanCompiler

**Future.**

A compiler that transforms a managed `GenerationPlan` into executable metadata.

A runnable plan compiler resolves semantic resources to field definitions and selected implementations to scheduler bindings.

### RunnableStage

**Future.**

An execution-ready stage entry inside a runnable plan.

### Scheduler binding

**Future.**

A binding from an operation implementation choice to the scheduler that can execute it.

### Stage-transient field

**Future.**

A workspace field shared across operations within a stage or bounded stage group.

### Unsafe collection

**Future.**

A low-level native or unsafe data structure used by execution infrastructure for explicit memory layout, lifetime, topology construction, controlled aliasing, memory-range operations, interop, artifact layout, or measured target workload performance.

### Value kind

**Future.**

The value representation category for a field.

Examples include integer, floating-point, byte, mask, vector, and structured value categories.
```
