#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Catalog
{
    /// <summary>
    /// Builds immutable generation catalog snapshots from authored catalog definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation catalog builder is the mutable assembly surface for catalog definitions. It preserves
    /// insertion order and rejects null entries immediately, but final duplicate, ownership, and semantic
    /// graph validation is performed by <see cref="GenerationCatalog"/> when <see cref="Build"/> is called.
    /// </para>
    /// <para>
    /// Resource definitions are catalog-owned semantic resources. They are added before contracts can be
    /// accepted by the catalog, because stage and operation contracts reference resources by accepted
    /// definition object, not by raw symbol.
    /// </para>
    /// <para>
    /// Route step definitions are owned by <see cref="StageRouteDefinition"/> instances and are not added
    /// independently to the builder.
    /// </para>
    /// <para>
    /// The builder is not a catalog, registry, service locator, Unity asset, execution container, ECS world
    /// object, runtime binding table, job scheduler, field-definition registry, or native data owner.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationCatalogBuilder"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationCatalogBuilder
    {
        private readonly List<GenerationSchemaDefinition> _generationSchemaDefinitions = new();

        private readonly List<ResourceDefinition> _resourceDefinitions = new();

        private readonly List<GenerationRecipeDefinition> _generationRecipeDefinitions = new();

        private readonly List<StageDefinition> _stageDefinitions = new();

        private readonly List<StageRouteDefinition> _stageRouteDefinitions = new();

        private readonly List<StageContract> _stageContracts = new();

        private readonly List<OperationDefinition> _operationDefinitions = new();

        private readonly List<OperationImplementationDefinition> _operationImplementationDefinitions = new();

        private readonly List<OperationContract> _operationContracts = new();

        /// <summary>
        /// Adds a generation schema definition to the builder.
        /// </summary>
        /// <param name="generationSchemaDefinition">The generation schema definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationSchemaDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddGenerationSchemaDefinition(
            GenerationSchemaDefinition generationSchemaDefinition)
        {
            AddRequired(
                _generationSchemaDefinitions,
                generationSchemaDefinition,
                nameof(generationSchemaDefinition));

            return this;
        }

        /// <summary>
        /// Adds generation schema definitions to the builder.
        /// </summary>
        /// <param name="generationSchemaDefinitions">The generation schema definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationSchemaDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="generationSchemaDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddGenerationSchemaDefinitions(
            IEnumerable<GenerationSchemaDefinition> generationSchemaDefinitions)
        {
            AddRange(
                _generationSchemaDefinitions,
                generationSchemaDefinitions,
                nameof(generationSchemaDefinitions),
                "Generation schema definitions");

            return this;
        }

        /// <summary>
        /// Adds a resource definition to the builder.
        /// </summary>
        /// <param name="resourceDefinition">The resource definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddResourceDefinition(ResourceDefinition resourceDefinition)
        {
            AddRequired(
                _resourceDefinitions,
                resourceDefinition,
                nameof(resourceDefinition));

            return this;
        }

        /// <summary>
        /// Adds resource definitions to the builder.
        /// </summary>
        /// <param name="resourceDefinitions">The resource definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="resourceDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddResourceDefinitions(
            IEnumerable<ResourceDefinition> resourceDefinitions)
        {
            AddRange(
                _resourceDefinitions,
                resourceDefinitions,
                nameof(resourceDefinitions),
                "Resource definitions");

            return this;
        }

        /// <summary>
        /// Adds a generation recipe definition to the builder.
        /// </summary>
        /// <param name="generationRecipeDefinition">The generation recipe definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationRecipeDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddGenerationRecipeDefinition(
            GenerationRecipeDefinition generationRecipeDefinition)
        {
            AddRequired(
                _generationRecipeDefinitions,
                generationRecipeDefinition,
                nameof(generationRecipeDefinition));

            return this;
        }

        /// <summary>
        /// Adds generation recipe definitions to the builder.
        /// </summary>
        /// <param name="generationRecipeDefinitions">The generation recipe definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationRecipeDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="generationRecipeDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddGenerationRecipeDefinitions(
            IEnumerable<GenerationRecipeDefinition> generationRecipeDefinitions)
        {
            AddRange(
                _generationRecipeDefinitions,
                generationRecipeDefinitions,
                nameof(generationRecipeDefinitions),
                "Generation recipe definitions");

            return this;
        }

        /// <summary>
        /// Adds a stage definition to the builder.
        /// </summary>
        /// <param name="stageDefinition">The stage definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddStageDefinition(StageDefinition stageDefinition)
        {
            AddRequired(
                _stageDefinitions,
                stageDefinition,
                nameof(stageDefinition));

            return this;
        }

        /// <summary>
        /// Adds stage definitions to the builder.
        /// </summary>
        /// <param name="stageDefinitions">The stage definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddStageDefinitions(IEnumerable<StageDefinition> stageDefinitions)
        {
            AddRange(
                _stageDefinitions,
                stageDefinitions,
                nameof(stageDefinitions),
                "Stage definitions");

            return this;
        }

        /// <summary>
        /// Adds a stage route definition to the builder.
        /// </summary>
        /// <param name="stageRouteDefinition">The stage route definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageRouteDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddStageRouteDefinition(StageRouteDefinition stageRouteDefinition)
        {
            AddRequired(
                _stageRouteDefinitions,
                stageRouteDefinition,
                nameof(stageRouteDefinition));

            return this;
        }

        /// <summary>
        /// Adds stage route definitions to the builder.
        /// </summary>
        /// <param name="stageRouteDefinitions">The stage route definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageRouteDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageRouteDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddStageRouteDefinitions(
            IEnumerable<StageRouteDefinition> stageRouteDefinitions)
        {
            AddRange(
                _stageRouteDefinitions,
                stageRouteDefinitions,
                nameof(stageRouteDefinitions),
                "Stage route definitions");

            return this;
        }

        /// <summary>
        /// Adds a stage contract to the builder.
        /// </summary>
        /// <param name="stageContract">The stage contract to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageContract"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddStageContract(StageContract stageContract)
        {
            AddRequired(
                _stageContracts,
                stageContract,
                nameof(stageContract));

            return this;
        }

        /// <summary>
        /// Adds stage contracts to the builder.
        /// </summary>
        /// <param name="stageContracts">The stage contracts to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageContracts"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageContracts"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddStageContracts(IEnumerable<StageContract> stageContracts)
        {
            AddRange(
                _stageContracts,
                stageContracts,
                nameof(stageContracts),
                "Stage contracts");

            return this;
        }

        /// <summary>
        /// Adds an operation definition to the builder.
        /// </summary>
        /// <param name="operationDefinition">The operation definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddOperationDefinition(OperationDefinition operationDefinition)
        {
            AddRequired(
                _operationDefinitions,
                operationDefinition,
                nameof(operationDefinition));

            return this;
        }

        /// <summary>
        /// Adds operation definitions to the builder.
        /// </summary>
        /// <param name="operationDefinitions">The operation definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddOperationDefinitions(
            IEnumerable<OperationDefinition> operationDefinitions)
        {
            AddRange(
                _operationDefinitions,
                operationDefinitions,
                nameof(operationDefinitions),
                "Operation definitions");

            return this;
        }

        /// <summary>
        /// Adds an operation implementation definition to the builder.
        /// </summary>
        /// <param name="operationImplementationDefinition">The operation implementation definition to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationImplementationDefinition"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddOperationImplementationDefinition(
            OperationImplementationDefinition operationImplementationDefinition)
        {
            AddRequired(
                _operationImplementationDefinitions,
                operationImplementationDefinition,
                nameof(operationImplementationDefinition));

            return this;
        }

        /// <summary>
        /// Adds operation implementation definitions to the builder.
        /// </summary>
        /// <param name="operationImplementationDefinitions">The operation implementation definitions to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationImplementationDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationImplementationDefinitions"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddOperationImplementationDefinitions(
            IEnumerable<OperationImplementationDefinition> operationImplementationDefinitions)
        {
            AddRange(
                _operationImplementationDefinitions,
                operationImplementationDefinitions,
                nameof(operationImplementationDefinitions),
                "Operation implementation definitions");

            return this;
        }

        /// <summary>
        /// Adds an operation contract to the builder.
        /// </summary>
        /// <param name="operationContract">The operation contract to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationContract"/> is null.
        /// </exception>
        public GenerationCatalogBuilder AddOperationContract(OperationContract operationContract)
        {
            AddRequired(
                _operationContracts,
                operationContract,
                nameof(operationContract));

            return this;
        }

        /// <summary>
        /// Adds operation contracts to the builder.
        /// </summary>
        /// <param name="operationContracts">The operation contracts to add.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationContracts"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationContracts"/> contains null entries.
        /// </exception>
        public GenerationCatalogBuilder AddOperationContracts(IEnumerable<OperationContract> operationContracts)
        {
            AddRange(
                _operationContracts,
                operationContracts,
                nameof(operationContracts),
                "Operation contracts");

            return this;
        }

        /// <summary>
        /// Builds an immutable accepted generation catalog snapshot.
        /// </summary>
        /// <returns>The built generation catalog.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the collected definitions cannot form a valid generation catalog.
        /// </exception>
        public GenerationCatalog Build()
        {
            return new(
                _generationSchemaDefinitions,
                _resourceDefinitions,
                _generationRecipeDefinitions,
                _stageDefinitions,
                _stageRouteDefinitions,
                _stageContracts,
                _operationDefinitions,
                _operationImplementationDefinitions,
                _operationContracts);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationCatalogBuilder)}({_generationSchemaDefinitions.Count} schema definitions, {_resourceDefinitions.Count} resource definitions, {_generationRecipeDefinitions.Count} recipe definitions, {_stageDefinitions.Count} stage definitions, {_stageRouteDefinitions.Count} stage route definitions, {_stageContracts.Count} stage contracts, {_operationDefinitions.Count} operation definitions, {_operationImplementationDefinitions.Count} operation implementation definitions, {_operationContracts.Count} operation contracts)";
        }

        private static void AddRequired<TDefinition>(
            List<TDefinition> destination,
            TDefinition definition,
            string parameterName)
            where TDefinition : class
        {
            if (definition is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            destination.Add(definition);
        }

        private static void AddRange<TDefinition>(
            List<TDefinition> destination,
            IEnumerable<TDefinition> definitions,
            string parameterName,
            string description)
            where TDefinition : class
        {
            if (definitions is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            foreach (TDefinition? definition in definitions)
            {
                if (definition is null)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain null entries.",
                        parameterName);
                }

                destination.Add(definition);
            }
        }
    }
}