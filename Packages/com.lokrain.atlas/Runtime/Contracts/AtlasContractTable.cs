// Runtime/Contracts/AtlasContractTable.cs

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Immutable ordered catalog of Atlas Field Contracts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contract-table order is the canonical source of Field slots. A Contract's slot is assigned
    /// from its index in this table. Slots are table-local execution identifiers; they are not
    /// durable Field identity and must not be serialized as long-lived Field references.
    /// </para>
    ///
    /// <para>
    /// The table copies input Contracts, validates each Contract, assigns canonical slots, rejects
    /// duplicate identities, and builds lookup data used by plan validation, storage allocation,
    /// shape resolution, hashing, and typed Field resolution.
    /// </para>
    ///
    /// <para>
    /// Runtime jobs should not receive Contract tables. Jobs should receive already-resolved typed
    /// native memory produced from validated tables, compiled plans, and runtime workspaces.
    /// </para>
    /// </remarks>
    public sealed class AtlasContractTable :
        IReadOnlyList<AtlasContract>
    {
        private readonly AtlasContract[] _contracts;
        private readonly Dictionary<StableDataId, AtlasFieldSlot> _slotsByStableId;

        /// <summary>
        /// Diagnostic table name used by validation reports, editor tooling, exceptions, hashes, and tests.
        /// </summary>
        /// <remarks>
        /// Table names are not durable Field identity. Durable identity belongs to <see cref="StableDataId"/>.
        /// </remarks>
        public readonly FixedString64Bytes Name;

        private AtlasContractTable(
            FixedString64Bytes name,
            AtlasContract[] contracts)
        {
            Name = name;
            _contracts = BuildCanonicalContractArray(contracts);
            _slotsByStableId = BuildSlotLookup(_contracts);
        }

        /// <summary>
        /// Gets the number of Contracts in this table.
        /// </summary>
        public int Count => _contracts.Length;

        /// <summary>
        /// Gets whether this table contains no Contracts.
        /// </summary>
        public bool IsEmpty => _contracts.Length == 0;

        /// <summary>
        /// Gets the Contract at a zero-based canonical table index.
        /// </summary>
        /// <param name="index">Zero-based Contract-table index.</param>
        /// <returns>The Contract assigned to the requested index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this table's Contract range.
        /// </exception>
        public AtlasContract this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _contracts[index];
            }
        }

        /// <summary>
        /// Gets the Contract assigned to a canonical Field slot.
        /// </summary>
        /// <param name="slot">Canonical Field slot.</param>
        /// <returns>The Contract assigned to the requested slot.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="slot"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slot"/> is outside this table's Contract range.
        /// </exception>
        public AtlasContract this[AtlasFieldSlot slot]
        {
            get
            {
                slot.ThrowIfInvalid();
                return this[slot.Index];
            }
        }

        /// <summary>
        /// Creates a Contract table from explicitly ordered Contracts.
        /// </summary>
        /// <param name="contracts">
        /// Contracts in canonical slot order. Contracts may be unslotted, or already slotted
        /// with slots matching their table position.
        /// </param>
        /// <returns>A validated immutable Contract table.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when Contracts are invalid, duplicated, inconsistently slotted, or exceed Atlas slot capacity.
        /// </exception>
        public static AtlasContractTable Create(params AtlasContract[] contracts)
        {
            return new AtlasContractTable(default, contracts);
        }

        /// <summary>
        /// Creates a named Contract table from explicitly ordered Contracts.
        /// </summary>
        /// <param name="name">Diagnostic table name.</param>
        /// <param name="contracts">
        /// Contracts in canonical slot order. Contracts may be unslotted, or already slotted
        /// with slots matching their table position.
        /// </param>
        /// <returns>A validated immutable Contract table.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when Contracts are invalid, duplicated, inconsistently slotted, or exceed Atlas slot capacity.
        /// </exception>
        public static AtlasContractTable Create(
            FixedString64Bytes name,
            params AtlasContract[] contracts)
        {
            return new AtlasContractTable(name, contracts);
        }

        /// <summary>
        /// Determines whether this table contains a Contract with the supplied stable Field identifier.
        /// </summary>
        /// <param name="stableId">Stable Field identifier to search for.</param>
        /// <returns><c>true</c> when this table contains the identifier; otherwise, <c>false</c>.</returns>
        public bool Contains(StableDataId stableId)
        {
            return stableId.IsValid &&
                   _slotsByStableId.ContainsKey(stableId);
        }

        /// <summary>
        /// Determines whether this table contains the supplied typed Field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns><c>true</c> when this table contains the typed Field; otherwise, <c>false</c>.</returns>
        public bool Contains<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Contains(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to resolve a stable Field identifier to its canonical table slot.
        /// </summary>
        /// <param name="stableId">Stable Field identifier to resolve.</param>
        /// <param name="slot">
        /// The resolved slot when the identifier is present; otherwise, <see cref="AtlasFieldSlot.Invalid"/>.
        /// </param>
        /// <returns><c>true</c> when the identifier was resolved; otherwise, <c>false</c>.</returns>
        public bool TryGetSlot(
            StableDataId stableId,
            out AtlasFieldSlot slot)
        {
            if (!stableId.IsValid)
            {
                slot = AtlasFieldSlot.Invalid;
                return false;
            }

            return _slotsByStableId.TryGetValue(stableId, out slot);
        }

        /// <summary>
        /// Attempts to resolve a typed Field declaration to its canonical table slot.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <param name="slot">
        /// The resolved slot when the typed Field is present; otherwise, <see cref="AtlasFieldSlot.Invalid"/>.
        /// </param>
        /// <returns><c>true</c> when the typed Field was resolved; otherwise, <c>false</c>.</returns>
        public bool TryGetSlot<TField, TElement>(out AtlasFieldSlot slot)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetSlot(AtlasField.StableId<TField, TElement>(), out slot);
        }

        /// <summary>
        /// Resolves a stable Field identifier to its canonical table slot.
        /// </summary>
        /// <param name="stableId">Stable Field identifier to resolve.</param>
        /// <returns>The canonical Field slot assigned to <paramref name="stableId"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is invalid or not present in this table.
        /// </exception>
        public AtlasFieldSlot GetRequiredSlot(StableDataId stableId)
        {
            if (TryGetSlot(stableId, out var slot))
            {
                return slot;
            }

            throw new ArgumentException(
                $"Atlas Contract table '{GetDiagnosticName()}' does not contain Field id '{stableId}'.",
                nameof(stableId));
        }

        /// <summary>
        /// Resolves a typed Field declaration to its canonical table slot.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>The canonical Field slot assigned to the typed Field.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the typed Field is not present in this table.
        /// </exception>
        public AtlasFieldSlot GetRequiredSlot<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredSlot(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to resolve a stable Field identifier to its Contract.
        /// </summary>
        /// <param name="stableId">Stable Field identifier to resolve.</param>
        /// <param name="contract">
        /// The resolved Contract when the identifier is present; otherwise, <see cref="AtlasContract.Empty"/>.
        /// </param>
        /// <returns><c>true</c> when the Contract was resolved; otherwise, <c>false</c>.</returns>
        public bool TryGetContract(
            StableDataId stableId,
            out AtlasContract contract)
        {
            if (TryGetSlot(stableId, out var slot))
            {
                contract = this[slot];
                return true;
            }

            contract = AtlasContract.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a typed Field declaration to its Contract.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <param name="contract">
        /// The resolved Contract when the typed Field is present; otherwise, <see cref="AtlasContract.Empty"/>.
        /// </param>
        /// <returns><c>true</c> when the Contract was resolved; otherwise, <c>false</c>.</returns>
        public bool TryGetContract<TField, TElement>(out AtlasContract contract)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetContract(AtlasField.StableId<TField, TElement>(), out contract);
        }

        /// <summary>
        /// Resolves a stable Field identifier to its Contract.
        /// </summary>
        /// <param name="stableId">Stable Field identifier to resolve.</param>
        /// <returns>The Contract assigned to <paramref name="stableId"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is invalid or not present in this table.
        /// </exception>
        public AtlasContract GetRequiredContract(StableDataId stableId)
        {
            return this[GetRequiredSlot(stableId)];
        }

        /// <summary>
        /// Resolves a typed Field declaration to its Contract.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>The Contract assigned to the typed Field.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the typed Field is not present in this table.
        /// </exception>
        public AtlasContract GetRequiredContract<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredContract(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Creates a resolved typed handle for a Field declaration contained in this table.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>A resolved Field handle containing stable identity and canonical slot.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the typed Field is not present in this table or its storage format is incompatible.
        /// </exception>
        public AtlasFieldHandle<TField, TElement> GetHandle<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            var contract = GetRequiredContract<TField, TElement>();

            contract.StorageFormat.ValidateElementTypeOrThrow<TElement>(nameof(contract));

            return AtlasFieldHandle<TField, TElement>.Resolved(contract.Slot);
        }

        /// <summary>
        /// Copies Contracts from this table into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array that receives Contracts in canonical order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is smaller than <see cref="Count"/>.
        /// </exception>
        public void CopyTo(AtlasContract[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _contracts.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than Contract count '{_contracts.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_contracts, destination, _contracts.Length);
        }

        /// <summary>
        /// Copies a Contract range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source index in this table.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of Contracts to copy.</param>
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
            AtlasContract[] destination,
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

            if (sourceIndex + length > _contracts.Length)
            {
                throw new ArgumentException(
                    "Source range exceeds Contract table bounds.",
                    nameof(length));
            }

            if (destinationIndex + length > destination.Length)
            {
                throw new ArgumentException(
                    "Destination range exceeds destination array bounds.",
                    nameof(length));
            }

            Array.Copy(_contracts, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Creates a managed copy of this table's Contracts.
        /// </summary>
        /// <returns>A new Contract array in canonical table order.</returns>
        /// <remarks>
        /// The returned array may be modified by the caller without affecting this table.
        /// </remarks>
        public AtlasContract[] ToArray()
        {
            var copy = new AtlasContract[_contracts.Length];
            Array.Copy(_contracts, copy, _contracts.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over Contracts in canonical table order.
        /// </summary>
        /// <returns>An enumerator over Contracts.</returns>
        public IEnumerator<AtlasContract> GetEnumerator()
        {
            for (var i = 0; i < _contracts.Length; i++)
            {
                yield return _contracts[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over Contracts in canonical table order.
        /// </summary>
        /// <returns>An enumerator over Contracts.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this Contract table.
        /// </summary>
        /// <returns>A string containing the table name and Contract count.</returns>
        public override string ToString()
        {
            return $"AtlasContractTable(Name={GetDiagnosticName()}, Count={Count})";
        }

        private static AtlasContract[] BuildCanonicalContractArray(AtlasContract[] contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (contracts.Length > AtlasConstants.MaxFieldSlots)
            {
                throw new ArgumentException(
                    $"Atlas Contract table contains '{contracts.Length}' Contracts, but the maximum supported count is '{AtlasConstants.MaxFieldSlots}'.",
                    nameof(contracts));
            }

            var canonical = new AtlasContract[contracts.Length];

            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];

                contract.ValidateOrThrow($"contracts[{i}]");
                ValidateDeclaredSlotMatchesIndex(contract, i);

                canonical[i] = contract.WithSlot(i);
                canonical[i].ValidateTableReadyOrThrow($"contracts[{i}]");
            }

            ValidateNoDuplicateStableIds(canonical);
            ValidateNoDuplicateDurableIdentities(canonical);
            ValidateNoDuplicateDebugNames(canonical);
            ValidateNoSelfReferentialShapes(canonical);
            ValidateFieldRelativeShapes(canonical);

            return canonical;
        }

        private static Dictionary<StableDataId, AtlasFieldSlot> BuildSlotLookup(AtlasContract[] contracts)
        {
            var lookup = new Dictionary<StableDataId, AtlasFieldSlot>(contracts.Length);

            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];
                lookup.Add(contract.StableId, contract.Slot);
            }

            return lookup;
        }

        private static void ValidateDeclaredSlotMatchesIndex(
            AtlasContract contract,
            int index)
        {
            if (!contract.Slot.IsValid)
            {
                return;
            }

            if (contract.Slot.Index == index)
            {
                return;
            }

            throw new ArgumentException(
                $"Atlas Contract '{contract.DebugName}' declares slot '{contract.Slot}', but its table index is '{index}'. " +
                "Contract-table order is the canonical source of slots.");
        }

        private static void ValidateNoDuplicateStableIds(AtlasContract[] contracts)
        {
            var seen = new HashSet<StableDataId>();

            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];

                if (seen.Add(contract.StableId))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas Contract table contains duplicate stable id '{contract.StableId}' at slot '{contract.Slot}'.");
            }
        }

        private static void ValidateNoDuplicateDurableIdentities(AtlasContract[] contracts)
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
                        $"Atlas Contract table contains multiple versions of durable identity '{left.StableId.High:X16}-{left.StableId.Low:X16}'. " +
                        $"First Contract is '{left.DebugName}' with version '{left.StableId.Version}', " +
                        $"second Contract is '{right.DebugName}' with version '{right.StableId.Version}'. " +
                        "A single Contract table must select exactly one version of each durable Field identity.");
                }
            }
        }

        private static void ValidateNoDuplicateDebugNames(AtlasContract[] contracts)
        {
            var seen = new HashSet<FixedString64Bytes>();

            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];

                if (seen.Add(contract.DebugName))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas Contract table contains duplicate debug name '{contract.DebugName}' at slot '{contract.Slot}'. " +
                    "Debug names are not durable identity, but table-local duplicates make diagnostics ambiguous.");
            }
        }

        private static void ValidateNoSelfReferentialShapes(AtlasContract[] contracts)
        {
            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];
                var shape = contract.LengthShape;

                if (!shape.DependsOnField)
                {
                    continue;
                }

                if (!shape.SourceFieldId.HasSameIdentityAs(contract.StableId))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas Contract '{contract.DebugName}' has a Field-relative length shape that references itself. Shape={shape}.");
            }
        }

        private static void ValidateFieldRelativeShapes(AtlasContract[] contracts)
        {
            var slotsByStableId = BuildSlotLookup(contracts);

            for (var i = 0; i < contracts.Length; i++)
            {
                var contract = contracts[i];
                var shape = contract.LengthShape;

                if (!shape.DependsOnField)
                {
                    continue;
                }

                if (slotsByStableId.TryGetValue(shape.SourceFieldId, out var sourceSlot))
                {
                    ValidateFieldRelativeShapeCompatibility(contract, contracts[sourceSlot.Index]);
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas Contract '{contract.DebugName}' depends on source Field id '{shape.SourceFieldId}', " +
                    "but that Field is not present in the same Contract table.");
            }
        }

        private static void ValidateFieldRelativeShapeCompatibility(
            AtlasContract target,
            AtlasContract source)
        {
            switch (target.LengthShape.Kind)
            {
                case LengthShapeKind.MatchFieldLength:
                    if (!SourceCanProvideLength(source))
                    {
                        throw new ArgumentException(
                            $"Atlas Contract '{target.DebugName}' matches the length of source Field '{source.DebugName}', " +
                            $"but source storage kind '{source.StorageFormat.Kind}' does not provide a stable resolvable length.");
                    }

                    return;

                case LengthShapeKind.CapacityFromField:
                    if (!SourceCanProvideLengthOrCapacity(source))
                    {
                        throw new ArgumentException(
                            $"Atlas Contract '{target.DebugName}' derives capacity from source Field '{source.DebugName}', " +
                            $"but source storage kind '{source.StorageFormat.Kind}' does not provide length or capacity.");
                    }

                    return;

                case LengthShapeKind.PrefixSumPayload:
                    if (!SourceCanProvidePrefixMetadata(source))
                    {
                        throw new ArgumentException(
                            $"Atlas Contract '{target.DebugName}' derives prefix-sum payload length from source Field '{source.DebugName}', " +
                            $"but source storage kind '{source.StorageFormat.Kind}' is not valid prefix metadata storage.");
                    }

                    return;
            }
        }

        private static bool SourceCanProvideLength(AtlasContract source)
        {
            return source.StorageFormat.Kind == StorageKind.Scalar ||
                   source.StorageFormat.Kind == StorageKind.NativeArray ||
                   source.StorageFormat.Kind == StorageKind.NativeList ||
                   source.StorageFormat.Kind == StorageKind.UnsafeList ||
                   source.StorageFormat.Kind == StorageKind.Blob ||
                   source.StorageFormat.Kind == StorageKind.External;
        }

        private static bool SourceCanProvideLengthOrCapacity(AtlasContract source)
        {
            return SourceCanProvideLength(source) ||
                   source.StorageFormat.Kind == StorageKind.NativeStream ||
                   source.StorageFormat.Kind == StorageKind.NativeParallelHashMap;
        }

        private static bool SourceCanProvidePrefixMetadata(AtlasContract source)
        {
            return source.StorageFormat.Kind == StorageKind.NativeArray ||
                   source.StorageFormat.Kind == StorageKind.NativeList ||
                   source.StorageFormat.Kind == StorageKind.UnsafeList ||
                   source.StorageFormat.Kind == StorageKind.External;
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_contracts.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Contract index must be between 0 and {_contracts.Length - 1}.");
        }

        private string GetDiagnosticName()
        {
            return Name.IsEmpty
                ? "<unnamed>"
                : Name.ToString();
        }
    }
}