# Catalog, recipe, request, and plan model

This article explains how Lokrain.Atlas moves from reusable generation definitions to one accepted managed generation plan.

Current Runtime architecture ends at `GenerationPlan`.

Runnable execution, native storage, scheduler ownership, jobs, artifacts, and ECS integration are planned architecture and are not part of the current model.

## Model summary

Lokrain.Atlas separates reusable inventory from per-run intent.

Reusable inventory is stored in a `GenerationCatalog`.

A `GenerationRecipeDefinition` selects reusable routes, contracts, and default implementation choices.

A `GenerationRequestDescriptor` describes caller intent with symbols.

A `GenerationRequestResolver` resolves the descriptor against a catalog.

A `GenerationRequest` is accepted resolved intent for one run.

A `GenerationPlanCompiler` compiles the request into a managed `GenerationPlan`.

```text
GenerationCatalog
  + GenerationRequestDescriptor
    -> GenerationRequestResolver
    -> GenerationRequestResolutionResult
    -> GenerationRequest
    -> GenerationPlanCompiler
    -> GenerationPlan
```

## GenerationCatalog

`GenerationCatalog` is immutable accepted inventory.

It owns accepted definition instances and validates graph consistency.

A catalog contains reusable definitions and contracts:

```text
GenerationSchemaDefinition
ResourceDefinition
StageKind
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract
OperationKind
OperationDefinition
OperationContract
OperationImplementationDefinition
GenerationRecipeDefinition
StageRouteChoice
StageRouteStepImplementationChoice
```

A catalog does not represent one generation run.

A catalog does not contain:

```text
Grid
Seed
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native storage
field handles
job handles
scheduler state
```

## Catalog ownership

Catalog ownership is reference-exact.

A definition belongs to a catalog only when that exact instance is owned by the catalog.

Symbol-equivalent definitions are not interchangeable.

Correct:

```text
Catalog owns ResourceDefinition instance A.

OperationContract.RequiredInputs contains instance A.
```

Incorrect:

```text
Catalog owns ResourceDefinition instance A.

OperationContract.RequiredInputs contains ResourceDefinition instance B.

A.Symbol == B.Symbol.
```

Instance B is not catalog-owned.

## GenerationCatalogBuilder

`GenerationCatalogBuilder` is mutable assembly state.

Use it to collect candidate definitions.

Call `Build` to create an immutable validated `GenerationCatalog`.

```text
GenerationCatalogBuilder
  AddGenerationSchemaDefinition(...)
  AddResourceDefinition(...)
  AddStageDefinition(...)
  AddOperationDefinition(...)
  AddGenerationRecipeDefinition(...)
  Build()
```

The builder is not accepted inventory.

The catalog is accepted inventory.

Mutating a builder after building a catalog must not mutate the catalog.

## GenerationRecipeDefinition

`GenerationRecipeDefinition` is a reusable generation template.

A recipe selects:

```text
generation schema
stage route choices
default route-step implementation choices
```

A recipe does not describe one generation run.

A recipe does not contain:

```text
Grid
Seed
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native storage
job scheduling
```

The same recipe can be used for many runs.

## StageRouteChoice

`StageRouteChoice` selects the route and stage contract used for one stage in a recipe.

It binds:

```text
StageDefinition
StageRouteDefinition
StageContract
```

The selected route must belong to the selected stage.

The selected contract must belong to the selected stage.

## StageRouteStepImplementationChoice

`StageRouteStepImplementationChoice` selects the operation, contract, and implementation for one route-step occurrence.

It binds:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

The route step identifies the operation definition by symbol.

The selected operation contract must belong to the selected operation definition.

The selected implementation definition must belong to the selected operation definition.

## Route-step occurrence identity

Route steps have their own symbols.

Implementation choices and implementation overrides target route-step symbols, not operation symbols.

This allows the same operation definition to appear multiple times in one route.

Correct:

```text
RouteStep A -> Operation X -> Implementation Fast
RouteStep B -> Operation X -> Implementation Accurate
```

Incorrect:

```text
Operation X -> Implementation Fast
```

The incorrect model cannot distinguish repeated operation occurrences.

## Resource flow

Stage and operation contracts use `ResourceDefinition`.

Contracts describe semantic resource flow.

Contracts do not describe storage.

Example operation flow:

```text
EvaluateContinentSuitability
  produces ContinentSuitability

FormContinentCandidate
  requires ContinentSuitability
  produces ContinentCandidate

ExtractMainContinent
  requires ContinentCandidate
  produces MainContinent
```

A selected route is valid only when each operation’s required inputs are available before the operation runs and the stage’s produced outputs are available after the route completes.

## GenerationRequestDescriptor

`GenerationRequestDescriptor` is symbolic caller intent.

It contains:

```text
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

A descriptor can be structurally valid before it is resolved against a catalog.

A descriptor may reference symbols that are not present in a specific catalog.

A descriptor is not an accepted request.

## OperationImplementationOverrideDescriptor

`OperationImplementationOverrideDescriptor` is a symbolic override for one selected route step.

It contains:

```text
StageRouteStepDefinitionSymbol
OperationImplementationDefinitionSymbol
```

The override target is a route-step symbol.

The override value is an operation-implementation symbol.

The resolver validates whether the override can be applied to the selected recipe and catalog.

## GenerationRequestResolver

`GenerationRequestResolver` resolves symbolic caller intent into accepted run intent.

It uses:

```text
GenerationCatalog
GenerationRequestDescriptor
```

It produces:

```text
GenerationRequestResolutionResult
```

The resolver owns descriptor satisfiability.

It checks:

```text
the recipe symbol exists in the catalog
override route-step symbols are selected by the recipe
override implementation symbols exist in the catalog
override implementations belong to the operation required by the target route step
final implementation choices are valid for the recipe
```

Resolver failures are expected boundary failures.

They are returned as structured resolution errors, not thrown as exceptions.

## GenerationRequestResolutionResult

`GenerationRequestResolutionResult` represents request-resolution output.

On success:

```text
Succeeded == true
Failed == false
GenerationRequest != null
Errors is empty
```

On failure:

```text
Succeeded == false
Failed == true
GenerationRequest == null
Errors is not empty
```

Resolution errors use stable error-code symbols.

## GenerationRequest

`GenerationRequest` is accepted resolved generation intent for one run.

It contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
StageRouteStepImplementationChoice list
```

A request contains accepted definitions.

A request contains final implementation choices.

A request contains no unresolved symbols.

A request is still managed semantic input. It does not allocate native storage or execute work.

## GenerationRunSettings

`GenerationRunSettings` contains run-specific settings.

Current run settings contain:

```text
Grid
Seed
```

Run settings belong to descriptors, requests, and plans.

Run settings do not belong to reusable definitions or recipes.

Correct:

```text
GenerationRequestDescriptor -> GenerationRunSettings
GenerationRequest -> GenerationRunSettings
GenerationPlan -> GenerationRunSettings
```

Incorrect:

```text
GenerationRecipeDefinition -> GenerationRunSettings
StageDefinition -> Grid
OperationDefinition -> Seed
```

## GenerationPlanCompiler

`GenerationPlanCompiler` converts an accepted `GenerationRequest` into a managed `GenerationPlan`.

The compiler owns managed plan construction.

It creates:

```text
StagePlanNode
OperationPlanNode
```

It preserves route-step operation order.

It orders stage plan nodes by semantic dependencies.

Independent stages use recipe order as the stable tie-breaker.

The compiler does not resolve descriptor symbols. Resolution already happened before request construction.

The compiler does not allocate native storage or schedule jobs.

## GenerationPlan

`GenerationPlan` is accepted managed semantic planning output.

It contains:

```text
GenerationRecipeDefinition
GenerationSchemaDefinition
GenerationRunSettings
StagePlanNode list
```

A plan contains accepted definitions and semantic order.

A plan contains no unresolved symbols.

A plan contains no executable job data.

A plan does not contain:

```text
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
NativeArray<T>
JobHandle
Entity
UnityEngine.Object
```

A generation plan is not a runnable plan.

## StagePlanNode

`StagePlanNode` represents one selected stage in a managed plan.

It contains:

```text
StageDefinition
StageRouteDefinition
StageContract
OperationPlanNode list
```

A stage plan node is compiler-created semantic data.

It does not execute the stage.

## OperationPlanNode

`OperationPlanNode` represents one selected operation occurrence in a managed plan.

It contains:

```text
StageRouteStepDefinition
OperationDefinition
OperationContract
OperationImplementationDefinition
```

An operation plan node is compiler-created semantic data.

It does not execute the operation.

## End-to-end flow

The current managed flow is:

```text
1. Build or obtain a GenerationCatalog.
2. Create a GenerationRequestDescriptor.
3. Resolve the descriptor with GenerationRequestResolver.
4. Inspect GenerationRequestResolutionResult.
5. Use the accepted GenerationRequest on success.
6. Compile the request with GenerationPlanCompiler.
7. Use the GenerationPlan as managed semantic planning output.
```

Example:

```text
GenerationCatalog catalog =
  LandmassGenerationCatalog.CreateCatalog();

GenerationRequestDescriptor descriptor =
  LandmassGenerationRequests.CreatePrimaryContinentalLandmass(grid, seed);

GenerationRequestResolutionResult result =
  resolver.Resolve(catalog, descriptor);

GenerationRequest request =
  result.GenerationRequest;

GenerationPlan plan =
  compiler.Compile(request);
```

## Failure boundaries

Invalid API usage throws.

Expected descriptor-resolution failure returns a result object.

Catalog construction failure throws.

Examples:

| Scenario | Boundary |
| --- | --- |
| Null required constructor argument | Exception |
| Invalid symbol text in `Create` | Exception |
| Unknown recipe symbol during resolution | Failed result |
| Unknown override implementation symbol | Failed result |
| Invalid catalog graph | Exception |
| Invalid final request choices | Exception |

## Current and planned boundary

Current architecture stops here:

```text
GenerationPlan
```

Planned execution starts after this point:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> RunnablePlan
  -> GenerationWorkspace
  -> OperationScheduler
  -> Jobs
```

Do not add planned execution responsibilities to current catalog, recipe, request, or plan objects.

## Summary

`GenerationCatalog` owns reusable accepted inventory.

`GenerationRecipeDefinition` defines reusable generation templates.

`GenerationRequestDescriptor` describes symbolic caller intent.

`GenerationRequestResolver` resolves descriptors against catalogs.

`GenerationRequest` represents accepted run intent.

`GenerationPlanCompiler` creates managed semantic plans.

`GenerationPlan` is the current Runtime endpoint.

Execution is planned after `GenerationPlan`.