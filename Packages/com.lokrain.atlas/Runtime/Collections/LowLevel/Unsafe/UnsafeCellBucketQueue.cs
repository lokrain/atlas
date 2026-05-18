#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Internal unsafe deterministic fixed-capacity bucket queue specialized for bounded-priority cell propagation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type owns unmanaged memory directly. It is deliberately not marked as a Unity
    /// <c>NativeContainer</c>. A separate native wrapper or scheduler-owned scratch lease must own Unity safety handles
    /// and dependency-aware disposal before this crosses Unity job boundaries as an owning container.
    /// </para>
    /// <para>
    /// The queue stores priorities as direct bucket indices. It is efficient when priorities are bounded,
    /// such as distances, normalized cost layers, water/coast expansion depths, and frontier waves.
    /// </para>
    /// <para>
    /// Once dequeue has advanced to a priority, later enqueues must not target a lower priority. This is
    /// the monotonic queue invariant. Call <see cref="Clear()"/> or <see cref="Clear(int)"/> to start a
    /// new propagation pass.
    /// </para>
    /// <para>
    /// Dequeue order is deterministic: ascending priority, then FIFO within each priority bucket.
    /// Canonical output therefore requires canonical enqueue order for equal-priority entries.
    /// </para>
    /// <para>
    /// Public methods validate all pointer-relevant structural state before mutation. A failed operation
    /// must not leave the queue partially mutated.
    /// </para>
    /// <para>
    /// This is a single-writer unsafe core. It is not thread-safe and does not provide a parallel writer.
    /// Duplicate cell indices are allowed.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UnsafeCellBucketQueue : IDisposable
    {
        private const int InvalidIndex = -1;
        private const string TypeName = nameof(UnsafeCellBucketQueue);

        [NativeDisableUnsafePtrRestriction]
        private Header* m_Header;

        [NativeDisableUnsafePtrRestriction]
        private int* m_Heads;

        [NativeDisableUnsafePtrRestriction]
        private int* m_Tails;

        [NativeDisableUnsafePtrRestriction]
        private Node* m_Nodes;

        private Allocator m_AllocatorLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeCellBucketQueue"/> struct.
        /// </summary>
        /// <param name="bucketCount">The number of priority buckets. Valid priorities are in the range [0, bucketCount).</param>
        /// <param name="capacity">The maximum number of queued entries.</param>
        /// <param name="allocator">The allocator used for unmanaged storage.</param>
        public UnsafeCellBucketQueue(int bucketCount, int capacity, Allocator allocator)
            : this(bucketCount, capacity, allocator, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeCellBucketQueue"/> struct.
        /// </summary>
        /// <param name="bucketCount">The number of priority buckets. Valid priorities are in the range [0, bucketCount).</param>
        /// <param name="capacity">The maximum number of queued entries.</param>
        /// <param name="allocator">The allocator used for unmanaged storage.</param>
        /// <param name="startingPriority">The initial monotonic priority cursor.</param>
        public UnsafeCellBucketQueue(int bucketCount, int capacity, Allocator allocator, int startingPriority)
        {
            ValidateConstructorArguments(bucketCount, capacity, allocator, startingPriority);

            m_Header = null;
            m_Heads = null;
            m_Tails = null;
            m_Nodes = null;
            m_AllocatorLabel = allocator;

            try
            {
                m_Header = (Header*)UnsafeUtility.MallocTracked(
                    UnsafeUtility.SizeOf<Header>(),
                    UnsafeUtility.AlignOf<Header>(),
                    allocator,
                    1);

                m_Heads = (int*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<int>(bucketCount, nameof(bucketCount)),
                    UnsafeUtility.AlignOf<int>(),
                    allocator,
                    1);

                m_Tails = (int*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<int>(bucketCount, nameof(bucketCount)),
                    UnsafeUtility.AlignOf<int>(),
                    allocator,
                    1);

                m_Nodes = (Node*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<Node>(capacity, nameof(capacity)),
                    UnsafeUtility.AlignOf<Node>(),
                    allocator,
                    1);

                m_Header->BucketCount = bucketCount;
                m_Header->Capacity = capacity;
                m_Header->Count = 0;
                m_Header->CurrentPriority = startingPriority;
                m_Header->FreeHead = InvalidIndex;

                InitializeEmptyStorage(startingPriority);
            }
            catch
            {
                FreeAllocatedMemory();

                m_Header = null;
                m_Heads = null;
                m_Tails = null;
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
            get => m_Header != null && m_Heads != null && m_Tails != null && m_Nodes != null;
        }

        /// <summary>
        /// Gets the number of entries currently queued.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->Count;
            }
        }

        /// <summary>
        /// Gets the maximum number of actively queued entries.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->Capacity;
            }
        }

        /// <summary>
        /// Gets the number of priority buckets.
        /// </summary>
        public int BucketCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->BucketCount;
            }
        }

        /// <summary>
        /// Gets the current monotonic priority cursor.
        /// </summary>
        public int CurrentPriority
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->CurrentPriority;
            }
        }

        /// <summary>
        /// Gets the remaining active-entry capacity.
        /// </summary>
        public int RemainingCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->Capacity - m_Header->Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether no entries are queued.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->Count == 0;
            }
        }

        /// <summary>
        /// Clears the queue and resets the monotonic priority cursor to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Clear(0);
        }

        /// <summary>
        /// Clears the queue and sets the starting monotonic priority cursor.
        /// </summary>
        /// <param name="startingPriority">The starting monotonic priority cursor.</param>
        public void Clear(int startingPriority)
        {
            CheckCreated();
            CheckHeaderDimensionsAndPointers();
            CheckPriorityInRange(startingPriority, m_Header->BucketCount, nameof(startingPriority));

            InitializeEmptyStorage(startingPriority);
        }

        /// <summary>
        /// Enqueues a cell with the specified priority.
        /// </summary>
        /// <param name="priority">The priority bucket. Lower priorities are dequeued first.</param>
        /// <param name="cellIndex">The non-negative cell index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(int priority, int cellIndex)
        {
            if (!TryEnqueue(priority, cellIndex))
            {
                throw new InvalidOperationException(TypeName + " capacity exceeded.");
            }
        }

        /// <summary>
        /// Attempts to enqueue a cell with the specified priority.
        /// </summary>
        /// <param name="priority">The priority bucket. Lower priorities are dequeued first.</param>
        /// <param name="cellIndex">The non-negative cell index.</param>
        /// <returns>
        /// <see langword="true"/> when the item was enqueued; <see langword="false"/> when the fixed capacity is already full.
        /// </returns>
        /// <remarks>
        /// Invalid priority, invalid cell index, uncreated storage, corrupt state, and monotonic-priority violations
        /// remain deterministic failures. Only normal capacity exhaustion returns <see langword="false"/>.
        /// </remarks>
        public bool TryEnqueue(int priority, int cellIndex)
        {
            CheckCreated();
            CheckHeaderState();
            CheckPriorityInRange(priority, m_Header->BucketCount, nameof(priority));
            UnsafeCollectionChecks.CheckNonNegativeCellIndex(cellIndex);
            UnsafeCollectionChecks.CheckMonotonicPriority(priority, m_Header->CurrentPriority, TypeName);

            int count = m_Header->Count;
            int capacity = m_Header->Capacity;

            if (count >= capacity)
            {
                return false;
            }

            CheckBucketForAppend(priority);
            CheckFreeHeadState(count, capacity, m_Header->FreeHead);

            int head = m_Heads[priority];
            int tail = m_Tails[priority];
            int nodeIndex = m_Header->FreeHead;
            int nextFreeHead = m_Nodes[nodeIndex].Next;

            m_Nodes[nodeIndex].CellIndex = cellIndex;
            m_Nodes[nodeIndex].Next = InvalidIndex;

            if (head == InvalidIndex)
            {
                m_Heads[priority] = nodeIndex;
                m_Tails[priority] = nodeIndex;
            }
            else
            {
                m_Nodes[tail].Next = nodeIndex;
                m_Tails[priority] = nodeIndex;
            }

            m_Header->FreeHead = nextFreeHead;
            m_Header->Count = count + 1;

            return true;
        }

        /// <summary>
        /// Attempts to return the next item without removing it.
        /// </summary>
        /// <param name="item">The returned item when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        public bool TryPeek(out UnsafeCellBucketQueueItem item)
        {
            CheckCreated();
            CheckHeaderState();

            if (m_Header->Count == 0)
            {
                item = default;
                return false;
            }

            int priority = FindNextOccupiedPriority(m_Header->CurrentPriority);

            if (priority == InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                item = default;
                return false;
            }

            CheckBucketEndpoints(priority);

            int nodeIndex = m_Heads[priority];
            CheckActiveHeadNode(priority, nodeIndex);

            Node node = m_Nodes[nodeIndex];
            item = new UnsafeCellBucketQueueItem(priority, node.CellIndex);
            return true;
        }

        /// <summary>
        /// Attempts to remove and return the next item.
        /// </summary>
        /// <param name="item">The returned item when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        public bool TryDequeue(out UnsafeCellBucketQueueItem item)
        {
            if (TryDequeue(out int priority, out int cellIndex))
            {
                item = new UnsafeCellBucketQueueItem(priority, cellIndex);
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
        public bool TryDequeue(out int priority, out int cellIndex)
        {
            CheckCreated();
            CheckHeaderState();

            int count = m_Header->Count;
            int capacity = m_Header->Capacity;

            if (count == 0)
            {
                priority = default;
                cellIndex = default;
                return false;
            }

            priority = FindNextOccupiedPriority(m_Header->CurrentPriority);

            if (priority == InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                cellIndex = default;
                return false;
            }

            CheckBucketEndpoints(priority);

            int nodeIndex = m_Heads[priority];
            CheckActiveHeadNode(priority, nodeIndex);

            Node node = m_Nodes[nodeIndex];
            int nextNodeIndex = node.Next;
            int tail = m_Tails[priority];

            if (nextNodeIndex == InvalidIndex)
            {
                if (tail != nodeIndex)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }
            }
            else
            {
                if (tail == nodeIndex)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                CheckActiveLinkedNode(nextNodeIndex);
            }

            int oldFreeHead = m_Header->FreeHead;
            CheckFreeHeadState(count, capacity, oldFreeHead);

            m_Heads[priority] = nextNodeIndex;

            if (nextNodeIndex == InvalidIndex)
            {
                m_Tails[priority] = InvalidIndex;
            }

            m_Nodes[nodeIndex].CellIndex = InvalidIndex;
            m_Nodes[nodeIndex].Next = oldFreeHead;

            m_Header->FreeHead = nodeIndex;
            m_Header->Count = count - 1;
            m_Header->CurrentPriority = priority;

            cellIndex = node.CellIndex;
            return true;
        }

        /// <summary>
        /// Releases all unmanaged memory owned by this queue value.
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
            m_Heads = null;
            m_Tails = null;
            m_Nodes = null;
            m_AllocatorLabel = Allocator.Invalid;
        }

        private void InitializeEmptyStorage(int startingPriority)
        {
            CheckHeaderDimensionsAndPointers();
            CheckPriorityInRange(startingPriority, m_Header->BucketCount, nameof(startingPriority));

            UnsafeUtility.MemSet(
                m_Heads,
                byte.MaxValue,
                UnsafeCollectionSize.CheckedPositiveElementBytes<int>(m_Header->BucketCount, nameof(BucketCount)));

            UnsafeUtility.MemSet(
                m_Tails,
                byte.MaxValue,
                UnsafeCollectionSize.CheckedPositiveElementBytes<int>(m_Header->BucketCount, nameof(BucketCount)));

            for (int nodeIndex = 0; nodeIndex < m_Header->Capacity - 1; nodeIndex++)
            {
                m_Nodes[nodeIndex].CellIndex = InvalidIndex;
                m_Nodes[nodeIndex].Next = nodeIndex + 1;
            }

            m_Nodes[m_Header->Capacity - 1].CellIndex = InvalidIndex;
            m_Nodes[m_Header->Capacity - 1].Next = InvalidIndex;

            m_Header->Count = 0;
            m_Header->CurrentPriority = startingPriority;
            m_Header->FreeHead = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindNextOccupiedPriority(int startPriority)
        {
            CheckPriorityInRange(startPriority, m_Header->BucketCount, nameof(startPriority));

            for (int priority = startPriority; priority < m_Header->BucketCount; priority++)
            {
                if (m_Heads[priority] != InvalidIndex)
                {
                    return priority;
                }
            }

            return InvalidIndex;
        }

        private void FreeAllocatedMemory()
        {
            if (m_Nodes != null)
            {
                UnsafeUtility.FreeTracked(m_Nodes, m_AllocatorLabel);
            }

            if (m_Tails != null)
            {
                UnsafeUtility.FreeTracked(m_Tails, m_AllocatorLabel);
            }

            if (m_Heads != null)
            {
                UnsafeUtility.FreeTracked(m_Heads, m_AllocatorLabel);
            }

            if (m_Header != null)
            {
                UnsafeUtility.FreeTracked(m_Header, m_AllocatorLabel);
            }
        }

        private static void ValidateConstructorArguments(
            int bucketCount,
            int capacity,
            Allocator allocator,
            int startingPriority)
        {
            UnsafeCollectionChecks.CheckPositive(bucketCount, nameof(bucketCount));
            UnsafeCollectionChecks.CheckPositive(capacity, nameof(capacity));
            UnsafeCollectionChecks.CheckAllocator(allocator, nameof(allocator));
            CheckPriorityInRange(startingPriority, bucketCount, nameof(startingPriority));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderDimensionsAndPointers()
        {
            if (m_Header == null || m_Heads == null || m_Tails == null || m_Nodes == null)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            UnsafeCollectionChecks.CheckPositiveCapacity(m_Header->BucketCount, TypeName);
            UnsafeCollectionChecks.CheckPositiveCapacity(m_Header->Capacity, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderState()
        {
            CheckHeaderDimensionsAndPointers();
            UnsafeCollectionChecks.CheckCountInCapacity(m_Header->Count, m_Header->Capacity, TypeName);
            CheckCurrentPriorityState(m_Header->CurrentPriority, m_Header->BucketCount);
            CheckFreeHeadState(m_Header->Count, m_Header->Capacity, m_Header->FreeHead);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckPriorityInRange(int priority, int bucketCount, string parameterName)
        {
            UnsafeCollectionChecks.CheckIndexInRange(priority, bucketCount, parameterName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCurrentPriorityState(int currentPriority, int bucketCount)
        {
            if ((uint)currentPriority >= (uint)bucketCount)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNodeIndexInRange(int nodeIndex)
        {
            if ((uint)nodeIndex >= (uint)m_Header->Capacity)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBucketEndpoints(int priority)
        {
            int head = m_Heads[priority];
            int tail = m_Tails[priority];

            if (head == InvalidIndex)
            {
                if (tail != InvalidIndex)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                return;
            }

            if (tail == InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            CheckNodeIndexInRange(head);
            CheckNodeIndexInRange(tail);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBucketForAppend(int priority)
        {
            CheckBucketEndpoints(priority);

            int head = m_Heads[priority];

            if (head == InvalidIndex)
            {
                return;
            }

            CheckActiveHeadNode(priority, head);
            CheckActiveTailNode(m_Tails[priority]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckActiveHeadNode(int priority, int nodeIndex)
        {
            CheckNodeIndexInRange(nodeIndex);

            Node node = m_Nodes[nodeIndex];

            if (node.CellIndex < 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            int tail = m_Tails[priority];

            if (nodeIndex == tail)
            {
                if (node.Next != InvalidIndex)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                return;
            }

            if (node.Next == InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            CheckActiveLinkedNode(node.Next);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckActiveTailNode(int nodeIndex)
        {
            CheckNodeIndexInRange(nodeIndex);

            Node node = m_Nodes[nodeIndex];

            if (node.CellIndex < 0 || node.Next != InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckActiveLinkedNode(int nodeIndex)
        {
            CheckNodeIndexInRange(nodeIndex);

            if (m_Nodes[nodeIndex].CellIndex < 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckFreeHeadState(int count, int capacity, int freeHead)
        {
            int freeCount = capacity - count;

            if (freeCount == 0)
            {
                if (freeHead != InvalidIndex)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                return;
            }

            if (freeHead == InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            CheckFreeNodePayload(freeHead);

            int nextFreeHead = m_Nodes[freeHead].Next;

            if (freeCount == 1)
            {
                if (nextFreeHead != InvalidIndex)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }

                return;
            }

            if (nextFreeHead == InvalidIndex || nextFreeHead == freeHead)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            CheckFreeNodePayload(nextFreeHead);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckFreeNodePayload(int nodeIndex)
        {
            CheckNodeIndexInRange(nodeIndex);

            if (m_Nodes[nodeIndex].CellIndex != InvalidIndex)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public int BucketCount;
            public int Capacity;
            public int Count;
            public int CurrentPriority;
            public int FreeHead;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Node
        {
            public int CellIndex;
            public int Next;
        }
    }

    /// <summary>
    /// Value returned by <see cref="UnsafeCellBucketQueue"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct UnsafeCellBucketQueueItem : IEquatable<UnsafeCellBucketQueueItem>
    {
        /// <summary>
        /// Gets the priority bucket. Lower values are dequeued first.
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// Gets the grid cell index.
        /// </summary>
        public readonly int CellIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeCellBucketQueueItem"/> struct.
        /// </summary>
        /// <param name="priority">The priority bucket.</param>
        /// <param name="cellIndex">The grid cell index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeCellBucketQueueItem(int priority, int cellIndex)
        {
            Priority = priority;
            CellIndex = cellIndex;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UnsafeCellBucketQueueItem other)
        {
            return Priority == other.Priority && CellIndex == other.CellIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is UnsafeCellBucketQueueItem other && Equals(other);
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
            return nameof(Priority) + "=" + Priority + ", " + nameof(CellIndex) + "=" + CellIndex;
        }

        /// <summary>
        /// Determines whether two bucket-queue items are equal.
        /// </summary>
        public static bool operator ==(UnsafeCellBucketQueueItem left, UnsafeCellBucketQueueItem right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two bucket-queue items are not equal.
        /// </summary>
        public static bool operator !=(UnsafeCellBucketQueueItem left, UnsafeCellBucketQueueItem right)
        {
            return !left.Equals(right);
        }
    }
}