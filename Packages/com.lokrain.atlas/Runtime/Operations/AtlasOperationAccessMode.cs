// Runtime/Operations/AtlasOperationAccessMode.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Describe symbolic field-access requirements for Atlas operation definitions.
// - Preserve zero-valid StableDataId semantics.
// - Keep access declarations separate from compiled memory bindings.
// - Validate operation-local access-mode and access-flag consistency.
//
// Design notes
// - default(AtlasOperationAccess) is a valid value object, but it is not a concrete access declaration.
// - StableDataId default/zero is valid.
// - AtlasOperationAccess.Empty is a compatibility alias for default, not an invalid sentinel.
// - AtlasOperationAccess.Invalid is a compatibility alias for default, not an invalid sentinel.
// - Missing lookup results must be represented by bool-returning APIs or explicit presence flags.
// - AtlasOperationAccessMode.None is valid as a default enum value, but not valid for concrete declarations.
// - BindingName is diagnostic/ABI metadata, not dispatch identity.
// - Jobs must not receive this type. Jobs should receive compiled addresses, typed slices/views,
//   or resolved native containers.
// - GetHashCode is deterministic and does not use System.HashCode.

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines the kind of field access required by an Atlas operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operation access mode describes how an operation intends to use a field after compilation.
    /// It is validated against the field contract, operation ordering, stage boundaries, storage
    /// kind, lifetime, ownership, and dependency graph before jobs are scheduled.
    /// </para>
    ///
    /// <para>
    /// This enum is symbolic operation ABI metadata. It is not a runtime memory container and is
    /// not a job API. Jobs receive concrete resolved memory, not symbolic access declarations.
    /// </para>
    ///
    /// <para>
    /// <see cref="None"/> is valid as a default enum value, but it is not valid for a concrete
    /// operation access declaration.
    /// </para>
    /// </remarks>
    public enum AtlasOperationAccessMode : byte
    {
        /// <summary>
        /// No concrete access mode is declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operation reads field contents without writing them.
        /// </summary>
        Read = 1,

        /// <summary>
        /// The operation writes field contents without depending on previous contents.
        /// </summary>
        /// <remarks>
        /// Validators should require either clear-before-write, discard-before-write, full coverage,
        /// or another explicit proof that stale content cannot affect correctness.
        /// </remarks>
        Write = 2,

        /// <summary>
        /// The operation reads existing field contents and writes updated contents.
        /// </summary>
        ReadWrite = 3,

        /// <summary>
        /// The operation appends produced records to a variable-length field.
        /// </summary>
        /// <remarks>
        /// Append access is appropriate for streams, queues, lists, event buffers, record payloads,
        /// and producer-side variable output. Validators must verify storage support and parallel
        /// writer safety before scheduling.
        /// </remarks>
        Append = 4,

        /// <summary>
        /// The operation consumes records from a producer-consumer field.
        /// </summary>
        /// <remarks>
        /// Consume access is appropriate for queues, streams, command buffers, and explicit
        /// producer-consumer pipelines. Consumption may mutate container state even when the
        /// operation's logical purpose is to read produced records.
        /// </remarks>
        Consume = 5
    }
}