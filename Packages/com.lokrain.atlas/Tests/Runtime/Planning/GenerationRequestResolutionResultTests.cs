#nullable enable

using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationRequestResolutionResultTests
    {
        private const string UnknownRecipeSymbolText =
            "lokrain.atlas.tests.resolution_result.recipe.unknown";

        private const string OtherUnknownRecipeSymbolText =
            "lokrain.atlas.tests.resolution_result.recipe.other_unknown";

        [Test]
        public void Resolve_WithKnownRecipe_ReturnsSuccessfulResult()
        {
            GenerationRequestResolutionResult result = ResolveKnownRecipe();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.GenerationRequest, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Resolve_WithUnknownRecipe_ReturnsFailedResult()
        {
            Symbol unknownRecipeSymbol = Symbol.Create(UnknownRecipeSymbolText);

            GenerationRequestResolutionResult result = ResolveUnknownRecipe(unknownRecipeSymbol);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.GenerationRequest, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].SubjectSymbol, Is.EqualTo(unknownRecipeSymbol));
        }

        [Test]
        public void Errors_OnFailedResult_AreStableSnapshot()
        {
            Symbol unknownRecipeSymbol = Symbol.Create(UnknownRecipeSymbolText);

            GenerationRequestResolutionResult result = ResolveUnknownRecipe(unknownRecipeSymbol);

            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].SubjectSymbol, Is.EqualTo(unknownRecipeSymbol));
        }

        [Test]
        public void Equals_WithEquivalentSuccessfulResults_ReturnsTrue()
        {
            GenerationRequestResolutionResult left = ResolveKnownRecipe();
            GenerationRequestResolutionResult right = ResolveKnownRecipe();

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithEquivalentFailedResults_ReturnsTrue()
        {
            Symbol unknownRecipeSymbol = Symbol.Create(UnknownRecipeSymbolText);

            GenerationRequestResolutionResult left = ResolveUnknownRecipe(unknownRecipeSymbol);
            GenerationRequestResolutionResult right = ResolveUnknownRecipe(unknownRecipeSymbol);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithSuccessfulAndFailedResults_ReturnsFalse()
        {
            GenerationRequestResolutionResult left = ResolveKnownRecipe();
            GenerationRequestResolutionResult right = ResolveUnknownRecipe(
                Symbol.Create(UnknownRecipeSymbolText));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentFailedErrors_ReturnsFalse()
        {
            GenerationRequestResolutionResult left = ResolveUnknownRecipe(
                Symbol.Create(UnknownRecipeSymbolText));

            GenerationRequestResolutionResult right = ResolveUnknownRecipe(
                Symbol.Create(OtherUnknownRecipeSymbolText));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationRequestResolutionResult result = ResolveKnownRecipe();

            Assert.That(result.Equals("GenerationRequestResolutionResult"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationRequestResolutionResult? left = null;
            GenerationRequestResolutionResult? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationRequestResolutionResult? left = ResolveKnownRecipe();
            GenerationRequestResolutionResult? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_WithSuccessfulResult_ReturnsSuccessSummary()
        {
            GenerationRequestResolutionResult result = ResolveKnownRecipe();

            string value = result.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRequestResolutionResult(Succeeded: true, GenerationRequest: GenerationRequest(GenerationRecipeDefinition: lokrain.atlas.landmass.recipe.primary_continental_landmass, RunSettings: GenerationRunSettings(Grid: Grid(Width: 256, Depth: 256), Seed: 123), StageRouteStepImplementationChoices: 5))"));
        }

        [Test]
        public void ToString_WithFailedResult_ReturnsFailureSummary()
        {
            GenerationRequestResolutionResult result = ResolveUnknownRecipe(
                Symbol.Create(UnknownRecipeSymbolText));

            string value = result.ToString();

            Assert.That(
                value,
                Is.EqualTo("GenerationRequestResolutionResult(Succeeded: false, Errors: 1)"));
        }

        private static GenerationRequestResolutionResult ResolveKnownRecipe()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    new Grid(256, 256),
                    new Seed(123UL));

            return new GenerationRequestResolver().Resolve(catalog, descriptor);
        }

        private static GenerationRequestResolutionResult ResolveUnknownRecipe(Symbol recipeSymbol)
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            var descriptor = new GenerationRequestDescriptor(
                recipeSymbol,
                new GenerationRunSettings(
                    new Grid(256, 256),
                    new Seed(123UL)));

            return new GenerationRequestResolver().Resolve(catalog, descriptor);
        }
    }
}