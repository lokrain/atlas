# ADR-004 Landmass and PrimaryContinent Route Placement

Status: Accepted  
Date: 2026-05-17

## Context

Atlas starts with Earth-like map generation. The first schema requires Landmass. The first Landmass strategy is PrimaryContinent.

The architecture must avoid hardcoding product-specific Landmass vocabulary into generic infrastructure.

## Decision

Landmass vocabulary belongs under:

```text
Runtime/Generation/Landmass/
```

Generic infrastructure must not expose Landmass constants.

PrimaryContinent is a stage route, not a schema, not a stage kind, and not an operation kind.

## Ownership

```text
Earth schema
  requires StageKind landmass

Landmass StageKind
  semantic category

Landmass StageDefinition
  reusable catalog stage definition
  Kind = landmass
  Route = landmass.primary_continent

PrimaryContinent StageRouteDefinition
  StageKind = landmass
  RequiredOperationKinds = ordered operation chain
```

## PrimaryContinent Operation Chain

The PrimaryContinent route requires:

```text
evaluate_continent_suitability
form_continent_candidate
preserve_main_continent
complete_continent_area
compose_base_elevation
```

The route owns the operation-kind chain.

The Landmass stage kind does not permanently mean PrimaryContinent.

Earth does not know PrimaryContinent.

## Consequences

New Landmass routes can be added without changing the Earth schema.

Generic `Stages` and `Operations` infrastructure remains product-agnostic.

The compiler validates that selected Landmass stage definitions satisfy Earth's required Landmass stage kind.

## Rejected Alternatives

### Put Landmass constants in StageKind

Rejected because generic infrastructure must not own product vocabulary.

### Put PrimaryContinent directly in Earth schema

Rejected because schemas require stage kinds, not routes.

### Put the operation chain directly on Landmass kind

Rejected because different Landmass routes may satisfy Landmass differently.
