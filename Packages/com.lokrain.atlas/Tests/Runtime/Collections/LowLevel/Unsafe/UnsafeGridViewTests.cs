#nullable enable

using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe.Tests
{
    public sealed unsafe class UnsafeGridViewTests
    {
        [Test]
        public void Constructor_WithNullItems_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => _ = new UnsafeGridView<int>(null, 1, 1));
        }

        [Test]
        public void Constructor_WithZeroWidth_Throws()
        {
            int* items = AllocateIntBuffer(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => _ = new UnsafeGridView<int>(items, 0, 1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Constructor_WithNegativeWidth_Throws()
        {
            int* items = AllocateIntBuffer(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => _ = new UnsafeGridView<int>(items, -1, 1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Constructor_WithZeroHeight_Throws()
        {
            int* items = AllocateIntBuffer(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => _ = new UnsafeGridView<int>(items, 1, 0));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Constructor_WithNegativeHeight_Throws()
        {
            int* items = AllocateIntBuffer(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => _ = new UnsafeGridView<int>(items, 1, -1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Constructor_WithStrideLessThanWidth_Throws()
        {
            int* items = AllocateIntBuffer(6);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => _ = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 2));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Constructor_WithTightlyPackedArguments_CreatesView()
        {
            int* items = AllocateIntBuffer(6);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2);

                Assert.That(view.IsCreated, Is.True);
                Assert.That(view.Width, Is.EqualTo(3));
                Assert.That(view.Height, Is.EqualTo(2));
                Assert.That(view.Stride, Is.EqualTo(3));
                Assert.That(view.Length, Is.EqualTo(6));
                Assert.That(view.BackingLength, Is.EqualTo(6));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Constructor_WithExplicitStride_CreatesPaddedView()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.That(view.IsCreated, Is.True);
                Assert.That(view.Width, Is.EqualTo(3));
                Assert.That(view.Height, Is.EqualTo(2));
                Assert.That(view.Stride, Is.EqualTo(5));
                Assert.That(view.Length, Is.EqualTo(6));
                Assert.That(view.BackingLength, Is.EqualTo(8));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Default_IsCreated_ReturnsFalse()
        {
            UnsafeGridView<int> view = default;

            Assert.That(view.IsCreated, Is.False);
        }

        [Test]
        public void Width_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => _ = view.Width);
        }

        [Test]
        public void Height_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => _ = view.Height);
        }

        [Test]
        public void Stride_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => _ = view.Stride);
        }

        [Test]
        public void Length_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => _ = view.Length);
        }

        [Test]
        public void BackingLength_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => _ = view.BackingLength);
        }

        [Test]
        public void Contains_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.Contains(0, 0));
        }

        [Test]
        public void ContainsLogicalIndex_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.ContainsLogicalIndex(0));
        }

        [Test]
        public void ToLogicalIndex_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.ToLogicalIndex(0, 0));
        }

        [Test]
        public void ToBackingIndex_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.ToBackingIndex(0, 0));
        }

        [Test]
        public void ToCoordinates_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.ToCoordinates(0, out _, out _));
        }

        [Test]
        public void LogicalToBackingIndex_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.LogicalToBackingIndex(0));
        }

        [Test]
        public void Read_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.Read(0, 0));
        }

        [Test]
        public void ReadLogical_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.ReadLogical(0));
        }

        [Test]
        public void Write_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.Write(0, 0, 1));
        }

        [Test]
        public void WriteLogical_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.WriteLogical(0, 1));
        }

        [Test]
        public void ElementAt_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() =>
            {
                ref int value = ref view.ElementAt(0, 0);
                _ = value;
            });
        }

        [Test]
        public void ElementAtLogical_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() =>
            {
                ref int value = ref view.ElementAtLogical(0);
                _ = value;
            });
        }

        [Test]
        public void GetRow_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.GetRow(0));
        }

        [Test]
        public void Fill_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.Fill(1));
        }

        [Test]
        public void Clear_WhenDefault_Throws()
        {
            UnsafeGridView<int> view = default;

            Assert.Throws<InvalidOperationException>(() => view.Clear());
        }

        [Test]
        public void Contains_ReturnsWhetherCoordinateIsInsideGrid()
        {
            int* items = AllocateIntBuffer(6);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2);

                Assert.That(view.Contains(-1, 0), Is.False);
                Assert.That(view.Contains(0, -1), Is.False);
                Assert.That(view.Contains(0, 0), Is.True);
                Assert.That(view.Contains(2, 1), Is.True);
                Assert.That(view.Contains(3, 1), Is.False);
                Assert.That(view.Contains(2, 2), Is.False);
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ContainsLogicalIndex_ReturnsWhetherIndexIsInsideGrid()
        {
            int* items = AllocateIntBuffer(6);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2);

                Assert.That(view.ContainsLogicalIndex(-1), Is.False);
                Assert.That(view.ContainsLogicalIndex(0), Is.True);
                Assert.That(view.ContainsLogicalIndex(5), Is.True);
                Assert.That(view.ContainsLogicalIndex(6), Is.False);
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ToLogicalIndex_ConvertsCoordinateToLogicalIndex()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.That(view.ToLogicalIndex(0, 0), Is.EqualTo(0));
                Assert.That(view.ToLogicalIndex(1, 0), Is.EqualTo(1));
                Assert.That(view.ToLogicalIndex(2, 0), Is.EqualTo(2));
                Assert.That(view.ToLogicalIndex(0, 1), Is.EqualTo(3));
                Assert.That(view.ToLogicalIndex(2, 1), Is.EqualTo(5));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ToBackingIndex_ConvertsCoordinateToBackingIndex()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.That(view.ToBackingIndex(0, 0), Is.EqualTo(0));
                Assert.That(view.ToBackingIndex(1, 0), Is.EqualTo(1));
                Assert.That(view.ToBackingIndex(2, 0), Is.EqualTo(2));
                Assert.That(view.ToBackingIndex(0, 1), Is.EqualTo(5));
                Assert.That(view.ToBackingIndex(2, 1), Is.EqualTo(7));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ToCoordinates_ConvertsLogicalIndexToCoordinate()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                view.ToCoordinates(0, out int x0, out int y0);
                view.ToCoordinates(2, out int x2, out int y2);
                view.ToCoordinates(3, out int x3, out int y3);
                view.ToCoordinates(5, out int x5, out int y5);

                Assert.That(x0, Is.EqualTo(0));
                Assert.That(y0, Is.EqualTo(0));

                Assert.That(x2, Is.EqualTo(2));
                Assert.That(y2, Is.EqualTo(0));

                Assert.That(x3, Is.EqualTo(0));
                Assert.That(y3, Is.EqualTo(1));

                Assert.That(x5, Is.EqualTo(2));
                Assert.That(y5, Is.EqualTo(1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void LogicalToBackingIndex_ConvertsLogicalIndexToBackingIndex()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.That(view.LogicalToBackingIndex(0), Is.EqualTo(0));
                Assert.That(view.LogicalToBackingIndex(1), Is.EqualTo(1));
                Assert.That(view.LogicalToBackingIndex(2), Is.EqualTo(2));
                Assert.That(view.LogicalToBackingIndex(3), Is.EqualTo(5));
                Assert.That(view.LogicalToBackingIndex(4), Is.EqualTo(6));
                Assert.That(view.LogicalToBackingIndex(5), Is.EqualTo(7));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void CoordinateAndIndexConversion_WithInvalidInput_Throws()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToLogicalIndex(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToLogicalIndex(3, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToLogicalIndex(0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToLogicalIndex(0, 2));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToBackingIndex(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToBackingIndex(3, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToBackingIndex(0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToBackingIndex(0, 2));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToCoordinates(-1, out _, out _));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ToCoordinates(6, out _, out _));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.LogicalToBackingIndex(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.LogicalToBackingIndex(6));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ReadAndWrite_ByCoordinate_UpdateBackingMemory()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                view.Write(0, 0, 10);
                view.Write(2, 0, 20);
                view.Write(1, 1, 30);

                Assert.That(view.Read(0, 0), Is.EqualTo(10));
                Assert.That(view.Read(2, 0), Is.EqualTo(20));
                Assert.That(view.Read(1, 1), Is.EqualTo(30));

                Assert.That(items[0], Is.EqualTo(10));
                Assert.That(items[2], Is.EqualTo(20));
                Assert.That(items[6], Is.EqualTo(30));
                Assert.That(items[3], Is.EqualTo(-1));
                Assert.That(items[4], Is.EqualTo(-1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ReadAndWriteLogical_UpdateBackingMemory()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                view.WriteLogical(0, 10);
                view.WriteLogical(2, 20);
                view.WriteLogical(4, 30);

                Assert.That(view.ReadLogical(0), Is.EqualTo(10));
                Assert.That(view.ReadLogical(2), Is.EqualTo(20));
                Assert.That(view.ReadLogical(4), Is.EqualTo(30));

                Assert.That(items[0], Is.EqualTo(10));
                Assert.That(items[2], Is.EqualTo(20));
                Assert.That(items[6], Is.EqualTo(30));
                Assert.That(items[3], Is.EqualTo(-1));
                Assert.That(items[4], Is.EqualTo(-1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ReadWrite_WithInvalidInput_ThrowsAndDoesNotMutate()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.Throws<ArgumentOutOfRangeException>(() => view.Read(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.Read(3, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.Read(0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.Read(0, 2));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.Write(-1, 0, 99));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.Write(3, 0, 99));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.Write(0, -1, 99));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.Write(0, 2, 99));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.ReadLogical(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.ReadLogical(6));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.WriteLogical(-1, 99));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.WriteLogical(6, 99));

                for (int index = 0; index < 8; index++)
                {
                    Assert.That(items[index], Is.EqualTo(-1), $"Backing index {index} should not mutate.");
                }
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ElementAt_ByCoordinate_ReturnsMutableReference()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                ref int value = ref view.ElementAt(1, 1);
                value = 42;

                Assert.That(view.Read(1, 1), Is.EqualTo(42));
                Assert.That(items[6], Is.EqualTo(42));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ElementAtLogical_ReturnsMutableReference()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                ref int value = ref view.ElementAtLogical(4);
                value = 42;

                Assert.That(view.ReadLogical(4), Is.EqualTo(42));
                Assert.That(items[6], Is.EqualTo(42));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void ElementAt_WithInvalidInput_ThrowsAndDoesNotMutate()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ref int value = ref view.ElementAt(3, 0);
                    _ = value;
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ref int value = ref view.ElementAtLogical(6);
                    _ = value;
                });

                for (int index = 0; index < 8; index++)
                {
                    Assert.That(items[index], Is.EqualTo(-1), $"Backing index {index} should not mutate.");
                }
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Fill_FillsOnlyLogicalElementsAndLeavesPaddingUntouched()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                view.Fill(7);

                Assert.That(items[0], Is.EqualTo(7));
                Assert.That(items[1], Is.EqualTo(7));
                Assert.That(items[2], Is.EqualTo(7));
                Assert.That(items[3], Is.EqualTo(-1));
                Assert.That(items[4], Is.EqualTo(-1));
                Assert.That(items[5], Is.EqualTo(7));
                Assert.That(items[6], Is.EqualTo(7));
                Assert.That(items[7], Is.EqualTo(7));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void Clear_ClearsOnlyLogicalElementsAndLeavesPaddingUntouched()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, 9);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                view.Clear();

                Assert.That(items[0], Is.EqualTo(0));
                Assert.That(items[1], Is.EqualTo(0));
                Assert.That(items[2], Is.EqualTo(0));
                Assert.That(items[3], Is.EqualTo(9));
                Assert.That(items[4], Is.EqualTo(9));
                Assert.That(items[5], Is.EqualTo(0));
                Assert.That(items[6], Is.EqualTo(0));
                Assert.That(items[7], Is.EqualTo(0));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void GetRow_WithInvalidRow_Throws()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);

                Assert.Throws<ArgumentOutOfRangeException>(() => view.GetRow(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => view.GetRow(2));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void GetRow_ReturnsMutableRowViewForLogicalRow()
        {
            int* items = AllocateIntBuffer(8);

            try
            {
                FillBacking(items, 8, -1);

                var view = new UnsafeGridView<int>(items, width: 3, height: 2, stride: 5);
                UnsafeGridRow<int> row = view.GetRow(1);

                Assert.That(row.IsCreated, Is.True);
                Assert.That(row.Length, Is.EqualTo(3));

                row.Write(0, 10);
                row.Write(1, 20);
                row.Write(2, 30);

                Assert.That(items[5], Is.EqualTo(10));
                Assert.That(items[6], Is.EqualTo(20));
                Assert.That(items[7], Is.EqualTo(30));
                Assert.That(items[3], Is.EqualTo(-1));
                Assert.That(items[4], Is.EqualTo(-1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void GridRow_Default_IsCreatedReturnsFalse()
        {
            UnsafeGridRow<int> row = default;

            Assert.That(row.IsCreated, Is.False);
        }

        [Test]
        public void GridRow_DefaultPropertiesThrow()
        {
            UnsafeGridRow<int> row = default;

            Assert.Throws<InvalidOperationException>(() => _ = row.Length);
        }

        [Test]
        public void GridRow_DefaultMethodsThrow()
        {
            UnsafeGridRow<int> row = default;

            Assert.Throws<InvalidOperationException>(() => row.Contains(0));
            Assert.Throws<InvalidOperationException>(() => row.Read(0));
            Assert.Throws<InvalidOperationException>(() => row.Write(0, 1));

            Assert.Throws<InvalidOperationException>(() =>
            {
                ref int value = ref row.ElementAt(0);
                _ = value;
            });

            Assert.Throws<InvalidOperationException>(() => row.Fill(1));
            Assert.Throws<InvalidOperationException>(() => row.Clear());
        }

        [Test]
        public void GridRow_Contains_ReturnsWhetherIndexIsInsideRow()
        {
            int* items = AllocateIntBuffer(3);

            try
            {
                var row = new UnsafeGridRow<int>(items, 3);

                Assert.That(row.Contains(-1), Is.False);
                Assert.That(row.Contains(0), Is.True);
                Assert.That(row.Contains(2), Is.True);
                Assert.That(row.Contains(3), Is.False);
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void GridRow_ReadWriteAndElementAt_UpdateBackingMemory()
        {
            int* items = AllocateIntBuffer(3);

            try
            {
                FillBacking(items, 3, -1);

                var row = new UnsafeGridRow<int>(items, 3);

                row.Write(0, 10);
                row.Write(1, 20);

                ref int value = ref row.ElementAt(2);
                value = 30;

                Assert.That(row.Read(0), Is.EqualTo(10));
                Assert.That(row.Read(1), Is.EqualTo(20));
                Assert.That(row.Read(2), Is.EqualTo(30));

                Assert.That(items[0], Is.EqualTo(10));
                Assert.That(items[1], Is.EqualTo(20));
                Assert.That(items[2], Is.EqualTo(30));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void GridRow_WithInvalidIndex_ThrowsAndDoesNotMutate()
        {
            int* items = AllocateIntBuffer(3);

            try
            {
                FillBacking(items, 3, -1);

                var row = new UnsafeGridRow<int>(items, 3);

                Assert.Throws<ArgumentOutOfRangeException>(() => row.Read(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => row.Read(3));
                Assert.Throws<ArgumentOutOfRangeException>(() => row.Write(-1, 99));
                Assert.Throws<ArgumentOutOfRangeException>(() => row.Write(3, 99));

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ref int value = ref row.ElementAt(3);
                    _ = value;
                });

                Assert.That(items[0], Is.EqualTo(-1));
                Assert.That(items[1], Is.EqualTo(-1));
                Assert.That(items[2], Is.EqualTo(-1));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        [Test]
        public void GridRow_FillAndClear_UpdateAllRowElements()
        {
            int* items = AllocateIntBuffer(3);

            try
            {
                var row = new UnsafeGridRow<int>(items, 3);

                row.Fill(7);

                Assert.That(items[0], Is.EqualTo(7));
                Assert.That(items[1], Is.EqualTo(7));
                Assert.That(items[2], Is.EqualTo(7));

                row.Clear();

                Assert.That(items[0], Is.EqualTo(0));
                Assert.That(items[1], Is.EqualTo(0));
                Assert.That(items[2], Is.EqualTo(0));
            }
            finally
            {
                FreeBuffer(items);
            }
        }

        private static int* AllocateIntBuffer(int length)
        {
            int* items = (int*)UnsafeUtility.Malloc(
                sizeof(int) * length,
                UnsafeUtility.AlignOf<int>(),
                Allocator.Persistent);

            UnsafeUtility.MemClear(items, sizeof(int) * length);

            return items;
        }

        private static void FreeBuffer(int* items)
        {
            UnsafeUtility.Free(items, Allocator.Persistent);
        }

        private static void FillBacking(int* items, int length, int value)
        {
            for (int index = 0; index < length; index++)
            {
                items[index] = value;
            }
        }
    }
}