// Runtime/Core/StableDataId.cs

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Represents a stable, versioned identity for an Atlas Field contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The 128-bit identity is the durable Field identity. It must remain stable across
    /// refactors, assembly renames, namespace changes, Unity domain reloads, editor sessions,
    /// player builds, and Contract-table reordering.
    /// </para>
    ///
    /// <para>
    /// Do not derive this value from C# type names, assembly names, Unity asset paths,
    /// Contract-table slots, ECS component type indices, or any other refactor-sensitive
    /// source.
    /// </para>
    ///
    /// <para>
    /// <see cref="Version"/> is part of equality. Increment it when the Field contract
    /// changes incompatibly, including changes to element layout, storage kind, ownership,
    /// lifetime, shape rules, or hash participation semantics.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct StableDataId :
        IEquatable<StableDataId>,
        IComparable<StableDataId>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 31;

        /// <summary>
        /// Represents the reserved invalid identifier.
        /// </summary>
        /// <remarks>
        /// Atlas Field declarations must not use this value. It is intended for default
        /// initialization, sentinel values, and validation failure paths.
        /// </remarks>
        public static readonly StableDataId Empty = default;

        /// <summary>
        /// High 64 bits of the durable 128-bit Field identity.
        /// </summary>
        public readonly ulong High;

        /// <summary>
        /// Low 64 bits of the durable 128-bit Field identity.
        /// </summary>
        public readonly ulong Low;

        /// <summary>
        /// Semantic version of the Field contract.
        /// </summary>
        /// <remarks>
        /// Version zero is reserved. Valid Atlas Field declarations should use version one
        /// or higher.
        /// </remarks>
        public readonly ushort Version;

        /// <summary>
        /// Creates a stable Field identity from explicit identity and version components.
        /// </summary>
        /// <param name="high">High 64 bits of the durable 128-bit Field identity.</param>
        /// <param name="low">Low 64 bits of the durable 128-bit Field identity.</param>
        /// <param name="version">Semantic Field contract version. Zero is reserved.</param>
        public StableDataId(ulong high, ulong low, ushort version)
        {
            High = high;
            Low = low;
            Version = version;
        }

        /// <summary>
        /// Gets whether this value is exactly the reserved default identifier.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => High == 0UL && Low == 0UL && Version == 0;
        }

        /// <summary>
        /// Gets whether this value is valid for an Atlas Field declaration.
        /// </summary>
        /// <remarks>
        /// A valid identifier requires a non-zero 128-bit identity and a non-zero version.
        /// </remarks>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (High != 0UL || Low != 0UL) && Version != 0;
        }

        /// <summary>
        /// Creates a new identifier with the same durable identity and a different contract version.
        /// </summary>
        /// <param name="version">The replacement semantic contract version.</param>
        /// <returns>A new identifier with the same identity and the supplied version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StableDataId WithVersion(ushort version)
        {
            return new StableDataId(High, Low, version);
        }

        /// <summary>
        /// Determines whether two identifiers refer to the same durable Field identity,
        /// ignoring contract version.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns>
        /// <c>true</c> when the 128-bit identity matches; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameIdentityAs(StableDataId other)
        {
            return High == other.High && Low == other.Low;
        }

        /// <summary>
        /// Determines whether this identifier is a newer contract version of the same durable identity.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns>
        /// <c>true</c> when both identifiers share the same 128-bit identity and this identifier
        /// has a greater version.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNewerVersionOf(StableDataId other)
        {
            return HasSameIdentityAs(other) && Version > other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is an older contract version of the same durable identity.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns>
        /// <c>true</c> when both identifiers share the same 128-bit identity and this identifier
        /// has a smaller version.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOlderVersionOf(StableDataId other)
        {
            return HasSameIdentityAs(other) && Version < other.Version;
        }

        /// <summary>
        /// Throws when this identifier is not valid for an Atlas Field declaration.
        /// </summary>
        /// <param name="parameterName">
        /// Optional parameter name used by the thrown exception.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the identity is zero, the version is zero, or both are zero.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            if (IsValid)
            {
                return;
            }

            throw new ArgumentException(
                "Stable data id must have a non-zero identity and a non-zero version.",
                parameterName ?? nameof(StableDataId));
        }

        /// <summary>
        /// Determines whether this identifier is equal to another identifier.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns>
        /// <c>true</c> when high identity bits, low identity bits, and version all match.
        /// </returns>
        public bool Equals(StableDataId other)
        {
            return High == other.High &&
                   Low == other.Low &&
                   Version == other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this identifier.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is a <see cref="StableDataId"/> with the
        /// same high identity bits, low identity bits, and version.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is StableDataId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <remarks>
        /// This hash is for managed collection lookup only. Schema hashes, content hashes,
        /// replay hashes, and deterministic compatibility hashes should use Atlas hashing
        /// utilities instead of <see cref="GetHashCode"/>.
        /// </remarks>
        /// <returns>A managed hash code for this identifier.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;

                hash = (hash * HashMultiplier) + High.GetHashCode();
                hash = (hash * HashMultiplier) + Low.GetHashCode();
                hash = (hash * HashMultiplier) + Version.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Compares identifiers using Atlas canonical diagnostic order.
        /// </summary>
        /// <remarks>
        /// Ordering is by high identity bits, then low identity bits, then version. Contract
        /// table order is still defined by the table itself; this comparison exists for
        /// validation, diagnostics, deterministic sorting of error reports, and tests.
        /// </remarks>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns>
        /// A negative value when this identifier sorts before <paramref name="other"/>,
        /// zero when they are equal, and a positive value when this identifier sorts after it.
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
        /// Returns a culture-invariant diagnostic representation of this identifier.
        /// </summary>
        /// <returns>
        /// A string containing the high identity bits, low identity bits, and version.
        /// </returns>
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
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns>
        /// <c>true</c> when high identity bits, low identity bits, and version all match.
        /// </returns>
        public static bool operator ==(StableDataId left, StableDataId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two identifiers are not equal.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns>
        /// <c>true</c> when any identity or version component differs.
        /// </returns>
        public static bool operator !=(StableDataId left, StableDataId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left identifier sorts before the right identifier.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><c>true</c> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
        public static bool operator <(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left identifier sorts after the right identifier.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns><c>true</c> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
        public static bool operator >(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left identifier sorts before or equal to the right identifier.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="left"/> sorts before or is equal to
        /// <paramref name="right"/>.
        /// </returns>
        public static bool operator <=(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left identifier sorts after or equal to the right identifier.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="left"/> sorts after or is equal to
        /// <paramref name="right"/>.
        /// </returns>
        public static bool operator >=(StableDataId left, StableDataId right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}