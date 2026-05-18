# ResourceDefinition before FieldDefinition

This decision record explains why Lokrain.Atlas models semantic resources before storage-facing fields.

## Decision

Lokrain.Atlas defines `ResourceDefinition` as semantic Runtime architecture before using `FieldDefinition` as managed field metadata.

`ResourceDefinition` is semantic metadata.

`FieldDefinition` is current managed field metadata.

Current contracts use `ResourceDefinition` for required inputs and produced outputs.

Future runnable compilation may bind resources to field definitions.

## Status

Accepted.

`ResourceDefinition` is implemented.

`FieldDefinition` is implemented as managed metadata.

Current semantic planning ends at `GenerationPlan`; managed field metadata is current Runtime metadata used by future runnable compilation.

## Context

Generation architecture needs two separate concepts:

```text
what generated value exists
how that value is represented for execution
```

These are not the same responsibility.

A semantic resource is stable across execution profiles, storage layouts, diagnostic policies, and implementation choices.

A field representation may vary by execution profile, platform, memory policy, diagnostic mode, or implementation strategy.

Examples:

```text
BaseElevation
ContinentSuitability
ContinentCandidate
MainContinent
ContinentalLandmassArea
```

These names describe semantic generated values.

They do not require one fixed storage layout.

## Problem

If storage-facing fields are introduced before semantic resources, contracts become coupled to execution details too early.

Invalid model:

```text
OperationContract
  RequiredInputs: FieldDefinition
  ProducedOutputs: FieldDefinition
```

This makes operation and stage contracts depend on storage representation.

It also forces current managed planning to know concepts that belong to planned execution:

```text
field shape
field value kind
workspace allocation
field handles
native containers
scheduler bindings
artifact capture
diagnostic capture
```

That violates the current Runtime boundary.

## Chosen model

Use `ResourceDefinition` for semantic identity.

Use resource definitions in contracts.

```text
StageContract
  RequiredInputs: IReadOnlyList<ResourceDefinition>
  ProducedOutputs: IReadOnlyList<ResourceDefinition>

OperationContract
  RequiredInputs: IReadOnlyList<ResourceDefinition>
  ProducedOutputs: IReadOnlyList<ResourceDefinition>
```

Keep field definitions outside semantic contracts. Use them as managed metadata when compiling a managed plan into future executable metadata.

Planned runnable-compilation model:

```text
GenerationPlan
  + FieldDefinitionSet
  + ExecutionProfile
    -> RunnablePlanCompiler
    -> RunnablePlan
    -> GenerationWorkspace
```

## Why this is correct

Semantic generation planning must be valid before execution storage exists.

A recipe should be able to say:

```text
EvaluateContinentSuitability produces ContinentSuitability.
FormContinentCandidate requires ContinentSuitability and produces ContinentCandidate.
```

That statement is true regardless of whether the future execution representation is:

```text
dense cell-grid field
bit mask
compressed sparse structure
temporary native buffer
external field binding
diagnostic capture field
```

The semantic dependency does not change when storage changes.

## ResourceDefinition responsibilities

`ResourceDefinition` owns:

```text
stable resource symbol
display name
generation schema
semantic generated-value identity
contract resource-flow identity
catalog ownership participation
```

`ResourceDefinition` does not own:

```text
field shape
field value kind
native container type
allocation policy
field handle
workspace storage
scheduler binding
job dependency
artifact payload
diagnostic capture
```

## FieldDefinition responsibilities

`FieldDefinition` owns current managed field metadata.

A field definition describes:

```text
resource mapping
field symbol
display name
field shape
field value kind
```

Runnable compilation may combine field definitions with execution profiles, implementation metadata, scheduler bindings, capture policy, and diagnostics policy. Those later execution concerns do not belong to `FieldDefinition`.

A field definition must not replace resource identity.

A field definition must not allocate storage.

Actual allocation belongs to planned `GenerationWorkspace`.

## Catalog impact

`GenerationCatalog` owns resource definitions as current accepted inventory.

Catalog validation can verify:

```text
resource symbol uniqueness
resource schema ownership
contract resource ownership
stage resource flow
operation resource flow
recipe graph consistency
cross-catalog resource reference rejection
```

Catalog validation does not need field definitions to validate semantic resource flow.

This keeps catalog validation independent from execution policy.

## Recipe impact

Recipes select semantic generation flow.

A recipe should not depend on a specific storage layout.

Correct:

```text
GenerationRecipeDefinition
  selects stage routes
  selects stage contracts
  selects operation contracts
  selects default implementation choices
```

Incorrect:

```text
GenerationRecipeDefinition
  selects field handles
  selects native containers
  selects workspace allocation layout
```

Storage choices belong after managed plan compilation.

## Request impact

Requests represent one accepted resolved run.

A request contains:

```text
GenerationRecipeDefinition
GenerationRunSettings
final StageRouteStepImplementationChoice list
```

A request does not contain:

```text
FieldDefinition
FieldHandle
NativeArray<T>
GenerationWorkspace
OperationScheduler
```

This keeps request resolution focused on symbolic intent and accepted definitions.

## Plan impact

`GenerationPlan` is managed semantic output.

It can carry resource-definition-based contracts through stage and operation plan nodes.

It must not contain field handles or native storage.

Correct:

```text
GenerationPlan
  StagePlanNode
    OperationPlanNode
      OperationContract
        ResourceDefinition inputs and outputs
```

Incorrect:

```text
GenerationPlan
  OperationPlanNode
    FieldHandle inputs and outputs
```

Field binding belongs to planned runnable compilation.

## Execution impact

Future execution can bind semantic resources to storage representations after the managed plan is valid.

Planned flow:

```text
GenerationPlan
  -> RunnablePlanCompiler
     uses FieldDefinitionSet
     uses ExecutionProfile
  -> RunnablePlan
  -> GenerationWorkspace
  -> OperationScheduler
  -> Jobs
```

This allows different execution profiles to use different storage policy without changing the semantic recipe.

## Alternatives considered

### Use raw symbols in contracts

Rejected.

Raw symbol lists do not carry accepted object ownership.

They make contract validation weaker and push semantic validation later.

Invalid model:

```text
RequiredInputSymbols
ProducedOutputSymbols
```

Contracts now use `ResourceDefinition` directly.

### Use FieldDefinition in contracts

Rejected.

It couples semantic contracts to storage-facing execution metadata.

It also introduces future execution dependencies into current Runtime planning.

### Make ResourceDefinition own FieldDefinition

Rejected.

A resource can have different field representations under different execution profiles.

A resource is semantic identity, not a storage owner.

Invalid model:

```text
ResourceDefinition.FieldDefinition
```

### Make GenerationPlan own field handles

Rejected.

`GenerationPlan` is managed semantic data.

Field handles belong to planned workspace execution.

Invalid model:

```text
GenerationPlan.FieldHandles
```

### Delay ResourceDefinition until FieldDefinition exists

Rejected.

Current catalog, recipe, request, and plan architecture needs semantic resource flow now.

Waiting for field definitions would block valid managed planning and force contracts to use raw symbols or storage concepts.

## Consequences

This decision creates a stable semantic layer before execution architecture.

Benefits:

```text
contracts are storage-independent
catalog validation can validate resource ownership now
recipes remain reusable across execution profiles
requests remain accepted semantic run intent
plans remain managed semantic output
future field definitions can evolve without changing semantic contracts
```

Trade-offs:

```text
future runnable compilation must bind resources to fields explicitly
runnable compilation must validate field coverage for the selected execution profile
tests must distinguish resource identity from field representation
documentation must clearly mark fields as planned
```

The trade-offs are intentional.

## Rules

Use `ResourceDefinition` for current semantic resource flow.

Do not reintroduce raw resource symbol lists in contracts.

Do not add `FieldDefinition` to current contracts.

Do not add field handles to current managed plans.

Do not add native storage to resources, contracts, recipes, requests, or plans.

Keep field definitions as managed metadata outside contracts, catalogs, recipes, requests, and plans; use them during runnable compilation after semantic planning is stable.

## Correct examples

Current semantic contract:

```text
OperationContract
  OperationDefinition: FormContinentCandidate
  RequiredInputs:
    ContinentSuitability
  ProducedOutputs:
    ContinentCandidate
```

Planned field binding:

```text
ResourceDefinition ContinentSuitability
  -> FieldDefinition ContinentSuitabilityField
  -> workspace field allocation
```

Planned runnable operation:

```text
RunnableOperation FormContinentCandidate
  input field binding: ContinentSuitabilityField
  output field binding: ContinentCandidateField
```

## Incorrect examples

Do not model current contracts like this:

```text
OperationContract
  RequiredInputSymbols:
    lokrain.atlas.landmass.resource.continent_suitability
```

Do not model resources like this:

```text
ResourceDefinition
  FieldDefinition
  NativeArray<T>
  FieldHandle
```

Do not model plans like this:

```text
GenerationPlan
  RunnableOperation
  FieldHandle
  JobHandle
```

## Implementation guidance

Current implementation should remain focused on:

```text
ResourceDefinition
StageContract
OperationContract
GenerationCatalog
GenerationRecipeDefinition
GenerationRequest
GenerationPlan
```

Future implementation should proceed in this order:

```text
FieldDefinition
FieldDefinitionSet
ExecutionProfile
RunnablePlanCompiler
RunnablePlan
GenerationWorkspace
OperationScheduler
Jobs
```

Do not skip directly from `GenerationPlan` to native execution without explicit runnable metadata and workspace ownership.

## Validation checklist

Before changing resource or field architecture, verify:

```text
Contracts still use ResourceDefinition.
Catalog ownership remains reference-exact.
Resources do not contain storage metadata.
Recipes do not contain execution policy.
Requests do not contain field handles.
Plans do not contain native storage.
FieldDefinition is implemented managed metadata.
RunnablePlanCompiler owns resource-to-field binding.
GenerationWorkspace owns native allocation.
OperationScheduler owns job scheduling.
```

## Summary

`ResourceDefinition` comes before `FieldDefinition` because semantic resource flow must be valid before field representation participates in runnable compilation.

Resources define what generated values are.

Fields define how those values are represented for execution.

Current Runtime owns resources.

Future execution owns fields, workspaces, schedulers, and jobs.

## Current implementation note

`FieldDefinition`, `FieldDefinitionSet`, `FieldShape`, `FieldValueKind`, `ExecutionProfile`, and `ExecutionProfileSet` are now implemented managed metadata.

The decision still stands: contracts use `ResourceDefinition`, not `FieldDefinition`; `GenerationCatalog` owns semantic inventory, not field or execution profile metadata; and runnable execution remains planned.
