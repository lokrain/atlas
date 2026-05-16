// Packages/com.lokrain.atlas/Runtime/Generation/Operations/PreserveMainContinent/PreserveMainContinentOperation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.PreserveMainContinent
//
// Purpose
// - Declare the PreserveMainContinent operation contract.
// - Keep durable operation identity separate from stage route occurrence and job scheduling.
// - Preserve field access metadata before executors, schedulers, and jobs are implemented.

using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Operations.PreserveMainContinent
{
    /// <summary>
    /// Contract metadata for the PreserveMainContinent operation.
    /// </summary>
    public static class PreserveMainContinentOperation
    {
        /// <summary>
        /// Durable operation identity for PreserveMainContinent.
        /// </summary>
        public static readonly AtlasOperationId Id =
            new(0xCCFE_75F5_F2B6_D669UL, 0x7BDE_E027_FCBE_C670UL, 1);

        /// <summary>
        /// Stable diagnostic operation name.
        /// </summary>
        public const string Name = "operation.landmass.preserve_main_continent";

        /// <summary>
        /// Creates this operation definition.
        /// </summary>
        public static AtlasOperationDefinition CreateDefinition()
        {
            return AtlasOperationDefinition.Create(
                Id,
                new FixedString64Bytes(Name),
                AtlasOperationRole.TopologyProcessing,
                AtlasOperationAccess.Read<AtlasLandmassFields.ContinentCandidateMask, byte>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.ContinentPrimaryMask, byte>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.ContinentArea, int>());
        }
    }
}
