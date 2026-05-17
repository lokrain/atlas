# Phase 01 Core, Catalog, Recipes, and Planning Implementation Plan

## Scope

Phase 01 establishes the managed generation model:

```text
Core primitives
schemas
stage definitions
operation definitions
catalog inventory
recipe definitions
symbolic request descriptors
request resolution
accepted requests
managed plan compilation
Landmass built-in recipe
```

Phase 01 does not implement:

```text
field definitions
artifact definitions
workspace allocation
Burst jobs
ECS/DOTS execution
runnable plan compiler
binary serialization
Unity editor authoring UI
```

## Current Target Flow

```text
GenerationRequestDescriptor
  -> GenerationRequestResolver + GenerationCatalog
  -> GenerationRequest
  -> GenerationPlanCompiler
  -> GenerationPlan
```

## File Order

### Core

```text
Runtime/Core/Symbol.cs
Runtime/Core/DisplayName.cs
Runtime/Core/Map/Cell.cs
Runtime/Core/Map/CellIndex.cs
Runtime/Core/Map/Grid.cs
Runtime/Core/Map/Seed.cs
```

### Schemas

```text
Runtime/Schemas/GenerationSchemaDefinition.cs
Runtime/Schemas/BuiltInGenerationSchemas.cs
```

### Stages

```text
Runtime/Stages/StageKind.cs
Runtime/Stages/StageDefinition.cs
Runtime/Stages/StageRouteStepDefinition.cs
Runtime/Stages/StageRouteDefinition.cs
Runtime/Stages/StageContract.cs
```

### Operations

```text
Runtime/Operations/OperationKind.cs
Runtime/Operations/OperationDefinition.cs
Runtime/Operations/OperationImplementationDefinition.cs
Runtime/Operations/OperationContract.cs
```

### Recipes

```text
Runtime/Recipes/StageRouteChoice.cs
Runtime/Recipes/StageRouteStepImplementationChoice.cs
Runtime/Recipes/GenerationRecipeDefinition.cs
```

### Catalog

```text
Runtime/Catalog/GenerationCatalog.cs
Runtime/Catalog/GenerationCatalogBuilder.cs
```

### Planning

```text
Runtime/Planning/GenerationRunSettings.cs
Runtime/Planning/OperationImplementationOverrideDescriptor.cs
Runtime/Planning/GenerationRequestDescriptor.cs
Runtime/Planning/GenerationRequestResolutionError.cs
Runtime/Planning/GenerationRequestResolutionResult.cs
Runtime/Planning/GenerationRequestResolver.cs
Runtime/Planning/GenerationRequest.cs
Runtime/Planning/OperationPlanNode.cs
Runtime/Planning/StagePlanNode.cs
Runtime/Planning/GenerationPlan.cs
Runtime/Planning/GenerationPlanCompiler.cs
```

### Landmass

```text
Runtime/Generation/Landmass/LandmassStageKinds.cs
Runtime/Generation/Landmass/LandmassOperationKinds.cs
Runtime/Generation/Landmass/LandmassStageDefinitions.cs
Runtime/Generation/Landmass/LandmassResourceSymbols.cs
Runtime/Generation/Landmass/LandmassStageContracts.cs
Runtime/Generation/Landmass/Operations/LandmassOperationDefinitions.cs
Runtime/Generation/Landmass/Operations/LandmassOperationContracts.cs
Runtime/Generation/Landmass/Operations/LandmassOperationImplementations.cs
Runtime/Generation/Landmass/Routes/LandmassStageRouteSteps.cs
Runtime/Generation/Landmass/Routes/LandmassStageRoutes.cs
Runtime/Generation/Landmass/LandmassGenerationRecipes.cs
Runtime/Generation/Landmass/LandmassGenerationCatalog.cs
Runtime/Generation/Landmass/LandmassGenerationRequests.cs
```

## Files That Must Not Exist

Old files from the previous architecture should be removed:

```text
Runtime/Planning/GenerationPlanCompilerResult.cs
Runtime/Planning/GenerationPlanCompilerError.cs
Runtime/Planning/StageDefinitionSelection.cs
Runtime/Planning/StageRouteSelection.cs
Runtime/Planning/OperationImplementationSelection.cs
```

## Expected Tests

### Core

```text
Symbol validation
DisplayName validation
Grid coordinate/index validation
Seed parse/format
```

### Catalog

```text
duplicate symbols rejected
foreign references rejected
route contract incompatibility rejected
recipe foreign references rejected
```

### Recipes

```text
recipe requires implementation choice for every route step
recipe rejects implementation choices outside selected routes
recipe validates route contracts
recipe validates stage dependency satisfiability
```

### Request Resolution

```text
known recipe descriptor resolves
missing recipe returns recipe_not_found
unknown override route step returns route_step_not_selected_by_recipe
unknown implementation returns implementation_not_found
mismatched implementation returns implementation_operation_mismatch
override changes final implementation choice
```

### Plan Compiler

```text
accepted request compiles
compiler uses final request implementation choices
stage route choices are dependency ordered
Landmass recipe emits expected stage and operation plan nodes
```

## Rejected Shortcuts

Do not make `GenerationRequest` carry unresolved symbols.

Do not make `GenerationPlanCompiler` accept a catalog.

Do not make `GenerationCatalog` resolve descriptors.

Do not introduce UnityEngine, Burst, Collections, Jobs, Mathematics, or Entities into the current Runtime assembly.

Do not introduce field/artifact/workspace/job concepts before the managed planning architecture is stable.

## Completion Criteria

Phase 01 is complete when:

```text
Runtime compiles with no Unity/package references in Lokrain.Atlas.asmdef
old planning files are gone
Landmass catalog builds
Primary Continental Landmass descriptor resolves through catalog
accepted request compiles into expected managed plan
negative resolver tests return structured errors
all docs match descriptor -> resolver -> accepted request -> compiler architecture
```
