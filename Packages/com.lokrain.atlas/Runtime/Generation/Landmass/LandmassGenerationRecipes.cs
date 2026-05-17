#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Recipes;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned landmass generation recipe definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass generation recipes are accepted resolved generation templates. They select built-in stage routes
    /// and operation implementations using catalog-owned definition objects, not unresolved request symbols.
    /// </para>
    /// <para>
    /// Recipe symbols are stable machine-facing catalog identity values. Display names are user-facing metadata
    /// only.
    /// </para>
    /// <para>
    /// Recipes do not contain executable bindings, runtime state, job data, native containers, ECS systems,
    /// Burst function pointers, or Unity runtime objects.
    /// </para>
    /// </remarks>
    public static class LandmassGenerationRecipes
    {
        private const string PrimaryContinentalLandmassSymbolValue =
            "lokrain.atlas.landmass.recipe.primary_continental_landmass";

        /// <summary>
        /// Gets the built-in primary continental landmass generation recipe.
        /// </summary>
        public static GenerationRecipeDefinition PrimaryContinentalLandmass { get; } = new(
            Symbol.Create(PrimaryContinentalLandmassSymbolValue),
            DisplayName.Create("Primary Continental Landmass"),
            LandmassStageDefinitions.ContinentalLandmass.GenerationSchema,
            new[]
            {
                new StageRouteChoice(
                    LandmassStageDefinitions.ContinentalLandmass,
                    LandmassStageRoutes.PrimaryContinentalLandmass,
                    LandmassStageContracts.ContinentalLandmass)
            },
            new[]
            {
                new StageRouteStepImplementationChoice(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                    LandmassOperationDefinitions.EvaluateContinentSuitability,
                    LandmassOperationContracts.EvaluateContinentSuitability,
                    LandmassOperationImplementations.EvaluateContinentSuitabilityDefault),

                new StageRouteStepImplementationChoice(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate,
                    LandmassOperationDefinitions.FormContinentCandidate,
                    LandmassOperationContracts.FormContinentCandidate,
                    LandmassOperationImplementations.FormContinentCandidateDefault),

                new StageRouteStepImplementationChoice(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent,
                    LandmassOperationDefinitions.ExtractMainContinent,
                    LandmassOperationContracts.ExtractMainContinent,
                    LandmassOperationImplementations.ExtractMainContinentDefault),

                new StageRouteStepImplementationChoice(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea,
                    LandmassOperationDefinitions.CompleteContinentArea,
                    LandmassOperationContracts.CompleteContinentArea,
                    LandmassOperationImplementations.CompleteContinentAreaDefault),

                new StageRouteStepImplementationChoice(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation,
                    LandmassOperationDefinitions.ComposeBaseElevation,
                    LandmassOperationContracts.ComposeBaseElevation,
                    LandmassOperationImplementations.ComposeBaseElevationDefault)
            });

        private static readonly GenerationRecipeDefinition[] GenerationRecipes =
        {
            PrimaryContinentalLandmass
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass generation recipe definitions.
        /// </summary>
        public static IReadOnlyList<GenerationRecipeDefinition> All { get; } =
            new ReadOnlyCollection<GenerationRecipeDefinition>(GenerationRecipes);
    }
}