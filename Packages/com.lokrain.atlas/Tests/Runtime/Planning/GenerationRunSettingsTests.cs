#nullable enable

using System;
using Lokrain.Atlas.Core.Map;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationRunSettingsTests
    {
        [Test]
        public void Constructor_WithGridAndSeed_StoresRunSettings()
        {
            var grid = new Grid(256, 256);
            var seed = new Seed(123UL);

            var settings = new GenerationRunSettings(grid, seed);

            Assert.That(settings.Grid, Is.SameAs(grid));
            Assert.That(settings.Seed, Is.EqualTo(seed));
        }

        [Test]
        public void Constructor_WithNullGrid_ThrowsArgumentNullException()
        {
            var seed = new Seed(123UL);

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRunSettings(null!, seed));
        }

        [Test]
        public void Equals_WithSameGridAndSeed_ReturnsTrue()
        {
            var grid = new Grid(256, 256);
            var seed = new Seed(123UL);

            var left = new GenerationRunSettings(grid, seed);
            var right = new GenerationRunSettings(grid, seed);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithEquivalentGridAndSameSeed_ReturnsTrue()
        {
            var left = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));

            var right = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentGrid_ReturnsFalse()
        {
            var left = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));

            var right = new GenerationRunSettings(
                new Grid(512, 256),
                new Seed(123UL));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentSeed_ReturnsFalse()
        {
            var left = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));

            var right = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(456UL));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            var settings = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));

            Assert.That(settings.Equals("GenerationRunSettings"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationRunSettings? left = null;
            GenerationRunSettings? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationRunSettings? left = new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));

            GenerationRunSettings? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }
    }
}