#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Non-owning unsafe CSR/span-list view for deterministic one-to-many data.
    /// </summary>
    /// <typeparam name="T">The unmanaged item type.</typeparam>
    /// <remarks>
    /// <para>
    /// This type is a read-only view over externally owned memory. It never allocates, frees, resizes,
    /// sorts, or mutates backing storage.
    /// </para>
    /// <para>
    /// Layout is compressed sparse row style. The offset buffer contains <c>OwnerCount + 1</c> entries.
    /// For owner <c>i</c>, the item range is <c>[Offsets[i], Offsets[i + 1])</c>.
    /// </para>
    /// <para>
    /// Offsets must start at zero, be monotonic, and end at <see cref="ItemCount"/>. Constructor validation
    /// is always-on because malformed offsets can otherwise produce invalid pointer access in release builds.
    /// </para>
    /// <para>
    /// This type does not own lifetime. The caller must guarantee that offsets and items remain valid for
    /// the full lifetime of the view.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("OwnerCount = {" + nameof(OwnerCount) + "}, ItemCount = {" + nameof(ItemCount) + "}")]
    internal unsafe readonly struct UnsafeGridSpanList<T>
        where T : unmanaged
    {
        private const string TypeName = nameof(UnsafeGridSpanList<T>);

        [NativeDisableUnsafePtrRestriction]
        private readonly int* m_Offsets;

        [NativeDisableUnsafePtrRestriction]
        private readonly T* m_Items;

        private readonly int m_OwnerCount;
        private readonly int m_ItemCount;

        /// <summary>
        /// Initializes a new non-owning CSR/span-list view over externally owned memory.
        /// </summary>
        /// <param name="offsets">
        /// Pointer to offset memory. The memory must contain <paramref name="ownerCount"/> + 1 integers.
        /// </param>
        /// <param name="items">
        /// Pointer to item memory. The memory must contain <paramref name="itemCount"/> items, or may be
        /// null when <paramref name="itemCount"/> is zero.
        /// </param>
        /// <param name="ownerCount">The number of owners/spans.</param>
        /// <param name="itemCount">The total number of items across all owners.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="offsets"/> is null, or when <paramref name="items"/> is null while
        /// <paramref name="itemCount"/> is greater than zero.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerCount"/> or <paramref name="itemCount"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the offset buffer does not describe a valid CSR layout.
        /// </exception>
        public UnsafeGridSpanList(int* offsets, T* items, int ownerCount, int itemCount)
        {
            ValidateConstructorArguments(offsets, items, ownerCount, itemCount);
            ValidateOffsets(offsets, ownerCount, itemCount);

            m_Offsets = offsets;
            m_Items = items;
            m_OwnerCount = ownerCount;
            m_ItemCount = itemCount;
        }

        /// <summary>
        /// Gets a value indicating whether this value references offset memory.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Offsets != null;
        }

        /// <summary>
        /// Gets the number of owners/spans.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        public int OwnerCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckState();
                return m_OwnerCount;
            }
        }

        /// <summary>
        /// Gets the total number of items across all owners.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        public int ItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckState();
                return m_ItemCount;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the list contains no items.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckState();
                return m_ItemCount == 0;
            }
        }

        /// <summary>
        /// Determines whether the supplied owner index is inside the owner range.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns><see langword="true"/> when the owner index is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsOwner(int ownerIndex)
        {
            CheckCreated();
            CheckState();
            return (uint)ownerIndex < (uint)m_OwnerCount;
        }

        /// <summary>
        /// Determines whether the supplied absolute item index is inside the item range.
        /// </summary>
        /// <param name="itemIndex">The absolute item index.</param>
        /// <returns><see langword="true"/> when the item index is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsItem(int itemIndex)
        {
            CheckCreated();
            CheckState();
            return (uint)itemIndex < (uint)m_ItemCount;
        }

        /// <summary>
        /// Determines whether the specified owner has at least one item.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns><see langword="true"/> when the owner span is non-empty; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasItems(int ownerIndex)
        {
            return GetSpanLength(ownerIndex) != 0;
        }

        /// <summary>
        /// Gets the item count for one owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The number of items in the owner's span.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSpanLength(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out int start, out int end);
            return end - start;
        }

        /// <summary>
        /// Gets the inclusive item start index for one owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The inclusive item start index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSpanStart(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out int start, out _);
            return start;
        }

        /// <summary>
        /// Gets the exclusive item end index for one owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The exclusive item end index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSpanEnd(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out _, out int end);
            return end;
        }

        /// <summary>
        /// Gets a non-owning span view for one owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The owner item span.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeGridSpan<T> GetSpan(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out int start, out int end);

            int length = end - start;
            T* items = length == 0 ? null : m_Items + start;

            return new UnsafeGridSpan<T>(items, length, isCreated: true);
        }

        /// <summary>
        /// Reads an item by absolute item index.
        /// </summary>
        /// <param name="itemIndex">The absolute item index in the range [0, ItemCount).</param>
        /// <returns>The item value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="itemIndex"/> is outside the item range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadItem(int itemIndex)
        {
            CheckCreated();
            CheckState();
            CheckItemIndex(itemIndex, m_ItemCount);
            CheckItemsAvailable();

            return m_Items[itemIndex];
        }

        /// <summary>
        /// Reads an owner-relative item.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <param name="spanItemIndex">The item index relative to the owner span.</param>
        /// <returns>The item value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> or <paramref name="spanItemIndex"/> is outside its valid range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadSpanItem(int ownerIndex, int spanItemIndex)
        {
            GetOwnerSpan(ownerIndex, out int start, out int end);

            int length = end - start;

            CheckSpanItemIndex(spanItemIndex, length);
            CheckItemsAvailable();

            return m_Items[start + spanItemIndex];
        }

        /// <summary>
        /// Creates an empty non-owning span-list view over valid externally owned offset memory.
        /// </summary>
        /// <remarks>
        /// The offset memory must contain <paramref name="ownerCount"/> + 1 zero entries.
        /// </remarks>
        /// <param name="offsets">Pointer to offset memory.</param>
        /// <param name="ownerCount">The number of owners/spans.</param>
        /// <returns>An empty span-list view.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="offsets"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerCount"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the offset buffer does not contain an empty CSR layout.
        /// </exception>
        public static UnsafeGridSpanList<T> CreateEmpty(int* offsets, int ownerCount)
        {
            return new UnsafeGridSpanList<T>(offsets, null, ownerCount, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetOwnerSpan(int ownerIndex, out int start, out int end)
        {
            CheckCreated();
            CheckState();
            CheckOwnerIndex(ownerIndex, m_OwnerCount);

            start = m_Offsets[ownerIndex];
            end = m_Offsets[ownerIndex + 1];

            CheckSpanRange(start, end, m_ItemCount);
        }

        private static void ValidateConstructorArguments(int* offsets, T* items, int ownerCount, int itemCount)
        {
            UnsafeCollectionChecks.CheckNonNegative(ownerCount, nameof(ownerCount));
            UnsafeCollectionChecks.CheckNonNegative(itemCount, nameof(itemCount));

            UnsafeCollectionSize.CheckedOffsetLength(ownerCount);

            if (offsets == null)
            {
                throw new ArgumentNullException(
                    nameof(offsets),
                    "UnsafeGridSpanList offsets pointer must not be null.");
            }

            if (itemCount > 0 && items == null)
            {
                throw new ArgumentNullException(
                    nameof(items),
                    "UnsafeGridSpanList items pointer must not be null when item count is greater than zero.");
            }
        }

        private static void ValidateOffsets(int* offsets, int ownerCount, int itemCount)
        {
            if (offsets[0] != 0)
            {
                throw new ArgumentException(
                    "UnsafeGridSpanList offsets must start at zero.",
                    nameof(offsets));
            }

            int previous = 0;

            for (int index = 1; index <= ownerCount; index++)
            {
                int current = offsets[index];

                if (current < previous)
                {
                    throw new ArgumentException(
                        "UnsafeGridSpanList offsets must be monotonic.",
                        nameof(offsets));
                }

                if (current > itemCount)
                {
                    throw new ArgumentException(
                        "UnsafeGridSpanList offset exceeds item count.",
                        nameof(offsets));
                }

                previous = current;
            }

            if (offsets[ownerCount] != itemCount)
            {
                throw new ArgumentException(
                    "UnsafeGridSpanList final offset must equal item count.",
                    nameof(offsets));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckState()
        {
            if (m_OwnerCount < 0 || m_ItemCount < 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Offsets == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_ItemCount > 0 && m_Items == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Offsets[0] != 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Offsets[m_OwnerCount] != m_ItemCount)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckItemsAvailable()
        {
            if (m_ItemCount > 0 && m_Items == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOwnerIndex(int ownerIndex, int ownerCount)
        {
            UnsafeCollectionChecks.CheckIndexInRange(ownerIndex, ownerCount, nameof(ownerIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckItemIndex(int itemIndex, int itemCount)
        {
            UnsafeCollectionChecks.CheckIndexInRange(itemIndex, itemCount, nameof(itemIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckSpanItemIndex(int spanItemIndex, int spanLength)
        {
            UnsafeCollectionChecks.CheckIndexInRange(spanItemIndex, spanLength, nameof(spanItemIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckSpanRange(int start, int end, int itemCount)
        {
            if (start < 0 || start > end || end > itemCount)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }
    }

    /// <summary>
    /// Non-owning unsafe contiguous read-only item span.
    /// </summary>
    /// <typeparam name="T">The unmanaged item type.</typeparam>
    /// <remarks>
    /// This type does not own memory and must not outlive the backing storage that created it.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {" + nameof(Length) + "}")]
    internal unsafe readonly struct UnsafeGridSpan<T>
        where T : unmanaged
    {
        private const string TypeName = nameof(UnsafeGridSpan<T>);

        private readonly byte m_IsCreated;

        [NativeDisableUnsafePtrRestriction]
        private readonly T* m_Items;

        private readonly int m_Length;

        /// <summary>
        /// Initializes a new non-owning item span.
        /// </summary>
        /// <param name="items">Pointer to the first item, or null when <paramref name="length"/> is zero.</param>
        /// <param name="length">The span length.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="items"/> is null while <paramref name="length"/> is greater than zero.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="length"/> is negative.
        /// </exception>
        public UnsafeGridSpan(T* items, int length)
            : this(items, length, isCreated: true)
        {
        }

        internal UnsafeGridSpan(T* items, int length, bool isCreated)
        {
            ValidateConstructorArguments(items, length);

            m_IsCreated = isCreated ? (byte)1 : (byte)0;
            m_Items = items;
            m_Length = length;
        }

        /// <summary>
        /// Gets a value indicating whether this value was explicitly constructed as a span.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsCreated != 0;
        }

        /// <summary>
        /// Gets the number of items in the span.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the span is uncreated or structurally invalid.
        /// </exception>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckState();
                return m_Length;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the span contains no items.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the span is uncreated or structurally invalid.
        /// </exception>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckState();
                return m_Length == 0;
            }
        }

        /// <summary>
        /// Determines whether the supplied index is inside the span.
        /// </summary>
        /// <param name="index">The span-relative index.</param>
        /// <returns><see langword="true"/> when the index is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the span is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsIndex(int index)
        {
            CheckCreated();
            CheckState();
            return (uint)index < (uint)m_Length;
        }

        /// <summary>
        /// Reads an item by span-relative index.
        /// </summary>
        /// <param name="index">The span-relative index.</param>
        /// <returns>The item value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the span.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the span is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read(int index)
        {
            CheckCreated();
            CheckState();
            CheckIndex(index, m_Length);

            return m_Items[index];
        }

        private static void ValidateConstructorArguments(T* items, int length)
        {
            UnsafeCollectionChecks.CheckNonNegative(length, nameof(length));

            if (length > 0 && items == null)
            {
                throw new ArgumentNullException(
                    nameof(items),
                    "UnsafeGridSpan items pointer must not be null when length is greater than zero.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckState()
        {
            if (m_Length < 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Length > 0 && m_Items == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIndex(int index, int length)
        {
            UnsafeCollectionChecks.CheckIndexInRange(index, length, nameof(index));
        }
    }
}