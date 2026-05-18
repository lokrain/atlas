# Architecture rules

This document defines the primary architecture rules for Lokrain.Atlas.

Use this page as the entry point for architecture guidance. Detailed rules are split into focused guideline documents.

## Rule summary

Lokrain.Atlas architecture is boundary-driven.

Each type owns one responsibility.

Each layer owns one boundary.

Accepted objects validate their own local invariants.

Catalogs validate accepted definition graphs.

Descriptors express symbolic caller intent.

Resolvers convert symbolic intent into accepted requests.

Plan compilers convert accepted requests into managed semantic plans.

Future runnable compilers convert managed semantic plans into executable metadata.

Future workspaces own native storage.

Future schedulers own execution control flow.

Future jobs execute deterministic native transforms.

## Current Runtime boundary

Current Runtime architecture ends at `GenerationPlan`.

Current Runtime includes:

```text
Core values
Generation schemas
Resource definitions
Stage and operation definitions
Stage and operation contracts
Generation recipes
Generation catalogs
Generation request descriptors
Generation request resolution
Generation requests
Generation plan compilation
Generation plans
```

Current Runtime does not include:

```text
FieldDefinition
RunnablePlan
GenerationWorkspace
OperationScheduler
native storage allocation
job scheduling
Burst execution
artifact capture
ECS execution integration
```

Future execution concepts must not be documented or modeled as current Runtime behavior until corresponding Runtime code exists.

## Layer order

Dependencies flow in this direction:

```text
Core
  -> Schemas
  -> Resources
  -> Stages
  -> Operations
  -> Catalog
  -> Recipes
  -> Planning
  -> Future execution
  -> Unity adapters
  -> Editor tooling
  -> Tests
```

A higher layer may reference lower-layer accepted objects.

A lower layer must not reference a higher layer to complete its meaning.

## Ownership rule

Ownership must be explicit.

A type must not borrow another layer’s authority.

Examples:

| Responsibility | Owner |
| --- | --- |
| Symbol syntax | `Symbol` |
| Display-name syntax | `DisplayName` |
| Grid dimensions | `Grid` |
| Resource local validity | `ResourceDefinition` |
| Contract local validity | `StageContract` and `OperationContract` |
| Definition graph validity | `GenerationCatalog` |
| Descriptor satisfiability | `GenerationRequestResolver` |
| Final request validity | `GenerationRequest` |
| Managed stage ordering | `GenerationPlanCompiler` |
| Native allocation | Planned workspace |
| Job dependency wiring | Planned scheduler |

Do not make lower-level objects inspect higher-level owners.

## Accepted object rule

A non-null accepted object is valid for the invariants owned by its type.

Constructors and factories reject invalid local state.

Accepted objects do not expose partially valid instances.

Correct:

```text
Symbol rejects invalid symbol text.
DisplayName rejects invalid display text.
Grid rejects invalid dimensions.
StageContract rejects invalid local resource flow.
GenerationRequest rejects invalid final implementation choices.
```

Incorrect:

```text
ResourceDefinition accepts null schema and expects the catalog to catch it.
StageContract stores caller-owned mutable lists.
GenerationRequest stores unresolved symbols.
GenerationPlan allows null stage nodes.
```

## Definition rule

Definitions describe reusable package inventory.

Definitions do not represent one generation run.

Definitions must not own run settings, requests, plans, native containers, field handles, scheduler bindings, job handles, or Unity object identity.

Correct:

```text
ResourceDefinition
StageDefinition
OperationDefinition
OperationImplementationDefinition
GenerationRecipeDefinition
```

Incorrect:

```text
ResourceData
StageRuntimeState
OperationProcessor
RecipeContext
FieldBackedResourceDefinition
```

## Catalog rule

`GenerationCatalog` owns accepted definition inventory.

Catalog ownership is reference-exact.

Two definitions with the same symbol are not interchangeable when only one instance belongs to the catalog.

A catalog validates:

```text
definition uniqueness
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

A catalog does not represent a generation run.

## Resource and contract rule

`ResourceDefinition` describes the semantic identity of a generated value.

Stage and operation contracts use `ResourceDefinition` inputs and outputs.

Contracts describe semantic resource flow. They do not describe storage.

Correct:

```text
OperationContract
  requires ContinentSuitability
  produces ContinentCandidate
```

Incorrect:

```text
OperationContract
  requires NativeArray<float>
  produces FieldHandle
```

Storage-facing metadata belongs to planned field definitions and runnable plan compilation.

## Descriptor rule

Descriptors are symbolic caller intent.

Descriptors may contain symbols, run settings, and other descriptor objects.

Descriptors must not contain accepted catalog definitions.

Correct:

```text
GenerationRequestDescriptor
  GenerationRecipeDefinitionSymbol
  GenerationRunSettings
  OperationImplementationOverrideDescriptor list
```

Incorrect:

```text
GenerationRequestDescriptor
  GenerationRecipeDefinition
  StageRouteStepDefinition
  OperationImplementationDefinition
```

A descriptor can be structurally valid before catalog resolution.

A descriptor is not an accepted request.

## Request rule

`GenerationRequest` is accepted resolved generation intent for one run.

A request contains accepted definitions, run settings, and final implementation choices.

A request contains no unresolved symbols.

Correct:

```text
GenerationRequest
  recipe definition
  run settings
  final route-step implementation choices
```

Incorrect:

```text
GenerationRequest
  recipe symbol
  override descriptors
  unresolved operation symbols
  native containers
```

## Plan rule

`GenerationPlan` is managed semantic data.

A plan may contain accepted definitions, run settings, stage plan nodes, operation plan nodes, and resource-definition-based contracts.

A plan must not contain:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
FieldDefinition
FieldHandle
SchedulerBinding
JobHandle
Burst job structs
ECS entities
UnityEngine.Object
```

A generation plan is not a runnable plan.

## Ordering rule

Generation-relevant ordering must be explicit.

Ordering is semantic for:

```text
stage route choices
route steps
route-step implementation choices
stage plan nodes
operation plan nodes
resolution errors
```

Do not depend on dictionary enumeration order, hash set enumeration order, managed object allocation order, Unity scene hierarchy order, Unity asset import order, or thread timing.

## Determinism rule

Deterministic generation must depend only on stable accepted inputs.

Valid deterministic inputs include:

```text
Symbol
Grid
Seed
GenerationRunSettings
accepted recipe choices
accepted implementation choices
GenerationRequest
GenerationPlan ordering
```

Do not use these as deterministic identity:

```text
DisplayName
Unity object instance ID
Unity asset path
managed object reference identity
process-local string hash code
current time
global random state
editor selection state
```

Display names are metadata.

Symbols are identity.

## Unity boundary rule

Runtime domain objects must not become Unity object wrappers.

Unity-facing objects may adapt Unity data into Atlas descriptors or accepted definitions.

Unity-facing objects must not replace the domain model.

Correct:

```text
ScriptableObject authoring asset -> creates descriptor input
Editor window -> displays catalog data
Importer -> translates external data into package domain objects
```

Incorrect:

```text
ScriptableObject is the canonical recipe.
MonoBehaviour owns GenerationCatalog identity.
GameObject name is a Symbol.
Unity asset path defines deterministic generation identity.
```

## Future execution rule

Future execution concepts stay outside current managed Runtime objects.

Correct future boundary:

```text
GenerationPlan
  -> RunnablePlanCompiler
  -> RunnablePlan
  -> GenerationWorkspace
  -> OperationScheduler
  -> Burst jobs
```

Incorrect current boundary:

```text
GenerationPlan owns NativeArray<T>
GenerationPlan owns FieldHandle
GenerationRecipeDefinition owns scheduler binding
OperationContract owns FieldDefinition
ResourceDefinition owns storage layout
```

## Related guideline documents

Use these documents for detailed rules:

| Document | Purpose |
| --- | --- |
| `Domain Object Rules.md` | Accepted objects, definitions, descriptors, requests, plans, immutability, and collection rules. |
| `Catalog Ownership Rules.md` | Catalog ownership, graph validation, exact-reference ownership, and cross-catalog rejection. |
| `Runtime Boundary Rules.md` | Current Runtime boundary, future execution boundary, Unity boundary, native storage, scheduler, and job rules. |
| `Dependency Rules.md` | Allowed dependency direction between package areas. |
| `Error Handling Rules.md` | Exceptions versus result objects. |
| `Naming Guidelines.md` | Naming rules for public API, symbols, files, namespaces, and generation modules. |

## Review checklist

Before accepting an architecture change, verify:

```text
The changed type owns the responsibility being added.
The dependency direction still flows forward.
No current managed object contains future execution state.
No descriptor contains accepted catalog definitions.
No request contains unresolved symbols.
No plan contains native storage or job state.
No job knows symbols, catalogs, resources, requests, or plans.
Catalog-owned objects reference definitions owned by the same catalog.
Resource flow uses ResourceDefinition, not raw symbol lists.
Expected resolution failure uses result objects.
Invalid API usage throws precise exceptions.
Ordering that affects generation is explicit.
Unity object identity does not define package-domain identity.
Mutable input collections are snapped before storage.
Accepted objects expose read-only state.
```