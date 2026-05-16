// Packages/com.lokrain.atlas/Runtime/Executors/AtlasOperationExecutorRegistry.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Executors
//
// Purpose
// - Store the managed executor table used by Atlas operation runners.
// - Resolve durable AtlasOperationId values to concrete IAtlasOperationExecutor instances.
// - Preserve deterministic registration order for diagnostics.
// - Keep executor dispatch separate from compiled plans, workspace memory, jobs, artifacts, and debug rendering.
//
// Design notes
// - This registry is immutable after creation.
// - Registration order is preserved.
// - Lookup is by AtlasOperationId, not by C# type.
// - Duplicate operation ids are rejected.
// - The all-zero/default AtlasOperationId is valid if the operation catalog defines it.
// - Missing executor state is represented by bool-returning lookup APIs or explicit exceptions.
// - Concrete jobs are not registered here; managed operation executors are.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Operations;

namespace Lokrain.Atlas.Executors
{
    /// <summary>
    /// Immutable registry mapping durable operation identities to managed operation executors.
    /// </summary>
    public sealed class AtlasOperationExecutorRegistry :
        IReadOnlyList<IAtlasOperationExecutor>
    {
        private readonly IAtlasOperationExecutor[] _executors;
        private readonly Dictionary<AtlasOperationId, int> _indexByOperationId;

        private AtlasOperationExecutorRegistry(
            IAtlasOperationExecutor[] executors)
        {
            _executors = CopyAndValidateExecutors(executors);
            _indexByOperationId = BuildLookup(_executors);
        }

        /// <summary>
        /// Gets an empty executor registry.
        /// </summary>
        public static AtlasOperationExecutorRegistry Empty { get; } =
            new AtlasOperationExecutorRegistry(Array.Empty<IAtlasOperationExecutor>());

        /// <summary>
        /// Gets the number of registered executors.
        /// </summary>
        public int Count => _executors.Length;

        /// <summary>
        /// Gets whether this registry contains no executors.
        /// </summary>
        public bool IsEmpty => _executors.Length == 0;

        /// <summary>
        /// Gets the registered executor at a deterministic registration index.
        /// </summary>
        public IAtlasOperationExecutor this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _executors[index];
            }
        }

        /// <summary>
        /// Gets the registered executor for an operation id.
        /// </summary>
        public IAtlasOperationExecutor this[AtlasOperationId operationId] =>
            GetRequiredExecutor(operationId);

        /// <summary>
        /// Creates an immutable registry from explicitly ordered executor instances.
        /// </summary>
        public static AtlasOperationExecutorRegistry Create(
            params IAtlasOperationExecutor[] executors)
        {
            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            if (executors.Length == 0)
            {
                return Empty;
            }

            return new AtlasOperationExecutorRegistry(executors);
        }

        /// <summary>
        /// Creates an immutable registry from an enumerable executor source.
        /// </summary>
        public static AtlasOperationExecutorRegistry Create(
            IEnumerable<IAtlasOperationExecutor> executors)
        {
            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            var list = new List<IAtlasOperationExecutor>();

            foreach (var executor in executors)
            {
                list.Add(executor);
            }

            if (list.Count == 0)
            {
                return Empty;
            }

            return new AtlasOperationExecutorRegistry(list.ToArray());
        }

        /// <summary>
        /// Returns whether an executor is registered for the supplied operation id.
        /// </summary>
        public bool Contains(
            AtlasOperationId operationId)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            return _indexByOperationId.ContainsKey(operationId);
        }

        /// <summary>
        /// Returns whether an executor is registered for the supplied operation id.
        /// </summary>
        public bool ContainsExecutor(
            AtlasOperationId operationId)
        {
            return Contains(operationId);
        }

        /// <summary>
        /// Attempts to get the registration index for an operation id.
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
        /// Attempts to get the executor registered for an operation id.
        /// </summary>
        public bool TryGetExecutor(
            AtlasOperationId operationId,
            out IAtlasOperationExecutor executor)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            if (_indexByOperationId.TryGetValue(operationId, out var index))
            {
                executor = _executors[index];
                return true;
            }

            executor = null;
            return false;
        }

        /// <summary>
        /// Gets the executor registered for an operation id.
        /// </summary>
        public IAtlasOperationExecutor GetRequiredExecutor(
            AtlasOperationId operationId)
        {
            if (TryGetExecutor(operationId, out var executor))
            {
                return executor;
            }

            throw new KeyNotFoundException(
                $"No Atlas operation executor is registered for operation id '{operationId}'.");
        }

        /// <summary>
        /// Copies registered executors into a caller-provided destination array.
        /// </summary>
        public void CopyTo(
            IAtlasOperationExecutor[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _executors.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than executor count '{_executors.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_executors, destination, _executors.Length);
        }

        /// <summary>
        /// Creates a managed copy of registered executors in deterministic registration order.
        /// </summary>
        public IAtlasOperationExecutor[] ToArray()
        {
            var copy = new IAtlasOperationExecutor[_executors.Length];
            Array.Copy(_executors, copy, _executors.Length);
            return copy;
        }

        /// <summary>
        /// Gets an enumerator over registered executors in deterministic registration order.
        /// </summary>
        public IEnumerator<IAtlasOperationExecutor> GetEnumerator()
        {
            for (var i = 0; i < _executors.Length; i++)
            {
                yield return _executors[i];
            }
        }

        /// <summary>
        /// Gets an enumerator over registered executors in deterministic registration order.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this registry.
        /// </summary>
        public override string ToString()
        {
            return $"AtlasOperationExecutorRegistry(Count={Count})";
        }

        private static IAtlasOperationExecutor[] CopyAndValidateExecutors(
            IAtlasOperationExecutor[] executors)
        {
            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            var copy = new IAtlasOperationExecutor[executors.Length];

            for (var i = 0; i < executors.Length; i++)
            {
                var executor = executors[i];

                ValidateExecutorOrThrow(
                    executor,
                    i);

                copy[i] = executor;
            }

            ValidateNoDuplicateOperationIds(copy);

            return copy;
        }

        private static Dictionary<AtlasOperationId, int> BuildLookup(
            IAtlasOperationExecutor[] executors)
        {
            var lookup = new Dictionary<AtlasOperationId, int>(executors.Length);

            for (var i = 0; i < executors.Length; i++)
            {
                lookup.Add(executors[i].OperationId, i);
            }

            return lookup;
        }

        private static void ValidateExecutorOrThrow(
            IAtlasOperationExecutor executor,
            int index)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(
                    nameof(executor),
                    $"Atlas operation executor at registration index '{index}' is null.");
            }

            executor.OperationId.ValidateOrThrow(
                $"executors[{index}].OperationId");

            if (executor.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas operation executor at registration index '{index}' has an empty debug name.",
                    nameof(executor));
            }
        }

        private static void ValidateNoDuplicateOperationIds(
            IAtlasOperationExecutor[] executors)
        {
            for (var i = 0; i < executors.Length; i++)
            {
                var left = executors[i];

                for (var j = i + 1; j < executors.Length; j++)
                {
                    var right = executors[j];

                    if (left.OperationId != right.OperationId)
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Duplicate Atlas operation executor registration for operation id '{left.OperationId}' at indices '{i}' and '{j}'. " +
                        $"Left executor '{left.DebugName}', right executor '{right.DebugName}'.",
                        nameof(executors));
                }
            }
        }

        private void ThrowIfIndexOutOfRange(
            int index)
        {
            if ((uint)index < (uint)_executors.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Executor index must be between 0 and {_executors.Length - 1}.");
        }
    }
}