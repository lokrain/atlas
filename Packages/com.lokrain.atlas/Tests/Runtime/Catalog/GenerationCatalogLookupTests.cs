#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Catalog.Tests
{
    public sealed class GenerationCatalogLookupTests
    {
        [Test]
        public void Contains_WithKnownSymbols_ReturnsTrue()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            Assert.That(
                catalog.ContainsGenerationSchemaDefinition(BuiltInGenerationSchemas.World.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsResourceDefinition(LandmassResourceDefinitions.MainContinent.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsGenerationRecipeDefinition(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsStageDefinition(LandmassStageDefinitions.ContinentalLandmass.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsStageRouteDefinition(LandmassStageRoutes.PrimaryContinentalLandmass.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsStageRouteStepDefinition(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsStageContract(LandmassStageDefinitions.ContinentalLandmass.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsOperationDefinition(LandmassOperationDefinitions.ExtractMainContinent.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsOperationImplementationDefinition(
                    LandmassOperationImplementations.ExtractMainContinentDefault.Symbol),
                Is.True);

            Assert.That(
                catalog.ContainsOperationContract(LandmassOperationDefinitions.ExtractMainContinent.Symbol),
                Is.True);
        }

        [Test]
        public void Contains_WithUnknownSymbols_ReturnsFalse()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            Assert.That(
                catalog.ContainsGenerationSchemaDefinition(Unknown("schema")),
                Is.False);

            Assert.That(
                catalog.ContainsResourceDefinition(Unknown("resource")),
                Is.False);

            Assert.That(
                catalog.ContainsGenerationRecipeDefinition(Unknown("recipe")),
                Is.False);

            Assert.That(
                catalog.ContainsStageDefinition(Unknown("stage")),
                Is.False);

            Assert.That(
                catalog.ContainsStageRouteDefinition(Unknown("route")),
                Is.False);

            Assert.That(
                catalog.ContainsStageRouteStepDefinition(Unknown("route_step")),
                Is.False);

            Assert.That(
                catalog.ContainsStageContract(Unknown("stage_contract")),
                Is.False);

            Assert.That(
                catalog.ContainsOperationDefinition(Unknown("operation")),
                Is.False);

            Assert.That(
                catalog.ContainsOperationImplementationDefinition(Unknown("implementation")),
                Is.False);

            Assert.That(
                catalog.ContainsOperationContract(Unknown("operation_contract")),
                Is.False);
        }

        [Test]
        public void Get_WithKnownSymbols_ReturnsCatalogOwnedDefinitions()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            Assert.That(
                catalog.GetGenerationSchemaDefinition(BuiltInGenerationSchemas.World.Symbol),
                Is.SameAs(BuiltInGenerationSchemas.World));

            Assert.That(
                catalog.GetResourceDefinition(LandmassResourceDefinitions.MainContinent.Symbol),
                Is.SameAs(LandmassResourceDefinitions.MainContinent));

            Assert.That(
                catalog.GetGenerationRecipeDefinition(LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol),
                Is.SameAs(LandmassGenerationRecipes.PrimaryContinentalLandmass));

            Assert.That(
                catalog.GetStageDefinition(LandmassStageDefinitions.ContinentalLandmass.Symbol),
                Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));

            Assert.That(
                catalog.GetStageRouteDefinition(LandmassStageRoutes.PrimaryContinentalLandmass.Symbol),
                Is.SameAs(LandmassStageRoutes.PrimaryContinentalLandmass));

            Assert.That(
                catalog.GetStageRouteStepDefinition(
                    LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol),
                Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(
                catalog.GetStageContract(LandmassStageDefinitions.ContinentalLandmass.Symbol),
                Is.SameAs(LandmassStageContracts.ContinentalLandmass));

            Assert.That(
                catalog.GetOperationDefinition(LandmassOperationDefinitions.ExtractMainContinent.Symbol),
                Is.SameAs(LandmassOperationDefinitions.ExtractMainContinent));

            Assert.That(
                catalog.GetOperationImplementationDefinition(
                    LandmassOperationImplementations.ExtractMainContinentDefault.Symbol),
                Is.SameAs(LandmassOperationImplementations.ExtractMainContinentDefault));

            Assert.That(
                catalog.GetOperationContract(LandmassOperationDefinitions.ExtractMainContinent.Symbol),
                Is.SameAs(LandmassOperationContracts.ExtractMainContinent));
        }

        [Test]
        public void Get_WithUnknownSymbols_ThrowsKeyNotFoundException()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetGenerationSchemaDefinition(Unknown("schema")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetResourceDefinition(Unknown("resource")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetGenerationRecipeDefinition(Unknown("recipe")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetStageDefinition(Unknown("stage")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetStageRouteDefinition(Unknown("route")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetStageRouteStepDefinition(Unknown("route_step")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetStageContract(Unknown("stage_contract")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetOperationDefinition(Unknown("operation")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetOperationImplementationDefinition(Unknown("implementation")));

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetOperationContract(Unknown("operation_contract")));
        }

        [Test]
        public void TryGet_WithKnownSymbols_ReturnsTrueAndCatalogOwnedDefinitions()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            bool schemaFound = catalog.TryGetGenerationSchemaDefinition(
                BuiltInGenerationSchemas.World.Symbol,
                out Schemas.GenerationSchemaDefinition? schema);

            bool resourceFound = catalog.TryGetResourceDefinition(
                LandmassResourceDefinitions.MainContinent.Symbol,
                out ResourceDefinition? resource);

            bool recipeFound = catalog.TryGetGenerationRecipeDefinition(
                LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol,
                out Recipes.GenerationRecipeDefinition? recipe);

            bool stageFound = catalog.TryGetStageDefinition(
                LandmassStageDefinitions.ContinentalLandmass.Symbol,
                out Stages.StageDefinition? stage);

            bool routeFound = catalog.TryGetStageRouteDefinition(
                LandmassStageRoutes.PrimaryContinentalLandmass.Symbol,
                out Stages.StageRouteDefinition? route);

            bool routeStepFound = catalog.TryGetStageRouteStepDefinition(
                LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent.Symbol,
                out Stages.StageRouteStepDefinition? routeStep);

            bool stageContractFound = catalog.TryGetStageContract(
                LandmassStageDefinitions.ContinentalLandmass.Symbol,
                out Stages.StageContract? stageContract);

            bool operationFound = catalog.TryGetOperationDefinition(
                LandmassOperationDefinitions.ExtractMainContinent.Symbol,
                out Operations.OperationDefinition? operation);

            bool implementationFound = catalog.TryGetOperationImplementationDefinition(
                LandmassOperationImplementations.ExtractMainContinentDefault.Symbol,
                out Operations.OperationImplementationDefinition? implementation);

            bool operationContractFound = catalog.TryGetOperationContract(
                LandmassOperationDefinitions.ExtractMainContinent.Symbol,
                out Operations.OperationContract? operationContract);

            Assert.That(schemaFound, Is.True);
            Assert.That(schema, Is.SameAs(BuiltInGenerationSchemas.World));

            Assert.That(resourceFound, Is.True);
            Assert.That(resource, Is.SameAs(LandmassResourceDefinitions.MainContinent));

            Assert.That(recipeFound, Is.True);
            Assert.That(recipe, Is.SameAs(LandmassGenerationRecipes.PrimaryContinentalLandmass));

            Assert.That(stageFound, Is.True);
            Assert.That(stage, Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));

            Assert.That(routeFound, Is.True);
            Assert.That(route, Is.SameAs(LandmassStageRoutes.PrimaryContinentalLandmass));

            Assert.That(routeStepFound, Is.True);
            Assert.That(routeStep, Is.SameAs(LandmassStageRouteSteps.PrimaryContinentalLandmassExtractMainContinent));

            Assert.That(stageContractFound, Is.True);
            Assert.That(stageContract, Is.SameAs(LandmassStageContracts.ContinentalLandmass));

            Assert.That(operationFound, Is.True);
            Assert.That(operation, Is.SameAs(LandmassOperationDefinitions.ExtractMainContinent));

            Assert.That(implementationFound, Is.True);
            Assert.That(implementation, Is.SameAs(LandmassOperationImplementations.ExtractMainContinentDefault));

            Assert.That(operationContractFound, Is.True);
            Assert.That(operationContract, Is.SameAs(LandmassOperationContracts.ExtractMainContinent));
        }

        [Test]
        public void TryGet_WithUnknownSymbols_ReturnsFalse()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            Assert.That(
                catalog.TryGetGenerationSchemaDefinition(Unknown("schema"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetResourceDefinition(Unknown("resource"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetGenerationRecipeDefinition(Unknown("recipe"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetStageDefinition(Unknown("stage"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetStageRouteDefinition(Unknown("route"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetStageRouteStepDefinition(Unknown("route_step"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetStageContract(Unknown("stage_contract"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetOperationDefinition(Unknown("operation"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetOperationImplementationDefinition(Unknown("implementation"), out _),
                Is.False);

            Assert.That(
                catalog.TryGetOperationContract(Unknown("operation_contract"), out _),
                Is.False);
        }

        [Test]
        public void LookupMethods_WithNullSymbol_ThrowArgumentNullException()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsGenerationSchemaDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetGenerationSchemaDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetGenerationSchemaDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsResourceDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetResourceDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetResourceDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsGenerationRecipeDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetGenerationRecipeDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetGenerationRecipeDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsStageDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetStageDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetStageDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsStageRouteDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetStageRouteDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetStageRouteDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsStageRouteStepDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetStageRouteStepDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetStageRouteStepDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsStageContract(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetStageContract(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetStageContract(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsOperationDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetOperationDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetOperationDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsOperationImplementationDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetOperationImplementationDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetOperationImplementationDefinition(null!, out _));

            Assert.Throws<ArgumentNullException>(
                () => catalog.ContainsOperationContract(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.GetOperationContract(null!));

            Assert.Throws<ArgumentNullException>(
                () => catalog.TryGetOperationContract(null!, out _));
        }

        [Test]
        public void ToString_ReturnsCatalogCounts()
        {
            GenerationCatalog catalog = LandmassGenerationCatalog.CreateCatalog();

            string value = catalog.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationCatalog(GenerationSchemaDefinitions: 1, ResourceDefinitions: 5, GenerationRecipeDefinitions: 1, StageDefinitions: 1, StageRouteDefinitions: 1, StageRouteStepDefinitions: 5, OperationDefinitions: 5, OperationImplementationDefinitions: 5)"));
        }

        private static Symbol Unknown(string suffix)
        {
            return Symbol.Create("lokrain.atlas.tests.catalog_lookup.unknown." + suffix);
        }
    }
}