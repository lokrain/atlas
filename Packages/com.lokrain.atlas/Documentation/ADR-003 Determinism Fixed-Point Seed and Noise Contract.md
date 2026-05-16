# ADR-003 — Determinism, Fixed-Point, Seed, and Noise Contract

## Status

Accepted.

## Date

2026-05-16

## Context

Atlas canonical generation outputs must be reproducible under the same input contract. Algorithms may use random numbers, noise, reductions, parallel jobs, histograms, connected components, iterative solvers, and tie-break rules. Any of these can introduce nondeterminism if not explicitly constrained.

## Decision

Atlas canonical generation must be deterministic for the same package version, pipeline schema, field contracts, operation contracts, route selection, seed, parameters, dimensions, and input artifacts.

Canonical generation must use integer-first or fixed-point math unless an accepted ADR explicitly permits floating-point for a specific system.

Canonical elevation uses:

```text
Q16.16 signed fixed-point stored as Int32
```

Seeds must use stable Atlas-owned value objects and deterministic derivation. Noise used by canonical generation must be Atlas-owned deterministic noise.

Parallel reductions must use deterministic partial buffers and stable merge order. Tie-breaks must be explicit. Repeated algorithms must be bounded.

## Policies

Jobs must not use `System.Random`, `UnityEngine.Random`, wall-clock time, frame counters, object instance IDs, process-dependent hash codes, or undefined scheduling order.

Design docs may describe algorithms using float-like notation, but implementation must translate that notation into deterministic fixed-point or integer math unless floating-point is explicitly accepted.

Any algorithm that chooses among equal candidates must define a tie-break. No tie-break may rely on thread order, hash map internal order, or job scheduling order.

Repeated algorithms must define maximum pass count, termination condition, no-progress behavior, and failure behavior.

## Invariants

```text
Canonical generation is deterministic for the same complete input contract.
Canonical elevation uses approved fixed representation.
Seeds are stable value objects.
Derived seeds are deterministic and domain-separated.
Canonical noise is Atlas-owned and deterministic.
Parallel reductions use stable merge order.
Tie-breaks are explicit.
Repeated algorithms are bounded.
Jobs do not depend on undefined scheduling order.
Artifact hashes are stable for stable logical content.
```
