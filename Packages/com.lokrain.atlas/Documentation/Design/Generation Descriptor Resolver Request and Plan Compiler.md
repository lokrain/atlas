# Generation Descriptor, Resolver, Request, and Plan Compiler

## Purpose

This specification defines how symbolic generation intent becomes an accepted request and then a managed generation plan.

The design preserves two requirements:

```text
callers can select generation by stable symbols
accepted domain objects are always valid
```

## Ownership

### GenerationRequestDescriptor

Owned by Planning.

Represents unresolved symbolic intent.

Contains:

```text
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

### GenerationCatalog

Owned by Catalog.

Represents accepted definition and recipe inventory.

Contains schemas, recipes, stages, routes, route steps, operations, implementations, and contracts.

### GenerationRequestResolver

Owned by Planning.

Resolves descriptors through a catalog.

Produces accepted requests or structured resolution errors.

### GenerationRequest

Owned by Planning.

Represents accepted resolved intent.

Contains recipe, run settings, and final implementation choices.

### GenerationPlanCompiler

Owned by Planning.

Transforms accepted requests into managed plans.

## Data Model

### Descriptor

```text
GenerationRequestDescriptor
  GenerationRecipeDefinitionSymbol
  GenerationRunSettings
  OperationImplementationOverrides
```

### Override Descriptor

```text
OperationImplementationOverrideDescriptor
  StageRouteStepDefinitionSymbol
  OperationImplementationDefinitionSymbol
```

### Resolver Result

```text
GenerationRequestResolutionResult
  Succeeded
  Failed
  GenerationRequest?
  Errors
```

### Accepted Request

```text
GenerationRequest
  GenerationRecipeDefinition
  GenerationRunSettings
  StageRouteStepImplementationChoices
```

### Managed Plan

```text
GenerationPlan
  GenerationRecipeDefinition
  GenerationRunSettings
  StagePlanNodes
```

## Resolution Behavior

`GenerationRequestResolver.Resolve(catalog, descriptor)` performs this flow:

```text
1. Look up descriptor.GenerationRecipeDefinitionSymbol in catalog.
2. If missing, return recipe_not_found error.
3. Copy recipe default implementation choices.
4. For each operation implementation override:
   a. verify route step belongs to recipe
   b. look up implementation in catalog
   c. verify implementation belongs to the route-step operation
   d. replace final choice for that route step
5. Construct accepted GenerationRequest.
6. Return success result.
```

The resolver does not compile a plan.

The catalog does not resolve the descriptor by itself.

## Resolution Error Codes

Current resolver errors use stable symbols:

```text
lokrain.atlas.planning.recipe_not_found
lokrain.atlas.planning.route_step_not_selected_by_recipe
lokrain.atlas.planning.implementation_not_found
lokrain.atlas.planning.implementation_operation_mismatch
```

## Request Validation

`GenerationRequest` verifies:

```text
recipe is non-null
run settings are non-null
implementation choices are non-null
every recipe route step has exactly one final implementation choice
no duplicate route-step choices
choices belong to the recipe schema
choices reference route steps selected by the recipe
route operation contracts satisfy stage contracts
```

A constructed `GenerationRequest` is accepted and valid.

## Compilation Behavior

`GenerationPlanCompiler.Compile(request)` performs this flow:

```text
1. Reject null request.
2. Order recipe stage-route choices by stage contract dependencies.
3. Use request final implementation choices.
4. Build OperationPlanNode values for each route step.
5. Build StagePlanNode values for each stage route.
6. Construct GenerationPlan.
```

The compiler does not:

```text
query catalogs
resolve symbols
produce resolution errors
schedule jobs
allocate native containers
create executable bindings
reference Unity runtime objects
```

## Stage Ordering

The compiler orders selected stages by stage contract dependencies.

Algorithm:

```text
available = empty set
remaining = recipe stage route choices in recipe order
ordered = empty list

while remaining is not empty:
  for each remaining stage in recipe order:
    if every required input is in available:
      move stage to ordered
      add produced outputs to available

  if no stage moved:
    throw invariant exception
```

Recipe construction also validates stage dependency satisfiability, so the compiler failure path should only occur for impossible invariant bugs.

## Failure Behavior

Expected descriptor/catalog failures return `GenerationRequestResolutionResult.Failure`.

Invalid API usage throws exceptions.

Accepted request compilation should not return a normal failure result.

## Testing Requirements

Tests should verify:

```text
descriptor resolves through Landmass catalog
missing recipe returns resolution error
unknown override route step returns resolution error
unknown implementation returns resolution error
implementation/operation mismatch returns resolution error
override changes final request implementation choice
compiler uses request final implementation choices
compiler emits expected Landmass plan shape
```

## Open Questions

Recipe-specific parameter/settings descriptors are deferred.

Runnable plan compilation is deferred.

Field and artifact resource definitions are deferred.
