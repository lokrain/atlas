#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Planning;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassGenerationRequestsTests
    {
        [Test]
        public void CreatePrimaryContinentalLandmass_WithGridAndSeed_ReturnsDescriptorForPrimaryRecipe()
        {
            var grid = new Grid(256, 256);
            var seed = new Seed(123UL);

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    grid,
                    seed);

            Assert.That(
                descriptor.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol));

            Assert.That(descriptor.RunSettings.Grid, Is.SameAs(grid));
            Assert.That(descriptor.RunSettings.Seed, Is.EqualTo(seed));
            Assert.That(descriptor.OperationImplementationOverrides, Is.Empty);
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithNullGrid_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    null!,
                    new Seed(123UL)));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithRunSettings_ReturnsDescriptorForPrimaryRecipe()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(runSettings);

            Assert.That(
                descriptor.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol));

            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Is.Empty);
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithNullRunSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    null!));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithGridSeedAndOverrides_ReturnsDescriptorWithOverrides()
        {
            var grid = new Grid(256, 256);
            var seed = new Seed(123UL);
            OperationImplementationOverrideDescriptor implementationOverride = CreateImplementationOverride();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    grid,
                    seed,
                    new[]
                    {
                        implementationOverride
                    });

            Assert.That(
                descriptor.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol));

            Assert.That(descriptor.RunSettings.Grid, Is.SameAs(grid));
            Assert.That(descriptor.RunSettings.Seed, Is.EqualTo(seed));
            Assert.That(descriptor.OperationImplementationOverrides, Has.Count.EqualTo(1));
            Assert.That(descriptor.OperationImplementationOverrides[0], Is.SameAs(implementationOverride));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithGridSeedAndNullOverrides_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    new Grid(256, 256),
                    new Seed(123UL),
                    null!));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithNullGridSeedAndOverrides_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    null!,
                    new Seed(123UL),
                    Array.Empty<OperationImplementationOverrideDescriptor>()));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithRunSettingsAndOverrides_ReturnsDescriptorWithOverrides()
        {
            GenerationRunSettings runSettings = CreateRunSettings();
            OperationImplementationOverrideDescriptor implementationOverride = CreateImplementationOverride();

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    runSettings,
                    new[]
                    {
                        implementationOverride
                    });

            Assert.That(
                descriptor.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol));

            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Has.Count.EqualTo(1));
            Assert.That(descriptor.OperationImplementationOverrides[0], Is.SameAs(implementationOverride));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithRunSettingsAndOverrideSourceList_StoresSnapshot()
        {
            GenerationRunSettings runSettings = CreateRunSettings();
            OperationImplementationOverrideDescriptor implementationOverride = CreateImplementationOverride();

            var overrides = new List<OperationImplementationOverrideDescriptor>
            {
                implementationOverride
            };

            GenerationRequestDescriptor descriptor =
                LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    runSettings,
                    overrides);

            overrides.Clear();

            Assert.That(descriptor.OperationImplementationOverrides, Has.Count.EqualTo(1));
            Assert.That(descriptor.OperationImplementationOverrides[0], Is.SameAs(implementationOverride));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithNullRunSettingsAndOverrides_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    null!,
                    Array.Empty<OperationImplementationOverrideDescriptor>()));
        }

        [Test]
        public void CreatePrimaryContinentalLandmass_WithRunSettingsAndNullOverrides_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationRequests.CreatePrimaryContinentalLandmass(
                    CreateRunSettings(),
                    null!));
        }

        private static GenerationRunSettings CreateRunSettings()
        {
            return new GenerationRunSettings(
                new Grid(256, 256),
                new Seed(123UL));
        }

        private static OperationImplementationOverrideDescriptor CreateImplementationOverride()
        {
            return new OperationImplementationOverrideDescriptor(
                LandmassStageRouteSteps.PrimaryContinentalLandmassEvaluateContinentSuitability.Symbol,
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault.Symbol);
        }
    }
}