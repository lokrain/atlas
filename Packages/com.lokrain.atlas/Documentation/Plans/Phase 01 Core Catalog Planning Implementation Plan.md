# Phase 01 Core Catalog Planning Implementation Plan

Package: `com.lokrain.atlas`

## Goal

Implement the managed architecture spine of Atlas:

```text
Core primitives
Catalog definitions
Generation request
Catalog builder
Plan compiler
Accepted generation plan
Built-in Earth/Landmass/PrimaryContinent vocabulary
```

No execution, jobs, fields, workspace, artifacts, or editor tooling are included in this phase.

## Non-Goals

Do not implement:

```text
RunnablePlanCompiler
RunnablePlan
Unity jobs
Burst code
NativeArray ownership
field memory layout
artifact serialization
ScriptableObject authoring
settings compiler
SymbolId
binary plan format
```

## File Order

### Step 1 — Core primitives

```text
Runtime/Core/Symbol.cs
Runtime/Core/DisplayName.cs
Runtime/Core/Map/Cell.cs
Runtime/Core/Map/CellIndex.cs
Runtime/Core/Map/Grid.cs
Runtime/Core/Map/Seed.cs
```

Tests:

```text
SymbolTests
DisplayNameTests
GridTests
SeedTests
```

### Step 2 — Definition primitives

```text
Runtime/Schemas/GenerationSchemaDefinition.cs
Runtime/Stages/StageKind.cs
Runtime/Stages/StageDefinition.cs
Runtime/Stages/StageRouteDefinition.cs
Runtime/Operations/OperationKind.cs
Runtime/Operations/OperationDefinition.cs
Runtime/Operations/OperationImplementationDefinition.cs
```

Tests:

```text
GenerationSchemaDefinitionTests
StageDefinitionTests
StageRouteDefinitionTests
OperationDefinitionTests
OperationImplementationDefinitionTests
```

### Step 3 — Catalog

```text
Runtime/Catalog/GenerationCatalog.cs
Runtime/Catalog/GenerationCatalogBuilder.cs
```

Tests:

```text
GenerationCatalogBuilderRejectsDuplicateSymbols
GenerationCatalogResolvesDefinitions
GenerationCatalogBuildOrderIsDeterministic
```

### Step 4 — Request and selections

```text
Runtime/Planning/GenerationRequest.cs
Runtime/Planning/StageDefinitionSelection.cs
Runtime/Planning/OperationImplementationSelection.cs
```

Tests:

```text
GenerationRequestNormalizesMetadata
GenerationRequestKeepsIdAndDisplayNameOutOfDeterminism
StageDefinitionSelectionRejectsInvalidSymbols
OperationImplementationSelectionRejectsInvalidSymbols
```

### Step 5 — Plan compiler result

```text
Runtime/Planning/GenerationPlanCompilerError.cs
Runtime/Planning/GenerationPlanCompilerResult.cs
```

Tests:

```text
CompilerResultSuccessHasPlanAndNoErrors
CompilerResultFailureHasErrorsAndNoPlan
CompilerResultRejectsInvalidConstructionStates
```

### Step 6 — Accepted plan nodes

```text
Runtime/Planning/GenerationPlan.cs
Runtime/Planning/StagePlanNode.cs
Runtime/Planning/OperationPlanNode.cs
```

Constructors are internal where needed so only the compiler can create accepted plans.

Tests:

```text
GenerationPlanCannotBePartiallyCreatedByPublicApi
StagePlanNodeCopiesCollections
OperationPlanNodePreservesResolvedDefinitions
```

### Step 7 — Plan compiler

```text
Runtime/Planning/GenerationPlanCompiler.cs
```

Tests:

```text
CompilerAcceptsEarthLandmassPrimaryContinentPlan
CompilerRejectsUnknownSchema
CompilerRejectsUnknownStageDefinition
CompilerRejectsStageKindMismatch
CompilerRejectsUnknownRoute
CompilerRejectsRouteKindMismatch
CompilerRejectsUnknownOperationDefinition
CompilerRejectsUnknownOperationImplementation
CompilerRejectsImplementationKindMismatch
```

### Step 8 — Built-in vocabulary

```text
Runtime/Generation/Landmass/StageKind.cs
Runtime/Generation/Landmass/OperationKinds.cs
Runtime/Generation/Landmass/StageDefinitions.cs
Runtime/Generation/Landmass/Routes/PrimaryContinent.cs
Runtime/Generation/Landmass/OperationDefinitions/EvaluateContinentSuitability.cs
Runtime/Generation/Landmass/OperationDefinitions/FormContinentCandidate.cs
Runtime/Generation/Landmass/OperationDefinitions/PreserveMainContinent.cs
Runtime/Generation/Landmass/OperationDefinitions/CompleteContinentArea.cs
Runtime/Generation/Landmass/OperationDefinitions/ComposeBaseElevation.cs
Runtime/Schemas/BuiltInSchemas.cs
```

Tests:

```text
BuiltInEarthRequiresLandmass
PrimaryContinentRouteOwnsLandmassOperationChain
LandmassVocabularyDoesNotLiveInGenericInfrastructure
```

## Completion Criteria

Phase 01 is complete when:

```text
All core primitives are tested.
Built-in Earth schema compiles with selected Landmass PrimaryContinent stage definition.
No execution dependencies exist in Core/Catalog/Planning.
No invalid GenerationPlan can be constructed through public API.
Catalog rejects duplicate definitions.
Plan compiler returns structured errors for invalid authored selections.
```
