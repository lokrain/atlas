# Naming guidelines

This document defines naming rules for Lokrain.Atlas.

Naming must make architecture boundaries visible. A name should identify what the object is, what owns it, and whether it is current Runtime architecture or planned execution architecture.

## General rules

Use clear domain names.

Use stable public API names.

Use names that match the type’s responsibility.

Avoid names that describe implementation convenience instead of domain meaning.

Correct:

```text
ResourceDefinition
GenerationCatalog
GenerationRequestDescriptor
GenerationRequestResolver
GenerationPlanCompiler
OperationImplementationOverrideDescriptor
```

Incorrect:

```text
ResourceData
CatalogThing
RequestInfo
ResolverHelper
PlanMaker
OverrideDto
```

## Use existing platform names directly

Do not wrap, rename, or rebrand Unity, C#, .NET, DOTS, Burst, Jobs, Collections, or Mathematics concepts unless Atlas owns additional domain meaning, lifecycle ownership, invariants, or an external contract.

Correct:

```text
NativeArray<T>
JobHandle
Entity
float3
ScriptableObject
```

Incorrect:

```text
AtlasNativeArray<T>
AtlasJobHandle
AtlasEntity
AtlasVector3
AtlasScriptableAsset
```

A wrapper name is allowed only when the type owns new semantics.

Correct package-owned names:

```text
Symbol
DisplayName
Grid
Seed
ResourceDefinition
GenerationCatalog
GenerationRequest
GenerationPlan
```

## Prefer domain nouns

Use nouns for definitions, descriptors, contracts, plans, results, and settings.

Correct:

```text
GenerationSchemaDefinition
StageRouteDefinition
OperationContract
GenerationRunSettings
GenerationRequestResolutionResult
```

Incorrect:

```text
GenerateSchema
RouteStage
ContractOperation
RunGenerationSettings
ResolveRequestResult
```

## Use verbs for behavior

Use verbs for methods that perform work.

Correct:

```text
Create
TryCreate
Parse
TryParse
Resolve
Compile
Build
Contains
Get
TryGet
Add
```

Incorrect:

```text
Do
Process
Handle
RunThing
MakeStuff
Manage
```

## Suffix rules

Use suffixes consistently.

| Suffix | Use |
| --- | --- |
| `Definition` | Reusable accepted package inventory. |
| `Contract` | Semantic resource-flow contract. |
| `Descriptor` | Symbolic caller input before resolution. |
| `Result` | Boundary result object. |
| `Error` | Structured error object. |
| `Settings` | Configuration for one operation or run. |
| `Builder` | Mutable assembly object that creates an accepted object. |
| `Resolver` | Converts symbolic input into accepted objects. |
| `Compiler` | Transforms accepted input into another accepted representation. |
| `Node` | Element inside a compiled managed plan. |
| `Kind` | Semantic category. |
| `Choice` | Accepted selection among available definitions. |

Do not use these suffixes interchangeably.

Correct:

```text
GenerationRequestDescriptor
GenerationRequestResolutionResult
GenerationRequestResolutionError
GenerationCatalogBuilder
GenerationRequestResolver
GenerationPlanCompiler
StagePlanNode
OperationKind
StageRouteChoice
```

Incorrect:

```text
GenerationRequestData
GenerationRequestResolveInfo
GenerationRequestExceptionInfo
GenerationCatalogFactoryBuilderManager
GenerationRequestCompiler
GenerationPlanResolver
StagePlanItem
OperationType
StageRouteSelectionData
```

## Definition names

Use `Definition` for reusable accepted inventory.

Definitions are not run instances.

Correct:

```text
ResourceDefinition
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
OperationDefinition
OperationImplementationDefinition
GenerationRecipeDefinition
GenerationSchemaDefinition
```

Incorrect:

```text
Resource
Stage
StageRoute
StageRouteStep
Operation
OperationImplementation
GenerationRecipe
GenerationSchema
```

The shorter names can be used in prose when the type is already clear, but public API type names should keep the suffix.

## Contract names

Use `Contract` for semantic input/output resource flow.

Correct:

```text
StageContract
OperationContract
```

Do not use `Contract` for executable scheduling, storage layout, or Unity authoring data.

Incorrect:

```text
FieldContract
JobContract
SchedulerContract
ScriptableObjectContract
```

## Descriptor names

Use `Descriptor` for symbolic input before catalog resolution.

Descriptors may contain unresolved symbols.

Correct:

```text
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
```

Incorrect:

```text
GenerationRequestInfo
GenerationRequestData
OperationImplementationOverride
```

Do not name accepted objects as descriptors.

Incorrect:

```text
GenerationRequestDescriptor // when it contains accepted definitions
```

## Request names

Use `Request` for accepted resolved run intent.

A request must not contain unresolved symbols.

Correct:

```text
GenerationRequest
```

Incorrect:

```text
GenerationRunRequest
ResolvedGenerationRequestDescriptor
GenerationRequestData
```

## Result and error names

Use `Result` for expected boundary outcomes.

Use `Error` for structured failure details.

Correct:

```text
GenerationRequestResolutionResult
GenerationRequestResolutionError
```

Incorrect:

```text
GenerationRequestResolverResponse
GenerationRequestFailureInfo
GenerationRequestProblem
```

Use exceptions for invalid API usage, not for expected descriptor-resolution failures.

## Compiler names

Use `Compiler` when accepted input is transformed into a different accepted representation.

Correct:

```text
GenerationPlanCompiler
RunnablePlanCompiler
```

Incorrect:

```text
GenerationPlanBuilder
GenerationPlanResolver
GenerationPlanProcessor
```

A builder assembles candidate inventory. A compiler transforms accepted input.

## Resolver names

Use `Resolver` when symbolic input is resolved against accepted inventory.

Correct:

```text
GenerationRequestResolver
```

Incorrect:

```text
GenerationRequestCompiler
GenerationRequestBuilder
GenerationRequestFactory
```

A resolver may return a result object when failure is expected.

## Builder names

Use `Builder` for mutable assembly objects.

Correct:

```text
GenerationCatalogBuilder
```

Incorrect:

```text
GenerationCatalogMutable
GenerationCatalogFactory
GenerationCatalogCollector
```

A builder is not accepted inventory.

## Plan names

Use `Plan` for managed semantic planning output.

Use `Node` for elements inside a plan.

Correct:

```text
GenerationPlan
StagePlanNode
OperationPlanNode
```

Incorrect:

```text
GenerationExecutionPlan
StageExecutionNode
OperationJobNode
```

Do not use execution names for managed semantic plans.

`GenerationPlan` is not a runnable plan.

## Future execution names

Use execution names only for planned execution concepts.

Correct current managed metadata names:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
ExecutionProfileSet
```

Correct planned execution names:

```text
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
FieldBinding
SchedulerBinding
FieldHandle
GenerationWorkspace
OperationScheduler
OperationScratch
```

Do not use planned execution names for current managed Runtime concepts.

Incorrect current names:

```text
ResourceDefinition contains FieldDefinition
GenerationPlan contains RunnableOperation
OperationContract contains FieldHandle
StagePlanNode contains OperationScheduler
```

## Resource and field names

Use `ResourceDefinition` for semantic generated values.

Use `FieldDefinition` for current managed field metadata that maps one `ResourceDefinition` to representation metadata. Do not use it as resource identity, catalog inventory, native storage, or a field handle.

Correct:

```text
ResourceDefinition ContinentSuitability
ResourceDefinition BaseElevation
```

Correct current field metadata:

```text
FieldDefinition BaseElevationField
```

Incorrect:

```text
ResourceFieldDefinition
FieldResourceDefinition
NativeResourceDefinition
StorageResourceDefinition
```

Do not mix semantic resource names with storage names.

## Stage names

Use stage names for coarse generation phases.

Correct:

```text
LandmassStageDefinitions.ContinentalLandmass
LandmassStageKinds.ContinentalLandmass
```

Stage names should not describe implementation details.

Incorrect:

```text
RunContinentJobs
AllocateLandmassBuffers
FloodFillContinentNativeArray
```

## Operation names

Use operation names for semantic work units.

Correct:

```text
EvaluateContinentSuitability
FormContinentCandidate
ExtractMainContinent
CompleteContinentArea
ComposeBaseElevation
```

Avoid names that encode storage, threading, or implementation detail.

Incorrect:

```text
EvaluateContinentSuitabilityJob
FormContinentCandidateNativeArray
ExtractMainContinentBurst
CompleteContinentAreaUnsafe
```

Execution implementation names may mention implementation strategy only when the strategy is part of a selectable implementation identity.

## Route-step names

Use route-step names for route-specific operation occurrences.

Route-step names should include enough context to distinguish repeated operation occurrences.

Correct:

```text
PrimaryContinentalLandmassEvaluateContinentSuitability
PrimaryContinentalLandmassFormContinentCandidate
PrimaryContinentalLandmassExtractMainContinent
```

Incorrect:

```text
Step1
Step2
Evaluate
OperationStep
```

Implementation overrides target route steps, so route-step names must be stable and specific.

## Implementation names

Use implementation names for selectable operation implementations.

For built-in defaults, use `Default` as the implementation suffix.

Correct:

```text
EvaluateContinentSuitabilityDefault
FormContinentCandidateDefault
ExtractMainContinentDefault
```

Incorrect:

```text
EvaluateContinentSuitabilityImpl
EvaluateContinentSuitabilityProcessor
EvaluateContinentSuitabilityRunner
```

If multiple implementations exist, the distinguishing word should describe the selectable behavior or algorithm.

Correct:

```text
ExtractMainContinentDefault
ExtractMainContinentDeterministicFloodFill
ExtractMainContinentUnionFind
```

## Built-in group names

Use plural group names for static built-in definition containers.

Correct:

```text
BuiltInGenerationSchemas
LandmassResourceDefinitions
LandmassStageKinds
LandmassStageDefinitions
LandmassStageContracts
LandmassOperationKinds
LandmassOperationDefinitions
LandmassOperationContracts
LandmassOperationImplementations
LandmassGenerationRecipes
```

Incorrect:

```text
BuiltInGenerationSchema
LandmassResources
LandmassStageRegistry
LandmassOperationCollection
LandmassRecipeList
```

A group name should describe the definition category it exposes.

## Catalog factory names

Use clear names for generation-module catalog helpers.

Correct:

```text
LandmassGenerationCatalog.AddTo(...)
LandmassGenerationCatalog.CreateCatalog()
```

Incorrect:

```text
LandmassCatalog.Register(...)
LandmassCatalog.Make()
LandmassCatalog.Install()
```

Use `AddTo` when adding module definitions to an existing builder.

Use `CreateCatalog` when creating a standalone catalog.

## Method naming rules

Use standard .NET-style method pairs.

| Method | Use |
| --- | --- |
| `Create` | Validate primitive input and return an accepted object or throw. |
| `TryCreate` | Validate primitive input and return success/failure. |
| `Parse` | Parse text and return a value or throw. |
| `TryParse` | Parse text and return success/failure. |
| `Build` | Create accepted immutable output from mutable builder state. |
| `Resolve` | Resolve symbolic input against accepted inventory. |
| `Compile` | Transform accepted input into another accepted representation. |
| `Contains` | Test whether an accepted object or symbol exists. |
| `Get` | Return an object or throw when missing. |
| `TryGet` | Return success/failure and an out value. |
| `Add` | Add one item to mutable builder state. |
| `AddRange` or plural `Add...s` | Add multiple items to mutable builder state. |

Prefer domain-specific clarity when plural methods read better.

Correct:

```text
AddResourceDefinition(...)
AddResourceDefinitions(...)
ContainsResourceDefinition(...)
TryGetResourceDefinition(...)
GetResourceDefinition(...)
```

Incorrect:

```text
RegisterResource(...)
PutResource(...)
FindResource(...)
MaybeGetResource(...)
```

## Property naming rules

Use nouns for properties.

Use the exact type concept in property names when ambiguity matters.

Correct:

```text
GenerationRecipeDefinition
GenerationSchemaDefinition
StageRouteStepDefinition
OperationImplementationDefinition
RequiredInputs
ProducedOutputs
StagePlanNodes
OperationPlanNodes
```

Incorrect:

```text
Recipe
Schema
Step
Implementation
Inputs
Outputs
Stages
Operations
```

Short names are acceptable only when the declaring type removes ambiguity.

## Collection naming rules

Use plural names for collections.

Use category-specific names for public API collections.

Correct:

```text
ResourceDefinitions
StageDefinitions
StageRouteDefinitions
StageRouteStepDefinitions
OperationDefinitions
OperationImplementationDefinitions
GenerationRecipeDefinitions
StagePlanNodes
OperationPlanNodes
```

Incorrect:

```text
Resources
Stages
Routes
Steps
Operations
Implementations
Recipes
Nodes
Items
```

Prefer precision over brevity for public APIs.

## Boolean naming rules

Use boolean names that read as assertions.

Correct:

```text
Succeeded
Failed
IsValid
IsReadOnly
ContainsResourceDefinition(...)
```

Incorrect:

```text
Success
Failure
Valid
Readonly
HasResource(...)
```

`Contains...` is preferred for catalog membership checks.

## Exception and error naming rules

Exception parameter names should match method parameters.

Error codes should be stable symbols.

Correct:

```text
ArgumentNullException(nameof(value))
ArgumentOutOfRangeException(nameof(width), width, ...)
lokrain.atlas.planning.recipe_not_found
```

Incorrect:

```text
ArgumentNullException("input")
ArgumentException("bad")
RecipeNotFound
```

## Namespace rules

Namespaces mirror package architecture.

Correct examples:

```text
Lokrain.Atlas.Core
Lokrain.Atlas.Core.Map
Lokrain.Atlas.Schemas
Lokrain.Atlas.Resources
Lokrain.Atlas.Stages
Lokrain.Atlas.Operations
Lokrain.Atlas.Catalog
Lokrain.Atlas.Recipes
Lokrain.Atlas.Planning
Lokrain.Atlas.Generation.Landmass
Lokrain.Atlas.Generation.Landmass.Routes
Lokrain.Atlas.Generation.Landmass.Operations
```

Do not use vague utility namespaces.

Incorrect:

```text
Lokrain.Atlas.Common
Lokrain.Atlas.Helpers
Lokrain.Atlas.Managers
Lokrain.Atlas.Data
```

## File naming rules

Use one public type per file.

The file name must match the public type name.

Correct:

```text
GenerationCatalog.cs
GenerationRequestDescriptor.cs
OperationImplementationOverrideDescriptor.cs
```

Incorrect:

```text
CatalogStuff.cs
RequestTypes.cs
PlanningModels.cs
Definitions.cs
```

Static built-in groups may use one file per group.

Correct:

```text
LandmassResourceDefinitions.cs
LandmassOperationDefinitions.cs
LandmassGenerationRecipes.cs
```

## Symbol naming rules

Symbols are stable machine-facing identity.

Use lowercase dot-separated namespaces.

Use underscores inside the final segment when multiple words are needed.

Correct:

```text
lokrain.atlas.world
lokrain.atlas.landmass.resource.continent_suitability
lokrain.atlas.landmass.operation.extract_main_continent
lokrain.atlas.landmass.route.primary_continental_landmass
```

Incorrect:

```text
Lokrain.Atlas.World
lokrain atlas world
lokrain.atlas.landmass.resource.ContinentSuitability
lokrain.atlas.landmass.resource.continent-suitability
```

Symbols must not depend on display names, Unity asset paths, scene names, object names, or localization.

## Symbol category segments

Use explicit category segments.

Correct:

```text
schema
resource
stage_kind
stage
route
route_step
operation_kind
operation
implementation
recipe
```

Examples:

```text
lokrain.atlas.landmass.resource.base_elevation
lokrain.atlas.landmass.stage.continental_landmass
lokrain.atlas.landmass.operation.compose_base_elevation
lokrain.atlas.landmass.implementation.compose_base_elevation.default
lokrain.atlas.landmass.recipe.primary_continental_landmass
```

## Display name rules

Display names are user-facing metadata.

Use title case for built-in display names.

Correct:

```text
Continent Suitability
Primary Continental Landmass
Compose Base Elevation
Atlas World Generation
```

Incorrect:

```text
continent_suitability
lokrain.atlas.landmass.resource.continent_suitability
PRIMARY CONTINENTAL LANDMASS
```

Display names must not be used for identity or lookup.

## Abbreviation rules

Avoid abbreviations unless they are standard in the domain or platform.

Correct:

```text
GenerationRequestDescriptor
OperationImplementationDefinition
```

Incorrect:

```text
GenReqDesc
OpImplDef
```

Allowed platform abbreviations may be used when standard:

```text
ECS
DOTS
API
ID
```

Prefer `Symbol` over `Id` when the value is a package-owned stable token.

## Avoid vague suffixes

Do not use vague suffixes for public API types.

Avoid:

```text
Manager
Handler
Processor
Helper
Util
Data
Info
Context
Model
Item
Entry
Wrapper
```

Use the domain role instead.

Correct:

```text
GenerationRequestResolver
GenerationPlanCompiler
GenerationCatalogBuilder
GenerationRequestResolutionResult
```

Incorrect:

```text
GenerationRequestManager
GenerationPlanProcessor
GenerationCatalogHelper
GenerationRequestInfo
```

## Avoid implementation leakage

Do not include implementation technologies in semantic type names.

Incorrect current Runtime names:

```text
NativeResourceDefinition
BurstOperationDefinition
JobStageDefinition
EcsGenerationRecipe
UnsafeStageRoute
```

Use implementation-specific names only in implementation or execution layers where the technology is the actual responsibility.

Correct planned execution names:

```text
NativeFieldStorage
BurstOperationJob
EcsGenerationOutputAdapter
```

## Test naming rules

Test class names should match the type or built-in group under test.

Correct:

```text
GenerationCatalogTests
GenerationRequestDescriptorTests
LandmassResourceDefinitionsTests
```

Test method names should describe scenario and expected outcome.

Correct:

```text
Constructor_WithNullSymbol_ThrowsArgumentNullException
Resolve_WithUnknownRecipeSymbol_ReturnsRecipeNotFoundError
All_ReturnsResourceDefinitionsInDeclaredOrder
```

Incorrect:

```text
Test1
ConstructorWorks
BadInput
ResolveTest
```

## Documentation naming rules

Documentation file names should match the document job.

Correct:

```text
Architecture Rules.md
Naming Guidelines.md
Catalog Ownership Rules.md
Runtime Boundary Rules.md
Glossary.md
Implementation Plan.md
```

Incorrect:

```text
Architecture Notes.md
Random Ideas.md
Old Plan.md
Stuff To Remember.md
```

Use folder names to identify document type:

```text
Overview
Concepts
Guidelines
Reference
Future
Decisions
Plans
```

## Checklist

Before accepting a name, verify:

```text
The name identifies one responsibility.
The suffix matches the type category.
The name does not hide a boundary crossing.
The name does not describe implementation convenience.
The name does not wrap a platform concept without added Atlas semantics.
Definitions use Definition.
Descriptors use Descriptor.
Results use Result.
Errors use Error.
Compilers compile accepted input into another accepted representation.
Resolvers resolve symbolic input against accepted inventory.
Builders build accepted immutable objects from mutable assembly state.
Plans remain managed semantic plans.
Execution names are used only for execution concepts.
Symbols are lowercase, stable, and category-qualified.
Display names are user-facing metadata only.
```