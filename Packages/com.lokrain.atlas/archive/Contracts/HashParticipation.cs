// Runtime/Contracts/HashParticipation.cs

using System;
using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Defines which parts of an Atlas Field participate in deterministic hash calculations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hash participation is a contract-level policy. It tells Atlas which aspects of a Field
    /// are relevant for Contract compatibility, plan-cache identity, shape-cache identity,
    /// replay validation, deterministic diagnostics, and content-based verification.
    /// </para>
    ///
    /// <para>
    /// This enum does not perform hashing by itself. Hash construction belongs to Atlas hashing
    /// utilities and validators. Field declarations only declare whether a Field should
    /// participate in each hash category.
    /// </para>
    ///
    /// <para>
    /// Content hashing can be expensive and should not be enabled by default for large transient
    /// buffers. It is primarily intended for replay, rollback, deterministic tests, validation,
    /// and explicit cache keys.
    /// </para>
    /// </remarks>
    [Flags]
    public enum HashParticipation : byte
    {
        /// <summary>
        /// The Field does not participate in Atlas hash calculations.
        /// </summary>
        /// <remarks>
        /// This is appropriate for diagnostics-only Fields, temporary scratch buffers,
        /// editor-only state, and Fields whose existence should not affect compatibility or
        /// deterministic validation.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The Field contributes Contract-schema metadata to schema hashes.
        /// </summary>
        /// <remarks>
        /// Schema participation should include stable identity, version, storage kind, element
        /// layout, ownership policy, lifetime policy, access-relevant flags, and hash policy.
        /// It should not include runtime length, capacity, or content.
        /// </remarks>
        Schema = 1 << 0,

        /// <summary>
        /// The Field contributes resolved length or capacity information to shape hashes.
        /// </summary>
        /// <remarks>
        /// Shape participation is useful for plan compilation, storage reuse, cache validation,
        /// and diagnostics where a plan depends on concrete Field lengths or capacities.
        /// </remarks>
        Shape = 1 << 1,

        /// <summary>
        /// The Field contributes runtime contents to content hashes.
        /// </summary>
        /// <remarks>
        /// Content participation should be opt-in. It requires a deterministic byte-level or
        /// element-level hashing strategy for the Field storage format and element type.
        /// </remarks>
        Content = 1 << 2,

        /// <summary>
        /// The Field stable identifier version contributes to compatibility hashes.
        /// </summary>
        /// <remarks>
        /// Version participation is normally enabled for concrete Fields. Disabling it means
        /// two Contracts with the same durable identity but different contract versions may be
        /// treated as compatible by hash-based systems.
        /// </remarks>
        Version = 1 << 3,

        /// <summary>
        /// The Field contributes ownership and lifetime policies to compatibility hashes.
        /// </summary>
        /// <remarks>
        /// Ownership and lifetime changes can alter disposal responsibility, allocator choice,
        /// dependency tracking, and aliasing safety. They should usually participate in schema
        /// compatibility.
        /// </remarks>
        OwnershipAndLifetime = 1 << 4,

        /// <summary>
        /// The Field contributes declared flags and access-relevant policy bits to schema hashes.
        /// </summary>
        /// <remarks>
        /// This includes flags that affect allocation, clearing, deterministic ordering,
        /// parallel-write eligibility, aliasing, and storage safety.
        /// </remarks>
        Policy = 1 << 5,

        /// <summary>
        /// Default participation for normal Contract-driven Fields.
        /// </summary>
        /// <remarks>
        /// This is suitable for most Fields whose Contract schema and resolved shape should
        /// affect compatibility and plan/storage cache identity, but whose runtime contents should
        /// not be hashed automatically.
        /// </remarks>
        Default = Schema | Shape | Version | OwnershipAndLifetime | Policy,

        /// <summary>
        /// Full participation including runtime contents.
        /// </summary>
        /// <remarks>
        /// Use this only when content hashing is explicitly required and the Field storage format
        /// has a deterministic hashing implementation.
        /// </remarks>
        Full = Default | Content
    }

    /// <summary>
    /// Provides allocation-free helpers for working with <see cref="HashParticipation"/>.
    /// </summary>
    /// <remarks>
    /// These helpers avoid <see cref="Enum.HasFlag(Enum)"/> so checks remain allocation-free and
    /// suitable for validation, Contract hashing, and Burst-compatible utility paths.
    /// </remarks>
    public static class HashParticipationExtensions
    {
        /// <summary>
        /// Determines whether all requested hash participation flags are present.
        /// </summary>
        /// <param name="value">The participation set to inspect.</param>
        /// <param name="flags">The flags that must all be present.</param>
        /// <returns><c>true</c> when all requested flags are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this HashParticipation value, HashParticipation flags)
        {
            return (value & flags) == flags;
        }

        /// <summary>
        /// Determines whether at least one requested hash participation flag is present.
        /// </summary>
        /// <param name="value">The participation set to inspect.</param>
        /// <param name="flags">The flags where any match is accepted.</param>
        /// <returns><c>true</c> when at least one requested flag is present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this HashParticipation value, HashParticipation flags)
        {
            return (value & flags) != 0;
        }

        /// <summary>
        /// Determines whether none of the requested hash participation flags are present.
        /// </summary>
        /// <param name="value">The participation set to inspect.</param>
        /// <param name="flags">The flags that must all be absent.</param>
        /// <returns><c>true</c> when none of the requested flags are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(this HashParticipation value, HashParticipation flags)
        {
            return (value & flags) == 0;
        }

        /// <summary>
        /// Gets whether this participation policy contributes to Contract schema hashes.
        /// </summary>
        /// <param name="value">The participation policy to inspect.</param>
        /// <returns><c>true</c> when schema participation is enabled; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IncludesSchema(this HashParticipation value)
        {
            return value.HasAny(HashParticipation.Schema);
        }

        /// <summary>
        /// Gets whether this participation policy contributes to resolved shape hashes.
        /// </summary>
        /// <param name="value">The participation policy to inspect.</param>
        /// <returns><c>true</c> when shape participation is enabled; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IncludesShape(this HashParticipation value)
        {
            return value.HasAny(HashParticipation.Shape);
        }

        /// <summary>
        /// Gets whether this participation policy contributes to runtime content hashes.
        /// </summary>
        /// <param name="value">The participation policy to inspect.</param>
        /// <returns><c>true</c> when content participation is enabled; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IncludesContent(this HashParticipation value)
        {
            return value.HasAny(HashParticipation.Content);
        }

        /// <summary>
        /// Gets whether this participation policy affects compatibility hashes.
        /// </summary>
        /// <param name="value">The participation policy to inspect.</param>
        /// <returns>
        /// <c>true</c> when version, ownership/lifetime, policy, or schema participation is enabled;
        /// otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AffectsCompatibility(this HashParticipation value)
        {
            return value.HasAny(
                HashParticipation.Schema |
                HashParticipation.Version |
                HashParticipation.OwnershipAndLifetime |
                HashParticipation.Policy);
        }
    }
}