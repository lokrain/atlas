#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests.Routes
{
    public sealed class LandmassStageRouteStepsTests
    {
        [Test]
        public void All_ReturnsStageRouteStepsInDeclaredOrder()
        {
            Assert.That(LandmassStageRouteSteps.All, Has.Count.EqualTo(5));

            Assert.That(
                LandmassStageRouteSteps.All[0],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability));

            Assert.That(
                LandmassStageRouteSteps.All[1],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate));

            Assert.That(
                LandmassStageRouteSteps.All[2],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(
                LandmassStageRouteSteps.All[3],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea));

            Assert.That(
                LandmassStageRouteSteps.All[4],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation));
        }

        [Test]
        public void Definitions_UseExpectedSymbolsDisplayNamesAndOperationDefinitionSymbols()
        {
            AssertStageRouteStep(
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                "lokrain.atlas.landmass.route_step.primary_continental_landmass.evaluate_continent_suitability",
                "Evaluate Continent Suitability",
                LandmassOperationDefinitions.EvaluateContinentSuitability);

            AssertStageRouteStep(
                LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate,
                "lokrain.atlas.landmass.route_step.primary_continental_landmass.form_continent_candidate",
                "Form Continent Candidate",
                LandmassOperationDefinitions.FormContinentCandidate);

            AssertStageRouteStep(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent,
                "lokrain.atlas.landmass.route_step.primary_continental_landmass.extract_main_continent",
                "Extract Main Continent",
                LandmassOperationDefinitions.ExtractMainContinent);

            AssertStageRouteStep(
                LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea,
                "lokrain.atlas.landmass.route_step.primary_continental_landmass.complete_continent_area",
                "Complete Continent Area",
                LandmassOperationDefinitions.CompleteContinentArea);

            AssertStageRouteStep(
                LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation,
                "lokrain.atlas.landmass.route_step.primary_continental_landmass.compose_base_elevation",
                "Compose Base Elevation",
                LandmassOperationDefinitions.ComposeBaseElevation);
        }

        [Test]
        public void All_DoesNotContainDuplicateStageRouteStepSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (StageRouteStepDefinition stageRouteStep in LandmassStageRouteSteps.All)
            {
                Assert.That(
                    symbols.Add(stageRouteStep.Symbol.Value),
                    Is.True,
                    $"Duplicate stage route step symbol: {stageRouteStep.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassStageRouteSteps.All,
                Is.InstanceOf<ICollection<StageRouteStepDefinition>>());

            ICollection<StageRouteStepDefinition> stageRouteSteps =
                (ICollection<StageRouteStepDefinition>)LandmassStageRouteSteps.All;

            Assert.That(stageRouteSteps.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => stageRouteSteps.Add(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability));

            Assert.Throws<NotSupportedException>(
                stageRouteSteps.Clear);
        }

        private static void AssertStageRouteStep(
            StageRouteStepDefinition stageRouteStep,
            string expectedSymbolValue,
            string expectedDisplayNameValue,
            OperationDefinition expectedOperationDefinition)
        {
            Assert.That(stageRouteStep.Symbol.Value, Is.EqualTo(expectedSymbolValue));
            Assert.That(stageRouteStep.DisplayName.Value, Is.EqualTo(expectedDisplayNameValue));
            Assert.That(stageRouteStep.OperationDefinitionSymbol, Is.EqualTo(expectedOperationDefinition.Symbol));
        }
    }
}