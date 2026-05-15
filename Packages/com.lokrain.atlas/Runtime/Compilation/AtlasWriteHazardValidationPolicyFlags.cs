// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasWriteHazardValidationPolicyFlags.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan write-hazard validation.
// - Decide which writes are legal for Field ownership, storage kind, and ordering semantics.
// - Keep write validation explicit instead of hard-coding execution assumptions.
// - Preserve the boundary between compiler validation and workspace/runtime memory checks.
//
// Design notes
// - This is metadata policy, not runtime memory state.
// - This policy does not allocate workspace storage.
// - This policy does not schedule jobs.
// - This policy does not prove concrete container safety.
// - default(AtlasWriteHazardValidationPolicy) is valid and strict.
// - ProductionDefault is conservative for deterministic Atlas-owned compiled plans.

using System;
using Lokrain.Atlas.Contracts;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Flags controlling compiled-plan write-hazard validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags define what write forms the compiler validator may accept before workspace
    /// memory and concrete schedulers exist. They do not prove that a runtime container instance
    /// is valid, that a job implementation is race-free, or that dependency handles are correct.
    /// </para>
    ///
    /// <para>
    /// Numeric values are part of the diagnostics-facing policy contract. Do not reorder existing
    /// values after release.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasWriteHazardValidationPolicyFlags : uint
    {
        /// <summary>
        /// Strict policy. Only Atlas-owned and job-owned writable storage is accepted by default.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allows writes to externally owned Fields.
        /// </summary>
        AllowExternalOwnedWrites = 1u << 0,

        /// <summary>
        /// Allows writes to borrowed Fields.
        /// </summary>
        AllowBorrowedWrites = 1u << 1,

        /// <summary>
        /// Allows writes to imported Fields.
        /// </summary>
        AllowImportedWrites = 1u << 2,

        /// <summary>
        /// Allows writes to adopted Fields.
        /// </summary>
        AllowAdoptedWrites = 1u << 3,

        /// <summary>
        /// Allows writes to Fields whose lifetime is externally controlled.
        /// </summary>
        AllowExternalLifetimeWrites = 1u << 4,

        /// <summary>
        /// Allows writes to Fields declared with <see cref="StorageKind.External"/>.
        /// </summary>
        AllowExternalStorageWrites = 1u << 5,

        /// <summary>
        /// Allows append operations on <see cref="StorageKind.NativeList"/>.
        /// </summary>
        AllowNativeListAppend = 1u << 6,

        /// <summary>
        /// Allows append operations on <see cref="StorageKind.UnsafeList"/>.
        /// </summary>
        AllowUnsafeListAppend = 1u << 7,

        /// <summary>
        /// Allows append operations on <see cref="StorageKind.NativeStream"/>.
        /// </summary>
        AllowNativeStreamAppend = 1u << 8,

        /// <summary>
        /// Allows append operations on <see cref="StorageKind.External"/>.
        /// </summary>
        AllowExternalStorageAppend = 1u << 9,

        /// <summary>
        /// Allows consume operations on <see cref="StorageKind.NativeList"/>.
        /// </summary>
        AllowNativeListConsume = 1u << 10,

        /// <summary>
        /// Allows consume operations on <see cref="StorageKind.UnsafeList"/>.
        /// </summary>
        AllowUnsafeListConsume = 1u << 11,

        /// <summary>
        /// Allows consume operations on <see cref="StorageKind.NativeStream"/>.
        /// </summary>
        AllowNativeStreamConsume = 1u << 12,

        /// <summary>
        /// Allows consume operations on <see cref="StorageKind.External"/>.
        /// </summary>
        AllowExternalStorageConsume = 1u << 13,

        /// <summary>
        /// Requires operation-level parallel writes to be allowed by the Field Contract.
        /// </summary>
        RequireFieldParallelWriteFlag = 1u << 14,

        /// <summary>
        /// Requires deterministic-order operation writes when the Field Contract declares deterministic order.
        /// </summary>
        RequireDeterministicWriteFlagForDeterministicFields = 1u << 15,

        /// <summary>
        /// Requires append operations to declare deterministic order.
        /// </summary>
        RequireDeterministicAppendOrder = 1u << 16,

        /// <summary>
        /// Requires consume operations to declare deterministic order.
        /// </summary>
        RequireDeterministicConsumeOrder = 1u << 17,

        /// <summary>
        /// Requires write operations to declare either discard-before-write or preserve-existing-content.
        /// </summary>
        RequireExplicitWriteContentPolicy = 1u << 18,

        /// <summary>
        /// Rejects discard-before-write and preserve-existing-content when both are declared on the same binding.
        /// </summary>
        RejectContradictoryWriteContentPolicy = 1u << 19,

        /// <summary>
        /// Rejects writes to blob storage.
        /// </summary>
        RejectBlobWrites = 1u << 20,

        /// <summary>
        /// Rejects writes on shape-only bindings.
        /// </summary>
        RejectShapeOnlyWrites = 1u << 21,

        /// <summary>
        /// Rejects parallel-write operation flags unless the access actually writes content.
        /// </summary>
        RejectParallelFlagOnNonWrites = 1u << 22,

        /// <summary>
        /// Rejects exclusive-write operation flags unless the access actually writes content.
        /// </summary>
        RejectExclusiveFlagOnNonWrites = 1u << 23
    }
} 