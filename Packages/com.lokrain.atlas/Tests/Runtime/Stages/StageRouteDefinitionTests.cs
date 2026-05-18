#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Stages.Tests
{
    public sealed class StageRouteDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesStageRouteDefinition()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteStepDefinition firstStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.generate_height",
                "Generate Height");

            StageRouteStepDefinition secondStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.smooth_height",
                "Smooth Height");

            var stageRouteDefinition = new StageRouteDefinition(
                stageDefinition,
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    firstStep,
                    secondStep
                });

            Assert.That(stageRouteDefinition.StageDefinition, Is.SameAs(stageDefinition));
            Assert.That(stageRouteDefinition.Symbol, Is.EqualTo(Symbol.Create("lokrain.atlas.tests.stage_route.default")));
            Assert.That(stageRouteDefinition.DisplayName, Is.EqualTo(DisplayName.Create("Default Route")));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions, Has.Count.EqualTo(2));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[0], Is.SameAs(firstStep));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[1], Is.SameAs(secondStep));
        }

        [Test]
        public void Constructor_WithNullStageDefinition_ThrowsArgumentNullException()
        {
            StageRouteStepDefinition routeStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.generate_height",
                "Generate Height");

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteDefinition(
                    null!,
                    Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                    DisplayName.Create("Default Route"),
                    new[]
                    {
                        routeStep
                    }));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteStepDefinition routeStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.generate_height",
                "Generate Height");

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteDefinition(
                    stageDefinition,
                    null!,
                    DisplayName.Create("Default Route"),
                    new[]
                    {
                        routeStep
                    }));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteStepDefinition routeStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.generate_height",
                "Generate Height");

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteDefinition(
                    stageDefinition,
                    Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                    null!,
                    new[]
                    {
                        routeStep
                    }));
        }

        [Test]
        public void Constructor_WithNullStageRouteStepDefinitions_ThrowsArgumentNullException()
        {
            StageDefinition stageDefinition = CreateStage();

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteDefinition(
                    stageDefinition,
                    Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                    DisplayName.Create("Default Route"),
                    null!));
        }

        [Test]
        public void Constructor_WithEmptyStageRouteStepDefinitions_ThrowsArgumentException()
        {
            StageDefinition stageDefinition = CreateStage();

            Assert.Throws<ArgumentException>(
                () => new StageRouteDefinition(
                    stageDefinition,
                    Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                    DisplayName.Create("Default Route"),
                    Array.Empty<StageRouteStepDefinition>()));
        }

        [Test]
        public void Constructor_WithNullStageRouteStepDefinitionEntry_ThrowsArgumentException()
        {
            StageDefinition stageDefinition = CreateStage();

            Assert.Throws<ArgumentException>(
                () => new StageRouteDefinition(
                    stageDefinition,
                    Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                    DisplayName.Create("Default Route"),
                    new StageRouteStepDefinition?[]
                    {
                        null
                    }!));
        }

        [Test]
        public void Constructor_WithDuplicateStageRouteStepSymbol_ThrowsArgumentException()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteStepDefinition firstStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.generate_height",
                "Generate Height");

            StageRouteStepDefinition duplicateStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.generate_height",
                "Generate Height Again");

            Assert.Throws<ArgumentException>(
                () => new StageRouteDefinition(
                    stageDefinition,
                    Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                    DisplayName.Create("Default Route"),
                    new[]
                    {
                        firstStep,
                        duplicateStep
                    }));
        }

        [Test]
        public void Constructor_WithRepeatedOperationDefinitionSymbol_CreatesStageRouteDefinition()
        {
            StageDefinition stageDefinition = CreateStage();
            Symbol operationDefinitionSymbol = Symbol.Create("lokrain.atlas.tests.operation.generate_height");

            StageRouteStepDefinition firstStep = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height_first_pass"),
                DisplayName.Create("Generate Height First Pass"),
                operationDefinitionSymbol);

            StageRouteStepDefinition secondStep = new(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height_second_pass"),
                DisplayName.Create("Generate Height Second Pass"),
                operationDefinitionSymbol);

            var stageRouteDefinition = new StageRouteDefinition(
                stageDefinition,
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    firstStep,
                    secondStep
                });

            Assert.That(stageRouteDefinition.StageRouteStepDefinitions, Has.Count.EqualTo(2));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[0].OperationDefinitionSymbol, Is.SameAs(operationDefinitionSymbol));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[1].OperationDefinitionSymbol, Is.SameAs(operationDefinitionSymbol));
        }

        [Test]
        public void Constructor_PreservesStageRouteStepOrder()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteStepDefinition firstStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.first",
                "First Step");

            StageRouteStepDefinition secondStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.second",
                "Second Step");

            StageRouteStepDefinition thirdStep = CreateRouteStep(
                "lokrain.atlas.tests.route_step.third",
                "Third Step");

            var stageRouteDefinition = new StageRouteDefinition(
                stageDefinition,
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    firstStep,
                    secondStep,
                    thirdStep
                });

            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[0], Is.SameAs(firstStep));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[1], Is.SameAs(secondStep));
            Assert.That(stageRouteDefinition.StageRouteStepDefinitions[2], Is.SameAs(thirdStep));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            StageRouteDefinition left = new(
                CreateStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_height",
                        "Generate Height")
                });

            StageRouteDefinition right = new(
                CreateAlternativeStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Alternative Default Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_moisture",
                        "Generate Moisture")
                });

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            StageRouteDefinition left = new(
                CreateStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_height",
                        "Generate Height")
                });

            StageRouteDefinition right = new(
                CreateStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.alternative"),
                DisplayName.Create("Alternative Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_height",
                        "Generate Height")
                });

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StageRouteDefinition stageRouteDefinition = new(
                CreateStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_height",
                        "Generate Height")
                });

            Assert.That(stageRouteDefinition.Equals("StageRouteDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageRouteDefinition? left = null;
            StageRouteDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            StageRouteDefinition? left = new(
                CreateStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_height",
                        "Generate Height")
                });

            StageRouteDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsStageRouteDefinitionSummary()
        {
            StageRouteDefinition stageRouteDefinition = new(
                CreateStage(),
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Route"),
                new[]
                {
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.generate_height",
                        "Generate Height"),
                    CreateRouteStep(
                        "lokrain.atlas.tests.route_step.smooth_height",
                        "Smooth Height")
                });

            string value = stageRouteDefinition.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StageRouteDefinition(Symbol: lokrain.atlas.tests.stage_route.default, StageDefinition: lokrain.atlas.tests.stage.landmass, StageRouteStepDefinitions: 2, DisplayName: Default Route)"));
        }

        private static StageDefinition CreateStage()
        {
            return new StageDefinition(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));
        }

        private static StageDefinition CreateAlternativeStage()
        {
            return new StageDefinition(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.climate"),
                Symbol.Create("lokrain.atlas.tests.stage.climate"),
                DisplayName.Create("Climate Stage"));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }

        private static StageRouteStepDefinition CreateRouteStep(
            string symbol,
            string displayName)
        {
            return new StageRouteStepDefinition(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"));
        }
    }
}