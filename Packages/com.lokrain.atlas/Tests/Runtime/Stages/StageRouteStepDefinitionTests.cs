#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Stages.Tests
{
    public sealed class StageRouteStepDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesStageRouteStepDefinition()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.route_step.generate_height");
            DisplayName displayName = DisplayName.Create("Generate Height");
            Symbol operationDefinitionSymbol = Symbol.Create("lokrain.atlas.tests.operation.generate_height");

            var stageRouteStepDefinition = new StageRouteStepDefinition(
                symbol,
                displayName,
                operationDefinitionSymbol);

            Assert.That(stageRouteStepDefinition.Symbol, Is.SameAs(symbol));
            Assert.That(stageRouteStepDefinition.DisplayName, Is.SameAs(displayName));
            Assert.That(stageRouteStepDefinition.OperationDefinitionSymbol, Is.SameAs(operationDefinitionSymbol));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            DisplayName displayName = DisplayName.Create("Generate Height");
            Symbol operationDefinitionSymbol = Symbol.Create("lokrain.atlas.tests.operation.generate_height");

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepDefinition(
                    null!,
                    displayName,
                    operationDefinitionSymbol));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.route_step.generate_height");
            Symbol operationDefinitionSymbol = Symbol.Create("lokrain.atlas.tests.operation.generate_height");

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepDefinition(
                    symbol,
                    null!,
                    operationDefinitionSymbol));
        }

        [Test]
        public void Constructor_WithNullOperationDefinitionSymbol_ThrowsArgumentNullException()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.route_step.generate_height");
            DisplayName displayName = DisplayName.Create("Generate Height");

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepDefinition(
                    symbol,
                    displayName,
                    null!));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            StageRouteStepDefinition left = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));

            StageRouteStepDefinition right = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height With Different Display Name"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height_alternative"));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            StageRouteStepDefinition left = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));

            StageRouteStepDefinition right = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_moisture"),
                DisplayName.Create("Generate Moisture"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StageRouteStepDefinition stageRouteStepDefinition = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));

            Assert.That(stageRouteStepDefinition.Equals("StageRouteStepDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageRouteStepDefinition? left = null;
            StageRouteStepDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            StageRouteStepDefinition? left = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));

            StageRouteStepDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsStageRouteStepDefinitionSummary()
        {
            StageRouteStepDefinition stageRouteStepDefinition = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));

            string value = stageRouteStepDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StageRouteStepDefinition(Symbol: lokrain.atlas.tests.route_step.generate_height, OperationDefinitionSymbol: lokrain.atlas.tests.operation.generate_height, DisplayName: Generate Height)"));
        }
    }
}