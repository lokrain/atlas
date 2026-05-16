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
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var visitor = new AtlasWriteHazardBindingValidator(
                policy,
                diagnostics);

            AtlasCompiledPlanBindingWalker.VisitBindings(
                plan,
                ref visitor);

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
    }
}