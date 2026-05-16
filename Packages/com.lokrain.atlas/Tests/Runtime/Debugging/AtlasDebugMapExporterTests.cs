// Packages/com.lokrain.atlas/Tests/Runtime/Debugging/AtlasDebugMapExporterTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging.Tests
//
// Purpose
// - Verify debug-map export orchestration is owned by AtlasDebugMapExporter.
// - Verify AtlasDebugMapRequest remains request data and does not execute artifact export.
// - Verify AtlasRunWorkflow uses the exporter boundary for debug-map output.

using System;
using System.IO;
using Lokrain.Atlas.Artifacts;
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

namespace Lokrain.Atlas.Debugging.Tests
{
    public sealed class AtlasDebugMapExporterTests
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
        public void RequestType_DoesNotExposeArtifactExportExecution()
        {
            var method = typeof(AtlasDebugMapRequest).GetMethod("ExportFromArtifact");

            Assert.That(method, Is.Null);
        }

        [Test]
        public void ExportFromArtifact_ByteMaskByStableId_WritesTgaAndReturnsImage()
        {
            var artifact = CreateArtifact();
            var filePath = CreateTempTgaPath();

            try
            {
                var request = AtlasDebugMapRequest.ByteMask(
                    FieldId,
                    width: 2,
                    height: 2,
                    filePath,
                    AtlasDebugMapPalette.ProductionDefault);

                var image = AtlasDebugMapExporter.ExportFromArtifact(
                    artifact,
                    request);

                Assert.That(image, Is.Not.Null);
                Assert.That(image.Width, Is.EqualTo(2));
                Assert.That(image.Height, Is.EqualTo(2));
                Assert.That(File.Exists(filePath), Is.True);
                Assert.That(new FileInfo(filePath).Length, Is.GreaterThan(0));
            }
            finally
            {
                DeleteFileIfExists(filePath);
            }
        }

        [Test]
        public void Run_WithDebugMap_ExportsThroughWorkflowBoundary()
        {
            var pipeline = CreatePipeline();
            var filePath = CreateTempTgaPath();

            try
            {
                var request = AtlasRunRequest.Create(
                    pipeline,
                    CreateContractTable(),
                    AtlasDefaultOperationExecutorRegistry.Create(pipeline),
                    Allocator.TempJob,
                    debugMap: AtlasDebugMapRequest.ByteMask(
                        FieldId,
                        width: 2,
                        height: 2,
                        filePath,
                        AtlasDebugMapPalette.ProductionDefault));

                using var result = AtlasRunWorkflow.Run(request);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.HasArtifact, Is.True);
                Assert.That(result.HasDebugMapImage, Is.True);
                Assert.That(result.DebugMapImage.Width, Is.EqualTo(2));
                Assert.That(result.DebugMapImage.Height, Is.EqualTo(2));
                Assert.That(result.DebugMapFilePath, Is.EqualTo(filePath));
                Assert.That(File.Exists(filePath), Is.True);
            }
            finally
            {
                DeleteFileIfExists(filePath);
            }
        }

        [Test]
        public void ExportFromArtifact_NullArtifact_ThrowsArgumentNullException()
        {
            var request = AtlasDebugMapRequest.ByteMask(
                FieldId,
                width: 2,
                height: 2,
                CreateTempTgaPath(),
                AtlasDebugMapPalette.ProductionDefault);

            Assert.Throws<ArgumentNullException>(() =>
                AtlasDebugMapExporter.ExportFromArtifact(
                    artifact: null,
                    request));
        }

        [Test]
        public void ExportFromArtifact_NullRequest_ThrowsArgumentNullException()
        {
            var artifact = CreateArtifact();

            Assert.Throws<ArgumentNullException>(() =>
                AtlasDebugMapExporter.ExportFromArtifact(
                    artifact,
                    request: null));
        }

        private static AtlasArtifact CreateArtifact()
        {
            var pipeline = CreatePipeline();
            var request = AtlasRunRequest.Create(
                pipeline,
                CreateContractTable(),
                AtlasDefaultOperationExecutorRegistry.Create(pipeline),
                Allocator.TempJob,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Succeeded, Is.True);

            return AtlasArtifactCapture.Capture(
                result.GetRequiredExecutionContext(),
                computeContentHashes: false);
        }

        private static AtlasPipelineDefinition CreatePipeline()
        {
            var operation = AtlasOperationDefinition.Create(
                OperationId,
                new FixedString64Bytes("debug.map.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("debug.map.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("debug.map.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("debug.map.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("debug.map.contracts"),
                AtlasContractFactory.Create<byte>(
                    FieldId,
                    AtlasFieldRole.Diagnostic,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("debug.map.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("debug.map.field")));
        }

        private static string CreateTempTgaPath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "lokrain-atlas-debug-map-" + Guid.NewGuid().ToString("N") + ".tga");
        }

        private static void DeleteFileIfExists(
            string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
