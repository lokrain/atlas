// Packages/com.lokrain.atlas/Runtime/Operations/AtlasWriteCoverage.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Describe the semantic coverage of an operation's write to a field.
// - Distinguish "does not read old contents" from "overwrites all logical contents".
// - Give dataflow validation enough information to prove whether later reads are safe.

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines how much of a field's content an operation writes or mutates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Write coverage is not a storage clearing policy. <see cref="AtlasOperationAccessFlags.DiscardBeforeWrite"/>
    /// means previous contents are not semantically read. It does not prove every logical element
    /// is overwritten. This enum supplies the missing proof used by dataflow validation.
    /// </para>
    /// </remarks>
    public enum AtlasWriteCoverage : byte
    {
        /// <summary>
        /// The access does not write content.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operation writes every logical element of the field.
        /// </summary>
        FullLogicalLength = 1,

        /// <summary>
        /// The operation writes every allocated capacity element of the field.
        /// </summary>
        FullCapacity = 2,

        /// <summary>
        /// The operation writes a known subset of logical elements.
        /// </summary>
        PartialLogicalLength = 3,

        /// <summary>
        /// The operation writes an indexed sparse subset of elements.
        /// </summary>
        SparseIndexed = 4,

        /// <summary>
        /// The operation appends records to a variable-length payload.
        /// </summary>
        AppendRecords = 5,

        /// <summary>
        /// The operation consumes records or mutates producer-consumer container state.
        /// </summary>
        ConsumeRecords = 6,

        /// <summary>
        /// Coverage is controlled by an explicit external storage grant.
        /// </summary>
        ExternalContract = 7
    }
}