# Generation Catalog and Plan Compiler

Package: `com.lokrain.atlas`  
Namespace root: `Lokrain.Atlas`

## Purpose

This document defines how Atlas resolves authored generation intent into an accepted managed plan.

The model is definition-first:

```text
Definitions are reusable catalog entries.
Requests select symbols.
The compiler resolves symbols through the catalog.
Plans contain accepted resolved nodes.
```

## Data Flow

```text
GenerationRequest
  SchemaSymbol
  StageDefinitionSelections
  OperationImplementationSelections
  Grid
  Seed
  Settings

GenerationCatalog
  GenerationSchemaDefinitions
  StageDefinitions
  StageRouteDefinitions
  OperationDefinitions
  OperationImplementationDefinitions

GenerationPlanCompiler
  validates and resolves

GenerationPlan
  SchemaDefinition
  StagePlanNodes
  OperationPlanNodes
```

## GenerationRequest

A request is valid normalized user intent.

It contains:

```text
Guid Id
DisplayName
Map.Grid
Map.Seed
SchemaSymbol
StageDefinitionSelection[]
OperationImplementationSelection[]
typed settings selections later
```

`Id` and `DisplayName` are metadata.

`Grid`, `Seed`, schema symbol, selected definitions, and settings are deterministic generation input.

## GenerationCatalog

The catalog is immutable and authoritative.

It resolves:

```text
Schema symbol -> GenerationSchemaDefinition
Stage definition symbol -> StageDefinition
Stage route symbol -> StageRouteDefinition
Operation kind -> OperationDefinition
Operation implementation symbol -> OperationImplementationDefinition
```

The catalog rejects duplicate symbols during construction.

Catalog construction must be deterministic.

## GenerationSchemaDefinition

A schema definition declares the required stage kind chain.

Example:

```text
Earth
  Symbol = earth
  RequiredStageKinds:
    landmass
```

Schemas do not select stage definitions.

## StageDefinition

A stage definition declares:

```text
Symbol
DisplayName
StageKind
StageRouteDefinition symbol
```

Example:

```text
landmass.primary_continent_stage
  StageKind = landmass
  StageRoute = landmass.primary_continent
```

## StageRouteDefinition

A stage route declares:

```text
Symbol
DisplayName
StageKind
RequiredOperationKinds
```

Example:

```text
landmass.primary_continent
  StageKind = landmass
  RequiredOperationKinds:
    evaluate_continent_suitability
    form_continent_candidate
    preserve_main_continent
    complete_continent_area
    compose_base_elevation
```

## OperationDefinition

An operation definition declares:

```text
Symbol
DisplayName
OperationKind
contract data later
```

Field reads, writes, lifetimes, and coverage will be added when the field model exists.

## OperationImplementationDefinition

An operation implementation definition declares:

```text
Symbol
DisplayName
OperationKind
settings contract later
execution binding later
```

It does not expose job graphs in the planning layer.

## Plan Compilation

The compiler validates:

```text
schema symbol resolves
selected stage definitions resolve
selected stage definitions satisfy required stage kinds in strict order
selected stage route resolves
stage route kind matches selected stage definition kind
route required operation kinds resolve to operation definitions
selected operation implementations resolve
selected operation implementation kind matches required operation kind
```

## Strict Ordering

Stage kind requirements are strict ordered requirements.

Route operation-kind requirements are strict ordered requirements.

Optional insertion and extension points are not part of the initial contract.

## Compiler Output

`GenerationPlan` contains resolved plan nodes.

```text
GenerationPlan
  Request
  SchemaDefinition
  StagePlanNode[]

StagePlanNode
  StageDefinition
  StageRouteDefinition
  OperationPlanNode[]

OperationPlanNode
  OperationDefinition
  OperationImplementationDefinition
```

A failed compilation does not return a partial `GenerationPlan`.

## Deferred to RunnablePlanCompiler

The planning compiler does not validate:

```text
field memory layout
native storage
scheduler availability
job graph correctness
Burst compatibility
workspace ownership
artifact capture
compiled settings descriptors
SymbolId tables
```

Those belong to `RunnablePlanCompiler` and Execution.
