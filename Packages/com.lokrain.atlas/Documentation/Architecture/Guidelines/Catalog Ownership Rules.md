# Catalog ownership rules

This document defines catalog ownership, catalog validation, and cross-catalog reference rules for Lokrain.Atlas.

A `GenerationCatalog` is accepted immutable inventory. It owns the definition instances it exposes and validates that the definition graph is internally consistent.

## Ownership model

Catalog ownership is reference-exact.

A definition belongs to a catalog only when that exact object instance is registered in that catalog.

Symbol equality does not establish catalog ownership.

Correct:

```text
catalog
  ResourceDefinition instance A
  StageContract instance B
    ProducedOutputs
      ResourceDefinition instance A
```

Incorrect:

```text
catalog
  ResourceDefinition instance A

StageContract instance B
  ProducedOutputs
    ResourceDefinition instance C

A.Symbol == C.Symbol
```

In the incorrect example, instance C is not catalog-owned even though it has the same symbol as instance A.

## Catalog responsibilities

A catalog validates accepted inventory.

A catalog owns:

```text
definition lookup
definition uniqueness
definition ownership
schema consistency
resource ownership
stage ownership
route ownership
route-step consistency
operation ownership
implementation compatibility
contract resource ownership
recipe graph consistency
cross-catalog reference rejection
```

A catalog does not own:

```text
run settings
request descriptors
request resolution workflow
accepted request construction
managed plan compilation
native storage allocation
field binding
job scheduling
artifact capture
```

## Builder responsibilities

`GenerationCatalogBuilder` is mutable assembly state.

`GenerationCatalog` is immutable accepted inventory.

The builder may collect candidate definitions in caller-provided order.

The catalog validates the collected definitions before exposing them as accepted inventory.

Correct:

```text
GenerationCatalogBuilder
  AddGenerationSchemaDefinition(...)
  AddResourceDefinition(...)
  AddStageDefinition(...)
  Build()

GenerationCatalog
  validates inventory
  exposes read-only definitions
```

Incorrect:

```text
GenerationCatalog exposes mutable builder lists.
GenerationCatalogBuilder is treated as accepted inventory.
Mutating the builder changes an existing catalog.
```

## Uniqueness rules

A catalog must reject duplicate definition identity within the same definition category.

Symbols are category-local identity for most definition categories.

Examples of duplicate categories:

```text
GenerationSchemaDefinition.Symbol
ResourceDefinition.Symbol
StageDefinition.Symbol
StageRouteDefinition.Symbol
StageRouteStepDefinition.Symbol
OperationDefinition.Symbol
OperationImplementationDefinition.Symbol
GenerationRecipeDefinition.Symbol
```

Contract uniqueness is based on the definition that owns the contract.

Examples:

```text
StageContract.StageDefinition.Symbol
OperationContract.OperationDefinition.Symbol
```

A symbol may appear in different categories only when the model explicitly permits that category separation. Prefer distinct symbol namespaces for clarity.

## Schema consistency rules

Definitions that participate in one graph must use the same generation schema unless a type explicitly supports cross-schema composition.

Current Runtime graph validation is schema-local.

A catalog must reject definitions whose referenced schema is not catalog-owned.

Correct:

```text
catalog
  GenerationSchemaDefinition World
  ResourceDefinition Elevation -> World
  StageDefinition Landmass -> World
  OperationDefinition ComposeBaseElevation -> World
```

Incorrect:

```text
catalog
  GenerationSchemaDefinition World

ResourceDefinition Elevation -> symbol-equivalent World instance not owned by catalog
```

## Resource ownership rules

Every `ResourceDefinition` referenced by a catalog-owned contract must be catalog-owned.

This applies to:

```text
StageContract.RequiredInputs
StageContract.ProducedOutputs
OperationContract.RequiredInputs
OperationContract.ProducedOutputs
```

A resource with the same symbol as a catalog-owned resource is not sufficient.

Correct:

```text
OperationContract
  RequiredInputs
    catalog-owned ResourceDefinition ContinentSuitability
```

Incorrect:

```text
OperationContract
  RequiredInputs
    new ResourceDefinition(
      same symbol as catalog ContinentSuitability,
      same schema as catalog World)
```

## Stage ownership rules

Every stage-related object must reference catalog-owned stage definitions.

This applies to:

```text
StageRouteDefinition.StageDefinition
StageContract.StageDefinition
StageRouteChoice.StageDefinition
StageRouteChoice.StageRouteDefinition.StageDefinition
StageRouteChoice.StageContract.StageDefinition
StagePlanNode.StageDefinition
```

A stage route or contract for a symbol-equivalent but different stage instance is not catalog-owned.

## Route ownership rules

Every selected route must be catalog-owned.

A `StageRouteDefinition` must reference a catalog-owned `StageDefinition`.

A recipe stage-route choice must reference:

```text
catalog-owned StageDefinition
catalog-owned StageRouteDefinition
catalog-owned StageContract
```

The selected route and selected contract must belong to the selected stage.

Correct:

```text
StageRouteChoice
  StageDefinition: catalog-owned Landmass
  StageRouteDefinition: catalog-owned PrimaryLandmass route for Landmass
  StageContract: catalog-owned contract for Landmass
```

Incorrect:

```text
StageRouteChoice
  StageDefinition: catalog-owned Landmass
  StageRouteDefinition: route for symbol-equivalent Landmass instance not owned by catalog
  StageContract: contract for catalog-owned Landmass
```

## Route-step ownership rules

Every route step used by a catalog-owned route must be catalog-owned through its route.

Every route-step implementation choice in a recipe must target a route step selected by the recipe.

Route-step symbols identify route-step occurrences.

Implementation overrides target route-step symbols, not operation symbols.

Correct:

```text
StageRouteDefinition PrimaryLandmass
  StageRouteStepDefinition EvaluateSuitability
  StageRouteStepDefinition FormCandidate

GenerationRecipeDefinition
  selects PrimaryLandmass
  selects implementation for EvaluateSuitability
  selects implementation for FormCandidate
```

Incorrect:

```text
GenerationRecipeDefinition
  selects implementation for route step not contained by any selected route
```

## Operation ownership rules

Every operation-related object must reference catalog-owned operation definitions.

This applies to:

```text
OperationContract.OperationDefinition
OperationImplementationDefinition.OperationDefinition
StageRouteStepImplementationChoice.OperationDefinition
StageRouteStepImplementationChoice.OperationContract.OperationDefinition
StageRouteStepImplementationChoice.OperationImplementationDefinition.OperationDefinition
OperationPlanNode.OperationDefinition
```

A route step identifies its target operation by symbol. Catalog validation must ensure the referenced operation exists in the catalog.

## Implementation compatibility rules

An operation implementation definition is compatible only with its owning operation definition.

A route-step implementation choice must bind:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

The route step's operation-definition symbol must match the selected operation definition.

The selected operation contract must belong to the selected operation definition.

The selected operation implementation definition must belong to the selected operation definition.

Correct:

```text
RouteStep ExtractMainContinent -> Operation ExtractMainContinent

Choice
  RouteStep: ExtractMainContinent
  OperationDefinition: ExtractMainContinent
  OperationContract: contract for ExtractMainContinent
  OperationImplementationDefinition: implementation for ExtractMainContinent
```

Incorrect:

```text
RouteStep ExtractMainContinent -> Operation ExtractMainContinent

Choice
  OperationImplementationDefinition: implementation for EvaluateContinentSuitability
```

## Contract resource-flow rules

A stage contract declares the resources required before a stage and produced by the stage.

An operation contract declares the resources required before an operation and produced by the operation.

A selected stage route must satisfy its selected stage contract.

For a route to satisfy a stage contract:

```text
stage required inputs are initially available to the route
each operation required input must be available before that operation
each operation produced output becomes available after that operation
stage produced outputs must be available after the route completes
```

A recipe must reject selected routes whose operation flow cannot satisfy the selected stage contract.

## Recipe graph rules

A recipe is a reusable generation template.

A recipe must contain internally consistent selected route choices and route-step implementation choices.

A recipe must not select:

```text
duplicate stage definitions
duplicate stage routes
duplicate selected route steps
route-step implementation choices for unselected route steps
operation contracts for the wrong operation
operation implementations for the wrong operation
definitions from another catalog graph
definitions from another schema
```

A recipe may contain stage dependencies when one selected stage produces resources required by another selected stage.

A recipe must not contain unsatisfied stage dependencies.

## Cross-catalog rejection rules

Catalog validation must reject mixed graphs.

A mixed graph references a definition instance that was not registered in the same catalog.

Common invalid cases:

```text
ResourceDefinition references schema instance not owned by catalog.
StageDefinition references schema instance not owned by catalog.
OperationDefinition references schema instance not owned by catalog.
StageContract references resource instance not owned by catalog.
OperationContract references resource instance not owned by catalog.
StageRouteChoice references route instance not owned by catalog.
StageRouteStepImplementationChoice references operation contract not owned by catalog.
GenerationRecipeDefinition references selected definitions not owned by catalog.
```

Do not repair mixed graphs by symbol lookup during catalog validation. Reject them.

## Lookup rules

Catalog lookup returns catalog-owned instances.

Lookup by symbol must not create new definitions.

Lookup by symbol must not return symbol-equivalent external instances.

Correct:

```text
catalog.GetResourceDefinition(symbol) -> catalog-owned ResourceDefinition
```

Incorrect:

```text
catalog.GetResourceDefinition(symbol) -> new ResourceDefinition(...)
catalog.GetResourceDefinition(symbol) -> caller-provided symbol-equivalent object
```

## Contains and TryGet rules

Catalog query methods must preserve ownership semantics.

`Contains...` returns `true` only when the catalog has a definition in that category with the given symbol.

`TryGet...` returns the catalog-owned definition instance.

`Get...` returns the catalog-owned definition instance or throws when the symbol is unknown.

## Error boundary

Catalog construction is a programmer/API usage boundary.

Invalid catalog inventory should throw precise exceptions during catalog creation.

Do not use request-resolution result objects for catalog construction failures.

Expected descriptor failures belong to `GenerationRequestResolver`.

## Examples

### Valid ownership

```text
World schema instance A

ContinentSuitability resource instance B
  Schema: A

EvaluateContinentSuitability operation instance C
  Schema: A

EvaluateContinentSuitability contract instance D
  OperationDefinition: C
  ProducedOutputs: B

Catalog owns A, B, C, D.
```

### Invalid symbol-equivalent resource

```text
Catalog owns:
  World schema instance A
  ContinentSuitability resource instance B

OperationContract references:
  ContinentSuitability resource instance C

B.Symbol == C.Symbol
B.GenerationSchema == C.GenerationSchema

C is still not catalog-owned.
```

Reject this graph.

### Invalid symbol-equivalent schema

```text
Catalog owns:
  World schema instance A

ResourceDefinition references:
  World schema instance B

A.Symbol == B.Symbol

B is not catalog-owned.
```

Reject this graph.

## Checklist

Before accepting catalog-related code, verify:

```text
The catalog owns every definition instance it exposes.
Every referenced schema is catalog-owned.
Every referenced resource is catalog-owned.
Every referenced stage is catalog-owned.
Every referenced route is catalog-owned.
Every referenced operation is catalog-owned.
Every referenced operation contract is catalog-owned.
Every referenced implementation is catalog-owned.
Recipe route choices reference selected catalog-owned routes.
Recipe implementation choices target selected route steps.
Stage and operation contracts use ResourceDefinition, not raw symbol lists.
Symbol-equivalent external instances are rejected.
Lookup returns catalog-owned instances.
The builder does not leak mutable state into the catalog.
Catalog construction throws for invalid inventory.
Descriptor resolution failures are handled by the resolver, not the catalog.
```