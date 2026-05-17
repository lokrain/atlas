#nullable enable

using System;
using System.Text;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Planning;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Tests.Editor.Planning
{
    public sealed class GenerationPlanCompilerSmokeDumpTests
    {
        [Test]
        public void Compile_ReordersSelectedStagesByContractDependencies_AndDumpsResolvedPlan()
        {
            GenerationSchemaDefinition schema = BuiltInGenerationSchemas.World;

            Symbol continentMask = Symbol.Create("lokrain.atlas.resource.continent_mask");
            Symbol baseElevation = Symbol.Create("lokrain.atlas.resource.base_elevation");

            StageKind landmassSeedStageKind = StageKind.Create("lokrain.atlas.stage.landmass_seed");
            StageKind elevationStageKind = StageKind.Create("lokrain.atlas.stage.elevation");

            OperationKind landmassSeedOperationKind = OperationKind.Create("lokrain.atlas.operation.landmass_seed");
            OperationKind elevationOperationKind = OperationKind.Create("lokrain.atlas.operation.elevation");

            StageDefinition landmassSeedStage = new(
                schema,
                landmassSeedStageKind,
                Symbol.Create("lokrain.atlas.stage.landmass_seed"),
                DisplayName.Create("Landmass Seed"));

            StageDefinition elevationStage = new(
                schema,
                elevationStageKind,
                Symbol.Create("lokrain.atlas.stage.elevation"),
                DisplayName.Create("Elevation"));

            OperationDefinition createContinentMaskOperation = new(
                schema,
                landmassSeedOperationKind,
                Symbol.Create("lokrain.atlas.operation.create_continent_mask"),
                DisplayName.Create("Create Continent Mask"));

            OperationDefinition createBaseElevationOperation = new(
                schema,
                elevationOperationKind,
                Symbol.Create("lokrain.atlas.operation.create_base_elevation"),
                DisplayName.Create("Create Base Elevation"));

            OperationImplementationDefinition createContinentMaskImplementation = new(
                createContinentMaskOperation,
                Symbol.Create("lokrain.atlas.implementation.create_continent_mask.default"),
                DisplayName.Create("Default Continent Mask"));

            OperationImplementationDefinition createBaseElevationImplementation = new(
                createBaseElevationOperation,
                Symbol.Create("lokrain.atlas.implementation.create_base_elevation.default"),
                DisplayName.Create("Default Base Elevation"));

            OperationContract createContinentMaskContract = new(
                createContinentMaskOperation,
                Array.Empty<Symbol>(),
                new[] { continentMask });

            OperationContract createBaseElevationContract = new(
                createBaseElevationOperation,
                new[] { continentMask },
                new[] { baseElevation });

            StageRouteStepDefinition createContinentMaskStep = new(
                Symbol.Create("lokrain.atlas.route_step.create_continent_mask"),
                DisplayName.Create("Create Continent Mask Step"),
                createContinentMaskOperation.Symbol);

            StageRouteStepDefinition createBaseElevationStep = new(
                Symbol.Create("lokrain.atlas.route_step.create_base_elevation"),
                DisplayName.Create("Create Base Elevation Step"),
                createBaseElevationOperation.Symbol);

            StageRouteDefinition landmassSeedRoute = new(
                landmassSeedStage,
                Symbol.Create("lokrain.atlas.route.landmass_seed.primary"),
                DisplayName.Create("Primary Landmass Seed Route"),
                new[] { createContinentMaskStep });

            StageRouteDefinition elevationRoute = new(
                elevationStage,
                Symbol.Create("lokrain.atlas.route.elevation.primary"),
                DisplayName.Create("Primary Elevation Route"),
                new[] { createBaseElevationStep });

            StageContract landmassSeedContract = new(
                landmassSeedStage,
                Array.Empty<Symbol>(),
                new[] { continentMask });

            StageContract elevationContract = new(
                elevationStage,
                new[] { continentMask },
                new[] { baseElevation });

            GenerationCatalog catalog = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(schema)
                .AddStageDefinition(landmassSeedStage)
                .AddStageDefinition(elevationStage)
                .AddStageRouteDefinition(landmassSeedRoute)
                .AddStageRouteDefinition(elevationRoute)
                .AddStageContract(landmassSeedContract)
                .AddStageContract(elevationContract)
                .AddOperationDefinition(createContinentMaskOperation)
                .AddOperationDefinition(createBaseElevationOperation)
                .AddOperationImplementationDefinition(createContinentMaskImplementation)
                .AddOperationImplementationDefinition(createBaseElevationImplementation)
                .AddOperationContract(createContinentMaskContract)
                .AddOperationContract(createBaseElevationContract)
                .Build();

            GenerationRequest request = new(
                schema.Symbol,
                new Grid(256, 256),
                new Seed(123UL),
                new[]
                {
                    // Intentionally reversed. Compiler should place Landmass Seed before Elevation.
                    new StageRouteSelection(elevationStage.Symbol, elevationRoute.Symbol),
                    new StageRouteSelection(landmassSeedStage.Symbol, landmassSeedRoute.Symbol)
                },
                new[]
                {
                    new OperationImplementationSelection(
                        createBaseElevationStep.Symbol,
                        createBaseElevationImplementation.Symbol),
                    new OperationImplementationSelection(
                        createContinentMaskStep.Symbol,
                        createContinentMaskImplementation.Symbol)
                });

            GenerationPlanCompiler compiler = new();
            GenerationPlanCompilerResult result = compiler.Compile(catalog, request);

            string dump = Dump(result);
            TestContext.Out.WriteLine(dump);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.GenerationPlan, Is.Not.Null);
            Assert.That(result.GenerationPlan!.StagePlanNodes.Count, Is.EqualTo(2));
            Assert.That(result.GenerationPlan.StagePlanNodes[0].StageDefinition, Is.SameAs(landmassSeedStage));
            Assert.That(result.GenerationPlan.StagePlanNodes[1].StageDefinition, Is.SameAs(elevationStage));
        }

        private static string Dump(GenerationPlanCompilerResult result)
        {
            var builder = new StringBuilder();

            builder.AppendLine("GenerationPlanCompilerResult");
            builder.AppendLine($"  Succeeded: {result.Succeeded}");

            if (result.Failed)
            {
                builder.AppendLine($"  Errors: {result.Errors.Count}");

                for (int index = 0; index < result.Errors.Count; index++)
                {
                    GenerationPlanCompilerError error = result.Errors[index];

                    builder.AppendLine($"    [{index}] {error.Code}");
                    builder.AppendLine($"        Subject: {error.SubjectSymbol?.ToString() ?? "<none>"}");
                    builder.AppendLine($"        Message: {error.Message}");
                }

                return builder.ToString();
            }

            GenerationPlan plan = result.GenerationPlan!;

            builder.AppendLine($"  Schema: {plan.GenerationSchemaDefinition.Symbol}");
            builder.AppendLine($"  Grid: {plan.Grid.Width}x{plan.Grid.Depth}");
            builder.AppendLine($"  CellCount: {plan.Grid.CellCount}");
            builder.AppendLine($"  Seed: {plan.Seed.ToHexString()}");
            builder.AppendLine($"  Stages: {plan.StagePlanNodes.Count}");

            for (int stageIndex = 0; stageIndex < plan.StagePlanNodes.Count; stageIndex++)
            {
                StagePlanNode stageNode = plan.StagePlanNodes[stageIndex];

                builder.AppendLine($"    [{stageIndex}] Stage");
                builder.AppendLine($"        Definition: {stageNode.StageDefinition.Symbol}");
                builder.AppendLine($"        Route: {stageNode.StageRouteDefinition.Symbol}");
                builder.AppendLine($"        Required: {JoinSymbols(stageNode.StageContract.RequiredInputSymbols)}");
                builder.AppendLine($"        Produced: {JoinSymbols(stageNode.StageContract.ProducedOutputSymbols)}");
                builder.AppendLine($"        Operations: {stageNode.OperationPlanNodes.Count}");

                for (int operationIndex = 0; operationIndex < stageNode.OperationPlanNodes.Count; operationIndex++)
                {
                    OperationPlanNode operationNode = stageNode.OperationPlanNodes[operationIndex];

                    builder.AppendLine($"          [{operationIndex}] Operation");
                    builder.AppendLine($"              RouteStep: {operationNode.StageRouteStepDefinition.Symbol}");
                    builder.AppendLine($"              Definition: {operationNode.OperationDefinition.Symbol}");
                    builder.AppendLine($"              Implementation: {operationNode.OperationImplementationDefinition.Symbol}");
                    builder.AppendLine($"              Required: {JoinSymbols(operationNode.OperationContract.RequiredInputSymbols)}");
                    builder.AppendLine($"              Produced: {JoinSymbols(operationNode.OperationContract.ProducedOutputSymbols)}");
                }
            }

            return builder.ToString();
        }

        private static string JoinSymbols(System.Collections.Generic.IReadOnlyList<Symbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return "<none>";
            }

            var builder = new StringBuilder();

            for (int index = 0; index < symbols.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(", ");
                }

                builder.Append(symbols[index]);
            }

            return builder.ToString();
        }
    }
}