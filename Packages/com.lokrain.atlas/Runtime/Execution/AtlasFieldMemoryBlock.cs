// Packages/com.lokrain.atlas/Runtime/Execution/AtlasFieldMemoryBlock.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Own native byte storage for one resolved Atlas field.
// - Preserve the source Contract and resolved shape metadata.
// - Expose typed NativeArray/NativeSlice views for scalar and NativeArray storage.
// - Stay independent from operation scheduling, artifact writing, and rendering.
//
// Design notes
// - This is the first workspace-owned memory primitive.
// - This class owns and disposes the native byte buffer.
// - It intentionally supports only Scalar and NativeArray for the first vertical slice.
// - NativeList, UnsafeList, NativeStream, NativeParallelHashMap, Blob, and External storage
//   require dedicated container ownership models and must not be faked as raw arrays.
// - Jobs should receive typed views from this block, not resolve field identity.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Workspace-owned native memory for one resolved Atlas field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasFieldMemoryBlock"/> is the first concrete memory ownership boundary in
    /// Atlas execution. It owns a native byte buffer sized from an <see cref="AtlasResolvedShape"/>
    /// and exposes typed views after validating the requested unmanaged element type against the
    /// source <see cref="AtlasContract"/>.
    /// </para>
    ///
    /// <para>
    /// This class intentionally supports only <see cref="StorageKind.Scalar"/> and
    /// <see cref="StorageKind.NativeArray"/>. Growable containers, streams, hash maps, blobs, and
    /// external storage require dedicated runtime objects with different mutation, disposal, and
    /// safety semantics.
    /// </para>
    ///
    /// <para>
    /// This type does not schedule jobs, does not write artifacts, and does not know operation
    /// semantics. It only owns one allocated field buffer.
    /// </para>
    /// </remarks>
    public sealed class AtlasFieldMemoryBlock : IDisposable
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private byte _state;
        private NativeArray<byte> _bytes;

        /// <summary>
        /// Source Contract row used to validate ownership, storage, and typed access.
        /// </summary>
        public readonly AtlasContract Contract;

        /// <summary>
        /// Concrete resolved shape used to size this block.
        /// </summary>
        public readonly AtlasResolvedShape Shape;

        /// <summary>
        /// Allocator used to allocate the native byte buffer.
        /// </summary>
        public readonly Allocator Allocator;

        private AtlasFieldMemoryBlock(
            AtlasContract contract,
            AtlasResolvedShape shape,
            Allocator allocator,
            NativeArrayOptions options)
        {
            ValidateAllocationInputsOrThrow(
                contract,
                shape,
                allocator);

            Contract = contract;
            Shape = shape;
            Allocator = allocator;

            var byteCapacity = checked((int)shape.ByteCapacity);

            _bytes = new NativeArray<byte>(
                byteCapacity,
                allocator,
                options);

            _state = AliveState;
        }

        /// <summary>
        /// Gets the durable field identity.
        /// </summary>
        public StableDataId StableId => Contract.StableId;

        /// <summary>
        /// Gets the canonical Contract-table slot.
        /// </summary>
        public AtlasFieldSlot Slot => Contract.Slot;

        /// <summary>
        /// Gets the semantic field role.
        /// </summary>
        public AtlasFieldRole Role => Contract.Role;

        /// <summary>
        /// Gets the physical storage format.
        /// </summary>
        public StorageFormat StorageFormat => Contract.StorageFormat;

        /// <summary>
        /// Gets the declared storage kind.
        /// </summary>
        public StorageKind StorageKind => Contract.StorageFormat.Kind;

        /// <summary>
        /// Gets the resolved logical element length.
        /// </summary>
        public int Length => Shape.Length;

        /// <summary>
        /// Gets the resolved element capacity.
        /// </summary>
        public int Capacity => Shape.Capacity;

        /// <summary>
        /// Gets the resolved logical byte length.
        /// </summary>
        public long ByteLength => Shape.ByteLength;

        /// <summary>
        /// Gets the resolved byte capacity.
        /// </summary>
        public long ByteCapacity => Shape.ByteCapacity;

        /// <summary>
        /// Gets whether this block is currently alive and its native buffer was created.
        /// </summary>
        public bool IsCreated => _state == AliveState && _bytes.IsCreated;

        /// <summary>
        /// Gets whether this block has been disposed.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Creates an owned native memory block for one resolved Contract field.
        /// </summary>
        /// <param name="contract">Source Contract row.</param>
        /// <param name="shape">Resolved shape matching the Contract row.</param>
        /// <param name="allocator">Unity allocator used for native memory.</param>
        /// <param name="options">NativeArray initialization option.</param>
        /// <returns>A live workspace-owned field memory block.</returns>
        public static AtlasFieldMemoryBlock Create(
            AtlasContract contract,
            AtlasResolvedShape shape,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            return new AtlasFieldMemoryBlock(
                contract,
                shape,
                allocator,
                options);
        }

        /// <summary>
        /// Returns whether this storage kind is supported by this first native byte-block allocator.
        /// </summary>
        /// <param name="kind">Storage kind to inspect.</param>
        /// <returns><c>true</c> for scalar and native-array storage; otherwise, <c>false</c>.</returns>
        public static bool SupportsOwnedByteBlock(StorageKind kind)
        {
            return kind == StorageKind.Scalar ||
                   kind == StorageKind.NativeArray;
        }

        /// <summary>
        /// Returns the full native byte capacity buffer.
        /// </summary>
        /// <returns>The owned byte buffer.</returns>
        public NativeArray<byte> GetByteCapacityArray()
        {
            ThrowIfDisposed();
            return _bytes;
        }

        /// <summary>
        /// Returns the logical byte-length slice.
        /// </summary>
        /// <returns>A byte slice covering only the resolved logical length.</returns>
        public NativeSlice<byte> GetByteLengthSlice()
        {
            ThrowIfDisposed();

            return new NativeSlice<byte>(
                _bytes,
                0,
                checked((int)ByteLength));
        }

        /// <summary>
        /// Returns a typed view over the full resolved capacity.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <returns>A typed NativeArray view over the full capacity.</returns>
        /// <remarks>
        /// <para>
        /// For fixed-size fields, capacity equals logical length. This method exists so workspace
        /// code can use one access path for both scalar and array fields.
        /// </para>
        ///
        /// <para>
        /// Unity's <see cref="NativeArray{T}.Reinterpret{U}(int)"/> argument is the expected size
        /// of the source element type, not the target element type. The source array here is
        /// <see cref="byte"/>, so the correct value is always <c>sizeof(byte)</c>.
        /// </para>
        /// </remarks>
        public NativeArray<TElement> GetTypedCapacityArray<TElement>()
            where TElement : unmanaged
        {
            ThrowIfDisposed();
            ValidateTypedAccessOrThrow<TElement>();

            return _bytes.Reinterpret<TElement>(sizeof(byte));
        }

        /// <summary>
        /// Returns a typed view over the resolved logical length.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <returns>A typed NativeSlice view over the logical field length.</returns>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>()
            where TElement : unmanaged
        {
            var capacityArray = GetTypedCapacityArray<TElement>();

            return new NativeSlice<TElement>(
                capacityArray,
                0,
                Length);
        }

        /// <summary>
        /// Clears the full byte capacity.
        /// </summary>
        /// <remarks>
        /// This is intentionally simple for the first vertical slice. Large production clears can
        /// later route through Burst jobs or field-specific clear semantics without changing
        /// ownership.
        /// </remarks>
        public void Clear()
        {
            ThrowIfDisposed();

            for (var i = 0; i < _bytes.Length; i++)
            {
                _bytes[i] = 0;
            }
        }

        /// <summary>
        /// Throws if this block has been disposed or if its native storage is missing.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state == DisposedState)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasFieldMemoryBlock),
                    $"Atlas field memory block '{GetDiagnosticName()}' has been disposed.");
            }

            if (!_bytes.IsCreated)
            {
                throw new InvalidOperationException(
                    $"Atlas field memory block '{GetDiagnosticName()}' has no created native buffer.");
            }
        }

        /// <summary>
        /// Disposes the owned native byte buffer.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            if (_bytes.IsCreated)
            {
                _bytes.Dispose();
            }

            _state = DisposedState;
        }

        /// <summary>
        /// Returns a stable diagnostic field name.
        /// </summary>
        /// <returns>The Contract debug name when present; otherwise, the stable id.</returns>
        public string GetDiagnosticName()
        {
            return Contract.GetDiagnosticName();
        }

        /// <summary>
        /// Returns a diagnostic representation of this memory block.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return
                $"AtlasFieldMemoryBlock(Name={GetDiagnosticName()}, Slot={Slot}, Storage={StorageKind}, Length={Length}, Capacity={Capacity}, Bytes={ByteLength}/{ByteCapacity}, Created={IsCreated})";
        }

        private void ValidateTypedAccessOrThrow<TElement>()
            where TElement : unmanaged
        {
            if (!SupportsOwnedByteBlock(StorageKind))
            {
                throw new NotSupportedException(
                    $"Atlas field memory block '{GetDiagnosticName()}' cannot expose storage kind '{StorageKind}' as a NativeArray.");
            }

            StorageFormat.ValidateElementTypeOrThrow<TElement>(
                nameof(TElement));

            if (StorageFormat.ElementSize <= 0)
            {
                throw new InvalidOperationException(
                    $"Atlas field memory block '{GetDiagnosticName()}' has invalid element size '{StorageFormat.ElementSize}'.");
            }

            if (_bytes.Length != checked(StorageFormat.ElementSize * Capacity))
            {
                throw new InvalidOperationException(
                    $"Atlas field memory block '{GetDiagnosticName()}' has byte capacity '{_bytes.Length}', but expected '{StorageFormat.ElementSize * Capacity}'.");
            }
        }

        private static void ValidateAllocationInputsOrThrow(
            AtlasContract contract,
            AtlasResolvedShape shape,
            Allocator allocator)
        {
            contract.ValidateTableReadyOrThrow(nameof(contract));
            shape.ValidateOrThrow(nameof(shape));

            if (allocator == Allocator.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(allocator),
                    allocator,
                    "Atlas field memory blocks require a concrete Unity allocator.");
            }

            if (!SupportsOwnedByteBlock(contract.StorageFormat.Kind))
            {
                throw new NotSupportedException(
                    $"Atlas field '{contract.GetDiagnosticName()}' declares storage kind '{contract.StorageFormat.Kind}', which is not supported by AtlasFieldMemoryBlock. Use a dedicated runtime container model for this storage kind.");
            }

            if (contract.Ownership != OwnershipPolicy.AtlasOwned &&
                contract.Ownership != OwnershipPolicy.Adopted)
            {
                throw new NotSupportedException(
                    $"Atlas field '{contract.GetDiagnosticName()}' declares ownership '{contract.Ownership}'. AtlasFieldMemoryBlock may only allocate Atlas-owned or adopted storage.");
            }

            if (contract.StorageFormat != shape.StorageFormat)
            {
                throw new ArgumentException(
                    $"Atlas field '{contract.GetDiagnosticName()}' storage format does not match resolved shape storage format.",
                    nameof(shape));
            }

            if (contract.StableId != shape.StableId)
            {
                throw new ArgumentException(
                    $"Atlas field '{contract.GetDiagnosticName()}' stable id does not match resolved shape stable id.",
                    nameof(shape));
            }

            if (contract.Slot != shape.Slot)
            {
                throw new ArgumentException(
                    $"Atlas field '{contract.GetDiagnosticName()}' slot does not match resolved shape slot.",
                    nameof(shape));
            }

            if (contract.Role != shape.Role)
            {
                throw new ArgumentException(
                    $"Atlas field '{contract.GetDiagnosticName()}' role does not match resolved shape role.",
                    nameof(shape));
            }

            if (shape.ByteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    $"Atlas field '{contract.GetDiagnosticName()}' byte capacity '{shape.ByteCapacity}' exceeds NativeArray length capacity.");
            }

            if (shape.ByteCapacity != checked((long)contract.StorageFormat.ElementSize * shape.Capacity))
            {
                throw new ArgumentException(
                    $"Atlas field '{contract.GetDiagnosticName()}' resolved byte capacity is inconsistent with element size and capacity.",
                    nameof(shape));
            }
        }
    }
}