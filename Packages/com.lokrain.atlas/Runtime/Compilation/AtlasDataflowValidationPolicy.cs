// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasDataflowValidationPolicy.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan content dataflow validation.
// - Decide which present bindings may be read before a prior producing operation.
// - Decide what content availability a write establishes.
// - Distinguish partial writes from full logical-content writes.
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
// - DiscardBeforeWrite is not write coverage. AtlasWriteCoverage decides what is available.

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
    public readonly struct AtlasDataflowValidationPolicy :
        IEquatable<AtlasDataflowValidationPolicy>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Strict dataflow policy.
        /// </summary>
        public static readonly AtlasDataflowValidationPolicy Strict = default;

        /// <summary>
        /// Recommended default policy for production compiled-plan validation.
        /// </summary>
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
        /// Gets whether external-role fields are allowed as initial content.
        /// </summary>
        public bool AllowsExternalRoleInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowExternalRoleInitialRead);
        }

        /// <summary>
        /// Gets whether external-owned fields are allowed as initial content.
        /// </summary>
        public bool AllowsExternalOwnedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowExternalOwnedInitialRead);
        }

        /// <summary>
        /// Gets whether borrowed fields are allowed as initial content.
        /// </summary>
        public bool AllowsBorrowedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowBorrowedInitialRead);
        }

        /// <summary>
        /// Gets whether imported fields are allowed as initial content.
        /// </summary>
        public bool AllowsImportedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowImportedInitialRead);
        }

        /// <summary>
        /// Gets whether adopted fields are allowed as initial content.
        /// </summary>
        public bool AllowsAdoptedInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowAdoptedInitialRead);
        }

        /// <summary>
        /// Gets whether external-lifetime fields are allowed as initial content.
        /// </summary>
        public bool AllowsExternalLifetimeInitialRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags.HasAny(AtlasDataflowValidationPolicyFlags.AllowExternalLifetimeInitialRead);
        }

        /// <summary>
        /// Creates a policy with additional flags enabled.
        /// </summary>
        public AtlasDataflowValidationPolicy WithFlags(AtlasDataflowValidationPolicyFlags flags)
        {
            return new AtlasDataflowValidationPolicy(Flags | flags);
        }

        /// <summary>
        /// Creates a policy with flags disabled.
        /// </summary>
        public AtlasDataflowValidationPolicy WithoutFlags(AtlasDataflowValidationPolicyFlags flags)
        {
            return new AtlasDataflowValidationPolicy(Flags & ~flags);
        }

        /// <summary>
        /// Returns whether a contract may be treated as initially readable.
        /// </summary>
        public bool AllowsInitialRead(AtlasContract contract)
        {
            return InitialAvailability(contract).HasAnyContent();
        }

        /// <summary>
        /// Returns the initial content availability supplied by policy for a contract.
        /// </summary>
        public AtlasContentAvailability InitialAvailability(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return AtlasContentAvailability.None;
            }

            if (AllowsExternalRoleInitialRead && contract.Role == AtlasFieldRole.External)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            if (AllowsExternalOwnedInitialRead && contract.Ownership == OwnershipPolicy.ExternalOwned)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            if (AllowsBorrowedInitialRead && contract.Ownership == OwnershipPolicy.Borrowed)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            if (AllowsImportedInitialRead && contract.Ownership == OwnershipPolicy.Imported)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            if (AllowsAdoptedInitialRead && contract.Ownership == OwnershipPolicy.Adopted)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            if (AllowsExternalLifetimeInitialRead && contract.Lifetime == LifetimePolicy.External)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            return AtlasContentAvailability.None;
        }

        /// <summary>
        /// Returns whether a compiled binding requires any previous content before execution.
        /// </summary>
        public bool RequiresPriorContent(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly)
            {
                return false;
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume)
            {
                return true;
            }

            if (binding.ReadsContent)
            {
                return true;
            }

            return binding.WritesContent &&
                   binding.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent);
        }

        /// <summary>
        /// Returns whether a compiled binding requires full logical previous content before execution.
        /// </summary>
        public bool RequiresFullPriorContent(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly)
            {
                return false;
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume)
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
        public bool CanProduceWithoutPriorContent(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly || !binding.WritesContent)
            {
                return false;
            }

            if (binding.Mode == AtlasOperationAccessMode.Append)
            {
                return true;
            }

            if (binding.Mode != AtlasOperationAccessMode.Write)
            {
                return false;
            }

            return binding.Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite) &&
                   binding.WriteCoverage.WritesAnyContent();
        }

        /// <summary>
        /// Returns the content availability established by a binding after execution.
        /// </summary>
        public AtlasContentAvailability EstablishedAvailability(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly || !binding.WritesContent)
            {
                return AtlasContentAvailability.None;
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume)
            {
                return AtlasContentAvailability.None;
            }

            if (binding.WriteCoverage == AtlasWriteCoverage.ExternalContract)
            {
                return AtlasContentAvailability.ExternalContractContent;
            }

            if (binding.WriteCoverage.MakesFullLogicalContentAvailable())
            {
                return AtlasContentAvailability.FullLogicalContent;
            }

            if (binding.WriteCoverage.IsPartialContentWrite())
            {
                return AtlasContentAvailability.PartialContent;
            }

            return AtlasContentAvailability.None;
        }

        /// <summary>
        /// Compatibility predicate for callers that only need to know whether a binding establishes any content.
        /// </summary>
        public bool EstablishesContentForLaterReads(AtlasCompiledBinding binding)
        {
            return EstablishedAvailability(binding).HasAnyContent();
        }

        /// <summary>
        /// Determines whether this policy equals another policy.
        /// </summary>
        public bool Equals(AtlasDataflowValidationPolicy other)
        {
            return Flags == other.Flags;
        }

        /// <summary>
        /// Determines whether this policy equals an object instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasDataflowValidationPolicy other && Equals(other);
        }

        /// <summary>
        /// Returns a managed hash code for this policy.
        /// </summary>
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