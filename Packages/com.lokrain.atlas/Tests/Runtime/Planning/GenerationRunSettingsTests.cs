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
            Grid grid = new(256, 256);
            Seed seed = new(123UL);

            GenerationRunSettings settings = new(grid, seed);

            Assert.That(settings.Grid, Is.SameAs(grid));
            Assert.That(settings.Seed, Is.EqualTo(seed));
        }

        [Test]
        public void Constructor_WithNullGrid_ThrowsArgumentNullException()
        {
            Seed seed = new(123UL);

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRunSettings(null!, seed));
        }

        [Test]
        public void Equals_WithSameGridAndSeed_ReturnsTrue()
        {
            Grid grid = new(256, 256);
            Seed seed = new(123UL);

            GenerationRunSettings left = new(grid, seed);
            GenerationRunSettings right = new(grid, seed);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithEquivalentGridAndSameSeed_ReturnsTrue()
        {
            GenerationRunSettings left = new(
                new Grid(256, 256),
                new Seed(123UL));

            GenerationRunSettings right = new(
                new Grid(256, 256),
                new Seed(123UL));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentGrid_ReturnsFalse()
        {
            GenerationRunSettings left = new(
                new Grid(256, 256),
                new Seed(123UL));

            GenerationRunSettings right = new(
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
            GenerationRunSettings left = new(
                new Grid(256, 256),
                new Seed(123UL));

            GenerationRunSettings right = new(
                new Grid(256, 256),
                new Seed(456UL));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            GenerationRunSettings settings = new(
                new Grid(256, 256),
                new Seed(123UL));

            Assert.That(settings.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationRunSettings settings = new(
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
            GenerationRunSettings? left = new(
                new Grid(256, 256),
                new Seed(123UL));

            GenerationRunSettings? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsRunSettingsSummary()
        {
            GenerationRunSettings settings = new(
                new Grid(256, 256),
                new Seed(123UL));

            string value = settings.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRunSettings(Grid: Grid(Width: 256, Depth: 256), Seed: 123)"));
        }
    }
}