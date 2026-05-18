#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests.Routes
{
    public sealed class LandmassStageRoutesTests
    {
        [Test]
        public void All_ReturnsStageRoutesInDeclaredOrder()
        {
            Assert.That(LandmassStageRoutes.All, Has.Count.EqualTo(1));

            Assert.That(
                LandmassStageRoutes.All[0],
                Is.SameAs(LandmassStageRoutes.PrimaryContinentalLandmass));
        }

        [Test]
        public void PrimaryContinentalLandmass_UsesExpectedStageSymbolAndDisplayName()
        {
            StageRouteDefinition stageRoute = LandmassStageRoutes.PrimaryContinentalLandmass;

            Assert.That(stageRoute.StageDefinition, Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));
            Assert.That(stageRoute.Symbol.Value, Is.EqualTo("lokrain.atlas.landmass.route.primary_continental_landmass"));
            Assert.That(stageRoute.DisplayName.Value, Is.EqualTo("Primary Continental Landmass"));
        }

        [Test]
        public void PrimaryContinentalLandmass_ReturnsRouteStepsInDeclaredOrder()
        {
            StageRouteDefinition stageRoute = LandmassStageRoutes.PrimaryContinentalLandmass;

            Assert.That(stageRoute.StageRouteStepDefinitions, Has.Count.EqualTo(5));

            Assert.That(
                stageRoute.StageRouteStepDefinitions[0],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability));

            Assert.That(
                stageRoute.StageRouteStepDefinitions[1],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate));

            Assert.That(
                stageRoute.StageRouteStepDefinitions[2],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(
                stageRoute.StageRouteStepDefinitions[3],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea));

            Assert.That(
                stageRoute.StageRouteStepDefinitions[4],
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation));
        }

        [Test]
        public void All_DoesNotContainDuplicateStageRouteSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (StageRouteDefinition stageRoute in LandmassStageRoutes.All)
            {
                Assert.That(
                    symbols.Add(stageRoute.Symbol.Value),
                    Is.True,
                    $"Duplicate stage route symbol: {stageRoute.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassStageRoutes.All,
                Is.InstanceOf<ICollection<StageRouteDefinition>>());

            ICollection<StageRouteDefinition> stageRoutes =
                (ICollection<StageRouteDefinition>)LandmassStageRoutes.All;

            Assert.That(stageRoutes.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => stageRoutes.Add(LandmassStageRoutes.PrimaryContinentalLandmass));

            Assert.Throws<NotSupportedException>(
                stageRoutes.Clear);
        }
    }
}