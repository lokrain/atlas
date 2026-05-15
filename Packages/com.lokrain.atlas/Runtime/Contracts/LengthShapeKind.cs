// Runtime/Contracts/LengthShapeKind.cs

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Defines the rule used to resolve a Field's runtime length or capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Length shape is schema metadata. It describes how storage size should be resolved before
    /// jobs are scheduled. Jobs should receive already-resolved native containers and must not
    /// interpret length-shape rules directly.
    /// </para>
    ///
    /// <para>
    /// Shape resolution is context-dependent. Query-based shapes require a resolver that knows
    /// the relevant ECS query, authoring set, simulation set, or external data source. Field-
    /// relative shapes require the source Field to exist in the same validated Contract table.
    /// </para>
    /// </remarks>
    public enum LengthShapeKind : byte
    {
        /// <summary>
        /// No length-shape rule is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for concrete Contracts and is reserved for default
        /// initialization and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The Field contains exactly one logical element.
        /// </summary>
        Scalar = 1,

        /// <summary>
        /// The Field has a fixed length declared in the Contract.
        /// </summary>
        Fixed = 2,

        /// <summary>
        /// The Field length must match another Field's resolved length.
        /// </summary>
        MatchFieldLength = 3,

        /// <summary>
        /// The Field length is resolved from a named entity-query count or equivalent data set.
        /// </summary>
        QueryCount = 4,

        /// <summary>
        /// The Field length is resolved from a named chunk count or equivalent partition count.
        /// </summary>
        ChunkCount = 5,

        /// <summary>
        /// The Field capacity is derived from another Field's resolved length or capacity.
        /// </summary>
        CapacityFromField = 6,

        /// <summary>
        /// The Field length is resolved from prefix-sum metadata.
        /// </summary>
        PrefixSumPayload = 7,

        /// <summary>
        /// The Field length is provided by an external owner or integration layer.
        /// </summary>
        External = 8
    }
}