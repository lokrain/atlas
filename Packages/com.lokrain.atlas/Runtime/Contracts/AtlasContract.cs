// Runtime/Contracts/AtlasContract.cs

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Describes one Field contract row in an Atlas Contract table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Contract is schema metadata. It defines stable identity, semantic role, storage format,
    /// ownership, lifetime, length shape, Field flags, hash participation, and diagnostic name
    /// for one Atlas Field.
    /// </para>
    ///
    /// <para>
    /// Contracts do not own memory. Runtime storage is allocated from a validated Contract table,
    /// then jobs receive already-resolved native containers.
    /// </para>
    ///
    /// <para>
    /// The Contract slot is table-local. An unslotted Contract may be created from a Field
    /// declaration and later assigned a canonical slot by <see cref="AtlasContractTable"/>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasContract :
        IEquatable<AtlasContract>
    {
        /// <summary>
        /// Reserved invalid Contract.
        /// </summary>
        public static readonly AtlasContract Empty = default;

        /// <summary>
        /// Durable, versioned Field identity.
        /// </summary>
        public readonly StableDataId StableId;

        /// <summary>
        /// Canonical table-local Field slot.
        /// </summary>
        /// <remarks>
        /// The slot may be invalid before a Contract is placed into a Contract table.
        /// </remarks>
        public readonly AtlasFieldSlot Slot;

        /// <summary>
        /// Semantic role of the Field in the generated-world contract.
        /// </summary>
        public readonly AtlasFieldRole Role;

        /// <summary>
        /// Physical unmanaged storage format required by the Field.
        /// </summary>
        public readonly StorageFormat StorageFormat;

        /// <summary>
        /// Allocation and disposal ownership policy for the Field storage.
        /// </summary>
        public readonly OwnershipPolicy Ownership;

        /// <summary>
        /// Validity interval policy for the Field storage.
        /// </summary>
        public readonly LifetimePolicy Lifetime;

        /// <summary>
        /// Rule used to resolve Field length or capacity before scheduling.
        /// </summary>
        public readonly LengthShape LengthShape;

        /// <summary>
        /// Durable behavior flags for the Field.
        /// </summary>
        public readonly AtlasFieldFlags Flags;

        /// <summary>
        /// Hash participation policy for Contract, shape, compatibility, and content hashes.
        /// </summary>
        public readonly HashParticipation HashParticipation;

        /// <summary>
        /// Stable diagnostic name used by validation, tooling, exceptions, and tests.
        /// </summary>
        /// <remarks>
        /// Debug names are not durable identity. Use <see cref="StableId"/> for identity.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Creates an Atlas Contract from explicit schema fields.
        /// </summary>
        /// <param name="stableId">Durable, versioned Field identity.</param>
        /// <param name="slot">Table-local Field slot. May be invalid before table assignment.</param>
        /// <param name="role">Semantic role of the Field.</param>
        /// <param name="storageFormat">Physical unmanaged storage format.</param>
        /// <param name="ownership">Allocation and disposal ownership policy.</param>
        /// <param name="lifetime">Storage validity interval policy.</param>
        /// <param name="lengthShape">Rule used to resolve length or capacity.</param>
        /// <param name="flags">Durable Field behavior flags.</param>
        /// <param name="hashParticipation">Hash participation policy.</param>
        /// <param name="debugName">Stable diagnostic name.</param>
        public AtlasContract(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
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
            LengthShape = lengthShape;
            Flags = flags;
            HashParticipation = hashParticipation;
            DebugName = debugName;
        }

        /// <summary>
        /// Gets whether this Contract has valid Field identity, semantic role, and storage contract.
        /// </summary>
        /// <remarks>
        /// This property does not require a valid slot. Unslotted Contracts are valid before
        /// Contract-table construction assigns canonical slots.
        /// </remarks>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StableId.IsValid &&
                   Role != AtlasFieldRole.None &&
                   StorageFormat.IsValid &&
                   Ownership != OwnershipPolicy.None &&
                   Lifetime != LifetimePolicy.None &&
                   LengthShape.IsValid &&
                   !DebugName.IsEmpty;
        }

        /// <summary>
        /// Gets whether this Contract has been assigned a canonical table slot.
        /// </summary>
        public bool IsSlotted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slot.IsValid;
        }

        /// <summary>
        /// Gets whether this Contract is valid and has a canonical table slot.
        /// </summary>
        public bool IsTableReady
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsValid && IsSlotted;
        }

        /// <summary>
        /// Creates an unslotted Contract from a typed Atlas Field declaration.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>
        /// An unslotted Contract whose metadata is read from <c>default(TField)</c>.
        /// </returns>
        /// <exception cref="AtlasException">
        /// Thrown when the Field declaration is invalid.
        /// </exception>
        public static AtlasContract Of<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            AtlasField.ValidateDeclarationOrThrow<TField, TElement>();

            var field = default(TField);

            return new AtlasContract(
                stableId: field.StableId,
                slot: AtlasFieldSlot.Invalid,
                role: field.Role,
                storageFormat: StorageFormat.Create<TElement>(field.StorageKind),
                ownership: field.Ownership,
                lifetime: field.Lifetime,
                lengthShape: field.LengthShape,
                flags: field.Flags,
                hashParticipation: field.HashParticipation,
                debugName: field.DebugName);
        }

        /// <summary>
        /// Creates a Contract copy with a canonical table slot.
        /// </summary>
        /// <param name="slot">Canonical table-local Field slot.</param>
        /// <returns>A copy of this Contract with the supplied slot.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="slot"/> is invalid.
        /// </exception>
        public AtlasContract WithSlot(AtlasFieldSlot slot)
        {
            slot.ThrowIfInvalid();

            return new AtlasContract(
                StableId,
                slot,
                Role,
                StorageFormat,
                Ownership,
                Lifetime,
                LengthShape,
                Flags,
                HashParticipation,
                DebugName);
        }

        /// <summary>
        /// Creates a Contract copy with a canonical table slot from a zero-based index.
        /// </summary>
        /// <param name="slotIndex">Zero-based Contract-table slot.</param>
        /// <returns>A copy of this Contract with the supplied slot.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slotIndex"/> is outside the valid Atlas slot range.
        /// </exception>
        public AtlasContract WithSlot(int slotIndex)
        {
            return WithSlot(AtlasFieldSlot.FromIndex(slotIndex));
        }

        /// <summary>
        /// Throws when this Contract is invalid for Contract-table construction.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the Contract is missing required schema metadata or declares an invalid policy combination.
        /// </exception>
        /// <remarks>
        /// This validation does not require a slot. Contract tables assign canonical slots after
        /// receiving ordered Contracts.
        /// </remarks>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasContract);

            if (!StableId.IsValid)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' has an invalid stable id.",
                    name);
            }

            if (Role == AtlasFieldRole.None)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' has no Field role.",
                    name);
            }

            StorageFormat.ValidateOrThrow(name);
            LengthShape.ValidateOrThrow(name);

            if (Ownership == OwnershipPolicy.None)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' has no ownership policy.",
                    name);
            }

            if (Lifetime == LifetimePolicy.None)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' has no lifetime policy.",
                    name);
            }

            if (DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas Contract has an empty debug name.",
                    name);
            }

            ValidatePolicyCombinationOrThrow(name);
        }

        /// <summary>
        /// Throws when this Contract is invalid for runtime table usage.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the Contract is invalid or has no canonical slot.
        /// </exception>
        public void ValidateTableReadyOrThrow(string parameterName = null)
        {
            ValidateOrThrow(parameterName);

            if (Slot.IsValid)
            {
                return;
            }

            throw new ArgumentException(
                $"Atlas Contract '{DebugName}' has no assigned table slot.",
                parameterName ?? nameof(AtlasContract));
        }

        /// <summary>
        /// Determines whether this Contract is equal to another Contract.
        /// </summary>
        /// <param name="other">The Contract to compare with this Contract.</param>
        /// <returns><c>true</c> when all Contract fields match; otherwise, <c>false</c>.</returns>
        public bool Equals(AtlasContract other)
        {
            return StableId == other.StableId &&
                   Slot == other.Slot &&
                   Role == other.Role &&
                   StorageFormat == other.StorageFormat &&
                   Ownership == other.Ownership &&
                   Lifetime == other.Lifetime &&
                   LengthShape == other.LengthShape &&
                   Flags == other.Flags &&
                   HashParticipation == other.HashParticipation &&
                   DebugName.Equals(other.DebugName);
        }

        /// <summary>
        /// Determines whether this Contract is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this Contract.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasContract"/> with matching fields.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasContract other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code for this Contract.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StableId.GetHashCode();
                hash = (hash * 397) ^ Slot.GetHashCode();
                hash = (hash * 397) ^ (int)Role;
                hash = (hash * 397) ^ StorageFormat.GetHashCode();
                hash = (hash * 397) ^ (int)Ownership;
                hash = (hash * 397) ^ (int)Lifetime;
                hash = (hash * 397) ^ LengthShape.GetHashCode();
                hash = (hash * 397) ^ (int)Flags;
                hash = (hash * 397) ^ (int)HashParticipation;
                hash = (hash * 397) ^ DebugName.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this Contract.
        /// </summary>
        /// <returns>A string containing debug name, role, stable identity, slot, and storage format.</returns>
        public override string ToString()
        {
            return $"{DebugName} [{StableId}] Role={Role} Slot={Slot} Storage={StorageFormat}";
        }

        /// <summary>
        /// Determines whether two Contracts are equal.
        /// </summary>
        public static bool operator ==(AtlasContract left, AtlasContract right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two Contracts are not equal.
        /// </summary>
        public static bool operator !=(AtlasContract left, AtlasContract right)
        {
            return !left.Equals(right);
        }

        private void ValidatePolicyCombinationOrThrow(string parameterName)
        {
            if (Flags.HasAll(AtlasFieldFlags.ClearOnAcquire | AtlasFieldFlags.PreserveContent))
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' declares both " +
                    $"{nameof(AtlasFieldFlags.ClearOnAcquire)} and {nameof(AtlasFieldFlags.PreserveContent)}.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasFieldFlags.AllowsUninitializedMemory) &&
                Flags.HasNone(AtlasFieldFlags.DiscardBeforeWrite))
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' declares " +
                    $"{nameof(AtlasFieldFlags.AllowsUninitializedMemory)} without " +
                    $"{nameof(AtlasFieldFlags.DiscardBeforeWrite)}.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasFieldFlags.Resizable) &&
                !StorageFormat.SupportsResize)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' is marked resizable, but storage kind " +
                    $"'{StorageFormat.Kind}' does not support resizing.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasFieldFlags.AllowsExternalAlias) &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' allows external aliasing with incompatible ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (StorageFormat.Kind == StorageKind.External &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' uses external storage with incompatible ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (Lifetime == LifetimePolicy.External &&
                Ownership != OwnershipPolicy.ExternalOwned &&
                Ownership != OwnershipPolicy.Borrowed &&
                Ownership != OwnershipPolicy.Imported)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' uses external lifetime with incompatible ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (Role == AtlasFieldRole.External &&
                !IsExternalCompatibleOwnership(Ownership))
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' declares external Field role with incompatible ownership policy '{Ownership}'.",
                    parameterName);
            }

            if (Role == AtlasFieldRole.Canonical &&
                HashParticipation == HashParticipation.None)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' declares canonical Field role but opts out of all hash participation.",
                    parameterName);
            }

            if (Role == AtlasFieldRole.Payload &&
                HashParticipation == HashParticipation.None)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' declares payload Field role but opts out of all hash participation.",
                    parameterName);
            }

            if (StorageFormat.Kind == StorageKind.Scalar &&
                !LengthShape.IsScalar)
            {
                throw new ArgumentException(
                    $"Atlas Contract '{DebugName}' uses scalar storage but does not declare scalar length shape.",
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
    }
}