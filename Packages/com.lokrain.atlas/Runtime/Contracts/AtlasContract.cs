// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasContract.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Represent one immutable field-contract row in an Atlas contract table.
// - Preserve zero-valid StableDataId and AtlasFieldSlot semantics.
// - Represent assigned/unassigned slot state explicitly instead of reserving an invalid slot.
// - Preserve semantic shape-domain identity as part of the field ABI.
// - Validate semantic field-contract metadata at construction/table boundaries.
// - Keep contract metadata allocation-free, deterministic, and suitable for compiled runtime metadata.
//
// Design notes
// - default(AtlasContract) is a valid value object, but it is not a valid concrete field contract.
// - StableDataId default/zero is valid.
// - AtlasFieldSlot default/zero is valid and represents slot zero.
// - Slot assignment is represented by HasAssignedSlot.
// - Do not infer slot assignment from Slot == default.
// - ShapeDomain describes what resolved length/capacity means.
// - LengthShape describes how resolved length/capacity is produced.
// - Empty/default is an inert payload for bool-returning lookup APIs, not an invalid sentinel.
// - Missing lookup results must be represented by a bool-returning API, not by returning default.
// - This type describes schema metadata only. It does not own storage.
// - Jobs should not consume AtlasContract directly in hot loops. Jobs should receive compiled
//   addresses, typed slices/views, or resolved native containers.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Describes one immutable field-contract row in an Atlas contract table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A contract is schema metadata. It defines the stable identity, table-local slot, semantic role,
    /// storage format, ownership policy, lifetime policy, shape-domain identity, length shape,
    /// field flags, hash participation, and diagnostic name for one Atlas field.
    /// </para>
    ///
    /// <para>
    /// A contract does not own memory. Runtime storage is allocated later from validated contract
    /// tables, compiled plans, and workspace layout decisions. Jobs must receive already-resolved
    /// native containers, typed slices/views, or compiled memory addresses instead of resolving
    /// contracts by field identity.
    /// </para>
    ///
    /// <para>
    /// The slot is table-local execution metadata. It is not durable field identity. Slot zero is a
    /// valid slot, so assigned/unassigned slot state is represented explicitly by
    /// <see cref="HasAssignedSlot"/>.
    /// </para>
    ///
    /// <para>
    /// Shape domain is part of the ABI because numeric length is not self-describing. Two fields may
    /// have the same resolved length and storage format while representing incompatible domains,
    /// such as cells, vertices, graph nodes, graph edges, component rows, or external records.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasContract :
        IEquatable<AtlasContract>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        private const byte SlotUnassigned = 0;
        private const byte SlotAssigned = 1;


        private readonly byte _slotPresence;

        /// <summary>
        /// Gets the durable, versioned field identity.
        /// </summary>
        /// <remarks>
        /// Zero/default is valid and must not be interpreted as missing or invalid.
        /// </remarks>
        public readonly StableDataId StableId;

        /// <summary>
        /// Gets the table-local field slot.
        /// </summary>
        /// <remarks>
        /// Slot zero/default is valid. Use <see cref="HasAssignedSlot"/> to determine whether this
        /// slot is assigned.
        /// </remarks>
        public readonly AtlasFieldSlot Slot;

        /// <summary>
        /// Gets the semantic role of the field in the generated-world contract.
        /// </summary>
        public readonly AtlasFieldRole Role;

        /// <summary>
        /// Gets the physical unmanaged storage format required by the field.
        /// </summary>
        public readonly StorageFormat StorageFormat;

        /// <summary>
        /// Gets the allocation and disposal ownership policy for the field storage.
        /// </summary>
        public readonly OwnershipPolicy Ownership;

        /// <summary>
        /// Gets the validity interval policy for the field storage.
        /// </summary>
        public readonly LifetimePolicy Lifetime;

        /// <summary>
        /// Gets the semantic domain used to interpret resolved length and capacity.
        /// </summary>
        /// <remarks>
        /// Shape domain answers what the elements mean. It intentionally does not describe byte
        /// format, ownership, allocator, or resolution rule.
        /// </remarks>
        public readonly AtlasShapeDomain ShapeDomain;

        /// <summary>
        /// Gets the rule used to resolve field length or capacity before scheduling.
        /// </summary>
        public readonly LengthShape LengthShape;

        /// <summary>
        /// Gets durable field behavior flags.
        /// </summary>
        public readonly AtlasFieldFlags Flags;

        /// <summary>
        /// Gets the hash participation policy for contract, shape, compatibility, and content hashes.
        /// </summary>
        public readonly HashParticipation HashParticipation;

        /// <summary>
        /// Gets the stable diagnostic name used by validation, tooling, exceptions, and tests.
        /// </summary>
        /// <remarks>
        /// Debug names are not durable identity. Use <see cref="StableId"/> for durable identity.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Initializes a new slotted contract from explicit schema metadata.
        /// </summary>
        public AtlasContract(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            AtlasShapeDomain shapeDomain,
            LengthShape lengthShape,
            AtlasFieldFlags flags,
            HashParticipation hashParticipation,
            FixedString64Bytes debugName)
            : this(
                stableId,
                slot,
                hasAssignedSlot: true,
                role,
                storageFormat,
                ownership,
                lifetime,
                shapeDomain,
                lengthShape,
                flags,
                hashParticipation,
                debugName)
        {
        }

        private AtlasContract(
            StableDataId stableId,
            AtlasFieldSlot slot,
            bool hasAssignedSlot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            AtlasShapeDomain shapeDomain,
            LengthShape lengthShape,
            AtlasFieldFlags flags,
            HashParticipation hashParticipation,
            FixedString64Bytes debugName)
        {
            StableId = stableId;
            Slot = slot;
            Role = role;
            StorageFormat = storageFormat;
            Ownership = ownership;
            Lifetime = lifetime;
            ShapeDomain = shapeDomain;
            LengthShape = lengthShape;
            Flags = flags;
            HashParticipation = hashParticipation;
            DebugName = debugName;
            _slotPresence = hasAssignedSlot ? SlotAssigned : SlotUnassigned;
        }

        /// <summary>
        /// Gets whether this value has meaningful concrete field-contract schema metadata.
        /// </summary>
        /// <remarks>
        /// This does not require an assigned table slot. Unslotted contracts are expected before
        /// contract-table construction. This property is semantic validation shorthand, not a
        /// bit-pattern validity check.
        /// </remarks>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Role != AtlasFieldRole.None &&
                   StorageFormat.IsValid &&
                   Ownership != OwnershipPolicy.None &&
                   Lifetime != LifetimePolicy.None &&
                   ShapeDomain.IsConcrete &&
                   LengthShape.IsValid &&
                   !DebugName.IsEmpty;
        }


        /// <summary>
        /// Gets whether this contract has an assigned canonical table-local field slot.
        /// </summary>
        public bool HasAssignedSlot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _slotPresence == SlotAssigned;
        }

        /// <summary>
        /// Gets whether this contract has no assigned canonical table-local field slot.
        /// </summary>
        public bool IsUnslotted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _slotPresence != SlotAssigned;
        }

        /// <summary>
        /// Legacy alias for <see cref="HasAssignedSlot"/>.
        /// </summary>
        public bool IsSlotted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HasAssignedSlot;
        }

        /// <summary>
        /// Gets whether this contract has meaningful schema metadata and an assigned table slot.
        /// </summary>
        public bool IsTableReady
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsValid && HasAssignedSlot;
        }

        /// <summary>
        /// Creates an unslotted contract from explicit schema metadata.
        /// </summary>
        /// <remarks>
        /// The returned contract has no assigned slot. This is the correct state before insertion
        /// into an <see cref="AtlasContractTable"/>.
        /// </remarks>
        public static AtlasContract Unslotted(
            StableDataId stableId,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            AtlasShapeDomain shapeDomain,
            LengthShape lengthShape,
            AtlasFieldFlags flags,
            HashParticipation hashParticipation,
            FixedString64Bytes debugName)
        {
            return new AtlasContract(
                stableId,
                default,
                hasAssignedSlot: false,
                role,
                storageFormat,
                ownership,
                lifetime,
                shapeDomain,
                lengthShape,
                flags,
                hashParticipation,
                debugName);
        }

        /// <summary>
        /// Creates an unslotted contract from a typed Atlas field declaration.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <returns>An unslotted contract whose metadata is read from <c>default(TField)</c>.</returns>
        /// <exception cref="AtlasException">Thrown when the field declaration is invalid.</exception>
        /// <remarks>
        /// Field declaration types are expected to expose metadata from their default value. This
        /// method validates the declaration and then creates an unslotted schema row. Contract-table
        /// construction assigns the final table-local slot.
        /// </remarks>
        public static AtlasContract Of<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            AtlasField.ValidateDeclarationOrThrow<TField, TElement>();

            var field = default(TField);

            return new AtlasContract(
                stableId: field.StableId,
                slot: default,
                hasAssignedSlot: false,
                role: field.Role,
                storageFormat: StorageFormat.Create<TElement>(field.StorageKind),
                ownership: field.Ownership,
                lifetime: field.Lifetime,
                shapeDomain: field.ShapeDomain,
                lengthShape: field.LengthShape,
                flags: field.Flags,
                hashParticipation: field.HashParticipation,
                debugName: field.DebugName);
        }

        /// <summary>
        /// Creates a copy of this contract with an assigned canonical table-local slot.
        /// </summary>
        /// <param name="slot">The assigned table-local field slot. Slot zero/default is valid.</param>
        /// <returns>A copy of this contract with <see cref="HasAssignedSlot"/> set to <c>true</c>.</returns>
        public AtlasContract WithSlot(AtlasFieldSlot slot)
        {
            return new AtlasContract(
                StableId,
                slot,
                hasAssignedSlot: true,
                Role,
                StorageFormat,
                Ownership,
                Lifetime,
                ShapeDomain,
                LengthShape,
                Flags,
                HashParticipation,
                DebugName);
        }

        /// <summary>
        /// Creates a copy of this contract with an assigned canonical table-local slot.
        /// </summary>
        /// <param name="slotIndex">The zero-based contract-table slot index. Slot index zero is valid.</param>
        /// <returns>A copy of this contract with the supplied assigned slot.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slotIndex"/> falls outside the supported field-slot range.
        /// </exception>
        public AtlasContract WithSlot(int slotIndex)
        {
            return WithSlot(AtlasFieldSlot.FromIndex(slotIndex));
        }

        /// <summary>
        /// Creates a copy of this contract without an assigned table-local slot.
        /// </summary>
        /// <returns>A copy of this contract with <see cref="HasAssignedSlot"/> set to <c>false</c>.</returns>
        public AtlasContract WithoutSlot()
        {
            return new AtlasContract(
                StableId,
                default,
                hasAssignedSlot: false,
                Role,
                StorageFormat,
                Ownership,
                Lifetime,
                ShapeDomain,
                LengthShape,
                Flags,
                HashParticipation,
                DebugName);
        }

        /// <summary>
        /// Creates a copy of this contract with a different semantic shape domain.
        /// </summary>
        /// <param name="shapeDomain">The replacement shape domain.</param>
        /// <returns>A copy of this contract with the supplied shape domain.</returns>
        public AtlasContract WithShapeDomain(AtlasShapeDomain shapeDomain)
        {
            var contract = new AtlasContract(
                StableId,
                Slot,
                HasAssignedSlot,
                Role,
                StorageFormat,
                Ownership,
                Lifetime,
                shapeDomain,
                LengthShape,
                Flags,
                HashParticipation,
                DebugName);

            contract.ValidateOrThrow(nameof(contract));
            return contract;
        }

        /// <summary>
        /// Returns the diagnostic name of this contract.
        /// </summary>
        /// <returns>The declared debug name when present; otherwise, an invariant fallback string.</returns>
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
        /// Validates semantic schema metadata for contract-table construction.
        /// </summary>
        /// <param name="parameterName">Optional caller parameter name used by thrown exceptions.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the contract is missing required schema metadata or declares an invalid policy combination.
        /// </exception>
        /// <remarks>
        /// This validation does not require an assigned slot. Unslotted contracts are valid inputs
        /// to contract-table construction.
        /// </remarks>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasContract);
            var diagnosticName = GetDiagnosticName();

            if (Role == AtlasFieldRole.None)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares no field role.",
                    name);
            }

            StorageFormat.ValidateOrThrow(name);
            ShapeDomain.ValidateOrThrow(name);
            LengthShape.ValidateOrThrow(name);

            if (Ownership == OwnershipPolicy.None)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares no ownership policy.",
                    name);
            }

            if (Lifetime == LifetimePolicy.None)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares no lifetime policy.",
                    name);
            }

            if (DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares an empty debug name.",
                    name);
            }

            ValidatePolicyCombinationOrThrow(name, diagnosticName);
            ValidateShapeDomainPolicyOrThrow(name, diagnosticName);
        }

        /// <summary>
        /// Validates semantic schema metadata and requires an assigned table slot.
        /// </summary>
        /// <param name="parameterName">Optional caller parameter name used by thrown exceptions.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the contract is semantically invalid or has no assigned table slot.
        /// </exception>
        public void ValidateTableReadyOrThrow(string parameterName = null)
        {
            ValidateOrThrow(parameterName);

            if (HasAssignedSlot)
            {
                return;
            }

            throw new ArgumentException(
                $"Atlas contract '{GetDiagnosticName()}' has no assigned table slot.",
                parameterName ?? nameof(AtlasContract));
        }

        /// <summary>
        /// Returns whether this contract has the same durable stable identity as another contract.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameStableIdAs(AtlasContract other)
        {
            return StableId == other.StableId;
        }

        /// <summary>
        /// Returns whether this contract has the same semantic shape domain as another contract.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameShapeDomainAs(AtlasContract other)
        {
            return ShapeDomain == other.ShapeDomain;
        }

        /// <summary>
        /// Determines whether this contract equals another contract.
        /// </summary>
        public bool Equals(AtlasContract other)
        {
            return StableId == other.StableId &&
                   Slot == other.Slot &&
                   _slotPresence == other._slotPresence &&
                   Role == other.Role &&
                   StorageFormat == other.StorageFormat &&
                   Ownership == other.Ownership &&
                   Lifetime == other.Lifetime &&
                   ShapeDomain == other.ShapeDomain &&
                   LengthShape == other.LengthShape &&
                   Flags == other.Flags &&
                   HashParticipation == other.HashParticipation &&
                   DebugName.Equals(other.DebugName);
        }

        /// <summary>
        /// Determines whether this contract equals another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasContract other && Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this contract.
        /// </summary>
        /// <returns>A deterministic 32-bit hash code.</returns>
        /// <remarks>
        /// This intentionally avoids <see cref="HashCode"/> so metadata hashing does not depend on
        /// runtime implementation details.
        /// </remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ StableId.GetHashCode();
                hash = (hash * HashMultiplier) ^ Slot.GetHashCode();
                hash = (hash * HashMultiplier) ^ _slotPresence;
                hash = (hash * HashMultiplier) ^ (int)Role;
                hash = (hash * HashMultiplier) ^ StorageFormat.GetHashCode();
                hash = (hash * HashMultiplier) ^ (int)Ownership;
                hash = (hash * HashMultiplier) ^ (int)Lifetime;
                hash = (hash * HashMultiplier) ^ ShapeDomain.GetHashCode();
                hash = (hash * HashMultiplier) ^ LengthShape.GetHashCode();
                hash = (hash * HashMultiplier) ^ (int)Flags;
                hash = (hash * HashMultiplier) ^ (int)HashParticipation;
                hash = (hash * HashMultiplier) ^ DebugName.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns an invariant diagnostic representation of this contract.
        /// </summary>
        /// <returns>A stable diagnostic string.</returns>
        public override string ToString()
        {
            var slotText = HasAssignedSlot
                ? Slot.ToString()
                : "Unassigned";

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} [{1}] Role={2} Slot={3} Domain={4} LengthShape={5} Storage={6} Ownership={7} Lifetime={8}",
                DebugName,
                StableId,
                Role,
                slotText,
                ShapeDomain,
                LengthShape,
                StorageFormat,
                Ownership,
                Lifetime);
        }

        private void ValidatePolicyCombinationOrThrow(
            string parameterName,
            string diagnosticName)
        {
            if (Flags.HasAll(AtlasFieldFlags.ClearOnAcquire | AtlasFieldFlags.PreserveContent))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares both " +
                    $"{nameof(AtlasFieldFlags.ClearOnAcquire)} and " +
                    $"{nameof(AtlasFieldFlags.PreserveContent)}.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasFieldFlags.AllowsUninitializedMemory) &&
                Flags.HasNone(AtlasFieldFlags.DiscardBeforeWrite))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares " +
                    $"{nameof(AtlasFieldFlags.AllowsUninitializedMemory)} without " +
                    $"{nameof(AtlasFieldFlags.DiscardBeforeWrite)}.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasFieldFlags.Resizable) &&
                !StorageFormat.SupportsResize)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' is marked resizable, but storage kind " +
                    $"'{StorageFormat.Kind}' does not support resizing.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasFieldFlags.AllowsExternalAlias) &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' allows external aliasing with incompatible " +
                    $"ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (StorageFormat.Kind == StorageKind.External &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' uses external storage with incompatible " +
                    $"ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (Lifetime == LifetimePolicy.External &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' uses external lifetime with incompatible " +
                    $"ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (Role == AtlasFieldRole.External &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares external field role with incompatible " +
                    $"ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (Role == AtlasFieldRole.Canonical &&
                HashParticipation == Lokrain.Atlas.Contracts.HashParticipation.None)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares canonical field role but opts out of all " +
                    "hash participation.",
                    parameterName);
            }

            if (Role == AtlasFieldRole.Payload &&
                HashParticipation == Lokrain.Atlas.Contracts.HashParticipation.None)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares payload field role but opts out of all " +
                    "hash participation.",
                    parameterName);
            }

            if (StorageFormat.Kind == StorageKind.Scalar &&
                !LengthShape.IsScalar)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' uses scalar storage but does not declare scalar " +
                    "length shape.",
                    parameterName);
            }
        }

        private void ValidateShapeDomainPolicyOrThrow(
            string parameterName,
            string diagnosticName)
        {
            if (ShapeDomain.Kind == AtlasShapeDomainKind.Scalar &&
                !LengthShape.IsScalar)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares scalar shape domain but length shape is '{LengthShape.Kind}'.",
                    parameterName);
            }

            if (LengthShape.IsScalar &&
                ShapeDomain.Kind != AtlasShapeDomainKind.Scalar &&
                ShapeDomain.Kind != AtlasShapeDomainKind.FixedVector)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares scalar length shape with incompatible shape domain '{ShapeDomain.Kind}'.",
                    parameterName);
            }

            if (ShapeDomain.Kind == AtlasShapeDomainKind.External &&
                StorageFormat.Kind != StorageKind.External)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares external shape domain with non-external storage kind '{StorageFormat.Kind}'.",
                    parameterName);
            }

            if (StorageFormat.Kind == StorageKind.External &&
                ShapeDomain.Kind != AtlasShapeDomainKind.External)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares external storage with non-external shape domain '{ShapeDomain.Kind}'.",
                    parameterName);
            }

            if (ShapeDomain.Kind == AtlasShapeDomainKind.External &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares external shape domain with incompatible ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (LengthShape.Kind == LengthShapeKind.External &&
                ShapeDomain.Kind != AtlasShapeDomainKind.External)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares external length shape with non-external shape domain '{ShapeDomain.Kind}'.",
                    parameterName);
            }

            if (ShapeDomain.Kind == AtlasShapeDomainKind.PrefixSumPayload &&
                LengthShape.Kind != LengthShapeKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares prefix-sum payload shape domain with length shape '{LengthShape.Kind}'.",
                    parameterName);
            }

            if (LengthShape.Kind == LengthShapeKind.PrefixSumPayload &&
                ShapeDomain.Kind != AtlasShapeDomainKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares prefix-sum payload length shape with shape domain '{ShapeDomain.Kind}'.",
                    parameterName);
            }

            if (ShapeDomain.HasSourceField &&
                ShapeDomain.SourceFieldId != LengthShape.SourceFieldId &&
                LengthShape.Kind != LengthShapeKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    $"Atlas contract '{diagnosticName}' declares shape-domain source field '{ShapeDomain.SourceFieldId}' " +
                    $"that does not match length-shape source field '{LengthShape.SourceFieldId}'.",
                    parameterName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsExternalCompatibleOwnership(OwnershipPolicy ownership)
        {
            return ownership == OwnershipPolicy.ExternalOwned ||
                   ownership == OwnershipPolicy.Borrowed ||
                   ownership == OwnershipPolicy.Imported ||
                   ownership == OwnershipPolicy.Adopted;
        }

        /// <summary>
        /// Determines whether two contracts are equal.
        /// </summary>
        public static bool operator ==(AtlasContract left, AtlasContract right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two contracts are not equal.
        /// </summary>
        public static bool operator !=(AtlasContract left, AtlasContract right)
        {
            return !left.Equals(right);
        }
    }
}