#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Generation.Landmass.Routes
{
    /// <summary>
    /// Provides Atlas-owned landmass stage route definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass stage routes define ordered operation occurrences for built-in landmass stages. They are catalog
    /// metadata only; they are not execution bindings, runtime identifiers, job data, native containers, ECS
    /// systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Route symbols are stable machine-facing catalog identity values. Display names are user-facing metadata
    /// only.
    /// </para>
    /// </remarks>
    public static class LandmassStageRoutes
    {
        private const string PrimaryContinentalLandmassSymbolValue =
            "lokrain.atlas.landmass.route.primary_continental_landmass";

        /// <summary>
        /// Gets the built-in primary continental landmass stage route.
        /// </summary>
        public static StageRouteDefinition PrimaryContinentalLandmass { get; } = new(
            LandmassStageDefinitions.ContinentalLandmass,
            Symbol.Create(PrimaryContinentalLandmassSymbolValue),
            DisplayName.Create("Primary Continental Landmass"),
            new[]
            {
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate,
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent,
                LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea,
                LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation
            });

        private static readonly StageRouteDefinition[] StageRoutes =
        {
            PrimaryContinentalLandmass
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass stage route definitions.
        /// </summary>
        public static IReadOnlyList<StageRouteDefinition> All { get; } =
            new ReadOnlyCollection<StageRouteDefinition>(StageRoutes);
    }
}