// Packages/com.lokrain.atlas/Tests/Runtime/Execution/AtlasRunWorkflowTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution.Tests
//
// Purpose
// - Verify AtlasRunWorkflow wires compilation, shape resolution, workspace allocation, execution,
//   artifact capture, and result ownership into one production orchestration boundary.
// - Verify phase-specific failures do not masquerade as successful runs.
// - Verify completed and scheduled execution requests produce honest execution results.

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
    public sealed class AtlasRunWorkflowTests
    {
        private static readonly StableDataId WritableFieldId =
            new(0x9100_0000_0000_0001UL, 0x9200_0000_0000_0001UL, 1);

        private static readonly StableDataId ReadOnlyFieldId =
            new(0x9100_0000_0000_0002UL, 0x9200_0000_0000_0002UL, 1);

        private static readonly AtlasOperationId WriteOperationId =
            new(0x9300_0000_0000_0001UL, 0x9400_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId ReadOperationId =
            new(0x9300_0000_0000_0002UL, 0x9400_0000_0000_0002UL, 1);

        private static readonly AtlasStageId StageId =
            new(0x9500_0000_0000_0001UL, 0x9600_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0x9700_0000_0000_0001UL, 0x9800_0000_0000_0001UL, 1);

        [Test]
        public void Run_NullRequest_ReturnsRequestValidationFailure()
        {
            using var result = AtlasRunWorkflow.Run(request: null);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.Status, Is.EqualTo(AtlasRunWorkflowResultStatus.Failed));
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.RequestValidation));
            Assert.That(result.HasException, Is.True);
            Assert.That(result.HasWorkspace, Is.False);
            Assert.That(result.OwnsWorkspace, Is.False);
        }

        [Test]
        public void Run_ValidCompletedRequest_ReturnsCompletedResultWithArtifact()
        {
            var request = CreateWriteRequest(
                AtlasOperationExecutorRegistry.Create(new NoOpExecutor(WriteOperationId)));

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Status, Is.EqualTo(AtlasRunWorkflowResultStatus.Succeeded));
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.Completed));
            Assert.That(result.HasCompilation, Is.True);
            Assert.That(result.Compilation.Succeeded, Is.True);
            Assert.That(result.HasPlan, Is.True);
            Assert.That(result.HasShapes, Is.True);
            Assert.That(result.HasWorkspace, Is.True);
            Assert.That(result.OwnsWorkspace, Is.True);
            Assert.That(result.HasExecutionContext, Is.True);
            Assert.That(result.HasExecution, Is.True);
            Assert.That(result.Execution.IsCompleted, Is.True);
            Assert.That(result.Execution.ScheduledOperationCount, Is.EqualTo(1));
            Assert.That(result.HasArtifact, Is.True);
            Assert.That(result.Artifact.FieldCount, Is.EqualTo(1));
            Assert.That(result.WroteArtifactFile, Is.False);
            Assert.That(result.HasDebugMapImage, Is.False);
        }


        [Test]
        public void Run_ValidCompletedRequestWithDefaultExecutors_ReturnsCompletedResultWithArtifact()
        {
            var pipeline = CreateWritePipeline();
            var registry = AtlasDefaultOperationExecutorRegistry.Create(pipeline);

            var request = AtlasRunRequest.Create(
                pipeline,
                CreateWritableContractTable(),
                registry,
                Allocator.TempJob);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.Completed));
            Assert.That(result.HasExecution, Is.True);
            Assert.That(result.Execution.IsCompleted, Is.True);
            Assert.That(result.Execution.ScheduledOperationCount, Is.EqualTo(1));
            Assert.That(result.HasArtifact, Is.True);
        }

        [Test]
        public void Run_WithoutArtifactCapture_ReturnsCompletedResultWithoutArtifact()
        {
            var request = AtlasRunRequest.Create(
                CreateWritePipeline(),
                CreateWritableContractTable(),
                AtlasOperationExecutorRegistry.Create(new NoOpExecutor(WriteOperationId)),
                Allocator.TempJob,
                completeExecution: true,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.Completed));
            Assert.That(result.Execution.IsCompleted, Is.True);
            Assert.That(result.HasArtifact, Is.False);
            Assert.That(result.HasDebugMapImage, Is.False);
        }

        [Test]
        public void Run_WithoutCompletionAndWithoutArtifactCapture_ReturnsScheduledResult()
        {
            var request = AtlasRunRequest.Create(
                CreateWritePipeline(),
                CreateWritableContractTable(),
                AtlasOperationExecutorRegistry.Create(new NoOpExecutor(WriteOperationId)),
                Allocator.TempJob,
                completeExecution: false,
                captureArtifact: false,
                computeArtifactContentHashes: false);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.Completed));
            Assert.That(result.Execution.IsScheduled, Is.True);
            Assert.That(result.HasArtifact, Is.False);
        }

        [Test]
        public void Run_CompilationFailure_ReturnsCompilationPhaseWithoutWorkspace()
        {
            var request = AtlasRunRequest.Create(
                CreateReadBeforeWritePipeline(),
                CreateReadOnlyContractTable(),
                AtlasOperationExecutorRegistry.Create(new NoOpExecutor(ReadOperationId)),
                Allocator.TempJob);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.Compilation));
            Assert.That(result.HasCompilation, Is.True);
            Assert.That(result.Compilation.Failed, Is.True);
            Assert.That(result.HasShapes, Is.False);
            Assert.That(result.HasWorkspace, Is.False);
            Assert.That(result.HasExecutionContext, Is.False);
            Assert.That(result.HasExecution, Is.False);
        }

        [Test]
        public void Run_MissingExecutor_ReturnsExecutionPhaseWithOwnedWorkspace()
        {
            var request = CreateWriteRequest(AtlasOperationExecutorRegistry.Empty);

            using var result = AtlasRunWorkflow.Run(request);

            Assert.That(result.Failed, Is.True);
            Assert.That(result.Phase, Is.EqualTo(AtlasRunWorkflowPhase.Execution));
            Assert.That(result.HasCompilation, Is.True);
            Assert.That(result.Compilation.Succeeded, Is.True);
            Assert.That(result.HasShapes, Is.True);
            Assert.That(result.HasWorkspace, Is.True);
            Assert.That(result.OwnsWorkspace, Is.True);
            Assert.That(result.HasExecutionContext, Is.True);
            Assert.That(result.HasExecution, Is.False);
            Assert.That(result.HasArtifact, Is.False);
        }

        private static AtlasRunRequest CreateWriteRequest(
            AtlasOperationExecutorRegistry registry)
        {
            return AtlasRunRequest.Create(
                CreateWritePipeline(),
                CreateWritableContractTable(),
                registry,
                Allocator.TempJob);
        }

        private static AtlasPipelineDefinition CreateWritePipeline()
        {
            return CreatePipeline(CreateWriteOperation());
        }

        private static AtlasPipelineDefinition CreateReadBeforeWritePipeline()
        {
            return CreatePipeline(CreateReadOperation());
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
                    AtlasWriteCoverage.FullCapacity,
                    new FixedString64Bytes("writable.field")));
        }

        private static AtlasOperationDefinition CreateReadOperation()
        {
            return AtlasOperationDefinition.Create(
                ReadOperationId,
                new FixedString64Bytes("workflow.read"),
                AtlasOperationRole.Validation,
                AtlasOperationAccess.Create(
                    ReadOnlyFieldId,
                    AtlasOperationAccessMode.Read,
                    AtlasOperationAccessFlags.None,
                    new FixedString64Bytes("read.only.field")));
        }

        private static AtlasContractTable CreateWritableContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workflow.contracts"),
                CreateNativeArrayContract(
                    WritableFieldId,
                    "workflow.writable.field"));
        }

        private static AtlasContractTable CreateReadOnlyContractTable()
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("workflow.contracts"),
                CreateNativeArrayContract(
                    ReadOnlyFieldId,
                    "workflow.read.only.field"));
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

        private sealed class NoOpExecutor : IAtlasOperationExecutor
        {
            public NoOpExecutor(
                AtlasOperationId operationId)
            {
                OperationId = operationId;
                DebugName = new FixedString64Bytes("workflow.noop.executor");
            }

            public AtlasOperationId OperationId { get; }

            public FixedString64Bytes DebugName { get; }

            public JobHandle Execute(
                AtlasExecutionContext context,
                AtlasCompiledOperation operation,
                JobHandle inputDeps)
            {
                Assert.That(operation.OperationId, Is.EqualTo(OperationId));
                return inputDeps;
            }
        }
    }
}
