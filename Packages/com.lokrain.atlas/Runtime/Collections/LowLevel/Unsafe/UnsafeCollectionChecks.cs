#nullable enable

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Provides always-on structural guards for internal unsafe collection infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These checks protect pointer access, state transitions, allocation boundaries, and release-build
    /// memory safety. They must not be gated behind <c>ENABLE_UNITY_COLLECTIONS_CHECKS</c>.
    /// </para>
    /// <para>
    /// Keep these messages simple. Rich diagnostics belong in editor-only or test-only validators, not
    /// hot unsafe paths.
    /// </para>
    /// </remarks>
    internal static class UnsafeCollectionChecks
    {
        /// <summary>
        /// Validates that an unsafe collection is created.
        /// </summary>
        /// <param name="isCreated">Whether the collection references created storage.</param>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the collection is not created.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckCreated(bool isCreated, string typeName)
        {
            if (!isCreated)
            {
                throw new InvalidOperationException(typeName + " is not created.");
            }
        }

        /// <summary>
        /// Validates that an allocator can own unmanaged collection storage.
        /// </summary>
        /// <param name="allocator">The allocator to validate.</param>
        /// <param name="parameterName">The allocator parameter name.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the allocator is invalid for allocation.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckAllocator(Allocator allocator, string parameterName)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException(
                    "Allocator must be Temp, TempJob, Persistent, or another valid allocator.",
                    parameterName);
            }
        }

        /// <summary>
        /// Validates that a constructor capacity argument is greater than zero.
        /// </summary>
        /// <param name="capacity">The capacity argument.</param>
        /// <param name="parameterName">The capacity parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the capacity is less than or equal to zero.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPositiveArgument(int capacity, string parameterName)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    capacity,
                    "Value must be greater than zero.");
            }
        }

        /// <summary>
        /// Validates that a constructor count or length argument is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckNonNegativeArgument(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be greater than or equal to zero.");
            }
        }

        /// <summary>
        /// Validates that an internal capacity field is greater than zero.
        /// </summary>
        /// <param name="capacity">The capacity field.</param>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the capacity field is structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPositiveCapacity(int capacity, string typeName)
        {
            if (capacity <= 0)
            {
                throw new InvalidOperationException(typeName + " capacity is structurally invalid.");
            }
        }

        /// <summary>
        /// Validates that an internal count field is inside the inclusive range [0, capacity].
        /// </summary>
        /// <param name="count">The count field.</param>
        /// <param name="capacity">The capacity field.</param>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the count field is structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckCountInCapacity(int count, int capacity, string typeName)
        {
            CheckPositiveCapacity(capacity, typeName);

            if (count < 0 || count > capacity)
            {
                throw new InvalidOperationException(typeName + " count is outside capacity.");
            }
        }

        /// <summary>
        /// Validates that at least one additional element can be written.
        /// </summary>
        /// <param name="count">The current count field.</param>
        /// <param name="capacity">The capacity field.</param>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the collection is full or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckCapacityAvailable(int count, int capacity, string typeName)
        {
            CheckCountInCapacity(count, capacity, typeName);

            if (count >= capacity)
            {
                throw new InvalidOperationException(typeName + " capacity exceeded.");
            }
        }

        /// <summary>
        /// Validates that an index is inside the range [0, length).
        /// </summary>
        /// <param name="index">The index value.</param>
        /// <param name="length">The length value.</param>
        /// <param name="parameterName">The index parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the index is outside the valid range.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckIndexInRange(int index, int length, string parameterName)
        {
            if ((uint)index >= (uint)length)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    index,
                    "Index is outside the valid range.");
            }
        }

        /// <summary>
        /// Validates that a grid cell index is greater than or equal to zero.
        /// </summary>
        /// <param name="cellIndex">The cell index value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the cell index is negative.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckNonNegativeCellIndex(int cellIndex)
        {
            if (cellIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellIndex),
                    cellIndex,
                    "Cell index must be greater than or equal to zero.");
            }
        }

        /// <summary>
        /// Validates that a value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="parameterName">The value parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be greater than or equal to zero.");
            }
        }

        /// <summary>
        /// Validates that a value is greater than zero.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="parameterName">The value parameter name.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is less than or equal to zero.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be greater than zero.");
            }
        }

        /// <summary>
        /// Validates that a phase value matches the expected phase value.
        /// </summary>
        /// <param name="actualPhase">The actual phase value.</param>
        /// <param name="expectedPhase">The expected phase value.</param>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the phase is not valid for the requested operation.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPhase(int actualPhase, int expectedPhase, string typeName)
        {
            if (actualPhase != expectedPhase)
            {
                throw new InvalidOperationException(typeName + " is in an invalid phase for this operation.");
            }
        }

        /// <summary>
        /// Validates that a priority preserves a monotonic priority invariant.
        /// </summary>
        /// <param name="priority">The priority being inserted.</param>
        /// <param name="currentPriority">The current monotonic priority cursor.</param>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the priority is lower than the current monotonic priority cursor.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckMonotonicPriority(int priority, int currentPriority, string typeName)
        {
            if (priority < currentPriority)
            {
                throw new InvalidOperationException(typeName + " priority violates the monotonic queue invariant.");
            }
        }

        /// <summary>
        /// Validates that a partition count is greater than zero.
        /// </summary>
        /// <param name="partitionCount">The partition count.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the partition count is less than or equal to zero.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPartitionCount(int partitionCount)
        {
            if (partitionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partitionCount),
                    partitionCount,
                    "Partition count must be greater than zero.");
            }
        }

        /// <summary>
        /// Validates that a partition index is inside the range [0, partitionCount).
        /// </summary>
        /// <param name="partitionIndex">The partition index.</param>
        /// <param name="partitionCount">The partition count.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the partition index is outside the valid range.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPartitionIndex(int partitionIndex, int partitionCount)
        {
            CheckPartitionCount(partitionCount);

            if ((uint)partitionIndex >= (uint)partitionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partitionIndex),
                    partitionIndex,
                    "Partition index is outside the valid range.");
            }
        }

        /// <summary>
        /// Throws when structural corruption would otherwise lead to invalid pointer access.
        /// </summary>
        /// <param name="typeName">The collection type name used in the exception message.</param>
        /// <exception cref="InvalidOperationException">
        /// Always thrown.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowCorruptState(string typeName)
        {
            throw new InvalidOperationException(typeName + " structural state is corrupt.");
        }
    }
}