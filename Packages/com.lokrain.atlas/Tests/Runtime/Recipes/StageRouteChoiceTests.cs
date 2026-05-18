#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Recipes.Tests
{
    public sealed class StageRouteChoiceTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesStageRouteChoice()
        {
            StageDefinition stageDefinition = CreateStage();
            StageRouteDefinition stageRouteDefinition = CreateStageRoute(stageDefinition);
            StageContract stageContract = CreateStageContract(stageDefinition);

            StageRouteChoice stageRouteChoice = new(
                stageDefinition,
                stageRouteDefinition,
                stageContract);

            Assert.That(stageRouteChoice.StageDefinition, Is.SameAs(stageDefinition));
            Assert.That(stageRouteChoice.StageRouteDefinition, Is.SameAs(stageRouteDefinition));
            Assert.That(stageRouteChoice.StageContract, Is.SameAs(stageContract));
        }

        [Test]
        public void Constructor_WithNullStageDefinition_ThrowsArgumentNullException()
        {
            StageDefinition stageDefinition = CreateStage();

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteChoice(
                    null!,
                    CreateStageRoute(stageDefinition),
                    CreateStageContract(stageDefinition)));
        }

        [Test]
        public void Constructor_WithNullStageRouteDefinition_ThrowsArgumentNullException()
        {
            StageDefinition stageDefinition = CreateStage();

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteChoice(
                    stageDefinition,
                    null!,
                    CreateStageContract(stageDefinition)));
        }

        [Test]
        public void Constructor_WithNullStageContract_ThrowsArgumentNullException()
        {
            StageDefinition stageDefinition = CreateStage();

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteChoice(
                    stageDefinition,
                    CreateStageRoute(stageDefinition),
                    null!));
        }

        [Test]
        public void Constructor_WithStageRouteDefinitionForDifferentStage_ThrowsArgumentException()
        {
            StageDefinition stageDefinition = CreateStage();
            StageDefinition alternativeStageDefinition = CreateAlternativeStage();

            Assert.Throws<ArgumentException>(
                () => new StageRouteChoice(
                    stageDefinition,
                    CreateStageRoute(alternativeStageDefinition),
                    CreateStageContract(stageDefinition)));
        }

        [Test]
        public void Constructor_WithStageContractForDifferentStage_ThrowsArgumentException()
        {
            StageDefinition stageDefinition = CreateStage();
            StageDefinition alternativeStageDefinition = CreateAlternativeStage();

            Assert.Throws<ArgumentException>(
                () => new StageRouteChoice(
                    stageDefinition,
                    CreateStageRoute(stageDefinition),
                    CreateStageContract(alternativeStageDefinition)));
        }

        [Test]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            StageDefinition stageDefinition = CreateStage();
            StageRouteDefinition stageRouteDefinition = CreateStageRoute(stageDefinition);
            StageContract stageContract = CreateStageContract(stageDefinition);

            StageRouteChoice left = new(
                stageDefinition,
                stageRouteDefinition,
                stageContract);

            StageRouteChoice right = new(
                stageDefinition,
                stageRouteDefinition,
                stageContract);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithEquivalentStageContractForSameStage_ReturnsTrue()
        {
            StageDefinition stageDefinition = CreateStage();
            StageRouteDefinition stageRouteDefinition = CreateStageRoute(stageDefinition);

            StageRouteChoice left = new(
                stageDefinition,
                stageRouteDefinition,
                CreateStageContract(stageDefinition));

            StageRouteChoice right = new(
                stageDefinition,
                stageRouteDefinition,
                CreateAlternativeStageContract(stageDefinition));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentStageDefinition_ReturnsFalse()
        {
            StageDefinition leftStageDefinition = CreateStage();
            StageRouteDefinition leftStageRouteDefinition = CreateStageRoute(leftStageDefinition);
            StageContract leftStageContract = CreateStageContract(leftStageDefinition);

            StageDefinition rightStageDefinition = CreateAlternativeStage();
            StageRouteDefinition rightStageRouteDefinition = CreateStageRoute(rightStageDefinition);
            StageContract rightStageContract = CreateStageContract(rightStageDefinition);

            StageRouteChoice left = new(
                leftStageDefinition,
                leftStageRouteDefinition,
                leftStageContract);

            StageRouteChoice right = new(
                rightStageDefinition,
                rightStageRouteDefinition,
                rightStageContract);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentStageRouteDefinition_ReturnsFalse()
        {
            StageDefinition stageDefinition = CreateStage();
            StageContract stageContract = CreateStageContract(stageDefinition);

            StageRouteChoice left = new(
                stageDefinition,
                CreateStageRoute(stageDefinition),
                stageContract);

            StageRouteChoice right = new(
                stageDefinition,
                CreateAlternativeStageRoute(stageDefinition),
                stageContract);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteChoice stageRouteChoice = new(
                stageDefinition,
                CreateStageRoute(stageDefinition),
                CreateStageContract(stageDefinition));

            Assert.That(stageRouteChoice.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteChoice stageRouteChoice = new(
                stageDefinition,
                CreateStageRoute(stageDefinition),
                CreateStageContract(stageDefinition));

            Assert.That(stageRouteChoice.Equals("StageRouteChoice"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageRouteChoice? left = null;
            StageRouteChoice? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteChoice? left = new StageRouteChoice(
                stageDefinition,
                CreateStageRoute(stageDefinition),
                CreateStageContract(stageDefinition));

            StageRouteChoice? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsStageRouteChoiceSummary()
        {
            StageDefinition stageDefinition = CreateStage();

            StageRouteChoice stageRouteChoice = new(
                stageDefinition,
                CreateStageRoute(stageDefinition),
                CreateStageContract(stageDefinition));

            string value = stageRouteChoice.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StageRouteChoice(StageDefinition: lokrain.atlas.tests.stage.landmass, StageRouteDefinition: lokrain.atlas.tests.stage_route.default)"));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }

        private static StageDefinition CreateStage()
        {
            return CreateStage(CreateSchema());
        }

        private static StageDefinition CreateAlternativeStage()
        {
            return new(
                CreateSchema(),
                StageKind.Create("lokrain.atlas.tests.stage_kind.climate"),
                Symbol.Create("lokrain.atlas.tests.stage.climate"),
                DisplayName.Create("Climate Stage"));
        }

        private static StageDefinition CreateStage(GenerationSchemaDefinition schema)
        {
            return new(
                schema,
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.landmass"),
                DisplayName.Create("Landmass Stage"));
        }

        private static StageRouteDefinition CreateStageRoute(StageDefinition stageDefinition)
        {
            return new(
                stageDefinition,
                Symbol.Create("lokrain.atlas.tests.stage_route.default"),
                DisplayName.Create("Default Stage Route"),
                new[]
                {
                    CreateStageRouteStep(
                        "lokrain.atlas.tests.route_step.generate_landmass",
                        "Generate Landmass")
                });
        }

        private static StageRouteDefinition CreateAlternativeStageRoute(StageDefinition stageDefinition)
        {
            return new(
                stageDefinition,
                Symbol.Create("lokrain.atlas.tests.stage_route.alternative"),
                DisplayName.Create("Alternative Stage Route"),
                new[]
                {
                    CreateStageRouteStep(
                        "lokrain.atlas.tests.route_step.generate_landmass_alternative",
                        "Generate Landmass Alternative")
                });
        }

        private static StageRouteStepDefinition CreateStageRouteStep(
            string symbol,
            string displayName)
        {
            return new(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                Symbol.Create("lokrain.atlas.tests.operation.generate_landmass"));
        }

        private static StageContract CreateStageContract(StageDefinition stageDefinition)
        {
            return new(
                stageDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        stageDefinition.GenerationSchema,
                        "lokrain.atlas.tests.resource.landmass",
                        "Landmass Resource")
                });
        }

        private static StageContract CreateAlternativeStageContract(StageDefinition stageDefinition)
        {
            return new(
                stageDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        stageDefinition.GenerationSchema,
                        "lokrain.atlas.tests.resource.alternative_landmass",
                        "Alternative Landmass Resource")
                });
        }

        private static ResourceDefinition CreateResource(
            GenerationSchemaDefinition schema,
            string symbol,
            string displayName)
        {
            return new(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                schema);
        }
    }
}