// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasContentAvailabilityExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Provide allocation-free predicates for dataflow content availability.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Provides allocation-free helpers for <see cref="AtlasContentAvailability"/>.
    /// </summary>
    public static class AtlasContentAvailabilityExtensions
    {
        /// <summary>
        /// Gets whether any content is known to exist.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyContent(this AtlasContentAvailability availability)
        {
            return availability != AtlasContentAvailability.None;
        }

        /// <summary>
        /// Gets whether full logical content has been proven available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFullLogicalContent(this AtlasContentAvailability availability)
        {
            return availability == AtlasContentAvailability.FullLogicalContent ||
                   availability == AtlasContentAvailability.ExternalContractContent;
        }

        /// <summary>
        /// Gets whether content availability is externally fenced.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsExternalContractContent(this AtlasContentAvailability availability)
        {
            return availability == AtlasContentAvailability.ExternalContractContent;
        }

        /// <summary>
        /// Returns the stronger of two availability states.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasContentAvailability Max(
            this AtlasContentAvailability left,
            AtlasContentAvailability right)
        {
            return left >= right
                ? left
                : right;
        }
    }
}
