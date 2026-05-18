#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned built-in landmass managed field definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass field definitions describe managed representation metadata for built-in landmass semantic
    /// resources. They are used by runnable compilation and execution infrastructure to identify the fields
    /// required by landmass generation.
    /// </para>
    /// <para>
    /// These definitions are managed metadata only. They are not storage allocations, native containers,
    /// executable operation bindings, scheduler allocations, artifacts, ECS data, Unity runtime objects, or job data.
    /// </para>
    /// <para>
    /// Catalog ownership and execution support are established outside this type.
    /// </para>
    /// </remarks>
    public static class LandmassFieldDefinitions
    {
        private const string ContinentSuitabilitySymbolValue =
            "lokrain.atlas.landmass.field.continent_suitability";

        private const string ContinentCandidateSymbolValue =
            "lokrain.atlas.landmass.field.continent_candidate";

        private const string MainContinentSymbolValue =
            "lokrain.atlas.landmass.field.main_continent";

        private const string ContinentalLandmassAreaSymbolValue =
            "lokrain.atlas.landmass.field.continental_landmass_area";

        private const string BaseElevationSymbolValue =
            "lokrain.atlas.landmass.field.base_elevation";

        /// <summary>
        /// Gets the continent suitability field definition.
        /// </summary>
        public static FieldDefinition ContinentSuitability { get; } =
            new(
                LandmassResourceDefinitions.ContinentSuitability,
                Symbol.Create(ContinentSuitabilitySymbolValue),
                DisplayName.Create("Continent Suitability Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

        /// <summary>
        /// Gets the continent candidate field definition.
        /// </summary>
        public static FieldDefinition ContinentCandidate { get; } =
            new(
                LandmassResourceDefinitions.ContinentCandidate,
                Symbol.Create(ContinentCandidateSymbolValue),
                DisplayName.Create("Continent Candidate Field"),
                FieldShape.Grid,
                FieldValueKind.Boolean);

        /// <summary>
        /// Gets the main continent field definition.
        /// </summary>
        public static FieldDefinition MainContinent { get; } =
            new(
                LandmassResourceDefinitions.MainContinent,
                Symbol.Create(MainContinentSymbolValue),
                DisplayName.Create("Main Continent Field"),
                FieldShape.Grid,
                FieldValueKind.Boolean);

        /// <summary>
        /// Gets the completed continental landmass area field definition.
        /// </summary>
        public static FieldDefinition ContinentalLandmassArea { get; } =
            new(
                LandmassResourceDefinitions.ContinentalLandmassArea,
                Symbol.Create(ContinentalLandmassAreaSymbolValue),
                DisplayName.Create("Continental Landmass Area Field"),
                FieldShape.Grid,
                FieldValueKind.Boolean);

        /// <summary>
        /// Gets the base elevation field definition.
        /// </summary>
        public static FieldDefinition BaseElevation { get; } =
            new(
                LandmassResourceDefinitions.BaseElevation,
                Symbol.Create(BaseElevationSymbolValue),
                DisplayName.Create("Base Elevation Field"),
                FieldShape.Grid,
                FieldValueKind.Single);

        private static readonly FieldDefinition[] FieldDefinitions =
        {
            ContinentSuitability,
            ContinentCandidate,
            MainContinent,
            ContinentalLandmassArea,
            BaseElevation
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass field definitions in declared semantic order.
        /// </summary>
        public static IReadOnlyList<FieldDefinition> All { get; } =
            new ReadOnlyCollection<FieldDefinition>(FieldDefinitions);
    }
}
