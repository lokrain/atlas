// Packages/com.lokrain.atlas/Runtime/Executors/AtlasDefaultOperationExecutorRegistry.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Executors
//
// Purpose
// - Build the package-owned default executor registry from authored operation metadata.
// - Register built-in executors without hard-coding durable operation ids in executor code.
// - Keep executor discovery separate from compiled plans, workspace memory, jobs, artifacts, and debug rendering.
// - Make AtlasRunWorkflow usable for pipelines that contain currently supported built-in operation shapes.
//
// Design notes
// - Operation ids remain supplied by operation definitions.
// - The default registry does not invent operation identities.
// - Executor selection is deliberately conservative.
// - Unsupported operations are ignored here and remain normal missing-executor failures at execution time.
// - Clear-field executor registration requires WorkspacePreparation role and write-only full-overwrite/discard operation contracts.
// - Repeated operation occurrences produce one executor registration per durable operation id.

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Unity.Collections;

namespace Lokrain.Atlas.Executors
{
    /// <summary>
    /// Builds package-owned executor registries for built-in Atlas operation executors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDefaultOperationExecutorRegistry"/> is the managed bridge between authored
    /// operation contracts and built-in executor instances. It does not own durable operation ids;
    /// those ids are read from <see cref="AtlasOperationDefinition"/>. This keeps operation identity
    /// in the catalog/authoring layer while still allowing callers to create a usable runtime
    /// registry from a pipeline or operation sequence.
    /// </para>
    ///
    /// <para>
    /// The factory is intentionally conservative. It only registers executors for operation shapes
    /// that this package can execute safely today. Unsupported operation contracts are ignored and
    /// will still fail through normal missing-executor diagnostics when a plan containing them is
    /// executed without a custom executor.
    /// </para>
    /// </remarks>
    public static class AtlasDefaultOperationExecutorRegistry
    {
        /// <summary>
        /// Default batch count used by built-in clear-field jobs.
        /// </summary>
        public const int DefaultClearFieldsInnerloopBatchCount = 256;

        private static readonly FixedString64Bytes ClearFieldsExecutorName =
            new("atlas.executor.clear-fields");

        /// <summary>
        /// Creates a default executor registry for all supported operation definitions in a pipeline.
        /// </summary>
        /// <param name="pipeline">Pipeline whose authored operation definitions should be inspected.</param>
        /// <param name="clearFieldsInnerloopBatchCount">Batch count used by clear-field jobs.</param>
        /// <returns>An immutable executor registry containing supported built-in executors.</returns>
        public static AtlasOperationExecutorRegistry Create(
            AtlasPipelineDefinition pipeline,
            int clearFieldsInnerloopBatchCount = DefaultClearFieldsInnerloopBatchCount)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            ValidateClearFieldsBatchCountOrThrow(clearFieldsInnerloopBatchCount);

            var executors = new List<IAtlasOperationExecutor>();
            var registeredOperationIds = new HashSet<AtlasOperationId>();

            for (var stageIndex = 0; stageIndex < pipeline.Count; stageIndex++)
            {
                var stage = pipeline[stageIndex];

                for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
                {
                    TryAddDefaultExecutor(
                        stage[operationIndex],
                        clearFieldsInnerloopBatchCount,
                        registeredOperationIds,
                        executors);
                }
            }

            return AtlasOperationExecutorRegistry.Create(executors);
        }

        /// <summary>
        /// Creates a default executor registry for all supported operation definitions in an operation catalog.
        /// </summary>
        /// <param name="catalog">Operation catalog whose unique operation definitions should be inspected.</param>
        /// <param name="clearFieldsInnerloopBatchCount">Batch count used by clear-field jobs.</param>
        /// <returns>An immutable executor registry containing supported built-in executors.</returns>
        public static AtlasOperationExecutorRegistry Create(
            AtlasOperationCatalog catalog,
            int clearFieldsInnerloopBatchCount = DefaultClearFieldsInnerloopBatchCount)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            ValidateClearFieldsBatchCountOrThrow(clearFieldsInnerloopBatchCount);

            var executors = new List<IAtlasOperationExecutor>();
            var registeredOperationIds = new HashSet<AtlasOperationId>();

            for (var i = 0; i < catalog.Count; i++)
            {
                TryAddDefaultExecutor(
                    catalog[i],
                    clearFieldsInnerloopBatchCount,
                    registeredOperationIds,
                    executors);
            }

            return AtlasOperationExecutorRegistry.Create(executors);
        }

        /// <summary>
        /// Creates a default executor registry for all supported operation definitions in an operation set.
        /// </summary>
        /// <param name="operations">Operation definitions to inspect in deterministic order.</param>
        /// <param name="clearFieldsInnerloopBatchCount">Batch count used by clear-field jobs.</param>
        /// <returns>An immutable executor registry containing supported built-in executors.</returns>
        public static AtlasOperationExecutorRegistry Create(
            AtlasOperationSet operations,
            int clearFieldsInnerloopBatchCount = DefaultClearFieldsInnerloopBatchCount)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            ValidateClearFieldsBatchCountOrThrow(clearFieldsInnerloopBatchCount);

            var executors = new List<IAtlasOperationExecutor>();
            var registeredOperationIds = new HashSet<AtlasOperationId>();

            for (var i = 0; i < operations.Count; i++)
            {
                TryAddDefaultExecutor(
                    operations[i],
                    clearFieldsInnerloopBatchCount,
                    registeredOperationIds,
                    executors);
            }

            return AtlasOperationExecutorRegistry.Create(executors);
        }

        /// <summary>
        /// Creates a default executor registry for all supported operation definitions in an enumerable source.
        /// </summary>
        /// <param name="operations">Operation definitions to inspect in deterministic enumeration order.</param>
        /// <param name="clearFieldsInnerloopBatchCount">Batch count used by clear-field jobs.</param>
        /// <returns>An immutable executor registry containing supported built-in executors.</returns>
        public static AtlasOperationExecutorRegistry Create(
            IEnumerable<AtlasOperationDefinition> operations,
            int clearFieldsInnerloopBatchCount = DefaultClearFieldsInnerloopBatchCount)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            ValidateClearFieldsBatchCountOrThrow(clearFieldsInnerloopBatchCount);

            var executors = new List<IAtlasOperationExecutor>();
            var registeredOperationIds = new HashSet<AtlasOperationId>();

            foreach (var operation in operations)
            {
                if (operation == null)
                {
                    throw new ArgumentException(
                        "Default executor registry creation does not accept null operation definitions.",
                        nameof(operations));
                }

                TryAddDefaultExecutor(
                    operation,
                    clearFieldsInnerloopBatchCount,
                    registeredOperationIds,
                    executors);
            }

            return AtlasOperationExecutorRegistry.Create(executors);
        }

        /// <summary>
        /// Gets whether the package can create a built-in executor for the supplied operation definition.
        /// </summary>
        /// <param name="operation">Operation definition to inspect.</param>
        /// <returns><c>true</c> when a built-in executor can be created; otherwise, <c>false</c>.</returns>
        public static bool CanCreateExecutor(
            AtlasOperationDefinition operation)
        {
            if (operation == null)
            {
                return false;
            }

            return IsClearFieldsOperation(operation);
        }

        /// <summary>
        /// Gets whether an operation definition matches the supported clear-fields executor role and contract shape.
        /// </summary>
        /// <param name="operation">Operation definition to inspect.</param>
        /// <returns><c>true</c> when the clear-fields executor can execute this operation.</returns>
        public static bool IsClearFieldsOperation(
            AtlasOperationDefinition operation)
        {
            if (operation == null ||
                operation.Count == 0 ||
                operation.Role != AtlasOperationRole.WorkspacePreparation)
            {
                return false;
            }

            for (var i = 0; i < operation.Count; i++)
            {
                if (!IsClearFieldsAccess(operation[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsClearFieldsAccess(
            AtlasOperationAccess access)
        {
            return access.Mode == AtlasOperationAccessMode.Write &&
                   !access.BindingName.IsEmpty &&
                   !access.Flags.HasAny(AtlasOperationAccessFlags.ShapeOnly) &&
                   !access.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent) &&
                   !access.ReadsContent &&
                   access.WritesContent &&
                   access.Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite) &&
                   access.Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite) &&
                   access.WriteCoverage == AtlasWriteCoverage.FullCapacity;
        }

        private static void TryAddDefaultExecutor(
            AtlasOperationDefinition operation,
            int clearFieldsInnerloopBatchCount,
            HashSet<AtlasOperationId> registeredOperationIds,
            List<IAtlasOperationExecutor> executors)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (!CanCreateExecutor(operation))
            {
                return;
            }

            if (!registeredOperationIds.Add(operation.OperationId))
            {
                return;
            }

            executors.Add(CreateClearFieldsExecutor(
                operation,
                clearFieldsInnerloopBatchCount));
        }

        private static IAtlasOperationExecutor CreateClearFieldsExecutor(
            AtlasOperationDefinition operation,
            int clearFieldsInnerloopBatchCount)
        {
            return new AtlasClearFieldsOperationExecutor(
                operation.OperationId,
                CreateExecutorDebugName(operation),
                clearFieldsInnerloopBatchCount);
        }

        private static FixedString64Bytes CreateExecutorDebugName(
            AtlasOperationDefinition operation)
        {
            return operation.DebugName.IsEmpty
                ? ClearFieldsExecutorName
                : operation.DebugName;
        }

        private static void ValidateClearFieldsBatchCountOrThrow(
            int clearFieldsInnerloopBatchCount)
        {
            if (clearFieldsInnerloopBatchCount > 0)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(clearFieldsInnerloopBatchCount),
                clearFieldsInnerloopBatchCount,
                "Clear-fields inner-loop batch count must be positive.");
        }
    }
}
