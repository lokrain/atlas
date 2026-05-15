// Runtime/Compilation/AtlasCompiledBinding.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one operation access declaration after resolution against the field contract table.
// - Preserve operation-local binding order.
// - Bind symbolic operation access to either a concrete field contract or an explicit optional-missing binding.
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
    /// <remarks>
    /// <para>
    /// A compiled binding is the first compilation boundary where symbolic operation access is
    /// resolved against the field contract table. It preserves the operation-local binding index and
    /// either carries the resolved contract or records that an optional binding is absent.
    /// </para>
    ///
    /// <para>
    /// This type does not own memory. It does not expose native containers. It does not schedule
    /// jobs. Runtime workspaces and memory resolvers use compiled bindings to produce execution
    /// bindings appropriate for concrete schedulers.
    /// </para>
    ///
    /// <para>
    /// Missing bindings are valid only for optional operation access declarations. Required access
    /// must always resolve to a present contract during plan compilation.
    /// </para>
    /// </remarks>
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
        /// <remarks>
        /// This value is not an invalid sentinel. Callers must use the boolean result of the
        /// Try-style API to determine whether this payload is meaningful.
        /// </remarks>
        public static readonly AtlasCompiledBinding Empty = default;

        /// <summary>
        /// Compatibility alias for <see cref="Empty"/>.
        /// </summary>
        /// <remarks>
        /// This value is not an invalid sentinel. It is retained only for older call sites.
        /// </remarks>
        public static readonly AtlasCompiledBinding Invalid = default;

        private readonly byte _presence;

        /// <summary>
        /// Zero-based operation-local binding index.
        /// </summary>
        /// <remarks>
        /// This index corresponds to the access declaration index inside the source operation
        /// definition. Operation binding order is ABI and must not be sorted by field id or slot.
        /// </remarks>
        public readonly int BindingIndex;

        /// <summary>
        /// Original operation access declaration.
        /// </summary>
        public readonly AtlasOperationAccess Access;

        /// <summary>
        /// Resolved field contract when <see cref="IsPresent"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// When <see cref="IsMissingOptional"/> is <c>true</c>, this value is an inert default
        /// payload. Do not infer missing state from <see cref="Contract"/>. Use
        /// <see cref="IsPresent"/> or <see cref="IsMissingOptional"/>.
        /// </remarks>
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
        /// <remarks>
        /// This is semantic validation shorthand. It does not mean default bit patterns are invalid.
        /// </remarks>
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
        /// <remarks>
        /// This is a semantic validation query, not a bit-pattern validity query.
        /// </remarks>
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
        /// Gets the resolved field slot when present, or the default slot payload when absent.
        /// </summary>
        /// <remarks>
        /// Slot zero/default is valid. Callers must check <see cref="IsPresent"/> before consuming
        /// this value as a resolved slot.
        /// </remarks>
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
        /// <param name="bindingIndex">Zero-based operation-local binding index.</param>
        /// <param name="access">Source operation access declaration.</param>
        /// <param name="contract">Resolved field contract.</param>
        /// <returns>A validated present compiled binding.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when arguments do not form a valid resolved binding.
        /// </exception>
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
        /// <param name="bindingIndex">Zero-based operation-local binding index.</param>
        /// <param name="access">Source optional operation access declaration.</param>
        /// <returns>A validated missing optional compiled binding.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="access"/> is invalid or not optional.
        /// </exception>
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
        /// <param name="bindingIndex">Zero-based operation-local binding index.</param>
        /// <param name="access">Source operation access declaration.</param>
        /// <param name="contracts">Contract table used for field resolution.</param>
        /// <returns>
        /// A present compiled binding when the field exists, or a missing optional binding when the
        /// field is absent and the access declaration is optional.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the access declaration is invalid or required field access cannot be resolved.
        /// </exception>
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
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when this binding has a negative index, invalid access declaration, invalid
        /// presence state, missing required contract, or mismatched field identity.
        /// </exception>
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
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this binding is missing.
        /// </exception>
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
        /// <returns>The resolved field contract.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this binding is missing.
        /// </exception>
        public AtlasContract GetRequiredContract()
        {
            ThrowIfMissing(nameof(Contract));
            return Contract;
        }

        /// <summary>
        /// Determines whether this binding is equal to another binding.
        /// </summary>
        /// <param name="other">The binding to compare with this binding.</param>
        /// <returns><c>true</c> when binding index, access declaration, contract, and presence state match.</returns>
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
        /// <param name="obj">The object to compare with this binding.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal <see cref="AtlasCompiledBinding"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasCompiledBinding other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this binding.
        /// </summary>
        /// <returns>A deterministic hash code derived from binding index, access, contract, and presence state.</returns>
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
        /// <returns>A stable diagnostic string.</returns>
        public override string ToString()
        {
            return IsPresent
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "AtlasCompiledBinding(Index={0}, Binding={1}, Field={2}, Slot={3}, Mode={4}, Flags={5})",
                    BindingIndex,
                    BindingName,
                    Contract.DebugName,
                    Contract.Slot.Index,
                    Mode,
                    Flags)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "AtlasCompiledBinding(Index={0}, Binding={1}, MissingOptional=True, FieldId={2}, Mode={3}, Flags={4})",
                    BindingIndex,
                    BindingName,
                    FieldId,
                    Mode,
                    Flags);
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