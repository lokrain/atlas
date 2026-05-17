// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasWriteHazardValidationPolicyFlagsExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan write-hazard validation.
// - Decide which writes are legal for Field ownership, storage kind, and ordering semantics.
// - Keep write validation explicit instead of hard-coding execution assumptions.
// - Preserve the boundary between compiler validation and workspace/runtime memory checks.
//
// Design notes
// - This is metadata policy, not runtime memory state.
// - This policy does not allocate workspace storage.
// - This policy does not schedule jobs.
// - This policy does not prove concrete container safety.
// - default(AtlasWriteHazardValidationPolicy) is valid and strict.
// - ProductionDefault is conservative for deterministic Atlas-owned compiled plans.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Extension helpers for <see cref="AtlasWriteHazardValidationPolicyFlags"/>.
    /// </summary>
    public static class AtlasWriteHazardValidationPolicyFlagsExtensions
    {
        /// <summary>
        /// Returns whether all requested flags are present.
        /// </summary>
        /// <param name="value">Current flag set.</param>
        /// <param name="flags">Flags to test.</param>
        /// <returns><c>true</c> when all requested flags are present; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(
            this AtlasWriteHazardValidationPolicyFlags value,
            AtlasWriteHazardValidationPolicyFlags flags)
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
            this AtlasWriteHazardValidationPolicyFlags value,
            AtlasWriteHazardValidationPolicyFlags flags)
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
            this AtlasWriteHazardValidationPolicyFlags value,
            AtlasWriteHazardValidationPolicyFlags flags)
        {
            return (value & flags) == 0;
        }
    }
}