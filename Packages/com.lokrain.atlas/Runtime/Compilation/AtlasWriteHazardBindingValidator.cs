// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasWriteHazardBindingValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Validate one compiled binding occurrence for write-hazard correctness.
// - Keep binding-level validation separate from compiled-plan traversal and public facade orchestration.
// - Delegate focused storage/content and parallel/exclusive policies to their dedicated validators.

using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Binding-level write-hazard visitor used by <see cref="AtlasWriteHazardValidator"/>.
    /// </summary>
    internal struct AtlasWriteHazardBindingValidator :
        IAtlasCompiledBindingVisitor
    {
        private readonly AtlasWriteHazardValidationPolicy _policy;
        private readonly AtlasDiagnosticBuffer _diagnostics;

        public AtlasWriteHazardBindingValidator(
            AtlasWriteHazardValidationPolicy policy,
            AtlasDiagnosticBuffer diagnostics)
        {
            _policy = policy;
            _diagnostics = diagnostics;
        }

        /// <summary>
        /// Validates one compiled binding occurrence.
        /// </summary>
        public void VisitBinding(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasCompiledBindingCursor cursor)
        {
            var location = cursor.CreateLocation(binding);

            if (!ValidateBindingPayload(
                    binding,
                    cursor,
                    location))
            {
                return;
            }

            AtlasStorageWritePolicyValidator.ValidateShapeOnlyDeclaration(
                operation,
                binding,
                location,
                _policy,
                _diagnostics);

            if (!binding.IsPresent)
            {
                return;
            }

            if (!ValidatePresentContract(
                    binding,
                    location))
            {
                return;
            }

            AtlasParallelWritePolicyValidator.Validate(
                operation,
                binding,
                location,
                _policy,
                _diagnostics);

            if (!binding.WritesContent)
            {
                return;
            }

            AtlasStorageWritePolicyValidator.ValidateWrite(
                operation,
                binding,
                location,
                _policy,
                _diagnostics);
        }

        private bool ValidateBindingPayload(
            AtlasCompiledBinding binding,
            AtlasCompiledBindingCursor cursor,
            AtlasDiagnosticLocation location)
        {
            if (binding.IsValid)
            {
                return true;
            }

            _diagnostics.AddError(
                AtlasWriteHazardDiagnosticCodes.InvalidBinding,
                location,
                AtlasDiagnosticText.Message($"Atlas write-hazard validation found an invalid binding at stage '{cursor.StageIndex}', operation '{cursor.OperationIndex}', binding '{cursor.BindingIndex}'."));

            return false;
        }

        private bool ValidatePresentContract(
            AtlasCompiledBinding binding,
            AtlasDiagnosticLocation location)
        {
            if (binding.Contract.IsTableReady)
            {
                return true;
            }

            _diagnostics.AddError(
                AtlasWriteHazardDiagnosticCodes.InvalidPresentContract,
                location,
                AtlasDiagnosticText.Message($"Atlas write-hazard validation found present binding '{AtlasDiagnosticText.Name(binding.BindingName)}' with a Contract that is not table-ready."));

            return false;
        }
    }
}
