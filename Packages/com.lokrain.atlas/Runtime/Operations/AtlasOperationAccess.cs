// Runtime/Operations/AtlasOperationAccess.cs

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines the kind of Field access required by an Atlas operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operation access mode describes how an operation intends to use a Field after compilation.
    /// It is validated against the Field Contract, operation ordering, stage boundaries, storage
    /// kind, lifetime, ownership, and dependency graph before jobs are scheduled.
    /// </para>
    ///
    /// <para>
    /// This enum is not a runtime memory container. Jobs receive concrete resolved memory, not
    /// symbolic operation access declarations.
    /// </para>
    /// </remarks>
    public enum AtlasOperationAccessMode : byte
    {
        /// <summary>
        /// No access mode is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for concrete operation access declarations and is reserved for
        /// default initialization and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The operation reads Field contents without writing them.
        /// </summary>
        Read = 1,

        /// <summary>
        /// The operation writes Field contents without depending on previous contents.
        /// </summary>
        /// <remarks>
        /// Validators should require either clear-before-write, discard-before-write, full coverage,
        /// or another explicit proof that stale content cannot affect correctness.
        /// </remarks>
        Write = 2,

        /// <summary>
        /// The operation reads existing Field contents and writes updated contents.
        /// </summary>
        ReadWrite = 3,

        /// <summary>
        /// The operation appends produced records to a variable-length Field.
        /// </summary>
        /// <remarks>
        /// Append access is appropriate for streams, queues, lists, event buffers, record payloads,
        /// and producer-side variable output. Validators must verify storage support and parallel
        /// writer safety before scheduling.
        /// </remarks>
        Append = 4,

        /// <summary>
        /// The operation consumes records from a producer-consumer Field.
        /// </summary>
        /// <remarks>
        /// Consume access is appropriate for queues, streams, command buffers, and explicit
        /// producer-consumer pipelines. Consumption may mutate container state even when the
        /// operation's logical purpose is to read produced records.
        /// </remarks>
        Consume = 5
    }

    /// <summary>
    /// Defines operation-specific access requirements for a Field binding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags describe how a particular operation uses a Field. They are validated against
    /// the Field's own flags and Contract. Field flags declare what is allowed by the catalog;
    /// operation access flags declare what one operation actually requires.
    /// </para>
    /// </remarks>
    [Flags]
    public enum AtlasOperationAccessFlags : ushort
    {
        /// <summary>
        /// No operation access flags are declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operation can run when the Field binding is absent.
        /// </summary>
        /// <remarks>
        /// Optional access is valid only when the compiled plan and operation implementation have
        /// an explicit fallback path. Validators should reject optional access to non-optional
        /// Fields unless the pipeline profile explicitly permits it.
        /// </remarks>
        Optional = 1 << 0,

        /// <summary>
        /// The operation needs only resolved shape metadata and does not access Field contents.
        /// </summary>
        /// <remarks>
        /// Shape-only access is useful when an operation needs length, capacity, partition count,
        /// or presence information but not the memory payload.
        /// </remarks>
        ShapeOnly = 1 << 1,

        /// <summary>
        /// The operation discards previous contents before writing.
        /// </summary>
        /// <remarks>
        /// This flag is operation-local. It does not imply that storage is cleared to zero; it
        /// means previous contents are not semantically read by the operation.
        /// </remarks>
        DiscardBeforeWrite = 1 << 2,

        /// <summary>
        /// The operation requires previous contents to remain valid before it runs.
        /// </summary>
        /// <remarks>
        /// This is relevant for read-write updates, incremental solvers, cached payloads, and
        /// persistent working sets.
        /// </remarks>
        PreserveExistingContent = 1 << 3,

        /// <summary>
        /// The operation may write to the Field from parallel workers.
        /// </summary>
        /// <remarks>
        /// This declares an operation requirement only. The validator must still verify that the
        /// Field Contract, storage kind, write pattern, dependency graph, and container API support
        /// safe parallel writes.
        /// </remarks>
        AllowsParallelWrite = 1 << 4,

        /// <summary>
        /// The operation requires deterministic output order for this Field.
        /// </summary>
        /// <remarks>
        /// This is relevant for append output, streams, queues, payload records, artifact hashes,
        /// replay validation, rollback, and deterministic tests.
        /// </remarks>
        RequiresDeterministicOrder = 1 << 5,

        /// <summary>
        /// The operation requires exclusive write authority for this Field during execution.
        /// </summary>
        /// <remarks>
        /// Validators should reject overlapping writes, aliasing writes, and incompatible parallel
        /// writer usage when this flag is present.
        /// </remarks>
        RequiresExclusiveWrite = 1 << 6
    }

    /// <summary>
    /// Describes one symbolic Field binding required by an Atlas operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operation access declarations are the contract between operation definitions and the
    /// compiler. They name which Fields an operation reads, writes, appends to, consumes from,
    /// or uses for shape-only metadata.
    /// </para>
    ///
    /// <para>
    /// Access declarations are symbolic. The compiler resolves <see cref="FieldId"/> through the
    /// ordered Contract table and produces concrete memory bindings for the executor. Jobs should
    /// never receive this type.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasOperationAccess :
        IEquatable<AtlasOperationAccess>
    {
        /// <summary>
        /// Reserved invalid operation access declaration.
        /// </summary>
        public static readonly AtlasOperationAccess Empty = default;

        /// <summary>
        /// Stable identity of the Field accessed by the operation.
        /// </summary>
        public readonly StableDataId FieldId;

        /// <summary>
        /// Declared access mode for the Field.
        /// </summary>
        public readonly AtlasOperationAccessMode Mode;

        /// <summary>
        /// Operation-specific access flags.
        /// </summary>
        public readonly AtlasOperationAccessFlags Flags;

        /// <summary>
        /// Stable diagnostic binding name used by validation reports, tooling, and tests.
        /// </summary>
        /// <remarks>
        /// Binding names are not durable identity. The accessed Field is identified by
        /// <see cref="FieldId"/>.
        /// </remarks>
        public readonly FixedString64Bytes BindingName;

        /// <summary>
        /// Creates an operation access declaration from explicit fields.
        /// </summary>
        /// <param name="fieldId">Stable identity of the accessed Field.</param>
        /// <param name="mode">Access mode required by the operation.</param>
        /// <param name="flags">Operation-specific access flags.</param>
        /// <param name="bindingName">Diagnostic binding name.</param>
        public AtlasOperationAccess(
            StableDataId fieldId,
            AtlasOperationAccessMode mode,
            AtlasOperationAccessFlags flags,
            FixedString64Bytes bindingName)
        {
            FieldId = fieldId;
            Mode = mode;
            Flags = flags;
            BindingName = bindingName;
        }

        /// <summary>
        /// Gets whether this access declaration is valid for a concrete operation definition.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FieldId.IsValid &&
                   Mode != AtlasOperationAccessMode.None &&
                   !BindingName.IsEmpty;
        }

        /// <summary>
        /// Gets whether this declaration requires Field contents to be read.
        /// </summary>
        public bool ReadsContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !Flags.HasAny(AtlasOperationAccessFlags.ShapeOnly) &&
                   (Mode == AtlasOperationAccessMode.Read ||
                    Mode == AtlasOperationAccessMode.ReadWrite ||
                    Mode == AtlasOperationAccessMode.Consume);
        }

        /// <summary>
        /// Gets whether this declaration requires Field contents or container state to be written.
        /// </summary>
        public bool WritesContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !Flags.HasAny(AtlasOperationAccessFlags.ShapeOnly) &&
                   (Mode == AtlasOperationAccessMode.Write ||
                    Mode == AtlasOperationAccessMode.ReadWrite ||
                    Mode == AtlasOperationAccessMode.Append ||
                    Mode == AtlasOperationAccessMode.Consume);
        }

        /// <summary>
        /// Gets whether this declaration uses only resolved shape metadata.
        /// </summary>
        public bool IsShapeOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasOperationAccessFlags.ShapeOnly);
        }

        /// <summary>
        /// Gets whether this declaration may be absent from the compiled plan.
        /// </summary>
        public bool IsOptional
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasOperationAccessFlags.Optional);
        }

        /// <summary>
        /// Creates a read access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess Read<TField, TElement>(FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Read,
                AtlasOperationAccessFlags.None,
                bindingName);
        }

        /// <summary>
        /// Creates a write access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess Write<TField, TElement>(FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Write,
                AtlasOperationAccessFlags.DiscardBeforeWrite | AtlasOperationAccessFlags.RequiresExclusiveWrite,
                bindingName);
        }

        /// <summary>
        /// Creates a read-write access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess ReadWrite<TField, TElement>(FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.ReadWrite,
                AtlasOperationAccessFlags.PreserveExistingContent | AtlasOperationAccessFlags.RequiresExclusiveWrite,
                bindingName);
        }

        /// <summary>
        /// Creates an append access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess Append<TField, TElement>(FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Append,
                AtlasOperationAccessFlags.AllowsParallelWrite,
                bindingName);
        }

        /// <summary>
        /// Creates a consume access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess Consume<TField, TElement>(FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Consume,
                AtlasOperationAccessFlags.RequiresExclusiveWrite,
                bindingName);
        }

        /// <summary>
        /// Creates a shape-only access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess ShapeOnly<TField, TElement>(FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Read,
                AtlasOperationAccessFlags.ShapeOnly,
                bindingName);
        }

        /// <summary>
        /// Creates an operation access declaration for a typed Field.
        /// </summary>
        public static AtlasOperationAccess Create<TField, TElement>(
            AtlasOperationAccessMode mode,
            AtlasOperationAccessFlags flags,
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            var resolvedBindingName = bindingName.IsEmpty
                ? AtlasField.DebugName<TField, TElement>()
                : bindingName;

            var access = new AtlasOperationAccess(
                AtlasField.StableId<TField, TElement>(),
                mode,
                flags,
                resolvedBindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates an operation access declaration from an explicit Field identifier.
        /// </summary>
        /// <param name="fieldId">Stable identity of the accessed Field.</param>
        /// <param name="mode">Access mode required by the operation.</param>
        /// <param name="flags">Operation-specific access flags.</param>
        /// <param name="bindingName">Diagnostic binding name.</param>
        /// <returns>A validated operation access declaration.</returns>
        public static AtlasOperationAccess Create(
            StableDataId fieldId,
            AtlasOperationAccessMode mode,
            AtlasOperationAccessFlags flags,
            FixedString64Bytes bindingName)
        {
            var access = new AtlasOperationAccess(
                fieldId,
                mode,
                flags,
                bindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration with additional access flags.
        /// </summary>
        /// <param name="flags">Flags to add.</param>
        /// <returns>A validated access declaration with the additional flags applied.</returns>
        public AtlasOperationAccess WithFlags(AtlasOperationAccessFlags flags)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags | flags,
                BindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration without the supplied access flags.
        /// </summary>
        /// <param name="flags">Flags to remove.</param>
        /// <returns>A validated access declaration with the supplied flags removed.</returns>
        public AtlasOperationAccess WithoutFlags(AtlasOperationAccessFlags flags)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags & ~flags,
                BindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration with a different binding name.
        /// </summary>
        /// <param name="bindingName">New diagnostic binding name.</param>
        /// <returns>A validated access declaration with the supplied binding name.</returns>
        public AtlasOperationAccess WithBindingName(FixedString64Bytes bindingName)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags,
                bindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Throws when this access declaration is invalid or internally inconsistent.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when this declaration has invalid Field identity, access mode, binding name, or flag combination.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasOperationAccess);

            if (!FieldId.IsValid)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' has an invalid Field id.",
                    name);
            }

            if (Mode == AtlasOperationAccessMode.None)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' has no access mode.",
                    name);
            }

            if (BindingName.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas operation access has an empty binding name.",
                    name);
            }

            ValidateFlagCombinationOrThrow(name);
        }

        /// <summary>
        /// Determines whether this declaration is equal to another declaration.
        /// </summary>
        /// <param name="other">The declaration to compare with this declaration.</param>
        /// <returns><c>true</c> when all declaration fields match; otherwise, <c>false</c>.</returns>
        public bool Equals(AtlasOperationAccess other)
        {
            return FieldId == other.FieldId &&
                   Mode == other.Mode &&
                   Flags == other.Flags &&
                   BindingName.Equals(other.BindingName);
        }

        /// <summary>
        /// Determines whether this declaration is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this declaration.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal <see cref="AtlasOperationAccess"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasOperationAccess other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code derived from Field id, mode, flags, and binding name.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = FieldId.GetHashCode();
                hash = (hash * 397) ^ (int)Mode;
                hash = (hash * 397) ^ (int)Flags;
                hash = (hash * 397) ^ BindingName.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this operation access declaration.
        /// </summary>
        /// <returns>A string containing binding name, access mode, flags, and Field id.</returns>
        public override string ToString()
        {
            return $"{BindingName} Mode={Mode} Flags={Flags} Field={FieldId}";
        }

        /// <summary>
        /// Determines whether two operation access declarations are equal.
        /// </summary>
        public static bool operator ==(AtlasOperationAccess left, AtlasOperationAccess right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two operation access declarations are not equal.
        /// </summary>
        public static bool operator !=(AtlasOperationAccess left, AtlasOperationAccess right)
        {
            return !left.Equals(right);
        }

        private void ValidateFlagCombinationOrThrow(string parameterName)
        {
            if (Flags.HasAny(AtlasOperationAccessFlags.ShapeOnly) &&
                Mode != AtlasOperationAccessMode.Read)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' declares shape-only access with non-read mode '{Mode}'.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite) &&
                !Mode.Writes())
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' declares discard-before-write without write access.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent) &&
                !Mode.Reads())
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' preserves existing content without read access.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.AllowsParallelWrite) &&
                !Mode.Writes())
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' allows parallel write without write access.",
                    parameterName);
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite) &&
                !Mode.Writes())
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' requires exclusive write without write access.",
                    parameterName);
            }

            if (Flags.HasAll(
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.PreserveExistingContent))
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' declares both discard-before-write and preserve-existing-content.",
                    parameterName);
            }

            if (Mode == AtlasOperationAccessMode.Append &&
                Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' appends to a Field but also preserves existing content. " +
                    "Use read-write access if previous contents are semantically read.",
                    parameterName);
            }
        }
    }

    /// <summary>
    /// Provides allocation-free helpers for <see cref="AtlasOperationAccessMode"/>.
    /// </summary>
    public static class AtlasOperationAccessModeExtensions
    {
        /// <summary>
        /// Determines whether an access mode reads Field contents or consumes container state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Reads(this AtlasOperationAccessMode mode)
        {
            return mode == AtlasOperationAccessMode.Read ||
                   mode == AtlasOperationAccessMode.ReadWrite ||
                   mode == AtlasOperationAccessMode.Consume;
        }

        /// <summary>
        /// Determines whether an access mode writes Field contents or mutates container state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Writes(this AtlasOperationAccessMode mode)
        {
            return mode == AtlasOperationAccessMode.Write ||
                   mode == AtlasOperationAccessMode.ReadWrite ||
                   mode == AtlasOperationAccessMode.Append ||
                   mode == AtlasOperationAccessMode.Consume;
        }

        /// <summary>
        /// Determines whether an access mode is valid for a concrete operation access declaration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this AtlasOperationAccessMode mode)
        {
            return mode != AtlasOperationAccessMode.None;
        }
    }

    /// <summary>
    /// Provides allocation-free helpers for <see cref="AtlasOperationAccessFlags"/>.
    /// </summary>
    public static class AtlasOperationAccessFlagsExtensions
    {
        /// <summary>
        /// Determines whether all requested flags are present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this AtlasOperationAccessFlags value, AtlasOperationAccessFlags flags)
        {
            return (value & flags) == flags;
        }

        /// <summary>
        /// Determines whether at least one requested flag is present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this AtlasOperationAccessFlags value, AtlasOperationAccessFlags flags)
        {
            return (value & flags) != 0;
        }

        /// <summary>
        /// Determines whether none of the requested flags are present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(this AtlasOperationAccessFlags value, AtlasOperationAccessFlags flags)
        {
            return (value & flags) == 0;
        }
    }
} 