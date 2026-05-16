// Packages/com.lokrain.atlas/Runtime/Generation/Operations/ComposeBaseElevation/ComposeBaseElevationOperation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.ComposeBaseElevation
//
// Purpose
// - Declare the ComposeBaseElevation operation contract.
// - Keep durable operation identity separate from stage route occurrence and job scheduling.
// - Preserve field access metadata before executors, schedulers, and jobs are implemented.

using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Operations.ComposeBaseElevation
{
    /// <summary>
    /// Contract metadata for the ComposeBaseElevation operation.
    /// </summary>
    public static class ComposeBaseElevationOperation
    {
        /// <summary>
        /// Durable operation identity for ComposeBaseElevation.
        /// </summary>
        public static readonly AtlasOperationId Id =
            new(0x65C2_BDE2_6D92_B56FUL, 0xAA49_D53C_31E4_598BUL, 1);

        /// <summary>
        /// Stable diagnostic operation name.
        /// </summary>
        public const string Name = "operation.landmass.compose_base_elevation";

        /// <summary>
        /// Creates this operation definition.
        /// </summary>
        public static AtlasOperationDefinition CreateDefinition()
        {
            return AtlasOperationDefinition.Create(
                Id,
                new FixedString64Bytes(Name),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Read<AtlasLandmassFields.LandMask, byte>(),
                AtlasOperationAccess.Read<AtlasLandmassFields.OceanMask, byte>(),
                AtlasOperationAccess.Read<AtlasLandmassFields.LandLabel, int>(),
                AtlasOperationAccess.Read<AtlasLandmassFields.ContinentSuitability, int>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.BaseElevation, int>());
        }
    }
}
