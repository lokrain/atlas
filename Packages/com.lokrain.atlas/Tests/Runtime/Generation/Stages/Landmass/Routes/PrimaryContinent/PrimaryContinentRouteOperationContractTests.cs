// Packages/com.lokrain.atlas/Tests/Runtime/Generation/Stages/Landmass/Routes/PrimaryContinent/PrimaryContinentRouteOperationContractTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent.Tests
//
// Purpose
// - Verify the Landmass stage PrimaryContinent route operation contracts.
// - Protect route operation order, access declarations, roles, write coverage, and dataflow.
// - Ensure operation contracts are represented before executors, schedulers, and jobs are added.

using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Generation.Stages.Landmass.Fields;
using Lokrain.Atlas.Generation.Operations.CompleteContinentArea;
using Lokrain.Atlas.Generation.Operations.ComposeBaseElevation;
using Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability;
using Lokrain.Atlas.Generation.Operations.FormContinentCandidate;
using Lokrain.Atlas.Generation.Operations.PreserveMainContinent;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent.Tests
{
    public sealed class PrimaryContinentRouteOperationContractTests
    {
        private static readonly AtlasDiagnosticCode ReadBeforeWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 100);

        private static readonly AtlasDiagnosticCode PreserveBeforeWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 101);

        private static readonly AtlasStageId TestLandmassStageId =
            new(0x891B_EC3B_D6CA_1F51UL, 0x577A_09C0_0FB5_533DUL, 1);

        private static readonly AtlasPipelineId TestPipelineId =
            new(0x10C8_76A5_A0D8_B6B9UL, 0x28C6_7619_7434_1D42UL, 1);

        [Test]
        public void CreateOperationCatalog_ReturnsAllOperationsInDeterministicRouteOrder()
        {
            var catalog = PrimaryContinentRoute.CreateOperationCatalog();

            Assert.That(catalog.Name, Is.EqualTo(PrimaryContinentRoute.OperationCatalogName));
            Assert.That(catalog.Count, Is.EqualTo(PrimaryContinentRoute.OperationCount));

            AssertOperation(
                catalog[0],
                EvaluateContinentSuitabilityOperation.Id,
                EvaluateContinentSuitabilityOperation.Name,
                AtlasOperationRole.SupportGeneration,
                expectedAccessCount: 2);

            AssertOperation(
                catalog[1],
                FormContinentCandidateOperation.Id,
                FormContinentCandidateOperation.Name,
                AtlasOperationRole.TopologyProcessing,
                expectedAccessCount: 3);

            AssertOperation(
                catalog[2],
                PreserveMainContinentOperation.Id,
                PreserveMainContinentOperation.Name,
                AtlasOperationRole.TopologyProcessing,
                expectedAccessCount: 3);

            AssertOperation(
                catalog[3],
                CompleteContinentAreaOperation.Id,
                CompleteContinentAreaOperation.Name,
                AtlasOperationRole.CanonicalGeneration,
                expectedAccessCount: 7);

            AssertOperation(
                catalog[4],
                ComposeBaseElevationOperation.Id,
                ComposeBaseElevationOperation.Name,
                AtlasOperationRole.CanonicalGeneration,
                expectedAccessCount: 5);
        }

        [Test]
        public void CreateOperationSet_PreservesRouteOperationOrder()
        {
            var operations = PrimaryContinentRoute.CreateOperationSet();

            Assert.That(operations.Name, Is.EqualTo(PrimaryContinentRoute.OperationSetName));
            Assert.That(operations.Count, Is.EqualTo(PrimaryContinentRoute.OperationCount));
            Assert.That(operations[0].OperationId, Is.EqualTo(EvaluateContinentSuitabilityOperation.Id));
            Assert.That(operations[1].OperationId, Is.EqualTo(FormContinentCandidateOperation.Id));
            Assert.That(operations[2].OperationId, Is.EqualTo(PreserveMainContinentOperation.Id));
            Assert.That(operations[3].OperationId, Is.EqualTo(CompleteContinentAreaOperation.Id));
            Assert.That(operations[4].OperationId, Is.EqualTo(ComposeBaseElevationOperation.Id));
        }

        [Test]
        public void EvaluateContinentSuitability_WritesSuitabilityAndCutoffFullCapacity()
        {
            var operation = EvaluateContinentSuitabilityOperation.CreateDefinition();

            AssertWriteFullCapacity(
                operation[0],
                AtlasLandmassFieldIds.ContinentSuitability,
                AtlasLandmassFieldNames.ContinentSuitability,
                AtlasFieldRole.StageTransient);

            AssertWriteFullCapacity(
                operation[1],
                AtlasLandmassFieldIds.ContinentSuitabilityCutoff,
                AtlasLandmassFieldNames.ContinentSuitabilityCutoff,
                AtlasFieldRole.StageTransient);
        }

        [Test]
        public void FormContinentCandidate_ReadsSuitabilityAndCutoffThenWritesCandidateMask()
        {
            var operation = FormContinentCandidateOperation.CreateDefinition();

            AssertRead(operation[0], AtlasLandmassFieldIds.ContinentSuitability, AtlasLandmassFieldNames.ContinentSuitability);
            AssertRead(operation[1], AtlasLandmassFieldIds.ContinentSuitabilityCutoff, AtlasLandmassFieldNames.ContinentSuitabilityCutoff);
            AssertWriteFullCapacity(operation[2], AtlasLandmassFieldIds.ContinentCandidateMask, AtlasLandmassFieldNames.ContinentCandidateMask, AtlasFieldRole.StageTransient);
        }

        [Test]
        public void PreserveMainContinent_ReadsCandidateMaskThenWritesPrimaryMaskAndArea()
        {
            var operation = PreserveMainContinentOperation.CreateDefinition();

            AssertRead(operation[0], AtlasLandmassFieldIds.ContinentCandidateMask, AtlasLandmassFieldNames.ContinentCandidateMask);
            AssertWriteFullCapacity(operation[1], AtlasLandmassFieldIds.ContinentPrimaryMask, AtlasLandmassFieldNames.ContinentPrimaryMask, AtlasFieldRole.StageTransient);
            AssertWriteFullCapacity(operation[2], AtlasLandmassFieldIds.ContinentArea, AtlasLandmassFieldNames.ContinentArea, AtlasFieldRole.StageTransient);
        }

        [Test]
        public void CompleteContinentArea_ReadsAndMutatesTransientTopologyThenWritesCanonicalMasks()
        {
            var operation = CompleteContinentAreaOperation.CreateDefinition();

            AssertRead(operation[0], AtlasLandmassFieldIds.ContinentSuitability, AtlasLandmassFieldNames.ContinentSuitability);
            AssertReadWrite(operation[1], AtlasLandmassFieldIds.ContinentPrimaryMask, AtlasLandmassFieldNames.ContinentPrimaryMask, AtlasWriteCoverage.FullLogicalLength);
            AssertReadWrite(operation[2], AtlasLandmassFieldIds.ContinentArea, AtlasLandmassFieldNames.ContinentArea, AtlasWriteCoverage.FullCapacity);
            AssertWriteFullCapacity(operation[3], AtlasLandmassFieldIds.ContinentGrowthCutoff, AtlasLandmassFieldNames.ContinentGrowthCutoff, AtlasFieldRole.StageTransient);
            AssertWriteFullCapacity(operation[4], AtlasLandmassFieldIds.LandMask, AtlasLandmassFieldNames.LandMask, AtlasFieldRole.Canonical);
            AssertWriteFullCapacity(operation[5], AtlasLandmassFieldIds.OceanMask, AtlasLandmassFieldNames.OceanMask, AtlasFieldRole.Canonical);
            AssertWriteFullCapacity(operation[6], AtlasLandmassFieldIds.LandLabel, AtlasLandmassFieldNames.LandLabel, AtlasFieldRole.Canonical);
        }

        [Test]
        public void ComposeBaseElevation_ReadsAcceptedTopologyAndWritesBaseElevation()
        {
            var operation = ComposeBaseElevationOperation.CreateDefinition();

            AssertRead(operation[0], AtlasLandmassFieldIds.LandMask, AtlasLandmassFieldNames.LandMask);
            AssertRead(operation[1], AtlasLandmassFieldIds.OceanMask, AtlasLandmassFieldNames.OceanMask);
            AssertRead(operation[2], AtlasLandmassFieldIds.LandLabel, AtlasLandmassFieldNames.LandLabel);
            AssertRead(operation[3], AtlasLandmassFieldIds.ContinentSuitability, AtlasLandmassFieldNames.ContinentSuitability);
            AssertWriteFullCapacity(operation[4], AtlasLandmassFieldIds.BaseElevation, AtlasLandmassFieldNames.BaseElevation, AtlasFieldRole.Canonical);
        }

        [Test]
        public void PrimaryContinentRouteOrder_CompilesAndPassesStrictDataflow()
        {
            var result = AtlasCompilationWorkflow.CompileValidated(
                CreatePipeline(PrimaryContinentRoute.CreateOperationSet()),
                AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable(),
                AtlasPipelineValidationPolicy.Open,
                AtlasDataflowValidationPolicy.Strict,
                AtlasWriteHazardValidationPolicy.ProductionDefault);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.HasPlan, Is.True);

            var plan = result.GetRequiredPlan();
            Assert.That(plan.Count, Is.EqualTo(1));
            Assert.That(plan.OperationCount, Is.EqualTo(PrimaryContinentRoute.OperationCount));
            Assert.That(plan[0][0].OperationId, Is.EqualTo(EvaluateContinentSuitabilityOperation.Id));
            Assert.That(plan[0][4].OperationId, Is.EqualTo(ComposeBaseElevationOperation.Id));
        }

        [Test]
        public void ComposeBaseElevationBeforeTopologyPublish_FailsStrictDataflow()
        {
            var operations = AtlasOperationSet.Create(
                new FixedString64Bytes("operations.landmass.invalid.compose_first"),
                ComposeBaseElevationOperation.CreateDefinition(),
                EvaluateContinentSuitabilityOperation.CreateDefinition());

            var result = AtlasCompilationWorkflow.CompileValidated(
                CreatePipeline(operations),
                AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable(),
                AtlasPipelineValidationPolicy.Open,
                AtlasDataflowValidationPolicy.Strict,
                AtlasWriteHazardValidationPolicy.ProductionDefault);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.HasPlan, Is.False);
            Assert.That(result.ContainsCode(ReadBeforeWriteCode), Is.True);
        }

        [Test]
        public void CompleteContinentAreaBeforePreserveMainContinent_FailsStrictDataflowOnPreservedContentMutation()
        {
            var operations = AtlasOperationSet.Create(
                new FixedString64Bytes("operations.landmass.invalid.complete_before_preserve"),
                EvaluateContinentSuitabilityOperation.CreateDefinition(),
                CompleteContinentAreaOperation.CreateDefinition());

            var result = AtlasCompilationWorkflow.CompileValidated(
                CreatePipeline(operations),
                AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable(),
                AtlasPipelineValidationPolicy.Open,
                AtlasDataflowValidationPolicy.Strict,
                AtlasWriteHazardValidationPolicy.ProductionDefault);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.HasPlan, Is.False);
            Assert.That(result.ContainsCode(PreserveBeforeWriteCode), Is.True);
        }

        private static AtlasPipelineDefinition CreatePipeline(
            AtlasOperationSet operations)
        {
            var stage = AtlasStageDefinition.Create(
                TestLandmassStageId,
                new FixedString64Bytes("stage.landmass.test"),
                operations);

            return AtlasPipelineDefinition.Create(
                TestPipelineId,
                new FixedString64Bytes("pipeline.landmass.test"),
                stage);
        }

        private static void AssertOperation(
            AtlasOperationDefinition operation,
            AtlasOperationId expectedOperationId,
            string expectedDebugName,
            AtlasOperationRole expectedRole,
            int expectedAccessCount)
        {
            Assert.That(operation.OperationId, Is.EqualTo(expectedOperationId));
            Assert.That(operation.DebugName, Is.EqualTo(new FixedString64Bytes(expectedDebugName)));
            Assert.That(operation.Role, Is.EqualTo(expectedRole));
            Assert.That(operation.Count, Is.EqualTo(expectedAccessCount));
        }

        private static void AssertRead(
            AtlasOperationAccess access,
            StableDataId expectedFieldId,
            string expectedBindingName)
        {
            Assert.That(access.FieldId, Is.EqualTo(expectedFieldId));
            Assert.That(access.BindingName, Is.EqualTo(new FixedString64Bytes(expectedBindingName)));
            Assert.That(access.Mode, Is.EqualTo(AtlasOperationAccessMode.Read));
            Assert.That(access.Flags, Is.EqualTo(AtlasOperationAccessFlags.None));
            Assert.That(access.WriteCoverage, Is.EqualTo(AtlasWriteCoverage.None));
            Assert.That(access.ReadsContent, Is.True);
            Assert.That(access.WritesContent, Is.False);
        }

        private static void AssertWriteFullCapacity(
            AtlasOperationAccess access,
            StableDataId expectedFieldId,
            string expectedBindingName,
            AtlasFieldRole expectedRole)
        {
            var contract = AtlasLandmassFieldCatalog.CreateCatalog()[expectedFieldId];

            Assert.That(contract.Role, Is.EqualTo(expectedRole));
            Assert.That(access.FieldId, Is.EqualTo(expectedFieldId));
            Assert.That(access.BindingName, Is.EqualTo(new FixedString64Bytes(expectedBindingName)));
            Assert.That(access.Mode, Is.EqualTo(AtlasOperationAccessMode.Write));
            Assert.That(access.Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite), Is.True);
            Assert.That(access.Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite), Is.True);
            Assert.That(access.WriteCoverage, Is.EqualTo(AtlasWriteCoverage.FullCapacity));
            Assert.That(access.ReadsContent, Is.False);
            Assert.That(access.WritesContent, Is.True);
            Assert.That(access.WritesFullLogicalContent, Is.True);
        }

        private static void AssertReadWrite(
            AtlasOperationAccess access,
            StableDataId expectedFieldId,
            string expectedBindingName,
            AtlasWriteCoverage expectedWriteCoverage)
        {
            Assert.That(access.FieldId, Is.EqualTo(expectedFieldId));
            Assert.That(access.BindingName, Is.EqualTo(new FixedString64Bytes(expectedBindingName)));
            Assert.That(access.Mode, Is.EqualTo(AtlasOperationAccessMode.ReadWrite));
            Assert.That(access.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent), Is.True);
            Assert.That(access.Flags.HasAny(AtlasOperationAccessFlags.RequiresExclusiveWrite), Is.True);
            Assert.That(access.WriteCoverage, Is.EqualTo(expectedWriteCoverage));
            Assert.That(access.ReadsContent, Is.True);
            Assert.That(access.WritesContent, Is.True);
            Assert.That(access.WritesFullLogicalContent, Is.True);
        }
    }
}
