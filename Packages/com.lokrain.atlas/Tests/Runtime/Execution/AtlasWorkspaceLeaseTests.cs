// Packages/com.lokrain.atlas/Tests/Runtime/Execution/AtlasWorkspaceLeaseTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution.Tests
//
// Purpose
// - Verify AtlasWorkspaceLease transfers workspace and dependency ownership safely.
// - Verify lease disposal completes pending work before disposing workspace memory.
// - Verify result disposal after lease transfer does not dispose the transferred workspace.

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
    public sealed class AtlasWorkspaceLeaseTests
    {
        private static readonly StableDataId WritableFieldId =
            new(0xB100_0000_0000_0001UL, 0xB200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId WriteOperationId =
            new(0xB300_0000_0000_0001UL, 0xB400_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xB500_0000_0000_0001UL, 0xB600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0xB700_0000_0000_0001UL, 0xB800_0000_0000_0001UL, 1);

        [Test]
        public void ReleaseWorkspaceLease_TransfersWorkspaceAndPendingDependency()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 101);

            using var lease = result.ReleaseWorkspaceLease();

            Assert.That(result.HasWorkspace, Is.False);
            Assert.That(result.OwnsWorkspace, Is.False);
            Assert.That(result.HasPendingExecutionDependency, Is.False);
            Assert.That(lease.HasWorkspace, Is.True);
            Assert.That(lease.OwnsWorkspace, Is.True);
            Assert.That(lease.HasPendingDependency, Is.True);
        }

        [Test]
        public void LeaseComplete_CompletesPendingDependency()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 103);
            using var lease = result.ReleaseWorkspaceLease();

            lease.Complete();

            Assert.That(marker[0], Is.EqualTo(103));
            Assert.That(lease.HasPendingDependency, Is.False);
            Assert.That(lease.HasWorkspace, Is.True);
        }

        [Test]
        public void LeaseDispose_CompletesDependencyBeforeDisposingWorkspace()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 107);
            var lease = result.ReleaseWorkspaceLease();
            var workspace = lease.GetRequiredWorkspace();

            lease.Dispose();

            Assert.That(marker[0], Is.EqualTo(107));
            Assert.That(workspace.IsDisposed, Is.True);
            Assert.That(lease.IsDisposed, Is.True);
        }

        [Test]
        public void ResultDispose_AfterLeaseTransfer_DoesNotDisposeTransferredWorkspace()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            var result = RunScheduled(marker, 109);
            var lease = result.ReleaseWorkspaceLease();
            var workspace = lease.GetRequiredWorkspace();

            result.Dispose();

            try
            {
                Assert.That(workspace.IsDisposed, Is.False);
                Assert.That(lease.HasWorkspace, Is.True);
                Assert.That(lease.OwnsWorkspace, Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        [Test]
        public void CompleteAndReleaseWorkspaceOwnership_CompletesBeforeReturningWorkspace()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            using var result = RunScheduled(marker, 113);
            using var lease = result.ReleaseWorkspaceLease();

            var workspace = lease.CompleteAndReleaseWorkspaceOwnership();

            try
            {
                Assert.That(marker[0], Is.EqualTo(113));
                Assert.That(workspace.IsDisposed, Is.False);
                Assert.That(lease.HasWorkspace, Is.False);
                Assert.That(lease.OwnsWorkspace, Is.False);
                Assert.That(lease.HasPendingDependency, Is.False);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        [Test]
        public void ReleaseWorkspaceLease_AfterResultDisposed_ThrowsObjectDisposedException()
        {
            using var marker = new NativeArray<int>(1, Allocator.TempJob);
            var result = RunScheduled(marker, 127);

            result.Dispose();

            Assert.Throws<System.ObjectDisposedException>(() =>
                result.ReleaseWorkspaceLease());
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
                new FixedString64Bytes("workspace.lease.write"),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Create(
                    WritableFieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    new FixedString64Bytes("workspace.lease.field")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("workspace.lease.stage"),
                operation);

            return AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("workspace.lease.pipeline"),
                stage);
        }

        private static AtlasContractTable CreateContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workspace.lease.contracts"),
                AtlasContractFactory.Create<int>(
                    WritableFieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("workspace.lease.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("workspace.lease.field")));
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
                DebugName = new FixedString64Bytes("workspace.lease.marker.executor");
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
