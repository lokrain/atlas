// Runtime/Fields/AtlasFieldHandle.cs

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Represents a typed reference to an Atlas Field and its resolved Contract-table slot.
    /// </summary>
    /// <typeparam name="TField">
    /// Field declaration type that owns the stable Field metadata.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// Unmanaged element type stored by the Field.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// A Field handle binds three things together: the Field declaration type, the
    /// Field element type, and the Contract-table slot assigned during table construction
    /// or validation.
    /// </para>
    ///
    /// <para>
    /// The stable identity comes from <c>default(TField)</c>. The slot is table-local and
    /// may change when the table is reordered. Do not serialize this handle as durable identity;
    /// serialize <see cref="StableId"/> instead.
    /// </para>
    ///
    /// <para>
    /// Handles are intended for setup, validation, scheduling, and storage resolution. Jobs
    /// should receive resolved native containers, not handles.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasFieldHandle<TField, TElement> :
        IEquatable<AtlasFieldHandle<TField, TElement>>
        where TField : struct, IAtlasField<TElement>
        where TElement : unmanaged
    {
        /// <summary>
        /// Represents an unresolved typed handle for this Field declaration.
        /// </summary>
        /// <remarks>
        /// The handle still contains the Field's stable identity, but its slot is invalid.
        /// It may be used before the Field is placed in a Contract table.
        /// </remarks>
        public static AtlasFieldHandle<TField, TElement> Unresolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new AtlasFieldHandle<TField, TElement>(
                AtlasField.StableId<TField, TElement>(),
                AtlasFieldSlot.Invalid);
        }

        /// <summary>
        /// Durable, versioned identity of the Field contract.
        /// </summary>
        public readonly StableDataId StableId;

        /// <summary>
        /// Canonical Contract-table slot assigned to this Field.
        /// </summary>
        /// <remarks>
        /// The slot is table-local and must not be treated as durable identity.
        /// </remarks>
        public readonly AtlasFieldSlot Slot;

        private AtlasFieldHandle(StableDataId stableId, AtlasFieldSlot slot)
        {
            StableId = stableId;
            Slot = slot;
        }

        /// <summary>
        /// Gets whether this handle has a valid Contract-table slot.
        /// </summary>
        public bool IsResolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slot.IsValid;
        }

        /// <summary>
        /// Gets whether this handle does not have a valid Contract-table slot.
        /// </summary>
        public bool IsUnresolved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slot.IsInvalid;
        }

        /// <summary>
        /// Gets the Field declaration value used as the metadata source.
        /// </summary>
        /// <remarks>
        /// Field declarations are expected to be immutable empty value types whose metadata
        /// is safe to read from <c>default(TField)</c>.
        /// </remarks>
        public TField Declaration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => default;
        }

        /// <summary>
        /// Creates a resolved typed handle for a table slot.
        /// </summary>
        /// <param name="slot">The validated table-local slot assigned to the Field.</param>
        /// <returns>A resolved typed Field handle.</returns>
        /// <exception cref="AtlasException">
        /// Thrown when the Field declaration is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="slot"/> is invalid.
        /// </exception>
        public static AtlasFieldHandle<TField, TElement> Resolved(AtlasFieldSlot slot)
        {
            AtlasField.ValidateDeclarationOrThrow<TField, TElement>();
            slot.ThrowIfInvalid();

            return new AtlasFieldHandle<TField, TElement>(
                AtlasField.StableId<TField, TElement>(),
                slot);
        }

        /// <summary>
        /// Creates a resolved typed handle for a zero-based table index.
        /// </summary>
        /// <param name="slotIndex">Zero-based Contract-table slot.</param>
        /// <returns>A resolved typed Field handle.</returns>
        /// <exception cref="AtlasException">
        /// Thrown when the Field declaration is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="slotIndex"/> is outside the valid Atlas slot range.
        /// </exception>
        public static AtlasFieldHandle<TField, TElement> Resolved(int slotIndex)
        {
            return Resolved(AtlasFieldSlot.FromIndex(slotIndex));
        }

        /// <summary>
        /// Throws when this handle is not resolved to a valid Contract-table slot.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this handle is unresolved.
        /// </exception>
        public void ThrowIfUnresolved()
        {
            if (IsResolved)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Atlas Field handle '{AtlasField.GetDiagnosticName<TField, TElement>()}' is unresolved.");
        }

        /// <summary>
        /// Throws when this handle's stable identity does not match the current Field declaration.
        /// </summary>
        /// <exception cref="AtlasException">
        /// Thrown when the handle identity differs from <c>default(TField).StableId</c>.
        /// </exception>
        /// <remarks>
        /// This check detects stale handles created from older declarations or incorrect
        /// manual construction paths. It does not validate Contract-table membership.
        /// </remarks>
        public void ThrowIfIdentityMismatch()
        {
            var declaredStableId = AtlasField.StableId<TField, TElement>();

            if (StableId == declaredStableId)
            {
                return;
            }

            throw new AtlasException(
                $"Atlas Field handle '{AtlasField.GetDiagnosticName<TField, TElement>()}' has stable id " +
                $"'{StableId}', but the Field declaration currently reports '{declaredStableId}'.");
        }

        /// <summary>
        /// Determines whether this handle is equal to another handle of the same Field type.
        /// </summary>
        /// <param name="other">The handle to compare with this handle.</param>
        /// <returns>
        /// <c>true</c> when stable identity and slot both match; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(AtlasFieldHandle<TField, TElement> other)
        {
            return StableId == other.StableId &&
                   Slot == other.Slot;
        }

        /// <summary>
        /// Determines whether this handle is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this handle.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is a typed handle with the same stable
        /// identity and slot.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasFieldHandle<TField, TElement> other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code for this typed handle.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StableId.GetHashCode();
                hash = (hash * 397) ^ Slot.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this typed handle.
        /// </summary>
        /// <returns>
        /// A string containing the Field diagnostic name, stable identity, and slot state.
        /// </returns>
        /// <remarks>
        /// This method allocates and is intended for diagnostics, editor tooling, exceptions,
        /// and tests.
        /// </remarks>
        public override string ToString()
        {
            return $"{AtlasField.GetDiagnosticName<TField, TElement>()} [{StableId}] Slot={Slot}";
        }

        /// <summary>
        /// Determines whether two typed handles are equal.
        /// </summary>
        /// <param name="left">The first handle.</param>
        /// <param name="right">The second handle.</param>
        /// <returns>
        /// <c>true</c> when stable identity and slot both match; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(
            AtlasFieldHandle<TField, TElement> left,
            AtlasFieldHandle<TField, TElement> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two typed handles are not equal.
        /// </summary>
        /// <param name="left">The first handle.</param>
        /// <param name="right">The second handle.</param>
        /// <returns>
        /// <c>true</c> when stable identity or slot differs; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(
            AtlasFieldHandle<TField, TElement> left,
            AtlasFieldHandle<TField, TElement> right)
        {
            return !left.Equals(right);
        }
    }
}