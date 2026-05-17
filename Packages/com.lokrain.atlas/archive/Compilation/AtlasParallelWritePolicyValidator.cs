// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasParallelWritePolicyValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate parallel/exclusive write flag declarations on compiled operation bindings.
// - Keep scheduler-adjacent write-declaration policy separate from storage/content write policy.
// - Preserve AtlasWriteHazardValidator public diagnostics and traversal behavior.

using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Validates parallel and exclusive write declarations for compiled bindings.
    /// </summary>
    internal static class AtlasParallelWritePolicyValidator
    {
        public static void Validate(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            ValidateParallelDeclaration(
                operation,
                binding,
                location,
                policy,
                diagnostics);

            ValidateExclusiveDeclaration(
                operation,
                binding,
                location,
                policy,
                diagnostics);
        }

        private static void ValidateParallelDeclaration(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsParallelWriteDeclaration(binding))
            {
                return;
            }

            var reason = binding.WritesContent
                ? $"Field '{AtlasDiagnosticText.Name(binding.Contract.DebugName)}' does not declare compatible parallel-write permission."
                : "the binding does not write content.";

            diagnostics.AddError(
                AtlasWriteHazardDiagnosticCodes.ParallelWriteRejected,
                location,
                AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' declares parallel write access, but {reason}"));
        }

        private static void ValidateExclusiveDeclaration(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location,
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (policy.AllowsExclusiveWriteDeclaration(binding))
            {
                return;
            }

            diagnostics.AddError(
                AtlasWriteHazardDiagnosticCodes.ExclusiveWriteRejected,
                location,
                AtlasDiagnosticText.Message($"Atlas operation '{AtlasDiagnosticText.Name(operation.DebugName)}' binding '{AtlasDiagnosticText.Name(binding.BindingName)}' declares exclusive write access, but the binding does not write content."));
        }
    }
}
