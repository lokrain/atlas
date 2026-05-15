// Packages/com.lokrain.atlas/Runtime/Operations/AtlasOperationAccess.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Describe symbolic field-access requirements for Atlas operation definitions.
// - Preserve zero-valid StableDataId semantics.
// - Keep access declarations separate from compiled memory bindings.
// - Validate operation-local access-mode, access-flag, and write-coverage consistency.
//
// Design notes
// - default(AtlasOperationAccess) is a valid value object, but it is not a concrete access declaration.
// - StableDataId default/zero is valid.
// - Missing lookup state must be represented by bool-returning APIs or explicit presence flags.
// - AtlasOperationAccessMode.None is valid as a default enum value, but not valid for concrete declarations.
// - BindingName is diagnostic/ABI metadata, not dispatch identity.
// - DiscardBeforeWrite does not prove full overwrite. WriteCoverage owns that proof.
// - Jobs must not receive this type. Jobs should receive compiled addresses, typed slices/views,
//   or resolved native containers.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Describes one symbolic field binding required by an Atlas operation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasOperationAccess :
        IEquatable<AtlasOperationAccess>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Compatibility alias for the default operation access payload.
        /// </summary>
        public static readonly AtlasOperationAccess Empty = default;

        /// <summary>
        /// Compatibility alias for the default operation access payload.
        /// </summary>
        public static readonly AtlasOperationAccess Invalid = default;

        /// <summary>
        /// Stable identity of the field accessed by the operation.
        /// </summary>
        public readonly StableDataId FieldId;

        /// <summary>
        /// Declared access mode for the field.
        /// </summary>
        public readonly AtlasOperationAccessMode Mode;

        /// <summary>
        /// Operation-specific access flags.
        /// </summary>
        public readonly AtlasOperationAccessFlags Flags;

        /// <summary>
        /// Declared write coverage for this binding.
        /// </summary>
        /// <remarks>
        /// This value tells validators whether a write produces full logical content, partial
        /// content, appended records, consumed records, or externally fenced content.
        /// </remarks>
        public readonly AtlasWriteCoverage WriteCoverage;

        /// <summary>
        /// Stable diagnostic binding name used by validation reports, tooling, and tests.
        /// </summary>
        public readonly FixedString64Bytes BindingName;

        /// <summary>
        /// Creates an operation access declaration from explicit fields.
        /// </summary>
        public AtlasOperationAccess(
            StableDataId fieldId,
            AtlasOperationAccessMode mode,
            AtlasOperationAccessFlags flags,
            AtlasWriteCoverage writeCoverage,
            FixedString64Bytes bindingName)
        {
            FieldId = fieldId;
            Mode = mode;
            Flags = flags;
            WriteCoverage = writeCoverage;
            BindingName = bindingName;
        }

        /// <summary>
        /// Gets whether this access declaration is semantically valid for a concrete operation definition.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mode != AtlasOperationAccessMode.None &&
                   !BindingName.IsEmpty &&
                   IsFlagCombinationValid() &&
                   IsWriteCoverageCombinationValid();
        }

        /// <summary>
        /// Gets whether this value is structurally invalid.
        /// </summary>
        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        /// <summary>
        /// Gets whether this declaration requires field contents to be read.
        /// </summary>
        public bool ReadsContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsShapeOnly &&
                   Mode.Reads();
        }

        /// <summary>
        /// Gets whether this declaration writes field contents or mutates container state.
        /// </summary>
        public bool WritesContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsShapeOnly &&
                   Mode.Writes();
        }

        /// <summary>
        /// Gets whether this declaration proves full logical content after execution.
        /// </summary>
        public bool WritesFullLogicalContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WritesContent &&
                   WriteCoverage.MakesFullLogicalContentAvailable();
        }

        /// <summary>
        /// Gets whether this declaration writes only partial logical content or appended records.
        /// </summary>
        public bool WritesPartialContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WritesContent &&
                   WriteCoverage.IsPartialContentWrite();
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
        /// Gets whether this declaration requires deterministic ordering for records.
        /// </summary>
        public bool RequiresDeterministicOrder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasOperationAccessFlags.RequiresDeterministicOrder);
        }

        /// <summary>
        /// Gets whether this declaration requires exclusive write authority.
        /// </summary>
        public bool RequiresExclusiveWrite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite);
        }

        /// <summary>
        /// Creates a read access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess Read<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Read,
                AtlasOperationAccessFlags.None,
                AtlasWriteCoverage.None,
                bindingName);
        }

        /// <summary>
        /// Creates a full-overwrite access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess WriteFull<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Write,
                AtlasOperationAccessFlags.DiscardBeforeWrite,
                AtlasWriteCoverage.FullLogicalLength,
                bindingName);
        }

        /// <summary>
        /// Creates a capacity-clearing write access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess WriteFullCapacity<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Write,
                AtlasOperationAccessFlags.DiscardBeforeWrite,
                AtlasWriteCoverage.FullCapacity,
                bindingName);
        }

        /// <summary>
        /// Creates a partial write access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess WritePartial<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Write,
                AtlasOperationAccessFlags.DiscardBeforeWrite,
                AtlasWriteCoverage.PartialLogicalLength,
                bindingName);
        }

        /// <summary>
        /// Creates a sparse indexed write access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess WriteSparse<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Write,
                AtlasOperationAccessFlags.DiscardBeforeWrite,
                AtlasWriteCoverage.SparseIndexed,
                bindingName);
        }

        /// <summary>
        /// Creates a read-write access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess ReadWrite<TField, TElement>(
            AtlasWriteCoverage writeCoverage,
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.ReadWrite,
                AtlasOperationAccessFlags.PreserveExistingContent,
                writeCoverage,
                bindingName);
        }

        /// <summary>
        /// Creates an append access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess Append<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Append,
                AtlasOperationAccessFlags.RequiresDeterministicOrder,
                AtlasWriteCoverage.AppendRecords,
                bindingName);
        }

        /// <summary>
        /// Creates a consume access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess Consume<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Consume,
                AtlasOperationAccessFlags.RequiresDeterministicOrder,
                AtlasWriteCoverage.ConsumeRecords,
                bindingName);
        }

        /// <summary>
        /// Creates a shape-only access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess ShapeOnly<TField, TElement>(
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Create<TField, TElement>(
                AtlasOperationAccessMode.Read,
                AtlasOperationAccessFlags.ShapeOnly,
                AtlasWriteCoverage.None,
                bindingName);
        }

        /// <summary>
        /// Creates an operation access declaration for a typed field.
        /// </summary>
        public static AtlasOperationAccess Create<TField, TElement>(
            AtlasOperationAccessMode mode,
            AtlasOperationAccessFlags flags,
            AtlasWriteCoverage writeCoverage,
            FixedString64Bytes bindingName = default)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            AtlasField.ValidateDeclarationOrThrow<TField, TElement>();

            var fieldName = AtlasField.DebugName<TField, TElement>();
            var resolvedBindingName = bindingName.IsEmpty ? fieldName : bindingName;

            var access = new AtlasOperationAccess(
                AtlasField.StableId<TField, TElement>(),
                mode,
                flags,
                writeCoverage,
                resolvedBindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates an operation access declaration from an explicit field identifier.
        /// </summary>
        public static AtlasOperationAccess Create(
            StableDataId fieldId,
            AtlasOperationAccessMode mode,
            AtlasOperationAccessFlags flags,
            AtlasWriteCoverage writeCoverage,
            FixedString64Bytes bindingName)
        {
            var access = new AtlasOperationAccess(
                fieldId,
                mode,
                flags,
                writeCoverage,
                bindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration with additional access flags.
        /// </summary>
        public AtlasOperationAccess WithFlags(AtlasOperationAccessFlags flags)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags | flags,
                WriteCoverage,
                BindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration without the supplied access flags.
        /// </summary>
        public AtlasOperationAccess WithoutFlags(AtlasOperationAccessFlags flags)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags & ~flags,
                WriteCoverage,
                BindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration with a different access mode.
        /// </summary>
        public AtlasOperationAccess WithMode(
            AtlasOperationAccessMode mode,
            AtlasWriteCoverage writeCoverage)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                mode,
                Flags,
                writeCoverage,
                BindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration with a different write coverage.
        /// </summary>
        public AtlasOperationAccess WithWriteCoverage(AtlasWriteCoverage writeCoverage)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags,
                writeCoverage,
                BindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Creates a copy of this declaration with a different binding name.
        /// </summary>
        public AtlasOperationAccess WithBindingName(FixedString64Bytes bindingName)
        {
            var access = new AtlasOperationAccess(
                FieldId,
                Mode,
                Flags,
                WriteCoverage,
                bindingName);

            access.ValidateOrThrow(nameof(access));
            return access;
        }

        /// <summary>
        /// Throws when this access declaration is not valid as a concrete operation binding declaration.
        /// </summary>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasOperationAccess);

            if (Mode == AtlasOperationAccessMode.None)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' has no concrete access mode.",
                    name);
            }

            if (BindingName.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas operation access has an empty binding name.",
                    name);
            }

            ValidateFlagCombinationOrThrow(name);
            ValidateWriteCoverageCombinationOrThrow(name);
        }

        /// <summary>
        /// Determines whether this declaration is equal to another declaration.
        /// </summary>
        public bool Equals(AtlasOperationAccess other)
        {
            return FieldId == other.FieldId &&
                   Mode == other.Mode &&
                   Flags == other.Flags &&
                   WriteCoverage == other.WriteCoverage &&
                   BindingName.Equals(other.BindingName);
        }

        /// <summary>
        /// Determines whether this declaration is equal to an object instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasOperationAccess other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this declaration.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ FieldId.GetHashCode();
                hash = (hash * HashMultiplier) ^ (int)Mode;
                hash = (hash * HashMultiplier) ^ (int)Flags;
                hash = (hash * HashMultiplier) ^ (int)WriteCoverage;
                hash = (hash * HashMultiplier) ^ BindingName.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this operation access declaration.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} Mode={1} Flags={2} Coverage={3} Field={4}",
                BindingName,
                Mode,
                Flags,
                WriteCoverage,
                FieldId);
        }

        private bool IsFlagCombinationValid()
        {
            if (Flags.HasAny(AtlasOperationAccessFlags.ShapeOnly) &&
                Mode != AtlasOperationAccessMode.Read)
            {
                return false;
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite) &&
                !Mode.Writes())
            {
                return false;
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent) &&
                !Mode.Reads())
            {
                return false;
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.AllowsParallelWrite) &&
                !Mode.Writes())
            {
                return false;
            }

            if (Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite) &&
                !Mode.Writes())
            {
                return false;
            }

            if (Flags.HasAll(
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.PreserveExistingContent))
            {
                return false;
            }

            if (Mode == AtlasOperationAccessMode.Append &&
                Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                return false;
            }

            return true;
        }

        private bool IsWriteCoverageCombinationValid()
        {
            if (IsShapeOnly)
            {
                return WriteCoverage == AtlasWriteCoverage.None;
            }

            if (!Mode.Writes())
            {
                return WriteCoverage == AtlasWriteCoverage.None;
            }

            if (Mode == AtlasOperationAccessMode.Append)
            {
                return WriteCoverage == AtlasWriteCoverage.AppendRecords;
            }

            if (Mode == AtlasOperationAccessMode.Consume)
            {
                return WriteCoverage == AtlasWriteCoverage.ConsumeRecords;
            }

            if (WriteCoverage == AtlasWriteCoverage.None ||
                WriteCoverage == AtlasWriteCoverage.AppendRecords ||
                WriteCoverage == AtlasWriteCoverage.ConsumeRecords)
            {
                return false;
            }

            if (Mode == AtlasOperationAccessMode.Write &&
                Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                return false;
            }

            if (Mode == AtlasOperationAccessMode.ReadWrite &&
                !Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                return false;
            }

            return true;
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
                    $"Atlas operation access '{BindingName}' appends to a field but also preserves existing content. " +
                    "Use read-write access if previous contents are semantically read.",
                    parameterName);
            }
        }

        private void ValidateWriteCoverageCombinationOrThrow(string parameterName)
        {
            if (IsShapeOnly && WriteCoverage != AtlasWriteCoverage.None)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' declares shape-only access with write coverage '{WriteCoverage}'.",
                    parameterName);
            }

            if (!Mode.Writes() && WriteCoverage != AtlasWriteCoverage.None)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' declares write coverage '{WriteCoverage}' without write-capable access mode.",
                    parameterName);
            }

            if (Mode == AtlasOperationAccessMode.Append &&
                WriteCoverage != AtlasWriteCoverage.AppendRecords)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' uses append mode but declares write coverage '{WriteCoverage}'.",
                    parameterName);
            }

            if (Mode == AtlasOperationAccessMode.Consume &&
                WriteCoverage != AtlasWriteCoverage.ConsumeRecords)
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' uses consume mode but declares write coverage '{WriteCoverage}'.",
                    parameterName);
            }

            if (Mode == AtlasOperationAccessMode.Write ||
                Mode == AtlasOperationAccessMode.ReadWrite)
            {
                if (WriteCoverage == AtlasWriteCoverage.None ||
                    WriteCoverage == AtlasWriteCoverage.AppendRecords ||
                    WriteCoverage == AtlasWriteCoverage.ConsumeRecords)
                {
                    throw new ArgumentException(
                        $"Atlas operation access '{BindingName}' uses mode '{Mode}' with invalid write coverage '{WriteCoverage}'.",
                        parameterName);
                }
            }

            if (Mode == AtlasOperationAccessMode.Write &&
                WriteCoverage.MakesFullLogicalContentAvailable() &&
                !Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite))
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' fully writes content without declaring discard-before-write.",
                    parameterName);
            }

            if (Mode == AtlasOperationAccessMode.ReadWrite &&
                !Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
            {
                throw new ArgumentException(
                    $"Atlas operation access '{BindingName}' uses read-write mode without declaring preserve-existing-content.",
                    parameterName);
            }
        }

        public static bool operator ==(AtlasOperationAccess left, AtlasOperationAccess right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AtlasOperationAccess left, AtlasOperationAccess right)
        {
            return !left.Equals(right);
        }
    }
}