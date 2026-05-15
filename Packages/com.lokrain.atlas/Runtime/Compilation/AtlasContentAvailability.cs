// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasContentAvailability.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent content availability during compiled-plan dataflow validation.
// - Distinguish partial writes from full logical-content writes.
// - Prevent discard-before-write from being treated as proof of full content coverage.
//
// Design notes
// - This is compiler validation metadata only.
// - This is not runtime workspace memory state.
// - FullCapacity writes are represented as FullLogicalContent for dataflow purposes.
// - ExternalContractContent means an explicit policy accepts externally supplied content.

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Describes the content state known by dataflow validation for one field.
    /// </summary>
    public enum AtlasContentAvailability : byte
    {
        /// <summary>
        /// No content is known to be available.
        /// </summary>
        None = 0,

        /// <summary>
        /// Some content exists, but full logical coverage has not been proven.
        /// </summary>
        PartialContent = 1,

        /// <summary>
        /// Full logical content has been proven available.
        /// </summary>
        FullLogicalContent = 2,

        /// <summary>
        /// Content is available through an accepted external contract.
        /// </summary>
        ExternalContractContent = 3
    }
}
