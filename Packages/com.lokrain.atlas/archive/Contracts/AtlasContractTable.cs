// Runtime/Contracts/AtlasContractTable.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Store an immutable ordered table of Atlas field contracts.
// - Assign canonical zero-based field slots from explicit table order.
// - Preserve zero-valid StableDataId and AtlasFieldSlot semantics.
// - Resolve stable field identities to table-local slots and contracts.
// - Validate duplicate identities, slot consistency, and field-relative length shapes.
//
// Design notes
// - Contract-table order is the canonical source of AtlasFieldSlot values.
// - Slot zero is valid.
// - StableDataId zero/default is valid.
// - Missing lookup state is represented only by bool-returning Try methods.
// - Try methods set out payloads to default on failure, but that payload is not semantic.
// - Do not interpret default AtlasContract or default AtlasFieldSlot as missing without the bool result.
// - Contract tables are setup/compiler metadata, not hot-loop job data.
// - Jobs should receive resolved native storage, typed slices/views, or compiled addresses.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Immutable ordered table of Atlas field contracts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contract-table order is the canonical source of field slots. The contract at index zero is
    /// assigned slot zero. Slot zero is valid and must not be treated as missing, invalid, or
    /// unresolved.
    /// </para>
    ///
    /// <para>
    /// A contract table copies the input contracts, validates declaration metadata, assigns
    /// canonical table-local slots, rejects duplicate stable identities, and validates
    /// field-relative length-shape dependencies.
    /// </para>
    ///
    /// <para>
    /// This table is an authoring/compiler/runtime-metadata object. It does not allocate field
    /// storage, own native memory, schedule jobs, or produce artifacts. Later compilation stages
    /// should turn validated table metadata into workspace layouts, resolved addresses, and
    /// executable operation bindings.
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
        /// Table names are not durable field identity. Durable identity belongs to <see cref="StableDataId"/>.
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
        /// Gets the number of contracts in this table.
        /// </summary>
        public int Count => _contracts.Length;

        /// <summary>
        /// Gets whether this table contains no contracts.
        /// </summary>
        public bool IsEmpty => _contracts.Length == 0;

        /// <summary>
        /// Gets the contract at a zero-based canonical table index.
        /// </summary>
        /// <param name="index">The zero-based contract-table index.</param>
        /// <returns>The contract assigned to <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this table's range.
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
        /// Gets the contract assigned to a canonical field slot.
        /// </summary>
        /// <param name="slot">The canonical field slot. Slot zero/default is valid.</param>
        /// <returns>The contract assigned to <paramref name="slot"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slot"/> is outside this table's range.
        /// </exception>
        public AtlasContract this[AtlasFieldSlot slot] => this[slot.Index];

        /// <summary>
        /// Creates a contract table from explicitly ordered contracts.
        /// </summary>
        /// <param name="contracts">
        /// Contracts in canonical slot order. Contracts may be unslotted, or already slotted with
        /// slots matching their table position.
        /// </param>
        /// <returns>A validated immutable contract table.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when contracts are invalid, duplicated, inconsistently slotted, or exceed Atlas
        /// slot capacity.
        /// </exception>
        public static AtlasContractTable Create(params AtlasContract[] contracts)
        {
            return new AtlasContractTable(default, contracts);
        }

        /// <summary>
        /// Creates a named contract table from explicitly ordered contracts.
        /// </summary>
        /// <param name="name">The diagnostic table name.</param>
        /// <param name="contracts">
        /// Contracts in canonical slot order. Contracts may be unslotted, or already slotted with
        /// slots matching their table position.
        /// </param>
        /// <returns>A validated immutable contract table.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when contracts are invalid, duplicated, inconsistently slotted, or exceed Atlas
        /// slot capacity.
        /// </exception>
        public static AtlasContractTable Create(
            FixedString64Bytes name,
            params AtlasContract[] contracts)
        {
            return new AtlasContractTable(name, contracts);
        }

        /// <summary>
        /// Determines whether this table contains a contract with the supplied stable field identifier.
        /// </summary>
        /// <param name="stableId">The stable field identifier to search for. Zero/default is valid.</param>
        /// <returns><c>true</c> when this table contains the identifier; otherwise, <c>false</c>.</returns>
        public bool Contains(StableDataId stableId)
        {
            return _slotsByStableId.ContainsKey(stableId);
        }

        /// <summary>
        /// Determines whether this table contains the supplied typed field declaration.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <returns><c>true</c> when this table contains the typed field; otherwise, <c>false</c>.</returns>
        public bool Contains<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Contains(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to resolve a stable field identifier to its canonical table slot.
        /// </summary>
        /// <param name="stableId">The stable field identifier to resolve. Zero/default is valid.</param>
        /// <param name="slot">
        /// The resolved slot when this method returns <c>true</c>; otherwise, the default slot
        /// payload.
        /// </param>
        /// <returns><c>true</c> when the identifier was resolved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The output value is not semantically meaningful when this method returns <c>false</c>.
        /// The returned boolean owns success/failure state.
        /// </remarks>
        public bool TryGetSlot(
            StableDataId stableId,
            out AtlasFieldSlot slot)
        {
            return _slotsByStableId.TryGetValue(stableId, out slot);
        }

        /// <summary>
        /// Attempts to resolve a typed field declaration to its canonical table slot.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <param name="slot">
        /// The resolved slot when this method returns <c>true</c>; otherwise, the default slot
        /// payload.
        /// </param>
        /// <returns><c>true</c> when the typed field was resolved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The output value is not semantically meaningful when this method returns <c>false</c>.
        /// The returned boolean owns success/failure state.
        /// </remarks>
        public bool TryGetSlot<TField, TElement>(out AtlasFieldSlot slot)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetSlot(AtlasField.StableId<TField, TElement>(), out slot);
        }

        /// <summary>
        /// Resolves a stable field identifier to its canonical table slot.
        /// </summary>
        /// <param name="stableId">The stable field identifier to resolve. Zero/default is valid.</param>
        /// <returns>The canonical field slot assigned to <paramref name="stableId"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is not present in this table.
        /// </exception>
        public AtlasFieldSlot GetRequiredSlot(StableDataId stableId)
        {
            if (TryGetSlot(stableId, out var slot))
            {
                return slot;
            }

            throw new ArgumentException(
                $"Atlas contract table '{GetDiagnosticName()}' does not contain field id '{stableId}'.",
                nameof(stableId));
        }

        /// <summary>
        /// Resolves a typed field declaration to its canonical table slot.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <returns>The canonical field slot assigned to the typed field.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the typed field is not present in this table.
        /// </exception>
        public AtlasFieldSlot GetRequiredSlot<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredSlot(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to resolve a stable field identifier to its contract.
        /// </summary>
        /// <param name="stableId">The stable field identifier to resolve. Zero/default is valid.</param>
        /// <param name="contract">
        /// The resolved contract when this method returns <c>true</c>; otherwise, the default
        /// contract payload.
        /// </param>
        /// <returns><c>true</c> when the contract was resolved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The output value is not semantically meaningful when this method returns <c>false</c>.
        /// The returned boolean owns success/failure state.
        /// </remarks>
        public bool TryGetContract(
            StableDataId stableId,
            out AtlasContract contract)
        {
            if (TryGetSlot(stableId, out var slot))
            {
                contract = this[slot];
                return true;
            }

            contract = default;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a typed field declaration to its contract.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <param name="contract">
        /// The resolved contract when this method returns <c>true</c>; otherwise, the default
        /// contract payload.
        /// </param>
        /// <returns><c>true</c> when the contract was resolved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The output value is not semantically meaningful when this method returns <c>false</c>.
        /// The returned boolean owns success/failure state.
        /// </remarks>
        public bool TryGetContract<TField, TElement>(out AtlasContract contract)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetContract(AtlasField.StableId<TField, TElement>(), out contract);
        }

        /// <summary>
        /// Resolves a stable field identifier to its contract.
        /// </summary>
        /// <param name="stableId">The stable field identifier to resolve. Zero/default is valid.</param>
        /// <returns>The contract assigned to <paramref name="stableId"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is not present in this table.
        /// </exception>
        public AtlasContract GetRequiredContract(StableDataId stableId)
        {
            return this[GetRequiredSlot(stableId)];
        }

        /// <summary>
        /// Resolves a typed field declaration to its contract.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <returns>The contract assigned to the typed field.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the typed field is not present in this table.
        /// </exception>
        public AtlasContract GetRequiredContract<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredContract(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Creates a resolved typed handle for a field declaration contained in this table.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <returns>A resolved field handle containing stable identity and canonical slot.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the typed field is not present in this table or its storage format is incompatible.
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
        /// Copies contracts from this table into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array that receives contracts in canonical order.</param>
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
                    $"Destination array length '{destination.Length}' is smaller than contract count '{_contracts.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_contracts, destination, _contracts.Length);
        }

        /// <summary>
        /// Copies a contract range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">The first source index in this table.</param>
        /// <param name="destination">The destination array.</param>
        /// <param name="destinationIndex">The first destination index.</param>
        /// <param name="length">The number of contracts to copy.</param>
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
                throw new ArgumentOutOfRangeException(
                    nameof(sourceIndex),
                    sourceIndex,
                    "Source index must be non-negative.");
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(destinationIndex),
                    destinationIndex,
                    "Destination index must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Length must be non-negative.");
            }

            if (sourceIndex + length > _contracts.Length)
            {
                throw new ArgumentException(
                    "Source range exceeds contract table bounds.",
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
        /// Creates a managed copy of this table's contracts.
        /// </summary>
        /// <returns>A new contract array in canonical table order.</returns>
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
        /// Gets a managed enumerator over contracts in canonical table order.
        /// </summary>
        /// <returns>An enumerator over contracts.</returns>
        public IEnumerator<AtlasContract> GetEnumerator()
        {
            for (var i = 0; i < _contracts.Length; i++)
            {
                yield return _contracts[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over contracts in canonical table order.
        /// </summary>
        /// <returns>An enumerator over contracts.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this contract table.
        /// </summary>
        /// <returns>A string containing the table name and contract count.</returns>
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
                    $"Atlas contract table contains '{contracts.Length}' contracts, but the maximum supported count is '{AtlasConstants.MaxFieldSlots}'.",
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
            if (!contract.HasAssignedSlot)
            {
                return;
            }

            if (contract.Slot.Index == index)
            {
                return;
            }

            throw new ArgumentException(
                $"Atlas contract '{contract.DebugName}' declares slot '{contract.Slot}', but its table index is '{index}'. " +
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
                    $"Atlas contract table contains duplicate stable id '{contract.StableId}' at slot '{contract.Slot}'.");
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
                        $"Atlas contract table contains multiple versions of durable identity '{left.StableId.High:X16}-{left.StableId.Low:X16}'. " +
                        $"First contract is '{left.DebugName}' with version '{left.StableId.Version}', " +
                        $"second contract is '{right.DebugName}' with version '{right.StableId.Version}'. " +
                        "A single contract table must select exactly one version of each durable field identity.");
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
                    $"Atlas contract table contains duplicate debug name '{contract.DebugName}' at slot '{contract.Slot}'. " +
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
                    $"Atlas contract '{contract.DebugName}' has a field-relative length shape that references itself. Shape={shape}.");
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
                    $"Atlas contract '{contract.DebugName}' depends on source field id '{shape.SourceFieldId}', " +
                    "but that field is not present in the same contract table.");
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
                            $"Atlas contract '{target.DebugName}' matches the length of source field '{source.DebugName}', " +
                            $"but source storage kind '{source.StorageFormat.Kind}' does not provide a stable resolvable length.");
                    }

                    return;

                case LengthShapeKind.CapacityFromField:
                    if (!SourceCanProvideLengthOrCapacity(source))
                    {
                        throw new ArgumentException(
                            $"Atlas contract '{target.DebugName}' derives capacity from source field '{source.DebugName}', " +
                            $"but source storage kind '{source.StorageFormat.Kind}' does not provide length or capacity.");
                    }

                    return;

                case LengthShapeKind.PrefixSumPayload:
                    if (!SourceCanProvidePrefixMetadata(source))
                    {
                        throw new ArgumentException(
                            $"Atlas contract '{target.DebugName}' derives prefix-sum payload length from source field '{source.DebugName}', " +
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