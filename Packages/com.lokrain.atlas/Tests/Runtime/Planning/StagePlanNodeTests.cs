#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class StagePlanNodeTests
    {
        [Test]
        public void CompilePrimaryContinentalLandmass_CreatesExpectedStagePlanNode()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            StagePlanNode stagePlanNode = plan.StagePlanNodes[0];

            Assert.That(
                stagePlanNode.StageDefinition,
                Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));

            Assert.That(
                stagePlanNode.StageRouteDefinition,
                Is.SameAs(LandmassStageRoutes.PrimaryContinentalLandmass));

            Assert.That(
                stagePlanNode.StageContract,
                Is.SameAs(LandmassStageContracts.ContinentalLandmass));
        }

        [Test]
        public void CompilePrimaryContinentalLandmass_CreatesExpectedOperationPlanNodesInRouteOrder()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            StagePlanNode stagePlanNode = plan.StagePlanNodes[0];

            Assert.That(stagePlanNode.OperationPlanNodes, Has.Count.EqualTo(5));

            AssertOperationPlanNode(
                stagePlanNode.OperationPlanNodes[0],
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault,
                LandmassOperationContracts.EvaluateContinentSuitability);

            AssertOperationPlanNode(
                stagePlanNode.OperationPlanNodes[1],
                LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate,
                LandmassOperationDefinitions.FormContinentCandidate,
                LandmassOperationImplementations.FormContinentCandidateDefault,
                LandmassOperationContracts.FormContinentCandidate);

            AssertOperationPlanNode(
                stagePlanNode.OperationPlanNodes[2],
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent,
                LandmassOperationDefinitions.ExtractMainContinent,
                LandmassOperationImplementations.ExtractMainContinentDefault,
                LandmassOperationContracts.ExtractMainContinent);

            AssertOperationPlanNode(
                stagePlanNode.OperationPlanNodes[3],
                LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea,
                LandmassOperationDefinitions.CompleteContinentArea,
                LandmassOperationImplementations.CompleteContinentAreaDefault,
                LandmassOperationContracts.CompleteContinentArea);

            AssertOperationPlanNode(
                stagePlanNode.OperationPlanNodes[4],
                LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation,
                LandmassOperationDefinitions.ComposeBaseElevation,
                LandmassOperationImplementations.ComposeBaseElevationDefault,
                LandmassOperationContracts.ComposeBaseElevation);
        }

        [Test]
        public void OperationPlanNodes_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            StagePlanNode stagePlanNode =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            Assert.That(
                stagePlanNode.OperationPlanNodes,
                Is.InstanceOf<ICollection<OperationPlanNode>>());

            ICollection<OperationPlanNode> operationPlanNodes =
                (ICollection<OperationPlanNode>)stagePlanNode.OperationPlanNodes;

            Assert.That(operationPlanNodes.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => operationPlanNodes.Add(stagePlanNode.OperationPlanNodes[0]));

            Assert.Throws<NotSupportedException>(
                operationPlanNodes.Clear);
        }

        [Test]
        public void Equals_WithEquivalentCompilerCreatedNode_ReturnsTrue()
        {
            StagePlanNode left =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            StagePlanNode right =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            StagePlanNode stagePlanNode =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            Assert.That(stagePlanNode.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            StagePlanNode stagePlanNode =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            Assert.That(stagePlanNode.Equals("StagePlanNode"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StagePlanNode? left = null;
            StagePlanNode? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            StagePlanNode? left =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            StagePlanNode? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsStagePlanNodeSummary()
        {
            StagePlanNode stagePlanNode =
                CompilePrimaryContinentalLandmassPlan()
                    .StagePlanNodes[0];

            string value = stagePlanNode.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StagePlanNode(StageDefinition: lokrain.atlas.landmass.stage.continental_landmass, StageRouteDefinition: lokrain.atlas.landmass.route.primary_continental_landmass, OperationPlanNodes: 5)"));
        }

        private static void AssertOperationPlanNode(
            OperationPlanNode operationPlanNode,
            Stages.StageRouteStepDefinition expectedStageRouteStepDefinition,
            Operations.OperationDefinition expectedOperationDefinition,
            Operations.OperationImplementationDefinition expectedOperationImplementationDefinition,
            Operations.OperationContract expectedOperationContract)
        {
            Assert.That(
                operationPlanNode.StageRouteStepDefinition,
                Is.SameAs(expectedStageRouteStepDefinition));

            Assert.That(
                operationPlanNode.OperationDefinition,
                Is.SameAs(expectedOperationDefinition));

            Assert.That(
                operationPlanNode.OperationImplementationDefinition,
                Is.SameAs(expectedOperationImplementationDefinition));

            Assert.That(
                operationPlanNode.OperationContract,
                Is.SameAs(expectedOperationContract));
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