// Packages/com.lokrain.atlas/Tests/Runtime/Artifacts/AtlasArtifactBinaryReaderTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts.Tests
//
// Purpose
// - Verify artifact binary stream reading is owned by AtlasArtifactBinaryReader.
// - Verify writer output round-trips through the reader without losing metadata or payload bytes.
// - Verify corrupt or truncated streams are rejected before partial artifacts escape.

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
    public sealed class AtlasArtifactBinaryReaderTests
    {
        private static readonly StableDataId FieldId =
            new(0xD100_0000_0000_0001UL, 0xD200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0xD300_0000_0000_0001UL, 0xD400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xD500_0000_0000_0001UL, 0xD600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xD700_0000_0000_0001UL, 0xD800_0000_0000_0001UL, 1);

        [Test]
        public void ReadFromStream_WriterOutput_RoundTripsArtifact()
        {
            var expected = CreateArtifact();
            var bytes = WriteArtifact(expected);

            using var stream = new MemoryStream(bytes);

            var actual = AtlasArtifactBinaryReader.ReadFromStream(stream);

            AssertArtifactEqual(actual, expected);
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void AtlasArtifactReaderFacade_ProducesSameArtifactAsBinaryReader()
        {
            var expected = CreateArtifact();
            var bytes = WriteArtifact(expected);

            using var binaryStream = new MemoryStream(bytes);
            using var facadeStream = new MemoryStream(bytes);

            var binaryArtifact = AtlasArtifactBinaryReader.ReadFromStream(binaryStream);
            var facadeArtifact = AtlasArtifactReader.ReadFromStream(facadeStream);

            AssertArtifactEqual(facadeArtifact, binaryArtifact);
        }

        [Test]
        public void ReadFromStream_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AtlasArtifactBinaryReader.ReadFromStream(null));
        }

        [Test]
        public void ReadFromStream_BadWriterMarker_ThrowsInvalidDataException()
        {
            var bytes = WriteArtifact(CreateArtifact());
            bytes[0] ^= 0xFF;

            using var stream = new MemoryStream(bytes);

            Assert.Throws<InvalidDataException>(() =>
                AtlasArtifactBinaryReader.ReadFromStream(stream));
        }

        [Test]
        public void ReadFromStream_UnsupportedWriterSchema_ThrowsInvalidDataException()
        {
            var bytes = WriteArtifact(CreateArtifact());
            bytes[4] = 0xFF;
            bytes[5] = 0x7F;

            using var stream = new MemoryStream(bytes);

            Assert.Throws<InvalidDataException>(() =>
                AtlasArtifactBinaryReader.ReadFromStream(stream));
        }

        [Test]
        public void ReadFromStream_BadHeaderMagic_ThrowsInvalidDataException()
        {
            var bytes = WriteArtifact(CreateArtifact());
            bytes[8] ^= 0xFF;

            using var stream = new MemoryStream(bytes);

            Assert.Throws<InvalidDataException>(() =>
                AtlasArtifactBinaryReader.ReadFromStream(stream));
        }

        [Test]
        public void ReadFromStream_TruncatedPayload_ThrowsInvalidDataException()
        {
            var bytes = WriteArtifact(CreateArtifact());
            var truncated = new byte[bytes.Length - 1];

            Array.Copy(
                bytes,
                truncated,
                truncated.Length);

            using var stream = new MemoryStream(truncated);

            Assert.Throws<InvalidDataException>(() =>
                AtlasArtifactBinaryReader.ReadFromStream(stream));
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
                new FixedString64Bytes("artifact.reader.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.reader.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("artifact.reader.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("artifact.reader.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("artifact.reader.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.reader.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("artifact.reader.field")));
        }

        private static byte[] WriteArtifact(
            AtlasArtifact artifact)
        {
            using var stream = new MemoryStream();

            AtlasArtifactBinaryWriter.WriteToStream(
                artifact,
                stream);

            return stream.ToArray();
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
