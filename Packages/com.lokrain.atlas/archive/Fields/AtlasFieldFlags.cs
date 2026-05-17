// Runtime/Fields/AtlasFieldFlags.cs

using System;
using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Defines Field-level behavior flags used by Atlas validation, storage allocation,
    /// clearing policy, and scheduling checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags describe Field behavior that is independent from the Field's declared
    /// read/write access in a specific plan. Plan access is modeled separately by
    /// <see cref="AtlasFieldAccess"/>.
    /// </para>
    ///
    /// <para>
    /// Flags should describe durable Field semantics. Do not use this enum for temporary
    /// scheduler decisions, per-frame state, runtime validation results, or Contract-table
    /// ordering.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasFieldFlags : uint
    {
        /// <summary>
        /// No special Field behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Field may be absent from a compatible runtime storage frame.
        /// </summary>
        /// <remarks>
        /// Optional Fields require explicit plan support. A job plan that requires the
        /// Field must still fail validation when the Field is missing.
        /// </remarks>
        Optional = 1u << 0,

        /// <summary>
        /// The Field may change length or capacity during its declared lifetime.
        /// </summary>
        /// <remarks>
        /// Resizable Fields require storage formats that support resizing. Fixed-size
        /// containers such as <c>NativeArray&lt;T&gt;</c> should not use this flag.
        /// </remarks>
        Resizable = 1u << 1,

        /// <summary>
        /// The Field storage must be cleared when acquired for a new lifetime interval.
        /// </summary>
        /// <remarks>
        /// This is appropriate for accumulation buffers, counters, temporary outputs, and
        /// Fields whose previous contents must not be observed by the next user.
        /// </remarks>
        ClearOnAcquire = 1u << 2,

        /// <summary>
        /// Existing contents are intentionally undefined before the first write in a plan.
        /// </summary>
        /// <remarks>
        /// Validators may require write-only access before read access for Fields using
        /// this flag. This enables allocation paths that avoid unnecessary initialization.
        /// </remarks>
        DiscardBeforeWrite = 1u << 3,

        /// <summary>
        /// The Field must preserve its content across acquisitions within its declared lifetime.
        /// </summary>
        /// <remarks>
        /// This flag is incompatible with <see cref="ClearOnAcquire"/> for the same acquisition
        /// boundary. Validators should reject contradictory clearing and preservation policies.
        /// </remarks>
        PreserveContent = 1u << 4,

        /// <summary>
        /// The Field requires deterministic iteration or write ordering.
        /// </summary>
        /// <remarks>
        /// This flag is intended for replay, rollback, deterministic simulation, validation,
        /// and reproducible tests. It should not be applied to every Field by default.
        /// </remarks>
        DeterministicOrder = 1u << 5,

        /// <summary>
        /// The Field permits parallel writes when a plan declares a compatible write pattern.
        /// </summary>
        /// <remarks>
        /// This flag does not make arbitrary parallel writes safe. The storage format, plan
        /// access declaration, and job implementation must still provide a valid non-overlapping,
        /// atomic, or explicitly synchronized write strategy.
        /// </remarks>
        AllowsParallelWrite = 1u << 6,

        /// <summary>
        /// The Field may be allocated without zero-initializing memory.
        /// </summary>
        /// <remarks>
        /// This flag is only safe when all consumers treat the initial content as undefined
        /// until written. Validators should require this to be paired with
        /// <see cref="DiscardBeforeWrite"/> or an equivalent write-before-read guarantee.
        /// </remarks>
        AllowsUninitializedMemory = 1u << 7,

        /// <summary>
        /// The Field may alias externally owned storage.
        /// </summary>
        /// <remarks>
        /// Aliased Fields require stricter ownership and lifetime validation. Atlas must
        /// not dispose memory it does not own.
        /// </remarks>
        AllowsExternalAlias = 1u << 8,

        /// <summary>
        /// The Field is intended for diagnostics or tooling rather than simulation-critical work.
        /// </summary>
        /// <remarks>
        /// Diagnostic Fields may be stripped, disabled, or excluded from deterministic
        /// compatibility checks depending on package configuration.
        /// </remarks>
        Diagnostic = 1u << 9
    }

    /// <summary>
    /// Provides allocation-free helpers for working with <see cref="AtlasFieldFlags"/>.
    /// </summary>
    /// <remarks>
    /// Use these helpers instead of <see cref="Enum.HasFlag(Enum)"/> in performance-sensitive
    /// code. They avoid boxing and are suitable for Burst-compatible call sites when used with
    /// unmanaged data.
    /// </remarks>
    public static class AtlasFieldFlagsExtensions
    {
        /// <summary>
        /// Determines whether all requested flags are present.
        /// </summary>
        /// <param name="value">The flag set to inspect.</param>
        /// <param name="flags">The flags that must all be present.</param>
        /// <returns><c>true</c> when all requested flags are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this AtlasFieldFlags value, AtlasFieldFlags flags)
        {
            return (value & flags) == flags;
        }

        /// <summary>
        /// Determines whether at least one requested flag is present.
        /// </summary>
        /// <param name="value">The flag set to inspect.</param>
        /// <param name="flags">The flags where any match is accepted.</param>
        /// <returns><c>true</c> when at least one requested flag is present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this AtlasFieldFlags value, AtlasFieldFlags flags)
        {
            return (value & flags) != 0;
        }

        /// <summary>
        /// Determines whether none of the requested flags are present.
        /// </summary>
        /// <param name="value">The flag set to inspect.</param>
        /// <param name="flags">The flags that must all be absent.</param>
        /// <returns><c>true</c> when none of the requested flags are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(this AtlasFieldFlags value, AtlasFieldFlags flags)
        {
            return (value & flags) == 0;
        }
    }
}