// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasDataflowValidationPolicyFlagsExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan content dataflow validation.
// - Decide which present bindings may be read before a prior producing operation.
// - Decide which write modes can establish initialized content for later operations.
// - Keep read-before-write validation explicit instead of hard-coding route assumptions.
//
// Design notes
// - This is metadata policy, not runtime memory state.
// - This policy does not allocate workspace storage.
// - This policy does not schedule jobs.
// - This policy does not validate route-specific stage presence or stage uniqueness.
// - default(AtlasDataflowValidationPolicy) is valid and strict.
// - ProductionDefault allows external/imported/adopted/borrowed/external-lifetime inputs
//   to be treated as initially readable.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Extension helpers for <see cref="AtlasDataflowValidationPolicyFlags"/>.
    /// </summary>
    public static class AtlasDataflowValidationPolicyFlagsExtensions
    {
        /// <summary>
        /// Returns whether all requested flags are present.
        /// </summary>
        /// <param name="value">Current flag set.</param>
        /// <param name="flags">Flags to test.</param>
        /// <returns><c>true</c> when all requested flags are present; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(
            this AtlasDataflowValidationPolicyFlags value,
            AtlasDataflowValidationPolicyFlags flags)
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
            this AtlasDataflowValidationPolicyFlags value,
            AtlasDataflowValidationPolicyFlags flags)
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
            this AtlasDataflowValidationPolicyFlags value,
            AtlasDataflowValidationPolicyFlags flags)
        {
            return (value & flags) == 0;
        }
    }
}