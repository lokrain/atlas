// Packages/com.lokrain.atlas/Runtime/Generation/Operations/EvaluateContinentSuitability/EvaluateContinentSuitabilityJobRequest.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
//
// Purpose
// - Define the typed scheduler request for EvaluateContinentSuitability.
// - Keep field binding resolution outside jobs.
// - Pass only resolved native containers, dimensions, parameters, and batch policy to the scheduler.

using Unity.Collections;

namespace Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
{
    /// <summary>
    /// Typed scheduler request for the EvaluateContinentSuitability operation.
    /// </summary>
    public readonly struct EvaluateContinentSuitabilityJobRequest
    {
        public EvaluateContinentSuitabilityJobRequest(
            NativeArray<int> suitabilityQ16,
            NativeArray<int> suitabilityCutoffQ16,
            int width,
            int height,
            EvaluateContinentSuitabilityParameters parameters,
            int innerloopBatchCount)
        {
            SuitabilityQ16 = suitabilityQ16;
            SuitabilityCutoffQ16 = suitabilityCutoffQ16;
            Width = width;
            Height = height;
            Parameters = parameters;
            InnerloopBatchCount = innerloopBatchCount;
        }

        /// <summary>
        /// Output Q16.16 suitability field, one element per map cell.
        /// </summary>
        public NativeArray<int> SuitabilityQ16 { get; }

        /// <summary>
        /// Output scalar Q16.16 cutoff. Must contain exactly one element.
        /// </summary>
        public NativeArray<int> SuitabilityCutoffQ16 { get; }

        /// <summary>
        /// Map width in cells.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Map height in cells.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Deterministic operation parameters.
        /// </summary>
        public EvaluateContinentSuitabilityParameters Parameters { get; }

        /// <summary>
        /// Inner-loop batch count for the cell suitability job.
        /// </summary>
        public int InnerloopBatchCount { get; }
    }
}
