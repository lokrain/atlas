# Atlas Package Architecture and Naming Rules

Package: `com.lokrain.atlas`  
Namespace root: `Lokrain.Atlas`

This document defines the mandatory naming, layering, ownership, and architectural rules for the Atlas package. These rules are not style preferences. They exist to protect deterministic generation, maintainable public APIs, Unity package boundaries, Burst/job safety, and the distinction between authored intent, catalog definitions, compiled plans, and executable runtime data.

## 1. Core Architectural Principle

Atlas is a deterministic procedural map-generation package built around this pipeline:

```text
GenerationRequest
-> GenerationCatalog
-> GenerationPlanCompiler
-> GenerationPlan
-> RunnablePlanCompiler
-> RunnablePlan
-> Execution
-> Artifacts
````

The foundational rule is:

```text
Request selects symbols.
Catalog owns definitions.
Compiler resolves symbols.
Plan contains resolved nodes.
Runnable compiler resolves execution.
Jobs receive only raw data.
```

A request is authored intent.
A catalog is known vocabulary and definitions.
A compiler proves that authored intent satisfies catalog definitions.
A plan is compiler-accepted managed data.
A runnable plan is execution-ready compiled data.
A job receives only unmanaged job requests and native containers.

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

Invalid authored choices are not represented as invalid domain objects. They are compiler input that either produces a valid output or compiler errors.

Constructors throw for programmer errors:

```text
null required reference
invalid primitive value
invalid symbol syntax
invalid display name
invalid grid dimensions
```

Compilers return structured errors for invalid authored selections:

```text
unknown schema symbol
unknown stage definition
stage kind mismatch
unknown route
operation implementation mismatch
missing required operation
```

## 3. Layering Rules

### 3.1 Runtime/Core

`Runtime/Core` contains only universal dependency-light primitives.

Allowed:

```text
Symbol
DisplayName
Map.Grid
Map.Cell
Map.CellIndex
Map.Seed
```

Forbidden in `Core`:

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

Core primitives must be usable in pure managed tests and editor tooling without Unity runtime dependencies.

### 3.2 Runtime/Schemas

Schemas define required generation structure.

Schemas do not select concrete stage definitions.
Schemas do not execute.
Schemas do not own jobs, schedulers, fields, or workspace memory.

Example:

```text
Earth schema requires StageKind landmass.
```

### 3.3 Runtime/Stages

Stages define semantic generation phases and reusable stage definitions.

A stage kind is a semantic category:

```text
landmass
hydrology
climate
```

A stage definition is a reusable catalog entry that can satisfy a stage kind.

A stage route is a strategy for satisfying a stage kind.

Example:

```text
StageKind:
  landmass

StageDefinition:
  landmass.primary_continent_stage
  Kind = landmass
  Route = landmass.primary_continent

StageRouteDefinition:
  landmass.primary_continent
  StageKind = landmass
  RequiredOperationKinds = [...]
```

### 3.4 Runtime/Operations

Operations define deterministic transform contracts and implementation definitions.

An operation kind is a semantic transform category.

An operation definition describes the operation contract.

An operation implementation definition describes a named strategy that satisfies an operation kind.

Operation implementations do not expose job graphs in the planning layer.

Example:

```text
OperationKind:
  compose_base_elevation

OperationDefinition:
  compose_base_elevation

OperationImplementationDefinition:
  compose_base_elevation.fixed_point_primary_continent
  OperationKind = compose_base_elevation
```

### 3.5 Runtime/Catalog

The catalog is immutable and authoritative.

The catalog owns known definitions:

```text
schemas
stage definitions
stage routes
operation definitions
operation implementation definitions
```

The catalog must reject duplicate symbols during construction.

The catalog must not expose generic object-bag APIs.

Allowed API shape:

```text
GetSchema(symbol)
GetStageDefinition(symbol)
GetStageRoute(symbol)
GetOperationDefinition(kind)
GetOperationImplementation(symbol)
```

Forbidden API shape:

```text
Get<T>(symbol)
Register(object)
Dictionary<string, object>
global mutable catalog
reflection-based discovery as the primary mechanism
```

### 3.6 Runtime/Planning

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

Planning does not allocate native memory.
Planning does not reference jobs.
Planning does not reference Burst.
Planning does not execute generation.

### 3.7 Runtime/Execution

Execution owns runnable plans, workspace memory, schedulers, native containers, and job requests.

Execution is the first layer allowed to depend on:

```text
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Mathematics
```

Jobs must not receive:

```text
Symbol
DisplayName
schemas
catalog definitions
GenerationRequest
GenerationPlan
managed objects
strings
```

Jobs receive only:

```text
unmanaged structs
numeric ids
fixed-point values
NativeArray / NativeList / NativeStream where appropriate
```

### 3.8 Editor

Editor tooling belongs under `Editor`.

Editor code may use:

```text
UnityEditor
UnityEngine
ScriptableObject authoring assets
inspectors
windows
importers
```

Runtime code must not depend on editor code.

ScriptableObjects are authoring adapters, not canonical runtime generation state.

## 4. Unity Package Rules

The package follows Unity package folder boundaries:

```text
Runtime/
Editor/
Tests/Runtime/
Tests/Editor/
Documentation/
```

Start with one runtime assembly definition unless dependency splits are required:

```text
Runtime/Lokrain.Atlas.asmdef
Tests/Runtime/Lokrain.Atlas.Tests.asmdef
```

Folder layout must still allow later assembly splits:

```text
Core
Schemas
Stages
Operations
Catalog
Planning
Execution
Generation
Artifacts
```

Do not create empty assembly boundaries without dependency pressure.

## 5. Naming Rules

### 5.1 Public Type Names

Public type names must be explicit and readable at call sites.

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

Short names may look clean in folders, but public Unity/C# APIs must remain clear when imported through `using` directives.

### 5.2 File Names

File names match public type names.

Use:

```text
Runtime/Stages/StageKind.cs
Runtime/Stages/StageDefinition.cs
Runtime/Operations/OperationKind.cs
Runtime/Operations/OperationDefinition.cs
Runtime/Core/Map/CellIndex.cs
```

Do not use a filename that hides the public type’s meaning.

### 5.3 Namespace Names

Namespaces mirror architectural layers.

Use:

```csharp
Lokrain.Atlas.Core
Lokrain.Atlas.Core.Map
Lokrain.Atlas.Schemas
Lokrain.Atlas.Stages
Lokrain.Atlas.Operations
Lokrain.Atlas.Catalog
Lokrain.Atlas.Planning
Lokrain.Atlas.Generation.Landmass
```

Do not place stage, operation, schema, catalog, or planning concepts under `Lokrain.Atlas.Core`.

### 5.4 Symbols

`Symbol` is a syntax primitive.

A symbol is not identity.
A symbol is not kind.
A symbol is not a display name.

Domain concepts contain symbols.

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

### 5.5 Display Names

`DisplayName` is a user-facing metadata primitive.

Use it for names shown in UI, logs, inspectors, and reports.

Do not use display names for identity, lookup, deterministic generation, catalog resolution, or artifact compatibility.

## 6. Map Naming Rules

Use terrain terminology.

Use:

```text
Width
Depth
Cell
CellIndex
Grid
Seed
```

Do not use `Height` for horizontal map dimensions. Height means elevation.

Map primitives:

```text
Map.Grid
  Width
  Depth
  CellCount

Map.Cell
  X
  Z

Map.CellIndex
  Value

Map.Seed
  UInt64 deterministic root seed
```

`Grid` owns bounds and conversion.

`Cell` and `CellIndex` are created through `Grid`.

Jobs do not use `Grid`, `Cell`, or `CellIndex` in hot loops. Jobs use raw numeric dimensions and indexes.

## 7. Definition Model

Atlas uses catalog definitions, not occurrence-heavy public modeling.

### 7.1 Schema Definition

A schema definition declares required stage kinds.

Example:

```text
Earth
  RequiredStageKinds:
    landmass
```

The schema does not select stage definitions.

### 7.2 Stage Definition

A stage definition is a reusable catalog entry.

It declares:

```text
Symbol
DisplayName
StageKind
StageRouteDefinition symbol
settings contract when available
```

A stage definition can satisfy a schema requirement when its stage kind matches.

### 7.3 Stage Route Definition

A stage route definition is a strategy for satisfying a stage kind.

It declares:

```text
Symbol
DisplayName
StageKind
ordered required operation kinds
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

### 7.4 Operation Definition

An operation definition describes a semantic deterministic transform contract.

It declares:

```text
Symbol
DisplayName
OperationKind
operation contract data
```

Field access requirements are added here when the field model exists.

### 7.5 Operation Implementation Definition

An operation implementation definition describes a named strategy that satisfies an operation kind.

It declares:

```text
Symbol
DisplayName
OperationKind
settings contract when available
execution binding when execution layer exists
```

It does not expose jobs or scheduler internals in the planning layer.

## 8. Request, Catalog, Plan Rules

### 8.1 GenerationRequest

A request is valid normalized authored intent.

It contains:

```text
Guid Id
DisplayName
Map.Grid
Map.Seed
SchemaSymbol
stage definition selections
operation implementation selections
typed settings selections
```

`Id` is metadata only.

`DisplayName` is metadata only.

`Grid`, `Seed`, schema symbol, selected definitions, and settings are generation input.

A request does not contain resolved catalog definitions.

### 8.2 GenerationCatalog

The catalog is immutable known vocabulary.

It contains definitions.

It does not compile.

It does not execute.

It is passed into compilers explicitly.

### 8.3 GenerationPlanCompiler

The compiler resolves a request through a catalog.

It validates:

```text
schema exists
selected stage definitions exist
selected stage definition kinds satisfy schema required stage kinds
selected stage routes exist
stage route kinds match selected stage definitions
route required operation kinds exist
operation definitions exist
selected operation implementations exist
operation implementation kinds match operation definitions
settings are present and valid once settings contracts exist
```

The compiler returns either a successful result with `GenerationPlan` or a failed result with compiler errors.

### 8.4 GenerationPlan

A plan is compiler output.

A plan has no public constructor.

If a `GenerationPlan` instance exists, it is accepted and resolved.

A failed compilation never returns a partial plan.

## 9. Compiler Result Rules

Compiler results must enforce:

```text
Succeeded:
  Plan != null
  Errors.Count == 0

Failed:
  Plan == null
  Errors.Count > 0
```

Compiler errors are specific to compiler failure.

They are not a global diagnostics framework.

A compiler error contains:

```text
Symbol Code
string Message
```

No severity system.
No nested diagnostics system.
No location model until tests require it.

## 10. Settings Rules

Settings must be typed.

Forbidden:

```text
Dictionary<string, object>
object settings bag
stringly typed setting lookup
runtime casts inside jobs
```

Allowed direction:

```text
LandmassSettings
EvaluateContinentSuitabilitySettings
CompleteContinentAreaSettings
ComposeBaseElevationSettings
```

Planning settings are managed typed objects.

Execution settings are compiled unmanaged structs.

Runtime jobs receive only unmanaged settings.

## 11. Route Rules

Route is a first-class stage strategy.

A route is not:

```text
schema
stage kind
operation kind
operation implementation
job graph
```

A route owns the ordered operation-kind chain for a stage strategy.

Example:

```text
PrimaryContinent is a Landmass route.
Earth does not know PrimaryContinent.
Landmass stage kind does not permanently mean PrimaryContinent.
```

## 12. Landmass Rules

Landmass product vocabulary belongs under:

```text
Runtime/Generation/Landmass/
```

Generic infrastructure must not hardcode Landmass symbols.

Correct:

```text
Runtime/Generation/Landmass/StageKind.cs
  defines landmass stage kind

Runtime/Generation/Landmass/OperationKinds.cs
  defines Landmass operation kinds

Runtime/Generation/Landmass/Routes/PrimaryContinent.cs
  defines PrimaryContinent route
```

Incorrect:

```text
Stages.StageKind.Landmass
Operations.OperationKind.ComposeBaseElevation
Schemas.Earth hardcoding PrimaryContinent
```

## 13. Determinism Rules

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

Use package-owned deterministic hashing and seed derivation.

## 14. Execution Boundary Rules

Planning objects are managed and allocation-friendly.

Execution objects are explicit and allocation-controlled.

Jobs must not resolve symbols, catalogs, schemas, routes, definitions, fields, or settings contracts.

Schedulers/executors receive compiled runtime data.

Jobs receive only resolved native views and unmanaged job settings.

## 15. Error Handling Rules

Constructors throw exceptions for programmer errors.

Examples:

```text
null required argument
invalid symbol
invalid display name
invalid grid dimension
invalid seed text
duplicate catalog definition registration
```

Compilers return structured errors for authored selection failures.

Examples:

```text
unknown selected stage definition
selected stage kind does not satisfy schema
unknown route
operation implementation kind mismatch
missing setting
invalid authored setting value
```

## 16. Collection Rules

Constructors must copy caller-provided collections.

Do not store caller-owned arrays or lists.

Expose collections as `IReadOnlyList<T>` backed by immutable private arrays.

Never mutate arrays after construction.

## 17. Static State Rules

Static readonly immutable vocabulary is allowed.

Allowed:

```text
BuiltInSchemas.Earth
Landmass stage kind symbol
PrimaryContinent route definition
```

Forbidden:

```text
global mutable catalog
current request singleton
current plan singleton
runtime mutable static cache
static generated request id
```

## 18. Reflection Rules

Do not use reflection scanning as the primary catalog discovery mechanism.

Use explicit catalog builder registration.

This protects IL2CPP, code stripping, determinism, and testability.

## 19. Public API Rules

Public APIs must be clear at call sites.

Prefer explicit type names over short clever names.

Use:

```text
StageKind
OperationKind
StageRouteDefinition
OperationImplementationDefinition
GenerationSchemaDefinition
```

Do not use:

```text
Kind
Route
Definition
Implementation
Occurrence
```

unless the type is private/internal and the containing scope makes meaning unavoidable.

## 20. Testing Rules

Foundational tests must protect invariants.

First test areas:

```text
SymbolTests
DisplayNameTests
GridTests
SeedTests
GenerationSchemaDefinitionTests
StageRouteDefinitionTests
GenerationCatalogTests
GenerationPlanCompilerTests
```

Tests must not weaken production invariants to get green.

Tests must distinguish:

```text
constructor/programmer error -> exception
authored selection error -> compiler error result
```

## 21. Forbidden Patterns

The following are forbidden in this package architecture:

```text
invalid domain objects
mutable global catalog
ScriptableObject as runtime authority
Dictionary<string, object> settings
reflection-first catalog discovery
jobs reading symbols or schemas
planning depending on execution
execution depending on editor
stage kind hardcoding product vocabulary
operation kind hardcoding product vocabulary
schema requiring concrete implementations
route hidden inside stage kind
operation job graph hidden inside operation kind
```

## 22. Final Rule

When uncertain, use this decision test:

```text
Is this reusable vocabulary?
  Put it in Catalog definitions.

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

The package succeeds when each object has one reason to exist, one owner, and one lifecycle.

``` 