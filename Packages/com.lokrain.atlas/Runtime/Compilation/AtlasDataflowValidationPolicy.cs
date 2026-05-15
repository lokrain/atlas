// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasDataflowValidationPolicy.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan content dataflow validation.
// - Decide which present bindings may be read before a prior producing operation.
// - Decide which write modes can establish initialized content for later operations.
// - Keep read-before-write validation explicit instead of hard-coding route assumptions.
//
// Design notes
// - This is metadata policy, not runtime memory state.
// - This policy does not allocate workspace storage.
// - This policy does not schedule jobs.
// - This policy does not validate route-specific stage presence or stage uniqueness.
// - default(AtlasDataflowValidationPolicy) is valid and strict.
// - ProductionDefault allows external/imported/adopted/borrowed/external-lifetime inputs
//   to be treated as initially readable.

using System;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Policy used by compiled-plan content dataflow validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDataflowValidationPolicy"/> answers a narrow question: when walking a
    /// compiled operation sequence, is a content read satisfied by prior produced content or by
    /// an explicitly allowed initial-content source?
    /// </para>
    ///
    /// <para>
    /// The policy intentionally does not infer that canonical, support, scratch, payload, or
    /// diagnostic Fields are initially readable. Those Fields must be produced earlier in the
    /// compiled sequence unless a route-specific validator or future authored input model says
    /// otherwise.
    /// </para>
    ///
    /// <para>
    /// This type is safe to default-initialize. The default value is strict and treats no Field as
    /// initially readable.
    /// </para>
    /// </remarks>
    public readonly struct AtlasDataflowValidationPolicy :
        IEquatable<AtlasDataflowValidationPolicy>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Strict dataflow policy.
        /// </summary>
        /// <remarks>
        /// No content read is allowed before a prior producing operation, except shape-only access
        /// and absent optional bindings, which do not require content.
        /// </remarks>
        public static readonly AtlasDataflowValidationPolicy Strict = default;

        /// <summary>
        /// Recommended default policy for production compiled-plan validation.
        /// </summary>
        /// <remarks>
        /// This allows externally supplied storage to be used as initial input while still requiring
        /// Atlas-owned internal Fields to be produced before content reads.
        /// </remarks>
        public static readonly AtlasDataflowValidationPolicy ProductionDefault =
            new(
                AtlasDataflowValidationPolicyFlags.AllowExternalRoleInitialRead |
                AtlasDataflowValidationPolicyFlags.AllowExternalOwnedInitialRead |
                AtlasDataflowValidationPolicyFlags.AllowBorrowedInitialRead |
                AtlasDataflowValidationPolicyFlags.AllowImportedInitialRead |
                AtlasDataflowValidationPolicyFlags.AllowAdoptedInitialRead |
                AtlasDataflowValidationPolicyFlags.AllowExternalLifetimeInitialRead);

        /// <summary>
        /// Policy flags.
        /// </summary>
        public readonly AtlasDataflowValidationPolicyFlags Flags;

        /// <summary>
        /// Creates a dataflow validation policy from explicit flags.
        /// </summary>
        /// <param name="flags">Policy flags.</param>
        public AtlasDataflowValidationPolicy(AtlasDataflowValidationPolicyFlags flags)
        {
            Flags = flags;
        }

        /// <summary>
        /// Gets whether this policy is the strict default policy.
        /// </summary>
        public bool IsStrict
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags == AtlasDataflowValidationPolicyFlags.None;
        }

        /// <summary>
        /// Gets whether external-role Fields are allowed as initial content.
        /// </summary>
        public bool AllowsExternalRoleInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowExternalRoleInitialRead);
        }

        /// <summary>
        /// Gets whether external-owned Fields are allowed as initial content.
        /// </summary>
        public bool AllowsExternalOwnedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowExternalOwnedInitialRead);
        }

        /// <summary>
        /// Gets whether borrowed Fields are allowed as initial content.
        /// </summary>
        public bool AllowsBorrowedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowBorrowedInitialRead);
        }

        /// <summary>
        /// Gets whether imported Fields are allowed as initial content.
        /// </summary>
        public bool AllowsImportedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowImportedInitialRead);
        }

        /// <summary>
        /// Gets whether adopted Fields are allowed as initial content.
        /// </summary>
        public bool AllowsAdoptedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowAdoptedInitialRead);
        }

        /// <summary>
        /// Gets whether external-lifetime Fields are allowed as initial content.
        /// </summary>
        public bool AllowsExternalLifetimeInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowExternalLifetimeInitialRead);
        }

        /// <summary>
        /// Creates a policy with additional flags enabled.
        /// </summary>
        /// <param name="flags">Flags to enable.</param>
        /// <returns>A policy with the supplied flags enabled.</returns>
        public AtlasDataflowValidationPolicy WithFlags(AtlasDataflowValidationPolicyFlags flags)
        {
            return new AtlasDataflowValidationPolicy(Flags | flags);
        }

        /// <summary>
        /// Creates a policy with flags disabled.
        /// </summary>
        /// <param name="flags">Flags to disable.</param>
        /// <returns>A policy with the supplied flags disabled.</returns>
        public AtlasDataflowValidationPolicy WithoutFlags(AtlasDataflowValidationPolicyFlags flags)
        {
            return new AtlasDataflowValidationPolicy(Flags & ~flags);
        }

        /// <summary>
        /// Returns whether a Contract may be treated as initially readable.
        /// </summary>
        /// <param name="contract">Resolved Field Contract.</param>
        /// <returns>
        /// <c>true</c> when the Contract matches one of the policy's allowed initial-content sources;
        /// otherwise <c>false</c>.
        /// </returns>
        public bool AllowsInitialRead(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return false;
            }

            if (AllowsExternalRoleInitialRead && contract.Role == AtlasFieldRole.External)
            {
                return true;
            }

            if (AllowsExternalOwnedInitialRead && contract.Ownership == OwnershipPolicy.ExternalOwned)
            {
                return true;
            }

            if (AllowsBorrowedInitialRead && contract.Ownership == OwnershipPolicy.Borrowed)
            {
                return true;
            }

            if (AllowsImportedInitialRead && contract.Ownership == OwnershipPolicy.Imported)
            {
                return true;
            }

            if (AllowsAdoptedInitialRead && contract.Ownership == OwnershipPolicy.Adopted)
            {
                return true;
            }

            if (AllowsExternalLifetimeInitialRead && contract.Lifetime == LifetimePolicy.External)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether a compiled binding requires previously initialized content before execution.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns>
        /// <c>true</c> when the binding reads existing content, consumes existing content, performs
        /// read-write access, or declares preservation of existing content; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Shape-only bindings and missing optional bindings do not require content.
        /// </remarks>
        public bool RequiresPriorContent(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly)
            {
                return false;
            }

            if (binding.ReadsContent)
            {
                return true;
            }

            return binding.WritesContent &&
                   binding.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent);
        }

        /// <summary>
        /// Returns whether a compiled binding can write content without requiring previous content.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns>
        /// <c>true</c> for append access and discard-before-write writes; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Read-write and consume access are not first writes. They require already initialized
        /// content before they can safely execute.
        /// </remarks>
        public bool CanProduceWithoutPriorContent(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly || !binding.WritesContent)
            {
                return false;
            }

            return binding.Mode == AtlasOperationAccessMode.Append ||
                   (binding.Mode == AtlasOperationAccessMode.Write &&
                    binding.Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite));
        }

        /// <summary>
        /// Returns whether a compiled binding establishes content availability for later bindings.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns>
        /// <c>true</c> when the binding writes, appends, consumes, or mutates present content;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method answers only whether the binding may make the Field available after the
        /// operation has run. A validator must still separately verify that any required prior
        /// content was available before the operation.
        /// </remarks>
        public bool EstablishesContentForLaterReads(AtlasCompiledBinding binding)
        {
            return binding.IsPresent &&
                   !binding.IsShapeOnly &&
                   binding.WritesContent;
        }

        /// <summary>
        /// Determines whether this policy equals another policy.
        /// </summary>
        /// <param name="other">Policy to compare against.</param>
        /// <returns><c>true</c> when flags match; otherwise <c>false</c>.</returns>
        public bool Equals(AtlasDataflowValidationPolicy other)
        {
            return Flags == other.Flags;
        }

        /// <summary>
        /// Determines whether this policy equals an object instance.
        /// </summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasDataflowValidationPolicy"/>
        /// with the same flags; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasDataflowValidationPolicy other && Equals(other);
        }

        /// <summary>
        /// Returns a managed hash code for this policy.
        /// </summary>
        /// <returns>A managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (HashSeed * HashMultiplier) ^ Flags.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a compact policy label.
        /// </summary>
        /// <returns>A compact policy label.</returns>
        public override string ToString()
        {
            return $"AtlasDataflowValidationPolicy({Flags})";
        }

        /// <summary>
        /// Determines whether two policies are equal.
        /// </summary>
        public static bool operator ==(
            AtlasDataflowValidationPolicy left,
            AtlasDataflowValidationPolicy right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two policies are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasDataflowValidationPolicy left,
            AtlasDataflowValidationPolicy right)
        {
            return !left.Equals(right);
        }
    }
}