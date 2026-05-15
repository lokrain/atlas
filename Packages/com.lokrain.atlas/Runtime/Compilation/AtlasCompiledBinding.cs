// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasCompiledBinding.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one operation access declaration after resolution against the field contract table.
// - Preserve operation-local binding order.
// - Bind symbolic operation access to either a concrete field contract or an explicit optional-missing binding.
// - Preserve declared write coverage for dataflow validation and execution policy.
// - Keep compilation output separate from runtime native memory.
//
// Design notes
// - This is compilation metadata, not a job payload.
// - Jobs should receive resolved native containers, numeric parameters, and scheduler-specific data.
// - Required operation access must resolve to a present contract.
// - Optional operation access may compile as missing while preserving the binding index.
// - Shape-only access may still resolve to a present contract because shape is contract/table-derived metadata.
// - Operation access order is significant and must not be sorted.
// - default(AtlasCompiledBinding) is a valid value object, but it is not a meaningful compiled binding.
// - Missing/present state is represented explicitly by IsPresent / IsMissingOptional.
// - AtlasContract.Empty/default is only an inert payload for missing optional bindings; the presence flag owns meaning.
// - AtlasFieldSlot default/zero is valid and must not be used as missing state.
// - StableDataId default/zero is valid and must not be used as missing state.
// - DiscardBeforeWrite does not prove full write coverage; WriteCoverage owns that proof.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Resolved binding for one operation access declaration.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasCompiledBinding :
        IEquatable<AtlasCompiledBinding>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        private const byte Missing = 0;
        private const byte Present = 1;

        /// <summary>
        /// Default compiled binding payload used by Try-style APIs when no binding is found.
        /// </summary>
        public static readonly AtlasCompiledBinding Empty = default;

        /// <summary>
        /// Compatibility alias for <see cref="Empty"/>.
        /// </summary>
        public static readonly AtlasCompiledBinding Invalid = default;

        private readonly byte _presence;

        /// <summary>
        /// Zero-based operation-local binding index.
        /// </summary>
        public readonly int BindingIndex;

        /// <summary>
        /// Original operation access declaration.
        /// </summary>
        public readonly AtlasOperationAccess Access;

        /// <summary>
        /// Resolved field contract when <see cref="IsPresent"/> is true.
        /// </summary>
        public readonly AtlasContract Contract;

        private AtlasCompiledBinding(
            int bindingIndex,
            AtlasOperationAccess access,
            AtlasContract contract,
            byte presence)
        {
            BindingIndex = bindingIndex;
            Access = access;
            Contract = contract;
            _presence = presence;
        }

        /// <summary>
        /// Gets whether this binding resolved to a concrete field contract.
        /// </summary>
        public bool IsPresent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _presence == Present;
        }

        /// <summary>
        /// Gets whether this binding represents an absent optional field binding.
        /// </summary>
        public bool IsMissingOptional
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _presence == Missing &&
                   Access.IsValid &&
                   Access.IsOptional;
        }

        /// <summary>
        /// Gets whether this binding is valid compiled metadata.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BindingIndex >= 0 &&
                   Access.IsValid &&
                   IsPresenceValid();
        }

        /// <summary>
        /// Gets whether this compiled binding is not semantically valid metadata.
        /// </summary>
        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsValid;
        }

        /// <summary>
        /// Gets the stable field identifier declared by the operation access.
        /// </summary>
        public StableDataId FieldId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.FieldId;
        }

        /// <summary>
        /// Gets the diagnostic binding name declared by the operation access.
        /// </summary>
        public FixedString64Bytes BindingName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.BindingName;
        }

        /// <summary>
        /// Gets the declared operation access mode.
        /// </summary>
        public AtlasOperationAccessMode Mode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.Mode;
        }

        /// <summary>
        /// Gets the declared operation access flags.
        /// </summary>
        public AtlasOperationAccessFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.Flags;
        }

        /// <summary>
        /// Gets the declared write coverage.
        /// </summary>
        public AtlasWriteCoverage WriteCoverage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.WriteCoverage;
        }

        /// <summary>
        /// Gets the resolved field slot when present, or the default slot payload when absent.
        /// </summary>
        public AtlasFieldSlot Slot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent
                ? Contract.Slot
                : default;
        }

        /// <summary>
        /// Gets whether the operation declaration reads field contents and the binding is present.
        /// </summary>
        public bool ReadsContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   Access.ReadsContent;
        }

        /// <summary>
        /// Gets whether the operation declaration writes, appends, consumes, or mutates field contents and the binding is present.
        /// </summary>
        public bool WritesContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   Access.WritesContent;
        }

        /// <summary>
        /// Gets whether this present binding proves full logical content after execution.
        /// </summary>
        public bool WritesFullLogicalContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   Access.WritesFullLogicalContent;
        }

        /// <summary>
        /// Gets whether this present binding writes partial logical content, sparse content, or appended records.
        /// </summary>
        public bool WritesPartialContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   Access.WritesPartialContent;
        }

        /// <summary>
        /// Gets whether the operation declaration reads field contents regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.ReadsContent;
        }

        /// <summary>
        /// Gets whether the operation declaration writes field contents regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentWrite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.WritesContent;
        }

        /// <summary>
        /// Gets whether the operation declaration proves full logical content regardless of optional resolution.
        /// </summary>
        public bool DeclaresFullLogicalWrite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.WritesFullLogicalContent;
        }

        /// <summary>
        /// Gets whether this binding requires only shape metadata when present.
        /// </summary>
        public bool IsShapeOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.IsShapeOnly;
        }

        /// <summary>
        /// Gets whether this binding requires content memory when present.
        /// </summary>
        public bool RequiresContentMemory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   !Access.IsShapeOnly;
        }

        /// <summary>
        /// Gets whether this binding declared optional access.
        /// </summary>
        public bool IsOptional
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.IsOptional;
        }

        /// <summary>
        /// Creates a present compiled binding from a resolved contract.
        /// </summary>
        public static AtlasCompiledBinding PresentBinding(
            int bindingIndex,
            AtlasOperationAccess access,
            AtlasContract contract)
        {
            var binding = new AtlasCompiledBinding(
                bindingIndex,
                access,
                contract,
                Present);

            binding.ValidateOrThrow(nameof(binding));
            return binding;
        }

        /// <summary>
        /// Creates a missing compiled binding for an absent optional access declaration.
        /// </summary>
        public static AtlasCompiledBinding MissingOptionalBinding(
            int bindingIndex,
            AtlasOperationAccess access)
        {
            var binding = new AtlasCompiledBinding(
                bindingIndex,
                access,
                default,
                Missing);

            binding.ValidateOrThrow(nameof(binding));
            return binding;
        }

        /// <summary>
        /// Resolves an operation access declaration against a contract table.
        /// </summary>
        public static AtlasCompiledBinding Resolve(
            int bindingIndex,
            AtlasOperationAccess access,
            AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            access.ValidateOrThrow(nameof(access));

            if (contracts.TryGetContract(access.FieldId, out var contract))
            {
                return PresentBinding(
                    bindingIndex,
                    access,
                    contract);
            }

            if (access.IsOptional)
            {
                return MissingOptionalBinding(
                    bindingIndex,
                    access);
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Required Atlas binding '{0}' could not resolve field id '{1}' in contract table '{2}'.",
                    access.BindingName,
                    access.FieldId,
                    GetDiagnosticName(contracts.Name)),
                nameof(access));
        }

        /// <summary>
        /// Throws when this compiled binding is invalid.
        /// </summary>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasCompiledBinding);

            if (BindingIndex < 0)
            {
                throw new ArgumentException(
                    "Atlas compiled binding must have a non-negative binding index.",
                    name);
            }

            Access.ValidateOrThrow($"{name}.{nameof(Access)}");

            if (_presence != Present && _presence != Missing)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas compiled binding '{0}' has invalid presence state '{1}'.",
                        Access.BindingName,
                        _presence),
                    name);
            }

            if (_presence == Missing)
            {
                if (!Access.IsOptional)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas compiled binding '{0}' is missing but its access declaration is not optional.",
                            Access.BindingName),
                        name);
                }

                return;
            }

            if (!Contract.IsTableReady)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas compiled binding '{0}' resolved to a contract that is not table-ready.",
                        Access.BindingName),
                    name);
            }

            if (Contract.StableId != Access.FieldId)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas compiled binding '{0}' targets field id '{1}' but resolved contract '{2}' has field id '{3}'.",
                        Access.BindingName,
                        Access.FieldId,
                        Contract.DebugName,
                        Contract.StableId),
                    name);
            }
        }

        /// <summary>
        /// Throws when this binding is not present.
        /// </summary>
        public void ThrowIfMissing(string parameterName = null)
        {
            _ = parameterName;

            if (IsPresent)
            {
                return;
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas compiled binding '{0}' is missing and cannot provide resolved contract data.",
                    BindingName));
        }

        /// <summary>
        /// Gets the resolved contract or throws when this binding is missing.
        /// </summary>
        public AtlasContract GetRequiredContract()
        {
            ThrowIfMissing(nameof(Contract));
            return Contract;
        }

        /// <summary>
        /// Determines whether this binding is equal to another binding.
        /// </summary>
        public bool Equals(AtlasCompiledBinding other)
        {
            return BindingIndex == other.BindingIndex &&
                   _presence == other._presence &&
                   Access.Equals(other.Access) &&
                   Contract.Equals(other.Contract);
        }

        /// <summary>
        /// Determines whether this binding is equal to an object instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasCompiledBinding other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this binding.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ BindingIndex;
                hash = (hash * HashMultiplier) ^ _presence;
                hash = (hash * HashMultiplier) ^ Access.GetHashCode();
                hash = (hash * HashMultiplier) ^ Contract.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this compiled binding.
        /// </summary>
        public override string ToString()
        {
            return IsPresent
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "AtlasCompiledBinding(Index={0}, Binding={1}, Field={2}, Slot={3}, Mode={4}, Flags={5}, Coverage={6})",
                    BindingIndex,
                    BindingName,
                    Contract.DebugName,
                    Contract.Slot.Index,
                    Mode,
                    Flags,
                    WriteCoverage)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "AtlasCompiledBinding(Index={0}, Binding={1}, MissingOptional=True, FieldId={2}, Mode={3}, Flags={4}, Coverage={5})",
                    BindingIndex,
                    BindingName,
                    FieldId,
                    Mode,
                    Flags,
                    WriteCoverage);
        }

        /// <summary>
        /// Determines whether two compiled bindings are equal.
        /// </summary>
        public static bool operator ==(AtlasCompiledBinding left, AtlasCompiledBinding right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two compiled bindings are not equal.
        /// </summary>
        public static bool operator !=(AtlasCompiledBinding left, AtlasCompiledBinding right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsPresenceValid()
        {
            if (_presence == Present)
            {
                return Contract.IsTableReady &&
                       Contract.StableId == Access.FieldId;
            }

            return _presence == Missing &&
                   Access.IsOptional;
        }

        private static string GetDiagnosticName(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? "<unnamed>"
                : name.ToString();
        }
    }
}
