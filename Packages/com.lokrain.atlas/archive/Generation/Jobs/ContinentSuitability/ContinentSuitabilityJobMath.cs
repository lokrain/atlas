// Packages/com.lokrain.atlas/Runtime/Generation/Jobs/ContinentSuitability/ContinentSuitabilityJobMath.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Jobs.ContinentSuitability
//
// Purpose
// - Centralize deterministic integer math used by continent-suitability jobs.
// - Avoid floating-point, System.Random, UnityEngine.Random, and platform-dependent hash behavior.
// - Keep hard-excluded cells represented by a deterministic integer sentinel.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Generation.Jobs.ContinentSuitability
{
    /// <summary>
    /// Deterministic integer math for primary-continent suitability jobs.
    /// </summary>
    public static class ContinentSuitabilityJobMath
    {
        public const int Q16One = 1 << 16;
        public const int ExcludedSuitabilityQ16 = int.MinValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHardOceanBorderCell(
            int x,
            int y,
            int width,
            int height,
            int hardOceanBorderCellCount)
        {
            return hardOceanBorderCellCount > 0
                   && (x < hardOceanBorderCellCount
                       || y < hardOceanBorderCellCount
                       || x >= width - hardOceanBorderCellCount
                       || y >= height - hardOceanBorderCellCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EvaluateSuitabilityQ16(
            int x,
            int y,
            int width,
            int height,
            int hardOceanBorderCellCount,
            int radialFalloffWeightQ16,
            int noiseWeightQ16,
            uint seed)
        {
            if (IsHardOceanBorderCell(
                    x,
                    y,
                    width,
                    height,
                    hardOceanBorderCellCount))
            {
                return ExcludedSuitabilityQ16;
            }

            var radial = EvaluateRadialFalloffQ16(
                x,
                y,
                width,
                height);

            var noise = EvaluateNoiseQ16(
                x,
                y,
                seed);

            var totalWeight = radialFalloffWeightQ16 + noiseWeightQ16;
            var weighted = ((long)radial * radialFalloffWeightQ16)
                           + ((long)noise * noiseWeightQ16);

            var score = (int)(weighted / totalWeight);
            return ClampQ16(score);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ScoreToHistogramBin(
            int scoreQ16,
            int binCount)
        {
            if (scoreQ16 <= 0)
            {
                return 0;
            }

            if (scoreQ16 >= Q16One)
            {
                return binCount - 1;
            }

            return (int)(((long)scoreQ16 * binCount) >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HistogramBinToCutoffQ16(
            int bin,
            int binCount)
        {
            if (bin <= 0)
            {
                return 0;
            }

            if (bin >= binCount - 1)
            {
                return Q16One;
            }

            return (int)(((long)bin << 16) / binCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EvaluateRadialFalloffQ16(
            int x,
            int y,
            int width,
            int height)
        {
            var denominatorX = width > 1 ? width - 1 : 1;
            var denominatorY = height > 1 ? height - 1 : 1;

            var nxQ16 = (((long)x << 17) / denominatorX) - Q16One;
            var nyQ16 = (((long)y << 17) / denominatorY) - Q16One;

            var distanceSquaredQ16 = ((nxQ16 * nxQ16) + (nyQ16 * nyQ16)) >> 16;
            var score = Q16One - (int)(distanceSquaredQ16 >> 1);

            return ClampQ16(score);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EvaluateNoiseQ16(
            int x,
            int y,
            uint seed)
        {
            var mixed = Mix32(
                seed ^ ((uint)x * 0x9E37_79B9U) ^ ((uint)y * 0x85EB_CA6BU));

            return (int)(mixed & 0xFFFFU);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Mix32(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB_352DU;
            value ^= value >> 15;
            value *= 0x846C_A68BU;
            value ^= value >> 16;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ClampQ16(int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return value >= Q16One ? Q16One : value;
        }
    }
}
