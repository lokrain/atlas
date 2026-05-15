// Runtime/Compilation/AtlasCompiledStage.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one stage occurrence after all operation Field access has been resolved.
// - Preserve stage occurrence index and stage-local operation occurrence order.
// - Keep compiled stage metadata separate from concrete jobs, schedulers, native memory, and artifacts.
//
// Design notes
// - This is compilation metadata, not runtime job payload.
// - Repeated stage definitions are valid at pipeline level; stage occurrence index is therefore essential.
// - Repeated operation definitions are valid inside a stage; operation occurrence index remains meaningful.
// - Operation order is preserved exactly from AtlasStageDefinition.
// - This type deliberately uses immutable arrays and linear scans instead of dictionaries.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Stages;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Compiled representation of one stage occurrence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage definition is a durable semantic contract. A stage occurrence is one concrete use
    /// of that contract in an authored pipeline sequence. Because a pipeline may intentionally
    /// repeat a stage contract, the stage occurrence index is part of compiled identity.
    /// </para>
    ///
    /// <para>
    /// This type preserves stage-local operation order and stores one
    /// <see cref="AtlasCompiledOperation"/> for each source operation occurrence.
    /// </para>
    ///
    /// <para>
    /// This type does not own native memory and does not schedule jobs. Later execution layers
    /// consume compiled stages and operations to produce concrete scheduler payloads and workspace
    /// memory views.
    /// </para>
    /// </remarks>
    public sealed class AtlasCompiledStage :
        IReadOnlyList<AtlasCompiledOperation>
    {
        private const int InvalidOperationIndex = -1;

        private readonly AtlasCompiledOperation[] _operations;

        /// <summary>
        /// Zero-based stage occurrence index inside the source pipeline or parent sequence.
        /// </summary>
        /// <remarks>
        /// This is not durable stage identity. Durable stage identity belongs to
        /// <see cref="StageId"/>. The occurrence index distinguishes repeated uses of the same
        /// stage contract.
        /// </remarks>
        public readonly int StageIndex;

        /// <summary>
        /// Stable, versioned identity of the source stage contract.
        /// </summary>
        public readonly AtlasStageId StageId;

        /// <summary>
        /// Stable diagnostic stage name from the source stage definition.
        /// </summary>
        public readonly FixedString64Bytes DebugName;

        private AtlasCompiledStage(
            int stageIndex,
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            AtlasCompiledOperation[] operations)
        {
            StageIndex = stageIndex;
            StageId = stageId;
            DebugName = debugName;

            _operations = CopyAndValidateOperations(
                stageIndex,
                stageId,
                debugName,
                operations);
        }

        /// <summary>
        /// Gets the number of compiled operation occurrences in this stage occurrence.
        /// </summary>
        public int Count => _operations.Length;

        /// <summary>
        /// Gets whether this compiled stage has no operation occurrences.
        /// </summary>
        /// <remarks>
        /// Concrete compiled stages are required to contain at least one operation occurrence, so
        /// this property normally returns <c>false</c>.
        /// </remarks>
        public bool IsEmpty => _operations.Length == 0;

        /// <summary>
        /// Gets the total number of compiled bindings across all operation occurrences.
        /// </summary>
        public int BindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _operations.Length; i++)
                {
                    count += _operations[i].Count;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of bindings that resolved to concrete Field Contracts.
        /// </summary>
        public int PresentBindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _operations.Length; i++)
                {
                    count += _operations[i].PresentBindingCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of absent optional bindings.
        /// </summary>
        public int MissingOptionalBindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _operations.Length; i++)
                {
                    count += _operations[i].MissingOptionalBindingCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets whether at least one present binding reads Field contents.
        /// </summary>
        public bool ReadsContent
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].ReadsContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one present binding writes, appends, consumes, or mutates Field contents.
        /// </summary>
        public bool WritesContent
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].WritesContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one source binding declares a content read, regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentRead
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].DeclaresContentRead)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one source binding declares a content write, regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentWrite
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].DeclaresContentWrite)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled stage contains at least one optional binding.
        /// </summary>
        public bool HasOptionalBinding
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].HasOptionalBinding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled stage contains at least one missing optional binding.
        /// </summary>
        public bool HasMissingOptionalBinding
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].HasMissingOptionalBinding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled stage contains at least one shape-only binding.
        /// </summary>
        public bool HasShapeOnlyBinding
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].HasShapeOnlyBinding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled stage has at least one present binding that requires content memory.
        /// </summary>
        public bool RequiresContentMemory
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].RequiresContentMemory)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the compiled operation at a zero-based stage-local operation occurrence index.
        /// </summary>
        /// <param name="index">Zero-based stage-local operation occurrence index.</param>
        /// <returns>The compiled operation at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this stage's operation range.
        /// </exception>
        public AtlasCompiledOperation this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _operations[index];
            }
        }

        /// <summary>
        /// Compiles one stage occurrence by resolving all operation access declarations against a Contract table.
        /// </summary>
        /// <param name="stageIndex">Zero-based stage occurrence index.</param>
        /// <param name="stage">Source stage definition.</param>
        /// <param name="contracts">Contract table used for Field resolution.</param>
        /// <returns>A compiled stage occurrence with resolved operation bindings.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stage"/> or <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the stage occurrence cannot be compiled.
        /// </exception>
        public static AtlasCompiledStage Compile(
            int stageIndex,
            AtlasStageDefinition stage,
            AtlasContractTable contracts)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            ValidateStageHeaderOrThrow(
                stageIndex,
                stage.StageId,
                stage.DebugName,
                stage.Count);

            var operations = new AtlasCompiledOperation[stage.Count];

            for (var i = 0; i < stage.Count; i++)
            {
                operations[i] = AtlasCompiledOperation.Compile(
                    i,
                    stage[i],
                    contracts);
            }

            return new AtlasCompiledStage(
                stageIndex,
                stage.StageId,
                stage.DebugName,
                operations);
        }

        /// <summary>
        /// Creates a compiled stage from already compiled operations.
        /// </summary>
        /// <param name="stageIndex">Zero-based stage occurrence index.</param>
        /// <param name="stageId">Stable, versioned source stage identity.</param>
        /// <param name="debugName">Stable diagnostic stage name.</param>
        /// <param name="operations">Compiled operations in stage-local operation order.</param>
        /// <returns>A validated compiled stage occurrence.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operations"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the header or operations are invalid.
        /// </exception>
        public static AtlasCompiledStage Create(
            int stageIndex,
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            params AtlasCompiledOperation[] operations)
        {
            return new AtlasCompiledStage(
                stageIndex,
                stageId,
                debugName,
                operations);
        }

        /// <summary>
        /// Determines whether this compiled stage contains at least one occurrence of the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool Contains(AtlasOperationId operationId)
        {
            return IndexOfFirst(operationId) != InvalidOperationIndex;
        }

        /// <summary>
        /// Determines whether this compiled stage contains at least one operation occurrence with the supplied debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool ContainsOperation(FixedString64Bytes operationName)
        {
            return IndexOfFirstOperation(operationName) != InvalidOperationIndex;
        }

        /// <summary>
        /// Returns the first stage-local operation occurrence index matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns>The first matching operation occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirst(AtlasOperationId operationId)
        {
            if (!operationId.IsValid)
            {
                return InvalidOperationIndex;
            }

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].OperationId == operationId)
                {
                    return i;
                }
            }

            return InvalidOperationIndex;
        }

        /// <summary>
        /// Returns the first stage-local operation occurrence index matching the supplied debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <returns>The first matching operation occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirstOperation(FixedString64Bytes operationName)
        {
            if (operationName.IsEmpty)
            {
                return InvalidOperationIndex;
            }

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].DebugName.Equals(operationName))
                {
                    return i;
                }
            }

            return InvalidOperationIndex;
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence index for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <returns>The first matching operation occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationId"/> is invalid or absent from this compiled stage.
        /// </exception>
        public int GetRequiredFirstIndex(AtlasOperationId operationId)
        {
            var index = IndexOfFirst(operationId);

            if (index != InvalidOperationIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Compiled Atlas stage '{DebugName}' at occurrence index '{StageIndex}' does not contain operation id '{operationId}'.",
                nameof(operationId));
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence index for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <returns>The first matching operation occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationName"/> is empty or absent from this compiled stage.
        /// </exception>
        public int GetRequiredFirstOperationIndex(FixedString64Bytes operationName)
        {
            var index = IndexOfFirstOperation(operationName);

            if (index != InvalidOperationIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Compiled Atlas stage '{DebugName}' at occurrence index '{StageIndex}' does not contain operation debug name '{operationName}'.",
                nameof(operationName));
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence index for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <param name="index">First matching operation index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstIndex(
            AtlasOperationId operationId,
            out int index)
        {
            index = IndexOfFirst(operationId);
            return index != InvalidOperationIndex;
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence index for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <param name="index">First matching operation index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperationIndex(
            FixedString64Bytes operationName,
            out int index)
        {
            index = IndexOfFirstOperation(operationName);
            return index != InvalidOperationIndex;
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <param name="operation">First matching compiled operation when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperation(
            AtlasOperationId operationId,
            out AtlasCompiledOperation operation)
        {
            var index = IndexOfFirst(operationId);

            if (index != InvalidOperationIndex)
            {
                operation = _operations[index];
                return true;
            }

            operation = null;
            return false;
        }

        /// <summary>
        /// Attempts to resolve the first stage-local operation occurrence for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <param name="operation">First matching compiled operation when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching operation occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperation(
            FixedString64Bytes operationName,
            out AtlasCompiledOperation operation)
        {
            var index = IndexOfFirstOperation(operationName);

            if (index != InvalidOperationIndex)
            {
                operation = _operations[index];
                return true;
            }

            operation = null;
            return false;
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <returns>The first matching compiled operation.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationId"/> is invalid or absent from this compiled stage.
        /// </exception>
        public AtlasCompiledOperation GetRequiredFirstOperation(AtlasOperationId operationId)
        {
            return _operations[GetRequiredFirstIndex(operationId)];
        }

        /// <summary>
        /// Resolves the first stage-local operation occurrence for an operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to resolve.</param>
        /// <returns>The first matching compiled operation.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationName"/> is empty or absent from this compiled stage.
        /// </exception>
        public AtlasCompiledOperation GetRequiredFirstOperation(FixedString64Bytes operationName)
        {
            return _operations[GetRequiredFirstOperationIndex(operationName)];
        }

        /// <summary>
        /// Counts stage-local operation occurrences matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to count.</param>
        /// <returns>The number of matching operation occurrences.</returns>
        public int CountOf(AtlasOperationId operationId)
        {
            if (!operationId.IsValid)
            {
                return 0;
            }

            var count = 0;

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].OperationId == operationId)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts stage-local operation occurrences matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to count.</param>
        /// <returns>The number of matching operation occurrences.</returns>
        public int CountOfOperation(FixedString64Bytes operationName)
        {
            if (operationName.IsEmpty)
            {
                return 0;
            }

            var count = 0;

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].DebugName.Equals(operationName))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies all stage-local operation occurrence indices matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        public int CopyIndices(
            AtlasOperationId operationId,
            int[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var count = CountOf(operationId);

            if (destination.Length < count)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than matching operation count '{count}'.",
                    nameof(destination));
            }

            if (!operationId.IsValid)
            {
                return 0;
            }

            var writeIndex = 0;

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].OperationId == operationId)
                {
                    destination[writeIndex++] = i;
                }
            }

            return writeIndex;
        }

        /// <summary>
        /// Copies all stage-local operation occurrence indices matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        public int CopyOperationIndices(
            FixedString64Bytes operationName,
            int[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var count = CountOfOperation(operationName);

            if (destination.Length < count)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than matching operation count '{count}'.",
                    nameof(destination));
            }

            if (operationName.IsEmpty)
            {
                return 0;
            }

            var writeIndex = 0;

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].DebugName.Equals(operationName))
                {
                    destination[writeIndex++] = i;
                }
            }

            return writeIndex;
        }

        /// <summary>
        /// Creates a new managed array containing all stage-local operation occurrence indices matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToIndexArray(AtlasOperationId operationId)
        {
            var indices = new int[CountOf(operationId)];
            CopyIndices(operationId, indices);
            return indices;
        }

        /// <summary>
        /// Creates a new managed array containing all stage-local operation occurrence indices matching the supplied operation debug name.
        /// </summary>
        /// <param name="operationName">Diagnostic operation name to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToOperationIndexArray(FixedString64Bytes operationName)
        {
            var indices = new int[CountOfOperation(operationName)];
            CopyOperationIndices(operationName, indices);
            return indices;
        }

        /// <summary>
        /// Copies compiled operation occurrences into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving compiled operations in stage-local order.</param>
        public void CopyTo(AtlasCompiledOperation[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _operations.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than compiled operation count '{_operations.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_operations, destination, _operations.Length);
        }

        /// <summary>
        /// Copies a compiled operation range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source operation index in this stage.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of operation occurrences to copy.</param>
        public void CopyTo(
            int sourceIndex,
            AtlasCompiledOperation[] destination,
            int destinationIndex,
            int length)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Source index must be non-negative.");
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), destinationIndex, "Destination index must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
            }

            if (sourceIndex + length > _operations.Length)
            {
                throw new ArgumentException(
                    "Source range exceeds compiled stage bounds.",
                    nameof(length));
            }

            if (destinationIndex + length > destination.Length)
            {
                throw new ArgumentException(
                    "Destination range exceeds destination array bounds.",
                    nameof(length));
            }

            Array.Copy(_operations, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Creates a managed copy of this stage's compiled operation occurrences.
        /// </summary>
        /// <returns>A new compiled-operation array in stage-local operation order.</returns>
        public AtlasCompiledOperation[] ToArray()
        {
            var copy = new AtlasCompiledOperation[_operations.Length];
            Array.Copy(_operations, copy, _operations.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over compiled operation occurrences in stage-local order.
        /// </summary>
        /// <returns>An enumerator over compiled operations.</returns>
        public IEnumerator<AtlasCompiledOperation> GetEnumerator()
        {
            for (var i = 0; i < _operations.Length; i++)
            {
                yield return _operations[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over compiled operation occurrences in stage-local order.
        /// </summary>
        /// <returns>An enumerator over compiled operations.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this compiled stage.
        /// </summary>
        /// <returns>A string containing stage name, id, occurrence index, operation count, and binding counts.</returns>
        public override string ToString()
        {
            return $"AtlasCompiledStage(Index={StageIndex}, Name={DebugName}, Id={StageId}, Operations={Count}, Bindings={BindingCount}, Present={PresentBindingCount}, MissingOptional={MissingOptionalBindingCount})";
        }

        private static AtlasCompiledOperation[] CopyAndValidateOperations(
            int stageIndex,
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            AtlasCompiledOperation[] operations)
        {
            ValidateStageHeaderOrThrow(
                stageIndex,
                stageId,
                debugName,
                operations == null ? 0 : operations.Length);

            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            var copy = new AtlasCompiledOperation[operations.Length];

            for (var i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];

                ValidateOperationOrThrow(
                    debugName,
                    operation,
                    i);

                copy[i] = operation;
            }

            return copy;
        }

        private static void ValidateStageHeaderOrThrow(
            int stageIndex,
            AtlasStageId stageId,
            FixedString64Bytes debugName,
            int operationCount)
        {
            if (stageIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageIndex),
                    stageIndex,
                    "Compiled stage occurrence index must be non-negative.");
            }

            stageId.ValidateOrThrow(nameof(stageId));

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Compiled Atlas stage must have a non-empty debug name.",
                    nameof(debugName));
            }

            if (operationCount <= 0)
            {
                throw new ArgumentException(
                    $"Compiled Atlas stage '{debugName}' must contain at least one operation occurrence.",
                    nameof(operationCount));
            }
        }

        private static void ValidateOperationOrThrow(
            FixedString64Bytes stageName,
            AtlasCompiledOperation operation,
            int index)
        {
            if (operation == null)
            {
                throw new ArgumentException(
                    $"Compiled Atlas stage '{stageName}' contains a null operation occurrence at index '{index}'.",
                    nameof(operation));
            }

            if (operation.OperationIndex != index)
            {
                throw new ArgumentException(
                    $"Compiled Atlas stage '{stageName}' contains operation '{operation.DebugName}' with operation index '{operation.OperationIndex}', but expected '{index}'.",
                    nameof(operation));
            }

            operation.OperationId.ValidateOrThrow($"operations[{index}].OperationId");

            if (operation.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Compiled Atlas stage '{stageName}' contains an operation occurrence with an empty debug name at index '{index}'.",
                    nameof(operation));
            }

            if (operation.IsEmpty)
            {
                throw new ArgumentException(
                    $"Compiled Atlas stage '{stageName}' contains operation occurrence '{operation.DebugName}' with no compiled bindings.",
                    nameof(operation));
            }
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_operations.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Compiled operation index must be between 0 and {_operations.Length - 1}.");
        }
    }
}