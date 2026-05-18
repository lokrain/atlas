#nullable enable

// Packages/com.lokrain.atlas/Runtime/Collections/Native/NativeCellPriorityQueue.cs
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Collections.Native
//
// Purpose:
// Provides a Unity NativeContainer safety wrapper over UnsafeCellPriorityQueue.
//
// Design notes:
// - This is an internal job-facing wrapper, not public Atlas domain API.
// - The unsafe core owns the raw unmanaged heap storage.
// - This wrapper owns Unity job-safety access checks and dependency-aware disposal.
// - Allocator.Temp is rejected because this wrapper is intended to cross scheduled-job boundaries.
// - The queue is single-writer. It has no ParallelWriter.
// - Ordering is deterministic: priority ascending, then cell index ascending.

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
    /// Internal Unity NativeContainer wrapper over <see cref="UnsafeCellPriorityQueue"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper exists for scheduled-job use when the queue must be passed across Unity job boundaries.
    /// Prefer scheduler-owned scratch leases and non-owning job views unless a real job consumer needs a
    /// NativeContainer-shaped value.
    /// </para>
    ///
    /// <para>
    /// The queue is fixed-capacity and single-writer. It does not provide a parallel writer.
    /// Duplicate cell indices are allowed; algorithms should use external finalized/visited/best-priority
    /// fields when stale entries must be ignored.
    /// </para>
    /// </remarks>
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}, Capacity = {" + nameof(Capacity) + "}")]
    internal unsafe struct NativeCellPriorityQueue : IDisposable
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly int StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativeCellPriorityQueue>();

        private AtomicSafetyHandle m_Safety;
#endif

        private UnsafeCellPriorityQueue m_Queue;

        /// <summary>
        /// Creates a fixed-capacity native priority queue.
        /// </summary>
        /// <param name="capacity">Maximum number of queued entries.</param>
        /// <param name="allocator">Allocator used for unmanaged storage. <see cref="Allocator.Temp"/> is rejected.</param>
        public NativeCellPriorityQueue(int capacity, Allocator allocator)
        {
            CheckJobCapableAllocator(allocator);

            m_Queue = default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = default;
#endif

            m_Queue = new UnsafeCellPriorityQueue(capacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            try
            {
                m_Safety = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, StaticSafetyId);
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
        /// Returns true when this wrapper references allocated queue storage.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Queue.IsCreated;
        }

        /// <summary>
        /// Number of entries currently queued.
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
        /// Maximum number of queued entries.
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
        /// Remaining active-entry capacity.
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
        /// Returns true when no entries are queued.
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
        /// Clears the queue without releasing capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            m_Queue.Clear();
        }

        /// <summary>
        /// Adds a cell to the queue.
        /// </summary>
        /// <param name="priority">Priority value. Lower values are dequeued first.</param>
        /// <param name="cellIndex">Grid cell index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(int priority, int cellIndex)
        {
            CheckWrite();
            m_Queue.Enqueue(priority, cellIndex);
        }

        /// <summary>
        /// Attempts to add a cell to the queue.
        /// </summary>
        /// <param name="priority">Priority value. Lower values are dequeued first.</param>
        /// <param name="cellIndex">Grid cell index.</param>
        /// <returns>True when the item was enqueued; false when fixed capacity is already full.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(int priority, int cellIndex)
        {
            CheckWrite();
            return m_Queue.TryEnqueue(priority, cellIndex);
        }

        /// <summary>
        /// Attempts to return the next item without removing it.
        /// </summary>
        /// <param name="item">Returned item when available.</param>
        /// <returns>True when an item was available.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out UnsafeCellPriorityQueueItem item)
        {
            CheckRead();
            return m_Queue.TryPeek(out item);
        }

        /// <summary>
        /// Attempts to remove and return the next item.
        /// </summary>
        /// <param name="item">Returned item when available.</param>
        /// <returns>True when an item was available.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out UnsafeCellPriorityQueueItem item)
        {
            CheckWrite();
            return m_Queue.TryDequeue(out item);
        }

        /// <summary>
        /// Attempts to remove and return the next item.
        /// </summary>
        /// <param name="priority">Returned priority when available.</param>
        /// <param name="cellIndex">Returned cell index when available.</param>
        /// <returns>True when an item was available.</returns>
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
        /// Schedules disposal after <paramref name="inputDeps"/> completes.
        /// </summary>
        /// <param name="inputDeps">Dependency that protects all prior queue users.</param>
        /// <returns>Dependency that includes queue disposal.</returns>
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
                throw new ArgumentException("Allocator.Temp is not valid for job-capable native queue storage.", nameof(allocator));
            }
        }

        private struct DisposeJob : IJob
        {
            public UnsafeCellPriorityQueue Queue;

            public void Execute()
            {
                Queue.Dispose();
            }
        }
    }
}
