// Runtime/Stages/AtlasStageId.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Stages
//
// Purpose
// - Represent a stable, versioned stage contract identity.
// - Preserve stage identity as distinct from field and operation identity.
// - Keep default/zero stage identifiers valid.
// - Avoid invalid sentinels in unmanaged/Burst-facing value objects.
//
// Design notes
// - default(AtlasStageId) is valid.
// - AtlasStageId.Zero is valid.
// - AtlasStageId.Empty is a compatibility alias for Zero, not an invalid sentinel.
// - Version 0 is valid.
// - This type does not encode missing, unsupported, undeclared, or disabled state.
// - Stage declaration/support belongs to stage catalog metadata.
// - Missing lookup results must be represented by bool-returning APIs or explicit presence flags.
// - GetHashCode is deterministic and does not use System.HashCode.

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
    /// Stage identity is separate from field and operation identity even though all three use the
    /// same durable 128-bit identity plus version shape. Keeping a distinct stage identifier prevents
    /// accidentally passing operation or field identifiers where stage identifiers are required.
    /// </para>
    ///
    /// <para>
    /// A stage is a durable public macro boundary. It is not a job, not a scheduler, and not an
    /// algorithm micro-step. Concrete jobs and schedulers are implementation details behind operation
    /// execution.
    /// </para>
    ///
    /// <para>
    /// This identifier must remain stable across refactors, assembly renames, operation composition
    /// changes, job implementation changes, Unity domain reloads, editor sessions, and player builds.
    /// </para>
    ///
    /// <para>
    /// The all-zero/default value is valid. This type intentionally has no invalid bit pattern.
    /// Missing stage lookup state must be represented by an explicit boolean, option wrapper,
    /// containing presence flag, or catalog lookup result.
    /// </para>
    ///
    /// <para>
    /// Increment the version when the durable stage contract changes incompatibly: ordered operation
    /// ABI, external requirements, guarantees, validation rules, deterministic output contract, or
    /// public stage semantics.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasStageId :
        IEquatable<AtlasStageId>,
        IComparable<AtlasStageId>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// The all-zero stage identifier.
        /// </summary>
        /// <remarks>
        /// This value is valid. It is not an invalid sentinel.
        /// </remarks>
        public static readonly AtlasStageId Zero = default;

        /// <summary>
        /// Compatibility alias for <see cref="Zero"/>.
        /// </summary>
        /// <remarks>
        /// This value is valid. It is not an invalid or missing sentinel.
        /// </remarks>
        public static readonly AtlasStageId Empty = default;

        /// <summary>
        /// Compatibility alias for <see cref="Zero"/>.
        /// </summary>
        /// <remarks>
        /// This value is valid. It is retained only for older call sites and must not be used to
        /// represent invalid state.
        /// </remarks>
        public static readonly AtlasStageId Invalid = default;

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
        /// <remarks>
        /// Version zero is valid.
        /// </remarks>
        public readonly ushort Version;

        /// <summary>
        /// Creates a stable stage identifier from explicit identity parts.
        /// </summary>
        /// <param name="high">High 64 bits of the durable stage identity.</param>
        /// <param name="low">Low 64 bits of the durable stage identity.</param>
        /// <param name="version">Semantic stage contract version. Version zero is valid.</param>
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
        /// Gets whether this stage identifier is the all-zero value.
        /// </summary>
        /// <remarks>
        /// Zero is valid and does not mean missing or invalid.
        /// </remarks>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => High == 0UL &&
                   Low == 0UL &&
                   Version == 0;
        }

        /// <summary>
        /// Compatibility alias for <see cref="IsZero"/>.
        /// </summary>
        /// <remarks>
        /// Empty does not mean invalid. This property is retained only for source compatibility.
        /// </remarks>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsZero;
        }

        /// <summary>
        /// Gets whether this stage identifier is structurally valid.
        /// </summary>
        /// <remarks>
        /// Every bit pattern is valid. This property is retained for compatibility with older call
        /// sites that expected an <c>IsValid</c> member.
        /// </remarks>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }

        /// <summary>
        /// Gets whether this stage identifier is structurally invalid.
        /// </summary>
        /// <remarks>
        /// This type has no invalid value. This property always returns <c>false</c>.
        /// </remarks>
        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        /// <summary>
        /// Creates a stage identifier with the same durable identity and a different version.
        /// </summary>
        /// <param name="version">New semantic stage contract version. Version zero is valid.</param>
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
        /// <param name="stableId">Stable identifier to convert. Zero/default is valid.</param>
        /// <returns>A stage identifier with matching identity and version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AtlasStageId FromStableDataId(StableDataId stableId)
        {
            return new AtlasStageId(
                stableId.High,
                stableId.Low,
                stableId.Version);
        }

        /// <summary>
        /// Validates this identifier.
        /// </summary>
        /// <param name="parameterName">Optional parameter name retained for source compatibility.</param>
        /// <remarks>
        /// This method intentionally performs no checks because every bit pattern is valid.
        /// Declaration, availability, route support, and stage-surface compatibility must be
        /// validated through the stage catalog and profile compiler.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ValidateOrThrow(string parameterName = null)
        {
            _ = parameterName;
        }

        /// <summary>
        /// Determines whether this identifier is equal to another stage identifier.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when identity and version match.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Returns a deterministic hash code for this identifier.
        /// </summary>
        /// <returns>A deterministic 32-bit hash code derived from identity and version.</returns>
        /// <remarks>
        /// This intentionally avoids <see cref="HashCode"/> so hashing does not depend on runtime
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