# Architecture rules

This document defines required architecture rules for Lokrain.Atlas.

These rules apply to Runtime domain objects, catalog construction, request resolution, managed plan compilation, and future execution architecture.

## Core rule

Each architecture layer owns one boundary.

A layer may consume accepted objects from lower layers. It must not reach upward into higher layers or hide another layer’s responsibilities inside itself.

Correct direction:

```text
Core
  -> Definitions
  -> Catalog
  -> Request descriptor
  -> Request resolver
  -> Generation request
  -> Plan compiler
  -> Generation plan
  -> Future runnable plan compiler
  -> Future runnable plan
  -> Future workspace
  -> Future scheduler
  -> Future jobs
````

Incorrect direction:

```text
ResourceDefinition -> GenerationCatalog
StageContract -> FieldDefinition
GenerationRecipeDefinition -> GenerationRunSettings
GenerationPlan -> JobHandle
Job -> Symbol
Job -> GenerationCatalog
```

## Current versus future rule

Current Runtime architecture ends at `GenerationPlan`.

The following are current implemented architecture concepts:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract
OperationDefinition
OperationContract
OperationImplementationDefinition
GenerationCatalog
GenerationCatalogBuilder
GenerationRecipeDefinition
GenerationRequestDescriptor
OperationImplementationOverrideDescriptor
GenerationRequestResolver
GenerationRequestResolutionResult
GenerationRequest
GenerationPlanCompiler
GenerationPlan
StagePlanNode
OperationPlanNode
```

The following are future execution concepts and must not be documented or modeled as current implemented Runtime behavior unless corresponding code exists:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
SchedulerBinding
GenerationWorkspace
FieldHandle
OperationScheduler
OperationScratch
JobHandle ownership
Burst job execution
Artifact capture
```

Future concepts must not be smuggled into current managed objects as hidden fields or implied responsibilities.

## Accepted object rule

A non-null accepted object must be valid for the invariants owned by its type.

Constructors and factories must reject invalid local state.

Accepted objects should not expose partially valid instances.

Correct:

```text
ResourceDefinition constructor validates symbol, display name, and schema reference.
StageContract constructor validates required and produced resources.
GenerationRequest constructor validates recipe, settings, and implementation choices.
```

Incorrect:

```text
ResourceDefinition accepts null schema and expects catalog to catch it.
StageContract stores caller-owned mutable lists.
GenerationRequest stores unresolved symbols.
GenerationPlan allows null stage nodes.
```

## Local invariant rule

Each type validates only the invariants it owns.

Local invariants belong in the type constructor or factory.

Graph invariants belong to the graph owner.

Examples:

| Invariant                    | Owner                                 |
| ---------------------------- | ------------------------------------- |
| Symbol syntax                | `Symbol`                              |
| Display name syntax          | `DisplayName`                         |
| Grid dimensions              | `Grid`                                |
| Resource local shape         | `ResourceDefinition`                  |
| Contract local shape         | `StageContract` / `OperationContract` |
| Definition symbol uniqueness | `GenerationCatalog`                   |
| Cross-catalog object reuse   | `GenerationCatalog`                   |
| Descriptor satisfiability    | `GenerationRequestResolver`           |
| Managed plan ordering        | `GenerationPlanCompiler`              |
| Native allocation            | Future workspace                      |
| Job dependencies             | Future scheduler                      |

Do not make low-level objects inspect higher-level owners.

## Catalog ownership rule

`GenerationCatalog` owns the accepted definition instances it exposes.

A catalog-owned graph must reference definitions owned by the same catalog.

Catalog ownership is stricter than symbol equality.

Correct:

```text
catalog A
  recipe A
    stage A
    route A
    route step A
    operation A
    implementation A
    resource A
```

Incorrect:

```text
catalog A
  recipe A
    stage from catalog B
    resource from catalog B
    implementation from catalog B
```

Two definitions with the same symbol are not interchangeable when they belong to different catalog instances.

## Catalog responsibility rule

`GenerationCatalog` validates accepted inventory.

It owns:

```text
definition lookup
definition uniqueness
schema consistency
resource ownership
stage ownership
route ownership
route-step consistency
operation implementation compatibility
recipe graph consistency
contract resource ownership
cross-catalog reference rejection
```

It must not own:

```text
per-run settings
request descriptors
request resolution workflow
managed plan compilation
native storage allocation
job scheduling
execution artifact capture
```

## Builder rule

A builder is mutable assembly state.

An accepted catalog is immutable accepted inventory.

Correct:

```text
GenerationCatalogBuilder collects candidate definitions.
GenerationCatalog validates and exposes accepted immutable inventory.
```

Incorrect:

```text
GenerationCatalog exposes builder-owned mutable lists.
GenerationCatalogBuilder is passed as if it were accepted inventory.
Builder mutation changes an existing catalog.
```

Builders must not leak mutable state into accepted objects.

## Definition rule

Definitions describe reusable package inventory.

Definitions may reference lower-level accepted values and definitions when the domain requires it.

Definitions must not represent one generation run.

Definitions must not own:

```text
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native containers
field handles
scheduler bindings
job handles
```

## Resource rule

`ResourceDefinition` is the semantic identity of a generated value.

Stage and operation contracts must use `ResourceDefinition` for required inputs and produced outputs.

Contracts must not use raw symbol lists for resource flow.

Correct:

```text
OperationContract.RequiredInputs  -> ResourceDefinition list
OperationContract.ProducedOutputs -> ResourceDefinition list
```

Incorrect:

```text
OperationContract.RequiredInputSymbols
OperationContract.ProducedOutputSymbols
OperationContract.FieldDefinitions
OperationContract.NativeArrays
```

Resources are not fields, storage, schedulers, or jobs.

## Contract rule

Stage and operation contracts describe semantic resource flow.

Contracts must not describe execution storage.

A contract may say:

```text
requires Height
produces Land
```

A contract must not say:

```text
Height uses NativeArray<float>
Land uses field handle 4
this operation runs scheduler X
this operation depends on JobHandle Y
```

## Recipe rule

A recipe is a reusable generation template.

A recipe selects:

```text
stage routes
default route-step implementation choices
```

A recipe must not contain:

```text
Grid
Seed
GenerationRunSettings
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
native storage
scheduler state
job handles
```

The same recipe must be usable for many runs.

## Route-step occurrence rule

Implementation choices target route-step occurrences.

Use `StageRouteStepDefinition.Symbol` for override target identity.

Do not use only `OperationDefinition.Symbol` as the override target.

Reason: the same operation definition may appear multiple times in a route.

Correct:

```text
route step A -> operation X -> implementation Fast
route step B -> operation X -> implementation Accurate
```

Incorrect:

```text
operation X -> implementation Fast
```

The incorrect form cannot distinguish occurrences.

## Descriptor rule

Descriptors are symbolic caller intent.

Descriptors may contain:

```text
Symbol
GenerationRunSettings
other descriptor objects
```

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

Descriptors are valid before catalog resolution, but not accepted requests.

## Resolver rule

`GenerationRequestResolver` owns descriptor satisfiability.

It resolves symbolic descriptor input through a catalog and produces a `GenerationRequestResolutionResult`.

The resolver owns:

```text
recipe symbol lookup
override target lookup
implementation symbol lookup
implementation compatibility validation
final implementation choice selection
resolution errors
```

The resolver must not compile managed plans, allocate native storage, or schedule jobs.

## Result rule

Expected catalog-satisfiability failure uses result objects.

Use result objects for:

```text
requested recipe symbol not found
override route-step symbol not found
implementation symbol not found
implementation incompatible with target operation
descriptor cannot be satisfied by catalog
```

Throw exceptions for invalid API usage:

```text
null arguments
invalid constructor input
invalid symbols
invalid display names
invalid grid dimensions
```

Do not use exceptions as normal control flow for request-resolution failure.

## Request rule

`GenerationRequest` is accepted resolved intent for one run.

A request contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
final StageRouteStepImplementationChoice list
```

A request must contain no unresolved symbols.

A request is not a plan and not execution state.

It must not contain:

```text
field definitions
native containers
scheduler bindings
job handles
dependency handles
Burst job data
```

## Plan compiler rule

`GenerationPlanCompiler` consumes an accepted request.

The compiler must not perform normal descriptor resolution.

Correct:

```text
GenerationRequestDescriptor + GenerationCatalog
  -> GenerationRequestResolver
  -> GenerationRequest
  -> GenerationPlanCompiler
  -> GenerationPlan
```

Incorrect:

```text
GenerationRequestDescriptor
  -> GenerationPlanCompiler
  -> GenerationPlan
```

The compiler may defensively validate request consistency, but request resolution owns symbol satisfaction.

## Plan rule

`GenerationPlan` is managed semantic data.

It may contain:

```text
GenerationRecipeDefinition
GenerationRunSettings
StagePlanNode list
OperationPlanNode list
accepted definitions
resource-definition-based contracts
```

It must not contain:

```text
NativeArray<T>
NativeList<T>
NativeHashMap<TKey, TValue>
FieldHandle
SchedulerBinding
JobHandle
Burst job structs
ECS entities
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

Do not depend on:

```text
dictionary enumeration order
hash set enumeration order
managed object allocation order
Unity scene hierarchy order
Unity asset import order
thread timing
```

When unordered data is compiled into ordered data, the ordering rule must be stable and documented by the owner.

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
random global state
editor selection state
```

## Unity boundary rule

Runtime domain objects must not become Unity object wrappers.

Managed Runtime architecture should not depend on:

```text
UnityEngine.Object
ScriptableObject
MonoBehaviour
GameObject
UnityEditor
ECS World
ECS System
native container allocation
job scheduling
```

Unity-facing objects may adapt data into Atlas descriptors or definitions.

They must not replace the domain model.

Correct:

```text
ScriptableObject authoring asset -> creates descriptor or definition input
Editor window -> validates and displays catalog data
Importer -> translates external data into package domain objects
```

Incorrect:

```text
ScriptableObject is the canonical recipe
MonoBehaviour owns GenerationCatalog identity
GameObject name is a Symbol
Unity asset path defines deterministic generation identity
```

## Native storage boundary rule

Current managed planning objects must not allocate or own native containers.

Native storage belongs to future workspace execution.

Do not add native containers to:

```text
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
StagePlanNode
OperationPlanNode
ResourceDefinition
StageContract
OperationContract
```

Future native storage must be owned by `GenerationWorkspace` or a similarly explicit execution owner.

## Scheduler boundary rule

Schedulers are future execution controllers.

A scheduler may own:

```text
workspace access
dependency wiring
job scheduling
operation scratch allocation
iteration policy
termination policy
execution failure policy
```

A scheduler must not own:

```text
symbol resolution
catalog lookup
recipe selection
request resolution
managed plan compilation
semantic resource identity
```

Do not put scheduler behavior in definitions, recipes, requests, or managed plans.

## Job boundary rule

Jobs are future deterministic transforms over native data.

Jobs receive native containers and unmanaged values only.

Jobs must not know:

```text
Symbol
DisplayName
GenerationCatalog
GenerationRecipeDefinition
GenerationRequestDescriptor
GenerationRequest
GenerationPlan
ResourceDefinition
FieldDefinition
GenerationWorkspace
OperationScheduler
UnityEngine.Object
UnityEditor
```

If a job needs something, the scheduler must resolve it before scheduling and pass it as native data or unmanaged parameters.

## Field boundary rule

`FieldDefinition` is future storage-facing metadata.

It must not be introduced into current resource contracts as if it were current architecture.

Correct future boundary:

```text
ResourceDefinition -> FieldDefinition -> workspace allocation
```

Incorrect current boundary:

```text
OperationContract -> FieldDefinition
StageContract -> NativeArray<T>
GenerationPlan -> FieldHandle
```

Resource contracts stay semantic.

Field metadata belongs to future runnable compilation and workspace allocation.

## Error ownership rule

Errors should be produced by the owner of the failing boundary.

Examples:

| Failure                                 | Owner                                       |
| --------------------------------------- | ------------------------------------------- |
| Invalid symbol text                     | `Symbol`                                    |
| Invalid display name text               | `DisplayName`                               |
| Invalid grid dimensions                 | `Grid`                                      |
| Duplicate catalog symbols               | `GenerationCatalog`                         |
| Missing recipe in descriptor resolution | `GenerationRequestResolver`                 |
| Invalid final request state             | `GenerationRequest`                         |
| Invalid managed plan state              | `GenerationPlanCompiler` / `GenerationPlan` |
| Native allocation failure               | Future workspace                            |
| Job dependency failure                  | Future scheduler                            |

Do not make unrelated layers report or repair failures they do not own.

## Public API rule

Public APIs should expose domain concepts directly when those concepts are package-owned.

Do not wrap or rename Unity, C#, DOTS, or .NET concepts unless Atlas owns additional meaning, invariants, lifecycle, or external contract.

Good package-owned concepts:

```text
Symbol
DisplayName
ResourceDefinition
GenerationCatalog
GenerationRequest
GenerationPlan
```

Bad wrapper concepts:

```text
AtlasList<T> wrapping List<T> without new semantics
AtlasNativeArray<T> wrapping NativeArray<T> without ownership rules
AtlasMonoBehaviour wrapping MonoBehaviour without a domain invariant
```

## Immutability rule

Accepted domain objects should be immutable unless mutability is the object’s explicit purpose.

Mutable objects must be named and scoped as mutable assembly or execution state.

Examples:

```text
GenerationCatalogBuilder -> mutable assembly surface
GenerationCatalog        -> immutable accepted inventory
```

Do not expose mutable collections from accepted objects.

Expose read-only views or copied snapshots.

## Collection rule

Constructors and factories that accept enumerable input must snapshot it before storing it.

Correct:

```text
copy enumerable input
validate copied values
reject null entries
reject duplicates when required
store private immutable/read-only collection
expose IReadOnlyList<T>
```

Incorrect:

```text
store caller-owned List<T>
trust lazy enumerable input
expose mutable List<T>
allow caller mutation after construction
```

## Naming-boundary rule

A name must match the architectural role.

Use:

```text
Descriptor
Definition
Catalog
Recipe
Request
Plan
Compiler
Resolver
Builder
Result
Error
```

only when the type owns that role.

Do not use:

```text
Manager
Data
Info
Context
Handler
Processor
```

when a precise architecture term exists.

Naming details belong in `Guidelines/Naming Guidelines.md`.

## Dependency rule

Lower layers must not reference higher layers.

The dependency direction must preserve this order:

```text
Core
Definitions
Catalog
Recipes
Requests
Plans
Future execution
Unity adapters
Editor tooling
Tests
```

Detailed assembly and namespace dependency guidance belongs in `Guidelines/Dependency Rules.md`.

## Extension rule

New generation modules must use the same architecture boundaries as built-in modules.

A module may add:

```text
schemas
resources
stages
routes
route steps
operations
implementations
contracts
recipes
descriptor factories
catalog factories
```

A module must not bypass:

```text
catalog validation
request resolution
accepted request construction
managed plan compilation
resource-definition-based contracts
```

## Review checklist

Before accepting an architecture change, verify:

```text
The changed type owns the responsibility being added.
The dependency direction still flows forward.
No current object contains future execution state.
No descriptor contains accepted catalog definitions.
No request contains unresolved symbols.
No plan contains native storage or job state.
No job knows symbols, catalogs, resources, requests, or plans.
Catalog-owned objects reference only definitions owned by the same catalog.
Resource flow uses ResourceDefinition, not raw symbol lists.
Expected resolution failure uses result objects.
Invalid API usage throws precise exceptions.
Ordering that affects generation is explicit.
Unity object identity does not define package-domain identity.
```

## Summary

Atlas architecture is boundary-driven.

Core values own local invariants.

Definitions describe reusable inventory.

Catalogs own accepted definition graphs.

Descriptors express symbolic intent.

Resolvers produce accepted requests.

Plan compilers produce managed semantic plans.

Future runnable compilers produce executable metadata.

Future workspaces own native storage.

Future schedulers own job orchestration.

Future jobs execute deterministic native transforms.

A type is correct when it owns one clear responsibility and does not borrow another layer’s authority.

```