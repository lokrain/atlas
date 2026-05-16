// Packages/com.lokrain.atlas/Tests/Runtime/Artifacts/AtlasArtifactFileWriterTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts.Tests
//
// Purpose
// - Verify artifact file writing is owned by AtlasArtifactFileWriter.
// - Verify file writer delegates binary schema output to AtlasArtifactBinaryWriter.
// - Verify file-path validation is isolated from binary stream serialization.

using System;
using System.IO;
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
    public sealed class AtlasArtifactFileWriterTests
    {
        private static readonly StableDataId FieldId =
            new(0xC100_0000_0000_0001UL, 0xC200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0xC300_0000_0000_0001UL, 0xC400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xC500_0000_0000_0001UL, 0xC600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xC700_0000_0000_0001UL, 0xC800_0000_0000_0001UL, 1);

        [Test]
        public void WriteToFile_ValidArtifact_WritesSameBytesAsBinaryWriter()
        {
            var artifact = CreateArtifact();
            var filePath = CreateTempArtifactPath();

            using var expectedStream = new MemoryStream();
            AtlasArtifactBinaryWriter.WriteToStream(
                artifact,
                expectedStream);

            AtlasArtifactFileWriter.WriteToFile(
                artifact,
                filePath,
                overwrite: true);

            var actualBytes = File.ReadAllBytes(filePath);

            Assert.That(actualBytes, Is.EqualTo(expectedStream.ToArray()));
        }

        [Test]
        public void WriteToFile_WhitespacePath_ThrowsArgumentException()
        {
            var artifact = CreateArtifact();

            Assert.Throws<ArgumentException>(() =>
                AtlasArtifactFileWriter.WriteToFile(
                    artifact,
                    "   "));
        }

        [Test]
        public void WriteToFile_ExistingFileWithoutOverwrite_ThrowsIOException()
        {
            var artifact = CreateArtifact();
            var filePath = CreateTempArtifactPath();

            File.WriteAllBytes(
                filePath,
                new byte[] { 1, 2, 3, 4 });

            Assert.Throws<IOException>(() =>
                AtlasArtifactFileWriter.WriteToFile(
                    artifact,
                    filePath,
                    overwrite: false));
        }

        private static AtlasArtifact CreateArtifact()
        {
            var pipeline = CreatePipeline();
            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);
            var request = AtlasRunRequest.Create(
                pipeline,
                CreateContractTable(),
                registry,
                Allocator.TempJob,
                completeExecution: true,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Succeeded, Is.True);

            return AtlasArtifactCapture.Capture(
                result.GetRequiredExecutionContext(),
                computeContentHashes: true);
        }

        private static AtlasPipelineDefinition CreatePipeline()
        {
            var operation = AtlasOperationDefinition.Create(
                OperationId,
                new FixedString64Bytes("artifact.file.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.file.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("artifact.file.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("artifact.file.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("artifact.file.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.file.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("artifact.file.field")));
        }

        private static string CreateTempArtifactPath()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "AtlasArtifactFileWriterTests");

            Directory.CreateDirectory(directory);

            return Path.Combine(
                directory,
                Guid.NewGuid().ToString("N") + ".atlasartifact");
        }
    }
}
