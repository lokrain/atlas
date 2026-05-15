// Runtime/Stages/AtlasStageDefinition.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Stages
//
// Purpose
// - Define one durable Atlas stage contract.
// - Bind stable stage identity and diagnostic stage name to an ordered operation sequence.
// - Preserve authored operation occurrence order exactly.
// - Keep stage contracts separate from executable jobs, schedulers, native memory, and artifacts.
//
// Design notes
// - A stage is a durable semantic boundary over one ordered operation set.
// - Repeated operation definitions inside a stage are valid.
// - Operation occurrence index is meaningful inside the stage.
// - Stage identity is not operation identity.
// - Stage definitions are authoring/compilation metadata, not runtime execution payloads.
// - Jobs must not receive this type.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Defines one durable Atlas stage contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage is a named semantic boundary in a generation route. It owns a stable stage id,
    /// a stable diagnostic name, and an authored ordered sequence of operation occurrences.
    /// </para>
    ///
    /// <para>
    /// A stage does not own memory. It does not schedule jobs. It does not resolve Field slots.
    /// It does not know concrete native containers. The compiler resolves the stage's operation
    /// access declarations against the Field Contract table and emits a compiled execution plan.
    /// </para>
    ///
    /// <para>
    /// Operation order is preserved exactly. Repeated operation definitions are legal and expected:
    /// refinement passes, relaxation passes, smoothing passes, repair passes, solver iterations,
    /// validation passes, and diagnostics may reuse the same durable operation contract multiple
    /// times inside one stage.
    /// </para>
    /// </remarks>
    public sealed class AtlasStageDefinition :
        IReadOnlyList<AtlasOperationDefinition>
    {
        /// <summary>
        /// Stable, versioned identity of this stage contract.
        /// </summary>
        public readonly AtlasStageId StageId;

        /// <summary>
        /// Stable diagnostic stage name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        /// <remarks>
        /// Stage names are not durable identity. Durable identity belongs to <see cref="StageId"/>.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Ordered operation occurrences owned by this stage.
        /// </summary>
        public readonly AtlasOperationSet Operations;

        private AtlasStageDefinition(
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            AtlasOperationSet operations)
        {
            StageId = stageId;
            DebugName = debugName;
            Operations = operations;

            ValidateOrThrow(
                stageId,
                debugName,
                operations);
        }

        /// <summary>
        /// Gets the number of operation occurrences in this stage.
        /// </summary>
        public int Count => Operations.Count;

        /// <summary>
        /// Gets whether this stage contains no operation occurrences.
        /// </summary>
        /// <remarks>
        /// Concrete stage definitions created by this type are required to contain at least one
        /// operation occurrence, so this property normally returns <c>false</c>.
        /// </remarks>
        public bool IsEmpty => Operations.IsEmpty;

        /// <summary>
        /// Gets whether at least one operation occurrence reads Field contents.
        /// </summary>
        public bool ReadsContent => Operations.ReadsContent;

        /// <summary>
        /// Gets whether at least one operation occurrence writes, appends, consumes, or mutates Field contents.
        /// </summary>
        public bool WritesContent => Operations.WritesContent;

        /// <summary>
        /// Gets whether at least one operation occurrence declares an optional Field binding.
        /// </summary>
        public bool HasOptionalAccess => Operations.HasOptionalAccess;

        /// <summary>
        /// Gets whether at least one operation occurrence declares a shape-only Field binding.
        /// </summary>
        public bool HasShapeOnlyAccess => Operations.HasShapeOnlyAccess;

        /// <summary>
        /// Gets the operation definition at a zero-based stage-local operation occurrence index.
        /// </summary>
        /// <param name="index">Zero-based operation occurrence index.</param>
        /// <returns>The operation definition at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this stage's operation range.
        /// </exception>
        public AtlasOperationDefinition this[int index] => Operations[index];

        /// <summary>
        /// Creates a stage definition from an already constructed operation set.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage identity.</param>
        /// <param name="debugName">Stable diagnostic stage name.</param>
        /// <param name="operations">Ordered operation occurrences owned by the stage.</param>
        /// <returns>A validated stage definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageId"/> is invalid, <paramref name="debugName"/> is empty,
        /// or <paramref name="operations"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operations"/> is <c>null</c>.
        /// </exception>
        public static AtlasStageDefinition Create(
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            AtlasOperationSet operations)
        {
            return new AtlasStageDefinition(
                stageId,
                debugName,
                operations);
        }

        /// <summary>
        /// Creates a stage definition from explicitly ordered operation occurrences.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage identity.</param>
        /// <param name="debugName">Stable diagnostic stage name.</param>
        /// <param name="operations">Operation occurrences in authored stage order.</param>
        /// <returns>A validated stage definition.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operations"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the stage header is invalid or any operation occurrence is invalid.
        /// </exception>
        public static AtlasStageDefinition Create(
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            params AtlasOperationDefinition[] operations)
        {
            return new AtlasStageDefinition(
                stageId,
                debugName,
                AtlasOperationSet.Create(debugName, operations));
        }

        /// <summary>
        /// Determines whether this stage contains at least one occurrence of the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns><c>true</c> when at least one matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool Contains(AtlasOperationId operationId)
        {
            return Operations.Contains(operationId);
        }

        /// <summary>
        /// Determines whether this stage contains at least one operation occurrence with the supplied debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <returns><c>true</c> when at least one matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool ContainsOperation(FixedString64Bytes operationName)
        {
            return Operations.Contains(operationName);
        }

        /// <summary>
        /// Returns the first stage-local operation occurrence index matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns>The first matching operation occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirst(AtlasOperationId operationId)
        {
            return Operations.IndexOfFirst(operationId);
        }

        /// <summary>
        /// Returns the first stage-local operation occurrence index matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <returns>The first matching operation occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirstOperation(FixedString64Bytes operationName)
        {
            return Operations.IndexOfFirst(operationName);
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence index for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <returns>The first matching operation occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationId"/> is invalid or absent from this stage.
        /// </exception>
        public int GetRequiredFirstIndex(AtlasOperationId operationId)
        {
            return Operations.GetRequiredFirstIndex(operationId);
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence index for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <returns>The first matching operation occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationName"/> is empty or absent from this stage.
        /// </exception>
        public int GetRequiredFirstOperationIndex(FixedString64Bytes operationName)
        {
            return Operations.GetRequiredFirstIndex(operationName);
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence index for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <param name="index">First matching stage-local operation index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstIndex(
            AtlasOperationId operationId,
            out int index)
        {
            return Operations.TryGetFirstIndex(operationId, out index);
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence index for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <param name="index">First matching stage-local operation index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperationIndex(
            FixedString64Bytes operationName,
            out int index)
        {
            return Operations.TryGetFirstIndex(operationName, out index);
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <param name="operation">First matching operation definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperation(
            AtlasOperationId operationId,
            out AtlasOperationDefinition operation)
        {
            return Operations.TryGetFirstOperation(operationId, out operation);
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <param name="operation">First matching operation definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperation(
            FixedString64Bytes operationName,
            out AtlasOperationDefinition operation)
        {
            return Operations.TryGetFirstOperation(operationName, out operation);
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <returns>The first matching operation definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationId"/> is invalid or absent from this stage.
        /// </exception>
        public AtlasOperationDefinition GetRequiredFirstOperation(AtlasOperationId operationId)
        {
            return Operations.GetRequiredFirstOperation(operationId);
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <returns>The first matching operation definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationName"/> is empty or absent from this stage.
        /// </exception>
        public AtlasOperationDefinition GetRequiredFirstOperation(FixedString64Bytes operationName)
        {
            return Operations.GetRequiredFirstOperation(operationName);
        }

        /// <summary>
        /// Counts stage-local operation occurrences matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to count.</param>
        /// <returns>The number of matching operation occurrences.</returns>
        public int CountOf(AtlasOperationId operationId)
        {
            return Operations.CountOf(operationId);
        }

        /// <summary>
        /// Counts stage-local operation occurrences matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to count.</param>
        /// <returns>The number of matching operation occurrences.</returns>
        public int CountOfOperation(FixedString64Bytes operationName)
        {
            return Operations.CountOf(operationName);
        }

        /// <summary>
        /// Copies all stage-local occurrence indices matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        public int CopyIndices(
            AtlasOperationId operationId,
            int[] destination)
        {
            return Operations.CopyIndices(operationId, destination);
        }

        /// <summary>
        /// Copies all stage-local occurrence indices matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        public int CopyOperationIndices(
            FixedString64Bytes operationName,
            int[] destination)
        {
            return Operations.CopyIndices(operationName, destination);
        }

        /// <summary>
        /// Creates a new managed array containing all stage-local occurrence indices matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToIndexArray(AtlasOperationId operationId)
        {
            return Operations.ToIndexArray(operationId);
        }

        /// <summary>
        /// Creates a new managed array containing all stage-local occurrence indices matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToOperationIndexArray(FixedString64Bytes operationName)
        {
            return Operations.ToIndexArray(operationName);
        }

        /// <summary>
        /// Copies operation occurrences from this stage into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving operation definitions in stage order.</param>
        public void CopyTo(AtlasOperationDefinition[] destination)
        {
            Operations.CopyTo(destination);
        }

        /// <summary>
        /// Copies a stage-local operation occurrence range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source index in this stage.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of operation occurrences to copy.</param>
        public void CopyTo(
            int sourceIndex,
            AtlasOperationDefinition[] destination,
            int destinationIndex,
            int length)
        {
            Operations.CopyTo(
                sourceIndex,
                destination,
                destinationIndex,
                length);
        }

        /// <summary>
        /// Creates a managed copy of this stage's operation occurrences.
        /// </summary>
        /// <returns>A new operation-definition array in stage order.</returns>
        public AtlasOperationDefinition[] ToArray()
        {
            return Operations.ToArray();
        }

        /// <summary>
        /// Gets a managed enumerator over stage-local operation occurrences in authored order.
        /// </summary>
        /// <returns>An enumerator over operation definitions.</returns>
        public IEnumerator<AtlasOperationDefinition> GetEnumerator()
        {
            return Operations.GetEnumerator();
        }

        /// <summary>
        /// Gets a managed enumerator over stage-local operation occurrences in authored order.
        /// </summary>
        /// <returns>An enumerator over operation definitions.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this stage definition.
        /// </summary>
        /// <returns>A string containing the stage name, stage id, and operation occurrence count.</returns>
        public override string ToString()
        {
            return $"AtlasStageDefinition(Name={GetDiagnosticName(DebugName)}, StageId={StageId}, OperationCount={Count})";
        }

        private static void ValidateOrThrow(
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            AtlasOperationSet operations)
        {
            stageId.ValidateOrThrow(nameof(stageId));

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas Stage definition must have a non-empty debug name.",
                    nameof(debugName));
            }

            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            if (operations.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas Stage definition '{debugName}' must contain at least one operation occurrence.",
                    nameof(operations));
            }
        }

        private static string GetDiagnosticName(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? "<unnamed>"
                : name.ToString();
        }
    }
}