#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Stages.Tests
{
    public sealed class StageKindTests
    {
        [Test]
        public void Constructor_WithValidSymbol_CreatesStageKind()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.stage_kind.landmass");

            var stageKind = new StageKind(symbol);

            Assert.That(stageKind.Symbol, Is.SameAs(symbol));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageKind(null!));
        }

        [TestCase("lokrain.atlas.tests.stage_kind.landmass")]
        [TestCase("lokrain.atlas.tests.stage_kind.climate")]
        public void Create_WithValidSymbol_ReturnsStageKind(string symbol)
        {
            StageKind stageKind = StageKind.Create(symbol);

            Assert.That(stageKind.Symbol.Value, Is.EqualTo(symbol));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Landmass")]
        [TestCase("0lokrain.atlas.tests.stage_kind.landmass")]
        [TestCase("lokrain atlas tests stage kind landmass")]
        public void Create_WithInvalidSymbol_ThrowsArgumentException(string? symbol)
        {
            Assert.Throws<ArgumentException>(
                () => StageKind.Create(symbol));
        }

        [TestCase("lokrain.atlas.tests.stage_kind.landmass")]
        [TestCase("lokrain.atlas.tests.stage_kind.climate")]
        public void TryCreate_WithValidSymbol_ReturnsTrueAndStageKind(string symbol)
        {
            bool succeeded = StageKind.TryCreate(symbol, out StageKind? stageKind);

            Assert.That(succeeded, Is.True);
            Assert.That(stageKind, Is.Not.Null);
            Assert.That(stageKind!.Symbol.Value, Is.EqualTo(symbol));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Landmass")]
        [TestCase("lokrain atlas tests stage kind landmass")]
        public void TryCreate_WithInvalidSymbol_ReturnsFalseAndNull(string? symbol)
        {
            bool succeeded = StageKind.TryCreate(symbol, out StageKind? stageKind);

            Assert.That(succeeded, Is.False);
            Assert.That(stageKind, Is.Null);
        }

        [TestCase("lokrain.atlas.tests.stage_kind.landmass")]
        [TestCase("lokrain.atlas.tests.stage_kind.climate")]
        public void IsValid_WithValidSymbol_ReturnsTrue(string symbol)
        {
            bool isValid = StageKind.IsValid(symbol);

            Assert.That(isValid, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Landmass")]
        [TestCase("lokrain atlas tests stage kind landmass")]
        public void IsValid_WithInvalidSymbol_ReturnsFalse(string? symbol)
        {
            bool isValid = StageKind.IsValid(symbol);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            StageKind left = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");
            StageKind right = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            StageKind left = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");
            StageKind right = StageKind.Create("lokrain.atlas.tests.stage_kind.climate");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            StageKind stageKind = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");

            Assert.That(stageKind.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StageKind stageKind = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");

            Assert.That(stageKind.Equals("StageKind"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageKind? left = null;
            StageKind? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            StageKind? left = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");
            StageKind? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsSymbolValue()
        {
            StageKind stageKind = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");

            string value = stageKind.ToString();

            Assert.That(value, Is.EqualTo("lokrain.atlas.tests.stage_kind.landmass"));
        }
    }
}