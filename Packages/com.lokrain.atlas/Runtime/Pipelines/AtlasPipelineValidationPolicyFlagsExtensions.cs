// Packages/com.lokrain.atlas/Runtime/Pipelines/AtlasPipelineValidationPolicyFlagsExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Pipelines
//
// Purpose
// - Define route/preset policy for Atlas pipeline validation.
// - Keep generic pipeline metadata permissive while allowing concrete presets to reject invalid structures.
// - Express required, allowed, and forbidden stage/operation contracts.
// - Express duplicate and ordering rules without baking them into AtlasPipelineDefinition or AtlasCompiledPlan.
//
// Design notes
// - This is policy metadata, not execution metadata.
// - This type does not allocate workspace memory.
// - This type does not schedule jobs.
// - This type does not resolve Field storage.
// - Generic Atlas pipelines may repeat stages and operations.
// - Concrete route/preset policies may reject repeats deliberately.
// - default(AtlasPipelineValidationPolicy) is valid and open: it imposes no extra route rules.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Pipelines
{
    /// <summary>
    /// Extension helpers for <see cref="AtlasPipelineValidationPolicyFlags"/>.
    /// </summary>
    public static class AtlasPipelineValidationPolicyFlagsExtensions
    {
        /// <summary>
        /// Returns whether all requested flags are present.
        /// </summary>
        /// <param name="value">Current flag set.</param>
        /// <param name="flags">Flags to test.</param>
        /// <returns><c>true</c> when all requested flags are present; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(
            this AtlasPipelineValidationPolicyFlags value,
            AtlasPipelineValidationPolicyFlags flags)
        {
            return (value & flags) == flags;
        }

        /// <summary>
        /// Returns whether any requested flag is present.
        /// </summary>
        /// <param name="value">Current flag set.</param>
        /// <param name="flags">Flags to test.</param>
        /// <returns><c>true</c> when any requested flag is present; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(
            this AtlasPipelineValidationPolicyFlags value,
            AtlasPipelineValidationPolicyFlags flags)
        {
            return (value & flags) != 0;
        }

        /// <summary>
        /// Returns whether none of the requested flags are present.
        /// </summary>
        /// <param name="value">Current flag set.</param>
        /// <param name="flags">Flags to test.</param>
        /// <returns><c>true</c> when none of the requested flags are present; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(
            this AtlasPipelineValidationPolicyFlags value,
            AtlasPipelineValidationPolicyFlags flags)
        {
            return (value & flags) == 0;
        }
    }
}