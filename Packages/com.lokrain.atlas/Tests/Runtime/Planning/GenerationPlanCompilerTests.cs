#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationPlanCompilerTests
    {
        [Test]
        public void Compile_WithNullRequest_ThrowsArgumentNullException()
        {
            GenerationPlanCompiler compiler = new();

            Assert.Throws<ArgumentNullException>(
                () => compiler.Compile(null!));
        }

        [Test]
        public void Compile_WithValidRequest_CreatesGenerationPlan()
        {
            StageGraph graph = CreateStageGraph("landmass");

            GenerationRecipeDefinition recipe = CreateRecipe(
                "single_stage",
                graph.Schema,
                new[]
                {
                    graph.StageRouteChoice
                },
                new[]
                {
                    graph.StageRouteStepImplementationChoice
                });

            GenerationRunSettings runSettings = CreateRunSettings();
            GenerationRequest request = new(recipe, runSettings);

            GenerationPlan plan = new GenerationPlanCompiler().Compile(request);

            Assert.That(plan.GenerationRecipeDefinition, Is.SameAs(recipe));
            Assert.That(plan.GenerationSchemaDefinition, Is.SameAs(graph.Schema));
            Assert.That(plan.RunSettings, Is.SameAs(runSettings));
            Assert.That(plan.StagePlanNodes, Has.Count.EqualTo(1));
            Assert.That(plan.StagePlanNodes[0].StageDefinition, Is.SameAs(graph.StageDefinition));
            Assert.That(plan.StagePlanNodes[0].StageRouteDefinition, Is.SameAs(graph.StageRouteDefinition));
            Assert.That(plan.StagePlanNodes[0].StageContract, Is.SameAs(graph.StageContract));
            Assert.That(plan.StagePlanNodes[0].OperationPlanNodes, Has.Count.EqualTo(1));
            Assert.That(
                plan.StagePlanNodes[0].OperationPlanNodes[0].OperationImplementationDefinition,
                Is.SameAs(graph.OperationImplementationDefinition));
        }

        [Test]
        public void Compile_WithStageDependencyInReverseRecipeOrder_OrdersProducerBeforeConsumer()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            ResourceDefinition sharedResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.shared",
                "Shared Resource");

            ResourceDefinition finalResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.final",
                "Final Resource");

            StageGraph producer = CreateStageGraph(
                "producer",
                schema,
                stageProducedOutput: sharedResource,
                operationProducedOutput: sharedResource);

            StageGraph consumer = CreateStageGraph(
                "consumer",
                schema,
                stageRequiredInput: sharedResource,
                stageProducedOutput: finalResource,
                operationRequiredInput: sharedResource,
                operationProducedOutput: finalResource);

            GenerationRecipeDefinition recipe = CreateRecipe(
                "dependency_order",
                schema,
                new[]
                {
                    consumer.StageRouteChoice,
                    producer.StageRouteChoice
                },
                new[]
                {
                    consumer.StageRouteStepImplementationChoice,
                    producer.StageRouteStepImplementationChoice
                });

            GenerationPlan plan = new GenerationPlanCompiler().Compile(
                new GenerationRequest(recipe, CreateRunSettings()));

            Assert.That(plan.StagePlanNodes, Has.Count.EqualTo(2));
            Assert.That(plan.StagePlanNodes[0].StageDefinition, Is.SameAs(producer.StageDefinition));
            Assert.That(plan.StagePlanNodes[1].StageDefinition, Is.SameAs(consumer.StageDefinition));
        }

        [Test]
        public void Compile_WithIndependentStages_KeepsRecipeOrderAsTieBreaker()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            StageGraph first = CreateStageGraph("first", schema);
            StageGraph second = CreateStageGraph("second", schema);

            GenerationRecipeDefinition recipe = CreateRecipe(
                "independent_order",
                schema,
                new[]
                {
                    second.StageRouteChoice,
                    first.StageRouteChoice
                },
                new[]
                {
                    second.StageRouteStepImplementationChoice,
                    first.StageRouteStepImplementationChoice
                });

            GenerationPlan plan = new GenerationPlanCompiler().Compile(
                new GenerationRequest(recipe, CreateRunSettings()));

            Assert.That(plan.StagePlanNodes, Has.Count.EqualTo(2));
            Assert.That(plan.StagePlanNodes[0].StageDefinition, Is.SameAs(second.StageDefinition));
            Assert.That(plan.StagePlanNodes[1].StageDefinition, Is.SameAs(first.StageDefinition));
        }

        [Test]
        public void Compile_WithExplicitRequestImplementationChoice_UsesRequestChoice()
        {
            StageGraph graph = CreateStageGraph("landmass");

            OperationImplementationDefinition alternateImplementation = new(
                graph.OperationDefinition,
                Symbol.Create("lokrain.atlas.tests.implementation.landmass.alternate"),
                DisplayName.Create("Landmass Alternate Implementation"));

            StageRouteStepImplementationChoice alternateChoice = new(
                graph.StageRouteStepDefinition,
                graph.OperationDefinition,
                graph.OperationContract,
                alternateImplementation);

            GenerationRecipeDefinition recipe = CreateRecipe(
                "implementation_override",
                graph.Schema,
                new[]
                {
                    graph.StageRouteChoice
                },
                new[]
                {
                    graph.StageRouteStepImplementationChoice
                });

            GenerationRequest request = new(
                recipe,
                CreateRunSettings(),
                new[]
                {
                    alternateChoice
                });

            GenerationPlan plan = new GenerationPlanCompiler().Compile(request);

            Assert.That(
                plan.StagePlanNodes[0].OperationPlanNodes[0].OperationImplementationDefinition,
                Is.SameAs(alternateImplementation));
        }

        [Test]
        public void Compile_WithRequestImplementationChoicesOutOfRecipeOrder_CreatesOperationPlanNodesInRouteOrder()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            ResourceDefinition firstResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.first",
                "First Resource");

            ResourceDefinition secondResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.second",
                "Second Resource");

            StageDefinition stageDefinition = CreateStage(schema, "two_step");
            OperationDefinition firstOperation = CreateOperation(schema, "first");
            OperationDefinition secondOperation = CreateOperation(schema, "second");

            StageRouteStepDefinition firstRouteStep = new(
                Symbol.Create("lokrain.atlas.tests.route_step.two_step.first"),
                DisplayName.Create("First Route Step"),
                firstOperation.Symbol);

            StageRouteStepDefinition secondRouteStep = new(
                Symbol.Create("lokrain.atlas.tests.route_step.two_step.second"),
                DisplayName.Create("Second Route Step"),
                secondOperation.Symbol);

            StageRouteDefinition stageRoute = new(
                stageDefinition,
                Symbol.Create("lokrain.atlas.tests.stage_route.two_step"),
                DisplayName.Create("Two Step Route"),
                new[]
                {
                    firstRouteStep,
                    secondRouteStep
                });

            StageContract stageContract = new(
                stageDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    secondResource
                });

            OperationContract firstContract = new(
                firstOperation,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    firstResource
                });

            OperationContract secondContract = new(
                secondOperation,
                new[]
                {
                    firstResource
                },
                new[]
                {
                    secondResource
                });

            OperationImplementationDefinition firstImplementation = new(
                firstOperation,
                Symbol.Create("lokrain.atlas.tests.implementation.first.default"),
                DisplayName.Create("First Default Implementation"));

            OperationImplementationDefinition secondImplementation = new(
                secondOperation,
                Symbol.Create("lokrain.atlas.tests.implementation.second.default"),
                DisplayName.Create("Second Default Implementation"));

            StageRouteChoice stageRouteChoice = new(
                stageDefinition,
                stageRoute,
                stageContract);

            StageRouteStepImplementationChoice firstChoice = new(
                firstRouteStep,
                firstOperation,
                firstContract,
                firstImplementation);

            StageRouteStepImplementationChoice secondChoice = new(
                secondRouteStep,
                secondOperation,
                secondContract,
                secondImplementation);

            GenerationRecipeDefinition recipe = CreateRecipe(
                "two_step",
                schema,
                new[]
                {
                    stageRouteChoice
                },
                new[]
                {
                    firstChoice,
                    secondChoice
                });

            GenerationRequest request = new(
                recipe,
                CreateRunSettings(),
                new[]
                {
                    secondChoice,
                    firstChoice
                });

            GenerationPlan plan = new GenerationPlanCompiler().Compile(request);

            Assert.That(plan.StagePlanNodes[0].OperationPlanNodes, Has.Count.EqualTo(2));
            Assert.That(plan.StagePlanNodes[0].OperationPlanNodes[0].StageRouteStepDefinition, Is.SameAs(firstRouteStep));
            Assert.That(plan.StagePlanNodes[0].OperationPlanNodes[0].OperationImplementationDefinition, Is.SameAs(firstImplementation));
            Assert.That(plan.StagePlanNodes[0].OperationPlanNodes[1].StageRouteStepDefinition, Is.SameAs(secondRouteStep));
            Assert.That(plan.StagePlanNodes[0].OperationPlanNodes[1].OperationImplementationDefinition, Is.SameAs(secondImplementation));
        }

        private static GenerationRecipeDefinition CreateRecipe(
            string suffix,
            GenerationSchemaDefinition schema,
            StageRouteChoice[] stageRouteChoices,
            StageRouteStepImplementationChoice[] implementationChoices)
        {
            return new(
                Symbol.Create($"lokrain.atlas.tests.recipe.{suffix}"),
                DisplayName.Create($"{suffix} Recipe"),
                schema,
                stageRouteChoices,
                implementationChoices);
        }

        private static StageGraph CreateStageGraph(
            string suffix,
            GenerationSchemaDefinition? schema = null,
            ResourceDefinition? stageRequiredInput = null,
            ResourceDefinition? stageProducedOutput = null,
            ResourceDefinition? operationRequiredInput = null,
            ResourceDefinition? operationProducedOutput = null)
        {
            GenerationSchemaDefinition resolvedSchema = schema ?? CreateSchema();

            ResourceDefinition resolvedStageProducedOutput =
                stageProducedOutput
                ?? CreateResource(
                    resolvedSchema,
                    $"lokrain.atlas.tests.resource.{suffix}_output",
                    $"{suffix} Output");

            ResourceDefinition resolvedOperationProducedOutput =
                operationProducedOutput ?? resolvedStageProducedOutput;

            StageDefinition stageDefinition = CreateStage(resolvedSchema, suffix);
            OperationDefinition operationDefinition = CreateOperation(resolvedSchema, suffix);

            StageRouteStepDefinition stageRouteStepDefinition = new(
                Symbol.Create($"lokrain.atlas.tests.route_step.{suffix}"),
                DisplayName.Create($"{suffix} Route Step"),
                operationDefinition.Symbol);

            StageRouteDefinition stageRouteDefinition = new(
                stageDefinition,
                Symbol.Create($"lokrain.atlas.tests.stage_route.{suffix}"),
                DisplayName.Create($"{suffix} Stage Route"),
                new[]
                {
                    stageRouteStepDefinition
                });

            StageContract stageContract = new(
                stageDefinition,
                ToResourceArray(stageRequiredInput),
                new[]
                {
                    resolvedStageProducedOutput
                });

            OperationContract operationContract = new(
                operationDefinition,
                ToResourceArray(operationRequiredInput),
                new[]
                {
                    resolvedOperationProducedOutput
                });

            OperationImplementationDefinition operationImplementationDefinition = new(
                operationDefinition,
                Symbol.Create($"lokrain.atlas.tests.implementation.{suffix}.default"),
                DisplayName.Create($"{suffix} Default Implementation"));

            StageRouteChoice stageRouteChoice = new(
                stageDefinition,
                stageRouteDefinition,
                stageContract);

            StageRouteStepImplementationChoice implementationChoice = new(
                stageRouteStepDefinition,
                operationDefinition,
                operationContract,
                operationImplementationDefinition);

            return new(
                resolvedSchema,
                stageDefinition,
                stageRouteDefinition,
                stageRouteStepDefinition,
                stageContract,
                operationDefinition,
                operationContract,
                operationImplementationDefinition,
                stageRouteChoice,
                implementationChoice);
        }

        private static ResourceDefinition[] ToResourceArray(ResourceDefinition? resourceDefinition)
        {
            if (resourceDefinition is null)
            {
                return Array.Empty<ResourceDefinition>();
            }

            return new[]
            {
                resourceDefinition
            };
        }

        private static GenerationRunSettings CreateRunSettings()
        {
            return new(new Grid(256, 256), new Seed(123UL));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }

        private static StageDefinition CreateStage(
            GenerationSchemaDefinition schema,
            string suffix)
        {
            return new(
                schema,
                StageKind.Create($"lokrain.atlas.tests.stage_kind.{suffix}"),
                Symbol.Create($"lokrain.atlas.tests.stage.{suffix}"),
                DisplayName.Create($"{suffix} Stage"));
        }

        private static OperationDefinition CreateOperation(
            GenerationSchemaDefinition schema,
            string suffix)
        {
            return new(
                schema,
                OperationKind.Create($"lokrain.atlas.tests.operation_kind.{suffix}"),
                Symbol.Create($"lokrain.atlas.tests.operation.{suffix}"),
                DisplayName.Create($"{suffix} Operation"));
        }

        private static ResourceDefinition CreateResource(
            GenerationSchemaDefinition schema,
            string symbolValue,
            string displayNameValue)
        {
            return new(
                Symbol.Create(symbolValue),
                DisplayName.Create(displayNameValue),
                schema);
        }

        private sealed class StageGraph
        {
            public StageGraph(
                GenerationSchemaDefinition schema,
                StageDefinition stageDefinition,
                StageRouteDefinition stageRouteDefinition,
                StageRouteStepDefinition stageRouteStepDefinition,
                StageContract stageContract,
                OperationDefinition operationDefinition,
                OperationContract operationContract,
                OperationImplementationDefinition operationImplementationDefinition,
                StageRouteChoice stageRouteChoice,
                StageRouteStepImplementationChoice stageRouteStepImplementationChoice)
            {
                Schema = schema;
                StageDefinition = stageDefinition;
                StageRouteDefinition = stageRouteDefinition;
                StageRouteStepDefinition = stageRouteStepDefinition;
                StageContract = stageContract;
                OperationDefinition = operationDefinition;
                OperationContract = operationContract;
                OperationImplementationDefinition = operationImplementationDefinition;
                StageRouteChoice = stageRouteChoice;
                StageRouteStepImplementationChoice = stageRouteStepImplementationChoice;
            }

            public GenerationSchemaDefinition Schema { get; }

            public StageDefinition StageDefinition { get; }

            public StageRouteDefinition StageRouteDefinition { get; }

            public StageRouteStepDefinition StageRouteStepDefinition { get; }

            public StageContract StageContract { get; }

            public OperationDefinition OperationDefinition { get; }

            public OperationContract OperationContract { get; }

            public OperationImplementationDefinition OperationImplementationDefinition { get; }

            public StageRouteChoice StageRouteChoice { get; }

            public StageRouteStepImplementationChoice StageRouteStepImplementationChoice { get; }
        }
    }
}