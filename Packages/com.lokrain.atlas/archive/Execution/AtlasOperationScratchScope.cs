// Packages/com.lokrain.atlas/Runtime/Execution/AtlasOperationScratchScope.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Own multiple operation-scratch allocations for one scheduler execution.
// - Allocate scratch NativeArray leases through a shared allocator policy.
// - Dispose all owned scratch immediately on failure paths.
// - Chain all owned scratch disposal into a returned JobHandle on scheduled success paths.
//
// Design notes
// - A scope is operation-local and should not cross operation boundaries.
// - A scope is not a workspace and does not allocate fields.
// - A scheduler should return the JobHandle produced by Dispose(JobHandle).
// - Jobs must not receive this scope; they receive resolved NativeArray values.

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Owns operation-local scratch allocations for one scheduler invocation.
    /// </summary>
    public sealed class AtlasOperationScratchScope : IDisposable
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private readonly List<IDisposableScratchAllocation> _allocations;
        private byte _state;

        /// <summary>
        /// Creates a scratch scope using a job-safe allocator.
        /// </summary>
        public AtlasOperationScratchScope(Allocator allocator)
            : this(new AtlasOperationScratchAllocator(allocator))
        {
        }

        /// <summary>
        /// Creates a scratch scope using an explicit operation scratch allocator.
        /// </summary>
        public AtlasOperationScratchScope(AtlasOperationScratchAllocator allocator)
        {
            Allocator = allocator;
            _allocations = new List<IDisposableScratchAllocation>();
            _state = AliveState;
        }

        /// <summary>
        /// Gets the allocator used by this scope.
        /// </summary>
        public AtlasOperationScratchAllocator Allocator { get; }

        /// <summary>
        /// Gets whether this scratch scope has been disposed or scheduled for disposal.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Gets the number of scratch allocations owned by this scope.
        /// </summary>
        public int AllocationCount => _allocations.Count;

        /// <summary>
        /// Allocates an operation-local scratch NativeArray and registers it with this scope.
        /// </summary>
        public AtlasOperationScratchArray<TElement> AllocateNativeArray<TElement>(
            int length,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
            where TElement : unmanaged
        {
            ThrowIfDisposed();

            var allocation = Allocator.AllocateNativeArray<TElement>(
                length,
                options);

            _allocations.Add(
                new DisposableScratchAllocation<TElement>(allocation));

            return allocation;
        }

        /// <summary>
        /// Schedules disposal of all owned scratch after the supplied dependency and returns the final disposal dependency.
        /// </summary>
        public JobHandle Dispose(JobHandle dependency)
        {
            if (_state == DisposedState)
            {
                return dependency;
            }

            var finalDependency = dependency;

            for (var i = _allocations.Count - 1; i >= 0; i--)
            {
                finalDependency = _allocations[i].Dispose(finalDependency);
            }

            _allocations.Clear();
            _state = DisposedState;
            GC.SuppressFinalize(this);

            return finalDependency;
        }

        /// <summary>
        /// Disposes all owned scratch immediately. Use only when no scheduled job can still access it.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            for (var i = _allocations.Count - 1; i >= 0; i--)
            {
                _allocations[i].Dispose();
            }

            _allocations.Clear();
            _state = DisposedState;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throws when this scope has been disposed or scheduled for disposal.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state != DisposedState)
            {
                return;
            }

            throw new ObjectDisposedException(
                nameof(AtlasOperationScratchScope),
                "Atlas operation scratch scope has been disposed or scheduled for dependency-aware disposal.");
        }

        private interface IDisposableScratchAllocation : IDisposable
        {
            JobHandle Dispose(JobHandle dependency);
        }

        private readonly struct DisposableScratchAllocation<TElement> : IDisposableScratchAllocation
            where TElement : unmanaged
        {
            private readonly AtlasOperationScratchArray<TElement> _allocation;

            public DisposableScratchAllocation(
                AtlasOperationScratchArray<TElement> allocation)
            {
                _allocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
            }

            public JobHandle Dispose(JobHandle dependency)
            {
                return _allocation.Dispose(dependency);
            }

            public void Dispose()
            {
                _allocation.Dispose();
            }
        }
    }
}
