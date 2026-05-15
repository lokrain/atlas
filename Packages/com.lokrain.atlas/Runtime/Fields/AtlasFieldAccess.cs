// Runtime/Fields/AtlasFieldAccess.cs

using System;
using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Defines how a plan or binding intends to access an Atlas Field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Field access is plan-specific. It should not be confused with
    /// <see cref="AtlasFieldFlags"/>, which describes durable Field behavior.
    /// </para>
    ///
    /// <para>
    /// Access declarations are used by validators to detect missing dependencies,
    /// unsafe read/write combinations, illegal parallel writes, invalid discard usage,
    /// and lifetime conflicts before jobs are scheduled.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasFieldAccess : byte
    {
        /// <summary>
        /// No access is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for a required Field binding. It is reserved for
        /// default initialization, optional binding resolution, and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The plan may read existing Field contents.
        /// </summary>
        /// <remarks>
        /// Read access requires the Field contents to be initialized and valid for the
        /// plan's execution lifetime.
        /// </remarks>
        Read = 1 << 0,

        /// <summary>
        /// The plan may write Field contents.
        /// </summary>
        /// <remarks>
        /// Write access requires exclusive access unless the plan declares a write pattern
        /// that is compatible with the Field storage format and Field flags.
        /// </remarks>
        Write = 1 << 1,

        /// <summary>
        /// The plan writes every element before any read is allowed.
        /// </summary>
        /// <remarks>
        /// Discard-write access allows validators and storage allocators to treat previous
        /// contents as undefined. This access is appropriate for full-output buffers where
        /// the job overwrites the complete logical range.
        /// </remarks>
        DiscardWrite = 1 << 2,

        /// <summary>
        /// The plan may resize the Field.
        /// </summary>
        /// <remarks>
        /// Resize access is only legal for storage formats and Field declarations that
        /// explicitly support resizing. Resizing must not occur concurrently with readers
        /// or writers that assume a stable length.
        /// </remarks>
        Resize = 1 << 3,

        /// <summary>
        /// The plan may write from parallel worker executions.
        /// </summary>
        /// <remarks>
        /// Parallel-write access does not by itself make writes safe. The plan must still
        /// use a non-overlapping index partition, append-only writer, atomic primitive,
        /// or another explicitly validated synchronization strategy.
        /// </remarks>
        ParallelWrite = 1 << 4,

        /// <summary>
        /// The plan may read and write Field contents.
        /// </summary>
        /// <remarks>
        /// Read-write access requires existing contents to be initialized before execution
        /// and requires exclusive access unless a more specific safe parallel pattern is declared.
        /// </remarks>
        ReadWrite = Read | Write
    }

    /// <summary>
    /// Provides allocation-free helpers for working with <see cref="AtlasFieldAccess"/>.
    /// </summary>
    /// <remarks>
    /// These helpers avoid <see cref="Enum.HasFlag(Enum)"/> so access checks can remain
    /// allocation-free and suitable for validation paths that may execute frequently.
    /// </remarks>
    public static class AtlasFieldAccessExtensions
    {
        /// <summary>
        /// Determines whether all requested access flags are present.
        /// </summary>
        /// <param name="value">The access set to inspect.</param>
        /// <param name="access">The access flags that must all be present.</param>
        /// <returns><c>true</c> when all requested flags are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this AtlasFieldAccess value, AtlasFieldAccess access)
        {
            return (value & access) == access;
        }

        /// <summary>
        /// Determines whether at least one requested access flag is present.
        /// </summary>
        /// <param name="value">The access set to inspect.</param>
        /// <param name="access">The access flags where any match is accepted.</param>
        /// <returns><c>true</c> when at least one requested flag is present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this AtlasFieldAccess value, AtlasFieldAccess access)
        {
            return (value & access) != 0;
        }

        /// <summary>
        /// Determines whether none of the requested access flags are present.
        /// </summary>
        /// <param name="value">The access set to inspect.</param>
        /// <param name="access">The access flags that must all be absent.</param>
        /// <returns><c>true</c> when none of the requested flags are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(this AtlasFieldAccess value, AtlasFieldAccess access)
        {
            return (value & access) == 0;
        }

        /// <summary>
        /// Gets whether this access declaration can observe existing Field contents.
        /// </summary>
        /// <param name="value">The access declaration to inspect.</param>
        /// <returns><c>true</c> when the declaration includes read access; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanRead(this AtlasFieldAccess value)
        {
            return value.HasAny(AtlasFieldAccess.Read);
        }

        /// <summary>
        /// Gets whether this access declaration can modify Field contents.
        /// </summary>
        /// <param name="value">The access declaration to inspect.</param>
        /// <returns><c>true</c> when the declaration includes write access; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanWrite(this AtlasFieldAccess value)
        {
            return value.HasAny(AtlasFieldAccess.Write | AtlasFieldAccess.DiscardWrite);
        }

        /// <summary>
        /// Gets whether this access declaration requires initialized Field contents.
        /// </summary>
        /// <param name="value">The access declaration to inspect.</param>
        /// <returns>
        /// <c>true</c> when existing contents may be read before being overwritten;
        /// otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RequiresInitializedContent(this AtlasFieldAccess value)
        {
            return value.HasAny(AtlasFieldAccess.Read) &&
                   value.HasNone(AtlasFieldAccess.DiscardWrite);
        }

        /// <summary>
        /// Gets whether this access declaration requires exclusive mutable access.
        /// </summary>
        /// <param name="value">The access declaration to inspect.</param>
        /// <returns>
        /// <c>true</c> when the access writes without declaring a compatible parallel-write mode;
        /// otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RequiresExclusiveWrite(this AtlasFieldAccess value)
        {
            return value.CanWrite() &&
                   value.HasNone(AtlasFieldAccess.ParallelWrite);
        }
    }
}