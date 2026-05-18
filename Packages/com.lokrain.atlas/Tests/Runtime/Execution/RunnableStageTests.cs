#nullable enable

using System;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Planning;
using NUnit.Framework;


namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class RunnableStageTests
    {
        [Test]
        public void Constructor_WithNullStagePlanNode_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new RunnableStage(
                    new StageIndex(0),
                    stagePlanNode: null!,
                    Array.Empty<FieldIndex>(),
                    Array.Empty<FieldIndex>(),
                    Array.Empty<OperationIndex>()),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stagePlanNode"));
        }

        [Test]
        public void Constructor_WithNullRequiredInputFieldIndices_ThrowsArgumentNullException()
        {
            StagePlanNode stagePlanNode = GetStagePlanNode();

            Assert.That(
                () => new RunnableStage(
                    new StageIndex(0),
                    stagePlanNode,
                    requiredInputFieldIndices: null!,
                    new[] { new FieldIndex(1), new FieldIndex(0) },
                    CreateOperationIndices()),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("requiredInputFieldIndices"));
        }

        [Test]
        public void Constructor_WithWrongProducedOutputFieldIndexCount_ThrowsArgumentException()
        {
            StagePlanNode stagePlanNode = GetStagePlanNode();

            Assert.That(
                () => new RunnableStage(
                    new StageIndex(0),
                    stagePlanNode,
                    Array.Empty<FieldIndex>(),
                    new[] { new FieldIndex(1) },
                    CreateOperationIndices()),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("producedOutputFieldIndices"));
        }

        [Test]
        public void Constructor_WithDuplicateOperationIndices_ThrowsArgumentException()
        {
            StagePlanNode stagePlanNode = GetStagePlanNode();

            Assert.That(
                () => new RunnableStage(
                    new StageIndex(0),
                    stagePlanNode,
                    Array.Empty<FieldIndex>(),
                    new[] { new FieldIndex(1), new FieldIndex(0) },
                    new[]
                    {
                        new OperationIndex(0),
                        new OperationIndex(1),
                        new OperationIndex(2),
                        new OperationIndex(2),
                        new OperationIndex(4)
                    }),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("operationIndices"));
        }

        [Test]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            StagePlanNode stagePlanNode = GetStagePlanNode();
            FieldIndex[] requiredInputs = Array.Empty<FieldIndex>();
            FieldIndex[] producedOutputs = { new(1), new(0) };
            OperationIndex[] operationIndices = CreateOperationIndices();

            var stage = new RunnableStage(
                new StageIndex(0),
                stagePlanNode,
                requiredInputs,
                producedOutputs,
                operationIndices);

            Assert.That(stage.StageIndex, Is.EqualTo(new StageIndex(0)));
            Assert.That(stage.StagePlanNode, Is.SameAs(stagePlanNode));
            Assert.That(stage.RequiredInputFieldIndices, Is.EqualTo(requiredInputs));
            Assert.That(stage.ProducedOutputFieldIndices, Is.EqualTo(producedOutputs));
            Assert.That(stage.OperationIndices, Is.EqualTo(operationIndices));
        }

        [Test]
        public void Equals_WithSameValuesAndReferences_ReturnsTrue()
        {
            StagePlanNode stagePlanNode = GetStagePlanNode();

            var left = CreateStage(stagePlanNode);
            var right = CreateStage(stagePlanNode);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentStagePlanNodeReference_ReturnsFalse()
        {
            StagePlanNode leftNode = CompilePrimaryContinentalLandmassPlan().StagePlanNodes[0];
            StagePlanNode rightNode = CompilePrimaryContinentalLandmassPlan().StagePlanNodes[0];

            Assert.That(leftNode == rightNode, Is.True);
            Assert.That(ReferenceEquals(leftNode, rightNode), Is.False);

            Assert.That(CreateStage(leftNode).Equals(CreateStage(rightNode)), Is.False);
        }

        [Test]
        public void ToString_ContainsHighSignalIdentityData()
        {
            StagePlanNode stagePlanNode = GetStagePlanNode();
            RunnableStage stage = CreateStage(stagePlanNode);

            string text = stage.ToString();

            Assert.That(text, Does.Contain(nameof(RunnableStage)));
            Assert.That(text, Does.Contain("StageIndex(0)"));
            Assert.That(text, Does.Contain(stagePlanNode.StageDefinition.Symbol.ToString()));
        }

        private static RunnableStage CreateStage(StagePlanNode stagePlanNode)
        {
            return new RunnableStage(
                new StageIndex(0),
                stagePlanNode,
                Array.Empty<FieldIndex>(),
                new[] { new FieldIndex(1), new FieldIndex(0) },
                CreateOperationIndices());
        }

        private static OperationIndex[] CreateOperationIndices()
        {
            return new[]
            {
                new OperationIndex(0),
                new OperationIndex(1),
                new OperationIndex(2),
                new OperationIndex(3),
                new OperationIndex(4)
            };
        }

        private static StagePlanNode GetStagePlanNode()
        {
            return CompilePrimaryContinentalLandmassPlan().StagePlanNodes[0];
        }

        private static GenerationPlan CompilePrimaryContinentalLandmassPlan()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    new Grid(256, 256),
                    new Seed(123UL));

            GenerationRequestResolutionResult resolutionResult =
                new GenerationRequestResolver().Resolve(catalog, descriptor);

            Assert.That(resolutionResult.Succeeded, Is.True);
            Assert.That(resolutionResult.GenerationRequest, Is.Not.Null);

            return new GenerationPlanCompiler().Compile(resolutionResult.GenerationRequest!);
        }
    }
}
