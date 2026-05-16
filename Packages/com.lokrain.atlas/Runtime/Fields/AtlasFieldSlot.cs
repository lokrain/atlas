// Runtime/Fields/AtlasFieldSlot.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields
//
// Purpose
// - Represent zero-based contract-table slots.
// - Keep slot zero and every ushort slot payload valid.
// - Avoid invalid slot sentinels.
// - Force missing/unresolved state to be represented explicitly by the containing type.
//
// Design notes
// - default(AtlasFieldSlot) is valid and represents slot zero.
// - A slot is table-local compiled metadata, not durable field identity.
// - Durable identity belongs to StableDataId or the production field catalog.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Represents the canonical zero-based table slot assigned to an Atlas field contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field slot is a table-local index produced by contract-table construction. It is not a
    /// durable field identity and must not be serialized as a stable external reference.
    /// </para>
    ///
    /// <para>
    /// The default value is valid and represents slot zero. This type intentionally has no invalid
    /// bit pattern. Optional, missing, or unresolved state must be represented by an explicit
    /// boolean/presence field on the containing type.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasFieldSlot :
        IEquatable<AtlasFieldSlot>,
        IComparable<AtlasFieldSlot>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;


        /// <summary>
        /// The first valid field slot index.
        /// </summary>
        public static readonly AtlasFieldSlot First = new AtlasFieldSlot((ushort)AtlasConstants.FirstFieldSlot);

        /// <summary>
        /// The last valid field slot index.
        /// </summary>
        public static readonly AtlasFieldSlot Last = new AtlasFieldSlot((ushort)AtlasConstants.LastFieldSlot);

        private readonly ushort _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="AtlasFieldSlot"/> struct.
        /// </summary>
        /// <param name="value">The zero-based table slot value.</param>
        /// <remarks>
        /// Every <see cref="ushort"/> value is structurally valid, but package-level limits may be
        /// narrower. Use <see cref="FromIndex"/> when validating user or compiler input.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtlasFieldSlot(ushort value)
        {
            _value = value;
        }



        /// <summary>
        /// Gets the zero-based slot value.
        /// </summary>
        public ushort Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }

        /// <summary>
        /// Gets the zero-based slot value as an array index.
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }


        /// <summary>
        /// Creates a field slot from a zero-based table index.
        /// </summary>
        /// <param name="index">The zero-based slot index.</param>
        /// <returns>A field slot representing <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> falls outside the supported field-slot range.
        /// </exception>
        public static AtlasFieldSlot FromIndex(int index)
        {
            if (index < AtlasConstants.FirstFieldSlot ||
                index > AtlasConstants.LastFieldSlot)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Field slot index must be between {0} and {1}.",
                        AtlasConstants.FirstFieldSlot,
                        AtlasConstants.LastFieldSlot));
            }

            return new AtlasFieldSlot((ushort)index);
        }

        /// <summary>
        /// Attempts to create a field slot from a zero-based table index.
        /// </summary>
        /// <param name="index">The zero-based slot index.</param>
        /// <param name="slot">
        /// The created slot when this method returns <c>true</c>; otherwise, the default slot.
        /// </param>
        /// <returns>
        /// <c>true</c> when <paramref name="index"/> is within the supported slot range; otherwise,
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The output value is not semantically meaningful when this method returns <c>false</c>.
        /// The returned boolean owns success/failure state.
        /// </remarks>
        public static bool TryFromIndex(int index, out AtlasFieldSlot slot)
        {
            if (index < AtlasConstants.FirstFieldSlot ||
                index > AtlasConstants.LastFieldSlot)
            {
                slot = default;
                return false;
            }

            slot = new AtlasFieldSlot((ushort)index);
            return true;
        }

        /// <summary>
        /// Validates this slot.
        /// </summary>
        /// <remarks>
        /// This method intentionally performs no checks because every bit pattern is valid. It is
        /// retained for compatibility with older validation call sites.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowIfInvalid()
        {
        }

        /// <summary>
        /// Validates this slot.
        /// </summary>
        /// <param name="parameterName">Optional caller parameter name retained for compatibility.</param>
        /// <remarks>
        /// This method intentionally performs no checks because every bit pattern is valid.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ValidateOrThrow(string parameterName = null)
        {
            _ = parameterName;
        }

        /// <summary>
        /// Determines whether this slot equals another slot.
        /// </summary>
        /// <param name="other">The slot to compare against.</param>
        /// <returns><c>true</c> when both slots contain the same slot value; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AtlasFieldSlot other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Determines whether this slot equals another object.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal slot; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasFieldSlot other && Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this slot.
        /// </summary>
        /// <returns>A deterministic 32-bit hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (HashSeed * HashMultiplier) ^ _value;
            }
        }

        /// <summary>
        /// Compares this slot with another slot.
        /// </summary>
        /// <param name="other">The slot to compare against.</param>
        /// <returns>
        /// A negative value when this slot sorts before <paramref name="other"/>, zero when equal,
        /// or a positive value when this slot sorts after <paramref name="other"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(AtlasFieldSlot other)
        {
            return _value.CompareTo(other._value);
        }

        /// <summary>
        /// Returns an invariant diagnostic representation of this slot.
        /// </summary>
        /// <returns>The zero-based slot value.</returns>
        public override string ToString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines whether two slots are equal.
        /// </summary>
        public static bool operator ==(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two slots are not equal.
        /// </summary>
        public static bool operator !=(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left slot sorts before the right slot.
        /// </summary>
        public static bool operator <(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left slot sorts after the right slot.
        /// </summary>
        public static bool operator >(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left slot sorts before or equal to the right slot.
        /// </summary>
        public static bool operator <=(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left slot sorts after or equal to the right slot.
        /// </summary>
        public static bool operator >=(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}