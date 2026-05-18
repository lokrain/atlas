#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Recipes;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Compiles accepted generation requests into resolved managed generation plans.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generation plan compiler transforms an accepted generation request into a managed generation plan.
    /// It does not resolve raw symbols, validate external descriptors, create executable bindings, runtime state,
    /// job data, native containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Request acceptance, recipe construction, implementation override resolution, and catalog/descriptor
    /// resolution happen before this compiler is invoked. The compiler receives already accepted domain objects
    /// and produces an accepted managed plan.
    /// </para>
    /// <para>
    /// Stage route choices are ordered by stage contract dependencies. Recipe order is used as a deterministic
    /// tie-breaker when multiple selected stages are ready.
    /// </para>
    /// <para>
    /// Stage dependencies are evaluated through accepted semantic resource definitions, not raw resource symbols.
    /// Runnable-plan compilation later resolves execution-ready metadata and runtime bindings.
    /// </para>
    /// </remarks>
    public sealed class GenerationPlanCompiler
    {
        /// <summary>
        /// Compiles the specified accepted generation request into a resolved managed generation plan.
        /// </summary>
        /// <param name="request">The accepted generation request.</param>
        /// <returns>The resolved managed generation plan.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an accepted request violates a compiler-required invariant.
        /// </exception>
        public GenerationPlan Compile(GenerationRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IReadOnlyList<StageRouteChoice> orderedStageRouteChoices =
                OrderStageRouteChoicesByDependencies(request.GenerationRecipeDefinition.StageRouteChoices);

            Dictionary<Symbol, StageRouteStepImplementationChoice> implementationChoicesByRouteStepSymbol =
                CreateImplementationChoiceIndex(request.StageRouteStepImplementationChoices);

            var stagePlanNodes = new List<StagePlanNode>();

            for (int stageIndex = 0; stageIndex < orderedStageRouteChoices.Count; stageIndex++)
            {
                StageRouteChoice stageRouteChoice = orderedStageRouteChoices[stageIndex];

                var operationPlanNodes = new List<OperationPlanNode>();

                for (int stepIndex = 0;
                    stepIndex < stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions.Count;
                    stepIndex++)
                {
                    StageRouteStepDefinition stageRouteStepDefinition =
                        stageRouteChoice.StageRouteDefinition.StageRouteStepDefinitions[stepIndex];

                    StageRouteStepImplementationChoice implementationChoice =
                        GetImplementationChoice(
                            implementationChoicesByRouteStepSymbol,
                            stageRouteStepDefinition);

                    operationPlanNodes.Add(new OperationPlanNode(
                        implementationChoice.StageRouteStepDefinition,
                        implementationChoice.OperationDefinition,
                        implementationChoice.OperationImplementationDefinition,
                        implementationChoice.OperationContract));
                }

                stagePlanNodes.Add(new StagePlanNode(
                    stageRouteChoice.StageDefinition,
                    stageRouteChoice.StageRouteDefinition,
                    stageRouteChoice.StageContract,
                    operationPlanNodes));
            }

            return new GenerationPlan(
                request.GenerationRecipeDefinition,
                request.RunSettings,
                stagePlanNodes);
        }

        private static IReadOnlyList<StageRouteChoice> OrderStageRouteChoicesByDependencies(
            IReadOnlyList<StageRouteChoice> stageRouteChoices)
        {
            var remainingStageRouteChoices = new List<StageRouteChoice>();

            for (int index = 0; index < stageRouteChoices.Count; index++)
            {
                remainingStageRouteChoices.Add(stageRouteChoices[index]);
            }

            var orderedStageRouteChoices = new List<StageRouteChoice>();
            var availableResources = new HashSet<ResourceDefinition>();

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

                    orderedStageRouteChoices.Add(stageRouteChoice);
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

                    throw new InvalidOperationException(
                        $"Accepted generation recipe contains unsatisfied stage dependency for stage definition '{blockedChoice.StageDefinition.Symbol}'. Required input resource '{missingInput.Symbol}' is not available.");
                }
            }

            return orderedStageRouteChoices;
        }

        private static Dictionary<Symbol, StageRouteStepImplementationChoice> CreateImplementationChoiceIndex(
            IReadOnlyList<StageRouteStepImplementationChoice> implementationChoices)
        {
            var implementationChoicesByRouteStepSymbol =
                new Dictionary<Symbol, StageRouteStepImplementationChoice>();

            for (int index = 0; index < implementationChoices.Count; index++)
            {
                StageRouteStepImplementationChoice implementationChoice = implementationChoices[index];

                implementationChoicesByRouteStepSymbol.Add(
                    implementationChoice.StageRouteStepDefinition.Symbol,
                    implementationChoice);
            }

            return implementationChoicesByRouteStepSymbol;
        }

        private static StageRouteStepImplementationChoice GetImplementationChoice(
            IReadOnlyDictionary<Symbol, StageRouteStepImplementationChoice> implementationChoicesByRouteStepSymbol,
            StageRouteStepDefinition stageRouteStepDefinition)
        {
            if (implementationChoicesByRouteStepSymbol.TryGetValue(
                stageRouteStepDefinition.Symbol,
                out StageRouteStepImplementationChoice implementationChoice))
            {
                return implementationChoice;
            }

            throw new InvalidOperationException(
                $"Accepted generation request does not contain an implementation choice for route step '{stageRouteStepDefinition.Symbol}'.");
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
    }
}