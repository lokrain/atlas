#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents an accepted generation request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation request combines a resolved generation recipe, generation-wide run settings, and the final
    /// resolved route-step implementation choices for this run. It is an accepted domain object, not a raw symbol
    /// descriptor and not an unresolved user selection.
    /// </para>
    /// <para>
    /// The recipe defines the selected schema and stage routes. The request stores the final implementation
    /// choices after applying any descriptor-level implementation overrides to the recipe defaults.
    /// </para>
    /// <para>
    /// Route and operation compatibility is validated through accepted semantic resource definitions, not raw
    /// resource symbols.
    /// </para>
    /// <para>
    /// A generation request does not contain executable bindings, runtime state, job data, native containers,
    /// ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationRequest"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationRequest : IEquatable<GenerationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRequest"/> class using the recipe's default
        /// implementation choices.
        /// </summary>
        /// <param name="generationRecipeDefinition">The accepted resolved generation recipe.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationRecipeDefinition"/> or <paramref name="runSettings"/> is null.
        /// </exception>
        public GenerationRequest(
            GenerationRecipeDefinition generationRecipeDefinition,
            GenerationRunSettings runSettings)
            : this(
                generationRecipeDefinition,
                runSettings,
                generationRecipeDefinition is null
                    ? throw new ArgumentNullException(nameof(generationRecipeDefinition))
                    : generationRecipeDefinition.StageRouteStepImplementationChoices)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRequest"/> class.
        /// </summary>
        /// <param name="generationRecipeDefinition">The accepted resolved generation recipe.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <param name="stageRouteStepImplementationChoices">The final resolved route-step implementation choices.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when implementation choices do not exactly satisfy the recipe's selected route steps.
        /// </exception>
        public GenerationRequest(
            GenerationRecipeDefinition generationRecipeDefinition,
            GenerationRunSettings runSettings,
            IEnumerable<StageRouteStepImplementationChoice> stageRouteStepImplementationChoices)
        {
            if (generationRecipeDefinition is null)
            {
                throw new ArgumentNullException(nameof(generationRecipeDefinition));
            }

            if (runSettings is null)
            {
                throw new ArgumentNullException(nameof(runSettings));
            }

            if (stageRouteStepImplementationChoices is null)
            {
                throw new ArgumentNullException(nameof(stageRouteStepImplementationChoices));
            }

            StageRouteStepImplementationChoice[] copiedStageRouteStepImplementationChoices =
                CopyStageRouteStepImplementationChoices(
                    generationRecipeDefinition,
                    stageRouteStepImplementationChoices);

            ValidateRouteContractCompatibility(
                generationRecipeDefinition.StageRouteChoices,
                copiedStageRouteStepImplementationChoices);

            GenerationRecipeDefinition = generationRecipeDefinition;
            RunSettings = runSettings;
            StageRouteStepImplementationChoices =
                new ReadOnlyCollection<StageRouteStepImplementationChoice>(
                    copiedStageRouteStepImplementationChoices);
        }

        /// <summary>
        /// Gets the accepted resolved generation recipe.
        /// </summary>
        public GenerationRecipeDefinition GenerationRecipeDefinition { get; }

        /// <summary>
        /// Gets the generation-wide run settings.
        /// </summary>
        public GenerationRunSettings RunSettings { get; }

        /// <summary>
        /// Gets the final resolved route-step implementation choices for this request.
        /// </summary>
        public IReadOnlyList<StageRouteStepImplementationChoice> StageRouteStepImplementationChoices { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationRequest? other)
        {
            return other is not null
                && GenerationRecipeDefinition == other.GenerationRecipeDefinition
                && RunSettings == other.RunSettings
                && SequenceEquals(StageRouteStepImplementationChoices, other.StageRouteStepImplementationChoices);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationRequest other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationRecipeDefinition);
            hashCode.Add(RunSettings);

            for (int index = 0; index < StageRouteStepImplementationChoices.Count; index++)
            {
                hashCode.Add(StageRouteStepImplementationChoices[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationRequest)}({nameof(GenerationRecipeDefinition)}: {GenerationRecipeDefinition.Symbol}, {nameof(RunSettings)}: {RunSettings}, {nameof(StageRouteStepImplementationChoices)}: {StageRouteStepImplementationChoices.Count})";
        }

        /// <summary>
        /// Determines whether two generation requests are equal.
        /// </summary>
        public static bool operator ==(GenerationRequest? left, GenerationRequest? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation requests are not equal.
        /// </summary>
        public static bool operator !=(GenerationRequest? left, GenerationRequest? right)
        {
            return !Equals(left, right);
        }

        private static StageRouteStepImplementationChoice[] CopyStageRouteStepImplementationChoices(
            GenerationRecipeDefinition generationRecipeDefinition,
            IEnumerable<StageRouteStepImplementationChoice> stageRouteStepImplementationChoices)
        {
            Dictionary<Symbol, StageRouteStepDefinition> recipeRouteStepsBySymbol =
                CreateRecipeRouteStepIndex(generationRecipeDefinition.StageRouteChoices);

            var copiedChoices = new List<StageRouteStepImplementationChoice>();
            var selectedRouteStepSymbols = new HashSet<Symbol>();

            foreach (StageRouteStepImplementationChoice? choice in stageRouteStepImplementationChoices)
            {
                if (choice is null)
                {
                    throw new ArgumentException(
                        "Stage route step implementation choices cannot contain null entries.",
                        nameof(stageRouteStepImplementationChoices));
                }

                if (!ReferenceEquals(
                    choice.OperationDefinition.GenerationSchema,
                    generationRecipeDefinition.GenerationSchemaDefinition))
                {
                    throw new ArgumentException(
                        $"Stage route step implementation choice for route step '{choice.StageRouteStepDefinition.Symbol}' belongs to operation generation schema '{choice.OperationDefinition.GenerationSchema.Symbol}', but the generation recipe belongs to generation schema '{generationRecipeDefinition.GenerationSchemaDefinition.Symbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }

                if (!recipeRouteStepsBySymbol.TryGetValue(
                        choice.StageRouteStepDefinition.Symbol,
                        out StageRouteStepDefinition recipeRouteStepDefinition)
                    || !ReferenceEquals(recipeRouteStepDefinition, choice.StageRouteStepDefinition))
                {
                    throw new ArgumentException(
                        $"Stage route step implementation choice references route step '{choice.StageRouteStepDefinition.Symbol}', but that route step is not part of generation recipe '{generationRecipeDefinition.Symbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }

                if (!selectedRouteStepSymbols.Add(choice.StageRouteStepDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage route step implementation choices cannot contain duplicate route-step symbol '{choice.StageRouteStepDefinition.Symbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }

                copiedChoices.Add(choice);
            }

            foreach (Symbol routeStepSymbol in recipeRouteStepsBySymbol.Keys)
            {
                if (!selectedRouteStepSymbols.Contains(routeStepSymbol))
                {
                    throw new ArgumentException(
                        $"Generation request must contain an implementation choice for route step '{routeStepSymbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }
            }

            return copiedChoices.ToArray();
        }

        private static Dictionary<Symbol, StageRouteStepDefinition> CreateRecipeRouteStepIndex(
            IReadOnlyList<StageRouteChoice> stageRouteChoices)
        {
            var routeStepsBySymbol = new Dictionary<Symbol, StageRouteStepDefinition>();

            for (int stageIndex = 0; stageIndex < stageRouteChoices.Count; stageIndex++)
            {
                StageRouteChoice stageRouteChoice = stageRouteChoices[stageIndex];

                for (int stepIndex = 0;
                    stepIndex < stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions.Count;
                    stepIndex++)
                {
                    StageRouteStepDefinition routeStepDefinition =
                        stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions[stepIndex];

                    routeStepsBySymbol.Add(
                        routeStepDefinition.Symbol,
                        routeStepDefinition);
                }
            }

            return routeStepsBySymbol;
        }

        private static void ValidateRouteContractCompatibility(
            IReadOnlyList<StageRouteChoice> stageRouteChoices,
            IReadOnlyList<StageRouteStepImplementationChoice> stageRouteStepImplementationChoices)
        {
            Dictionary<Symbol, StageRouteStepImplementationChoice> implementationChoicesByRouteStepSymbol =
                CreateImplementationChoiceIndex(stageRouteStepImplementationChoices);

            for (int stageIndex = 0; stageIndex < stageRouteChoices.Count; stageIndex++)
            {
                StageRouteChoice stageRouteChoice = stageRouteChoices[stageIndex];

                var availableResources = new HashSet<ResourceDefinition>();

                for (int inputIndex = 0;
                    inputIndex < stageRouteChoice.StageContract.RequiredInputs.Count;
                    inputIndex++)
                {
                    availableResources.Add(stageRouteChoice.StageContract.RequiredInputs[inputIndex]);
                }

                for (int stepIndex = 0;
                    stepIndex < stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions.Count;
                    stepIndex++)
                {
                    StageRouteStepDefinition routeStepDefinition =
                        stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions[stepIndex];

                    StageRouteStepImplementationChoice implementationChoice =
                        implementationChoicesByRouteStepSymbol[routeStepDefinition.Symbol];

                    for (int inputIndex = 0;
                        inputIndex < implementationChoice.OperationContract.RequiredInputs.Count;
                        inputIndex++)
                    {
                        ResourceDefinition requiredInput =
                            implementationChoice.OperationContract.RequiredInputs[inputIndex];

                        if (!availableResources.Contains(requiredInput))
                        {
                            throw new ArgumentException(
                                $"Stage route '{stageRouteChoice.StageRouteDefinition.Symbol}' contains route step '{routeStepDefinition.Symbol}' requiring input resource '{requiredInput.Symbol}', but that resource is not available from the stage contract inputs or previous route steps.",
                                nameof(stageRouteStepImplementationChoices));
                        }
                    }

                    for (int outputIndex = 0;
                        outputIndex < implementationChoice.OperationContract.ProducedOutputs.Count;
                        outputIndex++)
                    {
                        availableResources.Add(implementationChoice.OperationContract.ProducedOutputs[outputIndex]);
                    }
                }

                for (int outputIndex = 0;
                    outputIndex < stageRouteChoice.StageContract.ProducedOutputs.Count;
                    outputIndex++)
                {
                    ResourceDefinition producedOutput =
                        stageRouteChoice.StageContract.ProducedOutputs[outputIndex];

                    if (!availableResources.Contains(producedOutput))
                    {
                        throw new ArgumentException(
                            $"Stage route '{stageRouteChoice.StageRouteDefinition.Symbol}' does not produce required stage output resource '{producedOutput.Symbol}' for stage definition '{stageRouteChoice.StageDefinition.Symbol}'.",
                            nameof(stageRouteStepImplementationChoices));
                    }
                }
            }
        }

        private static Dictionary<Symbol, StageRouteStepImplementationChoice> CreateImplementationChoiceIndex(
            IReadOnlyList<StageRouteStepImplementationChoice> stageRouteStepImplementationChoices)
        {
            var choicesByRouteStepSymbol = new Dictionary<Symbol, StageRouteStepImplementationChoice>();

            for (int index = 0; index < stageRouteStepImplementationChoices.Count; index++)
            {
                StageRouteStepImplementationChoice choice = stageRouteStepImplementationChoices[index];

                choicesByRouteStepSymbol.Add(
                    choice.StageRouteStepDefinition.Symbol,
                    choice);
            }

            return choicesByRouteStepSymbol;
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