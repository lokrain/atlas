// Packages/com.lokrain.atlas/Runtime/Executors/IAtlasOperationExecutor.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Executors
//
// Purpose
// - Define the managed execution contract for one compiled Atlas operation kind.
// - Bridge compiled operation metadata to Burst/job-friendly scheduler code.
// - Preserve dependency chaining through JobHandle.
// - Keep symbolic Field resolution out of jobs.
//
// Design notes
// - Executors are runtime adapters for compiled operation behavior.
// - Executors are not authored operation definitions.
// - Executors are not catalog entries.
// - Executors do not own workspace memory.
// - Executors do not allocate workspace memory.
// - Executors do not write durable artifacts.
// - Executors do not render debug output.
// - Jobs scheduled by executors should receive typed native views, numeric parameters,
//   dimensions, seeds, and other Burst-compatible payloads.

using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Operations;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Executors
{
    /// <summary>
    /// Managed executor for one Atlas operation contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IAtlasOperationExecutor"/> is the runtime seam between compiled Atlas
    /// operation metadata and concrete scheduler/job implementation. Implementations should use
    /// <see cref="AtlasExecutionContext"/> to resolve compiled bindings into typed native views,
    /// then pass those views into Burst-compatible jobs.
    /// </para>
    ///
    /// <para>
    /// Dispatch is deliberately by durable <see cref="AtlasOperationId"/>, not by C# generic type.
    /// Operation identity belongs to the authored/catalog/compiler model. Executor class names are
    /// implementation details and may change without changing durable operation identity.
    /// </para>
    ///
    /// <para>
    /// This interface returns a <see cref="JobHandle"/> because the production execution contract
    /// must support dependency chaining. A synchronous executor may complete internally and return
    /// <paramref name="inputDeps"/>, but asynchronous Burst/job execution is the normal path.
    /// </para>
    ///
    /// <para>
    /// Do not pass this interface, <see cref="AtlasExecutionContext"/>, or
    /// <see cref="AtlasCompiledOperation"/> into Burst jobs. Jobs should receive already-resolved
    /// native containers and blittable parameters only.
    /// </para>
    /// </remarks>
    public interface IAtlasOperationExecutor
    {
        /// <summary>
        /// Gets the durable operation contract identity supported by this executor.
        /// </summary>
        AtlasOperationId OperationId { get; }

        /// <summary>
        /// Gets a stable diagnostic executor name.
        /// </summary>
        FixedString64Bytes DebugName { get; }

        /// <summary>
        /// Executes or schedules one compiled operation occurrence.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace-owned memory.</param>
        /// <param name="operation">Compiled operation occurrence to execute.</param>
        /// <param name="inputDeps">Dependency handle that this operation must depend on.</param>
        /// <returns>The dependency handle representing this operation's scheduled work.</returns>
        /// <remarks>
        /// Implementations should validate that <paramref name="operation"/> matches
        /// <see cref="OperationId"/> before resolving bindings. The returned handle becomes the
        /// dependency input for the next operation when the runner executes operations linearly.
        /// </remarks>
        JobHandle Execute(
            AtlasExecutionContext context,
            AtlasCompiledOperation operation,
            JobHandle inputDeps);
    }
}