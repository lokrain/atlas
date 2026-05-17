# ADR-003 Determinism, Symbols, Run Settings, Grid, and Seed

Status: Accepted  
Date: 2026-05-17

## Context

Atlas must generate deterministic map output from explicit inputs. The package must distinguish stable machine-facing tokens, user-facing display names, spatial run settings, deterministic seeds, and recipe-specific algorithm settings.

## Decision

Atlas defines these core primitives:

```text
Symbol
DisplayName
Map.Grid
Map.Cell
Map.CellIndex
Map.Seed
```

Atlas also defines:

```text
GenerationRunSettings
```

`GenerationRunSettings` contains generation-wide invocation settings for one run. It currently contains:

```text
Grid
Seed
```

## Symbol

`Symbol` is a stable machine-facing token.

It is syntax only.

It is not display text, not a runtime numeric ID, and not an execution handle.

Domain types contain symbols to express meaning.

## DisplayName

`DisplayName` is user-facing metadata.

It must not affect deterministic generation, lookup, artifact compatibility, or execution.

## Grid

`Grid` defines the horizontal map domain and memory shape.

It uses terrain terminology:

```text
Width
Depth
CellCount
LastIndexValue
```

Atlas does not use `Height` for horizontal dimensions. Height means elevation.

`Grid` owns coordinate/index bounds and conversion.

## Cell and CellIndex

`Cell` and `CellIndex` are validated managed map coordinate/index values created through `Grid`.

They are not hot-loop job types.

Execution jobs later use raw dimensions, indexes, and native arrays prepared by runnable/execution layers.

## Seed

`Seed` is the deterministic root seed for a generation run.

`Seed` is a value object. A zero seed is valid.

Seed is not request identity and not display metadata.

Seed derivation must be package-owned and deterministic.

## GenerationRunSettings

`GenerationRunSettings` is not global/static state.

It is per-run invocation data:

```text
GenerationRunSettings(Grid, Seed)
```

Grid and Seed are first-class run settings because they affect the whole run, allocation shape, deterministic root, and all downstream execution.

Recipe-specific algorithm settings are separate and deferred.

## Deferred Recipe Settings

Future setting descriptors may represent values such as:

```text
target continent area ratio
coastline noise strength
smoothing iterations
thresholds
```

Those settings should be selected by symbols and resolved before accepted execution.

They should not replace Grid and Seed.

## Forbidden Determinism Sources

Canonical generation output must not depend on:

```text
Guid request id
DisplayName
object reference identity
dictionary iteration order
system culture
current time
UnityEngine.Random
System.Random without explicit deterministic contract
string.GetHashCode
reflection discovery order
```

## Deferred Runtime IDs

`SymbolId` is deferred until runnable/execution compilation.

Planning uses `Symbol`.

Runnable plans may introduce numeric IDs.

Jobs may use numeric IDs only.

## Rejected Alternatives

### Grid and Seed as generic recipe parameters

Rejected because Grid and Seed are generation-wide invocation settings, not recipe-specific tunables.

### GlobalGenerationSettings name

Rejected because the settings are per run, not global process state.

### Kind inherits Symbol

Rejected because symbol syntax and domain meaning are different concepts.

### Map height as horizontal dimension

Rejected because height means elevation in terrain generation.
