// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/AtlasLandmassSchemaNames.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass
//
// Purpose
// - Centralize Landmass stage-schema diagnostic names that are not field names.
// - Avoid duplicating route, policy, and contract-table labels across validators and tests.

namespace Lokrain.Atlas.Generation.Stages.Landmass
{
    /// <summary>
    /// Stable diagnostic names used by the Landmass stage schema layer.
    /// </summary>
    public static class AtlasLandmassSchemaNames
    {
        /// <summary>
        /// Diagnostic route name for the accepted PrimaryContinent route.
        /// </summary>
        public const string PrimaryContinentRoute = "route.landmass.primary_continent";

        /// <summary>
        /// Diagnostic contract-table name for the PrimaryContinent route field set.
        /// </summary>
        public const string PrimaryContinentContractTable = "contracts.landmass.primary_continent";
    }
}
