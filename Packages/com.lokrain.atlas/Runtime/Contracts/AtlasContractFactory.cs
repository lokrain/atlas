// Runtime/Contracts/AtlasContractFactory.cs

using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Provides validated factory methods for creating Atlas Field Contracts.
    /// </summary>
    public static class AtlasContractFactory
    {
        /// <summary>
        /// Creates an unslotted Contract from a typed Atlas Field declaration.
        /// </summary>
        public static AtlasContract Of<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>();
        }

        /// <summary>
        /// Creates a slotted Contract from a typed Atlas Field declaration.
        /// </summary>
        public static AtlasContract Of<TField, TElement>(AtlasFieldSlot slot)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>().WithSlot(slot);
        }

        /// <summary>
        /// Creates a slotted Contract from a typed Atlas Field declaration and zero-based slot index.
        /// </summary>
        public static AtlasContract Of<TField, TElement>(int slotIndex)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>().WithSlot(slotIndex);
        }

        /// <summary>
        /// Creates an unslotted Contract from explicit schema fields.
        /// </summary>
        public static AtlasContract Create(
            StableDataId stableId,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            LengthShape lengthShape,
            AtlasFieldFlags flags,
            HashParticipation hashParticipation,
            FixedString64Bytes debugName)
        {
            var contract = new AtlasContract(
                stableId,
                AtlasFieldSlot.Invalid,
                role,
                storageFormat,
                ownership,
                lifetime,
                lengthShape,
                flags,
                hashParticipation,
                debugName);

            contract.ValidateOrThrow(nameof(contract));
            return contract;
        }

        /// <summary>
        /// Creates a slotted Contract from explicit schema fields.
        /// </summary>
        public static AtlasContract Create(
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
            slot.ThrowIfInvalid();

            var contract = new AtlasContract(
                stableId,
                slot,
                role,
                storageFormat,
                ownership,
                lifetime,
                lengthShape,
                flags,
                hashParticipation,
                debugName);

            contract.ValidateTableReadyOrThrow(nameof(contract));
            return contract;
        }

        /// <summary>
        /// Creates an unslotted Contract from explicit schema fields and a typed element layout.
        /// </summary>
        public static AtlasContract Create<TElement>(
            StableDataId stableId,
            AtlasFieldRole role,
            StorageKind storageKind,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            LengthShape lengthShape,
            AtlasFieldFlags flags,
            HashParticipation hashParticipation,
            FixedString64Bytes debugName)
            where TElement : unmanaged
        {
            return Create(
                stableId,
                role,
                StorageFormat.Create<TElement>(storageKind),
                ownership,
                lifetime,
                lengthShape,
                flags,
                hashParticipation,
                debugName);
        }

        /// <summary>
        /// Creates a slotted Contract from explicit schema fields and a typed element layout.
        /// </summary>
        public static AtlasContract Create<TElement>(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageKind storageKind,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            LengthShape lengthShape,
            AtlasFieldFlags flags,
            HashParticipation hashParticipation,
            FixedString64Bytes debugName)
            where TElement : unmanaged
        {
            return Create(
                stableId,
                slot,
                role,
                StorageFormat.Create<TElement>(storageKind),
                ownership,
                lifetime,
                lengthShape,
                flags,
                hashParticipation,
                debugName);
        }
    }
}