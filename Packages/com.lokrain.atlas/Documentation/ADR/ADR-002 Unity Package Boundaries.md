# ADR-002 Unity Package Boundaries

Status: Accepted  
Date: 2026-05-17

## Context

Atlas is a Unity 6.4 package. Unity package code must respect Runtime, Editor, and Tests separation. Runtime deterministic generation must not depend on Editor tooling, and planning code must not leak Burst/job execution concerns.

## Decision

Atlas uses Unity package boundaries:

```text
Runtime/
Editor/
Tests/Runtime/
Tests/Editor/
Documentation~/
Samples~/
```

Initial assembly definition strategy:

```text
Runtime/Lokrain.Atlas.asmdef
Tests/Runtime/Lokrain.Atlas.Tests.asmdef
```

Additional asmdefs are introduced only when dependency pressure exists.

## Runtime Rules

Runtime code must not reference `UnityEditor`.

Core, schemas, catalog, and planning must not reference:

```text
UnityEngine
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Entities
```

Execution may reference:

```text
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Mathematics
```

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

ScriptableObjects are authoring adapters. They are not canonical runtime generation state.

## Test Rules

Pure managed primitives, catalog definitions, and planning compilers should be tested in runtime tests.

Editor authoring adapters should be tested under `Tests/Editor/` when they exist.

Job/workspace/runtime execution tests are added when `RunnablePlanCompiler` and execution infrastructure exist.

## Consequences

Folder layout and namespace layout must support later asmdef splits.

No Runtime type may require Editor-only dependencies.

Planning and Execution remain separate assemblies when the dependency split becomes useful.

## Rejected Alternatives

### Full asmdef split from day one

Rejected because it creates dependency churn before the layer boundaries are populated.

### One monolithic folder without layer separation

Rejected because it makes Unity assembly splits difficult later.

### ScriptableObjects as runtime authority

Rejected because runtime deterministic generation must use pure runtime definitions and compiled plans.
