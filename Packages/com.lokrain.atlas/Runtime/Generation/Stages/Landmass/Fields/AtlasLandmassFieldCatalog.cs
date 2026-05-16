// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFieldCatalog.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Fields
//
// Purpose
// - Publish the Landmass stage field contract catalog.
// - Keep catalog order deterministic and independent from contract-table slot assignment.
// - Provide the selected PrimaryContinent field set without exposing mutable shared arrays.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Fields
{
    /// <summary>
    /// Field contract catalog for the Landmass stage.
    /// </summary>
    public static class AtlasLandmassFieldCatalog
    {
        /// <summary>
        /// Diagnostic catalog name.
        /// </summary>
        public static readonly FixedString64Bytes CatalogName =
            new("catalog.landmass.fields");

        /// <summary>
        /// Diagnostic contract-table name for the PrimaryContinent route field set.
        /// </summary>
        public static readonly FixedString64Bytes PrimaryContinentTableName =
            new(AtlasLandmassSchemaNames.PrimaryContinentContractTable);

        /// <summary>
        /// Number of field contracts required by the PrimaryContinent route.
        /// </summary>
        public const int PrimaryContinentFieldCount = 10;

        /// <summary>
        /// Creates the unique Landmass field contract catalog.
        /// </summary>
        public static AtlasContractCatalog CreateCatalog()
        {
            return AtlasContractCatalog.Create(
                CatalogName,
                CreatePrimaryContinentContracts());
        }

        /// <summary>
        /// Creates a freshly allocated array of PrimaryContinent field contracts in deterministic catalog order.
        /// </summary>
        public static AtlasContract[] CreatePrimaryContinentContracts()
        {
            return new[]
            {
                AtlasContract.Of<AtlasLandmassFields.LandMask, byte>(),
                AtlasContract.Of<AtlasLandmassFields.OceanMask, byte>(),
                AtlasContract.Of<AtlasLandmassFields.LandLabel, int>(),
                AtlasContract.Of<AtlasLandmassFields.BaseElevation, int>(),
                AtlasContract.Of<AtlasLandmassFields.ContinentSuitability, int>(),
                AtlasContract.Of<AtlasLandmassFields.ContinentSuitabilityCutoff, int>(),
                AtlasContract.Of<AtlasLandmassFields.ContinentCandidateMask, byte>(),
                AtlasContract.Of<AtlasLandmassFields.ContinentPrimaryMask, byte>(),
                AtlasContract.Of<AtlasLandmassFields.ContinentArea, int>(),
                AtlasContract.Of<AtlasLandmassFields.ContinentGrowthCutoff, int>()
            };
        }

        /// <summary>
        /// Creates a freshly allocated array of PrimaryContinent field ids in deterministic route order.
        /// </summary>
        public static StableDataId[] CreatePrimaryContinentFieldIds()
        {
            return new[]
            {
                AtlasLandmassFieldIds.LandMask,
                AtlasLandmassFieldIds.OceanMask,
                AtlasLandmassFieldIds.LandLabel,
                AtlasLandmassFieldIds.BaseElevation,
                AtlasLandmassFieldIds.ContinentSuitability,
                AtlasLandmassFieldIds.ContinentSuitabilityCutoff,
                AtlasLandmassFieldIds.ContinentCandidateMask,
                AtlasLandmassFieldIds.ContinentPrimaryMask,
                AtlasLandmassFieldIds.ContinentArea,
                AtlasLandmassFieldIds.ContinentGrowthCutoff
            };
        }

        /// <summary>
        /// Creates a freshly slotted PrimaryContinent contract table from the Landmass field catalog.
        /// </summary>
        public static AtlasContractTable CreatePrimaryContinentContractTable()
        {
            return CreateCatalog().CreateContractTable(
                PrimaryContinentTableName,
                CreatePrimaryContinentFieldIds());
        }
    }
}
