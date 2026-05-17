# ResourceDefinition before FieldDefinition

This document records the decision to model `ResourceDefinition` before introducing `FieldDefinition`.

`ResourceDefinition` is current implemented Runtime architecture.

`FieldDefinition` is future execution architecture.

## Decision

Atlas models semantic generated values with `ResourceDefinition` before modeling storage-facing execution fields with `FieldDefinition`.

Stage and operation contracts use `ResourceDefinition` for required inputs and produced outputs.

`FieldDefinition` must not be introduced into current stage or operation contracts.

The required boundary is:

```text
ResourceDefinition
        |
        v
FieldDefinition
        |
        v
GenerationWorkspace allocation
````

A resource defines what generated value exists.

A field definition defines how that value is represented for execution.

A workspace allocation owns the native storage for one run.

## Status

Accepted.

## Context

The current Runtime architecture supports managed generation planning.

The managed pipeline is:

```text
GenerationRequestDescriptor
        +
GenerationCatalog
        |
        v
GenerationRequestResolver
        |
        v
GenerationRequest
        |
        v
GenerationPlanCompiler
        |
        v
GenerationPlan
```

This pipeline needs to reason about semantic data flow.

It must know which generated values are required and produced by stages and operations.

It does not need to know native storage layout, allocation policy, scheduler binding, job dependencies, or artifact capture behavior.

## Problem

Stage and operation contracts need stable resource identity.

The architecture must express facts such as:

```text
operation A requires Height
operation A produces Land

operation B requires Land
operation B produces Coast
```

These are semantic planning facts.

They should be valid before execution storage is designed.

If contracts use raw symbols, the catalog cannot validate resource ownership strongly enough.

If contracts use field definitions too early, managed planning becomes coupled to storage and execution design.

## Required separation

The architecture must separate these questions:

| Question                                                  | Owner                        |
| --------------------------------------------------------- | ---------------------------- |
| What semantic generated value is required or produced?    | `ResourceDefinition`         |
| How is that value represented for execution?              | Future `FieldDefinition`     |
| Which native container stores it for one run?             | Future `GenerationWorkspace` |
| Which operation schedules the jobs that read or write it? | Future `OperationScheduler`  |
| Which job transforms the data?                            | Future job structs           |

A current managed plan needs the first answer only.

Future execution needs all answers, but at later boundaries.

## ResourceDefinition responsibility

`ResourceDefinition` owns semantic identity for generated values.

It should contain package-owned metadata such as:

```text
Symbol
DisplayName
GenerationSchemaDefinition
```

It must not contain:

```text
FieldDefinition
NativeArray<T>
NativeList<T>
FieldHandle
SchedulerBinding
JobHandle
OperationScheduler
artifact output path
Unity object identity
```

A resource definition is catalog-owned metadata.

It is not storage and not executable state.

## FieldDefinition responsibility

`FieldDefinition` is planned future metadata.

It will describe storage-facing representation for a resource.

It may eventually contain or reference:

```text
ResourceDefinition
FieldLifetime
FieldShape
ValueKind
ExecutionProfile
StoragePolicy
CapturePolicy
```

It must not allocate memory.

It must not schedule jobs.

It must not replace `ResourceDefinition` as semantic identity.

## Workspace responsibility

`GenerationWorkspace` is planned future execution state.

It will allocate and own native containers for one generation run.

The workspace may allocate storage based on:

```text
RunnablePlan
FieldDefinition
ExecutionProfile
Grid
external input bindings
artifact capture requirements
```

The workspace must not resolve request symbols, select recipes, or compile managed plans.

## Consequences

`StageContract` and `OperationContract` use `ResourceDefinition`.

Correct current contract model:

```text
StageContract
  RequiredInputs  -> ResourceDefinition list
  ProducedOutputs -> ResourceDefinition list

OperationContract
  RequiredInputs  -> ResourceDefinition list
  ProducedOutputs -> ResourceDefinition list
```

Incorrect current contract models:

```text
StageContract
  RequiredInputSymbols
  ProducedOutputSymbols

OperationContract
  RequiredFields
  ProducedFields

OperationContract
  NativeInputs
  NativeOutputs
```

The current contract layer stays semantic.

Future runnable compilation will bridge semantic resources to execution fields.

## Catalog validation consequence

Because contracts use `ResourceDefinition`, the catalog can validate exact resource ownership.

The catalog can reject:

```text
operation contract from catalog A
  references resource definition from catalog B
```

This is stronger than symbol-based validation.

Symbol equality alone is not enough because two catalog instances may contain equivalent-looking definitions with different ownership.

## Request resolution consequence

Request resolution remains about symbolic request satisfiability.

The resolver resolves:

```text
recipe symbol
route-step override symbol
implementation symbol
```

It does not resolve field definitions, native storage, scheduler bindings, or job dependencies.

Missing field definitions are not request-resolution errors.

They belong to future runnable plan compilation.

## Plan compilation consequence

`GenerationPlanCompiler` consumes an accepted `GenerationRequest`.

It compiles a managed semantic plan that carries resource-definition-based contracts.

It does not bind resources to fields.

It does not allocate storage.

It does not schedule jobs.

It does not choose scheduler bindings.

## Future runnable compilation consequence

Future `RunnablePlanCompiler` will bridge current semantic planning and future execution metadata.

Expected future binding:

```text
ResourceDefinition -> FieldDefinition
OperationImplementationDefinition -> SchedulerBinding
OperationPlanNode -> RunnableOperation
```

The runnable compiler will validate that the selected execution configuration can represent and execute the managed plan.

Examples of future runnable compilation failures:

```text
missing field definition for resource
field definition incompatible with execution profile
missing scheduler binding for selected implementation
scheduler binding incompatible with field value kind
external input binding missing
```

These failures are not catalog construction errors and not request-resolution errors.

## Rejected option: raw resource symbols in contracts

Contracts could use raw symbols:

```text
RequiredInputSymbols
ProducedOutputSymbols
```

This option is rejected.

Raw symbols are too weak for accepted contract metadata.

Problems:

```text
catalog ownership cannot be validated by object identity
contracts remain detached from accepted resource definitions
resource metadata is unavailable to plans
typos become harder to localize
future resource-to-field binding has weaker inputs
tests can pass with symbol strings that do not belong to the catalog graph
```

Symbols remain valid for descriptors and lookup.

Accepted contracts should use accepted resources.

## Rejected option: FieldDefinition in current contracts

Contracts could reference `FieldDefinition` directly.

This option is rejected for current architecture.

Problems:

```text
managed planning becomes coupled to storage design
field lifetime leaks into semantic contracts
execution profile policy leaks into catalog definitions
native representation decisions become prerequisites for resource planning
contract tests must change when storage representation changes
future scheduler constraints pollute current stage/operation definitions
```

`FieldDefinition` belongs after semantic planning, not before it.

## Rejected option: ResourceDefinition owns FieldDefinition

A resource could directly own one field definition.

This option is rejected.

Problems:

```text
one semantic resource may require multiple representations
storage representation may vary by execution profile
debug and production profiles may capture different fields
external input fields may use different ownership policy
payload fields may be derived from canonical resources
resource identity would change when storage policy changes
```

A resource should remain stable even when execution representation changes.

## Rejected option: GenerationPlan stores field handles

A managed plan could store future field handles.

This option is rejected.

Problems:

```text
field handles are per-workspace execution state
managed plans should be reusable semantic data
workspace allocation has not happened during managed plan compilation
field handle identity is not semantic identity
plan tests would depend on execution allocation order
```

Field handles belong to future workspace execution.

## Rejected option: OperationImplementationDefinition owns scheduler code

An operation implementation definition could directly own executable scheduler or job code.

This option is rejected for current managed metadata.

Problems:

```text
operation implementation metadata becomes execution code
Runtime planning becomes coupled to Jobs/Burst assemblies
scheduler/profile compatibility cannot be selected independently
tests for recipe and request logic require execution infrastructure
future execution backends become harder to swap
```

Current operation implementation definitions identify selectable implementation choices.

Future scheduler bindings map those choices to executable schedulers.

## Accepted current model

The accepted current model is:

```text
ResourceDefinition
  semantic generated value

StageContract / OperationContract
  resource-definition-based semantic flow

GenerationCatalog
  validates exact resource ownership

GenerationRequest
  accepted resolved run intent

GenerationPlan
  managed semantic plan with resource contracts
```

This model is complete for current managed planning.

## Accepted future model

The accepted future model is:

```text
GenerationPlan
        +
FieldDefinitionSet
        +
ExecutionProfile
        +
SchedulerBindingCatalog
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
Jobs
```

Future execution will bind semantic resources to storage-facing fields without changing current resource identity.

## Invariants

The decision requires these invariants:

```text
ResourceDefinition is semantic identity.
StageContract and OperationContract use ResourceDefinition.
Catalog validates contract resource ownership.
GenerationPlan remains managed semantic data.
FieldDefinition remains future storage-facing metadata.
GenerationWorkspace owns future native storage.
Schedulers own future job orchestration.
Jobs receive native containers and unmanaged values only.
```

## Impact on documentation

Current architecture documents must describe `ResourceDefinition` as implemented current architecture.

Future documents may describe `FieldDefinition` only as planned architecture.

Documentation must not describe field definitions as current contract members.

Documentation must not describe resources as native storage.

Documentation must not describe managed plans as executable job data.

## Impact on tests

Current tests should verify:

```text
ResourceDefinition local validation
catalog resource ownership
catalog resource lookup
stage contracts use resource definitions
operation contracts use resource definitions
plans preserve resource-definition-based contracts
landmass module exposes LandmassResourceDefinitions
old symbolic resource contract names are absent from Runtime/Test code
```

Future tests should verify resource-to-field binding only when future field and runnable compilation code exists.

## Review checklist

Before changing this decision, verify:

```text
The change does not weaken catalog ownership validation.
The change does not make contracts raw-symbol based.
The change does not make current contracts field-based.
The change does not put native storage into managed plans.
The change does not make resources execution-profile dependent.
The change does not make jobs aware of resources or symbols.
The change preserves a clear future bridge from resources to fields.
```

## Summary

`ResourceDefinition` comes before `FieldDefinition` because managed planning needs semantic generated-value identity before execution storage exists.

Resources define what values are required and produced.

Fields will define how those values are represented for execution.

Workspaces will allocate native storage.

Schedulers will schedule jobs.

Jobs will transform native data.

Keeping these boundaries separate makes the current managed architecture complete while leaving room for future execution design.

```
