#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Recipes
{
    /// <summary>
    /// Defines an accepted resolved generation recipe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation recipe definition is a reusable resolved generation template. It selects stage routes and
    /// operation implementations using catalog-owned definition objects, not unresolved symbols.
    /// </para>
    /// <para>
    /// Recipe symbols are stable machine-facing catalog identity values. Display names are user-facing metadata
    /// only and must not be used for lookup, deterministic generation, catalog resolution, or artifact
    /// compatibility.
    /// </para>
    /// <para>
    /// A recipe validates that every selected route step has exactly one implementation choice, every
    /// implementation choice belongs to a selected route step, route operation contracts satisfy their stage
    /// contracts, and selected stages can be ordered by their stage contract dependencies.
    /// </para>
    /// <para>
    /// Route and stage dependency validation uses accepted semantic resource definitions, not raw resource
    /// symbols.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationRecipeDefinition"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationRecipeDefinition : IEquatable<GenerationRecipeDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRecipeDefinition"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing recipe symbol.</param>
        /// <param name="displayName">The user-facing recipe display name.</param>
        /// <param name="generationSchemaDefinition">The resolved generation schema definition.</param>
        /// <param name="stageRouteChoices">The selected stage routes.</param>
        /// <param name="stageRouteStepImplementationChoices">The selected route-step implementation choices.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the selected route and implementation choices cannot form a valid generation recipe.
        /// </exception>
        public GenerationRecipeDefinition(
            Symbol symbol,
            DisplayName displayName,
            GenerationSchemaDefinition generationSchemaDefinition,
            IEnumerable<StageRouteChoice> stageRouteChoices,
            IEnumerable<StageRouteStepImplementationChoice> stageRouteStepImplementationChoices)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (generationSchemaDefinition is null)
            {
                throw new ArgumentNullException(nameof(generationSchemaDefinition));
            }

            if (stageRouteChoices is null)
            {
                throw new ArgumentNullException(nameof(stageRouteChoices));
            }

            if (stageRouteStepImplementationChoices is null)
            {
                throw new ArgumentNullException(nameof(stageRouteStepImplementationChoices));
            }

            StageRouteChoice[] copiedStageRouteChoices = CopyStageRouteChoices(
                generationSchemaDefinition,
                stageRouteChoices);

            StageRouteStepImplementationChoice[] copiedStageRouteStepImplementationChoices =
                CopyStageRouteStepImplementationChoices(
                    generationSchemaDefinition,
                    copiedStageRouteChoices,
                    stageRouteStepImplementationChoices);

            ValidateRouteContractCompatibility(
                copiedStageRouteChoices,
                copiedStageRouteStepImplementationChoices);

            ValidateStageDependencySatisfiability(copiedStageRouteChoices);

            Symbol = symbol;
            DisplayName = displayName;
            GenerationSchemaDefinition = generationSchemaDefinition;
            StageRouteChoices = new ReadOnlyCollection<StageRouteChoice>(copiedStageRouteChoices);
            StageRouteStepImplementationChoices =
                new ReadOnlyCollection<StageRouteStepImplementationChoice>(
                    copiedStageRouteStepImplementationChoices);
        }

        /// <summary>
        /// Gets the stable machine-facing recipe symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing recipe display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <summary>
        /// Gets the resolved generation schema definition.
        /// </summary>
        public GenerationSchemaDefinition GenerationSchemaDefinition { get; }

        /// <summary>
        /// Gets the selected stage routes.
        /// </summary>
        public IReadOnlyList<StageRouteChoice> StageRouteChoices { get; }

        /// <summary>
        /// Gets the selected route-step implementation choices.
        /// </summary>
        public IReadOnlyList<StageRouteStepImplementationChoice> StageRouteStepImplementationChoices { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationRecipeDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationRecipeDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationRecipeDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(DisplayName)}: {DisplayName}, {nameof(GenerationSchemaDefinition)}: {GenerationSchemaDefinition.Symbol}, {nameof(StageRouteChoices)}: {StageRouteChoices.Count})";
        }

        /// <summary>
        /// Determines whether two generation recipe definitions are equal.
        /// </summary>
        public static bool operator ==(GenerationRecipeDefinition? left, GenerationRecipeDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation recipe definitions are not equal.
        /// </summary>
        public static bool operator !=(GenerationRecipeDefinition? left, GenerationRecipeDefinition? right)
        {
            return !Equals(left, right);
        }

        private static StageRouteChoice[] CopyStageRouteChoices(
            GenerationSchemaDefinition generationSchemaDefinition,
            IEnumerable<StageRouteChoice> stageRouteChoices)
        {
            var copiedChoices = new List<StageRouteChoice>();
            var selectedStageDefinitionSymbols = new HashSet<Symbol>();
            var selectedStageRouteDefinitionSymbols = new HashSet<Symbol>();

            foreach (StageRouteChoice? choice in stageRouteChoices)
            {
                if (choice is null)
                {
                    throw new ArgumentException(
                        "Stage route choices cannot contain null entries.",
                        nameof(stageRouteChoices));
                }

                if (!ReferenceEquals(choice.StageDefinition.GenerationSchema, generationSchemaDefinition))
                {
                    throw new ArgumentException(
                        $"Stage route choice for stage definition '{choice.StageDefinition.Symbol}' belongs to generation schema '{choice.StageDefinition.GenerationSchema.Symbol}', but the recipe belongs to generation schema '{generationSchemaDefinition.Symbol}'.",
                        nameof(stageRouteChoices));
                }

                if (!selectedStageDefinitionSymbols.Add(choice.StageDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage route choices cannot contain duplicate stage-definition symbol '{choice.StageDefinition.Symbol}'.",
                        nameof(stageRouteChoices));
                }

                if (!selectedStageRouteDefinitionSymbols.Add(choice.StageRouteDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage route choices cannot contain duplicate stage-route-definition symbol '{choice.StageRouteDefinition.Symbol}'.",
                        nameof(stageRouteChoices));
                }

                copiedChoices.Add(choice);
            }

            if (copiedChoices.Count == 0)
            {
                throw new ArgumentException(
                    "Generation recipe must contain at least one stage route choice.",
                    nameof(stageRouteChoices));
            }

            return copiedChoices.ToArray();
        }

        private static StageRouteStepImplementationChoice[] CopyStageRouteStepImplementationChoices(
            GenerationSchemaDefinition generationSchemaDefinition,
            IReadOnlyList<StageRouteChoice> stageRouteChoices,
            IEnumerable<StageRouteStepImplementationChoice> stageRouteStepImplementationChoices)
        {
            Dictionary<Symbol, StageRouteStepDefinition> selectedRouteStepsBySymbol =
                CreateSelectedRouteStepIndex(stageRouteChoices);

            var copiedChoices = new List<StageRouteStepImplementationChoice>();
            var selectedImplementationChoiceSymbols = new HashSet<Symbol>();

            foreach (StageRouteStepImplementationChoice? choice in stageRouteStepImplementationChoices)
            {
                if (choice is null)
                {
                    throw new ArgumentException(
                        "Stage route step implementation choices cannot contain null entries.",
                        nameof(stageRouteStepImplementationChoices));
                }

                if (!ReferenceEquals(choice.OperationDefinition.GenerationSchema, generationSchemaDefinition))
                {
                    throw new ArgumentException(
                        $"Stage route step implementation choice for route step '{choice.StageRouteStepDefinition.Symbol}' belongs to operation generation schema '{choice.OperationDefinition.GenerationSchema.Symbol}', but the recipe belongs to generation schema '{generationSchemaDefinition.Symbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }

                if (!selectedRouteStepsBySymbol.TryGetValue(
                        choice.StageRouteStepDefinition.Symbol,
                        out StageRouteStepDefinition selectedRouteStepDefinition)
                    || !ReferenceEquals(selectedRouteStepDefinition, choice.StageRouteStepDefinition))
                {
                    throw new ArgumentException(
                        $"Stage route step implementation choice references route step '{choice.StageRouteStepDefinition.Symbol}', but that route step is not part of the recipe's selected stage routes.",
                        nameof(stageRouteStepImplementationChoices));
                }

                if (!selectedImplementationChoiceSymbols.Add(choice.StageRouteStepDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage route step implementation choices cannot contain duplicate route-step symbol '{choice.StageRouteStepDefinition.Symbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }

                copiedChoices.Add(choice);
            }

            foreach (Symbol selectedRouteStepSymbol in selectedRouteStepsBySymbol.Keys)
            {
                if (!selectedImplementationChoiceSymbols.Contains(selectedRouteStepSymbol))
                {
                    throw new ArgumentException(
                        $"Generation recipe must contain an implementation choice for route step '{selectedRouteStepSymbol}'.",
                        nameof(stageRouteStepImplementationChoices));
                }
            }

            return copiedChoices.ToArray();
        }

        private static Dictionary<Symbol, StageRouteStepDefinition> CreateSelectedRouteStepIndex(
            IReadOnlyList<StageRouteChoice> stageRouteChoices)
        {
            var selectedRouteStepsBySymbol = new Dictionary<Symbol, StageRouteStepDefinition>();

            for (int stageIndex = 0; stageIndex < stageRouteChoices.Count; stageIndex++)
            {
                StageRouteChoice stageRouteChoice = stageRouteChoices[stageIndex];

                for (int stepIndex = 0;
                    stepIndex < stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions.Count;
                    stepIndex++)
                {
                    StageRouteStepDefinition routeStepDefinition =
                        stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions[stepIndex];

                    if (selectedRouteStepsBySymbol.ContainsKey(routeStepDefinition.Symbol))
                    {
                        throw new ArgumentException(
                            $"Generation recipe selected stage routes cannot contain duplicate route-step symbol '{routeStepDefinition.Symbol}'.",
                            nameof(stageRouteChoices));
                    }

                    selectedRouteStepsBySymbol.Add(routeStepDefinition.Symbol, routeStepDefinition);
                }
            }

            return selectedRouteStepsBySymbol;
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

        private static void ValidateStageDependencySatisfiability(
            IReadOnlyList<StageRouteChoice> stageRouteChoices)
        {
            var availableResources = new HashSet<ResourceDefinition>();
            var remainingStageRouteChoices = new List<StageRouteChoice>();

            for (int index = 0; index < stageRouteChoices.Count; index++)
            {
                remainingStageRouteChoices.Add(stageRouteChoices[index]);
            }

            while (remainingStageRouteChoices.Count != 0)
            {
                bool madeProgress = false;

                for (int index = 0; index < remainingStageRouteChoices.Count;)
                {
                    StageRouteChoice stageRouteChoice = remainingStageRouteChoices[index];

                    if (!AreRequiredInputsAvailable(stageRouteChoice.StageContract, availableResources))
                    {
                        index++;
                        continue;
                    }

                    AddProducedOutputs(stageRouteChoice.StageContract, availableResources);
                    remainingStageRouteChoices.RemoveAt(index);
                    madeProgress = true;
                }

                if (!madeProgress)
                {
                    StageRouteChoice blockedChoice = remainingStageRouteChoices[0];
                    ResourceDefinition missingInput = GetFirstMissingInput(
                        blockedChoice.StageContract,
                        availableResources);

                    throw new ArgumentException(
                        $"Generation recipe cannot satisfy stage dependency for stage definition '{blockedChoice.StageDefinition.Symbol}'. Required input resource '{missingInput.Symbol}' is not produced by any satisfiable selected stage.",
                        nameof(stageRouteChoices));
                }
            }
        }

        private static bool AreRequiredInputsAvailable(
            StageContract stageContract,
            HashSet<ResourceDefinition> availableResources)
        {
            for (int index = 0; index < stageContract.RequiredInputs.Count; index++)
            {
                if (!availableResources.Contains(stageContract.RequiredInputs[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddProducedOutputs(
            StageContract stageContract,
            HashSet<ResourceDefinition> availableResources)
        {
            for (int index = 0; index < stageContract.ProducedOutputs.Count; index++)
            {
                availableResources.Add(stageContract.ProducedOutputs[index]);
            }
        }

        private static ResourceDefinition GetFirstMissingInput(
            StageContract stageContract,
            HashSet<ResourceDefinition> availableResources)
        {
            for (int index = 0; index < stageContract.RequiredInputs.Count; index++)
            {
                ResourceDefinition requiredInput = stageContract.RequiredInputs[index];

                if (!availableResources.Contains(requiredInput))
                {
                    return requiredInput;
                }
            }

            throw new InvalidOperationException(
                "Stage contract does not contain a missing required input resource.");
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
    }
}