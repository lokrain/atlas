// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasResolvedShapeSet.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Store one resolved shape per Contract-table field slot.
// - Preserve Contract-table canonical order.
// - Validate that resolved shapes exactly match the Contract table.
// - Preserve semantic shape-domain identity across compiler, workspace-layout, artifact, and diagnostics boundaries.
// - Provide deterministic linear lookup by slot, StableDataId, typed field declaration, or shape domain.
// - Remain managed compiler metadata only; no native memory ownership, allocation layout, execution, or jobs.
//
// Design notes
// - This is shape-resolution output, not workspace memory.
// - This is not a workspace layout.
// - This is not an execution plan.
// - Slot zero and StableDataId zero are valid.
// - Absence is represented by bool-returning APIs, not sentinel ids.
// - The set keeps the Contract table reference by design, matching compiled-plan metadata style.
// - No dictionaries are used here. Contract-table and shape-set sizes are compiler metadata scale.
// - Shape rows must be in canonical Contract-table slot order.
// - Shape-domain identity is validated against the source Contract table.
// - Capacity is allowed to exceed logical length.
// - Jobs should receive resolved native containers/views or numeric addresses, not query this set.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// <see cref="AtlasResolvedShapeSet"/> is produced after semantic validation and before
    /// workspace-layout compilation. It contains concrete logical length, capacity, byte-size,
    /// storage-schema, and semantic shape-domain metadata for every Contract-table field.
    /// </para>
    ///
    /// <para>
    /// This type deliberately does not allocate field storage, calculate memory offsets, schedule
    /// operations, or produce artifact payload data. Later passes consume this set to build
    /// workspace layout, workspace memory, artifacts, and diagnostic exports.
    /// </para>
    ///
    /// <para>
    /// The set is slot-ordered. Shape at index <c>i</c> must describe the Contract assigned to
    /// slot <c>i</c>. This keeps later memory layout deterministic and avoids identity lookups in
    /// execution/jobs.
    /// </para>
    /// </remarks>
    public sealed class AtlasResolvedShapeSet :
        IReadOnlyList<AtlasResolvedShape>
    {
        private readonly AtlasResolvedShape[] _shapes;

        /// <summary>
        /// Contract table used to validate and interpret resolved shape rows.
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
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            Name = name;
            Contracts = contracts;
            _shapes = BuildCanonicalShapeArray(
                contracts,
                shapes);
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
        /// Gets whether at least one resolved shape belongs to a dense-grid domain.
        /// </summary>
        public bool HasDenseGridDomain
        {
            get
            {
                for (var i = 0; i < _shapes.Length; i++)
                {
                    if (_shapes[i].IsDenseGrid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one resolved shape belongs to a variable-payload domain.
        /// </summary>
        public bool HasVariablePayloadDomain
        {
            get
            {
                for (var i = 0; i < _shapes.Length; i++)
                {
                    if (_shapes[i].IsVariablePayload)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one resolved shape belongs to an external domain.
        /// </summary>
        public bool HasExternalDomain
        {
            get
            {
                for (var i = 0; i < _shapes.Length; i++)
                {
                    if (_shapes[i].ShapeDomain.IsExternal)
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
        public AtlasResolvedShape this[AtlasFieldSlot slot] =>
            GetRequiredShape(slot);

        /// <summary>
        /// Creates a resolved shape set using the Contract table's diagnostic name.
        /// </summary>
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
        /// Determines whether this set contains a shape for the supplied canonical slot.
        /// </summary>
        public bool Contains(AtlasFieldSlot slot)
        {
            return TryGetShape(
                slot,
                out _);
        }

        /// <summary>
        /// Determines whether this set contains a shape for the supplied stable field id.
        /// </summary>
        public bool Contains(StableDataId stableId)
        {
            return TryGetShape(
                stableId,
                out _);
        }

        /// <summary>
        /// Determines whether this set contains a shape for the supplied typed field declaration.
        /// </summary>
        public bool Contains<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Contains(
                AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Determines whether this set contains at least one shape in the supplied domain.
        /// </summary>
        public bool ContainsDomain(AtlasShapeDomain shapeDomain)
        {
            return TryGetFirstShape(
                shapeDomain,
                out _);
        }

        /// <summary>
        /// Determines whether this set contains at least one shape in the supplied domain kind.
        /// </summary>
        public bool ContainsDomainKind(AtlasShapeDomainKind kind)
        {
            return CountDomainKind(kind) > 0;
        }

        /// <summary>
        /// Counts resolved shapes in the supplied domain kind.
        /// </summary>
        public int CountDomainKind(AtlasShapeDomainKind kind)
        {
            if (kind == AtlasShapeDomainKind.None)
            {
                return 0;
            }

            var count = 0;

            for (var i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i].ShapeDomain.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Attempts to get a shape by canonical Contract-table slot.
        /// </summary>
        public bool TryGetShape(
            AtlasFieldSlot slot,
            out AtlasResolvedShape shape)
        {
            var index = slot.Index;

            if (index >= 0 &&
                index < _shapes.Length &&
                _shapes[index].Slot == slot)
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
        /// Attempts to get the first shape in the supplied concrete shape domain.
        /// </summary>
        public bool TryGetFirstShape(
            AtlasShapeDomain shapeDomain,
            out AtlasResolvedShape shape)
        {
            if (!shapeDomain.IsConcrete)
            {
                shape = default;
                return false;
            }

            for (var i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i].ShapeDomain == shapeDomain)
                {
                    shape = _shapes[i];
                    return true;
                }
            }

            shape = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the first shape in the supplied shape-domain kind.
        /// </summary>
        public bool TryGetFirstShape(
            AtlasShapeDomainKind kind,
            out AtlasResolvedShape shape)
        {
            if (kind == AtlasShapeDomainKind.None)
            {
                shape = default;
                return false;
            }

            for (var i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i].ShapeDomain.Kind == kind)
                {
                    shape = _shapes[i];
                    return true;
                }
            }

            shape = default;
            return false;
        }

        /// <summary>
        /// Gets a required shape by canonical Contract-table slot.
        /// </summary>
        public AtlasResolvedShape GetRequiredShape(AtlasFieldSlot slot)
        {
            if (TryGetShape(slot, out var shape))
            {
                return shape;
            }

            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas resolved shape set '{0}' does not contain slot '{1}'.",
                    GetDiagnosticName(),
                    slot));
        }

        /// <summary>
        /// Gets a required shape by stable field id.
        /// </summary>
        public AtlasResolvedShape GetRequiredShape(StableDataId stableId)
        {
            if (TryGetShape(stableId, out var shape))
            {
                return shape;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas resolved shape set '{0}' does not contain field id '{1}'.",
                    GetDiagnosticName(),
                    stableId),
                nameof(stableId));
        }

        /// <summary>
        /// Gets a required shape by typed field declaration.
        /// </summary>
        public AtlasResolvedShape GetRequiredShape<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredShape(
                AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Gets the first required shape in the supplied concrete shape domain.
        /// </summary>
        public AtlasResolvedShape GetRequiredFirstShape(AtlasShapeDomain shapeDomain)
        {
            if (TryGetFirstShape(shapeDomain, out var shape))
            {
                return shape;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas resolved shape set '{0}' does not contain shape domain '{1}'.",
                    GetDiagnosticName(),
                    shapeDomain),
                nameof(shapeDomain));
        }

        /// <summary>
        /// Gets the first required shape in the supplied shape-domain kind.
        /// </summary>
        public AtlasResolvedShape GetRequiredFirstShape(AtlasShapeDomainKind kind)
        {
            if (TryGetFirstShape(kind, out var shape))
            {
                return shape;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas resolved shape set '{0}' does not contain shape-domain kind '{1}'.",
                    GetDiagnosticName(),
                    kind),
                nameof(kind));
        }

        /// <summary>
        /// Copies all shapes into a caller-provided destination array.
        /// </summary>
        public void CopyTo(AtlasResolvedShape[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _shapes.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Destination array length {0} is smaller than resolved shape count {1}.",
                        destination.Length,
                        _shapes.Length),
                    nameof(destination));
            }

            Array.Copy(
                _shapes,
                destination,
                _shapes.Length);
        }

        /// <summary>
        /// Copies a shape range into a caller-provided destination array.
        /// </summary>
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

            if (sourceIndex > _shapes.Length - length)
            {
                throw new ArgumentException(
                    "Source range exceeds resolved shape set bounds.",
                    nameof(length));
            }

            if (destinationIndex > destination.Length - length)
            {
                throw new ArgumentException(
                    "Destination range exceeds destination array bounds.",
                    nameof(length));
            }

            Array.Copy(
                _shapes,
                sourceIndex,
                destination,
                destinationIndex,
                length);
        }

        /// <summary>
        /// Copies all shapes matching the supplied shape-domain kind into a caller-provided destination array.
        /// </summary>
        public int CopyDomainKindTo(
            AtlasShapeDomainKind kind,
            AtlasResolvedShape[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (kind == AtlasShapeDomainKind.None)
            {
                return 0;
            }

            var count = CountDomainKind(kind);

            if (destination.Length < count)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Destination array length {0} is smaller than matching domain shape count {1}.",
                        destination.Length,
                        count),
                    nameof(destination));
            }

            var writeIndex = 0;

            for (var i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i].ShapeDomain.Kind == kind)
                {
                    destination[writeIndex] = _shapes[i];
                    writeIndex++;
                }
            }

            return writeIndex;
        }

        /// <summary>
        /// Creates a managed copy of this set's shapes.
        /// </summary>
        public AtlasResolvedShape[] ToArray()
        {
            var copy = new AtlasResolvedShape[_shapes.Length];

            Array.Copy(
                _shapes,
                copy,
                _shapes.Length);

            return copy;
        }

        /// <summary>
        /// Creates a managed copy of all shapes matching the supplied shape-domain kind.
        /// </summary>
        public AtlasResolvedShape[] ToArray(AtlasShapeDomainKind kind)
        {
            var count = CountDomainKind(kind);
            var copy = new AtlasResolvedShape[count];

            if (count == 0)
            {
                return copy;
            }

            CopyDomainKindTo(
                kind,
                copy);

            return copy;
        }

        /// <summary>
        /// Returns the diagnostic name of this resolved shape set.
        /// </summary>
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
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this shape set.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasResolvedShapeSet(Name={0}, Count={1}, ByteLength={2}, ByteCapacity={3}, DenseGrid={4}, VariablePayload={5}, External={6})",
                GetDiagnosticName(),
                Count,
                TotalByteLength,
                TotalByteCapacity,
                HasDenseGridDomain,
                HasVariablePayloadDomain,
                HasExternalDomain);
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

            Array.Copy(
                shapes,
                copy,
                shapes.Length);

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
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape count {0} must match Contract-table count {1}.",
                        shapes.Length,
                        contracts.Count),
                    parameterName);
            }

            for (var i = 0; i < shapes.Length; i++)
            {
                var contract = contracts[i];
                var shape = shapes[i];

                contract.ValidateTableReadyOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "contracts[{0}]",
                        i));

                shape.ValidateOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}[{1}]",
                        parameterName,
                        i));

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
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape '{0}' declares slot '{1}', but its shape-set index is '{2}'.",
                        shape.GetDiagnosticName(),
                        shape.Slot,
                        index),
                    parameterName);
            }

            if (contract.Slot.Index != index)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' declares slot '{1}', but its table index is '{2}'.",
                        contract.GetDiagnosticName(),
                        contract.Slot,
                        index),
                    parameterName);
            }

            if (shape.StableId != contract.StableId)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape at slot '{0}' has field id '{1}', but Contract table expects '{2}'.",
                        index,
                        shape.StableId,
                        contract.StableId),
                    parameterName);
            }

            if (shape.Slot != contract.Slot)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape at slot '{0}' has slot '{1}', but Contract table expects slot '{2}'.",
                        index,
                        shape.Slot,
                        contract.Slot),
                    parameterName);
            }

            if (shape.Role != contract.Role)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape '{0}' has role '{1}', but Contract expects '{2}'.",
                        shape.GetDiagnosticName(),
                        shape.Role,
                        contract.Role),
                    parameterName);
            }

            if (shape.StorageFormat != contract.StorageFormat)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape '{0}' has storage format '{1}', but Contract expects '{2}'.",
                        shape.GetDiagnosticName(),
                        shape.StorageFormat,
                        contract.StorageFormat),
                    parameterName);
            }

            if (shape.ShapeDomain != contract.ShapeDomain)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape '{0}' has shape domain '{1}', but Contract expects '{2}'.",
                        shape.GetDiagnosticName(),
                        shape.ShapeDomain,
                        contract.ShapeDomain),
                    parameterName);
            }

            if (shape.DeclaredShape != contract.LengthShape)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape '{0}' has declared shape '{1}', but Contract expects '{2}'.",
                        shape.GetDiagnosticName(),
                        shape.DeclaredShape,
                        contract.LengthShape),
                    parameterName);
            }

            if (!shape.DebugName.Equals(contract.DebugName))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape at slot '{0}' has debug name '{1}', but Contract expects '{2}'.",
                        index,
                        shape.DebugName,
                        contract.DebugName),
                    parameterName);
            }
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if (index >= 0 &&
                index < _shapes.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Resolved shape index must be between 0 and {0}.",
                    _shapes.Length - 1));
        }
    }
}