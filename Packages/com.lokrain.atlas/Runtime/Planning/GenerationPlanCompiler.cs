#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Catalog;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Compiles unresolved generation requests into resolved managed generation plans.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generation plan compiler resolves request-side symbols through an accepted generation catalog. It
    /// produces managed planning nodes only; it does not create executable bindings, runtime state, job data,
    /// native containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Stage route selections are resolved first, then ordered by stage contract dependencies. Request insertion
    /// order is used as a deterministic tie-breaker when multiple selected stages are ready.
    /// </para>
    /// <para>
    /// Invalid API usage throws exceptions. Catalog-dependent request failures are returned as structured compiler
    /// errors through <see cref="GenerationPlanCompilerResult"/>.
    /// </para>
    /// <para>
    /// Runnable-plan compilation later resolves execution-ready metadata and runtime bindings.
    /// </para>
    /// </remarks>
    public sealed class GenerationPlanCompiler
    {
        private static readonly Symbol GenerationSchemaNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.schema_not_found");

        private static readonly Symbol StageDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.stage_not_found");

        private static readonly Symbol StageDefinitionSchemaMismatchCode =
            Symbol.Create("lokrain.atlas.planning.stage_schema_mismatch");

        private static readonly Symbol StageRouteDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.route_not_found");

        private static readonly Symbol StageRouteDefinitionStageMismatchCode =
            Symbol.Create("lokrain.atlas.planning.route_stage_mismatch");

        private static readonly Symbol StageRouteDefinitionSchemaMismatchCode =
            Symbol.Create("lokrain.atlas.planning.route_schema_mismatch");

        private static readonly Symbol StageRouteStepDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.route_step_not_found");

        private static readonly Symbol StageRouteStepDefinitionNotSelectedCode =
            Symbol.Create("lokrain.atlas.planning.route_step_not_selected");

        private static readonly Symbol OperationDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.operation_not_found");

        private static readonly Symbol OperationImplementationDefinitionNotFoundCode =
            Symbol.Create("lokrain.atlas.planning.implementation_not_found");

        private static readonly Symbol OperationImplementationDefinitionMismatchCode =
            Symbol.Create("lokrain.atlas.planning.implementation_operation_mismatch");

        private static readonly Symbol OperationImplementationSelectionMissingCode =
            Symbol.Create("lokrain.atlas.planning.implementation_selection_missing");

        private static readonly Symbol StageRequiredInputMissingCode =
            Symbol.Create("lokrain.atlas.planning.stage_required_input_missing");

        /// <summary>
        /// Compiles the specified generation request into a resolved managed generation plan.
        /// </summary>
        /// <param name="catalog">The accepted generation catalog.</param>
        /// <param name="request">The unresolved generation request.</param>
        /// <returns>The generation plan compiler result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="catalog"/> or <paramref name="request"/> is null.
        /// </exception>
        public GenerationPlanCompilerResult Compile(
            GenerationCatalog catalog,
            GenerationRequest request)
        {
            if (catalog is null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var errors = new List<GenerationPlanCompilerError>();

            if (!catalog.TryGetGenerationSchemaDefinition(
                request.GenerationSchemaDefinitionSymbol,
                out GenerationSchemaDefinition? generationSchemaDefinition))
            {
                AddError(
                    errors,
                    GenerationSchemaNotFoundCode,
                    $"Generation schema definition '{request.GenerationSchemaDefinitionSymbol}' was not found in the generation catalog.",
                    request.GenerationSchemaDefinitionSymbol);

                return GenerationPlanCompilerResult.Failure(errors);
            }

            List<ResolvedStageRoute> resolvedStageRoutes = ResolveStageRoutes(
                catalog,
                request,
                generationSchemaDefinition!,
                errors);

            Dictionary<Symbol, StageRouteStepDefinition> selectedStageRouteStepDefinitionsBySymbol =
                GetSelectedStageRouteStepDefinitionsBySymbol(resolvedStageRoutes);

            Dictionary<Symbol, OperationImplementationDefinition> operationImplementationDefinitionsByRouteStepSymbol =
                ResolveOperationImplementationSelections(
                    catalog,
                    request,
                    selectedStageRouteStepDefinitionsBySymbol,
                    errors);

            AddMissingOperationImplementationSelectionErrors(
                selectedStageRouteStepDefinitionsBySymbol,
                operationImplementationDefinitionsByRouteStepSymbol,
                errors);

            List<ResolvedStageRoute> orderedStageRoutes = OrderStageRoutesByDependencies(
                catalog,
                resolvedStageRoutes,
                errors);

            if (errors.Count != 0)
            {
                return GenerationPlanCompilerResult.Failure(errors);
            }

            GenerationPlan generationPlan = CreateGenerationPlan(
                catalog,
                request,
                generationSchemaDefinition!,
                orderedStageRoutes,
                operationImplementationDefinitionsByRouteStepSymbol);

            return GenerationPlanCompilerResult.Success(generationPlan);
        }

        private static List<ResolvedStageRoute> ResolveStageRoutes(
            GenerationCatalog catalog,
            GenerationRequest request,
            GenerationSchemaDefinition generationSchemaDefinition,
            List<GenerationPlanCompilerError> errors)
        {
            var resolvedStageRoutes = new List<ResolvedStageRoute>();

            for (int index = 0; index < request.StageRouteSelections.Count; index++)
            {
                StageRouteSelection selection = request.StageRouteSelections[index];

                bool hasSelectionError = false;

                if (!catalog.TryGetStageDefinition(
                    selection.StageDefinitionSymbol,
                    out StageDefinition? stageDefinition))
                {
                    AddError(
                        errors,
                        StageDefinitionNotFoundCode,
                        $"Stage definition '{selection.StageDefinitionSymbol}' was not found in the generation catalog.",
                        selection.StageDefinitionSymbol);

                    hasSelectionError = true;
                }
                else if (!ReferenceEquals(stageDefinition!.GenerationSchema, generationSchemaDefinition))
                {
                    AddError(
                        errors,
                        StageDefinitionSchemaMismatchCode,
                        $"Stage definition '{stageDefinition.Symbol}' belongs to generation schema '{stageDefinition.GenerationSchema.Symbol}', but the request selected generation schema '{generationSchemaDefinition.Symbol}'.",
                        stageDefinition.Symbol);

                    hasSelectionError = true;
                }

                if (!catalog.TryGetStageRouteDefinition(
                    selection.StageRouteDefinitionSymbol,
                    out StageRouteDefinition? stageRouteDefinition))
                {
                    AddError(
                        errors,
                        StageRouteDefinitionNotFoundCode,
                        $"Stage route definition '{selection.StageRouteDefinitionSymbol}' was not found in the generation catalog.",
                        selection.StageRouteDefinitionSymbol);

                    hasSelectionError = true;
                }
                else
                {
                    if (stageDefinition is not null
                        && !ReferenceEquals(stageRouteDefinition!.StageDefinition, stageDefinition))
                    {
                        AddError(
                            errors,
                            StageRouteDefinitionStageMismatchCode,
                            $"Stage route definition '{stageRouteDefinition.Symbol}' belongs to stage definition '{stageRouteDefinition.StageDefinition.Symbol}', but the request selected stage definition '{stageDefinition.Symbol}'.",
                            stageRouteDefinition.Symbol);

                        hasSelectionError = true;
                    }

                    if (!ReferenceEquals(stageRouteDefinition!.StageDefinition.GenerationSchema, generationSchemaDefinition))
                    {
                        AddError(
                            errors,
                            StageRouteDefinitionSchemaMismatchCode,
                            $"Stage route definition '{stageRouteDefinition.Symbol}' belongs to generation schema '{stageRouteDefinition.StageDefinition.GenerationSchema.Symbol}', but the request selected generation schema '{generationSchemaDefinition.Symbol}'.",
                            stageRouteDefinition.Symbol);

                        hasSelectionError = true;
                    }
                }

                if (!hasSelectionError && stageDefinition is not null && stageRouteDefinition is not null)
                {
                    resolvedStageRoutes.Add(new(stageDefinition, stageRouteDefinition));
                }
            }

            return resolvedStageRoutes;
        }

        private static Dictionary<Symbol, StageRouteStepDefinition> GetSelectedStageRouteStepDefinitionsBySymbol(
            IEnumerable<ResolvedStageRoute> resolvedStageRoutes)
        {
            var selectedStageRouteStepDefinitionsBySymbol = new Dictionary<Symbol, StageRouteStepDefinition>();

            foreach (ResolvedStageRoute resolvedStageRoute in resolvedStageRoutes)
            {
                for (int index = 0; index < resolvedStageRoute.StageRouteDefinition.StageRouteStepDefinitions.Count; index++)
                {
                    StageRouteStepDefinition stageRouteStepDefinition =
                        resolvedStageRoute.StageRouteDefinition.StageRouteStepDefinitions[index];

                    selectedStageRouteStepDefinitionsBySymbol.Add(
                        stageRouteStepDefinition.Symbol,
                        stageRouteStepDefinition);
                }
            }

            return selectedStageRouteStepDefinitionsBySymbol;
        }

        private static Dictionary<Symbol, OperationImplementationDefinition> ResolveOperationImplementationSelections(
            GenerationCatalog catalog,
            GenerationRequest request,
            Dictionary<Symbol, StageRouteStepDefinition> selectedStageRouteStepDefinitionsBySymbol,
            List<GenerationPlanCompilerError> errors)
        {
            var operationImplementationDefinitionsByRouteStepSymbol =
                new Dictionary<Symbol, OperationImplementationDefinition>();

            for (int index = 0; index < request.OperationImplementationSelections.Count; index++)
            {
                OperationImplementationSelection selection = request.OperationImplementationSelections[index];

                if (!catalog.TryGetStageRouteStepDefinition(
                    selection.StageRouteStepDefinitionSymbol,
                    out StageRouteStepDefinition? catalogStageRouteStepDefinition))
                {
                    AddError(
                        errors,
                        StageRouteStepDefinitionNotFoundCode,
                        $"Stage route step definition '{selection.StageRouteStepDefinitionSymbol}' was not found in the generation catalog.",
                        selection.StageRouteStepDefinitionSymbol);

                    continue;
                }

                if (!selectedStageRouteStepDefinitionsBySymbol.TryGetValue(
                    selection.StageRouteStepDefinitionSymbol,
                    out StageRouteStepDefinition? selectedStageRouteStepDefinition)
                    || !ReferenceEquals(selectedStageRouteStepDefinition, catalogStageRouteStepDefinition))
                {
                    AddError(
                        errors,
                        StageRouteStepDefinitionNotSelectedCode,
                        $"Stage route step definition '{selection.StageRouteStepDefinitionSymbol}' is not part of any selected stage route.",
                        selection.StageRouteStepDefinitionSymbol);

                    continue;
                }

                if (!catalog.TryGetOperationImplementationDefinition(
                    selection.OperationImplementationDefinitionSymbol,
                    out OperationImplementationDefinition? operationImplementationDefinition))
                {
                    AddError(
                        errors,
                        OperationImplementationDefinitionNotFoundCode,
                        $"Operation implementation definition '{selection.OperationImplementationDefinitionSymbol}' was not found in the generation catalog.",
                        selection.OperationImplementationDefinitionSymbol);

                    continue;
                }

                if (!catalog.TryGetOperationDefinition(
                    selectedStageRouteStepDefinition.OperationDefinitionSymbol,
                    out OperationDefinition? operationDefinition))
                {
                    AddError(
                        errors,
                        OperationDefinitionNotFoundCode,
                        $"Operation definition '{selectedStageRouteStepDefinition.OperationDefinitionSymbol}' referenced by route step '{selectedStageRouteStepDefinition.Symbol}' was not found in the generation catalog.",
                        selectedStageRouteStepDefinition.OperationDefinitionSymbol);

                    continue;
                }

                if (!ReferenceEquals(operationImplementationDefinition!.OperationDefinition, operationDefinition))
                {
                    AddError(
                        errors,
                        OperationImplementationDefinitionMismatchCode,
                        $"Operation implementation definition '{operationImplementationDefinition.Symbol}' belongs to operation definition '{operationImplementationDefinition.OperationDefinition.Symbol}', but route step '{selectedStageRouteStepDefinition.Symbol}' requires operation definition '{operationDefinition!.Symbol}'.",
                        operationImplementationDefinition.Symbol);

                    continue;
                }

                operationImplementationDefinitionsByRouteStepSymbol.Add(
                    selectedStageRouteStepDefinition.Symbol,
                    operationImplementationDefinition);
            }

            return operationImplementationDefinitionsByRouteStepSymbol;
        }

        private static void AddMissingOperationImplementationSelectionErrors(
            Dictionary<Symbol, StageRouteStepDefinition> selectedStageRouteStepDefinitionsBySymbol,
            Dictionary<Symbol, OperationImplementationDefinition> operationImplementationDefinitionsByRouteStepSymbol,
            List<GenerationPlanCompilerError> errors)
        {
            foreach (StageRouteStepDefinition stageRouteStepDefinition in selectedStageRouteStepDefinitionsBySymbol.Values)
            {
                if (!operationImplementationDefinitionsByRouteStepSymbol.ContainsKey(stageRouteStepDefinition.Symbol))
                {
                    AddError(
                        errors,
                        OperationImplementationSelectionMissingCode,
                        $"Stage route step definition '{stageRouteStepDefinition.Symbol}' requires an operation implementation selection.",
                        stageRouteStepDefinition.Symbol);
                }
            }
        }

        private static List<ResolvedStageRoute> OrderStageRoutesByDependencies(
            GenerationCatalog catalog,
            IEnumerable<ResolvedStageRoute> resolvedStageRoutes,
            List<GenerationPlanCompilerError> errors)
        {
            var remainingStageRoutes = new List<ResolvedStageRoute>(resolvedStageRoutes);
            var orderedStageRoutes = new List<ResolvedStageRoute>();
            var availableSymbols = new HashSet<Symbol>();

            while (remainingStageRoutes.Count != 0)
            {
                bool madeProgress = false;

                for (int index = 0; index < remainingStageRoutes.Count;)
                {
                    ResolvedStageRoute resolvedStageRoute = remainingStageRoutes[index];
                    StageContract stageContract = catalog.GetStageContract(resolvedStageRoute.StageDefinition.Symbol);

                    if (!AreRequiredInputsAvailable(stageContract, availableSymbols))
                    {
                        index++;
                        continue;
                    }

                    orderedStageRoutes.Add(resolvedStageRoute);
                    AddProducedOutputs(stageContract, availableSymbols);
                    remainingStageRoutes.RemoveAt(index);
                    madeProgress = true;
                }

                if (!madeProgress)
                {
                    AddStageDependencyErrors(
                        catalog,
                        remainingStageRoutes,
                        availableSymbols,
                        errors);

                    break;
                }
            }

            return orderedStageRoutes;
        }

        private static bool AreRequiredInputsAvailable(
            StageContract stageContract,
            HashSet<Symbol> availableSymbols)
        {
            for (int index = 0; index < stageContract.RequiredInputSymbols.Count; index++)
            {
                if (!availableSymbols.Contains(stageContract.RequiredInputSymbols[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddProducedOutputs(
            StageContract stageContract,
            HashSet<Symbol> availableSymbols)
        {
            for (int index = 0; index < stageContract.ProducedOutputSymbols.Count; index++)
            {
                availableSymbols.Add(stageContract.ProducedOutputSymbols[index]);
            }
        }

        private static void AddStageDependencyErrors(
            GenerationCatalog catalog,
            IEnumerable<ResolvedStageRoute> remainingStageRoutes,
            HashSet<Symbol> availableSymbols,
            List<GenerationPlanCompilerError> errors)
        {
            foreach (ResolvedStageRoute resolvedStageRoute in remainingStageRoutes)
            {
                StageContract stageContract = catalog.GetStageContract(resolvedStageRoute.StageDefinition.Symbol);

                for (int index = 0; index < stageContract.RequiredInputSymbols.Count; index++)
                {
                    Symbol requiredInputSymbol = stageContract.RequiredInputSymbols[index];

                    if (!availableSymbols.Contains(requiredInputSymbol))
                    {
                        AddError(
                            errors,
                            StageRequiredInputMissingCode,
                            $"Stage definition '{resolvedStageRoute.StageDefinition.Symbol}' requires input symbol '{requiredInputSymbol}', but that symbol is not available from any previously planned selected stage output.",
                            requiredInputSymbol);
                    }
                }
            }
        }

        private static GenerationPlan CreateGenerationPlan(
            GenerationCatalog catalog,
            GenerationRequest request,
            GenerationSchemaDefinition generationSchemaDefinition,
            IEnumerable<ResolvedStageRoute> orderedStageRoutes,
            Dictionary<Symbol, OperationImplementationDefinition> operationImplementationDefinitionsByRouteStepSymbol)
        {
            var stagePlanNodes = new List<StagePlanNode>();

            foreach (ResolvedStageRoute resolvedStageRoute in orderedStageRoutes)
            {
                StageContract stageContract = catalog.GetStageContract(
                    resolvedStageRoute.StageDefinition.Symbol);

                var operationPlanNodes = new List<OperationPlanNode>();

                for (int index = 0; index < resolvedStageRoute.StageRouteDefinition.StageRouteStepDefinitions.Count; index++)
                {
                    StageRouteStepDefinition stageRouteStepDefinition =
                        resolvedStageRoute.StageRouteDefinition.StageRouteStepDefinitions[index];

                    OperationDefinition operationDefinition = catalog.GetOperationDefinition(
                        stageRouteStepDefinition.OperationDefinitionSymbol);

                    OperationContract operationContract = catalog.GetOperationContract(
                        operationDefinition.Symbol);

                    OperationImplementationDefinition operationImplementationDefinition =
                        operationImplementationDefinitionsByRouteStepSymbol[stageRouteStepDefinition.Symbol];

                    operationPlanNodes.Add(new(
                        stageRouteStepDefinition,
                        operationDefinition,
                        operationImplementationDefinition,
                        operationContract));
                }

                stagePlanNodes.Add(new(
                    resolvedStageRoute.StageDefinition,
                    resolvedStageRoute.StageRouteDefinition,
                    stageContract,
                    operationPlanNodes));
            }

            return new(
                generationSchemaDefinition,
                request.Grid,
                request.Seed,
                stagePlanNodes);
        }

        private static void AddError(
            List<GenerationPlanCompilerError> errors,
            Symbol code,
            string message,
            Symbol subjectSymbol)
        {
            errors.Add(new(code, message, subjectSymbol));
        }

        private sealed class ResolvedStageRoute
        {
            public ResolvedStageRoute(
                StageDefinition stageDefinition,
                StageRouteDefinition stageRouteDefinition)
            {
                StageDefinition = stageDefinition;
                StageRouteDefinition = stageRouteDefinition;
            }

            public StageDefinition StageDefinition { get; }

            public StageRouteDefinition StageRouteDefinition { get; }
        }
    }
}