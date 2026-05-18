#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class OperationIndexTests
    {
        [Test]
        public void Constructor_WithZeroValue_CreatesOperationIndex()
        {
            OperationIndex operationIndex = new(0);

            Assert.That(operationIndex.Value, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPositiveValue_CreatesOperationIndex()
        {
            OperationIndex operationIndex = new(7);

            Assert.That(operationIndex.Value, Is.EqualTo(7));
        }

        [Test]
        public void Constructor_WithNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OperationIndex(-1));
        }

        [Test]
        public void TryCreate_WithZeroValue_ReturnsTrueAndOperationIndex()
        {
            bool succeeded = OperationIndex.TryCreate(0, out OperationIndex operationIndex);

            Assert.That(succeeded, Is.True);
            Assert.That(operationIndex.Value, Is.EqualTo(0));
        }

        [Test]
        public void TryCreate_WithPositiveValue_ReturnsTrueAndOperationIndex()
        {
            bool succeeded = OperationIndex.TryCreate(7, out OperationIndex operationIndex);

            Assert.That(succeeded, Is.True);
            Assert.That(operationIndex.Value, Is.EqualTo(7));
        }

        [Test]
        public void TryCreate_WithNegativeValue_ReturnsFalseAndDefaultOperationIndex()
        {
            bool succeeded = OperationIndex.TryCreate(-1, out OperationIndex operationIndex);

            Assert.That(succeeded, Is.False);
            Assert.That(operationIndex, Is.EqualTo(default(OperationIndex)));
        }

        [Test]
        public void CompareTo_WithLowerValue_ReturnsPositiveValue()
        {
            OperationIndex left = new(7);
            OperationIndex right = new(3);

            Assert.That(left.CompareTo(right), Is.GreaterThan(0));
        }

        [Test]
        public void CompareTo_WithSameValue_ReturnsZero()
        {
            OperationIndex left = new(7);
            OperationIndex right = new(7);

            Assert.That(left.CompareTo(right), Is.EqualTo(0));
        }

        [Test]
        public void CompareTo_WithHigherValue_ReturnsNegativeValue()
        {
            OperationIndex left = new(3);
            OperationIndex right = new(7);

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareToObject_WithOperationIndex_ReturnsValueComparison()
        {
            OperationIndex left = new(3);
            object right = new OperationIndex(7);

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareToObject_WithNull_ReturnsPositiveValue()
        {
            OperationIndex operationIndex = new(3);

            Assert.That(operationIndex.CompareTo(null), Is.GreaterThan(0));
        }

        [Test]
        public void CompareToObject_WithDifferentObjectType_ThrowsArgumentException()
        {
            OperationIndex operationIndex = new(3);

            Assert.Throws<ArgumentException>(() => operationIndex.CompareTo("OperationIndex"));
        }

        [Test]
        public void Sort_OrdersByValue()
        {
            OperationIndex[] operationIndices =
            {
                new(2),
                new(0),
                new(1),
            };

            Array.Sort(operationIndices);

            Assert.That(
                operationIndices,
                Is.EqualTo(new[]
                {
                    new OperationIndex(0),
                    new OperationIndex(1),
                    new OperationIndex(2),
                }));
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            OperationIndex left = new(7);
            OperationIndex right = new(7);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            OperationIndex left = new(7);
            OperationIndex right = new(8);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            OperationIndex operationIndex = new(7);

            Assert.That(operationIndex.Equals("OperationIndex"), Is.False);
        }

        [Test]
        public void ToString_ReturnsOperationIndexSummary()
        {
            OperationIndex operationIndex = new(7);

            string value = operationIndex.ToString();

            Assert.That(value, Is.EqualTo("OperationIndex(7)"));
        }
    }
}
