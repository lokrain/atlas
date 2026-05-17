# ADR-004 Landmass Primary Continental Recipe Placement

Status: Accepted  
Date: 2026-05-17

## Context

Atlas starts with world-like map generation. The first built-in generation module is Landmass. The first Landmass recipe is the Primary Continental Landmass recipe.

The architecture must avoid hardcoding Landmass vocabulary into generic infrastructure.

## Decision

Landmass vocabulary belongs under:

```text
Runtime/Generation/Landmass/
```

Generic infrastructure must not expose Landmass constants.

The first Landmass recipe is:

```text
lokrain.atlas.landmass.recipe.primary_continental_landmass
```

The first Landmass stage is:

```text
lokrain.atlas.landmass.stage.continental_landmass
```

The first Landmass route is:

```text
lokrain.atlas.landmass.route.primary_continental_landmass
```

## Ownership

```text
World schema
  shared generation schema

ContinentalLandmass StageKind
  semantic stage category

ContinentalLandmass StageDefinition
  catalog-owned stage definition

PrimaryContinentalLandmass StageRouteDefinition
  selected route for the continental landmass stage

PrimaryContinentalLandmass GenerationRecipeDefinition
  resolved recipe selecting the stage route and default implementations
```

## Operation Chain

The Primary Continental Landmass route uses this operation chain:

```text
EvaluateContinentSuitability
FormContinentCandidate
ExtractMainContinent
CompleteContinentArea
ComposeBaseElevation
```

The symbolic resource chain is:

```text
<none>
  -> continent_suitability
  -> continent_candidate
  -> main_continent
  -> continental_landmass_area
  -> base_elevation
```

## Naming Rules

Use:

```text
ExtractMainContinent
main_continent_extraction
extract_main_continent
```

Do not use old preserve/preservation terminology for this operation.

Use `PrimaryContinentalLandmass`, not `PrimaryContinent`, for route and recipe names.

## Consequences

Landmass built-ins remain optional module definitions that can be registered into a catalog.

Generic planning remains reusable for future modules such as climate, hydrology, biomes, and presentation payload generation.

Recipes provide stable symbolic entry points for descriptor-based generation.

## Rejected Alternatives

### Generic infrastructure owns Landmass constants

Rejected because product-specific vocabulary must not leak into core planning APIs.

### Landmass as a schema

Rejected because Landmass is a generation domain/stage concern, not a top-level schema by itself.

### Operation chain as compiler hardcoding

Rejected because operation sequencing belongs to route and recipe definitions.
