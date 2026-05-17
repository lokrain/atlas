#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Catalog.Tests
{
    public sealed class GenerationCatalogBuilderTests
    {
        [Test]
        public void Build_WithOnlyGenerationSchemas_ReturnsCatalogSnapshot()
        {
            GenerationSchemaDefinition firstSchema = CreateSchemaDefinition(
                "lokrain.atlas.tests.schema.first",
                "First Test Schema");

            GenerationSchemaDefinition secondSchema = CreateSchemaDefinition(
                "lokrain.atlas.tests.schema.second",
                "Second Test Schema");

            var builder = new GenerationCatalogBuilder();

            GenerationCatalog catalog = builder
                .AddGenerationSchemaDefinition(firstSchema)
                .AddGenerationSchemaDefinition(secondSchema)
                .Build();

            Assert.That(catalog.GenerationSchemaDefinitions, Has.Count.EqualTo(2));
            Assert.That(catalog.GenerationSchemaDefinitions[0], Is.SameAs(firstSchema));
            Assert.That(catalog.GenerationSchemaDefinitions[1], Is.SameAs(secondSchema));
        }

        [Test]
        public void Build_WithResourceDefinitions_ReturnsCatalogSnapshotAndResourceLookup()
        {
            GenerationSchemaDefinition schema = CreateSchemaDefinition(
                "lokrain.atlas.tests.schema.resources",
                "Resource Test Schema");

            ResourceDefinition firstResource = CreateResourceDefinition(
                schema,
                "lokrain.atlas.tests.resource.first",
                "First Test Resource");

            ResourceDefinition secondResource = CreateResourceDefinition(
                schema,
                "lokrain.atlas.tests.resource.second",
                "Second Test Resource");

            var builder = new GenerationCatalogBuilder();

            GenerationCatalog catalog = builder
                .AddGenerationSchemaDefinition(schema)
                .AddResourceDefinition(firstResource)
                .AddResourceDefinition(secondResource)
                .Build();

            Assert.That(catalog.ResourceDefinitions, Has.Count.EqualTo(2));
            Assert.That(catalog.ResourceDefinitions[0], Is.SameAs(firstResource));
            Assert.That(catalog.ResourceDefinitions[1], Is.SameAs(secondResource));

            Assert.That(catalog.ContainsResourceDefinition(firstResource.Symbol), Is.True);
            Assert.That(catalog.ContainsResourceDefinition(secondResource.Symbol), Is.True);

            Assert.That(catalog.GetResourceDefinition(firstResource.Symbol), Is.SameAs(firstResource));
            Assert.That(catalog.GetResourceDefinition(secondResource.Symbol), Is.SameAs(secondResource));

            Assert.That(
                catalog.TryGetResourceDefinition(firstResource.Symbol, out ResourceDefinition? resolvedResource),
                Is.True);

            Assert.That(resolvedResource, Is.SameAs(firstResource));
        }

        [Test]
        public void Build_AfterBuilderMutation_DoesNotMutatePreviousCatalogSnapshot()
        {
            GenerationSchemaDefinition firstSchema = CreateSchemaDefinition(
                "lokrain.atlas.tests.schema.first",
                "First Test Schema");

            GenerationSchemaDefinition secondSchema = CreateSchemaDefinition(
                "lokrain.atlas.tests.schema.second",
                "Second Test Schema");

            ResourceDefinition resource = CreateResourceDefinition(
                secondSchema,
                "lokrain.atlas.tests.resource.second",
                "Second Test Resource");

            var builder = new GenerationCatalogBuilder();

            GenerationCatalog firstCatalog = builder
                .AddGenerationSchemaDefinition(firstSchema)
                .Build();

            GenerationCatalog secondCatalog = builder
                .AddGenerationSchemaDefinition(secondSchema)
                .AddResourceDefinition(resource)
                .Build();

            Assert.That(firstCatalog.GenerationSchemaDefinitions, Has.Count.EqualTo(1));
            Assert.That(firstCatalog.GenerationSchemaDefinitions[0], Is.SameAs(firstSchema));
            Assert.That(firstCatalog.ResourceDefinitions, Is.Empty);

            Assert.That(secondCatalog.GenerationSchemaDefinitions, Has.Count.EqualTo(2));
            Assert.That(secondCatalog.GenerationSchemaDefinitions[0], Is.SameAs(firstSchema));
            Assert.That(secondCatalog.GenerationSchemaDefinitions[1], Is.SameAs(secondSchema));
            Assert.That(secondCatalog.ResourceDefinitions, Has.Count.EqualTo(1));
            Assert.That(secondCatalog.ResourceDefinitions[0], Is.SameAs(resource));
        }

        [Test]
        public void AddSingleDefinition_WithNullDefinition_ThrowsArgumentNullException()
        {
            var builder = new GenerationCatalogBuilder();

            Assert.Throws<ArgumentNullException>(
                () => builder.AddGenerationSchemaDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddResourceDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddGenerationRecipeDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddStageDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddStageRouteDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddStageContract(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddOperationDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddOperationImplementationDefinition(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddOperationContract(null!));
        }

        [Test]
        public void AddRange_WithNullCollection_ThrowsArgumentNullException()
        {
            var builder = new GenerationCatalogBuilder();

            Assert.Throws<ArgumentNullException>(
                () => builder.AddGenerationSchemaDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddResourceDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddGenerationRecipeDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddStageDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddStageRouteDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddStageContracts(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddOperationDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddOperationImplementationDefinitions(null!));

            Assert.Throws<ArgumentNullException>(
                () => builder.AddOperationContracts(null!));
        }

        [Test]
        public void AddRange_WithNullEntry_ThrowsArgumentException()
        {
            var builder = new GenerationCatalogBuilder();

            Assert.Throws<ArgumentException>(
                () => builder.AddGenerationSchemaDefinitions(
                    new GenerationSchemaDefinition[]
                    {
                        BuiltInGenerationSchemas.World,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddResourceDefinitions(
                    new ResourceDefinition[]
                    {
                        LandmassResourceDefinitions.ContinentSuitability,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddGenerationRecipeDefinitions(
                    new GenerationRecipeDefinition[]
                    {
                        LandmassGenerationRecipes.PrimaryContinentalLandmass,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddStageDefinitions(
                    new StageDefinition[]
                    {
                        LandmassStageDefinitions.ContinentalLandmass,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddStageRouteDefinitions(
                    new StageRouteDefinition[]
                    {
                        LandmassStageRoutes.PrimaryContinentalLandmass,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddStageContracts(
                    new StageContract[]
                    {
                        LandmassStageContracts.ContinentalLandmass,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddOperationDefinitions(
                    new OperationDefinition[]
                    {
                        LandmassOperationDefinitions.EvaluateContinentSuitability,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddOperationImplementationDefinitions(
                    new OperationImplementationDefinition[]
                    {
                        LandmassOperationImplementations.EvaluateContinentSuitabilityDefault,
                        null!
                    }));

            Assert.Throws<ArgumentException>(
                () => builder.AddOperationContracts(
                    new OperationContract[]
                    {
                        LandmassOperationContracts.EvaluateContinentSuitability,
                        null!
                    }));
        }

        [Test]
        public void ToString_ReturnsDefinitionCounts()
        {
            var builder = new GenerationCatalogBuilder();

            builder
                .AddGenerationSchemaDefinition(BuiltInGenerationSchemas.World)
                .AddResourceDefinition(LandmassResourceDefinitions.ContinentSuitability)
                .AddStageDefinition(LandmassStageDefinitions.ContinentalLandmass)
                .AddOperationDefinition(LandmassOperationDefinitions.EvaluateContinentSuitability);

            string value = builder.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationCatalogBuilder(1 schema definitions, 1 resource definitions, 0 recipe definitions, 1 stage definitions, 0 stage route definitions, 0 stage contracts, 1 operation definitions, 0 operation implementation definitions, 0 operation contracts)"));
        }

        private static GenerationSchemaDefinition CreateSchemaDefinition(
            string symbol,
            string displayName)
        {
            return new GenerationSchemaDefinition(
                Symbol.Create(symbol),
                DisplayName.Create(displayName));
        }

        private static ResourceDefinition CreateResourceDefinition(
            GenerationSchemaDefinition generationSchema,
            string symbol,
            string displayName)
        {
            return new ResourceDefinition(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                generationSchema);
        }
    }
}