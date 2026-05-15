// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasFieldAddress.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Represent the numeric byte-block address of one Atlas field inside a compiled workspace layout.
// - Separate physical memory facts from semantic shape-resolution facts.
// - Give execution-plan compilation and schedulers a compact unmanaged field binding.
// - Keep jobs away from Contracts, compiled semantic plans, workspace owners, and managed metadata.
//
// Design notes
// - This is layout/execution metadata, not authored semantic metadata.
// - This type does not own memory.
// - This type does not allocate memory.
// - This type does not expose Unity native containers.
// - This type intentionally supports fixed contiguous byte-block storage only.
// - Stream, hash-map, blob, and external storage require dedicated physical binding models.
// - default(AtlasFieldAddress) is unbound for Burst/unmanaged compatibility.
// - Bound addresses are created only through factory methods and are valid after construction.
// - Missing/unbound state is represented by an explicit binding-state byte, not by sentinel ids.
// - Slot zero, block index zero, byte offset zero, and type-hash zero are valid values.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Numeric physical address of one field inside a compiled Atlas workspace layout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasFieldAddress"/> is produced by workspace-layout compilation after semantic
    /// field shape resolution. It tells later execution products where a field lives physically:
    /// which storage block contains it, where its byte range starts, and how many logical and
    /// capacity elements it owns.
    /// </para>
    ///
    /// <para>
    /// This value deliberately does not carry <c>AtlasContract</c>, <c>AtlasResolvedShape</c>,
    /// operation contracts, pipeline contracts, debug strings, or workspace ownership. Schedulers
    /// and jobs should use this value together with already-resolved native memory blocks or typed
    /// views.
    /// </para>
    ///
    /// <para>
    /// The default value is allowed only as an unbound unmanaged payload. Use
    /// <see cref="IsBound"/> or <see cref="ValidateBoundOrThrow"/> at construction and compiler
    /// boundaries. Normal execution should consume only bound addresses.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasFieldAddress :
        IEquatable<AtlasFieldAddress>,
        IComparable<AtlasFieldAddress>
    {
        private const byte UnboundState = 0;
        private const byte BoundState = 1;

        private readonly byte _bindingState;

        /// <summary>
        /// Canonical Contract-table slot for the field.
        /// </summary>
        /// <remarks>
        /// This is a numeric table slot, not durable semantic identity. Slot zero is valid.
        /// </remarks>
        public readonly AtlasFieldSlot Slot;

        /// <summary>
        /// Concrete unmanaged storage format expected at this address.
        /// </summary>
        public readonly StorageFormat StorageFormat;

        /// <summary>
        /// Zero-based physical workspace storage-block index.
        /// </summary>
        /// <remarks>
        /// Block index zero is valid. Absence is represented by <see cref="IsUnbound"/>, not by a
        /// negative index.
        /// </remarks>
        public readonly int BlockIndex;

        /// <summary>
        /// Byte offset from the start of the physical workspace storage block.
        /// </summary>
        /// <remarks>
        /// Offset zero is valid. For bound addresses, this offset is aligned to
        /// <see cref="StorageFormat.ElementAlignment"/>.
        /// </remarks>
        public readonly long ByteOffset;

        /// <summary>
        /// Resolved logical element length.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Resolved element capacity.
        /// </summary>
        public readonly int Capacity;

        /// <summary>
        /// Resolved logical byte length.
        /// </summary>
        public readonly long ByteLength;

        /// <summary>
        /// Resolved byte capacity.
        /// </summary>
        public readonly long ByteCapacity;

        private AtlasFieldAddress(
            AtlasFieldSlot slot,
            StorageFormat storageFormat,
            int blockIndex,
            long byteOffset,
            int length,
            int capacity)
        {
            Slot = slot;
            StorageFormat = storageFormat;
            BlockIndex = blockIndex;
            ByteOffset = byteOffset;
            Length = length;
            Capacity = capacity;
            ByteLength = ComputeByteCount(storageFormat, length);
            ByteCapacity = ComputeByteCount(storageFormat, capacity);
            _bindingState = BoundState;
        }

        /// <summary>
        /// Gets whether this address has been bound by workspace-layout compilation.
        /// </summary>
        public bool IsBound
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bindingState == BoundState;
        }

        /// <summary>
        /// Gets whether this address is the default unbound unmanaged payload.
        /// </summary>
        public bool IsUnbound
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bindingState != BoundState;
        }

        /// <summary>
        /// Gets the declared storage kind.
        /// </summary>
        public StorageKind StorageKind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StorageFormat.Kind;
        }

        /// <summary>
        /// Gets the size in bytes of one element.
        /// </summary>
        public int ElementSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StorageFormat.ElementSize;
        }

        /// <summary>
        /// Gets the required byte alignment of one element.
        /// </summary>
        public int ElementAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StorageFormat.ElementAlignment;
        }

        /// <summary>
        /// Gets the exclusive end byte offset of the logical field range.
        /// </summary>
        public long LogicalEndByteOffsetExclusive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ByteOffset + ByteLength;
        }

        /// <summary>
        /// Gets the exclusive end byte offset of the allocated field-capacity range.
        /// </summary>
        public long CapacityEndByteOffsetExclusive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ByteOffset + ByteCapacity;
        }

        /// <summary>
        /// Gets whether capacity exceeds logical length.
        /// </summary>
        public bool HasCapacitySlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsBound && Capacity > Length;
        }

        /// <summary>
        /// Gets whether this address refers to a non-empty physical byte range.
        /// </summary>
        public bool RequiresMemory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsBound && ByteCapacity > 0L;
        }

        /// <summary>
        /// Creates a bound field address whose capacity equals its logical length.
        /// </summary>
        public static AtlasFieldAddress Create(
            AtlasFieldSlot slot,
            StorageFormat storageFormat,
            int blockIndex,
            long byteOffset,
            int length)
        {
            return Create(
                slot,
                storageFormat,
                blockIndex,
                byteOffset,
                length,
                length);
        }

        /// <summary>
        /// Creates a bound field address.
        /// </summary>
        public static AtlasFieldAddress Create(
            AtlasFieldSlot slot,
            StorageFormat storageFormat,
            int blockIndex,
            long byteOffset,
            int length,
            int capacity)
        {
            ValidateInputsOrThrow(
                slot,
                storageFormat,
                blockIndex,
                byteOffset,
                length,
                capacity);

            return new AtlasFieldAddress(
                slot,
                storageFormat,
                blockIndex,
                byteOffset,
                length,
                capacity);
        }

        /// <summary>
        /// Returns a copy of this address assigned to a different block location.
        /// </summary>
        public AtlasFieldAddress WithBlockLocation(
            int blockIndex,
            long byteOffset)
        {
            ValidateBoundOrThrow();

            return Create(
                Slot,
                StorageFormat,
                blockIndex,
                byteOffset,
                Length,
                Capacity);
        }

        /// <summary>
        /// Returns a copy of this address with a different logical length and matching capacity.
        /// </summary>
        public AtlasFieldAddress WithLength(int length)
        {
            ValidateBoundOrThrow();

            return Create(
                Slot,
                StorageFormat,
                BlockIndex,
                ByteOffset,
                length,
                length);
        }

        /// <summary>
        /// Returns a copy of this address with different logical length and capacity.
        /// </summary>
        public AtlasFieldAddress WithLengthAndCapacity(
            int length,
            int capacity)
        {
            ValidateBoundOrThrow();

            return Create(
                Slot,
                StorageFormat,
                BlockIndex,
                ByteOffset,
                length,
                capacity);
        }

        /// <summary>
        /// Returns whether the supplied logical element index is inside this field's logical range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsLogicalElementIndex(int elementIndex)
        {
            return IsBound &&
                   elementIndex >= 0 &&
                   elementIndex < Length;
        }

        /// <summary>
        /// Returns whether the supplied element index is inside this field's allocated capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsCapacityElementIndex(int elementIndex)
        {
            return IsBound &&
                   elementIndex >= 0 &&
                   elementIndex < Capacity;
        }

        /// <summary>
        /// Attempts to compute the block-relative byte offset of a logical element.
        /// </summary>
        public bool TryGetLogicalElementByteOffset(
            int elementIndex,
            out long blockByteOffset)
        {
            if (!ContainsLogicalElementIndex(elementIndex))
            {
                blockByteOffset = 0L;
                return false;
            }

            blockByteOffset = ByteOffset + checked((long)elementIndex * StorageFormat.ElementSize);
            return true;
        }

        /// <summary>
        /// Attempts to compute the block-relative byte offset of an allocated-capacity element.
        /// </summary>
        public bool TryGetCapacityElementByteOffset(
            int elementIndex,
            out long blockByteOffset)
        {
            if (!ContainsCapacityElementIndex(elementIndex))
            {
                blockByteOffset = 0L;
                return false;
            }

            blockByteOffset = ByteOffset + checked((long)elementIndex * StorageFormat.ElementSize);
            return true;
        }

        /// <summary>
        /// Computes the block-relative byte offset of a logical element.
        /// </summary>
        public long GetLogicalElementByteOffsetChecked(int elementIndex)
        {
            ValidateBoundOrThrow();

            if (!TryGetLogicalElementByteOffset(elementIndex, out var blockByteOffset))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elementIndex),
                    elementIndex,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Logical element index must be between 0 and {0}.",
                        Length - 1));
            }

            return blockByteOffset;
        }

        /// <summary>
        /// Computes the block-relative byte offset of an allocated-capacity element.
        /// </summary>
        public long GetCapacityElementByteOffsetChecked(int elementIndex)
        {
            ValidateBoundOrThrow();

            if (!TryGetCapacityElementByteOffset(elementIndex, out var blockByteOffset))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elementIndex),
                    elementIndex,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Capacity element index must be between 0 and {0}.",
                        Capacity - 1));
            }

            return blockByteOffset;
        }

        /// <summary>
        /// Returns whether a block-relative byte range is inside this address's logical byte range.
        /// </summary>
        public bool ContainsLogicalBlockByteRange(
            long blockByteOffset,
            long byteCount)
        {
            if (IsUnbound ||
                blockByteOffset < ByteOffset ||
                byteCount < 0L ||
                blockByteOffset > long.MaxValue - byteCount)
            {
                return false;
            }

            return blockByteOffset + byteCount <= LogicalEndByteOffsetExclusive;
        }

        /// <summary>
        /// Returns whether a block-relative byte range is inside this address's allocated capacity.
        /// </summary>
        public bool ContainsCapacityBlockByteRange(
            long blockByteOffset,
            long byteCount)
        {
            if (IsUnbound ||
                blockByteOffset < ByteOffset ||
                byteCount < 0L ||
                blockByteOffset > long.MaxValue - byteCount)
            {
                return false;
            }

            return blockByteOffset + byteCount <= CapacityEndByteOffsetExclusive;
        }

        /// <summary>
        /// Validates that this value is a bound, internally consistent field address.
        /// </summary>
        public void ValidateBoundOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasFieldAddress);

            if (IsUnbound)
            {
                throw new ArgumentException(
                    "Atlas field address is unbound.",
                    name);
            }

            ValidateInputsOrThrow(
                Slot,
                StorageFormat,
                BlockIndex,
                ByteOffset,
                Length,
                Capacity,
                name);

            var expectedByteLength = ComputeByteCount(
                StorageFormat,
                Length);

            var expectedByteCapacity = ComputeByteCount(
                StorageFormat,
                Capacity);

            if (ByteLength != expectedByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas field address byte length {0} does not match expected byte length {1}.",
                        ByteLength,
                        expectedByteLength),
                    name);
            }

            if (ByteCapacity != expectedByteCapacity)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas field address byte capacity {0} does not match expected byte capacity {1}.",
                        ByteCapacity,
                        expectedByteCapacity),
                    name);
            }
        }

        /// <summary>
        /// Determines whether this address equals another address.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AtlasFieldAddress other)
        {
            return _bindingState == other._bindingState &&
                   Slot == other.Slot &&
                   StorageFormat == other.StorageFormat &&
                   BlockIndex == other.BlockIndex &&
                   ByteOffset == other.ByteOffset &&
                   Length == other.Length &&
                   Capacity == other.Capacity &&
                   ByteLength == other.ByteLength &&
                   ByteCapacity == other.ByteCapacity;
        }

        /// <summary>
        /// Determines whether this address equals an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasFieldAddress other &&
                   Equals(other);
        }

        /// <summary>
        /// Compares this address with another address using physical block order first.
        /// </summary>
        public int CompareTo(AtlasFieldAddress other)
        {
            var blockComparison = BlockIndex.CompareTo(other.BlockIndex);
            if (blockComparison != 0)
            {
                return blockComparison;
            }

            var offsetComparison = ByteOffset.CompareTo(other.ByteOffset);
            if (offsetComparison != 0)
            {
                return offsetComparison;
            }

            var slotComparison = Slot.CompareTo(other.Slot);
            if (slotComparison != 0)
            {
                return slotComparison;
            }

            return CapacityEndByteOffsetExclusive.CompareTo(other.CapacityEndByteOffsetExclusive);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _bindingState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Slot.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ StorageFormat.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ BlockIndex;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteOffset);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Length;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Capacity;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteLength);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteCapacity);
                return hash;
            }
        }

        /// <summary>
        /// Returns an invariant diagnostic representation.
        /// </summary>
        public override string ToString()
        {
            if (IsUnbound)
            {
                return "AtlasFieldAddress(Unbound)";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasFieldAddress(Slot={0}, Storage={1}, Block={2}, Offset={3}, Length={4}, Capacity={5}, Bytes={6}/{7})",
                Slot,
                StorageKind,
                BlockIndex,
                ByteOffset,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity);
        }

        /// <summary>
        /// Determines whether two addresses are equal.
        /// </summary>
        public static bool operator ==(
            AtlasFieldAddress left,
            AtlasFieldAddress right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two addresses are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasFieldAddress left,
            AtlasFieldAddress right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left address sorts before the right address.
        /// </summary>
        public static bool operator <(
            AtlasFieldAddress left,
            AtlasFieldAddress right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left address sorts after the right address.
        /// </summary>
        public static bool operator >(
            AtlasFieldAddress left,
            AtlasFieldAddress right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left address sorts before or equal to the right address.
        /// </summary>
        public static bool operator <=(
            AtlasFieldAddress left,
            AtlasFieldAddress right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left address sorts after or equal to the right address.
        /// </summary>
        public static bool operator >=(
            AtlasFieldAddress left,
            AtlasFieldAddress right)
        {
            return left.CompareTo(right) >= 0;
        }

        private static void ValidateInputsOrThrow(
            AtlasFieldSlot slot,
            StorageFormat storageFormat,
            int blockIndex,
            long byteOffset,
            int length,
            int capacity,
            string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasFieldAddress);

            slot.ValidateOrThrow(name);
            storageFormat.ValidateOrThrow(name);

            if (!IsFixedContiguousStorageKind(storageFormat.Kind))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas field addresses currently support only {0} and {1} storage. Storage kind '{2}' requires a dedicated physical binding model.",
                        StorageKind.Scalar,
                        StorageKind.NativeArray,
                        storageFormat.Kind));
            }

            if (blockIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockIndex),
                    blockIndex,
                    "Workspace storage block index must be greater than or equal to zero.");
            }

            if (byteOffset < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(byteOffset),
                    byteOffset,
                    "Workspace byte offset must be greater than or equal to zero.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Field address length must be greater than or equal to zero.");
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Field address capacity must be greater than or equal to zero.");
            }

            if (capacity < length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Field address capacity {0} must be greater than or equal to logical length {1}.",
                        capacity,
                        length),
                    name);
            }

            if (storageFormat.Kind == StorageKind.Scalar &&
                (length != 1 || capacity != 1))
            {
                throw new ArgumentException(
                    "Scalar field addresses must have length 1 and capacity 1.",
                    name);
            }


            if (!IsAligned(byteOffset, storageFormat.ElementAlignment))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Field address byte offset {0} is not aligned to element alignment {1}.",
                        byteOffset,
                        storageFormat.ElementAlignment),
                    name);
            }

            _ = ComputeByteCount(storageFormat, length);
            _ = ComputeByteCount(storageFormat, capacity);
            _ = checked(byteOffset + ComputeByteCount(storageFormat, capacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFixedContiguousStorageKind(StorageKind storageKind)
        {
            return storageKind == StorageKind.Scalar ||
                   storageKind == StorageKind.NativeArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAligned(
            long byteOffset,
            int alignment)
        {
            return alignment <= 1 ||
                   byteOffset % alignment == 0L;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ComputeByteCount(
            StorageFormat storageFormat,
            int elementCount)
        {
            return checked((long)storageFormat.ElementSize * elementCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FoldLong(long value)
        {
            unchecked
            {
                var bits = (ulong)value;
                return (int)(bits ^ (bits >> 32));
            }
        }
    }
}