# ADR-002 Unity Package and Assembly Boundaries

Status: Accepted  
Date: 2026-05-17

## Context

Atlas is a Unity 6.4 package. Unity package code must respect Runtime, Editor, Tests, and Documentation separation.

The current Runtime layer is a pure managed domain/planning layer. Later execution work will require Burst, Jobs, Unity Collections, Unity.Mathematics, and possibly ECS/DOTS, but those dependencies must not leak into the current catalog/planning assembly.

## Decision

Atlas uses Unity package boundaries:

```text
Runtime/
Editor/
Tests/Runtime/
Tests/Editor/
Documentation/
Samples~/
```

The current Runtime assembly is:

```text
Runtime/Lokrain.Atlas.asmdef
```

It should be configured as:

```json
{
  "name": "Lokrain.Atlas",
  "rootNamespace": "Lokrain.Atlas",
  "references": [],
  "noEngineReferences": true
}
```

The current Runtime assembly must not reference:

```text
UnityEngine
UnityEditor
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Entities
Unity.Mathematics
```

## Future Execution Assemblies

Execution code will be added in a separate assembly when runnable plans, workspaces, and jobs exist.

A later execution assembly may reference:

```text
Lokrain.Atlas
Unity.Burst
Unity.Collections
Unity.Jobs
Unity.Mathematics
Unity.Entities, if needed
```

That assembly is not part of the current managed planning layer.

## Editor Rules

Editor code belongs under `Editor/`.

Editor code may use:

```text
UnityEditor
UnityEngine
ScriptableObject authoring assets
inspectors
windows
importers
```

ScriptableObjects are authoring adapters, not canonical runtime generation state.

## Test Rules

Managed primitives, catalog definitions, recipes, descriptor resolution, accepted requests, and plan compilation should be tested without Unity runtime dependencies.

Editor authoring adapters should be tested under `Tests/Editor/` when they exist.

Job, workspace, and runnable execution tests are added only after execution infrastructure exists.

## Consequences

The current Runtime assembly remains highly testable and independent from Unity runtime APIs.

Execution dependencies are introduced at the correct boundary.

Folder and namespace layout must support later assembly splits.

## Rejected Alternatives

### Burst/Collections references in the current Runtime assembly

Rejected because the current layer is metadata, descriptor resolution, recipes, and planning only.

### Full asmdef split from day one

Rejected because it creates dependency churn before execution layers exist.

### ScriptableObjects as runtime authority

Rejected because deterministic generation must use accepted runtime definitions, descriptors, recipes, requests, and plans.
