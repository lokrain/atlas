// Runtime/Operations/AtlasOperationAccessFlags.cs
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
// - AtlasOperationAccess.Empty is a default payload for bool-returning lookup APIs, not an invalid sentinel.
// - Missing lookup results must be represented by bool-returning APIs or explicit presence flags.
// - AtlasOperationAccessMode.None is valid as a default enum value, but not valid for concrete declarations.
// - BindingName is diagnostic/ABI metadata, not dispatch identity.
// - Jobs must not receive this type. Jobs should receive compiled addresses, typed slices/views,
//   or resolved native containers.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines operation-specific access requirements for a field binding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags describe how a specific operation uses a field. They are validated against the
    /// field's own flags and contract. Field flags declare what is allowed by the field catalog;
    /// operation access flags declare what one operation actually requires.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasOperationAccessFlags : ushort
    {
        /// <summary>
        /// No operation access flags are declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operation can run when the field binding is absent.
        /// </summary>
        /// <remarks>
        /// Optional access is valid only when the compiled plan and operation implementation have
        /// an explicit fallback path. Validators should reject optional access to non-optional
        /// fields unless the pipeline profile explicitly permits it.
        /// </remarks>
        Optional = 1 << 0,

        /// <summary>
        /// The operation needs only resolved shape metadata and does not access field contents.
        /// </summary>
        /// <remarks>
        /// Shape-only access is useful when an operation needs length, capacity, partition count,
        /// or presence information but not the memory payload.
        /// </remarks>
        ShapeOnly = 1 << 1,

        /// <summary>
        /// The operation discards previous contents before writing.
        /// </summary>
        /// <remarks>
        /// This flag is operation-local. It does not imply that storage is cleared to zero. It means
        /// previous contents are not semantically read by the operation.
        /// </remarks>
        DiscardBeforeWrite = 1 << 2,

        /// <summary>
        /// The operation requires previous contents to remain valid before it runs.
        /// </summary>
        /// <remarks>
        /// This is relevant for read-write updates, incremental solvers, cached payloads, and
        /// persistent working sets.
        /// </remarks>
        PreserveExistingContent = 1 << 3,

        /// <summary>
        /// The operation may write to the field from parallel workers.
        /// </summary>
        /// <remarks>
        /// This declares an operation requirement only. The validator must still verify that the
        /// field contract, storage kind, write pattern, dependency graph, and container API support
        /// safe parallel writes.
        /// </remarks>
        AllowsParallelWrite = 1 << 4,

        /// <summary>
        /// The operation requires deterministic output order for this field.
        /// </summary>
        /// <remarks>
        /// This is relevant for append output, streams, queues, payload records, artifact hashes,
        /// replay validation, rollback, and deterministic tests.
        /// </remarks>
        RequiresDeterministicOrder = 1 << 5,

        /// <summary>
        /// The operation requires exclusive write authority for this field during execution.
        /// </summary>
        /// <remarks>
        /// Validators should reject overlapping writes, aliasing writes, and incompatible parallel
        /// writer usage when this flag is present.
        /// </remarks>
        RequiresExclusiveWrite = 1 << 6
    }
}