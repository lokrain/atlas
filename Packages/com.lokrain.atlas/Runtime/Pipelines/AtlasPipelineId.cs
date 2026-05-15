// Runtime/Pipelines/AtlasPipelineId.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Pipelines
//
// Purpose
// - Define stable, versioned identity for Atlas pipeline contracts.
// - Keep pipeline identity separate from Field identity, Operation identity, and Stage identity.
// - Support deterministic diagnostics, compatibility validation, generated documentation, and durable artifacts.
//
// Design notes
// - A pipeline is a durable named contract over an ordered stage sequence.
// - A pipeline id identifies the semantic pipeline/preset contract, not a runtime executor,
//   generated artifact, job chain, operation occurrence, or Unity asset instance.
// - Version changes should represent incompatible pipeline-contract changes.
// - Jobs must not receive this type.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Pipelines
{
    /// <summary>
    /// Stable, versioned identifier for an Atlas pipeline contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pipeline identity is separate from Field identity, Operation identity, and Stage identity
    /// even though all use the same durable 128-bit identity plus version shape. Keeping a distinct
    /// pipeline identifier prevents accidentally passing a Field, operation, or stage identifier
    /// where a pipeline identifier is required.
    /// </para>
    ///
    /// <para>
    /// A pipeline is a durable semantic route/preset contract. Its stage implementation may be
    /// reorganized and its compiled execution plan may change, but the pipeline id should remain
    /// stable while the public pipeline contract remains compatible.
    /// </para>
    ///
    /// <para>
    /// Increment the version when the pipeline contract changes incompatibly: stage sequence
    /// semantics, required Field catalog, route compatibility, deterministic artifact contract,
    /// validation rules, diagnostics contract, or hash participation.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasPipelineId :
        IEquatable<AtlasPipelineId>,
        IComparable<AtlasPipelineId>
    {
        /// <summary>
        /// Reserved invalid pipeline identifier.
        /// </summary>
        public static readonly AtlasPipelineId Empty = default;

        /// <summary>
        /// High 64 bits of the durable pipeline identity.
        /// </summary>
        public readonly ulong High;

        /// <summary>
        /// Low 64 bits of the durable pipeline identity.
        /// </summary>
        public readonly ulong Low;

        /// <summary>
        /// Semantic contract version of the pipeline.
        /// </summary>
        public readonly ushort Version;

        /// <summary>
        /// Creates a stable pipeline identifier from explicit identity parts.
        /// </summary>
        /// <param name="high">High 64 bits of the durable pipeline identity.</param>
        /// <param name="low">Low 64 bits of the durable pipeline identity.</param>
        /// <param name="version">Semantic pipeline contract version. Version zero is invalid.</param>
        public AtlasPipelineId(
            ulong high,
            ulong low,
            ushort version)
        {
            High = high;
            Low = low;
            Version = version;
        }

        /// <summary>
        /// Gets whether this pipeline identifier is the reserved empty value.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => High == 0UL &&
                   Low == 0UL &&
                   Version == 0;
        }

        /// <summary>
        /// Gets whether this pipeline identifier is valid for a concrete pipeline contract.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (High != 0UL || Low != 0UL) &&
                   Version != 0;
        }

        /// <summary>
        /// Creates a pipeline identifier with the same durable identity and a different version.
        /// </summary>
        /// <param name="version">New semantic pipeline contract version.</param>
        /// <returns>A pipeline identifier with the supplied version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtlasPipelineId WithVersion(ushort version)
        {
            return new AtlasPipelineId(High, Low, version);
        }

        /// <summary>
        /// Determines whether another pipeline identifier has the same durable identity, ignoring version.
        /// </summary>
        /// <param name="other">The pipeline identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when both identifiers refer to the same durable pipeline identity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasSameIdentityAs(AtlasPipelineId other)
        {
            return High == other.High &&
                   Low == other.Low;
        }

        /// <summary>
        /// Determines whether this identifier is a newer version of another pipeline identifier.
        /// </summary>
        /// <param name="other">The pipeline identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when durable identity matches and this version is greater.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNewerVersionOf(AtlasPipelineId other)
        {
            return HasSameIdentityAs(other) &&
                   Version > other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is an older version of another pipeline identifier.
        /// </summary>
        /// <param name="other">The pipeline identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when durable identity matches and this version is lower.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOlderVersionOf(AtlasPipelineId other)
        {
            return HasSameIdentityAs(other) &&
                   Version < other.Version;
        }

        /// <summary>
        /// Converts this pipeline identifier to the generic stable identifier representation.
        /// </summary>
        /// <returns>A <see cref="StableDataId"/> with matching identity and version.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StableDataId ToStableDataId()
        {
            return new StableDataId(High, Low, Version);
        }

        /// <summary>
        /// Creates a pipeline identifier from a generic stable identifier.
        /// </summary>
        /// <param name="stableId">Stable identifier to convert.</param>
        /// <returns>A pipeline identifier with matching identity and version.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stableId"/> is invalid.
        /// </exception>
        public static AtlasPipelineId FromStableDataId(StableDataId stableId)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return new AtlasPipelineId(
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
                "Atlas pipeline id must have a non-zero identity and a non-zero version.",
                parameterName ?? nameof(AtlasPipelineId));
        }

        /// <summary>
        /// Determines whether this identifier is equal to another pipeline identifier.
        /// </summary>
        /// <param name="other">The identifier to compare with this identifier.</param>
        /// <returns><c>true</c> when identity and version match.</returns>
        public bool Equals(AtlasPipelineId other)
        {
            return High == other.High &&
                   Low == other.Low &&
                   Version == other.Version;
        }

        /// <summary>
        /// Determines whether this identifier is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this identifier.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal <see cref="AtlasPipelineId"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasPipelineId other &&
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
        public int CompareTo(AtlasPipelineId other)
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
        /// Returns a diagnostic representation of this pipeline identifier.
        /// </summary>
        /// <returns>A stable diagnostic string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:X16}-{1:X16}-pipeline-v{2}",
                High,
                Low,
                Version);
        }

        /// <summary>
        /// Determines whether two pipeline identifiers are equal.
        /// </summary>
        public static bool operator ==(AtlasPipelineId left, AtlasPipelineId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two pipeline identifiers are not equal.
        /// </summary>
        public static bool operator !=(AtlasPipelineId left, AtlasPipelineId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether one pipeline identifier sorts before another.
        /// </summary>
        public static bool operator <(AtlasPipelineId left, AtlasPipelineId right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one pipeline identifier sorts after another.
        /// </summary>
        public static bool operator >(AtlasPipelineId left, AtlasPipelineId right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one pipeline identifier sorts before or equal to another.
        /// </summary>
        public static bool operator <=(AtlasPipelineId left, AtlasPipelineId right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one pipeline identifier sorts after or equal to another.
        /// </summary>
        public static bool operator >=(AtlasPipelineId left, AtlasPipelineId right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}