// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasResolvedShape.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one concrete resolved runtime shape for one Atlas Contract-table field.
// - Preserve explicit resolved/unresolved state without using invalid StableDataId or slot sentinels.
// - Carry semantic shape-domain identity through the compiler/workspace/artifact boundary.
// - Carry length, capacity, element layout, and byte-size metadata before workspace-layout compilation.
// - Remain allocation-free, immutable, deterministic, and safe as compiler metadata.
//
// Design notes
// - default(AtlasResolvedShape) is unresolved, not an invalid field identity.
// - StableDataId zero/default is valid.
// - AtlasFieldSlot zero/default is valid.
// - Missing/unresolved state is represented by _resolutionState.
// - ShapeDomain describes what resolved length/capacity means.
// - DeclaredShape describes how length/capacity was resolved.
// - Resolved logical length and resolved physical capacity are related but not identical concepts.
// - Capacity must be greater than or equal to length.
// - Scalar storage must resolve to length 1 and capacity 1.
// - NativeArray storage may resolve to capacity greater than length.
// - This type does not allocate memory.
// - This type does not own memory.
// - This type is not a workspace layout.
// - This type is not a field address.
// - Jobs should not resolve shapes; jobs should consume already allocated native containers/views.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Concrete resolved length, capacity, domain, and byte-size metadata for one Atlas field Contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasResolvedShape"/> is produced after plan/Contract validation and before
    /// workspace-layout compilation. It converts symbolic <see cref="LengthShape"/> metadata into
    /// concrete runtime sizing information while preserving the source Contract identity,
    /// shape-domain identity, and storage schema.
    /// </para>
    ///
    /// <para>
    /// This value does not allocate memory and does not expose runtime storage. It is compiler
    /// metadata used by later memory-layout, workspace, artifact, and debug-export passes.
    /// </para>
    ///
    /// <para>
    /// Because <see cref="StableDataId"/> and <see cref="AtlasFieldSlot"/> both allow zero/default
    /// as valid values, this type uses explicit resolution state instead of sentinel identity values.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasResolvedShape :
        IEquatable<AtlasResolvedShape>
    {
        private const byte UnresolvedState = 0;
        private const byte ResolvedState = 1;

        private readonly byte _resolutionState;

        /// <summary>
        /// Durable field identity copied from the source Contract.
        /// </summary>
        public readonly StableDataId StableId;

        /// <summary>
        /// Contract-table slot copied from the source Contract.
        /// </summary>
        public readonly AtlasFieldSlot Slot;

        /// <summary>
        /// Semantic role copied from the source Contract.
        /// </summary>
        public readonly AtlasFieldRole Role;

        /// <summary>
        /// Physical storage format copied from the source Contract.
        /// </summary>
        public readonly StorageFormat StorageFormat;

        /// <summary>
        /// Semantic shape domain copied from the source Contract.
        /// </summary>
        public readonly AtlasShapeDomain ShapeDomain;

        /// <summary>
        /// Original declared symbolic length shape copied from the source Contract.
        /// </summary>
        public readonly LengthShape DeclaredShape;

        /// <summary>
        /// Stable diagnostic name copied from the source Contract.
        /// </summary>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Resolved logical element length.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Resolved element capacity.
        /// </summary>
        /// <remarks>
        /// Capacity must be greater than or equal to <see cref="Length"/>. Scalar storage requires
        /// capacity and length to both be one. Contiguous array storage may reserve slack capacity
        /// when the shape resolver or later layout policy requires it.
        /// </remarks>
        public readonly int Capacity;

        /// <summary>
        /// Resolved byte count for logical element length.
        /// </summary>
        public readonly long ByteLength;

        /// <summary>
        /// Resolved byte count for allocated element capacity.
        /// </summary>
        public readonly long ByteCapacity;

        private AtlasResolvedShape(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            FixedString64Bytes debugName,
            int length,
            int capacity,
            long byteLength,
            long byteCapacity)
        {
            StableId = stableId;
            Slot = slot;
            Role = role;
            StorageFormat = storageFormat;
            ShapeDomain = shapeDomain;
            DeclaredShape = declaredShape;
            DebugName = debugName;
            Length = length;
            Capacity = capacity;
            ByteLength = byteLength;
            ByteCapacity = byteCapacity;
            _resolutionState = ResolvedState;
        }

        /// <summary>
        /// Gets whether this value contains resolved shape metadata.
        /// </summary>
        public bool IsResolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _resolutionState == ResolvedState;
        }

        /// <summary>
        /// Gets whether this value is unresolved.
        /// </summary>
        public bool IsUnresolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _resolutionState != ResolvedState;
        }

        /// <summary>
        /// Gets whether the resolved logical length is zero.
        /// </summary>
        public bool IsZeroLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsResolved && Length == 0;
        }

        /// <summary>
        /// Gets whether the resolved capacity is zero.
        /// </summary>
        public bool IsZeroCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsResolved && Capacity == 0;
        }

        /// <summary>
        /// Gets whether capacity exceeds logical length.
        /// </summary>
        public bool HasCapacitySlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsResolved && Capacity > Length;
        }

        /// <summary>
        /// Gets whether this shape requires non-zero memory capacity.
        /// </summary>
        public bool RequiresMemory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsResolved && ByteCapacity > 0L;
        }

        /// <summary>
        /// Gets whether this resolved shape represents dense grid data.
        /// </summary>
        public bool IsDenseGrid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsResolved && ShapeDomain.IsDenseGrid;
        }

        /// <summary>
        /// Gets whether this resolved shape represents variable payload data.
        /// </summary>
        public bool IsVariablePayload
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsResolved && ShapeDomain.IsVariablePayload;
        }

        /// <summary>
        /// Creates a resolved shape whose capacity equals its logical length.
        /// </summary>
        public static AtlasResolvedShape Create(
            AtlasContract contract,
            int length)
        {
            return Create(
                contract,
                length,
                length);
        }

        /// <summary>
        /// Creates a resolved shape from a table-ready Contract and explicit length/capacity.
        /// </summary>
        public static AtlasResolvedShape Create(
            AtlasContract contract,
            int length,
            int capacity)
        {
            contract.ValidateTableReadyOrThrow(nameof(contract));

            return Create(
                contract.StableId,
                contract.Slot,
                contract.Role,
                contract.StorageFormat,
                contract.ShapeDomain,
                contract.LengthShape,
                contract.DebugName,
                length,
                capacity);
        }

        /// <summary>
        /// Creates a resolved shape from explicit field, domain, and storage metadata.
        /// </summary>
        public static AtlasResolvedShape Create(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            FixedString64Bytes debugName,
            int length,
            int capacity)
        {
            ValidateInputsOrThrow(
                stableId,
                slot,
                role,
                storageFormat,
                shapeDomain,
                declaredShape,
                debugName,
                length,
                capacity);

            var byteLength = ComputeByteCount(
                storageFormat,
                length);

            var byteCapacity = ComputeByteCount(
                storageFormat,
                capacity);

            return new AtlasResolvedShape(
                stableId,
                slot,
                role,
                storageFormat,
                shapeDomain,
                declaredShape,
                debugName,
                length,
                capacity,
                byteLength,
                byteCapacity);
        }

        /// <summary>
        /// Returns a copy with a different resolved length and matching capacity.
        /// </summary>
        public AtlasResolvedShape WithLength(int length)
        {
            ValidateResolvedOrThrow();

            return Create(
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                length,
                length);
        }

        /// <summary>
        /// Returns a copy with different resolved length and capacity.
        /// </summary>
        public AtlasResolvedShape WithLengthAndCapacity(
            int length,
            int capacity)
        {
            ValidateResolvedOrThrow();

            return Create(
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                length,
                capacity);
        }

        /// <summary>
        /// Returns a copy with a different shape domain.
        /// </summary>
        public AtlasResolvedShape WithShapeDomain(AtlasShapeDomain shapeDomain)
        {
            ValidateResolvedOrThrow();

            return Create(
                StableId,
                Slot,
                Role,
                StorageFormat,
                shapeDomain,
                DeclaredShape,
                DebugName,
                Length,
                Capacity);
        }

        /// <summary>
        /// Validates that this value contains resolved and internally consistent shape metadata.
        /// </summary>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasResolvedShape);

            if (IsUnresolved)
            {
                throw new ArgumentException(
                    "Atlas resolved shape is unresolved.",
                    name);
            }

            ValidateInputsOrThrow(
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                name);

            var expectedByteLength = ComputeByteCount(
                StorageFormat,
                Length);

            var expectedByteCapacity = ComputeByteCount(
                StorageFormat,
                Capacity);

            if (ByteLength != expectedByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas resolved shape '{0}' has byte length {1}, but expected {2}.",
                        GetDiagnosticName(),
                        ByteLength,
                        expectedByteLength),
                    name);
            }

            if (ByteCapacity != expectedByteCapacity)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas resolved shape '{0}' has byte capacity {1}, but expected {2}.",
                        GetDiagnosticName(),
                        ByteCapacity,
                        expectedByteCapacity),
                    name);
            }
        }

        /// <summary>
        /// Throws when this value is unresolved.
        /// </summary>
        public void ValidateResolvedOrThrow(string parameterName = null)
        {
            if (IsResolved)
            {
                return;
            }

            throw new ArgumentException(
                "Atlas resolved shape is unresolved.",
                parameterName ?? nameof(AtlasResolvedShape));
        }

        /// <summary>
        /// Returns a stable diagnostic field name.
        /// </summary>
        public string GetDiagnosticName()
        {
            if (!DebugName.IsEmpty)
            {
                return DebugName.ToString();
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "StableId:{0}",
                StableId);
        }

        /// <summary>
        /// Determines whether this shape is equal to another shape.
        /// </summary>
        public bool Equals(AtlasResolvedShape other)
        {
            return _resolutionState == other._resolutionState &&
                   StableId == other.StableId &&
                   Slot == other.Slot &&
                   Role == other.Role &&
                   StorageFormat == other.StorageFormat &&
                   ShapeDomain == other.ShapeDomain &&
                   DeclaredShape == other.DeclaredShape &&
                   DebugName.Equals(other.DebugName) &&
                   Length == other.Length &&
                   Capacity == other.Capacity &&
                   ByteLength == other.ByteLength &&
                   ByteCapacity == other.ByteCapacity;
        }

        /// <summary>
        /// Determines whether this shape is equal to an object instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasResolvedShape other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _resolutionState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ StableId.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Slot.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ (int)Role;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ StorageFormat.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ShapeDomain.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ DeclaredShape.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ DebugName.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Length;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Capacity;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteLength);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteCapacity);
                return hash;
            }
        }

        /// <summary>
        /// Returns a stable diagnostic string.
        /// </summary>
        public override string ToString()
        {
            if (IsUnresolved)
            {
                return "AtlasResolvedShape(Unresolved)";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} [{1}] Slot={2} Role={3} Domain={4} Storage={5} Length={6} Capacity={7} Bytes={8}/{9}",
                DebugName,
                StableId,
                Slot,
                Role,
                ShapeDomain,
                StorageFormat.Kind,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity);
        }

        public static bool operator ==(
            AtlasResolvedShape left,
            AtlasResolvedShape right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            AtlasResolvedShape left,
            AtlasResolvedShape right)
        {
            return !left.Equals(right);
        }

        private static void ValidateInputsOrThrow(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            FixedString64Bytes debugName,
            int length,
            int capacity,
            string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasResolvedShape);

            stableId.ValidateOrThrow(name);
            slot.ValidateOrThrow(name);

            if (role == AtlasFieldRole.None)
            {
                throw new ArgumentException(
                    "Resolved shape requires a concrete field role.",
                    name);
            }

            storageFormat.ValidateOrThrow(name);
            shapeDomain.ValidateOrThrow(name);
            declaredShape.ValidateOrThrow(name);

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Resolved shape requires a non-empty diagnostic name.",
                    name);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Resolved length must be greater than or equal to zero.");
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Resolved capacity must be greater than or equal to zero.");
            }

            if (capacity < length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved capacity {0} must be greater than or equal to resolved length {1}.",
                        capacity,
                        length),
                    name);
            }

            if (storageFormat.Kind == StorageKind.Scalar &&
                (length != 1 || capacity != 1))
            {
                throw new ArgumentException(
                    "Scalar storage must resolve to length 1 and capacity 1.",
                    name);
            }

            ValidateDomainShapeCompatibilityOrThrow(
                shapeDomain,
                declaredShape,
                storageFormat,
                length,
                capacity,
                name);
        }

        private static void ValidateDomainShapeCompatibilityOrThrow(
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            StorageFormat storageFormat,
            int length,
            int capacity,
            string parameterName)
        {
            if (shapeDomain.Kind == AtlasShapeDomainKind.Scalar &&
                (declaredShape.Kind != LengthShapeKind.Scalar || length != 1 || capacity != 1))
            {
                throw new ArgumentException(
                    "Scalar shape domain must resolve from scalar length shape to length 1 and capacity 1.",
                    parameterName);
            }

            if (declaredShape.Kind == LengthShapeKind.Scalar &&
                shapeDomain.Kind != AtlasShapeDomainKind.Scalar &&
                shapeDomain.Kind != AtlasShapeDomainKind.FixedVector)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Scalar length shape is incompatible with shape domain '{0}'.",
                        shapeDomain.Kind),
                    parameterName);
            }

            if (shapeDomain.Kind == AtlasShapeDomainKind.External &&
                storageFormat.Kind != StorageKind.External)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "External shape domain requires external storage kind, but storage kind is '{0}'.",
                        storageFormat.Kind),
                    parameterName);
            }

            if (storageFormat.Kind == StorageKind.External &&
                shapeDomain.Kind != AtlasShapeDomainKind.External)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "External storage kind requires external shape domain, but shape domain is '{0}'.",
                        shapeDomain.Kind),
                    parameterName);
            }

            if (declaredShape.Kind == LengthShapeKind.External &&
                shapeDomain.Kind != AtlasShapeDomainKind.External)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "External length shape requires external shape domain, but shape domain is '{0}'.",
                        shapeDomain.Kind),
                    parameterName);
            }

            if (declaredShape.Kind == LengthShapeKind.PrefixSumPayload &&
                shapeDomain.Kind != AtlasShapeDomainKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Prefix-sum payload length shape requires prefix-sum payload shape domain, but shape domain is '{0}'.",
                        shapeDomain.Kind),
                    parameterName);
            }

            if (shapeDomain.Kind == AtlasShapeDomainKind.PrefixSumPayload &&
                declaredShape.Kind != LengthShapeKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Prefix-sum payload shape domain requires prefix-sum payload length shape, but length shape is '{0}'.",
                        declaredShape.Kind),
                    parameterName);
            }

            if (shapeDomain.HasSourceField &&
                shapeDomain.SourceFieldId != declaredShape.SourceFieldId &&
                declaredShape.Kind != LengthShapeKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Shape domain source field '{0}' does not match declared length-shape source field '{1}'.",
                        shapeDomain.SourceFieldId,
                        declaredShape.SourceFieldId),
                    parameterName);
            }
        }

        private static long ComputeByteCount(
            StorageFormat storageFormat,
            int elementCount)
        {
            return checked((long)storageFormat.ElementSize * elementCount);
        }

        private static int FoldLong(long value)
        {
            unchecked
            {
                var bits = (ulong)value;
                return (int)(bits ^ (bits >> 32));
            }
        }
    }
}