// Packages/com.lokrain.atlas/Runtime/Generation/Jobs/ContinentSuitability/EvaluateTileContinentSuitabilityJob.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Jobs.ContinentSuitability
//
// Purpose
// - Evaluate deterministic Q16.16 continent suitability for each map cell.
// - Mark hard-ocean border cells with the excluded suitability sentinel.
// - Perform one Burst/job-friendly transformation over resolved native data.

using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Generation.Jobs.ContinentSuitability
{
    /// <summary>
    /// Writes one deterministic suitability score per map cell.
    /// </summary>
    public struct EvaluateTileContinentSuitabilityJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<int> SuitabilityQ16;

        public int Width;
        public int Height;
        public int HardOceanBorderCellCount;
        public int RadialFalloffWeightQ16;
        public int NoiseWeightQ16;
        public uint Seed;

        public void Execute(int index)
        {
            var x = index % Width;
            var y = index / Width;

            SuitabilityQ16[index] = ContinentSuitabilityJobMath.EvaluateSuitabilityQ16(
                x,
                y,
                Width,
                Height,
                HardOceanBorderCellCount,
                RadialFalloffWeightQ16,
                NoiseWeightQ16,
                Seed);
        }
    }
}
