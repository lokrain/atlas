// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/LandmassStage.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass
//
// Purpose
// - Declare the durable Landmass stage identity and schema-facing helpers.
// - Bind the accepted PrimaryContinent route to the Landmass stage without making the route a stage.
// - Publish required canonical outputs for stage-schema validation.
// - Provide a route-specific pipeline validation policy without introducing jobs, executors, or schedulers.

using Lokrain.Atlas.Core;
using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass
{
    /// <summary>
    /// Durable schema contract for the Landmass stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass is the semantic stage. PrimaryContinent is one accepted route inside the stage.
    /// This type owns the stage identity and stage-level required outputs; route operation order
    /// remains in <see cref="PrimaryContinentRoute"/>.
    /// </para>
    /// </remarks>
    public static class LandmassStage
    {
        /// <summary>
        /// Durable stage identity for the Landmass stage.
        /// </summary>
        public static readonly AtlasStageId Id =
            new(0xC3F8_C044_C3F5_0A7FUL, 0x82C3_8E20_4CF2_736BUL, 1);

        /// <summary>
        /// Stable diagnostic stage name.
        /// </summary>
        public const string Name = "stage.landmass";

        /// <summary>
        /// Diagnostic stage name as a fixed string.
        /// </summary>
        public static readonly FixedString64Bytes DebugName =
            new(Name);

        /// <summary>
        /// Diagnostic pipeline-policy name for the accepted PrimaryContinent route.
        /// </summary>
        public static readonly FixedString64Bytes PrimaryContinentPolicyName =
            new("policy.landmass.primary_continent");

        /// <summary>
        /// Number of canonical outputs required from every accepted Landmass route.
        /// </summary>
        public const int RequiredCanonicalOutputCount = 4;

        /// <summary>
        /// Creates the Landmass stage definition using the accepted PrimaryContinent route.
        /// </summary>
        public static AtlasStageDefinition CreatePrimaryContinentStageDefinition()
        {
            return AtlasStageDefinition.Create(
                Id,
                DebugName,
                PrimaryContinentRoute.CreateOperationSet());
        }

        /// <summary>
        /// Creates the validation policy that accepts the Landmass PrimaryContinent route shape.
        /// </summary>
        /// <remarks>
        /// The policy constrains stage identity, allowed operations, and operation order. It does
        /// not validate field output compatibility; that belongs to <see cref="LandmassStageSchemaValidator"/>.
        /// </remarks>
        public static AtlasPipelineValidationPolicy CreatePrimaryContinentPipelineValidationPolicy()
        {
            var routeOperationIds = PrimaryContinentRoute.CreateOperationIds();

            return AtlasPipelineValidationPolicy.Create(
                PrimaryContinentPolicyName,
                AtlasPipelineValidationPolicyFlags.RejectRepeatedStageIdentity |
                AtlasPipelineValidationPolicyFlags.RejectRepeatedOperationIdentityWithinStage |
                AtlasPipelineValidationPolicyFlags.EnforceRequiredStageOrder |
                AtlasPipelineValidationPolicyFlags.EnforceRequiredOperationOrder |
                AtlasPipelineValidationPolicyFlags.EnforceAllowedStages |
                AtlasPipelineValidationPolicyFlags.EnforceAllowedOperations |
                AtlasPipelineValidationPolicyFlags.RequireContentWrite,
                requiredStages: new[] { Id },
                allowedStages: new[] { Id },
                requiredOperations: routeOperationIds,
                allowedOperations: routeOperationIds);
        }

        /// <summary>
        /// Creates the required canonical output field ids for the Landmass stage.
        /// </summary>
        public static StableDataId[] CreateRequiredCanonicalOutputFieldIds()
        {
            return new[]
            {
                AtlasLandmassFieldIds.LandMask,
                AtlasLandmassFieldIds.OceanMask,
                AtlasLandmassFieldIds.LandLabel,
                AtlasLandmassFieldIds.BaseElevation
            };
        }
    }
}
