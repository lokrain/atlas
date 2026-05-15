// Packages/com.lokrain.atlas/Runtime/Operations/AtlasWriteCoverage.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Describe the semantic coverage of an operation write to a field.
// - Distinguish "previous content is discarded" from "all logical content is written".
// - Give dataflow validation enough information to prove whether later reads are safe.
//
// Design notes
// - This is symbolic operation metadata, not a runtime memory container.
// - Coverage is declared per operation binding.
// - DiscardBeforeWrite is a read-dependency policy; it does not prove full write coverage.
// - FullCapacity implies full logical content for dataflow, but also states the operation owns slack bytes.
// - Partial and sparse writes establish content existence, not ordinary full-field readability.

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines how much content an operation writes or mutates for one field binding.
    /// </summary>
    public enum AtlasWriteCoverage : byte
    {
        /// <summary>
        /// The binding does not write content.
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
        /// The operation writes a known subset of logical elements while preserving the rest through prior content.
        /// </summary>
        PartialLogicalLength = 3,

        /// <summary>
        /// The operation writes an indexed sparse subset of elements.
        /// </summary>
        SparseIndexed = 4,

        /// <summary>
        /// The operation appends records to a variable-length field.
        /// </summary>
        AppendRecords = 5,

        /// <summary>
        /// The operation consumes records or mutates producer-consumer container state.
        /// </summary>
        ConsumeRecords = 6,

        /// <summary>
        /// Content availability is controlled by an explicitly validated external storage contract.
        /// </summary>
        ExternalContract = 7
    }
}
