#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;

namespace Lokrain.Atlas.Generation.Landmass.Operations
{
    /// <summary>
    /// Provides Atlas-owned landmass operation implementation definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass operation implementation definitions identify selectable implementation options for built-in
    /// landmass operations. They are catalog metadata only; they do not contain executable bindings, delegates,
    /// reflection types, ECS systems, Burst function pointers, job structs, runtime state, job data, or native
    /// containers.
    /// </para>
    /// <para>
    /// Executable implementation binding belongs to the runnable-plan compilation layer. These definitions only
    /// provide stable machine-facing implementation identity and user-facing metadata.
    /// </para>
    /// </remarks>
    public static class LandmassOperationImplementations
    {
        private const string EvaluateContinentSuitabilityDefaultSymbolValue =
            "lokrain.atlas.landmass.implementation.evaluate_continent_suitability.default";

        private const string FormContinentCandidateDefaultSymbolValue =
            "lokrain.atlas.landmass.implementation.form_continent_candidate.default";

        private const string ExtractMainContinentDefaultSymbolValue =
            "lokrain.atlas.landmass.implementation.extract_main_continent.default";

        private const string CompleteContinentAreaDefaultSymbolValue =
            "lokrain.atlas.landmass.implementation.complete_continent_area.default";

        private const string ComposeBaseElevationDefaultSymbolValue =
            "lokrain.atlas.landmass.implementation.compose_base_elevation.default";

        /// <summary>
        /// Gets the default evaluate continent suitability implementation definition.
        /// </summary>
        public static OperationImplementationDefinition EvaluateContinentSuitabilityDefault { get; } = new(
            LandmassOperationDefinitions.EvaluateContinentSuitability,
            Symbol.Create(EvaluateContinentSuitabilityDefaultSymbolValue),
            DisplayName.Create("Default Evaluate Continent Suitability"));

        /// <summary>
        /// Gets the default form continent candidate implementation definition.
        /// </summary>
        public static OperationImplementationDefinition FormContinentCandidateDefault { get; } = new(
            LandmassOperationDefinitions.FormContinentCandidate,
            Symbol.Create(FormContinentCandidateDefaultSymbolValue),
            DisplayName.Create("Default Form Continent Candidate"));

        /// <summary>
        /// Gets the default extract main continent implementation definition.
        /// </summary>
        public static OperationImplementationDefinition ExtractMainContinentDefault { get; } = new(
            LandmassOperationDefinitions.ExtractMainContinent,
            Symbol.Create(ExtractMainContinentDefaultSymbolValue),
            DisplayName.Create("Default Extract Main Continent"));

        /// <summary>
        /// Gets the default complete continent area implementation definition.
        /// </summary>
        public static OperationImplementationDefinition CompleteContinentAreaDefault { get; } = new(
            LandmassOperationDefinitions.CompleteContinentArea,
            Symbol.Create(CompleteContinentAreaDefaultSymbolValue),
            DisplayName.Create("Default Complete Continent Area"));

        /// <summary>
        /// Gets the default compose base elevation implementation definition.
        /// </summary>
        public static OperationImplementationDefinition ComposeBaseElevationDefault { get; } = new(
            LandmassOperationDefinitions.ComposeBaseElevation,
            Symbol.Create(ComposeBaseElevationDefaultSymbolValue),
            DisplayName.Create("Default Compose Base Elevation"));

        private static readonly OperationImplementationDefinition[] OperationImplementations =
        {
            EvaluateContinentSuitabilityDefault,
            FormContinentCandidateDefault,
            ExtractMainContinentDefault,
            CompleteContinentAreaDefault,
            ComposeBaseElevationDefault
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass operation implementation definitions.
        /// </summary>
        public static IReadOnlyList<OperationImplementationDefinition> All { get; } =
            new ReadOnlyCollection<OperationImplementationDefinition>(OperationImplementations);
    }
}