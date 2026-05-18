#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassGenerationRecipesTests
    {
        [Test]
        public void All_ReturnsGenerationRecipesInDeclaredOrder()
        {
            Assert.That(LandmassGenerationRecipes.All, Has.Count.EqualTo(1));

            Assert.That(
                LandmassGenerationRecipes.All[0],
                Is.SameAs(LandmassGenerationRecipes.PrimaryContinentalLandmass));
        }

        [Test]
        public void PrimaryContinentalLandmass_UsesExpectedSchemaSymbolAndDisplayName()
        {
            GenerationRecipeDefinition recipe = LandmassGenerationRecipes.PrimaryContinentalLandmass;

            Assert.That(recipe.GenerationSchemaDefinition, Is.SameAs(BuiltInGenerationSchemas.World));
            Assert.That(recipe.Symbol.Value, Is.EqualTo("lokrain.atlas.landmass.recipe.primary_continental_landmass"));
            Assert.That(recipe.DisplayName.Value, Is.EqualTo("Primary Continental Landmass"));
        }

        [Test]
        public void PrimaryContinentalLandmass_SelectsExpectedStageRouteChoice()
        {
            GenerationRecipeDefinition recipe = LandmassGenerationRecipes.PrimaryContinentalLandmass;

            Assert.That(recipe.StageRouteChoices, Has.Count.EqualTo(1));

            AssertStageRouteChoice(
                recipe.StageRouteChoices[0],
                LandmassStageDefinitions.ContinentalLandmass,
                LandmassStageRoutes.PrimaryContinentalLandmass,
                LandmassStageContracts.ContinentalLandmass);
        }

        [Test]
        public void PrimaryContinentalLandmass_SelectsExpectedImplementationChoicesInDeclaredOrder()
        {
            GenerationRecipeDefinition recipe = LandmassGenerationRecipes.PrimaryContinentalLandmass;

            Assert.That(recipe.StageRouteStepImplementationChoices, Has.Count.EqualTo(5));

            AssertStageRouteStepImplementationChoice(
                recipe.StageRouteStepImplementationChoices[0],
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                LandmassOperationContracts.EvaluateContinentSuitability,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault);

            AssertStageRouteStepImplementationChoice(
                recipe.StageRouteStepImplementationChoices[1],
                LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate,
                LandmassOperationDefinitions.FormContinentCandidate,
                LandmassOperationContracts.FormContinentCandidate,
                LandmassOperationImplementations.FormContinentCandidateDefault);

            AssertStageRouteStepImplementationChoice(
                recipe.StageRouteStepImplementationChoices[2],
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent,
                LandmassOperationDefinitions.ExtractMainContinent,
                LandmassOperationContracts.ExtractMainContinent,
                LandmassOperationImplementations.ExtractMainContinentDefault);

            AssertStageRouteStepImplementationChoice(
                recipe.StageRouteStepImplementationChoices[3],
                LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea,
                LandmassOperationDefinitions.CompleteContinentArea,
                LandmassOperationContracts.CompleteContinentArea,
                LandmassOperationImplementations.CompleteContinentAreaDefault);

            AssertStageRouteStepImplementationChoice(
                recipe.StageRouteStepImplementationChoices[4],
                LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation,
                LandmassOperationDefinitions.ComposeBaseElevation,
                LandmassOperationContracts.ComposeBaseElevation,
                LandmassOperationImplementations.ComposeBaseElevationDefault);
        }

        [Test]
        public void All_DoesNotContainDuplicateGenerationRecipeSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (GenerationRecipeDefinition recipe in LandmassGenerationRecipes.All)
            {
                Assert.That(
                    symbols.Add(recipe.Symbol.Value),
                    Is.True,
                    $"Duplicate generation recipe symbol: {recipe.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassGenerationRecipes.All,
                Is.InstanceOf<ICollection<GenerationRecipeDefinition>>());

            ICollection<GenerationRecipeDefinition> recipes =
                (ICollection<GenerationRecipeDefinition>)LandmassGenerationRecipes.All;

            Assert.That(recipes.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => recipes.Add(LandmassGenerationRecipes.PrimaryContinentalLandmass));

            Assert.Throws<NotSupportedException>(
                recipes.Clear);
        }

        private static void AssertStageRouteChoice(
            StageRouteChoice stageRouteChoice,
            StageDefinition expectedStageDefinition,
            StageRouteDefinition expectedStageRouteDefinition,
            StageContract expectedStageContract)
        {
            Assert.That(stageRouteChoice.StageDefinition, Is.SameAs(expectedStageDefinition));
            Assert.That(stageRouteChoice.StageRouteDefinition, Is.SameAs(expectedStageRouteDefinition));
            Assert.That(stageRouteChoice.StageContract, Is.SameAs(expectedStageContract));
        }

        private static void AssertStageRouteStepImplementationChoice(
            StageRouteStepImplementationChoice choice,
            StageRouteStepDefinition expectedStageRouteStepDefinition,
            OperationDefinition expectedOperationDefinition,
            OperationContract expectedOperationContract,
            OperationImplementationDefinition expectedOperationImplementationDefinition)
        {
            Assert.That(choice.StageRouteStepDefinition, Is.SameAs(expectedStageRouteStepDefinition));
            Assert.That(choice.OperationDefinition, Is.SameAs(expectedOperationDefinition));
            Assert.That(choice.OperationContract, Is.SameAs(expectedOperationContract));
            Assert.That(choice.OperationImplementationDefinition, Is.SameAs(expectedOperationImplementationDefinition));
        }
    }
}