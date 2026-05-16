// Packages/com.lokrain.atlas/Tests/Runtime/Artifacts/AtlasArtifactCaptureTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts.Tests
//
// Purpose
// - Verify artifact capture is owned by AtlasArtifactCapture rather than AtlasArtifact data.
// - Verify logical capture serializes logical content bytes and preserves capacity metadata.
// - Verify capacity-snapshot capture remains explicit.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Executors;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Artifacts.Tests
{
    public sealed class AtlasArtifactCaptureTests
    {
        private static readonly StableDataId FieldId =
            new(0xA100_0000_0000_0001UL, 0xA200_0000_0000_0001UL, 1);

        private static readonly StableDataId StageTransientFieldId =
            new(0xA100_0000_0000_0002UL, 0xA200_0000_0000_0002UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0xA300_0000_0000_0001UL, 0xA400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xA500_0000_0000_0001UL, 0xA600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xA700_0000_0000_0001UL, 0xA800_0000_0000_0001UL, 1);

        [Test]
        public void Capture_CompletedExecutionContext_ReturnsLogicalContentArtifact()
        {
            var pipeline = CreatePipeline();
            var contracts = CreateContractTable();
            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);
            var request = AtlasRunRequest.Create(
                pipeline,
                contracts,
                registry,
                Allocator.TempJob,
                completeExecution: true,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            var artifact = AtlasArtifactCapture.Capture(
                result.GetRequiredExecutionContext(),
                computeContentHashes: true);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(artifact.FieldCount, Is.EqualTo(1));
            Assert.That(artifact.SerializesLogicalContent, Is.True);
            Assert.That(artifact.SerializesCapacity, Is.True);
            Assert.That(artifact.PayloadByteLength, Is.EqualTo(artifact.Header.TotalByteLength));
            Assert.That(artifact.Header.HasContentHash, Is.True);
            Assert.That(artifact[0].HasContentHash, Is.True);
        }

        [Test]
        public void CaptureCapacitySnapshot_CompletedWorkspace_ReturnsExplicitCapacityPayloadArtifact()
        {
            var pipeline = CreatePipeline();
            var contracts = CreateContractTable();
            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);
            var request = AtlasRunRequest.Create(
                pipeline,
                contracts,
                registry,
                Allocator.TempJob,
                completeExecution: true,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            var artifact = AtlasArtifactCapture.CaptureCapacitySnapshot(
                result.Plan,
                result.GetRequiredWorkspace(),
                computeContentHashes: true);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(artifact.FieldCount, Is.EqualTo(1));
            Assert.That(artifact.SerializesCapacity, Is.True);
            Assert.That(artifact.PayloadByteLength, Is.EqualTo(artifact.Header.TotalByteCapacity));
            Assert.That(artifact.Header.HasContentHash, Is.True);
            Assert.That(artifact[0].HasContentHash, Is.True);
        }

        [Test]
        public void Capture_DefaultProfile_ExcludesStageTransientFields()
        {
            var pipeline = CreatePipelineWithStageTransientWrite();
            var contracts = CreateContractTableWithStageTransient();
            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);
            var request = AtlasRunRequest.Create(
                pipeline,
                contracts,
                registry,
                Allocator.TempJob,
                completeExecution: true,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            var artifact = AtlasArtifactCapture.Capture(
                result.GetRequiredExecutionContext(),
                computeContentHashes: false);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.GetRequiredWorkspace().Count, Is.EqualTo(2));
            Assert.That(artifact.Header.ContractCount, Is.EqualTo(2));
            Assert.That(artifact.FieldCount, Is.EqualTo(1));
            Assert.That(artifact.TryGetField(FieldId, out var canonicalField), Is.True);
            Assert.That(canonicalField.Role, Is.EqualTo(AtlasFieldRole.Canonical));
            Assert.That(artifact.TryGetField(StageTransientFieldId, out _), Is.False);
            Assert.That(artifact.PayloadByteLength, Is.EqualTo(canonicalField.PayloadByteLength));
        }

        private static AtlasPipelineDefinition CreatePipeline()
        {
            var operation = AtlasOperationDefinition.Create(
                OperationId,
                new FixedString64Bytes("artifact.capture.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.capture.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("artifact.capture.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("artifact.capture.pipeline"),
                stage);
        }

        private static AtlasPipelineDefinition CreatePipelineWithStageTransientWrite()
        {
            var operation = AtlasOperationDefinition.Create(
                OperationId,
                new FixedString64Bytes("artifact.capture.transient.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.capture.field")),
                AtlasOperationAccess.Create(
                    StageTransientFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.capture.transient")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("artifact.capture.transient.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("artifact.capture.transient.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("artifact.capture.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.capture.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("artifact.capture.field")));
        }

        private static AtlasContractTable CreateContractTableWithStageTransient()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("artifact.capture.transient.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.capture.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("artifact.capture.field")),
                AtlasContractFactory.Create<int>(
                    StageTransientFieldId,
                    AtlasFieldRole.StageTransient,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Plan,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.capture.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.None,
                    new FixedString64Bytes("artifact.capture.transient")));
        }
    }
}
