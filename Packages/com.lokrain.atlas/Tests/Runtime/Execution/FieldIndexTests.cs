#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class FieldIndexTests
    {
        [Test]
        public void Constructor_WithZeroValue_CreatesFieldIndex()
        {
            FieldIndex fieldIndex = new(0);

            Assert.That(fieldIndex.Value, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPositiveValue_CreatesFieldIndex()
        {
            FieldIndex fieldIndex = new(7);

            Assert.That(fieldIndex.Value, Is.EqualTo(7));
        }

        [Test]
        public void Constructor_WithNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FieldIndex(-1));
        }

        [Test]
        public void TryCreate_WithZeroValue_ReturnsTrueAndFieldIndex()
        {
            bool succeeded = FieldIndex.TryCreate(0, out FieldIndex fieldIndex);

            Assert.That(succeeded, Is.True);
            Assert.That(fieldIndex.Value, Is.EqualTo(0));
        }

        [Test]
        public void TryCreate_WithPositiveValue_ReturnsTrueAndFieldIndex()
        {
            bool succeeded = FieldIndex.TryCreate(7, out FieldIndex fieldIndex);

            Assert.That(succeeded, Is.True);
            Assert.That(fieldIndex.Value, Is.EqualTo(7));
        }

        [Test]
        public void TryCreate_WithNegativeValue_ReturnsFalseAndDefaultFieldIndex()
        {
            bool succeeded = FieldIndex.TryCreate(-1, out FieldIndex fieldIndex);

            Assert.That(succeeded, Is.False);
            Assert.That(fieldIndex, Is.EqualTo(default(FieldIndex)));
        }

        [Test]
        public void CompareTo_WithLowerValue_ReturnsPositiveValue()
        {
            FieldIndex left = new(7);
            FieldIndex right = new(3);

            Assert.That(left.CompareTo(right), Is.GreaterThan(0));
        }

        [Test]
        public void CompareTo_WithSameValue_ReturnsZero()
        {
            FieldIndex left = new(7);
            FieldIndex right = new(7);

            Assert.That(left.CompareTo(right), Is.EqualTo(0));
        }

        [Test]
        public void CompareTo_WithHigherValue_ReturnsNegativeValue()
        {
            FieldIndex left = new(3);
            FieldIndex right = new(7);

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareToObject_WithFieldIndex_ReturnsValueComparison()
        {
            FieldIndex left = new(3);
            object right = new FieldIndex(7);

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareToObject_WithNull_ReturnsPositiveValue()
        {
            FieldIndex fieldIndex = new(3);

            Assert.That(fieldIndex.CompareTo(null), Is.GreaterThan(0));
        }

        [Test]
        public void CompareToObject_WithDifferentObjectType_ThrowsArgumentException()
        {
            FieldIndex fieldIndex = new(3);

            Assert.Throws<ArgumentException>(() => fieldIndex.CompareTo("FieldIndex"));
        }

        [Test]
        public void Sort_OrdersByValue()
        {
            FieldIndex[] fieldIndices =
            {
                new(2),
                new(0),
                new(1),
            };

            Array.Sort(fieldIndices);

            Assert.That(
                fieldIndices,
                Is.EqualTo(new[]
                {
                    new FieldIndex(0),
                    new FieldIndex(1),
                    new FieldIndex(2),
                }));
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            FieldIndex left = new(7);
            FieldIndex right = new(7);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            FieldIndex left = new(7);
            FieldIndex right = new(8);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            FieldIndex fieldIndex = new(7);

            Assert.That(fieldIndex.Equals("FieldIndex"), Is.False);
        }

        [Test]
        public void ToString_ReturnsFieldIndexSummary()
        {
            FieldIndex fieldIndex = new(7);

            string value = fieldIndex.ToString();

            Assert.That(value, Is.EqualTo("FieldIndex(7)"));
        }
    }
}
