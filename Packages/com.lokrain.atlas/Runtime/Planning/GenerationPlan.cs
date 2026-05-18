#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a compiler-created managed semantic plan for one generation run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation plan contains the accepted generation recipe, generation-wide run settings, and resolved
    /// managed stage plan nodes created by the generation plan compiler.
    /// </para>
    /// <para>
    /// Stage plan node order is compiler-created managed execution order. The order must satisfy stage contract
    /// dependencies: every required stage input must be available from a previous stage output.
    /// </para>
    /// <para>
    /// Dependency validation uses accepted semantic resource definitions, not raw resource symbols.
    /// </para>
    /// <para>
    /// This type is managed planning data only. It does not contain executable metadata, scheduler bindings,
    /// runtime state, job data, native containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Runnable-plan compilation later converts this managed plan into execution-ready metadata. Workspace,
    /// scheduler, native-storage, and job ownership belong after this boundary.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationPlan"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationPlan : IEquatable<GenerationPlan>
    {
        internal GenerationPlan(
            GenerationRecipeDefinition generationRecipeDefinition,
            GenerationRunSettings runSettings,
            IEnumerable<StagePlanNode> stagePlanNodes)
        {
            if (generationRecipeDefinition is null)
            {
                throw new ArgumentNullException(nameof(generationRecipeDefinition));
            }

            if (runSettings is null)
            {
                throw new ArgumentNullException(nameof(runSettings));
            }

            if (stagePlanNodes is null)
            {
                throw new ArgumentNullException(nameof(stagePlanNodes));
            }

            StagePlanNode[] copiedStagePlanNodes = CopyStagePlanNodes(
                generationRecipeDefinition,
                stagePlanNodes);

            ValidateStagePlanNodeOrder(copiedStagePlanNodes);

            GenerationRecipeDefinition = generationRecipeDefinition;
            RunSettings = runSettings;
            StagePlanNodes = new ReadOnlyCollection<StagePlanNode>(copiedStagePlanNodes);
        }

        /// <summary>
        /// Gets the accepted generation recipe.
        /// </summary>
        public GenerationRecipeDefinition GenerationRecipeDefinition { get; }

        /// <summary>
        /// Gets the generation-wide run settings.
        /// </summary>
        public GenerationRunSettings RunSettings { get; }

        /// <summary>
        /// Gets the accepted generation schema definition.
        /// </summary>
        public GenerationSchemaDefinition GenerationSchemaDefinition =>
            GenerationRecipeDefinition.GenerationSchemaDefinition;

        /// <summary>
        /// Gets the accepted generation grid.
        /// </summary>
        public Grid Grid => RunSettings.Grid;

        /// <summary>
        /// Gets the accepted generation seed.
        /// </summary>
        public Seed Seed => RunSettings.Seed;

        /// <summary>
        /// Gets the resolved managed stage plan nodes in compiler-created managed execution order.
        /// </summary>
        public IReadOnlyList<StagePlanNode> StagePlanNodes { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationPlan? other)
        {
            return other is not null
                && GenerationRecipeDefinition == other.GenerationRecipeDefinition
                && RunSettings == other.RunSettings
                && SequenceEquals(StagePlanNodes, other.StagePlanNodes);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationPlan other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationRecipeDefinition);
            hashCode.Add(RunSettings);

            for (int index = 0; index < StagePlanNodes.Count; index++)
            {
                hashCode.Add(StagePlanNodes[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationPlan)}({nameof(GenerationRecipeDefinition)}: {GenerationRecipeDefinition.Symbol}, {nameof(RunSettings)}: {RunSettings}, {nameof(StagePlanNodes)}: {StagePlanNodes.Count})";
        }

        /// <summary>
        /// Determines whether two generation plans are equal.
        /// </summary>
        public static bool operator ==(GenerationPlan? left, GenerationPlan? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation plans are not equal.
        /// </summary>
        public static bool operator !=(GenerationPlan? left, GenerationPlan? right)
        {
            return !Equals(left, right);
        }

        private static StagePlanNode[] CopyStagePlanNodes(
            GenerationRecipeDefinition generationRecipeDefinition,
            IEnumerable<StagePlanNode> stagePlanNodes)
        {
            Dictionary<Symbol, StageRouteChoice> recipeStageRouteChoicesByRouteSymbol =
                CreateRecipeStageRouteChoiceIndex(generationRecipeDefinition.StageRouteChoices);

            var copiedStagePlanNodes = new List<StagePlanNode>();
            var stageDefinitionSymbols = new HashSet<Symbol>();
            var stageRouteDefinitionSymbols = new HashSet<Symbol>();

            foreach (StagePlanNode? stagePlanNode in stagePlanNodes)
            {
                if (stagePlanNode is null)
                {
                    throw new ArgumentException(
                        "Stage plan nodes cannot contain null entries.",
                        nameof(stagePlanNodes));
                }

                if (!ReferenceEquals(
                    stagePlanNode.StageDefinition.GenerationSchema,
                    generationRecipeDefinition.GenerationSchemaDefinition))
                {
                    throw new ArgumentException(
                        $"Stage plan node for stage definition '{stagePlanNode.StageDefinition.Symbol}' belongs to generation schema '{stagePlanNode.StageDefinition.GenerationSchema.Symbol}', but the generation recipe belongs to generation schema '{generationRecipeDefinition.GenerationSchemaDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!recipeStageRouteChoicesByRouteSymbol.TryGetValue(
                        stagePlanNode.StageRouteDefinition.Symbol,
                        out StageRouteChoice recipeStageRouteChoice)
                    || !ReferenceEquals(
                        recipeStageRouteChoice.StageRouteDefinition,
                        stagePlanNode.StageRouteDefinition))
                {
                    throw new ArgumentException(
                        $"Stage plan node references stage route definition '{stagePlanNode.StageRouteDefinition.Symbol}', but that route is not selected by generation recipe '{generationRecipeDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!ReferenceEquals(recipeStageRouteChoice.StageDefinition, stagePlanNode.StageDefinition))
                {
                    throw new ArgumentException(
                        $"Stage plan node references stage definition '{stagePlanNode.StageDefinition.Symbol}', but recipe route choice '{recipeStageRouteChoice.StageRouteDefinition.Symbol}' belongs to stage definition '{recipeStageRouteChoice.StageDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!ReferenceEquals(recipeStageRouteChoice.StageContract, stagePlanNode.StageContract))
                {
                    throw new ArgumentException(
                        $"Stage plan node for stage definition '{stagePlanNode.StageDefinition.Symbol}' does not use the stage contract selected by generation recipe '{generationRecipeDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!stageDefinitionSymbols.Add(stagePlanNode.StageDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage plan nodes cannot contain duplicate stage-definition symbol '{stagePlanNode.StageDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!stageRouteDefinitionSymbols.Add(stagePlanNode.StageRouteDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage plan nodes cannot contain duplicate stage-route-definition symbol '{stagePlanNode.StageRouteDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                copiedStagePlanNodes.Add(stagePlanNode);
            }

            if (copiedStagePlanNodes.Count != generationRecipeDefinition.StageRouteChoices.Count)
            {
                throw new ArgumentException(
                    $"Generation plan for recipe '{generationRecipeDefinition.Symbol}' requires {generationRecipeDefinition.StageRouteChoices.Count} stage plan nodes, but {copiedStagePlanNodes.Count} were provided.",
                    nameof(stagePlanNodes));
            }

            return copiedStagePlanNodes.ToArray();
        }

        private static Dictionary<Symbol, StageRouteChoice> CreateRecipeStageRouteChoiceIndex(
            IReadOnlyList<StageRouteChoice> stageRouteChoices)
        {
            var stageRouteChoicesByRouteSymbol = new Dictionary<Symbol, StageRouteChoice>();

            for (int index = 0; index < stageRouteChoices.Count; index++)
            {
                StageRouteChoice choice = stageRouteChoices[index];

                stageRouteChoicesByRouteSymbol.Add(
                    choice.StageRouteDefinition.Symbol,
                    choice);
            }

            return stageRouteChoicesByRouteSymbol;
        }

        private static void ValidateStagePlanNodeOrder(
            IReadOnlyList<StagePlanNode> stagePlanNodes)
        {
            var availableResources = new HashSet<ResourceDefinition>();

            for (int index = 0; index < stagePlanNodes.Count; index++)
            {
                StagePlanNode stagePlanNode = stagePlanNodes[index];

                for (int inputIndex = 0;
                    inputIndex < stagePlanNode.StageContract.RequiredInputs.Count;
                    inputIndex++)
                {
                    ResourceDefinition requiredInput = stagePlanNode.StageContract.RequiredInputs[inputIndex];

                    if (!availableResources.Contains(requiredInput))
                    {
                        throw new ArgumentException(
                            $"Stage plan node at index {index} for stage definition '{stagePlanNode.StageDefinition.Symbol}' requires input resource '{requiredInput.Symbol}', but that resource is not available from previous stage outputs.",
                            nameof(stagePlanNodes));
                    }
                }

                for (int outputIndex = 0;
                    outputIndex < stagePlanNode.StageContract.ProducedOutputs.Count;
                    outputIndex++)
                {
                    availableResources.Add(stagePlanNode.StageContract.ProducedOutputs[outputIndex]);
                }
            }
        }

        private static bool SequenceEquals<TValue>(
            IReadOnlyList<TValue> left,
            IReadOnlyList<TValue> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var comparer = EqualityComparer<TValue>.Default;

            for (int index = 0; index < left.Count; index++)
            {
                if (!comparer.Equals(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}