#nullable enable

using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class OperationPlanNodeTests
    {
        [Test]
        public void CompilePrimaryContinentalLandmass_CreatesExpectedOperationPlanNode()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            OperationPlanNode operationPlanNode =
                plan.StagePlanNodes[0].OperationPlanNodes[2];

            Assert.That(
                operationPlanNode.StageRouteStepDefinition,
                Is.SameAs(Generation.Landmass.Routes.LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(
                operationPlanNode.OperationDefinition,
                Is.SameAs(LandmassOperationDefinitions.ExtractMainContinent));

            Assert.That(
                operationPlanNode.OperationImplementationDefinition,
                Is.SameAs(LandmassOperationImplementations.ExtractMainContinentDefault));

            Assert.That(
                operationPlanNode.OperationContract,
                Is.SameAs(LandmassOperationContracts.ExtractMainContinent));
        }

        [Test]
        public void Equals_WithEquivalentCompilerCreatedNode_ReturnsTrue()
        {
            OperationPlanNode left =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0]
                    .OperationPlanNodes[2];

            OperationPlanNode right =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0]
                    .OperationPlanNodes[2];

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentOperationPlanNode_ReturnsFalse()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            OperationPlanNode left = plan.StagePlanNodes[0].OperationPlanNodes[0];
            OperationPlanNode right = plan.StagePlanNodes[0].OperationPlanNodes[2];

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            OperationPlanNode operationPlanNode =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0]
                    .OperationPlanNodes[2];

            Assert.That(operationPlanNode.Equals("OperationPlanNode"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            OperationPlanNode? left = null;
            OperationPlanNode? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            OperationPlanNode? left =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0]
                    .OperationPlanNodes[2];

            OperationPlanNode? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsOperationPlanNodeSummary()
        {
            OperationPlanNode operationPlanNode =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0]
                    .OperationPlanNodes[2];

            string value = operationPlanNode.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "OperationPlanNode(StageRouteStepDefinition: lokrain.atlas.landmass.route_step.primary_continental_landmass.extract_main_continent, OperationDefinition: lokrain.atlas.landmass.operation.extract_main_continent, OperationImplementationDefinition: lokrain.atlas.landmass.implementation.extract_main_continent.default)"));
        }

        private static GenerationPlan CompilePrimaryContinentalLandmassPlan()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    new Grid(256, 256),
                    new Seed(123UL));

            GenerationRequestResolutionResult resolutionResult =
                new GenerationRequestResolver().Resolve(catalog, descriptor);

            Assert.That(resolutionResult.Succeeded, Is.True);
            Assert.That(resolutionResult.GenerationRequest, Is.Not.Null);

            return new GenerationPlanCompiler().Compile(
                resolutionResult.GenerationRequest!);
        }
    }
}