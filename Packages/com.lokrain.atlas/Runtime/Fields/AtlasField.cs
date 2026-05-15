// Packages/com.lokrain.atlas/Runtime/Fields/AtlasField.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields
// Purpose:
// - Centralize allocation-free metadata reads for typed Atlas field declarations.
// - Validate declaration-level invariants before contracts are built.
// - Keep jobs independent from field declaration APIs by requiring compiled/resolved storage.
//
// Design notes:
// - This is a setup/validation helper, not a hot-loop job API.
// - Burst jobs should receive resolved native containers, typed slices, or compiled addresses.
// - This file intentionally preserves the current Lokrain.Atlas namespace/package identity.

using System;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Unity.Collections;

using AtlasHashParticipation = Lokrain.Atlas.Contracts.HashParticipation;
using AtlasLengthShape = Lokrain.Atlas.Contracts.LengthShape;
using AtlasLifetimePolicy = Lokrain.Atlas.Contracts.LifetimePolicy;
using AtlasOwnershipPolicy = Lokrain.Atlas.Contracts.OwnershipPolicy;
using AtlasStorageKind = Lokrain.Atlas.Contracts.StorageKind;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Provides allocation-free helpers for reading typed Atlas Field declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Atlas Field declarations are expected to be empty immutable value types. Their metadata
    /// must be readable from <c>default(TField)</c>. This type centralizes that convention so
    /// Contract factories, validators, storage allocators, plan compilers, and resolvers use the
    /// same declaration path.
    /// </para>
    ///
    /// <para>
    /// These helpers are intended for setup, validation, Contract construction, and storage
    /// resolution. Burst jobs should receive resolved native containers and should not call Field
    /// declaration APIs.
    /// </para>
    /// </remarks>
    public static class AtlasField
    {
        /// <summary>
        /// Returns the default value of a Field declaration type.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>The default Field declaration value used as the source of stable metadata.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TField Declaration<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default;
        }

        /// <summary>
        /// Reads the stable identifier from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StableDataId StableId<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).StableId;
        }

        /// <summary>
        /// Reads the semantic role from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasFieldRole Role<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).Role;
        }

        /// <summary>
        /// Reads the storage kind from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasStorageKind StorageKind<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).StorageKind;
        }

        /// <summary>
        /// Reads the ownership policy from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasOwnershipPolicy Ownership<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).Ownership;
        }

        /// <summary>
        /// Reads the lifetime policy from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasLifetimePolicy Lifetime<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).Lifetime;
        }

        /// <summary>
        /// Reads the length shape from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasLengthShape LengthShape<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).LengthShape;
        }

        /// <summary>
        /// Reads Field flags from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasFieldFlags Flags<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).Flags;
        }

        /// <summary>
        /// Reads hash participation from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasHashParticipation HashParticipation<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).HashParticipation;
        }

        /// <summary>
        /// Reads the diagnostic name from a Field declaration type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString64Bytes DebugName<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return default(TField).DebugName;
        }

        /// <summary>
        /// Creates a Contract from a Field declaration type.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>An unslotted validated Contract for the Field declaration.</returns>
        public static AtlasContract Contract<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return AtlasContract.Of<TField, TElement>();
        }

        /// <summary>
        /// Validates the declaration-level invariants of a Field type.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <exception cref="AtlasException">
        /// Thrown when the declaration has an invalid stable identifier, role, debug name,
        /// storage kind, ownership policy, lifetime policy, length shape, hash policy, or flags.
        /// </exception>
        /// <remarks>
        /// This validation does not verify Contract-table ordering, slot uniqueness, storage
        /// allocation compatibility, operation access compatibility, stage ordering, or compiled
        /// plan validity. Those checks belong to table, compiler, storage, and plan validators.
        /// </remarks>
        public static void ValidateDeclarationOrThrow<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            var field = default(TField);
            var diagnosticName = GetDiagnosticName<TField, TElement>();

            ValidateStableIdOrThrow(field.StableId, diagnosticName);
            ValidateRoleOrThrow(field.Role, diagnosticName);
            ValidateStorageKindOrThrow(field.StorageKind, diagnosticName);
            ValidateOwnershipOrThrow(field.Ownership, diagnosticName);
            ValidateLifetimeOrThrow(field.Lifetime, diagnosticName);
            ValidateLengthShapeOrThrow(field.LengthShape, diagnosticName);
            ValidateDebugNameOrThrow<TField, TElement>(field.DebugName);
            ValidateFlagsOrThrow(field.Flags, field.StorageKind, field.Ownership, diagnosticName);
            ValidateRolePolicyOrThrow(field.Role, field.Ownership, field.HashParticipation, diagnosticName);
        }

        /// <summary>
        /// Creates a managed diagnostic name for a Field declaration type.
        /// </summary>
        /// <typeparam name="TField">Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the Field.</typeparam>
        /// <returns>The declared debug name when present; otherwise, the Field declaration type name.</returns>
        /// <remarks>
        /// This method allocates when converting the fixed string or type name to a managed string.
        /// It should only be used for diagnostics, exceptions, editor tooling, and tests.
        /// </remarks>
        public static string GetDiagnosticName<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            var debugName = default(TField).DebugName;

            if (!debugName.IsEmpty)
            {
                return debugName.ToString();
            }

            return typeof(TField).FullName ?? typeof(TField).Name;
        }

        private static void ValidateStableIdOrThrow(StableDataId stableId, string diagnosticName)
        {
            if (stableId.IsValid)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field '{diagnosticName}' declares an invalid stable id. " +
                "A Field stable id must have a non-zero identity and a non-zero version.");
        }

        private static void ValidateRoleOrThrow(AtlasFieldRole role, string diagnosticName)
        {
            if (role != AtlasFieldRole.None)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field '{diagnosticName}' declares no Field role.");
        }

        private static void ValidateStorageKindOrThrow(AtlasStorageKind storageKind, string diagnosticName)
        {
            if (storageKind != AtlasStorageKind.None)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field '{diagnosticName}' declares no storage kind.");
        }

        private static void ValidateOwnershipOrThrow(AtlasOwnershipPolicy ownership, string diagnosticName)
        {
            if (ownership != AtlasOwnershipPolicy.None)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field '{diagnosticName}' declares no ownership policy.");
        }

        private static void ValidateLifetimeOrThrow(AtlasLifetimePolicy lifetime, string diagnosticName)
        {
            if (lifetime != AtlasLifetimePolicy.None)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field '{diagnosticName}' declares no lifetime policy.");
        }

        private static void ValidateLengthShapeOrThrow(AtlasLengthShape lengthShape, string diagnosticName)
        {
            try
            {
                lengthShape.ValidateOrThrow(nameof(lengthShape));
            }
            catch (Exception exception)
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' declares an invalid length shape '{lengthShape}'.",
                    exception);
            }
        }

        private static void ValidateDebugNameOrThrow<TField, TElement>(FixedString64Bytes debugName)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            if (!debugName.IsEmpty)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field '{typeof(TField).FullName ?? typeof(TField).Name}' declares an empty debug name.");
        }

        private static void ValidateFlagsOrThrow(
            AtlasFieldFlags flags,
            AtlasStorageKind storageKind,
            AtlasOwnershipPolicy ownership,
            string diagnosticName)
        {
            if (flags.HasAll(AtlasFieldFlags.ClearOnAcquire | AtlasFieldFlags.PreserveContent))
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' declares both " +
                    $"{nameof(AtlasFieldFlags.ClearOnAcquire)} and {nameof(AtlasFieldFlags.PreserveContent)}.");
            }

            if (flags.HasAny(AtlasFieldFlags.AllowsUninitializedMemory) &&
                flags.HasNone(AtlasFieldFlags.DiscardBeforeWrite))
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' declares " +
                    $"{nameof(AtlasFieldFlags.AllowsUninitializedMemory)} without " +
                    $"{nameof(AtlasFieldFlags.DiscardBeforeWrite)}.");
            }

            if (flags.HasAny(AtlasFieldFlags.AllowsExternalAlias) &&
                !IsExternalCompatibleOwnership(ownership))
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' allows external aliasing with incompatible ownership policy '{ownership}'.");
            }

            if (storageKind == AtlasStorageKind.External &&
                !IsExternalCompatibleOwnership(ownership))
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' uses external storage with incompatible ownership policy '{ownership}'.");
            }
        }

        private static void ValidateRolePolicyOrThrow(
            AtlasFieldRole role,
            AtlasOwnershipPolicy ownership,
            AtlasHashParticipation hashParticipation,
            string diagnosticName)
        {
            if (role == AtlasFieldRole.External &&
                !IsExternalCompatibleOwnership(ownership))
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' declares external Field role with incompatible ownership policy '{ownership}'.");
            }

            if (role == AtlasFieldRole.Canonical &&
                hashParticipation == AtlasHashParticipation.None)
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' declares canonical Field role but opts out of all hash participation.");
            }

            if (role == AtlasFieldRole.Payload &&
                hashParticipation == AtlasHashParticipation.None)
            {
                throw new AtlasException(
                    $"Atlas Field '{diagnosticName}' declares payload Field role but opts out of all hash participation.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsExternalCompatibleOwnership(AtlasOwnershipPolicy ownership)
        {
            return ownership == AtlasOwnershipPolicy.ExternalOwned ||
                   ownership == AtlasOwnershipPolicy.Borrowed ||
                   ownership == AtlasOwnershipPolicy.Imported ||
                   ownership == AtlasOwnershipPolicy.Adopted;
        }
    }
}