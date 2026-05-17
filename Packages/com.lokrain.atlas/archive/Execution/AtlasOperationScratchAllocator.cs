// Packages/com.lokrain.atlas/Runtime/Execution/AtlasOperationScratchAllocator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Provide a small operation-scratch allocation boundary for schedulers and executors.
// - Allocate operation-private NativeArray scratch leases.
// - Enforce job-safe allocator policy for scratch that may be consumed by scheduled jobs.
// - Keep scratch allocation separate from workspace field storage and artifact capture.
//
// Design notes
// - This type allocates scratch, not Atlas fields.
// - This type does not know StableDataId, operation catalogs, compiled bindings, or artifacts.
// - Operation schedulers own when scratch is allocated and when disposal is chained.
// - Jobs receive NativeArray values, never this allocator.

using System;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Allocates operation-local scratch native containers for operation schedulers.
    /// </summary>
    public readonly struct AtlasOperationScratchAllocator
    {
        /// <summary>
        /// Allocator used for scratch native containers.
        /// </summary>
        public readonly Allocator Allocator;

        /// <summary>
        /// Creates a scratch allocator using a job-safe Unity allocator.
        /// </summary>
        public AtlasOperationScratchAllocator(Allocator allocator)
        {
            ValidateScratchAllocatorOrThrow(
                allocator,
                nameof(allocator));

            Allocator = allocator;
        }

        /// <summary>
        /// Creates a scratch allocator backed by <see cref="Allocator.TempJob"/>.
        /// </summary>
        public static AtlasOperationScratchAllocator TempJob =>
            new(Allocator.TempJob);

        /// <summary>
        /// Creates a scratch allocator backed by <see cref="Allocator.Persistent"/>.
        /// </summary>
        public static AtlasOperationScratchAllocator Persistent =>
            new(Allocator.Persistent);

        /// <summary>
        /// Allocates an operation-local scratch NativeArray lease.
        /// </summary>
        public AtlasOperationScratchArray<TElement> AllocateNativeArray<TElement>(
            int length,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
            where TElement : unmanaged
        {
            return new AtlasOperationScratchArray<TElement>(
                length,
                Allocator,
                options);
        }

        /// <summary>
        /// Creates a disposable scratch scope using this allocator.
        /// </summary>
        public AtlasOperationScratchScope CreateScope()
        {
            return new AtlasOperationScratchScope(this);
        }

        /// <summary>
        /// Validates that the allocator is valid for operation scratch used by scheduled jobs.
        /// </summary>
        internal static void ValidateScratchAllocatorOrThrow(
            Allocator allocator,
            string parameterName)
        {
            if (allocator == Allocator.TempJob ||
                allocator == Allocator.Persistent)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                parameterName,
                allocator,
                "Atlas operation scratch requires Allocator.TempJob or Allocator.Persistent because scratch may be used by scheduled jobs.");
        }
    }
}
