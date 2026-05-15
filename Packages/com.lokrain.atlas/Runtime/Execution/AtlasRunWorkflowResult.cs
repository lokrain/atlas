// Packages/com.lokrain.atlas/Runtime/Execution/AtlasRunWorkflowResult.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Represent the managed result of one Atlas run workflow.
// - Preserve compilation, shape resolution, workspace, execution, artifact, and debug-map output.
// - Report the exact workflow phase that failed.
// - Own workflow-created workspace memory and dispose it deterministically.
//
// Design notes
// - This is orchestration result state, not a durable artifact.
// - The artifact owns managed payload bytes and survives workspace disposal.
// - The debug-map image owns managed RGBA bytes and survives workspace disposal.
// - The workspace remains native memory and must be disposed.
// - A successful workflow result owns the returned workspace unless ownership is explicitly released.
// - Failed workflow results may still own a partially allocated workspace.
// - Disposing the result disposes owned workspace memory only.
// - Disposing the result does not dispose artifact/debug-map managed data because those are ordinary managed objects.

using System;
using System.Globalization;
using Lokrain.Atlas.Artifacts;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Debugging;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Managed result of a complete Atlas run workflow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasRunWorkflowResult"/> is the ownership boundary returned by
    /// <see cref="AtlasRunWorkflow"/>. It keeps every major workflow product visible to callers:
    /// compilation metadata, resolved shapes, workspace memory, execution result, captured artifact,
    /// and optional debug-map image.
    /// </para>
    ///
    /// <para>
    /// The result owns workflow-created workspace memory. Call <see cref="Dispose"/> when the caller
    /// is done inspecting workspace fields. Use <see cref="ReleaseWorkspaceOwnership"/> only when
    /// ownership is intentionally transferred to another owner.
    /// </para>
    ///
    /// <para>
    /// Artifacts and debug-map images are detached managed outputs. They remain usable after
    /// disposing the result because they do not reference workspace-owned native memory.
    /// </para>
    /// </remarks>
    public sealed class AtlasRunWorkflowResult :
        IDisposable
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private AtlasWorkspace _workspace;
        private bool _ownsWorkspace;
        private byte _state;

        private AtlasRunWorkflowResult(
            AtlasRunWorkflowResultStatus status,
            AtlasRunWorkflowPhase phase,
            AtlasRunRequest request,
            AtlasCompilationResult compilation,
            AtlasResolvedShapeSet shapes,
            AtlasWorkspace workspace,
            bool ownsWorkspace,
            AtlasExecutionContext executionContext,
            AtlasExecutionResult execution,
            AtlasArtifact artifact,
            AtlasDebugMapImage debugMapImage,
            string artifactFilePath,
            string debugMapFilePath,
            FixedString512Bytes message,
            Exception exception)
        {
            ValidateConstructorArgumentsOrThrow(
                status,
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
                exception);

            Status = status;
            Phase = phase;
            Request = request;
            Compilation = compilation;
            Shapes = shapes;
            _workspace = workspace;
            _ownsWorkspace = ownsWorkspace && workspace != null;
            ExecutionContext = executionContext;
            Execution = execution;
            Artifact = artifact;
            DebugMapImage = debugMapImage;
            ArtifactFilePath = artifactFilePath;
            DebugMapFilePath = debugMapFilePath;
            Message = message;
            Exception = exception;
            _state = AliveState;
        }

        /// <summary>
        /// Gets the terminal workflow status.
        /// </summary>
        public AtlasRunWorkflowResultStatus Status { get; }

        /// <summary>
        /// Gets the workflow phase associated with this result.
        /// </summary>
        public AtlasRunWorkflowPhase Phase { get; }

        /// <summary>
        /// Gets the request consumed by the workflow.
        /// </summary>
        public AtlasRunRequest Request { get; }

        /// <summary>
        /// Gets the compilation result.
        /// </summary>
        public AtlasCompilationResult Compilation { get; }

        /// <summary>
        /// Gets the compiled plan when compilation succeeded.
        /// </summary>
        public AtlasCompiledPlan Plan => Compilation?.Plan;

        /// <summary>
        /// Gets the resolved field shapes used for workspace allocation.
        /// </summary>
        public AtlasResolvedShapeSet Shapes { get; }

        /// <summary>
        /// Gets the workspace returned by the workflow.
        /// </summary>
        /// <remarks>
        /// This workspace may be disposed when <see cref="IsDisposed"/> is true. Use
        /// <see cref="GetRequiredWorkspace"/> for exception-based access to a live workspace.
        /// </remarks>
        public AtlasWorkspace Workspace => _workspace;

        /// <summary>
        /// Gets the execution context created for the compiled plan and workspace.
        /// </summary>
        public AtlasExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the operation execution result.
        /// </summary>
        public AtlasExecutionResult Execution { get; }

        /// <summary>
        /// Gets the captured artifact, when artifact capture was requested and succeeded.
        /// </summary>
        public AtlasArtifact Artifact { get; }

        /// <summary>
        /// Gets the debug-map image, when debug-map export was requested and succeeded.
        /// </summary>
        public AtlasDebugMapImage DebugMapImage { get; }

        /// <summary>
        /// Gets the artifact file path written by the workflow, when artifact file output succeeded.
        /// </summary>
        public string ArtifactFilePath { get; }

        /// <summary>
        /// Gets the debug-map file path written by the workflow, when debug-map output succeeded.
        /// </summary>
        public string DebugMapFilePath { get; }

        /// <summary>
        /// Gets the workflow result message.
        /// </summary>
        public FixedString512Bytes Message { get; }

        /// <summary>
        /// Gets the managed exception captured by the workflow, when available.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets whether the workflow completed successfully.
        /// </summary>
        public bool Succeeded => Status == AtlasRunWorkflowResultStatus.Succeeded;

        /// <summary>
        /// Gets whether the workflow failed.
        /// </summary>
        public bool Failed => Status == AtlasRunWorkflowResultStatus.Failed;

        /// <summary>
        /// Gets whether the result has been disposed.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Gets whether a request is attached.
        /// </summary>
        public bool HasRequest => Request != null;

        /// <summary>
        /// Gets whether compilation result metadata is attached.
        /// </summary>
        public bool HasCompilation => Compilation != null;

        /// <summary>
        /// Gets whether a compiled plan is attached.
        /// </summary>
        public bool HasPlan => Plan != null;

        /// <summary>
        /// Gets whether resolved shapes are attached.
        /// </summary>
        public bool HasShapes => Shapes != null;

        /// <summary>
        /// Gets whether a workspace reference is attached.
        /// </summary>
        public bool HasWorkspace => _workspace != null;

        /// <summary>
        /// Gets whether this result owns the attached workspace.
        /// </summary>
        public bool OwnsWorkspace => _ownsWorkspace && _workspace != null;

        /// <summary>
        /// Gets whether an execution context is attached.
        /// </summary>
        public bool HasExecutionContext => ExecutionContext != null;

        /// <summary>
        /// Gets whether an execution result is attached.
        /// </summary>
        public bool HasExecution => Execution != null;

        /// <summary>
        /// Gets whether an artifact is attached.
        /// </summary>
        public bool HasArtifact => Artifact != null;

        /// <summary>
        /// Gets whether a debug-map image is attached.
        /// </summary>
        public bool HasDebugMapImage => DebugMapImage != null;

        /// <summary>
        /// Gets whether an artifact output file path is attached.
        /// </summary>
        public bool HasArtifactFilePath => HasPath(ArtifactFilePath);

        /// <summary>
        /// Gets whether a debug-map output file path is attached.
        /// </summary>
        public bool HasDebugMapFilePath => HasPath(DebugMapFilePath);

        /// <summary>
        /// Gets whether artifact file output completed.
        /// </summary>
        public bool WroteArtifactFile => Succeeded && HasArtifact && HasArtifactFilePath;

        /// <summary>
        /// Gets whether debug-map file output completed.
        /// </summary>
        public bool WroteDebugMapFile => Succeeded && HasDebugMapImage && HasDebugMapFilePath;

        /// <summary>
        /// Gets whether a managed exception is attached.
        /// </summary>
        public bool HasException => Exception != null;

        /// <summary>
        /// Creates a successful workflow result.
        /// </summary>
        public static AtlasRunWorkflowResult Success(
            AtlasRunRequest request,
            AtlasCompilationResult compilation,
            AtlasResolvedShapeSet shapes,
            AtlasWorkspace workspace,
            AtlasExecutionContext executionContext,
            AtlasExecutionResult execution,
            AtlasArtifact artifact,
            AtlasDebugMapImage debugMapImage,
            string artifactFilePath,
            string debugMapFilePath)
        {
            return new AtlasRunWorkflowResult(
                AtlasRunWorkflowResultStatus.Succeeded,
                AtlasRunWorkflowPhase.Completed,
                request,
                compilation,
                shapes,
                workspace,
                ownsWorkspace: true,
                executionContext,
                execution,
                artifact,
                debugMapImage,
                artifactFilePath,
                debugMapFilePath,
                CreateMessage("Atlas run workflow completed successfully."),
                exception: null);
        }

        /// <summary>
        /// Creates a failed workflow result.
        /// </summary>
        public static AtlasRunWorkflowResult Failure(
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
            Exception exception = null)
        {
            return new AtlasRunWorkflowResult(
                AtlasRunWorkflowResultStatus.Failed,
                phase,
                request,
                compilation,
                shapes,
                workspace,
                ownsWorkspace: true,
                executionContext,
                execution,
                artifact,
                debugMapImage,
                artifactFilePath,
                debugMapFilePath,
                CreateFailureMessage(phase, message, exception),
                exception);
        }

        /// <summary>
        /// Creates a failed result for request validation failure.
        /// </summary>
        public static AtlasRunWorkflowResult RequestValidationFailure(
            AtlasRunRequest request,
            string message,
            Exception exception = null)
        {
            return Failure(
                AtlasRunWorkflowPhase.RequestValidation,
                request,
                compilation: null,
                shapes: null,
                workspace: null,
                executionContext: null,
                execution: null,
                artifact: null,
                debugMapImage: null,
                artifactFilePath: null,
                debugMapFilePath: null,
                message,
                exception);
        }

        /// <summary>
        /// Creates a failed result for compilation failure.
        /// </summary>
        public static AtlasRunWorkflowResult CompilationFailure(
            AtlasRunRequest request,
            AtlasCompilationResult compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return Failure(
                AtlasRunWorkflowPhase.Compilation,
                request,
                compilation,
                shapes: null,
                workspace: null,
                executionContext: null,
                execution: null,
                artifact: null,
                debugMapImage: null,
                artifactFilePath: null,
                debugMapFilePath: null,
                compilation.ToReportString(),
                exception: null);
        }

        /// <summary>
        /// Creates a failed result for execution failure.
        /// </summary>
        public static AtlasRunWorkflowResult ExecutionFailure(
            AtlasRunRequest request,
            AtlasCompilationResult compilation,
            AtlasResolvedShapeSet shapes,
            AtlasWorkspace workspace,
            AtlasExecutionContext executionContext,
            AtlasExecutionResult execution)
        {
            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            return Failure(
                AtlasRunWorkflowPhase.Execution,
                request,
                compilation,
                shapes,
                workspace,
                executionContext,
                execution,
                artifact: null,
                debugMapImage: null,
                artifactFilePath: null,
                debugMapFilePath: null,
                execution.Message.IsEmpty
                    ? "Atlas execution failed."
                    : execution.Message.ToString(),
                execution.Exception);
        }

        /// <summary>
        /// Gets a required live workspace.
        /// </summary>
        public AtlasWorkspace GetRequiredWorkspace()
        {
            ThrowIfDisposed();

            if (_workspace == null)
            {
                throw new InvalidOperationException(
                    "Atlas run workflow result does not contain a workspace.");
            }

            if (_workspace.IsDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasWorkspace),
                    "Atlas run workflow result contains a disposed workspace.");
            }

            return _workspace;
        }

        /// <summary>
        /// Gets a required execution context.
        /// </summary>
        public AtlasExecutionContext GetRequiredExecutionContext()
        {
            ThrowIfDisposed();

            if (ExecutionContext != null)
            {
                return ExecutionContext;
            }

            throw new InvalidOperationException(
                "Atlas run workflow result does not contain an execution context.");
        }

        /// <summary>
        /// Gets a required artifact.
        /// </summary>
        public AtlasArtifact GetRequiredArtifact()
        {
            if (Artifact != null)
            {
                return Artifact;
            }

            throw new InvalidOperationException(
                "Atlas run workflow result does not contain an artifact.");
        }

        /// <summary>
        /// Gets a required debug-map image.
        /// </summary>
        public AtlasDebugMapImage GetRequiredDebugMapImage()
        {
            if (DebugMapImage != null)
            {
                return DebugMapImage;
            }

            throw new InvalidOperationException(
                "Atlas run workflow result does not contain a debug-map image.");
        }

        /// <summary>
        /// Transfers workspace ownership out of this result.
        /// </summary>
        /// <returns>The attached workspace.</returns>
        /// <remarks>
        /// After calling this method, disposing this result will not dispose the returned workspace.
        /// The caller becomes responsible for disposing it.
        /// </remarks>
        public AtlasWorkspace ReleaseWorkspaceOwnership()
        {
            ThrowIfDisposed();

            if (_workspace == null)
            {
                throw new InvalidOperationException(
                    "Atlas run workflow result does not contain a workspace to release.");
            }

            var released = _workspace;
            _ownsWorkspace = false;
            _workspace = null;

            return released;
        }

        /// <summary>
        /// Throws when this result represents failure.
        /// </summary>
        public void ThrowIfFailed()
        {
            if (!Failed)
            {
                return;
            }

            throw new InvalidOperationException(
                GetFailureMessage(),
                Exception);
        }

        /// <summary>
        /// Throws when this result has been disposed.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state == DisposedState)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasRunWorkflowResult),
                    "Atlas run workflow result has been disposed.");
            }
        }

        /// <summary>
        /// Disposes owned workspace memory.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            if (_ownsWorkspace && _workspace != null)
            {
                _workspace.Dispose();
            }

            _ownsWorkspace = false;
            _state = DisposedState;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a diagnostic representation of this workflow result.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasRunWorkflowResult(Status={0}, Phase={1}, Compilation={2}, Shapes={3}, Workspace={4}, OwnsWorkspace={5}, Execution={6}, Artifact={7}, DebugMap={8}, ArtifactFile='{9}', DebugMapFile='{10}', Message={11})",
                Status,
                Phase,
                HasCompilation ? Compilation.ToString() : "<none>",
                HasShapes ? Shapes.GetDiagnosticName() : "<none>",
                HasWorkspace ? Workspace.ToString() : "<none>",
                OwnsWorkspace,
                HasExecution ? Execution.ToString() : "<none>",
                HasArtifact ? Artifact.ToString() : "<none>",
                HasDebugMapImage
                    ? string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}x{1}",
                        DebugMapImage.Width,
                        DebugMapImage.Height)
                    : "<none>",
                HasArtifactFilePath ? ArtifactFilePath : "<none>",
                HasDebugMapFilePath ? DebugMapFilePath : "<none>",
                Message);
        }

        private string GetFailureMessage()
        {
            if (!Message.IsEmpty)
            {
                return Message.ToString();
            }

            if (Compilation != null && Compilation.Failed)
            {
                return Compilation.ToReportString();
            }

            if (Execution != null && Execution.Failed && !Execution.Message.IsEmpty)
            {
                return Execution.Message.ToString();
            }

            if (Exception != null && !string.IsNullOrWhiteSpace(Exception.Message))
            {
                return Exception.Message;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Atlas run workflow failed in phase '{0}'.",
                Phase);
        }

        private static void ValidateConstructorArgumentsOrThrow(
            AtlasRunWorkflowResultStatus status,
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
            Exception exception)
        {
            if (status == AtlasRunWorkflowResultStatus.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Atlas run workflow result status must be Succeeded or Failed.");
            }

            if (phase == AtlasRunWorkflowPhase.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phase),
                    phase,
                    "Atlas run workflow result phase must be concrete.");
            }

            if (artifactFilePath != null && !HasPath(artifactFilePath))
            {
                throw new ArgumentException(
                    "Artifact file path must not be empty or whitespace when supplied.",
                    nameof(artifactFilePath));
            }

            if (debugMapFilePath != null && !HasPath(debugMapFilePath))
            {
                throw new ArgumentException(
                    "Debug-map file path must not be empty or whitespace when supplied.",
                    nameof(debugMapFilePath));
            }

            if (status == AtlasRunWorkflowResultStatus.Succeeded)
            {
                ValidateSuccessfulResultOrThrow(
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
                    exception);

                return;
            }

            ValidateFailedResultOrThrow(
                phase,
                compilation,
                execution,
                artifact,
                debugMapImage);
        }

        private static void ValidateSuccessfulResultOrThrow(
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
            Exception exception)
        {
            if (phase != AtlasRunWorkflowPhase.Completed)
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow results must use the Completed phase.",
                    nameof(phase));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (compilation.Failed)
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow results cannot contain failed compilation.",
                    nameof(compilation));
            }

            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            if (execution.Failed)
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow results cannot contain failed execution.",
                    nameof(execution));
            }

            if (request.RequiresArtifactCapture && artifact == null)
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow result requires an artifact because the request requires artifact capture.",
                    nameof(artifact));
            }

            if (request.HasDebugMap && debugMapImage == null)
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow result requires a debug-map image because the request contains a debug-map export.",
                    nameof(debugMapImage));
            }

            if (request.HasArtifactFilePath && !HasPath(artifactFilePath))
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow result requires the artifact file path written by the workflow.",
                    nameof(artifactFilePath));
            }

            if (request.HasDebugMap && !HasPath(debugMapFilePath))
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow result requires the debug-map file path written by the workflow.",
                    nameof(debugMapFilePath));
            }

            if (exception != null)
            {
                throw new ArgumentException(
                    "Successful Atlas run workflow results must not contain an exception.",
                    nameof(exception));
            }
        }

        private static void ValidateFailedResultOrThrow(
            AtlasRunWorkflowPhase phase,
            AtlasCompilationResult compilation,
            AtlasExecutionResult execution,
            AtlasArtifact artifact,
            AtlasDebugMapImage debugMapImage)
        {
            if (phase == AtlasRunWorkflowPhase.Completed)
            {
                throw new ArgumentException(
                    "Failed Atlas run workflow results must report the failing phase, not Completed.",
                    nameof(phase));
            }

            if (phase == AtlasRunWorkflowPhase.Compilation &&
                compilation != null &&
                compilation.Succeeded)
            {
                throw new ArgumentException(
                    "Compilation failure results cannot contain successful compilation.",
                    nameof(compilation));
            }

            if (phase == AtlasRunWorkflowPhase.Execution &&
                execution != null &&
                execution.Succeeded)
            {
                throw new ArgumentException(
                    "Execution failure results cannot contain successful execution.",
                    nameof(execution));
            }

            if (phase < AtlasRunWorkflowPhase.ArtifactCapture &&
                artifact != null)
            {
                throw new ArgumentException(
                    "Failure results before artifact capture must not contain an artifact.",
                    nameof(artifact));
            }

            if (phase < AtlasRunWorkflowPhase.DebugMapExport &&
                debugMapImage != null)
            {
                throw new ArgumentException(
                    "Failure results before debug-map export must not contain a debug-map image.",
                    nameof(debugMapImage));
            }
        }

        private static FixedString512Bytes CreateFailureMessage(
            AtlasRunWorkflowPhase phase,
            string message,
            Exception exception)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                return CreateMessage(message);
            }

            if (exception != null && !string.IsNullOrWhiteSpace(exception.Message))
            {
                return CreateMessage(exception.Message);
            }

            return CreateMessage(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas run workflow failed in phase '{0}'.",
                    phase));
        }

        private static FixedString512Bytes CreateMessage(
            string message)
        {
            var fixedMessage = new FixedString512Bytes();

            if (!string.IsNullOrEmpty(message))
            {
                fixedMessage.Append(message);
            }

            return fixedMessage;
        }

        private static bool HasPath(
            string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath);
        }
    }
}