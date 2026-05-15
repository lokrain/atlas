// Packages/com.lokrain.atlas/Runtime/Executors/AtlasClearFieldsOperationExecutor.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Executors
//
// Purpose
// - Execute a clear-fields operation over compiled writable bindings.
// - Clear workspace-owned byte capacity for each eligible field binding.
// - Preserve JobHandle dependency chaining.
// - Keep operation identity supplied by catalog/definition code instead of hard-coding it here.
//
// Design notes
// - This is a concrete managed operation executor.
// - The executor is registered by durable AtlasOperationId.
// - The executor clears only present content-memory bindings.
// - The executor rejects content-reading bindings.
// - The executor rejects shape-only bindings for clearing.
// - The executor schedules byte-clear jobs over workspace-owned NativeArray<byte> buffers.
// - The executor does not allocate workspace memory.
// - The executor does not dispose workspace memory.
// - The executor does not write artifacts.
// - The executor does not render debug output.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Operations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Executors
{
    /// <summary>
    /// Clears all writable field buffers declared by one compiled clear operation occurrence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasClearFieldsOperationExecutor"/> is intentionally parameterized by
    /// <see cref="AtlasOperationId"/>. The operation catalog or caller supplies the durable
    /// operation identity. This avoids creating a second source of truth inside runtime executor
    /// code.
    /// </para>
    ///
    /// <para>
    /// The operation definition should declare only write bindings with
    /// <see cref="AtlasOperationAccessFlags.DiscardBeforeWrite"/>. Previous field contents are
    /// semantically discarded and the executor clears the full byte capacity of each target block.
    /// </para>
    ///
    /// <para>
    /// Clearing full capacity, not only logical length, is intentional. Capacity slack is still
    /// workspace-owned memory and must not leak stale bytes into later diagnostics, artifacts,
    /// hashes, debug views, or widened runtime slices.
    /// </para>
    /// </remarks>
    public sealed class AtlasClearFieldsOperationExecutor : IAtlasOperationExecutor
    {
        private readonly int _innerloopBatchCount;

        /// <summary>
        /// Durable operation id handled by this executor instance.
        /// </summary>
        public AtlasOperationId OperationId { get; }

        /// <summary>
        /// Stable diagnostic executor name.
        /// </summary>
        public FixedString64Bytes DebugName { get; }

        /// <summary>
        /// Creates a clear-fields executor for one durable operation identity.
        /// </summary>
        /// <param name="operationId">Durable operation id to execute.</param>
        /// <param name="debugName">Diagnostic executor name.</param>
        /// <param name="innerloopBatchCount">Batch count used by byte-clear jobs.</param>
        public AtlasClearFieldsOperationExecutor(
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            int innerloopBatchCount = 256)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Clear-fields executor debug name must not be empty.",
                    nameof(debugName));
            }

            if (innerloopBatchCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(innerloopBatchCount),
                    innerloopBatchCount,
                    "Inner-loop batch count must be positive.");
            }

            OperationId = operationId;
            DebugName = debugName;
            _innerloopBatchCount = innerloopBatchCount;
        }

        /// <summary>
        /// Executes one compiled clear operation occurrence.
        /// </summary>
        /// <param name="context">Execution context binding the compiled plan to workspace memory.</param>
        /// <param name="operation">Compiled clear operation occurrence.</param>
        /// <param name="inputDeps">Dependency handle that clear work must depend on.</param>
        /// <returns>The dependency handle representing all scheduled clear jobs.</returns>
        public JobHandle Execute(
            AtlasExecutionContext context,
            AtlasCompiledOperation operation,
            JobHandle inputDeps)
        {
            ValidateInputsOrThrow(
                context,
                operation);

            var dependencies = inputDeps;
            var scheduledCount = 0;

            for (var i = 0; i < operation.Count; i++)
            {
                var binding = operation[i];

                if (!binding.IsPresent)
                {
                    continue;
                }

                ValidateClearBindingOrThrow(
                    operation,
                    binding);

                if (HasEarlierPresentBindingForSameSlot(
                        operation,
                        binding,
                        i))
                {
                    continue;
                }

                var block = context.GetRequiredBlock(binding);
                var bytes = block.GetByteCapacityArray();

                if (!bytes.IsCreated ||
                    bytes.Length == 0)
                {
                    continue;
                }

                dependencies = new ClearByteCapacityJob
                {
                    Bytes = bytes
                }.Schedule(
                    bytes.Length,
                    _innerloopBatchCount,
                    dependencies);

                scheduledCount++;
            }

            if (scheduledCount == 0)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' did not schedule any clear work. A clear operation must contain at least one present content-memory write binding.");
            }

            return dependencies;
        }

        private void ValidateInputsOrThrow(
            AtlasExecutionContext context,
            AtlasCompiledOperation operation)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            operation.OperationId.ValidateOrThrow(nameof(operation.OperationId));

            if (operation.OperationId != OperationId)
            {
                throw new InvalidOperationException(
                    $"Executor '{DebugName}' handles operation id '{OperationId}', but compiled operation '{operation.DebugName}' has id '{operation.OperationId}'.");
            }

            if (operation.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' has no bindings.");
            }

            if (!operation.RequiresContentMemory)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' has no present content-memory bindings.");
            }

            if (operation.ReadsContent ||
                operation.DeclaresContentRead)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' declares content reads. Clear operations must only discard-write target fields.");
            }

            if (!operation.DeclaresContentWrite)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' declares no content writes.");
            }

            context.Workspace.ThrowIfDisposed();
        }

        private static void ValidateClearBindingOrThrow(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding)
        {
            binding.ValidateOrThrow(nameof(binding));

            if (!binding.IsPresent)
            {
                return;
            }

            if (binding.IsShapeOnly)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' is shape-only and cannot be cleared.");
            }

            if (!binding.RequiresContentMemory)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' does not require content memory and cannot be cleared.");
            }

            if (binding.ReadsContent ||
                binding.DeclaresContentRead)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' reads content. Clear bindings must not read previous field contents.");
            }

            if (!binding.WritesContent ||
                !binding.DeclaresContentWrite)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' does not write content. Clear bindings must be writable.");
            }

            if (binding.Mode != AtlasOperationAccessMode.Write)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' uses access mode '{binding.Mode}'. Clear bindings must use '{AtlasOperationAccessMode.Write}'.");
            }

            if (!binding.Flags.HasAll(AtlasOperationAccessFlags.DiscardBeforeWrite))
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' does not declare '{AtlasOperationAccessFlags.DiscardBeforeWrite}'. Clear operations must explicitly discard previous contents.");
            }

            if (binding.WriteCoverage != AtlasWriteCoverage.FullCapacity)
            {
                throw new InvalidOperationException(
                    $"Clear operation '{operation.DebugName}' binding '{binding.BindingName}' declares write coverage '{binding.WriteCoverage}'. Clear operations must declare '{AtlasWriteCoverage.FullCapacity}' because the executor clears full byte capacity.");
            }
        }

        private static bool HasEarlierPresentBindingForSameSlot(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            int currentIndex)
        {
            var slot = binding.Contract.Slot;

            for (var i = 0; i < currentIndex; i++)
            {
                var earlier = operation[i];

                if (!earlier.IsPresent)
                {
                    continue;
                }

                if (earlier.Contract.Slot == slot)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private struct ClearByteCapacityJob : IJobParallelFor
        {
            public NativeArray<byte> Bytes;

            public void Execute(int index)
            {
                Bytes[index] = 0;
            }
        }
    }
}