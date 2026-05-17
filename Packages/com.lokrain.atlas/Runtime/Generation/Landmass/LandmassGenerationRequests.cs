#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Planning;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides convenience factories for Atlas-owned landmass generation request descriptors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass generation request factories create symbolic generation request descriptors for built-in landmass
    /// recipes. They do not resolve descriptors, compile plans, create executable bindings, schedule jobs,
    /// allocate native containers, or reference Unity runtime objects.
    /// </para>
    /// <para>
    /// The returned descriptors follow the standard architecture: descriptors select recipes and overrides by
    /// symbol, request resolvers resolve descriptors through catalogs into accepted generation requests, and
    /// generation plan compilers transform accepted requests into managed plans.
    /// </para>
    /// </remarks>
    public static class LandmassGenerationRequests
    {
        /// <summary>
        /// Creates a symbolic request descriptor for the built-in primary continental landmass recipe using the
        /// recipe's default implementation choices.
        /// </summary>
        /// <param name="grid">The accepted generation grid.</param>
        /// <param name="seed">The accepted generation seed.</param>
        /// <returns>The symbolic generation request descriptor.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="grid"/> is null.
        /// </exception>
        public static GenerationRequestDescriptor CreatePrimaryContinentalLandmass(
            Grid grid,
            Seed seed)
        {
            if (grid is null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            return CreatePrimaryContinentalLandmass(
                new GenerationRunSettings(grid, seed));
        }

        /// <summary>
        /// Creates a symbolic request descriptor for the built-in primary continental landmass recipe using the
        /// recipe's default implementation choices.
        /// </summary>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <returns>The symbolic generation request descriptor.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="runSettings"/> is null.
        /// </exception>
        public static GenerationRequestDescriptor CreatePrimaryContinentalLandmass(
            GenerationRunSettings runSettings)
        {
            if (runSettings is null)
            {
                throw new ArgumentNullException(nameof(runSettings));
            }

            return new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol,
                runSettings);
        }

        /// <summary>
        /// Creates a symbolic request descriptor for the built-in primary continental landmass recipe using the
        /// specified implementation overrides.
        /// </summary>
        /// <param name="grid">The accepted generation grid.</param>
        /// <param name="seed">The accepted generation seed.</param>
        /// <param name="operationImplementationOverrides">The operation implementation overrides.</param>
        /// <returns>The symbolic generation request descriptor.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="grid"/> or <paramref name="operationImplementationOverrides"/> is null.
        /// </exception>
        public static GenerationRequestDescriptor CreatePrimaryContinentalLandmass(
            Grid grid,
            Seed seed,
            IEnumerable<OperationImplementationOverrideDescriptor> operationImplementationOverrides)
        {
            if (grid is null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            return CreatePrimaryContinentalLandmass(
                new GenerationRunSettings(grid, seed),
                operationImplementationOverrides);
        }

        /// <summary>
        /// Creates a symbolic request descriptor for the built-in primary continental landmass recipe using the
        /// specified implementation overrides.
        /// </summary>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <param name="operationImplementationOverrides">The operation implementation overrides.</param>
        /// <returns>The symbolic generation request descriptor.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="runSettings"/> or <paramref name="operationImplementationOverrides"/> is null.
        /// </exception>
        public static GenerationRequestDescriptor CreatePrimaryContinentalLandmass(
            GenerationRunSettings runSettings,
            IEnumerable<OperationImplementationOverrideDescriptor> operationImplementationOverrides)
        {
            if (runSettings is null)
            {
                throw new ArgumentNullException(nameof(runSettings));
            }

            if (operationImplementationOverrides is null)
            {
                throw new ArgumentNullException(nameof(operationImplementationOverrides));
            }

            return new(
                LandmassGenerationRecipes.PrimaryContinentalLandmass.Symbol,
                runSettings,
                operationImplementationOverrides);
        }
    }
}