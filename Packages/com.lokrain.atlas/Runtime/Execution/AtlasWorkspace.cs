// Packages/com.lokrain.atlas/Runtime/Execution/AtlasWorkspace.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Own all allocated field memory blocks for one resolved Atlas execution shape set.
// - Preserve canonical Contract-table slot order.
// - Expose lookup by slot, StableDataId, and typed field declaration.
// - Dispose all field memory deterministically.
//
// Design notes
// - This is memory ownership, not operation scheduling.
// - This is not an artifact.
// - This is not a renderer or preview object.
// - This first workspace slice supports only AtlasFieldMemoryBlock-supported storage.
// - Unsupported storage kinds fail during workspace creation instead of producing fake storage.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Workspace-owned native memory for one resolved Atlas plan shape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWorkspace"/> is the aggregate memory owner in Atlas execution.
    /// It allocates one <see cref="AtlasFieldMemoryBlock"/> per resolved shape and preserves
    /// canonical Contract-table slot order.
    /// </para>
    ///
    /// <para>
    /// The workspace does not know operation semantics. It does not compile, validate dataflow,
    /// schedule jobs, write artifacts, or render debug maps. Those systems consume workspace-owned
    /// typed views.
    /// </para>
    ///
    /// <para>
    /// This implementation intentionally supports only storage kinds handled by
    /// <see cref="AtlasFieldMemoryBlock"/>. Growable lists, streams, hash maps, blobs, and external
    /// memory require dedicated container ownership models.
    /// </para>
    /// </remarks>
    public sealed class AtlasWorkspace :
        IDisposable,
        IReadOnlyList<AtlasFieldMemoryBlock>
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private readonly AtlasFieldMemoryBlock[] _blocks;
        private byte _state;

        /// <summary>
        /// Resolved shapes used to allocate this workspace.
        /// </summary>
        public readonly AtlasResolvedShapeSet Shapes;

        /// <summary>
        /// Allocator used for all owned native memory blocks.
        /// </summary>
        public readonly Allocator Allocator;

        private AtlasWorkspace(
            AtlasResolvedShapeSet shapes,
            Allocator allocator,
            NativeArrayOptions options)
        {
            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            ValidateAllocatorOrThrow(allocator);

            shapes.ValidateOrThrow(nameof(shapes));

            Shapes = shapes;
            Allocator = allocator;
            _blocks = AllocateBlocks(shapes, allocator, options);
            _state = AliveState;
        }

        /// <summary>
        /// Gets the Contract table that owns canonical field slots.
        /// </summary>
        public AtlasContractTable Contracts => Shapes.Contracts;

        /// <summary>
        /// Gets the number of allocated field blocks.
        /// </summary>
        public int Count => _blocks.Length;

        /// <summary>
        /// Gets whether the workspace contains no field blocks.
        /// </summary>
        public bool IsEmpty => _blocks.Length == 0;

        /// <summary>
        /// Gets whether the workspace is alive.
        /// </summary>
        public bool IsCreated => _state == AliveState;

        /// <summary>
        /// Gets whether the workspace has been disposed.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Gets the total logical byte length of all workspace fields.
        /// </summary>
        public long TotalByteLength => Shapes.TotalByteLength;

        /// <summary>
        /// Gets the total allocated byte capacity of all workspace fields.
        /// </summary>
        public long TotalByteCapacity => Shapes.TotalByteCapacity;

        /// <summary>
        /// Gets a field memory block by canonical workspace index.
        /// </summary>
        /// <param name="index">Zero-based canonical field index.</param>
        /// <returns>The field memory block at <paramref name="index"/>.</returns>
        public AtlasFieldMemoryBlock this[int index]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfIndexOutOfRange(index);
                return _blocks[index];
            }
        }

        /// <summary>
        /// Gets a field memory block by canonical Contract-table slot.
        /// </summary>
        /// <param name="slot">Canonical field slot.</param>
        /// <returns>The field memory block assigned to <paramref name="slot"/>.</returns>
        public AtlasFieldMemoryBlock this[AtlasFieldSlot slot] => this[slot.Index];

        /// <summary>
        /// Allocates a workspace from a resolved shape set.
        /// </summary>
        /// <param name="shapes">Resolved shape set.</param>
        /// <param name="allocator">Unity allocator used for owned native memory.</param>
        /// <param name="options">Native allocation initialization option.</param>
        /// <returns>A live Atlas workspace.</returns>
        public static AtlasWorkspace Create(
            AtlasResolvedShapeSet shapes,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            return new AtlasWorkspace(
                shapes,
                allocator,
                options);
        }

        /// <summary>
        /// Allocates a workspace from a Contract table by resolving shapes first.
        /// </summary>
        /// <param name="contracts">Contract table.</param>
        /// <param name="allocator">Unity allocator used for owned native memory.</param>
        /// <param name="options">Native allocation initialization option.</param>
        /// <returns>A live Atlas workspace.</returns>
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
        /// Attempts to get a field memory block by canonical slot.
        /// </summary>
        /// <param name="slot">Canonical field slot.</param>
        /// <param name="block">Resolved block on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the slot exists in this workspace.</returns>
        public bool TryGetBlock(
            AtlasFieldSlot slot,
            out AtlasFieldMemoryBlock block)
        {
            ThrowIfDisposed();

            var index = slot.Index;

            if (index >= 0 && index < _blocks.Length)
            {
                block = _blocks[index];
                return true;
            }

            block = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a field memory block by stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id.</param>
        /// <param name="block">Resolved block on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the field exists in this workspace.</returns>
        public bool TryGetBlock(
            StableDataId stableId,
            out AtlasFieldMemoryBlock block)
        {
            ThrowIfDisposed();

            if (Contracts.TryGetSlot(stableId, out var slot))
            {
                return TryGetBlock(slot, out block);
            }

            block = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a field memory block by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged field element type.</typeparam>
        /// <param name="block">Resolved block on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the field exists in this workspace.</returns>
        public bool TryGetBlock<TField, TElement>(
            out AtlasFieldMemoryBlock block)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetBlock(
                AtlasField.StableId<TField, TElement>(),
                out block);
        }

        /// <summary>
        /// Gets a required field memory block by canonical slot.
        /// </summary>
        /// <param name="slot">Canonical field slot.</param>
        /// <returns>The field memory block assigned to <paramref name="slot"/>.</returns>
        public AtlasFieldMemoryBlock GetRequiredBlock(AtlasFieldSlot slot)
        {
            if (TryGetBlock(slot, out var block))
            {
                return block;
            }

            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Atlas workspace '{GetDiagnosticName()}' does not contain slot '{slot}'.");
        }

        /// <summary>
        /// Gets a required field memory block by stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id.</param>
        /// <returns>The field memory block assigned to <paramref name="stableId"/>.</returns>
        public AtlasFieldMemoryBlock GetRequiredBlock(StableDataId stableId)
        {
            if (TryGetBlock(stableId, out var block))
            {
                return block;
            }

            throw new ArgumentException(
                $"Atlas workspace '{GetDiagnosticName()}' does not contain field id '{stableId}'.",
                nameof(stableId));
        }

        /// <summary>
        /// Gets a required field memory block by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged field element type.</typeparam>
        /// <returns>The field memory block assigned to the typed field declaration.</returns>
        public AtlasFieldMemoryBlock GetRequiredBlock<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredBlock(
                AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Gets a typed native capacity view by canonical slot.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="slot">Canonical field slot.</param>
        /// <returns>A typed NativeArray view over the full resolved field capacity.</returns>
        public NativeArray<TElement> GetTypedCapacityArray<TElement>(
            AtlasFieldSlot slot)
            where TElement : unmanaged
        {
            return GetRequiredBlock(slot)
                .GetTypedCapacityArray<TElement>();
        }

        /// <summary>
        /// Gets a typed native logical-length view by canonical slot.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="slot">Canonical field slot.</param>
        /// <returns>A typed NativeSlice view over the resolved logical field length.</returns>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            AtlasFieldSlot slot)
            where TElement : unmanaged
        {
            return GetRequiredBlock(slot)
                .GetTypedLengthSlice<TElement>();
        }

        /// <summary>
        /// Gets a typed native capacity view by stable field id.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="stableId">Stable field id.</param>
        /// <returns>A typed NativeArray view over the full resolved field capacity.</returns>
        public NativeArray<TElement> GetTypedCapacityArray<TElement>(
            StableDataId stableId)
            where TElement : unmanaged
        {
            return GetRequiredBlock(stableId)
                .GetTypedCapacityArray<TElement>();
        }

        /// <summary>
        /// Gets a typed native logical-length view by stable field id.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="stableId">Stable field id.</param>
        /// <returns>A typed NativeSlice view over the resolved logical field length.</returns>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            StableDataId stableId)
            where TElement : unmanaged
        {
            return GetRequiredBlock(stableId)
                .GetTypedLengthSlice<TElement>();
        }

        /// <summary>
        /// Gets a typed native capacity view by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <returns>A typed NativeArray view over the full resolved field capacity.</returns>
        public NativeArray<TElement> GetTypedCapacityArray<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredBlock<TField, TElement>()
                .GetTypedCapacityArray<TElement>();
        }

        /// <summary>
        /// Gets a typed native logical-length view by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <returns>A typed NativeSlice view over the resolved logical field length.</returns>
        public NativeSlice<TElement> GetTypedLengthSlice<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredBlock<TField, TElement>()
                .GetTypedLengthSlice<TElement>();
        }

        /// <summary>
        /// Clears every allocated field block to zero bytes.
        /// </summary>
        public void ClearAll()
        {
            ThrowIfDisposed();

            for (var i = 0; i < _blocks.Length; i++)
            {
                _blocks[i].Clear();
            }
        }

        /// <summary>
        /// Clears one allocated field block by canonical slot.
        /// </summary>
        /// <param name="slot">Canonical field slot.</param>
        public void Clear(AtlasFieldSlot slot)
        {
            GetRequiredBlock(slot).Clear();
        }

        /// <summary>
        /// Clears one allocated field block by stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id.</param>
        public void Clear(StableDataId stableId)
        {
            GetRequiredBlock(stableId).Clear();
        }

        /// <summary>
        /// Clears one allocated field block by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged field element type.</typeparam>
        public void Clear<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            GetRequiredBlock<TField, TElement>().Clear();
        }

        /// <summary>
        /// Throws when the workspace has been disposed.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state == DisposedState)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasWorkspace),
                    $"Atlas workspace '{GetDiagnosticName()}' has been disposed.");
            }
        }

        /// <summary>
        /// Disposes all owned field memory blocks.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            for (var i = _blocks.Length - 1; i >= 0; i--)
            {
                _blocks[i]?.Dispose();
            }

            _state = DisposedState;
        }

        /// <summary>
        /// Gets an enumerator over allocated field blocks in canonical slot order.
        /// </summary>
        /// <returns>An enumerator over field memory blocks.</returns>
        public IEnumerator<AtlasFieldMemoryBlock> GetEnumerator()
        {
            ThrowIfDisposed();

            for (var i = 0; i < _blocks.Length; i++)
            {
                yield return _blocks[i];
            }
        }

        /// <summary>
        /// Gets an enumerator over allocated field blocks in canonical slot order.
        /// </summary>
        /// <returns>An enumerator over field memory blocks.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a stable diagnostic workspace name.
        /// </summary>
        /// <returns>The shape-set name when present; otherwise, an invariant fallback.</returns>
        public string GetDiagnosticName()
        {
            return Shapes.GetDiagnosticName();
        }

        /// <summary>
        /// Returns a diagnostic representation of this workspace.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return
                $"AtlasWorkspace(Name={GetDiagnosticName()}, Count={Count}, ByteLength={TotalByteLength}, ByteCapacity={TotalByteCapacity}, Created={IsCreated})";
        }

        private static AtlasFieldMemoryBlock[] AllocateBlocks(
            AtlasResolvedShapeSet shapes,
            Allocator allocator,
            NativeArrayOptions options)
        {
            var blocks = new AtlasFieldMemoryBlock[shapes.Count];

            try
            {
                for (var i = 0; i < shapes.Count; i++)
                {
                    var shape = shapes[i];
                    var contract = shapes.Contracts[i];

                    if (!AtlasFieldMemoryBlock.SupportsOwnedByteBlock(contract.StorageFormat.Kind))
                    {
                        throw new NotSupportedException(
                            $"Atlas workspace cannot allocate field '{contract.GetDiagnosticName()}' with storage kind '{contract.StorageFormat.Kind}' using AtlasFieldMemoryBlock.");
                    }

                    blocks[i] = AtlasFieldMemoryBlock.Create(
                        contract,
                        shape,
                        allocator,
                        options);
                }

                return blocks;
            }
            catch
            {
                for (var i = blocks.Length - 1; i >= 0; i--)
                {
                    blocks[i]?.Dispose();
                }

                throw;
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

        private void ThrowIfIndexOutOfRange(int index)
        {
            if (index >= 0 && index < _blocks.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Workspace field index must be between 0 and {_blocks.Length - 1}.");
        }
    }
}