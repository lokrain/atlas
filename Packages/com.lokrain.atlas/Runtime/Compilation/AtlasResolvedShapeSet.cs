// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasResolvedShapeSet.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Store one resolved shape per Contract-table field slot.
// - Preserve Contract-table canonical order.
// - Validate that resolved shapes exactly match the Contract table.
// - Provide linear lookup by slot, StableDataId, or typed field declaration.
// - Remain managed compiler metadata only; no native memory ownership, allocation layout, or jobs.
//
// Design notes
// - This is shape-resolution output, not workspace memory.
// - Slot zero and StableDataId zero are valid; absence is represented by bool-returning APIs.
// - The set keeps the Contract table reference by design, matching compiled-plan metadata style.
// - No dictionaries are needed here. Contract-table and shape-set sizes are compiler metadata scale.
// - Shape rows must be in canonical Contract-table slot order.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Immutable ordered set of resolved field shapes for one Atlas Contract table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasResolvedShapeSet"/> is produced after plan validation and before workspace
    /// memory layout. It contains concrete length, capacity, and byte-size metadata for every
    /// Contract-table field.
    /// </para>
    ///
    /// <para>
    /// This type deliberately does not allocate field storage, calculate memory offsets, schedule
    /// operations, or produce artifact data. Later passes should consume this set to build memory
    /// layout and workspace bindings.
    /// </para>
    ///
    /// <para>
    /// The set is slot-ordered. Shape at index <c>i</c> must describe the Contract assigned to slot
    /// <c>i</c>. This keeps later memory layout deterministic and avoids identity lookups in jobs.
    /// </para>
    /// </remarks>
    public sealed class AtlasResolvedShapeSet :
        IReadOnlyList<AtlasResolvedShape>
    {
        private readonly AtlasResolvedShape[] _shapes;

        /// <summary>
        /// Contract table used to validate and interpret the resolved shape rows.
        /// </summary>
        public readonly AtlasContractTable Contracts;

        /// <summary>
        /// Diagnostic name used by exceptions, reports, tooling, and tests.
        /// </summary>
        public readonly FixedString64Bytes Name;

        private AtlasResolvedShapeSet(
            FixedString64Bytes name,
            AtlasContractTable contracts,
            AtlasResolvedShape[] shapes)
        {
            Name = name;
            Contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
            _shapes = BuildCanonicalShapeArray(contracts, shapes);
        }

        /// <summary>
        /// Gets the number of resolved shapes.
        /// </summary>
        public int Count => _shapes.Length;

        /// <summary>
        /// Gets whether the set contains no resolved shapes.
        /// </summary>
        public bool IsEmpty => _shapes.Length == 0;

        /// <summary>
        /// Gets the total logical byte length across all resolved shapes.
        /// </summary>
        public long TotalByteLength
        {
            get
            {
                long total = 0L;

                for (var i = 0; i < _shapes.Length; i++)
                {
                    total = checked(total + _shapes[i].ByteLength);
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the total allocated byte capacity across all resolved shapes.
        /// </summary>
        public long TotalByteCapacity
        {
            get
            {
                long total = 0L;

                for (var i = 0; i < _shapes.Length; i++)
                {
                    total = checked(total + _shapes[i].ByteCapacity);
                }

                return total;
            }
        }

        /// <summary>
        /// Gets whether at least one shape has capacity greater than logical length.
        /// </summary>
        public bool HasCapacitySlack
        {
            get
            {
                for (var i = 0; i < _shapes.Length; i++)
                {
                    if (_shapes[i].HasCapacitySlack)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one shape requires non-zero memory capacity.
        /// </summary>
        public bool RequiresMemory
        {
            get
            {
                for (var i = 0; i < _shapes.Length; i++)
                {
                    if (_shapes[i].RequiresMemory)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a resolved shape by canonical set index.
        /// </summary>
        /// <param name="index">Zero-based shape index.</param>
        /// <returns>The resolved shape at <paramref name="index"/>.</returns>
        public AtlasResolvedShape this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _shapes[index];
            }
        }

        /// <summary>
        /// Gets a resolved shape by canonical Contract-table slot.
        /// </summary>
        /// <param name="slot">Canonical field slot. Slot zero/default is valid.</param>
        /// <returns>The resolved shape assigned to <paramref name="slot"/>.</returns>
        public AtlasResolvedShape this[AtlasFieldSlot slot] => this[slot.Index];

        /// <summary>
        /// Creates a resolved shape set using the Contract table's diagnostic name.
        /// </summary>
        /// <param name="contracts">Contract table that owns canonical field slot order.</param>
        /// <param name="shapes">Resolved shapes in canonical Contract-table slot order.</param>
        /// <returns>A validated immutable resolved shape set.</returns>
        public static AtlasResolvedShapeSet Create(
            AtlasContractTable contracts,
            params AtlasResolvedShape[] shapes)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            return new AtlasResolvedShapeSet(
                contracts.Name,
                contracts,
                shapes);
        }

        /// <summary>
        /// Creates a named resolved shape set.
        /// </summary>
        /// <param name="name">Diagnostic shape-set name.</param>
        /// <param name="contracts">Contract table that owns canonical field slot order.</param>
        /// <param name="shapes">Resolved shapes in canonical Contract-table slot order.</param>
        /// <returns>A validated immutable resolved shape set.</returns>
        public static AtlasResolvedShapeSet Create(
            FixedString64Bytes name,
            AtlasContractTable contracts,
            params AtlasResolvedShape[] shapes)
        {
            return new AtlasResolvedShapeSet(
                name,
                contracts,
                shapes);
        }

        /// <summary>
        /// Determines whether this set contains a shape for the supplied stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id. Zero/default is valid.</param>
        /// <returns><c>true</c> when a matching shape exists; otherwise, <c>false</c>.</returns>
        public bool Contains(StableDataId stableId)
        {
            return TryGetShape(stableId, out _);
        }

        /// <summary>
        /// Determines whether this set contains a shape for the supplied typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged field element type.</typeparam>
        /// <returns><c>true</c> when a matching shape exists; otherwise, <c>false</c>.</returns>
        public bool Contains<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Contains(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to get a shape by canonical Contract-table slot.
        /// </summary>
        /// <param name="slot">Canonical field slot. Slot zero/default is valid.</param>
        /// <param name="shape">
        /// The resolved shape when this method returns <c>true</c>; otherwise, default payload.
        /// </param>
        /// <returns><c>true</c> when the slot is inside this set; otherwise, <c>false</c>.</returns>
        public bool TryGetShape(
            AtlasFieldSlot slot,
            out AtlasResolvedShape shape)
        {
            var index = slot.Index;

            if (index >= 0 && index < _shapes.Length)
            {
                shape = _shapes[index];
                return true;
            }

            shape = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a shape by stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id. Zero/default is valid.</param>
        /// <param name="shape">
        /// The resolved shape when this method returns <c>true</c>; otherwise, default payload.
        /// </param>
        /// <returns><c>true</c> when a matching shape exists; otherwise, <c>false</c>.</returns>
        public bool TryGetShape(
            StableDataId stableId,
            out AtlasResolvedShape shape)
        {
            for (var i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i].StableId == stableId)
                {
                    shape = _shapes[i];
                    return true;
                }
            }

            shape = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a shape by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged field element type.</typeparam>
        /// <param name="shape">
        /// The resolved shape when this method returns <c>true</c>; otherwise, default payload.
        /// </param>
        /// <returns><c>true</c> when a matching shape exists; otherwise, <c>false</c>.</returns>
        public bool TryGetShape<TField, TElement>(
            out AtlasResolvedShape shape)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetShape(
                AtlasField.StableId<TField, TElement>(),
                out shape);
        }

        /// <summary>
        /// Gets a required shape by canonical Contract-table slot.
        /// </summary>
        /// <param name="slot">Canonical field slot. Slot zero/default is valid.</param>
        /// <returns>The resolved shape assigned to <paramref name="slot"/>.</returns>
        public AtlasResolvedShape GetRequiredShape(AtlasFieldSlot slot)
        {
            if (TryGetShape(slot, out var shape))
            {
                return shape;
            }

            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Atlas resolved shape set '{GetDiagnosticName()}' does not contain slot '{slot}'.");
        }

        /// <summary>
        /// Gets a required shape by stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id. Zero/default is valid.</param>
        /// <returns>The resolved shape assigned to <paramref name="stableId"/>.</returns>
        public AtlasResolvedShape GetRequiredShape(StableDataId stableId)
        {
            if (TryGetShape(stableId, out var shape))
            {
                return shape;
            }

            throw new ArgumentException(
                $"Atlas resolved shape set '{GetDiagnosticName()}' does not contain field id '{stableId}'.",
                nameof(stableId));
        }

        /// <summary>
        /// Gets a required shape by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged field element type.</typeparam>
        /// <returns>The resolved shape assigned to the typed field declaration.</returns>
        public AtlasResolvedShape GetRequiredShape<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredShape(
                AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Copies all shapes into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array.</param>
        public void CopyTo(AtlasResolvedShape[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _shapes.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than resolved shape count '{_shapes.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_shapes, destination, _shapes.Length);
        }

        /// <summary>
        /// Copies a shape range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source index.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of shapes to copy.</param>
        public void CopyTo(
            int sourceIndex,
            AtlasResolvedShape[] destination,
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

            if (sourceIndex + length > _shapes.Length)
            {
                throw new ArgumentException(
                    "Source range exceeds resolved shape set bounds.",
                    nameof(length));
            }

            if (destinationIndex + length > destination.Length)
            {
                throw new ArgumentException(
                    "Destination range exceeds destination array bounds.",
                    nameof(length));
            }

            Array.Copy(_shapes, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Creates a managed copy of this set's shapes.
        /// </summary>
        /// <returns>A new array in canonical shape order.</returns>
        public AtlasResolvedShape[] ToArray()
        {
            var copy = new AtlasResolvedShape[_shapes.Length];
            Array.Copy(_shapes, copy, _shapes.Length);
            return copy;
        }

        /// <summary>
        /// Returns the diagnostic name of this resolved shape set.
        /// </summary>
        /// <returns>The declared name when present; otherwise, an invariant fallback string.</returns>
        public string GetDiagnosticName()
        {
            if (!Name.IsEmpty)
            {
                return Name.ToString();
            }

            if (Contracts != null && !Contracts.Name.IsEmpty)
            {
                return Contracts.Name.ToString();
            }

            return "<unnamed-shape-set>";
        }

        /// <summary>
        /// Validates the current shape set against its Contract table.
        /// </summary>
        /// <param name="parameterName">Optional caller parameter name.</param>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasResolvedShapeSet);

            if (Contracts == null)
            {
                throw new ArgumentException(
                    "Atlas resolved shape set has no Contract table.",
                    name);
            }

            ValidateShapeArrayAgainstContracts(
                Contracts,
                _shapes,
                name);
        }

        /// <summary>
        /// Gets an enumerator over shapes in canonical Contract-table slot order.
        /// </summary>
        /// <returns>An enumerator over resolved shapes.</returns>
        public IEnumerator<AtlasResolvedShape> GetEnumerator()
        {
            for (var i = 0; i < _shapes.Length; i++)
            {
                yield return _shapes[i];
            }
        }

        /// <summary>
        /// Gets an enumerator over shapes in canonical Contract-table slot order.
        /// </summary>
        /// <returns>An enumerator over resolved shapes.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this shape set.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return
                $"AtlasResolvedShapeSet(Name={GetDiagnosticName()}, Count={Count}, ByteLength={TotalByteLength}, ByteCapacity={TotalByteCapacity})";
        }

        private static AtlasResolvedShape[] BuildCanonicalShapeArray(
            AtlasContractTable contracts,
            AtlasResolvedShape[] shapes)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            ValidateShapeArrayAgainstContracts(
                contracts,
                shapes,
                nameof(shapes));

            var copy = new AtlasResolvedShape[shapes.Length];
            Array.Copy(shapes, copy, shapes.Length);
            return copy;
        }

        private static void ValidateShapeArrayAgainstContracts(
            AtlasContractTable contracts,
            AtlasResolvedShape[] shapes,
            string parameterName)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            if (shapes.Length != contracts.Count)
            {
                throw new ArgumentException(
                    $"Resolved shape count '{shapes.Length}' must match Contract-table count '{contracts.Count}'.",
                    parameterName);
            }

            for (var i = 0; i < shapes.Length; i++)
            {
                var contract = contracts[i];
                var shape = shapes[i];

                contract.ValidateTableReadyOrThrow($"contracts[{i}]");
                shape.ValidateOrThrow($"{parameterName}[{i}]");

                ValidateShapeMatchesContract(
                    shape,
                    contract,
                    i,
                    parameterName);
            }
        }

        private static void ValidateShapeMatchesContract(
            AtlasResolvedShape shape,
            AtlasContract contract,
            int index,
            string parameterName)
        {
            if (shape.Slot.Index != index)
            {
                throw new ArgumentException(
                    $"Resolved shape '{shape.GetDiagnosticName()}' declares slot '{shape.Slot}', but its shape-set index is '{index}'.",
                    parameterName);
            }

            if (contract.Slot.Index != index)
            {
                throw new ArgumentException(
                    $"Contract '{contract.GetDiagnosticName()}' declares slot '{contract.Slot}', but its table index is '{index}'.",
                    parameterName);
            }

            if (shape.StableId != contract.StableId)
            {
                throw new ArgumentException(
                    $"Resolved shape at slot '{index}' has field id '{shape.StableId}', but Contract table expects '{contract.StableId}'.",
                    parameterName);
            }

            if (shape.Role != contract.Role)
            {
                throw new ArgumentException(
                    $"Resolved shape '{shape.GetDiagnosticName()}' has role '{shape.Role}', but Contract expects '{contract.Role}'.",
                    parameterName);
            }

            if (shape.StorageFormat != contract.StorageFormat)
            {
                throw new ArgumentException(
                    $"Resolved shape '{shape.GetDiagnosticName()}' has storage format '{shape.StorageFormat}', but Contract expects '{contract.StorageFormat}'.",
                    parameterName);
            }

            if (shape.DeclaredShape != contract.LengthShape)
            {
                throw new ArgumentException(
                    $"Resolved shape '{shape.GetDiagnosticName()}' has declared shape '{shape.DeclaredShape}', but Contract expects '{contract.LengthShape}'.",
                    parameterName);
            }

            if (!shape.DebugName.Equals(contract.DebugName))
            {
                throw new ArgumentException(
                    $"Resolved shape at slot '{index}' has debug name '{shape.DebugName}', but Contract expects '{contract.DebugName}'.",
                    parameterName);
            }
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if (index >= 0 && index < _shapes.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Resolved shape index must be between 0 and {_shapes.Length - 1}.");
        }
    }
}