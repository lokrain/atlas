# Unity Package Layout

## Purpose

This document defines the package folder and assembly layout for the current managed Atlas architecture.

## Package Root

```text
com.lokrain.atlas/
  Runtime/
  Editor/
  Tests/
  Documentation/
  Samples~/
```

## Runtime Layout

Current Runtime layout:

```text
Runtime/
  Lokrain.Atlas.asmdef

  Core/
    Symbol.cs
    DisplayName.cs
    Map/
      Cell.cs
      CellIndex.cs
      Grid.cs
      Seed.cs

  Schemas/
    GenerationSchemaDefinition.cs
    BuiltInGenerationSchemas.cs

  Stages/
    StageKind.cs
    StageDefinition.cs
    StageRouteStepDefinition.cs
    StageRouteDefinition.cs
    StageContract.cs

  Operations/
    OperationKind.cs
    OperationDefinition.cs
    OperationImplementationDefinition.cs
    OperationContract.cs

  Catalog/
    GenerationCatalog.cs
    GenerationCatalogBuilder.cs

  Recipes/
    StageRouteChoice.cs
    StageRouteStepImplementationChoice.cs
    GenerationRecipeDefinition.cs

  Planning/
    GenerationRunSettings.cs
    OperationImplementationOverrideDescriptor.cs
    GenerationRequestDescriptor.cs
    GenerationRequestResolutionError.cs
    GenerationRequestResolutionResult.cs
    GenerationRequestResolver.cs
    GenerationRequest.cs
    OperationPlanNode.cs
    StagePlanNode.cs
    GenerationPlan.cs
    GenerationPlanCompiler.cs

  Generation/
    Landmass/
      LandmassStageKinds.cs
      LandmassOperationKinds.cs
      LandmassStageDefinitions.cs
      LandmassResourceSymbols.cs
      LandmassStageContracts.cs
      LandmassGenerationCatalog.cs
      LandmassGenerationRecipes.cs
      LandmassGenerationRequests.cs
      Operations/
        LandmassOperationDefinitions.cs
        LandmassOperationContracts.cs
        LandmassOperationImplementations.cs
      Routes/
        LandmassStageRouteSteps.cs
        LandmassStageRoutes.cs
```

## Runtime Assembly

Current Runtime assembly:

```text
Lokrain.Atlas
```

Recommended asmdef boundary:

```json
{
  "name": "Lokrain.Atlas",
  "rootNamespace": "Lokrain.Atlas",
  "references": [],
  "noEngineReferences": true
}
```

The current assembly must not reference:

```text
UnityEngine
UnityEditor
Unity.Burst
Unity.Collections
Unity.Jobs
Unity.Entities
Unity.Mathematics
```

## Future Execution Layout

Execution should be introduced later in a separate assembly.

Example future layout:

```text
Runtime/Execution/
  Lokrain.Atlas.Execution.asmdef
  RunnablePlanCompiler.cs
  RunnablePlan.cs
  WorkspaceLayout.cs
  Workspace.cs
  Operation executors
  Job schedulers
  Burst jobs
```

That assembly may reference:

```text
Lokrain.Atlas
Unity.Burst
Unity.Collections
Unity.Jobs
Unity.Mathematics
Unity.Entities, if required
```

## Editor Layout

Editor code belongs under:

```text
Editor/
```

Editor code may contain:

```text
inspectors
windows
importers
ScriptableObject authoring adapters
JSON import/export tooling
```

Editor code must produce descriptors or catalog definitions. It must not become canonical runtime truth.

## Tests Layout

Suggested tests:

```text
Tests/Runtime/
  Core primitive tests
  Catalog validation tests
  Recipe validation tests
  Request descriptor tests
  Request resolver tests
  Plan compiler tests

Tests/Editor/
  Editor adapter tests
  importer/exporter tests
```

Smoke dumps may live in Editor tests while the architecture is stabilizing.

## Naming Rule

Public type names must stand without folder context.

Prefer:

```text
LandmassGenerationRecipes
LandmassOperationDefinitions
```

Avoid:

```text
Recipes
OperationDefinitions
```

inside public module namespaces.
