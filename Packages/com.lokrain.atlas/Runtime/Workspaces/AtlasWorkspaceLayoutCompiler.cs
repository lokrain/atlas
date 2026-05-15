// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasWorkspaceLayoutCompiler.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Compile resolved semantic shape facts into deterministic workspace memory layout facts.
// - Pack fixed contiguous Atlas-owned field storage into physical workspace storage blocks.
// - Produce AtlasFieldAddress values for execution-plan binding and later workspace view creation.
// - Keep workspace allocation independent from AtlasResolvedShapeSet and authored Contracts.
//
// Design notes
// - This is a compiler boundary, not a workspace allocator.
// - This type does not allocate native memory.
// - This type does not own or dispose native containers.
// - This type does not know operations, stages, routes, schedulers, jobs, JobHandles, or artifacts.
// - The current physical layout model supports only Atlas-owned Scalar and NativeArray storage.
// - Growable, stream, hash-map, blob, borrowed, imported, adopted, and external storage require
//   dedicated physical binding models and must not be faked as byte ranges.
// - Entries are emitted in canonical field-slot order.
// - Storage blocks are emitted in physical block-index order.
// - The first implementation emits one packed byte block for all supported fixed contiguous fields.
// - Zero-length NativeArray fields receive addresses but do not advance block used length.
// - Slot zero, StableDataId zero, block index zero, byte offset zero, and type-hash zero are valid.

using System;
using System.Globalization;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Unity.Collections;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Compiles resolved Atlas field shapes into a concrete workspace memory layout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWorkspaceLayoutCompiler"/> is the boundary between semantic shape
    /// resolution and workspace allocation. It consumes <see cref="AtlasResolvedShapeSet"/>,
    /// validates that every resolved field can be represented by the current physical memory
    /// model, assigns deterministic byte offsets, and emits <see cref="AtlasWorkspaceLayout"/>.
    /// </para>
    ///
    /// <para>
    /// This compiler intentionally does not allocate memory. <see cref="AtlasWorkspace"/> must
    /// allocate from the resulting <see cref="AtlasWorkspaceLayout"/>. Execution-plan compilation
    /// should bind scheduler requirements to the emitted <see cref="AtlasFieldAddress"/> values.
    /// </para>
    /// </remarks>
    public static class AtlasWorkspaceLayoutCompiler
    {
        private const int PrimaryStorageBlockIndex = 0;

        /// <summary>
        /// Compiles a workspace layout using the resolved shape set's diagnostic name.
        /// </summary>
        /// <param name="shapes">Resolved field shapes in canonical Contract-table slot order.</param>
        /// <returns>A validated workspace layout.</returns>
        public static AtlasWorkspaceLayout Compile(AtlasResolvedShapeSet shapes)
        {
            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            return Compile(
                shapes.Name,
                shapes);
        }

        /// <summary>
        /// Compiles a named workspace layout.
        /// </summary>
        /// <param name="name">Diagnostic layout name.</param>
        /// <param name="shapes">Resolved field shapes in canonical Contract-table slot order.</param>
        /// <returns>A validated workspace layout.</returns>
        public static AtlasWorkspaceLayout Compile(
            FixedString64Bytes name,
            AtlasResolvedShapeSet shapes)
        {
            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            ValidateCompilableOrThrow(shapes);

            if (shapes.Count == 0)
            {
                return AtlasWorkspaceLayout.Create(
                    ResolveLayoutName(name, shapes),
                    Array.Empty<AtlasWorkspaceLayoutEntry>(),
                    Array.Empty<AtlasStorageBlockPlan>());
            }

            var entries = new AtlasWorkspaceLayoutEntry[shapes.Count];
            var currentByteOffset = 0L;
            var usedByteLength = 0L;
            var requiredAlignment = 1;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var alignedByteOffset = AlignUpChecked(
                    currentByteOffset,
                    shape.StorageFormat.ElementAlignment);

                var address = AtlasFieldAddress.Create(
                    shape.Slot,
                    shape.StorageFormat,
                    PrimaryStorageBlockIndex,
                    alignedByteOffset,
                    shape.Length,
                    shape.Capacity);

                entries[i] = AtlasWorkspaceLayoutEntry.Create(
                    shape,
                    address);

                if (shape.ByteCapacity > 0L)
                {
                    currentByteOffset = checked(alignedByteOffset + shape.ByteCapacity);

                    if (currentByteOffset > usedByteLength)
                    {
                        usedByteLength = currentByteOffset;
                    }
                }

                if (shape.StorageFormat.ElementAlignment > requiredAlignment)
                {
                    requiredAlignment = shape.StorageFormat.ElementAlignment;
                }
            }

            var storageBlocks = new[]
            {
                AtlasStorageBlockPlan.CreateAligned(
                    PrimaryStorageBlockIndex,
                    usedByteLength,
                    requiredAlignment,
                    entries.Length)
            };

            return AtlasWorkspaceLayout.Create(
                ResolveLayoutName(name, shapes),
                entries,
                storageBlocks);
        }

        /// <summary>
        /// Returns whether the supplied resolved shape set can be compiled by the current layout model.
        /// </summary>
        /// <param name="shapes">Resolved field shape set.</param>
        /// <returns><c>true</c> when layout compilation can succeed; otherwise, <c>false</c>.</returns>
        public static bool CanCompile(AtlasResolvedShapeSet shapes)
        {
            try
            {
                ValidateCompilableOrThrow(shapes);
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
        /// Validates that the supplied resolved shape set can be compiled by the current layout model.
        /// </summary>
        /// <param name="shapes">Resolved field shape set.</param>
        public static void ValidateCompilableOrThrow(AtlasResolvedShapeSet shapes)
        {
            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            shapes.ValidateOrThrow(nameof(shapes));

            if (shapes.Contracts == null)
            {
                throw new ArgumentException(
                    "Resolved shape set does not reference a Contract table.",
                    nameof(shapes));
            }

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var contract = shapes.Contracts[i];

                ValidateCompilableFieldOrThrow(
                    contract,
                    shape,
                    i);
            }

            ValidatePackedBlockCapacityOrThrow(shapes);
        }

        private static void ValidateCompilableFieldOrThrow(
            AtlasContract contract,
            AtlasResolvedShape shape,
            int index)
        {
            var parameterName = string.Format(
                CultureInfo.InvariantCulture,
                "shapes[{0}]",
                index);

            contract.ValidateTableReadyOrThrow(parameterName);
            shape.ValidateOrThrow(parameterName);

            if (contract.StableId != shape.StableId)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot bind shape at index {0} because Contract stable id '{1}' does not match shape stable id '{2}'.",
                        index,
                        contract.StableId,
                        shape.StableId),
                    parameterName);
            }

            if (contract.Slot != shape.Slot)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot bind field '{0}' because Contract slot '{1}' does not match shape slot '{2}'.",
                        contract.GetDiagnosticName(),
                        contract.Slot,
                        shape.Slot),
                    parameterName);
            }

            if (shape.Slot.Index != index)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler requires canonical slot order. Shape at index {0} has slot '{1}'.",
                        index,
                        shape.Slot),
                    parameterName);
            }

            if (contract.Role != shape.Role)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot bind field '{0}' because Contract role '{1}' does not match shape role '{2}'.",
                        contract.GetDiagnosticName(),
                        contract.Role,
                        shape.Role),
                    parameterName);
            }

            if (contract.StorageFormat != shape.StorageFormat)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot bind field '{0}' because Contract storage format '{1}' does not match shape storage format '{2}'.",
                        contract.GetDiagnosticName(),
                        contract.StorageFormat,
                        shape.StorageFormat),
                    parameterName);
            }

            if (contract.ShapeDomain != shape.ShapeDomain)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot bind field '{0}' because Contract shape domain '{1}' does not match shape domain '{2}'.",
                        contract.GetDiagnosticName(),
                        contract.ShapeDomain,
                        shape.ShapeDomain),
                    parameterName);
            }

            if (contract.LengthShape != shape.DeclaredShape)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot bind field '{0}' because Contract length shape '{1}' does not match shape declared length shape '{2}'.",
                        contract.GetDiagnosticName(),
                        contract.LengthShape,
                        shape.DeclaredShape),
                    parameterName);
            }

            if (contract.Ownership != OwnershipPolicy.AtlasOwned)
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot allocate field '{0}' with ownership policy '{1}'. This layout path only supports Atlas-owned memory. Borrowed, imported, adopted, job-owned, and external-owned storage require explicit acquisition or binding models.",
                        contract.GetDiagnosticName(),
                        contract.Ownership));
            }

            if (!SupportsFixedContiguousByteBlock(shape.StorageFormat.Kind))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot allocate field '{0}' with storage kind '{1}'. Current workspace byte-block layout supports only Scalar and NativeArray storage.",
                        contract.GetDiagnosticName(),
                        shape.StorageFormat.Kind));
            }

            if (shape.ByteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot allocate field '{0}' because byte capacity '{1}' exceeds NativeArray length capacity.",
                        contract.GetDiagnosticName(),
                        shape.ByteCapacity));
            }
        }

        private static void ValidatePackedBlockCapacityOrThrow(AtlasResolvedShapeSet shapes)
        {
            var currentByteOffset = 0L;
            var usedByteLength = 0L;
            var requiredAlignment = 1;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var alignedByteOffset = AlignUpChecked(
                    currentByteOffset,
                    shape.StorageFormat.ElementAlignment);

                if (shape.ByteCapacity > 0L)
                {
                    currentByteOffset = checked(alignedByteOffset + shape.ByteCapacity);

                    if (currentByteOffset > usedByteLength)
                    {
                        usedByteLength = currentByteOffset;
                    }
                }

                if (shape.StorageFormat.ElementAlignment > requiredAlignment)
                {
                    requiredAlignment = shape.StorageFormat.ElementAlignment;
                }
            }

            var byteCapacity = AtlasStorageBlockPlan.AlignUpChecked(
                usedByteLength,
                requiredAlignment);

            if (byteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout compiler cannot allocate packed storage block because byte capacity '{0}' exceeds NativeArray length capacity.",
                        byteCapacity));
            }
        }

        private static FixedString64Bytes ResolveLayoutName(
            FixedString64Bytes requestedName,
            AtlasResolvedShapeSet shapes)
        {
            if (!requestedName.IsEmpty)
            {
                return requestedName;
            }

            if (shapes != null && !shapes.Name.IsEmpty)
            {
                return shapes.Name;
            }

            return default;
        }

        private static bool SupportsFixedContiguousByteBlock(StorageKind storageKind)
        {
            return storageKind == StorageKind.Scalar ||
                   storageKind == StorageKind.NativeArray;
        }

        private static long AlignUpChecked(
            long value,
            int alignment)
        {
            return AtlasStorageBlockPlan.AlignUpChecked(
                value,
                alignment);
        }
    }
}