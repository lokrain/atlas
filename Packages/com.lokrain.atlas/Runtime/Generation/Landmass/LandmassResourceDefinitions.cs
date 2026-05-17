#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned built-in landmass semantic resource definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass resource definitions are stable catalog-owned semantic resources used by built-in landmass
    /// stage and operation contracts. They describe what values exist in the managed generation graph.
    /// </para>
    /// <para>
    /// They are not field definitions, artifact definitions, runtime identifiers, execution bindings, job data,
    /// native containers, ECS systems, Unity runtime objects, storage layouts, or scheduler allocations.
    /// </para>
    /// <para>
    /// Field definitions and execution bindings will be introduced later by the execution/profile layer.
    /// </para>
    /// </remarks>
    public static class LandmassResourceDefinitions
    {
        private const string ContinentSuitabilitySymbolValue =
            "lokrain.atlas.landmass.resource.continent_suitability";

        private const string ContinentCandidateSymbolValue =
            "lokrain.atlas.landmass.resource.continent_candidate";

        private const string MainContinentSymbolValue =
            "lokrain.atlas.landmass.resource.main_continent";

        private const string ContinentalLandmassAreaSymbolValue =
            "lokrain.atlas.landmass.resource.continental_landmass_area";

        private const string BaseElevationSymbolValue =
            "lokrain.atlas.landmass.resource.base_elevation";

        /// <summary>
        /// Gets the continent suitability resource definition.
        /// </summary>
        public static ResourceDefinition ContinentSuitability { get; } =
            new(
                Symbol.Create(ContinentSuitabilitySymbolValue),
                DisplayName.Create("Continent Suitability"),
                BuiltInGenerationSchemas.World);

        /// <summary>
        /// Gets the continent candidate resource definition.
        /// </summary>
        public static ResourceDefinition ContinentCandidate { get; } =
            new(
                Symbol.Create(ContinentCandidateSymbolValue),
                DisplayName.Create("Continent Candidate"),
                BuiltInGenerationSchemas.World);

        /// <summary>
        /// Gets the main continent resource definition.
        /// </summary>
        public static ResourceDefinition MainContinent { get; } =
            new(
                Symbol.Create(MainContinentSymbolValue),
                DisplayName.Create("Main Continent"),
                BuiltInGenerationSchemas.World);

        /// <summary>
        /// Gets the completed continental landmass area resource definition.
        /// </summary>
        public static ResourceDefinition ContinentalLandmassArea { get; } =
            new(
                Symbol.Create(ContinentalLandmassAreaSymbolValue),
                DisplayName.Create("Continental Landmass Area"),
                BuiltInGenerationSchemas.World);

        /// <summary>
        /// Gets the base elevation resource definition.
        /// </summary>
        public static ResourceDefinition BaseElevation { get; } =
            new(
                Symbol.Create(BaseElevationSymbolValue),
                DisplayName.Create("Base Elevation"),
                BuiltInGenerationSchemas.World);

        private static readonly ResourceDefinition[] ResourceDefinitions =
        {
            ContinentSuitability,
            ContinentCandidate,
            MainContinent,
            ContinentalLandmassArea,
            BaseElevation
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass resource definitions.
        /// </summary>
        public static IReadOnlyList<ResourceDefinition> All { get; } =
            new ReadOnlyCollection<ResourceDefinition>(ResourceDefinitions);
    }
}