# Lokrain.Atlas architecture documentation

This documentation defines the architecture of the Lokrain.Atlas Unity package.

The architecture separates authored intent, accepted managed domain objects, semantic resources, managed plans, future executable metadata, native workspace storage, schedulers, and jobs. Each layer has a single ownership boundary. Objects that cross a boundary must be accepted, validated, and owned by the layer that exposes them.

## Architecture model

Lokrain.Atlas uses a staged generation model.

```mermaid
flowchart TD
    A[GenerationRequestDescriptor<br/>symbolic authoring intent] --> B[GenerationRequestResolver]
    C[GenerationCatalog<br/>accepted package inventory] --> B

    B --> D[GenerationRequest<br/>accepted resolved intent]
    D --> E[GenerationPlanCompiler]
    E --> F[GenerationPlan<br/>managed semantic plan]

    F --> G[RunnablePlanCompiler<br/>future]
    H[Field definitions and execution profiles<br/>future] --> G
    I[Scheduler bindings<br/>future] --> G

    G --> J[RunnablePlan<br/>future executable metadata]
    J --> K[GenerationWorkspace<br/>future native storage owner]
    K --> L[Operation schedulers<br/>future job graph owners]
    L --> M[Burst jobs<br/>future deterministic transforms]
````

## Current implemented architecture

The current Runtime architecture covers the managed catalog, recipe, request, and plan model.

The implemented model includes:

* validated core value objects;
* generation schemas;
* semantic resource definitions;
* stage and operation contracts;
* stage, route, route-step, operation, and implementation definitions;
* immutable generation catalogs;
* symbolic request descriptors;
* request resolution into accepted requests;
* managed plan compilation.

The implemented model does not include executable runtime plans, field definitions, execution profiles, native workspace allocation, scheduler APIs, or Burst jobs.

## Current layer responsibilities

### Core

Core types define stable value objects and invariants used by the rest of the package.

Core types must not depend on catalog, recipe, planning, execution, Unity objects, native containers, or jobs.

### Definitions

Definitions describe accepted package inventory.

Definitions include schemas, resources, stages, routes, route steps, operations, operation implementations, and recipes. A non-null accepted definition instance is expected to be valid.

Definitions do not represent one generation run. They do not own native memory, scheduler bindings, job handles, or runtime execution state.

### Catalog

`GenerationCatalog` is the accepted immutable inventory for available generation definitions.

A catalog owns the accepted definition objects it exposes. Contracts and recipes must reference definitions owned by the same catalog. Cross-catalog object reuse is invalid.

The catalog is not a request resolver, not a plan compiler, and not an execution system.

### Request resolution

`GenerationRequestDescriptor` represents symbolic authoring intent.

`GenerationRequestResolver` resolves descriptor symbols through a `GenerationCatalog`, validates compatibility, and produces a `GenerationRequestResolutionResult`.

`GenerationRequest` represents accepted resolved intent for one generation run. It contains accepted definitions and final implementation choices. It does not contain unresolved symbols, native containers, jobs, or execution metadata.

### Plan compilation

`GenerationPlanCompiler` transforms an accepted `GenerationRequest` into a managed `GenerationPlan`.

`GenerationPlan` is semantic managed data. It orders the selected stages and operations for the run. It does not contain native storage, scheduler bindings, field handles, job handles, dependency handles, or executable job data.

### Resources

`ResourceDefinition` represents the semantic identity of a generated value.

Stage and operation contracts use resource definitions for required inputs and produced outputs. Contracts must not use raw symbol lists to describe resource flow.

A resource definition is not a field definition and is not native storage.

### Future execution model

The execution model is planned architecture and is not part of the current implemented Runtime.

Future execution concepts include:

* `FieldDefinition`;
* execution profiles;
* runnable plan compilation;
* `RunnablePlan`;
* `RunnableOperation`;
* `GenerationWorkspace`;
* field handles;
* operation schedulers;
* Burst jobs.

Future execution code must preserve the current boundary: managed planning decides what should happen; execution systems decide how storage and jobs are scheduled; jobs perform deterministic transforms over native containers and unmanaged values only.

## Documentation structure

### Overview

| File                                      | Purpose                                                                          |
| ----------------------------------------- | -------------------------------------------------------------------------------- |
| `Overview/Atlas Architecture Overview.md` | Introduces the package architecture, major layers, and current/future boundary.  |
| `Overview/Managed Generation Pipeline.md` | Explains descriptor resolution, accepted requests, and managed plan compilation. |

### Concepts

| File                                                | Purpose                                                                                                         |
| --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `Concepts/Accepted Domain Object Model.md`          | Defines accepted objects, descriptors, result objects, validation boundaries, and construction rules.           |
| `Concepts/Catalog Recipe Request and Plan Model.md` | Explains how catalog inventory, recipes, request descriptors, accepted requests, and plans relate.              |
| `Concepts/Resource Field and Workspace Boundary.md` | Defines the boundary between semantic resources, future field definitions, and future native workspace storage. |

### Guidelines

| File                                 | Purpose                                                                                             |
| ------------------------------------ | --------------------------------------------------------------------------------------------------- |
| `Guidelines/Architecture Rules.md`   | Defines validity, ownership, layering, and boundary rules.                                          |
| `Guidelines/Naming Guidelines.md`    | Defines naming rules for public API, domain concepts, symbols, resources, and landmass definitions. |
| `Guidelines/Dependency Rules.md`     | Defines allowed dependency direction between package layers.                                        |
| `Guidelines/Error Handling Rules.md` | Defines when APIs throw and when they return result objects.                                        |

### Reference

| File                    | Purpose                                                                            |
| ----------------------- | ---------------------------------------------------------------------------------- |
| `Reference/Glossary.md` | Defines architecture terminology without design rationale or implementation plans. |

### Future

| File                                                | Purpose                                                                                          |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `Future/Field Definition and Execution Profiles.md` | Defines the planned storage-facing field model and execution profile boundary.                   |
| `Future/Runnable Plan Compilation.md`               | Defines the planned compiler from managed plans to executable metadata.                          |
| `Future/Scheduler Workspace and Job Ownership.md`   | Defines planned ownership of native storage, scheduling, scratch memory, dependencies, and jobs. |

### Decisions

| File                                                     | Purpose                                                                             |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| `Decisions/ResourceDefinition Before FieldDefinition.md` | Records why semantic resources are modeled before storage-facing field definitions. |
| `Decisions/Rejections and Deferrals.md`                  | Records rejected architecture options and intentionally deferred concepts.          |

### Plans

| File                           | Purpose                                                                             |
| ------------------------------ | ----------------------------------------------------------------------------------- |
| `Plans/Implementation Plan.md` | Defines the ordered implementation work that follows from the current architecture. |

## Documentation rules

Architecture documents describe the current required design.

Concept documents explain the model. Guideline documents define rules. Reference documents define terms. Future documents describe planned architecture that is not implemented. Decision documents record rationale and rejected options. Plan documents define work order.

Current architecture documents must not describe completed migration work, obsolete names, or previous designs as part of the active model.

Future concepts must be explicitly marked as future architecture. They must not be described as implemented Runtime behavior until the corresponding code exists.

The glossary must define terms only. It must not argue for design choices, duplicate guideline rules, or contain implementation steps.

Naming guidelines must stay focused on names. Ownership, validity, dependency, and error-handling rules belong in their dedicated guideline documents.

Implementation plans must not redefine architecture. They should reference architecture documents and describe the next concrete work sequence.

## Primary invariants

Accepted domain objects must be valid after construction.

Descriptors are symbolic input and are not accepted requests.

Catalogs own accepted definition objects and validate exact ownership.

Recipes describe generation templates and do not represent one run.

Requests describe one accepted resolved generation run and contain no unresolved symbols.

Plans are managed semantic data and contain no native execution state.

Resource definitions describe generated values semantically.

Field definitions are future storage-facing metadata.

Native containers belong to future workspace execution, not catalog or planning.

Schedulers own future execution control flow, dependency wiring, and job scheduling.

Jobs must not know symbols, catalogs, recipes, requests, plans, resources, field definitions, workspaces, or schedulers.

Jobs receive native containers and unmanaged values only.

```