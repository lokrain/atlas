#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Stages.Tests
{
    public sealed class StageDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesStageDefinition()
        {
            GenerationSchemaDefinition generationSchema = CreateSchema();
            StageKind stageKind = StageKind.Create("lokrain.atlas.tests.stage_kind.landmass");
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.stage.landmass");
            DisplayName displayName = DisplayName.Create("Landmass Stage");

            var stageDefinition = new StageDefinition(
                generationSchema,
                stageKind,
                symbol,
                displayName);

            Assert.That(stageDefinition.GenerationSchema, Is.SameAs(generationSchema));
            Assert.That(stageDefinition.StageKind, Is.SameAs(stageKind));
            Assert.That(stageDefinition.Symbol, Is.SameAs(symbol));
            Assert.That(stageDefinition.DisplayName, Is.SameAs(displayName));
        }

        [Test]
        public void Constructor_WithNullGenerationSchema_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageDefinition(
                    null!,
                    StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                    Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                    DisplayName.Create("Landmass Stage")));
        }

        [Test]
        public void Constructor_WithNullStageKind_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageDefinition(
                    CreateSchema(),
                    null!,
                    Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                    DisplayName.Create("Landmass Stage")));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageDefinition(
                    CreateSchema(),
                    StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                    null!,
                    DisplayName.Create("Landmass Stage")));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageDefinition(
                    CreateSchema(),
                    StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                    Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                    null!));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            StageDefinition left = new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));

            StageDefinition right = new(
                CreateAlternativeSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.climate"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Different Landmass Stage Display Name"));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            StageDefinition left = new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));

            StageDefinition right = new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.climate"),
                DisplayName.Create("Climate Stage"));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StageDefinition stageDefinition = new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));

            Assert.That(stageDefinition.Equals("StageDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageDefinition? left = null;
            StageDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            StageDefinition? left = new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));

            StageDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsStageDefinitionSummary()
        {
            StageDefinition stageDefinition = new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));

            string value = stageDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StageDefinition(Symbol: lokrain.atlas.tests.stage.landmass, StageKind: lokrain.atlas.tests.stage_kind.landmass, GenerationSchema: lokrain.atlas.tests.schema.world, DisplayName: Landmass Stage)"));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }

        private static GenerationSchemaDefinition CreateAlternativeSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.alternative"),
                DisplayName.Create("Alternative Test Schema"));
        }
    }
}