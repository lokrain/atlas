// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasContractCatalog.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Define a unique catalog of durable Atlas field contracts.
// - Resolve StableDataId values to contract metadata before contract tables assign slots.
// - Keep unique contract storage separate from AtlasContractTable slot assignment.
// - Preserve deterministic catalog order for diagnostics, tooling, generated docs, and tests.
//
// Design notes
// - This is a unique contract catalog, not a slotted contract table.
// - Duplicate stable ids are rejected.
// - Multiple versions of the same durable field identity are rejected inside one catalog.
// - Duplicate debug names are rejected because catalog-level lookup by debug name must be unambiguous.
// - Table-local slots are stripped at the catalog boundary.
// - Contract table creation from selected ids assigns fresh canonical zero-based slots.
// - The all-zero/default StableDataId is valid when explicitly cataloged.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Immutable catalog of unique Atlas field contracts.
    /// </summary>
    public sealed class AtlasContractCatalog :
        IReadOnlyList<AtlasContract>,
        IAtlasContractProvider
    {
        private readonly AtlasContract[] _contracts;
        private readonly Dictionary<StableDataId, int> _indexByStableId;
        private readonly Dictionary<FixedString64Bytes, int> _indexByDebugName;

        private AtlasContractCatalog(
            FixedString64Bytes name,
            AtlasContract[] contracts)
        {
            Name = name;
            _contracts = CopyAndValidateContracts(name, contracts);
            _indexByStableId = BuildStableIdLookup(_contracts);
            _indexByDebugName = BuildDebugNameLookup(_contracts);
        }

        /// <summary>
        /// Gets an empty contract catalog.
        /// </summary>
        public static AtlasContractCatalog Empty { get; } =
            new AtlasContractCatalog(default, Array.Empty<AtlasContract>());

        /// <summary>
        /// Diagnostic catalog name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Gets the number of unique contracts in this catalog.
        /// </summary>
        public int Count => _contracts.Length;

        /// <summary>
        /// Gets whether this catalog contains no contracts.
        /// </summary>
        public bool IsEmpty => _contracts.Length == 0;

        /// <summary>
        /// Gets the contract at a deterministic catalog index.
        /// </summary>
        public AtlasContract this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _contracts[index];
            }
        }

        /// <summary>
        /// Gets the contract registered for a durable field id.
        /// </summary>
        public AtlasContract this[StableDataId stableId] =>
            GetRequiredContract(stableId);

        /// <summary>
        /// Gets the contract registered for a diagnostic debug name.
        /// </summary>
        public AtlasContract this[FixedString64Bytes debugName] =>
            GetRequiredContract(debugName);

        /// <summary>
        /// Creates an unnamed unique contract catalog.
        /// </summary>
        public static AtlasContractCatalog Create(
            params AtlasContract[] contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (contracts.Length == 0)
            {
                return Empty;
            }

            return new AtlasContractCatalog(default, contracts);
        }

        /// <summary>
        /// Creates a named unique contract catalog.
        /// </summary>
        public static AtlasContractCatalog Create(
            FixedString64Bytes name,
            params AtlasContract[] contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (contracts.Length == 0 && name.IsEmpty)
            {
                return Empty;
            }

            return new AtlasContractCatalog(name, contracts);
        }

        /// <summary>
        /// Creates a named unique contract catalog from an enumerable source.
        /// </summary>
        public static AtlasContractCatalog Create(
            FixedString64Bytes name,
            IEnumerable<AtlasContract> contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var list = new List<AtlasContract>();

            foreach (var contract in contracts)
            {
                list.Add(contract);
            }

            if (list.Count == 0 && name.IsEmpty)
            {
                return Empty;
            }

            return new AtlasContractCatalog(name, list.ToArray());
        }

        /// <summary>
        /// Determines whether this catalog contains the supplied stable field id.
        /// </summary>
        public bool Contains(
            StableDataId stableId)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return _indexByStableId.ContainsKey(stableId);
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
        /// Attempts to resolve the catalog index for a durable field id.
        /// </summary>
        public bool TryGetIndex(
            StableDataId stableId,
            out int index)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            if (_indexByStableId.TryGetValue(stableId, out index))
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
        /// Attempts to resolve a contract by durable field id.
        /// </summary>
        public bool TryGetContract(
            StableDataId stableId,
            out AtlasContract contract)
        {
            if (TryGetIndex(stableId, out var index))
            {
                contract = _contracts[index];
                return true;
            }

            contract = default;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a contract by diagnostic debug name.
        /// </summary>
        public bool TryGetContract(
            FixedString64Bytes debugName,
            out AtlasContract contract)
        {
            if (TryGetIndex(debugName, out var index))
            {
                contract = _contracts[index];
                return true;
            }

            contract = default;
            return false;
        }

        /// <summary>
        /// Resolves a contract by durable field id.
        /// </summary>
        public AtlasContract GetRequiredContract(
            StableDataId stableId)
        {
            if (TryGetContract(stableId, out var contract))
            {
                return contract;
            }

            throw new KeyNotFoundException(
                $"Atlas contract catalog '{GetDiagnosticName()}' does not contain field id '{stableId}'.");
        }

        /// <summary>
        /// Resolves a contract by diagnostic debug name.
        /// </summary>
        public AtlasContract GetRequiredContract(
            FixedString64Bytes debugName)
        {
            if (TryGetContract(debugName, out var contract))
            {
                return contract;
            }

            throw new KeyNotFoundException(
                $"Atlas contract catalog '{GetDiagnosticName()}' does not contain contract debug name '{debugName}'.");
        }

        /// <summary>
        /// Builds a freshly slotted contract table from cataloged field ids.
        /// </summary>
        public AtlasContractTable CreateContractTable(
            FixedString64Bytes tableName,
            params StableDataId[] stableIds)
        {
            if (stableIds == null)
            {
                throw new ArgumentNullException(nameof(stableIds));
            }

            var contracts = new AtlasContract[stableIds.Length];

            for (var i = 0; i < stableIds.Length; i++)
            {
                contracts[i] = GetRequiredContract(stableIds[i]).WithoutSlot();
            }

            return AtlasContractTable.Create(tableName, contracts);
        }

        /// <summary>
        /// Builds an unnamed freshly slotted contract table from cataloged field ids.
        /// </summary>
        public AtlasContractTable CreateContractTable(
            params StableDataId[] stableIds)
        {
            return CreateContractTable(default, stableIds);
        }

        /// <summary>
        /// Copies cataloged contracts into a caller-provided destination array.
        /// </summary>
        public void CopyTo(
            AtlasContract[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _contracts.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than contract catalog count '{_contracts.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_contracts, destination, _contracts.Length);
        }

        /// <summary>
        /// Creates a managed copy of cataloged contracts in deterministic catalog order.
        /// </summary>
        public AtlasContract[] ToArray()
        {
            var copy = new AtlasContract[_contracts.Length];
            Array.Copy(_contracts, copy, _contracts.Length);
            return copy;
        }

        /// <summary>
        /// Gets an enumerator over contracts in deterministic catalog order.
        /// </summary>
        public IEnumerator<AtlasContract> GetEnumerator()
        {
            for (var i = 0; i < _contracts.Length; i++)
            {
                yield return _contracts[i];
            }
        }

        /// <summary>
        /// Gets an enumerator over contracts in deterministic catalog order.
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
            return $"AtlasContractCatalog(Name={GetDiagnosticName()}, Count={Count})";
        }

        private static AtlasContract[] CopyAndValidateContracts(
            FixedString64Bytes catalogName,
            AtlasContract[] contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var copy = new AtlasContract[contracts.Length];

            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];

                ValidateContractOrThrow(catalogName, contract, i);

                copy[i] = contract.WithoutSlot();
            }

            ValidateNoDuplicateStableIds(catalogName, copy);
            ValidateNoDuplicateDurableIdentities(catalogName, copy);
            ValidateNoDuplicateDebugNames(catalogName, copy);

            return copy;
        }

        private static Dictionary<StableDataId, int> BuildStableIdLookup(
            AtlasContract[] contracts)
        {
            var lookup = new Dictionary<StableDataId, int>(contracts.Length);

            for (var i = 0; i < contracts.Length; i++)
            {
                lookup.Add(contracts[i].StableId, i);
            }

            return lookup;
        }

        private static Dictionary<FixedString64Bytes, int> BuildDebugNameLookup(
            AtlasContract[] contracts)
        {
            var lookup = new Dictionary<FixedString64Bytes, int>(contracts.Length);

            for (var i = 0; i < contracts.Length; i++)
            {
                lookup.Add(contracts[i].DebugName, i);
            }

            return lookup;
        }

        private static void ValidateContractOrThrow(
            FixedString64Bytes catalogName,
            AtlasContract contract,
            int index)
        {
            try
            {
                contract.ValidateOrThrow($"contracts[{index}]");
            }
            catch (Exception exception) when (exception is ArgumentException || exception is AtlasException)
            {
                throw new ArgumentException(
                    $"Atlas contract catalog '{GetDiagnosticName(catalogName)}' contains an invalid contract at index '{index}'. {exception.Message}",
                    nameof(contract),
                    exception);
            }
        }

        private static void ValidateNoDuplicateStableIds(
            FixedString64Bytes catalogName,
            AtlasContract[] contracts)
        {
            for (var i = 0; i < contracts.Length; i++)
            {
                var left = contracts[i];

                for (var j = i + 1; j < contracts.Length; j++)
                {
                    var right = contracts[j];

                    if (left.StableId != right.StableId)
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Atlas contract catalog '{GetDiagnosticName(catalogName)}' contains duplicate stable id '{left.StableId}' at indices '{i}' and '{j}'. " +
                        $"Left contract '{left.DebugName}', right contract '{right.DebugName}'.",
                        nameof(contracts));
                }
            }
        }

        private static void ValidateNoDuplicateDurableIdentities(
            FixedString64Bytes catalogName,
            AtlasContract[] contracts)
        {
            for (var i = 0; i < contracts.Length; i++)
            {
                var left = contracts[i];

                for (var j = i + 1; j < contracts.Length; j++)
                {
                    var right = contracts[j];

                    if (!left.StableId.HasSameIdentityAs(right.StableId))
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Atlas contract catalog '{GetDiagnosticName(catalogName)}' contains multiple versions of durable field identity " +
                        $"'{left.StableId.High:X16}-{left.StableId.Low:X16}' at indices '{i}' and '{j}'. " +
                        $"Left contract '{left.DebugName}' version '{left.StableId.Version}', " +
                        $"right contract '{right.DebugName}' version '{right.StableId.Version}'.",
                        nameof(contracts));
                }
            }
        }

        private static void ValidateNoDuplicateDebugNames(
            FixedString64Bytes catalogName,
            AtlasContract[] contracts)
        {
            for (var i = 0; i < contracts.Length; i++)
            {
                var left = contracts[i];

                for (var j = i + 1; j < contracts.Length; j++)
                {
                    var right = contracts[j];

                    if (!left.DebugName.Equals(right.DebugName))
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Atlas contract catalog '{GetDiagnosticName(catalogName)}' contains duplicate contract debug name '{left.DebugName}' at indices '{i}' and '{j}'. " +
                        $"Left field id '{left.StableId}', right field id '{right.StableId}'.",
                        nameof(contracts));
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
            if ((uint)index < (uint)_contracts.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Contract catalog index must be between 0 and {_contracts.Length - 1}.");
        }
    }
}
