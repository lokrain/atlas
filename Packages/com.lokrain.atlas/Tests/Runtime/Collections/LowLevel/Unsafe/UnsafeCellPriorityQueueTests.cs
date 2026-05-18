#nullable enable

using System; 
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe.Tests
{
    internal sealed unsafe class UnsafeCellPriorityQueueTests
    {
        [Test]
        public void Constructor_WithZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellPriorityQueue(0, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeCellPriorityQueue(-1, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithAllocatorNone_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => _ = new UnsafeCellPriorityQueue(1, Allocator.None));
        }

        [Test]
        public void Default_IsCreated_ReturnsFalse()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.That(queue.IsCreated, Is.False);
        }

        [Test]
        public void Count_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.Count);
        }

        [Test]
        public void Capacity_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.Capacity);
        }

        [Test]
        public void RemainingCapacity_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.RemainingCapacity);
        }

        [Test]
        public void IsEmpty_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => _ = queue.IsEmpty);
        }

        [Test]
        public void Clear_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.Clear());
        }

        [Test]
        public void Enqueue_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(1, 10));
        }

        [Test]
        public void TryEnqueue_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryEnqueue(1, 10));
        }

        [Test]
        public void TryPeek_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryPeek(out _));
        }

        [Test]
        public void TryDequeue_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryDequeue(out _));
        }

        [Test]
        public void TryDequeue_WithPriorityAndCellIndex_WhenDefault_Throws()
        {
            UnsafeCellPriorityQueue queue = default;

            Assert.Throws<InvalidOperationException>(() => queue.TryDequeue(out _, out _));
        }

        [Test]
        public void Constructor_WithValidArguments_CreatesEmptyQueue()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(4);

            try
            {
                Assert.That(queue.IsCreated, Is.True);
                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.Capacity, Is.EqualTo(4));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(4));
                Assert.That(queue.IsEmpty, Is.True);
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WithNegativeCellIndex_Throws()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.Enqueue(1, -1));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(1));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WithNegativeCellIndex_Throws()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => queue.TryEnqueue(1, -1));

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(1));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enqueue_WhenAtCapacity_ThrowsAndDoesNotMutateExistingHeap()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(3);

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
                    new UnsafeCellPriorityQueueItem(1, 10),
                    new UnsafeCellPriorityQueueItem(2, 20),
                    new UnsafeCellPriorityQueueItem(3, 30));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryEnqueue_WhenAtCapacity_ReturnsFalseAndDoesNotMutateExistingHeap()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(3);

            try
            {
                Assert.That(queue.TryEnqueue(3, 30), Is.True);
                Assert.That(queue.TryEnqueue(1, 10), Is.True);
                Assert.That(queue.TryEnqueue(2, 20), Is.True);

                Assert.That(queue.TryEnqueue(0, 0), Is.False);

                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(0));

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellPriorityQueueItem(1, 10),
                    new UnsafeCellPriorityQueueItem(2, 20),
                    new UnsafeCellPriorityQueueItem(3, 30));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryPeek_WhenEmpty_ReturnsFalseAndDefaultItem()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(1);

            try
            {
                bool succeeded = queue.TryPeek(out UnsafeCellPriorityQueueItem item);

                Assert.That(succeeded, Is.False);
                Assert.That(item, Is.EqualTo(default(UnsafeCellPriorityQueueItem)));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_WhenEmpty_ReturnsFalseAndDefaultItem()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(1);

            try
            {
                bool succeeded = queue.TryDequeue(out UnsafeCellPriorityQueueItem item);

                Assert.That(succeeded, Is.False);
                Assert.That(item, Is.EqualTo(default(UnsafeCellPriorityQueueItem)));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_WithPriorityAndCellIndex_WhenEmpty_ReturnsFalseAndDefaults()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(1);

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
            UnsafeCellPriorityQueue queue = CreateQueue(3);

            try
            {
                queue.Enqueue(5, 50);
                queue.Enqueue(1, 20);
                queue.Enqueue(1, 10);

                bool succeeded = queue.TryPeek(out UnsafeCellPriorityQueueItem item);

                Assert.That(succeeded, Is.True);
                Assert.That(item, Is.EqualTo(new UnsafeCellPriorityQueueItem(1, 10)));
                Assert.That(queue.Count, Is.EqualTo(3));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_ReturnsItemsOrderedByPriorityThenCellIndex()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(7);

            try
            {
                queue.Enqueue(10, 5);
                queue.Enqueue(2, 9);
                queue.Enqueue(2, 3);
                queue.Enqueue(1, 100);
                queue.Enqueue(10, 1);
                queue.Enqueue(1, 50);
                queue.Enqueue(2, 3);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellPriorityQueueItem(1, 50),
                    new UnsafeCellPriorityQueueItem(1, 100),
                    new UnsafeCellPriorityQueueItem(2, 3),
                    new UnsafeCellPriorityQueueItem(2, 3),
                    new UnsafeCellPriorityQueueItem(2, 9),
                    new UnsafeCellPriorityQueueItem(10, 1),
                    new UnsafeCellPriorityQueueItem(10, 5));

                Assert.That(queue.IsEmpty, Is.True);
                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(7));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void TryDequeue_WithPriorityAndCellIndex_ReturnsItemsOrderedByPriorityThenCellIndex()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(4);

            try
            {
                queue.Enqueue(4, 4);
                queue.Enqueue(1, 9);
                queue.Enqueue(1, 2);
                queue.Enqueue(3, 1);

                Assert.That(queue.TryDequeue(out int priority0, out int cellIndex0), Is.True);
                Assert.That(priority0, Is.EqualTo(1));
                Assert.That(cellIndex0, Is.EqualTo(2));

                Assert.That(queue.TryDequeue(out int priority1, out int cellIndex1), Is.True);
                Assert.That(priority1, Is.EqualTo(1));
                Assert.That(cellIndex1, Is.EqualTo(9));

                Assert.That(queue.TryDequeue(out int priority2, out int cellIndex2), Is.True);
                Assert.That(priority2, Is.EqualTo(3));
                Assert.That(cellIndex2, Is.EqualTo(1));

                Assert.That(queue.TryDequeue(out int priority3, out int cellIndex3), Is.True);
                Assert.That(priority3, Is.EqualTo(4));
                Assert.That(cellIndex3, Is.EqualTo(4));

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
            UnsafeCellPriorityQueue queue = CreateQueue(3);

            try
            {
                queue.Enqueue(3, 42);
                queue.Enqueue(1, 42);
                queue.Enqueue(2, 42);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellPriorityQueueItem(1, 42),
                    new UnsafeCellPriorityQueueItem(2, 42),
                    new UnsafeCellPriorityQueueItem(3, 42));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Clear_RemovesItemsAndKeepsCapacity()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(3);

            try
            {
                queue.Enqueue(2, 20);
                queue.Enqueue(1, 10);

                queue.Clear();

                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.Capacity, Is.EqualTo(3));
                Assert.That(queue.RemainingCapacity, Is.EqualTo(3));
                Assert.That(queue.IsEmpty, Is.True);
                Assert.That(queue.TryDequeue(out _), Is.False);

                queue.Enqueue(0, 100);

                AssertDequeuesInOrder(
                    ref queue,
                    new UnsafeCellPriorityQueueItem(0, 100));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void RemainingCapacity_ReturnsCapacityMinusCount()
        {
            UnsafeCellPriorityQueue queue = CreateQueue(3);

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
            UnsafeCellPriorityQueue queue = CreateQueue(1);

            Assert.DoesNotThrow(() => queue.Dispose());
            Assert.DoesNotThrow(() => queue.Dispose());

            Assert.That(queue.IsCreated, Is.False);
        }

        [Test]
        public void UnsafeCellPriorityQueueItem_Equals_WithSamePriorityAndCellIndex_ReturnsTrue()
        {
            var left = new UnsafeCellPriorityQueueItem(1, 2);
            var right = new UnsafeCellPriorityQueueItem(1, 2);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void UnsafeCellPriorityQueueItem_Equals_WithDifferentPriority_ReturnsFalse()
        {
            var left = new UnsafeCellPriorityQueueItem(1, 2);
            var right = new UnsafeCellPriorityQueueItem(3, 2);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void UnsafeCellPriorityQueueItem_Equals_WithDifferentCellIndex_ReturnsFalse()
        {
            var left = new UnsafeCellPriorityQueueItem(1, 2);
            var right = new UnsafeCellPriorityQueueItem(1, 3);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void UnsafeCellPriorityQueueItem_ToString_ReturnsPriorityAndCellIndex()
        {
            var item = new UnsafeCellPriorityQueueItem(7, 42);

            Assert.That(item.ToString(), Is.EqualTo("Priority=7, CellIndex=42"));
        }

        private static UnsafeCellPriorityQueue CreateQueue(int capacity)
        {
            return new UnsafeCellPriorityQueue(capacity, Allocator.Persistent);
        }

        private static void AssertDequeuesInOrder(
            ref UnsafeCellPriorityQueue queue,
            params UnsafeCellPriorityQueueItem[] expectedItems)
        {
            for (int index = 0; index < expectedItems.Length; index++)
            {
                Assert.That(queue.TryDequeue(out UnsafeCellPriorityQueueItem item), Is.True);
                Assert.That(item, Is.EqualTo(expectedItems[index]), $"Unexpected item at dequeue index {index}.");
            }

            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }
}