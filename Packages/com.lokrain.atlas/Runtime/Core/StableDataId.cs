// Runtime/Core/StableDataId.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Core
//
// Purpose
// - Define a stable, versioned data identity value.
// - Keep zero/default values valid for production ABI safety.
// - Avoid invalid sentinels; absence is represented by explicit presence state elsewhere.
//
// Design notes
// - default(StableDataId) is valid.
// - StableDataId.Zero is valid.
// - Version 0 is valid.
// - This type does not encode missing/invalid state.
// - Optionality must be represented by the containing type through an explicit boolean/presence flag.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Represents a stable, versioned identity for an Atlas data contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The all-zero value is valid. This type intentionally does not reserve default, zero identity,
    /// or version zero as invalid. Missing, optional, or unresolved state must be represented by an
    /// explicit boolean or presence field owned by the containing type.
    /// </para>
    ///
    /// <para>
    /// This value is suitable for unmanaged/Burst-facing metadata because every bit pattern is valid.
    /// Code must not use <see cref="Zero"/> or <c>default</c> as an invalid
    /// sentinel.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct StableDataId :
        IEquatable<StableDataId>,
        IComparable<StableDataId>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// The all-zero identifier.
        /// </summary>
        /// <remarks>
        /// This value is valid. It is not an invalid sentinel and must not be used to represent absence.
        /// </remarks>
        public static readonly StableDataId Zero = default;


        /// <summary>
        /// Gets the high 64 bits of the durable 128-bit identity.
        /// </summary>
        public readonly ulong High;

        /// <summary>
        /// Gets the low 64 bits of the durable 128-bit identity.
        /// </summary>
        public readonly ulong Low;

        /// <summary>
        /// Gets the semantic contract version.
        /// </summary>
        /// <remarks>
        /// Version zero is valid.
        /// </remarks>
        public readonly ushort Version;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDataId"/> struct.
        /// </summary>
        /// <param name="high">The high 64 bits of the durable 128-bit identity.</param>
        /// <param name="low">The low 64 bits of the durable 128-bit identity.</param>
        /// <param name="version">The semantic contract version. Version zero is valid.</param>
        public StableDataId(ulong high, ulong low, ushort version)
        {
            High = high;
            Low = low;
            Version = version;
        }

        /// <summary>
        /// Gets whether this value is the all-zero identifier.
        /// </summary>
        /// <remarks>
        /// This is a value query only. A zero identifier is valid and does not mean missing or invalid.
        /// </remarks>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => High == 0UL && Low == 0UL && Version == 0;
        }



        /// <summary>
        /// Returns a copy of this identifier with a different semantic version.
        /// </summary>
        /// <param name="version">The semantic contract version to assign. Version zero is valid.</param>
        /// <returns>A copy of this identifier with <paramref name="version"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StableDataId WithVersion(ushort version)
        {
            return new StableDataId(High, Low, version);
        }

        /// <summary>
        /// Returns whether this value has the same 128-bit identity as another value, ignoring version.
        /// </summary>
        /// <param name="other">The identifier to compare against.</param>
        /// <returns><c>true</c> when the high and low identity words match; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameIdentityAs(StableDataId other)
        {
            return High == other.High && Low == other.Low;
        }

        /// <summary>
        /// Returns whether this value has the same identity as another value and a newer version.
        /// </summary>
        /// <param name="other">The identifier to compare against.</param>
        /// <returns><c>true</c> when identity matches and this version is greater; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNewerVersionOf(StableDataId other)
        {
            return HasSameIdentityAs(other) && Version > other.Version;
        }

        /// <summary>
        /// Returns whether this value has the same identity as another value and an older version.
        /// </summary>
        /// <param name="other">The identifier to compare against.</param>
        /// <returns><c>true</c> when identity matches and this version is lower; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOlderVersionOf(StableDataId other)
        {
            return HasSameIdentityAs(other) && Version < other.Version;
        }

        /// <summary>
        /// Validates this value.
        /// </summary>
        /// <param name="parameterName">Optional caller parameter name retained for source compatibility.</param>
        /// <remarks>
        /// This method intentionally performs no checks because every bit pattern is valid.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ValidateOrThrow(string parameterName = null)
        {
            _ = parameterName;
        }

        /// <summary>
        /// Determines whether this value equals another <see cref="StableDataId"/>.
        /// </summary>
        /// <param name="other">The value to compare against.</param>
        /// <returns><c>true</c> when all identity and version fields match; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StableDataId other)
        {
            return High == other.High && Low == other.Low && Version == other.Version;
        }

        /// <summary>
        /// Determines whether this value equals another object.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal <see cref="StableDataId"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is StableDataId other && Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this value.
        /// </summary>
        /// <returns>A deterministic 32-bit hash code.</returns>
        /// <remarks>
        /// This intentionally avoids <see cref="HashCode"/> so value hashing does not depend on runtime
        /// implementation details.
        /// </remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                var highFold = (int)(High ^ (High >> 32));
                var lowFold = (int)(Low ^ (Low >> 32));

                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ highFold;
                hash = (hash * HashMultiplier) ^ lowFold;
                hash = (hash * HashMultiplier) ^ Version;
                return hash;
            }
        }

        /// <summary>
        /// Compares this value with another <see cref="StableDataId"/>.
        /// </summary>
        /// <param name="other">The value to compare against.</param>
        /// <returns>
        /// A negative value when this value sorts before <paramref name="other"/>, zero when equal,
        /// or a positive value when this value sorts after <paramref name="other"/>.
        /// </returns>
        public int CompareTo(StableDataId other)
        {
            var highComparison = High.CompareTo(other.High);
            if (highComparison != 0)
            {
                return highComparison;
            }

            var lowComparison = Low.CompareTo(other.Low);
            if (lowComparison != 0)
            {
                return lowComparison;
            }

            return Version.CompareTo(other.Version);
        }

        /// <summary>
        /// Returns an invariant diagnostic representation of this identifier.
        /// </summary>
        /// <returns>A stable diagnostic string containing the high word, low word, and version.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:X16}-{1:X16}-v{2}",
                High,
                Low,
                Version);
        }

        /// <summary>
        /// Determines whether two identifiers are equal.
        /// </summary>
        public static bool operator ==(StableDataId left, StableDataId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two identifiers are not equal.
        /// </summary>
        public static bool operator !=(StableDataId left, StableDataId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left identifier sorts before the right identifier.
        /// </summary>
        public static bool operator <(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left identifier sorts after the right identifier.
        /// </summary>
        public static bool operator >(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left identifier sorts before or equal to the right identifier.
        /// </summary>
        public static bool operator <=(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left identifier sorts after or equal to the right identifier.
        /// </summary>
        public static bool operator >=(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}