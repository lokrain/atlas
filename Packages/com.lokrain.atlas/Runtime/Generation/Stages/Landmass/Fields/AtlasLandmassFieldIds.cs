// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFieldIds.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Fields
//
// Purpose
// - Define durable field identities for the Landmass stage contract.
// - Keep PrimaryContinent canonical and stage-transient field identities centralized.
// - Preserve route field identity independently from declaration type names, slots, and file layout.

using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Fields
{
    /// <summary>
    /// Durable field identities owned by the Landmass stage contract.
    /// </summary>
    /// <remarks>
    /// Stable identifiers must not change when declarations move between files, folders, or
    /// namespaces. Increment the version only when the field contract changes incompatibly.
    /// </remarks>
    public static class AtlasLandmassFieldIds
    {
        /// <summary>
        /// Canonical primary land mask: <c>field.land.mask</c>.
        /// </summary>
        public static readonly StableDataId LandMask =
            new(0x6AA4_367E_1AFC_2DA0UL, 0x5E32_BC15_BA27_ECBCUL, 1);

        /// <summary>
        /// Canonical ocean mask: <c>field.ocean.mask</c>.
        /// </summary>
        public static readonly StableDataId OceanMask =
            new(0x2C2D_714F_2B3C_21B7UL, 0x2A57_0C21_2F25_8659UL, 1);

        /// <summary>
        /// Canonical initial land label field: <c>field.land.label</c>.
        /// </summary>
        public static readonly StableDataId LandLabel =
            new(0x7747_D89A_1596_EF79UL, 0xDF8C_E05B_7A1A_8256UL, 1);

        /// <summary>
        /// Canonical full-map Q16.16 base elevation: <c>field.base.elevation</c>.
        /// </summary>
        public static readonly StableDataId BaseElevation =
            new(0xE63C_70D0_F93B_4D9AUL, 0x67C5_BF70_11C3_B741UL, 1);

        /// <summary>
        /// Stage-transient continent suitability field: <c>transient.continent.suitability</c>.
        /// </summary>
        public static readonly StableDataId ContinentSuitability =
            new(0xC8B7_1EA4_3658_333BUL, 0x61FF_28E7_39FD_4842UL, 1);

        /// <summary>
        /// Stage-transient suitability cutoff scalar: <c>transient.continent.suitability_cutoff</c>.
        /// </summary>
        public static readonly StableDataId ContinentSuitabilityCutoff =
            new(0x8211_90ED_C313_E151UL, 0xC093_6CCD_BB1A_90A4UL, 1);

        /// <summary>
        /// Stage-transient candidate mask field: <c>transient.continent.candidate_mask</c>.
        /// </summary>
        public static readonly StableDataId ContinentCandidateMask =
            new(0xA2DF_2052_B546_33E6UL, 0xBB38_573B_6711_BFF6UL, 1);

        /// <summary>
        /// Stage-transient preserved primary-continent mask: <c>transient.continent.primary_mask</c>.
        /// </summary>
        public static readonly StableDataId ContinentPrimaryMask =
            new(0x75E4_5AC5_7A6D_2880UL, 0x3F18_9769_4F5C_4B1CUL, 1);

        /// <summary>
        /// Stage-transient primary-continent area scalar: <c>transient.continent.area</c>.
        /// </summary>
        public static readonly StableDataId ContinentArea =
            new(0x1AFF_1751_2A55_480CUL, 0x6FC6_37F2_AC52_4C50UL, 1);

        /// <summary>
        /// Stage-transient growth cutoff scalar: <c>transient.continent.growth_cutoff</c>.
        /// </summary>
        public static readonly StableDataId ContinentGrowthCutoff =
            new(0xF7A1_1DBA_ED48_ABDBUL, 0x65BC_24D7_089A_8324UL, 1);
    }
}
