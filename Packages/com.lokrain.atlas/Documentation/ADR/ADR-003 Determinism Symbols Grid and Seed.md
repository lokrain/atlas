# ADR-003 Determinism, Symbols, Grid, and Seed

Status: Accepted  
Date: 2026-05-17

## Context

Atlas must generate deterministic map output from explicit inputs. The package must distinguish stable machine-facing tokens, user-facing display names, grid dimensions, and deterministic seeds.

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

## Symbol

`Symbol` is a stable machine-facing token.

It is syntax only.

It is not identity, not kind, and not display name.

Domain types contain `Symbol`.

## DisplayName

`DisplayName` is user-facing metadata.

It must not affect deterministic generation.

## Grid

`Grid` uses terrain terminology:

```text
Width
Depth
CellCount
```

Atlas does not use `Height` for horizontal dimensions. Height means elevation.

`Grid` owns coordinate/index bounds and conversion.

## Cell and CellIndex

`Cell` and `CellIndex` are created through `Grid`.

They are not hot-loop job types.

Execution jobs use raw integer dimensions and indexes.

## Seed

`Seed` is deterministic generation input.

It is not request identity and not display name.

Seed derivation must be package-owned and deterministic.

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

`SymbolId` is deferred until `RunnablePlanCompiler`.

Planning uses `Symbol`.

Runnable plans may introduce numeric IDs.

Jobs may use numeric IDs only.

## Rejected Alternatives

### Kind inherits Symbol

Rejected because symbol syntax and domain meaning are different concepts.

### Symbol<T>

Rejected because it adds public generic ceremony without improving the domain model.

### Map height as horizontal dimension

Rejected because height means elevation in terrain generation.
