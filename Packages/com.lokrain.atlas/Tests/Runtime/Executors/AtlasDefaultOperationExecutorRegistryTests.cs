// Packages/com.lokrain.atlas/Tests/Runtime/Executors/AtlasDefaultOperationExecutorRegistryTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Executors.Tests
//
// Purpose
// - Verify default executor registry creation from authored operation metadata.
// - Verify supported clear-field operation shapes register AtlasClearFieldsOperationExecutor instances.
// - Verify unsupported operation shapes remain intentionally unregistered.
// - Verify repeated operation occurrences register only one executor per durable operation id.

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Executors.Tests
{
    public sealed class AtlasDefaultOperationExecutorRegistryTests
    {
        private static readonly StableDataId FirstFieldId =
            new StableDataId(0x1100_0000_0000_0001UL, 0x1200_0000_0000_0001UL, 1);

        private static readonly StableDataId SecondFieldId =
            new StableDataId(0x1100_0000_0000_0002UL, 0x1200_0000_0000_0002UL, 1);

        private static readonly AtlasOperationId ClearOperationId =
            new AtlasOperationId(0x1300_0000_0000_0001UL, 0x1400_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId ReadOperationId =
            new AtlasOperationId(0x1300_0000_0000_0002UL, 0x1400_0000_0000_0002UL, 1);

        private static readonly AtlasOperationId PartialWriteOperationId =
            new AtlasOperationId(0x1300_0000_0000_0003UL, 0x1400_0000_0000_0003UL, 1);

        private static readonly AtlasOperationId FullCanonicalOperationId =
            new AtlasOperationId(0x1300_0000_0000_0004UL, 0x1400_0000_0000_0004UL, 1);

        private static readonly AtlasStageId StageId =
            new AtlasStageId(0x1500_0000_0000_0001UL, 0x1600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new AtlasPipelineId(0x1700_0000_0000_0001UL, 0x1800_0000_0000_0001UL, 1);

        [Test]
        public void Create_NullPipeline_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AtlasDefaultOperationExecutorRegistry.Create((AtlasPipelineDefinition)null));
        }

        [Test]
        public void Create_InvalidBatchCount_ThrowsArgumentOutOfRangeException()
        {
            var pipeline = CreatePipeline(CreateClearOperation());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AtlasDefaultOperationExecutorRegistry.Create(
                    pipeline,
                    clearFieldsInnerloopBatchCount: 0));
        }

        [Test]
        public void Create_PipelineWithClearOperation_RegistersClearExecutorByOperationId()
        {
            var operation = CreateClearOperation();
            var pipeline = CreatePipeline(operation);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Contains(operation.OperationId), Is.True);
            Assert.That(registry[operation.OperationId], Is.TypeOf<AtlasClearFieldsOperationExecutor>());
            Assert.That(registry[operation.OperationId].OperationId, Is.EqualTo(operation.OperationId));
        }

        [Test]
        public void Create_PipelineWithRepeatedClearOperation_RegistersOneExecutorPerOperationId()
        {
            var operation = CreateClearOperation();
            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("default.executors.stage"),
                operation,
                operation);

            var pipeline = AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("default.executors.pipeline"),
                stage);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Contains(operation.OperationId), Is.True);
        }

        [Test]
        public void Create_PipelineWithReadOnlyOperation_DoesNotRegisterExecutor()
        {
            var operation = CreateReadOperation();
            var pipeline = CreatePipeline(operation);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);

            Assert.That(registry.IsEmpty, Is.True);
            Assert.That(registry.Contains(operation.OperationId), Is.False);
        }

        [Test]
        public void Create_PipelineWithPartialWriteOperation_DoesNotRegisterExecutor()
        {
            var operation = CreatePartialWriteOperation();
            var pipeline = CreatePipeline(operation);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);

            Assert.That(registry.IsEmpty, Is.True);
            Assert.That(registry.Contains(operation.OperationId), Is.False);
        }

        [Test]
        public void Create_PipelineWithFullWriteNonWorkspacePreparationOperation_DoesNotRegisterExecutor()
        {
            var operation = CreateFullWriteCanonicalOperation();
            var pipeline = CreatePipeline(operation);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);

            Assert.That(registry.IsEmpty, Is.True);
            Assert.That(registry.Contains(operation.OperationId), Is.False);
        }

        [Test]
        public void Create_NullOperationCatalog_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AtlasDefaultOperationExecutorRegistry.Create((AtlasOperationCatalog)null));
        }

        [Test]
        public void Create_OperationCatalogWithClearOperation_RegistersClearExecutor()
        {
            var operation = CreateClearOperation();
            var catalog = AtlasOperationCatalog.Create(
                new FixedString64Bytes("default.executors.catalog"),
                operation);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(catalog);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Contains(operation.OperationId), Is.True);
            Assert.That(registry[operation.OperationId], Is.TypeOf<AtlasClearFieldsOperationExecutor>());
        }

        [Test]
        public void Create_OperationSetWithClearOperation_RegistersClearExecutor()
        {
            var operation = CreateClearOperation();
            var operations = AtlasOperationSet.Create(
                new FixedString64Bytes("default.executors.operations"),
                operation);

            var registry = AtlasDefaultOperationExecutorRegistry.Create(operations);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Contains(operation.OperationId), Is.True);
        }

        [Test]
        public void Create_EnumerableWithNullOperation_ThrowsArgumentException()
        {
            var operations = new AtlasOperationDefinition[]
            {
                CreateClearOperation(),
                null
            };

            Assert.Throws<ArgumentException>(() =>
                AtlasDefaultOperationExecutorRegistry.Create(
                    (IEnumerable<AtlasOperationDefinition>)operations));
        }

        [Test]
        public void CanCreateExecutor_ClearOperation_ReturnsTrue()
        {
            Assert.That(
                AtlasDefaultOperationExecutorRegistry.CanCreateExecutor(CreateClearOperation()),
                Is.True);
        }

        [Test]
        public void CanCreateExecutor_NonClearOperation_ReturnsFalse()
        {
            Assert.That(
                AtlasDefaultOperationExecutorRegistry.CanCreateExecutor(CreateReadOperation()),
                Is.False);
        }

        [Test]
        public void IsClearFieldsOperation_NullOperation_ReturnsFalse()
        {
            Assert.That(
                AtlasDefaultOperationExecutorRegistry.IsClearFieldsOperation(null),
                Is.False);
        }

        private static AtlasPipelineDefinition CreatePipeline(
            AtlasOperationDefinition operation)
        {
            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("default.executors.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("default.executors.pipeline"),
                stage);
        }

        private static AtlasOperationDefinition CreateClearOperation()
        {
            return AtlasOperationDefinition.Create(
                ClearOperationId,
                new FixedString64Bytes("default.executors.clear"),
                AtlasOperationRole.WorkspacePreparation,
                AtlasOperationAccess.Create(
                    FirstFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("first.field")),
                AtlasOperationAccess.Create(
                    SecondFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullLogicalLength,
                    new FixedString64Bytes("second.field")));
        }

        private static AtlasOperationDefinition CreateReadOperation()
        {
            return AtlasOperationDefinition.Create(
                ReadOperationId,
                new FixedString64Bytes("default.executors.read"),
                AtlasOperationRole.Validation,
                AtlasOperationAccess.Create(
                    FirstFieldId,
                    AtlasOperationAccessMode.Read,
                    AtlasOperationAccessFlags.None,
                    AtlasWriteCoverage.None,
                    new FixedString64Bytes("first.field")));
        }

        private static AtlasOperationDefinition CreatePartialWriteOperation()
        {
            return AtlasOperationDefinition.Create(
                PartialWriteOperationId,
                new FixedString64Bytes("default.executors.partial"),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Create(
                    FirstFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.PartialLogicalLength,
                    new FixedString64Bytes("first.field")));
        }

        private static AtlasOperationDefinition CreateFullWriteCanonicalOperation()
        {
            return AtlasOperationDefinition.Create(
                FullCanonicalOperationId,
                new FixedString64Bytes("default.executors.full.canonical"),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Create(
                    FirstFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullLogicalLength,
                    new FixedString64Bytes("first.field")));
        }
    }
}
