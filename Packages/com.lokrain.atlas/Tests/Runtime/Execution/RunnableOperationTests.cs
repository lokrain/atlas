#nullable enable

using System;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Planning;
using NUnit.Framework;


namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class RunnableOperationTests
    {
        [Test]
        public void Constructor_WithNullOperationPlanNode_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new RunnableOperation(
                    new OperationIndex(0),
                    new StageIndex(0),
                    operationPlanNode: null!,
                    Array.Empty<FieldIndex>(),
                    Array.Empty<FieldIndex>()),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("operationPlanNode"));
        }

        [Test]
        public void Constructor_WithNullRequiredInputFieldIndices_ThrowsArgumentNullException()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);

            Assert.That(
                () => new RunnableOperation(
                    new OperationIndex(1),
                    new StageIndex(0),
                    operationPlanNode,
                    requiredInputFieldIndices: null!,
                    new[] { new FieldIndex(1) }),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("requiredInputFieldIndices"));
        }

        [Test]
        public void Constructor_WithNullProducedOutputFieldIndices_ThrowsArgumentNullException()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);

            Assert.That(
                () => new RunnableOperation(
                    new OperationIndex(1),
                    new StageIndex(0),
                    operationPlanNode,
                    new[] { new FieldIndex(0) },
                    producedOutputFieldIndices: null!),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("producedOutputFieldIndices"));
        }

        [Test]
        public void Constructor_WithWrongRequiredInputFieldIndexCount_ThrowsArgumentException()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);

            Assert.That(
                () => new RunnableOperation(
                    new OperationIndex(1),
                    new StageIndex(0),
                    operationPlanNode,
                    Array.Empty<FieldIndex>(),
                    new[] { new FieldIndex(1) }),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("requiredInputFieldIndices"));
        }

        [Test]
        public void Constructor_WithDuplicateRequiredInputFieldIndices_ThrowsArgumentException()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);

            Assert.That(
                () => new RunnableOperation(
                    new OperationIndex(1),
                    new StageIndex(0),
                    operationPlanNode,
                    new[] { new FieldIndex(0), new FieldIndex(0) },
                    new[] { new FieldIndex(1) }),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("requiredInputFieldIndices"));
        }

        [Test]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);
            FieldIndex[] requiredInputs = { new(0) };
            FieldIndex[] producedOutputs = { new(1) };

            var operation = new RunnableOperation(
                new OperationIndex(1),
                new StageIndex(0),
                operationPlanNode,
                requiredInputs,
                producedOutputs);

            Assert.That(operation.OperationIndex, Is.EqualTo(new OperationIndex(1)));
            Assert.That(operation.StageIndex, Is.EqualTo(new StageIndex(0)));
            Assert.That(operation.OperationPlanNode, Is.SameAs(operationPlanNode));
            Assert.That(operation.RequiredInputFieldIndices, Is.EqualTo(requiredInputs));
            Assert.That(operation.ProducedOutputFieldIndices, Is.EqualTo(producedOutputs));
        }

        [Test]
        public void Equals_WithSameValuesAndReferences_ReturnsTrue()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);

            var left = new RunnableOperation(
                new OperationIndex(1),
                new StageIndex(0),
                operationPlanNode,
                new[] { new FieldIndex(0) },
                new[] { new FieldIndex(1) });

            var right = new RunnableOperation(
                new OperationIndex(1),
                new StageIndex(0),
                operationPlanNode,
                new[] { new FieldIndex(0) },
                new[] { new FieldIndex(1) });

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentOperationPlanNodeReference_ReturnsFalse()
        {
            OperationPlanNode leftNode = CompilePrimaryContinentalLandmassPlan().StagePlanNodes[0].OperationPlanNodes[1];
            OperationPlanNode rightNode = CompilePrimaryContinentalLandmassPlan().StagePlanNodes[0].OperationPlanNodes[1];

            Assert.That(leftNode == rightNode, Is.True);
            Assert.That(ReferenceEquals(leftNode, rightNode), Is.False);

            var left = new RunnableOperation(
                new OperationIndex(1),
                new StageIndex(0),
                leftNode,
                new[] { new FieldIndex(0) },
                new[] { new FieldIndex(1) });

            var right = new RunnableOperation(
                new OperationIndex(1),
                new StageIndex(0),
                rightNode,
                new[] { new FieldIndex(0) },
                new[] { new FieldIndex(1) });

            Assert.That(left.Equals(right), Is.False);
        }

        [Test]
        public void ToString_ContainsHighSignalIdentityData()
        {
            OperationPlanNode operationPlanNode = GetOperationPlanNode(1);

            var operation = new RunnableOperation(
                new OperationIndex(1),
                new StageIndex(0),
                operationPlanNode,
                new[] { new FieldIndex(0) },
                new[] { new FieldIndex(1) });

            string text = operation.ToString();

            Assert.That(text, Does.Contain(nameof(RunnableOperation)));
            Assert.That(text, Does.Contain("OperationIndex(1)"));
            Assert.That(text, Does.Contain("StageIndex(0)"));
            Assert.That(text, Does.Contain(operationPlanNode.OperationDefinition.Symbol.ToString()));
        }

        private static OperationPlanNode GetOperationPlanNode(int operationIndex)
        {
            return CompilePrimaryContinentalLandmassPlan().StagePlanNodes[0].OperationPlanNodes[operationIndex];
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
