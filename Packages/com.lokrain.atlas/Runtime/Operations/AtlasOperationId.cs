// Runtime/Operations/AtlasOperationId.cs

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Stable, versioned identifier for an Atlas operation contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operation identity is separate from Field identity even though both use the same durable
    /// 128-bit identity plus version shape. Keeping a distinct operation identifier prevents
    /// accidentally passing Field identifiers where operation identifiers are required.
    /// </para>
    ///
    /// <para>
    /// This identifier must remain stable across refactors, assembly renames, job implementation
    /// changes, operation ordering changes, Unity domain reloads, editor sessions, and player
    /// builds.
    /// </para>
    ///
    /// <para>
    /// Increment the version when the operation contract changes incompatibly: read/write
    /// requirements, execution semantics, deterministic output contract, required shape inputs,
    /// produced artifacts, or validation rules.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasOperationId :
        IEquatable<AtlasOperationId>,
        IComparable<AtlasOperationId>
    {
        /// <summary>
        /// Reserved invalid operation identifier.
        /// </summary>
        public static readonly AtlasOperationId Empty = default;

        /// <summary>
        /// High 64 bits of the durable operation identity.
        /// </summary>
        public readonly ulong High;

        /// <summary>
        /// Low 64 bits of the durable operation identity.
        /// </summary>
        public readonly ulong Low;

        /// <summary>
        /// Semantic contract version of the operation.
        /// </summary>
        public readonly ushort Version;

        /// <summary>
        /// Creates a stable operation identifier from explicit identity parts.
        /// </summary>
        /// <param name="high">High 64 bits of the durable operation identity.</param>
        /// <param name="low">Low 64 bits of the durable operation identity.</param>
        /// <param name="version">Semantic operation contract version. Version zero is invalid.</param>
        public AtlasOperationId(
            ulong high,
            ulong low,
            ushort version)
        {
            High = high;
            Low = low;
            Version = version;
        }

        /// <summary>
        /// Gets whether this operation identifier is the reserved empty value.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => High == 0UL &&
                   Low == 0UL &&
                   Version == 0;
        }

        /// <summary>
        /// Gets whether this operation identifier is valid for a concrete operation contract.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (High != 0UL || Low != 0UL) &&
                   Version != 0;
        }

        /// <summary>
        /// Creates an operation identifier with the same durable identity and a different version.
        /// </summary>
        /// <param name="version">New semantic operation contract version.</param>
        /// <returns>An operation identifier with the supplied version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtlasOperationId WithVersion(ushort version)
        {
            return new AtlasOperationId(High, Low, version);
        }

        /// <summary>
        /// Determines whether another operation identifier has the same durable identity, ignoring version.
        /// </summary>
        /// <param name="other">The operation identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when both identifiers refer to the same durable operation identity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameIdentityAs(AtlasOperationId other)
        {
            return High == other.High &&
                   Low == other.Low;
        }

        /// <summary>
        /// Determines whether this identifier is a newer version of another operation identifier.
        /// </summary>
        /// <param name="other">The operation identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when durable identity matches and this version is greater.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNewerVersionOf(AtlasOperationId other)
        {
            return HasSameIdentityAs(other) &&
                   Version > other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is an older version of another operation identifier.
        /// </summary>
        /// <param name="other">The operation identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when durable identity matches and this version is lower.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOlderVersionOf(AtlasOperationId other)
        {
            return HasSameIdentityAs(other) &&
                   Version < other.Version;
        }

        /// <summary>
        /// Converts this operation identifier to the generic stable identifier representation.
        /// </summary>
        /// <returns>A <see cref="StableDataId"/> with matching identity and version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StableDataId ToStableDataId()
        {
            return new StableDataId(High, Low, Version);
        }

        /// <summary>
        /// Creates an operation identifier from a generic stable identifier.
        /// </summary>
        /// <param name="stableId">Stable identifier to convert.</param>
        /// <returns>An operation identifier with matching identity and version.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is invalid.
        /// </exception>
        public static AtlasOperationId FromStableDataId(StableDataId stableId)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return new AtlasOperationId(
                stableId.High,
                stableId.Low,
                stableId.Version);
        }

        /// <summary>
        /// Throws when this identifier is invalid.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when this identifier has zero identity or zero version.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            if (IsValid)
            {
                return;
            }

            throw new ArgumentException(
                "Atlas operation id must have a non-zero identity and a non-zero version.",
                parameterName ?? nameof(AtlasOperationId));
        }

        /// <summary>
        /// Determines whether this identifier is equal to another operation identifier.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when identity and version match.</returns>
        public bool Equals(AtlasOperationId other)
        {
            return High == other.High &&
                   Low == other.Low &&
                   Version == other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this identifier.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal <see cref="AtlasOperationId"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasOperationId other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code derived from identity and version.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ High.GetHashCode();
                hash = (hash * 397) ^ Low.GetHashCode();
                hash = (hash * 397) ^ Version.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Compares this identifier to another identifier using canonical deterministic ordering.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns>A comparison value suitable for deterministic diagnostics and validation output.</returns>
        public int CompareTo(AtlasOperationId other)
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
        /// Returns a diagnostic representation of this operation identifier.
        /// </summary>
        /// <returns>A stable diagnostic string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:X16}-{1:X16}-op-v{2}",
                High,
                Low,
                Version);
        }

        /// <summary>
        /// Determines whether two operation identifiers are equal.
        /// </summary>
        public static bool operator ==(AtlasOperationId left, AtlasOperationId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two operation identifiers are not equal.
        /// </summary>
        public static bool operator !=(AtlasOperationId left, AtlasOperationId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether one operation identifier sorts before another.
        /// </summary>
        public static bool operator <(AtlasOperationId left, AtlasOperationId right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one operation identifier sorts after another.
        /// </summary>
        public static bool operator >(AtlasOperationId left, AtlasOperationId right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one operation identifier sorts before or equal to another.
        /// </summary>
        public static bool operator <=(AtlasOperationId left, AtlasOperationId right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one operation identifier sorts after or equal to another.
        /// </summary>
        public static bool operator >=(AtlasOperationId left, AtlasOperationId right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}