#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Fields.Tests
{
    public sealed class FieldDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesFieldDefinition()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition();
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.field.height");
            DisplayName displayName = DisplayName.Create("Height Field");

            FieldDefinition fieldDefinition = new(
                resourceDefinition,
                symbol,
                displayName,
                FieldShape.Grid,
                FieldValueKind.Single);

            Assert.That(fieldDefinition.ResourceDefinition, Is.SameAs(resourceDefinition));
            Assert.That(fieldDefinition.Symbol, Is.SameAs(symbol));
            Assert.That(fieldDefinition.DisplayName, Is.SameAs(displayName));
            Assert.That(fieldDefinition.Shape, Is.EqualTo(FieldShape.Grid));
            Assert.That(fieldDefinition.ValueKind, Is.EqualTo(FieldValueKind.Single));
        }

        [Test]
        public void Constructor_WithUnknownShapeAndValueKind_CreatesFieldDefinition()
        {
            FieldDefinition fieldDefinition = new(
                CreateResourceDefinition(),
                Symbol.Create("lokrain.atlas.tests.field.unspecified"),
                DisplayName.Create("Unspecified Field"),
                FieldShape.Unknown,
                FieldValueKind.Unknown);

            Assert.That(fieldDefinition.Shape, Is.EqualTo(FieldShape.Unknown));
            Assert.That(fieldDefinition.ValueKind, Is.EqualTo(FieldValueKind.Unknown));
        }

        [Test]
        public void Constructor_WithNullResourceDefinition_ThrowsArgumentNullException()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.field.height");
            DisplayName displayName = DisplayName.Create("Height Field");

            Assert.Throws<ArgumentNullException>(
                () => new FieldDefinition(
                    null!,
                    symbol,
                    displayName,
                    FieldShape.Grid,
                    FieldValueKind.Single));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition();
            DisplayName displayName = DisplayName.Create("Height Field");

            Assert.Throws<ArgumentNullException>(
                () => new FieldDefinition(
                    resourceDefinition,
                    null!,
                    displayName,
                    FieldShape.Grid,
                    FieldValueKind.Single));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition();
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.field.height");

            Assert.Throws<ArgumentNullException>(
                () => new FieldDefinition(
                    resourceDefinition,
                    symbol,
                    null!,
                    FieldShape.Grid,
                    FieldValueKind.Single));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            FieldDefinition left = new(
                CreateResourceDefinition(),
                Symbol.Create("lokrain.atlas.tests.field.height"),
                DisplayName.Create("Height Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

            FieldDefinition right = new(
                CreateAlternativeResourceDefinition(),
                Symbol.Create("lokrain.atlas.tests.field.height"),
                DisplayName.Create("Different Height Field Display Name"),
                FieldShape.Scalar,
                FieldValueKind.Double);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition();

            FieldDefinition left = new(
                resourceDefinition,
                Symbol.Create("lokrain.atlas.tests.field.height"),
                DisplayName.Create("Height Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

            FieldDefinition right = new(
                resourceDefinition,
                Symbol.Create("lokrain.atlas.tests.field.moisture"),
                DisplayName.Create("Moisture Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            FieldDefinition fieldDefinition = new(
                CreateResourceDefinition(),
                Symbol.Create("lokrain.atlas.tests.field.height"),
                DisplayName.Create("Height Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

            Assert.That(fieldDefinition.Equals("FieldDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            FieldDefinition? left = null;
            FieldDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            FieldDefinition? left = new(
                CreateResourceDefinition(),
                Symbol.Create("lokrain.atlas.tests.field.height"),
                DisplayName.Create("Height Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

            FieldDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsFieldDefinitionSummary()
        {
            FieldDefinition fieldDefinition = new(
                CreateResourceDefinition(),
                Symbol.Create("lokrain.atlas.tests.field.height"),
                DisplayName.Create("Height Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

            string value = fieldDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "FieldDefinition(Symbol: lokrain.atlas.tests.field.height, ResourceDefinition: lokrain.atlas.tests.resource.height, Shape: Grid, ValueKind: Single, DisplayName: Height Field)"));
        }

        private static ResourceDefinition CreateResourceDefinition()
        {
            return new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.height"),
                DisplayName.Create("Height Resource"),
                CreateSchema());
        }

        private static ResourceDefinition CreateAlternativeResourceDefinition()
        {
            return new ResourceDefinition(
                Symbol.Create("lokrain.atlas.tests.resource.moisture"),
                DisplayName.Create("Moisture Resource"),
                CreateSchema());
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }
    }
}
