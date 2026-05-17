#nullable enable

using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class LandmassGenerationPipelineTests
    {
        [Test]
        public void PrimaryContinentalLandmass_ResolvesAndCompilesExpectedPlan()
        {
            var grid = new Grid(256, 256);
            var seed = new Seed(123UL);

            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();
            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(grid, seed);

            var resolver = new GenerationRequestResolver();
            GenerationRequestResolutionResult resolutionResult =
                resolver.Resolve(catalog, descriptor);

            Assert.That(resolutionResult.Succeeded, Is.True);
            Assert.That(resolutionResult.Failed, Is.False);
            Assert.That(resolutionResult.Errors, Is.Empty);
            Assert.That(resolutionResult.GenerationRequest, Is.Not.Null);

            GenerationRequest request = resolutionResult.GenerationRequest!;

            Assert.That(
                request.GenerationRecipeDefinition.Symbol,
                Is.EqualTo(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol));

            Assert.That(
                request.GenerationRecipeDefinition.GenerationSchemaDefinition.Symbol,
                Is.EqualTo(BuiltInGenerationSchemas.World.Symbol));

            Assert.That(request.RunSettings.Grid, Is.SameAs(grid));
            Assert.That(request.RunSettings.Grid.CellCount, Is.EqualTo(65536));
            Assert.That(request.RunSettings.Seed, Is.EqualTo(seed));

            var compiler = new GenerationPlanCompiler();
            GenerationPlan plan = compiler.Compile(request);

            Assert.That(
                plan.GenerationRecipeDefinition.Symbol,
                Is.EqualTo(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol));

            Assert.That(
                plan.GenerationSchemaDefinition.Symbol,
                Is.EqualTo(BuiltInGenerationSchemas.World.Symbol));

            Assert.That(plan.Grid, Is.SameAs(grid));
            Assert.That(plan.Grid.CellCount, Is.EqualTo(65536));
            Assert.That(plan.Seed, Is.EqualTo(seed));

            Assert.That(plan.StagePlanNodes, Has.Count.EqualTo(1));

            StagePlanNode stagePlanNode = plan.StagePlanNodes[0];

            Assert.That(
                stagePlanNode.StageDefinition.Symbol,
                Is.EqualTo(LandmassStageDefinitions.ContinentalLandmass.Symbol));

            Assert.That(stagePlanNode.OperationPlanNodes, Has.Count.EqualTo(5));

            AssertOperation(
                stagePlanNode.OperationPlanNodes[0],
                LandmassOperationDefinitions.EvaluateContinentSuitability.Symbol);

            AssertOperation(
                stagePlanNode.OperationPlanNodes[1],
                LandmassOperationDefinitions.FormContinentCandidate.Symbol);

            AssertOperation(
                stagePlanNode.OperationPlanNodes[2],
                LandmassOperationDefinitions.ExtractMainContinent.Symbol);

            AssertOperation(
                stagePlanNode.OperationPlanNodes[3],
                LandmassOperationDefinitions.CompleteContinentArea.Symbol);

            AssertOperation(
                stagePlanNode.OperationPlanNodes[4],
                LandmassOperationDefinitions.ComposeBaseElevation.Symbol);
        }

        private static void AssertOperation(
            OperationPlanNode operationPlanNode,
            Symbol expectedOperationDefinitionSymbol)
        {
            Assert.That(
                operationPlanNode.OperationDefinition.Symbol,
                Is.EqualTo(expectedOperationDefinitionSymbol));
        }
    }
}