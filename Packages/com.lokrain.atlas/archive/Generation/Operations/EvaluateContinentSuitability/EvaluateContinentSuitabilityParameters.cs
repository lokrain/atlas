// Packages/com.lokrain.atlas/Runtime/Generation/Operations/EvaluateContinentSuitability/EvaluateContinentSuitabilityParameters.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
//
// Purpose
// - Define deterministic parameter data for the EvaluateContinentSuitability operation.
// - Keep suitability scoring tunables explicit and independent from jobs and field contracts.
// - Use integer and Q16.16 values only.

using System;

namespace Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
{
    /// <summary>
    /// Deterministic parameter block for primary-continent suitability evaluation.
    /// </summary>
    public readonly struct EvaluateContinentSuitabilityParameters
    {
        /// <summary>
        /// One in Q16.16 fixed-point representation.
        /// </summary>
        public const int Q16One = 1 << 16;

        /// <summary>
        /// Creates deterministic suitability parameters.
        /// </summary>
        public EvaluateContinentSuitabilityParameters(
            int hardOceanBorderCellCount,
            int radialFalloffWeightQ16,
            int noiseWeightQ16,
            int candidateTargetCellCount,
            int scoreBinCount,
            uint seed)
        {
            if (hardOceanBorderCellCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hardOceanBorderCellCount),
                    hardOceanBorderCellCount,
                    "Hard-ocean border cell count must be non-negative.");
            }

            if (radialFalloffWeightQ16 < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radialFalloffWeightQ16),
                    radialFalloffWeightQ16,
                    "Radial falloff weight must be non-negative.");
            }

            if (noiseWeightQ16 < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(noiseWeightQ16),
                    noiseWeightQ16,
                    "Noise weight must be non-negative.");
            }

            if (radialFalloffWeightQ16 + noiseWeightQ16 <= 0)
            {
                throw new ArgumentException(
                    "At least one suitability weight must be positive.");
            }

            if (candidateTargetCellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidateTargetCellCount),
                    candidateTargetCellCount,
                    "Candidate target cell count must be positive.");
            }

            if (scoreBinCount <= 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scoreBinCount),
                    scoreBinCount,
                    "Score bin count must be greater than one.");
            }

            HardOceanBorderCellCount = hardOceanBorderCellCount;
            RadialFalloffWeightQ16 = radialFalloffWeightQ16;
            NoiseWeightQ16 = noiseWeightQ16;
            CandidateTargetCellCount = candidateTargetCellCount;
            ScoreBinCount = scoreBinCount;
            Seed = seed;
        }

        /// <summary>
        /// Number of cells at each map edge that are hard-excluded from continent candidacy.
        /// </summary>
        public int HardOceanBorderCellCount { get; }

        /// <summary>
        /// Weight of center-biased radial suitability in Q16.16.
        /// </summary>
        public int RadialFalloffWeightQ16 { get; }

        /// <summary>
        /// Weight of deterministic integer noise suitability in Q16.16.
        /// </summary>
        public int NoiseWeightQ16 { get; }

        /// <summary>
        /// Conservative candidate target used for cutoff selection.
        /// </summary>
        public int CandidateTargetCellCount { get; }

        /// <summary>
        /// Histogram bin count used by cutoff selection.
        /// </summary>
        public int ScoreBinCount { get; }

        /// <summary>
        /// Deterministic operation seed.
        /// </summary>
        public uint Seed { get; }

        /// <summary>
        /// Creates production-default deterministic suitability parameters.
        /// </summary>
        public static EvaluateContinentSuitabilityParameters CreateDefault(
            int candidateTargetCellCount,
            uint seed)
        {
            return new EvaluateContinentSuitabilityParameters(
                hardOceanBorderCellCount: 1,
                radialFalloffWeightQ16: (Q16One * 3) / 4,
                noiseWeightQ16: Q16One / 4,
                candidateTargetCellCount: candidateTargetCellCount,
                scoreBinCount: 256,
                seed: seed);
        }
    }
}
