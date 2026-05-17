# Landmass Primary Continent Implementation Plan

## Document Type

Implementation Plan.

## Status

Draft.

## Purpose

This plan defines the implementation order for the `Landmass` stage `PrimaryContinent` route. It is intentionally separate from ADRs and design specifications. It can change as source code and tests reveal implementation constraints.

## Baseline Rule

Do not implement jobs before the field, operation, lifetime, and route contracts are represented in source and protected by tests.

Do not add a field without a producer/consumer contract.

Do not add an operation without tests.

Do not add operation scratch as a field.

## Phase 1 — Field Lifetime and Contract Vocabulary

Goal: make the code capable of expressing canonical and stage-transient fields.

Tasks:

```text
Add or extend field lifetime/category metadata for StageTransient.
Ensure artifact capture excludes StageTransient by default.
Ensure workspace layout can allocate StageTransient fields like normal workspace fields.
Ensure tests prove StageTransient is executable but not captured by default.
```

Expected tests:

```text
StageTransient contracts validate.
StageTransient fields compile into contract table.
StageTransient fields allocate in workspace.
StageTransient fields are excluded from default artifact capture.
```

Checkpoint: Runtime tests pass.

## Phase 2 — Landmass Field Contracts

Goal: define concrete Landmass route fields.

Canonical fields:

```text
field.land.mask
field.ocean.mask
field.land.label
field.base.elevation
```

Stage-transient fields:

```text
transient.continent.suitability
transient.continent.suitability_cutoff
transient.continent.candidate_mask
transient.continent.primary_mask
transient.continent.area
transient.continent.growth_cutoff
```

Expected tests:

```text
All fields exist in catalog.
Formats match contract table.
Shape domains match contract table.
Lifetimes match contract table.
Artifact policies match contract table.
```

Checkpoint: Runtime tests pass.

## Phase 3 — Landmass Operation Contracts

Goal: define operation metadata before executors/jobs.

Operations:

```text
EvaluateContinentSuitability
FormContinentCandidate
PreserveMainContinent
CompleteContinentArea
ComposeBaseElevation
```

Expected tests:

```text
Each operation has stable identity.
Each operation has correct role.
Each operation declares expected access contracts.
Operation ordering passes dataflow validation.
Invalid ordering fails dataflow validation.
```

Checkpoint: Runtime tests pass.

## Phase 4 — Route and Stage Schema

Goal: represent `Landmass` stage and `PrimaryContinent` route.

Tasks:

```text
Define Landmass stage schema.
Define PrimaryContinent route operation sequence.
Reject unsupported routes from runnable plans.
Require canonical Landmass outputs.
```

Expected tests:

```text
PrimaryContinent route compiles.
Missing route operation fails.
Wrong operation order fails.
Unsupported route fails.
Required outputs are enforced.
```

Checkpoint: Runtime tests pass.

## Phase 5 — Operation Scratch Support

Goal: support operation-private temporary native buffers safely.

Tasks:

```text
Add operation scratch allocator/lease if not already present.
Support NativeArray scratch allocation.
Chain scratch disposal into returned JobHandle.
Test disposal safety.
```

Expected tests:

```text
Scratch allocation succeeds.
Scratch disposal is dependency-aware.
Scratch is not a field.
Scratch cannot be captured.
```

Checkpoint: Runtime tests pass.

## Phase 6 — EvaluateContinentSuitability

Goal: first executable Landmass operation.

Scheduler:

```text
EvaluateContinentSuitabilityJobScheduler
```

Jobs:

```text
EvaluateTileContinentSuitabilityJob
AccumulateSuitabilityDistributionJob
SelectCandidateSuitabilityCutoffJob
```

Expected tests:

```text
same seed/params produce same suitability
hard-ocean border excludes cells
histogram merge is deterministic
cutoff selection is deterministic
operation writes suitability and cutoff
```

Checkpoint: Runtime tests pass.

## Phase 7 — FormContinentCandidate

Scheduler:

```text
FormContinentCandidateJobScheduler
```

Job:

```text
MarkCandidateContinentTilesJob
```

Expected tests:

```text
candidate mask follows cutoff
excluded cells never become candidates
operation reads suitability and cutoff
operation writes candidate mask
```

Checkpoint: Runtime tests pass.

## Phase 8 — PreserveMainContinent

Scheduler:

```text
PreserveMainContinentJobScheduler
```

Jobs:

```text
LabelCandidateLandWithinBlocksJob
AssignComponentGlobalRangesJob
LinkComponentsAcrossBlockBordersJob
MergeLinkedLandComponentsJob
MeasureConnectedLandComponentsJob
ChooseMainContinentJob
PreserveMainContinentTilesJob
CountMainContinentTilesJob
```

Expected tests:

```text
block-local labels are deterministic
cross-block links merge correctly
largest component wins
tie resolves by stable component id
primary mask contains only selected component
area equals primary mask count
```

Checkpoint: Runtime tests pass.

## Phase 9 — CompleteContinentArea

Scheduler:

```text
CompleteContinentAreaJobScheduler
```

Repeated jobs:

```text
IdentifyExpandableShoreTilesJob
AccumulateShoreSuitabilityDistributionJob
SelectExpansionSuitabilityCutoffJob
ExpandMainContinentTilesJob
CountCompletedContinentTilesJob
```

Publishing jobs:

```text
ClassifyOceanTilesJob
LabelMainContinentTilesJob
```

Expected tests:

```text
shore detection uses 4-neighbour adjacency
expansion preserves connectivity
tie expansion is deterministic
target area is reached when feasible
max-pass failure is deterministic
no-progress failure is deterministic
land/ocean masks are complements
land labels match land mask
```

Checkpoint: Runtime tests pass.

## Phase 10 — ComposeBaseElevation

Scheduler:

```text
ComposeBaseElevationJobScheduler
```

Initial jobs:

```text
ComposePrimaryContinentBaseElevationJob
ClampBaseElevationJob
```

Expected tests:

```text
every cell receives elevation
land and ocean profiles differ as configured
values are within range
same input produces same bytes
noise does not change accepted topology
```

Checkpoint: Runtime tests pass.

## Phase 11 — Full Landmass Workflow Integration

Goal: prove the route works through compile, workspace, execution, artifact, and readback.

Expected tests:

```text
PrimaryContinent route compiles.
All five operations execute in order.
Canonical outputs exist.
Stage-transient fields are excluded from default artifact.
Artifact round-trip preserves canonical outputs.
Debug export can visualize selected canonical fields.
```

Checkpoint: full Runtime tests pass.

## First Code Task Recommendation

Start with Phase 1, not jobs.

The first source task should be:

```text
Represent StageTransient field lifetime/category in Runtime contracts and artifact capture policy, with tests.
```

That unlocks the Landmass route without polluting canonical fields.
