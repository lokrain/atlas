#nullable enable

using NUnit.Framework;

namespace Lokrain.Atlas.Core.Map.Tests
{
    public sealed class CellTests
    {
        [Test]
        public void Default_ReturnsOriginCell()
        {
            Cell cell = default;

            Assert.That(cell.X, Is.EqualTo(0));
            Assert.That(cell.Z, Is.EqualTo(0));
        }

        [Test]
        public void GridGetCell_WithValidCoordinates_ReturnsCell()
        {
            var grid = new Grid(256, 256);

            Cell cell = grid.GetCell(5, 2);

            Assert.That(cell.X, Is.EqualTo(5));
            Assert.That(cell.Z, Is.EqualTo(2));
        }

        [Test]
        public void Deconstruct_ReturnsCoordinateComponents()
        {
            var grid = new Grid(256, 256);
            Cell cell = grid.GetCell(5, 2);

            var (x, z) = cell;

            Assert.That(x, Is.EqualTo(5));
            Assert.That(z, Is.EqualTo(2));
        }

        [Test]
        public void Equals_WithSameCoordinates_ReturnsTrue()
        {
            var grid = new Grid(256, 256);
            Cell left = grid.GetCell(5, 2);
            Cell right = grid.GetCell(5, 2);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentX_ReturnsFalse()
        {
            var grid = new Grid(256, 256);
            Cell left = grid.GetCell(5, 2);
            Cell right = grid.GetCell(6, 2);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentZ_ReturnsFalse()
        {
            var grid = new Grid(256, 256);
            Cell left = grid.GetCell(5, 2);
            Cell right = grid.GetCell(5, 3);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            var grid = new Grid(256, 256);
            Cell cell = grid.GetCell(5, 2);

            Assert.That(cell.Equals("Cell(5, 2)"), Is.False);
        }

        [Test]
        public void ToString_ReturnsCoordinateText()
        {
            var grid = new Grid(256, 256);
            Cell cell = grid.GetCell(5, 2);

            string value = cell.ToString();

            Assert.That(value, Is.EqualTo("Cell(5, 2)"));
        }
    }
}