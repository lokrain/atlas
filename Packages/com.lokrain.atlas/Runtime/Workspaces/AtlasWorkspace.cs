// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasWorkspace.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Own workspace native memory allocated from a compiled AtlasWorkspaceLayout.
// - Allocate physical storage blocks from AtlasStorageBlockPlan values.
// - Resolve field byte and typed views through AtlasFieldAddress values.
// - Keep workspace allocation independent from Contracts, resolved shape sets, execution, jobs, and artifacts.
//
// Design notes
// - This is memory ownership, not semantic compilation.
// - This is memory ownership, not operation scheduling.
// - This is not an execution plan.
// - This is not an artifact.
// - This type allocates concrete NativeArray<byte> storage blocks from AtlasWorkspaceLayout.
// - Fields are views into workspace-owned storage blocks, not independently allocated field buffers.
// - Jobs must receive already-resolved NativeSlice<T> / NativeArray<T> views or numeric addresses,
//   not this workspace owner.
// - default StableDataId, default AtlasFieldSlot, block index zero, and byte offset zero are valid.
// - Missing lookup state is represented by bool-returning APIs.
// - This class intentionally exposes typed slices for fields because fields may be packed into
//   shared physical blocks.
// - The old per-field AtlasFieldMemoryBlock model should be deleted or kept only as a temporary
//   migration adapter outside the canonical workspace path.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Workspace-owned native memory allocated from a compiled Atlas workspace layout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWorkspace"/> is the concrete memory owner for Atlas generation/runtime data.
    /// It allocates physical byte storage blocks from <see cref="AtlasWorkspaceLayout"/> and exposes
    /// field views by resolving <see cref="AtlasFieldAddress"/> values.
    /// </para>
    ///
    /// <para>
    /// The workspace does not own semantic compilation. It must not allocate directly from
    /// <see cref="AtlasResolvedShapeSet"/> as its primary model. Shape sets are accepted only by
    /// convenience factory methods that immediately compile a workspace layout first.
    /// </para>
    ///
    /// <para>
    /// Because multiple fields may share one physical storage block, canonical field access returns
    /// <see cref="NativeSlice{T}"/> views. Returning <see cref="NativeArray{T}"/> for a field is only
    /// correct when that field owns an entire physical block, which is no longer the canonical model.
    /// </para>
    /// </remarks>
    public sealed class AtlasWorkspace :
        IDisposable,
        IReadOnlyList<AtlasWorkspaceLayoutEntry>
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private readonly NativeArray<byte>[] _storageBlocks;
        private byte _state;

        /// <summary>
        /// Compiled memory layout used to allocate this workspace.
        /// </summary>
        public readonly AtlasWorkspaceLayout Layout;

        /// <summary>
        /// Allocator used for all owned native memory blocks.
        /// </summary>
        public readonly Allocator Allocator;

        private AtlasWorkspace(
            AtlasWorkspaceLayout layout,
            Allocator allocator,
            NativeArrayOptions options)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            ValidateAllocatorOrThrow(allocator);
            layout.ValidateOrThrow();

            Layout = layout;
            Allocator = allocator;
            _storageBlocks = AllocateStorageBlocks(
                layout,
                allocator,
                options);
            _state = AliveState;
        }

        /// <summary>
        /// Gets the number of field layout entries.
        /// </summary>
        public int Count => Layout.Count;

        /// <summary>
        /// Gets the number of allocated physical storage blocks.
        /// </summary>
        public int StorageBlockCount => _storageBlocks.Length;

        /// <summary>
        /// Gets whether this workspace contains no fields and no storage blocks.
        /// </summary>
        public bool IsEmpty => Layout.IsEmpty;

        /// <summary>
        /// Gets whether this workspace is alive.
        /// </summary>
        public bool IsCreated => _state == AliveState;

        /// <summary>
        /// Gets whether this workspace has been disposed.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Gets the total logical byte length across all field entries.
        /// </summary>
        public long TotalFieldByteLength => Layout.TotalFieldByteLength;

        /// <summary>
        /// Gets the total allocated byte capacity across all field entries.
        /// </summary>
        public long TotalFieldByteCapacity => Layout.TotalFieldByteCapacity;

        /// <summary>
        /// Gets the total used byte length across physical storage blocks.
        /// </summary>
        public long TotalBlockUsedByteLength => Layout.TotalBlockUsedByteLength;

        /// <summary>
        /// Gets the total allocated byte capacity across physical storage blocks.
        /// </summary>
        public long TotalBlockByteCapacity => Layout.TotalBlockByteCapacity;

        /// <summary>
        /// Gets a layout entry by canonical field index.
        /// </summary>
        public AtlasWorkspaceLayoutEntry this[int index]
        {
            get
            {
                ThrowIfDisposed();
                return Layout[index];
            }
        }

        /// <summary>
        /// Gets a layout entry by canonical field slot.
        /// </summary>
        public AtlasWorkspaceLayoutEntry this[AtlasFieldSlot slot]
        {
            get
            {
                ThrowIfDisposed();
                return Layout[slot];
            }
        }

        /// <summary>
        /// Allocates a workspace from a compiled workspace layout.
        /// </summary>
        public static AtlasWorkspace Create(
            AtlasWorkspaceLayout layout,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            return new AtlasWorkspace(
                layout,
                allocator,
                options);
        }

        /// <summary>
        /// Compiles a layout from resolved shapes and allocates a workspace.
        /// </summary>
        /// <remarks>
        /// This overload exists as a migration convenience. The canonical path is explicit:
        /// resolve shapes, compile <see cref="AtlasWorkspaceLayout"/>, then create the workspace.
        /// </remarks>
        public static AtlasWorkspace Create(
            AtlasResolvedShapeSet shapes,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            return Create(
                AtlasWorkspaceLayoutCompiler.Compile(shapes),
                allocator,
                options);
        }

        /// <summary>
        /// Resolves shapes from a Contract table, compiles a layout, and allocates a workspace.
        /// </summary>
        /// <remarks>
        /// This overload exists for call-site migration. Prefer creating and retaining the compiled
        /// layout explicitly when the same table is used repeatedly.
        /// </remarks>
        public static AtlasWorkspace Create(
            AtlasContractTable contracts,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            return Create(
                AtlasShapeResolver.Resolve(contracts),
                allocator,
                options);
        }

        /// <summary>
        /// Determines whether this workspace contains a field for the supplied slot.
        /// </summary>
        public bool Contains(AtlasFieldSlot slot)
        {
            ThrowIfDisposed();

            return Layout.Contains(slot);
        }

        /// <summary>
        /// Determines whether this workspace contains a field for the supplied stable field id.
        /// </summary>
        public bool Contains(StableDataId stableId)
        {
            ThrowIfDisposed();

            return Layout.Contains(stableId);
        }

        /// <summary>
        /// Determines whether this workspace contains a typed field declaration.
        /// </summary>
        public bool Contains<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            ThrowIfDisposed();

            return Layout.Contains<TField, TElement>();
        }

        /// <summary>
        /// Attempts to get a layout entry by canonical field slot.
        /// </summary>
        public bool TryGetEntry(
            AtlasFieldSlot slot,
            out AtlasWorkspaceLayoutEntry entry)
        {
            ThrowIfDisposed();

            return Layout.TryGetEntry(
                slot,
                out entry);
        }

        /// <summary>
        /// Attempts to get a layout entry by stable field id.
        /// </summary>
        public bool TryGetEntry(
            StableDataId stableId,
            out AtlasWorkspaceLayoutEntry entry)
        {
            ThrowIfDisposed();

            return Layout.TryGetEntry(
                stableId,
                out entry);
        }

        /// <summary>
        /// Attempts to get a layout entry by typed field declaration.
        /// </summary>
        public bool TryGetEntry<TField, TElement>(
            out AtlasWorkspaceLayoutEntry entry)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            ThrowIfDisposed();

            return Layout.TryGetEntry<TField, TElement>(
                out entry);
        }

        /// <summary>
        /// Gets a required layout entry by canonical field slot.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(AtlasFieldSlot slot)
        {
            ThrowIfDisposed();

            return Layout.GetRequiredEntry(slot);
        }

        /// <summary>
        /// Gets a required layout entry by stable field id.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(StableDataId stableId)
        {
            ThrowIfDisposed();

            return Layout.GetRequiredEntry(stableId);
        }

        /// <summary>
        /// Gets a required layout entry by typed field declaration.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            ThrowIfDisposed();

            return Layout.GetRequiredEntry<TField, TElement>();
        }

        /// <summary>
        /// Attempts to get a physical field address by canonical field slot.
        /// </summary>
        public bool TryGetAddress(
            AtlasFieldSlot slot,
            out AtlasFieldAddress address)
        {
            ThrowIfDisposed();

            return Layout.TryGetAddress(
                slot,
                out address);
        }

        /// <summary>
        /// Attempts to get a physical field address by stable field id.
        /// </summary>
        public bool TryGetAddress(
            StableDataId stableId,
            out AtlasFieldAddress address)
        {
            ThrowIfDisposed();

            return Layout.TryGetAddress(
                stableId,
                out address);
        }

        /// <summary>
        /// Attempts to get a physical field address by typed field declaration.
        /// </summary>
        public bool TryGetAddress<TField, TElement>(
            out AtlasFieldAddress address)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            ThrowIfDisposed();

            return Layout.TryGetAddress<TField, TElement>(
                out address);
        }

        /// <summary>
        /// Gets a required physical field address by canonical field slot.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress(AtlasFieldSlot slot)
        {
            ThrowIfDisposed();

            return Layout.GetRequiredAddress(slot);
        }

        /// <summary>
        /// Gets a required physical field address by stable field id.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress(StableDataId stableId)
        {
            ThrowIfDisposed();

            return Layout.GetRequiredAddress(stableId);
        }

        /// <summary>
        /// Gets a required physical field address by typed field declaration.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            ThrowIfDisposed();

            return Layout.GetRequiredAddress<TField, TElement>();
        }

        /// <summary>
        /// Attempts to get the full byte capacity array of a physical storage block.
        /// </summary>
        public bool TryGetStorageBlockByteCapacityArray(
            int blockIndex,
            out NativeArray<byte> bytes)
        {
            ThrowIfDisposed();

            if (blockIndex >= 0 &&
                blockIndex < _storageBlocks.Length)
            {
                bytes = _storageBlocks[blockIndex];
                return true;
            }

            bytes = default;
            return false;
        }

        /// <summary>
        /// Gets the full byte capacity array of a required physical storage block.
        /// </summary>
        public NativeArray<byte> GetRequiredStorageBlockByteCapacityArray(int blockIndex)
        {
            ThrowIfDisposed();

            if (TryGetStorageBlockByteCapacityArray(
                    blockIndex,
                    out var bytes))
            {
                return bytes;
            }

            throw new ArgumentOutOfRangeException(
                nameof(blockIndex),
                blockIndex,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas workspace '{0}' does not contain storage block '{1}'.",
                    GetDiagnosticName(),
                    blockIndex));
        }

        /// <summary>
        /// Attempts to get the byte capacity slice for a field by canonical slot.
        /// </summary>
        public bool TryGetFieldByteCapacitySlice(
            AtlasFieldSlot slot,
            out NativeSlice<byte> bytes)
        {
            ThrowIfDisposed();

            if (Layout.TryGetEntry(slot, out var entry))
            {
                bytes = GetFieldByteCapacitySlice(entry);
                return true;
            }

            bytes = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the byte capacity slice for a field by stable field id.
        /// </summary>
        public bool TryGetFieldByteCapacitySlice(
            StableDataId stableId,
            out NativeSlice<byte> bytes)
        {
            ThrowIfDisposed();

            if (Layout.TryGetEntry(stableId, out var entry))
            {
                bytes = GetFieldByteCapacitySlice(entry);
                return true;
            }

            bytes = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the byte capacity slice for a typed field declaration.
        /// </summary>
        public bool TryGetFieldByteCapacitySlice<TField, TElement>(
            out NativeSlice<byte> bytes)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetFieldByteCapacitySlice(
                AtlasField.StableId<TField, TElement>(),
                out bytes);
        }

        /// <summary>
        /// Gets the byte capacity slice for a required field by canonical slot.
        /// </summary>
        public NativeSlice<byte> GetFieldByteCapacitySlice(AtlasFieldSlot slot)
        {
            return GetFieldByteCapacitySlice(
                GetRequiredEntry(slot));
        }

        /// <summary>
        /// Gets the byte capacity slice for a required field by stable field id.
        /// </summary>
        public NativeSlice<byte> GetFieldByteCapacitySlice(StableDataId stableId)
        {
            return GetFieldByteCapacitySlice(
                GetRequiredEntry(stableId));
        }

        /// <summary>
        /// Gets the byte capacity slice for a required typed field declaration.
        /// </summary>
        public NativeSlice<byte> GetFieldByteCapacitySlice<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetFieldByteCapacitySlice(
                GetRequiredEntry<TField, TElement>());
        }

        /// <summary>
        /// Gets the logical byte-length slice for a required field by canonical slot.
        /// </summary>
        public NativeSlice<byte> GetFieldByteLengthSlice(AtlasFieldSlot slot)
        {
            return GetFieldByteLengthSlice(
                GetRequiredEntry(slot));
        }

        /// <summary>
        /// Gets the logical byte-length slice for a required field by stable field id.
        /// </summary>
        public NativeSlice<byte> GetFieldByteLengthSlice(StableDataId stableId)
        {
            return GetFieldByteLengthSlice(
                GetRequiredEntry(stableId));
        }

        /// <summary>
        /// Gets the logical byte-length slice for a required typed field declaration.
        /// </summary>
        public NativeSlice<byte> GetFieldByteLengthSlice<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetFieldByteLengthSlice(
                GetRequiredEntry<TField, TElement>());
        }

        /// <summary>
        /// Gets a typed capacity slice for a required field by canonical slot.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacitySlice<TElement>(
            AtlasFieldSlot slot)
            where TElement : unmanaged
        {
            return GetTypedCapacitySlice<TElement>(
                GetRequiredEntry(slot));
        }

        /// <summary>
        /// Gets a typed capacity slice for a required field by stable field id.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacitySlice<TElement>(
            StableDataId stableId)
            where TElement : unmanaged
        {
            return GetTypedCapacitySlice<TElement>(
                GetRequiredEntry(stableId));
        }

        /// <summary>
        /// Gets a typed capacity slice for a required typed field declaration.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacitySlice<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetTypedCapacitySlice<TElement>(
                GetRequiredEntry<TField, TElement>());
        }

        /// <summary>
        /// Gets a typed logical-length slice for a required field by canonical slot.
        /// </summary>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            AtlasFieldSlot slot)
            where TElement : unmanaged
        {
            return GetTypedLengthSlice<TElement>(
                GetRequiredEntry(slot));
        }

        /// <summary>
        /// Gets a typed logical-length slice for a required field by stable field id.
        /// </summary>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            StableDataId stableId)
            where TElement : unmanaged
        {
            return GetTypedLengthSlice<TElement>(
                GetRequiredEntry(stableId));
        }

        /// <summary>
        /// Gets a typed logical-length slice for a required typed field declaration.
        /// </summary>
        public NativeSlice<TElement> GetTypedLengthSlice<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetTypedLengthSlice<TElement>(
                GetRequiredEntry<TField, TElement>());
        }

        /// <summary>
        /// Clears every allocated physical storage block to zero bytes.
        /// </summary>
        public void ClearAll()
        {
            ThrowIfDisposed();

            for (var blockIndex = 0; blockIndex < _storageBlocks.Length; blockIndex++)
            {
                var bytes = _storageBlocks[blockIndex];

                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = 0;
                }
            }
        }

        /// <summary>
        /// Clears the allocated byte capacity range of one field by canonical slot.
        /// </summary>
        public void Clear(AtlasFieldSlot slot)
        {
            Clear(
                GetRequiredEntry(slot));
        }

        /// <summary>
        /// Clears the allocated byte capacity range of one field by stable field id.
        /// </summary>
        public void Clear(StableDataId stableId)
        {
            Clear(
                GetRequiredEntry(stableId));
        }

        /// <summary>
        /// Clears the allocated byte capacity range of one typed field declaration.
        /// </summary>
        public void Clear<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            Clear(
                GetRequiredEntry<TField, TElement>());
        }

        /// <summary>
        /// Throws when this workspace has been disposed.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state == DisposedState)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasWorkspace),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace '{0}' has been disposed.",
                        GetDiagnosticName()));
            }
        }

        /// <summary>
        /// Disposes all owned physical storage blocks.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            for (var i = _storageBlocks.Length - 1; i >= 0; i--)
            {
                if (_storageBlocks[i].IsCreated)
                {
                    _storageBlocks[i].Dispose();
                }

                _storageBlocks[i] = default;
            }

            _state = DisposedState;
        }

        /// <summary>
        /// Returns an enumerator over field layout entries in canonical slot order.
        /// </summary>
        public IEnumerator<AtlasWorkspaceLayoutEntry> GetEnumerator()
        {
            ThrowIfDisposed();

            return Layout.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator over field layout entries in canonical slot order.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets a stable managed diagnostic workspace name.
        /// </summary>
        public string GetDiagnosticName()
        {
            return Layout.GetDiagnosticName();
        }

        /// <summary>
        /// Returns an invariant diagnostic representation.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasWorkspace(Name={0}, Fields={1}, StorageBlocks={2}, FieldBytes={3}/{4}, BlockBytes={5}/{6}, Created={7})",
                GetDiagnosticName(),
                Count,
                StorageBlockCount,
                TotalFieldByteLength,
                TotalFieldByteCapacity,
                TotalBlockUsedByteLength,
                TotalBlockByteCapacity,
                IsCreated);
        }

        private NativeSlice<byte> GetFieldByteCapacitySlice(AtlasWorkspaceLayoutEntry entry)
        {
            ThrowIfDisposed();
            entry.ValidateBoundOrThrow(nameof(entry));

            var address = entry.Address;
            var block = GetRequiredStorageBlockByteCapacityArray(address.BlockIndex);

            return new NativeSlice<byte>(
                block,
                checked((int)address.ByteOffset),
                checked((int)address.ByteCapacity));
        }

        private NativeSlice<byte> GetFieldByteLengthSlice(AtlasWorkspaceLayoutEntry entry)
        {
            ThrowIfDisposed();
            entry.ValidateBoundOrThrow(nameof(entry));

            var address = entry.Address;
            var block = GetRequiredStorageBlockByteCapacityArray(address.BlockIndex);

            return new NativeSlice<byte>(
                block,
                checked((int)address.ByteOffset),
                checked((int)address.ByteLength));
        }

        private NativeSlice<TElement> GetTypedCapacitySlice<TElement>(
            AtlasWorkspaceLayoutEntry entry)
            where TElement : unmanaged
        {
            ValidateTypedAccessOrThrow<TElement>(
                entry,
                nameof(TElement));

            var bytes = GetFieldByteCapacitySlice(entry);
            var typed = bytes.SliceConvert<TElement>();

            if (typed.Length != entry.Capacity)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace field '{0}' typed capacity slice length {1} does not match layout capacity {2}.",
                        entry.GetDiagnosticName(),
                        typed.Length,
                        entry.Capacity));
            }

            return typed;
        }

        private NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            AtlasWorkspaceLayoutEntry entry)
            where TElement : unmanaged
        {
            ValidateTypedAccessOrThrow<TElement>(
                entry,
                nameof(TElement));

            var bytes = GetFieldByteLengthSlice(entry);
            var typed = bytes.SliceConvert<TElement>();

            if (typed.Length != entry.Length)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace field '{0}' typed length slice length {1} does not match layout length {2}.",
                        entry.GetDiagnosticName(),
                        typed.Length,
                        entry.Length));
            }

            return typed;
        }

        private void Clear(AtlasWorkspaceLayoutEntry entry)
        {
            var bytes = GetFieldByteCapacitySlice(entry);

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 0;
            }
        }

        private static NativeArray<byte>[] AllocateStorageBlocks(
            AtlasWorkspaceLayout layout,
            Allocator allocator,
            NativeArrayOptions options)
        {
            var blocks = new NativeArray<byte>[layout.StorageBlockCount];

            try
            {
                for (var i = 0; i < layout.StorageBlockCount; i++)
                {
                    var blockPlan = layout.GetRequiredStorageBlockPlan(i);

                    blocks[i] = new NativeArray<byte>(
                        blockPlan.ByteCapacityInt32,
                        allocator,
                        options);
                }

                return blocks;
            }
            catch
            {
                for (var i = blocks.Length - 1; i >= 0; i--)
                {
                    if (blocks[i].IsCreated)
                    {
                        blocks[i].Dispose();
                    }
                }

                throw;
            }
        }

        private static void ValidateTypedAccessOrThrow<TElement>(
            AtlasWorkspaceLayoutEntry entry,
            string parameterName)
            where TElement : unmanaged
        {
            entry.ValidateBoundOrThrow(nameof(entry));

            entry.StorageFormat.ValidateElementTypeOrThrow<TElement>(
                parameterName);

            if (entry.StorageFormat.ElementSize <= 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace field '{0}' has invalid element size {1}.",
                        entry.GetDiagnosticName(),
                        entry.StorageFormat.ElementSize));
            }

            if (entry.ByteCapacity % entry.StorageFormat.ElementSize != 0L)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace field '{0}' byte capacity {1} is not divisible by element size {2}.",
                        entry.GetDiagnosticName(),
                        entry.ByteCapacity,
                        entry.StorageFormat.ElementSize));
            }

            if (entry.ByteLength % entry.StorageFormat.ElementSize != 0L)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace field '{0}' byte length {1} is not divisible by element size {2}.",
                        entry.GetDiagnosticName(),
                        entry.ByteLength,
                        entry.StorageFormat.ElementSize));
            }
        }

        private static void ValidateAllocatorOrThrow(Allocator allocator)
        {
            if (allocator == Allocator.None ||
                allocator == Allocator.Invalid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(allocator),
                    allocator,
                    "Atlas workspace requires a concrete Unity allocator.");
            }
        }
    }
}