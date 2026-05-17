# ADR-001 Catalog Definition and Plan Compiler Architecture

Status: Accepted  
Date: 2026-05-17

## Context

The previous package direction explored selection and occurrence objects. That model protected some invariants, but it created too many parallel object graphs before the concrete catalog, route, settings, and runnable execution models existed.

Atlas needs a simpler public architecture that preserves the invariant that accepted domain objects cannot be invalid.

## Decision

Atlas uses a catalog-definition and compiler-resolved plan-node model.

```text
GenerationRequest
  valid normalized user intent

GenerationCatalog
  immutable definitions known to the package

GenerationPlanCompiler
  resolves request selections through the catalog

GenerationPlan
  compiler-created accepted managed plan
```

Definitions are reusable catalog entries.

Requests select definition symbols.

The compiler resolves symbols into accepted plan nodes.

A failed compilation does not return a partial plan.

## Definition Types

Atlas will define:

```text
GenerationSchemaDefinition
StageKind
StageDefinition
StageRouteDefinition
OperationKind
OperationDefinition
OperationImplementationDefinition
```

## Plan Types

Atlas will define:

```text
GenerationPlan
StagePlanNode
OperationPlanNode
```

Plan nodes are compiler output. They are not authoring objects.

## Compiler Scope

The `GenerationPlanCompiler` validates:

```text
schema exists
stage definitions exist
selected stage definition kinds satisfy schema required stage kinds
stage routes exist
stage route kind matches selected stage kind
route required operation kinds exist
operation definitions exist
selected operation implementations exist
operation implementation kinds match operation definitions
```

It does not validate executable native memory, field slots, schedulers, jobs, or storage.

Those belong to `RunnablePlanCompiler`.

## Consequences

The public API is definition-driven instead of occurrence-driven.

Unity editor tooling can author requests by symbol references.

Runtime planning remains pure managed C#.

The catalog becomes the authoritative source of meaning.

## Rejected Alternatives

### Selection/Occurrence mirror graph

Rejected because it creates duplicate shapes and can confuse raw input with compiler-accepted output.

### Validator returning reports for invalid plans

Rejected because `GenerationPlan` must never represent invalid state.

### Schema directly selecting concrete stage implementations

Rejected because schemas require semantic stage kinds, not implementations.
