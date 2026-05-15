// Runtime/Stages/AtlasStageId.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Stages
//
// Purpose
// - Define stable, versioned identity for Atlas stage contracts.
// - Keep stage identity separate from Field identity, Operation identity, and Pipeline identity.
// - Support deterministic diagnostics, compatibility validation, generated documentation, and durable artifacts.
//
// Design notes
// - A stage is a durable named contract over an ordered operation sequence.
// - A stage id identifies the semantic stage contract, not a job, scheduler, operation occurrence, or implementation class.
// - Version changes should represent incompatible stage-contract changes.
// - Jobs must not receive this type.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Stable, versioned identifier for an Atlas stage contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage identity is separate from Field identity and Operation identity even though all three
    /// use the same durable 128-bit identity plus version shape. Keeping a distinct stage
    /// identifier prevents accidentally passing operation or Field identifiers where stage
    /// identifiers are required.
    /// </para>
    ///
    /// <para>
    /// A stage is a durable semantic contract. Its implementation may be reorganized, its jobs may
    /// change, and its operation definitions may be refactored, but the stage id should remain
    /// stable while the public stage contract remains compatible.
    /// </para>
    ///
    /// <para>
    /// Increment the version when the stage contract changes incompatibly: operation sequence
    /// semantics, required inputs, produced outputs, validation rules, deterministic artifact
    /// contribution, stage boundary semantics, diagnostics contract, or route compatibility.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasStageId :
        IEquatable<AtlasStageId>,
        IComparable<AtlasStageId>
    {
        /// <summary>
        /// Reserved invalid stage identifier.
        /// </summary>
        public static readonly AtlasStageId Empty = default;

        /// <summary>
        /// High 64 bits of the durable stage identity.
        /// </summary>
        public readonly ulong High;

        /// <summary>
        /// Low 64 bits of the durable stage identity.
        /// </summary>
        public readonly ulong Low;

        /// <summary>
        /// Semantic contract version of the stage.
        /// </summary>
        public readonly ushort Version;

        /// <summary>
        /// Creates a stable stage identifier from explicit identity parts.
        /// </summary>
        /// <param name="high">High 64 bits of the durable stage identity.</param>
        /// <param name="low">Low 64 bits of the durable stage identity.</param>
        /// <param name="version">Semantic stage contract version. Version zero is invalid.</param>
        public AtlasStageId(
            ulong high,
            ulong low,
            ushort version)
        {
            High = high;
            Low = low;
            Version = version;
        }

        /// <summary>
        /// Gets whether this stage identifier is the reserved empty value.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => High == 0UL &&
                   Low == 0UL &&
                   Version == 0;
        }

        /// <summary>
        /// Gets whether this stage identifier is valid for a concrete stage contract.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (High != 0UL || Low != 0UL) &&
                   Version != 0;
        }

        /// <summary>
        /// Creates a stage identifier with the same durable identity and a different version.
        /// </summary>
        /// <param name="version">New semantic stage contract version.</param>
        /// <returns>A stage identifier with the supplied version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtlasStageId WithVersion(ushort version)
        {
            return new AtlasStageId(High, Low, version);
        }

        /// <summary>
        /// Determines whether another stage identifier has the same durable identity, ignoring version.
        /// </summary>
        /// <param name="other">The stage identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when both identifiers refer to the same durable stage identity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameIdentityAs(AtlasStageId other)
        {
            return High == other.High &&
                   Low == other.Low;
        }

        /// <summary>
        /// Determines whether this identifier is a newer version of another stage identifier.
        /// </summary>
        /// <param name="other">The stage identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when durable identity matches and this version is greater.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNewerVersionOf(AtlasStageId other)
        {
            return HasSameIdentityAs(other) &&
                   Version > other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is an older version of another stage identifier.
        /// </summary>
        /// <param name="other">The stage identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when durable identity matches and this version is lower.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOlderVersionOf(AtlasStageId other)
        {
            return HasSameIdentityAs(other) &&
                   Version < other.Version;
        }

        /// <summary>
        /// Converts this stage identifier to the generic stable identifier representation.
        /// </summary>
        /// <returns>A <see cref="StableDataId"/> with matching identity and version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StableDataId ToStableDataId()
        {
            return new StableDataId(High, Low, Version);
        }

        /// <summary>
        /// Creates a stage identifier from a generic stable identifier.
        /// </summary>
        /// <param name="stableId">Stable identifier to convert.</param>
        /// <returns>A stage identifier with matching identity and version.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is invalid.
        /// </exception>
        public static AtlasStageId FromStableDataId(StableDataId stableId)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return new AtlasStageId(
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
                "Atlas stage id must have a non-zero identity and a non-zero version.",
                parameterName ?? nameof(AtlasStageId));
        }

        /// <summary>
        /// Determines whether this identifier is equal to another stage identifier.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when identity and version match.</returns>
        public bool Equals(AtlasStageId other)
        {
            return High == other.High &&
                   Low == other.Low &&
                   Version == other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this identifier.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal <see cref="AtlasStageId"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasStageId other &&
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
        public int CompareTo(AtlasStageId other)
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
        /// Returns a diagnostic representation of this stage identifier.
        /// </summary>
        /// <returns>A stable diagnostic string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:X16}-{1:X16}-stage-v{2}",
                High,
                Low,
                Version);
        }

        /// <summary>
        /// Determines whether two stage identifiers are equal.
        /// </summary>
        public static bool operator ==(AtlasStageId left, AtlasStageId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two stage identifiers are not equal.
        /// </summary>
        public static bool operator !=(AtlasStageId left, AtlasStageId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether one stage identifier sorts before another.
        /// </summary>
        public static bool operator <(AtlasStageId left, AtlasStageId right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one stage identifier sorts after another.
        /// </summary>
        public static bool operator >(AtlasStageId left, AtlasStageId right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one stage identifier sorts before or equal to another.
        /// </summary>
        public static bool operator <=(AtlasStageId left, AtlasStageId right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one stage identifier sorts after or equal to another.
        /// </summary>
        public static bool operator >=(AtlasStageId left, AtlasStageId right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}