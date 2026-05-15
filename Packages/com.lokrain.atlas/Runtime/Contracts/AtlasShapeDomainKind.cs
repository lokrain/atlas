// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasShapeDomainKind.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Define the semantic domain used to interpret a field's resolved shape.
// - Prevent unrelated fields with equal numeric length/capacity from being treated as interchangeable.
// - Keep domain identity separate from physical storage format, ownership, lifetime, and role.
//
// Design notes
// - This is contract/compiler metadata, not runtime storage.
// - This enum describes what the elements mean, not how bytes are stored.
// - Numeric length is not enough shape identity.
// - Shape domain is part of field ABI and artifact metadata.
// - Jobs should receive resolved numeric layout, not branch on this enum in hot loops.

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Identifies the semantic domain used to interpret a field's resolved length and capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A resolved length of <c>65536</c> is not self-describing. It may represent cells,
    /// vertices, chunks, graph nodes, graph edges, component rows, stream records, ECS entities,
    /// or externally supplied rows. <see cref="AtlasShapeDomainKind"/> provides the first-order
    /// semantic identity needed by validation, artifact export, debug-map generation, and executor
    /// policy.
    /// </para>
    ///
    /// <para>
    /// Storage belongs to <see cref="StorageFormat"/> and <see cref="StorageKind"/>.
    /// Domain belongs to schema meaning.
    /// </para>
    /// </remarks>
    public enum AtlasShapeDomainKind : byte
    {
        /// <summary>
        /// No concrete shape domain is declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// A single scalar value.
        /// </summary>
        Scalar = 1,

        /// <summary>
        /// A fixed-size vector payload where element position has vector-component meaning.
        /// </summary>
        FixedVector = 2,

        /// <summary>
        /// A dense one-dimensional row domain.
        /// </summary>
        LinearRows = 3,

        /// <summary>
        /// A dense two-dimensional cell grid.
        /// </summary>
        CellGrid2D = 10,

        /// <summary>
        /// A dense two-dimensional vertex grid.
        /// </summary>
        VertexGrid2D = 11,

        /// <summary>
        /// A dense three-dimensional voxel grid.
        /// </summary>
        VoxelGrid3D = 12,

        /// <summary>
        /// A chunk, tile, page, patch, or partition domain.
        /// </summary>
        ChunkSet = 20,

        /// <summary>
        /// A Unity ECS entity query result or equivalent entity-row domain.
        /// </summary>
        EntitySet = 30,

        /// <summary>
        /// A connected-component row domain.
        /// </summary>
        ComponentSet = 40,

        /// <summary>
        /// A graph-node row domain.
        /// </summary>
        GraphNodeSet = 50,

        /// <summary>
        /// A graph-edge row domain.
        /// </summary>
        GraphEdgeSet = 51,

        /// <summary>
        /// A variable-length record stream, event stream, append payload, or serialized row payload.
        /// </summary>
        RecordStream = 60,

        /// <summary>
        /// A payload domain derived from prefix sums, offsets, or compacted source rows.
        /// </summary>
        PrefixSumPayload = 61,

        /// <summary>
        /// A sparse set keyed by explicit indices.
        /// </summary>
        SparseIndexSet = 70,

        /// <summary>
        /// A domain owned by an external system and explicitly granted to Atlas.
        /// </summary>
        External = 250
    }
}