# Unity Package Layout

Package: `com.lokrain.atlas`

## Purpose

This document defines the Unity package folder and assembly strategy for Atlas.

Atlas follows Unity package conventions while keeping runtime deterministic generation separate from editor authoring and execution-specific Burst/job code.

## Package Root

```text
Packages/com.lokrain.atlas/
  package.json
  Runtime/
  Editor/
  Tests/
  Documentation~/
  Samples~/
```

## Runtime Layout

```text
Runtime/
  Lokrain.Atlas.asmdef

  Core/
  Schemas/
  Stages/
  Operations/
  Catalog/
  Planning/
  Generation/
  Execution/
  Fields/
  Artifacts/
```

Initial package work should include only folders that have real source files.

Do not create empty architecture folders just to show intent.

## Editor Layout

```text
Editor/
  Lokrain.Atlas.Editor.asmdef later
```

Editor code is introduced only when authoring assets, inspectors, windows, or importers exist.

Runtime must not reference Editor.

## Tests Layout

```text
Tests/
  Runtime/
    Lokrain.Atlas.Tests.asmdef
  Editor/
    Lokrain.Atlas.Editor.Tests.asmdef later
```

Pure managed runtime tests belong in `Tests/Runtime`.

Editor tooling tests belong in `Tests/Editor` after editor tooling exists.

## Documentation Layout

```text
Documentation~/
  README.md
  Architecture/
  ADR/
  Design/
  Plans/
  Templates/
```

`Documentation~` is the package documentation folder.

## Assembly Strategy

Start with:

```text
Runtime/Lokrain.Atlas.asmdef
Tests/Runtime/Lokrain.Atlas.Tests.asmdef
```

Split later only when dependency pressure exists.

Expected future split:

```text
Lokrain.Atlas.Core
Lokrain.Atlas.Planning
Lokrain.Atlas.Generation
Lokrain.Atlas.Execution
Lokrain.Atlas.Editor
```

## Dependency Direction

```text
Core
  no package layer dependencies

Schemas / Stages / Operations
  depend on Core

Catalog
  depends on Core, Schemas, Stages, Operations

Planning
  depends on Core, Catalog, Schemas, Stages, Operations

Generation/Landmass
  depends on Core, Stages, Operations

Execution
  depends on planning output and Unity job/native packages

Editor
  depends on runtime assemblies
```

## Forbidden Runtime Dependencies

The following are forbidden in Core, Schemas, Stages, Operations, Catalog, and Planning:

```text
UnityEditor
UnityEngine
Unity.Collections
Unity.Jobs
Unity.Burst
Unity.Entities
```

Execution may depend on Unity.Collections, Unity.Jobs, Unity.Burst, and Unity.Mathematics.

Editor may depend on UnityEditor.
