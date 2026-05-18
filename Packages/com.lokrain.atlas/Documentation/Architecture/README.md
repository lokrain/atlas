# Lokrain.Atlas architecture documentation

This documentation set defines the current Lokrain.Atlas Runtime architecture, the planned execution architecture, and the rules used to keep both boundaries separate.

Current Runtime architecture includes managed domain objects, catalog validation, request resolution, managed plan compilation, managed field metadata, and managed execution profile identity.

Current Runtime architecture still stops before runnable execution. `RunnablePlanCompiler`, `RunnablePlan`, `GenerationWorkspace`, `OperationScheduler`, native storage, Burst jobs, artifact capture, diagnostics, and ECS execution integration remain planned architecture.

## Read first

- [Atlas Architecture Overview](Overview/Atlas%20Architecture%20Overview.md)
- [Managed Generation Pipeline](Overview/Managed%20Generation%20Pipeline.md)
- [Accepted Domain Object Model](Concepts/Accepted%20Domain%20Object%20Model.md)
- [Resource, Field, and Workspace Boundary](Concepts/Resource%20Field%20and%20Workspace%20Boundary.md)
- [Managed Field Metadata and Execution Profiles](Concepts/Managed%20Field%20Metadata%20and%20Execution%20Profiles.md)

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

- [Runnable Plan Compilation](Future/Runnable%20Plan%20Compilation.md)
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
StageContract and OperationContract use ResourceDefinition inputs and outputs.
GenerationRequestDescriptor is symbolic caller intent.
GenerationRequest contains accepted resolved intent.
GenerationPlan is managed semantic order, not executable metadata.
FieldDefinitionSet owns managed field metadata and deterministic symbol order.
ExecutionProfileSet owns managed execution profile identity and deterministic symbol order.
Dictionaries and hash sets are lookup or membership indexes only, never public order.
Runnable execution remains outside the current implemented boundary.
```
