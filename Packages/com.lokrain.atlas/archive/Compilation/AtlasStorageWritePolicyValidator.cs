// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasStorageWritePolicyValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate storage, ownership, lifetime, content, and deterministic-order write policy.
// - Keep write-storage policy separate from write-hazard traversal and parallel declaration validation.
// - Preserve AtlasWriteHazardValidator public diagnostics and pass ordering.

using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Operations;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Validates storage and content policy for compiled write bindings.
    /// </summary>
    internal static class AtlasStorageWritePolicyValidator
    {
        public static void ValidateShapeOnlyDeclaration(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsShapeOnlyWriteDeclaration(binding))
            {
                return;
            }

            diagnostics.AddError(
                AtlasWriteHazardDiagnosticCodes.ShapeOnlyWrite,
                location,
                AtlasDiagnosticText.Message($"Atlas binding '{AtlasDiagnosticText.Name(binding.BindingName)}' in operation '{AtlasDiagnosticText.Name(operation.DebugName)}' is shape-only but declares write-related access semantics."));
        }

        public static void ValidateWrite(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            ValidateWriteAuthorization(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateWriteModeStorage(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateWriteContentPolicy(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateDeterministicOrdering(
                operation,
                binding,
                location,
                policy,
                diagnostics);
        }

        private static void ValidateWriteAuthorization(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (!policy.AllowsWriteOwnership(binding.Contract))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.WriteOwnershipRejected,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' writes Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}', but ownership policy '{binding.Contract.Ownership}' is not writable under the active write-hazard policy."));
            }

            if (!policy.AllowsWriteLifetime(binding.Contract))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.WriteLifetimeRejected,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' writes Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}', but lifetime policy '{binding.Contract.Lifetime}' is not writable under the active write-hazard policy."));
            }

            if (!policy.AllowsWriteStorage(binding.Contract))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.WriteStorageRejected,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' writes Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}', but storage kind '{binding.Contract.StorageFormat.Kind}' is not writable under the active write-hazard policy."));
            }
        }

        private static void ValidateWriteModeStorage(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (binding.Mode == AtlasOperationAccessMode.Append &&
                !policy.AllowsAppendStorage(binding.Contract))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.AppendStorageRejected,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' appends to Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}', but storage kind '{binding.Contract.StorageFormat.Kind}' is not append-compatible under the active write-hazard policy."));
            }

            if (binding.Mode == AtlasOperationAccessMode.Consume &&
                !policy.AllowsConsumeStorage(binding.Contract))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.ConsumeStorageRejected,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' consumes Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}', but storage kind '{binding.Contract.StorageFormat.Kind}' is not consume-compatible under the active write-hazard policy."));
            }
        }

        private static void ValidateWriteContentPolicy(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (!policy.HasRequiredWriteContentPolicy(binding))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.MissingWriteContentPolicy,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' writes Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}' without an explicit write content policy."));
            }

            if (policy.HasContradictoryWriteContentPolicy(binding))
            {
                diagnostics.AddError(
                    AtlasWriteHazardDiagnosticCodes.ContradictoryWriteContentPolicy,
                    location,
                    AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' declares both discard-before-write and preserve-existing-content for Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}'."));
            }
        }

        private static void ValidateDeterministicOrdering(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsDeterministicWriteDeclaration(binding))
            {
                return;
            }

            diagnostics.AddError(
                AtlasWriteHazardDiagnosticCodes.DeterministicWriteOrderRejected,
                location,
                AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' writes Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}' without the deterministic-order declaration required by the active write-hazard policy."));
        }
    }
}
