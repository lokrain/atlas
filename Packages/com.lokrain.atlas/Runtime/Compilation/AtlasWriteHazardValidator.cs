// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasWriteHazardValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate compiled-plan write declarations before executable planning.
// - Detect incoherent write ownership, lifetime, storage, ordering, and content-policy declarations.
// - Keep write-hazard validation separate from structural validation, dataflow validation, workspace memory, and schedulers.
// - Provide diagnostics-first validation for editor, CI, tests, and compiler tooling.
//
// Design notes
// - This validator is metadata-only.
// - It does not allocate workspace memory.
// - It does not schedule jobs.
// - It does not prove native container aliasing or dependency safety.
// - It does not reject repeated stages or repeated operations.
// - It validates symbolic write declarations against resolved Field Contracts.
// - Runtime memory safety still belongs to workspace, memory resolver, and executable scheduler validation.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Validates compiled-plan write declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWriteHazardValidator"/> checks whether compiled operation bindings declare
    /// coherent write semantics for their resolved <see cref="AtlasContract"/> rows.
    /// </para>
    ///
    /// <para>
    /// This validator intentionally stops at metadata. It can reject unsupported append storage,
    /// blob writes, missing deterministic ordering declarations, contradictory content policy,
    /// and unauthorized writes to external or borrowed Fields. It cannot prove that a concrete
    /// job implementation partitions writes safely or that a concrete native container instance
    /// is valid.
    /// </para>
    /// </remarks>
    public static class AtlasWriteHazardValidator
    {
        private static readonly AtlasDiagnosticCode InvalidBindingCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 200);

        private static readonly AtlasDiagnosticCode InvalidPresentContractCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 201);

        private static readonly AtlasDiagnosticCode ShapeOnlyWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 202);

        private static readonly AtlasDiagnosticCode WriteOwnershipRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 203);

        private static readonly AtlasDiagnosticCode WriteLifetimeRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 204);

        private static readonly AtlasDiagnosticCode WriteStorageRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 205);

        private static readonly AtlasDiagnosticCode AppendStorageRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 206);

        private static readonly AtlasDiagnosticCode ConsumeStorageRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 207);

        private static readonly AtlasDiagnosticCode MissingWriteContentPolicyCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 208);

        private static readonly AtlasDiagnosticCode ContradictoryWriteContentPolicyCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 209);

        private static readonly AtlasDiagnosticCode ParallelWriteRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 210);

        private static readonly AtlasDiagnosticCode ExclusiveWriteRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 211);

        private static readonly AtlasDiagnosticCode DeterministicWriteOrderRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 212);

        /// <summary>
        /// Compiles, structurally validates, dataflow-validates, and write-hazard-validates a pipeline.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <returns>
        /// A successful result with a validated compiled plan, or a failed result with deterministic diagnostics.
        /// </returns>
        public static AtlasCompilationResult TryCompileAndValidateWriteHazards(
            Pipelines.AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            return TryCompileAndValidateWriteHazards(
                pipeline,
                contracts,
                AtlasDataflowValidationPolicy.ProductionDefault,
                AtlasWriteHazardValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Compiles, structurally validates, dataflow-validates, and write-hazard-validates a pipeline.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <param name="dataflowPolicy">Policy controlling allowed initial content reads.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <returns>
        /// A successful result with a validated compiled plan, or a failed result with deterministic diagnostics.
        /// </returns>
        public static AtlasCompilationResult TryCompileAndValidateWriteHazards(
            Pipelines.AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasDataflowValidationPolicy dataflowPolicy,
            AtlasWriteHazardValidationPolicy writePolicy)
        {
            var compilationResult = AtlasDataflowValidator.TryCompileAndValidateDataflow(
                pipeline,
                contracts,
                dataflowPolicy);

            if (compilationResult.Failed)
            {
                return compilationResult;
            }

            var diagnostics = AtlasDiagnosticBuffer.Create();

            diagnostics.AddRange(compilationResult);

            ValidateWriteHazardsOnly(
                compilationResult.Plan,
                writePolicy,
                diagnostics);

            return diagnostics.HasFailures
                ? AtlasCompilationResult.Failure(diagnostics)
                : AtlasCompilationResult.Success(compilationResult.Plan, diagnostics);
        }

        /// <summary>
        /// Structurally validates, dataflow-validates, and write-hazard-validates a compiled plan.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(AtlasCompiledPlan plan)
        {
            return Validate(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault,
                AtlasWriteHazardValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Structurally validates, dataflow-validates, and write-hazard-validates a compiled plan.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(
            AtlasCompiledPlan plan,
            AtlasWriteHazardValidationPolicy writePolicy)
        {
            return Validate(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault,
                writePolicy);
        }

        /// <summary>
        /// Structurally validates, dataflow-validates, and write-hazard-validates a compiled plan.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="dataflowPolicy">Policy controlling allowed initial content reads.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy dataflowPolicy,
            AtlasWriteHazardValidationPolicy writePolicy)
        {
            var diagnostics = AtlasDataflowValidator.Validate(
                plan,
                dataflowPolicy);

            if (diagnostics.HasFailures)
            {
                return diagnostics;
            }

            ValidateWriteHazardsOnly(
                plan,
                writePolicy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Structurally validates, dataflow-validates, and write-hazard-validates a compiled plan into an existing buffer.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="dataflowPolicy">Policy controlling allowed initial content reads.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <param name="diagnostics">Diagnostic buffer to append to.</param>
        /// <returns>The supplied diagnostic buffer.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        public static AtlasDiagnosticBuffer Validate(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy dataflowPolicy,
            AtlasWriteHazardValidationPolicy writePolicy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            AtlasDataflowValidator.Validate(
                plan,
                dataflowPolicy,
                diagnostics);

            if (diagnostics.HasFailures)
            {
                return diagnostics;
            }

            ValidateWriteHazardsOnly(
                plan,
                writePolicy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Write-hazard-validates an already structurally and dataflow-valid compiled plan.
        /// </summary>
        /// <param name="plan">Structurally and dataflow-valid compiled plan.</param>
        /// <returns>Write-hazard diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer ValidateWriteHazardsOnly(AtlasCompiledPlan plan)
        {
            return ValidateWriteHazardsOnly(
                plan,
                AtlasWriteHazardValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Write-hazard-validates an already structurally and dataflow-valid compiled plan.
        /// </summary>
        /// <param name="plan">Structurally and dataflow-valid compiled plan.</param>
        /// <param name="policy">Policy controlling accepted write declarations.</param>
        /// <returns>Write-hazard diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer ValidateWriteHazardsOnly(
            AtlasCompiledPlan plan,
            AtlasWriteHazardValidationPolicy policy)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            ValidateWriteHazardsOnly(
                plan,
                policy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Write-hazard-validates an already structurally and dataflow-valid compiled plan into an existing buffer.
        /// </summary>
        /// <param name="plan">Structurally and dataflow-valid compiled plan.</param>
        /// <param name="policy">Policy controlling accepted write declarations.</param>
        /// <param name="diagnostics">Diagnostic buffer to append to.</param>
        /// <returns>The supplied diagnostic buffer.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="plan"/> or <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        public static AtlasDiagnosticBuffer ValidateWriteHazardsOnly(
            AtlasCompiledPlan plan,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            for (var stageIndex = 0; stageIndex < plan.Count; stageIndex++)
            {
                var stage = plan[stageIndex];

                for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
                {
                    var operation = stage[operationIndex];

                    for (var bindingIndex = 0; bindingIndex < operation.Count; bindingIndex++)
                    {
                        ValidateBinding(
                            operation,
                            operation[bindingIndex],
                            stageIndex,
                            operationIndex,
                            bindingIndex,
                            policy,
                            diagnostics);
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Throws when a compiled plan fails structural, dataflow, or write-hazard validation.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(AtlasCompiledPlan plan)
        {
            ValidateOrThrow(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault,
                AtlasWriteHazardValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Throws when a compiled plan fails structural, dataflow, or write-hazard validation.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(
            AtlasCompiledPlan plan,
            AtlasWriteHazardValidationPolicy writePolicy)
        {
            ValidateOrThrow(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault,
                writePolicy);
        }

        /// <summary>
        /// Throws when a compiled plan fails structural, dataflow, or write-hazard validation.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="dataflowPolicy">Policy controlling allowed initial content reads.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy dataflowPolicy,
            AtlasWriteHazardValidationPolicy writePolicy)
        {
            var diagnostics = Validate(
                plan,
                dataflowPolicy,
                writePolicy);

            if (!diagnostics.HasFailures)
            {
                return;
            }

            throw new InvalidOperationException(
                diagnostics.ToReportString());
        }

        private static void ValidateBinding(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            int stageIndex,
            int operationIndex,
            int bindingIndex,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            var location = CreateBindingLocation(
                binding,
                stageIndex,
                operationIndex,
                bindingIndex);

            if (!binding.IsValid)
            {
                diagnostics.AddError(
                    InvalidBindingCode,
                    location,
                    ToMessage($"Atlas write-hazard validation found an invalid binding at stage '{stageIndex}', operation '{operationIndex}', binding '{bindingIndex}'."));

                return;
            }

            ValidateShapeOnlyDeclaration(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            if (!binding.IsPresent)
            {
                return;
            }

            if (!binding.Contract.IsTableReady)
            {
                diagnostics.AddError(
                    InvalidPresentContractCode,
                    location,
                    ToMessage($"Atlas write-hazard validation found present binding '{GetDiagnosticName(binding.BindingName)}' with a Contract that is not table-ready."));

                return;
            }

            ValidateParallelDeclaration(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateExclusiveDeclaration(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            if (!binding.WritesContent)
            {
                return;
            }

            ValidateWriteAuthorization(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateWriteModeStorage(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateWriteContentPolicy(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateDeterministicOrdering(
                operation,
                binding,
                location,
                policy,
                diagnostics);
        }

        private static void ValidateShapeOnlyDeclaration(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsShapeOnlyWriteDeclaration(binding))
            {
                return;
            }

            diagnostics.AddError(
                ShapeOnlyWriteCode,
                location,
                ToMessage($"Atlas binding '{GetDiagnosticName(binding.BindingName)}' in operation '{GetDiagnosticName(operation.DebugName)}' is shape-only but declares write-related access semantics."));
        }

        private static void ValidateWriteAuthorization(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (!policy.AllowsWriteOwnership(binding.Contract))
            {
                diagnostics.AddError(
                    WriteOwnershipRejectedCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' writes Field '{GetDiagnosticName(binding.Contract.DebugName)}', but ownership policy '{binding.Contract.Ownership}' is not writable under the active write-hazard policy."));
            }

            if (!policy.AllowsWriteLifetime(binding.Contract))
            {
                diagnostics.AddError(
                    WriteLifetimeRejectedCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' writes Field '{GetDiagnosticName(binding.Contract.DebugName)}', but lifetime policy '{binding.Contract.Lifetime}' is not writable under the active write-hazard policy."));
            }

            if (!policy.AllowsWriteStorage(binding.Contract))
            {
                diagnostics.AddError(
                    WriteStorageRejectedCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' writes Field '{GetDiagnosticName(binding.Contract.DebugName)}', but storage kind '{binding.Contract.StorageFormat.Kind}' is not writable under the active write-hazard policy."));
            }
        }

        private static void ValidateWriteModeStorage(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (binding.Mode == AtlasOperationAccessMode.Append &&
                !policy.AllowsAppendStorage(binding.Contract))
            {
                diagnostics.AddError(
                    AppendStorageRejectedCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' appends to Field '{GetDiagnosticName(binding.Contract.DebugName)}', but storage kind '{binding.Contract.StorageFormat.Kind}' is not append-compatible under the active write-hazard policy."));
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume &&
                !policy.AllowsConsumeStorage(binding.Contract))
            {
                diagnostics.AddError(
                    ConsumeStorageRejectedCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' consumes Field '{GetDiagnosticName(binding.Contract.DebugName)}', but storage kind '{binding.Contract.StorageFormat.Kind}' is not consume-compatible under the active write-hazard policy."));
            }
        }

        private static void ValidateWriteContentPolicy(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (!policy.HasRequiredWriteContentPolicy(binding))
            {
                diagnostics.AddError(
                    MissingWriteContentPolicyCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' writes Field '{GetDiagnosticName(binding.Contract.DebugName)}' without an explicit write content policy."));
            }

            if (policy.HasContradictoryWriteContentPolicy(binding))
            {
                diagnostics.AddError(
                    ContradictoryWriteContentPolicyCode,
                    location,
                    ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' declares both discard-before-write and preserve-existing-content for Field '{GetDiagnosticName(binding.Contract.DebugName)}'."));
            }
        }

        private static void ValidateParallelDeclaration(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsParallelWriteDeclaration(binding))
            {
                return;
            }

            var reason = binding.WritesContent
                ? $"Field '{GetDiagnosticName(binding.Contract.DebugName)}' does not declare compatible parallel-write permission."
                : "the binding does not write content.";

            diagnostics.AddError(
                ParallelWriteRejectedCode,
                location,
                ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' declares parallel write access, but {reason}"));
        }

        private static void ValidateExclusiveDeclaration(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsExclusiveWriteDeclaration(binding))
            {
                return;
            }

            diagnostics.AddError(
                ExclusiveWriteRejectedCode,
                location,
                ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' declares exclusive write access, but the binding does not write content."));
        }

        private static void ValidateDeterministicOrdering(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsDeterministicWriteDeclaration(binding))
            {
                return;
            }

            diagnostics.AddError(
                DeterministicWriteOrderRejectedCode,
                location,
                ToMessage($"Atlas operation '{GetDiagnosticName(operation.DebugName)}' binding '{GetDiagnosticName(binding.BindingName)}' writes Field '{GetDiagnosticName(binding.Contract.DebugName)}' without the deterministic-order declaration required by the active write-hazard policy."));
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

        private static FixedString512Bytes ToMessage(string value)
        {
            return string.IsNullOrEmpty(value)
                ? new FixedString512Bytes("<empty diagnostic message>")
                : new FixedString512Bytes(Truncate(value, 511));
        }

        private static string Truncate(
            string value,
            int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(
                0,
                maxLength);
        }

        private static string GetDiagnosticName(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? "<unnamed>"
                : name.ToString();
        }
    }
}