#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationPlanTests
    {
        [Test]
        public void CompilePrimaryContinentalLandmass_CreatesExpectedGenerationPlan()
        {
            Grid grid = new(256, 256);
            Seed seed = new(123UL);

            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan(grid, seed);

            Assert.That(plan.GenerationRecipeDefinition, Is.SameAs(LandmassGenerationRecipes.PrimaryContinentalLandmass));
            Assert.That(plan.GenerationSchemaDefinition, Is.SameAs(BuiltInGenerationSchemas.World));
            Assert.That(plan.RunSettings.Grid, Is.SameAs(grid));
            Assert.That(plan.RunSettings.Seed, Is.EqualTo(seed));
            Assert.That(plan.Grid, Is.SameAs(grid));
            Assert.That(plan.Seed, Is.EqualTo(seed));
        }

        [Test]
        public void CompilePrimaryContinentalLandmass_CreatesExpectedStagePlanNodes()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            Assert.That(plan.StagePlanNodes, Has.Count.EqualTo(1));

            Assert.That(
                plan.StagePlanNodes[0].StageDefinition,
                Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));

            Assert.That(
                plan.StagePlanNodes[0].StageRouteDefinition,
                Is.SameAs(Generation.Landmass.Routes.LandmassStageRoutes.PrimaryContinentalLandmass));

            Assert.That(
                plan.StagePlanNodes[0].StageContract,
                Is.SameAs(LandmassStageContracts.ContinentalLandmass));
        }

        [Test]
        public void StagePlanNodes_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            Assert.That(
                plan.StagePlanNodes,
                Is.InstanceOf<ICollection<StagePlanNode>>());

            ICollection<StagePlanNode> stagePlanNodes =
                (ICollection<StagePlanNode>)plan.StagePlanNodes;

            Assert.That(stagePlanNodes.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => stagePlanNodes.Add(plan.StagePlanNodes[0]));

            Assert.Throws<NotSupportedException>(
                stagePlanNodes.Clear);
        }

        [Test]
        public void Equals_WithEquivalentCompilerCreatedPlan_ReturnsTrue()
        {
            GenerationPlan left = CompilePrimaryContinentalLandmassPlan();
            GenerationPlan right = CompilePrimaryContinentalLandmassPlan();

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentRunSettings_ReturnsFalse()
        {
            GenerationPlan left = CompilePrimaryContinentalLandmassPlan(new Grid(256, 256), new Seed(123UL));
            GenerationPlan right = CompilePrimaryContinentalLandmassPlan(new Grid(256, 256), new Seed(456UL));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            Assert.That(plan.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan();

            Assert.That(plan.Equals("GenerationPlan"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationPlan? left = null;
            GenerationPlan? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationPlan? left = CompilePrimaryContinentalLandmassPlan();
            GenerationPlan? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsGenerationPlanSummary()
        {
            GenerationPlan plan = CompilePrimaryContinentalLandmassPlan(new Grid(256, 256), new Seed(123UL));

            string value = plan.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationPlan(GenerationRecipeDefinition: lokrain.atlas.landmass.recipe.primary_continental_landmass, RunSettings: GenerationRunSettings(Grid: Grid(Width: 256, Depth: 256), Seed: 123), StagePlanNodes: 1)"));
        }

        private static GenerationPlan CompilePrimaryContinentalLandmassPlan()
        {
            return CompilePrimaryContinentalLandmassPlan(new Grid(256, 256), new Seed(123UL));
        }

        private static GenerationPlan CompilePrimaryContinentalLandmassPlan(
            Grid grid,
            Seed seed)
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(grid, seed);

            GenerationRequestResolver resolver = new();
            GenerationRequestResolutionResult resolutionResult = resolver.Resolve(catalog, descriptor);

            Assert.That(resolutionResult.Succeeded, Is.True);
            Assert.That(resolutionResult.GenerationRequest, Is.Not.Null);

            GenerationPlanCompiler compiler = new();

            return compiler.Compile(resolutionResult.GenerationRequest!);
        }
    }
}