#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Generation.Landmass.Routes
{
    /// <summary>
    /// Provides Atlas-owned landmass stage route step definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass stage route steps identify ordered operation occurrences inside built-in landmass stage routes.
    /// They are not operation definitions, implementation definitions, execution bindings, runtime identifiers,
    /// job data, native containers, ECS systems, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Route step symbols are stable machine-facing occurrence identities. Multiple route steps may reference the
    /// same operation definition in future routes while preserving distinct per-step identity.
    /// </para>
    /// </remarks>
    public static class LandmassStageRouteSteps
    {
        private const string PrimaryContinentalLandmassEvaluateContinentSuitabilitySymbolValue =
            "lokrain.atlas.landmass.route_step.primary_continental_landmass.evaluate_continent_suitability";

        private const string PrimaryContinentalLandmassFormContinentCandidateSymbolValue =
            "lokrain.atlas.landmass.route_step.primary_continental_landmass.form_continent_candidate";

        private const string PrimaryContinentalLandmassExtractMainContinentSymbolValue =
            "lokrain.atlas.landmass.route_step.primary_continental_landmass.extract_main_continent";

        private const string PrimaryContinentalLandmassCompleteContinentAreaSymbolValue =
            "lokrain.atlas.landmass.route_step.primary_continental_landmass.complete_continent_area";

        private const string PrimaryContinentalLandmassComposeBaseElevationSymbolValue =
            "lokrain.atlas.landmass.route_step.primary_continental_landmass.compose_base_elevation";

        /// <summary>
        /// Gets the primary continental landmass evaluate continent suitability route step.
        /// </summary>
        public static StageRouteStepDefinition PrimaryContinentalLandmassEvaluateContinentSuitability { get; } = new(
            Symbol.Create(PrimaryContinentalLandmassEvaluateContinentSuitabilitySymbolValue),
            DisplayName.Create("Evaluate Continent Suitability"),
            LandmassOperationDefinitions.EvaluateContinentSuitability.Symbol);

        /// <summary>
        /// Gets the primary continental landmass form continent candidate route step.
        /// </summary>
        public static StageRouteStepDefinition PrimaryContinentalLandmassFormContinentCandidate { get; } = new(
            Symbol.Create(PrimaryContinentalLandmassFormContinentCandidateSymbolValue),
            DisplayName.Create("Form Continent Candidate"),
            LandmassOperationDefinitions.FormContinentCandidate.Symbol);

        /// <summary>
        /// Gets the primary continental landmass extract main continent route step.
        /// </summary>
        public static StageRouteStepDefinition PrimaryContinentalLandmassExtractMainContinent { get; } = new(
            Symbol.Create(PrimaryContinentalLandmassExtractMainContinentSymbolValue),
            DisplayName.Create("Extract Main Continent"),
            LandmassOperationDefinitions.ExtractMainContinent.Symbol);

        /// <summary>
        /// Gets the primary continental landmass complete continent area route step.
        /// </summary>
        public static StageRouteStepDefinition PrimaryContinentalLandmassCompleteContinentArea { get; } = new(
            Symbol.Create(PrimaryContinentalLandmassCompleteContinentAreaSymbolValue),
            DisplayName.Create("Complete Continent Area"),
            LandmassOperationDefinitions.CompleteContinentArea.Symbol);

        /// <summary>
        /// Gets the primary continental landmass compose base elevation route step.
        /// </summary>
        public static StageRouteStepDefinition PrimaryContinentalLandmassComposeBaseElevation { get; } = new(
            Symbol.Create(PrimaryContinentalLandmassComposeBaseElevationSymbolValue),
            DisplayName.Create("Compose Base Elevation"),
            LandmassOperationDefinitions.ComposeBaseElevation.Symbol);

        private static readonly StageRouteStepDefinition[] StageRouteSteps =
        {
            PrimaryContinentalLandmassEvaluateContinentSuitability,
            PrimaryContinentalLandmassFormContinentCandidate,
            PrimaryContinentalLandmassExtractMainContinent,
            PrimaryContinentalLandmassCompleteContinentArea,
            PrimaryContinentalLandmassComposeBaseElevation
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass stage route step definitions.
        /// </summary>
        public static IReadOnlyList<StageRouteStepDefinition> All { get; } =
            new ReadOnlyCollection<StageRouteStepDefinition>(StageRouteSteps);
    }
}