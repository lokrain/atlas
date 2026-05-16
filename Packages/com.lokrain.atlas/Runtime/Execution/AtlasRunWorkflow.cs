// Packages/com.lokrain.atlas/Runtime/Execution/AtlasRunWorkflow.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Orchestrate one complete Atlas managed run from request validation to execution output.
// - Compile authored pipeline metadata into a validated compiled plan.
// - Resolve field shapes, compile workspace layout, allocate workspace memory, and create execution context.
// - Dispatch compiled operations through the registered executor table.
// - Optionally capture durable artifacts, write artifact files, and export debug-map images.
//
// Design notes
// - This is managed orchestration, not Burst/job payload.
// - This type does not define operation semantics, field schemas, or pipeline policy rules.
// - This type owns the order of production workflow phases and converts phase failures into result objects.
// - Shape resolution is explicit and happens after validated compilation.
// - Workspace layout compilation is explicit and happens before allocation.
// - Artifact capture and debug-map export are allowed only after completed execution.
// - Scheduled execution without completion is supported only when the request does not require artifact capture.

using System;
using Lokrain.Atlas.Artifacts;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Debugging;
using Lokrain.Atlas.Workspaces;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Production managed orchestration entry point for one Atlas run request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasRunWorkflow"/> is the vertical workflow boundary that wires the existing
    /// package layers together without moving their responsibilities into one monolithic object.
    /// It validates the request, compiles pipeline metadata, resolves shapes, allocates workspace
    /// memory, dispatches operation executors, then optionally captures artifacts and debug maps.
    /// </para>
    ///
    /// <para>
    /// The workflow is intentionally result-returning. Phase failures are reported through
    /// <see cref="AtlasRunWorkflowResult"/> with the last successfully produced intermediate
    /// objects attached when safe. Unexpected exceptions are captured and associated with the exact
    /// phase that produced them.
    /// </para>
    /// </remarks>
    public static class AtlasRunWorkflow
    {
        /// <summary>
        /// Runs the supplied request with no incoming job dependency.
        /// </summary>
        /// <param name="request">Validated or user-created run request.</param>
        /// <returns>A workflow result describing success or the exact failed phase.</returns>
        public static AtlasRunWorkflowResult Run(
            AtlasRunRequest request)
        {
            return Run(
                request,
                default);
        }

        /// <summary>
        /// Runs the supplied request after an incoming job dependency.
        /// </summary>
        /// <param name="request">Validated or user-created run request.</param>
        /// <param name="inputDeps">Initial dependency that the first scheduled operation must depend on.</param>
        /// <returns>A workflow result describing success or the exact failed phase.</returns>
        public static AtlasRunWorkflowResult Run(
            AtlasRunRequest request,
            JobHandle inputDeps)
        {
            AtlasCompilationResult compilation = null;
            AtlasResolvedShapeSet shapes = null;
            AtlasWorkspace workspace = null;
            AtlasExecutionContext executionContext = null;
            AtlasExecutionResult execution = null;
            AtlasArtifact artifact = null;
            AtlasDebugMapImage debugMapImage = null;
            string artifactFilePath = null;
            string debugMapFilePath = null;

            try
            {
                ValidateRequestOrThrow(request);
            }
            catch (Exception exception)
            {
                return AtlasRunWorkflowResult.RequestValidationFailure(
                    request,
                    exception.Message,
                    exception);
            }

            try
            {
                compilation = AtlasCompilationWorkflow.CompileValidated(
                    request.Pipeline,
                    request.Contracts);

                if (compilation.Failed)
                {
                    return AtlasRunWorkflowResult.CompilationFailure(
                        request,
                        compilation);
                }
            }
            catch (Exception exception)
            {
                return CreateFailure(
                    AtlasRunWorkflowPhase.Compilation,
                    request,
                    compilation,
                    shapes,
                    workspace,
                    executionContext,
                    execution,
                    artifact,
                    debugMapImage,
                    artifactFilePath,
                    debugMapFilePath,
                    "Atlas compilation failed.",
                    exception);
            }

            try
            {
                shapes = AtlasShapeResolver.Resolve(
                    compilation.GetRequiredPlan());
            }
            catch (Exception exception)
            {
                return CreateFailure(
                    AtlasRunWorkflowPhase.ShapeResolution,
                    request,
                    compilation,
                    shapes,
                    workspace,
                    executionContext,
                    execution,
                    artifact,
                    debugMapImage,
                    artifactFilePath,
                    debugMapFilePath,
                    "Atlas shape resolution failed.",
                    exception);
            }

            try
            {
                var layout = AtlasWorkspaceLayoutCompiler.Compile(shapes);

                workspace = AtlasWorkspace.Create(
                    layout,
                    request.WorkspaceAllocator,
                    request.WorkspaceAllocationOptions);
            }
            catch (Exception exception)
            {
                return CreateFailure(
                    AtlasRunWorkflowPhase.WorkspaceAllocation,
                    request,
                    compilation,
                    shapes,
                    workspace,
                    executionContext,
                    execution,
                    artifact,
                    debugMapImage,
                    artifactFilePath,
                    debugMapFilePath,
                    "Atlas workspace allocation failed.",
                    exception);
            }

            try
            {
                executionContext = AtlasExecutionContext.Create(
                    compilation,
                    workspace);
            }
            catch (Exception exception)
            {
                return CreateFailure(
                    AtlasRunWorkflowPhase.ExecutionContextCreation,
                    request,
                    compilation,
                    shapes,
                    workspace,
                    executionContext,
                    execution,
                    artifact,
                    debugMapImage,
                    artifactFilePath,
                    debugMapFilePath,
                    "Atlas execution context creation failed.",
                    exception);
            }

            try
            {
                var runner = AtlasOperationRunner.Create(request.Executors);

                runner.ValidateCanExecuteAllOrThrow(executionContext);

                execution = request.CompleteExecution
                    ? runner.TryExecuteAllAndComplete(
                        executionContext,
                        inputDeps)
                    : runner.TryExecuteAll(
                        executionContext,
                        inputDeps);

                if (execution.Failed)
                {
                    return AtlasRunWorkflowResult.ExecutionFailure(
                        request,
                        compilation,
                        shapes,
                        workspace,
                        executionContext,
                        execution);
                }
            }
            catch (Exception exception)
            {
                return CreateFailure(
                    AtlasRunWorkflowPhase.Execution,
                    request,
                    compilation,
                    shapes,
                    workspace,
                    executionContext,
                    execution,
                    artifact,
                    debugMapImage,
                    artifactFilePath,
                    debugMapFilePath,
                    "Atlas execution failed.",
                    exception);
            }

            if (request.RequiresArtifactCapture)
            {
                try
                {
                    artifact = AtlasArtifact.Capture(
                        executionContext,
                        request.ComputeArtifactContentHashes);
                }
                catch (Exception exception)
                {
                    return CreateFailure(
                        AtlasRunWorkflowPhase.ArtifactCapture,
                        request,
                        compilation,
                        shapes,
                        workspace,
                        executionContext,
                        execution,
                        artifact,
                        debugMapImage,
                        artifactFilePath,
                        debugMapFilePath,
                        "Atlas artifact capture failed.",
                        exception);
                }
            }

            if (request.HasArtifactFilePath)
            {
                try
                {
                    AtlasArtifactWriter.WriteToFile(
                        artifact,
                        request.ArtifactFilePath,
                        request.OverwriteArtifactFile);

                    artifactFilePath = request.ArtifactFilePath;
                }
                catch (Exception exception)
                {
                    return CreateFailure(
                        AtlasRunWorkflowPhase.ArtifactWrite,
                        request,
                        compilation,
                        shapes,
                        workspace,
                        executionContext,
                        execution,
                        artifact,
                        debugMapImage,
                        artifactFilePath,
                        debugMapFilePath,
                        "Atlas artifact file writing failed.",
                        exception);
                }
            }

            if (request.HasDebugMap)
            {
                try
                {
                    debugMapImage = request.DebugMap.ExportFromArtifact(artifact);
                    debugMapFilePath = request.DebugMap.FilePath;
                }
                catch (Exception exception)
                {
                    return CreateFailure(
                        AtlasRunWorkflowPhase.DebugMapExport,
                        request,
                        compilation,
                        shapes,
                        workspace,
                        executionContext,
                        execution,
                        artifact,
                        debugMapImage,
                        artifactFilePath,
                        debugMapFilePath,
                        "Atlas debug-map export failed.",
                        exception);
                }
            }

            return AtlasRunWorkflowResult.Success(
                request,
                compilation,
                shapes,
                workspace,
                executionContext,
                execution,
                artifact,
                debugMapImage,
                artifactFilePath,
                debugMapFilePath);
        }

        private static void ValidateRequestOrThrow(
            AtlasRunRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.ValidateOrThrow();
        }

        private static AtlasRunWorkflowResult CreateFailure(
            AtlasRunWorkflowPhase phase,
            AtlasRunRequest request,
            AtlasCompilationResult compilation,
            AtlasResolvedShapeSet shapes,
            AtlasWorkspace workspace,
            AtlasExecutionContext executionContext,
            AtlasExecutionResult execution,
            AtlasArtifact artifact,
            AtlasDebugMapImage debugMapImage,
            string artifactFilePath,
            string debugMapFilePath,
            string message,
            Exception exception)
        {
            return AtlasRunWorkflowResult.Failure(
                phase,
                request,
                compilation,
                shapes,
                workspace,
                executionContext,
                execution,
                artifact,
                debugMapImage,
                artifactFilePath,
                debugMapFilePath,
                message,
                exception);
        }
    }
}
