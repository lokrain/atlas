#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Resources.Tests
{
    public sealed class ResourceDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesResourceDefinition()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.resource.height");
            DisplayName displayName = DisplayName.Create("Height Resource");
            GenerationSchemaDefinition generationSchema = CreateSchema();

            var resourceDefinition = new ResourceDefinition(
                symbol,
                displayName,
                generationSchema);

            Assert.That(resourceDefinition.Symbol, Is.SameAs(symbol));
            Assert.That(resourceDefinition.DisplayName, Is.SameAs(displayName));
            Assert.That(resourceDefinition.GenerationSchema, Is.SameAs(generationSchema));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            DisplayName displayName = DisplayName.Create("Height Resource");
            GenerationSchemaDefinition generationSchema = CreateSchema();

            Assert.Throws<ArgumentNullException>(
                () => new ResourceDefinition(
                    null!,
                    displayName,
                    generationSchema));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.resource.height");
            GenerationSchemaDefinition generationSchema = CreateSchema();

            Assert.Throws<ArgumentNullException>(
                () => new ResourceDefinition(
                    symbol,
                    null!,
                    generationSchema));
        }

        [Test]
        public void Constructor_WithNullGenerationSchema_ThrowsArgumentNullException()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.resource.height");
            DisplayName displayName = DisplayName.Create("Height Resource");

            Assert.Throws<ArgumentNullException>(
                () => new ResourceDefinition(
                    symbol,
                    displayName,
                    null!));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            GenerationSchemaDefinition generationSchema = CreateSchema();

            ResourceDefinition left = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Height Resource"),
                generationSchema);

            ResourceDefinition right = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Different Height Resource Display Name"),
                generationSchema);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            GenerationSchemaDefinition generationSchema = CreateSchema();

            ResourceDefinition left = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Height Resource"),
                generationSchema);

            ResourceDefinition right = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.moisture"),
                DisplayName.Create("Moisture Resource"),
                generationSchema);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            ResourceDefinition resourceDefinition = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Height Resource"),
                CreateSchema());

            Assert.That(resourceDefinition.Equals("ResourceDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            ResourceDefinition? left = null;
            ResourceDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            ResourceDefinition? left = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Height Resource"),
                CreateSchema());

            ResourceDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsResourceDefinitionSummary()
        {
            ResourceDefinition resourceDefinition = new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Height Resource"),
                CreateSchema());

            string value = resourceDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "ResourceDefinition(Symbol: lokrain.atlas.tests.resource.height, GenerationSchema: lokrain.atlas.tests.schema.world, DisplayName: Height Resource)"));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }
    }
}