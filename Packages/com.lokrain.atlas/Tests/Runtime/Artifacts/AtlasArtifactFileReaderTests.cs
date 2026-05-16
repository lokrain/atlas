// Packages/com.lokrain.atlas/Tests/Runtime/Artifacts/AtlasArtifactFileReaderTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts.Tests
//
// Purpose
// - Verify artifact file reading is owned by AtlasArtifactFileReader.
// - Verify file reader delegates binary schema input to AtlasArtifactBinaryReader.
// - Verify file-path validation is isolated from binary stream deserialization.

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
    public sealed class AtlasArtifactFileReaderTests
    {
        private static readonly StableDataId FieldId =
            new(0xE100_0000_0000_0001UL, 0xE200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0xE300_0000_0000_0001UL, 0xE400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xE500_0000_0000_0001UL, 0xE600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xE700_0000_0000_0001UL, 0xE800_0000_0000_0001UL, 1);

        [Test]
        public void ReadFromFile_WriterOutput_RoundTripsArtifact()
        {
            var expected = CreateArtifact();
            var filePath = CreateTempArtifactPath();

            AtlasArtifactFileWriter.WriteToFile(
                expected,
                filePath,
                overwrite: true);

            var actual = AtlasArtifactFileReader.ReadFromFile(filePath);

            AssertArtifactEqual(actual, expected);
        }

        [Test]
        public void AtlasArtifactReaderFacade_ProducesSameArtifactAsFileReader()
        {
            var expected = CreateArtifact();
            var filePath = CreateTempArtifactPath();

            AtlasArtifactFileWriter.WriteToFile(
                expected,
                filePath,
                overwrite: true);

            var fileReaderArtifact = AtlasArtifactFileReader.ReadFromFile(filePath);
            var facadeArtifact = AtlasArtifactReader.ReadFromFile(filePath);

            AssertArtifactEqual(facadeArtifact, fileReaderArtifact);
        }

        [Test]
        public void ReadFromFile_WhitespacePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                AtlasArtifactFileReader.ReadFromFile("   "));
        }

        [Test]
        public void ReadFromFile_MissingFile_ThrowsFileNotFoundException()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "AtlasArtifactFileReaderTests");

            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(
                directory,
                Guid.NewGuid().ToString("N") + ".atlasartifact");

            Assert.Throws<FileNotFoundException>(() =>
                AtlasArtifactFileReader.ReadFromFile(filePath));
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
                new FixedString64Bytes("artifact.file.reader.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.file.reader.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("artifact.file.reader.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("artifact.file.reader.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("artifact.file.reader.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.file.reader.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("artifact.file.reader.field")));
        }

        private static string CreateTempArtifactPath()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "AtlasArtifactFileReaderTests");

            Directory.CreateDirectory(directory);

            return Path.Combine(
                directory,
                Guid.NewGuid().ToString("N") + ".atlasartifact");
        }

        private static void AssertArtifactEqual(
            AtlasArtifact actual,
            AtlasArtifact expected)
        {
            Assert.That(actual.Header, Is.EqualTo(expected.Header));
            Assert.That(actual.FieldCount, Is.EqualTo(expected.FieldCount));
            Assert.That(actual.PayloadByteLength, Is.EqualTo(expected.PayloadByteLength));
            Assert.That(actual.GetPayloadCopy(), Is.EqualTo(expected.GetPayloadCopy()));

            for (var i = 0; i < expected.FieldCount; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]));
                Assert.That(actual.GetFieldPayloadCopy(i), Is.EqualTo(expected.GetFieldPayloadCopy(i)));
            }
        }
    }
}
