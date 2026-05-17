// Packages/com.lokrain.atlas/Runtime/Execution/AtlasOperationRunner.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Execute a compiled Atlas plan through registered managed operation executors.
// - Chain JobHandle dependencies in compiled operation order.
// - Provide throwing and result-returning execution APIs.
// - Keep executor dispatch separate from compilation, workspace allocation, artifacts, and debug rendering.
//
// Design notes
// - The runner does not own the workspace.
// - The runner does not dispose the workspace.
// - The runner does not allocate field memory.
// - The runner does not resolve symbolic Field ids for jobs.
// - The runner dispatches by durable AtlasOperationId.
// - Executors resolve compiled bindings through AtlasExecutionContext.
// - Jobs receive typed native views and blittable parameters, not this runner.
// - Result-returning APIs are for orchestration/debug UI.
// - Throwing APIs remain the strict internal/runtime path.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Executors;
using Lokrain.Atlas.Operations;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Runs compiled Atlas operation occurrences through a registered executor table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasOperationRunner"/> is the managed execution dispatcher for a compiled
    /// Atlas plan. It walks flattened compiled operation occurrences in deterministic compiled
    /// order, resolves each operation id through <see cref="AtlasOperationExecutorRegistry"/>,
    /// and chains job dependencies through each executor.
    /// </para>
    ///
    /// <para>
    /// The runner is intentionally not a scheduler graph optimizer yet. It performs linear
    /// dependency chaining because the current compiler validates dataflow and write hazards but
    /// does not yet emit a parallel executable dependency graph.
    /// </para>
    ///
    /// <para>
    /// This type does not allocate or dispose workspace memory. Callers own the
    /// <see cref="AtlasWorkspace"/> lifetime through <see cref="AtlasExecutionContext"/>.
    /// </para>
    /// </remarks>
    public sealed class AtlasOperationRunner
    {
        /// <summary>
        /// Executor registry used for durable operation-id dispatch.
        /// </summary>
        public readonly AtlasOperationExecutorRegistry Registry;

        private AtlasOperationRunner(
            AtlasOperationExecutorRegistry registry)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Gets whether this runner has no registered executors.
        /// </summary>
        public bool IsEmpty => Registry.IsEmpty;

        /// <summary>
        /// Gets the number of registered executors.
        /// </summary>
        public int ExecutorCount => Registry.Count;

        /// <summary>
        /// Creates an operation runner from an immutable executor registry.
        /// </summary>
        /// <param name="registry">Executor registry.</param>
        /// <returns>A runner using <paramref name="registry"/>.</returns>
        public static AtlasOperationRunner Create(
            AtlasOperationExecutorRegistry registry)
        {
            return new AtlasOperationRunner(registry);
        }

        /// <summary>
        /// Creates an operation runner from explicitly ordered executors.
        /// </summary>
        /// <param name="executors">Executor instances in deterministic registration order.</param>
        /// <returns>A runner using a new immutable executor registry.</returns>
        public static AtlasOperationRunner Create(
            params IAtlasOperationExecutor[] executors)
        {
            return new AtlasOperationRunner(
                AtlasOperationExecutorRegistry.Create(executors));
        }

        /// <summary>
        /// Executes every compiled operation occurrence in flattened compiled order.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="inputDeps">Initial dependency handle.</param>
        /// <returns>The final dependency handle for the full operation chain.</returns>
        public JobHandle ExecuteAll(
            AtlasExecutionContext context,
            JobHandle inputDeps = default)
        {
            ValidateContextOrThrow(context);

            return ExecuteRange(
                context,
                startFlattenedOperationIndex: 0,
                operationCount: context.OperationCount,
                inputDeps);
        }

        /// <summary>
        /// Executes every compiled operation occurrence and completes the final dependency handle.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="inputDeps">Initial dependency handle.</param>
        public void ExecuteAllAndComplete(
            AtlasExecutionContext context,
            JobHandle inputDeps = default)
        {
            ExecuteAll(
                    context,
                    inputDeps)
                .Complete();
        }

        /// <summary>
        /// Executes every compiled operation occurrence and returns managed scheduling status.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="inputDeps">Initial dependency handle.</param>
        /// <returns>An execution result containing the final dependency handle or failure information.</returns>
        public AtlasExecutionResult TryExecuteAll(
            AtlasExecutionContext context,
            JobHandle inputDeps = default)
        {
            return TryExecuteRange(
                context,
                startFlattenedOperationIndex: 0,
                operationCount: context?.OperationCount ?? 0,
                inputDeps);
        }

        /// <summary>
        /// Executes every compiled operation occurrence, completes the final dependency handle,
        /// and returns managed status.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="inputDeps">Initial dependency handle.</param>
        /// <returns>A completed execution result or failure information.</returns>
        public AtlasExecutionResult TryExecuteAllAndComplete(
            AtlasExecutionContext context,
            JobHandle inputDeps = default)
        {
            var result = TryExecuteAll(
                context,
                inputDeps);

            if (result.Failed)
            {
                return result;
            }

            try
            {
                result.FinalDependency.Complete();

                return AtlasExecutionResult.Completed(
                    result.ScheduledOperationCount);
            }
            catch (Exception exception)
            {
                return CreateCompletionFailureResult(
                    context,
                    result.ScheduledOperationCount,
                    exception);
            }
        }

        /// <summary>
        /// Executes a contiguous flattened operation range.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="startFlattenedOperationIndex">First flattened operation index.</param>
        /// <param name="operationCount">Number of operations to execute.</param>
        /// <param name="inputDeps">Initial dependency handle.</param>
        /// <returns>The final dependency handle for the executed operation range.</returns>
        public JobHandle ExecuteRange(
            AtlasExecutionContext context,
            int startFlattenedOperationIndex,
            int operationCount,
            JobHandle inputDeps = default)
        {
            ValidateContextOrThrow(context);
            ValidateOperationRangeOrThrow(
                context,
                startFlattenedOperationIndex,
                operationCount);

            var dependencies = inputDeps;
            var endExclusive = startFlattenedOperationIndex + operationCount;

            for (var i = startFlattenedOperationIndex; i < endExclusive; i++)
            {
                dependencies = ExecuteOne(
                    context,
                    i,
                    dependencies);
            }

            return dependencies;
        }

        /// <summary>
        /// Executes a contiguous flattened operation range and returns managed scheduling status.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="startFlattenedOperationIndex">First flattened operation index.</param>
        /// <param name="operationCount">Number of operations to execute.</param>
        /// <param name="inputDeps">Initial dependency handle.</param>
        /// <returns>An execution result containing the final dependency handle or failure information.</returns>
        public AtlasExecutionResult TryExecuteRange(
            AtlasExecutionContext context,
            int startFlattenedOperationIndex,
            int operationCount,
            JobHandle inputDeps = default)
        {
            try
            {
                ValidateContextOrThrow(context);
                ValidateOperationRangeOrThrow(
                    context,
                    startFlattenedOperationIndex,
                    operationCount);
            }
            catch (Exception exception)
            {
                return AtlasExecutionResult.FailedSetup(
                    exception.Message,
                    exception);
            }

            var dependencies = inputDeps;
            var scheduledCount = 0;
            var endExclusive = startFlattenedOperationIndex + operationCount;

            for (var i = startFlattenedOperationIndex; i < endExclusive; i++)
            {
                AtlasCompiledOperation operation = null;

                try
                {
                    if (!context.TryGetOperation(
                            i,
                            out var stageIndex,
                            out var stageOperationIndex,
                            out operation))
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(i),
                            i,
                            $"Flattened operation index must be between 0 and {context.OperationCount - 1}.");
                    }

                    dependencies = ExecuteOperation(
                        context,
                        stageIndex,
                        stageOperationIndex,
                        i,
                        operation,
                        dependencies);

                    scheduledCount++;
                }
                catch (Exception exception)
                {
                    return AtlasExecutionResult.FailedResult(
                        scheduledCount,
                        i,
                        operation?.OperationId ?? default,
                        operation?.DebugName ?? default,
                        exception.Message,
                        exception);
                }
            }

            return AtlasExecutionResult.Scheduled(
                dependencies,
                scheduledCount);
        }

        /// <summary>
        /// Executes one flattened compiled operation occurrence.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="flattenedOperationIndex">Zero-based flattened operation index.</param>
        /// <param name="inputDeps">Dependency handle that the operation must depend on.</param>
        /// <returns>The dependency handle returned by the operation executor.</returns>
        public JobHandle ExecuteOne(
            AtlasExecutionContext context,
            int flattenedOperationIndex,
            JobHandle inputDeps = default)
        {
            ValidateContextOrThrow(context);

            if (!context.TryGetOperation(
                    flattenedOperationIndex,
                    out var stageIndex,
                    out var operationIndex,
                    out var operation))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(flattenedOperationIndex),
                    flattenedOperationIndex,
                    $"Flattened operation index must be between 0 and {context.OperationCount - 1}.");
            }

            return ExecuteOperation(
                context,
                stageIndex,
                operationIndex,
                flattenedOperationIndex,
                operation,
                inputDeps);
        }

        /// <summary>
        /// Executes one flattened compiled operation occurrence and returns managed scheduling status.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="flattenedOperationIndex">Zero-based flattened operation index.</param>
        /// <param name="inputDeps">Dependency handle that the operation must depend on.</param>
        /// <returns>An execution result containing the final dependency handle or failure information.</returns>
        public AtlasExecutionResult TryExecuteOne(
            AtlasExecutionContext context,
            int flattenedOperationIndex,
            JobHandle inputDeps = default)
        {
            return TryExecuteRange(
                context,
                flattenedOperationIndex,
                operationCount: 1,
                inputDeps);
        }

        /// <summary>
        /// Executes one compiled operation occurrence.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="operation">Compiled operation occurrence.</param>
        /// <param name="inputDeps">Dependency handle that the operation must depend on.</param>
        /// <returns>The dependency handle returned by the operation executor.</returns>
        public JobHandle ExecuteOperation(
            AtlasExecutionContext context,
            AtlasCompiledOperation operation,
            JobHandle inputDeps = default)
        {
            ValidateContextOrThrow(context);
            ValidateOperationOrThrow(operation);

            return ExecuteOperation(
                context,
                stageIndex: -1,
                operationIndex: operation.OperationIndex,
                flattenedOperationIndex: -1,
                operation,
                inputDeps);
        }

        /// <summary>
        /// Executes one compiled operation occurrence and returns managed scheduling status.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="operation">Compiled operation occurrence.</param>
        /// <param name="inputDeps">Dependency handle that the operation must depend on.</param>
        /// <returns>An execution result containing the final dependency handle or failure information.</returns>
        public AtlasExecutionResult TryExecuteOperation(
            AtlasExecutionContext context,
            AtlasCompiledOperation operation,
            JobHandle inputDeps = default)
        {
            try
            {
                ValidateContextOrThrow(context);
                ValidateOperationOrThrow(operation);

                var dependency = ExecuteOperation(
                    context,
                    stageIndex: -1,
                    operationIndex: operation.OperationIndex,
                    flattenedOperationIndex: -1,
                    operation,
                    inputDeps);

                return AtlasExecutionResult.Scheduled(
                    dependency,
                    scheduledOperationCount: 1);
            }
            catch (Exception exception)
            {
                return AtlasExecutionResult.FailedResult(
                    scheduledOperationCount: 0,
                    failedFlattenedOperationIndex: 0,
                    failedOperationId: operation?.OperationId ?? default,
                    failedOperationName: operation?.DebugName ?? default,
                    message: exception.Message,
                    exception);
            }
        }

        /// <summary>
        /// Validates that every operation in the context has a registered executor.
        /// </summary>
        /// <param name="context">Execution context to validate.</param>
        public void ValidateCanExecuteAllOrThrow(
            AtlasExecutionContext context)
        {
            ValidateContextOrThrow(context);

            for (var i = 0; i < context.OperationCount; i++)
            {
                var operation = context.GetRequiredOperation(i);

                if (!Registry.ContainsExecutor(operation.OperationId))
                {
                    throw CreateMissingExecutorException(
                        operation.OperationId,
                        operation.DebugName,
                        flattenedOperationIndex: i);
                }
            }
        }

        /// <summary>
        /// Attempts to find the first flattened operation index whose operation id has no registered executor.
        /// </summary>
        /// <param name="context">Execution context to inspect.</param>
        /// <param name="flattenedOperationIndex">First missing executor operation index; otherwise, -1.</param>
        /// <param name="operationId">Missing operation id; otherwise, default payload.</param>
        /// <returns><c>true</c> when a missing executor was found; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstMissingExecutor(
            AtlasExecutionContext context,
            out int flattenedOperationIndex,
            out AtlasOperationId operationId)
        {
            ValidateContextOrThrow(context);

            for (var i = 0; i < context.OperationCount; i++)
            {
                var operation = context.GetRequiredOperation(i);

                if (!Registry.ContainsExecutor(operation.OperationId))
                {
                    flattenedOperationIndex = i;
                    operationId = operation.OperationId;
                    return true;
                }
            }

            flattenedOperationIndex = -1;
            operationId = default;
            return false;
        }

        /// <summary>
        /// Returns a diagnostic representation of this runner.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return $"AtlasOperationRunner(Executors={ExecutorCount})";
        }

        private JobHandle ExecuteOperation(
            AtlasExecutionContext context,
            int stageIndex,
            int operationIndex,
            int flattenedOperationIndex,
            AtlasCompiledOperation operation,
            JobHandle inputDeps)
        {
            ValidateOperationOrThrow(operation);

            if (!Registry.TryGetExecutor(operation.OperationId, out var executor))
            {
                throw CreateMissingExecutorException(
                    operation.OperationId,
                    operation.DebugName,
                    flattenedOperationIndex);
            }

            if (executor.OperationId != operation.OperationId)
            {
                throw new InvalidOperationException(
                    $"Atlas operation executor registry returned executor '{executor.DebugName}' for compiled operation '{operation.DebugName}', " +
                    $"but executor id '{executor.OperationId}' does not match operation id '{operation.OperationId}'.");
            }

            try
            {
                return executor.Execute(
                    context,
                    operation,
                    inputDeps);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    FormatExecutionFailureMessage(
                        executor,
                        operation,
                        stageIndex,
                        operationIndex,
                        flattenedOperationIndex),
                    exception);
            }
        }

        private static void ValidateContextOrThrow(
            AtlasExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Plan == null)
            {
                throw new ArgumentException(
                    "Atlas execution context does not reference a compiled plan.",
                    nameof(context));
            }

            if (context.Workspace == null)
            {
                throw new ArgumentException(
                    "Atlas execution context does not reference a workspace.",
                    nameof(context));
            }

            context.Workspace.ThrowIfDisposed();
        }

        private static void ValidateOperationOrThrow(
            AtlasCompiledOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            operation.OperationId.ValidateOrThrow(nameof(operation.OperationId));
        }

        private static void ValidateOperationRangeOrThrow(
            AtlasExecutionContext context,
            int startFlattenedOperationIndex,
            int operationCount)
        {
            if (startFlattenedOperationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startFlattenedOperationIndex),
                    startFlattenedOperationIndex,
                    "Start operation index must be non-negative.");
            }

            if (operationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(operationCount),
                    operationCount,
                    "Operation count must be non-negative.");
            }

            if (operationCount == 0)
            {
                if (startFlattenedOperationIndex <= context.OperationCount)
                {
                    return;
                }

                throw new ArgumentOutOfRangeException(
                    nameof(startFlattenedOperationIndex),
                    startFlattenedOperationIndex,
                    $"Start operation index must be between 0 and {context.OperationCount} for an empty range.");
            }

            var endExclusive = checked(startFlattenedOperationIndex + operationCount);

            if (endExclusive > context.OperationCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(operationCount),
                    operationCount,
                    $"Operation range [{startFlattenedOperationIndex}, {endExclusive}) exceeds compiled operation count '{context.OperationCount}'.");
            }
        }

        private static Exception CreateMissingExecutorException(
            AtlasOperationId operationId,
            FixedString64Bytes operationName,
            int flattenedOperationIndex)
        {
            var location =
                flattenedOperationIndex >= 0
                    ? $" at flattened operation index '{flattenedOperationIndex}'"
                    : string.Empty;

            return new InvalidOperationException(
                $"No Atlas operation executor is registered for operation '{operationName}' with id '{operationId}'{location}.");
        }

        private static AtlasExecutionResult CreateCompletionFailureResult(
            AtlasExecutionContext context,
            int scheduledOperationCount,
            Exception exception)
        {
            if (context == null ||
                scheduledOperationCount <= 0 ||
                context.OperationCount <= 0)
            {
                return AtlasExecutionResult.FailedSetup(
                    exception.Message,
                    exception);
            }

            var lastIndex = Math.Min(
                scheduledOperationCount - 1,
                context.OperationCount - 1);

            AtlasCompiledOperation operation = null;

            try
            {
                operation = context.GetRequiredOperation(lastIndex);
            }
            catch
            {
                return AtlasExecutionResult.FailedSetup(
                    exception.Message,
                    exception);
            }

            return AtlasExecutionResult.FailedResult(
                scheduledOperationCount,
                lastIndex,
                operation.OperationId,
                operation.DebugName,
                exception.Message,
                exception);
        }

        private static string FormatExecutionFailureMessage(
            IAtlasOperationExecutor executor,
            AtlasCompiledOperation operation,
            int stageIndex,
            int operationIndex,
            int flattenedOperationIndex)
        {
            var location =
                flattenedOperationIndex >= 0
                    ? $"flattened index {flattenedOperationIndex}, stage index {stageIndex}, operation index {operationIndex}"
                    : $"operation index {operationIndex}";

            return
                $"Atlas operation executor '{executor.DebugName}' failed while executing compiled operation '{operation.DebugName}' ({location}, id {operation.OperationId}).";
        }
    }
}