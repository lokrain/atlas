// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasShapeDomainKind.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Define the semantic domain a resolved field shape belongs to.
// - Prevent unrelated fields with equal numeric length from being treated as interchangeable.
// - Keep domain identity separate from physical storage format and resolved element count.

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Identifies the semantic domain used to interpret a field's resolved length and capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Numeric length is not sufficient shape identity. A value of <c>65536</c> may represent
    /// grid cells, vertices, chunks, entities, records, graph nodes, graph edges, or external rows.
    /// This enum provides the first-order semantic domain needed by validators, artifacts, debug
    /// export, executor policy, and tooling.
    /// </para>
    ///
    /// <para>
    /// The domain does not describe physical storage. Storage belongs to <see cref="StorageFormat"/>
    /// and <see cref="StorageKind"/>. The domain describes what the elements mean.
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
        /// A dense two-dimensional cell grid.
        /// </summary>
        CellGrid2D = 2,

        /// <summary>
        /// A dense two-dimensional vertex grid.
        /// </summary>
        VertexGrid2D = 3,

        /// <summary>
        /// A dense three-dimensional voxel grid.
        /// </summary>
        VoxelGrid3D = 4,

        /// <summary>
        /// A chunk, tile, patch, page, or partition set.
        /// </summary>
        ChunkSet = 5,

        /// <summary>
        /// A Unity ECS entity query or equivalent entity set.
        /// </summary>
        EntitySet = 6,

        /// <summary>
        /// A connected-component row set.
        /// </summary>
        ComponentSet = 7,

        /// <summary>
        /// A graph node row set.
        /// </summary>
        GraphNodeSet = 8,

        /// <summary>
        /// A graph edge row set.
        /// </summary>
        GraphEdgeSet = 9,

        /// <summary>
        /// A variable record stream, event stream, append payload, or serialized row payload.
        /// </summary>
        RecordStream = 10,

        /// <summary>
        /// A prefix-sum-derived payload domain.
        /// </summary>
        PrefixSumPayload = 11,

        /// <summary>
        /// A domain owned by an external system and explicitly granted to Atlas.
        /// </summary>
        External = 12
    }
}