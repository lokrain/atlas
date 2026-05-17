# Atlas architecture overview

Lokrain.Atlas is a Unity package for deterministic world-generation architecture.

The package is organized around a strict separation between:

- authored symbolic intent;
- accepted managed domain objects;
- catalog-owned definitions;
- request resolution;
- managed plan compilation;
- future executable plan metadata;
- future native workspace storage;
- future schedulers and jobs.

The current Runtime implements the managed architecture up to `GenerationPlan`. Execution concepts such as field definitions, runnable plans, workspaces, schedulers, and Burst jobs are planned architecture and are not implemented Runtime behavior.

## Architecture goals

The architecture is designed to preserve these properties:

- deterministic generation inputs;
- stable machine-facing symbols;
- validated accepted domain objects;
- clear ownership boundaries;
- immutable catalog snapshots;
- explicit request resolution;
- managed plans without execution state;
- future Burst-compatible execution boundaries;
- no hidden coupling between authoring, planning, storage, and scheduling.

Atlas avoids treating Unity assets, editor objects, native containers, jobs, or ECS systems as canonical domain state.

Unity-facing objects may adapt user-authored data into Atlas descriptors or definitions. They do not replace the package domain model.

## Current architecture

The implemented architecture is a managed planning pipeline.

```text
GenerationRequestDescriptor
        +
GenerationCatalog
        |
        v
GenerationRequestResolver
        |
        v
GenerationRequestResolutionResult
        |
        v
GenerationRequest
        |
        v
GenerationPlanCompiler
        |
        v
GenerationPlan
````

The current architecture answers these questions:

| Question                                                                                    | Current owner                     |
| ------------------------------------------------------------------------------------------- | --------------------------------- |
| What stable values and identifiers exist?                                                   | Core value objects                |
| What schemas, resources, stages, operations, implementations, recipes, and contracts exist? | Definitions and catalog           |
| Which recipe and implementation overrides did the caller request?                           | Request descriptor                |
| Can the symbolic request be satisfied by this catalog?                                      | Request resolver                  |
| What accepted generation run should be planned?                                             | Generation request                |
| What ordered managed generation plan should run?                                            | Plan compiler and generation plan |

The current architecture does not execute generation work.

## Primary layers

### Core

Core contains reusable value objects and low-level invariants.

Examples:

```text
Symbol
DisplayName
Grid
Cell
CellIndex
Seed
```

Core types are independent of catalog, planning, Unity objects, execution, jobs, and native containers.

### Definitions

Definitions describe accepted reusable package inventory.

Examples:

```text
GenerationSchemaDefinition
ResourceDefinition
StageDefinition
StageRouteDefinition
StageRouteStepDefinition
StageContract
OperationDefinition
OperationContract
OperationImplementationDefinition
GenerationRecipeDefinition
```

Definitions are metadata. They do not represent one generation run and do not own execution state.

### Catalog

`GenerationCatalog` is an immutable accepted inventory of definitions.

The catalog owns the accepted definitions it exposes. Catalog-owned graphs must reference definitions from the same catalog instance. Cross-catalog object reuse is invalid.

The catalog provides lookup and validates graph consistency. It does not resolve request descriptors, compile plans, allocate storage, or execute jobs.

### Request descriptor

`GenerationRequestDescriptor` is symbolic input.

It contains the selected recipe symbol, run settings, and optional implementation override descriptors.

A descriptor is structurally valid after construction, but it is not catalog-resolved. Its symbols may or may not exist in a specific catalog.

### Request resolver

`GenerationRequestResolver` converts symbolic intent into accepted resolved intent.

It resolves descriptor symbols through a catalog and returns a `GenerationRequestResolutionResult`.

Expected catalog-satisfiability failures are returned as structured resolution errors. Invalid API usage throws exceptions.

### Generation request

`GenerationRequest` is accepted resolved generation intent for one run.

It contains the selected recipe, run settings, and final implementation choices.

A request contains accepted definitions, not unresolved symbols.

### Plan compiler

`GenerationPlanCompiler` converts an accepted request into a managed semantic plan.

The compiler does not resolve symbols through the catalog during normal plan compilation. Resolution is already complete before a request reaches the compiler.

### Generation plan

`GenerationPlan` is accepted managed semantic data for one generation run.

It contains the selected recipe, run settings, ordered stage plan nodes, and ordered operation plan nodes.

A generation plan is not executable job data. It contains no native storage, field handles, job handles, dependency handles, scheduler bindings, or Burst function pointers.

## Resource model

`ResourceDefinition` represents the semantic identity of a generated value.

Stage and operation contracts declare required inputs and produced outputs as resource definitions.

A resource answers:

```text
What value is required or produced?
```

A resource does not answer:

```text
How is the value stored?
Which native container owns it?
Which scheduler writes it?
Which job reads it?
Which artifact captures it?
```

Those are future execution concerns.

## Future execution architecture

The planned execution architecture begins after managed plan compilation.

```text
GenerationPlan
        |
        v
RunnablePlanCompiler
        |
        v
RunnablePlan
        |
        v
GenerationWorkspace
        |
        v
OperationScheduler
        |
        v
Burst jobs
```

Future execution architecture introduces these planned concepts:

```text
FieldDefinition
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
RunnableStage
RunnableOperation
SchedulerBinding
GenerationWorkspace
OperationScheduler
FieldHandle
OperationScratch
```

These concepts are not part of the current implemented Runtime unless code exists for them.

## Future execution boundary

The future execution model must preserve this boundary:

| Layer                  | Owns                                              | Must not own                                                      |
| ---------------------- | ------------------------------------------------- | ----------------------------------------------------------------- |
| Managed plan           | Semantic ordered generation work                  | Native storage, job handles, scheduler bindings                   |
| Runnable plan compiler | Binding semantic resources to executable metadata | Native allocation, job scheduling                                 |
| Runnable plan          | Execution metadata                                | Native storage lifetime, running jobs                             |
| Workspace              | Native storage allocation and disposal            | Catalog lookup, recipe selection, semantic planning               |
| Scheduler              | Operation control flow and job scheduling         | Catalog resolution, symbolic lookup                               |
| Job                    | Deterministic transform over native data          | Symbols, catalog, recipes, requests, plans, resources, schedulers |

Jobs must receive only native containers and unmanaged values.

Jobs must not inspect symbols, catalogs, schemas, recipes, requests, plans, resources, field definitions, workspaces, or schedulers.

## Unity boundary

Atlas Runtime domain objects are package domain objects, not Unity scene objects.

Runtime managed planning code should stay independent from:

```text
UnityEngine.Object
ScriptableObject
MonoBehaviour
GameObject
UnityEditor
ECS World
ECS System
NativeContainer allocation
Job scheduling
```

Unity-facing code may exist as adapters around the domain model.

Examples:

| Unity-facing object | Correct role                                                   |
| ------------------- | -------------------------------------------------------------- |
| ScriptableObject    | Authoring adapter for definitions or descriptors               |
| Editor window       | Tooling surface for validation and inspection                  |
| Importer            | Translation from external data into descriptors or definitions |
| ECS system          | Future execution integration, not catalog ownership            |
| MonoBehaviour       | Integration adapter, not canonical generation state            |

The package domain model remains the source of truth.

## Determinism boundary

Deterministic generation depends on stable accepted inputs.

Current deterministic input concepts include:

```text
Symbol
Grid
Seed
GenerationRunSettings
GenerationRecipeDefinition
StageRouteChoice
StageRouteStepImplementationChoice
GenerationRequest
GenerationPlan
```

Display names, Unity object names, asset paths, editor selection state, managed object identity, dictionary enumeration order, and process-local hash codes must not define deterministic generation semantics.

## Ownership boundary

Ownership is explicit.

The catalog owns accepted definitions.

The request owns one resolved run intent.

The plan owns one managed semantic plan.

Future workspace execution owns native storage.

Future schedulers own execution control flow.

Future jobs own only their local deterministic transform logic.

No layer should reach backward to reinterpret an earlier symbolic boundary or reach forward to allocate another layer’s execution state.

## Error-handling boundary

Atlas separates invalid API usage from expected domain failure.

Invalid API usage throws exceptions.

Examples:

```text
null arguments
duplicate entries in constructor inputs
out-of-range grid coordinates
invalid symbols
invalid display names
```

Expected catalog-satisfiability failure uses result objects.

Examples:

```text
missing recipe symbol
missing implementation override target
override implementation not compatible with selected route step
```

Execution failure policy is future architecture and belongs to scheduler/workspace design.

## Documentation map

Use the architecture documentation by purpose:

| Purpose                        | Location      |
| ------------------------------ | ------------- |
| System overview                | `Overview/`   |
| Domain model explanation       | `Concepts/`   |
| Required rules                 | `Guidelines/` |
| Term definitions               | `Reference/`  |
| Planned execution architecture | `Future/`     |
| Recorded design decisions      | `Decisions/`  |
| Ordered implementation work    | `Plans/`      |

Current architecture documents describe how the package must work now.

Future documents describe planned architecture only.

Decision documents may explain rejected options.

Plan documents describe work order and must not redefine the architecture.

```