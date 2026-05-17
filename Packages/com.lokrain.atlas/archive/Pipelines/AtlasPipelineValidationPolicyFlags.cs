// Packages/com.lokrain.atlas/Runtime/Pipelines/AtlasPipelineValidationPolicyFlags.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Pipelines
//
// Purpose
// - Define route/preset policy for Atlas pipeline validation.
// - Keep generic pipeline metadata permissive while allowing concrete presets to reject invalid structures.
// - Express required, allowed, and forbidden stage/operation contracts.
// - Express duplicate and ordering rules without baking them into AtlasPipelineDefinition or AtlasCompiledPlan.
//
// Design notes
// - This is policy metadata, not execution metadata.
// - This type does not allocate workspace memory.
// - This type does not schedule jobs.
// - This type does not resolve Field storage.
// - Generic Atlas pipelines may repeat stages and operations.
// - Concrete route/preset policies may reject repeats deliberately.
// - default(AtlasPipelineValidationPolicy) is valid and open: it imposes no extra route rules.

using System;

namespace Lokrain.Atlas.Pipelines
{
    /// <summary>
    /// Flags controlling route/preset validation for Atlas pipelines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags are policy, not structural validity. The generic pipeline model deliberately
    /// allows repeated stages and repeated operations. A concrete route or preset can use these
    /// flags to restrict that freedom.
    /// </para>
    ///
    /// <para>
    /// Numeric values are part of the diagnostics-facing policy contract. Do not reorder existing
    /// values after release.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasPipelineValidationPolicyFlags : uint
    {
        /// <summary>
        /// Open policy. No route-specific constraints are imposed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Reject repeated stage ids in the pipeline.
        /// </summary>
        RejectRepeatedStageId = 1u << 0,

        /// <summary>
        /// Reject repeated stage durable identities even when versions differ.
        /// </summary>
        RejectRepeatedStageIdentity = 1u << 1,

        /// <summary>
        /// Reject repeated operation ids inside the same stage.
        /// </summary>
        RejectRepeatedOperationIdWithinStage = 1u << 2,

        /// <summary>
        /// Reject repeated operation durable identities inside the same stage even when versions differ.
        /// </summary>
        RejectRepeatedOperationIdentityWithinStage = 1u << 3,

        /// <summary>
        /// Reject repeated operation ids anywhere in the full pipeline.
        /// </summary>
        RejectRepeatedOperationIdAcrossPipeline = 1u << 4,

        /// <summary>
        /// Reject repeated operation durable identities anywhere in the full pipeline even when versions differ.
        /// </summary>
        RejectRepeatedOperationIdentityAcrossPipeline = 1u << 5,

        /// <summary>
        /// Enforce that every required stage appears in the policy-declared order.
        /// </summary>
        EnforceRequiredStageOrder = 1u << 6,

        /// <summary>
        /// Enforce that every required operation appears in the policy-declared order across the flattened pipeline.
        /// </summary>
        EnforceRequiredOperationOrder = 1u << 7,

        /// <summary>
        /// Enforce the allowed stage list when it is non-empty.
        /// </summary>
        EnforceAllowedStages = 1u << 8,

        /// <summary>
        /// Enforce the forbidden stage list when it is non-empty.
        /// </summary>
        EnforceForbiddenStages = 1u << 9,

        /// <summary>
        /// Enforce the allowed operation list when it is non-empty.
        /// </summary>
        EnforceAllowedOperations = 1u << 10,

        /// <summary>
        /// Enforce the forbidden operation list when it is non-empty.
        /// </summary>
        EnforceForbiddenOperations = 1u << 11,

        /// <summary>
        /// Require the pipeline to contain at least one content-reading operation.
        /// </summary>
        RequireContentRead = 1u << 12,

        /// <summary>
        /// Require the pipeline to contain at least one content-writing operation.
        /// </summary>
        RequireContentWrite = 1u << 13
    }
}