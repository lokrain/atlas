// Packages/com.lokrain.atlas/Tests/Runtime/Artifacts/AtlasArtifactBinaryWriterTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts.Tests
//
// Purpose
// - Verify artifact binary stream writing is owned by AtlasArtifactBinaryWriter.
// - Verify the binary writer leaves destination streams open.
// - Verify the old facade delegates to the binary writer without changing bytes.

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
    public sealed class AtlasArtifactBinaryWriterTests
    {
        private static readonly StableDataId FieldId =
            new(0xB100_0000_0000_0001UL, 0xB200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0xB300_0000_0000_0001UL, 0xB400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xB500_0000_0000_0001UL, 0xB600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xB700_0000_0000_0001UL, 0xB800_0000_0000_0001UL, 1);

        [Test]
        public void WriteToStream_ValidArtifact_WritesPreambleAndPayload()
        {
            var artifact = CreateArtifact();

            using var stream = new MemoryStream();

            AtlasArtifactBinaryWriter.WriteToStream(
                artifact,
                stream);

            var bytes = stream.ToArray();

            Assert.That(bytes.Length, Is.GreaterThan(artifact.PayloadByteLength));
            Assert.That(ReadUInt32LittleEndian(bytes, 0), Is.EqualTo(AtlasArtifactBinaryWriter.WriterFormatMarker));
            Assert.That(ReadUInt16LittleEndian(bytes, 4), Is.EqualTo(AtlasArtifactBinaryWriter.WriterSchemaVersion));
            Assert.That(stream.CanWrite, Is.True);
        }

        [Test]
        public void WriteToStream_NullArtifact_ThrowsArgumentNullException()
        {
            using var stream = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() =>
                AtlasArtifactBinaryWriter.WriteToStream(
                    null,
                    stream));
        }

        [Test]
        public void WriteToStream_NullStream_ThrowsArgumentNullException()
        {
            var artifact = CreateArtifact();

            Assert.Throws<ArgumentNullException>(() =>
                AtlasArtifactBinaryWriter.WriteToStream(
                    artifact,
                    null));
        }

        [Test]
        public void WriteToStream_NonWritableStream_ThrowsArgumentException()
        {
            var artifact = CreateArtifact();

            using var stream = new MemoryStream(
                new byte[16],
                writable: false);

            Assert.Throws<ArgumentException>(() =>
                AtlasArtifactBinaryWriter.WriteToStream(
                    artifact,
                    stream));
        }

        [Test]
        public void AtlasArtifactWriterFacade_ProducesSameBytesAsBinaryWriter()
        {
            var artifact = CreateArtifact();

            using var binaryStream = new MemoryStream();
            using var facadeStream = new MemoryStream();

            AtlasArtifactBinaryWriter.WriteToStream(
                artifact,
                binaryStream);

            AtlasArtifactWriter.WriteToStream(
                artifact,
                facadeStream);

            Assert.That(facadeStream.ToArray(), Is.EqualTo(binaryStream.ToArray()));
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
                new FixedString64Bytes("artifact.binary.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("artifact.binary.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("artifact.binary.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("artifact.binary.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("artifact.binary.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("artifact.binary.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("artifact.binary.field")));
        }

        private static uint ReadUInt32LittleEndian(
            byte[] bytes,
            int offset)
        {
            return (uint)bytes[offset] |
                   ((uint)bytes[offset + 1] << 8) |
                   ((uint)bytes[offset + 2] << 16) |
                   ((uint)bytes[offset + 3] << 24);
        }

        private static ushort ReadUInt16LittleEndian(
            byte[] bytes,
            int offset)
        {
            return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
        }
    }
}
