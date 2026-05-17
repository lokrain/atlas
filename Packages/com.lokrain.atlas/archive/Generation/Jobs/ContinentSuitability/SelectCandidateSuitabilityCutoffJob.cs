// Packages/com.lokrain.atlas/Runtime/Generation/Jobs/ContinentSuitability/SelectCandidateSuitabilityCutoffJob.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Jobs.ContinentSuitability
//
// Purpose
// - Select a deterministic conservative Q16.16 candidate cutoff from a suitability histogram.
// - Prefer higher suitability bins until the candidate target count is reached.
// - Write one scalar cutoff value for downstream candidate-mask formation.

using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Generation.Jobs.ContinentSuitability
{
    /// <summary>
    /// Selects a candidate suitability cutoff from a deterministic histogram.
    /// </summary>
    public struct SelectCandidateSuitabilityCutoffJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> Histogram;

        [WriteOnly]
        public NativeArray<int> SuitabilityCutoffQ16;

        public int CandidateTargetCellCount;

        public void Execute()
        {
            var accumulated = 0;

            for (var bin = Histogram.Length - 1; bin >= 0; bin--)
            {
                accumulated += Histogram[bin];

                if (accumulated < CandidateTargetCellCount)
                {
                    continue;
                }

                SuitabilityCutoffQ16[0] = ContinentSuitabilityJobMath.HistogramBinToCutoffQ16(
                    bin,
                    Histogram.Length);
                return;
            }

            SuitabilityCutoffQ16[0] = ContinentSuitabilityJobMath.Q16One;
        }
    }
}
