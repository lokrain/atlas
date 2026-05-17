#nullable enable

using NUnit.Framework;

namespace Lokrain.Atlas.Core.Map.Tests
{
    public sealed class CellIndexTests
    {
        [Test]
        public void Default_ReturnsZeroIndex()
        {
            CellIndex index = default;

            Assert.That(index.Value, Is.EqualTo(0));
        }

        [Test]
        public void GridGetIndex_WithValidCoordinates_ReturnsCellIndex()
        {
            var grid = new Grid(256, 256);

            CellIndex index = grid.GetIndex(5, 2);

            Assert.That(index.Value, Is.EqualTo(517));
        }

        [Test]
        public void ExplicitIntConversion_ReturnsValue()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            int value = (int)index;

            Assert.That(value, Is.EqualTo(517));
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            var grid = new Grid(256, 256);
            CellIndex left = grid.GetIndex(5, 2);
            CellIndex right = grid.GetIndex(5, 2);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            var grid = new Grid(256, 256);
            CellIndex left = grid.GetIndex(5, 2);
            CellIndex right = grid.GetIndex(6, 2);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            Assert.That(index.Equals("CellIndex(517)"), Is.False);
        }

        [Test]
        public void ToString_ReturnsIndexText()
        {
            var grid = new Grid(256, 256);
            CellIndex index = grid.GetIndex(5, 2);

            string value = index.ToString();

            Assert.That(value, Is.EqualTo("CellIndex(517)"));
        }
    }
}