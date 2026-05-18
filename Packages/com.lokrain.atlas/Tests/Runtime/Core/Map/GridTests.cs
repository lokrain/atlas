#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Core.Map.Tests
{
    public sealed class GridTests
    {
        [TestCase(Grid.MinWidth, Grid.MinDepth)]
        [TestCase(Grid.MaxWidth, Grid.MaxDepth)]
        [TestCase(256, 512)]
        [TestCase(512, 256)]
        public void Constructor_WithValidDimensions_CreatesGrid(
            int width,
            int depth)
        {
            var grid = new Grid(width, depth);

            Assert.That(grid.Width, Is.EqualTo(width));
            Assert.That(grid.Depth, Is.EqualTo(depth));
            Assert.That(grid.CellCount, Is.EqualTo(width * depth));
            Assert.That(grid.LastIndexValue, Is.EqualTo(width * depth - 1));
        }

        [TestCase(Grid.MinWidth - 1, 256)]
        [TestCase(Grid.MaxWidth + 1, 256)]
        public void Constructor_WithInvalidWidth_ThrowsArgumentOutOfRangeException(
            int width,
            int depth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(width, depth));
        }

        [TestCase(256, Grid.MinDepth - 1)]
        [TestCase(256, Grid.MaxDepth + 1)]
        public void Constructor_WithInvalidDepth_ThrowsArgumentOutOfRangeException(
            int width,
            int depth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(width, depth));
        }

        [TestCase(0, 0)]
        [TestCase(255, 0)]
        [TestCase(0, 255)]
        [TestCase(255, 255)]
        public void Contains_WithCoordinatesInsideGrid_ReturnsTrue(
            int x,
            int z)
        {
            var grid = new Grid(256, 256);

            bool contains = grid.Contains(x, z);

            Assert.That(contains, Is.True);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(256, 0)]
        [TestCase(0, 256)]
        public void Contains_WithCoordinatesOutsideGrid_ReturnsFalse(
            int x,
            int z)
        {
            var grid = new Grid(256, 256);

            bool contains = grid.Contains(x, z);

            Assert.That(contains, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(65535)]
        public void Contains_WithIndexInsideGrid_ReturnsTrue(int index)
        {
            var grid = new Grid(256, 256);

            bool contains = grid.Contains(index);

            Assert.That(contains, Is.True);
        }

        [TestCase(-1)]
        [TestCase(65536)]
        public void Contains_WithIndexOutsideGrid_ReturnsFalse(int index)
        {
            var grid = new Grid(256, 256);

            bool contains = grid.Contains(index);

            Assert.That(contains, Is.False);
        }

        [Test]
        public void Contains_WithCellInsideGrid_ReturnsTrue()
        {
            var grid = new Grid(256, 256);
            Cell cell = grid.GetCell(5, 2);

            bool contains = grid.Contains(cell);

            Assert.That(contains, Is.True);
        }

        [Test]
        public void Contains_WithCellOutsideGrid_ReturnsFalse()
        {
            var smallGrid = new Grid(256, 256);
            var largeGrid = new Grid(512, 512);
            Cell cell = largeGrid.GetCell(300, 2);

            bool contains = smallGrid.Contains(cell);

            Assert.That(contains, Is.False);
        }

        [Test]
        public void Contains_WithCellIndexInsideGrid_ReturnsTrue()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            bool contains = grid.Contains(index);

            Assert.That(contains, Is.True);
        }

        [Test]
        public void Contains_WithCellIndexOutsideGrid_ReturnsFalse()
        {
            var smallGrid = new Grid(256, 256);
            var largeGrid = new Grid(512, 512);
            CellIndex index = largeGrid.GetIndex(0, 128);

            bool contains = smallGrid.Contains(index);

            Assert.That(contains, Is.False);
        }

        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 1)]
        [TestCase(255, 0, 255)]
        [TestCase(0, 1, 256)]
        [TestCase(5, 2, 517)]
        [TestCase(255, 255, 65535)]
        public void GetIndexValue_WithValidCoordinates_ReturnsRowMajorIndex(
            int x,
            int z,
            int expectedIndex)
        {
            var grid = new Grid(256, 256);

            int index = grid.GetIndexValue(x, z);

            Assert.That(index, Is.EqualTo(expectedIndex));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(256, 0)]
        [TestCase(0, 256)]
        public void GetIndexValue_WithInvalidCoordinates_ThrowsArgumentOutOfRangeException(
            int x,
            int z)
        {
            var grid = new Grid(256, 256);

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetIndexValue(x, z));
        }

        [Test]
        public void TryGetIndexValue_WithValidCoordinates_ReturnsTrueAndIndex()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetIndexValue(5, 2, out int index);

            Assert.That(succeeded, Is.True);
            Assert.That(index, Is.EqualTo(517));
        }

        [Test]
        public void TryGetIndexValue_WithInvalidCoordinates_ReturnsFalse()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetIndexValue(256, 0, out _);

            Assert.That(succeeded, Is.False);
        }

        [Test]
        public void GetIndex_WithValidCoordinates_ReturnsCellIndex()
        {
            var grid = new Grid(256, 256);

            CellIndex index = grid.GetIndex(5, 2);

            Assert.That(index.Value, Is.EqualTo(517));
        }

        [Test]
        public void GetIndex_WithValidCell_ReturnsCellIndex()
        {
            var grid = new Grid(256, 256);
            Cell cell = grid.GetCell(5, 2);

            CellIndex index = grid.GetIndex(cell);

            Assert.That(index.Value, Is.EqualTo(517));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(256, 0)]
        [TestCase(0, 256)]
        public void GetIndex_WithInvalidCoordinates_ThrowsArgumentOutOfRangeException(
            int x,
            int z)
        {
            var grid = new Grid(256, 256);

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetIndex(x, z));
        }

        [Test]
        public void TryGetIndex_WithValidCoordinates_ReturnsTrueAndCellIndex()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetIndex(5, 2, out CellIndex index);

            Assert.That(succeeded, Is.True);
            Assert.That(index.Value, Is.EqualTo(517));
        }

        [Test]
        public void TryGetIndex_WithInvalidCoordinates_ReturnsFalse()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetIndex(256, 0, out _);

            Assert.That(succeeded, Is.False);
        }

        [TestCase(0, 0, 0)]
        [TestCase(1, 1, 0)]
        [TestCase(255, 255, 0)]
        [TestCase(256, 0, 1)]
        [TestCase(517, 5, 2)]
        [TestCase(65535, 255, 255)]
        public void GetCell_WithValidIndex_ReturnsCell(
            int index,
            int expectedX,
            int expectedZ)
        {
            var grid = new Grid(256, 256);

            Cell cell = grid.GetCell(index);

            Assert.That(cell.X, Is.EqualTo(expectedX));
            Assert.That(cell.Z, Is.EqualTo(expectedZ));
        }

        [TestCase(-1)]
        [TestCase(65536)]
        public void GetCell_WithInvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var grid = new Grid(256, 256);

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCell(index));
        }

        [Test]
        public void GetCell_WithValidCoordinates_ReturnsCell()
        {
            var grid = new Grid(256, 256);

            Cell cell = grid.GetCell(5, 2);

            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(256, 0)]
        [TestCase(0, 256)]
        public void GetCell_WithInvalidCoordinates_ThrowsArgumentOutOfRangeException(
            int x,
            int z)
        {
            var grid = new Grid(256, 256);

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCell(x, z));
        }

        [Test]
        public void GetCell_WithValidCellIndex_ReturnsCell()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            Cell cell = grid.GetCell(index);

            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void TryGetCell_WithValidCoordinates_ReturnsTrueAndCell()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetCell(5, 2, out Cell cell);

            Assert.That(succeeded, Is.True);
            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void TryGetCell_WithInvalidCoordinates_ReturnsFalse()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetCell(256, 0, out _);

            Assert.That(succeeded, Is.False);
        }

        [Test]
        public void TryGetCell_WithValidIndex_ReturnsTrueAndCell()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetCell(517, out Cell cell);

            Assert.That(succeeded, Is.True);
            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void TryGetCell_WithInvalidIndex_ReturnsFalse()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetCell(65536, out _);

            Assert.That(succeeded, Is.False);
        }

        [Test]
        public void Indexer_WithCoordinates_ReturnsCell()
        {
            var grid = new Grid(256, 256);

            Cell cell = grid[5, 2];

            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void Indexer_WithIndex_ReturnsCell()
        {
            var grid = new Grid(256, 256);

            Cell cell = grid[517];

            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void Indexer_WithCellIndex_ReturnsCell()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            Cell cell = grid[index];

            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void GetCellComponents_WithValidIndex_ReturnsCoordinates()
        {
            var grid = new Grid(256, 256);

            grid.GetCellComponents(517, out int x, out int z);

            Assert.That(x, Is.EqualTo(5));
            Assert.That(z, Is.EqualTo(2));
        }

        [Test]
        public void GetCellComponents_WithValidCellIndex_ReturnsCoordinates()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            grid.GetCellComponents(index, out int x, out int z);

            Assert.That(x, Is.EqualTo(5));
            Assert.That(z, Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(65536)]
        public void GetCellComponents_WithInvalidIndex_ThrowsArgumentOutOfRangeException(
            int index)
        {
            var grid = new Grid(256, 256);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => grid.GetCellComponents(index, out _, out _));
        }

        [Test]
        public void TryGetCellComponents_WithValidIndex_ReturnsTrueAndCoordinates()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetCellComponents(517, out int x, out int z);

            Assert.That(succeeded, Is.True);
            Assert.That(x, Is.EqualTo(5));
            Assert.That(z, Is.EqualTo(2));
        }

        [Test]
        public void TryGetCellComponents_WithInvalidIndex_ReturnsFalse()
        {
            var grid = new Grid(256, 256);

            bool succeeded = grid.TryGetCellComponents(65536, out _, out _);

            Assert.That(succeeded, Is.False);
        }

        [Test]
        public void Equals_WithSameDimensions_ReturnsTrue()
        {
            var left = new Grid(256, 512);
            var right = new Grid(256, 512);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentDimensions_ReturnsFalse()
        {
            var left = new Grid(256, 512);
            var right = new Grid(512, 256);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            Grid? left = null;
            Grid? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            Grid? left = new(256, 256);
            Grid? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsDimensionText()
        {
            var grid = new Grid(256, 512);

            string value = grid.ToString();

            Assert.That(value, Is.EqualTo("Grid(Width: 256, Depth: 512)"));
        }
    }
}