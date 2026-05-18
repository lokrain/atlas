#nullable enable

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Specifies the managed scalar value kind described by a field definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field value kind describes the semantic value category of generated field data. It is managed metadata,
    /// not storage allocation, memory layout, executable job data, native container ownership, ECS component data,
    /// or scheduler binding.
    /// </para>
    /// <para>
    /// The value names use CLR type terminology. Storage-specific formats, packing, quantization, and native
    /// memory layout belong to future execution infrastructure.
    /// </para>
    /// </remarks>
    public enum FieldValueKind
    {
        /// <summary>
        /// No field value kind has been specified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A Boolean value.
        /// </summary>
        Boolean = 1,

        /// <summary>
        /// A signed 32-bit integer value.
        /// </summary>
        Int32 = 2,

        /// <summary>
        /// An unsigned 32-bit integer value.
        /// </summary>
        UInt32 = 3,

        /// <summary>
        /// A single-precision floating-point value.
        /// </summary>
        Single = 4,

        /// <summary>
        /// A double-precision floating-point value.
        /// </summary>
        Double = 5
    }
}