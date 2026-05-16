// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasPlanValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate compiled Atlas pipeline metadata before executable planning.
// - Report structural compiled-plan failures through deterministic diagnostics.
// - Prove that present bindings still match the plan Contract table.
// - Preserve a throwing wrapper for hard contract/programmer flows.
//
// Design notes
// - This validator is metadata-only.
// - It does not allocate native memory.
// - It does not schedule jobs.
// - It does not reject repeated stages or repeated operations.
// - It does not enforce route-specific rules such as mandatory stages or unique macro phases.
// - It does not validate read-before-write or write hazards.
// - Dataflow, route policy, and memory-hazard validation should be separate validators.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Validates compiled Atlas plan metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasPlanValidator"/> validates structural integrity and binding-to-table
    /// consistency for an <see cref="AtlasCompiledPlan"/>.
    /// </para>
    ///
    /// <para>
    /// This type intentionally does not enforce route policy. A continental route, island route,
    /// debug route, or test route may choose different stage repetition, mandatory-stage, and
    /// dataflow-readiness rules.
    /// </para>
    /// </remarks>
    public static class AtlasPlanValidator
    {
        private static readonly AtlasDiagnosticCode NullPlanCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 1);

        private static readonly AtlasDiagnosticCode InvalidPipelineIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 2);

        private static readonly AtlasDiagnosticCode EmptyPipelineDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 3);

        private static readonly AtlasDiagnosticCode NullContractTableCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 4);

        private static readonly AtlasDiagnosticCode EmptyContractTableCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 5);

        private static readonly AtlasDiagnosticCode EmptyPlanCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 6);

        private static readonly AtlasDiagnosticCode StageCountMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 7);

        private static readonly AtlasDiagnosticCode NullStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 8);

        private static readonly AtlasDiagnosticCode StageIndexMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 9);

        private static readonly AtlasDiagnosticCode InvalidStageIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 10);

        private static readonly AtlasDiagnosticCode EmptyStageDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 11);

        private static readonly AtlasDiagnosticCode EmptyStageCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 12);

        private static readonly AtlasDiagnosticCode NullOperationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 13);

        private static readonly AtlasDiagnosticCode OperationIndexMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 14);

        private static readonly AtlasDiagnosticCode InvalidOperationIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 15);

        private static readonly AtlasDiagnosticCode EmptyOperationDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 16);

        private static readonly AtlasDiagnosticCode EmptyOperationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 17);

        private static readonly AtlasDiagnosticCode InvalidBindingCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 18);

        private static readonly AtlasDiagnosticCode BindingIndexMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 19);

        private static readonly AtlasDiagnosticCode MissingOptionalCarriesContractCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 20);

        private static readonly AtlasDiagnosticCode MissingOptionalRequiresMemoryCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 21);

        private static readonly AtlasDiagnosticCode MissingOptionalButContractPresentCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 22);

        private static readonly AtlasDiagnosticCode PresentBindingContractNotTableReadyCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 23);

        private static readonly AtlasDiagnosticCode PresentBindingFieldIdMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 24);

        private static readonly AtlasDiagnosticCode PresentBindingContractMissingCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 25);

        private static readonly AtlasDiagnosticCode PresentBindingContractMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 26);

        private static readonly AtlasDiagnosticCode PresentBindingSlotMismatchCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 27);

        private static readonly AtlasDiagnosticCode ShapeOnlyRequiresMemoryCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 28);

        /// <summary>
        /// Compiles a pipeline and validates the resulting compiled plan.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <returns>A structurally valid compiled plan.</returns>
        public static AtlasCompiledPlan CompileAndValidate(
            Pipelines.AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            var plan = AtlasPlanCompiler.Compile(
                pipeline,
                contracts);

            ValidateOrThrow(plan);
            return plan;
        }

        /// <summary>
        /// Attempts to compile and validate a pipeline.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <returns>
        /// A successful result with a validated compiled plan, or a failed result with diagnostics.
        /// </returns>
        public static AtlasCompilationResult TryCompileAndValidate(
            Pipelines.AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            var compilationResult = AtlasPlanCompiler.TryCompile(
                pipeline,
                contracts);

            if (compilationResult.Failed)
            {
                return compilationResult;
            }

            var diagnostics = AtlasDiagnosticBuffer.Create();

            diagnostics.AddRange(compilationResult);
            Validate(
                compilationResult.Plan,
                diagnostics);

            return diagnostics.HasFailures
                ? AtlasCompilationResult.Failure(diagnostics)
                : AtlasCompilationResult.Success(compilationResult.Plan, diagnostics);
        }

        /// <summary>
        /// Validates a compiled plan and returns diagnostics.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <returns>Validation diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(AtlasCompiledPlan plan)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            Validate(
                plan,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Validates a compiled plan into an existing diagnostic buffer.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="diagnostics">Diagnostic buffer to append to.</param>
        /// <returns>The supplied diagnostic buffer.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        public static AtlasDiagnosticBuffer Validate(
            AtlasCompiledPlan plan,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (!ValidatePlanHeader(
                    plan,
                    diagnostics))
            {
                return diagnostics;
            }

            ValidateStages(
                plan,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Throws when a compiled plan is not structurally valid.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(AtlasCompiledPlan plan)
        {
            var diagnostics = Validate(plan);

            if (!diagnostics.HasFailures)
            {
                return;
            }

            throw new InvalidOperationException(
                diagnostics.ToReportString());
        }

        private static bool ValidatePlanHeader(
            AtlasCompiledPlan plan,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (plan == null)
            {
                diagnostics.AddFatal(
                    NullPlanCode,
                    AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPlanValidator")),
                    AtlasDiagnosticText.Message("Atlas compiled plan validation requires a non-null compiled plan."));

                return false;
            }

            var location = CreatePlanLocation(plan);


            if (plan.DebugName.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyPipelineDebugNameCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas compiled plan has an empty pipeline debug name."));
            }

            if (plan.Contracts == null)
            {
                diagnostics.AddFatal(
                    NullContractTableCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas compiled plan has no Contract table."));

                return false;
            }

            if (plan.Contracts.Count <= 0)
            {
                diagnostics.AddError(
                    EmptyContractTableCode,
                    AtlasDiagnosticLocation.Contract(default, plan.Contracts.Name),
                    AtlasDiagnosticText.Message("Atlas compiled plan uses an empty Contract table."));
            }

            if (plan.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyPlanCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas compiled plan contains no compiled stage occurrences."));

                return false;
            }

            if (plan.StageCount != plan.Count)
            {
                diagnostics.AddError(
                    StageCountMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled plan reports StageCount '{plan.StageCount}', but Count is '{plan.Count}'."));
            }

            return true;
        }

        private static void ValidateStages(
            AtlasCompiledPlan plan,
            AtlasDiagnosticBuffer diagnostics)
        {
            for (var stageIndex = 0; stageIndex < plan.Count; stageIndex++)
            {
                ValidateStage(
                    plan,
                    plan[stageIndex],
                    stageIndex,
                    diagnostics);
            }
        }

        private static void ValidateStage(
            AtlasCompiledPlan plan,
            AtlasCompiledStage stage,
            int expectedStageIndex,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (stage == null)
            {
                diagnostics.AddError(
                    NullStageCode,
                    AtlasDiagnosticLocation.CompiledStage(
                        default,
                        expectedStageIndex,
                        AtlasDiagnosticText.Name64("null-stage")),
                    AtlasDiagnosticText.Message($"Atlas compiled plan contains a null stage at index '{expectedStageIndex}'."));

                return;
            }

            var location = CreateStageLocation(
                stage,
                expectedStageIndex);

            if (stage.StageIndex != expectedStageIndex)
            {
                diagnostics.AddError(
                    StageIndexMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled stage '{AtlasDiagnosticText.Name(stage.DebugName)}' has StageIndex '{stage.StageIndex}', but appears at index '{expectedStageIndex}'."));
            }


            if (stage.DebugName.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyStageDebugNameCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled stage at index '{expectedStageIndex}' has an empty debug name."));
            }

            if (stage.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyStageCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled stage '{AtlasDiagnosticText.Name(stage.DebugName)}' contains no compiled operation occurrences."));

                return;
            }

            for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
            {
                ValidateOperation(
                    plan,
                    stage,
                    stage[operationIndex],
                    expectedStageIndex,
                    operationIndex,
                    diagnostics);
            }
        }

        private static void ValidateOperation(
            AtlasCompiledPlan plan,
            AtlasCompiledStage stage,
            AtlasCompiledOperation operation,
            int stageIndex,
            int expectedOperationIndex,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (operation == null)
            {
                diagnostics.AddError(
                    NullOperationCode,
                    AtlasDiagnosticLocation.CompiledOperation(
                        default,
                        stageIndex,
                        expectedOperationIndex,
                        AtlasDiagnosticText.Name64("null-operation")),
                    AtlasDiagnosticText.Message($"Atlas compiled stage '{AtlasDiagnosticText.Name(stage.DebugName)}' contains a null operation at index '{expectedOperationIndex}'."));

                return;
            }

            var location = CreateOperationLocation(
                operation,
                stageIndex,
                expectedOperationIndex);

            if (operation.OperationIndex != expectedOperationIndex)
            {
                diagnostics.AddError(
                    OperationIndexMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled operation '{AtlasDiagnosticText.Name(operation.DebugName)}' has OperationIndex '{operation.OperationIndex}', but appears at index '{expectedOperationIndex}'."));
            }


            if (operation.DebugName.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyOperationDebugNameCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled operation at stage '{stageIndex}', index '{expectedOperationIndex}', has an empty debug name."));
            }

            if (operation.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyOperationCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled operation '{AtlasDiagnosticText.Name(operation.DebugName)}' contains no compiled bindings."));

                return;
            }

            for (var bindingIndex = 0; bindingIndex < operation.Count; bindingIndex++)
            {
                ValidateBinding(
                    plan,
                    operation,
                    operation[bindingIndex],
                    stageIndex,
                    expectedOperationIndex,
                    bindingIndex,
                    diagnostics);
            }
        }

        private static void ValidateBinding(
            AtlasCompiledPlan plan,
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            int stageIndex,
            int operationIndex,
            int expectedBindingIndex,
            AtlasDiagnosticBuffer diagnostics)
        {
            var location = CreateBindingLocation(
                binding,
                stageIndex,
                operationIndex,
                expectedBindingIndex);

            if (!binding.IsValid)
            {
                diagnostics.AddError(
                    InvalidBindingCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled binding at stage '{stageIndex}', operation '{operationIndex}', binding '{expectedBindingIndex}' is invalid."));

                return;
            }

            if (binding.BindingIndex != expectedBindingIndex)
            {
                diagnostics.AddError(
                    BindingIndexMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas compiled binding '{AtlasDiagnosticText.Name(binding.BindingName)}' has BindingIndex '{binding.BindingIndex}', but appears at index '{expectedBindingIndex}'."));
            }

            if (binding.IsMissingOptional)
            {
                ValidateMissingOptionalBinding(
                    plan,
                    operation,
                    binding,
                    location,
                    diagnostics);

                return;
            }

            ValidatePresentBinding(
                plan,
                operation,
                binding,
                location,
                diagnostics);
        }

        private static void ValidateMissingOptionalBinding(
            AtlasCompiledPlan plan,
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (binding.Contract.IsValid)
            {
                diagnostics.AddError(
                    MissingOptionalCarriesContractCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas missing optional binding '{AtlasDiagnosticText.Name(binding.BindingName)}' in operation '{AtlasDiagnosticText.Name(operation.DebugName)}' carries a valid Contract."));
            }

            if (binding.RequiresContentMemory)
            {
                diagnostics.AddError(
                    MissingOptionalRequiresMemoryCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas missing optional binding '{AtlasDiagnosticText.Name(binding.BindingName)}' in operation '{AtlasDiagnosticText.Name(operation.DebugName)}' incorrectly requires content memory."));
            }

            if (plan.Contracts.TryGetContract(binding.FieldId, out var resolvedContract))
            {
                diagnostics.AddError(
                    MissingOptionalButContractPresentCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas binding '{AtlasDiagnosticText.Name(binding.BindingName)}' is marked missing optional, but Contract '{AtlasDiagnosticText.Name(resolvedContract.DebugName)}' is present in table '{AtlasDiagnosticText.Name(plan.Contracts.Name)}'."));
            }
        }

        private static void ValidatePresentBinding(
            AtlasCompiledPlan plan,
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (!binding.Contract.IsTableReady)
            {
                diagnostics.AddError(
                    PresentBindingContractNotTableReadyCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' in operation '{AtlasDiagnosticText.Name(operation.DebugName)}' has a Contract that is not table-ready."));
            }

            if (binding.Contract.StableId != binding.FieldId)
            {
                diagnostics.AddError(
                    PresentBindingFieldIdMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' has access Field id '{binding.FieldId}' but Contract Field id '{binding.Contract.StableId}'."));
            }

            if (!plan.Contracts.TryGetContract(binding.FieldId, out var resolvedContract))
            {
                diagnostics.AddError(
                    PresentBindingContractMissingCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' references Field id '{binding.FieldId}', but Contract table '{AtlasDiagnosticText.Name(plan.Contracts.Name)}' does not contain that Field."));

                return;
            }

            if (resolvedContract != binding.Contract)
            {
                diagnostics.AddError(
                    PresentBindingContractMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' does not match the Contract currently resolved from table '{AtlasDiagnosticText.Name(plan.Contracts.Name)}'."));
            }

            if (binding.Slot != binding.Contract.Slot)
            {
                diagnostics.AddError(
                    PresentBindingSlotMismatchCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' reports slot '{binding.Slot}', but its Contract reports slot '{binding.Contract.Slot}'."));
            }

            if (binding.RequiresContentMemory && binding.IsShapeOnly)
            {
                diagnostics.AddError(
                    ShapeOnlyRequiresMemoryCode,
                    location,
                    AtlasDiagnosticText.Message($"Atlas present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' is shape-only but incorrectly requires content memory."));
            }
        }

        private static AtlasDiagnosticLocation CreatePlanLocation(AtlasCompiledPlan plan)
        {
            if (plan == null)
            {
                return AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPlanValidator"));
            }

            var stableId = plan.PipelineId.ToStableDataId();

            return AtlasDiagnosticLocation.CompiledPlan(
                stableId,
                plan.DebugName);
        }

        private static AtlasDiagnosticLocation CreateStageLocation(
            AtlasCompiledStage stage,
            int stageIndex)
        {
            if (stage == null)
            {
                return AtlasDiagnosticLocation.CompiledStage(
                    default,
                    stageIndex,
                    AtlasDiagnosticText.Name64("null-stage"));
            }

            var stableId = stage.StageId.ToStableDataId();

            return AtlasDiagnosticLocation.CompiledStage(
                stableId,
                stageIndex,
                stage.DebugName);
        }

        private static AtlasDiagnosticLocation CreateOperationLocation(
            AtlasCompiledOperation operation,
            int stageIndex,
            int operationIndex)
        {
            if (operation == null)
            {
                return AtlasDiagnosticLocation.CompiledOperation(
                    default,
                    stageIndex,
                    operationIndex,
                    AtlasDiagnosticText.Name64("null-operation"));
            }

            var stableId = operation.OperationId.ToStableDataId();

            return AtlasDiagnosticLocation.CompiledOperation(
                stableId,
                stageIndex,
                operationIndex,
                operation.DebugName);
        }

        private static AtlasDiagnosticLocation CreateBindingLocation(
            AtlasCompiledBinding binding,
            int stageIndex,
            int operationIndex,
            int bindingIndex)
        {
            return AtlasDiagnosticLocation.CompiledBinding(
                binding.FieldId,
                stageIndex,
                operationIndex,
                bindingIndex,
                binding.BindingName);
        }

    }
}