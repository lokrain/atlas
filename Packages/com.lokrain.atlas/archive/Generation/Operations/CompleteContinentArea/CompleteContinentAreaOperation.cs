// Packages/com.lokrain.atlas/Runtime/Generation/Operations/CompleteContinentArea/CompleteContinentAreaOperation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.CompleteContinentArea
//
// Purpose
// - Declare the CompleteContinentArea operation contract.
// - Keep durable operation identity separate from stage route occurrence and job scheduling.
// - Preserve field access metadata before executors, schedulers, and jobs are implemented.

using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Operations.CompleteContinentArea
{
    /// <summary>
    /// Contract metadata for the CompleteContinentArea operation.
    /// </summary>
    public static class CompleteContinentAreaOperation
    {
        /// <summary>
        /// Durable operation identity for CompleteContinentArea.
        /// </summary>
        public static readonly AtlasOperationId Id =
            new(0x0535_76DC_26C2_B0A4UL, 0x4DDA_3AAC_73CA_18D9UL, 1);

        /// <summary>
        /// Stable diagnostic operation name.
        /// </summary>
        public const string Name = "operation.landmass.complete_continent_area";

        /// <summary>
        /// Creates this operation definition.
        /// </summary>
        public static AtlasOperationDefinition CreateDefinition()
        {
            return AtlasOperationDefinition.Create(
                Id,
                new FixedString64Bytes(Name),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Read<AtlasLandmassFields.ContinentSuitability, int>(),
                AtlasOperationAccess.ReadWrite<AtlasLandmassFields.ContinentPrimaryMask, byte>(
                    AtlasWriteCoverage.FullLogicalLength),
                AtlasOperationAccess.ReadWrite<AtlasLandmassFields.ContinentArea, int>(
                    AtlasWriteCoverage.FullCapacity),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.ContinentGrowthCutoff, int>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.LandMask, byte>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.OceanMask, byte>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.LandLabel, int>());
        }
    }
}
