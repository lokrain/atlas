// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasCompilationWorkflow.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Provide the public validated compilation workflow for Atlas pipeline metadata.
// - Run authored pipeline policy validation before plan compilation.
// - Run compiled-plan structural, dataflow, and write-hazard validation after compilation.
// - Return a single ordered diagnostic result without allocating workspace memory or scheduling jobs.
//
// Design notes
// - This is managed compiler/tooling orchestration, not Burst/job payload.
// - This type does not define pipeline semantics; it consumes AtlasPipelineDefinition.
// - This type does not resolve concrete shapes, build executable plans, allocate memory, or run operations.
// - Validation stops after the first failing pass to avoid cascading diagnostics from invalid input state.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Pipelines;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Orchestrates the production metadata validation sequence for Atlas plan compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasCompilationWorkflow"/> is the boundary between authored route metadata and
    /// later compiler products. It validates an <see cref="AtlasPipelineDefinition"/> against an
    /// explicit route policy, compiles it against an <see cref="AtlasContractTable"/>, then validates
    /// the resulting <see cref="AtlasCompiledPlan"/> structurally and semantically.
    /// </para>
    ///
    /// <para>
    /// The workflow deliberately stops after each failing pass. Policy failures prevent compilation;
    /// compilation failures prevent compiled-plan validation; structural failures prevent dataflow;
    /// dataflow failures prevent write-hazard validation. This keeps diagnostics deterministic and
    /// prevents later passes from interpreting invalid intermediate state.
    /// </para>
    ///
    /// <para>
    /// This type does not perform shape resolution, ABI hashing, executable-plan construction,
    /// workspace allocation, operation scheduling, or artifact generation. Those are later passes
    /// that must consume a validated compiled plan.
    /// </para>
    /// </remarks>
    public static class AtlasCompilationWorkflow
    {
        /// <summary>
        /// Compiles and validates a pipeline using conservative production metadata policies.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition.</param>
        /// <param name="contracts">Field Contract table used to resolve operation access.</param>
        /// <returns>
        /// A successful result with a fully validated compiled plan, or a failed result with ordered
        /// diagnostics from the first failing pass.
        /// </returns>
        public static AtlasCompilationResult CompileValidated(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            return CompileValidated(
                pipeline,
                contracts,
                AtlasPipelineValidationPolicy.Conservative,
                AtlasDataflowValidationPolicy.ProductionDefault,
                AtlasWriteHazardValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Compiles and validates a pipeline using an explicit pipeline policy and production-default
        /// compiled-plan policies.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition.</param>
        /// <param name="contracts">Field Contract table used to resolve operation access.</param>
        /// <param name="pipelinePolicy">Route/preset policy for authored pipeline validation.</param>
        /// <returns>
        /// A successful result with a fully validated compiled plan, or a failed result with ordered
        /// diagnostics from the first failing pass.
        /// </returns>
        public static AtlasCompilationResult CompileValidated(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasPipelineValidationPolicy pipelinePolicy)
        {
            return CompileValidated(
                pipeline,
                contracts,
                pipelinePolicy,
                AtlasDataflowValidationPolicy.ProductionDefault,
                AtlasWriteHazardValidationPolicy.ProductionDefault);
        }

        /// <summary>
        /// Compiles and validates a pipeline using explicit policies for every validation layer.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition.</param>
        /// <param name="contracts">Field Contract table used to resolve operation access.</param>
        /// <param name="pipelinePolicy">Route/preset policy for authored pipeline validation.</param>
        /// <param name="dataflowPolicy">Policy controlling allowed initial content reads.</param>
        /// <param name="writePolicy">Policy controlling accepted write declarations.</param>
        /// <returns>
        /// A successful result with a fully validated compiled plan, or a failed result with ordered
        /// diagnostics from the first failing pass.
        /// </returns>
        public static AtlasCompilationResult CompileValidated(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasPipelineValidationPolicy pipelinePolicy,
            AtlasDataflowValidationPolicy dataflowPolicy,
            AtlasWriteHazardValidationPolicy writePolicy)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            AtlasPipelinePolicyValidator.Validate(
                pipeline,
                pipelinePolicy,
                diagnostics);

            if (diagnostics.HasFailures)
            {
                return AtlasCompilationResult.Failure(diagnostics);
            }

            var compilationResult = AtlasPlanCompiler.TryCompile(
                pipeline,
                contracts);

            diagnostics.AddRange(compilationResult);

            if (compilationResult.Failed)
            {
                return AtlasCompilationResult.Failure(diagnostics);
            }

            var plan = compilationResult.Plan;

            AtlasPlanValidator.Validate(
                plan,
                diagnostics);

            if (diagnostics.HasFailures)
            {
                return AtlasCompilationResult.Failure(diagnostics);
            }

            AtlasDataflowValidator.ValidateDataflowOnly(
                plan,
                dataflowPolicy,
                diagnostics);

            if (diagnostics.HasFailures)
            {
                return AtlasCompilationResult.Failure(diagnostics);
            }

            AtlasWriteHazardValidator.ValidateWriteHazardsOnly(
                plan,
                writePolicy,
                diagnostics);

            return diagnostics.HasFailures
                ? AtlasCompilationResult.Failure(diagnostics)
                : AtlasCompilationResult.Success(plan, diagnostics);
        }
    }
}