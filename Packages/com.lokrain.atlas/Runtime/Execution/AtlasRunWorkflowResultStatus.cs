// Packages/com.lokrain.atlas/Runtime/Execution/AtlasRunWorkflowResultStatus.cs
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
    /// Terminal status of an Atlas run workflow.
    /// </summary>
    public enum AtlasRunWorkflowResultStatus : byte
    {
        /// <summary>
        /// No workflow result has been produced.
        /// </summary>
        None = 0,

        /// <summary>
        /// The workflow completed successfully.
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// The workflow failed in a reported phase.
        /// </summary>
        Failed = 2
    }
}