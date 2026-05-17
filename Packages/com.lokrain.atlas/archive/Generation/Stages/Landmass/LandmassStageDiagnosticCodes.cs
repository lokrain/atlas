// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/LandmassStageDiagnosticCodes.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass
//
// Purpose
// - Own Landmass stage-schema diagnostic code identities.
// - Keep route/schema diagnostics separate from generic pipeline and compiler diagnostics.
// - Provide stable codes for tests, CI output, and editor tooling.

using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Generation.Stages.Landmass
{
    /// <summary>
    /// Diagnostic codes owned by the Landmass stage schema layer.
    /// </summary>
    public static class LandmassStageDiagnosticCodes
    {
        /// <summary>
        /// The validated stage definition was null.
        /// </summary>
        public static readonly AtlasDiagnosticCode NullStageDefinition =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1000);

        /// <summary>
        /// The validated contract table was null.
        /// </summary>
        public static readonly AtlasDiagnosticCode NullContractTable =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1001);

        /// <summary>
        /// The stage identity does not match the Landmass stage contract.
        /// </summary>
        public static readonly AtlasDiagnosticCode StageIdMismatch =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1002);

        /// <summary>
        /// The route operation sequence is not the PrimaryContinent sequence.
        /// </summary>
        public static readonly AtlasDiagnosticCode PrimaryContinentOperationSequenceMismatch =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1003);

        /// <summary>
        /// A required canonical Landmass output field is absent from the contract table.
        /// </summary>
        public static readonly AtlasDiagnosticCode MissingRequiredCanonicalOutput =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1004);

        /// <summary>
        /// A required Landmass output field exists but is not declared as canonical.
        /// </summary>
        public static readonly AtlasDiagnosticCode RequiredOutputIsNotCanonical =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1005);

        /// <summary>
        /// The route operation sequence does not write a required Landmass output field.
        /// </summary>
        public static readonly AtlasDiagnosticCode RequiredOutputHasNoRouteProducer =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Stages, 1006);
    }
}
