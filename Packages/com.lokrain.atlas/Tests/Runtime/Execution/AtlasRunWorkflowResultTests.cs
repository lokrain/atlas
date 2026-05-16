// Packages/com.lokrain.atlas/Tests/Runtime/Execution/AtlasRunWorkflowResultTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution.Tests
//
// Purpose
// - Verify AtlasRunWorkflowResult owns scheduled execution dependencies explicitly.
// - Verify completion and workspace ownership transfer preserve native-memory safety.
// - Verify failed workflow results do not allow successful ownership operations.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Executors;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class AtlasRunWorkflowResultTests
    {
        private static readonly StableDataId WritableFieldId =
            new(0xA100_0000_0000_0001UL, 0xA200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId WriteOperationId =
            new(0xA300_0000_0000_0001UL, 0xA400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xA500_0000_0000_0001UL, 0xA600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xA700_0000_0000_0001UL, 0xA800_0000_0000_0001UL, 1);

        [Test]
        public void ScheduledResult_ExposesOwnedPendingDependency()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 11);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.HasExecution, Is.True);
            Assert.That(result.Execution.IsScheduled, Is.True);
            Assert.That(result.HasPendingExecutionDependency, Is.True);
            Assert.That(result.HasCompletedExecution, Is.False);
            Assert.That(result.OwnsWorkspace, Is.True);
        }

        [Test]
        public void Complete_ScheduledResult_CompletesDependencyAndUpdatesExecutionState()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 23);

            result.Complete();

            Assert.That(marker[0], Is.EqualTo(23));
            Assert.That(result.HasPendingExecutionDependency, Is.False);
            Assert.That(result.HasCompletedExecution, Is.True);
            Assert.That(result.Execution.IsCompleted, Is.True);
        }

        [Test]
        public void Complete_IsIdempotent()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 37);

            result.Complete();
            result.Complete();

            Assert.That(marker[0], Is.EqualTo(37));
            Assert.That(result.HasPendingExecutionDependency, Is.False);
            Assert.That(result.Execution.IsCompleted, Is.True);
        }

        [Test]
        public void Dispose_ScheduledResult_CompletesDependencyBeforeWorkspaceDisposal()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            var result = RunScheduled(marker, 41);
            var workspace = result.GetRequiredWorkspace();

            result.Dispose();

            Assert.That(marker[0], Is.EqualTo(41));
            Assert.That(workspace.IsDisposed, Is.True);
            Assert.That(result.IsDisposed, Is.True);
        }

        [Test]
        public void CompleteAndReleaseWorkspaceOwnership_ScheduledResult_CompletesBeforeReturningWorkspace()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 53);

            var workspace = result.CompleteAndReleaseWorkspaceOwnership();

            try
            {
                Assert.That(marker[0], Is.EqualTo(53));
                Assert.That(workspace.IsDisposed, Is.False);
                Assert.That(result.HasWorkspace, Is.False);
                Assert.That(result.OwnsWorkspace, Is.False);
                Assert.That(result.HasPendingExecutionDependency, Is.False);
                Assert.That(result.Execution.IsCompleted, Is.True);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        [Test]
        public void Complete_FailedResult_ThrowsInvalidOperationException()
        {
            using var result = AtlasRunWorkflowResult.RequestValidationFailure(
                request: null,
                message: "failed");

            Assert.Throws<InvalidOperationException>(() => result.Complete());
        }

        [Test]
        public void CompleteAndReleaseWorkspaceOwnership_FailedResult_ThrowsInvalidOperationException()
        {
            using var result = AtlasRunWorkflowResult.RequestValidationFailure(
                request: null,
                message: "failed");

            Assert.Throws<InvalidOperationException>(() =>
                result.CompleteAndReleaseWorkspaceOwnership());
        }

        private static AtlasRunWorkflowResult RunScheduled(
            NativeArray<int> marker,
            int value)
        {
            var request = AtlasRunRequest.Create(
                CreatePipeline(),
                CreateContractTable(),
                AtlasOperationExecutorRegistry.Create(
                    new MarkerWriteExecutor(WriteOperationId, marker, value)),
                Allocator.TempJob,
                completeExecution: false,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            return AtlasRunWorkflow.Run(request);
        }

        private static AtlasPipelineDefinition CreatePipeline()
        {
            var operation = AtlasOperationDefinition.Create(
                WriteOperationId,
                new FixedString64Bytes("workflow.result.write"),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Create(
                    WritableFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    new FixedString64Bytes("workflow.result.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("workflow.result.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("workflow.result.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workflow.result.contracts"),
                AtlasContractFactory.Create<int>(
                    WritableFieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("workflow.result.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("workflow.result.field")));
        }

        private sealed class MarkerWriteExecutor : IAtlasOperationExecutor
        {
            private readonly NativeArray<int> _marker;
            private readonly int _value;

            public MarkerWriteExecutor(
                AtlasOperationId operationId,
                NativeArray<int> marker,
                int value)
            {
                OperationId = operationId;
                DebugName = new FixedString64Bytes("workflow.result.marker.executor");
                _marker = marker;
                _value = value;
            }

            public AtlasOperationId OperationId { get; }

            public FixedString64Bytes DebugName { get; }

            public JobHandle Execute(
                AtlasExecutionContext context,
                AtlasCompiledOperation operation,
                JobHandle inputDeps)
            {
                Assert.That(operation.OperationId, Is.EqualTo(OperationId));

                return new MarkerWriteJob
                {
                    Marker = _marker,
                    Value = _value
                }.Schedule(inputDeps);
            }
        }

        private struct MarkerWriteJob : IJob
        {
            public NativeArray<int> Marker;
            public int Value;

            public void Execute()
            {
                Marker[0] = Value;
            }
        }
    }
}
