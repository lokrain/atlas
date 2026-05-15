// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactField.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Represent one durable field-payload table entry inside an Atlas artifact.
// - Preserve field identity, slot, role, storage format, resolved shape, and payload byte range.
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
    /// its stable field identity, Contract-table slot, resolved storage shape, byte range, and
    /// optional content hash.
    /// </para>
    ///
    /// <para>
    /// This value is intentionally metadata only. It must not own workspace memory, expose native
    /// containers, schedule jobs, or depend on rendering/debug systems. Artifact writers use this
    /// entry to describe where each workspace field was serialized into the durable payload.
    /// </para>
    ///
    /// <para>
    /// The per-field content hash is optional because the early vertical slice can write useful
    /// artifact metadata and payloads before final field-content hashing is complete. Because zero
    /// is a valid hash value, <see cref="HasContentHash"/> owns hash presence.
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
        /// <remarks>
        /// This is the row index inside the artifact field table. For the canonical Contract-table
        /// order used by the first artifact writer, this should match <see cref="Slot"/>.<c>Index</c>.
        /// </remarks>
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
        /// Payload-relative byte offset where this field's bytes begin.
        /// </summary>
        public readonly long ByteOffset;

        /// <summary>
        /// Optional deterministic hash of this field's serialized content bytes.
        /// </summary>
        /// <remarks>
        /// Zero is valid. Use <see cref="HasContentHash"/> to check presence.
        /// </remarks>
        public readonly ulong ContentHash;

        private AtlasArtifactField(
            int fieldIndex,
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            LengthShape declaredShape,
            FixedString64Bytes debugName,
            int length,
            int capacity,
            long byteLength,
            long byteCapacity,
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
                declaredShape,
                length,
                capacity,
                byteLength,
                byteCapacity,
                byteOffset);

            FieldIndex = fieldIndex;
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
            get => checked(ByteOffset + ByteCapacity);
        }

        /// <summary>
        /// Gets whether this field payload has zero allocated bytes.
        /// </summary>
        public bool IsZeroCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ByteCapacity == 0L;
        }

        /// <summary>
        /// Gets whether this field payload has capacity bytes beyond logical bytes.
        /// </summary>
        public bool HasCapacitySlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ByteCapacity > ByteLength;
        }

        /// <summary>
        /// Creates an artifact field entry from a resolved shape.
        /// </summary>
        /// <param name="shape">Resolved shape represented by this artifact field.</param>
        /// <param name="byteOffset">Payload-relative byte offset.</param>
        /// <returns>A concrete artifact field entry without content hash.</returns>
        public static AtlasArtifactField Create(
            AtlasResolvedShape shape,
            long byteOffset)
        {
            shape.ValidateOrThrow(nameof(shape));

            return Create(
                fieldIndex: shape.Slot.Index,
                shape,
                byteOffset);
        }

        /// <summary>
        /// Creates an artifact field entry from a resolved shape and explicit field-table index.
        /// </summary>
        /// <param name="fieldIndex">Artifact field-table index.</param>
        /// <param name="shape">Resolved shape represented by this artifact field.</param>
        /// <param name="byteOffset">Payload-relative byte offset.</param>
        /// <returns>A concrete artifact field entry without content hash.</returns>
        public static AtlasArtifactField Create(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset)
        {
            shape.ValidateOrThrow(nameof(shape));

            return new AtlasArtifactField(
                fieldIndex,
                shape.StableId,
                shape.Slot,
                shape.Role,
                shape.StorageFormat,
                shape.DeclaredShape,
                shape.DebugName,
                shape.Length,
                shape.Capacity,
                shape.ByteLength,
                shape.ByteCapacity,
                byteOffset,
                contentHash: 0UL,
                hasContentHash: false);
        }

        /// <summary>
        /// Creates an artifact field entry from a resolved shape and content hash.
        /// </summary>
        /// <param name="shape">Resolved shape represented by this artifact field.</param>
        /// <param name="byteOffset">Payload-relative byte offset.</param>
        /// <param name="contentHash">Deterministic field-content hash. Zero is valid.</param>
        /// <returns>A concrete artifact field entry with content hash.</returns>
        public static AtlasArtifactField Create(
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return Create(
                fieldIndex: shape.Slot.Index,
                shape,
                byteOffset,
                contentHash);
        }

        /// <summary>
        /// Creates an artifact field entry from a resolved shape, explicit field-table index, and content hash.
        /// </summary>
        /// <param name="fieldIndex">Artifact field-table index.</param>
        /// <param name="shape">Resolved shape represented by this artifact field.</param>
        /// <param name="byteOffset">Payload-relative byte offset.</param>
        /// <param name="contentHash">Deterministic field-content hash. Zero is valid.</param>
        /// <returns>A concrete artifact field entry with content hash.</returns>
        public static AtlasArtifactField Create(
            int fieldIndex,
            AtlasResolvedShape shape,
            long byteOffset,
            ulong contentHash)
        {
            shape.ValidateOrThrow(nameof(shape));

            return new AtlasArtifactField(
                fieldIndex,
                shape.StableId,
                shape.Slot,
                shape.Role,
                shape.StorageFormat,
                shape.DeclaredShape,
                shape.DebugName,
                shape.Length,
                shape.Capacity,
                shape.ByteLength,
                shape.ByteCapacity,
                byteOffset,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Creates an artifact field entry from a Contract row and matching resolved shape.
        /// </summary>
        /// <param name="contract">Source Contract row.</param>
        /// <param name="shape">Resolved shape matching <paramref name="contract"/>.</param>
        /// <param name="byteOffset">Payload-relative byte offset.</param>
        /// <returns>A concrete artifact field entry without content hash.</returns>
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
        /// Creates an artifact field entry from a Contract row, matching resolved shape, and content hash.
        /// </summary>
        /// <param name="contract">Source Contract row.</param>
        /// <param name="shape">Resolved shape matching <paramref name="contract"/>.</param>
        /// <param name="byteOffset">Payload-relative byte offset.</param>
        /// <param name="contentHash">Deterministic field-content hash. Zero is valid.</param>
        /// <returns>A concrete artifact field entry with content hash.</returns>
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
        /// Validates that this artifact field entry is concrete and internally consistent.
        /// </summary>
        /// <param name="parameterName">Parameter name used by thrown exceptions.</param>
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
                DeclaredShape,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity,
                ByteOffset,
                name);
        }

        /// <summary>
        /// Returns a copy of this artifact field with a content hash.
        /// </summary>
        /// <param name="contentHash">Deterministic field-content hash. Zero is valid.</param>
        /// <returns>A concrete artifact field entry with the supplied content hash.</returns>
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
                DeclaredShape,
                DebugName,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity,
                ByteOffset,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Determines whether this artifact field equals another artifact field.
        /// </summary>
        /// <param name="other">Artifact field to compare.</param>
        /// <returns><c>true</c> when both entries contain identical metadata.</returns>
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
                   DeclaredShape == other.DeclaredShape &&
                   DebugName.Equals(other.DebugName) &&
                   Length == other.Length &&
                   Capacity == other.Capacity &&
                   ByteLength == other.ByteLength &&
                   ByteCapacity == other.ByteCapacity &&
                   ByteOffset == other.ByteOffset &&
                   ContentHash == other.ContentHash;
        }

        /// <summary>
        /// Determines whether this artifact field equals another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal artifact field.</returns>
        public override bool Equals(
            object obj)
        {
            return obj is AtlasArtifactField other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        /// <returns>A deterministic hash code.</returns>
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
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ DeclaredShape.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ DebugName.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Length;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Capacity;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ByteLength.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ByteCapacity.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ByteOffset.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ContentHash.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this artifact field.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
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
                "AtlasArtifactField(Index={0}, Name={1}, StableId={2}, Slot={3}, Role={4}, Storage={5}, Length={6}, Capacity={7}, Offset={8}, Bytes={9}/{10}, ContentHash={11})",
                FieldIndex,
                DebugName,
                StableId,
                Slot,
                Role,
                StorageFormat.Kind,
                Length,
                Capacity,
                ByteOffset,
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
                contract.LengthShape != shape.DeclaredShape ||
                !contract.DebugName.Equals(shape.DebugName))
            {
                throw new ArgumentException(
                    $"Atlas artifact field contract '{contract.GetDiagnosticName()}' does not match resolved shape '{shape.GetDiagnosticName()}'.",
                    nameof(shape));
            }
        }

        private static void ValidateInputsOrThrow(
            int fieldIndex,
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            LengthShape declaredShape,
            int length,
            int capacity,
            long byteLength,
            long byteCapacity,
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
            storageFormat.ValidateOrThrow(name);
            declaredShape.ValidateOrThrow(name);

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
                    $"Artifact field byte length '{byteLength}' does not match expected byte length '{expectedByteLength}'.",
                    name);
            }

            if (byteCapacity != expectedByteCapacity)
            {
                throw new ArgumentException(
                    $"Artifact field byte capacity '{byteCapacity}' does not match expected byte capacity '{expectedByteCapacity}'.",
                    name);
            }

            _ = role;
            _ = checked(byteOffset + byteCapacity);
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