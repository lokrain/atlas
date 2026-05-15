// Runtime/Fields/AtlasFieldSlot.cs

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Represents the canonical table slot assigned to an Atlas Field Contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Slots are table-local indexes. They are not stable identity and must not be serialized
    /// as durable Field references. Use <see cref="StableDataId"/> for durable identity.
    /// </para>
    ///
    /// <para>
    /// The default value of <see cref="AtlasFieldSlot"/> is invalid. This is intentional:
    /// unresolved slots should fail validation instead of accidentally resolving to slot zero.
    /// </para>
    ///
    /// <para>
    /// Valid slots are zero-based and are suitable for direct indexing into Contract and
    /// runtime storage arrays after validation.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasFieldSlot :
        IEquatable<AtlasFieldSlot>,
        IComparable<AtlasFieldSlot>
    {
        private const ushort InvalidEncodedValue = 0;

        /// <summary>
        /// Represents an unresolved, missing, or invalid Field slot.
        /// </summary>
        public static readonly AtlasFieldSlot Invalid = default;

        private readonly ushort _encodedValue;

        private AtlasFieldSlot(ushort encodedValue, bool _)
        {
            _encodedValue = encodedValue;
        }

        /// <summary>
        /// Creates a valid Field slot from a zero-based table index.
        /// </summary>
        /// <param name="value">Zero-based Contract-table slot.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="value"/> is the reserved invalid slot sentinel.
        /// </exception>
        public AtlasFieldSlot(ushort value)
        {
            if (value == AtlasConstants.InvalidFieldSlot)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Field slot value is reserved for invalid slots.");
            }

            _encodedValue = Encode(value);
        }

        /// <summary>
        /// Gets whether this slot is valid for Contract-table and storage-array indexing.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _encodedValue != InvalidEncodedValue;
        }

        /// <summary>
        /// Gets whether this slot is unresolved, missing, or invalid.
        /// </summary>
        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _encodedValue == InvalidEncodedValue;
        }

        /// <summary>
        /// Gets the zero-based slot value.
        /// </summary>
        /// <remarks>
        /// This property throws for invalid slots. Use <see cref="ValueOrInvalid"/> when a
        /// non-throwing sentinel value is required for diagnostics or validation reports.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this slot is invalid.
        /// </exception>
        public ushort Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfInvalid();
                return Decode(_encodedValue);
            }
        }

        /// <summary>
        /// Gets the zero-based slot value as an array index.
        /// </summary>
        /// <remarks>
        /// This property is equivalent to <see cref="Value"/> but returns <see cref="int"/> for
        /// APIs that index managed arrays, native arrays, and validation collections.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this slot is invalid.
        /// </exception>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value;
        }

        /// <summary>
        /// Gets the zero-based slot value or the reserved invalid slot sentinel.
        /// </summary>
        /// <remarks>
        /// This property does not throw. It is intended for diagnostics, validation reports,
        /// and serialization of transient validation state. It must not be used as durable
        /// Field identity.
        /// </remarks>
        public ushort ValueOrInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsValid ? Decode(_encodedValue) : AtlasConstants.InvalidFieldSlot;
        }

        /// <summary>
        /// Creates a valid Field slot from a zero-based table index.
        /// </summary>
        /// <param name="index">Zero-based Contract-table slot.</param>
        /// <returns>A valid Field slot for the supplied index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the valid Atlas slot range.
        /// </exception>
        public static AtlasFieldSlot FromIndex(int index)
        {
            if (index < AtlasConstants.FirstFieldSlot ||
                index > AtlasConstants.LastFieldSlot)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"Field slot index must be between {AtlasConstants.FirstFieldSlot} and {AtlasConstants.LastFieldSlot}.");
            }

            return new AtlasFieldSlot((ushort)index);
        }

        /// <summary>
        /// Attempts to create a valid Field slot from a zero-based table index.
        /// </summary>
        /// <param name="index">Zero-based Contract-table slot.</param>
        /// <param name="slot">
        /// The created slot when <paramref name="index"/> is valid; otherwise,
        /// <see cref="Invalid"/>.
        /// </param>
        /// <returns><c>true</c> when the index is valid; otherwise, <c>false</c>.</returns>
        public static bool TryFromIndex(int index, out AtlasFieldSlot slot)
        {
            if (index < AtlasConstants.FirstFieldSlot ||
                index > AtlasConstants.LastFieldSlot)
            {
                slot = Invalid;
                return false;
            }

            slot = new AtlasFieldSlot((ushort)index);
            return true;
        }

        /// <summary>
        /// Throws when this slot is invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this slot is invalid.
        /// </exception>
        public void ThrowIfInvalid()
        {
            if (IsValid)
            {
                return;
            }

            throw new InvalidOperationException("Atlas Field slot is invalid.");
        }

        /// <summary>
        /// Determines whether this slot is equal to another slot.
        /// </summary>
        /// <param name="other">The slot to compare with this slot.</param>
        /// <returns>
        /// <c>true</c> when both slots have the same validity state and value; otherwise,
        /// <c>false</c>.
        /// </returns>
        public bool Equals(AtlasFieldSlot other)
        {
            return _encodedValue == other._encodedValue;
        }

        /// <summary>
        /// Determines whether this slot is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this slot.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasFieldSlot"/> with
        /// the same validity state and value.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasFieldSlot other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code for this slot.</returns>
        public override int GetHashCode()
        {
            return _encodedValue.GetHashCode();
        }

        /// <summary>
        /// Compares slots by canonical table order.
        /// </summary>
        /// <remarks>
        /// Valid slots sort before invalid slots. Among valid slots, comparison is by numeric
        /// zero-based slot value.
        /// </remarks>
        /// <param name="other">The slot to compare with this slot.</param>
        /// <returns>
        /// A negative value when this slot sorts before <paramref name="other"/>, zero when
        /// both slots are equal, and a positive value when this slot sorts after it.
        /// </returns>
        public int CompareTo(AtlasFieldSlot other)
        {
            return ValueOrInvalid.CompareTo(other.ValueOrInvalid);
        }

        /// <summary>
        /// Returns a culture-invariant diagnostic representation of this slot.
        /// </summary>
        /// <returns>
        /// The zero-based slot value for valid slots; otherwise, the string <c>Invalid</c>.
        /// </returns>
        public override string ToString()
        {
            return IsValid
                ? Value.ToString(CultureInfo.InvariantCulture)
                : "Invalid";
        }

        /// <summary>
        /// Determines whether two slots are equal.
        /// </summary>
        /// <param name="left">The first slot.</param>
        /// <param name="right">The second slot.</param>
        /// <returns><c>true</c> when both slots have the same validity state and value.</returns>
        public static bool operator ==(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two slots are not equal.
        /// </summary>
        /// <param name="left">The first slot.</param>
        /// <param name="right">The second slot.</param>
        /// <returns><c>true</c> when the slots differ by validity state or value.</returns>
        public static bool operator !=(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left slot sorts before the right slot.
        /// </summary>
        /// <param name="left">The first slot.</param>
        /// <param name="right">The second slot.</param>
        /// <returns><c>true</c> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
        public static bool operator <(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left slot sorts after the right slot.
        /// </summary>
        /// <param name="left">The first slot.</param>
        /// <param name="right">The second slot.</param>
        /// <returns><c>true</c> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
        public static bool operator >(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left slot sorts before or equal to the right slot.
        /// </summary>
        /// <param name="left">The first slot.</param>
        /// <param name="right">The second slot.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="left"/> sorts before or is equal to
        /// <paramref name="right"/>.
        /// </returns>
        public static bool operator <=(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left slot sorts after or equal to the right slot.
        /// </summary>
        /// <param name="left">The first slot.</param>
        /// <param name="right">The second slot.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="left"/> sorts after or is equal to
        /// <paramref name="right"/>.
        /// </returns>
        public static bool operator >=(AtlasFieldSlot left, AtlasFieldSlot right)
        {
            return left.CompareTo(right) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Encode(ushort value)
        {
            return checked((ushort)(value + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Decode(ushort encodedValue)
        {
            return checked((ushort)(encodedValue - 1));
        }
    }
}