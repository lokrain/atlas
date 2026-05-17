// Runtime/Contracts/StorageKind.cs

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Defines the native storage family used by an Atlas Field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage kind describes the container category, not the element type. The element type,
    /// element size, element alignment, and stable type hash are represented by
    /// <see cref="StorageFormat"/>.
    /// </para>
    ///
    /// <para>
    /// Atlas validators use this value to verify that length shapes, ownership policies,
    /// lifetime policies, access modes, and allocation paths are compatible with the declared
    /// storage family.
    /// </para>
    /// </remarks>
    public enum StorageKind : byte
    {
        /// <summary>
        /// No storage kind is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for a concrete Field Contract and is reserved for
        /// default initialization and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Single unmanaged value storage.
        /// </summary>
        /// <remarks>
        /// Scalar storage represents exactly one element. It is suitable for counters,
        /// configuration values, singleton state, and compact plan inputs that do not require
        /// array indexing.
        /// </remarks>
        Scalar = 1,

        /// <summary>
        /// Fixed-length contiguous unmanaged array storage.
        /// </summary>
        /// <remarks>
        /// Native array storage is the default choice for dense parallel jobs. Length is
        /// resolved before scheduling and remains stable for the allocated lifetime interval.
        /// </remarks>
        NativeArray = 2,

        /// <summary>
        /// Variable-length contiguous unmanaged list storage.
        /// </summary>
        /// <remarks>
        /// Native list storage is appropriate when logical length changes during the Field
        /// lifetime but capacity can still be planned, validated, and owned by Atlas.
        /// </remarks>
        NativeList = 3,

        /// <summary>
        /// Low-level unmanaged list storage.
        /// </summary>
        /// <remarks>
        /// Unsafe list storage is intended for internal high-performance paths that require
        /// explicit control over safety, aliasing, or container reconstruction. Public APIs
        /// should prefer safer native containers unless the caller owns the invariant.
        /// </remarks>
        UnsafeList = 4,

        /// <summary>
        /// Per-thread or append-oriented stream storage.
        /// </summary>
        /// <remarks>
        /// Stream storage is appropriate for variable-size job output where each worker writes
        /// independent records and a later phase consumes the aggregated stream.
        /// </remarks>
        NativeStream = 5,

        /// <summary>
        /// Native parallel hash-map storage.
        /// </summary>
        /// <remarks>
        /// Hash-map storage should be used for actual key-based data access, not as the
        /// Contract-table lookup mechanism. Contract and slot resolution should happen
        /// before jobs receive native containers.
        /// </remarks>
        NativeParallelHashMap = 6,

        /// <summary>
        /// Immutable blob-style storage.
        /// </summary>
        /// <remarks>
        /// Blob storage is appropriate for immutable data shared by many jobs or frames.
        /// Atlas must treat blob memory according to its ownership and lifetime declaration.
        /// </remarks>
        Blob = 7,

        /// <summary>
        /// Storage whose memory is owned outside Atlas.
        /// </summary>
        /// <remarks>
        /// External storage may be borrowed or imported from ECS, native systems, custom
        /// allocators, or other owner-managed memory. Atlas must not dispose externally owned
        /// memory unless an explicit ownership transfer is declared.
        /// </remarks>
        External = 8
    }
}