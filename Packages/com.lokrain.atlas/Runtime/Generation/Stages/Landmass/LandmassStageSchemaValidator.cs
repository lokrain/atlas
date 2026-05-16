// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/LandmassStageSchemaValidator.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass
//
// Purpose
// - Validate Landmass stage route compatibility against stage-level output requirements.
// - Keep Landmass route/schema checks separate from generic pipeline metadata validation.
// - Prove PrimaryContinent produces the required canonical Landmass outputs before jobs exist.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent;
using Lokrain.Atlas.Stages;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass
{
    /// <summary>
    /// Validates Landmass stage-schema invariants that generic pipeline validation cannot know.
    /// </summary>
    public static class LandmassStageSchemaValidator
    {
        /// <summary>
        /// Validates the accepted PrimaryContinent route shape and Landmass canonical outputs.
        /// </summary>
        public static AtlasDiagnosticBuffer ValidatePrimaryContinentRoute(
            AtlasStageDefinition stage,
            AtlasContractTable contractTable)
        {
            var diagnostics = AtlasDiagnosticBuffer.Create();

            ValidatePrimaryContinentRoute(
                stage,
                contractTable,
                diagnostics);

            return diagnostics;
        }

        /// <summary>
        /// Validates the accepted PrimaryContinent route shape and Landmass canonical outputs into an existing buffer.
        /// </summary>
        public static AtlasDiagnosticBuffer ValidatePrimaryContinentRoute(
            AtlasStageDefinition stage,
            AtlasContractTable contractTable,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (!ValidateInputs(stage, contractTable, diagnostics))
            {
                return diagnostics;
            }

            ValidateStageIdentity(stage, diagnostics);
            ValidatePrimaryContinentOperationSequence(stage, diagnostics);
            ValidateRequiredCanonicalOutputs(stage, contractTable, diagnostics);

            return diagnostics;
        }

        private static bool ValidateInputs(
            AtlasStageDefinition stage,
            AtlasContractTable contractTable,
            AtlasDiagnosticBuffer diagnostics)
        {
            var valid = true;

            if (stage == null)
            {
                diagnostics.AddFatal(
                    LandmassStageDiagnosticCodes.NullStageDefinition,
                    AtlasDiagnosticLocation.Stage(default, LandmassStage.DebugName),
                    new FixedString512Bytes("Landmass PrimaryContinent schema validation requires a stage definition."));

                valid = false;
            }

            if (contractTable == null)
            {
                diagnostics.AddFatal(
                    LandmassStageDiagnosticCodes.NullContractTable,
                    AtlasDiagnosticLocation.Contract(default, new FixedString64Bytes(AtlasLandmassSchemaNames.PrimaryContinentContractTable)),
                    new FixedString512Bytes("Landmass PrimaryContinent schema validation requires a contract table."));

                valid = false;
            }

            return valid;
        }

        private static void ValidateStageIdentity(
            AtlasStageDefinition stage,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (stage.StageId == LandmassStage.Id)
            {
                return;
            }

            diagnostics.AddError(
                LandmassStageDiagnosticCodes.StageIdMismatch,
                AtlasDiagnosticLocation.Stage(stage.StageId.ToStableDataId(), stage.DebugName),
                new FixedString512Bytes("PrimaryContinent route must be attached to the Landmass stage identity."));
        }

        private static void ValidatePrimaryContinentOperationSequence(
            AtlasStageDefinition stage,
            AtlasDiagnosticBuffer diagnostics)
        {
            var expected = PrimaryContinentRoute.CreateOperationIds();

            if (stage.Count != expected.Length)
            {
                diagnostics.AddError(
                    LandmassStageDiagnosticCodes.PrimaryContinentOperationSequenceMismatch,
                    AtlasDiagnosticLocation.Stage(stage.StageId.ToStableDataId(), stage.DebugName),
                    new FixedString512Bytes("Landmass PrimaryContinent route must contain exactly the accepted operation sequence."));

                return;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                var actual = stage[i].OperationId;

                if (actual == expected[i])
                {
                    continue;
                }

                diagnostics.AddError(
                    LandmassStageDiagnosticCodes.PrimaryContinentOperationSequenceMismatch,
                    AtlasDiagnosticLocation.Create(
                        AtlasDiagnosticLocationKind.Operation,
                        actual.ToStableDataId(),
                        AtlasDiagnosticLocation.NoIndex,
                        i,
                        AtlasDiagnosticLocation.NoIndex,
                        stage[i].DebugName),
                    new FixedString512Bytes("Landmass PrimaryContinent route operation order does not match the accepted schema."));
            }
        }

        private static void ValidateRequiredCanonicalOutputs(
            AtlasStageDefinition stage,
            AtlasContractTable contractTable,
            AtlasDiagnosticBuffer diagnostics)
        {
            var requiredOutputs = LandmassStage.CreateRequiredCanonicalOutputFieldIds();

            for (var i = 0; i < requiredOutputs.Length; i++)
            {
                var fieldId = requiredOutputs[i];

                if (!contractTable.TryGetContract(fieldId, out var contract))
                {
                    diagnostics.AddError(
                        LandmassStageDiagnosticCodes.MissingRequiredCanonicalOutput,
                        AtlasDiagnosticLocation.Contract(fieldId),
                        new FixedString512Bytes("Landmass route contract table is missing a required canonical output field."));

                    continue;
                }

                if (contract.Role != AtlasFieldRole.Canonical)
                {
                    diagnostics.AddError(
                        LandmassStageDiagnosticCodes.RequiredOutputIsNotCanonical,
                        AtlasDiagnosticLocation.Contract(fieldId, contract.DebugName),
                        new FixedString512Bytes("Landmass required output field must be declared as canonical."));
                }

                if (!RouteWritesField(stage, fieldId))
                {
                    diagnostics.AddError(
                        LandmassStageDiagnosticCodes.RequiredOutputHasNoRouteProducer,
                        AtlasDiagnosticLocation.Contract(fieldId, contract.DebugName),
                        new FixedString512Bytes("Landmass PrimaryContinent route does not write a required canonical output field."));
                }
            }
        }

        private static bool RouteWritesField(
            AtlasStageDefinition stage,
            StableDataId fieldId)
        {
            for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
            {
                var operation = stage[operationIndex];

                for (var accessIndex = 0; accessIndex < operation.Count; accessIndex++)
                {
                    var access = operation[accessIndex];

                    if (access.FieldId == fieldId &&
                        access.WritesContent &&
                        access.WritesFullLogicalContent)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
