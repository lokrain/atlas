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
    /// Unsafe deterministic fixed-capacity binary min-priority queue specialized for cell-index propagation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type owns unmanaged memory directly. It is deliberately not marked as a Unity
    /// <c>NativeContainer</c>. A separate native wrapper or scheduler-owned scratch lease must own
    /// Unity safety handles, job dependencies, and dependency-aware disposal.
    /// </para>
    /// <para>
    /// Dequeue order is deterministic: lower priority first, then lower cell index. Equal priority and
    /// equal cell duplicate entries are considered equivalent.
    /// </para>
    /// <para>
    /// This queue permits duplicate cell indices. For Dijkstra-like or flood-fill algorithms, use an
    /// external finalized, visited, or best-priority field to suppress stale entries.
    /// </para>
    /// <para>
    /// This is a single-writer unsafe core. It is not thread-safe and does not provide a parallel writer.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}, Capacity = {" + nameof(Capacity) + "}")]
    internal unsafe struct UnsafeCellPriorityQueue : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private Header* m_Header;

        [NativeDisableUnsafePtrRestriction]
        private Node* m_Nodes;

        private Allocator m_AllocatorLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeCellPriorityQueue"/> struct.
        /// </summary>
        /// <param name="capacity">The maximum number of queued entries.</param>
        /// <param name="allocator">The allocator used for unmanaged storage.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="capacity"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is not a valid allocation allocator.
        /// </exception>
        public UnsafeCellPriorityQueue(int capacity, Allocator allocator)
        {
            ValidateConstructorArguments(capacity, allocator);

            m_Header = null;
            m_Nodes = null;
            m_AllocatorLabel = allocator;

            try
            {
                m_Header = (Header*)UnsafeUtility.MallocTracked(
                    UnsafeUtility.SizeOf<Header>(),
                    UnsafeUtility.AlignOf<Header>(),
                    allocator,
                    1);

                m_Nodes = (Node*)UnsafeUtility.MallocTracked(
                    GetAllocationSize<Node>(capacity),
                    UnsafeUtility.AlignOf<Node>(),
                    allocator,
                    1);

                m_Header->Capacity = capacity;
                m_Header->Count = 0;
            }
            catch
            {
                FreeAllocatedMemory();

                m_Header = null;
                m_Nodes = null;
                m_AllocatorLabel = Allocator.Invalid;

                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this value references allocated unmanaged queue storage.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Header != null && m_Nodes != null;
        }

        /// <summary>
        /// Gets the number of entries currently queued.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();

                int count = m_Header->Count;
                CheckCountInRange(count, m_Header->Capacity);

                return count;
            }
        }

        /// <summary>
        /// Gets the maximum number of actively queued entries.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated.
        /// </exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_Header->Capacity;
            }
        }

        /// <summary>
        /// Gets the remaining active-entry capacity.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        public int RemainingCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();

                int count = m_Header->Count;
                int capacity = m_Header->Capacity;

                CheckCountInRange(count, capacity);

                return capacity - count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether no entries are queued.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();

                int count = m_Header->Count;
                CheckCountInRange(count, m_Header->Capacity);

                return count == 0;
            }
        }

        /// <summary>
        /// Clears the queue without releasing capacity.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckCreated();
            CheckCountInRange(m_Header->Count, m_Header->Capacity);

            m_Header->Count = 0;
        }

        /// <summary>
        /// Enqueues a cell with the specified priority.
        /// </summary>
        /// <param name="priority">The priority used for ordering. Lower values are dequeued first.</param>
        /// <param name="cellIndex">The non-negative cell index.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated, structurally invalid, or already at fixed capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cellIndex"/> is negative.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(int priority, int cellIndex)
        {
            if (!TryEnqueue(priority, cellIndex))
            {
                throw new InvalidOperationException("UnsafeCellPriorityQueue capacity has been exceeded.");
            }
        }

        /// <summary>
        /// Attempts to enqueue a cell with the specified priority.
        /// </summary>
        /// <param name="priority">The priority used for ordering. Lower values are dequeued first.</param>
        /// <param name="cellIndex">The non-negative cell index.</param>
        /// <returns>
        /// <see langword="true"/> when the item was enqueued; <see langword="false"/> when the fixed capacity is already full.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cellIndex"/> is negative.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(int priority, int cellIndex)
        {
            CheckCreated();
            CheckCellIndex(cellIndex);

            int count = m_Header->Count;
            int capacity = m_Header->Capacity;

            CheckCountInRange(count, capacity);

            if (count >= capacity)
            {
                return false;
            }

            m_Header->Count = count + 1;
            SiftUp(count, new Node(priority, cellIndex));

            return true;
        }

        /// <summary>
        /// Attempts to return the next item without removing it.
        /// </summary>
        /// <param name="item">The returned item when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out UnsafeCellPriorityQueueItem item)
        {
            CheckCreated();

            int count = m_Header->Count;
            int capacity = m_Header->Capacity;

            CheckCountInRange(count, capacity);

            if (count == 0)
            {
                item = default;
                return false;
            }

            Node node = m_Nodes[0];
            item = new UnsafeCellPriorityQueueItem(node.Priority, node.CellIndex);
            return true;
        }

        /// <summary>
        /// Attempts to remove and return the next item.
        /// </summary>
        /// <param name="item">The returned item when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out UnsafeCellPriorityQueueItem item)
        {
            if (TryDequeue(out int priority, out int cellIndex))
            {
                item = new UnsafeCellPriorityQueueItem(priority, cellIndex);
                return true;
            }

            item = default;
            return false;
        }

        /// <summary>
        /// Attempts to remove and return the next item.
        /// </summary>
        /// <param name="priority">The returned priority when available.</param>
        /// <param name="cellIndex">The returned cell index when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out int priority, out int cellIndex)
        {
            CheckCreated();

            int count = m_Header->Count;
            int capacity = m_Header->Capacity;

            CheckCountInRange(count, capacity);

            if (count == 0)
            {
                priority = default;
                cellIndex = default;
                return false;
            }

            Node root = m_Nodes[0];

            count--;
            m_Header->Count = count;

            if (count > 0)
            {
                SiftDown(0, m_Nodes[count]);
            }

            priority = root.Priority;
            cellIndex = root.CellIndex;
            return true;
        }

        /// <summary>
        /// Releases all unmanaged memory owned by this queue value.
        /// </summary>
        /// <remarks>
        /// Because this is an unsafe core, disposing one value does not invalidate copied values.
        /// A native wrapper or scheduler-owned scratch lease is responsible for safety-handle invalidation
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
            m_Nodes = null;
            m_AllocatorLabel = Allocator.Invalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftUp(int index, Node node)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) >> 1;
                Node parent = m_Nodes[parentIndex];

                if (!Less(node, parent))
                {
                    break;
                }

                m_Nodes[index] = parent;
                index = parentIndex;
            }

            m_Nodes[index] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftDown(int index, Node node)
        {
            int half = m_Header->Count >> 1;

            while (index < half)
            {
                int leftIndex = (index << 1) + 1;
                int rightIndex = leftIndex + 1;

                int childIndex = leftIndex;
                Node child = m_Nodes[leftIndex];

                if (rightIndex < m_Header->Count)
                {
                    Node right = m_Nodes[rightIndex];

                    if (Less(right, child))
                    {
                        childIndex = rightIndex;
                        child = right;
                    }
                }

                if (!Less(child, node))
                {
                    break;
                }

                m_Nodes[index] = child;
                index = childIndex;
            }

            m_Nodes[index] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Less(Node left, Node right)
        {
            if (left.Priority != right.Priority)
            {
                return left.Priority < right.Priority;
            }

            return left.CellIndex < right.CellIndex;
        }

        private void FreeAllocatedMemory()
        {
            if (m_Nodes != null)
            {
                UnsafeUtility.FreeTracked(m_Nodes, m_AllocatorLabel);
            }

            if (m_Header != null)
            {
                UnsafeUtility.FreeTracked(m_Header, m_AllocatorLabel);
            }
        }

        private static void ValidateConstructorArguments(int capacity, Allocator allocator)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "UnsafeCellPriorityQueue capacity must be greater than zero.");
            }

            if (allocator <= Allocator.None)
            {
                throw new ArgumentException(
                    "UnsafeCellPriorityQueue allocator must be Temp, TempJob, Persistent, or a valid allocator.",
                    nameof(allocator));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            if (m_Header == null || m_Nodes == null)
            {
                throw new InvalidOperationException("UnsafeCellPriorityQueue is not created.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCellIndex(int cellIndex)
        {
            if (cellIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellIndex),
                    cellIndex,
                    "UnsafeCellPriorityQueue cell index must be greater than or equal to zero.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCountInRange(int count, int capacity)
        {
            if (count < 0 || count > capacity)
            {
                throw new InvalidOperationException("UnsafeCellPriorityQueue count is outside the valid range.");
            }
        }

        private static long GetAllocationSize<T>(int count)
            where T : struct
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "UnsafeCellPriorityQueue allocation count must be greater than zero.");
            }

            long byteCount = checked((long)UnsafeUtility.SizeOf<T>() * count);

            if (byteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "UnsafeCellPriorityQueue allocation size must be greater than zero.");
            }

            return byteCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public int Capacity;
            public int Count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct Node
        {
            public readonly int Priority;
            public readonly int CellIndex;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node(int priority, int cellIndex)
            {
                Priority = priority;
                CellIndex = cellIndex;
            }
        }
    }

    /// <summary>
    /// Value returned by <see cref="UnsafeCellPriorityQueue"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct UnsafeCellPriorityQueueItem : IEquatable<UnsafeCellPriorityQueueItem>
    {
        /// <summary>
        /// The priority value. Lower values are dequeued first.
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// The grid cell index.
        /// </summary>
        public readonly int CellIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeCellPriorityQueueItem"/> struct.
        /// </summary>
        /// <param name="priority">The priority value.</param>
        /// <param name="cellIndex">The grid cell index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeCellPriorityQueueItem(int priority, int cellIndex)
        {
            Priority = priority;
            CellIndex = cellIndex;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UnsafeCellPriorityQueueItem other)
        {
            return Priority == other.Priority && CellIndex == other.CellIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is UnsafeCellPriorityQueueItem other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Priority * 397) ^ CellIndex;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Priority=" + Priority + ", CellIndex=" + CellIndex;
        }

        /// <summary>
        /// Determines whether two priority-queue items are equal.
        /// </summary>
        public static bool operator ==(UnsafeCellPriorityQueueItem left, UnsafeCellPriorityQueueItem right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two priority-queue items are not equal.
        /// </summary>
        public static bool operator !=(UnsafeCellPriorityQueueItem left, UnsafeCellPriorityQueueItem right)
        {
            return !left.Equals(right);
        }
    }
}