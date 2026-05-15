// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasResolvedShape.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one concrete resolved runtime shape for one Atlas Contract-table field.
// - Preserve explicit resolved/unresolved state without using invalid StableDataId or slot sentinels.
// - Carry length, capacity, element layout, and byte-size metadata before workspace allocation.
// - Remain allocation-free, immutable, deterministic, and safe as compiler metadata.
//
// Design notes
// - default(AtlasResolvedShape) is unresolved, not an invalid field identity.
// - StableDataId zero/default is valid.
// - AtlasFieldSlot zero/default is valid.
// - Missing/unresolved state is represented by _resolutionState.
// - This type does not allocate memory.
// - This type does not own memory.
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
    /// Concrete resolved length, capacity, and byte-size metadata for one Atlas field contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasResolvedShape"/> is produced after plan validation and before workspace
    /// memory allocation. It converts symbolic <see cref="LengthShape"/> metadata into concrete
    /// runtime sizing information while preserving the source Contract identity and storage layout.
    /// </para>
    ///
    /// <para>
    /// This value does not allocate memory and does not expose runtime storage. It is compiler
    /// metadata used by later memory-layout and workspace passes.
    /// </para>
    ///
    /// <para>
    /// Because <see cref="StableDataId"/> and <see cref="AtlasFieldSlot"/> both allow zero/default
    /// as valid values, this type uses explicit presence state instead of sentinel identity values.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasResolvedShape :
        IEquatable<AtlasResolvedShape>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

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
        /// For fixed-size storage this equals <see cref="Length"/>. For growable storage this may
        /// be greater than <see cref="Length"/>.
        /// </remarks>
        public readonly int Capacity;

        /// <summary>
        /// Resolved byte count for the logical element length.
        /// </summary>
        public readonly long ByteLength;

        /// <summary>
        /// Resolved byte count for the allocated element capacity.
        /// </summary>
        public readonly long ByteCapacity;

        private AtlasResolvedShape(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
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
        /// Creates a resolved shape whose capacity equals its length.
        /// </summary>
        /// <param name="contract">Resolved table-ready source Contract.</param>
        /// <param name="length">Resolved logical length.</param>
        /// <returns>A resolved shape for the supplied Contract.</returns>
        public static AtlasResolvedShape Create(
            AtlasContract contract,
            int length)
        {
            return Create(contract, length, length);
        }

        /// <summary>
        /// Creates a resolved shape from a table-ready Contract and explicit length/capacity.
        /// </summary>
        /// <param name="contract">Resolved table-ready source Contract.</param>
        /// <param name="length">Resolved logical length.</param>
        /// <param name="capacity">Resolved element capacity.</param>
        /// <returns>A resolved shape for the supplied Contract.</returns>
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
                contract.LengthShape,
                contract.DebugName,
                length,
                capacity);
        }

        /// <summary>
        /// Creates a resolved shape from explicit field and storage metadata.
        /// </summary>
        /// <param name="stableId">Durable field identity. Zero/default is valid.</param>
        /// <param name="slot">Contract-table slot. Slot zero/default is valid.</param>
        /// <param name="role">Semantic field role.</param>
        /// <param name="storageFormat">Physical storage format.</param>
        /// <param name="declaredShape">Original symbolic length shape.</param>
        /// <param name="debugName">Stable diagnostic field name.</param>
        /// <param name="length">Resolved logical length.</param>
        /// <param name="capacity">Resolved element capacity.</param>
        /// <returns>A resolved shape value.</returns>
        public static AtlasResolvedShape Create(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
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
                declaredShape,
                debugName,
                length,
                capacity);

            var byteLength = ComputeByteCount(storageFormat, length);
            var byteCapacity = ComputeByteCount(storageFormat, capacity);

            return new AtlasResolvedShape(
                stableId,
                slot,
                role,
                storageFormat,
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
        /// <param name="length">New resolved logical length and capacity.</param>
        /// <returns>A resolved shape with updated length and capacity.</returns>
        public AtlasResolvedShape WithLength(int length)
        {
            ValidateResolvedOrThrow();

            return Create(
                StableId,
                Slot,
                Role,
                StorageFormat,
                DeclaredShape,
                DebugName,
                length,
                length);
        }

        /// <summary>
        /// Returns a copy with different resolved length and capacity.
        /// </summary>
        /// <param name="length">New resolved logical length.</param>
        /// <param name="capacity">New resolved element capacity.</param>
        /// <returns>A resolved shape with updated length and capacity.</returns>
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
                DeclaredShape,
                DebugName,
                length,
                capacity);
        }

        /// <summary>
        /// Validates that this value contains resolved and internally consistent shape metadata.
        /// </summary>
        /// <param name="parameterName">Optional caller parameter name.</param>
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
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                name);

            var expectedByteLength = ComputeByteCount(StorageFormat, Length);
            var expectedByteCapacity = ComputeByteCount(StorageFormat, Capacity);

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
        /// <param name="parameterName">Optional caller parameter name.</param>
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
        /// <returns>The debug name when present; otherwise the stable identity.</returns>
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
        /// <param name="other">The other shape.</param>
        /// <returns><c>true</c> when all fields match; otherwise, <c>false</c>.</returns>
        public bool Equals(AtlasResolvedShape other)
        {
            return _resolutionState == other._resolutionState &&
                   StableId == other.StableId &&
                   Slot == other.Slot &&
                   Role == other.Role &&
                   StorageFormat == other.StorageFormat &&
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
        /// <param name="obj">Object to compare.</param>
        /// <returns><c>true</c> when the object is an equal resolved shape.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasResolvedShape other && Equals(other);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        /// <returns>A deterministic hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ _resolutionState;
                hash = (hash * HashMultiplier) ^ StableId.GetHashCode();
                hash = (hash * HashMultiplier) ^ Slot.GetHashCode();
                hash = (hash * HashMultiplier) ^ (int)Role;
                hash = (hash * HashMultiplier) ^ StorageFormat.GetHashCode();
                hash = (hash * HashMultiplier) ^ DeclaredShape.GetHashCode();
                hash = (hash * HashMultiplier) ^ DebugName.GetHashCode();
                hash = (hash * HashMultiplier) ^ Length;
                hash = (hash * HashMultiplier) ^ Capacity;
                hash = (hash * HashMultiplier) ^ ByteLength.GetHashCode();
                hash = (hash * HashMultiplier) ^ ByteCapacity.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a stable diagnostic string.
        /// </summary>
        /// <returns>A diagnostic representation of the resolved shape.</returns>
        public override string ToString()
        {
            if (IsUnresolved)
            {
                return "AtlasResolvedShape(Unresolved)";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} [{1}] Slot={2} Role={3} Storage={4} Length={5} Capacity={6} Bytes={7}/{8}",
                DebugName,
                StableId,
                Slot,
                Role,
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

            if (storageFormat.RequiresFixedLength &&
                capacity != length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Storage kind '{0}' requires capacity to equal length.",
                        storageFormat.Kind),
                    name);
            }
        }

        private static long ComputeByteCount(
            StorageFormat storageFormat,
            int elementCount)
        {
            return checked((long)storageFormat.ElementSize * elementCount);
        }
    }
}