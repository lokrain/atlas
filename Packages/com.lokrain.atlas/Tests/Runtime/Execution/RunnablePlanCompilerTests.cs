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
    public sealed class RunnablePlanCompilerTests
    {
        [Test]
        public void Compile_WithNullGenerationPlan_ThrowsArgumentNullException()
        {
            var compiler = new RunnablePlanCompiler();

            Assert.That(
                () => compiler.Compile(
                    generationPlan: null!,
                    LandmassFieldDefinitionSet.Default,
                    BuiltInExecutionProfiles.Default),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("generationPlan"));
        }

        [Test]
        public void Compile_WithNullFieldDefinitionSet_ThrowsArgumentNullException()
        {
            var compiler = new RunnablePlanCompiler();

            Assert.That(
                () => compiler.Compile(
                    CompilePrimaryContinentalLandmassPlan(),
                    fieldDefinitionSet: null!,
                    BuiltInExecutionProfiles.Default),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fieldDefinitionSet"));
        }

        [Test]
        public void Compile_WithNullExecutionProfile_ThrowsArgumentNullException()
        {
            var compiler = new RunnablePlanCompiler();

            Assert.That(
                () => compiler.Compile(
                    CompilePrimaryContinentalLandmassPlan(),
                    LandmassFieldDefinitionSet.Default,
                    executionProfile: null!),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("executionProfile"));
        }

        [Test]
        public void Compile_WithPrimaryContinentalLandmassPlan_ReturnsRunnablePlan()
        {
            GenerationPlan generationPlan = CompilePrimaryContinentalLandmassPlan();
            var compiler = new RunnablePlanCompiler();

            RunnablePlanCompilationResult result = compiler.Compile(
                generationPlan,
                LandmassFieldDefinitionSet.Default,
                BuiltInExecutionProfiles.Default);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.RunnablePlan, Is.Not.Null);

            RunnablePlan runnablePlan = result.RunnablePlan!;

            Assert.That(runnablePlan.GenerationPlan, Is.SameAs(generationPlan));
            Assert.That(runnablePlan.ExecutionProfile, Is.SameAs(BuiltInExecutionProfiles.Default));
            Assert.That(runnablePlan.FieldBindings.Count, Is.EqualTo(5));
            Assert.That(runnablePlan.Stages.Count, Is.EqualTo(1));
            Assert.That(runnablePlan.Operations.Count, Is.EqualTo(5));
        }

        [Test]
        public void Compile_FieldBindings_AreOrderedByFieldDefinitionSymbol()
        {
            RunnablePlan runnablePlan = CompileRunnablePlan();

            string[] symbols = runnablePlan.FieldBindings
                .Select(static binding => binding.FieldDefinition.Symbol.ToString())
                .ToArray();

            Assert.That(
                symbols,
                Is.EqualTo(symbols.OrderBy(static symbol => symbol, StringComparer.Ordinal).ToArray()));
        }

        [Test]
        public void Compile_AssignsDenseTableIndices()
        {
            RunnablePlan runnablePlan = CompileRunnablePlan();

            for (int index = 0; index < runnablePlan.FieldBindings.Count; index++)
            {
                Assert.That(runnablePlan.FieldBindings[index].FieldIndex.Value, Is.EqualTo(index));
            }

            for (int index = 0; index < runnablePlan.Stages.Count; index++)
            {
                Assert.That(runnablePlan.Stages[index].StageIndex.Value, Is.EqualTo(index));
            }

            for (int index = 0; index < runnablePlan.Operations.Count; index++)
            {
                Assert.That(runnablePlan.Operations[index].OperationIndex.Value, Is.EqualTo(index));
            }
        }

        [Test]
        public void Compile_OperationsFollowStageRouteOrder()
        {
            GenerationPlan generationPlan = CompilePrimaryContinentalLandmassPlan();
            RunnablePlan runnablePlan = CompileRunnablePlan(generationPlan);

            for (int index = 0; index < generationPlan.StagePlanNodes[0].OperationPlanNodes.Count; index++)
            {
                Assert.That(
                    runnablePlan.Operations[index].OperationPlanNode,
                    Is.SameAs(generationPlan.StagePlanNodes[0].OperationPlanNodes[index]));

                Assert.That(
                    runnablePlan.Stages[0].OperationIndices[index],
                    Is.EqualTo(new OperationIndex(index)));
            }
        }

        [Test]
        public void Compile_FieldReferenceSequencesFollowSourceContractOrder()
        {
            RunnablePlan runnablePlan = CompileRunnablePlan();
            Dictionary<ResourceDefinition, FieldIndex> fieldIndicesByResource = runnablePlan.FieldBindings
                .ToDictionary(
                    static binding => binding.ResourceDefinition,
                    static binding => binding.FieldIndex);

            RunnableStage stage = runnablePlan.Stages[0];

            Assert.That(
                stage.ProducedOutputFieldIndices,
                Is.EqualTo(GetFieldIndices(stage.StagePlanNode.StageContract.ProducedOutputs, fieldIndicesByResource)));

            for (int index = 0; index < runnablePlan.Operations.Count; index++)
            {
                RunnableOperation operation = runnablePlan.Operations[index];

                Assert.That(
                    operation.RequiredInputFieldIndices,
                    Is.EqualTo(GetFieldIndices(operation.OperationPlanNode.OperationContract.RequiredInputs, fieldIndicesByResource)));

                Assert.That(
                    operation.ProducedOutputFieldIndices,
                    Is.EqualTo(GetFieldIndices(operation.OperationPlanNode.OperationContract.ProducedOutputs, fieldIndicesByResource)));
            }
        }

        [Test]
        public void Compile_DerivesFieldPlanRolesFromStageAndOperationContracts()
        {
            RunnablePlan runnablePlan = CompileRunnablePlan();

            Assert.That(
                GetBinding(runnablePlan, LandmassResourceDefinitions.ContinentSuitability).PlanRole,
                Is.EqualTo(FieldPlanRole.RequiredInputAndProducedOutput));

            Assert.That(
                GetBinding(runnablePlan, LandmassResourceDefinitions.ContinentCandidate).PlanRole,
                Is.EqualTo(FieldPlanRole.RequiredInputAndProducedOutput));

            Assert.That(
                GetBinding(runnablePlan, LandmassResourceDefinitions.MainContinent).PlanRole,
                Is.EqualTo(FieldPlanRole.RequiredInputAndProducedOutput));

            Assert.That(
                GetBinding(runnablePlan, LandmassResourceDefinitions.ContinentalLandmassArea).PlanRole,
                Is.EqualTo(FieldPlanRole.RequiredInputAndProducedOutput));

            Assert.That(
                GetBinding(runnablePlan, LandmassResourceDefinitions.BaseElevation).PlanRole,
                Is.EqualTo(FieldPlanRole.ProducedOutput));
        }

        [Test]
        public void Compile_WithMissingFieldDefinition_ReturnsFailureWithoutPartialPlan()
        {
            FieldDefinitionSet fieldDefinitionSet = new(
                LandmassFieldDefinitions.All.Where(
                    static fieldDefinition => !ReferenceEquals(
                        fieldDefinition,
                        LandmassFieldDefinitions.BaseElevation)));

            var compiler = new RunnablePlanCompiler();

            RunnablePlanCompilationResult result = compiler.Compile(
                CompilePrimaryContinentalLandmassPlan(),
                fieldDefinitionSet,
                BuiltInExecutionProfiles.Default);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RunnablePlan, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Code, Is.EqualTo(RunnablePlanCompilationErrorCode.MissingFieldDefinition));
            Assert.That(result.Errors[0].SubjectSymbol, Is.EqualTo(LandmassResourceDefinitions.BaseElevation.Symbol));
        }

        [Test]
        public void Compile_WithSymbolEquivalentFieldDefinitionResource_ReturnsFailureWithoutPartialPlan()
        {
            ResourceDefinition symbolEquivalentBaseElevation = new(
                Symbol.Create(LandmassResourceDefinitions.BaseElevation.Symbol.ToString()),
                DisplayName.Create("Symbol Equivalent Base Elevation"),
                LandmassResourceDefinitions.BaseElevation.GenerationSchema);

            FieldDefinition replacementBaseElevation = new(
                symbolEquivalentBaseElevation,
                LandmassFieldDefinitions.BaseElevation.Symbol,
                LandmassFieldDefinitions.BaseElevation.DisplayName,
                LandmassFieldDefinitions.BaseElevation.Shape,
                LandmassFieldDefinitions.BaseElevation.ValueKind);

            FieldDefinitionSet fieldDefinitionSet = new(
                LandmassFieldDefinitions.All
                    .Where(static fieldDefinition => !ReferenceEquals(fieldDefinition, LandmassFieldDefinitions.BaseElevation))
                    .Concat(new[] { replacementBaseElevation }));

            var compiler = new RunnablePlanCompiler();

            RunnablePlanCompilationResult result = compiler.Compile(
                CompilePrimaryContinentalLandmassPlan(),
                fieldDefinitionSet,
                BuiltInExecutionProfiles.Default);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RunnablePlan, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Code, Is.EqualTo(RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch));
            Assert.That(result.Errors[0].SubjectSymbol, Is.EqualTo(LandmassResourceDefinitions.BaseElevation.Symbol));
        }

        [Test]
        public void CompileOrThrow_WithSuccessfulCompilation_ReturnsRunnablePlan()
        {
            var compiler = new RunnablePlanCompiler();

            RunnablePlan runnablePlan = compiler.CompileOrThrow(
                CompilePrimaryContinentalLandmassPlan(),
                LandmassFieldDefinitionSet.Default,
                BuiltInExecutionProfiles.Default);

            Assert.That(runnablePlan, Is.Not.Null);
        }

        [Test]
        public void CompileOrThrow_WithFailedCompilation_ThrowsInvalidOperationException()
        {
            FieldDefinitionSet fieldDefinitionSet = new(
                LandmassFieldDefinitions.All.Where(
                    static fieldDefinition => !ReferenceEquals(
                        fieldDefinition,
                        LandmassFieldDefinitions.BaseElevation)));

            var compiler = new RunnablePlanCompiler();

            Assert.That(
                () => compiler.CompileOrThrow(
                    CompilePrimaryContinentalLandmassPlan(),
                    fieldDefinitionSet,
                    BuiltInExecutionProfiles.Default),
                Throws.TypeOf<InvalidOperationException>());
        }

        private static RunnablePlan CompileRunnablePlan()
        {
            return CompileRunnablePlan(CompilePrimaryContinentalLandmassPlan());
        }

        private static RunnablePlan CompileRunnablePlan(GenerationPlan generationPlan)
        {
            RunnablePlanCompilationResult result = new RunnablePlanCompiler().Compile(
                generationPlan,
                LandmassFieldDefinitionSet.Default,
                BuiltInExecutionProfiles.Default);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RunnablePlan, Is.Not.Null);

            return result.RunnablePlan!;
        }

        private static ResourceFieldBinding GetBinding(
            RunnablePlan runnablePlan,
            ResourceDefinition resourceDefinition)
        {
            return runnablePlan.FieldBindings.Single(
                binding => ReferenceEquals(binding.ResourceDefinition, resourceDefinition));
        }

        private static FieldIndex[] GetFieldIndices(
            IReadOnlyList<ResourceDefinition> resourceDefinitions,
            IReadOnlyDictionary<ResourceDefinition, FieldIndex> fieldIndicesByResource)
        {
            var fieldIndices = new FieldIndex[resourceDefinitions.Count];

            for (int index = 0; index < resourceDefinitions.Count; index++)
            {
                fieldIndices[index] = fieldIndicesByResource[resourceDefinitions[index]];
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
    }
}
