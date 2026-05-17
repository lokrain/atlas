# Atlas Package Architecture and Naming Rules

Package: `com.lokrain.atlas`  
Namespace root: `Lokrain.Atlas`  
Unity target: Unity 6.4  
C# target: C# 9

This document defines mandatory naming, layering, ownership, and architectural rules for Atlas. These rules protect deterministic generation, maintainable public APIs, Unity package boundaries, Burst/job safety, and the separation between authored intent, catalog definitions, compiled plans, and executable runtime data.

## 1. Core Pipeline

```text
GenerationRequest
-> GenerationCatalog
-> GenerationPlanCompiler
-> GenerationPlan
-> RunnablePlanCompiler
-> RunnablePlan
-> Execution
-> Artifacts
```

The foundational rule:

```text
Request selects symbols.
Catalog owns definitions.
Compiler resolves symbols.
Plan contains resolved nodes.
Runnable compiler resolves execution.
Jobs receive only raw data.
```

## 2. Validity Rule

Atlas domain objects must not represent invalid states.

If an instance exists, it must satisfy its local construction invariant.

Examples:

```text
Grid instance
  valid dimensions

Symbol instance
  valid stable token syntax

DisplayName instance
  valid user-facing display text

GenerationPlan instance
  compiler-accepted resolved plan
```

Invalid authored choices are compiler input. They either produce a valid compiler output or compiler errors.

Constructors throw for programmer errors. Compilers return structured errors for invalid authored selections.

## 3. Layer Ownership

### Runtime/Core

Contains only universal dependency-light primitives:

```text
Symbol
DisplayName
Map.Grid
Map.Cell
Map.CellIndex
Map.Seed
```

Forbidden in Core:

```text
UnityEngine
UnityEditor
Unity.Collections
Unity.Jobs
Burst
Entities
ScriptableObject
schemas
stages
operations
planning
execution
artifacts
```

### Runtime/Schemas

Schemas define required generation structure.

Schemas do not select concrete stage definitions. Schemas do not execute. Schemas do not own jobs, schedulers, fields, or workspace memory.

### Runtime/Stages

Stages define semantic generation phases and reusable stage definitions.

A `StageKind` is a semantic category. A `StageDefinition` is a reusable catalog entry. A `StageRouteDefinition` is a strategy for satisfying a stage kind.

### Runtime/Operations

Operations define deterministic transform contracts and implementation definitions.

An `OperationKind` is a semantic transform category. An `OperationDefinition` describes the operation contract. An `OperationImplementationDefinition` describes a named strategy that satisfies an operation kind.

### Runtime/Catalog

The catalog is immutable and authoritative.

It owns definitions:

```text
schemas
stage definitions
stage routes
operation definitions
operation implementation definitions
```

The catalog must reject duplicate symbols during construction.

Forbidden catalog APIs:

```text
Get<T>(symbol)
Register(object)
Dictionary<string, object>
global mutable catalog
reflection-first discovery
```

### Runtime/Planning

Planning resolves authored intent into accepted managed plans.

Planning owns:

```text
GenerationRequest
StageDefinitionSelection
OperationImplementationSelection
GenerationPlanCompiler
GenerationPlanCompilerResult
GenerationPlanCompilerError
GenerationPlan
StagePlanNode
OperationPlanNode
```

Planning does not allocate native memory, reference jobs, reference Burst, or execute generation.

### Runtime/Execution

Execution owns runnable plans, workspace memory, native containers, schedulers, and job requests.

Execution is the first layer allowed to depend on:

```text
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Mathematics
```

Jobs must not receive symbols, schemas, catalog definitions, strings, display names, requests, or plans.

Jobs receive only unmanaged structs, numeric IDs, fixed-point values, and native containers.

## 4. Unity Package Rules

The package uses Unity package folder boundaries:

```text
Runtime/
Editor/
Tests/Runtime/
Tests/Editor/
Documentation~/
Samples~/
```

Start with one runtime assembly definition and one runtime test assembly definition:

```text
Runtime/Lokrain.Atlas.asmdef
Tests/Runtime/Lokrain.Atlas.Tests.asmdef
```

Do not create empty assembly boundaries without dependency pressure. Keep folders ready for later asmdef splits.

## 5. Public API Naming Rules

Public API names must be explicit at call sites.

Use:

```text
StageKind
OperationKind
StageDefinition
OperationDefinition
StageRouteDefinition
OperationImplementationDefinition
GenerationSchemaDefinition
GenerationCatalog
GenerationPlan
StagePlanNode
OperationPlanNode
```

Do not use public types named only:

```text
Kind
Definition
Requirement
Occurrence
Implementation
Node
```

File names match public type names.

## 6. Symbol Rules

`Symbol` is a syntax primitive.

A symbol is not identity, not kind, and not display name.

Correct:

```text
StageKind has Symbol
OperationKind has Symbol
GenerationSchemaDefinition has Symbol
StageDefinition has Symbol
```

Incorrect:

```text
StageKind inherits Symbol
OperationKind inherits Symbol
Symbol<T>
```

## 7. DisplayName Rules

`DisplayName` is user-facing metadata.

Do not use display names for lookup, deterministic generation, catalog resolution, execution, or artifact compatibility.

## 8. Map Rules

Use terrain terminology:

```text
Width
Depth
Cell
CellIndex
Grid
Seed
```

Do not use `Height` for horizontal map dimensions. Height means elevation.

`Grid` owns bounds and conversion. `Cell` and `CellIndex` are created through `Grid`.

Jobs do not use `Grid`, `Cell`, or `CellIndex` in hot loops. Jobs use raw numeric dimensions and indexes.

## 9. Definition Model

Atlas uses catalog definitions.

### GenerationSchemaDefinition

Declares required stage kinds.

### StageDefinition

Reusable catalog entry that can satisfy a stage kind.

### StageRouteDefinition

Strategy for satisfying a stage kind. Owns the ordered operation-kind chain.

### OperationDefinition

Semantic deterministic transform contract.

### OperationImplementationDefinition

Named strategy that satisfies an operation kind.

## 10. Request, Catalog, Plan Rules

`GenerationRequest` is valid normalized authored intent.

`GenerationCatalog` is immutable known vocabulary.

`GenerationPlanCompiler` resolves a request through a catalog.

`GenerationPlan` is compiler output. It has no public constructor. A failed compilation never returns a partial plan.

## 11. Compiler Result Rules

Compiler results enforce:

```text
Succeeded:
  Plan != null
  Errors.Count == 0

Failed:
  Plan == null
  Errors.Count > 0
```

Compiler errors are specific to compiler failure. They are not a global diagnostics framework.

## 12. Settings Rules

Settings must be typed.

Forbidden:

```text
Dictionary<string, object>
object settings bag
stringly typed setting lookup
runtime casts inside jobs
```

Planning settings are managed typed objects. Execution settings are compiled unmanaged structs. The settings compiler belongs to the future `RunnablePlanCompiler` layer, not to Core or Planning.

## 13. Route Rules

A route is a first-class stage strategy.

A route is not schema, stage kind, operation kind, operation implementation, or job graph.

Example:

```text
PrimaryContinent is a Landmass route.
Earth does not know PrimaryContinent.
Landmass stage kind does not permanently mean PrimaryContinent.
```

## 14. Landmass Rules

Landmass vocabulary belongs under:

```text
Runtime/Generation/Landmass/
```

Generic infrastructure must not hardcode Landmass symbols.

## 15. Determinism Rules

Deterministic generation output must not depend on:

```text
Guid request id
DisplayName
object reference identity
dictionary iteration order
system culture
current time
UnityEngine.Random
System.Random without explicit deterministic contract
string.GetHashCode
reflection discovery order
```

Deterministic generation input includes:

```text
Grid
Seed
schema
selected definitions
settings
compiled field contracts
operation implementations
```

## 16. Collection Rules

Constructors must copy caller-provided collections.

Expose collections as `IReadOnlyList<T>` backed by private immutable arrays.

Never mutate arrays after construction.

## 17. Static State Rules

Static readonly immutable vocabulary is allowed.

Mutable global state is forbidden.

## 18. Reflection Rules

Do not use reflection scanning as the primary catalog discovery mechanism.

Use explicit catalog builder registration.

## 19. Deferred Execution Concerns

These are acknowledged but not implemented in the planning spine:

```text
SettingsCompiler
CompiledSettingsDescriptor
SymbolId
SymbolTable
RunnablePlan binary serialization
execution numeric ID tables
job contract tests
CI forbidden API checks
```

They belong to `RunnablePlanCompiler`, `Execution`, or CI tooling after the managed plan model exists.

## 20. Final Decision Test

When uncertain:

```text
Is this reusable vocabulary?
  Put it in catalog definitions.

Is this user intent?
  Put it in GenerationRequest.

Is this compiler-accepted result?
  Put it in GenerationPlan.

Is this execution-ready data?
  Put it in RunnablePlan or Execution.

Is this per-job data?
  Put it in unmanaged job requests.

Is this UI/editor authoring?
  Put it in Editor adapters.

Is this product-specific Landmass vocabulary?
  Put it under Runtime/Generation/Landmass.
```
