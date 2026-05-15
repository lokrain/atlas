// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasContentAvailability.cs
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
        /// Content is available through a validated external contract.
        /// </summary>
        ExternalContractContent = 3
    }
}
