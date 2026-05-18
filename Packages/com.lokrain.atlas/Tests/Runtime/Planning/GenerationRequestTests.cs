#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationRequestTests
    {
        [Test]
        public void Constructor_WithRecipeAndRunSettings_UsesRecipeDefaultImplementationChoices()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                runSettings);

            Assert.That(
                request.GenerationRecipeDefinition,
                Is.SameAs(LandmassGenerationRecipes.PrimaryContinentalLandmass));

            Assert.That(request.RunSettings, Is.SameAs(runSettings));

            Assert.That(
                request.StageRouteStepImplementationChoices,
                Has.Count.EqualTo(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass
                        .StageRouteStepImplementationChoices
                        .Count));

            Assert.That(
                request.StageRouteStepImplementationChoices[0],
                Is.SameAs(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass
                        .StageRouteStepImplementationChoices[0]));
        }

        [Test]
        public void Constructor_WithExplicitChoices_StoresChoicesInOrder()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            IReadOnlyList<StageRouteStepImplementationChoice> defaultChoices =
                LandmassGenerationRecipes.PrimaryContinentalLandmass.StageRouteStepImplementationChoices;

            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                runSettings,
                defaultChoices);

            Assert.That(request.StageRouteStepImplementationChoices, Has.Count.EqualTo(defaultChoices.Count));

            for (int index = 0; index < defaultChoices.Count; index++)
            {
                Assert.That(request.StageRouteStepImplementationChoices[index], Is.SameAs(defaultChoices[index]));
            }
        }

        [Test]
        public void Constructor_WithChoiceSourceList_StoresSnapshot()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();

            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings(),
                choices);

            choices.RemoveAt(choices.Count - 1);

            Assert.That(request.StageRouteStepImplementationChoices, Has.Count.EqualTo(5));
        }

        [Test]
        public void StageRouteStepImplementationChoices_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            Assert.That(
                request.StageRouteStepImplementationChoices,
                Is.InstanceOf<ICollection<StageRouteStepImplementationChoice>>());

            ICollection<StageRouteStepImplementationChoice> choices =
                (ICollection<StageRouteStepImplementationChoice>)request.StageRouteStepImplementationChoices;

            Assert.That(choices.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => choices.Add(request.StageRouteStepImplementationChoices[0]));

            Assert.Throws<NotSupportedException>(
                choices.Clear);
        }

        [Test]
        public void Constructor_WithNullRecipe_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequest(
                    null!,
                    CreateRunSettings()));
        }

        [Test]
        public void Constructor_WithNullRunSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    null!));
        }

        [Test]
        public void Constructor_WithNullChoicesCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    null!));
        }

        [Test]
        public void Constructor_WithNullChoiceEntry_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices[0] = null!;

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithMissingRouteStepChoice_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices.RemoveAt(choices.Count - 1);

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithDuplicateRouteStepChoice_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices[1] = choices[0];

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithChoiceForUnselectedRouteStep_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices[0] = CreateUnselectedRouteStepChoice();

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithSymbolEquivalentButDifferentRouteStepInstance_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices[0] = CreateSymbolEquivalentEvaluateContinentSuitabilityChoice();

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithChoiceForDifferentGenerationSchema_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices[0] = CreateDifferentSchemaEvaluateContinentSuitabilityChoice();

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithRouteStepInputNotAvailableFromStageInputOrPreviousStep_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();
            choices[0] = CreateEvaluateContinentSuitabilityChoiceWithUnavailableInput();

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithRouteNotProducingRequiredStageOutput_ThrowsArgumentException()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();

            ReplaceChoice(
                choices,
                LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation.Symbol,
                CreateComposeBaseElevationChoiceWithWrongOutput());

            Assert.Throws<ArgumentException>(
                () => new GenerationRequest(
                    LandmassGenerationRecipes.PrimaryContinentalLandmass,
                    CreateRunSettings(),
                    choices));
        }

        [Test]
        public void Constructor_WithAlternateCompatibleImplementationChoice_CreatesAcceptedRequest()
        {
            List<StageRouteStepImplementationChoice> choices = CreateDefaultChoiceList();

            StageRouteStepImplementationChoice alternateChoice =
                CreateAlternateExtractMainContinentChoice();

            ReplaceChoice(
                choices,
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                alternateChoice);

            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings(),
                choices);

            StageRouteStepImplementationChoice selectedChoice = FindChoice(
                request,
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol);

            Assert.That(selectedChoice, Is.SameAs(alternateChoice));

            Assert.That(
                selectedChoice.OperationImplementationDefinition.Symbol,
                Is.EqualTo(Symbol.Create("lokrain.atlas.tests.implementation.extract_main_continent.alternate")));
        }

        [Test]
        public void Equals_WithSameRecipeRunSettingsAndChoices_ReturnsTrue()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequest left = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                runSettings);

            GenerationRequest right = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                runSettings);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithEquivalentRunSettings_ReturnsTrue()
        {
            GenerationRequest left = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                new GenerationRunSettings(new Grid(256, 256), new Seed(123UL)));

            GenerationRequest right = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                new GenerationRunSettings(new Grid(256, 256), new Seed(123UL)));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentRunSettings_ReturnsFalse()
        {
            GenerationRequest left = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                new GenerationRunSettings(new Grid(256, 256), new Seed(123UL)));

            GenerationRequest right = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                new GenerationRunSettings(new Grid(512, 256), new Seed(123UL)));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentImplementationChoice_ReturnsFalse()
        {
            List<StageRouteStepImplementationChoice> alternateChoices = CreateDefaultChoiceList();

            ReplaceChoice(
                alternateChoices,
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                CreateAlternateExtractMainContinentChoice());

            GenerationRequest left = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            GenerationRequest right = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings(),
                alternateChoices);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithSameChoicesInDifferentOrder_ReturnsFalse()
        {
            List<StageRouteStepImplementationChoice> reversedChoices = CreateDefaultChoiceList();
            reversedChoices.Reverse();

            GenerationRequest left = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            GenerationRequest right = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings(),
                reversedChoices);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            Assert.That(request.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            Assert.That(request.Equals("GenerationRequest"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationRequest? left = null;
            GenerationRequest? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationRequest? left = new GenerationRequest(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            GenerationRequest? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsRequestSummary()
        {
            GenerationRequest request = new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass,
                CreateRunSettings());

            string value = request.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRequest(GenerationRecipeDefinition: lokrain.atlas.landmass.recipe.primary_continental_landmass, RunSettings: GenerationRunSettings(Grid: Grid(Width: 256, Depth: 256), Seed: 123), StageRouteStepImplementationChoices: 5)"));
        }

        private static GenerationRunSettings CreateRunSettings()
        {
            return new(
                new Grid(256, 256),
                new Seed(123UL));
        }

        private static List<StageRouteStepImplementationChoice> CreateDefaultChoiceList()
        {
            return new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass.StageRouteStepImplementationChoices);
        }

        private static StageRouteStepImplementationChoice CreateAlternateExtractMainContinentChoice()
        {
            OperationImplementationDefinition alternateImplementation = new(
                LandmassOperationDefinitions.ExtractMainContinent,
                Symbol.Create("lokrain.atlas.tests.implementation.extract_main_continent.alternate"),
                DisplayName.Create("Alternate Extract Main Continent"));

            return new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent,
                LandmassOperationDefinitions.ExtractMainContinent,
                LandmassOperationContracts.ExtractMainContinent,
                alternateImplementation);
        }

        private static StageRouteStepImplementationChoice CreateUnselectedRouteStepChoice()
        {
            StageRouteStepDefinition routeStep = new(
                Symbol.Create("lokrain.atlas.tests.route_step.unselected"),
                DisplayName.Create("Unselected Route Step"),
                LandmassOperationDefinitions.EvaluateContinentSuitability.Symbol);

            return new(
                routeStep,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                LandmassOperationContracts.EvaluateContinentSuitability,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault);
        }

        private static StageRouteStepImplementationChoice CreateSymbolEquivalentEvaluateContinentSuitabilityChoice()
        {
            StageRouteStepDefinition routeStep = new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability.Symbol,
                DisplayName.Create("Symbol Equivalent Evaluate Continent Suitability"),
                LandmassOperationDefinitions.EvaluateContinentSuitability.Symbol);

            return new(
                routeStep,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                LandmassOperationContracts.EvaluateContinentSuitability,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault);
        }

        private static StageRouteStepImplementationChoice CreateDifferentSchemaEvaluateContinentSuitabilityChoice()
        {
            GenerationSchemaDefinition schema = new(
                Symbol.Create("lokrain.atlas.tests.schema.alternative"),
                DisplayName.Create("Alternative Test Schema"));

            OperationDefinition operationDefinition = new(
                schema,
                LandmassOperationKinds.ContinentSuitabilityEvaluation,
                LandmassOperationDefinitions.EvaluateContinentSuitability.Symbol,
                DisplayName.Create("Alternative Evaluate Continent Suitability"));

            ResourceDefinition output = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.alternative_continent_suitability",
                "Alternative Continent Suitability");

            OperationContract operationContract = new(
                operationDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    output
                });

            OperationImplementationDefinition implementationDefinition = new(
                operationDefinition,
                Symbol.Create("lokrain.atlas.tests.implementation.alternative_evaluate_continent_suitability.default"),
                DisplayName.Create("Alternative Evaluate Continent Suitability Default"));

            return new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                operationDefinition,
                operationContract,
                implementationDefinition);
        }

        private static StageRouteStepImplementationChoice CreateEvaluateContinentSuitabilityChoiceWithUnavailableInput()
        {
            ResourceDefinition missingInput = CreateWorldResource(
                "lokrain.atlas.tests.resource.missing_input",
                "Missing Input");

            OperationContract operationContract = new(
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                new[]
                {
                    missingInput
                },
                new[]
                {
                    LandmassResourceDefinitions.ContinentSuitability
                });

            return new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                operationContract,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault);
        }

        private static StageRouteStepImplementationChoice CreateComposeBaseElevationChoiceWithWrongOutput()
        {
            ResourceDefinition wrongOutput = CreateWorldResource(
                "lokrain.atlas.tests.resource.wrong_base_elevation",
                "Wrong Base Elevation");

            OperationContract operationContract = new(
                LandmassOperationDefinitions.ComposeBaseElevation,
                new[]
                {
                    LandmassResourceDefinitions.ContinentalLandmassArea
                },
                new[]
                {
                    wrongOutput
                });

            return new(
                LandmassStageRouteSteps.PrimaryContinentalLandmassComposeBaseElevation,
                LandmassOperationDefinitions.ComposeBaseElevation,
                operationContract,
                LandmassOperationImplementations.ComposeBaseElevationDefault);
        }

        private static ResourceDefinition CreateWorldResource(
            string symbol,
            string displayName)
        {
            return CreateResource(
                BuiltInGenerationSchemas.World,
                symbol,
                displayName);
        }

        private static ResourceDefinition CreateResource(
            GenerationSchemaDefinition schema,
            string symbol,
            string displayName)
        {
            return new(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                schema);
        }

        private static void ReplaceChoice(
            IList<StageRouteStepImplementationChoice> choices,
            Symbol routeStepSymbol,
            StageRouteStepImplementationChoice replacement)
        {
            for (int index = 0; index < choices.Count; index++)
            {
                if (choices[index].StageRouteStepDefinition.Symbol == routeStepSymbol)
                {
                    choices[index] = replacement;
                    return;
                }
            }

            Assert.Fail("Expected route step choice was not found: " + routeStepSymbol);
        }

        private static StageRouteStepImplementationChoice FindChoice(
            GenerationRequest request,
            Symbol routeStepSymbol)
        {
            for (int index = 0; index < request.StageRouteStepImplementationChoices.Count; index++)
            {
                StageRouteStepImplementationChoice choice =
                    request.StageRouteStepImplementationChoices[index];

                if (choice.StageRouteStepDefinition.Symbol == routeStepSymbol)
                {
                    return choice;
                }
            }

            Assert.Fail("Expected route step choice was not found: " + routeStepSymbol);
            return null!;
        }
    }
}