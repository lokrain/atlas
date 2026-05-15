// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasDataflowValidationPolicyFlags.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan content dataflow validation.
// - Decide which present bindings may be read before a prior producing operation.
// - Decide which write modes can establish initialized content for later operations.
// - Keep read-before-write validation explicit instead of hard-coding route assumptions.
//
// Design notes
// - This is metadata policy, not runtime memory state.
// - This policy does not allocate workspace storage.
// - This policy does not schedule jobs.
// - This policy does not validate route-specific stage presence or stage uniqueness.
// - default(AtlasDataflowValidationPolicy) is valid and strict.
// - ProductionDefault allows external/imported/adopted/borrowed/external-lifetime inputs
//   to be treated as initially readable.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Flags controlling compiled-plan dataflow validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags describe what the validator may treat as already initialized before the first
    /// operation in a compiled plan. They do not prove that runtime memory exists. Runtime memory
    /// ownership and actual container validity still belong to workspace and memory-resolution
    /// validation.
    /// </para>
    ///
    /// <para>
    /// Numeric values are part of the diagnostics-facing policy contract. Do not reorder existing
    /// values after release.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasDataflowValidationPolicyFlags : ushort
    {
        /// <summary>
        /// Strict policy. No Field is considered initially readable unless a prior operation writes it.
        /// </summary>
        None = 0,

        /// <summary>
        /// Fields with <see cref="AtlasFieldRole.External"/> may be read before a prior write.
        /// </summary>
        AllowExternalRoleInitialRead = 1 << 0,

        /// <summary>
        /// Fields with <see cref="OwnershipPolicy.ExternalOwned"/> may be read before a prior write.
        /// </summary>
        AllowExternalOwnedInitialRead = 1 << 1,

        /// <summary>
        /// Fields with <see cref="OwnershipPolicy.Borrowed"/> may be read before a prior write.
        /// </summary>
        AllowBorrowedInitialRead = 1 << 2,

        /// <summary>
        /// Fields with <see cref="OwnershipPolicy.Imported"/> may be read before a prior write.
        /// </summary>
        AllowImportedInitialRead = 1 << 3,

        /// <summary>
        /// Fields with <see cref="OwnershipPolicy.Adopted"/> may be read before a prior write.
        /// </summary>
        AllowAdoptedInitialRead = 1 << 4,

        /// <summary>
        /// Fields with <see cref="LifetimePolicy.External"/> may be read before a prior write.
        /// </summary>
        AllowExternalLifetimeInitialRead = 1 << 5
    }
}