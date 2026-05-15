// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasDataflowValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate compiled-plan content dataflow before executable planning.
// - Detect content reads before a prior write or explicitly allowed initial input.
// - Distinguish partial write coverage from full logical-content availability.
// - Track field content availability across flattened stage/operation order.
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
// - Append establishes partial content unless external/full coverage is explicitly declared by the operation access.
// - FullLogicalLength and FullCapacity writes establish full logical availability.
// - PartialLogicalLength and SparseIndexed writes establish partial availability.
// - Preserve-existing writes and read-write bindings require prior full logical content.

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

        private static readonly AtlasDiagnosticCode ReadAfterPartialWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 104);

        private static readonly AtlasDiagnosticCode PreserveAfterPartialWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 105);

        /// <summary>
        /// Compiles, structurally validates, and dataflow-validates a pipeline using production-default dataflow policy.
        /// </summary>
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
        public static AtlasDiagnosticBuffer Validate(AtlasCompiledPlan plan)
        {
            return Validate(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Structurally validates and dataflow-validates a compiled plan using explicit dataflow policy.
        /// </summary>
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
        public static AtlasDiagnosticBuffer ValidateDataflowOnly(AtlasCompiledPlan plan)
        {
            return ValidateDataflowOnly(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Dataflow-validates an already structurally valid compiled plan using explicit policy.
        /// </summary>
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

            var availableContent = new Dictionary<StableDataId, AtlasContentAvailability>();

            SeedInitiallyReadableContent(
                plan,
                policy,
                availableContent);

            for (var stageIndex = 0; stageIndex < plan.Count; stageIndex++)
            {
                var stage = plan[stageIndex];

                for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
                {
                    var operation = stage[operationIndex];

                    ValidateOperationInputs(
                        operation,
                        stageIndex,
                        operationIndex,
                        policy,
                        availableContent,
                        diagnostics);

                    ApplyOperationContentEffects(
                        operation,
                        policy,
                        availableContent);
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Throws when a compiled plan fails structural or dataflow validation using production-default policy.
        /// </summary>
        public static void ValidateOrThrow(AtlasCompiledPlan plan)
        {
            ValidateOrThrow(
                plan,
                AtlasDataflowValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Throws when a compiled plan fails structural or dataflow validation using explicit policy.
        /// </summary>
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
            Dictionary<StableDataId, AtlasContentAvailability> availableContent)
        {
            for (var i = 0; i < plan.Contracts.Count; i++)
            {
                var contract = plan.Contracts[i];
                var availability = policy.InitialAvailability(contract);

                if (availability.HasAnyContent())
                {
                    availableContent[contract.StableId] = availability;
                }
            }
        }

        private static void ValidateOperationInputs(
            AtlasCompiledOperation operation,
            int stageIndex,
            int operationIndex,
            AtlasDataflowValidationPolicy policy,
            Dictionary<StableDataId, AtlasContentAvailability> availableContent,
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
                        AtlasDiagnosticText.Message($"Atlas dataflow validation found a present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' with a Contract that is not table-ready."));

                    continue;
                }

                if (!policy.RequiresPriorContent(binding))
                {
                    continue;
                }

                if (!availableContent.TryGetValue(binding.FieldId, out var availability) ||
                    !availability.HasAnyContent())
                {
                    AddMissingPriorContentDiagnostic(
                        operation,
                        binding,
                        stageIndex,
                        operationIndex,
                        bindingIndex,
                        diagnostics);

                    continue;
                }

                if (policy.RequiresFullPriorContent(binding) &&
                    !availability.HasFullLogicalContent())
                {
                    AddInsufficientPriorContentDiagnostic(
                        operation,
                        binding,
                        availability,
                        stageIndex,
                        operationIndex,
                        bindingIndex,
                        diagnostics);
                }
            }
        }

        private static void ApplyOperationContentEffects(
            AtlasCompiledOperation operation,
            AtlasDataflowValidationPolicy policy,
            Dictionary<StableDataId, AtlasContentAvailability> availableContent)
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

                var established = policy.EstablishedAvailability(binding);

                if (!established.HasAnyContent())
                {
                    continue;
                }

                if (availableContent.TryGetValue(binding.FieldId, out var existing))
                {
                    availableContent[binding.FieldId] = existing.Max(established);
                    continue;
                }

                availableContent.Add(
                    binding.FieldId,
                    established);
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
            var operationName = AtlasDiagnosticText.Name(operation.DebugName);
            var bindingName = AtlasDiagnosticText.Name(binding.BindingName);
            var contractName = AtlasDiagnosticText.Name(binding.Contract.DebugName);

            diagnostics.AddError(
                code,
                CreateBindingLocation(
                    binding,
                    stageIndex,
                    operationIndex,
                    bindingIndex),
                AtlasDiagnosticText.Message($"Atlas dataflow violation: operation '{operationName}' binding '{bindingName}' requires prior content for Field '{contractName}' in mode '{modeText}', but no previous operation produced it and the dataflow policy does not allow it as initial input."));
        }

        private static void AddInsufficientPriorContentDiagnostic(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasContentAvailability availability,
            int stageIndex,
            int operationIndex,
            int bindingIndex,
            AtlasDiagnosticBuffer diagnostics)
        {
            var code = GetInsufficientPriorContentCode(binding);
            var operationName = AtlasDiagnosticText.Name(operation.DebugName);
            var bindingName = AtlasDiagnosticText.Name(binding.BindingName);
            var contractName = AtlasDiagnosticText.Name(binding.Contract.DebugName);

            diagnostics.AddError(
                code,
                CreateBindingLocation(
                    binding,
                    stageIndex,
                    operationIndex,
                    bindingIndex),
                AtlasDiagnosticText.Message($"Atlas dataflow violation: operation '{operationName}' binding '{bindingName}' requires full logical content for Field '{contractName}', but current availability is '{availability}'. Partial, sparse, or appended content is not sufficient for this access."));
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

        private static AtlasDiagnosticCode GetInsufficientPriorContentCode(AtlasCompiledBinding binding)
        {
            if (binding.WritesContent &&
                binding.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                return PreserveAfterPartialWriteCode;
            }

            return ReadAfterPartialWriteCode;
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
