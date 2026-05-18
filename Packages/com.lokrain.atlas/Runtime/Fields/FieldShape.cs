#nullable enable

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Specifies the logical shape of values described by a field definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field shape describes how values are addressed by generation semantics. It is managed metadata,
    /// not storage allocation, memory layout, native container ownership, ECS component data, or scheduler binding.
    /// </para>
    /// <para>
    /// Storage-specific layout, compression, tiling, chunking, sparse representation, and executable memory ownership
    /// belong to execution infrastructure.
    /// </para>
    /// </remarks>
    public enum FieldShape
    {
        /// <summary>
        /// No field shape has been specified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A single value for the generation scope.
        /// </summary>
        Scalar = 1,

        /// <summary>
        /// One value per validated grid cell.
        /// </summary>
        Grid = 2
    }
}
