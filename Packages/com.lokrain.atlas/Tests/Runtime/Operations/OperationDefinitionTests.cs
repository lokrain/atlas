#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Operations.Tests
{
    public sealed class OperationDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesOperationDefinition()
        {
            GenerationSchemaDefinition generationSchema = CreateSchema();
            OperationKind operationKind = OperationKind.Create("lokrain.atlas.tests.operation_kind.height");
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.operation.generate_height");
            DisplayName displayName = DisplayName.Create("Generate Height");

            var operationDefinition = new OperationDefinition(
                generationSchema,
                operationKind,
                symbol,
                displayName);

            Assert.That(operationDefinition.GenerationSchema, Is.SameAs(generationSchema));
            Assert.That(operationDefinition.OperationKind, Is.SameAs(operationKind));
            Assert.That(operationDefinition.Symbol, Is.SameAs(symbol));
            Assert.That(operationDefinition.DisplayName, Is.SameAs(displayName));
        }

        [Test]
        public void Constructor_WithNullGenerationSchema_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationDefinition(
                    null!,
                    OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                    Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                    DisplayName.Create("Generate Height")));
        }

        [Test]
        public void Constructor_WithNullOperationKind_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationDefinition(
                    CreateSchema(),
                    null!,
                    Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                    DisplayName.Create("Generate Height")));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationDefinition(
                    CreateSchema(),
                    OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                    null!,
                    DisplayName.Create("Generate Height")));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationDefinition(
                    CreateSchema(),
                    OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                    Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                    null!));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            OperationDefinition left = new(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height"));

            OperationDefinition right = new(
                CreateAlternativeSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.moisture"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Different Generate Height Display Name"));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            OperationDefinition left = new(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height"));

            OperationDefinition right = new(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_moisture"),
                DisplayName.Create("Generate Moisture"));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            OperationDefinition operationDefinition = new(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height"));

            Assert.That(operationDefinition.Equals("OperationDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            OperationDefinition? left = null;
            OperationDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            OperationDefinition? left = new(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height"));

            OperationDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsOperationDefinitionSummary()
        {
            OperationDefinition operationDefinition = new(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height"));

            string value = operationDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "OperationDefinition(Symbol: lokrain.atlas.tests.operation.generate_height, OperationKind: lokrain.atlas.tests.operation_kind.height, GenerationSchema: lokrain.atlas.tests.schema.world, DisplayName: Generate Height)"));
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