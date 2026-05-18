# Glossary

This glossary defines Lokrain.Atlas architecture terms used by Runtime code, tests, and architecture documentation.

## Status terms

`Current` means the concept is implemented in Runtime code and may be documented as present behavior.

`Planned` means the concept is part of the approved architecture direction but is not implemented unless corresponding Runtime code exists.

`Future` means planned and intentionally outside the current implemented Runtime boundary.

`Rejected` means the concept, name, dependency, or responsibility must not be introduced without a new decision record.

`Deferred` means the concept is not current behavior and requires explicit implementation work before it can be described as implemented.

## Architecture boundary terms

`Current Runtime` is the implemented managed Runtime surface. It includes domain values, definitions, contracts, catalogs, recipes, request resolution, accepted requests, managed semantic plan compilation, managed field metadata, managed execution-profile identity, and managed runnable metadata compilation.

`Managed semantic Runtime` is the Runtime layer that describes generation meaning, identity, validation, request intent, and semantic plan order without native execution state.

`Managed runnable metadata` is immutable metadata compiled from a `GenerationPlan`, field metadata, and an execution profile. It prepares table-oriented executable metadata without allocating storage, scheduling jobs, binding ECS data, capturing artifacts, or capturing runtime diagnostics.

`Execution infrastructure` is the planned layer after managed runnable metadata. It includes workspace storage, scheduling, jobs, artifacts, diagnostics, and ECS execution integration.

`Semantic object` is a managed domain object that describes identity, meaning, configuration, contracts, accepted requests, or plans. Semantic objects do not own native containers, job handles, field handles, workspace allocations, scheduler state, or Unity object identity.

`Accepted object` is a validated object created inside an acceptance boundary. Accepted objects contain resolved references and construction invariants instead of unresolved caller symbols.

`Definition` is reusable managed inventory. Definitions describe stable package-domain concepts such as schemas, resources, fields, stages, routes, operations, operation implementations, execution profiles, and recipes.

`Contract` is semantic resource-flow metadata for a stage or operation. Contracts use accepted `ResourceDefinition` instances, not raw resource-symbol lists.

`Descriptor` is symbolic caller intent. Descriptors are input to resolution and may contain symbols before catalog-dependent validation occurs.

`Resolver` is a service that converts symbolic caller intent into accepted objects or structured resolution errors.

`Compiler` is a service that converts one accepted representation into another accepted representation without hidden lookup or mutable global state.

`Catalog ownership` is reference-exact ownership of accepted generation inventory by `GenerationCatalog`.

`Reference-exact ownership` means an object is owned only when the exact instance is registered by the owning object. Symbol-equivalent replacement instances are not owned.

`Symbol-equivalent` means two objects have equal symbols but are not necessarily the same instance or accepted by the same owner.

`Semantic resource flow` is the ordered dependency relationship between required and produced `ResourceDefinition` instances in stage and operation contracts.

`Storage-facing metadata` is managed metadata that describes how semantic resources are represented for execution. It is not storage ownership.

`Executable metadata` is immutable managed metadata derived from semantic plans, field metadata, execution-profile metadata, and implementation metadata. It does not own native storage.

`Native execution state` means native containers, handles, dependencies, workspace allocations, scheduler state, job structs, artifact buffers, or ECS execution data.

`Canonical order` is a deterministic public order defined by domain rules, explicit lists, arrays, or ordinal symbol sorting.

`Declared order` is the order in which a built-in provider intentionally lists items.

`Lookup index` is a private dictionary or hash set used only for lookup or membership. Lookup indexes do not define public order, generation order, diagnostic order, artifact order, or serialized order.

`Deterministic` means repeated execution with the same accepted inputs produces the same accepted ordering, metadata, and results without relying on unordered enumeration, allocation order, thread timing, current time, Unity scene order, or editor state.

## Current core terms

`Symbol` is stable package-domain identity text. Symbols are used for lookup, equality, deterministic ordering, and compatibility.

`DisplayName` is validated user-facing text for editor UI, diagnostics, and documentation. Display names are not identity.

`Grid` is validated terrain-grid dimensions and coordinate/index conversion.

`Cell` is a validated grid coordinate value.

`CellIndex` is a validated flattened zero-based grid-cell index value.

`Seed` is a deterministic numeric generation seed.

## Current schema and resource terms

`GenerationSchemaDefinition` is reusable semantic schema identity.

`BuiltInGenerationSchemas` is the built-in provider for package-owned schema definitions.

`ResourceDefinition` is semantic identity for a generated value. It belongs to a generation schema and is used by stage and operation contracts.

`LandmassResourceDefinitions` is the built-in landmass resource-definition provider.

`ResourceDefinition` is not field metadata, storage layout, workspace allocation, a native container, an artifact buffer, or an execution handle.

## Current field metadata terms

`FieldValueKind` identifies the managed value category used to represent a resource field.

`FieldShape` identifies the managed shape used to represent a resource field.

`FieldDefinition` maps one `ResourceDefinition` to managed field representation metadata.

`FieldDefinitionSet` is the accepted field metadata set used for field lookup and canonical field-symbol ordering.

`LandmassFieldDefinitions` is the built-in landmass field-definition provider.

`LandmassFieldDefinitionSet` is the accepted built-in landmass field metadata set.

`FieldDefinition` is not native storage, a workspace allocation, a field handle, an ECS binding, an artifact, or executable code.

## Current execution profile terms

`ExecutionProfile` identifies managed execution-profile metadata.

`ExecutionProfileSet` is the accepted execution-profile metadata set used for profile lookup and canonical profile-symbol ordering.

`BuiltInExecutionProfiles` is the built-in provider for package-owned execution-profile definitions.

`BuiltInExecutionProfileSet` is the accepted built-in execution-profile metadata set.

`ExecutionProfile` does not allocate storage, schedule work, select a job system path, capture artifacts, or execute operations.

## Current stage terms

`StageKind` classifies a stage at the managed semantic level.

`StageDefinition` is reusable stage identity.

`StageRouteDefinition` is an ordered reusable route through operation-definition symbols.

`StageRouteStepDefinition` is authored route-step metadata that identifies a route occurrence of an operation definition by symbol.

`StageContract` is semantic resource-flow metadata for one stage. It declares required inputs and produced outputs as `ResourceDefinition` instances.

## Current operation terms

`OperationKind` classifies an operation at the managed semantic level.

`OperationDefinition` is reusable operation identity.

`OperationContract` is semantic resource-flow metadata for one operation. It declares required inputs and produced outputs as `ResourceDefinition` instances.

`OperationImplementationDefinition` is selectable managed metadata for an implementation of an operation definition.

## Current catalog and recipe terms

`GenerationCatalog` is the accepted inventory boundary for schemas, resources, stages, routes, operations, operation implementations, and recipes.

`GenerationCatalogBuilder` builds a `GenerationCatalog` from explicit inventory and performs catalog construction validation.

`GenerationRecipeDefinition` is a reusable generation template made from stage route choices.

`StageRouteChoice` selects one route for a stage inside a recipe.

`StageRouteStepImplementationChoice` selects an operation implementation for one route step.

## Current request and semantic plan terms

`GenerationRequestDescriptor` is symbolic caller intent for a generation run.

`OperationImplementationOverrideDescriptor` is symbolic caller intent for overriding an operation implementation selection.

`GenerationRequestResolver` resolves a descriptor against a catalog into an accepted generation request or structured resolution errors.

`GenerationRequestResolutionResult` is the result object returned by request resolution.

`GenerationRequestResolutionError` is a structured request-resolution diagnostic.

`GenerationRequest` is accepted resolved generation intent.

`GenerationRunSettings` contains accepted run-level settings such as grid and seed.

`GenerationPlanCompiler` converts an accepted generation request into a managed semantic generation plan.

`GenerationPlan` is managed semantic planning output. It contains selected stage and operation plan nodes in deterministic order and does not contain native execution state.

`StagePlanNode` is accepted plan metadata for one selected stage.

`OperationPlanNode` is accepted plan metadata for one selected operation occurrence.

## Current runnable metadata terms

`FieldIndex` is a dense zero-based plan-local field-binding table position. It is not a durable identity, storage handle, resource identity, or field-definition identity.

`StageIndex` is a dense zero-based plan-local runnable-stage table position. It is not a durable identity or stage-definition identity.

`OperationIndex` is a dense zero-based plan-local runnable-operation table position. It is not a durable identity or operation-definition identity.

`FieldPlanRole` classifies a field binding as required input, produced output, or both at the runnable-plan boundary. It does not describe storage lifetime.

`FieldCapturePolicy` records future capture intent for a field binding. It does not capture artifacts or diagnostics.

`ResourceFieldBinding` is one immutable runnable field table row that connects a `ResourceDefinition` to its `FieldDefinition` by reference-exact ownership.

`RunnableOperation` is immutable managed runnable metadata for one operation plan-node occurrence.

`RunnableStage` is immutable managed runnable metadata for one stage plan-node occurrence.

`RunnablePlan` is immutable managed runnable metadata compiled from a `GenerationPlan`, `FieldDefinitionSet`, and `ExecutionProfile`.

`RunnablePlanCompiler` converts a `GenerationPlan`, `FieldDefinitionSet`, and `ExecutionProfile` into a `RunnablePlan` or deterministic structured compilation errors.

`RunnablePlanCompilationResult` is the result object returned by runnable-plan compilation.

`RunnablePlanCompilationError` is a structured runnable-plan compilation diagnostic.

`RunnablePlanCompilationErrorCode` is a stable machine-readable runnable-plan compilation failure code.

## Future execution terms

`GenerationWorkspace` is planned native storage ownership for executable generation data.

`FieldHandle` is a planned execution handle to workspace-owned field storage.

`WorkspaceAllocation` is planned metadata or state for workspace-owned allocation.

`OperationScheduler` is planned execution control flow for runnable operations, dependencies, scratch, and jobs.

`OperationScratch` is planned temporary execution memory for operations.

`Artifact capture` is planned execution infrastructure for retaining or exporting selected outputs.

`Runtime diagnostic capture` is planned execution infrastructure for runtime observability.

`ECS execution integration` is planned Unity ECS-facing execution integration.
