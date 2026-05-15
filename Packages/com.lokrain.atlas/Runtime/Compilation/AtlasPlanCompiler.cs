// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasPlanCompiler.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Provide the public compilation entry point for Atlas pipeline metadata.
// - Compile a symbolic pipeline definition against a concrete Field Contract table.
// - Provide both throwing and non-throwing compilation APIs.
// - Preserve the rule that the compiler owns resolution, not memory allocation or job scheduling.
// - Keep workspace, memory resolver, scheduler, execution, and artifact concerns out of this layer.
//
// Design notes
// - Compile throws and is intended for hard contract/programmer flows.
// - TryCompile returns AtlasCompilationResult and is intended for editor, CI, and tooling flows.
// - This compiler emits an AtlasCompiledPlan only.
// - It does not allocate native memory.
// - It does not create executable job scheduler chains.
// - It does not validate route-family policy such as unique stage ids or mandatory stage presence.
// - Repeated stages and repeated operations remain legal here.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Pipelines;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Compiles authored Atlas pipeline metadata into resolved compiled plan metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasPlanCompiler"/> is the public boundary between authored pipeline contracts
    /// and compiled pipeline contracts. It resolves symbolic Field access through the supplied
    /// <see cref="AtlasContractTable"/> and returns an <see cref="AtlasCompiledPlan"/>.
    /// </para>
    ///
    /// <para>
    /// This type intentionally does not produce runtime memory bindings. The Contract table owns
    /// Field meaning, the compiled plan owns resolved structure, and later execution-specific
    /// types should map compiled bindings to workspace-owned memory.
    /// </para>
    ///
    /// <para>
    /// This compiler is not a route-policy validator. It does not reject repeated stages,
    /// repeated operation definitions, or route-specific ordering rules beyond the structural
    /// validity already enforced by pipeline, stage, operation, binding, and Contract types.
    /// </para>
    /// </remarks>
    public static class AtlasPlanCompiler
    {
        private static readonly AtlasDiagnosticCode NullPipelineCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 1);

        private static readonly AtlasDiagnosticCode NullContractTableCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 2);

        private static readonly AtlasDiagnosticCode InvalidPipelineIdCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 3);

        private static readonly AtlasDiagnosticCode EmptyPipelineDebugNameCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 4);

        private static readonly AtlasDiagnosticCode EmptyPipelineCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 5);

        private static readonly AtlasDiagnosticCode EmptyContractTableCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 6);

        private static readonly AtlasDiagnosticCode CompilationExceptionCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 7);

        private static readonly AtlasDiagnosticCode NullCompiledPlanCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 8);

        /// <summary>
        /// Compiles a pipeline definition against a Field Contract table.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <returns>
        /// A compiled plan preserving pipeline-local stage order, stage-local operation order,
        /// and operation-local binding order.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="pipeline"/> or <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the pipeline or Contract table is not usable for compilation.
        /// </exception>
        /// <remarks>
        /// The returned plan is metadata only. Runtime execution should consume it through a
        /// separate memory resolver or executable-plan builder.
        /// </remarks>
        public static AtlasCompiledPlan Compile(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            ValidateInputsOrThrow(
                pipeline,
                contracts);

            return AtlasCompiledPlan.Compile(
                pipeline,
                contracts);
        }

        /// <summary>
        /// Attempts to compile a pipeline definition against a Field Contract table.
        /// </summary>
        /// <param name="pipeline">Authored pipeline definition to compile.</param>
        /// <param name="contracts">Contract table used to resolve operation Field access.</param>
        /// <returns>
        /// A successful <see cref="AtlasCompilationResult"/> with a compiled plan, or a failed
        /// result with deterministic diagnostics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is the diagnostics-friendly compiler entry point for editor tooling, CI,
        /// validation windows, and automated tests.
        /// </para>
        ///
        /// <para>
        /// Expected invalid input is reported as diagnostics. Unexpected exceptions from deeper
        /// compiler code are captured as fatal diagnostics so the caller receives a deterministic
        /// result instead of losing context.
        /// </para>
        /// </remarks>
        public static AtlasCompilationResult TryCompile(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            ValidateInputs(
                pipeline,
                contracts,
                diagnostics);

            if (diagnostics.HasFailures)
            {
                return AtlasCompilationResult.Failure(diagnostics);
            }

            try
            {
                var plan = AtlasCompiledPlan.Compile(
                    pipeline,
                    contracts);

                if (plan == null)
                {
                    diagnostics.AddFatal(
                        NullCompiledPlanCode,
                        CreatePipelineLocation(pipeline),
                        AtlasDiagnosticText.Message("Atlas compilation returned a null compiled plan."));

                    return AtlasCompilationResult.Failure(diagnostics);
                }

                return AtlasCompilationResult.Success(
                    plan,
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.AddFatal(
                    CompilationExceptionCode,
                    CreatePipelineLocation(pipeline),
                    AtlasDiagnosticText.Message($"Atlas compilation failed with '{exception.GetType().Name}': {exception.Message}"));

                return AtlasCompilationResult.Failure(diagnostics);
            }
        }

        /// <summary>
        /// Validates that the compiler inputs are structurally usable.
        /// </summary>
        /// <param name="pipeline">Pipeline definition to validate.</param>
        /// <param name="contracts">Contract table to validate.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="pipeline"/> or <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when either input cannot be used as a compilation source.
        /// </exception>
        /// <remarks>
        /// This method performs boundary-level validation only. Deep validation remains owned by
        /// the source contract objects and by <see cref="AtlasCompiledPlan.Compile"/>.
        /// </remarks>
        public static void ValidateInputsOrThrow(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            ValidatePipelineUsableOrThrow(pipeline);
            ValidateContractTableUsableOrThrow(contracts);
        }

        private static void ValidateInputs(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            ValidatePipeline(
                pipeline,
                diagnostics);

            ValidateContractTable(
                contracts,
                diagnostics);
        }

        private static void ValidatePipeline(
            AtlasPipelineDefinition pipeline,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (pipeline == null)
            {
                diagnostics.AddError(
                    NullPipelineCode,
                    AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPlanCompiler")),
                    AtlasDiagnosticText.Message("Atlas pipeline compilation requires a non-null pipeline definition."));

                return;
            }

            var location = CreatePipelineLocation(pipeline);

            if (!pipeline.PipelineId.IsValid)
            {
                diagnostics.AddError(
                    InvalidPipelineIdCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas pipeline compilation requires a valid pipeline id."));
            }

            if (pipeline.DebugName.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyPipelineDebugNameCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas pipeline compilation requires a non-empty pipeline debug name."));
            }

            if (pipeline.IsEmpty)
            {
                diagnostics.AddError(
                    EmptyPipelineCode,
                    location,
                    AtlasDiagnosticText.Message("Atlas pipeline compilation requires at least one stage occurrence."));
            }
        }

        private static void ValidateContractTable(
            AtlasContractTable contracts,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (contracts == null)
            {
                diagnostics.AddError(
                    NullContractTableCode,
                    AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPlanCompiler")),
                    AtlasDiagnosticText.Message("Atlas pipeline compilation requires a non-null Contract table."));

                return;
            }

            if (contracts.Count <= 0)
            {
                diagnostics.AddError(
                    EmptyContractTableCode,
                    AtlasDiagnosticLocation.Contract(default, contracts.Name),
                    AtlasDiagnosticText.Message("Atlas pipeline compilation requires a non-empty Contract table."));
            }
        }

        private static void ValidatePipelineUsableOrThrow(AtlasPipelineDefinition pipeline)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            if (!pipeline.PipelineId.IsValid)
            {
                throw new ArgumentException(
                    "Atlas pipeline compilation requires a valid pipeline id.",
                    nameof(pipeline));
            }

            if (pipeline.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas pipeline compilation requires a non-empty pipeline debug name.",
                    nameof(pipeline));
            }

            if (pipeline.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas pipeline '{pipeline.DebugName}' contains no stage occurrences.",
                    nameof(pipeline));
            }
        }

        private static void ValidateContractTableUsableOrThrow(AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (contracts.Count <= 0)
            {
                throw new ArgumentException(
                    "Atlas pipeline compilation requires a non-empty Contract table.",
                    nameof(contracts));
            }
        }

        private static AtlasDiagnosticLocation CreatePipelineLocation(AtlasPipelineDefinition pipeline)
        {
            if (pipeline == null)
            {
                return AtlasDiagnosticLocation.Package(AtlasDiagnosticText.Name64("AtlasPlanCompiler"));
            }

            var stableId = pipeline.PipelineId.IsValid
                ? pipeline.PipelineId.ToStableDataId()
                : default;

            return AtlasDiagnosticLocation.Pipeline(
                stableId,
                pipeline.DebugName);
        }
    }
}