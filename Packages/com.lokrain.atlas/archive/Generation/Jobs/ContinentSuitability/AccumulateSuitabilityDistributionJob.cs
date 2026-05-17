// Packages/com.lokrain.atlas/Runtime/Generation/Jobs/ContinentSuitability/AccumulateSuitabilityDistributionJob.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Jobs.ContinentSuitability
//
// Purpose
// - Build a deterministic suitability histogram from the resolved suitability field.
// - Exclude hard-ocean sentinel values from the distribution.
// - Keep merge order explicit by running one deterministic accumulation transform.

using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Generation.Jobs.ContinentSuitability
{
    /// <summary>
    /// Accumulates non-excluded suitability scores into histogram bins.
    /// </summary>
    public struct AccumulateSuitabilityDistributionJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> SuitabilityQ16;

        public NativeArray<int> Histogram;

        public void Execute()
        {
            for (var i = 0; i < Histogram.Length; i++)
            {
                Histogram[i] = 0;
            }

            for (var i = 0; i < SuitabilityQ16.Length; i++)
            {
                var score = SuitabilityQ16[i];
                if (score == ContinentSuitabilityJobMath.ExcludedSuitabilityQ16)
                {
                    continue;
                }

                var bin = ContinentSuitabilityJobMath.ScoreToHistogramBin(
                    score,
                    Histogram.Length);

                Histogram[bin]++;
            }
        }
    }
}
