#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class StageIndexTests
    {
        [Test]
        public void Constructor_WithZeroValue_CreatesStageIndex()
        {
            StageIndex stageIndex = new(0);

            Assert.That(stageIndex.Value, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPositiveValue_CreatesStageIndex()
        {
            StageIndex stageIndex = new(7);

            Assert.That(stageIndex.Value, Is.EqualTo(7));
        }

        [Test]
        public void Constructor_WithNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageIndex(-1));
        }

        [Test]
        public void TryCreate_WithZeroValue_ReturnsTrueAndStageIndex()
        {
            bool succeeded = StageIndex.TryCreate(0, out StageIndex stageIndex);

            Assert.That(succeeded, Is.True);
            Assert.That(stageIndex.Value, Is.EqualTo(0));
        }

        [Test]
        public void TryCreate_WithPositiveValue_ReturnsTrueAndStageIndex()
        {
            bool succeeded = StageIndex.TryCreate(7, out StageIndex stageIndex);

            Assert.That(succeeded, Is.True);
            Assert.That(stageIndex.Value, Is.EqualTo(7));
        }

        [Test]
        public void TryCreate_WithNegativeValue_ReturnsFalseAndDefaultStageIndex()
        {
            bool succeeded = StageIndex.TryCreate(-1, out StageIndex stageIndex);

            Assert.That(succeeded, Is.False);
            Assert.That(stageIndex, Is.EqualTo(default(StageIndex)));
        }

        [Test]
        public void CompareTo_WithLowerValue_ReturnsPositiveValue()
        {
            StageIndex left = new(7);
            StageIndex right = new(3);

            Assert.That(left.CompareTo(right), Is.GreaterThan(0));
        }

        [Test]
        public void CompareTo_WithSameValue_ReturnsZero()
        {
            StageIndex left = new(7);
            StageIndex right = new(7);

            Assert.That(left.CompareTo(right), Is.EqualTo(0));
        }

        [Test]
        public void CompareTo_WithHigherValue_ReturnsNegativeValue()
        {
            StageIndex left = new(3);
            StageIndex right = new(7);

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareToObject_WithStageIndex_ReturnsValueComparison()
        {
            StageIndex left = new(3);
            object right = new StageIndex(7);

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareToObject_WithNull_ReturnsPositiveValue()
        {
            StageIndex stageIndex = new(3);

            Assert.That(stageIndex.CompareTo(null), Is.GreaterThan(0));
        }

        [Test]
        public void CompareToObject_WithDifferentObjectType_ThrowsArgumentException()
        {
            StageIndex stageIndex = new(3);

            Assert.Throws<ArgumentException>(() => stageIndex.CompareTo("StageIndex"));
        }

        [Test]
        public void Sort_OrdersByValue()
        {
            StageIndex[] stageIndices =
            {
                new(2),
                new(0),
                new(1),
            };

            Array.Sort(stageIndices);

            Assert.That(
                stageIndices,
                Is.EqualTo(new[]
                {
                    new StageIndex(0),
                    new StageIndex(1),
                    new StageIndex(2),
                }));
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            StageIndex left = new(7);
            StageIndex right = new(7);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            StageIndex left = new(7);
            StageIndex right = new(8);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StageIndex stageIndex = new(7);

            Assert.That(stageIndex.Equals("StageIndex"), Is.False);
        }

        [Test]
        public void ToString_ReturnsStageIndexSummary()
        {
            StageIndex stageIndex = new(7);

            string value = stageIndex.ToString();

            Assert.That(value, Is.EqualTo("StageIndex(7)"));
        }
    }
}
