# Dependency rules

This document defines dependency direction rules for Lokrain.Atlas.

Dependencies must preserve architectural ownership. Lower layers define stable domain concepts. Higher layers compose, resolve, compile, execute, adapt, or test those concepts.

## Dependency direction

Allowed dependency direction:

```text
Core
  -> Schemas
  -> Resources
  -> Stages
  -> Operations
  -> Catalog
  -> Recipes
  -> Planning
  -> Fields
  -> Execution metadata
  -> Future execution
  -> Unity adapters
  -> Editor tooling
  -> Tests
```

A layer may reference layers above it in this diagram only through tests or explicitly separated adapter code.

## Core

Core contains general package-owned value objects and map primitives.

Examples:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed
```

Core may depend on:

```text
System
System.Collections.Generic
System.Globalization
```

Core must not depend on:

```text
Schemas
Resources
Stages
Operations
Catalog
Recipes
Planning
UnityEngine
UnityEditor
Burst
Jobs
Collections
ECS
```

Core types must not know generation schemas, catalogs, recipes, requests, plans, or execution infrastructure.

## Schemas

Schemas define generation families.

Examples:

```text
GenerationSchemaDefinition
BuiltInGenerationSchemas
```

Schemas may depend on:

```text
Core
```

Schemas must not depend on:

```text
Resources
Stages
Operations
Catalog
Recipes
Planning
UnityEngine
UnityEditor
Future execution
```

A schema is a low-level definition used by higher-level definitions.

## Resources

Resources define semantic generated values.

Examples:

```text
ResourceDefinition
```

Resources may depend on:

```text
Core
Schemas
```

Resources must not depend on:

```text
Stages
Operations
Catalog
Recipes
Planning
FieldDefinition
GenerationWorkspace
OperationScheduler
UnityEngine
UnityEditor
```

`ResourceDefinition` must not know storage layout, native containers, field handles, schedulers, jobs, artifacts, or Unity objects.

## Stages

Stages define stage categories, stage definitions, stage routes, route steps, and stage contracts.

Examples:

```text
StageKind
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract
```

Stages may depend on:

```text
Core
Schemas
Resources
```

Stages must not depend on:

```text
Operations
Catalog
Recipes
Planning
Future execution
UnityEngine
UnityEditor
```

Exception: `StageRouteStepDefinition` may contain an operation-definition symbol because route steps are symbolic authored occurrences. It must not reference `OperationDefinition`.

Correct:

```text
StageRouteStepDefinition.OperationDefinitionSymbol
```

Incorrect:

```text
StageRouteStepDefinition.OperationDefinition
```

## Operations

Operations define operation categories, operation definitions, operation contracts, and operation implementations.

Examples:

```text
OperationKind
OperationDefinition
OperationContract
OperationImplementationDefinition
```

Operations may depend on:

```text
Core
Schemas
Resources
```

Operations must not depend on:

```text
Stages
Catalog
Recipes
Planning
Future execution
UnityEngine
UnityEditor
```

Operation contracts use `ResourceDefinition`, not field definitions or native storage.

## Catalog

Catalog code owns accepted definition inventory and graph validation.

Examples:

```text
GenerationCatalog
GenerationCatalogBuilder
```

Catalog may depend on:

```text
Core
Schemas
Resources
Stages
Operations
Recipes
```

Catalog must not depend on:

```text
Planning
Future execution
UnityEngine
UnityEditor
```

Catalog validation may inspect definitions, contracts, routes, and recipes.

Catalog must not resolve generation request descriptors, compile plans, allocate native storage, or schedule jobs.

## Recipes

Recipes define reusable generation templates and selected route or implementation choices.

Examples:

```text
GenerationRecipeDefinition
StageRouteChoice
StageRouteStepImplementationChoice
```

Recipes may depend on:

```text
Core
Schemas
Resources
Stages
Operations
```

Recipes must not depend on:

```text
Catalog
Planning
Future execution
UnityEngine
UnityEditor
```

Recipes must be valid reusable templates independent of a specific catalog instance. Catalog ownership validation belongs to `GenerationCatalog`.

## Planning

Planning resolves descriptors and compiles managed semantic plans.

Examples:

```text
GenerationRunSettings
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
GenerationRequestResolver
GenerationRequestResolutionResult
GenerationRequestResolutionError
GenerationRequest
GenerationPlanCompiler
GenerationPlan
StagePlanNode
OperationPlanNode
```

Planning may depend on:

```text
Core
Schemas
Resources
Stages
Operations
Catalog
Recipes
```

Planning must not depend on:

```text
Future execution
UnityEngine
UnityEditor
Burst
Jobs
Collections
ECS
```

Planning may use catalog lookup and accepted definitions.

Planning must not allocate native storage, schedule jobs, bind field handles, or capture artifacts.

## Generation modules

Generation modules provide built-in generation definitions, contracts, routes, operations, recipes, request factories, and catalog helpers.

Example:

```text
Lokrain.Atlas.Generation.Landmass
```

Generation modules may depend on:

```text
Core
Schemas
Resources
Stages
Operations
Catalog
Recipes
Planning
Fields
Execution metadata
```

Generation modules must not depend on:

```text
UnityEngine
UnityEditor
Future execution
native storage
job scheduling
```

Generation module definition files must remain managed definition inventory.

Correct:

```text
LandmassResourceDefinitions
LandmassStageDefinitions
LandmassOperationDefinitions
LandmassGenerationRecipes
LandmassGenerationRequests
LandmassGenerationCatalog
```

Incorrect:

```text
LandmassNativeBuffers
LandmassJobScheduler
LandmassEcsSystem
LandmassScriptableRecipe
```

## Future execution

Future execution compiles runnable metadata, owns native storage, schedules jobs, and captures execution output.

Planned examples:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
GenerationWorkspace
OperationScheduler
```

Future execution may depend on:

```text
Core
Schemas
Resources
Stages
Operations
Catalog
Recipes
Planning
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Mathematics
ECS integration packages when isolated in execution/integration layers
```

Future execution must not push dependencies back into current managed Runtime layers.

Incorrect:

```text
ResourceDefinition depends on FieldDefinition.
GenerationPlan depends on RunnablePlan.
OperationContract depends on NativeArray<T>.
GenerationRecipeDefinition depends on OperationScheduler.
```

## Unity adapters

Unity adapters translate Unity-authored data into Atlas domain objects or display Atlas domain objects through Unity tooling.

Unity adapters may depend on:

```text
Runtime domain layers
UnityEngine
UnityEditor when editor-only
```

Unity adapters must not become canonical domain state.

Correct:

```text
ScriptableObject authoring asset creates GenerationRequestDescriptor.
Editor window displays GenerationCatalog.
Importer creates accepted definitions from external data.
```

Incorrect:

```text
ScriptableObject is the canonical GenerationRecipeDefinition.
GameObject name is the domain Symbol.
MonoBehaviour owns GenerationCatalog identity.
```

## Editor tooling

Editor tooling is editor-only.

Editor tooling may depend on:

```text
Runtime
UnityEngine
UnityEditor
```

Editor tooling must not be referenced by Runtime assemblies.

Correct:

```text
Editor assembly references Runtime assembly.
```

Incorrect:

```text
Runtime assembly references Editor assembly.
Runtime domain object references UnityEditor.
```

## Tests

Tests may depend on the code under test.

Runtime tests may depend on:

```text
Runtime assemblies
NUnit
Unity test framework
```

Tests may create invalid candidate graphs to verify rejection.

Tests must not define architecture by depending on implementation details that are intentionally private.

## Forbidden reverse dependencies

Do not introduce these dependencies:

```text
Core -> Schemas
Core -> Planning
Resources -> Catalog
Resources -> Planning
Stages -> Operations
Operations -> Stages
Definitions -> GenerationRunSettings
Definitions -> GenerationRequest
Definitions -> GenerationPlan
Catalog -> Planning
Recipes -> Catalog
Planning -> Future execution
Runtime -> Editor
Current Runtime -> UnityEngine object identity
Current Runtime -> UnityEditor
Current Runtime -> ECS execution
Current Runtime -> job scheduling
```

## Symbolic boundary exceptions

A lower layer may store a `Symbol` for a higher-layer definition when the relationship is intentionally unresolved.

Current approved example:

```text
StageRouteStepDefinition.OperationDefinitionSymbol
```

This exception is allowed because a route step is authored route metadata and operation binding happens later through recipe choices, catalog validation, and planning.

Do not replace this with a direct `OperationDefinition` dependency.

Do not add new symbolic boundary exceptions without documenting the ownership reason.

## Dependency versus ownership

A reference does not always mean ownership.

Examples:

```text
StageDefinition references GenerationSchemaDefinition.
OperationDefinition references OperationKind.
StageContract references ResourceDefinition.
GenerationRecipeDefinition references StageRouteChoice.
GenerationPlan references accepted definitions.
```

Ownership depends on the layer.

Catalog ownership is established by `GenerationCatalog`.

Run ownership is established by `GenerationRequest` and `GenerationPlan`.

Native storage ownership is planned for `GenerationWorkspace`.

## Assembly rules

Assembly definitions should enforce dependency direction.

Runtime assemblies must not reference editor assemblies.

Editor assemblies may reference runtime assemblies.

Test assemblies may reference runtime assemblies and test framework assemblies.

Future execution assemblies should be isolated from current semantic Runtime assemblies when native storage, Burst, Jobs, Collections, or ECS dependencies are introduced.

## Documentation dependency rules

Documentation must also preserve dependency direction.

Current architecture docs must not describe future execution as current Runtime dependency.

Future docs may reference current Runtime objects as inputs.

Current Runtime docs may reference future concepts only as future boundaries.

Correct:

```text
GenerationPlan is current managed semantic output.
RunnablePlanCompiler is planned future execution bridge.
```

Incorrect:

```text
GenerationPlan owns field handles.
OperationContract maps directly to NativeArray<T>.
ResourceDefinition stores FieldDefinition.
```

## Checklist

Before accepting a dependency, verify:

```text
The dependency points in the allowed direction.
The lower layer does not depend on the higher layer.
Definitions do not depend on run-specific objects.
Contracts do not depend on storage or execution objects.
Catalog does not depend on planning.
Recipes do not depend on catalog.
Planning does not depend on future execution.
Runtime does not depend on editor code.
Current managed Runtime does not depend on native execution infrastructure.
Unity object identity does not enter domain identity.
A symbolic boundary exception is documented and intentional.
Assembly references enforce the same direction as the architecture.
```