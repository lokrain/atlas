# Glossary

This glossary defines Lokrain.Atlas architecture terms.

Terms in the **Future terms** section describe planned architecture. They are not implemented Runtime behavior unless corresponding Runtime code exists.

## Current terms

### Accepted object

An object that has validated the invariants owned by its own type.

An accepted object can still be rejected by a higher-level owner when catalog ownership, graph consistency, dependency order, or descriptor satisfiability is invalid.

### Assembly boundary

A Unity assembly definition boundary.

Assembly boundaries control which code can reference Runtime APIs, Editor APIs, tests, samples, Unity APIs, Burst, Jobs, Collections, ECS/DOTS, and generation modules.

### Authoring adapter

A Unity-facing asset, importer, editor object, or tool that translates authored data into descriptors or accepted definitions.

Authoring adapters are not canonical Runtime state.

### Builder

A mutable object used to assemble an accepted immutable object.

`GenerationCatalogBuilder` collects candidate definitions and creates a `GenerationCatalog` after catalog validation succeeds.

### Catalog ownership

The relationship between a `GenerationCatalog` and the accepted definition instances it exposes.

Catalog ownership is reference-exact. Two definitions with the same symbol are not interchangeable when only one instance belongs to a catalog.

### Cell

A map coordinate with `X` and `Z` components.

A cell is inside a grid only when validated against that grid.

### CellIndex

A row-major flattened map cell index.

A cell index is inside a grid only when validated against that grid.

### Contract

Managed planning metadata that describes semantic resource flow.

Stage and operation contracts use `ResourceDefinition` inputs and outputs.

Contracts do not define storage.

### Definition

Accepted reusable package inventory.

Definitions describe reusable domain concepts such as schemas, resources, stages, routes, route steps, operations, implementations, and recipes.

Definitions do not represent one generation run.

### Descriptor

A symbolic input object that describes intent before catalog resolution.

A descriptor validates its own structure. Its symbols may still be unresolved for a specific catalog.

### DisplayName

Validated user-facing text used for editor UI, diagnostics, reports, and documentation.

A display name is metadata. It is not identity and is not used for lookup.

### Error object

A structured object that describes a failure.

Error objects are used inside result objects for expected boundary failures.

### GenerationCatalog

An immutable accepted inventory of generation definitions and contracts.

A generation catalog provides lookup, discovery, ownership validation, and graph validation for accepted definitions.

### GenerationCatalogBuilder

A mutable assembly surface for building a `GenerationCatalog`.

The builder accepts candidate definitions and delegates final catalog invariants to catalog creation.

### Generation module

A package area that owns built-in definitions, contracts, recipes, and descriptor factories for a generation domain.

The current built-in generation module is `Lokrain.Atlas.Generation.Landmass`.

### GenerationPlan

An accepted managed semantic plan for one generation run.

A generation plan contains run settings, the selected recipe, and ordered stage plan nodes.

A generation plan contains no native storage, job handles, scheduler state, field handles, or executable job data.

### GenerationPlanCompiler

The managed compiler that transforms an accepted `GenerationRequest` into a `GenerationPlan`.

### GenerationRecipeDefinition

A reusable accepted generation template.

A generation recipe has a symbol, display name, schema, selected stage routes, and default route-step implementation choices.

### GenerationRequest

Accepted resolved generation intent for one run.

A generation request contains accepted definitions, run settings, and final operation implementation choices.

A generation request contains no unresolved symbols.

### GenerationRequestDescriptor

A symbolic descriptor for generation intent.

A generation request descriptor contains a recipe symbol, run settings, and optional symbolic operation implementation overrides.

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

An accepted definition for a generation family.

A schema provides semantic context for resources, stages, operations, recipes, and generation modules.

### Grid

The horizontal map grid for one generation run.

A grid has `Width`, `Depth`, `CellCount`, and `LastIndexValue`.

`Width` is the horizontal X dimension. `Depth` is the horizontal Z dimension.

### Landmass

The current built-in generation module for continental landmass planning definitions.

### OperationContract

A resource-definition-based input/output contract for an operation.

An operation contract is managed planning metadata.

### OperationDefinition

An accepted definition of semantic generation work.

An operation definition belongs to a schema and an operation kind.

### OperationImplementationDefinition

An accepted selectable implementation option for an operation definition.

An operation implementation definition identifies an implementation choice. It does not execute work by itself.

### OperationImplementationOverrideDescriptor

A symbolic descriptor that overrides the selected implementation for one recipe route step.

### OperationKind

A semantic category of operation.

### OperationPlanNode

A compiler-created operation node inside a `StagePlanNode`.

An operation plan node represents one selected route-step operation and its selected implementation metadata.

### Recipe

A reusable generation template.

In Runtime APIs, the concrete recipe type is `GenerationRecipeDefinition`.

### ResourceDefinition

An accepted semantic definition of a generated value.

Stage and operation contracts use resource definitions to declare required inputs and produced outputs.

A resource definition does not describe storage layout.

### Result object

An object that represents the outcome of a boundary where failure is expected.

Result objects expose structured errors for normal negative outcomes.

### Runtime

Player-safe package code under `Runtime/`.

Current managed domain and planning Runtime code does not depend on `UnityEngine` or `UnityEditor`.

### Seed

The deterministic root seed for one generation run.

A zero seed is valid.

### StageContract

A resource-definition-based input/output contract for a stage.

A stage contract is managed planning metadata.

### StageDefinition

An accepted definition of a generation stage.

A stage definition belongs to a schema and has a stage kind, symbol, and display name.

### StageKind

A semantic category of generation stage.

### StagePlanNode

A compiler-created stage node inside a `GenerationPlan`.

A stage plan node represents one selected stage, its selected route, its stage contract, and its ordered operation plan nodes.

### StageRouteChoice

An accepted recipe choice binding a stage to the route and contract selected for that stage.

### StageRouteDefinition

An accepted ordered route for satisfying a stage.

A stage route owns an ordered list of route-step definitions.

### StageRouteStepDefinition

An accepted operation occurrence inside a stage route.

A route step has its own stable symbol so the same operation definition can appear multiple times with distinct per-occurrence identity.

### StageRouteStepImplementationChoice

An accepted choice binding a route-step occurrence to the selected operation definition, operation contract, and operation implementation definition for that occurrence.

### Symbol

A stable machine-facing token.

A symbol is identity text for lookup, catalog membership, descriptor resolution, and artifact compatibility.

## Future terms

### Artifact

A planned captured generation output intended for tooling, diagnostics, persistence, preview, export, or consumer use.

### Canonical field

A planned durable generated field that represents authoritative generated map truth for a resource.

### Diagnostic field

A planned validation, debug, or tooling field.

Diagnostic field capture depends on execution profile policy.

### Execution profile

A planned named configuration for selecting storage representation, capture behavior, scheduler binding, or implementation-specific execution policy.

### External field

A planned caller-provided, importer-provided, or tooling-provided field bound into a generation run.

### FieldDefinition

A planned storage-facing definition for a resource.

A field definition describes how a semantic resource is represented for execution.

### FieldDefinitionSet

A planned accepted collection of field definitions used by runnable plan compilation.

### Field handle

A planned execution-time handle used to address workspace-owned field storage.

### Field shape

The planned spatial or structural shape of a field.

Examples include cell-grid fields, scalar fields, sparse fields, and payload-specific shapes.

### Field value kind

The planned stored value category of a field.

Examples include scalar values, indexed values, masks, vectors, ranges, and payload-specific values.

### GenerationWorkspace

The planned native storage owner for one generation run.

A workspace owns allocation, access, and disposal of generated fields and execution-owned temporary storage.

### Job

A planned deterministic Burst-compatible transform over resolved native data.

Jobs receive native containers and unmanaged values.

### Native storage

Planned allocated native data for one generation run.

Native storage is owned by the execution workspace.

### Operation scratch

Planned private scheduler-owned native temporary storage.

Operation scratch is not canonical generated data.

### OperationScheduler

The planned execution controller for one runnable operation.

An operation scheduler owns operation execution control flow, dependency wiring, scratch allocation, job scheduling, repeated chains, termination policy, and failure policy.

### Runnable operation

A planned executable operation node created from managed planning metadata.

A runnable operation contains execution metadata and field bindings needed by the scheduler.

### Runnable plan

Planned executable metadata compiled from a `GenerationPlan`.

A runnable plan bridges managed semantic planning and native execution.

### RunnablePlanCompiler

The planned compiler that transforms a managed `GenerationPlan`, field definitions, and execution profiles into a `RunnablePlan`.

### Runnable stage

A planned executable stage node created from managed planning metadata.

A runnable stage groups runnable operations for execution.

### Scheduler binding

A planned binding between runnable metadata, workspace storage, and operation scheduler execution.

### Temporary field

A planned intermediate field used during execution.

A temporary field is not part of the canonical generated output unless explicitly captured by policy.

### Workspace allocation

A planned allocation made by `GenerationWorkspace` for canonical fields, temporary fields, external fields, diagnostic fields, or scratch storage.