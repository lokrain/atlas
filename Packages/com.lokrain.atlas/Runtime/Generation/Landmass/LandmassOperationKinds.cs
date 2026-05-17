#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Operations;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned landmass operation kinds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass operation kinds are stable catalog-facing categories used by built-in landmass operation
    /// definitions. They are not operation definitions, implementation definitions, execution bindings,
    /// runtime identifiers, job data, native containers, ECS systems, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Operation kind symbols are stable machine-facing contract values.
    /// </para>
    /// </remarks>
    public static class LandmassOperationKinds
    {
        private const string ContinentSuitabilityEvaluationSymbolValue =
            "lokrain.atlas.landmass.operation_kind.continent_suitability_evaluation";

        private const string ContinentCandidateFormationSymbolValue =
            "lokrain.atlas.landmass.operation_kind.continent_candidate_formation";

        private const string MainContinentExtractionSymbolValue =
            "lokrain.atlas.landmass.operation_kind.main_continent_extraction";

        private const string ContinentAreaCompletionSymbolValue =
            "lokrain.atlas.landmass.operation_kind.continent_area_completion";

        private const string BaseElevationCompositionSymbolValue =
            "lokrain.atlas.landmass.operation_kind.base_elevation_composition";

        /// <summary>
        /// Gets the built-in continent suitability evaluation operation kind.
        /// </summary>
        public static OperationKind ContinentSuitabilityEvaluation { get; } =
            OperationKind.Create(ContinentSuitabilityEvaluationSymbolValue);

        /// <summary>
        /// Gets the built-in continent candidate formation operation kind.
        /// </summary>
        public static OperationKind ContinentCandidateFormation { get; } =
            OperationKind.Create(ContinentCandidateFormationSymbolValue);

        /// <summary>
        /// Gets the built-in main continent extraction operation kind.
        /// </summary>
        public static OperationKind MainContinentExtraction { get; } =
            OperationKind.Create(MainContinentExtractionSymbolValue);

        /// <summary>
        /// Gets the built-in continent area completion operation kind.
        /// </summary>
        public static OperationKind ContinentAreaCompletion { get; } =
            OperationKind.Create(ContinentAreaCompletionSymbolValue);

        /// <summary>
        /// Gets the built-in base elevation composition operation kind.
        /// </summary>
        public static OperationKind BaseElevationComposition { get; } =
            OperationKind.Create(BaseElevationCompositionSymbolValue);

        private static readonly OperationKind[] OperationKinds =
        {
            ContinentSuitabilityEvaluation,
            ContinentCandidateFormation,
            MainContinentExtraction,
            ContinentAreaCompletion,
            BaseElevationComposition
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass operation kinds.
        /// </summary>
        public static IReadOnlyList<OperationKind> All { get; } =
            new ReadOnlyCollection<OperationKind>(OperationKinds);
    }
}