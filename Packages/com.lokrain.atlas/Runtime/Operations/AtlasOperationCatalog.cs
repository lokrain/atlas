// Packages/com.lokrain.atlas/Runtime/Operations/AtlasOperationCatalog.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Define a unique catalog of durable Atlas operation definitions.
// - Resolve AtlasOperationId values to operation contracts before stages and pipelines create occurrences.
// - Keep unique operation definition storage separate from AtlasOperationSet occurrence sequences.
// - Preserve deterministic catalog order for diagnostics, tooling, and tests.
//
// Design notes
// - This is a unique catalog, not an operation occurrence list.
// - Duplicate operation ids are rejected.
// - Duplicate debug names are rejected because catalog-level lookup by debug name must be unambiguous.
// - Repeated operation usage belongs to AtlasOperationSet, not this catalog.
// - This catalog does not imply executability and does not create operation executors.
// - The all-zero/default AtlasOperationId is valid when explicitly cataloged.

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Immutable catalog of unique Atlas operation definitions.
    /// </summary>
    public sealed class AtlasOperationCatalog :
        IReadOnlyList<AtlasOperationDefinition>,
        IAtlasOperationDefinitionProvider
    {
        private readonly AtlasOperationDefinition[] _operations;
        private readonly Dictionary<AtlasOperationId, int> _indexByOperationId;
        private readonly Dictionary<FixedString64Bytes, int> _indexByDebugName;

        private AtlasOperationCatalog(
            FixedString64Bytes name,
            AtlasOperationDefinition[] operations)
        {
            Name = name;
            _operations = CopyAndValidateOperations(name, operations);
            _indexByOperationId = BuildOperationIdLookup(_operations);
            _indexByDebugName = BuildDebugNameLookup(_operations);
        }

        /// <summary>
        /// Gets an empty operation catalog.
        /// </summary>
        public static AtlasOperationCatalog Empty { get; } =
            new AtlasOperationCatalog(default, Array.Empty<AtlasOperationDefinition>());

        /// <summary>
        /// Diagnostic catalog name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Gets the number of unique operation definitions in this catalog.
        /// </summary>
        public int Count => _operations.Length;

        /// <summary>
        /// Gets whether this catalog contains no operation definitions.
        /// </summary>
        public bool IsEmpty => _operations.Length == 0;

        /// <summary>
        /// Gets the operation definition at a deterministic catalog index.
        /// </summary>
        public AtlasOperationDefinition this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _operations[index];
            }
        }

        /// <summary>
        /// Gets the operation definition registered for a durable operation id.
        /// </summary>
        public AtlasOperationDefinition this[AtlasOperationId operationId] =>
            GetRequiredOperation(operationId);

        /// <summary>
        /// Gets the operation definition registered for a diagnostic debug name.
        /// </summary>
        public AtlasOperationDefinition this[FixedString64Bytes debugName] =>
            GetRequiredOperation(debugName);

        /// <summary>
        /// Creates an unnamed unique operation catalog.
        /// </summary>
        public static AtlasOperationCatalog Create(
            params AtlasOperationDefinition[] operations)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            if (operations.Length == 0)
            {
                return Empty;
            }

            return new AtlasOperationCatalog(default, operations);
        }

        /// <summary>
        /// Creates a named unique operation catalog.
        /// </summary>
        public static AtlasOperationCatalog Create(
            FixedString64Bytes name,
            params AtlasOperationDefinition[] operations)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            if (operations.Length == 0 && name.IsEmpty)
            {
                return Empty;
            }

            return new AtlasOperationCatalog(name, operations);
        }

        /// <summary>
        /// Creates a named unique operation catalog from an enumerable source.
        /// </summary>
        public static AtlasOperationCatalog Create(
            FixedString64Bytes name,
            IEnumerable<AtlasOperationDefinition> operations)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            var list = new List<AtlasOperationDefinition>();

            foreach (var operation in operations)
            {
                list.Add(operation);
            }

            if (list.Count == 0 && name.IsEmpty)
            {
                return Empty;
            }

            return new AtlasOperationCatalog(name, list.ToArray());
        }

        /// <summary>
        /// Determines whether this catalog contains the supplied operation id.
        /// </summary>
        public bool Contains(
            AtlasOperationId operationId)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            return _indexByOperationId.ContainsKey(operationId);
        }

        /// <summary>
        /// Determines whether this catalog contains the supplied debug name.
        /// </summary>
        public bool Contains(
            FixedString64Bytes debugName)
        {
            if (debugName.IsEmpty)
            {
                return false;
            }

            return _indexByDebugName.ContainsKey(debugName);
        }

        /// <summary>
        /// Attempts to resolve the catalog index for a durable operation id.
        /// </summary>
        public bool TryGetIndex(
            AtlasOperationId operationId,
            out int index)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            if (_indexByOperationId.TryGetValue(operationId, out index))
            {
                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Attempts to resolve the catalog index for a diagnostic debug name.
        /// </summary>
        public bool TryGetIndex(
            FixedString64Bytes debugName,
            out int index)
        {
            if (!debugName.IsEmpty &&
                _indexByDebugName.TryGetValue(debugName, out index))
            {
                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Attempts to resolve an operation definition by durable operation id.
        /// </summary>
        public bool TryGetOperation(
            AtlasOperationId operationId,
            out AtlasOperationDefinition operation)
        {
            if (TryGetIndex(operationId, out var index))
            {
                operation = _operations[index];
                return true;
            }

            operation = null;
            return false;
        }

        /// <summary>
        /// Attempts to resolve an operation definition by diagnostic debug name.
        /// </summary>
        public bool TryGetOperation(
            FixedString64Bytes debugName,
            out AtlasOperationDefinition operation)
        {
            if (TryGetIndex(debugName, out var index))
            {
                operation = _operations[index];
                return true;
            }

            operation = null;
            return false;
        }

        /// <summary>
        /// Resolves an operation definition by durable operation id.
        /// </summary>
        public AtlasOperationDefinition GetRequiredOperation(
            AtlasOperationId operationId)
        {
            if (TryGetOperation(operationId, out var operation))
            {
                return operation;
            }

            throw new KeyNotFoundException(
                $"Atlas operation catalog '{GetDiagnosticName()}' does not contain operation id '{operationId}'.");
        }

        /// <summary>
        /// Resolves an operation definition by diagnostic debug name.
        /// </summary>
        public AtlasOperationDefinition GetRequiredOperation(
            FixedString64Bytes debugName)
        {
            if (TryGetOperation(debugName, out var operation))
            {
                return operation;
            }

            throw new KeyNotFoundException(
                $"Atlas operation catalog '{GetDiagnosticName()}' does not contain operation debug name '{debugName}'.");
        }

        /// <summary>
        /// Builds an operation occurrence sequence from cataloged operation ids.
        /// </summary>
        public AtlasOperationSet CreateOperationSet(
            FixedString64Bytes setName,
            params AtlasOperationId[] operationIds)
        {
            if (operationIds == null)
            {
                throw new ArgumentNullException(nameof(operationIds));
            }

            var operations = new AtlasOperationDefinition[operationIds.Length];

            for (var i = 0; i < operationIds.Length; i++)
            {
                operations[i] = GetRequiredOperation(operationIds[i]);
            }

            return AtlasOperationSet.Create(setName, operations);
        }

        /// <summary>
        /// Builds an unnamed operation occurrence sequence from cataloged operation ids.
        /// </summary>
        public AtlasOperationSet CreateOperationSet(
            params AtlasOperationId[] operationIds)
        {
            return CreateOperationSet(default, operationIds);
        }

        /// <summary>
        /// Copies cataloged operation definitions into a caller-provided destination array.
        /// </summary>
        public void CopyTo(
            AtlasOperationDefinition[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _operations.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than operation catalog count '{_operations.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_operations, destination, _operations.Length);
        }

        /// <summary>
        /// Creates a managed copy of cataloged operation definitions in deterministic catalog order.
        /// </summary>
        public AtlasOperationDefinition[] ToArray()
        {
            var copy = new AtlasOperationDefinition[_operations.Length];
            Array.Copy(_operations, copy, _operations.Length);
            return copy;
        }

        /// <summary>
        /// Gets an enumerator over operation definitions in deterministic catalog order.
        /// </summary>
        public IEnumerator<AtlasOperationDefinition> GetEnumerator()
        {
            for (var i = 0; i < _operations.Length; i++)
            {
                yield return _operations[i];
            }
        }

        /// <summary>
        /// Gets an enumerator over operation definitions in deterministic catalog order.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this catalog.
        /// </summary>
        public override string ToString()
        {
            return $"AtlasOperationCatalog(Name={GetDiagnosticName()}, Count={Count})";
        }

        private static AtlasOperationDefinition[] CopyAndValidateOperations(
            FixedString64Bytes catalogName,
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

                ValidateOperationOrThrow(catalogName, operation, i);

                copy[i] = operation;
            }

            ValidateNoDuplicateOperationIds(catalogName, copy);
            ValidateNoDuplicateDebugNames(catalogName, copy);

            return copy;
        }

        private static Dictionary<AtlasOperationId, int> BuildOperationIdLookup(
            AtlasOperationDefinition[] operations)
        {
            var lookup = new Dictionary<AtlasOperationId, int>(operations.Length);

            for (var i = 0; i < operations.Length; i++)
            {
                lookup.Add(operations[i].OperationId, i);
            }

            return lookup;
        }

        private static Dictionary<FixedString64Bytes, int> BuildDebugNameLookup(
            AtlasOperationDefinition[] operations)
        {
            var lookup = new Dictionary<FixedString64Bytes, int>(operations.Length);

            for (var i = 0; i < operations.Length; i++)
            {
                lookup.Add(operations[i].DebugName, i);
            }

            return lookup;
        }

        private static void ValidateOperationOrThrow(
            FixedString64Bytes catalogName,
            AtlasOperationDefinition operation,
            int index)
        {
            if (operation == null)
            {
                throw new ArgumentException(
                    $"Atlas operation catalog '{GetDiagnosticName(catalogName)}' contains a null operation definition at index '{index}'.",
                    nameof(operation));
            }

            operation.OperationId.ValidateOrThrow($"operations[{index}].OperationId");

            if (operation.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas operation catalog '{GetDiagnosticName(catalogName)}' contains an operation definition with an empty debug name at index '{index}'.",
                    nameof(operation));
            }

            if (operation.Role == AtlasOperationRole.None)
            {
                throw new ArgumentException(
                    $"Atlas operation catalog '{GetDiagnosticName(catalogName)}' contains operation '{operation.DebugName}' with no semantic role at index '{index}'.",
                    nameof(operation));
            }
        }

        private static void ValidateNoDuplicateOperationIds(
            FixedString64Bytes catalogName,
            AtlasOperationDefinition[] operations)
        {
            for (var i = 0; i < operations.Length; i++)
            {
                var left = operations[i];

                for (var j = i + 1; j < operations.Length; j++)
                {
                    var right = operations[j];

                    if (left.OperationId != right.OperationId)
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Atlas operation catalog '{GetDiagnosticName(catalogName)}' contains duplicate operation id '{left.OperationId}' at indices '{i}' and '{j}'. " +
                        $"Left operation '{left.DebugName}', right operation '{right.DebugName}'.",
                        nameof(operations));
                }
            }
        }

        private static void ValidateNoDuplicateDebugNames(
            FixedString64Bytes catalogName,
            AtlasOperationDefinition[] operations)
        {
            for (var i = 0; i < operations.Length; i++)
            {
                var left = operations[i];

                for (var j = i + 1; j < operations.Length; j++)
                {
                    var right = operations[j];

                    if (!left.DebugName.Equals(right.DebugName))
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Atlas operation catalog '{GetDiagnosticName(catalogName)}' contains duplicate operation debug name '{left.DebugName}' at indices '{i}' and '{j}'. " +
                        $"Left operation id '{left.OperationId}', right operation id '{right.OperationId}'.",
                        nameof(operations));
                }
            }
        }

        private static string GetDiagnosticName(
            FixedString64Bytes name)
        {
            return name.IsEmpty ? "<unnamed>" : name.ToString();
        }

        private string GetDiagnosticName()
        {
            return GetDiagnosticName(Name);
        }

        private void ThrowIfIndexOutOfRange(
            int index)
        {
            if ((uint)index < (uint)_operations.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Operation catalog index must be between 0 and {_operations.Length - 1}.");
        }
    }
}
