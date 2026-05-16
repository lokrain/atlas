// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFieldNames.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Fields
//
// Purpose
// - Centralize stable diagnostic names for Landmass fields and shape domains.
// - Prevent duplicated string literals across field declarations, catalogs, and tests.

namespace Lokrain.Atlas.Generation.Stages.Landmass.Fields
{
    /// <summary>
    /// Stable diagnostic names used by the Landmass field contract declarations.
    /// </summary>
    public static class AtlasLandmassFieldNames
    {
        /// <summary>
        /// Shared dense map-cell shape resolver name.
        /// </summary>
        public const string MapCells = "map.cells";

        public const string LandMask = "field.land.mask";
        public const string OceanMask = "field.ocean.mask";
        public const string LandLabel = "field.land.label";
        public const string BaseElevation = "field.base.elevation";

        public const string ContinentSuitability = "transient.continent.suitability";
        public const string ContinentSuitabilityCutoff = "transient.continent.suitability_cutoff";
        public const string ContinentCandidateMask = "transient.continent.candidate_mask";
        public const string ContinentPrimaryMask = "transient.continent.primary_mask";
        public const string ContinentArea = "transient.continent.area";
        public const string ContinentGrowthCutoff = "transient.continent.growth_cutoff";
    }
}
