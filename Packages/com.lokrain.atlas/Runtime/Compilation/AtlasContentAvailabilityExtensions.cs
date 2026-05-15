// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasContentAvailabilityExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent content availability while validating compiled operation dataflow.
// - Distinguish partial writes from full logical-content writes.
// - Prevent discard-before-write from being treated as proof of full content coverage.
//
// Design notes
// - This is compiler validation metadata only.
// - This does not represent live workspace memory.
// - This does not represent native container validity.
// - A field can be readable by policy before any operation writes it.
// - A partial write may establish records or sparse content, but does not prove full logical content.
// - Full capacity implies full logical content for read validation.
// - Consume invalidates content availability after the operation.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Provides allocation-free helpers for <see cref="AtlasContentAvailability"/>.
    /// </summary>
    public static class AtlasContentAvailabilityExtensions
    {
        /// <summary>
        /// Returns whether any content is known to exist.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyContent(this AtlasContentAvailability availability)
        {
            return availability != AtlasContentAvailability.None;
        }

        /// <summary>
        /// Returns whether full logical content has been proven available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFullLogicalContent(this AtlasContentAvailability availability)
        {
            return availability == AtlasContentAvailability.FullLogicalContent ||
                   availability == AtlasContentAvailability.ExternalContractContent;
        }

        /// <summary>
        /// Returns whether content availability is externally fenced.
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
