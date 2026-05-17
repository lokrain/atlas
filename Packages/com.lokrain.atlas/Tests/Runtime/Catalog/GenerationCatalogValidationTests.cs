#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Catalog.Tests
{
    public sealed class GenerationCatalogValidationTests
    {
        [Test]
        public void Build_WithValidDefinitionGraph_ReturnsAcceptedCatalog()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            GenerationCatalog catalog = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageRouteDefinition(graph.Route)
                .AddStageContract(graph.StageContract)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation)
                .AddOperationContract(graph.OperationContract)
                .AddGenerationRecipeDefinition(graph.Recipe)
                .Build();

            Assert.That(catalog.GenerationSchemaDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.ResourceDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.StageDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.StageRouteDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.StageRouteStepDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.StageContracts, Has.Count.EqualTo(1));
            Assert.That(catalog.OperationDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.OperationImplementationDefinitions, Has.Count.EqualTo(1));
            Assert.That(catalog.OperationContracts, Has.Count.EqualTo(1));
            Assert.That(catalog.GenerationRecipeDefinitions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Build_WithDuplicateGenerationSchemaSymbol_ThrowsArgumentException()
        {
            GenerationSchemaDefinition firstSchema = TestDefinitions.CreateSchema(
                TestSymbols.Schema,
                "Schema");

            GenerationSchemaDefinition secondSchema = TestDefinitions.CreateSchema(
                TestSymbols.Schema,
                "Duplicate Schema");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(firstSchema)
                .AddGenerationSchemaDefinition(secondSchema);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateResourceDefinitionSymbol_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = TestDefinitions.CreateSchema(
                TestSymbols.Schema,
                "Schema");

            ResourceDefinition firstResource = TestDefinitions.CreateResource(
                schema,
                TestSymbols.Output,
                "Output");

            ResourceDefinition secondResource = TestDefinitions.CreateResource(
                schema,
                TestSymbols.Output,
                "Duplicate Output");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(schema)
                .AddResourceDefinition(firstResource)
                .AddResourceDefinition(secondResource);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithResourceDefinitionReferencingSchemaNotOwnedByCatalog_ThrowsArgumentException()
        {
            GenerationSchemaDefinition catalogSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.catalog"),
                "Catalog Schema");

            GenerationSchemaDefinition foreignSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.foreign"),
                "Foreign Schema");

            ResourceDefinition foreignResource = TestDefinitions.CreateResource(
                foreignSchema,
                TestSymbols.Output,
                "Foreign Output");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(catalogSchema)
                .AddResourceDefinition(foreignResource);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateStageDefinitionSymbol_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            StageDefinition duplicateStage = TestDefinitions.CreateStage(
                graph.Schema,
                TestSymbols.Stage,
                "Duplicate Stage");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddStageDefinition(graph.Stage)
                .AddStageDefinition(duplicateStage);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateStageRouteDefinitionSymbol_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            StageRouteDefinition duplicateRoute = new StageRouteDefinition(
                graph.Stage,
                graph.Route.Symbol,
                DisplayName.Create("Duplicate Route"),
                new[]
                {
                    graph.RouteStep
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddStageDefinition(graph.Stage)
                .AddStageRouteDefinition(graph.Route)
                .AddStageRouteDefinition(duplicateRoute);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateStageRouteStepSymbolAcrossRoutes_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            StageRouteDefinition secondRoute = new StageRouteDefinition(
                graph.Stage,
                Symbol.Create("lokrain.atlas.tests.catalog_validation.route.second"),
                DisplayName.Create("Second Route"),
                new[]
                {
                    graph.RouteStep
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddStageDefinition(graph.Stage)
                .AddStageRouteDefinition(graph.Route)
                .AddStageRouteDefinition(secondRoute);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateStageContractForSameStage_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            StageContract duplicateStageContract = new StageContract(
                graph.Stage,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    graph.OutputResource
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract)
                .AddStageContract(duplicateStageContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageContractReferencingResourceNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            ResourceDefinition unregisteredResource = TestDefinitions.CreateResource(
                graph.Schema,
                Symbol.Create("lokrain.atlas.tests.catalog_validation.resource.unregistered"),
                "Unregistered Output");

            StageContract stageContract = new StageContract(
                graph.Stage,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    unregisteredResource
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(stageContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateOperationDefinitionSymbol_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            OperationDefinition duplicateOperation = TestDefinitions.CreateOperation(
                graph.Schema,
                TestSymbols.Operation,
                "Duplicate Operation");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddOperationDefinition(graph.Operation)
                .AddOperationDefinition(duplicateOperation);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateOperationImplementationDefinitionSymbol_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            OperationImplementationDefinition duplicateImplementation = new OperationImplementationDefinition(
                graph.Operation,
                graph.OperationImplementation.Symbol,
                DisplayName.Create("Duplicate Implementation"));

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation)
                .AddOperationImplementationDefinition(duplicateImplementation);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithDuplicateOperationContractForSameOperation_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            OperationContract duplicateOperationContract = new OperationContract(
                graph.Operation,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    graph.OutputResource
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddOperationDefinition(graph.Operation)
                .AddOperationContract(graph.OperationContract)
                .AddOperationContract(duplicateOperationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithOperationContractReferencingResourceNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            ResourceDefinition unregisteredResource = TestDefinitions.CreateResource(
                graph.Schema,
                Symbol.Create("lokrain.atlas.tests.catalog_validation.resource.unregistered"),
                "Unregistered Output");

            OperationContract operationContract = new OperationContract(
                graph.Operation,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    unregisteredResource
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddOperationDefinition(graph.Operation)
                .AddOperationContract(operationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageDefinitionReferencingSchemaNotOwnedByCatalog_ThrowsArgumentException()
        {
            GenerationSchemaDefinition catalogSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.catalog"),
                "Catalog Schema");

            GenerationSchemaDefinition foreignSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.foreign"),
                "Foreign Schema");

            StageDefinition foreignStage = TestDefinitions.CreateStage(
                foreignSchema,
                TestSymbols.Stage,
                "Foreign Stage");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(catalogSchema)
                .AddStageDefinition(foreignStage);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithOperationDefinitionReferencingSchemaNotOwnedByCatalog_ThrowsArgumentException()
        {
            GenerationSchemaDefinition catalogSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.catalog"),
                "Catalog Schema");

            GenerationSchemaDefinition foreignSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.foreign"),
                "Foreign Schema");

            OperationDefinition foreignOperation = TestDefinitions.CreateOperation(
                foreignSchema,
                TestSymbols.Operation,
                "Foreign Operation");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(catalogSchema)
                .AddOperationDefinition(foreignOperation);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageContractReferencingStageNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageContract(graph.StageContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithOperationContractReferencingOperationNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddOperationContract(graph.OperationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithOperationImplementationReferencingOperationNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddOperationImplementationDefinition(graph.OperationImplementation);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageWithoutContract_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageRouteDefinition(graph.Route)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation)
                .AddOperationContract(graph.OperationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageWithoutRoute_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithOperationWithoutContract_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithOperationWithoutImplementation_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddOperationDefinition(graph.Operation)
                .AddOperationContract(graph.OperationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageRouteReferencingStageNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddStageRouteDefinition(graph.Route);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageRouteStepReferencingOperationNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            StageRouteStepDefinition missingOperationStep = new StageRouteStepDefinition(
                TestSymbols.RouteStep,
                DisplayName.Create("Missing Operation Step"),
                Symbol.Create("lokrain.atlas.tests.catalog_validation.operation.missing"));

            StageRouteDefinition route = new StageRouteDefinition(
                graph.Stage,
                TestSymbols.Route,
                DisplayName.Create("Route"),
                new[]
                {
                    missingOperationStep
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract)
                .AddStageRouteDefinition(route);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithStageRouteStepReferencingOperationFromDifferentSchema_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            GenerationSchemaDefinition foreignSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.foreign"),
                "Foreign Schema");

            ResourceDefinition foreignResource = TestDefinitions.CreateResource(
                foreignSchema,
                Symbol.Create("lokrain.atlas.tests.catalog_validation.resource.foreign"),
                "Foreign Output");

            OperationDefinition foreignOperation = TestDefinitions.CreateOperation(
                foreignSchema,
                Symbol.Create("lokrain.atlas.tests.catalog_validation.operation.foreign"),
                "Foreign Operation");

            OperationContract foreignOperationContract = new OperationContract(
                foreignOperation,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    foreignResource
                });

            OperationImplementationDefinition foreignImplementation = new OperationImplementationDefinition(
                foreignOperation,
                Symbol.Create("lokrain.atlas.tests.catalog_validation.implementation.foreign"),
                DisplayName.Create("Foreign Implementation"));

            StageRouteStepDefinition foreignOperationStep = new StageRouteStepDefinition(
                TestSymbols.RouteStep,
                DisplayName.Create("Foreign Operation Step"),
                foreignOperation.Symbol);

            StageRouteDefinition route = new StageRouteDefinition(
                graph.Stage,
                TestSymbols.Route,
                DisplayName.Create("Route"),
                new[]
                {
                    foreignOperationStep
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddGenerationSchemaDefinition(foreignSchema)
                .AddResourceDefinitions(graph.Resources)
                .AddResourceDefinition(foreignResource)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract)
                .AddStageRouteDefinition(route)
                .AddOperationDefinition(foreignOperation)
                .AddOperationImplementationDefinition(foreignImplementation)
                .AddOperationContract(foreignOperationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithRouteOperationInputNotAvailable_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            ResourceDefinition missingInput = TestDefinitions.CreateResource(
                graph.Schema,
                TestSymbols.MissingInput,
                "Missing Input");

            OperationContract operationContract = new OperationContract(
                graph.Operation,
                new[]
                {
                    missingInput
                },
                new[]
                {
                    graph.OutputResource
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddResourceDefinition(missingInput)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract)
                .AddStageRouteDefinition(graph.Route)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation)
                .AddOperationContract(operationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithRouteNotProducingRequiredStageOutput_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            ResourceDefinition differentOutput = TestDefinitions.CreateResource(
                graph.Schema,
                TestSymbols.DifferentOutput,
                "Different Output");

            OperationContract operationContract = new OperationContract(
                graph.Operation,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    differentOutput
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddResourceDefinition(differentOutput)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract)
                .AddStageRouteDefinition(graph.Route)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation)
                .AddOperationContract(operationContract);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithRecipeReferencingSchemaNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            GenerationSchemaDefinition catalogSchema = TestDefinitions.CreateSchema(
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema.catalog"),
                "Catalog Schema");

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(catalogSchema)
                .AddGenerationRecipeDefinition(graph.Recipe);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithRecipeReferencingStageNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddGenerationRecipeDefinition(graph.Recipe);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void Build_WithRecipeReferencingImplementationNotOwnedByCatalog_ThrowsArgumentException()
        {
            TestCatalogGraph graph = TestCatalogGraph.Create();

            OperationImplementationDefinition unregisteredImplementation =
                new OperationImplementationDefinition(
                    graph.Operation,
                    Symbol.Create("lokrain.atlas.tests.catalog_validation.implementation.unregistered"),
                    DisplayName.Create("Unregistered Implementation"));

            StageRouteStepImplementationChoice unregisteredImplementationChoice =
                new StageRouteStepImplementationChoice(
                    graph.RouteStep,
                    graph.Operation,
                    graph.OperationContract,
                    unregisteredImplementation);

            GenerationRecipeDefinition recipe = new GenerationRecipeDefinition(
                TestSymbols.Recipe,
                DisplayName.Create("Recipe"),
                graph.Schema,
                new[]
                {
                    graph.StageRouteChoice
                },
                new[]
                {
                    unregisteredImplementationChoice
                });

            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(graph.Schema)
                .AddResourceDefinitions(graph.Resources)
                .AddStageDefinition(graph.Stage)
                .AddStageContract(graph.StageContract)
                .AddStageRouteDefinition(graph.Route)
                .AddOperationDefinition(graph.Operation)
                .AddOperationImplementationDefinition(graph.OperationImplementation)
                .AddOperationContract(graph.OperationContract)
                .AddGenerationRecipeDefinition(recipe);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        private sealed class TestCatalogGraph
        {
            private TestCatalogGraph(
                GenerationSchemaDefinition schema,
                ResourceDefinition outputResource,
                StageDefinition stage,
                StageContract stageContract,
                StageRouteStepDefinition routeStep,
                StageRouteDefinition route,
                OperationDefinition operation,
                OperationContract operationContract,
                OperationImplementationDefinition operationImplementation,
                StageRouteChoice stageRouteChoice,
                StageRouteStepImplementationChoice operationImplementationChoice,
                GenerationRecipeDefinition recipe)
            {
                Schema = schema;
                OutputResource = outputResource;
                Stage = stage;
                StageContract = stageContract;
                RouteStep = routeStep;
                Route = route;
                Operation = operation;
                OperationContract = operationContract;
                OperationImplementation = operationImplementation;
                StageRouteChoice = stageRouteChoice;
                OperationImplementationChoice = operationImplementationChoice;
                Recipe = recipe;
            }

            public GenerationSchemaDefinition Schema { get; }

            public ResourceDefinition OutputResource { get; }

            public ResourceDefinition[] Resources => new[]
            {
                OutputResource
            };

            public StageDefinition Stage { get; }

            public StageContract StageContract { get; }

            public StageRouteStepDefinition RouteStep { get; }

            public StageRouteDefinition Route { get; }

            public OperationDefinition Operation { get; }

            public OperationContract OperationContract { get; }

            public OperationImplementationDefinition OperationImplementation { get; }

            public StageRouteChoice StageRouteChoice { get; }

            public StageRouteStepImplementationChoice OperationImplementationChoice { get; }

            public GenerationRecipeDefinition Recipe { get; }

            public static TestCatalogGraph Create()
            {
                GenerationSchemaDefinition schema = TestDefinitions.CreateSchema(
                    TestSymbols.Schema,
                    "Schema");

                ResourceDefinition outputResource = TestDefinitions.CreateResource(
                    schema,
                    TestSymbols.Output,
                    "Output");

                StageDefinition stage = TestDefinitions.CreateStage(
                    schema,
                    TestSymbols.Stage,
                    "Stage");

                StageContract stageContract = new StageContract(
                    stage,
                    Array.Empty<ResourceDefinition>(),
                    new[]
                    {
                        outputResource
                    });

                OperationDefinition operation = TestDefinitions.CreateOperation(
                    schema,
                    TestSymbols.Operation,
                    "Operation");

                OperationContract operationContract = new OperationContract(
                    operation,
                    Array.Empty<ResourceDefinition>(),
                    new[]
                    {
                        outputResource
                    });

                OperationImplementationDefinition operationImplementation =
                    new OperationImplementationDefinition(
                        operation,
                        TestSymbols.OperationImplementation,
                        DisplayName.Create("Operation Implementation"));

                StageRouteStepDefinition routeStep = new StageRouteStepDefinition(
                    TestSymbols.RouteStep,
                    DisplayName.Create("Route Step"),
                    operation.Symbol);

                StageRouteDefinition route = new StageRouteDefinition(
                    stage,
                    TestSymbols.Route,
                    DisplayName.Create("Route"),
                    new[]
                    {
                        routeStep
                    });

                StageRouteChoice stageRouteChoice = new StageRouteChoice(
                    stage,
                    route,
                    stageContract);

                StageRouteStepImplementationChoice operationImplementationChoice =
                    new StageRouteStepImplementationChoice(
                        routeStep,
                        operation,
                        operationContract,
                        operationImplementation);

                GenerationRecipeDefinition recipe = new GenerationRecipeDefinition(
                    TestSymbols.Recipe,
                    DisplayName.Create("Recipe"),
                    schema,
                    new[]
                    {
                        stageRouteChoice
                    },
                    new[]
                    {
                        operationImplementationChoice
                    });

                return new TestCatalogGraph(
                    schema,
                    outputResource,
                    stage,
                    stageContract,
                    routeStep,
                    route,
                    operation,
                    operationContract,
                    operationImplementation,
                    stageRouteChoice,
                    operationImplementationChoice,
                    recipe);
            }
        }

        private static class TestDefinitions
        {
            public static GenerationSchemaDefinition CreateSchema(
                Symbol symbol,
                string displayName)
            {
                return new GenerationSchemaDefinition(
                    symbol,
                    DisplayName.Create(displayName));
            }

            public static ResourceDefinition CreateResource(
                GenerationSchemaDefinition schema,
                Symbol symbol,
                string displayName)
            {
                return new ResourceDefinition(
                    symbol,
                    DisplayName.Create(displayName),
                    schema);
            }

            public static StageDefinition CreateStage(
                GenerationSchemaDefinition schema,
                Symbol symbol,
                string displayName)
            {
                return new StageDefinition(
                    schema,
                    StageKind.Create("lokrain.atlas.tests.catalog_validation.stage_kind"),
                    symbol,
                    DisplayName.Create(displayName));
            }

            public static OperationDefinition CreateOperation(
                GenerationSchemaDefinition schema,
                Symbol symbol,
                string displayName)
            {
                return new OperationDefinition(
                    schema,
                    OperationKind.Create("lokrain.atlas.tests.catalog_validation.operation_kind"),
                    symbol,
                    DisplayName.Create(displayName));
            }
        }

        private static class TestSymbols
        {
            public static readonly Symbol Schema =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.schema");

            public static readonly Symbol Stage =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.stage");

            public static readonly Symbol Route =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.route");

            public static readonly Symbol RouteStep =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.route_step");

            public static readonly Symbol Operation =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.operation");

            public static readonly Symbol OperationImplementation =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.implementation");

            public static readonly Symbol Recipe =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.recipe");

            public static readonly Symbol Output =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.resource.output");

            public static readonly Symbol DifferentOutput =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.resource.different_output");

            public static readonly Symbol MissingInput =
                Symbol.Create("lokrain.atlas.tests.catalog_validation.resource.missing_input");
        }
    }
}