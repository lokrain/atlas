#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Lokrain.Atlas.Collections.Native
{
    /// <summary>
    /// Internal Unity <c>NativeContainer</c> wrapper over <see cref="UnsafeCellBucketQueue"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper exists for scheduled-job use when the bucket queue must cross Unity job boundaries
    /// as an owned native container. Prefer scheduler-owned scratch leases and non-owning job views unless
    /// a real job consumer needs a <c>NativeContainer</c>-shaped value.
    /// </para>
    /// <para>
    /// The queue is fixed-capacity and single-writer. It does not provide a parallel writer.
    /// Duplicate cell indices are allowed.
    /// </para>
    /// <para>
    /// Priorities are direct bucket indices in the range [0, BucketCount). Dequeue order is deterministic:
    /// ascending priority, then FIFO within each priority bucket. Canonical output requires canonical
    /// enqueue order for equal-priority entries.
    /// </para>
    /// <para>
    /// Once dequeue has advanced to a priority, later enqueues must not target a lower priority. This is
    /// the monotonic queue invariant. Call <see cref="Clear()"/> or <see cref="Clear(int)"/> to start a
    /// new propagation pass.
    /// </para>
    /// <para>
    /// This type owns the unsafe queue storage. Because it is a struct, copied values can still reference
    /// the same storage. Unity safety handles catch many invalid uses when collection checks are enabled;
    /// scheduler-owned scratch should still be the preferred production ownership model.
    /// </para>
    /// </remarks>
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}, Capacity = {" + nameof(Capacity) + "}, BucketCount = {" + nameof(BucketCount) + "}")]
    internal unsafe struct NativeCellBucketQueue : IDisposable
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly int StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativeCellBucketQueue>();

        private AtomicSafetyHandle m_Safety;
#endif

        private UnsafeCellBucketQueue m_Queue;

        /// <summary>
        /// Initializes a new fixed-capacity native bucket queue with a starting priority of zero.
        /// </summary>
        /// <param name="bucketCount">The number of priority buckets. Valid priorities are in the range [0, bucketCount).</param>
        /// <param name="capacity">The maximum number of queued entries.</param>
        /// <param name="allocator">The allocator used for unmanaged storage. <see cref="Allocator.Temp"/> is rejected.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is invalid for job-capable native queue storage.
        /// </exception>
        public NativeCellBucketQueue(int bucketCount, int capacity, Allocator allocator)
            : this(bucketCount, capacity, allocator, startingPriority: 0)
        {
        }

        /// <summary>
        /// Initializes a new fixed-capacity native bucket queue with an explicit starting priority.
        /// </summary>
        /// <param name="bucketCount">The number of priority buckets. Valid priorities are in the range [0, bucketCount).</param>
        /// <param name="capacity">The maximum number of queued entries.</param>
        /// <param name="allocator">The allocator used for unmanaged storage. <see cref="Allocator.Temp"/> is rejected.</param>
        /// <param name="startingPriority">The initial monotonic priority cursor.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is invalid for job-capable native queue storage.
        /// </exception>
        public NativeCellBucketQueue(int bucketCount, int capacity, Allocator allocator, int startingPriority)
        {
            CheckJobCapableAllocator(allocator);

            m_Queue = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = default;
#endif

            m_Queue = new UnsafeCellBucketQueue(bucketCount, capacity, allocator, startingPriority);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            try
            {
                m_Safety = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, StaticSafetyId);
                AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
            }
            catch
            {
                m_Queue.Dispose();
                m_Queue = default;
                throw;
            }
#endif
        }

        /// <summary>
        /// Gets a value indicating whether this value references allocated queue storage.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Queue.IsCreated;
        }

        /// <summary>
        /// Gets the number of entries currently queued.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return m_Queue.Count;
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
                CheckRead();
                return m_Queue.Capacity;
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
                CheckRead();
                return m_Queue.BucketCount;
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
                CheckRead();
                return m_Queue.CurrentPriority;
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
                CheckRead();
                return m_Queue.RemainingCapacity;
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
                CheckRead();
                return m_Queue.IsEmpty;
            }
        }

        /// <summary>
        /// Clears the queue and resets the monotonic priority cursor to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            m_Queue.Clear();
        }

        /// <summary>
        /// Clears the queue and sets the starting monotonic priority cursor.
        /// </summary>
        /// <param name="startingPriority">The starting monotonic priority cursor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int startingPriority)
        {
            CheckWrite();
            m_Queue.Clear(startingPriority);
        }

        /// <summary>
        /// Enqueues a cell with the specified priority.
        /// </summary>
        /// <param name="priority">The priority bucket. Lower values are dequeued first.</param>
        /// <param name="cellIndex">The non-negative cell index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(int priority, int cellIndex)
        {
            CheckWrite();
            m_Queue.Enqueue(priority, cellIndex);
        }

        /// <summary>
        /// Attempts to enqueue a cell with the specified priority.
        /// </summary>
        /// <param name="priority">The priority bucket. Lower values are dequeued first.</param>
        /// <param name="cellIndex">The non-negative cell index.</param>
        /// <returns>
        /// <see langword="true"/> when the item was enqueued; <see langword="false"/> when the fixed capacity is already full.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(int priority, int cellIndex)
        {
            CheckWrite();
            return m_Queue.TryEnqueue(priority, cellIndex);
        }

        /// <summary>
        /// Attempts to return the next item without removing it.
        /// </summary>
        /// <param name="item">The returned item when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out NativeCellBucketQueueItem item)
        {
            CheckRead();

            if (m_Queue.TryPeek(out UnsafeCellBucketQueueItem unsafeItem))
            {
                item = new NativeCellBucketQueueItem(unsafeItem.Priority, unsafeItem.CellIndex);
                return true;
            }

            item = default;
            return false;
        }

        /// <summary>
        /// Attempts to remove and return the next item.
        /// </summary>
        /// <param name="item">The returned item when available.</param>
        /// <returns><see langword="true"/> when an item was available; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out NativeCellBucketQueueItem item)
        {
            if (TryDequeue(out int priority, out int cellIndex))
            {
                item = new NativeCellBucketQueueItem(priority, cellIndex);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out int priority, out int cellIndex)
        {
            CheckWrite();
            return m_Queue.TryDequeue(out priority, out cellIndex);
        }

        /// <summary>
        /// Releases all unmanaged memory immediately.
        /// </summary>
        public void Dispose()
        {
            if (!m_Queue.IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
            m_Safety = default;
#endif

            m_Queue.Dispose();
            m_Queue = default;
        }

        /// <summary>
        /// Schedules release of all unmanaged memory after the supplied dependency completes.
        /// </summary>
        /// <param name="inputDeps">The dependency that protects all prior queue users.</param>
        /// <returns>A dependency that includes queue disposal.</returns>
        /// <remarks>
        /// After calling this method, this queue value must not be used again.
        /// </remarks>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!m_Queue.IsCreated)
            {
                return inputDeps;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
            m_Safety = default;
#endif

            var queue = m_Queue;
            m_Queue = default;

            return new DisposeJob
            {
                Queue = queue
            }.Schedule(inputDeps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        private static void CheckJobCapableAllocator(Allocator allocator)
        {
            UnsafeCollectionChecks.CheckAllocator(allocator, nameof(allocator));

            if (allocator == Allocator.Temp)
            {
                throw new ArgumentException(
                    "Allocator.Temp is not valid for job-capable native queue storage.",
                    nameof(allocator));
            }
        }

        private struct DisposeJob : IJob
        {
            public UnsafeCellBucketQueue Queue;

            public void Execute()
            {
                Queue.Dispose();
            }
        }
    }

    /// <summary>
    /// Value returned by <see cref="NativeCellBucketQueue"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeCellBucketQueueItem : IEquatable<NativeCellBucketQueueItem>
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
        /// Initializes a new instance of the <see cref="NativeCellBucketQueueItem"/> struct.
        /// </summary>
        /// <param name="priority">The priority bucket.</param>
        /// <param name="cellIndex">The grid cell index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeCellBucketQueueItem(int priority, int cellIndex)
        {
            Priority = priority;
            CellIndex = cellIndex;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(NativeCellBucketQueueItem other)
        {
            return Priority == other.Priority && CellIndex == other.CellIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is NativeCellBucketQueueItem other && Equals(other);
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
        public static bool operator ==(NativeCellBucketQueueItem left, NativeCellBucketQueueItem right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two bucket-queue items are not equal.
        /// </summary>
        public static bool operator !=(NativeCellBucketQueueItem left, NativeCellBucketQueueItem right)
        {
            return !left.Equals(right);
        }
    }
}