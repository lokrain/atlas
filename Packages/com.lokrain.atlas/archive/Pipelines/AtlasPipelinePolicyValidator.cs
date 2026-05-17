// Packages/com.lokrain.atlas/Runtime/Pipelines/AtlasPipelinePolicyValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Pipelines
//
// Purpose
// - Validate authored Atlas pipeline definitions against route/preset policy.
// - Keep AtlasPipelineDefinition permissive while allowing concrete presets to reject invalid structures.
// - Validate required, allowed, forbidden, duplicate, ordering, and read/write capability policy.
// - Report deterministic diagnostics for editor, CI, tests, and compiler tooling.
//
// Design notes
// - This validator is metadata-only.
// - This validator does not compile pipelines.
// - This validator does not resolve Fields or workspace memory.
// - This validator does not schedule jobs.
// - This validator does not validate dataflow or write hazards.
// - Generic pipelines may repeat stages and operations.
// - Concrete policies decide whether repeats are legal.

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Pipelines
{
    /// <summary>
    /// Validates authored pipeline definitions against explicit route/preset policy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasPipelinePolicyValidator"/> is intentionally separate from
    /// <see cref="AtlasPipelineDefinition"/>. The definition type represents ordered authored
    /// metadata. This validator decides whether that metadata is legal for a specific route,
    /// preset, product profile, or test harness.
    /// </para>
    ///
    /// <para>
    /// This type validates authored stage and operation structure before compilation. Field
    /// resolution, binding validation, dataflow validation, write-hazard validation, shape
    /// resolution, workspace allocation, and execution planning belong to later layers.
    /// </para>
    /// </remarks>
    public static class AtlasPipelinePolicyValidator
    {
        private static readonly AtlasDiagnosticCode NullPipelineCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 300);

        private static readonly AtlasDiagnosticCode NullPolicyCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 301);

        private static readonly AtlasDiagnosticCode InvalidPipelineIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 302);

        private static readonly AtlasDiagnosticCode EmptyPipelineDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 303);

        private static readonly AtlasDiagnosticCode EmptyPipelineCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 304);

        private static readonly AtlasDiagnosticCode NullStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 305);

        private static readonly AtlasDiagnosticCode InvalidStageIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 306);

        private static readonly AtlasDiagnosticCode EmptyStageDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 307);

        private static readonly AtlasDiagnosticCode EmptyStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 308);

        private static readonly AtlasDiagnosticCode StageNotAllowedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 309);

        private static readonly AtlasDiagnosticCode StageForbiddenCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 310);

        private static readonly AtlasDiagnosticCode MissingRequiredStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 311);

        private static readonly AtlasDiagnosticCode RequiredStageOrderViolationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 312);

        private static readonly AtlasDiagnosticCode RepeatedStageIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 313);

        private static readonly AtlasDiagnosticCode RepeatedStageIdentityCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 314);

        private static readonly AtlasDiagnosticCode NullOperationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 315);

        private static readonly AtlasDiagnosticCode InvalidOperationIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 316);

        private static readonly AtlasDiagnosticCode EmptyOperationDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 317);

        private static readonly AtlasDiagnosticCode EmptyOperationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 318);

        private static readonly AtlasDiagnosticCode OperationNotAllowedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 319);

        private static readonly AtlasDiagnosticCode OperationForbiddenCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 320);

        private static readonly AtlasDiagnosticCode MissingRequiredOperationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 321);

        private static readonly AtlasDiagnosticCode RequiredOperationOrderViolationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 322);

        private static readonly AtlasDiagnosticCode RepeatedOperationIdWithinStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 323);

        private static readonly AtlasDiagnosticCode RepeatedOperationIdentityWithinStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 324);

        private static readonly AtlasDiagnosticCode RepeatedOperationIdAcrossPipelineCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 325);

        private static readonly AtlasDiagnosticCode RepeatedOperationIdentityAcrossPipelineCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 326);

        private static readonly AtlasDiagnosticCode MissingContentReadCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 327);

        private static readonly AtlasDiagnosticCode MissingContentWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 328);

        /// <summary>
        /// Validates an authored pipeline definition against the open policy.
        /// </summary>
        /// <param name="pipeline">Pipeline definition to validate.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(AtlasPipelineDefinition pipeline)
        {
            return Validate(
                pipeline,
                AtlasPipelineValidationPolicy.Open);
        }

        /// <summary>
        /// Validates an authored pipeline definition against an explicit policy.
        /// </summary>
        /// <param name="pipeline">Pipeline definition to validate.</param>
        /// <param name="policy">Route/preset validation policy.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            Validate(
                pipeline,
                policy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Validates an authored pipeline definition into an existing diagnostic buffer.
        /// </summary>
        /// <param name="pipeline">Pipeline definition to validate.</param>
        /// <param name="policy">Route/preset validation policy.</param>
        /// <param name="diagnostics">Diagnostic buffer to append to.</param>
        /// <returns>The supplied diagnostic buffer.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        public static AtlasDiagnosticBuffer Validate(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (!ValidateHeader(
                    pipeline,
                    policy,
                    diagnostics))
            {
                return diagnostics;
            }

            ValidatePipelineContentRequirements(
                pipeline,
                policy,
                diagnostics);

            ValidateStages(
                pipeline,
                policy,
                diagnostics);

            ValidateStageDuplicates(
                pipeline,
                policy,
                diagnostics);

            ValidateRequiredStages(
                pipeline,
                policy,
                diagnostics);

            ValidateOperations(
                pipeline,
                policy,
                diagnostics);

            ValidateOperationDuplicatesAcrossPipeline(
                pipeline,
                policy,
                diagnostics);

            ValidateRequiredOperations(
                pipeline,
                policy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Throws when an authored pipeline definition violates the open policy.
        /// </summary>
        /// <param name="pipeline">Pipeline definition to validate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(AtlasPipelineDefinition pipeline)
        {
            ValidateOrThrow(
                pipeline,
                AtlasPipelineValidationPolicy.Open);
        }

        /// <summary>
        /// Throws when an authored pipeline definition violates an explicit policy.
        /// </summary>
        /// <param name="pipeline">Pipeline definition to validate.</param>
        /// <param name="policy">Route/preset validation policy.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy)
        {
            var diagnostics = Validate(
                pipeline,
                policy);

            if (!diagnostics.HasFailures)
            {
                return;
            }

            throw new InvalidOperationException(
                diagnostics.ToReportString());
        }

        private static bool ValidateHeader(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy == null)
            {
                diagnostics.AddFatal(
                    NullPolicyCode,
                    AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPipelinePolicyValidator")),
                    AtlasDiagnosticText.Message("Atlas pipeline policy validation requires a non-null policy."));

                return false;
            }

            if (pipeline == null)
            {
                diagnostics.AddFatal(
                    NullPipelineCode,
                    AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPipelinePolicyValidator")),
                    AtlasDiagnosticText.Message("Atlas pipeline policy validation requires a non-null pipeline definition."));

                return false;
            }

            var location = CreatePipelineLocation(pipeline);


            if (pipeline.DebugName.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyPipelineDebugNameCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas pipeline policy validation requires a non-empty pipeline debug name."));
            }

            if (pipeline.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyPipelineCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas pipeline policy validation requires at least one stage occurrence."));

                return false;
            }

            return true;
        }

        private static void ValidatePipelineContentRequirements(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            var location = CreatePipelineLocation(pipeline);

            if (policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RequireContentRead) &&
                !pipeline.ReadsContent)
            {
                diagnostics.AddError(
                    MissingContentReadCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' violates policy '{AtlasDiagnosticText.Name(policy.Name)}': at least one content-reading operation is required."));
            }

            if (policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RequireContentWrite) &&
                !pipeline.WritesContent)
            {
                diagnostics.AddError(
                    MissingContentWriteCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' violates policy '{AtlasDiagnosticText.Name(policy.Name)}': at least one content-writing operation is required."));
            }
        }

        private static void ValidateStages(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            for (var stageIndex = 0; stageIndex < pipeline.Count; stageIndex++)
            {
                var stage = pipeline[stageIndex];

                if (stage == null)
                {
                    diagnostics.AddError(
                        NullStageCode,
                        AtlasDiagnosticLocation.Create(
                            AtlasDiagnosticLocationKind.Stage,
                            default,
                            stageIndex,
                            AtlasDiagnosticLocation.NoIndex,
                            AtlasDiagnosticLocation.NoIndex,
                            AtlasDiagnosticText.Name64("null-stage")),
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains a null stage at index '{stageIndex}'."));

                    continue;
                }

                var location = CreateStageLocation(
                    stage,
                    stageIndex);


                if (stage.DebugName.IsEmpty)
                {
                    diagnostics.AddError(
                        EmptyStageDebugNameCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains a stage with an empty debug name at index '{stageIndex}'."));
                }

                if (stage.IsEmpty)
                {
                    diagnostics.AddError(
                        EmptyStageCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains empty stage '{AtlasDiagnosticText.Name(stage.DebugName)}' at index '{stageIndex}'."));
                }

                if (!policy.AllowsStage(stage.StageId))
                {
                    diagnostics.AddError(
                        StageNotAllowedCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains stage '{AtlasDiagnosticText.Name(stage.DebugName)}' which is not allowed by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                }

                if (policy.ForbidsStage(stage.StageId))
                {
                    diagnostics.AddError(
                        StageForbiddenCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains forbidden stage '{AtlasDiagnosticText.Name(stage.DebugName)}' under policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                }
            }
        }

        private static void ValidateStageDuplicates(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            var rejectRepeatedId = policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RejectRepeatedStageId);
            var rejectRepeatedIdentity = policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RejectRepeatedStageIdentity);

            if (!rejectRepeatedId && !rejectRepeatedIdentity)
            {
                return;
            }

            for (var i = 0; i < pipeline.Count; i++)
            {
                var left = pipeline[i];

                if (left == null)
                {
                    continue;
                }

                for (var j = i + 1; j < pipeline.Count; j++)
                {
                    var right = pipeline[j];

                    if (right == null)
                    {
                        continue;
                    }

                    if (rejectRepeatedId && left.StageId == right.StageId)
                    {
                        diagnostics.AddError(
                            RepeatedStageIdCode,
                            CreateStageLocation(right, j),
                            AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' repeats stage id '{right.StageId}' at indices '{i}' and '{j}', which is rejected by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));

                        continue;
                    }

                    if (rejectRepeatedIdentity &&
                        left.StageId != right.StageId &&
                        left.StageId.HasSameIdentityAs(right.StageId))
                    {
                        diagnostics.AddError(
                            RepeatedStageIdentityCode,
                            CreateStageLocation(right, j),
                            AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' repeats stage identity at indices '{i}' and '{j}' using versions '{left.StageId.Version}' and '{right.StageId.Version}', which is rejected by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                    }
                }
            }
        }

        private static void ValidateRequiredStages(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.RequiredStageCount == 0)
            {
                return;
            }

            if (policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.EnforceRequiredStageOrder))
            {
                ValidateRequiredStageOrder(
                    pipeline,
                    policy,
                    diagnostics);

                return;
            }

            for (var i = 0; i < policy.RequiredStageCount; i++)
            {
                var requiredStage = policy.GetRequiredStage(i);

                if (IndexOfStage(pipeline, requiredStage, 0) >= 0)
                {
                    continue;
                }

                diagnostics.AddError(
                    MissingRequiredStageCode,
                    CreatePipelineLocation(pipeline),
                    AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' is missing required stage '{requiredStage}' from policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
            }
        }

        private static void ValidateRequiredStageOrder(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            var searchStart = 0;

            for (var i = 0; i < policy.RequiredStageCount; i++)
            {
                var requiredStage = policy.GetRequiredStage(i);
                var foundIndex = IndexOfStage(
                    pipeline,
                    requiredStage,
                    searchStart);

                if (foundIndex < 0)
                {
                    var code = IndexOfStage(pipeline, requiredStage, 0) >= 0
                        ? RequiredStageOrderViolationCode
                        : MissingRequiredStageCode;

                    diagnostics.AddError(
                        code,
                        CreatePipelineLocation(pipeline),
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' does not contain required stage '{requiredStage}' at or after required-order position '{i}' under policy '{AtlasDiagnosticText.Name(policy.Name)}'."));

                    continue;
                }

                searchStart = foundIndex + 1;
            }
        }

        private static void ValidateOperations(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            for (var stageIndex = 0; stageIndex < pipeline.Count; stageIndex++)
            {
                var stage = pipeline[stageIndex];

                if (stage == null)
                {
                    continue;
                }

                ValidateOperationsWithinStage(
                    pipeline,
                    stage,
                    stageIndex,
                    policy,
                    diagnostics);
            }
        }

        private static void ValidateOperationsWithinStage(
            AtlasPipelineDefinition pipeline,
            AtlasStageDefinition stage,
            int stageIndex,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
            {
                var operation = stage[operationIndex];

                if (operation == null)
                {
                    diagnostics.AddError(
                        NullOperationCode,
                        AtlasDiagnosticLocation.Create(
                            AtlasDiagnosticLocationKind.Operation,
                            default,
                            stageIndex,
                            operationIndex,
                            AtlasDiagnosticLocation.NoIndex,
                            AtlasDiagnosticText.Name64("null-operation")),
                        AtlasDiagnosticText.Message($"Atlas stage '{AtlasDiagnosticText.Name(stage.DebugName)}' contains a null operation at index '{operationIndex}'."));

                    continue;
                }

                var location = CreateOperationLocation(
                    operation,
                    stageIndex,
                    operationIndex);


                if (operation.DebugName.IsEmpty)
                {
                    diagnostics.AddError(
                        EmptyOperationDebugNameCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas stage '{AtlasDiagnosticText.Name(stage.DebugName)}' contains an operation with an empty debug name at index '{operationIndex}'."));
                }

                if (operation.IsEmpty)
                {
                    diagnostics.AddError(
                        EmptyOperationCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas stage '{AtlasDiagnosticText.Name(stage.DebugName)}' contains empty operation '{AtlasDiagnosticText.Name(operation.DebugName)}' at index '{operationIndex}'."));
                }

                if (!policy.AllowsOperation(operation.OperationId))
                {
                    diagnostics.AddError(
                        OperationNotAllowedCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains operation '{AtlasDiagnosticText.Name(operation.DebugName)}' which is not allowed by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                }

                if (policy.ForbidsOperation(operation.OperationId))
                {
                    diagnostics.AddError(
                        OperationForbiddenCode,
                        location,
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' contains forbidden operation '{AtlasDiagnosticText.Name(operation.DebugName)}' under policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                }
            }

            ValidateOperationDuplicatesWithinStage(
                stage,
                stageIndex,
                policy,
                diagnostics);
        }

        private static void ValidateOperationDuplicatesWithinStage(
            AtlasStageDefinition stage,
            int stageIndex,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            var rejectRepeatedId = policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RejectRepeatedOperationIdWithinStage);
            var rejectRepeatedIdentity = policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RejectRepeatedOperationIdentityWithinStage);

            if (!rejectRepeatedId && !rejectRepeatedIdentity)
            {
                return;
            }

            for (var i = 0; i < stage.Count; i++)
            {
                var left = stage[i];

                if (left == null)
                {
                    continue;
                }

                for (var j = i + 1; j < stage.Count; j++)
                {
                    var right = stage[j];

                    if (right == null)
                    {
                        continue;
                    }

                    if (rejectRepeatedId && left.OperationId == right.OperationId)
                    {
                        diagnostics.AddError(
                            RepeatedOperationIdWithinStageCode,
                            CreateOperationLocation(right, stageIndex, j),
                            AtlasDiagnosticText.Message($"Atlas stage '{AtlasDiagnosticText.Name(stage.DebugName)}' repeats operation id '{right.OperationId}' at indices '{i}' and '{j}', which is rejected by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));

                        continue;
                    }

                    if (rejectRepeatedIdentity &&
                        left.OperationId != right.OperationId &&
                        left.OperationId.HasSameIdentityAs(right.OperationId))
                    {
                        diagnostics.AddError(
                            RepeatedOperationIdentityWithinStageCode,
                            CreateOperationLocation(right, stageIndex, j),
                            AtlasDiagnosticText.Message($"Atlas stage '{AtlasDiagnosticText.Name(stage.DebugName)}' repeats operation identity at indices '{i}' and '{j}' using versions '{left.OperationId.Version}' and '{right.OperationId.Version}', which is rejected by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                    }
                }
            }
        }

        private static void ValidateOperationDuplicatesAcrossPipeline(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            var rejectRepeatedId = policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RejectRepeatedOperationIdAcrossPipeline);
            var rejectRepeatedIdentity = policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.RejectRepeatedOperationIdentityAcrossPipeline);

            if (!rejectRepeatedId && !rejectRepeatedIdentity)
            {
                return;
            }

            var occurrences = FlattenOperations(pipeline);

            for (var i = 0; i < occurrences.Count; i++)
            {
                var left = occurrences[i];

                if (left.Operation == null)
                {
                    continue;
                }

                for (var j = i + 1; j < occurrences.Count; j++)
                {
                    var right = occurrences[j];

                    if (right.Operation == null)
                    {
                        continue;
                    }

                    if (rejectRepeatedId &&
                        left.Operation.OperationId == right.Operation.OperationId)
                    {
                        diagnostics.AddError(
                            RepeatedOperationIdAcrossPipelineCode,
                            CreateOperationLocation(
                                right.Operation,
                                right.StageIndex,
                                right.OperationIndex),
                            AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' repeats operation id '{right.Operation.OperationId}' at flattened operation occurrences '{i}' and '{j}', which is rejected by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));

                        continue;
                    }

                    if (rejectRepeatedIdentity &&
                        left.Operation.OperationId != right.Operation.OperationId &&
                        left.Operation.OperationId.HasSameIdentityAs(right.Operation.OperationId))
                    {
                        diagnostics.AddError(
                            RepeatedOperationIdentityAcrossPipelineCode,
                            CreateOperationLocation(
                                right.Operation,
                                right.StageIndex,
                                right.OperationIndex),
                            AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' repeats operation identity at flattened operation occurrences '{i}' and '{j}' using versions '{left.Operation.OperationId.Version}' and '{right.Operation.OperationId.Version}', which is rejected by policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
                    }
                }
            }
        }

        private static void ValidateRequiredOperations(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.RequiredOperationCount == 0)
            {
                return;
            }

            var occurrences = FlattenOperations(pipeline);

            if (policy.Flags.HasAny(AtlasPipelineValidationPolicyFlags.EnforceRequiredOperationOrder))
            {
                ValidateRequiredOperationOrder(
                    pipeline,
                    policy,
                    occurrences,
                    diagnostics);

                return;
            }

            for (var i = 0; i < policy.RequiredOperationCount; i++)
            {
                var requiredOperation = policy.GetRequiredOperation(i);

                if (IndexOfOperation(occurrences, requiredOperation, 0) >= 0)
                {
                    continue;
                }

                diagnostics.AddError(
                    MissingRequiredOperationCode,
                    CreatePipelineLocation(pipeline),
                    AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' is missing required operation '{requiredOperation}' from policy '{AtlasDiagnosticText.Name(policy.Name)}'."));
            }
        }

        private static void ValidateRequiredOperationOrder(
            AtlasPipelineDefinition pipeline,
            AtlasPipelineValidationPolicy policy,
            List<OperationOccurrence> occurrences,
            AtlasDiagnosticBuffer diagnostics)
        {
            var searchStart = 0;

            for (var i = 0; i < policy.RequiredOperationCount; i++)
            {
                var requiredOperation = policy.GetRequiredOperation(i);
                var foundIndex = IndexOfOperation(
                    occurrences,
                    requiredOperation,
                    searchStart);

                if (foundIndex < 0)
                {
                    var code = IndexOfOperation(occurrences, requiredOperation, 0) >= 0
                        ? RequiredOperationOrderViolationCode
                        : MissingRequiredOperationCode;

                    diagnostics.AddError(
                        code,
                        CreatePipelineLocation(pipeline),
                        AtlasDiagnosticText.Message($"Atlas pipeline '{AtlasDiagnosticText.Name(pipeline.DebugName)}' does not contain required operation '{requiredOperation}' at or after required-order position '{i}' under policy '{AtlasDiagnosticText.Name(policy.Name)}'."));

                    continue;
                }

                searchStart = foundIndex + 1;
            }
        }

        private static int IndexOfStage(
            AtlasPipelineDefinition pipeline,
            AtlasStageId stageId,
            int startIndex)
        {
            for (var i = startIndex; i < pipeline.Count; i++)
            {
                var stage = pipeline[i];

                if (stage != null &&
                    stage.StageId == stageId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOfOperation(
            List<OperationOccurrence> occurrences,
            AtlasOperationId operationId,
            int startIndex)
        {
            for (var i = startIndex; i < occurrences.Count; i++)
            {
                var operation = occurrences[i].Operation;

                if (operation != null &&
                    operation.OperationId == operationId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<OperationOccurrence> FlattenOperations(AtlasPipelineDefinition pipeline)
        {
            var occurrences = new List<OperationOccurrence>(pipeline.OperationCount);

            for (var stageIndex = 0; stageIndex < pipeline.Count; stageIndex++)
            {
                var stage = pipeline[stageIndex];

                if (stage == null)
                {
                    continue;
                }

                for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
                {
                    occurrences.Add(new OperationOccurrence(
                        stageIndex,
                        operationIndex,
                        stage[operationIndex]));
                }
            }

            return occurrences;
        }

        private static AtlasDiagnosticLocation CreatePipelineLocation(AtlasPipelineDefinition pipeline)
        {
            if (pipeline == null)
            {
                return AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPipelinePolicyValidator"));
            }

            var stableId = pipeline.PipelineId.ToStableDataId();

            return AtlasDiagnosticLocation.Pipeline(
                stableId,
                pipeline.DebugName);
        }

        private static AtlasDiagnosticLocation CreateStageLocation(
            AtlasStageDefinition stage,
            int stageIndex)
        {
            if (stage == null)
            {
                return AtlasDiagnosticLocation.Create(
                    AtlasDiagnosticLocationKind.Stage,
                    default,
                    stageIndex,
                    AtlasDiagnosticLocation.NoIndex,
                    AtlasDiagnosticLocation.NoIndex,
                    AtlasDiagnosticText.Name64("null-stage"));
            }

            var stableId = stage.StageId.ToStableDataId();

            return AtlasDiagnosticLocation.Create(
                AtlasDiagnosticLocationKind.Stage,
                stableId,
                stageIndex,
                AtlasDiagnosticLocation.NoIndex,
                AtlasDiagnosticLocation.NoIndex,
                stage.DebugName);
        }

        private static AtlasDiagnosticLocation CreateOperationLocation(
            AtlasOperationDefinition operation,
            int stageIndex,
            int operationIndex)
        {
            if (operation == null)
            {
                return AtlasDiagnosticLocation.Create(
                    AtlasDiagnosticLocationKind.Operation,
                    default,
                    stageIndex,
                    operationIndex,
                    AtlasDiagnosticLocation.NoIndex,
                    AtlasDiagnosticText.Name64("null-operation"));
            }

            var stableId = operation.OperationId.ToStableDataId();

            return AtlasDiagnosticLocation.Create(
                AtlasDiagnosticLocationKind.Operation,
                stableId,
                stageIndex,
                operationIndex,
                AtlasDiagnosticLocation.NoIndex,
                operation.DebugName);
        }


        private readonly struct OperationOccurrence
        {
            public readonly int StageIndex;
            public readonly int OperationIndex;
            public readonly AtlasOperationDefinition Operation;

            public OperationOccurrence(
                int stageIndex,
                int operationIndex,
                AtlasOperationDefinition operation)
            {
                StageIndex = stageIndex;
                OperationIndex = operationIndex;
                Operation = operation;
            }
        }
    }
}