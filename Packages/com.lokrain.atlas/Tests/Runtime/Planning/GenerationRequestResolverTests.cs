#nullable enable

using System;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationRequestResolverTests
    {
        private static readonly Symbol RecipeNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.recipe_not_found");

        private static readonly Symbol RouteStepNotSelectedByRecipeCode =
            Symbol.Create("lokrain.atlas.planning.route_step_not_selected_by_recipe");

        private static readonly Symbol ImplementationNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.implementation_not_found");

        private static readonly Symbol ImplementationOperationMismatchCode =
            Symbol.Create("lokrain.atlas.planning.implementation_operation_mismatch");

        [Test]
        public void Resolve_WithoutOverrides_UsesRecipeDefaultImplementationChoices()
        {
            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings());

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(
                    LandmassGenerationCatalog.CreateCatalog(),
                    descriptor);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.GenerationRequest, Is.Not.Null);

            Assert.That(
                result.GenerationRequest!.GenerationRecipeDefinition,
                Is.SameAs(LandmassGenerationRecipes.PrimaryContinentalLandmass));

            Assert.That(
                result.GenerationRequest.StageRouteStepImplementationChoices,
                Has.Count.EqualTo(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass
                        .StageRouteStepImplementationChoices
                        .Count));

            for (int index = 0;
                 index < LandmassGenerationRecipes.PrimaryContinentalLandmass.StageRouteStepImplementationChoices.Count;
                 index++)
            {
                Assert.That(
                    result.GenerationRequest.StageRouteStepImplementationChoices[index],
                    Is.SameAs(
                        LandmassGenerationRecipes.PrimaryContinentalLandmass
                            .StageRouteStepImplementationChoices[index]));
            }
        }

        [Test]
        public void Resolve_WithKnownCompatibleImplementationOverride_UsesOverrideInAcceptedRequest()
        {
            OperationImplementationDefinition alternateImplementation =
                CreateAlternateExtractMainContinentImplementation();

            GenerationCatalog catalog = CreateLandmassCatalog(alternateImplementation);

            OperationImplementationOverrideDescriptor overrideDescriptor = new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                alternateImplementation.Symbol);

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    new[]
                    {
                        overrideDescriptor
                    });

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(catalog, descriptor);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.GenerationRequest, Is.Not.Null);

            StageRouteStepImplementationChoice choice = FindChoice(
                result.GenerationRequest!,
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol);

            Assert.That(
                choice.StageRouteStepDefinition,
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(
                choice.OperationDefinition,
                Is.SameAs(LandmassOperationDefinitions.ExtractMainContinent));

            Assert.That(
                choice.OperationContract,
                Is.SameAs(LandmassOperationContracts.ExtractMainContinent));

            Assert.That(
                choice.OperationImplementationDefinition,
                Is.SameAs(alternateImplementation));
        }

        [Test]
        public void Resolve_WithKnownCompatibleImplementationOverride_PreservesRecipeChoiceOrder()
        {
            OperationImplementationDefinition alternateImplementation =
                CreateAlternateExtractMainContinentImplementation();

            GenerationCatalog catalog = CreateLandmassCatalog(alternateImplementation);

            OperationImplementationOverrideDescriptor overrideDescriptor = new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                alternateImplementation.Symbol);

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    new[]
                    {
                        overrideDescriptor
                    });

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(catalog, descriptor);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.GenerationRequest, Is.Not.Null);

            Assert.That(
                result.GenerationRequest!.StageRouteStepImplementationChoices[0].StageRouteStepDefinition,
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability));

            Assert.That(
                result.GenerationRequest.StageRouteStepImplementationChoices[1].StageRouteStepDefinition,
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassFormContinentCandidate));

            Assert.That(
                result.GenerationRequest.StageRouteStepImplementationChoices[2].StageRouteStepDefinition,
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(
                result.GenerationRequest.StageRouteStepImplementationChoices[2].OperationImplementationDefinition,
                Is.SameAs(alternateImplementation));

            Assert.That(
                result.GenerationRequest.StageRouteStepImplementationChoices[3].StageRouteStepDefinition,
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassCompleteContinentArea));

            Assert.That(
                result.GenerationRequest.StageRouteStepImplementationChoices[4].StageRouteStepDefinition,
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation));
        }

        [Test]
        public void Resolve_WithUnknownRecipeSymbol_ReturnsRecipeNotFoundError()
        {
            Symbol unknownRecipeSymbol =
                Symbol.Create("lokrain.atlas.tests.recipe.unknown");

            GenerationRequestDescriptor descriptor = new(
                unknownRecipeSymbol,
                CreateRunSettings());

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(
                    LandmassGenerationCatalog.CreateCatalog(),
                    descriptor);

            AssertSingleError(
                result,
                RecipeNotFoundCode,
                unknownRecipeSymbol);
        }

        [Test]
        public void Resolve_WithOverrideForRouteStepNotSelectedByRecipe_ReturnsRouteStepNotSelectedError()
        {
            Symbol unknownRouteStepSymbol =
                Symbol.Create("lokrain.atlas.tests.route_step.not_selected_by_recipe");

            OperationImplementationOverrideDescriptor overrideDescriptor = new(
                unknownRouteStepSymbol,
                LandmassOperationImplementations.ExtractMainContinentDefault.Symbol);

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    new[]
                    {
                        overrideDescriptor
                    });

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(
                    LandmassGenerationCatalog.CreateCatalog(),
                    descriptor);

            AssertSingleError(
                result,
                RouteStepNotSelectedByRecipeCode,
                unknownRouteStepSymbol);
        }

        [Test]
        public void Resolve_WithUnknownImplementationSymbol_ReturnsImplementationNotFoundError()
        {
            Symbol unknownImplementationSymbol =
                Symbol.Create("lokrain.atlas.tests.implementation.unknown");

            OperationImplementationOverrideDescriptor overrideDescriptor = new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                unknownImplementationSymbol);

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    new[]
                    {
                        overrideDescriptor
                    });

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(
                    LandmassGenerationCatalog.CreateCatalog(),
                    descriptor);

            AssertSingleError(
                result,
                ImplementationNotFoundCode,
                unknownImplementationSymbol);
        }

        [Test]
        public void Resolve_WithImplementationForDifferentOperation_ReturnsImplementationMismatchError()
        {
            OperationImplementationOverrideDescriptor overrideDescriptor = new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault.Symbol);

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    new[]
                    {
                        overrideDescriptor
                    });

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(
                    LandmassGenerationCatalog.CreateCatalog(),
                    descriptor);

            AssertSingleError(
                result,
                ImplementationOperationMismatchCode,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault.Symbol);
        }

        [Test]
        public void Resolve_WithMultipleInvalidOverrides_ReturnsAllErrorsInDescriptorOrder()
        {
            Symbol unknownRouteStepSymbol =
                Symbol.Create("lokrain.atlas.tests.route_step.not_selected_by_recipe");

            Symbol unknownImplementationSymbol =
                Symbol.Create("lokrain.atlas.tests.implementation.unknown");

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    new[]
                    {
                        new OperationImplementationOverrideDescriptor(
                            unknownRouteStepSymbol,
                            LandmassOperationImplementations.ExtractMainContinentDefault.Symbol),

                        new OperationImplementationOverrideDescriptor(
                            LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                            unknownImplementationSymbol)
                    });

            GenerationRequestResolutionResult result =
                new GenerationRequestResolver().Resolve(
                    LandmassGenerationCatalog.CreateCatalog(),
                    descriptor);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.GenerationRequest, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2));

            Assert.That(result.Errors[0].Code, Is.EqualTo(RouteStepNotSelectedByRecipeCode));
            Assert.That(result.Errors[0].SubjectSymbol, Is.EqualTo(unknownRouteStepSymbol));
            Assert.That(result.Errors[0].Message, Is.Not.Empty);

            Assert.That(result.Errors[1].Code, Is.EqualTo(ImplementationNotFoundCode));
            Assert.That(result.Errors[1].SubjectSymbol, Is.EqualTo(unknownImplementationSymbol));
            Assert.That(result.Errors[1].Message, Is.Not.Empty);
        }

        [Test]
        public void Resolve_WithNullCatalog_ThrowsArgumentNullException()
        {
            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings());

            GenerationRequestResolver resolver = new();

            Assert.Throws<ArgumentNullException>(
                () => resolver.Resolve(null!, descriptor));
        }

        [Test]
        public void Resolve_WithNullDescriptor_ThrowsArgumentNullException()
        {
            GenerationRequestResolver resolver = new();

            Assert.Throws<ArgumentNullException>(
                () => resolver.Resolve(LandmassGenerationCatalog.CreateCatalog(), null!));
        }

        private static GenerationRunSettings CreateRunSettings()
        {
            return new(
                new Grid(256, 256),
                new Seed(123UL));
        }

        private static OperationImplementationDefinition CreateAlternateExtractMainContinentImplementation()
        {
            return new(
                LandmassOperationDefinitions.ExtractMainContinent,
                Symbol.Create("lokrain.atlas.tests.implementation.extract_main_continent.alternate"),
                DisplayName.Create("Alternate Extract Main Continent"));
        }

        private static GenerationCatalog CreateLandmassCatalog(
            OperationImplementationDefinition extraImplementation)
        {
            var builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(BuiltInGenerationSchemas.World);

            LandmassGenerationCatalog.AddTo(builder);

            return builder
                .AddOperationImplementationDefinition(extraImplementation)
                .Build();
        }

        private static StageRouteStepImplementationChoice FindChoice(
            GenerationRequest request,
            Symbol stageRouteStepDefinitionSymbol)
        {
            for (int index = 0; index < request.StageRouteStepImplementationChoices.Count; index++)
            {
                StageRouteStepImplementationChoice choice =
                    request.StageRouteStepImplementationChoices[index];

                if (choice.StageRouteStepDefinition.Symbol == stageRouteStepDefinitionSymbol)
                {
                    return choice;
                }
            }

            Assert.Fail(
                $"Expected route step implementation choice for route step '{stageRouteStepDefinitionSymbol}'.");

            return null!;
        }

        private static void AssertSingleError(
            GenerationRequestResolutionResult result,
            Symbol expectedCode,
            Symbol expectedSubjectSymbol)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.GenerationRequest, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));

            GenerationRequestResolutionError error = result.Errors[0];

            Assert.That(error.Code, Is.EqualTo(expectedCode));
            Assert.That(error.SubjectSymbol, Is.EqualTo(expectedSubjectSymbol));
            Assert.That(error.Message, Is.Not.Empty);
        }
    }
}