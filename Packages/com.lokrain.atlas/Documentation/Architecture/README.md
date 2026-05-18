# Lokrain.Atlas architecture documentation

This documentation set defines the current Lokrain.Atlas Runtime architecture, the current managed runnable-plan bridge, and the planned execution infrastructure that remains outside the implemented boundary.

Current Runtime architecture includes managed domain objects, catalog validation, request resolution, managed semantic plan compilation, managed field metadata, managed execution-profile identity, and managed runnable metadata compilation.

Current Runtime still stops before runtime execution. `GenerationWorkspace`, `OperationScheduler`, native storage ownership, Burst jobs, artifact capture execution, runtime diagnostic capture, and ECS execution integration remain planned architecture.

## Read first

- [Atlas Architecture Overview](Overview/Atlas%20Architecture%20Overview.md)
- [Managed Generation Pipeline](Overview/Managed%20Generation%20Pipeline.md)
- [Accepted Domain Object Model](Concepts/Accepted%20Domain%20Object%20Model.md)
- [Resource, Field, and Workspace Boundary](Concepts/Resource%20Field%20and%20Workspace%20Boundary.md)
- [Managed Field Metadata and Execution Profiles](Concepts/Managed%20Field%20Metadata%20and%20Execution%20Profiles.md)
- [Runnable Plan Compilation](Concepts/Runnable%20Plan%20Compilation.md)

## Guidelines

- [Architecture Rules](Guidelines/Architecture%20Rules.md)
- [Domain Object Rules](Guidelines/Domain%20Object%20Rules.md)
- [Catalog Ownership Rules](Guidelines/Catalog%20Ownership%20Rules.md)
- [Runtime Boundary Rules](Guidelines/Runtime%20Boundary%20Rules.md)
- [Naming Guidelines](Guidelines/Naming%20Guidelines.md)
- [Dependency Rules](Guidelines/Dependency%20Rules.md)
- [Error Handling Rules](Guidelines/Error%20Handling%20Rules.md)

## Reference

- [Glossary](Reference/Glossary.md)

## Future architecture

- [Scheduler Workspace and Job Ownership](Future/Scheduler%20Workspace%20and%20Job%20Ownership.md)
- [Low-Level Native Memory and Unsafe Collections](Future/Low-Level%20Native%20Memory%20and%20Unsafe%20Collections.md)

## Decisions and plans

- [ResourceDefinition Before FieldDefinition](Decisions/ResourceDefinition%20Before%20FieldDefinition.md)
- [Rejections and Deferrals](Decisions/Rejections%20and%20Deferrals.md)
- [Implementation Plan](Plans/Implementation%20Plan.md)

## Current Runtime invariants

Current Runtime code must preserve these invariants:

```text
Accepted objects validate their own construction invariants.
GenerationCatalog owns semantic inventory only.
GenerationCatalog does not own FieldDefinition or ExecutionProfile.
StageContract and OperationContract use ResourceDefinition inputs and outputs.
GenerationRequestDescriptor is symbolic caller intent.
GenerationRequest contains accepted resolved intent.
GenerationPlan is managed semantic order, not runtime execution state.
FieldDefinitionSet owns managed field metadata and deterministic field-symbol order.
ExecutionProfileSet owns managed execution-profile identity and deterministic profile-symbol order.
RunnablePlanCompiler consumes GenerationPlan, FieldDefinitionSet, and ExecutionProfile.
RunnablePlan is managed executable metadata, not runtime execution state.
ResourceFieldBinding connects ResourceDefinition to FieldDefinition by reference-exact ownership.
FieldIndex, StageIndex, and OperationIndex are dense plan-local table positions, not durable identities.
FieldPlanRole describes plan input/output role, not storage lifetime.
FieldCapturePolicy records future capture intent, not capture execution.
Dictionaries and hash sets are lookup or membership indexes only, never public order.
Workspace allocation, scheduler execution, jobs, native storage, ECS, artifacts, and diagnostics remain outside the current implemented boundary.
```
