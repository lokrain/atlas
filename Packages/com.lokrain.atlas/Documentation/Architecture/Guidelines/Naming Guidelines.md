# Naming guidelines

This document defines naming rules for Lokrain.Atlas architecture, public API, namespaces, files, symbols, resources, and generation modules.

Naming must communicate ownership and architectural role. A name should make it clear whether a type is a descriptor, definition, catalog, recipe, request, plan, compiler, resolver, result, error, builder, module surface, future execution object, or Unity adapter.

## Primary rule

Use the name that matches the architecture role.

Correct:

```text
ResourceDefinition
GenerationCatalog
GenerationRequestDescriptor
GenerationRequestResolver
GenerationRequestResolutionResult
GenerationRequest
GenerationPlanCompiler
GenerationPlan
````

Incorrect:

```text
ResourceData
GenerationManager
RequestInfo
PlanContext
OperationProcessor
```

Do not use generic suffixes when a precise architecture term exists.

## Namespace root

All package Runtime domain APIs use the root namespace:

```text
Lokrain.Atlas
```

Package areas use nested namespaces that match their architecture area.

Examples:

```text
Lokrain.Atlas.Core
Lokrain.Atlas.Core.Map
Lokrain.Atlas.Schemas
Lokrain.Atlas.Resources
Lokrain.Atlas.Stages
Lokrain.Atlas.Operations
Lokrain.Atlas.Catalog
Lokrain.Atlas.Recipes
Lokrain.Atlas.Requests
Lokrain.Atlas.Plans
Lokrain.Atlas.Generation.Landmass
```

Do not use namespaces that imply ownership by Unity scene objects, editor state, ECS worlds, or execution systems for current managed planning types.

## File names

A public type should be in a file with the same name as the type.

Correct:

```text
ResourceDefinition.cs
GenerationCatalog.cs
GenerationRequestDescriptor.cs
GenerationPlanCompiler.cs
LandmassResourceDefinitions.cs
```

Avoid multi-type files for public domain objects unless the additional type is tightly scoped, small, and intentionally subordinate.

Do not use vague file names:

```text
Definitions.cs
GenerationData.cs
Helpers.cs
Utils.cs
Common.cs
```

## Type suffixes

Use architecture suffixes consistently.

| Suffix                        | Use when                                                              |
| ----------------------------- | --------------------------------------------------------------------- |
| `Definition`                  | The type is accepted reusable package inventory.                      |
| `Descriptor`                  | The type is symbolic input before catalog resolution.                 |
| `Catalog`                     | The type is accepted immutable inventory and lookup.                  |
| `Builder`                     | The type is mutable assembly state for creating an accepted object.   |
| `Recipe` / `RecipeDefinition` | The type is a reusable generation template.                           |
| `Request`                     | The type is accepted resolved intent for one run.                     |
| `Resolver`                    | The type resolves symbolic input into accepted domain objects.        |
| `Result`                      | The type represents success or expected failure at a boundary.        |
| `Error`                       | The type describes a structured failure.                              |
| `Compiler`                    | The type transforms accepted input into a lower-level representation. |
| `Plan`                        | The type is managed semantic planning output.                         |
| `Node`                        | The type is an element inside a plan graph or ordered plan structure. |
| `Choice`                      | The type binds one accepted selection.                                |
| `Kind`                        | The type identifies a semantic category.                              |

Use these suffixes only when the type owns the corresponding responsibility.

## Generic suffixes to avoid

Avoid:

```text
Manager
Processor
Handler
Context
Data
Info
Model
Object
Helper
Utility
Service
Controller
```

These names are allowed only when there is no precise domain role and the type’s responsibility is still clear.

Prefer architecture names:

| Avoid                | Prefer                                                                                                     |
| -------------------- | ---------------------------------------------------------------------------------------------------------- |
| `GenerationManager`  | `GenerationRequestResolver`, `GenerationPlanCompiler`, or a specific owner.                                |
| `ResourceData`       | `ResourceDefinition` or future `FieldDefinition`, depending on meaning.                                    |
| `RequestInfo`        | `GenerationRequestDescriptor` or `GenerationRequest`.                                                      |
| `PlanContext`        | `GenerationPlan`, future `RunnablePlan`, or future `GenerationWorkspace`.                                  |
| `OperationProcessor` | `OperationScheduler` if it schedules execution, or a specific compiler/resolver if it transforms metadata. |

## Definition names

Use `Definition` for accepted reusable package inventory.

Correct:

```text
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
OperationDefinition
OperationImplementationDefinition
GenerationRecipeDefinition
```

Do not use `Definition` for one-run state, descriptors, execution state, native storage, jobs, or Unity adapters.

Incorrect:

```text
GenerationRequestDefinition
GenerationPlanDefinition
WorkspaceDefinition
JobDefinition
MonoBehaviourDefinition
```

A definition describes reusable inventory. It does not represent one generation run.

## Descriptor names

Use `Descriptor` for symbolic input before catalog resolution.

Correct:

```text
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
```

A descriptor name must imply unresolved symbolic intent.

Do not use `Descriptor` for accepted catalog-owned definitions or resolved requests.

Incorrect:

```text
ResourceDescriptor
GenerationRecipeDescriptor
GenerationRequestDescriptorResult
ResolvedDescriptor
```

If the object contains accepted definitions and no unresolved symbols, it is not a descriptor.

## Request names

Use `Request` for accepted resolved intent for one generation run.

Correct:

```text
GenerationRequest
```

Use `GenerationRequestDescriptor` for symbolic intent before resolution.

Do not use:

```text
ResolvedGenerationRequest
AcceptedGenerationRequest
GenerationRequestData
```

The architecture already defines `GenerationRequest` as accepted and resolved.

## Plan names

Use `Plan` for managed semantic planning output.

Correct:

```text
GenerationPlan
StagePlanNode
OperationPlanNode
GenerationPlanCompiler
```

Do not use `Plan` for native execution state or scheduled jobs.

Incorrect:

```text
JobPlan
NativePlan
WorkspacePlan
GenerationPlanJobHandle
```

Future executable metadata should use `RunnablePlan`, not `GenerationPlan`.

## Compiler names

Use `Compiler` for deterministic transformation from an accepted representation into another representation.

Correct:

```text
GenerationPlanCompiler
```

Future:

```text
RunnablePlanCompiler
```

A compiler should not be named `Builder` when it derives output from accepted input.

A compiler should not be named `Resolver` when it does not resolve symbols.

## Resolver names

Use `Resolver` for a boundary that resolves symbolic references into accepted objects.

Correct:

```text
GenerationRequestResolver
```

A resolver returns accepted output or structured errors for expected satisfiability failure.

Do not use `Resolver` for plan compilation, native allocation, scheduling, or job execution.

## Builder names

Use `Builder` for mutable assembly state used to create an accepted immutable object.

Correct:

```text
GenerationCatalogBuilder
```

A builder name must imply mutability and construction.

Do not use `Builder` for accepted immutable objects or deterministic compilers.

Incorrect:

```text
GenerationPlanBuilder
```

Use `GenerationPlanCompiler` when the object transforms an accepted request into a plan.

## Result and error names

Use `Result` for expected success/failure boundary output.

Correct:

```text
GenerationRequestResolutionResult
```

Use `Error` for structured domain failure.

Correct:

```text
GenerationRequestResolutionError
GenerationRequestResolutionErrorCode
```

Error-code enums should end with `Code`.

Diagnostic text should be named `Message`.

The symbol most related to the error should be named according to its role.

Example:

```text
SubjectSymbol
```

Do not name structured errors as exceptions unless they are thrown.

Incorrect:

```text
GenerationRequestResolutionException
RequestFailureInfo
ResolverProblem
```

## Choice names

Use `Choice` for accepted selections.

Correct:

```text
StageRouteChoice
StageRouteStepImplementationChoice
```

A choice contains accepted objects.

Do not use `Choice` for symbolic input. Use `Descriptor` for symbolic input.

Correct distinction:

```text
OperationImplementationOverrideDescriptor -> symbolic override input
StageRouteStepImplementationChoice        -> accepted final choice
```

## Kind names

Use `Kind` for stable semantic categories.

Correct:

```text
StageKind
OperationKind
```

Kinds should expose symbol identity.

Do not use `Type` when the name could be confused with `System.Type`.

Avoid:

```text
StageType
OperationType
```

Prefer:

```text
StageKind
OperationKind
```

## Contract names

Use `Contract` for semantic input/output requirements.

Correct:

```text
StageContract
OperationContract
```

Contract properties should describe semantic resource flow:

```text
RequiredInputs
ProducedOutputs
```

Do not use symbol-specific names for current resource-definition-based contracts:

```text
RequiredInputSymbols
ProducedOutputSymbols
```

Do not use storage-specific contract names:

```text
RequiredFields
ProducedNativeArrays
InputHandles
OutputHandles
```

Current contracts are resource-definition-based, not field/storage-based.

## Resource names

Use `ResourceDefinition` for semantic generated values.

Use resource names that describe domain meaning, not storage representation.

Good resource display concepts:

```text
Height
Land
Ocean
Coast
Continental Region
```

Good resource symbol endings:

```text
height
land
ocean
coast
continental_region
```

Avoid storage-shaped resource names:

```text
HeightFloatArray
LandNativeArray
OceanByteMask
CoastFieldHandle
RegionBuffer
```

Those names describe representation, not semantic resource identity.

## Field names

Use `FieldDefinition` only for future storage-facing metadata.

A field name may describe representation only when the type is actually storage-facing future architecture.

Correct future distinction:

```text
ResourceDefinition.Height
FieldDefinition.HeightFloatGrid
GenerationWorkspace allocation for HeightFloatGrid
```

Do not call current semantic resources fields.

Do not call native storage resources.

## Workspace names

Use `GenerationWorkspace` only for future per-run native storage ownership.

A workspace owns allocation, access, and disposal of native execution data.

Do not use `Workspace` for:

```text
catalog lookup
request resolution
managed plan compilation
static module definitions
editor-only authoring state
```

## Scheduler names

Use `OperationScheduler` only for future execution control flow.

A scheduler owns dependency wiring, scratch allocation, and job scheduling for executable operations.

Do not use `Scheduler` for plan compilation, catalog lookup, request resolution, or recipe selection.

## Job names

Use `Job` only for actual Unity job structs or job-like execution units.

A job name should describe the deterministic transform.

Examples:

```text
ExtractContinentalRegionsJob
ClassifyCoastCellsJob
WriteHeightFieldJob
```

Do not name managed planning objects as jobs.

Incorrect:

```text
GenerationPlanJob
OperationDefinitionJob
ResourceDefinitionJob
```

## Module surface names

Generation modules should use a domain prefix and plural static surfaces for groups of built-in definitions.

Current module:

```text
Landmass
```

Good module surfaces:

```text
LandmassResourceDefinitions
LandmassStageDefinitions
LandmassOperationDefinitions
LandmassOperationImplementationDefinitions
LandmassRecipeDefinitions
LandmassCatalogs
LandmassRequestDescriptors
```

Use plural names when the surface exposes multiple built-in objects.

Use singular names when the surface exposes one concept or factory.

Do not use old symbolic-only surface names when the surface now exposes accepted definitions.

Correct:

```text
LandmassResourceDefinitions
```

Incorrect:

```text
LandmassResourceSymbols
```

## Built-in definition property names

Built-in definition properties should use domain names.

Correct:

```text
Height
Land
Ocean
Coast
ContinentalRegion
```

Avoid suffixing every property with the type name when the containing type already provides context.

Correct:

```text
LandmassResourceDefinitions.Height
```

Avoid:

```text
LandmassResourceDefinitions.HeightResourceDefinition
```

Use a suffix only when ambiguity exists.

## Symbol naming

Symbols are stable machine-facing identity.

Symbols should be:

```text
lowercase
dot-separated by ownership/domain segments
underscore-separated inside the final semantic segment when needed
ASCII
stable
specific
```

Preferred pattern:

```text
lokrain.atlas.<module>.<category>.<name>
```

Examples:

```text
lokrain.atlas.schema.world
lokrain.atlas.landmass.resource.height
lokrain.atlas.landmass.resource.continental_region
lokrain.atlas.landmass.stage.continental_landmass
lokrain.atlas.landmass.operation.main_continent_extraction
```

Do not use display text as a symbol.

Incorrect:

```text
Height
Continental Region
Main Continent Extraction
```

Symbols should not include volatile implementation details.

Avoid:

```text
lokrain.atlas.landmass.resource.height_native_array_float_v2
lokrain.atlas.landmass.operation.fast_job_2026
```

## Symbol segment names

Use consistent category segments.

Recommended segments:

```text
schema
resource
stage_kind
stage
route
route_step
operation_kind
operation
operation_implementation
recipe
```

Examples:

```text
lokrain.atlas.landmass.route.primary_continental_landmass
lokrain.atlas.landmass.route_step.extract_main_continent
lokrain.atlas.landmass.operation_implementation.default_main_continent_extraction
```

Do not mix category names for the same concept.

Avoid using both:

```text
impl
implementation
operation_impl
operation_implementation
```

Choose one canonical segment and use it consistently.

## Display name naming

Display names are user-facing.

They should use normal title casing or readable sentence-style casing, depending on UI context.

Examples:

```text
Height
Continental Region
Main Continent Extraction
Primary Continental Landmass
```

Display names must not be used as identity.

Do not encode ownership paths into display names.

Avoid:

```text
lokrain.atlas.landmass.resource.height
LANDMASS_HEIGHT
HeightResourceDefinition
```

Those are symbol or code names, not display names.

## Property names

Public properties should use precise domain names.

Correct:

```text
Symbol
DisplayName
Schema
RequiredInputs
ProducedOutputs
Stage
Route
RouteStep
Operation
OperationImplementation
RunSettings
StagePlanNodes
OperationPlanNodes
```

Avoid vague property names:

```text
Name
Id
Data
Info
Items
Values
Stuff
Context
Target
Source
```

Use `Symbol` instead of `Id` when the value is a `Symbol`.

Use `DisplayName` instead of `Name` when the value is human-facing display text.

## Collection property names

Collection properties should use plural names.

Correct:

```text
ResourceDefinitions
StageDefinitions
OperationDefinitions
GenerationRecipeDefinitions
RequiredInputs
ProducedOutputs
StagePlanNodes
OperationPlanNodes
Errors
```

Do not use singular names for collections.

Incorrect:

```text
ResourceDefinition
StageDefinitionList
OperationNodeArray
ErrorCollection
```

The collection type already communicates list/array shape. The property name should communicate domain meaning.

## Boolean names

Boolean properties and methods should read as predicates.

Correct:

```text
IsSuccess
IsFailure
Contains(...)
TryGet(...)
TryResolve(...)
```

Avoid:

```text
Success
Failure
Valid
Resolved
```

Use `Try` only when the method follows the .NET Try pattern:

```text
bool TryGetValue(..., out TValue value)
```

Do not use `Try` for methods that throw on normal failure.

## Method names

Use .NET-style verbs that match behavior.

| Verb                        | Use when                                                      |
| --------------------------- | ------------------------------------------------------------- |
| `Create`                    | Factory creates and validates a new accepted object.          |
| `TryCreate`                 | Factory attempts creation without throwing for invalid input. |
| `Parse`                     | Converts text and throws on invalid text.                     |
| `TryParse`                  | Converts text and returns false on invalid text.              |
| `Resolve`                   | Converts symbolic input into accepted resolved output.        |
| `Compile`                   | Converts accepted input into a lower-level representation.    |
| `Add`                       | Adds one candidate object to mutable builder state.           |
| `AddRange` or plural `Add*` | Adds many candidate objects.                                  |
| `Contains`                  | Tests membership.                                             |
| `Get`                       | Retrieves and throws if invalid/missing by contract.          |
| `TryGet`                    | Retrieves and returns false when missing.                     |

Examples:

```text
DisplayName.Create
DisplayName.TryCreate
Seed.Parse
Seed.TryParse
GenerationRequestResolver.Resolve
GenerationPlanCompiler.Compile
GenerationCatalogBuilder.AddResourceDefinition
GenerationCatalogBuilder.AddResourceDefinitions
Grid.Contains
Grid.GetCell
Grid.TryGetCell
```

## Add method names

For builders, use singular and plural add methods when both are useful.

Correct:

```text
AddResourceDefinition(ResourceDefinition resourceDefinition)
AddResourceDefinitions(IEnumerable<ResourceDefinition> resourceDefinitions)
```

Use the domain noun, not `Item`.

Incorrect:

```text
AddItem(...)
AddData(...)
AddResources(...) // if the type is specifically ResourceDefinition and precision matters
```

Prefer exact type meaning over shorthand when the shorthand is ambiguous.

## Get method names

Use `Get` when failure means invalid API usage or violated caller precondition.

Example:

```text
Grid.GetCell(int x, int z)
```

Use `TryGet` when absence is expected.

Example:

```text
GenerationCatalog.TryGetResourceDefinition(Symbol symbol, out ResourceDefinition resourceDefinition)
```

Do not name methods `Find` when the absence behavior is unclear.

## Validation method names

Use `Validate` for methods that throw when invalid.

Use `IsValid` for methods that return a boolean.

Use `TryCreate` when validation and construction happen together.

Avoid:

```text
Check
Ensure
Verify
```

unless the codebase has a strong convention and the behavior is obvious.

## Exception names

Custom exceptions should be rare.

Prefer standard .NET exceptions for invalid API usage:

```text
ArgumentNullException
ArgumentException
ArgumentOutOfRangeException
InvalidOperationException
```

Do not create exception names for expected request-resolution errors.

Correct:

```text
GenerationRequestResolutionError
GenerationRequestResolutionErrorCode
```

Incorrect:

```text
MissingRecipeException
ImplementationOverrideException
```

Expected catalog-satisfiability failure should be a result error, not an exception.

## Enum names

Enums should use singular type names.

Correct:

```text
GenerationRequestResolutionErrorCode
```

Enum values should be precise and stable.

Correct:

```text
RecipeNotFound
OverrideTargetNotFound
ImplementationNotFound
ImplementationNotCompatible
DuplicateOverrideTarget
```

Avoid:

```text
Error
Invalid
Failed
BadRequest
UnknownProblem
```

Use `Unknown` only when the domain explicitly needs a safe fallback.

## Test names

Test names should describe observable behavior.

Preferred pattern:

```text
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:

```text
Create_NullSymbol_ThrowsArgumentNullException
Resolve_MissingRecipeSymbol_ReturnsRecipeNotFoundError
Compile_ValidRequest_PreservesRouteStepOrder
```

Avoid names that describe implementation details instead of behavior.

Incorrect:

```text
Test1
ResourceDefinitionWorks
ResolverUsesDictionary
```

## Assembly names

Runtime assembly names should match package and architecture area.

General pattern:

```text
Lokrain.Atlas
Lokrain.Atlas.Generation.Landmass
```

Editor assemblies should end with:

```text
.Editor
```

Test assemblies should end with:

```text
.Tests
```

When both editor and tests exist, `.Editor` comes before `.Tests` in the assembly name.

Examples:

```text
Lokrain.Atlas.Editor
Lokrain.Atlas.Tests
Lokrain.Atlas.Generation.Landmass.Editor
Lokrain.Atlas.Generation.Landmass.Tests
```

Do not place `.Editor` or `.Tests` in the middle of the name unless it represents the final assembly classification.

## Folder names

Folder names should match architecture areas and namespaces.

Current managed architecture folders may include:

```text
Core
Core/Map
Schemas
Resources
Stages
Operations
Catalog
Recipes
Requests
Plans
Generation/Landmass
```

Future execution folders should use explicit execution terms when implemented.

Examples:

```text
Fields
Execution
Workspaces
Schedulers
Jobs
```

Do not put execution types into `Plans` unless they are managed semantic plan types.

Do not put Unity editor adapters into Runtime folders.

## Landmass naming

Landmass is the current built-in generation module.

Use `Landmass` as the module prefix for built-in landmass surfaces.

Correct:

```text
LandmassResourceDefinitions
LandmassStageDefinitions
LandmassOperationDefinitions
LandmassRecipeDefinitions
```

Do not use `Terrain` and `Landmass` interchangeably.

Use `Terrain` only when the concept is specifically terrain representation, not the landmass generation module.

## Map dimension naming

Use `Width` and `Depth` for horizontal grid dimensions.

Use `X` and `Z` for horizontal cell coordinates.

Use `Height` only for elevation or height-field semantics.

Correct:

```text
Grid.Width
Grid.Depth
Cell.X
Cell.Z
LandmassResourceDefinitions.Height
```

Incorrect:

```text
Grid.Height
Cell.Y
MapRowsAsHeight
```

Atlas reserves height for elevation meaning.

## Resource naming in landmass

Landmass resource names should describe generated semantic values.

Current pattern:

```text
Height
Land
Ocean
Coast
ContinentalRegion
```

Use singular names when the resource represents one field/value concept.

Use plural names only when the semantic value is inherently a collection.

Avoid implementation names:

```text
HeightArray
LandMaskBytes
OceanNativeBuffer
CoastDebugTexture
RegionIdsNativeArray
```

## Operation naming

Operation names should describe semantic work.

Correct:

```text
MainContinentExtraction
CoastClassification
OceanClassification
HeightNormalization
```

Avoid implementation details in operation definition names:

```text
MainContinentBurstJob
FastCoastLoop
OceanNativeArrayWriter
```

Implementation definitions may include implementation strategy names when the strategy is a selectable domain choice.

## Implementation naming

Implementation definition names should identify selectable implementation choices.

Correct:

```text
DefaultMainContinentExtraction
DeterministicCoastClassification
```

Avoid names that imply executable code ownership unless the type actually owns execution code.

Incorrect for current metadata-only definitions:

```text
MainContinentExtractionJob
MainContinentExtractionScheduler
MainContinentExtractionBurstFunction
```

Future scheduler/job names may use scheduler/job terms when those types exist.

## Future execution naming

Future execution names must stay explicit.

Use:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
SchedulerBinding
GenerationWorkspace
OperationScheduler
OperationScratch
```

Do not use these names for current managed planning concepts.

Incorrect:

```text
GenerationPlan as RunnablePlan
ResourceDefinition as FieldDefinition
OperationImplementationDefinition as SchedulerBinding
OperationPlanNode as RunnableOperation
```

## Unity adapter naming

Unity-facing adapters should include the Unity role when useful.

Examples:

```text
LandmassRecipeAsset
GenerationRequestAuthoring
AtlasCatalogImporter
AtlasGenerationWindow
```

Unity adapters should not take canonical domain names away from Runtime objects.

Incorrect:

```text
GenerationRecipeDefinition : ScriptableObject
GenerationCatalog : ScriptableObject
GenerationRequest : MonoBehaviour
```

A ScriptableObject may author or serialize data used to create a domain object. It is not the domain object unless the architecture explicitly accepts that coupling.

## Avoid historical names

Do not keep obsolete names as aliases in current architecture docs or public API.

Examples of names that should not appear in current Runtime/Test code:

```text
LandmassResourceSymbols
RequiredInputSymbols
ProducedOutputSymbols
```

Use current names:

```text
LandmassResourceDefinitions
RequiredInputs
ProducedOutputs
```

Historical names may appear only in decision records when necessary to explain a rejected option or completed migration.

## Abbreviation rules

Avoid unexplained abbreviations in public API.

Prefer:

```text
Definition
Implementation
Operation
Generation
Resource
```

Avoid:

```text
Def
Impl
Op
Gen
Res
```

Short forms are acceptable only for conventional technical terms or local variables where readability improves.

Examples:

```text
x
z
id
uri
api
```

For domain types and public members, prefer full names.

## Acronym casing

Use standard .NET acronym casing.

Examples:

```text
Uri
Xml
Json
Id
Api
```

Do not use all-uppercase acronyms in PascalCase public API unless required by external convention.

Prefer:

```text
JsonPayload
ApiClient
IdMap
```

Avoid:

```text
JSONPayload
APIClient
IDMap
```

For Atlas specifically, prefer `Symbol` over `Id` when the value is a `Symbol`.

## Generic type parameter names

Use clear generic parameter names.

Correct:

```text
T
TValue
TDefinition
TResource
TOperation
```

Avoid:

```text
TX
TData
TThing
```

Use domain-specific names when the generic is constrained to a domain concept.

## Local variable names

Local variables should be short but domain-accurate.

Correct:

```text
catalog
descriptor
request
result
recipe
resourceDefinition
operationDefinition
implementationDefinition
stagePlanNode
operationPlanNode
```

Avoid:

```text
data
item
thing
obj
ctx
mgr
```

Use `index` for flattened indices and `x` / `z` for grid coordinates.

Use `cell` for coordinate objects and `cellIndex` for `CellIndex`.

## Parameter names

Parameter names should match the type’s domain role.

Correct:

```text
ResourceDefinition resourceDefinition
GenerationCatalog catalog
GenerationRequestDescriptor descriptor
GenerationRequest request
GenerationPlan plan
```

Avoid vague parameters:

```text
object value
object data
IEnumerable<T> items
string name
```

Use `value` for primitive/value-object factory input when the input is exactly the value being created.

Examples:

```text
Symbol.Create(string value)
DisplayName.Create(string value)
Seed.Parse(string value)
```

## Name collision rule

Do not introduce a new name that differs only by layer ambiguity.

Avoid having both:

```text
Resource
ResourceDefinition
Field
FieldDefinition
Operation
OperationDefinition
Implementation
OperationImplementationDefinition
```

unless each name has a clear and documented role.

Prefer the full architecture name for public API.

Use shorter names only for local variables or nested/private helpers.

## Documentation names

Documentation file names should match the document’s job.

Use:

```text
Overview
Concepts
Guidelines
Reference
Future
Decisions
Plans
```

A document name should not combine unrelated jobs.

Correct:

```text
Naming Guidelines.md
Architecture Rules.md
Dependency Rules.md
Glossary.md
Runnable Plan Compilation.md
```

Incorrect:

```text
Architecture and Naming Rules.md
Terminology and Implementation Plan.md
Future Ideas and Current Rules.md
```

## Review checklist

Before accepting a new name, verify:

```text
The name matches the architecture role.
The name does not hide ownership.
The name does not mix current and future concepts.
The name does not use display text as identity.
The name does not imply Unity ownership for domain objects.
The name does not imply execution behavior for metadata objects.
The name does not use Manager/Data/Info/Context when a precise term exists.
The name is stable enough for public API.
The symbol form is machine-facing and deterministic.
The display name form is human-facing and non-identifying.
```

## Summary

Names in Atlas are architectural contracts.

Use precise names for precise roles.

Use `Definition` for accepted reusable inventory.

Use `Descriptor` for symbolic input.

Use `Request` for accepted resolved run intent.

Use `Plan` for managed semantic planning output.

Use `ResourceDefinition` for semantic generated values.

Reserve field, workspace, scheduler, and job names for future execution architecture.

Do not use vague names when the architecture already has a correct term.

```