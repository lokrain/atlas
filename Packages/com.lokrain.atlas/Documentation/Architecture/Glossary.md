# Glossary

Package: `com.lokrain.atlas`  
Namespace root: `Lokrain.Atlas`

This glossary defines canonical Atlas terminology. Code, tests, ADRs, and editor tooling must use these terms consistently.

## Core Terms

### Symbol

A stable machine-facing token.

A `Symbol` is syntax only. It is not identity, not kind, and not a display name. Domain types contain symbols to express meaning.

Examples:

```text
earth
landmass
landmass.primary_continent
compose_base_elevation
```

### DisplayName

A user-facing label used in UI, logs, inspectors, and reports.

Display names are not used for catalog lookup, deterministic generation, artifact compatibility, or execution.

### Grid

The horizontal map grid.

A grid has:

```text
Width
Depth
CellCount
```

Atlas does not use `Height` for horizontal map dimensions. Height means elevation.

### Cell

A validated map cell coordinate with `X` and `Z` components.

Cells are created through `Grid`.

### CellIndex

A validated row-major map cell index.

Cell indexes are created through `Grid`.

### Seed

The deterministic root seed for generation.

Seed is part of deterministic generation input. Request IDs and display names are not seeds.

## Definition Terms

### GenerationSchemaDefinition

A catalog-owned definition that declares required stage kinds for a family of generated maps.

Example: `earth` requires `landmass`.

### StageKind

A semantic category of generation stage.

Examples:

```text
landmass
hydrology
climate
```

### StageDefinition

A reusable catalog entry that can satisfy a stage kind.

A stage definition declares its stage kind and selected route.

### StageRouteDefinition

A strategy for satisfying a stage kind.

A route owns the ordered operation-kind chain for that strategy.

Example: `landmass.primary_continent` is a Landmass route.

### OperationKind

A semantic deterministic transform category.

Examples:

```text
evaluate_continent_suitability
form_continent_candidate
compose_base_elevation
```

### OperationDefinition

A catalog-owned definition of an operation contract.

Field access requirements will live here once the field model exists.

### OperationImplementationDefinition

A named strategy that satisfies an operation kind.

It is not a job graph in the planning layer.

## Request and Plan Terms

### GenerationRequest

A valid normalized user intent.

It contains request metadata, deterministic inputs, and symbol-based selections.

### GenerationCatalog

An immutable authoritative collection of known definitions.

The catalog resolves symbols into definitions.

### GenerationPlanCompiler

The compiler that resolves a request through a catalog and either produces an accepted plan or compiler errors.

### GenerationPlan

Compiler-created accepted managed plan.

If a `GenerationPlan` exists, it is catalog-resolved and schema-consistent.

### StagePlanNode

Compiler-resolved stage node inside a `GenerationPlan`.

### OperationPlanNode

Compiler-resolved operation node inside a `StagePlanNode`.

### RunnablePlanCompiler

Deferred execution-layer compiler that converts an accepted managed plan into executable runtime data.

### RunnablePlan

Deferred execution-ready representation.

A runnable plan may use compiled numeric IDs, unmanaged settings, field slots, native memory plans, and scheduler bindings.

## Unity Terms

### Runtime

Player-safe code under `Runtime/`.

Runtime code must not reference `UnityEditor`.

### Editor

Unity Editor tooling under `Editor/`.

Editor code may use `UnityEditor`, inspectors, windows, importers, and authoring assets.

### Authoring Adapter

A Unity-facing asset or editor object that translates user-authored data into runtime request/catalog data.

ScriptableObjects are authoring adapters, not canonical runtime state.
