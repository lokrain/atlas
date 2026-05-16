// Packages/com.lokrain.atlas/Tests/Runtime/Generation/Stages/Landmass/LandmassStageSchemaTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Tests
//
// Purpose
// - Verify the Landmass stage schema and PrimaryContinent route policy.
// - Protect stage identity, route operation order, allowed-route policy, and required canonical outputs.
// - Keep schema tests separate from operation executor, scheduler, and job tests.

using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Generation.Operations.CompleteContinentArea;
using Lokrain.Atlas.Generation.Operations.ComposeBaseElevation;
using Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability;
using Lokrain.Atlas.Generation.Operations.FormContinentCandidate;
using Lokrain.Atlas.Generation.Operations.PreserveMainContinent;
using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Tests
{
    public sealed class LandmassStageSchemaTests
    {
        private static readonly AtlasDiagnosticCode OperationNotAllowedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 319);

        private static readonly AtlasDiagnosticCode MissingRequiredOperationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 321);

        private static readonly AtlasDiagnosticCode RequiredOperationOrderViolationCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 322);

        private static readonly AtlasPipelineId TestPipelineId =
            new(0x9243_BBA5_6F35_B610UL, 0x9566_A9E6_87CC_B835UL, 1);

        private static readonly AtlasStageId WrongStageId =
            new(0xE582_5E7B_DF97_D7B1UL, 0x4B70_2A6E_7658_5E20UL, 1);

        [Test]
        public void CreatePrimaryContinentStageDefinition_UsesLandmassIdentityAndRouteOperations()
        {
            var stage = LandmassStage.CreatePrimaryContinentStageDefinition();

            Assert.That(stage.StageId, Is.EqualTo(LandmassStage.Id));
            Assert.That(stage.DebugName, Is.EqualTo(LandmassStage.DebugName));
            Assert.That(stage.Count, Is.EqualTo(PrimaryContinentRoute.OperationCount));
            Assert.That(stage[0].OperationId, Is.EqualTo(EvaluateContinentSuitabilityOperation.Id));
            Assert.That(stage[1].OperationId, Is.EqualTo(FormContinentCandidateOperation.Id));
            Assert.That(stage[2].OperationId, Is.EqualTo(PreserveMainContinentOperation.Id));
            Assert.That(stage[3].OperationId, Is.EqualTo(CompleteContinentAreaOperation.Id));
            Assert.That(stage[4].OperationId, Is.EqualTo(ComposeBaseElevationOperation.Id));
        }

        [Test]
        public void CreateRequiredCanonicalOutputFieldIds_ReturnsLandmassStageOutputs()
        {
            var outputs = LandmassStage.CreateRequiredCanonicalOutputFieldIds();

            Assert.That(outputs.Length, Is.EqualTo(LandmassStage.RequiredCanonicalOutputCount));
            Assert.That(outputs[0], Is.EqualTo(AtlasLandmassFieldIds.LandMask));
            Assert.That(outputs[1], Is.EqualTo(AtlasLandmassFieldIds.OceanMask));
            Assert.That(outputs[2], Is.EqualTo(AtlasLandmassFieldIds.LandLabel));
            Assert.That(outputs[3], Is.EqualTo(AtlasLandmassFieldIds.BaseElevation));
        }

        [Test]
        public void PrimaryContinentPipelinePolicy_AcceptsAcceptedLandmassRoute()
        {
            var diagnostics = AtlasPipelinePolicyValidator.Validate(
                CreatePipeline(LandmassStage.CreatePrimaryContinentStageDefinition()),
                LandmassStage.CreatePrimaryContinentPipelineValidationPolicy());

            Assert.That(diagnostics.HasFailures, Is.False);
        }

        [Test]
        public void PrimaryContinentPipelinePolicy_RejectsMissingRouteOperation()
        {
            var stage = AtlasStageDefinition.Create(
                LandmassStage.Id,
                LandmassStage.DebugName,
                EvaluateContinentSuitabilityOperation.CreateDefinition(),
                FormContinentCandidateOperation.CreateDefinition(),
                PreserveMainContinentOperation.CreateDefinition(),
                CompleteContinentAreaOperation.CreateDefinition());

            var diagnostics = AtlasPipelinePolicyValidator.Validate(
                CreatePipeline(stage),
                LandmassStage.CreatePrimaryContinentPipelineValidationPolicy());

            Assert.That(diagnostics.HasFailures, Is.True);
            Assert.That(diagnostics.ContainsCode(MissingRequiredOperationCode), Is.True);
        }

        [Test]
        public void PrimaryContinentPipelinePolicy_RejectsWrongRouteOperationOrder()
        {
            var stage = AtlasStageDefinition.Create(
                LandmassStage.Id,
                LandmassStage.DebugName,
                EvaluateContinentSuitabilityOperation.CreateDefinition(),
                FormContinentCandidateOperation.CreateDefinition(),
                PreserveMainContinentOperation.CreateDefinition(),
                ComposeBaseElevationOperation.CreateDefinition(),
                CompleteContinentAreaOperation.CreateDefinition());

            var diagnostics = AtlasPipelinePolicyValidator.Validate(
                CreatePipeline(stage),
                LandmassStage.CreatePrimaryContinentPipelineValidationPolicy());

            Assert.That(diagnostics.HasFailures, Is.True);
            Assert.That(diagnostics.ContainsCode(RequiredOperationOrderViolationCode), Is.True);
        }

        [Test]
        public void PrimaryContinentPipelinePolicy_RejectsUnsupportedRouteOperation()
        {
            var stage = AtlasStageDefinition.Create(
                LandmassStage.Id,
                LandmassStage.DebugName,
                EvaluateContinentSuitabilityOperation.CreateDefinition(),
                FormContinentCandidateOperation.CreateDefinition(),
                CreateUnsupportedRouteOperation(),
                PreserveMainContinentOperation.CreateDefinition(),
                CompleteContinentAreaOperation.CreateDefinition(),
                ComposeBaseElevationOperation.CreateDefinition());

            var diagnostics = AtlasPipelinePolicyValidator.Validate(
                CreatePipeline(stage),
                LandmassStage.CreatePrimaryContinentPipelineValidationPolicy());

            Assert.That(diagnostics.HasFailures, Is.True);
            Assert.That(diagnostics.ContainsCode(OperationNotAllowedCode), Is.True);
        }

        [Test]
        public void ValidatePrimaryContinentRoute_AcceptsRouteSequenceAndRequiredOutputs()
        {
            var diagnostics = LandmassStageSchemaValidator.ValidatePrimaryContinentRoute(
                LandmassStage.CreatePrimaryContinentStageDefinition(),
                AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable());

            Assert.That(diagnostics.HasFailures, Is.False);
        }

        [Test]
        public void ValidatePrimaryContinentRoute_ReportsWrongStageIdentity()
        {
            var stage = AtlasStageDefinition.Create(
                WrongStageId,
                new FixedString64Bytes("stage.not_landmass"),
                PrimaryContinentRoute.CreateOperationSet());

            var diagnostics = LandmassStageSchemaValidator.ValidatePrimaryContinentRoute(
                stage,
                AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable());

            Assert.That(diagnostics.HasFailures, Is.True);
            Assert.That(diagnostics.ContainsCode(LandmassStageDiagnosticCodes.StageIdMismatch), Is.True);
        }

        [Test]
        public void ValidatePrimaryContinentRoute_ReportsMissingRequiredCanonicalOutputContract()
        {
            var table = AtlasLandmassFieldCatalog.CreateCatalog().CreateContractTable(
                new FixedString64Bytes("contracts.landmass.primary_continent.missing_base_elevation"),
                AtlasLandmassFieldIds.LandMask,
                AtlasLandmassFieldIds.OceanMask,
                AtlasLandmassFieldIds.LandLabel,
                AtlasLandmassFieldIds.ContinentSuitability,
                AtlasLandmassFieldIds.ContinentSuitabilityCutoff,
                AtlasLandmassFieldIds.ContinentCandidateMask,
                AtlasLandmassFieldIds.ContinentPrimaryMask,
                AtlasLandmassFieldIds.ContinentArea,
                AtlasLandmassFieldIds.ContinentGrowthCutoff);

            var diagnostics = LandmassStageSchemaValidator.ValidatePrimaryContinentRoute(
                LandmassStage.CreatePrimaryContinentStageDefinition(),
                table);

            Assert.That(diagnostics.HasFailures, Is.True);
            Assert.That(diagnostics.ContainsCode(LandmassStageDiagnosticCodes.MissingRequiredCanonicalOutput), Is.True);
        }

        [Test]
        public void ValidatePrimaryContinentRoute_ReportsRequiredOutputWithoutRouteProducer()
        {
            var stage = AtlasStageDefinition.Create(
                LandmassStage.Id,
                LandmassStage.DebugName,
                EvaluateContinentSuitabilityOperation.CreateDefinition(),
                FormContinentCandidateOperation.CreateDefinition(),
                PreserveMainContinentOperation.CreateDefinition(),
                CompleteContinentAreaOperation.CreateDefinition());

            var diagnostics = LandmassStageSchemaValidator.ValidatePrimaryContinentRoute(
                stage,
                AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable());

            Assert.That(diagnostics.HasFailures, Is.True);
            Assert.That(diagnostics.ContainsCode(LandmassStageDiagnosticCodes.RequiredOutputHasNoRouteProducer), Is.True);
        }

        private static AtlasPipelineDefinition CreatePipeline(
            AtlasStageDefinition stage)
        {
            return AtlasPipelineDefinition.Create(
                TestPipelineId,
                new FixedString64Bytes("pipeline.landmass.primary_continent.test"),
                stage);
        }

        private static AtlasOperationDefinition CreateUnsupportedRouteOperation()
        {
            return AtlasOperationDefinition.Create(
                new AtlasOperationId(0x3557_0E59_1B2C_79D1UL, 0x9B50_D467_A748_D98BUL, 1),
                new FixedString64Bytes("operation.landmass.unsupported_route_operation"),
                AtlasOperationRole.Debugging,
                AtlasOperationAccess.WriteFullCapacity<AtlasLandmassFields.LandMask, byte>());
        }
    }
}
