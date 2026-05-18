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
    /// Unsafe owning builder for finalized <see cref="UnsafeGridSpanList{T}"/> views.
    /// </summary>
    /// <typeparam name="T">The unmanaged item type.</typeparam>
    /// <remarks>
    /// <para>
    /// This builder owns unmanaged memory. The span list returned by <see cref="AsSpanList"/> is a
    /// non-owning view over this builder's memory and is valid only while the builder remains allocated
    /// and unchanged.
    /// </para>
    /// <para>
    /// The builder is phase-based: count, begin fill, add, complete, then expose the finalized CSR view.
    /// This avoids per-owner dynamic lists, hash-map iteration instability, and managed allocation.
    /// </para>
    /// <para>
    /// Owner-relative insertion order is preserved. Canonical output therefore requires canonical
    /// <see cref="Add(int, T)"/> order for each owner, or a separate deterministic sort/freeze pass.
    /// </para>
    /// <para>
    /// This is a single-writer unsafe core. It is not thread-safe and does not provide a parallel writer.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("OwnerCount = {" + nameof(OwnerCount) + "}, ItemCapacity = {" + nameof(ItemCapacity) + "}, ItemCount = {" + nameof(ItemCount) + "}, State = {" + nameof(StateName) + "}")]
    internal unsafe struct UnsafeGridSpanListBuilder<T> : IDisposable
        where T : unmanaged
    {
        private const byte StateCounting = 0;
        private const byte StateFilling = 1;
        private const byte StateCompleted = 2;

        private const string TypeName = nameof(UnsafeGridSpanListBuilder<T>);

        [NativeDisableUnsafePtrRestriction]
        private Header* m_Header;

        [NativeDisableUnsafePtrRestriction]
        private int* m_Offsets;

        [NativeDisableUnsafePtrRestriction]
        private int* m_Cursors;

        [NativeDisableUnsafePtrRestriction]
        private T* m_Items;

        private Allocator m_AllocatorLabel;

        /// <summary>
        /// Initializes a new reusable span-list builder.
        /// </summary>
        /// <param name="ownerCount">The number of owners/spans.</param>
        /// <param name="itemCapacity">The maximum total item count.</param>
        /// <param name="allocator">The allocator used for unmanaged storage.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerCount"/> or <paramref name="itemCapacity"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is not a valid allocation allocator, or when
        /// <paramref name="itemCapacity"/> is greater than zero while <paramref name="ownerCount"/> is zero.
        /// </exception>
        public UnsafeGridSpanListBuilder(int ownerCount, int itemCapacity, Allocator allocator)
        {
            ValidateConstructorArguments(ownerCount, itemCapacity, allocator);

            m_Header = null;
            m_Offsets = null;
            m_Cursors = null;
            m_Items = null;
            m_AllocatorLabel = allocator;

            int offsetLength = UnsafeCollectionSize.CheckedOffsetLength(ownerCount);

            try
            {
                m_Header = (Header*)UnsafeUtility.MallocTracked(
                    UnsafeUtility.SizeOf<Header>(),
                    UnsafeUtility.AlignOf<Header>(),
                    allocator,
                    1);

                m_Offsets = (int*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<int>(offsetLength, nameof(offsetLength)),
                    UnsafeUtility.AlignOf<int>(),
                    allocator,
                    1);

                if (ownerCount > 0)
                {
                    m_Cursors = (int*)UnsafeUtility.MallocTracked(
                        UnsafeCollectionSize.CheckedPositiveElementBytes<int>(ownerCount, nameof(ownerCount)),
                        UnsafeUtility.AlignOf<int>(),
                        allocator,
                        1);
                }

                if (itemCapacity > 0)
                {
                    m_Items = (T*)UnsafeUtility.MallocTracked(
                        UnsafeCollectionSize.CheckedPositiveElementBytes<T>(itemCapacity, nameof(itemCapacity)),
                        UnsafeUtility.AlignOf<T>(),
                        allocator,
                        1);
                }

                m_Header->OwnerCount = ownerCount;
                m_Header->ItemCapacity = itemCapacity;
                m_Header->ItemCount = 0;
                m_Header->FilledItemCount = 0;
                m_Header->State = StateCounting;

                Clear();
            }
            catch
            {
                FreeAllocatedMemory();

                m_Header = null;
                m_Offsets = null;
                m_Cursors = null;
                m_Items = null;
                m_AllocatorLabel = Allocator.Invalid;

                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this value references allocated builder storage.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Header != null && m_Offsets != null;
        }

        /// <summary>
        /// Gets the number of owners/spans.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public int OwnerCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->OwnerCount;
            }
        }

        /// <summary>
        /// Gets the maximum total item count supported by this builder.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public int ItemCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->ItemCapacity;
            }
        }

        /// <summary>
        /// Gets the counted or finalized item count.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public int ItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->ItemCount;
            }
        }

        /// <summary>
        /// Gets the number of items written during the fill phase.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public int FilledItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->FilledItemCount;
            }
        }

        /// <summary>
        /// Gets the remaining item capacity during the counting phase.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public int RemainingCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->ItemCapacity - m_Header->ItemCount;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the builder is in the counting phase.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public bool IsCounting
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->State == StateCounting;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the builder is in the filling phase.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public bool IsFilling
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->State == StateFilling;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the builder has completed filling and can expose a span-list view.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->State == StateCompleted;
            }
        }

        private string StateName
        {
            get
            {
                if (!IsCreated)
                {
                    return "NotCreated";
                }

                return GetStateName(m_Header->State);
            }
        }

        /// <summary>
        /// Clears the builder and returns it to the counting phase.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated or structurally invalid.
        /// </exception>
        public void Clear()
        {
            CheckCreated();
            CheckHeaderDimensionsAndPointers();

            int ownerCount = m_Header->OwnerCount;
            int offsetLength = UnsafeCollectionSize.CheckedOffsetLength(ownerCount);

            UnsafeUtility.MemClear(
                m_Offsets,
                UnsafeCollectionSize.CheckedPositiveElementBytes<int>(offsetLength, nameof(offsetLength)));

            if (ownerCount > 0)
            {
                UnsafeUtility.MemClear(
                    m_Cursors,
                    UnsafeCollectionSize.CheckedPositiveElementBytes<int>(ownerCount, nameof(ownerCount)));
            }

            m_Header->ItemCount = 0;
            m_Header->FilledItemCount = 0;
            m_Header->State = StateCounting;
        }

        /// <summary>
        /// Counts one item for an owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, not in the counting phase, or lacks capacity.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Count(int ownerIndex)
        {
            Count(ownerIndex, 1);
        }

        /// <summary>
        /// Counts multiple items for an owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <param name="itemCount">The number of items to count.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range or <paramref name="itemCount"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, not in the counting phase, or lacks capacity.
        /// </exception>
        public void Count(int ownerIndex, int itemCount)
        {
            CheckCreated();
            CheckHeaderState();
            CheckState(StateCounting);
            CheckOwnerIndex(ownerIndex, m_Header->OwnerCount);
            UnsafeCollectionChecks.CheckNonNegative(itemCount, nameof(itemCount));

            if (itemCount == 0)
            {
                return;
            }

            CheckCountingCapacityAvailable(m_Header->ItemCount, itemCount, m_Header->ItemCapacity);

            int offsetIndex = ownerIndex + 1;
            int ownerCountedItems = UnsafeCollectionSize.CheckedAdd(
                m_Offsets[offsetIndex],
                itemCount,
                TypeName + " owner count overflowed.");

            int totalItemCount = UnsafeCollectionSize.CheckedAdd(
                m_Header->ItemCount,
                itemCount,
                TypeName + " item count overflowed.");

            if (totalItemCount > m_Header->ItemCapacity)
            {
                throw new InvalidOperationException(TypeName + " item capacity exceeded.");
            }

            m_Offsets[offsetIndex] = ownerCountedItems;
            m_Header->ItemCount = totalItemCount;
        }

        /// <summary>
        /// Converts owner counts into prefix-sum offsets and enters the filling phase.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, or not in the counting phase.
        /// </exception>
        public void BeginFill()
        {
            CheckCreated();
            CheckHeaderState();
            CheckState(StateCounting);

            int running = 0;
            int countedTotal = m_Header->ItemCount;

            if (m_Offsets[0] != 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            for (int ownerIndex = 0; ownerIndex < m_Header->OwnerCount; ownerIndex++)
            {
                int ownerCount = m_Offsets[ownerIndex + 1];

                if (ownerCount < 0)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                running = UnsafeCollectionSize.CheckedAdd(
                    running,
                    ownerCount,
                    TypeName + " prefix sum overflowed.");

                if (running > m_Header->ItemCapacity)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                m_Offsets[ownerIndex + 1] = running;
                m_Cursors[ownerIndex] = m_Offsets[ownerIndex];
            }

            if (running != countedTotal)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            m_Header->FilledItemCount = 0;
            m_Header->State = StateFilling;
        }

        /// <summary>
        /// Adds one item to an owner span during the filling phase.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <param name="item">The item value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, not in the filling phase, or the owner
        /// receives more items than were counted.
        /// </exception>
        public void Add(int ownerIndex, T item)
        {
            CheckCreated();
            CheckHeaderState();
            CheckState(StateFilling);
            CheckOwnerIndex(ownerIndex, m_Header->OwnerCount);
            CheckItemsAvailable();

            int cursor = m_Cursors[ownerIndex];
            int end = m_Offsets[ownerIndex + 1];

            CheckOwnerFillCapacity(cursor, end);

            if ((uint)cursor >= (uint)m_Header->ItemCapacity)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            m_Items[cursor] = item;
            m_Cursors[ownerIndex] = cursor + 1;
            m_Header->FilledItemCount++;
        }

        /// <summary>
        /// Completes the filling phase and validates that every counted item was written.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, not in the filling phase, or incompletely filled.
        /// </exception>
        public void Complete()
        {
            CheckCreated();
            CheckHeaderState();
            CheckState(StateFilling);
            CheckFilledItemCount(m_Header->FilledItemCount, m_Header->ItemCount);

            for (int ownerIndex = 0; ownerIndex < m_Header->OwnerCount; ownerIndex++)
            {
                CheckOwnerCompletelyFilled(m_Cursors[ownerIndex], m_Offsets[ownerIndex + 1]);
            }

            m_Header->State = StateCompleted;
        }

        /// <summary>
        /// Gets the finalized non-owning span-list view.
        /// </summary>
        /// <returns>A read-only span-list view over this builder's memory.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, or not completed.
        /// </exception>
        public UnsafeGridSpanList<T> AsSpanList()
        {
            CheckCreated();
            CheckHeaderState();
            CheckState(StateCompleted);

            return new UnsafeGridSpanList<T>(
                m_Offsets,
                m_Header->ItemCount == 0 ? null : m_Items,
                m_Header->OwnerCount,
                m_Header->ItemCount);
        }

        /// <summary>
        /// Gets the finalized item count for an owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The number of items counted for the owner.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, or still in the counting phase.
        /// </exception>
        public int GetSpanLength(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out int start, out int end);
            return end - start;
        }

        /// <summary>
        /// Gets the finalized inclusive span start for an owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The inclusive item start index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, or still in the counting phase.
        /// </exception>
        public int GetSpanStart(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out int start, out _);
            return start;
        }

        /// <summary>
        /// Gets the finalized exclusive span end for an owner.
        /// </summary>
        /// <param name="ownerIndex">The owner index.</param>
        /// <returns>The exclusive item end index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="ownerIndex"/> is outside the owner range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the builder is uncreated, structurally invalid, or still in the counting phase.
        /// </exception>
        public int GetSpanEnd(int ownerIndex)
        {
            GetOwnerSpan(ownerIndex, out _, out int end);
            return end;
        }

        /// <summary>
        /// Releases all unmanaged memory owned by this builder value.
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
            m_Offsets = null;
            m_Cursors = null;
            m_Items = null;
            m_AllocatorLabel = Allocator.Invalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetOwnerSpan(int ownerIndex, out int start, out int end)
        {
            CheckCreated();
            CheckHeaderState();
            CheckNotCounting();
            CheckOwnerIndex(ownerIndex, m_Header->OwnerCount);

            start = m_Offsets[ownerIndex];
            end = m_Offsets[ownerIndex + 1];

            CheckSpanRange(start, end, m_Header->ItemCount);
        }

        private void FreeAllocatedMemory()
        {
            if (m_Items != null)
            {
                UnsafeUtility.FreeTracked(m_Items, m_AllocatorLabel);
            }

            if (m_Cursors != null)
            {
                UnsafeUtility.FreeTracked(m_Cursors, m_AllocatorLabel);
            }

            if (m_Offsets != null)
            {
                UnsafeUtility.FreeTracked(m_Offsets, m_AllocatorLabel);
            }

            if (m_Header != null)
            {
                UnsafeUtility.FreeTracked(m_Header, m_AllocatorLabel);
            }
        }

        private static void ValidateConstructorArguments(int ownerCount, int itemCapacity, Allocator allocator)
        {
            UnsafeCollectionChecks.CheckNonNegative(ownerCount, nameof(ownerCount));
            UnsafeCollectionChecks.CheckNonNegative(itemCapacity, nameof(itemCapacity));
            UnsafeCollectionChecks.CheckAllocator(allocator, nameof(allocator));
            UnsafeCollectionSize.CheckedOffsetLength(ownerCount);

            if (ownerCount == 0 && itemCapacity > 0)
            {
                throw new ArgumentException(
                    TypeName + " item capacity must be zero when owner count is zero.",
                    nameof(itemCapacity));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderDimensionsAndPointers()
        {
            if (m_Header->OwnerCount < 0 || m_Header->ItemCapacity < 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Header->OwnerCount == 0 && m_Header->ItemCapacity != 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Offsets == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Header->OwnerCount > 0 && m_Cursors == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Header->ItemCapacity > 0 && m_Items == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            UnsafeCollectionSize.CheckedOffsetLength(m_Header->OwnerCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderState()
        {
            CheckHeaderDimensionsAndPointers();

            if (m_Header->ItemCount < 0 || m_Header->ItemCount > m_Header->ItemCapacity)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Header->FilledItemCount < 0 || m_Header->FilledItemCount > m_Header->ItemCount)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            if (m_Header->State != StateCounting
                && m_Header->State != StateFilling
                && m_Header->State != StateCompleted)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckState(byte expectedState)
        {
            if (m_Header->State != expectedState)
            {
                throw new InvalidOperationException(TypeName + " is in an invalid phase for this operation.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNotCounting()
        {
            if (m_Header->State == StateCounting)
            {
                throw new InvalidOperationException(TypeName + " cannot expose finalized spans while counting.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOwnerIndex(int ownerIndex, int ownerCount)
        {
            UnsafeCollectionChecks.CheckIndexInRange(ownerIndex, ownerCount, nameof(ownerIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCountingCapacityAvailable(int currentItemCount, int addedItemCount, int itemCapacity)
        {
            if (addedItemCount > itemCapacity - currentItemCount)
            {
                throw new InvalidOperationException(TypeName + " item capacity exceeded.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckItemsAvailable()
        {
            if (m_Header->ItemCount > 0 && m_Items == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOwnerFillCapacity(int cursor, int end)
        {
            if (cursor >= end)
            {
                throw new InvalidOperationException(TypeName + " owner received more items than were counted.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckFilledItemCount(int filledItemCount, int itemCount)
        {
            if (filledItemCount != itemCount)
            {
                throw new InvalidOperationException(TypeName + " filled item count does not match counted item count.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOwnerCompletelyFilled(int cursor, int end)
        {
            if (cursor != end)
            {
                throw new InvalidOperationException(TypeName + " owner was not completely filled.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckSpanRange(int start, int end, int itemCount)
        {
            if (start < 0 || start > end || end > itemCount)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        private static string GetStateName(byte state)
        {
            switch (state)
            {
                case StateCounting:
                    return "Counting";

                case StateFilling:
                    return "Filling";

                case StateCompleted:
                    return "Completed";

                default:
                    return "Unknown";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public int OwnerCount;
            public int ItemCapacity;
            public int ItemCount;
            public int FilledItemCount;
            public byte State;
        }
    }
}