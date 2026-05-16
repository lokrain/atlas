// Packages/com.lokrain.atlas/Runtime/Generation/Operations/FormContinentCandidate/FormContinentCandidateOperation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Operations.FormContinentCandidate
//
// Purpose
// - Declare the FormContinentCandidate operation contract.
// - Keep durable operation identity separate from stage route occurrence and job scheduling.
// - Preserve field access metadata before executors, schedulers, and jobs are implemented.

using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Operations.FormContinentCandidate
{
    /// <summary>
    /// Contract metadata for the FormContinentCandidate operation.
    /// </summary>
    public static class FormContinentCandidateOperation
    {
        /// <summary>
        /// Durable operation identity for FormContinentCandidate.
        /// </summary>
        public static readonly AtlasOperationId Id =
            new(0x5F9A_027E_EFEA_E2FDUL, 0x741E_AD4E_6DDE_6450UL, 1);

        /// <summary>
        /// Stable diagnostic operation name.
        /// </summary>
        public const string Name = "operation.landmass.form_continent_candidate";

        /// <summary>
        /// Creates this operation definition.
        /// </summary>
        public static AtlasOperationDefinition CreateDefinition()
        {
            return AtlasOperationDefinition.Create(
                Id,
                new FixedString64Bytes(Name),
                AtlasOperationRole.TopologyProcessing,
                AtlasOperationAccess.Read<AtlasLandmassFields.ContinentSuitability, int>(),
                AtlasOperationAccess.Read<AtlasLandmassFields.ContinentSuitabilityCutoff, int>(),
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.ContinentCandidateMask, byte>());
        }
    }
}
