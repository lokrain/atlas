#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Recipes.Tests
{
    public sealed class StageRouteStepImplementationChoiceTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesChoice()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            var choice = new StageRouteStepImplementationChoice(
                routeStepDefinition,
                operationDefinition,
                operationContract,
                implementationDefinition);

            Assert.That(choice.StageRouteStepDefinition, Is.SameAs(routeStepDefinition));
            Assert.That(choice.OperationDefinition, Is.SameAs(operationDefinition));
            Assert.That(choice.OperationContract, Is.SameAs(operationContract));
            Assert.That(choice.OperationImplementationDefinition, Is.SameAs(implementationDefinition));
        }

        [Test]
        public void Constructor_WithNullStageRouteStepDefinition_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepImplementationChoice(
                    null!,
                    operationDefinition,
                    operationContract,
                    implementationDefinition));
        }

        [Test]
        public void Constructor_WithNullOperationDefinition_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepImplementationChoice(
                    routeStepDefinition,
                    null!,
                    operationContract,
                    implementationDefinition));
        }

        [Test]
        public void Constructor_WithNullOperationContract_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepImplementationChoice(
                    routeStepDefinition,
                    operationDefinition,
                    null!,
                    implementationDefinition));
        }

        [Test]
        public void Constructor_WithNullOperationImplementationDefinition_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            Assert.Throws<ArgumentNullException>(
                () => new StageRouteStepImplementationChoice(
                    routeStepDefinition,
                    operationDefinition,
                    operationContract,
                    null!));
        }

        [Test]
        public void Constructor_WithRouteStepReferencingDifferentOperation_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(Symbol.Create("lokrain.atlas.tests.operation.alternative"));

            Assert.Throws<ArgumentException>(
                () => new StageRouteStepImplementationChoice(
                    routeStepDefinition,
                    operationDefinition,
                    operationContract,
                    implementationDefinition));
        }

        [Test]
        public void Constructor_WithOperationContractForDifferentOperation_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationDefinition alternativeOperationDefinition = CreateAlternativeOperation(schema);

            OperationContract operationContract =
                CreateContract(schema, alternativeOperationDefinition);

            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            Assert.Throws<ArgumentException>(
                () => new StageRouteStepImplementationChoice(
                    routeStepDefinition,
                    operationDefinition,
                    operationContract,
                    implementationDefinition));
        }

        [Test]
        public void Constructor_WithImplementationForDifferentOperation_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationDefinition alternativeOperationDefinition = CreateAlternativeOperation(schema);

            OperationContract operationContract = CreateContract(schema, operationDefinition);

            OperationImplementationDefinition implementationDefinition =
                CreateAlternativeImplementation(alternativeOperationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            Assert.Throws<ArgumentException>(
                () => new StageRouteStepImplementationChoice(
                    routeStepDefinition,
                    operationDefinition,
                    operationContract,
                    implementationDefinition));
        }

        [Test]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            var left = new StageRouteStepImplementationChoice(
                routeStepDefinition,
                operationDefinition,
                operationContract,
                implementationDefinition);

            var right = new StageRouteStepImplementationChoice(
                routeStepDefinition,
                operationDefinition,
                operationContract,
                implementationDefinition);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentStageRouteStepDefinition_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            OperationImplementationDefinition implementationDefinition =
                CreateImplementation(operationDefinition);

            var left = new StageRouteStepImplementationChoice(
                CreateRouteStep(operationDefinition.Symbol),
                operationDefinition,
                operationContract,
                implementationDefinition);

            var right = new StageRouteStepImplementationChoice(
                CreateAlternativeRouteStep(operationDefinition.Symbol),
                operationDefinition,
                operationContract,
                implementationDefinition);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentOperationImplementationDefinition_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);
            OperationContract operationContract = CreateContract(schema, operationDefinition);
            StageRouteStepDefinition routeStepDefinition =
                CreateRouteStep(operationDefinition.Symbol);

            var left = new StageRouteStepImplementationChoice(
                routeStepDefinition,
                operationDefinition,
                operationContract,
                CreateImplementation(operationDefinition));

            var right = new StageRouteStepImplementationChoice(
                routeStepDefinition,
                operationDefinition,
                operationContract,
                CreateSecondImplementation(operationDefinition));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            var choice = new StageRouteStepImplementationChoice(
                CreateRouteStep(operationDefinition.Symbol),
                operationDefinition,
                CreateContract(schema, operationDefinition),
                CreateImplementation(operationDefinition));

            Assert.That(choice.Equals("StageRouteStepImplementationChoice"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageRouteStepImplementationChoice? left = null;
            StageRouteStepImplementationChoice? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            StageRouteStepImplementationChoice? left = new(
                CreateRouteStep(operationDefinition.Symbol),
                operationDefinition,
                CreateContract(schema, operationDefinition),
                CreateImplementation(operationDefinition));

            StageRouteStepImplementationChoice? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsChoiceSummary()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            var choice = new StageRouteStepImplementationChoice(
                CreateRouteStep(operationDefinition.Symbol),
                operationDefinition,
                CreateContract(schema, operationDefinition),
                CreateImplementation(operationDefinition));

            string value = choice.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StageRouteStepImplementationChoice(StageRouteStepDefinition: lokrain.atlas.tests.route_step.generate_height, OperationDefinition: lokrain.atlas.tests.operation.generate_height, OperationImplementationDefinition: lokrain.atlas.tests.operation_implementation.generate_height.default)"));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }

        private static OperationDefinition CreateOperation(GenerationSchemaDefinition schema)
        {
            return new OperationDefinition(
                schema,
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height Operation"));
        }

        private static OperationDefinition CreateAlternativeOperation(GenerationSchemaDefinition schema)
        {
            return new OperationDefinition(
                schema,
                OperationKind.Create("lokrain.atlas.tests.operation_kind.moisture"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_moisture"),
                DisplayName.Create("Generate Moisture Operation"));
        }

        private static OperationContract CreateContract(
            GenerationSchemaDefinition schema,
            OperationDefinition operationDefinition)
        {
            return new OperationContract(
                operationDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.height",
                        "Height Resource")
                });
        }

        private static OperationImplementationDefinition CreateImplementation(
            OperationDefinition operationDefinition)
        {
            return new OperationImplementationDefinition(
                operationDefinition,
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.default"),
                DisplayName.Create("Generate Height Default"));
        }

        private static OperationImplementationDefinition CreateSecondImplementation(
            OperationDefinition operationDefinition)
        {
            return new OperationImplementationDefinition(
                operationDefinition,
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_height.fast"),
                DisplayName.Create("Generate Height Fast"));
        }

        private static OperationImplementationDefinition CreateAlternativeImplementation(
            OperationDefinition operationDefinition)
        {
            return new OperationImplementationDefinition(
                operationDefinition,
                Symbol.Create("lokrain.atlas.tests.operation_implementation.generate_moisture.default"),
                DisplayName.Create("Generate Moisture Default"));
        }

        private static StageRouteStepDefinition CreateRouteStep(Symbol operationDefinitionSymbol)
        {
            return new StageRouteStepDefinition(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height"),
                DisplayName.Create("Generate Height Route Step"),
                operationDefinitionSymbol);
        }

        private static StageRouteStepDefinition CreateAlternativeRouteStep(Symbol operationDefinitionSymbol)
        {
            return new StageRouteStepDefinition(
                Symbol.Create("lokrain.atlas.tests.route_step.generate_height_second_pass"),
                DisplayName.Create("Generate Height Second Pass Route Step"),
                operationDefinitionSymbol);
        }

        private static ResourceDefinition CreateResource(
            GenerationSchemaDefinition schema,
            string symbol,
            string displayName)
        {
            return new ResourceDefinition(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                schema);
        }
    }
}