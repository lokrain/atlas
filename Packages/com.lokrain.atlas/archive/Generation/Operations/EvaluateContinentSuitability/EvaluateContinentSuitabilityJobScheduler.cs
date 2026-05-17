// Packages/com.lokrain.atlas/Runtime/Generation/Operations/EvaluateContinentSuitability/EvaluateContinentSuitabilityJobScheduler.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
//
// Purpose
// - Schedule the EvaluateContinentSuitability operation job graph.
// - Own operation scratch allocation and dependency-aware scratch disposal.
// - Keep job order and request validation outside the Burst job structs.

using System;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Generation.Jobs.ContinentSuitability;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
{
    /// <summary>
    /// Schedules deterministic continent-suitability evaluation jobs.
    /// </summary>
    public sealed class EvaluateContinentSuitabilityJobScheduler
    {
        private readonly Allocator _scratchAllocator;

        public EvaluateContinentSuitabilityJobScheduler()
            : this(Allocator.TempJob)
        {
        }

        public EvaluateContinentSuitabilityJobScheduler(Allocator scratchAllocator)
        {
            _scratchAllocator = scratchAllocator;
        }

        /// <summary>
        /// Schedules suitability scoring, histogram accumulation, cutoff selection, and scratch disposal.
        /// </summary>
        public JobHandle Schedule(
            in EvaluateContinentSuitabilityJobRequest request,
            JobHandle dependency)
        {
            ValidateRequestOrThrow(request);

            var scratchScope = new AtlasOperationScratchScope(_scratchAllocator);
            var histogram = scratchScope.AllocateNativeArray<int>(
                request.Parameters.ScoreBinCount,
                NativeArrayOptions.ClearMemory);

            var suitabilityDependency = new EvaluateTileContinentSuitabilityJob
            {
                SuitabilityQ16 = request.SuitabilityQ16,
                Width = request.Width,
                Height = request.Height,
                HardOceanBorderCellCount = request.Parameters.HardOceanBorderCellCount,
                RadialFalloffWeightQ16 = request.Parameters.RadialFalloffWeightQ16,
                NoiseWeightQ16 = request.Parameters.NoiseWeightQ16,
                Seed = request.Parameters.Seed
            }.Schedule(
                request.SuitabilityQ16.Length,
                request.InnerloopBatchCount,
                dependency);

            var histogramDependency = new AccumulateSuitabilityDistributionJob
            {
                SuitabilityQ16 = request.SuitabilityQ16,
                Histogram = histogram.Array
            }.Schedule(suitabilityDependency);

            var cutoffDependency = new SelectCandidateSuitabilityCutoffJob
            {
                Histogram = histogram.Array,
                SuitabilityCutoffQ16 = request.SuitabilityCutoffQ16,
                CandidateTargetCellCount = request.Parameters.CandidateTargetCellCount
            }.Schedule(histogramDependency);

            return scratchScope.Dispose(cutoffDependency);
        }

        private static void ValidateRequestOrThrow(
            in EvaluateContinentSuitabilityJobRequest request)
        {
            if (request.Width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.Width),
                    request.Width,
                    "Map width must be positive.");
            }

            if (request.Height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.Height),
                    request.Height,
                    "Map height must be positive.");
            }

            if (request.InnerloopBatchCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.InnerloopBatchCount),
                    request.InnerloopBatchCount,
                    "Inner-loop batch count must be positive.");
            }

            if (!request.SuitabilityQ16.IsCreated)
            {
                throw new ArgumentException(
                    "Suitability output must be a created NativeArray.",
                    nameof(request));
            }

            if (!request.SuitabilityCutoffQ16.IsCreated)
            {
                throw new ArgumentException(
                    "Suitability cutoff output must be a created NativeArray.",
                    nameof(request));
            }

            var expectedLength = checked(request.Width * request.Height);
            if (request.SuitabilityQ16.Length != expectedLength)
            {
                throw new ArgumentException(
                    "Suitability output length must equal width * height.",
                    nameof(request));
            }

            if (request.SuitabilityCutoffQ16.Length != 1)
            {
                throw new ArgumentException(
                    "Suitability cutoff output must contain exactly one scalar element.",
                    nameof(request));
            }
        }
    }
}
