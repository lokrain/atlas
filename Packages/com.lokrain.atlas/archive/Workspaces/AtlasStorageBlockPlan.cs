// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasStorageBlockPlan.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Describe one contiguous unmanaged byte-block allocation required by an Atlas workspace layout.
// - Separate workspace allocation facts from semantic shape-resolution facts.
// - Preserve deterministic block ordering, byte capacity, alignment, and address count.
// - Give AtlasWorkspace a stable allocation recipe without exposing Contracts or resolved shapes.
//
// Design notes
// - This is memory-layout metadata, not allocated memory.
// - This type does not own native containers.
// - This type does not allocate or dispose memory.
// - This type does not know Field Contracts, Operation Contracts, schedulers, jobs, or artifacts.
// - default(AtlasStorageBlockPlan) is unplanned for Burst/unmanaged compatibility.
// - Planned block index zero, byte offset zero, byte length zero, and byte capacity zero are valid.
// - Missing/unplanned state is represented by an explicit planning-state byte, not sentinel ids.
// - Required alignment is the required base/offset alignment for the block allocation.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Allocation plan for one contiguous workspace-owned unmanaged byte block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasStorageBlockPlan"/> is produced by workspace-layout compilation. It tells
    /// the workspace how many bytes a physical block requires, what alignment the allocation must
    /// satisfy, and how many field addresses are mapped into that block.
    /// </para>
    ///
    /// <para>
    /// This value is deliberately lower-level than resolved shape metadata. It does not carry
    /// contracts, stable ids, field slots, shape domains, length shapes, operation definitions,
    /// scheduler bindings, or managed diagnostic names.
    /// </para>
    ///
    /// <para>
    /// The default value is allowed only as an unplanned unmanaged payload. Compiler and workspace
    /// boundaries must call <see cref="ValidatePlannedOrThrow"/> before treating a value as an
    /// allocation instruction.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasStorageBlockPlan :
        IEquatable<AtlasStorageBlockPlan>,
        IComparable<AtlasStorageBlockPlan>
    {
        private const byte UnplannedState = 0;
        private const byte PlannedState = 1;

        private readonly byte _planningState;

        /// <summary>
        /// Zero-based physical block index inside the workspace layout.
        /// </summary>
        /// <remarks>
        /// Block index zero is valid. Absence is represented by <see cref="IsUnplanned"/>, not by
        /// a negative index.
        /// </remarks>
        public readonly int BlockIndex;

        /// <summary>
        /// Highest occupied byte end within the block before optional tail padding.
        /// </summary>
        /// <remarks>
        /// This is the byte extent actually covered by field address ranges. It may be lower than
        /// <see cref="ByteCapacity"/> when the compiler rounds the final allocation up to preserve
        /// block-level alignment.
        /// </remarks>
        public readonly long UsedByteLength;

        /// <summary>
        /// Total byte capacity that the workspace must allocate for this block.
        /// </summary>
        /// <remarks>
        /// This value is suitable for native byte-buffer allocation after converting through
        /// <see cref="ByteCapacityInt32"/>.
        /// </remarks>
        public readonly long ByteCapacity;

        /// <summary>
        /// Required byte alignment for the allocation base and planned field offsets.
        /// </summary>
        /// <remarks>
        /// This is the maximum alignment required by fields packed into this block. It must be a
        /// positive power of two.
        /// </remarks>
        public readonly int RequiredAlignment;

        /// <summary>
        /// Number of field addresses mapped into this storage block.
        /// </summary>
        /// <remarks>
        /// Zero-capacity fields still count as field addresses when they are mapped into the
        /// layout. A planned block must contain at least one mapped address.
        /// </remarks>
        public readonly int FieldAddressCount;

        private AtlasStorageBlockPlan(
            int blockIndex,
            long usedByteLength,
            long byteCapacity,
            int requiredAlignment,
            int fieldAddressCount)
        {
            BlockIndex = blockIndex;
            UsedByteLength = usedByteLength;
            ByteCapacity = byteCapacity;
            RequiredAlignment = requiredAlignment;
            FieldAddressCount = fieldAddressCount;
            _planningState = PlannedState;
        }

        /// <summary>
        /// Gets whether this value contains a planned storage block.
        /// </summary>
        public bool IsPlanned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _planningState == PlannedState;
        }

        /// <summary>
        /// Gets whether this value is the default unplanned payload.
        /// </summary>
        public bool IsUnplanned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _planningState != PlannedState;
        }

        /// <summary>
        /// Gets whether this block requires a non-zero allocation.
        /// </summary>
        public bool RequiresMemory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPlanned && ByteCapacity > 0L;
        }

        /// <summary>
        /// Gets whether this block contains at least one mapped field address.
        /// </summary>
        public bool HasFieldAddresses
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPlanned && FieldAddressCount > 0;
        }

        /// <summary>
        /// Gets the block byte capacity as a native-container-compatible <see cref="int"/>.
        /// </summary>
        /// <exception cref="OverflowException">
        /// Thrown when the block byte capacity exceeds native container length limits.
        /// </exception>
        public int ByteCapacityInt32
        {
            get
            {
                ValidatePlannedOrThrow();

                if (ByteCapacity > int.MaxValue)
                {
                    throw new OverflowException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas storage block {0} byte capacity {1} exceeds Int32 native container length capacity.",
                            BlockIndex,
                            ByteCapacity));
                }

                return checked((int)ByteCapacity);
            }
        }

        /// <summary>
        /// Gets the unused tail padding bytes reserved by this block.
        /// </summary>
        public long TailPaddingByteCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPlanned ? ByteCapacity - UsedByteLength : 0L;
        }

        /// <summary>
        /// Creates a planned storage block.
        /// </summary>
        public static AtlasStorageBlockPlan Create(
            int blockIndex,
            long usedByteLength,
            long byteCapacity,
            int requiredAlignment,
            int fieldAddressCount)
        {
            ValidateInputsOrThrow(
                blockIndex,
                usedByteLength,
                byteCapacity,
                requiredAlignment,
                fieldAddressCount);

            return new AtlasStorageBlockPlan(
                blockIndex,
                usedByteLength,
                byteCapacity,
                requiredAlignment,
                fieldAddressCount);
        }

        /// <summary>
        /// Creates a planned storage block whose capacity is the aligned used byte length.
        /// </summary>
        public static AtlasStorageBlockPlan CreateAligned(
            int blockIndex,
            long usedByteLength,
            int requiredAlignment,
            int fieldAddressCount)
        {
            var byteCapacity = AlignUpChecked(
                usedByteLength,
                requiredAlignment);

            return Create(
                blockIndex,
                usedByteLength,
                byteCapacity,
                requiredAlignment,
                fieldAddressCount);
        }

        /// <summary>
        /// Returns a copy with a different field-address count.
        /// </summary>
        public AtlasStorageBlockPlan WithFieldAddressCount(int fieldAddressCount)
        {
            ValidatePlannedOrThrow();

            return Create(
                BlockIndex,
                UsedByteLength,
                ByteCapacity,
                RequiredAlignment,
                fieldAddressCount);
        }

        /// <summary>
        /// Returns a copy with a different used byte length and aligned byte capacity.
        /// </summary>
        public AtlasStorageBlockPlan WithAlignedUsedByteLength(long usedByteLength)
        {
            ValidatePlannedOrThrow();

            return CreateAligned(
                BlockIndex,
                usedByteLength,
                RequiredAlignment,
                FieldAddressCount);
        }

        /// <summary>
        /// Returns a copy with a different byte capacity.
        /// </summary>
        public AtlasStorageBlockPlan WithByteCapacity(long byteCapacity)
        {
            ValidatePlannedOrThrow();

            return Create(
                BlockIndex,
                UsedByteLength,
                byteCapacity,
                RequiredAlignment,
                FieldAddressCount);
        }

        /// <summary>
        /// Returns whether a block-relative byte range is inside this block capacity.
        /// </summary>
        public bool ContainsByteRange(
            long byteOffset,
            long byteCount)
        {
            if (IsUnplanned ||
                byteOffset < 0L ||
                byteCount < 0L ||
                byteOffset > long.MaxValue - byteCount)
            {
                return false;
            }

            return byteOffset + byteCount <= ByteCapacity;
        }

        /// <summary>
        /// Returns whether a block-relative byte offset satisfies this block's required alignment.
        /// </summary>
        public bool IsOffsetAligned(long byteOffset)
        {
            return IsPlanned &&
                   byteOffset >= 0L &&
                   IsAligned(byteOffset, RequiredAlignment);
        }

        /// <summary>
        /// Aligns a byte offset to this block's required alignment.
        /// </summary>
        public long AlignOffsetChecked(long byteOffset)
        {
            ValidatePlannedOrThrow();

            if (byteOffset < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(byteOffset),
                    byteOffset,
                    "Byte offset must be greater than or equal to zero.");
            }

            return AlignUpChecked(
                byteOffset,
                RequiredAlignment);
        }

        /// <summary>
        /// Validates that this value is a planned, internally consistent storage block.
        /// </summary>
        public void ValidatePlannedOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasStorageBlockPlan);

            if (IsUnplanned)
            {
                throw new ArgumentException(
                    "Atlas storage block plan is unplanned.",
                    name);
            }

            ValidateInputsOrThrow(
                BlockIndex,
                UsedByteLength,
                ByteCapacity,
                RequiredAlignment,
                FieldAddressCount,
                name);
        }

        /// <summary>
        /// Determines whether this block plan equals another block plan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AtlasStorageBlockPlan other)
        {
            return _planningState == other._planningState &&
                   BlockIndex == other.BlockIndex &&
                   UsedByteLength == other.UsedByteLength &&
                   ByteCapacity == other.ByteCapacity &&
                   RequiredAlignment == other.RequiredAlignment &&
                   FieldAddressCount == other.FieldAddressCount;
        }

        /// <summary>
        /// Determines whether this block plan equals an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasStorageBlockPlan other &&
                   Equals(other);
        }

        /// <summary>
        /// Compares this block plan with another block plan by physical block index.
        /// </summary>
        public int CompareTo(AtlasStorageBlockPlan other)
        {
            var blockComparison = BlockIndex.CompareTo(other.BlockIndex);
            if (blockComparison != 0)
            {
                return blockComparison;
            }

            var capacityComparison = ByteCapacity.CompareTo(other.ByteCapacity);
            if (capacityComparison != 0)
            {
                return capacityComparison;
            }

            return UsedByteLength.CompareTo(other.UsedByteLength);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _planningState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ BlockIndex;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(UsedByteLength);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteCapacity);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ RequiredAlignment;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FieldAddressCount;
                return hash;
            }
        }

        /// <summary>
        /// Returns an invariant diagnostic representation.
        /// </summary>
        public override string ToString()
        {
            if (IsUnplanned)
            {
                return "AtlasStorageBlockPlan(Unplanned)";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasStorageBlockPlan(Block={0}, UsedBytes={1}, CapacityBytes={2}, Alignment={3}, FieldAddresses={4})",
                BlockIndex,
                UsedByteLength,
                ByteCapacity,
                RequiredAlignment,
                FieldAddressCount);
        }

        /// <summary>
        /// Determines whether two block plans are equal.
        /// </summary>
        public static bool operator ==(
            AtlasStorageBlockPlan left,
            AtlasStorageBlockPlan right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two block plans are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasStorageBlockPlan left,
            AtlasStorageBlockPlan right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left block plan sorts before the right block plan.
        /// </summary>
        public static bool operator <(
            AtlasStorageBlockPlan left,
            AtlasStorageBlockPlan right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left block plan sorts after the right block plan.
        /// </summary>
        public static bool operator >(
            AtlasStorageBlockPlan left,
            AtlasStorageBlockPlan right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left block plan sorts before or equal to the right block plan.
        /// </summary>
        public static bool operator <=(
            AtlasStorageBlockPlan left,
            AtlasStorageBlockPlan right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left block plan sorts after or equal to the right block plan.
        /// </summary>
        public static bool operator >=(
            AtlasStorageBlockPlan left,
            AtlasStorageBlockPlan right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Aligns a non-negative byte value upward to the supplied power-of-two alignment.
        /// </summary>
        public static long AlignUpChecked(
            long value,
            int alignment)
        {
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Value must be greater than or equal to zero.");
            }

            ValidateAlignmentOrThrow(alignment);

            var remainder = value % alignment;
            if (remainder == 0L)
            {
                return value;
            }

            return checked(value + (alignment - remainder));
        }

        private static void ValidateInputsOrThrow(
            int blockIndex,
            long usedByteLength,
            long byteCapacity,
            int requiredAlignment,
            int fieldAddressCount,
            string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasStorageBlockPlan);

            if (blockIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockIndex),
                    blockIndex,
                    "Storage block index must be greater than or equal to zero.");
            }

            if (usedByteLength < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(usedByteLength),
                    usedByteLength,
                    "Used byte length must be greater than or equal to zero.");
            }

            if (byteCapacity < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(byteCapacity),
                    byteCapacity,
                    "Byte capacity must be greater than or equal to zero.");
            }

            if (byteCapacity < usedByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Storage block byte capacity {0} must be greater than or equal to used byte length {1}.",
                        byteCapacity,
                        usedByteLength),
                    name);
            }

            if (byteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Storage block byte capacity {0} exceeds Int32 native container length capacity.",
                        byteCapacity));
            }

            ValidateAlignmentOrThrow(requiredAlignment);

            if (!IsAligned(byteCapacity, requiredAlignment))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Storage block byte capacity {0} must be aligned to required alignment {1}.",
                        byteCapacity,
                        requiredAlignment),
                    name);
            }

            if (fieldAddressCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldAddressCount),
                    fieldAddressCount,
                    "A planned storage block must contain at least one field address.");
            }
        }

        private static void ValidateAlignmentOrThrow(int alignment)
        {
            if (alignment <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(alignment),
                    alignment,
                    "Alignment must be greater than zero.");
            }

            if (!IsPowerOfTwo(alignment))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Alignment {0} must be a power of two.",
                        alignment),
                    nameof(alignment));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAligned(
            long value,
            int alignment)
        {
            return value % alignment == 0L;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 &&
                   (value & (value - 1)) == 0;
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