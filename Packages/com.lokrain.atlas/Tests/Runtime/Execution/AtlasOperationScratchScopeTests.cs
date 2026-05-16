// Packages/com.lokrain.atlas/Tests/Runtime/Execution/AtlasOperationScratchScopeTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution.Tests
//
// Purpose
// - Verify operation scratch native allocations are not modeled as fields.
// - Verify scratch scopes support immediate failure-path disposal.
// - Verify scratch scopes chain NativeArray disposal through returned JobHandle dependencies.
// - Verify scratch allocator policy rejects non-job-safe allocators.

using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class AtlasOperationScratchScopeTests
    {
        [Test]
        public void ScratchAllocator_RejectsNonJobSafeAllocators()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AtlasOperationScratchAllocator(Allocator.Invalid));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AtlasOperationScratchAllocator(Allocator.None));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AtlasOperationScratchAllocator(Allocator.Temp));
        }

        [Test]
        public void ScratchAllocator_AcceptsTempJobAndPersistentAllocators()
        {
            var tempJob = new AtlasOperationScratchAllocator(Allocator.TempJob);
            var persistent = new AtlasOperationScratchAllocator(Allocator.Persistent);

            Assert.That(tempJob.Allocator, Is.EqualTo(Allocator.TempJob));
            Assert.That(persistent.Allocator, Is.EqualTo(Allocator.Persistent));
        }

        [Test]
        public void AllocateNativeArray_CreatesScratchLeaseAndTracksAllocation()
        {
            using var scope = new AtlasOperationScratchScope(Allocator.TempJob);

            var scratch = scope.AllocateNativeArray<int>(4, NativeArrayOptions.ClearMemory);

            Assert.That(scope.AllocationCount, Is.EqualTo(1));
            Assert.That(scope.IsDisposed, Is.False);
            Assert.That(scratch.Length, Is.EqualTo(4));
            Assert.That(scratch.Allocator, Is.EqualTo(Allocator.TempJob));
            Assert.That(scratch.IsCreated, Is.True);
            Assert.That(scratch.IsDisposed, Is.False);
            Assert.That(scratch.Array.Length, Is.EqualTo(4));
            Assert.That(scratch.Array[0], Is.EqualTo(0));
        }

        [Test]
        public void AllocateNativeArray_RejectsNegativeLength()
        {
            using var scope = new AtlasOperationScratchScope(Allocator.TempJob);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                scope.AllocateNativeArray<int>(-1));
        }

        [Test]
        public void ZeroLengthScratch_IsValidButHasNoCreatedNativeAllocation()
        {
            using var scope = new AtlasOperationScratchScope(Allocator.TempJob);

            var scratch = scope.AllocateNativeArray<int>(0);

            Assert.That(scope.AllocationCount, Is.EqualTo(1));
            Assert.That(scratch.Length, Is.EqualTo(0));
            Assert.That(scratch.IsCreated, Is.False);
            Assert.Throws<InvalidOperationException>(() => scratch.GetRequiredArray());
        }

        [Test]
        public void Dispose_ImmediatelyDisposesOwnedScratch()
        {
            var scope = new AtlasOperationScratchScope(Allocator.TempJob);
            var scratch = scope.AllocateNativeArray<int>(2, NativeArrayOptions.ClearMemory);

            var array = scratch.Array;
            array[0] = 42;
            Assert.That(array[0], Is.EqualTo(42));

            scope.Dispose();

            Assert.That(scope.IsDisposed, Is.True);
            Assert.That(scope.AllocationCount, Is.EqualTo(0));
            Assert.That(scratch.IsDisposed, Is.True);
            Assert.That(scratch.IsCreated, Is.False);
            Assert.Throws<ObjectDisposedException>(() => scratch.GetRequiredArray());
            Assert.Throws<ObjectDisposedException>(() => scope.AllocateNativeArray<int>(1));
        }

        [Test]
        public void DisposeWithDependency_CompletesScratchUsingReturnedDependency()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            var scope = new AtlasOperationScratchScope(Allocator.TempJob);
            var scratch = scope.AllocateNativeArray<int>(1, NativeArrayOptions.ClearMemory);

            var job = new WriteScratchThenMarkerJob
            {
                Scratch = scratch.Array,
                Marker = marker,
                Value = 73
            }.Schedule();

            var finalDependency = scope.Dispose(job);

            Assert.That(scope.IsDisposed, Is.True);
            Assert.That(scope.AllocationCount, Is.EqualTo(0));
            Assert.That(scratch.IsDisposed, Is.True);
            Assert.Throws<ObjectDisposedException>(() => scratch.GetRequiredArray());

            finalDependency.Complete();

            Assert.That(marker[0], Is.EqualTo(73));
        }

        [Test]
        public void DisposeWithDependency_NoAllocationsStillPreservesInputDependency()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            var scope = new AtlasOperationScratchScope(Allocator.TempJob);

            var job = new WriteMarkerJob
            {
                Marker = marker,
                Value = 19
            }.Schedule();

            var finalDependency = scope.Dispose(job);
            finalDependency.Complete();

            Assert.That(scope.IsDisposed, Is.True);
            Assert.That(marker[0], Is.EqualTo(19));
        }

        private struct WriteScratchThenMarkerJob : IJob
        {
            public NativeArray<int> Scratch;
            public NativeArray<int> Marker;
            public int Value;

            public void Execute()
            {
                Scratch[0] = Value;
                Marker[0] = Scratch[0];
            }
        }

        private struct WriteMarkerJob : IJob
        {
            public NativeArray<int> Marker;
            public int Value;

            public void Execute()
            {
                Marker[0] = Value;
            }
        }
    }
}
