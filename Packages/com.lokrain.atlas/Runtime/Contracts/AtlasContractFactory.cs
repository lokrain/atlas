// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasContractFactory.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Provide the canonical factory surface for Atlas field contracts.
// - Create unslotted contracts from typed field declarations.
// - Create slotted contracts from typed field declarations or explicit schema metadata.
// - Require semantic shape-domain identity at explicit contract construction boundaries.
// - Validate contract metadata at construction boundaries.
// - Preserve zero-valid identifier and slot semantics.
//
// Design notes
// - StableDataId default/zero is valid.
// - AtlasFieldSlot default/zero is valid and represents slot zero.
// - Unslotted/slotted state is represented by AtlasContract.HasAssignedSlot.
// - ShapeDomain is required ABI metadata.
// - LengthShape is required resolution metadata.
// - This factory does not use invalid sentinels.
// - Try-style or lookup APIs must represent missing state with a bool, not with default payloads.
// - This factory is setup/authoring/compiler code, not hot-loop job code.
// - Burst jobs should receive resolved native containers, typed slices, or compiled addresses.

using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Provides validated factory methods for creating Atlas field contracts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Field contracts are schema metadata. They describe stable identity, semantic role, storage
    /// format, ownership, lifetime, shape domain, length shape, flags, hash participation, and
    /// diagnostic name for a field. They do not own memory and they are not job execution bindings.
    /// </para>
    ///
    /// <para>
    /// Contracts are first created as unslotted declarations. A contract table then assigns canonical
    /// zero-based slots. Slot zero is valid, so slot assignment must be represented by an explicit
    /// presence flag on <see cref="AtlasContract"/>, not by reserving a slot value as invalid.
    /// </para>
    ///
    /// <para>
    /// Explicit contract creation requires <see cref="AtlasShapeDomain"/> because numeric length is
    /// not enough shape identity. A field with length <c>N</c> may represent cells, vertices, chunks,
    /// graph nodes, graph edges, component rows, records, or external rows.
    /// </para>
    ///
    /// <para>
    /// This factory validates semantic metadata at creation boundaries. It does not validate
    /// operation ordering, read/write hazards, workspace layout, job scheduling, or artifact output.
    /// Those checks belong to the compiler, workspace planner, executor, and artifact pipeline.
    /// </para>
    /// </remarks>
    public static class AtlasContractFactory
    {
        /// <summary>
        /// Creates an unslotted contract from a typed Atlas field declaration.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <returns>
        /// A validated unslotted contract built from <typeparamref name="TField"/> metadata.
        /// </returns>
        /// <remarks>
        /// The returned contract has no assigned slot. This is the correct state before the contract
        /// is inserted into an <see cref="AtlasContractTable"/>. Shape-domain identity is read from
        /// the field declaration.
        /// </remarks>
        public static AtlasContract Of<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>();
        }

        /// <summary>
        /// Creates a slotted contract from a typed Atlas field declaration.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <param name="slot">The canonical zero-based table slot to assign. Slot zero is valid.</param>
        /// <returns>
        /// A validated slotted contract built from <typeparamref name="TField"/> metadata.
        /// </returns>
        /// <remarks>
        /// Prefer building unslotted contracts and letting <see cref="AtlasContractTable"/> assign
        /// slots. Use this overload only when constructing already-slotted metadata.
        /// </remarks>
        public static AtlasContract Of<TField, TElement>(AtlasFieldSlot slot)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>().WithSlot(slot);
        }

        /// <summary>
        /// Creates a slotted contract from a typed Atlas field declaration and zero-based slot index.
        /// </summary>
        /// <typeparam name="TField">The field declaration type.</typeparam>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <param name="slotIndex">The canonical zero-based table slot index. Slot index zero is valid.</param>
        /// <returns>
        /// A validated slotted contract built from <typeparamref name="TField"/> metadata.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slotIndex"/> is outside the supported field-slot range.
        /// </exception>
        /// <remarks>
        /// Prefer building unslotted contracts and letting <see cref="AtlasContractTable"/> assign
        /// slots. Use this overload only when constructing already-slotted metadata.
        /// </remarks>
        public static AtlasContract Of<TField, TElement>(int slotIndex)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>().WithSlot(slotIndex);
        }

        /// <summary>
        /// Creates an unslotted contract from explicit schema metadata.
        /// </summary>
        /// <param name="stableId">The durable field identity. Zero/default is valid.</param>
        /// <param name="role">The semantic field role.</param>
        /// <param name="storageFormat">The physical unmanaged storage format.</param>
        /// <param name="ownership">The allocation and disposal ownership policy.</param>
        /// <param name="lifetime">The storage lifetime policy.</param>
        /// <param name="shapeDomain">The semantic domain used to interpret resolved shape.</param>
        /// <param name="lengthShape">The rule used to resolve field length or capacity.</param>
        /// <param name="flags">The field behavior flags.</param>
        /// <param name="hashParticipation">The hash participation policy.</param>
        /// <param name="debugName">The diagnostic name used by validation, tooling, exceptions, and tests.</param>
        /// <returns>A validated unslotted contract.</returns>
        /// <remarks>
        /// This overload is useful when explicit schema metadata is already available and no typed
        /// declaration struct is required. The returned contract has no assigned slot.
        /// </remarks>
        public static AtlasContract Create(
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
            var contract = AtlasContract.Unslotted(
                stableId,
                role,
                storageFormat,
                ownership,
                lifetime,
                shapeDomain,
                lengthShape,
                flags,
                hashParticipation,
                debugName);

            contract.ValidateOrThrow(nameof(contract));
            return contract;
        }

        /// <summary>
        /// Creates a slotted contract from explicit schema metadata.
        /// </summary>
        /// <param name="stableId">The durable field identity. Zero/default is valid.</param>
        /// <param name="slot">The canonical zero-based table slot. Slot zero/default is valid.</param>
        /// <param name="role">The semantic field role.</param>
        /// <param name="storageFormat">The physical unmanaged storage format.</param>
        /// <param name="ownership">The allocation and disposal ownership policy.</param>
        /// <param name="lifetime">The storage lifetime policy.</param>
        /// <param name="shapeDomain">The semantic domain used to interpret resolved shape.</param>
        /// <param name="lengthShape">The rule used to resolve field length or capacity.</param>
        /// <param name="flags">The field behavior flags.</param>
        /// <param name="hashParticipation">The hash participation policy.</param>
        /// <param name="debugName">The diagnostic name used by validation, tooling, exceptions, and tests.</param>
        /// <returns>A validated slotted contract.</returns>
        /// <remarks>
        /// Slot zero is valid. The returned contract explicitly records that the slot is assigned.
        /// </remarks>
        public static AtlasContract Create(
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
        {
            var contract = new AtlasContract(
                stableId,
                slot,
                role,
                storageFormat,
                ownership,
                lifetime,
                shapeDomain,
                lengthShape,
                flags,
                hashParticipation,
                debugName);

            contract.ValidateTableReadyOrThrow(nameof(contract));
            return contract;
        }

        /// <summary>
        /// Creates an unslotted contract from explicit schema metadata and a typed element layout.
        /// </summary>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <param name="stableId">The durable field identity. Zero/default is valid.</param>
        /// <param name="role">The semantic field role.</param>
        /// <param name="storageKind">The physical native storage family.</param>
        /// <param name="ownership">The allocation and disposal ownership policy.</param>
        /// <param name="lifetime">The storage lifetime policy.</param>
        /// <param name="shapeDomain">The semantic domain used to interpret resolved shape.</param>
        /// <param name="lengthShape">The rule used to resolve field length or capacity.</param>
        /// <param name="flags">The field behavior flags.</param>
        /// <param name="hashParticipation">The hash participation policy.</param>
        /// <param name="debugName">The diagnostic name used by validation, tooling, exceptions, and tests.</param>
        /// <returns>A validated unslotted contract.</returns>
        /// <remarks>
        /// The storage format is derived from <typeparamref name="TElement"/> and
        /// <paramref name="storageKind"/>. The returned contract has no assigned slot.
        /// </remarks>
        public static AtlasContract Create<TElement>(
            StableDataId stableId,
            AtlasFieldRole role,
            StorageKind storageKind,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            AtlasShapeDomain shapeDomain,
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
                shapeDomain,
                lengthShape,
                flags,
                hashParticipation,
                debugName);
        }

        /// <summary>
        /// Creates a slotted contract from explicit schema metadata and a typed element layout.
        /// </summary>
        /// <typeparam name="TElement">The unmanaged element type stored by the field.</typeparam>
        /// <param name="stableId">The durable field identity. Zero/default is valid.</param>
        /// <param name="slot">The canonical zero-based table slot. Slot zero/default is valid.</param>
        /// <param name="role">The semantic field role.</param>
        /// <param name="storageKind">The physical native storage family.</param>
        /// <param name="ownership">The allocation and disposal ownership policy.</param>
        /// <param name="lifetime">The storage lifetime policy.</param>
        /// <param name="shapeDomain">The semantic domain used to interpret resolved shape.</param>
        /// <param name="lengthShape">The rule used to resolve field length or capacity.</param>
        /// <param name="flags">The field behavior flags.</param>
        /// <param name="hashParticipation">The hash participation policy.</param>
        /// <param name="debugName">The diagnostic name used by validation, tooling, exceptions, and tests.</param>
        /// <returns>A validated slotted contract.</returns>
        /// <remarks>
        /// The storage format is derived from <typeparamref name="TElement"/> and
        /// <paramref name="storageKind"/>. Slot zero is valid. The returned contract explicitly
        /// records that the slot is assigned.
        /// </remarks>
        public static AtlasContract Create<TElement>(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageKind storageKind,
            OwnershipPolicy ownership,
            LifetimePolicy lifetime,
            AtlasShapeDomain shapeDomain,
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
                shapeDomain,
                lengthShape,
                flags,
                hashParticipation,
                debugName);
        }
    }
}