#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Operations.Tests
{
    public sealed class OperationImplementationDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesOperationImplementationDefinition()
        {
            OperationDefinition operationDefinition = CreateOperation();
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default");
            DisplayName displayName = DisplayName.Create("Generate Height Default");

            var operationImplementationDefinition = new OperationImplementationDefinition(
                operationDefinition,
                symbol,
                displayName);

            Assert.That(operationImplementationDefinition.OperationDefinition, Is.SameAs(operationDefinition));
            Assert.That(operationImplementationDefinition.Symbol, Is.SameAs(symbol));
            Assert.That(operationImplementationDefinition.DisplayName, Is.SameAs(displayName));
        }

        [Test]
        public void Constructor_WithNullOperationDefinition_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationImplementationDefinition(
                    null!,
                    Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                    DisplayName.Create("Generate Height Default")));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationImplementationDefinition(
                    CreateOperation(),
                    null!,
                    DisplayName.Create("Generate Height Default")));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationImplementationDefinition(
                    CreateOperation(),
                    Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                    null!));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            OperationImplementationDefinition left = new(
                CreateOperation(),
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                DisplayName.Create("Generate Height Default"));

            OperationImplementationDefinition right = new(
                CreateAlternativeOperation(),
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                DisplayName.Create("Different Generate Height Default Display Name"));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            OperationDefinition operationDefinition = CreateOperation();

            OperationImplementationDefinition left = new(
                operationDefinition,
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                DisplayName.Create("Generate Height Default"));

            OperationImplementationDefinition right = new(
                operationDefinition,
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.fast"),
                DisplayName.Create("Generate Height Fast"));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            OperationImplementationDefinition operationImplementationDefinition =
                new(
                    CreateOperation(),
                    Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                    DisplayName.Create("Generate Height Default"));

            Assert.That(
                operationImplementationDefinition.Equals("OperationImplementationDefinition"),
                Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            OperationImplementationDefinition? left = null;
            OperationImplementationDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            OperationImplementationDefinition? left = new(
                CreateOperation(),
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                DisplayName.Create("Generate Height Default"));

            OperationImplementationDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsOperationImplementationDefinitionSummary()
        {
            OperationImplementationDefinition operationImplementationDefinition =
                new(
                    CreateOperation(),
                    Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                    DisplayName.Create("Generate Height Default"));

            string value = operationImplementationDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "OperationImplementationDefinition(Symbol: lokrain.atlas.tests.operation_implementation.generate_height.default, OperationDefinition: lokrain.atlas.tests.operation.generate_height, DisplayName: Generate Height Default)"));
        }

        private static OperationDefinition CreateOperation()
        {
            return new OperationDefinition(
                CreateSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height"));
        }

        private static OperationDefinition CreateAlternativeOperation()
        {
            return new OperationDefinition(
                CreateAlternativeSchema(),
                OperationKind.Create("lokrain.atlas.tests.operation_kind.moisture"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_moisture"),
                DisplayName.Create("Generate Moisture"));
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