# ADR-001 Descriptor, Resolver, Accepted Request, and Plan Compiler Architecture

Status: Accepted  
Date: 2026-05-17

## Context

Atlas requires symbolic generation intent. A caller must be able to say:

```text
generate recipe symbol X
using implementation symbol Y for route-step symbol Z
with these run settings
```

At the same time, Atlas has the rule that accepted domain objects must be valid.

The earlier model made `GenerationRequest` carry unresolved symbols and used `GenerationPlanCompiler` to resolve those symbols through `GenerationCatalog`. That made the request object ambiguous: it was a valid object structurally, but could still be unsatisfied by a catalog.

That contradicted the intended invariant.

## Decision

Atlas separates unresolved symbolic input from accepted generation intent.

The current flow is:

```text
GenerationRequestDescriptor
  valid symbolic descriptor

GenerationCatalog
  accepted definition and recipe inventory

GenerationRequestResolver
  resolves descriptor symbols through a catalog

GenerationRequestResolutionResult
  accepted request or structured resolution errors

GenerationRequest
  accepted resolved generation intent

GenerationPlanCompiler
  pure accepted-request transformer

GenerationPlan
  accepted managed plan
```

`GenerationPlanCompiler` has the core API:

```csharp
public GenerationPlan Compile(GenerationRequest request)
```

It does not take a catalog and does not return a result wrapper for normal flow.

## Descriptor Scope

`GenerationRequestDescriptor` contains:

```text
GenerationRecipeDefinitionSymbol
GenerationRunSettings
OperationImplementationOverrideDescriptor list
```

It is symbolic input for user code, editor tooling, importers, JSON descriptors, or higher-level APIs.

A descriptor may be unsatisfied by a catalog. That does not make the descriptor invalid. It means resolution fails.

## Catalog Scope

`GenerationCatalog` is an immutable accepted inventory of:

```text
schemas
recipes
stages
stage routes
stage route steps
stage contracts
operations
operation implementations
operation contracts
```

It validates definition ownership and recipe ownership. It does not resolve request descriptors itself.

## Resolver Scope

`GenerationRequestResolver` is the boundary between symbolic descriptors and accepted requests.

It validates descriptor/catalog satisfiability:

```text
recipe symbol exists
implementation override route step belongs to the selected recipe
implementation symbol exists
implementation belongs to the route-step operation
final implementation choices satisfy the recipe route steps
```

Failures are returned as `GenerationRequestResolutionError` values inside `GenerationRequestResolutionResult`.

## Request Scope

`GenerationRequest` contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
final resolved StageRouteStepImplementationChoice list
```

It is accepted and valid by construction.

The final implementation choices may be recipe defaults or descriptor-overridden choices.

## Compiler Scope

`GenerationPlanCompiler` transforms an accepted request into a `GenerationPlan`.

It does not:

```text
resolve symbols
query catalogs
return normal validation errors
create executable bindings
schedule jobs
allocate native containers
reference Unity runtime objects
```

It may throw only for invalid API usage or impossible invariant bugs.

## Consequences

The model preserves symbolic caller intent without weakening accepted domain objects.

Descriptor resolution errors occur before plan compilation.

`GenerationPlan` never represents partial or unresolved state.

Tests should cover descriptor resolution, override resolution, accepted request construction, and accepted plan compilation separately.

## Rejected Alternatives

### Raw symbol `GenerationRequest`

Rejected because it made unresolved symbolic input look like an accepted generation request.

### `GenerationPlanCompiler.Compile(catalog, request)`

Rejected because it made the compiler both resolver and compiler.

### `GenerationCatalog.CreateRequest(...)`

Rejected because the catalog should remain inventory/discovery, not a request orchestration service.

### Removing catalog entirely

Rejected because Atlas still needs an accepted inventory for definitions, recipes, duplicate-symbol validation, ownership validation, and tooling discovery.
