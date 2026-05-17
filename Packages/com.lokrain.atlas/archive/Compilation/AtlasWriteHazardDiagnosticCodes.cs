// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasWriteHazardDiagnosticCodes.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Own stable diagnostic code values used by write-hazard validation.
// - Keep split write-hazard validators aligned on the same diagnostic ABI.
// - Avoid duplicating numeric diagnostic literals across storage, ordering, and parallel-write validators.

using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Stable diagnostic code values emitted by Atlas write-hazard validation.
    /// </summary>
    internal static class AtlasWriteHazardDiagnosticCodes
    {
        public static readonly AtlasDiagnosticCode InvalidBinding =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 200);

        public static readonly AtlasDiagnosticCode InvalidPresentContract =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 201);

        public static readonly AtlasDiagnosticCode ShapeOnlyWrite =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 202);

        public static readonly AtlasDiagnosticCode WriteOwnershipRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 203);

        public static readonly AtlasDiagnosticCode WriteLifetimeRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 204);

        public static readonly AtlasDiagnosticCode WriteStorageRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 205);

        public static readonly AtlasDiagnosticCode AppendStorageRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 206);

        public static readonly AtlasDiagnosticCode ConsumeStorageRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 207);

        public static readonly AtlasDiagnosticCode MissingWriteContentPolicy =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 208);

        public static readonly AtlasDiagnosticCode ContradictoryWriteContentPolicy =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 209);

        public static readonly AtlasDiagnosticCode ParallelWriteRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 210);

        public static readonly AtlasDiagnosticCode ExclusiveWriteRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 211);

        public static readonly AtlasDiagnosticCode DeterministicWriteOrderRejected =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 212);
    }
}
