// Runtime/Operations/AtlasOperationSet.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Define an immutable ordered operation sequence used by stages, routes, and pipeline presets.
// - Preserve authored operation order exactly.
// - Allow the same operation definition to appear multiple times.
// - Treat operation occurrence index as the only sequence-local identity.
//
// Design notes
// - This type is intentionally not a mathematical set.
// - Repeated operations are valid and expected: smoothing passes, relaxation passes, refinement
//   passes, repair passes, iterative solvers, diagnostics, and staged validation may reuse the
//   same operation contract.
// - Operation identity belongs to AtlasOperationDefinition.OperationId.
// - Operation occurrence identity belongs to this sequence position and later compiled operation indices.
// - This type deliberately uses linear scans instead of dictionaries. Operation sets are authoring
//   and compilation metadata, not execution-time lookup tables.
// - Do not use this type as a unique operation catalog. If a unique registry is needed later,
//   create AtlasOperationCatalog separately.

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Immutable ordered sequence of Atlas operation definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Despite the historical name, this type does not enforce uniqueness. It represents an
    /// authored operation list where the same operation definition may appear multiple times.
    /// </para>
    ///
    /// <para>
    /// This distinction is important. Operation definitions describe durable operation contracts.
    /// Operation occurrences describe where and how many times those contracts are used by a stage,
    /// route, or pipeline preset.
    /// </para>
    ///
    /// <para>
    /// Single-value lookup by operation id or debug name is ambiguous when duplicates exist.
    /// This type therefore exposes first-match helpers and explicit match-count / copy-index APIs.
    /// </para>
    /// </remarks>
    public sealed class AtlasOperationSet :
        IReadOnlyList<AtlasOperationDefinition>
    {
        private const int InvalidOperationIndex = -1;

        private readonly AtlasOperationDefinition[] _operations;

        /// <summary>
        /// Diagnostic operation-set name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        public readonly FixedString64Bytes Name;

        private AtlasOperationSet(
            FixedString64Bytes name,
            AtlasOperationDefinition[] operations)
        {
            Name = name;
            _operations = CopyAndValidateOperations(name, operations);
        }

        /// <summary>
        /// Gets the number of operation occurrences in this set.
        /// </summary>
        public int Count => _operations.Length;

        /// <summary>
        /// Gets whether this set contains no operation occurrences.
        /// </summary>
        public bool IsEmpty => _operations.Length == 0;

        /// <summary>
        /// Gets whether at least one operation occurrence reads Field contents.
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
        /// Gets whether at least one operation occurrence writes, appends, consumes, or mutates Field contents.
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
        /// Gets whether at least one operation occurrence declares an optional Field binding.
        /// </summary>
        public bool HasOptionalAccess
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].HasOptionalAccess)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one operation occurrence declares a shape-only Field binding.
        /// </summary>
        public bool HasShapeOnlyAccess
        {
            get
            {
                for (var i = 0; i < _operations.Length; i++)
                {
                    if (_operations[i].HasShapeOnlyAccess)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the operation definition at a zero-based occurrence index.
        /// </summary>
        /// <param name="index">Zero-based operation occurrence index.</param>
        /// <returns>The operation definition at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this set's operation range.
        /// </exception>
        public AtlasOperationDefinition this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _operations[index];
            }
        }

        /// <summary>
        /// Creates an unnamed operation set from explicitly ordered operation occurrences.
        /// </summary>
        /// <param name="operations">Operation occurrences in authored order.</param>
        /// <returns>A validated immutable operation set.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operations"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an operation occurrence is <c>null</c> or invalid.
        /// </exception>
        public static AtlasOperationSet Create(params AtlasOperationDefinition[] operations)
        {
            return new AtlasOperationSet(default, operations);
        }

        /// <summary>
        /// Creates a named operation set from explicitly ordered operation occurrences.
        /// </summary>
        /// <param name="name">Diagnostic operation-set name.</param>
        /// <param name="operations">Operation occurrences in authored order.</param>
        /// <returns>A validated immutable operation set.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operations"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an operation occurrence is <c>null</c> or invalid.
        /// </exception>
        public static AtlasOperationSet Create(
            FixedString64Bytes name,
            params AtlasOperationDefinition[] operations)
        {
            return new AtlasOperationSet(name, operations);
        }

        /// <summary>
        /// Determines whether this set contains at least one occurrence of the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns><c>true</c> when at least one matching occurrence exists; otherwise, <c>false</c>.</returns>
        public bool Contains(AtlasOperationId operationId)
        {
            return IndexOfFirst(operationId) != InvalidOperationIndex;
        }

        /// <summary>
        /// Determines whether this set contains at least one occurrence with the supplied debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to search for.</param>
        /// <returns><c>true</c> when at least one matching occurrence exists; otherwise, <c>false</c>.</returns>
        public bool Contains(FixedString64Bytes debugName)
        {
            return IndexOfFirst(debugName) != InvalidOperationIndex;
        }

        /// <summary>
        /// Returns the first occurrence index matching the supplied operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <returns>The first matching occurrence index, or <c>-1</c> when absent.</returns>
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
        /// Returns the first occurrence index matching the supplied debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to search for.</param>
        /// <returns>The first matching occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirst(FixedString64Bytes debugName)
        {
            if (debugName.IsEmpty)
            {
                return InvalidOperationIndex;
            }

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].DebugName.Equals(debugName))
                {
                    return i;
                }
            }

            return InvalidOperationIndex;
        }

        /// <summary>
        /// Resolves the first occurrence index for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <returns>The first matching occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationId"/> is invalid or absent from this set.
        /// </exception>
        public int GetRequiredFirstIndex(AtlasOperationId operationId)
        {
            var index = IndexOfFirst(operationId);

            if (index != InvalidOperationIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Atlas Operation set '{GetDiagnosticName()}' does not contain operation id '{operationId}'.",
                nameof(operationId));
        }

        /// <summary>
        /// Resolves the first occurrence index for a debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to resolve.</param>
        /// <returns>The first matching occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="debugName"/> is empty or absent from this set.
        /// </exception>
        public int GetRequiredFirstIndex(FixedString64Bytes debugName)
        {
            var index = IndexOfFirst(debugName);

            if (index != InvalidOperationIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Atlas Operation set '{GetDiagnosticName()}' does not contain operation debug name '{debugName}'.",
                nameof(debugName));
        }

        /// <summary>
        /// Attempts to resolve the first occurrence index for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <param name="index">First matching occurrence index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstIndex(
            AtlasOperationId operationId,
            out int index)
        {
            index = IndexOfFirst(operationId);
            return index != InvalidOperationIndex;
        }

        /// <summary>
        /// Attempts to resolve the first occurrence index for a debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to resolve.</param>
        /// <param name="index">First matching occurrence index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstIndex(
            FixedString64Bytes debugName,
            out int index)
        {
            index = IndexOfFirst(debugName);
            return index != InvalidOperationIndex;
        }

        /// <summary>
        /// Attempts to resolve the first operation occurrence for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <param name="operation">First matching operation definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperation(
            AtlasOperationId operationId,
            out AtlasOperationDefinition operation)
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
        /// Attempts to resolve the first operation occurrence for a debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to resolve.</param>
        /// <param name="operation">First matching operation definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstOperation(
            FixedString64Bytes debugName,
            out AtlasOperationDefinition operation)
        {
            var index = IndexOfFirst(debugName);

            if (index != InvalidOperationIndex)
            {
                operation = _operations[index];
                return true;
            }

            operation = null;
            return false;
        }

        /// <summary>
        /// Resolves the first operation occurrence for an operation id.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to resolve.</param>
        /// <returns>The first matching operation definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationId"/> is invalid or absent from this set.
        /// </exception>
        public AtlasOperationDefinition GetRequiredFirstOperation(AtlasOperationId operationId)
        {
            return _operations[GetRequiredFirstIndex(operationId)];
        }

        /// <summary>
        /// Resolves the first operation occurrence for a debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to resolve.</param>
        /// <returns>The first matching operation definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="debugName"/> is empty or absent from this set.
        /// </exception>
        public AtlasOperationDefinition GetRequiredFirstOperation(FixedString64Bytes debugName)
        {
            return _operations[GetRequiredFirstIndex(debugName)];
        }

        /// <summary>
        /// Counts operation occurrences matching the supplied operation id.
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
        /// Counts operation occurrences matching the supplied debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to count.</param>
        /// <returns>The number of matching operation occurrences.</returns>
        public int CountOf(FixedString64Bytes debugName)
        {
            if (debugName.IsEmpty)
            {
                return 0;
            }

            var count = 0;

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].DebugName.Equals(debugName))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies all occurrence indices matching the supplied operation id into a caller-provided destination array.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation id to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is too small.
        /// </exception>
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
        /// Copies all occurrence indices matching the supplied debug name into a caller-provided destination array.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is too small.
        /// </exception>
        public int CopyIndices(
            FixedString64Bytes debugName,
            int[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var count = CountOf(debugName);

            if (destination.Length < count)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than matching operation count '{count}'.",
                    nameof(destination));
            }

            if (debugName.IsEmpty)
            {
                return 0;
            }

            var writeIndex = 0;

            for (var i = 0; i < _operations.Length; i++)
            {
                if (_operations[i].DebugName.Equals(debugName))
                {
                    destination[writeIndex++] = i;
                }
            }

            return writeIndex;
        }

        /// <summary>
        /// Creates a new managed array containing all occurrence indices matching the supplied operation id.
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
        /// Creates a new managed array containing all occurrence indices matching the supplied debug name.
        /// </summary>
        /// <param name="debugName">Diagnostic operation name to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToIndexArray(FixedString64Bytes debugName)
        {
            var indices = new int[CountOf(debugName)];
            CopyIndices(debugName, indices);
            return indices;
        }

        /// <summary>
        /// Copies operation occurrences from this set into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving operation definitions in authored order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is smaller than <see cref="Count"/>.
        /// </exception>
        public void CopyTo(AtlasOperationDefinition[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _operations.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than operation count '{_operations.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_operations, destination, _operations.Length);
        }

        /// <summary>
        /// Copies an operation occurrence range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source index in this set.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of operation occurrences to copy.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a range argument is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the source or destination range is invalid.
        /// </exception>
        public void CopyTo(
            int sourceIndex,
            AtlasOperationDefinition[] destination,
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
                    "Source range exceeds Operation set bounds.",
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
        /// Creates a managed copy of this set's operation occurrences.
        /// </summary>
        /// <returns>A new operation-definition array in authored order.</returns>
        public AtlasOperationDefinition[] ToArray()
        {
            var copy = new AtlasOperationDefinition[_operations.Length];
            Array.Copy(_operations, copy, _operations.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over operation occurrences in authored order.
        /// </summary>
        /// <returns>An enumerator over operation definitions.</returns>
        public IEnumerator<AtlasOperationDefinition> GetEnumerator()
        {
            for (var i = 0; i < _operations.Length; i++)
            {
                yield return _operations[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over operation occurrences in authored order.
        /// </summary>
        /// <returns>An enumerator over operation definitions.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this operation set.
        /// </summary>
        /// <returns>A string containing the set name and operation occurrence count.</returns>
        public override string ToString()
        {
            return $"AtlasOperationSet(Name={GetDiagnosticName()}, Count={Count})";
        }

        private static AtlasOperationDefinition[] CopyAndValidateOperations(
            FixedString64Bytes setName,
            AtlasOperationDefinition[] operations)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            var copy = new AtlasOperationDefinition[operations.Length];

            for (var i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];

                ValidateOperationDefinitionOrThrow(
                    setName,
                    operation,
                    i);

                copy[i] = operation;
            }

            return copy;
        }

        private static void ValidateOperationDefinitionOrThrow(
            FixedString64Bytes setName,
            AtlasOperationDefinition operation,
            int index)
        {
            if (operation == null)
            {
                throw new ArgumentException(
                    $"Atlas Operation set '{GetDiagnosticName(setName)}' contains a null operation occurrence at index '{index}'.");
            }

            operation.OperationId.ValidateOrThrow($"operations[{index}].OperationId");

            if (operation.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas Operation set '{GetDiagnosticName(setName)}' contains an operation occurrence with an empty debug name at index '{index}'.");
            }

            if (operation.Count == 0)
            {
                throw new ArgumentException(
                    $"Atlas Operation set '{GetDiagnosticName(setName)}' contains operation occurrence '{operation.DebugName}' with no Field access declarations.");
            }

            for (var i = 0; i < operation.Count; i++)
            {
                operation[i].ValidateOrThrow($"operations[{index}][{i}]");
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
                $"Operation index must be between 0 and {_operations.Length - 1}.");
        }

        private string GetDiagnosticName()
        {
            return GetDiagnosticName(Name);
        }

        private static string GetDiagnosticName(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? "<unnamed>"
                : name.ToString();
        }
    }
}