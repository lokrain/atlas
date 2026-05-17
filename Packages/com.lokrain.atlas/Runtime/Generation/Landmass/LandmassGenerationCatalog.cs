#nullable enable

using System;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Generation.Landmass.Routes;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Adds Atlas-owned landmass catalog definitions to a generation catalog builder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass generation catalog registration adds built-in landmass resource definitions, recipes, stage
    /// definitions, stage routes, stage contracts, operation definitions, operation implementations, and operation
    /// contracts required for built-in landmass planning.
    /// </para>
    /// <para>
    /// <see cref="AddTo"/> does not add shared generation schemas. Composition roots should add shared schemas
    /// once, then add one or more generation modules. <see cref="CreateCatalog"/> creates a standalone landmass
    /// catalog and therefore adds required shared schemas before adding landmass definitions.
    /// </para>
    /// <para>
    /// This type does not resolve request descriptors, build runnable plans, resolve executable bindings, schedule
    /// jobs, own native containers, or reference Unity runtime objects.
    /// </para>
    /// </remarks>
    public static class LandmassGenerationCatalog
    {
        /// <summary>
        /// Adds Atlas-owned landmass-specific catalog definitions to the specified builder.
        /// </summary>
        /// <param name="builder">The generation catalog builder.</param>
        /// <returns>The specified builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> is null.
        /// </exception>
        public static GenerationCatalogBuilder AddTo(GenerationCatalogBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder
                .AddResourceDefinitions(LandmassResourceDefinitions.All)
                .AddStageDefinitions(LandmassStageDefinitions.All)
                .AddStageRouteDefinitions(LandmassStageRoutes.All)
                .AddStageContracts(LandmassStageContracts.All)
                .AddOperationDefinitions(LandmassOperationDefinitions.All)
                .AddOperationImplementationDefinitions(LandmassOperationImplementations.All)
                .AddOperationContracts(LandmassOperationContracts.All)
                .AddGenerationRecipeDefinitions(LandmassGenerationRecipes.All);

            return builder;
        }

        /// <summary>
        /// Creates an accepted generation catalog containing Atlas-owned landmass definitions.
        /// </summary>
        /// <returns>The built generation catalog.</returns>
        public static GenerationCatalog CreateCatalog()
        {
            GenerationCatalogBuilder builder = new GenerationCatalogBuilder()
                .AddGenerationSchemaDefinition(BuiltInGenerationSchemas.World);

            return AddTo(builder).Build();
        }
    }
}