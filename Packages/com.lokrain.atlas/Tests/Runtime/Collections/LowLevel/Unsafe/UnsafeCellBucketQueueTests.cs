#nullable enable

using System;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe.Tests
{
    public sealed unsafe class UnsafeCellBucketQueueTests
    {
        [Test]
        public void Constructor_WithZeroBucketCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellBucketQueue(0, 1, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithNegativeBucketCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellBucketQueue(-1, 1, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellBucketQueue(1, 0, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellBucketQueue(1, -1, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithAllocatorNone_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => _ = new UnsafeCellBucketQueue(1, 1, Allocator.None));
        }

        [Test]
        public void Constructor_WithNegativeStartingPriority_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellBucketQueue(4, 1, Allocator.Persistent, -1));
        }

        [Test]
        public void Constructor_WithStartingPriorityEqualToBucketCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellBucketQueue(4, 1, Allocator.Persistent, 4));
        }

        [Test]
        public void Default_IsCreated_ReturnsFalse()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.That(queue.IsCreated, Is.False);
        }

        [Test]
        public void Count_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.Count);
        }

        [Test]
        public void Capacity_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.Capacity);
        }

        [Test]
        public void BucketCount_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.BucketCount);
        }

        [Test]
        public void CurrentPriority_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.CurrentPriority);
        }

        [Test]
        public void RemainingCapacity_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.RemainingCapacity);
        }

        [Test]
        public void IsEmpty_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.IsEmpty);
        }

        [Test]
        public void Clear_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.Clear());
        }

        [Test]
        public void Clear_WithStartingPriority_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.Clear(0));
        }

        [Test]
        public void Enqueue_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(0, 10));
        }

        [Test]
        public void TryEnqueue_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryEnqueue(0, 10));
        }

        [Test]
        public void TryPeek_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryPeek(out _));
        }

        [Test]
        public void TryDequeue_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryDequeue(out _));
        }

        [Test]
        public void TryDequeue_WithPriorityAndCellIndex_WhenDefault_Throws()
        {
            UnsafeCellBucketQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryDequeue(out _, out _));
        }

        [Test]
        public void Constructor_WithValidArguments_CreatesEmptyQueue()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                Assert.That(queue.IsCreated, Is.True);
                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.Capacity, Is.EqualTo(3));
                Assert.That(queue.BucketCount, Is.EqualTo(5));
                Assert.That(queue.CurrentPriority, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));
                Assert.That(queue.IsEmpty, Is.True);
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Constructor_WithStartingPriority_SetsCurrentPriority()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3, startingPriority: 2);

            try
            {
                Assert.That(queue.CurrentPriority, Is.EqualTo(2));
                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WithNegativePriority_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 4, capacity: 2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.Enqueue(-1, 10));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WithPriorityEqualToBucketCount_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 4, capacity: 2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.Enqueue(4, 10));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WithNegativePriority_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 4, capacity: 2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.TryEnqueue(-1, 10));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WithPriorityEqualToBucketCount_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 4, capacity: 2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.TryEnqueue(4, 10));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WithNegativeCellIndex_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 4, capacity: 2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.Enqueue(1, -1));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WithNegativeCellIndex_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 4, capacity: 2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.TryEnqueue(1, -1));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WhenAtCapacity_ThrowsAndDoesNotMutateExistingQueue()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                queue.Enqueue(3, 30);
                queue.Enqueue(1, 10);
                queue.Enqueue(2, 20);

                Assert.Throws<InvalidOperationException>(() => queue.Enqueue(0, 0));

                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(0));

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(1, 10),
                    new UnsafeCellBucketQueueItem(2, 20),
                    new UnsafeCellBucketQueueItem(3, 30));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WhenAtCapacity_ReturnsFalseAndDoesNotMutateExistingQueue()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                Assert.That(queue.TryEnqueue(3, 30), Is.True);
                Assert.That(queue.TryEnqueue(1, 10), Is.True);
                Assert.That(queue.TryEnqueue(2, 20), Is.True);

                Assert.That(queue.TryEnqueue(4, 40), Is.False);

                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(0));

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(1, 10),
                    new UnsafeCellBucketQueueItem(2, 20),
                    new UnsafeCellBucketQueueItem(3, 30));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryPeek_WhenEmpty_ReturnsFalseAndDefaultItem()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 3, capacity: 1);

            try
            {
                bool succeeded = queue.TryPeek(out UnsafeCellBucketQueueItem item);

                Assert.That(succeeded, Is.False);
                Assert.That(item, Is.EqualTo(default(UnsafeCellBucketQueueItem)));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_WhenEmpty_ReturnsFalseAndDefaultItem()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 3, capacity: 1);

            try
            {
                bool succeeded = queue.TryDequeue(out UnsafeCellBucketQueueItem item);

                Assert.That(succeeded, Is.False);
                Assert.That(item, Is.EqualTo(default(UnsafeCellBucketQueueItem)));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_WithPriorityAndCellIndex_WhenEmpty_ReturnsFalseAndDefaults()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 3, capacity: 1);

            try
            {
                bool succeeded = queue.TryDequeue(out int priority, out int cellIndex);

                Assert.That(succeeded, Is.False);
                Assert.That(priority, Is.EqualTo(default(int)));
                Assert.That(cellIndex, Is.EqualTo(default(int)));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryPeek_WhenNotEmpty_ReturnsNextItemWithoutRemovingIt()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                queue.Enqueue(3, 30);
                queue.Enqueue(1, 10);
                queue.Enqueue(1, 20);

                bool succeeded = queue.TryPeek(out UnsafeCellBucketQueueItem item);

                Assert.That(succeeded, Is.True);
                Assert.That(item, Is.EqualTo(new UnsafeCellBucketQueueItem(1, 10)));
                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.CurrentPriority, Is.EqualTo(0));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_ReturnsItemsOrderedByPriorityThenFifoWithinPriority()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 6, capacity: 8);

            try
            {
                queue.Enqueue(4, 40);
                queue.Enqueue(2, 20);
                queue.Enqueue(2, 21);
                queue.Enqueue(1, 10);
                queue.Enqueue(4, 41);
                queue.Enqueue(1, 11);
                queue.Enqueue(3, 30);
                queue.Enqueue(2, 22);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(1, 10),
                    new UnsafeCellBucketQueueItem(1, 11),
                    new UnsafeCellBucketQueueItem(2, 20),
                    new UnsafeCellBucketQueueItem(2, 21),
                    new UnsafeCellBucketQueueItem(2, 22),
                    new UnsafeCellBucketQueueItem(3, 30),
                    new UnsafeCellBucketQueueItem(4, 40),
                    new UnsafeCellBucketQueueItem(4, 41));

                Assert.That(queue.IsEmpty, Is.True);
                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(8));
                Assert.That(queue.CurrentPriority, Is.EqualTo(4));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_WithPriorityAndCellIndex_ReturnsItemsOrderedByPriorityThenFifoWithinPriority()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 4);

            try
            {
                queue.Enqueue(3, 30);
                queue.Enqueue(1, 10);
                queue.Enqueue(1, 11);
                queue.Enqueue(2, 20);

                Assert.That(queue.TryDequeue(out int priority0, out int cellIndex0), Is.True);
                Assert.That(priority0, Is.EqualTo(1));
                Assert.That(cellIndex0, Is.EqualTo(10));

                Assert.That(queue.TryDequeue(out int priority1, out int cellIndex1), Is.True);
                Assert.That(priority1, Is.EqualTo(1));
                Assert.That(cellIndex1, Is.EqualTo(11));

                Assert.That(queue.TryDequeue(out int priority2, out int cellIndex2), Is.True);
                Assert.That(priority2, Is.EqualTo(2));
                Assert.That(cellIndex2, Is.EqualTo(20));

                Assert.That(queue.TryDequeue(out int priority3, out int cellIndex3), Is.True);
                Assert.That(priority3, Is.EqualTo(3));
                Assert.That(cellIndex3, Is.EqualTo(30));

                Assert.That(queue.TryDequeue(out _, out _), Is.False);
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_AllowsDuplicateCellIndices()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                queue.Enqueue(3, 42);
                queue.Enqueue(1, 42);
                queue.Enqueue(2, 42);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(1, 42),
                    new UnsafeCellBucketQueueItem(2, 42),
                    new UnsafeCellBucketQueueItem(3, 42));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WithPriorityLowerThanCurrentPriority_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 6, capacity: 4);

            try
            {
                queue.Enqueue(2, 20);
                queue.Enqueue(4, 40);

                Assert.That(queue.TryDequeue(out UnsafeCellBucketQueueItem first), Is.True);
                Assert.That(first, Is.EqualTo(new UnsafeCellBucketQueueItem(2, 20)));
                Assert.That(queue.CurrentPriority, Is.EqualTo(2));

                Assert.Throws<InvalidOperationException>(() => queue.Enqueue(1, 10));

                Assert.That(queue.Count, Is.EqualTo(1));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(4, 40));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WithPriorityLowerThanCurrentPriority_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 6, capacity: 4);

            try
            {
                queue.Enqueue(2, 20);
                queue.Enqueue(4, 40);

                Assert.That(queue.TryDequeue(out UnsafeCellBucketQueueItem first), Is.True);
                Assert.That(first, Is.EqualTo(new UnsafeCellBucketQueueItem(2, 20)));
                Assert.That(queue.CurrentPriority, Is.EqualTo(2));

                Assert.Throws<InvalidOperationException>(() => queue.TryEnqueue(1, 10));

                Assert.That(queue.Count, Is.EqualTo(1));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(4, 40));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WithPriorityEqualToCurrentPriorityAfterDequeue_Succeeds()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 6, capacity: 4);

            try
            {
                queue.Enqueue(2, 20);

                Assert.That(queue.TryDequeue(out UnsafeCellBucketQueueItem first), Is.True);
                Assert.That(first, Is.EqualTo(new UnsafeCellBucketQueueItem(2, 20)));
                Assert.That(queue.CurrentPriority, Is.EqualTo(2));

                queue.Enqueue(2, 21);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(2, 21));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Clear_RemovesItemsKeepsCapacityAndResetsCurrentPriorityToZero()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3, startingPriority: 2);

            try
            {
                queue.Enqueue(2, 20);
                queue.Enqueue(4, 40);

                queue.Clear();

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.Capacity, Is.EqualTo(3));
                Assert.That(queue.BucketCount, Is.EqualTo(5));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));
                Assert.That(queue.CurrentPriority, Is.EqualTo(0));
                Assert.That(queue.IsEmpty, Is.True);
                Assert.That(queue.TryDequeue(out _), Is.False);

                queue.Enqueue(0, 100);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(0, 100));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Clear_WithStartingPriority_RemovesItemsKeepsCapacityAndSetsCurrentPriority()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                queue.Enqueue(1, 10);
                queue.Enqueue(4, 40);

                queue.Clear(3);

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));
                Assert.That(queue.CurrentPriority, Is.EqualTo(3));
                Assert.That(queue.IsEmpty, Is.True);

                Assert.Throws<InvalidOperationException>(() => queue.Enqueue(2, 20));

                queue.Enqueue(3, 30);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(3, 30));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Clear_WithInvalidStartingPriority_ThrowsAndDoesNotMutate()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                queue.Enqueue(1, 10);

                Assert.Throws<ArgumentOutOfRangeException>(() => queue.Clear(5));

                Assert.That(queue.Count, Is.EqualTo(1));
                Assert.That(queue.CurrentPriority, Is.EqualTo(0));

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellBucketQueueItem(1, 10));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void RemainingCapacity_ReturnsCapacityMinusCount()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 5, capacity: 3);

            try
            {
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));

                queue.Enqueue(2, 20);

                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));

                Assert.That(queue.TryEnqueue(1, 10), Is.True);

                Assert.That(queue.RemainingCapacity, Is.EqualTo(1));

                Assert.That(queue.TryDequeue(out _), Is.True);

                Assert.That(queue.RemainingCapacity, Is.EqualTo(2));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Dispose_WhenCalledTwiceOnSameValue_DoesNotThrow()
        {
            UnsafeCellBucketQueue queue = CreateQueue(bucketCount: 3, capacity: 1);

            Assert.DoesNotThrow(() => queue.Dispose());
            Assert.DoesNotThrow(() => queue.Dispose());

            Assert.That(queue.IsCreated, Is.False);
        }

        [Test]
        public void UnsafeCellBucketQueueItem_Equals_WithSamePriorityAndCellIndex_ReturnsTrue()
        {
            var left = new UnsafeCellBucketQueueItem(1, 2);
            var right = new UnsafeCellBucketQueueItem(1, 2);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void UnsafeCellBucketQueueItem_Equals_WithDifferentPriority_ReturnsFalse()
        {
            var left = new UnsafeCellBucketQueueItem(1, 2);
            var right = new UnsafeCellBucketQueueItem(3, 2);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void UnsafeCellBucketQueueItem_Equals_WithDifferentCellIndex_ReturnsFalse()
        {
            var left = new UnsafeCellBucketQueueItem(1, 2);
            var right = new UnsafeCellBucketQueueItem(1, 3);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void UnsafeCellBucketQueueItem_ToString_ReturnsPriorityAndCellIndex()
        {
            var item = new UnsafeCellBucketQueueItem(7, 42);

            Assert.That(item.ToString(), Is.EqualTo("Priority=7, CellIndex=42"));
        }

        private static UnsafeCellBucketQueue CreateQueue(
            int bucketCount,
            int capacity,
            int startingPriority = 0)
        {
            return new UnsafeCellBucketQueue(bucketCount, capacity, Allocator.Persistent, startingPriority);
        }

        private static void AssertDequeuesInOrder(
            ref UnsafeCellBucketQueue queue,
            params UnsafeCellBucketQueueItem[] expectedItems)
        {
            for (int index = 0; index < expectedItems.Length; index++)
            {
                Assert.That(queue.TryDequeue(out UnsafeCellBucketQueueItem item), Is.True);
                Assert.That(item, Is.EqualTo(expectedItems[index]), $"Unexpected item at dequeue index {index}.");
            }

            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }
}