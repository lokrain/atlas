# Dependency rules

This document defines dependency direction for Lokrain.Atlas architecture.

Dependencies must preserve ownership boundaries. A lower layer must not reference a higher layer to complete its own meaning.

## Primary rule

Dependencies flow from lower-level domain concepts toward higher-level orchestration.

Allowed direction:

```text
Core
  -> Schemas
  -> Resources
  -> Stages
  -> Operations
  -> Catalog
  -> Recipes
  -> Requests
  -> Plans
  -> Future execution
  -> Unity adapters
  -> Editor tooling
  -> Tests
````

A higher layer may reference lower-layer accepted objects.

A lower layer must not reference higher-layer orchestration objects.

## Current Runtime boundary

Current Runtime managed architecture ends at `GenerationPlan`.

Current Runtime managed planning code may depend on:

```text
System
System.Collections.Generic
System.Linq where appropriate
Lokrain.Atlas.Core
Lokrain.Atlas.Schemas
Lokrain.Atlas.Resources
Lokrain.Atlas.Stages
Lokrain.Atlas.Operations
Lokrain.Atlas.Catalog
Lokrain.Atlas.Recipes
Lokrain.Atlas.Requests
Lokrain.Atlas.Plans
Lokrain.Atlas.Generation.*
```

Current Runtime managed planning code must not depend on:

```text
UnityEditor
UnityEngine.Object ownership
MonoBehaviour
ScriptableObject as canonical domain state
GameObject
Scene
ECS World
ECS System
Unity.Collections native allocation
Unity.Jobs scheduling
Unity.Burst execution
JobHandle ownership
```

Unity-facing integration belongs in explicit adapter layers, not in core managed domain objects.

## Core dependencies

Core is the lowest package domain layer.

Core may depend on:

```text
System
System.Globalization
System.Text
```

Core must not depend on:

```text
Schemas
Resources
Stages
Operations
Catalog
Recipes
Requests
Plans
Generation modules
UnityEngine
UnityEditor
Unity.Collections
Unity.Jobs
Unity.Burst
ECS/DOTS
```

Examples of Core types:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed
```

Core types must remain reusable, deterministic, and independent from generation workflow.

## Schema dependencies

Schemas define generation-family identity.

Schema definitions may depend on:

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
Requests
Plans
Generation modules
Future execution
Unity adapters
Editor tooling
```

A schema does not know which resources, stages, operations, or recipes exist.

## Resource dependencies

Resources define semantic generated values.

`ResourceDefinition` may depend on:

```text
Core
Schemas
```

`ResourceDefinition` must not depend on:

```text
Stages
Operations
Catalog
Recipes
Requests
Plans
FieldDefinition
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
Unity objects
```

A resource identifies a generated value. It does not know who requires it, who produces it, how it is stored, or who schedules work for it.

## Stage dependencies

Stage definitions may depend on:

```text
Core
Schemas
Resources
```

Stage contracts may depend on:

```text
ResourceDefinition
```

Stage definitions and contracts must not depend on:

```text
Catalog
Recipes
Requests
Plans
Operations as execution dependencies
FieldDefinition
GenerationWorkspace
OperationScheduler
JobHandle
Native containers
Unity adapters
```

A stage can describe semantic phase metadata and resource flow. It must not know request resolution, plan compilation, storage, or scheduling.

## Operation dependencies

Operation definitions may depend on:

```text
Core
Schemas
Resources
```

Operation contracts may depend on:

```text
ResourceDefinition
```

Operation implementation definitions may depend on:

```text
Core
Schemas
Operations
```

Current operation implementation definitions are metadata. They must not depend on:

```text
Unity.Jobs
Unity.Burst
Native containers
OperationScheduler
RunnableOperation
GenerationWorkspace
JobHandle
```

A future execution binding layer may map operation implementation definitions to schedulers. The definition itself should remain managed metadata.

## Catalog dependencies

`GenerationCatalog` may depend on:

```text
Core
Schemas
Resources
Stages
Operations
Recipes
```

The catalog owns accepted definition inventory and validates graph consistency.

The catalog must not depend on:

```text
Requests
Plans
Future execution
Unity adapters
Editor tooling
Native containers
Jobs
Schedulers
```

Invalid dependencies:

```text
GenerationCatalog -> GenerationRequestDescriptor
GenerationCatalog -> GenerationRequest
GenerationCatalog -> GenerationPlan
GenerationCatalog -> GenerationWorkspace
GenerationCatalog -> OperationScheduler
```

The catalog provides lookup. It does not resolve one request or execute generation.

## Recipe dependencies

Recipes may depend on:

```text
Core
Schemas
Stages
Operations
Resources through contracts
```

Recipe definitions may contain accepted choices:

```text
StageRouteChoice
StageRouteStepImplementationChoice
```

Recipes must not depend on:

```text
Requests
Plans
Future execution
Unity adapters
Native storage
Schedulers
Jobs
```

Invalid dependencies:

```text
GenerationRecipeDefinition -> GenerationRunSettings
GenerationRecipeDefinition -> GenerationRequestDescriptor
GenerationRecipeDefinition -> GenerationRequest
GenerationRecipeDefinition -> GenerationPlan
GenerationRecipeDefinition -> NativeArray<T>
```

A recipe is reusable inventory, not one run.

## Request descriptor dependencies

Request descriptors may depend on:

```text
Core
Requests
GenerationRunSettings
```

A request descriptor may contain symbols and run settings.

A request descriptor must not depend on:

```text
Catalog
GenerationRecipeDefinition
StageRouteStepDefinition
OperationImplementationDefinition
GenerationRequest
GenerationPlan
Future execution
Native containers
Jobs
Unity scene objects
```

Invalid dependencies:

```text
GenerationRequestDescriptor -> GenerationCatalog
GenerationRequestDescriptor -> GenerationRecipeDefinition
OperationImplementationOverrideDescriptor -> StageRouteStepDefinition
OperationImplementationOverrideDescriptor -> OperationImplementationDefinition
```

Descriptors are symbolic. They must not contain accepted catalog-owned definitions.

## Resolver dependencies

`GenerationRequestResolver` may depend on:

```text
Catalog
Recipes
Requests
Stages
Operations
Core
```

The resolver bridges symbolic descriptors to accepted request objects.

The resolver may reference catalog-owned definitions because resolution is its boundary.

The resolver must not depend on:

```text
Plans
Future execution
Native containers
Schedulers
Jobs
Unity adapters
Editor tooling
```

Invalid dependencies:

```text
GenerationRequestResolver -> GenerationPlanCompiler
GenerationRequestResolver -> GenerationPlan
GenerationRequestResolver -> GenerationWorkspace
GenerationRequestResolver -> OperationScheduler
```

The resolver creates accepted requests. It does not compile plans.

## Request dependencies

`GenerationRequest` may depend on:

```text
Recipes
Requests
Core
Stages
Operations
```

It may contain accepted recipe, run settings, and final implementation choices.

`GenerationRequest` must not depend on:

```text
GenerationRequestDescriptor
GenerationRequestResolver
GenerationCatalog
Plans
Future execution
Native containers
Schedulers
Jobs
Unity adapters
```

Invalid dependencies:

```text
GenerationRequest -> GenerationRequestDescriptor
GenerationRequest -> GenerationCatalog
GenerationRequest -> GenerationPlan
GenerationRequest -> NativeArray<T>
GenerationRequest -> JobHandle
```

A request is accepted resolved intent. It does not carry unresolved input or execution state.

## Plan dependencies

`GenerationPlanCompiler` may depend on:

```text
Requests
Recipes
Stages
Operations
Plans
Core
```

`GenerationPlan` and plan nodes may depend on:

```text
Requests
Recipes
Stages
Operations
Core
```

Plan types must not depend on:

```text
Catalog for normal compilation
GenerationRequestDescriptor
GenerationRequestResolver
Future execution
Unity.Collections
Unity.Jobs
Unity.Burst
ECS systems
Unity adapters
```

Invalid dependencies:

```text
GenerationPlan -> GenerationCatalog
GenerationPlan -> GenerationRequestDescriptor
GenerationPlan -> FieldDefinition
GenerationPlan -> RunnablePlan
GenerationPlan -> GenerationWorkspace
GenerationPlan -> JobHandle
OperationPlanNode -> NativeArray<T>
```

A managed plan is semantic planning data, not executable runtime state.

## Generation module dependencies

Generation modules may depend on the managed domain layers needed to expose built-in definitions and helpers.

Example module:

```text
Lokrain.Atlas.Generation.Landmass
```

A generation module may depend on:

```text
Core
Schemas
Resources
Stages
Operations
Catalog
Recipes
Requests
Plans when it exposes plan-related helpers
```

A generation module must not bypass architecture boundaries.

Generation modules must not depend on:

```text
UnityEditor in Runtime code
Unity scene state
Native execution systems unless the module is explicitly an execution assembly
Tests
Samples
```

Correct module surfaces:

```text
LandmassResourceDefinitions
LandmassStageDefinitions
LandmassOperationDefinitions
LandmassRecipeDefinitions
LandmassCatalogs
LandmassRequestDescriptors
```

Incorrect module dependencies:

```text
LandmassResourceDefinitions -> GenerationWorkspace
LandmassRecipeDefinitions -> GenerationRunSettings defaults hidden in recipe state
LandmassRequestDescriptors -> Unity scene lookup
LandmassCatalogs -> Editor asset database
```

## Future execution dependencies

Future execution code starts after `GenerationPlan`.

Future runnable compilation may depend on:

```text
Plans
Resources
Operations
Future field definitions
Future scheduler bindings
```

Future workspace code may depend on:

```text
Future field definitions
Future runnable plan metadata
Unity.Collections
Unity.Jobs where needed for handles and safety
```

Future scheduler code may depend on:

```text
Future runnable operations
Future workspace access
Unity.Collections
Unity.Jobs
Unity.Burst-compatible job types
```

Future jobs may depend on:

```text
Unity.Collections
Unity.Jobs
Unity.Burst-compatible unmanaged data
Unity.Mathematics when needed
```

Future jobs must not depend on:

```text
Core.Symbol
DisplayName
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
ResourceDefinition
FieldDefinition as managed metadata
GenerationWorkspace
OperationScheduler
UnityEngine.Object
UnityEditor
```

Schedulers resolve execution metadata before scheduling jobs. Jobs receive native containers and unmanaged parameters only.

## Unity adapter dependencies

Unity adapters may depend on:

```text
UnityEngine
UnityEditor when inside Editor assemblies
Atlas Runtime domain objects
```

Unity adapters include:

```text
ScriptableObject authoring assets
Editor windows
importers
inspectors
MonoBehaviour integration shims
ECS integration systems
```

Unity adapters may translate Unity-authored data into Atlas descriptors or definitions.

Unity adapters must not become canonical Runtime domain state.

Correct dependency:

```text
Unity authoring asset -> Atlas descriptor or definition input
```

Incorrect dependency:

```text
Atlas ResourceDefinition -> ScriptableObject
Atlas GenerationCatalog -> AssetDatabase
Atlas GenerationRequest -> MonoBehaviour
Atlas GenerationPlan -> GameObject
```

## Editor dependencies

Editor code may depend on:

```text
UnityEditor
UnityEngine
Atlas Runtime
Editor-only validation and tooling
```

Runtime code must not depend on Editor code.

Editor code may create, inspect, validate, and serialize authoring data. It must not define core Runtime semantics.

Invalid dependency:

```text
Runtime -> Editor
Runtime -> UnityEditor
```

## Test dependencies

Test assemblies may depend on the Runtime assembly under test.

Tests may depend on test frameworks.

Runtime assemblies must not depend on tests.

Invalid dependency:

```text
Runtime -> Tests
Generation module Runtime -> Tests
```

Tests may reference internal members only through an intentional assembly-level friend relationship if the project explicitly accepts that policy. Public API behavior should be tested through public API where practical.

## Sample dependencies

Samples may depend on Runtime and Unity-facing adapters.

Runtime must not depend on samples.

Samples must not define canonical architecture semantics.

Invalid dependency:

```text
Runtime -> Samples
Architecture docs -> sample-only behavior as required architecture
```

## Assembly naming dependency rule

Assembly names should preserve dependency direction and classification.

Runtime assemblies should not end in `.Editor` or `.Tests`.

Editor assemblies must end with:

```text
.Editor
```

Test assemblies must end with:

```text
.Tests
```

When both editor and tests exist, `.Editor` comes before `.Tests` in the assembly name.

Examples:

```text
Lokrain.Atlas
Lokrain.Atlas.Editor
Lokrain.Atlas.Tests
Lokrain.Atlas.Generation.Landmass
Lokrain.Atlas.Generation.Landmass.Editor
Lokrain.Atlas.Generation.Landmass.Tests
```

Do not place `.Editor` or `.Tests` before the final domain segment unless the assembly’s classification is actually editor/test.

## Namespace dependency rule

Namespaces should reflect architecture areas.

Allowed current managed namespace direction:

```text
Lokrain.Atlas.Core
Lokrain.Atlas.Schemas
Lokrain.Atlas.Resources
Lokrain.Atlas.Stages
Lokrain.Atlas.Operations
Lokrain.Atlas.Catalog
Lokrain.Atlas.Recipes
Lokrain.Atlas.Requests
Lokrain.Atlas.Plans
Lokrain.Atlas.Generation.*
```

Avoid namespace references that imply backward dependencies.

Invalid examples:

```text
Lokrain.Atlas.Core referencing Lokrain.Atlas.Catalog
Lokrain.Atlas.Resources referencing Lokrain.Atlas.Plans
Lokrain.Atlas.Recipes referencing Lokrain.Atlas.Requests
Lokrain.Atlas.Plans referencing Lokrain.Atlas.Execution
```

## Type-reference rule

A type should reference the most precise lower-layer domain concept it needs.

Correct:

```text
OperationContract -> ResourceDefinition
GenerationRequestDescriptor -> Symbol
GenerationPlanCompiler -> GenerationRequest
```

Incorrect:

```text
OperationContract -> Symbol for resource flow
GenerationRequestDescriptor -> GenerationRecipeDefinition
GenerationPlanCompiler -> GenerationRequestDescriptor
```

Use accepted definitions where accepted definitions are required.

Use symbols where symbolic input is required.

Do not use raw strings where `Symbol` is required.

## No upward callback rule

Lower layers must not call upward through interfaces to avoid direct references.

This is invalid even when the compile-time dependency looks abstract.

Incorrect:

```text
ResourceDefinition accepts IResourceCatalogLookup
StageContract accepts IFieldAllocator
OperationDefinition accepts IOperationScheduler
GenerationPlan accepts IJobScheduler
```

Interfaces do not fix an ownership violation when the lower layer still depends on a higher-layer responsibility.

Use a higher-layer service to consume lower-layer objects instead.

## No service-locator rule

Do not use global service location to bypass dependencies.

Invalid:

```text
AtlasServices.Catalog
AtlasServices.CurrentWorkspace
AtlasServices.JobScheduler
Unity scene lookup from domain objects
static mutable current request
```

Domain objects must receive required accepted data explicitly through constructors, factories, or method parameters.

## No hidden Unity dependency rule

Runtime domain objects must not read Unity global state.

Invalid:

```text
Application.isPlaying inside domain objects
Time.frameCount for deterministic generation
Random.state for seed derivation
SceneManager lookup for request input
AssetDatabase for catalog lookup
Resources.Load for definitions
```

Unity state belongs in adapters. Adapters translate Unity state into explicit Atlas domain inputs.

## No hidden execution dependency rule

Managed planning objects must not contain execution-specific hidden state.

Invalid fields or properties in current planning objects:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
JobHandle
Entity
World
SystemHandle
FieldHandle
SchedulerBinding
Burst function pointer
```

If execution needs metadata, introduce it in future execution layers after `GenerationPlan`.

## Extension dependency rule

External or future modules must follow the same direction as built-in modules.

A module may add new accepted definitions and descriptors.

A module must not require lower core layers to know about the module.

Correct:

```text
Lokrain.Atlas.Generation.Landmass -> Lokrain.Atlas.Resources
```

Incorrect:

```text
Lokrain.Atlas.Resources -> Lokrain.Atlas.Generation.Landmass
```

The package core must not depend on a specific generation module.

## Dependency review checklist

Before accepting a dependency, verify:

```text
The dependency points from a higher layer to a lower layer.
The referenced type owns the concept being used.
The dependency does not make a reusable definition aware of one run.
The dependency does not make current managed planning aware of future execution state.
The dependency does not make Runtime code depend on Editor code.
The dependency does not make domain objects depend on Unity scene or asset state.
The dependency does not use an interface to hide an ownership violation.
The dependency does not introduce global service lookup.
The dependency does not make jobs aware of managed domain metadata.
```

## Summary

Dependency direction protects the architecture.

Core knows nothing about generation workflow.

Definitions know nothing about requests or plans.

Catalogs know definitions, not runs.

Descriptors know symbols, not accepted catalog objects.

Resolvers bridge descriptors and catalogs.

Requests contain accepted resolved intent.

Plans contain managed semantic ordering.

Future execution starts after plans.

Unity, Editor, tests, and samples are adapters or validation surfaces, not sources of domain truth.

```