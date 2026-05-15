// Runtime/Fields/AtlasFieldHandle.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields
//
// Purpose
// - Represent a typed reference to an Atlas field declaration and its resolved contract-table slot.
// - Preserve zero-valid StableDataId and AtlasFieldSlot semantics.
// - Represent resolved/unresolved state explicitly instead of reserving an invalid slot.
// - Keep typed handles small, immutable, deterministic, and allocation-free.
//
// Design notes
// - default(AtlasFieldHandle<TField, TElement>) is valid as an unresolved handle.
// - StableDataId default/zero is valid.
// - AtlasFieldSlot default/zero is valid and represents slot zero.
// - Resolved state is represented by IsResolved.
// - Do not infer resolved/unresolved state from Slot == default.
// - Do not infer resolved/unresolved state from StableId == default.
// - Handles are setup/compiler/scheduler metadata, not hot-loop job data.
// - Jobs should receive resolved native containers, typed slices/views, or compiled addresses.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Represents a typed reference to an Atlas field declaration and its resolved contract-table slot.
    /// </summary>
    /// <typeparam name="TField">
    /// Field declaration type that owns the stable field metadata.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// Unmanaged element type stored by the field.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// A typed field handle binds the field declaration type, the field element type, the field's
    /// stable identity, and the table-local slot assigned during contract-table construction.
    /// </para>
    ///
    /// <para>
    /// The stable identity comes from <c>default(TField)</c>. The slot is table-local and may change
    /// when the table order changes. Do not serialize a field handle as durable identity; serialize
    /// <see cref="StableId"/> or a production catalog field identity instead.
    /// </para>
    ///
    /// <para>
    /// Slot zero is valid. The default slot value cannot represent unresolved state. This type
    /// therefore stores an explicit resolved flag. Consumers must check <see cref="IsResolved"/>
    /// when they require a table-resolved handle.
    /// </para>
    ///
    /// <para>
    /// Handles are intended for setup, validation, scheduling, and storage resolution. Burst jobs
    /// should receive resolved native containers, typed slices/views, or compiled field addresses
    /// instead of handles.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasFieldHandle<TField, TElement> :
        IEquatable<AtlasFieldHandle<TField, TElement>>
        where TField : struct, IAtlasField<TElement>
        where TElement : unmanaged
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        private const byte UnresolvedState = 0;
        private const byte ResolvedState = 1;

        private readonly byte _resolutionState;

        /// <summary>
        /// Gets an unresolved typed handle for this field declaration.
        /// </summary>
        /// <remarks>
        /// This handle contains the field declaration's stable identity, but it has no assigned
        /// contract-table slot. Slot payload is default, which is slot zero, but the explicit
        /// resolution flag is false.
        /// </remarks>
        public static AtlasFieldHandle<TField, TElement> Unresolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CreateUnresolved();
        }

        /// <summary>
        /// Gets the durable, versioned identity of the field declaration.
        /// </summary>
        /// <remarks>
        /// Zero/default is valid and must not be interpreted as missing or invalid.
        /// </remarks>
        public readonly StableDataId StableId;

        /// <summary>
        /// Gets the contract-table slot assigned to this field.
        /// </summary>
        /// <remarks>
        /// Slot zero/default is valid. Use <see cref="IsResolved"/> to determine whether this slot
        /// is assigned.
        /// </remarks>
        public readonly AtlasFieldSlot Slot;

        private AtlasFieldHandle(
            StableDataId stableId,
            AtlasFieldSlot slot,
            bool isResolved)
        {
            StableId = stableId;
            Slot = slot;
            _resolutionState = isResolved ? ResolvedState : UnresolvedState;
        }

        /// <summary>
        /// Gets whether this handle has a resolved contract-table slot.
        /// </summary>
        public bool IsResolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _resolutionState == ResolvedState;
        }

        /// <summary>
        /// Gets whether this handle has no resolved contract-table slot.
        /// </summary>
        public bool IsUnresolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _resolutionState != ResolvedState;
        }

        /// <summary>
        /// Gets whether this handle is structurally valid.
        /// </summary>
        /// <remarks>
        /// Every bit pattern is valid as a value object. Unresolved does not mean invalid.
        /// </remarks>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }

        /// <summary>
        /// Gets whether this handle is invalid.
        /// </summary>
        /// <remarks>
        /// This type has no invalid value. This property always returns <c>false</c> and exists only
        /// for source compatibility with older call sites.
        /// </remarks>
        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        /// <summary>
        /// Gets the field declaration value used as the metadata source.
        /// </summary>
        /// <remarks>
        /// Field declarations are expected to be immutable empty value types whose metadata is safe
        /// to read from <c>default(TField)</c>.
        /// </remarks>
        public TField Declaration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => default;
        }

        /// <summary>
        /// Creates an unresolved typed handle for this field declaration.
        /// </summary>
        /// <returns>An unresolved handle with the field declaration's stable identity.</returns>
        /// <exception cref="AtlasException">
        /// Thrown when the field declaration is semantically invalid.
        /// </exception>
        public static AtlasFieldHandle<TField, TElement> CreateUnresolved()
        {
            AtlasField.ValidateDeclarationOrThrow<TField, TElement>();

            return new AtlasFieldHandle<TField, TElement>(
                AtlasField.StableId<TField, TElement>(),
                default,
                false);
        }

        /// <summary>
        /// Creates a resolved typed handle for a table-local field slot.
        /// </summary>
        /// <param name="slot">The resolved table-local slot. Slot zero/default is valid.</param>
        /// <returns>A resolved typed field handle.</returns>
        /// <exception cref="AtlasException">
        /// Thrown when the field declaration is semantically invalid.
        /// </exception>
        public static AtlasFieldHandle<TField, TElement> Resolved(AtlasFieldSlot slot)
        {
            AtlasField.ValidateDeclarationOrThrow<TField, TElement>();

            return new AtlasFieldHandle<TField, TElement>(
                AtlasField.StableId<TField, TElement>(),
                slot,
                true);
        }

        /// <summary>
        /// Creates a resolved typed handle for a zero-based contract-table slot index.
        /// </summary>
        /// <param name="slotIndex">The zero-based contract-table slot index. Slot index zero is valid.</param>
        /// <returns>A resolved typed field handle.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slotIndex"/> is outside the supported Atlas field-slot range.
        /// </exception>
        /// <exception cref="AtlasException">
        /// Thrown when the field declaration is semantically invalid.
        /// </exception>
        public static AtlasFieldHandle<TField, TElement> Resolved(int slotIndex)
        {
            return Resolved(AtlasFieldSlot.FromIndex(slotIndex));
        }

        /// <summary>
        /// Creates a resolved typed handle from a stable identity and slot.
        /// </summary>
        /// <param name="stableId">The stable identity to store. Zero/default is valid.</param>
        /// <param name="slot">The resolved table-local slot. Slot zero/default is valid.</param>
        /// <returns>A resolved typed field handle.</returns>
        /// <remarks>
        /// Prefer <see cref="Resolved(AtlasFieldSlot)"/> when constructing handles from typed
        /// declarations. This method is useful for deserialization, tests, and compiler internals
        /// that already resolved stable identity separately.
        /// </remarks>
        public static AtlasFieldHandle<TField, TElement> ResolvedUnchecked(
            StableDataId stableId,
            AtlasFieldSlot slot)
        {
            return new AtlasFieldHandle<TField, TElement>(
                stableId,
                slot,
                true);
        }

        /// <summary>
        /// Throws when this handle is not resolved to a contract-table slot.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when this handle is unresolved.</exception>
        public void ThrowIfUnresolved()
        {
            if (IsResolved)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Atlas field handle '{AtlasField.GetDiagnosticName<TField, TElement>()}' is unresolved.");
        }

        /// <summary>
        /// Throws when this handle is resolved and its stable identity does not match the current field declaration.
        /// </summary>
        /// <exception cref="AtlasException">
        /// Thrown when this handle is resolved and its stable identity differs from
        /// <c>default(TField).StableId</c>.
        /// </exception>
        /// <remarks>
        /// The check is skipped for unresolved handles because <c>default(AtlasFieldHandle)</c> is a
        /// valid unresolved value and may contain a default stable identity that is unrelated to
        /// <typeparamref name="TField"/>.
        /// </remarks>
        public void ThrowIfResolvedIdentityMismatch()
        {
            if (IsUnresolved)
            {
                return;
            }

            var declaredStableId = AtlasField.StableId<TField, TElement>();

            if (StableId == declaredStableId)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas field handle '{AtlasField.GetDiagnosticName<TField, TElement>()}' has stable id " +
                $"'{StableId}', but the field declaration currently reports '{declaredStableId}'.");
        }

        /// <summary>
        /// Throws when this handle is unresolved or its stable identity does not match the current field declaration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when this handle is unresolved.</exception>
        /// <exception cref="AtlasException">Thrown when the resolved handle identity is stale or mismatched.</exception>
        public void ValidateResolvedOrThrow()
        {
            ThrowIfUnresolved();
            ThrowIfResolvedIdentityMismatch();
        }

        /// <summary>
        /// Determines whether this handle references the same stable field identity as another handle.
        /// </summary>
        /// <param name="other">The handle to compare against.</param>
        /// <returns><c>true</c> when the stable identities match; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameStableIdAs(AtlasFieldHandle<TField, TElement> other)
        {
            return StableId == other.StableId;
        }

        /// <summary>
        /// Determines whether this handle references the same table-local slot as another handle.
        /// </summary>
        /// <param name="other">The handle to compare against.</param>
        /// <returns><c>true</c> when the slots match; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameSlotAs(AtlasFieldHandle<TField, TElement> other)
        {
            return Slot == other.Slot;
        }

        /// <summary>
        /// Determines whether this handle equals another handle of the same field type.
        /// </summary>
        /// <param name="other">The handle to compare with this handle.</param>
        /// <returns>
        /// <c>true</c> when stable identity, slot, and resolved state all match; otherwise,
        /// <c>false</c>.
        /// </returns>
        public bool Equals(AtlasFieldHandle<TField, TElement> other)
        {
            return StableId == other.StableId &&
                   Slot == other.Slot &&
                   _resolutionState == other._resolutionState;
        }

        /// <summary>
        /// Determines whether this handle equals an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this handle.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is a typed handle with the same stable identity,
        /// slot, and resolved state; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasFieldHandle<TField, TElement> other && Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this typed handle.
        /// </summary>
        /// <returns>A deterministic 32-bit hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ StableId.GetHashCode();
                hash = (hash * HashMultiplier) ^ Slot.GetHashCode();
                hash = (hash * HashMultiplier) ^ _resolutionState;
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this typed handle.
        /// </summary>
        /// <returns>A string containing the field diagnostic name, stable identity, slot, and resolution state.</returns>
        /// <remarks>
        /// This method allocates and is intended for diagnostics, editor tooling, exceptions, and tests.
        /// </remarks>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} [{1}] Slot={2} State={3}",
                AtlasField.GetDiagnosticName<TField, TElement>(),
                StableId,
                Slot,
                IsResolved ? "Resolved" : "Unresolved");
        }

        /// <summary>
        /// Determines whether two typed handles are equal.
        /// </summary>
        public static bool operator ==(
            AtlasFieldHandle<TField, TElement> left,
            AtlasFieldHandle<TField, TElement> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two typed handles are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasFieldHandle<TField, TElement> left,
            AtlasFieldHandle<TField, TElement> right)
        {
            return !left.Equals(right);
        }
    }
}