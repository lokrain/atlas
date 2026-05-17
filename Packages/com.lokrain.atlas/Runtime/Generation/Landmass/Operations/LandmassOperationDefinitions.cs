#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Generation.Landmass.Operations
{
    /// <summary>
    /// Provides Atlas-owned landmass operation definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass operation definitions are stable catalog-owned definitions for built-in landmass generation.
    /// They are not route steps, implementation definitions, execution bindings, runtime identifiers, job data,
    /// native containers, ECS systems, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Operation definition symbols are stable machine-facing catalog identity values. Display names are
    /// user-facing metadata only.
    /// </para>
    /// </remarks>
    public static class LandmassOperationDefinitions
    {
        private const string EvaluateContinentSuitabilitySymbolValue =
            "lokrain.atlas.landmass.operation.evaluate_continent_suitability";

        private const string FormContinentCandidateSymbolValue =
            "lokrain.atlas.landmass.operation.form_continent_candidate";

        private const string ExtractMainContinentSymbolValue =
            "lokrain.atlas.landmass.operation.extract_main_continent";

        private const string CompleteContinentAreaSymbolValue =
            "lokrain.atlas.landmass.operation.complete_continent_area";

        private const string ComposeBaseElevationSymbolValue =
            "lokrain.atlas.landmass.operation.compose_base_elevation";

        /// <summary>
        /// Gets the built-in evaluate continent suitability operation definition.
        /// </summary>
        public static OperationDefinition EvaluateContinentSuitability { get; } = new(
            BuiltInGenerationSchemas.World,
            LandmassOperationKinds.ContinentSuitabilityEvaluation,
            Symbol.Create(EvaluateContinentSuitabilitySymbolValue),
            DisplayName.Create("Evaluate Continent Suitability"));

        /// <summary>
        /// Gets the built-in form continent candidate operation definition.
        /// </summary>
        public static OperationDefinition FormContinentCandidate { get; } = new(
            BuiltInGenerationSchemas.World,
            LandmassOperationKinds.ContinentCandidateFormation,
            Symbol.Create(FormContinentCandidateSymbolValue),
            DisplayName.Create("Form Continent Candidate"));

        /// <summary>
        /// Gets the built-in extract main continent operation definition.
        /// </summary>
        public static OperationDefinition ExtractMainContinent { get; } = new(
            BuiltInGenerationSchemas.World,
            LandmassOperationKinds.MainContinentExtraction,
            Symbol.Create(ExtractMainContinentSymbolValue),
            DisplayName.Create("Extract Main Continent"));

        /// <summary>
        /// Gets the built-in complete continent area operation definition.
        /// </summary>
        public static OperationDefinition CompleteContinentArea { get; } = new(
            BuiltInGenerationSchemas.World,
            LandmassOperationKinds.ContinentAreaCompletion,
            Symbol.Create(CompleteContinentAreaSymbolValue),
            DisplayName.Create("Complete Continent Area"));

        /// <summary>
        /// Gets the built-in compose base elevation operation definition.
        /// </summary>
        public static OperationDefinition ComposeBaseElevation { get; } = new(
            BuiltInGenerationSchemas.World,
            LandmassOperationKinds.BaseElevationComposition,
            Symbol.Create(ComposeBaseElevationSymbolValue),
            DisplayName.Create("Compose Base Elevation"));

        private static readonly OperationDefinition[] OperationDefinitions =
        {
            EvaluateContinentSuitability,
            FormContinentCandidate,
            ExtractMainContinent,
            CompleteContinentArea,
            ComposeBaseElevation
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass operation definitions.
        /// </summary>
        public static IReadOnlyList<OperationDefinition> All { get; } =
            new ReadOnlyCollection<OperationDefinition>(OperationDefinitions);
    }
}