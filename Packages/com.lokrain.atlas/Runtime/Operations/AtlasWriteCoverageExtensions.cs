// Packages/com.lokrain.atlas/Runtime/Operations/AtlasWriteCoverageExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Provide allocation-free write-coverage predicates for validators and compiler code.

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
        /// Gets whether this coverage proves all logical content is available after the operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MakesFullLogicalContentAvailable(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.FullLogicalLength ||
                   coverage == AtlasWriteCoverage.FullCapacity;
        }

        /// <summary>
        /// Gets whether this coverage writes only part of the logical content.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPartialContentWrite(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.PartialLogicalLength ||
                   coverage == AtlasWriteCoverage.SparseIndexed ||
                   coverage == AtlasWriteCoverage.AppendRecords;
        }

        /// <summary>
        /// Gets whether this coverage mutates or consumes producer-consumer state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConsumesContainerState(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.ConsumeRecords;
        }

        /// <summary>
        /// Gets whether this coverage is controlled outside workspace-owned memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsExternalContract(this AtlasWriteCoverage coverage)
        {
            return coverage == AtlasWriteCoverage.ExternalContract;
        }

        /// <summary>
        /// Gets whether this coverage is a concrete declaration for a write-capable access mode.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConcreteWriteCoverage(this AtlasWriteCoverage coverage)
        {
            return coverage != AtlasWriteCoverage.None;
        }
    }
}