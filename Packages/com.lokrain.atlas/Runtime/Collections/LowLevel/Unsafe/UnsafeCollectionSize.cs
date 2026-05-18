#nullable enable

// Packages/com.lokrain.atlas/Runtime/Collections/LowLevel/Unsafe/UnsafeCollectionSize.cs
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Collections.LowLevel.Unsafe
//
// Purpose:
// Provides checked integer and allocation-size math for internal unsafe collection infrastructure.
//
// Design notes:
// - This file is internal low-level infrastructure, not public Atlas API.
// - Unsafe containers must prove allocation sizes and index ranges before pointer access.
// - Use long intermediates for multiplication/addition, then cast to int only after bounds are proven.
// - Hot-path callers should prefer already-validated sizes. Construction and resize paths should use this type.

using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Checked integer and allocation-size helpers for internal unsafe collection infrastructure.
    /// </summary>
    internal static class UnsafeCollectionSize
    {
        /// <summary>
        /// Validates that a count is greater than or equal to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
            }
        }

        /// <summary>
        /// Validates that a count is greater than zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }
        }

        /// <summary>
        /// Returns a checked element byte count for <typeparamref name="TElement"/>.
        /// Zero elements are allowed and return zero bytes.
        /// </summary>
        public static long CheckedElementBytes<TElement>(int count, string parameterName)
            where TElement : unmanaged
        {
            CheckNonNegative(count, parameterName);

            var elementSize = UnsafeUtility.SizeOf<TElement>();
            var byteCount = (long)elementSize * count;

            if (byteCount < 0L)
            {
                throw new ArgumentOutOfRangeException(parameterName, count, "Element byte count overflowed.");
            }

            return byteCount;
        }

        /// <summary>
        /// Returns a checked positive element byte count for <typeparamref name="TElement"/>.
        /// </summary>
        public static long CheckedPositiveElementBytes<TElement>(int count, string parameterName)
            where TElement : unmanaged
        {
            CheckPositive(count, parameterName);
            return CheckedElementBytes<TElement>(count, parameterName);
        }

        /// <summary>
        /// Returns a checked grid logical length for positive dimensions.
        /// </summary>
        public static int CheckedGridLength(int width, int height)
        {
            CheckPositive(width, nameof(width));
            CheckPositive(height, nameof(height));

            var length = (long)width * height;
            return CheckedIntLength(length, "Grid length overflowed.");
        }

        /// <summary>
        /// Returns a checked row-major backing length for positive dimensions and stride.
        /// </summary>
        public static int CheckedBackingLength(int width, int height, int stride)
        {
            CheckPositive(width, nameof(width));
            CheckPositive(height, nameof(height));

            if (stride < width)
            {
                throw new ArgumentOutOfRangeException(nameof(stride), stride, "Stride must be greater than or equal to width.");
            }

            var backingLength = ((long)height - 1L) * stride + width;
            return CheckedIntLength(backingLength, "Backing length overflowed.");
        }

        /// <summary>
        /// Returns the checked number of 64-bit words required to store <paramref name="bitLength"/> bits.
        /// Zero bits are allowed and return zero words.
        /// </summary>
        public static int CheckedWordLength(int bitLength)
        {
            CheckNonNegative(bitLength, nameof(bitLength));

            var wordLength = ((long)bitLength + 63L) >> 6;
            return CheckedIntLength(wordLength, "Word length overflowed.");
        }

        /// <summary>
        /// Returns the checked offset buffer length for an owner count.
        /// </summary>
        public static int CheckedOffsetLength(int ownerCount)
        {
            CheckNonNegative(ownerCount, nameof(ownerCount));
            return CheckedAdd(ownerCount, 1, "Offset length overflowed.");
        }

        /// <summary>
        /// Returns a checked integer sum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CheckedAdd(int left, int right, string message)
        {
            var result = (long)left + right;
            return CheckedIntLength(result, message);
        }

        /// <summary>
        /// Returns a checked integer sum of <paramref name="value"/> and one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CheckedAddOne(int value, string parameterName)
        {
            if (value == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value plus one overflowed.");
            }

            return value + 1;
        }

        /// <summary>
        /// Returns a checked integer product.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CheckedMultiply(int left, int right, string message)
        {
            var result = (long)left * right;
            return CheckedIntLength(result, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CheckedIntLength(long value, string message)
        {
            if (value < 0L || value > int.MaxValue)
            {
                throw new InvalidOperationException(message);
            }

            return (int)value;
        }
    }
}
