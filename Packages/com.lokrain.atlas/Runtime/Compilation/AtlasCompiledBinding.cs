// Runtime/Compilation/AtlasCompiledBinding.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one operation access declaration after resolution against the Field Contract table.
// - Preserve operation-local binding order.
// - Bind symbolic operation access to either a concrete Field Contract or an explicit optional-missing binding.
// - Keep compilation output separate from runtime native memory.
//
// Design notes
// - This is compilation metadata, not a job payload.
// - Jobs should receive resolved native containers, numeric parameters, and scheduler-specific data.
// - Required operation access must resolve to a present Contract.
// - Optional operation access may compile as missing while preserving the binding index.
// - Shape-only access may still resolve to a present Contract because shape is Contract/table-derived metadata.
// - Operation access order is significant and must not be sorted.

using System;
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
    /// resolved against the Field Contract table. It preserves the operation-local binding index
    /// and either carries the resolved Contract or records that an optional binding is absent.
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
    /// must always resolve to a present Contract during plan compilation.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasCompiledBinding :
        IEquatable<AtlasCompiledBinding>
    {
        private const byte Missing = 0;
        private const byte Present = 1;

        /// <summary>
        /// Reserved invalid compiled binding.
        /// </summary>
        public static readonly AtlasCompiledBinding Empty = default;

        private readonly byte _presence;

        /// <summary>
        /// Zero-based operation-local binding index.
        /// </summary>
        /// <remarks>
        /// This index corresponds to the access declaration index inside the source
        /// <see cref="AtlasOperationDefinition"/>.
        /// </remarks>
        public readonly int BindingIndex;

        /// <summary>
        /// Original operation access declaration.
        /// </summary>
        public readonly AtlasOperationAccess Access;

        /// <summary>
        /// Resolved Field Contract when <see cref="IsPresent"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// When <see cref="IsMissingOptional"/> is <c>true</c>, this value is
        /// <see cref="AtlasContract.Empty"/>.
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
        /// Gets whether this binding resolved to a concrete Field Contract.
        /// </summary>
        public bool IsPresent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _presence == Present;
        }

        /// <summary>
        /// Gets whether this binding represents an absent optional Field.
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
        /// Gets the stable Field identifier declared by the operation access.
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
        /// Gets the resolved Field slot, or <see cref="AtlasFieldSlot.Invalid"/> when the binding is absent.
        /// </summary>
        public AtlasFieldSlot Slot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent
                ? Contract.Slot
                : AtlasFieldSlot.Invalid;
        }

        /// <summary>
        /// Gets whether the operation declaration reads Field contents and the binding is present.
        /// </summary>
        public bool ReadsContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   Access.ReadsContent;
        }

        /// <summary>
        /// Gets whether the operation declaration writes, appends, consumes, or mutates Field contents and the binding is present.
        /// </summary>
        public bool WritesContent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPresent &&
                   Access.WritesContent;
        }

        /// <summary>
        /// Gets whether the operation declaration reads Field contents regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Access.ReadsContent;
        }

        /// <summary>
        /// Gets whether the operation declaration writes Field contents regardless of optional resolution.
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
        /// Creates a present compiled binding from a resolved Contract.
        /// </summary>
        /// <param name="bindingIndex">Zero-based operation-local binding index.</param>
        /// <param name="access">Source operation access declaration.</param>
        /// <param name="contract">Resolved Field Contract.</param>
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
                AtlasContract.Empty,
                Missing);

            binding.ValidateOrThrow(nameof(binding));
            return binding;
        }

        /// <summary>
        /// Resolves an operation access declaration against a Contract table.
        /// </summary>
        /// <param name="bindingIndex">Zero-based operation-local binding index.</param>
        /// <param name="access">Source operation access declaration.</param>
        /// <param name="contracts">Contract table used for Field resolution.</param>
        /// <returns>
        /// A present compiled binding when the Field exists, or a missing optional binding when
        /// the Field is absent and the access declaration is optional.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the access declaration is invalid or required Field access cannot be resolved.
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
                $"Required Atlas binding '{access.BindingName}' could not resolve Field id '{access.FieldId}' in Contract table '{GetDiagnosticName(contracts.Name)}'.",
                nameof(access));
        }

        /// <summary>
        /// Throws when this compiled binding is invalid.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when this binding has an invalid index, invalid access declaration, invalid
        /// presence state, missing required Contract, or mismatched Field identity.
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

            Access.ValidateOrThrow($"{name}.Access");

            if (_presence != Present && _presence != Missing)
            {
                throw new ArgumentException(
                    $"Atlas compiled binding '{Access.BindingName}' has invalid presence state '{_presence}'.",
                    name);
            }

            if (_presence == Missing)
            {
                if (!Access.IsOptional)
                {
                    throw new ArgumentException(
                        $"Atlas compiled binding '{Access.BindingName}' is missing but its access declaration is not optional.",
                        name);
                }

                if (!Contract.Equals(AtlasContract.Empty))
                {
                    throw new ArgumentException(
                        $"Atlas compiled binding '{Access.BindingName}' is missing but still carries a Contract.",
                        name);
                }

                return;
            }

            if (!Contract.IsTableReady)
            {
                throw new ArgumentException(
                    $"Atlas compiled binding '{Access.BindingName}' resolved to a Contract that is not table-ready.",
                    name);
            }

            if (Contract.StableId != Access.FieldId)
            {
                throw new ArgumentException(
                    $"Atlas compiled binding '{Access.BindingName}' targets Field id '{Access.FieldId}' but resolved Contract '{Contract.DebugName}' has Field id '{Contract.StableId}'.",
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
            if (IsPresent)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Atlas compiled binding '{BindingName}' is missing and cannot provide resolved Contract data.");
        }

        /// <summary>
        /// Gets the resolved Contract or throws when this binding is missing.
        /// </summary>
        /// <returns>The resolved Field Contract.</returns>
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
        /// <returns><c>true</c> when binding index, access declaration, Contract, and presence state match.</returns>
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
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code derived from binding index, access, Contract, and presence state.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = BindingIndex;
                hash = (hash * 397) ^ _presence.GetHashCode();
                hash = (hash * 397) ^ Access.GetHashCode();
                hash = (hash * 397) ^ Contract.GetHashCode();
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
                ? $"AtlasCompiledBinding(Index={BindingIndex}, Binding={BindingName}, Field={Contract.DebugName}, Slot={Contract.Slot.Index}, Mode={Mode}, Flags={Flags})"
                : $"AtlasCompiledBinding(Index={BindingIndex}, Binding={BindingName}, MissingOptional=True, FieldId={FieldId}, Mode={Mode}, Flags={Flags})";
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
                   Access.IsOptional &&
                   Contract.Equals(AtlasContract.Empty);
        }

        private static string GetDiagnosticName(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? "<unnamed>"
                : name.ToString();
        }
    }
}