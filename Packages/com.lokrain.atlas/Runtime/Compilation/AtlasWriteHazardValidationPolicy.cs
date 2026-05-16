// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasWriteHazardValidationPolicy.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define policy for compiled-plan write-hazard validation.
// - Decide which writes are legal for Field ownership, storage kind, and ordering semantics.
// - Keep write validation explicit instead of hard-coding execution assumptions.
// - Preserve the boundary between compiler validation and workspace/runtime memory checks.
//
// Design notes
// - This is metadata policy, not runtime memory state.
// - This policy does not allocate workspace storage.
// - This policy does not schedule jobs.
// - This policy does not prove concrete container safety.
// - default(AtlasWriteHazardValidationPolicy) is valid and strict.
// - ProductionDefault is conservative for deterministic Atlas-owned compiled plans.

using System;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Policy used by compiled-plan write-hazard validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWriteHazardValidationPolicy"/> validates the symbolic write contract before
    /// executable planning. It answers whether a compiled binding is a coherent write declaration
    /// for its resolved Field Contract.
    /// </para>
    ///
    /// <para>
    /// This policy does not validate actual native container instances, dependency handles, worker
    /// partitioning, atomics, aliasing, or runtime ownership grants. Those belong to workspace,
    /// memory resolver, and executable scheduler validation.
    /// </para>
    ///
    /// <para>
    /// The default value is strict. <see cref="ProductionDefault"/> is the recommended starting
    /// point for deterministic Atlas-owned plans.
    /// </para>
    /// </remarks>
    public readonly struct AtlasWriteHazardValidationPolicy :
        IEquatable<AtlasWriteHazardValidationPolicy>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Strict write-hazard policy.
        /// </summary>
        public static readonly AtlasWriteHazardValidationPolicy Strict = default;

        /// <summary>
        /// Recommended conservative production policy for compiled Atlas plans.
        /// </summary>
        public static readonly AtlasWriteHazardValidationPolicy ProductionDefault =
            new(
                AtlasWriteHazardValidationPolicyFlags.AllowAdoptedWrites |
                AtlasWriteHazardValidationPolicyFlags.AllowNativeListAppend |
                AtlasWriteHazardValidationPolicyFlags.AllowUnsafeListAppend |
                AtlasWriteHazardValidationPolicyFlags.AllowNativeStreamAppend |
                AtlasWriteHazardValidationPolicyFlags.AllowNativeListConsume |
                AtlasWriteHazardValidationPolicyFlags.AllowUnsafeListConsume |
                AtlasWriteHazardValidationPolicyFlags.AllowNativeStreamConsume |
                AtlasWriteHazardValidationPolicyFlags.RequireFieldParallelWriteFlag |
                AtlasWriteHazardValidationPolicyFlags.RequireDeterministicWriteFlagForDeterministicFields |
                AtlasWriteHazardValidationPolicyFlags.RequireDeterministicAppendOrder |
                AtlasWriteHazardValidationPolicyFlags.RequireDeterministicConsumeOrder |
                AtlasWriteHazardValidationPolicyFlags.RequireExplicitWriteContentPolicy |
                AtlasWriteHazardValidationPolicyFlags.RejectContradictoryWriteContentPolicy |
                AtlasWriteHazardValidationPolicyFlags.RejectBlobWrites |
                AtlasWriteHazardValidationPolicyFlags.RejectShapeOnlyWrites |
                AtlasWriteHazardValidationPolicyFlags.RejectParallelFlagOnNonWrites |
                AtlasWriteHazardValidationPolicyFlags.RejectExclusiveFlagOnNonWrites);

        /// <summary>
        /// Policy flags.
        /// </summary>
        public readonly AtlasWriteHazardValidationPolicyFlags Flags;

        /// <summary>
        /// Creates a write-hazard validation policy from explicit flags.
        /// </summary>
        /// <param name="flags">Policy flags.</param>
        public AtlasWriteHazardValidationPolicy(AtlasWriteHazardValidationPolicyFlags flags)
        {
            Flags = flags;
        }

        /// <summary>
        /// Gets whether this policy is the strict default policy.
        /// </summary>
        public bool IsStrict
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Flags == AtlasWriteHazardValidationPolicyFlags.None;
        }

        /// <summary>
        /// Creates a policy with additional flags enabled.
        /// </summary>
        /// <param name="flags">Flags to enable.</param>
        /// <returns>A policy with the supplied flags enabled.</returns>
        public AtlasWriteHazardValidationPolicy WithFlags(AtlasWriteHazardValidationPolicyFlags flags)
        {
            return new AtlasWriteHazardValidationPolicy(Flags | flags);
        }

        /// <summary>
        /// Creates a policy with flags disabled.
        /// </summary>
        /// <param name="flags">Flags to disable.</param>
        /// <returns>A policy with the supplied flags disabled.</returns>
        public AtlasWriteHazardValidationPolicy WithoutFlags(AtlasWriteHazardValidationPolicyFlags flags)
        {
            return new AtlasWriteHazardValidationPolicy(Flags & ~flags);
        }

        /// <summary>
        /// Returns whether writes are allowed for the Contract ownership and lifetime declaration.
        /// </summary>
        /// <param name="contract">Resolved Field Contract.</param>
        /// <returns><c>true</c> when the policy permits writes to the Contract; otherwise <c>false</c>.</returns>
        public bool AllowsWriteOwnership(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return false;
            }

            return contract.Ownership switch
            {
                OwnershipPolicy.AtlasOwned or OwnershipPolicy.JobOwned => true,
                OwnershipPolicy.Adopted => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowAdoptedWrites),
                OwnershipPolicy.ExternalOwned => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowExternalOwnedWrites),
                OwnershipPolicy.Borrowed => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowBorrowedWrites),
                OwnershipPolicy.Imported => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowImportedWrites),
                _ => false,
            };
        }

        /// <summary>
        /// Returns whether writes are allowed for the Contract lifetime declaration.
        /// </summary>
        /// <param name="contract">Resolved Field Contract.</param>
        /// <returns><c>true</c> when the policy permits writes for the Contract lifetime; otherwise <c>false</c>.</returns>
        public bool AllowsWriteLifetime(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return false;
            }

            return contract.Lifetime != LifetimePolicy.External ||
                   Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowExternalLifetimeWrites);
        }

        /// <summary>
        /// Returns whether write access is allowed for the Contract storage kind.
        /// </summary>
        /// <param name="contract">Resolved Field Contract.</param>
        /// <returns><c>true</c> when the storage kind may be written; otherwise <c>false</c>.</returns>
        public bool AllowsWriteStorage(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return false;
            }

            var kind = contract.StorageFormat.Kind;

            if (kind == StorageKind.Blob &&
                Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RejectBlobWrites))
            {
                return false;
            }

            if (kind == StorageKind.External &&
                !Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowExternalStorageWrites))
            {
                return false;
            }

            return kind != StorageKind.None;
        }

        /// <summary>
        /// Returns whether append access is supported for the Contract storage kind.
        /// </summary>
        /// <param name="contract">Resolved Field Contract.</param>
        /// <returns><c>true</c> when append access is accepted; otherwise <c>false</c>.</returns>
        public bool AllowsAppendStorage(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return false;
            }

            return contract.StorageFormat.Kind switch
            {
                StorageKind.NativeList => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowNativeListAppend),
                StorageKind.UnsafeList => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowUnsafeListAppend),
                StorageKind.NativeStream => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowNativeStreamAppend),
                StorageKind.External => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowExternalStorageAppend),
                _ => false
            };
        }

        /// <summary>
        /// Returns whether consume access is supported for the Contract storage kind.
        /// </summary>
        /// <param name="contract">Resolved Field Contract.</param>
        /// <returns><c>true</c> when consume access is accepted; otherwise <c>false</c>.</returns>
        public bool AllowsConsumeStorage(AtlasContract contract)
        {
            if (!contract.IsTableReady)
            {
                return false;
            }

            return contract.StorageFormat.Kind switch
            {
                StorageKind.NativeList => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowNativeListConsume),
                StorageKind.UnsafeList => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowUnsafeListConsume),
                StorageKind.NativeStream => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowNativeStreamConsume),
                StorageKind.External => Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.AllowExternalStorageConsume),
                _ => false
            };
        }

        /// <summary>
        /// Returns whether the binding has an explicit write content policy when required.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns><c>true</c> when the binding satisfies explicit write-content policy rules.</returns>
        public bool HasRequiredWriteContentPolicy(AtlasCompiledBinding binding)
        {
            if (!Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RequireExplicitWriteContentPolicy))
            {
                return true;
            }

            if (!binding.IsPresent || binding.IsShapeOnly || !binding.WritesContent)
            {
                return true;
            }

            if (!binding.WriteCoverage.WritesAnyContent())
            {
                return false;
            }

            if (binding.Mode == AtlasOperationAccessMode.Append)
            {
                return binding.WriteCoverage == AtlasWriteCoverage.AppendRecords;
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume)
            {
                return binding.WriteCoverage == AtlasWriteCoverage.ConsumeRecords;
            }

            if (!binding.WriteCoverage.IsFieldWriteCoverage())
            {
                return false;
            }

            return binding.Flags.HasAny(
                AtlasOperationAccessFlags.DiscardBeforeWrite |
                AtlasOperationAccessFlags.PreserveExistingContent);
        }

        /// <summary>
        /// Returns whether the binding has contradictory discard/preserve write policy.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns><c>true</c> when the binding declares contradictory write content policy.</returns>
        public bool HasContradictoryWriteContentPolicy(AtlasCompiledBinding binding)
        {
            return Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RejectContradictoryWriteContentPolicy) &&
                   binding.Flags.HasAll(
                       AtlasOperationAccessFlags.DiscardBeforeWrite |
                       AtlasOperationAccessFlags.PreserveExistingContent);
        }

        /// <summary>
        /// Returns whether operation-level parallel write flags are coherent with the Field Contract.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns><c>true</c> when parallel-write declaration is accepted; otherwise <c>false</c>.</returns>
        public bool AllowsParallelWriteDeclaration(AtlasCompiledBinding binding)
        {
            if (!binding.Flags.HasAny(AtlasOperationAccessFlags.AllowsParallelWrite))
            {
                return true;
            }

            if (Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RejectParallelFlagOnNonWrites) &&
                !binding.WritesContent)
            {
                return false;
            }

            if (!Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RequireFieldParallelWriteFlag))
            {
                return true;
            }

            return binding.Contract.Flags.HasAny(AtlasFieldFlags.AllowsParallelWrite);
        }

        /// <summary>
        /// Returns whether operation-level exclusive-write flags are coherent.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns><c>true</c> when exclusive-write declaration is accepted; otherwise <c>false</c>.</returns>
        public bool AllowsExclusiveWriteDeclaration(AtlasCompiledBinding binding)
        {
            if (!binding.Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite))
            {
                return true;
            }

            return !Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RejectExclusiveFlagOnNonWrites) ||
                   binding.WritesContent;
        }

        /// <summary>
        /// Returns whether deterministic write ordering is declared when required by policy and Field Contract.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns><c>true</c> when deterministic-order rules are satisfied; otherwise <c>false</c>.</returns>
        public bool AllowsDeterministicWriteDeclaration(AtlasCompiledBinding binding)
        {
            if (!binding.IsPresent || binding.IsShapeOnly || !binding.WritesContent)
            {
                return true;
            }

            if (Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RequireDeterministicWriteFlagForDeterministicFields) &&
                binding.Contract.Flags.HasAny(AtlasFieldFlags.DeterministicOrder) &&
                !binding.Flags.HasAny(AtlasOperationAccessFlags.RequiresDeterministicOrder))
            {
                return false;
            }

            if (binding.Mode == AtlasOperationAccessMode.Append &&
                Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RequireDeterministicAppendOrder) &&
                !binding.Flags.HasAny(AtlasOperationAccessFlags.RequiresDeterministicOrder))
            {
                return false;
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume &&
                Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RequireDeterministicConsumeOrder) &&
                !binding.Flags.HasAny(AtlasOperationAccessFlags.RequiresDeterministicOrder))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns whether a shape-only binding is allowed to carry write semantics.
        /// </summary>
        /// <param name="binding">Compiled binding to inspect.</param>
        /// <returns><c>true</c> when the binding satisfies shape-only write rules; otherwise <c>false</c>.</returns>
        public bool AllowsShapeOnlyWriteDeclaration(AtlasCompiledBinding binding)
        {
            if (!binding.IsShapeOnly)
            {
                return true;
            }

            if (!Flags.HasAny(AtlasWriteHazardValidationPolicyFlags.RejectShapeOnlyWrites))
            {
                return true;
            }

            return !binding.WritesContent &&
                   binding.Flags.HasNone(
                       AtlasOperationAccessFlags.DiscardBeforeWrite |
                       AtlasOperationAccessFlags.PreserveExistingContent |
                       AtlasOperationAccessFlags.AllowsParallelWrite |
                       AtlasOperationAccessFlags.RequiresExclusiveWrite);
        }

        /// <summary>
        /// Determines whether this policy equals another policy.
        /// </summary>
        /// <param name="other">Policy to compare against.</param>
        /// <returns><c>true</c> when flags match; otherwise <c>false</c>.</returns>
        public bool Equals(AtlasWriteHazardValidationPolicy other)
        {
            return Flags == other.Flags;
        }

        /// <summary>
        /// Determines whether this policy equals an object instance.
        /// </summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasWriteHazardValidationPolicy"/>
        /// with the same flags; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasWriteHazardValidationPolicy other && Equals(other);
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
            return $"AtlasWriteHazardValidationPolicy({Flags})";
        }

        /// <summary>
        /// Determines whether two policies are equal.
        /// </summary>
        public static bool operator ==(
            AtlasWriteHazardValidationPolicy left,
            AtlasWriteHazardValidationPolicy right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two policies are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasWriteHazardValidationPolicy left,
            AtlasWriteHazardValidationPolicy right)
        {
            return !left.Equals(right);
        }
    }
}