// Packages/com.lokrain.atlas/Runtime/Operations/AtlasWriteCoverageExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Provide allocation-free write-coverage predicates for compiler validators and executor policy.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Provides allocation-free helpers for <see cref="AtlasWriteCoverage"/>.
    /// </summary>
    public static class AtlasWriteCoverageExtensions
    {
        /// <summary>
        /// Gets whether this coverage writes any content or mutates container state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WritesAnyContent(this AtlasWriteCoverage coverage)
        {
            return coverage != AtlasWriteCoverage.None;
        }

        /// <summary>
        /// Gets whether this coverage proves all logical field content is available after the operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MakesFullLogicalContentAvailable(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.FullLogicalLength ||
                   coverage == AtlasWriteCoverage.FullCapacity ||
                   coverage == AtlasWriteCoverage.ExternalContract;
        }

        /// <summary>
        /// Gets whether this coverage writes only a subset of logical content or appends records.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPartialContentWrite(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.PartialLogicalLength ||
                   coverage == AtlasWriteCoverage.SparseIndexed ||
                   coverage == AtlasWriteCoverage.AppendRecords;
        }

        /// <summary>
        /// Gets whether this coverage is valid for a write/read-write field overwrite binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFieldWriteCoverage(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.FullLogicalLength ||
                   coverage == AtlasWriteCoverage.FullCapacity ||
                   coverage == AtlasWriteCoverage.PartialLogicalLength ||
                   coverage == AtlasWriteCoverage.SparseIndexed ||
                   coverage == AtlasWriteCoverage.ExternalContract;
        }

        /// <summary>
        /// Gets whether this coverage is valid for an append binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAppendCoverage(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.AppendRecords;
        }

        /// <summary>
        /// Gets whether this coverage is valid for a consume binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConsumeCoverage(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.ConsumeRecords;
        }

        /// <summary>
        /// Gets whether this coverage declares externally fenced content availability.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsExternalContract(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.ExternalContract;
        }
    }
}
