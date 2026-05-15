// Packages/com.lokrain.atlas/Runtime/Execution/AtlasMemoryResolver.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Validate whether resolved Atlas field shapes can be backed by the current workspace allocator.
// - Resolve Contract-table or compiled-plan shapes into workspace-owned native memory.
// - Keep allocation policy separate from compilation, operation scheduling, artifacts, and debug rendering.
//
// Design notes
// - The catalog owns meaning.
// - The compiler owns resolution.
// - The workspace owns memory.
// - Jobs own only numeric execution.
// - Artifacts own durable output.
// - This resolver is a managed execution-boundary policy point.
// - It does not schedule operations.
// - It does not expose FieldId lookup to jobs.
// - It does not write artifacts.
// - It does not render debug output.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Creates workspace-owned native memory from validated Atlas compilation metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasMemoryResolver"/> is the allocation-policy boundary between resolved
    /// compiler metadata and <see cref="AtlasWorkspace"/> memory ownership. It validates that every
    /// resolved field can be represented by the currently supported workspace memory model before
    /// allocation starts.
    /// </para>
    ///
    /// <para>
    /// This type deliberately stays thin. Shape resolution remains in <see cref="AtlasShapeResolver"/>.
    /// Memory ownership remains in <see cref="AtlasWorkspace"/> and <see cref="AtlasFieldMemoryBlock"/>.
    /// Operation execution, artifact export, and debug-map rendering are downstream systems.
    /// </para>
    /// </remarks>
    public static class AtlasMemoryResolver
    {
        /// <summary>
        /// Resolves field shapes from a compiled plan and allocates a workspace.
        /// </summary>
        /// <param name="plan">Compiled plan whose Contract table defines workspace fields.</param>
        /// <param name="allocator">Unity allocator used for workspace-owned native memory.</param>
        /// <param name="options">Native allocation initialization option.</param>
        /// <returns>A live Atlas workspace.</returns>
        public static AtlasWorkspace CreateWorkspace(
            AtlasCompiledPlan plan,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (plan.Contracts == null)
            {
                throw new ArgumentException(
                    "Compiled plan does not reference a Contract table.",
                    nameof(plan));
            }

            var shapes = AtlasShapeResolver.Resolve(plan);

            return CreateWorkspace(
                shapes,
                allocator,
                options);
        }

        /// <summary>
        /// Allocates a workspace from an already resolved shape set.
        /// </summary>
        /// <param name="shapes">Resolved field shape set.</param>
        /// <param name="allocator">Unity allocator used for workspace-owned native memory.</param>
        /// <param name="options">Native allocation initialization option.</param>
        /// <returns>A live Atlas workspace.</returns>
        public static AtlasWorkspace CreateWorkspace(
            AtlasResolvedShapeSet shapes,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            ValidateAllocatorOrThrow(allocator);
            ValidateAllocatableOrThrow(shapes);

            return AtlasWorkspace.Create(
                shapes,
                allocator,
                options);
        }

        /// <summary>
        /// Resolves field shapes from a Contract table and allocates a workspace.
        /// </summary>
        /// <param name="contracts">Contract table whose fields should be allocated.</param>
        /// <param name="allocator">Unity allocator used for workspace-owned native memory.</param>
        /// <param name="options">Native allocation initialization option.</param>
        /// <returns>A live Atlas workspace.</returns>
        public static AtlasWorkspace CreateWorkspace(
            AtlasContractTable contracts,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var shapes = AtlasShapeResolver.Resolve(contracts);

            return CreateWorkspace(
                shapes,
                allocator,
                options);
        }

        /// <summary>
        /// Returns whether the supplied resolved shape set can be allocated by the current workspace memory model.
        /// </summary>
        /// <param name="shapes">Resolved field shape set.</param>
        /// <returns><c>true</c> when all fields are allocatable; otherwise, <c>false</c>.</returns>
        public static bool CanAllocate(
            AtlasResolvedShapeSet shapes)
        {
            try
            {
                ValidateAllocatableOrThrow(shapes);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a resolved shape set can be allocated by the current workspace memory model.
        /// </summary>
        /// <param name="shapes">Resolved field shape set.</param>
        public static void ValidateAllocatableOrThrow(
            AtlasResolvedShapeSet shapes)
        {
            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            shapes.ValidateOrThrow(nameof(shapes));

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var contract = shapes.Contracts[i];

                ValidateAllocatableFieldOrThrow(
                    contract,
                    shape,
                    $"shapes[{i}]");
            }
        }

        private static void ValidateAllocatableFieldOrThrow(
            AtlasContract contract,
            AtlasResolvedShape shape,
            string parameterName)
        {
            contract.ValidateTableReadyOrThrow(parameterName);
            shape.ValidateOrThrow(parameterName);

            var storageKind = contract.StorageFormat.Kind;

            if (!AtlasFieldMemoryBlock.SupportsOwnedByteBlock(storageKind))
            {
                throw new NotSupportedException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' with storage kind '{storageKind}'. " +
                    "Use a dedicated workspace container model for growable, stream, map, blob, or external storage.");
            }

            if (contract.Ownership != OwnershipPolicy.AtlasOwned)
            {
                throw new NotSupportedException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' with ownership policy '{contract.Ownership}'. " +
                    "This allocation path creates new workspace-owned memory and therefore only supports AtlasOwned storage. " +
                    "Borrowed, imported, external-owned, job-owned, and adopted storage require explicit acquisition paths.");
            }

            if (contract.StorageFormat != shape.StorageFormat)
            {
                throw new ArgumentException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' because its Contract storage format does not match its resolved shape storage format.",
                    parameterName);
            }

            if (contract.StableId != shape.StableId)
            {
                throw new ArgumentException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' because its Contract stable id does not match its resolved shape stable id.",
                    parameterName);
            }

            if (contract.Slot != shape.Slot)
            {
                throw new ArgumentException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' because its Contract slot does not match its resolved shape slot.",
                    parameterName);
            }

            if (contract.Role != shape.Role)
            {
                throw new ArgumentException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' because its Contract role does not match its resolved shape role.",
                    parameterName);
            }

            if (shape.ByteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' because byte capacity '{shape.ByteCapacity}' exceeds NativeArray length capacity.");
            }

            var expectedByteCapacity = checked((long)contract.StorageFormat.ElementSize * shape.Capacity);

            if (shape.ByteCapacity != expectedByteCapacity)
            {
                throw new ArgumentException(
                    $"Atlas memory resolver cannot allocate field '{contract.GetDiagnosticName()}' because resolved byte capacity '{shape.ByteCapacity}' does not match expected byte capacity '{expectedByteCapacity}'.",
                    parameterName);
            }
        }

        private static void ValidateAllocatorOrThrow(
            Allocator allocator)
        {
            if (allocator == Allocator.None ||
                allocator == Allocator.Invalid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(allocator),
                    allocator,
                    "Atlas workspace allocation requires a concrete Unity allocator.");
            }
        }
    }
}