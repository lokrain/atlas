// Runtime/Operations/AtlasOperationAccessModeExtensions.cs
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
    /// Provides allocation-free helpers for <see cref="AtlasOperationAccessMode"/>.
    /// </summary>
    public static class AtlasOperationAccessModeExtensions
    {
        /// <summary>
        /// Determines whether an access mode reads field contents or consumes container state.
        /// </summary>
        /// <param name="mode">The access mode to inspect.</param>
        /// <returns><c>true</c> when the mode reads content or consumes container state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Reads(this AtlasOperationAccessMode mode)
        {
            return mode == AtlasOperationAccessMode.Read ||
                   mode == AtlasOperationAccessMode.ReadWrite ||
                   mode == AtlasOperationAccessMode.Consume;
        }

        /// <summary>
        /// Determines whether an access mode writes field contents or mutates container state.
        /// </summary>
        /// <param name="mode">The access mode to inspect.</param>
        /// <returns><c>true</c> when the mode writes content or mutates container state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Writes(this AtlasOperationAccessMode mode)
        {
            return mode == AtlasOperationAccessMode.Write ||
                   mode == AtlasOperationAccessMode.ReadWrite ||
                   mode == AtlasOperationAccessMode.Append ||
                   mode == AtlasOperationAccessMode.Consume;
        }

        /// <summary>
        /// Determines whether an access mode is valid for a concrete operation access declaration.
        /// </summary>
        /// <param name="mode">The access mode to inspect.</param>
        /// <returns><c>true</c> when the mode is concrete.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConcrete(this AtlasOperationAccessMode mode)
        {
            return mode != AtlasOperationAccessMode.None;
        }

        /// <summary>
        /// Compatibility alias for <see cref="IsConcrete"/>.
        /// </summary>
        /// <param name="mode">The access mode to inspect.</param>
        /// <returns><c>true</c> when the mode is concrete.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this AtlasOperationAccessMode mode)
        {
            return mode.IsConcrete();
        }
    }
}