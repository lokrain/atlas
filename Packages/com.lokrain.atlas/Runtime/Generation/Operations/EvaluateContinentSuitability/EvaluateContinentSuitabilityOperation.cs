// Packages/com.lokrain.atlas/Runtime/Generation/Operations/EvaluateContinentSuitability/EvaluateContinentSuitabilityOperation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
//
// Purpose
// - Declare the EvaluateContinentSuitability operation contract.
// - Keep durable operation identity separate from stage route occurrence and job scheduling.
// - Preserve field access metadata before executors, schedulers, and jobs are implemented.

using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability
{
    /// <summary>
    /// Contract metadata for the EvaluateContinentSuitability operation.
    /// </summary>
    public static class EvaluateContinentSuitabilityOperation
    {
        /// <summary>
        /// Durable operation identity for EvaluateContinentSuitability.
        /// </summary>
        public static readonly AtlasOperationId Id =
            new(0xD1A7_D054_663B_E6D4UL, 0x4FD8_464B_B80C_5ED5UL, 1);

        /// <summary>
        /// Stable diagnostic operation name.
        /// </summary>
        public const string Name = "operation.landmass.evaluate_continent_suitability";

        /// <summary>
        /// Creates this operation definition.
        /// </summary>
        public static AtlasOperationDefinition CreateDefinition()
        {
            return AtlasOperationDefinition.Create(
                Id,
                new FixedString64Bytes(Name),
                AtlasOperationRole.SupportGeneration,
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.ContinentSuitability, int>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.ContinentSuitabilityCutoff, int>());
        }
    }
}
