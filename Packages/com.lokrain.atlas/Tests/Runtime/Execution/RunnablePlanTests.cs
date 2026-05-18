#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Planning;
using Lokrain.Atlas.Resources;
using NUnit.Framework;


namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class RunnablePlanTests
    {
        [Test]
        public void Constructor_WithNullGenerationPlan_ThrowsArgumentNullException()
        {
            RunnablePlanParts parts = CreateRunnablePlanParts();

            Assert.That(
                () => new RunnablePlan(
                    generationPlan: null!,
                    BuiltInExecutionProfiles.Default,
                    parts.FieldBindings,
                    parts.Stages,
                    parts.Operations),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("generationPlan"));
        }

        [Test]
        public void Constructor_WithNullExecutionProfile_ThrowsArgumentNullException()
        {
            RunnablePlanParts parts = CreateRunnablePlanParts();

            Assert.That(
                () => new RunnablePlan(
                    parts.GenerationPlan,
                    executionProfile: null!,
                    parts.FieldBindings,
                    parts.Stages,
                    parts.Operations),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("executionProfile"));
        }

        [Test]
        public void Constructor_WithNonDenseFieldBindings_ThrowsArgumentException()
        {
            RunnablePlanParts parts = CreateRunnablePlanParts();
            ResourceFieldBinding[] fieldBindings = parts.FieldBindings.ToArray();

            fieldBindings[0] = new ResourceFieldBinding(
                new FieldIndex(1),
                fieldBindings[0].ResourceDefinition,
                fieldBindings[0].FieldDefinition,
                fieldBindings[0].PlanRole,
                fieldBindings[0].CapturePolicy);

            Assert.That(
                () => new RunnablePlan(
                    parts.GenerationPlan,
                    BuiltInExecutionProfiles.Default,
                    fieldBindings,
                    parts.Stages,
                    parts.Operations),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("fieldBindings"));
        }

        [Test]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            RunnablePlanParts parts = CreateRunnablePlanParts();

            var plan = new RunnablePlan(
                parts.GenerationPlan,
                BuiltInExecutionProfiles.Default,
                parts.FieldBindings,
                parts.Stages,
                parts.Operations);

            Assert.That(plan.GenerationPlan, Is.SameAs(parts.GenerationPlan));
            Assert.That(plan.ExecutionProfile, Is.SameAs(BuiltInExecutionProfiles.Default));
            Assert.That(plan.FieldBindings, Is.EqualTo(parts.FieldBindings));
            Assert.That(plan.Stages, Is.EqualTo(parts.Stages));
            Assert.That(plan.Operations, Is.EqualTo(parts.Operations));
        }

        [Test]
        public void Equals_WithSameValuesAndReferences_ReturnsTrue()
        {
            RunnablePlanParts parts = CreateRunnablePlanParts();

            var left = new RunnablePlan(
                parts.GenerationPlan,
                BuiltInExecutionProfiles.Default,
                parts.FieldBindings,
                parts.Stages,
                parts.Operations);

            var right = new RunnablePlan(
                parts.GenerationPlan,
                BuiltInExecutionProfiles.Default,
                parts.FieldBindings,
                parts.Stages,
                parts.Operations);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void ToString_ContainsHighSignalIdentityData()
        {
            RunnablePlanParts parts = CreateRunnablePlanParts();

            var plan = new RunnablePlan(
                parts.GenerationPlan,
                BuiltInExecutionProfiles.Default,
                parts.FieldBindings,
                parts.Stages,
                parts.Operations);

            string text = plan.ToString();

            Assert.That(text, Does.Contain(nameof(RunnablePlan)));
            Assert.That(text, Does.Contain(parts.GenerationPlan.GenerationRecipeDefinition.Symbol.ToString()));
            Assert.That(text, Does.Contain(BuiltInExecutionProfiles.Default.Symbol.ToString()));
            Assert.That(text, Does.Contain(nameof(RunnablePlan.FieldBindings)));
            Assert.That(text, Does.Contain(nameof(RunnablePlan.Stages)));
            Assert.That(text, Does.Contain(nameof(RunnablePlan.Operations)));
        }

        private static RunnablePlanParts CreateRunnablePlanParts()
        {
            GenerationPlan generationPlan = CompilePrimaryContinentalLandmassPlan();
            FieldDefinition[] fieldDefinitions = LandmassFieldDefinitionSet.Default.FieldDefinitions.ToArray();
            Array.Sort(fieldDefinitions, static (left, right) => left.Symbol.CompareTo(right.Symbol));

            var fieldIndicesByResourceSymbol = new Dictionary<Symbol, FieldIndex>();
            var fieldBindings = new ResourceFieldBinding[fieldDefinitions.Length];

            for (int index = 0; index < fieldDefinitions.Length; index++)
            {
                FieldDefinition fieldDefinition = fieldDefinitions[index];
                var fieldIndex = new FieldIndex(index);

                fieldIndicesByResourceSymbol.Add(fieldDefinition.ResourceDefinition.Symbol, fieldIndex);

                fieldBindings[index] = new ResourceFieldBinding(
                    fieldIndex,
                    fieldDefinition.ResourceDefinition,
                    fieldDefinition,
                    FieldPlanRole.ProducedOutput,
                    FieldCapturePolicy.DoNotCapture);
            }

            StagePlanNode stagePlanNode = generationPlan.StagePlanNodes[0];
            var operations = new RunnableOperation[stagePlanNode.OperationPlanNodes.Count];
            var operationIndices = new OperationIndex[stagePlanNode.OperationPlanNodes.Count];

            for (int operationIndex = 0;
                operationIndex < stagePlanNode.OperationPlanNodes.Count;
                operationIndex++)
            {
                OperationPlanNode operationPlanNode = stagePlanNode.OperationPlanNodes[operationIndex];
                operationIndices[operationIndex] = new OperationIndex(operationIndex);

                operations[operationIndex] = new RunnableOperation(
                    operationIndices[operationIndex],
                    new StageIndex(0),
                    operationPlanNode,
                    GetFieldIndices(operationPlanNode.OperationContract.RequiredInputs, fieldIndicesByResourceSymbol),
                    GetFieldIndices(operationPlanNode.OperationContract.ProducedOutputs, fieldIndicesByResourceSymbol));
            }

            var stages = new[]
            {
                new RunnableStage(
                    new StageIndex(0),
                    stagePlanNode,
                    GetFieldIndices(stagePlanNode.StageContract.RequiredInputs, fieldIndicesByResourceSymbol),
                    GetFieldIndices(stagePlanNode.StageContract.ProducedOutputs, fieldIndicesByResourceSymbol),
                    operationIndices)
            };

            return new RunnablePlanParts(
                generationPlan,
                fieldBindings,
                stages,
                operations);
        }

        private static FieldIndex[] GetFieldIndices(
            IReadOnlyList<ResourceDefinition> resourceDefinitions,
            IReadOnlyDictionary<Symbol, FieldIndex> fieldIndicesByResourceSymbol)
        {
            var fieldIndices = new FieldIndex[resourceDefinitions.Count];

            for (int index = 0; index < resourceDefinitions.Count; index++)
            {
                fieldIndices[index] = fieldIndicesByResourceSymbol[resourceDefinitions[index].Symbol];
            }

            return fieldIndices;
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

        private sealed class RunnablePlanParts
        {
            public RunnablePlanParts(
                GenerationPlan generationPlan,
                ResourceFieldBinding[] fieldBindings,
                RunnableStage[] stages,
                RunnableOperation[] operations)
            {
                GenerationPlan = generationPlan;
                FieldBindings = fieldBindings;
                Stages = stages;
                Operations = operations;
            }

            public GenerationPlan GenerationPlan { get; }

            public ResourceFieldBinding[] FieldBindings { get; }

            public RunnableStage[] Stages { get; }

            public RunnableOperation[] Operations { get; }
        }
    }
}
