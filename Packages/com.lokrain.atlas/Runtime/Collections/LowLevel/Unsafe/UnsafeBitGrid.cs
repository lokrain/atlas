#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Internal unsafe compact linear bit storage backed by unmanaged 64-bit words.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type owns unmanaged memory directly. It is deliberately not marked as a Unity
    /// <c>NativeContainer</c>. A separate native wrapper or scheduler-owned scratch lease must own Unity safety handles,
    /// job dependencies, and dependency-aware disposal before this crosses Unity job boundaries as an owning container.
    /// </para>
    /// <para>
    /// This type is intended for dense masks such as visited, dirty, land, water, coastline, frontier,
    /// and diagnostic flags.
    /// </para>
    /// <para>
    /// Arbitrary parallel bit writes are unsafe because two workers can update different bits in the same
    /// underlying word. Parallel mutation must partition work by word range.
    /// </para>
    /// <para>
    /// This is a single-writer unsafe core. Direct <see cref="Set(int)"/>, <see cref="Clear(int)"/>,
    /// and <see cref="Write(int, bool)"/> calls are for single-writer algorithms.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("BitLength = {" + nameof(BitLength) + "}, WordLength = {" + nameof(WordLength) + "}")]
    internal unsafe struct UnsafeBitGrid : IDisposable
    {
        private const int BitsPerWord = 64;
        private const int WordShift = 6;
        private const int WordBitMask = BitsPerWord - 1;
        private const string TypeName = nameof(UnsafeBitGrid);

        [NativeDisableUnsafePtrRestriction]
        private Header* m_Header;

        [NativeDisableUnsafePtrRestriction]
        private ulong* m_Words;

        private Allocator m_AllocatorLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeBitGrid"/> struct with all bits cleared.
        /// </summary>
        /// <param name="bitLength">The number of valid bits. Zero is valid.</param>
        /// <param name="allocator">The allocator used for unmanaged storage.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitLength"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is not a valid allocation allocator.
        /// </exception>
        public UnsafeBitGrid(int bitLength, Allocator allocator)
        {
            ValidateConstructorArguments(bitLength, allocator);

            m_Header = null;
            m_Words = null;
            m_AllocatorLabel = allocator;

            int wordLength = UnsafeCollectionSize.CheckedWordLength(bitLength);

            try
            {
                m_Header = (Header*)UnsafeUtility.MallocTracked(
                    UnsafeUtility.SizeOf<Header>(),
                    UnsafeUtility.AlignOf<Header>(),
                    allocator,
                    1);

                if (wordLength > 0)
                {
                    m_Words = (ulong*)UnsafeUtility.MallocTracked(
                        UnsafeCollectionSize.CheckedPositiveElementBytes<ulong>(wordLength, nameof(wordLength)),
                        UnsafeUtility.AlignOf<ulong>(),
                        allocator,
                        1);
                }

                m_Header->BitLength = bitLength;
                m_Header->WordLength = wordLength;

                ClearAll();
            }
            catch
            {
                FreeAllocatedMemory();

                m_Header = null;
                m_Words = null;
                m_AllocatorLabel = Allocator.Invalid;

                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this value references allocated bit-grid storage.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Header != null;
        }

        /// <summary>
        /// Gets the number of valid bits.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public int BitLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->BitLength;
            }
        }

        /// <summary>
        /// Gets the number of backing 64-bit words.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public int WordLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->WordLength;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this bit grid contains no valid bits.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->BitLength == 0;
            }
        }

        /// <summary>
        /// Determines whether the supplied bit index is inside the valid bit range.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <returns><see langword="true"/> when the bit index is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsBit(int bitIndex)
        {
            CheckCreated();
            CheckHeaderState();
            return (uint)bitIndex < (uint)m_Header->BitLength;
        }

        /// <summary>
        /// Determines whether the supplied word index is inside the backing word range.
        /// </summary>
        /// <param name="wordIndex">The word index.</param>
        /// <returns><see langword="true"/> when the word index is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsWord(int wordIndex)
        {
            CheckCreated();
            CheckHeaderState();
            return (uint)wordIndex < (uint)m_Header->WordLength;
        }

        /// <summary>
        /// Clears all valid bits.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public void ClearAll()
        {
            CheckCreated();
            CheckHeaderState();

            int wordLength = m_Header->WordLength;

            if (wordLength == 0)
            {
                return;
            }

            UnsafeUtility.MemClear(
                m_Words,
                UnsafeCollectionSize.CheckedPositiveElementBytes<ulong>(wordLength, nameof(wordLength)));
        }

        /// <summary>
        /// Sets all valid bits to one.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public void SetAll()
        {
            CheckCreated();
            CheckHeaderState();

            int wordLength = m_Header->WordLength;

            for (int index = 0; index < wordLength; index++)
            {
                m_Words[index] = ulong.MaxValue;
            }

            MaskUnusedBitsInLastWord();
        }

        /// <summary>
        /// Reads one bit.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <returns><see langword="true"/> when the bit is set; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the valid bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int bitIndex)
        {
            CheckCreated();
            CheckHeaderState();
            CheckBitIndex(bitIndex, m_Header->BitLength);

            int wordIndex = bitIndex >> WordShift;
            ulong mask = 1UL << (bitIndex & WordBitMask);

            return (m_Words[wordIndex] & mask) != 0UL;
        }

        /// <summary>
        /// Sets one bit.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the valid bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int bitIndex)
        {
            CheckCreated();
            CheckHeaderState();
            CheckBitIndex(bitIndex, m_Header->BitLength);

            int wordIndex = bitIndex >> WordShift;
            ulong mask = 1UL << (bitIndex & WordBitMask);

            m_Words[wordIndex] |= mask;
        }

        /// <summary>
        /// Clears one bit.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the valid bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int bitIndex)
        {
            CheckCreated();
            CheckHeaderState();
            CheckBitIndex(bitIndex, m_Header->BitLength);

            int wordIndex = bitIndex >> WordShift;
            ulong mask = 1UL << (bitIndex & WordBitMask);

            m_Words[wordIndex] &= ~mask;
        }

        /// <summary>
        /// Writes one bit.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <param name="value">The bit value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the valid bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int bitIndex, bool value)
        {
            if (value)
            {
                Set(bitIndex);
            }
            else
            {
                Clear(bitIndex);
            }
        }

        /// <summary>
        /// Reads one absolute backing word.
        /// </summary>
        /// <param name="wordIndex">The absolute word index.</param>
        /// <returns>The word value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="wordIndex"/> is outside the valid word range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadWord(int wordIndex)
        {
            CheckCreated();
            CheckHeaderState();
            CheckWordIndex(wordIndex, m_Header->WordLength);

            return m_Words[wordIndex];
        }

        /// <summary>
        /// Writes one absolute backing word.
        /// </summary>
        /// <remarks>
        /// When writing the last word, unused high bits are masked out.
        /// </remarks>
        /// <param name="wordIndex">The absolute word index.</param>
        /// <param name="value">The word value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="wordIndex"/> is outside the valid word range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteWord(int wordIndex, ulong value)
        {
            CheckCreated();
            CheckHeaderState();
            CheckWordIndex(wordIndex, m_Header->WordLength);

            if (wordIndex == m_Header->WordLength - 1)
            {
                value &= GetLastWordMask(m_Header->BitLength);
            }

            m_Words[wordIndex] = value;
        }

        /// <summary>
        /// Clears a contiguous absolute word range.
        /// </summary>
        /// <param name="wordStart">The inclusive word start.</param>
        /// <param name="wordEnd">The exclusive word end.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the word range is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public void ClearWordRange(int wordStart, int wordEnd)
        {
            CheckCreated();
            CheckHeaderState();
            CheckWordRange(wordStart, wordEnd, m_Header->WordLength);

            for (int wordIndex = wordStart; wordIndex < wordEnd; wordIndex++)
            {
                m_Words[wordIndex] = 0UL;
            }
        }

        /// <summary>
        /// Sets a contiguous absolute word range.
        /// </summary>
        /// <param name="wordStart">The inclusive word start.</param>
        /// <param name="wordEnd">The exclusive word end.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the word range is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public void SetWordRange(int wordStart, int wordEnd)
        {
            CheckCreated();
            CheckHeaderState();
            CheckWordRange(wordStart, wordEnd, m_Header->WordLength);

            for (int wordIndex = wordStart; wordIndex < wordEnd; wordIndex++)
            {
                m_Words[wordIndex] = ulong.MaxValue;
            }

            if (wordEnd == m_Header->WordLength)
            {
                MaskUnusedBitsInLastWord();
            }
        }

        /// <summary>
        /// Gets a non-owning word-range view.
        /// </summary>
        /// <param name="wordStart">The inclusive absolute word start.</param>
        /// <param name="wordEnd">The exclusive absolute word end.</param>
        /// <returns>A word-range view.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the word range is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public UnsafeBitGridWordRange GetWordRange(int wordStart, int wordEnd)
        {
            CheckCreated();
            CheckHeaderState();
            CheckWordRange(wordStart, wordEnd, m_Header->WordLength);

            return new UnsafeBitGridWordRange(
                m_Words,
                m_Header->BitLength,
                m_Header->WordLength,
                wordStart,
                wordEnd,
                isCreated: true);
        }

        /// <summary>
        /// Gets a deterministic word partition for a worker index.
        /// </summary>
        /// <param name="partitionIndex">The partition index in the range [0, partitionCount).</param>
        /// <param name="partitionCount">The number of partitions.</param>
        /// <returns>The word-range view assigned to the partition.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="partitionCount"/> or <paramref name="partitionIndex"/> is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the bit grid is uncreated or structurally invalid.
        /// </exception>
        public UnsafeBitGridWordRange GetWordPartition(int partitionIndex, int partitionCount)
        {
            CheckCreated();
            CheckHeaderState();
            UnsafeCollectionChecks.CheckPartitionIndex(partitionIndex, partitionCount);

            int wordLength = m_Header->WordLength;
            int wordStart = (int)(((long)wordLength * partitionIndex) / partitionCount);
            int wordEnd = (int)(((long)wordLength * (partitionIndex + 1)) / partitionCount);

            return new UnsafeBitGridWordRange(
                m_Words,
                m_Header->BitLength,
                wordLength,
                wordStart,
                wordEnd,
                isCreated: true);
        }

        /// <summary>
        /// Releases all unmanaged memory owned by this bit grid value.
        /// </summary>
        /// <remarks>
        /// Because this is an unsafe core, disposing one value does not invalidate copied values.
        /// A native wrapper or scheduler-owned scratch lease is responsible for Unity safety-handle invalidation
        /// and job dependency ownership.
        /// </remarks>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            FreeAllocatedMemory();

            m_Header = null;
            m_Words = null;
            m_AllocatorLabel = Allocator.Invalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MaskUnusedBitsInLastWord()
        {
            int wordLength = m_Header->WordLength;

            if (wordLength == 0)
            {
                return;
            }

            m_Words[wordLength - 1] &= GetLastWordMask(m_Header->BitLength);
        }

        private void FreeAllocatedMemory()
        {
            if (m_Words != null)
            {
                UnsafeUtility.FreeTracked(m_Words, m_AllocatorLabel);
            }

            if (m_Header != null)
            {
                UnsafeUtility.FreeTracked(m_Header, m_AllocatorLabel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetLastWordMask(int bitLength)
        {
            if (bitLength == 0)
            {
                return 0UL;
            }

            int usedBitsInLastWord = bitLength & WordBitMask;

            if (usedBitsInLastWord == 0)
            {
                return ulong.MaxValue;
            }

            return (1UL << usedBitsInLastWord) - 1UL;
        }

        private static void ValidateConstructorArguments(int bitLength, Allocator allocator)
        {
            UnsafeCollectionChecks.CheckNonNegativeArgument(bitLength, nameof(bitLength));
            UnsafeCollectionChecks.CheckAllocator(allocator, nameof(allocator));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderState()
        {
            if (m_Header->BitLength < 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            int expectedWordLength = UnsafeCollectionSize.CheckedWordLength(m_Header->BitLength);

            if (m_Header->WordLength != expectedWordLength)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Header->WordLength > 0 && m_Words == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckBitIndex(int bitIndex, int bitLength)
        {
            UnsafeCollectionChecks.CheckIndexInRange(bitIndex, bitLength, nameof(bitIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWordIndex(int wordIndex, int wordLength)
        {
            UnsafeCollectionChecks.CheckIndexInRange(wordIndex, wordLength, nameof(wordIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWordRange(int wordStart, int wordEnd, int wordLength)
        {
            if (wordStart < 0 || wordStart > wordEnd || wordEnd > wordLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wordStart),
                    wordStart,
                    "Word range must satisfy 0 <= start <= end <= word length.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public int BitLength;
            public int WordLength;
        }
    }

    /// <summary>
    /// Non-owning mutable word-range view over an <see cref="UnsafeBitGrid"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This view is designed for parallel jobs where each worker owns a disjoint absolute word range.
    /// It does not own memory and must not outlive the source bit grid.
    /// </para>
    /// <para>
    /// Mutating methods validate that the target bit or word belongs to the range. These ownership checks
    /// are always-on because they protect release-build memory safety.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("WordStart = {" + nameof(WordStart) + "}, WordEnd = {" + nameof(WordEnd) + "}")]
    internal unsafe readonly struct UnsafeBitGridWordRange
    {
        private const int BitsPerWord = 64;
        private const int WordShift = 6;
        private const int WordBitMask = BitsPerWord - 1;
        private const string TypeName = nameof(UnsafeBitGridWordRange);

        private readonly byte m_IsCreated;

        [NativeDisableUnsafePtrRestriction]
        private readonly ulong* m_Words;

        private readonly int m_BitLength;
        private readonly int m_WordLength;
        private readonly int m_WordStart;
        private readonly int m_WordEnd;

        internal UnsafeBitGridWordRange(
            ulong* words,
            int bitLength,
            int wordLength,
            int wordStart,
            int wordEnd,
            bool isCreated)
        {
            ValidateConstructorArguments(words, bitLength, wordLength, wordStart, wordEnd);

            m_IsCreated = isCreated ? (byte)1 : (byte)0;
            m_Words = words;
            m_BitLength = bitLength;
            m_WordLength = wordLength;
            m_WordStart = wordStart;
            m_WordEnd = wordEnd;
        }

        /// <summary>
        /// Gets a value indicating whether this value was explicitly constructed as a word-range view.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsCreated != 0;
        }

        /// <summary>
        /// Gets the number of valid bits in the source grid.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public int BitLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_BitLength;
            }
        }

        /// <summary>
        /// Gets the number of backing words in the source grid.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public int WordLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_WordLength;
            }
        }

        /// <summary>
        /// Gets the inclusive absolute word start.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public int WordStart
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_WordStart;
            }
        }

        /// <summary>
        /// Gets the exclusive absolute word end.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public int WordEnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_WordEnd;
            }
        }

        /// <summary>
        /// Gets the number of words owned by this range.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public int WordCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_WordEnd - m_WordStart;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this range contains no words.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_WordStart == m_WordEnd;
            }
        }

        /// <summary>
        /// Determines whether this range owns the supplied absolute word index.
        /// </summary>
        /// <param name="absoluteWordIndex">The absolute word index.</param>
        /// <returns><see langword="true"/> when the word is owned by this range; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsWord(int absoluteWordIndex)
        {
            CheckCreated();
            return absoluteWordIndex >= m_WordStart && absoluteWordIndex < m_WordEnd;
        }

        /// <summary>
        /// Determines whether this range owns the word containing the supplied bit index.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <returns><see langword="true"/> when the bit is in the source grid and owned by this range; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsBit(int bitIndex)
        {
            CheckCreated();

            if ((uint)bitIndex >= (uint)m_BitLength)
            {
                return false;
            }

            int wordIndex = bitIndex >> WordShift;
            return ContainsWord(wordIndex);
        }

        /// <summary>
        /// Reads one bit owned by this word range.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <returns><see langword="true"/> when the bit is set; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the source bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated or does not own the target word.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int bitIndex)
        {
            CheckCreated();
            CheckBitOwnedByRange(bitIndex, m_BitLength, m_WordStart, m_WordEnd);

            int wordIndex = bitIndex >> WordShift;
            ulong mask = 1UL << (bitIndex & WordBitMask);

            return (m_Words[wordIndex] & mask) != 0UL;
        }

        /// <summary>
        /// Sets one bit owned by this word range.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the source bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated or does not own the target word.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int bitIndex)
        {
            CheckCreated();
            CheckBitOwnedByRange(bitIndex, m_BitLength, m_WordStart, m_WordEnd);

            int wordIndex = bitIndex >> WordShift;
            ulong mask = 1UL << (bitIndex & WordBitMask);

            m_Words[wordIndex] |= mask;
        }

        /// <summary>
        /// Clears one bit owned by this word range.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the source bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated or does not own the target word.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int bitIndex)
        {
            CheckCreated();
            CheckBitOwnedByRange(bitIndex, m_BitLength, m_WordStart, m_WordEnd);

            int wordIndex = bitIndex >> WordShift;
            ulong mask = 1UL << (bitIndex & WordBitMask);

            m_Words[wordIndex] &= ~mask;
        }

        /// <summary>
        /// Writes one bit owned by this word range.
        /// </summary>
        /// <param name="bitIndex">The bit index.</param>
        /// <param name="value">The bit value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="bitIndex"/> is outside the source bit range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated or does not own the target word.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int bitIndex, bool value)
        {
            if (value)
            {
                Set(bitIndex);
            }
            else
            {
                Clear(bitIndex);
            }
        }

        /// <summary>
        /// Reads one absolute word owned by this range.
        /// </summary>
        /// <param name="absoluteWordIndex">The absolute word index.</param>
        /// <returns>The word value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated or does not own the target word.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadWord(int absoluteWordIndex)
        {
            CheckCreated();
            CheckWordOwnedByRange(absoluteWordIndex, m_WordStart, m_WordEnd);

            return m_Words[absoluteWordIndex];
        }

        /// <summary>
        /// Writes one absolute word owned by this range.
        /// </summary>
        /// <remarks>
        /// When writing the source grid's last word, unused high bits are masked out.
        /// </remarks>
        /// <param name="absoluteWordIndex">The absolute word index.</param>
        /// <param name="value">The word value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated or does not own the target word.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteWord(int absoluteWordIndex, ulong value)
        {
            CheckCreated();
            CheckWordOwnedByRange(absoluteWordIndex, m_WordStart, m_WordEnd);

            if (absoluteWordIndex == m_WordLength - 1)
            {
                value &= GetLastWordMask(m_BitLength);
            }

            m_Words[absoluteWordIndex] = value;
        }

        /// <summary>
        /// Clears every word owned by this range.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public void ClearWords()
        {
            CheckCreated();

            for (int wordIndex = m_WordStart; wordIndex < m_WordEnd; wordIndex++)
            {
                m_Words[wordIndex] = 0UL;
            }
        }

        /// <summary>
        /// Sets every valid bit in every word owned by this range.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the range is uncreated.
        /// </exception>
        public void SetWords()
        {
            CheckCreated();

            for (int wordIndex = m_WordStart; wordIndex < m_WordEnd; wordIndex++)
            {
                m_Words[wordIndex] = ulong.MaxValue;
            }

            if (m_WordEnd == m_WordLength && m_WordStart < m_WordEnd)
            {
                m_Words[m_WordLength - 1] &= GetLastWordMask(m_BitLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetLastWordMask(int bitLength)
        {
            if (bitLength == 0)
            {
                return 0UL;
            }

            int usedBitsInLastWord = bitLength & WordBitMask;

            if (usedBitsInLastWord == 0)
            {
                return ulong.MaxValue;
            }

            return (1UL << usedBitsInLastWord) - 1UL;
        }

        private static void ValidateConstructorArguments(
            ulong* words,
            int bitLength,
            int wordLength,
            int wordStart,
            int wordEnd)
        {
            UnsafeCollectionChecks.CheckNonNegativeArgument(bitLength, nameof(bitLength));
            UnsafeCollectionChecks.CheckNonNegativeArgument(wordLength, nameof(wordLength));

            int expectedWordLength = UnsafeCollectionSize.CheckedWordLength(bitLength);

            if (wordLength != expectedWordLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wordLength),
                    wordLength,
                    "Word length must match bit length.");
            }

            if (wordLength > 0 && words == null)
            {
                throw new ArgumentNullException(
                    nameof(words),
                    "Words pointer must not be null when word length is greater than zero.");
            }

            if (wordStart < 0 || wordStart > wordEnd || wordEnd > wordLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wordStart),
                    wordStart,
                    "Word range must satisfy 0 <= start <= end <= word length.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckBitOwnedByRange(int bitIndex, int bitLength, int wordStart, int wordEnd)
        {
            UnsafeCollectionChecks.CheckIndexInRange(bitIndex, bitLength, nameof(bitIndex));

            int wordIndex = bitIndex >> WordShift;

            if (wordIndex < wordStart || wordIndex >= wordEnd)
            {
                throw new InvalidOperationException(TypeName + " does not own the target bit word.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWordOwnedByRange(int wordIndex, int wordStart, int wordEnd)
        {
            if (wordIndex < wordStart || wordIndex >= wordEnd)
            {
                throw new InvalidOperationException(TypeName + " does not own the target word.");
            }
        }
    }
}