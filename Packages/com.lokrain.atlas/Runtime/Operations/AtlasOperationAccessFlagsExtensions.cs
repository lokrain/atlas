// Runtime/Operations/AtlasOperationAccessFlagsExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Describe symbolic field-access requirements for Atlas operation definitions.
// - Preserve zero-valid StableDataId semantics.
// - Keep access declarations separate from compiled memory bindings.
// - Validate operation-local access-mode and access-flag consistency.
//
// Design notes
// - default(AtlasOperationAccess) is a valid value object, but it is not a concrete access declaration.
// - StableDataId default/zero is valid.
// - AtlasOperationAccess.Empty is a compatibility alias for default, not an invalid sentinel.
// - AtlasOperationAccess.Invalid is a compatibility alias for default, not an invalid sentinel.
// - Missing lookup results must be represented by bool-returning APIs or explicit presence flags.
// - AtlasOperationAccessMode.None is valid as a default enum value, but not valid for concrete declarations.
// - BindingName is diagnostic/ABI metadata, not dispatch identity.
// - Jobs must not receive this type. Jobs should receive compiled addresses, typed slices/views,
//   or resolved native containers.
// - GetHashCode is deterministic and does not use System.HashCode.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Provides allocation-free helpers for <see cref="AtlasOperationAccessFlags"/>.
    /// </summary>
    public static class AtlasOperationAccessFlagsExtensions
    {
        /// <summary>
        /// Determines whether all requested flags are present.
        /// </summary>
        /// <param name="value">The flag set to inspect.</param>
        /// <param name="flags">The requested flags.</param>
        /// <returns><c>true</c> when all requested flags are present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(
            this AtlasOperationAccessFlags value,
            AtlasOperationAccessFlags flags)
        {
            return (value & flags) == flags;
        }

        /// <summary>
        /// Determines whether at least one requested flag is present.
        /// </summary>
        /// <param name="value">The flag set to inspect.</param>
        /// <param name="flags">The requested flags.</param>
        /// <returns><c>true</c> when at least one requested flag is present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(
            this AtlasOperationAccessFlags value,
            AtlasOperationAccessFlags flags)
        {
            return (value & flags) != 0;
        }

        /// <summary>
        /// Determines whether none of the requested flags are present.
        /// </summary>
        /// <param name="value">The flag set to inspect.</param>
        /// <param name="flags">The requested flags.</param>
        /// <returns><c>true</c> when none of the requested flags are present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(
            this AtlasOperationAccessFlags value,
            AtlasOperationAccessFlags flags)
        {
            return (value & flags) == 0;
        }
    }
}