// Packages/com.lokrain.atlas/Tests/Runtime/Compilation/AtlasCompilationWorkflowTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation.Tests
//
// Purpose
// - Verify AtlasCompilationWorkflow pass ordering.
// - Verify successful validated compilation returns a plan.
// - Verify each failing pass stops later passes from producing cascading diagnostics.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Diagnostics;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation.Tests
{
    public sealed class AtlasCompilationWorkflowTests
    {
        private static readonly AtlasDiagnosticCode StageForbiddenCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 310);

        private static readonly AtlasDiagnosticCode NullContractTableCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Compilation, 2);

        private static readonly AtlasDiagnosticCode NullPlanCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 1);

        private static readonly AtlasDiagnosticCode ReadBeforeWriteCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 100);

        private static readonly AtlasDiagnosticCode WriteStorageRejectedCode =
            AtlasDiagnosticCode.Create(AtlasDiagnosticDomain.Validation, 205);

        private static readonly StableDataId WritableFieldId =
            new(0x1000_0000_0000_0001UL, 0x2000_0000_0000_0001UL, 1);

        private static readonly StableDataId ReadOnlyFieldId =
            new(0x1000_0000_0000_0002UL, 0x2000_0000_0000_0002UL, 1);

        private static readonly StableDataId BlobFieldId =
            new(0x1000_0000_0000_0003UL, 0x2000_0000_0000_0003UL, 1);

        private static readonly AtlasOperationId WriteOperationId =
            new(0x3000_0000_0000_0001UL, 0x4000_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId ReadAndBlobWriteOperationId =
            new(0x3000_0000_0000_0002UL, 0x4000_0000_0000_0002UL, 1);

        private static readonly AtlasOperationId BlobWriteOperationId =
            new(0x3000_0000_0000_0003UL, 0x4000_0000_0000_0003UL, 1);

        private static readonly AtlasStageId StageId =
            new(0x5000_0000_0000_0001UL, 0x6000_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0x7000_0000_0000_0001UL, 0x8000_0000_0000_0001UL, 1);

        [Test]
        public void CompileValidated_ValidPlan_ReturnsSuccessfulResultWithPlan()
        {
            var pipeline = CreatePipeline(CreateWriteOperation());
            var contracts = CreateWritableContractTable();

            var result = AtlasCompilationWorkflow.CompileValidated(
                pipeline,
                contracts);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.HasPlan, Is.True);
            Assert.That(result.HasFailures, Is.False);
            Assert.That(result.GetRequiredPlan(), Is.Not.Null);
        }

        [Test]
        public void CompileValidated_PipelinePolicyFailure_StopsBeforeCompilation()
        {
            var pipeline = CreatePipeline(CreateWriteOperation());
            AtlasContractTable contracts = null;

            var policy = AtlasPipelineValidationPolicy.Create(
                new FixedString64Bytes("workflow.reject.stage"),
                AtlasPipelineValidationPolicyFlags.EnforceForbiddenStages,
                forbiddenStages: new[] { StageId });

            var result = AtlasCompilationWorkflow.CompileValidated(
                pipeline,
                contracts,
                policy);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.HasPlan, Is.False);
            Assert.That(result.ContainsCode(StageForbiddenCode), Is.True);
            Assert.That(result.ContainsCode(NullContractTableCode), Is.False);
        }

        [Test]
        public void CompileValidated_CompilationFailure_StopsBeforeCompiledPlanValidation()
        {
            var pipeline = CreatePipeline(CreateWriteOperation());
            AtlasContractTable contracts = null;

            var result = AtlasCompilationWorkflow.CompileValidated(
                pipeline,
                contracts,
                AtlasPipelineValidationPolicy.Open);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.HasPlan, Is.False);
            Assert.That(result.ContainsCode(NullContractTableCode), Is.True);
            Assert.That(result.ContainsCode(NullPlanCode), Is.False);
            Assert.That(result.ContainsCode(ReadBeforeWriteCode), Is.False);
            Assert.That(result.ContainsCode(WriteStorageRejectedCode), Is.False);
        }

        [Test]
        public void CompileValidated_DataflowFailure_StopsBeforeWriteHazardValidation()
        {
            var pipeline = CreatePipeline(CreateReadBeforeWriteAndBlobWriteOperation());
            var contracts = CreateDataflowAndBlobContractTable();

            var result = AtlasCompilationWorkflow.CompileValidated(
                pipeline,
                contracts,
                AtlasPipelineValidationPolicy.Open,
                AtlasDataflowValidationPolicy.Strict,
                AtlasWriteHazardValidationPolicy.ProductionDefault);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.HasPlan, Is.False);
            Assert.That(result.ContainsCode(ReadBeforeWriteCode), Is.True);
            Assert.That(result.ContainsCode(WriteStorageRejectedCode), Is.False);
        }

        [Test]
        public void CompileValidated_WriteHazardFailure_ReturnsWriteHazardDiagnostic()
        {
            var pipeline = CreatePipeline(CreateBlobWriteOperation());
            var contracts = CreateBlobContractTable();

            var result = AtlasCompilationWorkflow.CompileValidated(
                pipeline,
                contracts,
                AtlasPipelineValidationPolicy.Open,
                AtlasDataflowValidationPolicy.Strict,
                AtlasWriteHazardValidationPolicy.ProductionDefault);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.HasPlan, Is.False);
            Assert.That(result.ContainsCode(ReadBeforeWriteCode), Is.False);
            Assert.That(result.ContainsCode(WriteStorageRejectedCode), Is.True);
        }

        private static AtlasPipelineDefinition CreatePipeline(
            AtlasOperationDefinition operation)
        {
            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("workflow.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("workflow.pipeline"),
                stage);
        }

        private static AtlasOperationDefinition CreateWriteOperation()
        {
            return AtlasOperationDefinition.Create(
                WriteOperationId,
                new FixedString64Bytes("workflow.write"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    WritableFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    new FixedString64Bytes("writable.field")));
        }

        private static AtlasOperationDefinition CreateReadBeforeWriteAndBlobWriteOperation()
        {
            return AtlasOperationDefinition.Create(
                ReadAndBlobWriteOperationId,
                new FixedString64Bytes("workflow.read.and.blob.write"),
                AtlasOperationRole.Validation,
                AtlasOperationAccess.Create(
                    ReadOnlyFieldId,
                    AtlasOperationAccessMode.Read,
                    AtlasOperationAccessFlags.None,
                    new FixedString64Bytes("read.before.write.field")),
                AtlasOperationAccess.Create(
                    BlobFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    new FixedString64Bytes("blob.write.field")));
        }

        private static AtlasOperationDefinition CreateBlobWriteOperation()
        {
            return AtlasOperationDefinition.Create(
                BlobWriteOperationId,
                new FixedString64Bytes("workflow.blob.write"),
                AtlasOperationRole.ArtifactProduction,
                AtlasOperationAccess.Create(
                    BlobFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    new FixedString64Bytes("blob.write.field")));
        }

        private static AtlasContractTable CreateWritableContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workflow.contracts"),
                CreateNativeArrayContract(
                    WritableFieldId,
                    "workflow.writable.field"));
        }

        private static AtlasContractTable CreateDataflowAndBlobContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workflow.contracts"),
                CreateNativeArrayContract(
                    ReadOnlyFieldId,
                    "workflow.read.only.field"),
                CreateBlobContract(
                    BlobFieldId,
                    "workflow.blob.field"));
        }

        private static AtlasContractTable CreateBlobContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workflow.contracts"),
                CreateBlobContract(
                    BlobFieldId,
                    "workflow.blob.field"));
        }

        private static AtlasContract CreateNativeArrayContract(
            StableDataId fieldId,
            string debugName)
        {
            return AtlasContractFactory.Create<int>(
                fieldId,
                AtlasFieldRole.Canonical,
                StorageKind.NativeArray,
                OwnershipPolicy.AtlasOwned,
                LifetimePolicy.Frame,
                AtlasShapeDomain.LinearRows(new FixedString64Bytes("workflow.rows")),
                LengthShape.Fixed(4),
                AtlasFieldFlags.None,
                HashParticipation.Default,
                new FixedString64Bytes(debugName));
        }

        private static AtlasContract CreateBlobContract(
            StableDataId fieldId,
            string debugName)
        {
            return AtlasContractFactory.Create<int>(
                fieldId,
                AtlasFieldRole.Canonical,
                StorageKind.Blob,
                OwnershipPolicy.AtlasOwned,
                LifetimePolicy.Frame,
                AtlasShapeDomain.LinearRows(new FixedString64Bytes("workflow.rows")),
                LengthShape.Fixed(4),
                AtlasFieldFlags.None,
                HashParticipation.Default,
                new FixedString64Bytes(debugName));
        }
    }
}