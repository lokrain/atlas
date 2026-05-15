// Packages/com.lokrain.atlas/Runtime/Execution/AtlasRunWorkflowPhase.cs
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

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Workflow phase associated with an Atlas run result.
    /// </summary>
    public enum AtlasRunWorkflowPhase : byte
    {
        /// <summary>
        /// No workflow phase is available.
        /// </summary>
        None = 0,

        /// <summary>
        /// Request validation failed before compilation.
        /// </summary>
        RequestValidation = 1,

        /// <summary>
        /// Pipeline compilation failed.
        /// </summary>
        Compilation = 2,

        /// <summary>
        /// Shape resolution failed.
        /// </summary>
        ShapeResolution = 3,

        /// <summary>
        /// Workspace allocation failed.
        /// </summary>
        WorkspaceAllocation = 4,

        /// <summary>
        /// Execution context creation failed.
        /// </summary>
        ExecutionContextCreation = 5,

        /// <summary>
        /// Operation dispatch, scheduling, or completion failed.
        /// </summary>
        Execution = 6,

        /// <summary>
        /// Artifact capture failed.
        /// </summary>
        ArtifactCapture = 7,

        /// <summary>
        /// Artifact file writing failed.
        /// </summary>
        ArtifactWrite = 8,

        /// <summary>
        /// Debug-map export failed.
        /// </summary>
        DebugMapExport = 9,

        /// <summary>
        /// Workflow completed successfully.
        /// </summary>
        Completed = 10
    }
}