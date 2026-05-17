#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Recipes;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Resolves symbolic generation request descriptors into accepted generation requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation request resolver uses an accepted generation catalog to resolve descriptor symbols into
    /// catalog-owned recipe and implementation definitions. It produces an accepted generation request when the
    /// descriptor can be satisfied by the catalog.
    /// </para>
    /// <para>
    /// The resolver is the boundary between unresolved symbolic input and accepted planning domain objects. It
    /// does not compile generation plans, create executable bindings, schedule jobs, allocate native containers,
    /// or reference Unity runtime objects.
    /// </para>
    /// <para>
    /// Invalid API usage throws exceptions. Descriptor/catalog satisfiability failures are returned as structured
    /// resolution errors.
    /// </para>
    /// </remarks>
    public sealed class GenerationRequestResolver
    {
        private static readonly Symbol GenerationRecipeDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.recipe_not_found");

        private static readonly Symbol StageRouteStepDefinitionNotSelectedByRecipeCode =
            Symbol.Create("lokrain.atlas.planning.route_step_not_selected_by_recipe");

        private static readonly Symbol OperationImplementationDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.implementation_not_found");

        private static readonly Symbol OperationImplementationDefinitionMismatchCode =
            Symbol.Create("lokrain.atlas.planning.implementation_operation_mismatch");

        /// <summary>
        /// Resolves the specified symbolic generation request descriptor into an accepted generation request.
        /// </summary>
        /// <param name="catalog">The accepted generation catalog.</param>
        /// <param name="descriptor">The symbolic generation request descriptor.</param>
        /// <returns>The generation request resolution result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="catalog"/> or <paramref name="descriptor"/> is null.
        /// </exception>
        public GenerationRequestResolutionResult Resolve(
            GenerationCatalog catalog,
            GenerationRequestDescriptor descriptor)
        {
            if (catalog is null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var errors = new List<GenerationRequestResolutionError>();

            if (!catalog.TryGetGenerationRecipeDefinition(
                descriptor.GenerationRecipeDefinitionSymbol,
                out GenerationRecipeDefinition? generationRecipeDefinition))
            {
                AddError(
                    errors,
                    GenerationRecipeDefinitionNotFoundCode,
                    $"Generation recipe definition '{descriptor.GenerationRecipeDefinitionSymbol}' was not found in the generation catalog.",
                    descriptor.GenerationRecipeDefinitionSymbol);

                return GenerationRequestResolutionResult.Failure(errors);
            }

            Dictionary<Symbol, StageRouteStepImplementationChoice> defaultChoicesByRouteStepSymbol =
                CreateImplementationChoiceIndex(generationRecipeDefinition!.StageRouteStepImplementationChoices);

            Dictionary<Symbol, StageRouteStepImplementationChoice> finalChoicesByRouteStepSymbol =
                CreateImplementationChoiceIndex(generationRecipeDefinition.StageRouteStepImplementationChoices);

            ApplyOperationImplementationOverrides(
                catalog,
                descriptor,
                defaultChoicesByRouteStepSymbol,
                finalChoicesByRouteStepSymbol,
                errors);

            if (errors.Count != 0)
            {
                return GenerationRequestResolutionResult.Failure(errors);
            }

            StageRouteStepImplementationChoice[] finalChoices =
                CopyFinalChoicesInRecipeOrder(
                    generationRecipeDefinition.StageRouteStepImplementationChoices,
                    finalChoicesByRouteStepSymbol);

            var generationRequest = new GenerationRequest(
                generationRecipeDefinition,
                descriptor.RunSettings,
                finalChoices);

            return GenerationRequestResolutionResult.Success(generationRequest);
        }

        private static void ApplyOperationImplementationOverrides(
            GenerationCatalog catalog,
            GenerationRequestDescriptor descriptor,
            Dictionary<Symbol, StageRouteStepImplementationChoice> defaultChoicesByRouteStepSymbol,
            Dictionary<Symbol, StageRouteStepImplementationChoice> finalChoicesByRouteStepSymbol,
            List<GenerationRequestResolutionError> errors)
        {
            for (int index = 0; index < descriptor.OperationImplementationOverrides.Count; index++)
            {
                OperationImplementationOverrideDescriptor operationImplementationOverride =
                    descriptor.OperationImplementationOverrides[index];

                if (!defaultChoicesByRouteStepSymbol.TryGetValue(
                    operationImplementationOverride.StageRouteStepDefinitionSymbol,
                    out StageRouteStepImplementationChoice defaultChoice))
                {
                    AddError(
                        errors,
                        StageRouteStepDefinitionNotSelectedByRecipeCode,
                        $"Stage route step definition '{operationImplementationOverride.StageRouteStepDefinitionSymbol}' is not selected by generation recipe '{descriptor.GenerationRecipeDefinitionSymbol}'.",
                        operationImplementationOverride.StageRouteStepDefinitionSymbol);

                    continue;
                }

                if (!catalog.TryGetOperationImplementationDefinition(
                    operationImplementationOverride.OperationImplementationDefinitionSymbol,
                    out OperationImplementationDefinition? operationImplementationDefinition))
                {
                    AddError(
                        errors,
                        OperationImplementationDefinitionNotFoundCode,
                        $"Operation implementation definition '{operationImplementationOverride.OperationImplementationDefinitionSymbol}' was not found in the generation catalog.",
                        operationImplementationOverride.OperationImplementationDefinitionSymbol);

                    continue;
                }

                if (!ReferenceEquals(
                    operationImplementationDefinition!.OperationDefinition,
                    defaultChoice.OperationDefinition))
                {
                    AddError(
                        errors,
                        OperationImplementationDefinitionMismatchCode,
                        $"Operation implementation definition '{operationImplementationDefinition.Symbol}' belongs to operation definition '{operationImplementationDefinition.OperationDefinition.Symbol}', but route step '{defaultChoice.StageRouteStepDefinition.Symbol}' requires operation definition '{defaultChoice.OperationDefinition.Symbol}'.",
                        operationImplementationDefinition.Symbol);

                    continue;
                }

                finalChoicesByRouteStepSymbol[defaultChoice.StageRouteStepDefinition.Symbol] =
                    new StageRouteStepImplementationChoice(
                        defaultChoice.StageRouteStepDefinition,
                        defaultChoice.OperationDefinition,
                        defaultChoice.OperationContract,
                        operationImplementationDefinition);
            }
        }

        private static Dictionary<Symbol, StageRouteStepImplementationChoice> CreateImplementationChoiceIndex(
            IReadOnlyList<StageRouteStepImplementationChoice> implementationChoices)
        {
            var choicesByRouteStepSymbol = new Dictionary<Symbol, StageRouteStepImplementationChoice>();

            for (int index = 0; index < implementationChoices.Count; index++)
            {
                StageRouteStepImplementationChoice implementationChoice = implementationChoices[index];

                choicesByRouteStepSymbol.Add(
                    implementationChoice.StageRouteStepDefinition.Symbol,
                    implementationChoice);
            }

            return choicesByRouteStepSymbol;
        }

        private static StageRouteStepImplementationChoice[] CopyFinalChoicesInRecipeOrder(
            IReadOnlyList<StageRouteStepImplementationChoice> recipeChoices,
            Dictionary<Symbol, StageRouteStepImplementationChoice> finalChoicesByRouteStepSymbol)
        {
            var finalChoices = new StageRouteStepImplementationChoice[recipeChoices.Count];

            for (int index = 0; index < recipeChoices.Count; index++)
            {
                Symbol routeStepSymbol = recipeChoices[index].StageRouteStepDefinition.Symbol;
                finalChoices[index] = finalChoicesByRouteStepSymbol[routeStepSymbol];
            }

            return finalChoices;
        }

        private static void AddError(
            List<GenerationRequestResolutionError> errors,
            Symbol code,
            string message,
            Symbol subjectSymbol)
        {
            errors.Add(new(code, message, subjectSymbol));
        }
    }
}