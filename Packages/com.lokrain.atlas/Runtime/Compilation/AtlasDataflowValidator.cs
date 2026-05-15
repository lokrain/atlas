// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasDataflowValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate compiled-plan content dataflow before executable planning.
// - Detect content reads before a prior write or explicitly allowed initial input.
// - Track Field content availability across flattened stage/operation order.
// - Keep dataflow validation separate from route policy, workspace memory, and scheduler validation.
//
// Design notes
// - This validator is metadata-only.
// - It does not allocate native workspace memory.
// - It does not schedule jobs.
// - It does not reject repeated stages or repeated operations.
// - It treats each operation as an atomic step: reads are checked before writes from that operation become available.
// - Shape-only bindings do not require content and do not establish content.
// - Missing optional bindings do not require content and do not establish content.
// - Consume requires prior content and removes content availability for later operations.
// - Append and discard-before-write writes establish content availability.
// - Preserve-existing writes and read-write bindings require prior content and then keep/establish availability.

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Validates compiled-plan content dataflow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDataflowValidator"/> walks a compiled plan in flattened execution order
    /// and tracks which Fields have initialized content available for content reads.
    /// </para>
    ///
    /// <para>
    /// This validator is intentionally narrower than route validation. It does not know whether
    /// a continental route requires a specific stage set, whether a stage may repeat, or whether
    /// a route-specific Field must be produced by a specific operation. It only validates content
    /// availability across compiled operation order.
    /// </para>
    ///
    /// <para>
    /// Runtime memory still belongs to workspace and memory-resolution validation. A Field being
    /// considered available by this validator means the compiled metadata has a plausible producer
    /// or initial-content source. It does not prove that a concrete <c>NativeArray</c> exists.
    /// </para>
    /// </remarks>
    public static class AtlasDataflowValidator
    {
        private static readonly AtlasDiagnosticCode ReadBeforeWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 100);

        private static readonly AtlasDiagnosticCode PreserveBeforeWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 101);

        private static readonly AtlasDiagnosticCode ConsumeBeforeWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 102);

        private static readonly AtlasDiagnosticCode InvalidPresentContractCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 103);

        /// <summary>
        /// Compiles, structurally validates, and dataflow-validates a pipeline using production-default dataflow policy.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <returns>
        /// A successful result with a structurally and dataflow-valid compiled plan, or a failed
        /// result with deterministic diagnostics.
        /// </returns>
        public static AtlasCompilationResult TryCompileAndValidateDataflow(
            Pipelines.AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            return TryCompileAndValidateDataflow(
                pipeline,
                contracts,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Compiles, structurally validates, and dataflow-validates a pipeline using explicit dataflow policy.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <param name="policy">Dataflow policy controlling allowed initial content reads.</param>
        /// <returns>
        /// A successful result with a structurally and dataflow-valid compiled plan, or a failed
        /// result with deterministic diagnostics.
        /// </returns>
        public static AtlasCompilationResult TryCompileAndValidateDataflow(
            Pipelines.AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasDataflowValidationPolicy policy)
        {
            var compilationResult = AtlasPlanValidator.TryCompileAndValidate(
                pipeline,
                contracts);

            if (compilationResult.Failed)
            {
                return compilationResult;
            }

            var diagnostics = AtlasDiagnosticBuffer.Create();

            diagnostics.AddRange(compilationResult);

            ValidateDataflowOnly(
                compilationResult.Plan,
                policy,
                diagnostics);

            return diagnostics.HasFailures
                ? AtlasCompilationResult.Failure(diagnostics)
                : AtlasCompilationResult.Success(compilationResult.Plan, diagnostics);
        }

        /// <summary>
        /// Structurally validates and dataflow-validates a compiled plan using production-default dataflow policy.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(AtlasCompiledPlan plan)
        {
            return Validate(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Structurally validates and dataflow-validates a compiled plan using explicit dataflow policy.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="policy">Dataflow policy controlling allowed initial content reads.</param>
        /// <returns>Diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer Validate(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy policy)
        {
            var diagnostics = AtlasPlanValidator.Validate(plan);

            if (diagnostics.HasFailures)
            {
                return diagnostics;
            }

            ValidateDataflowOnly(
                plan,
                policy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Structurally validates and dataflow-validates a compiled plan into an existing diagnostic buffer.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="policy">Dataflow policy controlling allowed initial content reads.</param>
        /// <param name="diagnostics">Diagnostic buffer to append to.</param>
        /// <returns>The supplied diagnostic buffer.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        public static AtlasDiagnosticBuffer Validate(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            AtlasPlanValidator.Validate(
                plan,
                diagnostics);

            if (diagnostics.HasFailures)
            {
                return diagnostics;
            }

            ValidateDataflowOnly(
                plan,
                policy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Dataflow-validates an already structurally valid compiled plan using production-default policy.
        /// </summary>
        /// <param name="plan">Structurally valid compiled plan.</param>
        /// <returns>Dataflow diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer ValidateDataflowOnly(AtlasCompiledPlan plan)
        {
            return ValidateDataflowOnly(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Dataflow-validates an already structurally valid compiled plan using explicit policy.
        /// </summary>
        /// <param name="plan">Structurally valid compiled plan.</param>
        /// <param name="policy">Dataflow policy controlling allowed initial content reads.</param>
        /// <returns>Dataflow diagnostics in deterministic traversal order.</returns>
        public static AtlasDiagnosticBuffer ValidateDataflowOnly(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy policy)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            ValidateDataflowOnly(
                plan,
                policy,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Dataflow-validates an already structurally valid compiled plan into an existing diagnostic buffer.
        /// </summary>
        /// <param name="plan">Structurally valid compiled plan.</param>
        /// <param name="policy">Dataflow policy controlling allowed initial content reads.</param>
        /// <param name="diagnostics">Diagnostic buffer to append to.</param>
        /// <returns>The supplied diagnostic buffer.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="plan"/> or <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        public static AtlasDiagnosticBuffer ValidateDataflowOnly(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy policy,
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

            var availableContent = new HashSet<StableDataId>();

            SeedInitiallyReadableContent(
                plan,
                policy,
                availableContent);

            for (var stageIndex = 0; stageIndex < plan.Count; stageIndex++)
            {
                var stage = plan[stageIndex];

                for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
                {
                    ValidateOperationInputs(
                        stage[operationIndex],
                        stageIndex,
                        operationIndex,
                        policy,
                        availableContent,
                        diagnostics);

                    ApplyOperationContentEffects(
                        stage[operationIndex],
                        availableContent);
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Throws when a compiled plan fails structural or dataflow validation using production-default policy.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(AtlasCompiledPlan plan)
        {
            ValidateOrThrow(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Throws when a compiled plan fails structural or dataflow validation using explicit policy.
        /// </summary>
        /// <param name="plan">Compiled plan to validate.</param>
        /// <param name="policy">Dataflow policy controlling allowed initial content reads.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation reports at least one error or fatal diagnostic.
        /// </exception>
        public static void ValidateOrThrow(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy policy)
        {
            var diagnostics = Validate(
                plan,
                policy);

            if (!diagnostics.HasFailures)
            {
                return;
            }

            throw new InvalidOperationException(
                diagnostics.ToReportString());
        }

        private static void SeedInitiallyReadableContent(
            AtlasCompiledPlan plan,
            AtlasDataflowValidationPolicy policy,
            HashSet<StableDataId> availableContent)
        {
            for (var i = 0; i < plan.Contracts.Count; i++)
            {
                var contract = plan.Contracts[i];

                if (policy.AllowsInitialRead(contract))
                {
                    availableContent.Add(contract.StableId);
                }
            }
        }

        private static void ValidateOperationInputs(
            AtlasCompiledOperation operation,
            int stageIndex,
            int operationIndex,
            AtlasDataflowValidationPolicy policy,
            HashSet<StableDataId> availableContent,
            AtlasDiagnosticBuffer diagnostics)
        {
            for (var bindingIndex = 0; bindingIndex < operation.Count; bindingIndex++)
            {
                var binding = operation[bindingIndex];

                if (!binding.IsPresent || binding.IsShapeOnly)
                {
                    continue;
                }

                if (!binding.Contract.IsTableReady)
                {
                    diagnostics.AddError(
                        InvalidPresentContractCode,
                        CreateBindingLocation(
                            binding,
                            stageIndex,
                            operationIndex,
                            bindingIndex),
                        ToMessage($"Atlas dataflow validation found a present binding '{GetDiagnosticName(binding.BindingName)}' with a Contract that is not table-ready."));

                    continue;
                }

                if (!policy.RequiresPriorContent(binding))
                {
                    continue;
                }

                if (availableContent.Contains(binding.FieldId))
                {
                    continue;
                }

                AddMissingPriorContentDiagnostic(
                    operation,
                    binding,
                    stageIndex,
                    operationIndex,
                    bindingIndex,
                    diagnostics);
            }
        }

        private static void ApplyOperationContentEffects(
            AtlasCompiledOperation operation,
            HashSet<StableDataId> availableContent)
        {
            for (var bindingIndex = 0; bindingIndex < operation.Count; bindingIndex++)
            {
                var binding = operation[bindingIndex];

                if (!binding.IsPresent || binding.IsShapeOnly)
                {
                    continue;
                }

                if (binding.Mode == AtlasOperationAccessMode.Consume)
                {
                    availableContent.Remove(binding.FieldId);
                    continue;
                }

                if (binding.WritesContent)
                {
                    availableContent.Add(binding.FieldId);
                }
            }
        }

        private static void AddMissingPriorContentDiagnostic(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            int stageIndex,
            int operationIndex,
            int bindingIndex,
            AtlasDiagnosticBuffer diagnostics)
        {
            var code = GetMissingPriorContentCode(binding);
            var modeText = binding.Mode.ToString();
            var operationName = GetDiagnosticName(operation.DebugName);
            var bindingName = GetDiagnosticName(binding.BindingName);
            var contractName = GetDiagnosticName(binding.Contract.DebugName);

            diagnostics.AddError(
                code,
                CreateBindingLocation(
                    binding,
                    stageIndex,
                    operationIndex,
                    bindingIndex),
                ToMessage($"Atlas dataflow violation: operation '{operationName}' binding '{bindingName}' requires prior content for Field '{contractName}' in mode '{modeText}', but no previous operation produced it and the dataflow policy does not allow it as initial input."));
        }

        private static AtlasDiagnosticCode GetMissingPriorContentCode(AtlasCompiledBinding binding)
        {
            if (binding.Mode == AtlasOperationAccessMode.Consume)
            {
                return ConsumeBeforeWriteCode;
            }

            if (binding.WritesContent &&
                binding.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                return PreserveBeforeWriteCode;
            }

            return ReadBeforeWriteCode;
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