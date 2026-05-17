// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactField.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Represent one durable field-payload table entry inside an Atlas artifact.
// - Preserve field identity, slot, role, storage format, shape domain, resolved shape, and payload byte range.
// - Distinguish logical byte length, allocation byte capacity, and serialized payload byte length.
// - Represent optional per-field content hash without reserving zero as invalid.
// - Keep artifact field metadata separate from workspace memory ownership.
//
// Design notes
// - This is durable artifact metadata.
// - This does not own native memory.
// - This does not reference NativeArray, NativeSlice, JobHandle, UnityEngine, or renderer state.
// - Slot zero/default is valid.
// - StableDataId zero/default is valid.
// - ContentHash zero is valid; presence is represented by HasContentHash.
// - default(AtlasArtifactField) is not a concrete artifact field.
// - Missing/unwritten state is represented by IsConcrete, not by magic sentinels.
// - ByteOffset is artifact-payload-relative, not process memory address.
// - ByteLength is resolved logical content byte length.
// - ByteCapacity is resolved allocated storage byte capacity.
// - PayloadByteLength is the number of bytes serialized into the artifact payload.
// - ByteEndOffset is ByteOffset + PayloadByteLength.
// - Existing Create(...) overloads serialize capacity bytes for source compatibility.
// - Logical artifact capture should use CreateLogicalPayload(...) or CreateWithPayloadByteLength(...).

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Durable field-payload metadata entry inside an Atlas artifact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasArtifactField"/> describes one field payload written into an artifact:
    /// stable field identity, Contract-table slot, field role, shape domain, declared shape,
    /// storage format, resolved sizing, serialized payload byte range, and optional content hash.
    /// </para>
    ///
    /// <para>
    /// Logical content length and allocated capacity are separate from serialized artifact payload
    /// length. This lets artifact capture preserve allocation metadata while serializing only logical
    /// content bytes when required by durable artifact semantics.
    /// </para>
    ///
    /// <para>
    /// The per-field content hash is optional. Zero is a valid hash value, so
    /// <see cref="HasContentHash"/> owns hash presence.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasArtifactField :
        IEquatable<AtlasArtifactField>
    {
        private const byte NonConcreteState = 0;
        private const byte ConcreteState = 1;

        private const byte ContentHashAbsent = 0;
        private const byte ContentHashPresent = 1;

        private readonly byte _state;
        private readonly byte _contentHashState;

        /// <summary>
        /// Artifact field-table index.
        /// </summary>
        public readonly int FieldIndex;

        /// <summary>
        /// Durable field identity. Zero/default is valid.
        /// </summary>
        public readonly StableDataId StableId;

        /// <summary>
        /// Contract-table slot for this field. Slot zero/default is valid.
        /// </summary>
        public readonly AtlasFieldSlot Slot;

        /// <summary>
        /// Semantic field role.
        /// </summary>
        public readonly AtlasFieldRole Role;

        /// <summary>
        /// Physical storage format of the serialized field payload.
        /// </summary>
        public readonly StorageFormat StorageFormat;

        /// <summary>
        /// Semantic shape domain used to interpret resolved length and capacity.
        /// </summary>
        public readonly AtlasShapeDomain ShapeDomain;

        /// <summary>
        /// Declared symbolic shape from the Contract table.
        /// </summary>
        public readonly LengthShape DeclaredShape;

        /// <summary>
        /// Stable diagnostic field name.
        /// </summary>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Resolved logical element length.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Resolved element capacity.
        /// </summary>
        public readonly int Capacity;

        /// <summary>
        /// Resolved logical byte length.
        /// </summary>
        public readonly long ByteLength;

        /// <summary>
        /// Resolved allocated byte capacity.
        /// </summary>
        public readonly long ByteCapacity;

        /// <summary>
        /// Number of bytes serialized for this field into the artifact payload.
        /// </summary>
        /// <remarks>
        /// For logical-content artifacts this equals <see cref="ByteLength"/>. For capacity
        /// snapshots this equals <see cref="ByteCapacity"/>.
        /// </remarks>
        public readonly long PayloadByteLength;

        /// <summary>
        /// Payload-relative byte offset where this field's serialized bytes begin.
        /// </summary>
        public readonly long ByteOffset;

        /// <summary>
        /// Optional deterministic hash of this field's serialized payload bytes.
        /// </summary>
        public readonly ulong ContentHash;

        private AtlasArtifactField(
            int fieldIndex,
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
            long byteCapacity,
            long payloadByteLength,
            long byteOffset,
            ulong contentHash,
            bool hasContentHash)
        {
            ValidateInputsOrThrow(
                fieldIndex,
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
                byteCapacity,
                payloadByteLength,
                byteOffset);

            FieldIndex = fieldIndex;
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
            PayloadByteLength = payloadByteLength;
            ByteOffset = byteOffset;
            ContentHash = contentHash;
            _contentHashState = hasContentHash ? ContentHashPresent : ContentHashAbsent;
            _state = ConcreteState;
        }

        /// <summary>
        /// Gets whether this value represents a concrete artifact field entry.
        /// </summary>
        public bool IsConcrete
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state == ConcreteState;
        }

        /// <summary>
        /// Gets whether this value does not represent a concrete artifact field entry.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state != ConcreteState;
        }

        /// <summary>
        /// Gets whether <see cref="ContentHash"/> is present.
        /// </summary>
        public bool HasContentHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _contentHashState == ContentHashPresent;
        }

        /// <summary>
        /// Gets the exclusive payload-relative byte end offset.
        /// </summary>
        public long ByteEndOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => checked(ByteOffset + PayloadByteLength);
        }

        /// <summary>
        /// Gets whether this field has zero allocated bytes.
        /// </summary>
        public bool IsZeroCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ByteCapacity == 0L;
        }

        /// <summary>
        /// Gets whether this field has capacity bytes beyond logical bytes.
        /// </summary>
        public bool HasCapacitySlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ByteCapacity > ByteLength;
        }

        /// <summary>
        /// Gets whether this artifact field serializes logical content bytes.
        /// </summary>
        public bool SerializesLogicalContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PayloadByteLength == ByteLength;
        }

        /// <summary>
        /// Gets whether this artifact field serializes the full allocated capacity bytes.
        /// </summary>
        public bool SerializesCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PayloadByteLength == ByteCapacity;
        }

        /// <summary>
        /// Gets whether this artifact field serializes capacity slack bytes beyond logical content.
        /// </summary>
        public bool SerializesCapacitySlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PayloadByteLength > ByteLength;
        }

        /// <summary>
        /// Creates a capacity-payload artifact field entry from a resolved shape.
        /// </summary>
        public static AtlasArtifactField Create(
            AtlasResolvedShape shape,
            long byteOffset)
        {
            shape.ValidateOrThrow(nameof(shape));

            return Create(
                shape.Slot.Index,
                shape,
                byteOffset);
        }

        /// <summary>
        /// Creates a capacity-payload artifact field entry from a resolved shape and explicit field-table index.
        /// </summary>
        public static AtlasArtifactField Create(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                fieldIndex,
                shape,
                byteOffset,
                shape.ByteCapacity);
        }

        /// <summary>
        /// Creates a capacity-payload artifact field entry from a resolved shape and content hash.
        /// </summary>
        public static AtlasArtifactField Create(
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return Create(
                shape.Slot.Index,
                shape,
                byteOffset,
                contentHash);
        }

        /// <summary>
        /// Creates a capacity-payload artifact field entry from a resolved shape, explicit field-table index, and content hash.
        /// </summary>
        public static AtlasArtifactField Create(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                fieldIndex,
                shape,
                byteOffset,
                shape.ByteCapacity,
                contentHash);
        }

        /// <summary>
        /// Creates a logical-content artifact field entry from a resolved shape.
        /// </summary>
        public static AtlasArtifactField CreateLogicalPayload(
            AtlasResolvedShape shape,
            long byteOffset)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                shape.Slot.Index,
                shape,
                byteOffset,
                shape.ByteLength);
        }

        /// <summary>
        /// Creates a logical-content artifact field entry from a resolved shape and explicit field-table index.
        /// </summary>
        public static AtlasArtifactField CreateLogicalPayload(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                fieldIndex,
                shape,
                byteOffset,
                shape.ByteLength);
        }

        /// <summary>
        /// Creates a logical-content artifact field entry from a resolved shape and content hash.
        /// </summary>
        public static AtlasArtifactField CreateLogicalPayload(
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                shape.Slot.Index,
                shape,
                byteOffset,
                shape.ByteLength,
                contentHash);
        }

        /// <summary>
        /// Creates a logical-content artifact field entry from a resolved shape, explicit field-table index, and content hash.
        /// </summary>
        public static AtlasArtifactField CreateLogicalPayload(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                fieldIndex,
                shape,
                byteOffset,
                shape.ByteLength,
                contentHash);
        }

        /// <summary>
        /// Creates an artifact field entry with explicit serialized payload byte length.
        /// </summary>
        public static AtlasArtifactField CreateWithPayloadByteLength(
            AtlasResolvedShape shape,
            long byteOffset,
            long payloadByteLength)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                shape.Slot.Index,
                shape,
                byteOffset,
                payloadByteLength);
        }

        /// <summary>
        /// Creates an artifact field entry with explicit field-table index and serialized payload byte length.
        /// </summary>
        public static AtlasArtifactField CreateWithPayloadByteLength(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset,
            long payloadByteLength)
        {
            shape.ValidateOrThrow(nameof(shape));

            return new AtlasArtifactField(
                fieldIndex,
                shape.StableId,
                shape.Slot,
                shape.Role,
                shape.StorageFormat,
                shape.ShapeDomain,
                shape.DeclaredShape,
                shape.DebugName,
                shape.Length,
                shape.Capacity,
                shape.ByteLength,
                shape.ByteCapacity,
                payloadByteLength,
                byteOffset,
                0UL,
                hasContentHash: false);
        }

        /// <summary>
        /// Creates an artifact field entry with explicit serialized payload byte length and content hash.
        /// </summary>
        public static AtlasArtifactField CreateWithPayloadByteLength(
            AtlasResolvedShape shape,
            long byteOffset,
            long payloadByteLength,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return CreateWithPayloadByteLength(
                shape.Slot.Index,
                shape,
                byteOffset,
                payloadByteLength,
                contentHash);
        }

        /// <summary>
        /// Creates an artifact field entry with explicit field-table index, serialized payload byte length, and content hash.
        /// </summary>
        public static AtlasArtifactField CreateWithPayloadByteLength(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset,
            long payloadByteLength,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return new AtlasArtifactField(
                fieldIndex,
                shape.StableId,
                shape.Slot,
                shape.Role,
                shape.StorageFormat,
                shape.ShapeDomain,
                shape.DeclaredShape,
                shape.DebugName,
                shape.Length,
                shape.Capacity,
                shape.ByteLength,
                shape.ByteCapacity,
                payloadByteLength,
                byteOffset,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Creates an artifact field entry from serialized artifact metadata.
        /// </summary>
        internal static AtlasArtifactField CreateFromSerialized(
            int fieldIndex,
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
            long byteCapacity,
            long payloadByteLength,
            long byteOffset,
            ulong contentHash,
            bool hasContentHash)
        {
            return new AtlasArtifactField(
                fieldIndex,
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
                byteCapacity,
                payloadByteLength,
                byteOffset,
                contentHash,
                hasContentHash);
        }

        /// <summary>
        /// Creates a capacity-payload artifact field entry from a Contract row and matching resolved shape.
        /// </summary>
        public static AtlasArtifactField Create(
            AtlasContract contract,
            AtlasResolvedShape shape,
            long byteOffset)
        {
            ValidateContractMatchesShapeOrThrow(
                contract,
                shape);

            return Create(
                contract.Slot.Index,
                shape,
                byteOffset);
        }

        /// <summary>
        /// Creates a capacity-payload artifact field entry from a Contract row, matching resolved shape, and content hash.
        /// </summary>
        public static AtlasArtifactField Create(
            AtlasContract contract,
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            ValidateContractMatchesShapeOrThrow(
                contract,
                shape);

            return Create(
                contract.Slot.Index,
                shape,
                byteOffset,
                contentHash);
        }

        /// <summary>
        /// Creates a logical-content artifact field entry from a Contract row and matching resolved shape.
        /// </summary>
        public static AtlasArtifactField CreateLogicalPayload(
            AtlasContract contract,
            AtlasResolvedShape shape,
            long byteOffset)
        {
            ValidateContractMatchesShapeOrThrow(
                contract,
                shape);

            return CreateLogicalPayload(
                contract.Slot.Index,
                shape,
                byteOffset);
        }

        /// <summary>
        /// Creates a logical-content artifact field entry from a Contract row, matching resolved shape, and content hash.
        /// </summary>
        public static AtlasArtifactField CreateLogicalPayload(
            AtlasContract contract,
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            ValidateContractMatchesShapeOrThrow(
                contract,
                shape);

            return CreateLogicalPayload(
                contract.Slot.Index,
                shape,
                byteOffset,
                contentHash);
        }

        /// <summary>
        /// Creates an artifact field entry from a Contract row, matching resolved shape, and explicit payload byte length.
        /// </summary>
        public static AtlasArtifactField CreateWithPayloadByteLength(
            AtlasContract contract,
            AtlasResolvedShape shape,
            long byteOffset,
            long payloadByteLength)
        {
            ValidateContractMatchesShapeOrThrow(
                contract,
                shape);

            return CreateWithPayloadByteLength(
                contract.Slot.Index,
                shape,
                byteOffset,
                payloadByteLength);
        }

        /// <summary>
        /// Creates an artifact field entry from a Contract row, matching resolved shape, explicit payload byte length, and content hash.
        /// </summary>
        public static AtlasArtifactField CreateWithPayloadByteLength(
            AtlasContract contract,
            AtlasResolvedShape shape,
            long byteOffset,
            long payloadByteLength,
            ulong contentHash)
        {
            ValidateContractMatchesShapeOrThrow(
                contract,
                shape);

            return CreateWithPayloadByteLength(
                contract.Slot.Index,
                shape,
                byteOffset,
                payloadByteLength,
                contentHash);
        }

        /// <summary>
        /// Validates that this artifact field entry is concrete and internally consistent.
        /// </summary>
        public void ValidateOrThrow(
            string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasArtifactField);

            if (!IsConcrete)
            {
                throw new ArgumentException(
                    "Atlas artifact field is not concrete.",
                    name);
            }

            ValidateInputsOrThrow(
                FieldIndex,
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity,
                PayloadByteLength,
                ByteOffset,
                name);
        }

        /// <summary>
        /// Returns a copy of this artifact field with a content hash.
        /// </summary>
        public AtlasArtifactField WithContentHash(
            ulong contentHash)
        {
            ValidateOrThrow(nameof(AtlasArtifactField));

            return new AtlasArtifactField(
                FieldIndex,
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity,
                PayloadByteLength,
                ByteOffset,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Returns a copy of this artifact field without a content hash.
        /// </summary>
        public AtlasArtifactField WithoutContentHash()
        {
            ValidateOrThrow(nameof(AtlasArtifactField));

            return new AtlasArtifactField(
                FieldIndex,
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity,
                PayloadByteLength,
                ByteOffset,
                0UL,
                hasContentHash: false);
        }

        /// <summary>
        /// Returns a copy of this artifact field with a different serialized payload byte length.
        /// </summary>
        public AtlasArtifactField WithPayloadByteLength(
            long payloadByteLength)
        {
            ValidateOrThrow(nameof(AtlasArtifactField));

            return new AtlasArtifactField(
                FieldIndex,
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity,
                payloadByteLength,
                ByteOffset,
                ContentHash,
                HasContentHash);
        }

        /// <summary>
        /// Returns a logical-content payload variant of this artifact field.
        /// </summary>
        public AtlasArtifactField AsLogicalPayload()
        {
            return WithPayloadByteLength(ByteLength);
        }

        /// <summary>
        /// Returns a capacity-payload variant of this artifact field.
        /// </summary>
        public AtlasArtifactField AsCapacityPayload()
        {
            return WithPayloadByteLength(ByteCapacity);
        }

        /// <summary>
        /// Determines whether this artifact field equals another artifact field.
        /// </summary>
        public bool Equals(
            AtlasArtifactField other)
        {
            return _state == other._state &&
                   _contentHashState == other._contentHashState &&
                   FieldIndex == other.FieldIndex &&
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
                   ByteCapacity == other.ByteCapacity &&
                   PayloadByteLength == other.PayloadByteLength &&
                   ByteOffset == other.ByteOffset &&
                   ContentHash == other.ContentHash;
        }

        /// <summary>
        /// Determines whether this artifact field equals another object.
        /// </summary>
        public override bool Equals(
            object obj)
        {
            return obj is AtlasArtifactField other &&
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
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _state;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _contentHashState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FieldIndex;
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
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(PayloadByteLength);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(ByteOffset);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldULong(ContentHash);
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this artifact field.
        /// </summary>
        public override string ToString()
        {
            if (!IsConcrete)
            {
                return "AtlasArtifactField(<empty>)";
            }

            var contentHashText = HasContentHash
                ? FormatHex(ContentHash)
                : "<absent>";

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasArtifactField(Index={0}, Name={1}, StableId={2}, Slot={3}, Role={4}, Domain={5}, Storage={6}, Length={7}, Capacity={8}, Offset={9}, PayloadBytes={10}, Bytes={11}/{12}, ContentHash={13})",
                FieldIndex,
                DebugName,
                StableId,
                Slot,
                Role,
                ShapeDomain,
                StorageFormat.Kind,
                Length,
                Capacity,
                ByteOffset,
                PayloadByteLength,
                ByteLength,
                ByteCapacity,
                contentHashText);
        }

        /// <summary>
        /// Compares two artifact field entries for equality.
        /// </summary>
        public static bool operator ==(
            AtlasArtifactField left,
            AtlasArtifactField right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two artifact field entries for inequality.
        /// </summary>
        public static bool operator !=(
            AtlasArtifactField left,
            AtlasArtifactField right)
        {
            return !left.Equals(right);
        }

        private static void ValidateContractMatchesShapeOrThrow(
            AtlasContract contract,
            AtlasResolvedShape shape)
        {
            contract.ValidateTableReadyOrThrow(nameof(contract));
            shape.ValidateOrThrow(nameof(shape));

            if (contract.StableId != shape.StableId ||
                contract.Slot != shape.Slot ||
                contract.Role != shape.Role ||
                contract.StorageFormat != shape.StorageFormat ||
                contract.ShapeDomain != shape.ShapeDomain ||
                contract.LengthShape != shape.DeclaredShape ||
                !contract.DebugName.Equals(shape.DebugName))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas artifact field Contract '{0}' does not match resolved shape '{1}'.",
                        contract.GetDiagnosticName(),
                        shape.GetDiagnosticName()),
                    nameof(shape));
            }
        }

        private static void ValidateInputsOrThrow(
            int fieldIndex,
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
            long byteCapacity,
            long payloadByteLength,
            long byteOffset,
            string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasArtifactField);

            if (fieldIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldIndex),
                    fieldIndex,
                    "Artifact field index must be non-negative.");
            }

            stableId.ValidateOrThrow(name);
            slot.ValidateOrThrow(name);

            if (role == AtlasFieldRole.None)
            {
                throw new ArgumentException(
                    "Artifact field must declare a concrete field role.",
                    name);
            }

            storageFormat.ValidateOrThrow(name);
            shapeDomain.ValidateOrThrow(name);
            declaredShape.ValidateOrThrow(name);

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Artifact field must declare a non-empty diagnostic name.",
                    name);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Artifact field length must be non-negative.");
            }

            if (capacity < length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Artifact field capacity must be greater than or equal to logical length.");
            }

            if (byteLength < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(byteLength),
                    byteLength,
                    "Artifact field byte length must be non-negative.");
            }

            if (byteCapacity < byteLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(byteCapacity),
                    byteCapacity,
                    "Artifact field byte capacity must be greater than or equal to logical byte length.");
            }

            if (payloadByteLength < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloadByteLength),
                    payloadByteLength,
                    "Artifact field payload byte length must be non-negative.");
            }

            if (payloadByteLength > byteCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloadByteLength),
                    payloadByteLength,
                    "Artifact field payload byte length must be less than or equal to allocated byte capacity.");
            }

            if (payloadByteLength < byteLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloadByteLength),
                    payloadByteLength,
                    "Artifact field payload byte length must be greater than or equal to logical byte length.");
            }

            if (byteOffset < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(byteOffset),
                    byteOffset,
                    "Artifact field byte offset must be non-negative.");
            }

            var expectedByteLength = checked((long)storageFormat.ElementSize * length);
            var expectedByteCapacity = checked((long)storageFormat.ElementSize * capacity);

            if (byteLength != expectedByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field byte length '{0}' does not match expected byte length '{1}'.",
                        byteLength,
                        expectedByteLength),
                    name);
            }

            if (byteCapacity != expectedByteCapacity)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field byte capacity '{0}' does not match expected byte capacity '{1}'.",
                        byteCapacity,
                        expectedByteCapacity),
                    name);
            }

            _ = checked(byteOffset + payloadByteLength);
        }

        private static int FoldLong(long value)
        {
            return FoldULong(
                unchecked((ulong)value));
        }

        private static int FoldULong(ulong value)
        {
            unchecked
            {
                return (int)(value ^ (value >> 32));
            }
        }

        private static string FormatHex(
            ulong value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "0x{0:X16}",
                value);
        }
    }
}