#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassGenerationCatalogTests
    {
        [Test]
        public void AddTo_WithNullBuilder_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LandmassGenerationCatalog.AddTo(null!));
        }

        [Test]
        public void AddTo_WithValidBuilder_ReturnsSameBuilder()
        {
            var builder = new GenerationCatalogBuilder();

            GenerationCatalogBuilder returnedBuilder = LandmassGenerationCatalog.AddTo(builder);

            Assert.That(returnedBuilder, Is.SameAs(builder));
        }

        [Test]
        public void AddTo_DoesNotAddSharedGenerationSchemas()
        {
            var builder = new GenerationCatalogBuilder();

            LandmassGenerationCatalog.AddTo(builder);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void AddTo_WithSharedGenerationSchema_BuildsCatalogContainingExpectedLandmassDefinitions()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog
                .AddTo(
                    new GenerationCatalogBuilder()
                        .AddGenerationSchemaDefinition(BuiltInGenerationSchemas.World))
                .Build();

            AssertCatalogContainsExpectedLandmassDefinitions(catalog);
        }

        [Test]
        public void CreateCatalog_BuildsStandaloneCatalogContainingExpectedLandmassDefinitions()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            AssertCatalogContainsExpectedLandmassDefinitions(catalog);
        }

        private static void AssertCatalogContainsExpectedLandmassDefinitions(GenerationCatalog catalog)
        {
            AssertSameSequence(
                catalog.GenerationSchemaDefinitions,
                new[]
                {
                    BuiltInGenerationSchemas.World
                });

            AssertSameSequence(
                catalog.ResourceDefinitions,
                LandmassResourceDefinitions.All);

            AssertSameSequence(
                catalog.StageDefinitions,
                LandmassStageDefinitions.All);

            AssertSameSequence(
                catalog.StageRouteDefinitions,
                LandmassStageRoutes.All);

            AssertSameSequence(
                catalog.StageRouteStepDefinitions,
                LandmassStageRouteSteps.All);

            AssertSameSequence(
                catalog.StageContracts,
                LandmassStageContracts.All);

            AssertSameSequence(
                catalog.OperationDefinitions,
                LandmassOperationDefinitions.All);

            AssertSameSequence(
                catalog.OperationImplementationDefinitions,
                LandmassOperationImplementations.All);

            AssertSameSequence(
                catalog.OperationContracts,
                LandmassOperationContracts.All);

            AssertSameSequence(
                catalog.GenerationRecipeDefinitions,
                LandmassGenerationRecipes.All);

            AssertCatalogLookup(
                BuiltInGenerationSchemas.World.Symbol,
                BuiltInGenerationSchemas.World,
                catalog.ContainsGenerationSchemaDefinition,
                catalog.GetGenerationSchemaDefinition);

            foreach (ResourceDefinition resourceDefinition in LandmassResourceDefinitions.All)
            {
                AssertCatalogLookup(
                    resourceDefinition.Symbol,
                    resourceDefinition,
                    catalog.ContainsResourceDefinition,
                    catalog.GetResourceDefinition);
            }

            foreach (StageDefinition stageDefinition in LandmassStageDefinitions.All)
            {
                AssertCatalogLookup(
                    stageDefinition.Symbol,
                    stageDefinition,
                    catalog.ContainsStageDefinition,
                    catalog.GetStageDefinition);
            }

            foreach (StageRouteDefinition stageRouteDefinition in LandmassStageRoutes.All)
            {
                AssertCatalogLookup(
                    stageRouteDefinition.Symbol,
                    stageRouteDefinition,
                    catalog.ContainsStageRouteDefinition,
                    catalog.GetStageRouteDefinition);
            }

            foreach (StageRouteStepDefinition stageRouteStepDefinition in LandmassStageRouteSteps.All)
            {
                AssertCatalogLookup(
                    stageRouteStepDefinition.Symbol,
                    stageRouteStepDefinition,
                    catalog.ContainsStageRouteStepDefinition,
                    catalog.GetStageRouteStepDefinition);
            }

            foreach (StageContract stageContract in LandmassStageContracts.All)
            {
                AssertCatalogLookup(
                    stageContract.StageDefinition.Symbol,
                    stageContract,
                    catalog.ContainsStageContract,
                    catalog.GetStageContract);
            }

            foreach (OperationDefinition operationDefinition in LandmassOperationDefinitions.All)
            {
                AssertCatalogLookup(
                    operationDefinition.Symbol,
                    operationDefinition,
                    catalog.ContainsOperationDefinition,
                    catalog.GetOperationDefinition);
            }

            foreach (OperationImplementationDefinition operationImplementationDefinition in
                LandmassOperationImplementations.All)
            {
                AssertCatalogLookup(
                    operationImplementationDefinition.Symbol,
                    operationImplementationDefinition,
                    catalog.ContainsOperationImplementationDefinition,
                    catalog.GetOperationImplementationDefinition);
            }

            foreach (OperationContract operationContract in LandmassOperationContracts.All)
            {
                AssertCatalogLookup(
                    operationContract.OperationDefinition.Symbol,
                    operationContract,
                    catalog.ContainsOperationContract,
                    catalog.GetOperationContract);
            }

            foreach (GenerationRecipeDefinition generationRecipeDefinition in LandmassGenerationRecipes.All)
            {
                AssertCatalogLookup(
                    generationRecipeDefinition.Symbol,
                    generationRecipeDefinition,
                    catalog.ContainsGenerationRecipeDefinition,
                    catalog.GetGenerationRecipeDefinition);
            }
        }

        private static void AssertSameSequence<TDefinition>(
            IReadOnlyList<TDefinition> actualDefinitions,
            IReadOnlyList<TDefinition> expectedDefinitions)
            where TDefinition : class
        {
            Assert.That(actualDefinitions, Has.Count.EqualTo(expectedDefinitions.Count));

            for (int index = 0; index < expectedDefinitions.Count; index++)
            {
                Assert.That(
                    actualDefinitions[index],
                    Is.SameAs(expectedDefinitions[index]));
            }
        }

        private static void AssertCatalogLookup<TDefinition>(
            Symbol symbol,
            TDefinition expectedDefinition,
            Func<Symbol, bool> containsDefinition,
            Func<Symbol, TDefinition> getDefinition)
            where TDefinition : class
        {
            Assert.That(containsDefinition(symbol), Is.True);
            Assert.That(getDefinition(symbol), Is.SameAs(expectedDefinition));
        }
    }
}