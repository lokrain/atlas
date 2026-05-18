#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Planning;
using Lokrain.Atlas.Resources;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Compiles managed semantic generation plans into managed runnable-plan metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler consumes a <see cref="GenerationPlan"/>, a <see cref="FieldDefinitionSet"/>, and an
    /// <see cref="ExecutionProfile"/>. It creates deterministic field, stage, and operation tables without
    /// allocating native storage, scheduling jobs, binding ECS data, capturing artifacts, or capturing runtime
    /// diagnostics.
    /// </para>
    /// <para>
    /// The compiler is stateless. Consistency failures are returned as deterministic structured compilation
    /// errors from <see cref="Compile"/>. <see cref="CompileOrThrow"/> throws when compilation fails.
    /// </para>
    /// </remarks>
    public sealed class RunnablePlanCompiler
    {
        /// <summary>
        /// Compiles the specified generation plan into managed runnable-plan metadata.
        /// </summary>
        /// <param name="generationPlan">The source managed semantic generation plan.</param>
        /// <param name="fieldDefinitionSet">The accepted managed field definition set.</param>
        /// <param name="executionProfile">The selected managed execution profile.</param>
        /// <returns>The runnable-plan compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is <see langword="null"/>.
        /// </exception>
        public RunnablePlanCompilationResult Compile(
            GenerationPlan generationPlan,
            FieldDefinitionSet fieldDefinitionSet,
            ExecutionProfile executionProfile)
        {
            if (generationPlan is null)
            {
                throw new ArgumentNullException(nameof(generationPlan));
            }

            if (fieldDefinitionSet is null)
            {
                throw new ArgumentNullException(nameof(fieldDefinitionSet));
            }

            if (executionProfile is null)
            {
                throw new ArgumentNullException(nameof(executionProfile));
            }

            ResourceUsage[] resourceUsages = CollectResourceUsages(generationPlan);

            List<RunnablePlanCompilationError> errors = ResolveFieldDefinitions(
                resourceUsages,
                fieldDefinitionSet,
                out ResourceFieldBindingSource[] bindingSources);

            if (errors.Count > 0)
            {
                return RunnablePlanCompilationResult.Failure(errors);
            }

            Array.Sort(
                bindingSources,
                static (left, right) => left.FieldDefinition.Symbol.CompareTo(right.FieldDefinition.Symbol));

            Dictionary<Symbol, FieldIndex> fieldIndicesByResourceSymbol = CreateFieldBindings(
                bindingSources,
                out ResourceFieldBinding[] fieldBindings);

            CreateRunnableRows(
                generationPlan,
                fieldIndicesByResourceSymbol,
                out RunnableStage[] stages,
                out RunnableOperation[] operations);

            var runnablePlan = new RunnablePlan(
                generationPlan,
                executionProfile,
                fieldBindings,
                stages,
                operations);

            return RunnablePlanCompilationResult.Success(runnablePlan);
        }

        /// <summary>
        /// Compiles the specified generation plan or throws when compilation fails.
        /// </summary>
        /// <param name="generationPlan">The source managed semantic generation plan.</param>
        /// <param name="fieldDefinitionSet">The accepted managed field definition set.</param>
        /// <param name="executionProfile">The selected managed execution profile.</param>
        /// <returns>The compiled runnable plan.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when runnable-plan compilation fails.
        /// </exception>
        public RunnablePlan CompileOrThrow(
            GenerationPlan generationPlan,
            FieldDefinitionSet fieldDefinitionSet,
            ExecutionProfile executionProfile)
        {
            RunnablePlanCompilationResult result = Compile(
                generationPlan,
                fieldDefinitionSet,
                executionProfile);

            if (!result.Succeeded)
            {
                RunnablePlanCompilationError firstError = result.Errors[0];

                throw new InvalidOperationException(
                    $"Runnable-plan compilation failed with {result.Errors.Count} error(s). First error: {firstError.Code}: {firstError.Message}");
            }

            return result.RunnablePlan!;
        }

        private static ResourceUsage[] CollectResourceUsages(GenerationPlan generationPlan)
        {
            var resourceUsages = new List<ResourceUsage>();
            var resourceUsagesBySymbol = new Dictionary<Symbol, ResourceUsage>();

            for (int stageIndex = 0; stageIndex < generationPlan.StagePlanNodes.Count; stageIndex++)
            {
                StagePlanNode stagePlanNode = generationPlan.StagePlanNodes[stageIndex];

                AddResourceUsages(
                    stagePlanNode.StageContract.RequiredInputs,
                    requiresInput: true,
                    producesOutput: false,
                    resourceUsages,
                    resourceUsagesBySymbol);

                AddResourceUsages(
                    stagePlanNode.StageContract.ProducedOutputs,
                    requiresInput: false,
                    producesOutput: true,
                    resourceUsages,
                    resourceUsagesBySymbol);

                for (int operationIndex = 0;
                    operationIndex < stagePlanNode.OperationPlanNodes.Count;
                    operationIndex++)
                {
                    OperationPlanNode operationPlanNode = stagePlanNode.OperationPlanNodes[operationIndex];

                    AddResourceUsages(
                        operationPlanNode.OperationContract.RequiredInputs,
                        requiresInput: true,
                        producesOutput: false,
                        resourceUsages,
                        resourceUsagesBySymbol);

                    AddResourceUsages(
                        operationPlanNode.OperationContract.ProducedOutputs,
                        requiresInput: false,
                        producesOutput: true,
                        resourceUsages,
                        resourceUsagesBySymbol);
                }
            }

            ResourceUsage[] copiedResourceUsages = resourceUsages.ToArray();

            Array.Sort(
                copiedResourceUsages,
                static (left, right) => left.ResourceDefinition.Symbol.CompareTo(right.ResourceDefinition.Symbol));

            return copiedResourceUsages;
        }

        private static void AddResourceUsages(
            IReadOnlyList<ResourceDefinition> resourceDefinitions,
            bool requiresInput,
            bool producesOutput,
            List<ResourceUsage> resourceUsages,
            Dictionary<Symbol, ResourceUsage> resourceUsagesBySymbol)
        {
            for (int index = 0; index < resourceDefinitions.Count; index++)
            {
                ResourceDefinition resourceDefinition = resourceDefinitions[index];

                if (!resourceUsagesBySymbol.TryGetValue(resourceDefinition.Symbol, out ResourceUsage resourceUsage))
                {
                    resourceUsage = new ResourceUsage(resourceDefinition);
                    resourceUsages.Add(resourceUsage);
                    resourceUsagesBySymbol.Add(resourceDefinition.Symbol, resourceUsage);
                }

                resourceUsage.RequiresInput |= requiresInput;
                resourceUsage.ProducesOutput |= producesOutput;
            }
        }

        private static List<RunnablePlanCompilationError> ResolveFieldDefinitions(
            IReadOnlyList<ResourceUsage> resourceUsages,
            FieldDefinitionSet fieldDefinitionSet,
            out ResourceFieldBindingSource[] bindingSources)
        {
            var errors = new List<RunnablePlanCompilationError>();
            var resolvedBindingSources = new List<ResourceFieldBindingSource>();

            for (int index = 0; index < resourceUsages.Count; index++)
            {
                ResourceUsage resourceUsage = resourceUsages[index];
                ResourceDefinition resourceDefinition = resourceUsage.ResourceDefinition;

                if (!fieldDefinitionSet.TryGetFieldDefinitionForResourceDefinitionSymbol(
                        resourceDefinition.Symbol,
                        out FieldDefinition? fieldDefinition))
                {
                    errors.Add(new RunnablePlanCompilationError(
                        RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                        $"No field definition was found for resource definition '{resourceDefinition.Symbol}'.",
                        resourceDefinition.Symbol));

                    continue;
                }

                if (!ReferenceEquals(fieldDefinition.ResourceDefinition, resourceDefinition))
                {
                    errors.Add(new RunnablePlanCompilationError(
                        RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch,
                        $"Field definition '{fieldDefinition.Symbol}' references resource definition '{fieldDefinition.ResourceDefinition.Symbol}', but the generation plan uses a different resource definition instance for '{resourceDefinition.Symbol}'.",
                        resourceDefinition.Symbol));

                    continue;
                }

                resolvedBindingSources.Add(new ResourceFieldBindingSource(
                    resourceDefinition,
                    fieldDefinition,
                    resourceUsage.ToPlanRole()));
            }

            bindingSources = resolvedBindingSources.ToArray();
            return errors;
        }

        private static Dictionary<Symbol, FieldIndex> CreateFieldBindings(
            IReadOnlyList<ResourceFieldBindingSource> bindingSources,
            out ResourceFieldBinding[] fieldBindings)
        {
            fieldBindings = new ResourceFieldBinding[bindingSources.Count];
            var fieldIndicesByResourceSymbol = new Dictionary<Symbol, FieldIndex>();

            for (int index = 0; index < bindingSources.Count; index++)
            {
                ResourceFieldBindingSource bindingSource = bindingSources[index];
                var fieldIndex = new FieldIndex(index);

                fieldBindings[index] = new ResourceFieldBinding(
                    fieldIndex,
                    bindingSource.ResourceDefinition,
                    bindingSource.FieldDefinition,
                    bindingSource.PlanRole,
                    FieldCapturePolicy.DoNotCapture);

                fieldIndicesByResourceSymbol.Add(
                    bindingSource.ResourceDefinition.Symbol,
                    fieldIndex);
            }

            return fieldIndicesByResourceSymbol;
        }

        private static void CreateRunnableRows(
            GenerationPlan generationPlan,
            IReadOnlyDictionary<Symbol, FieldIndex> fieldIndicesByResourceSymbol,
            out RunnableStage[] stages,
            out RunnableOperation[] operations)
        {
            stages = new RunnableStage[generationPlan.StagePlanNodes.Count];
            var operationRows = new List<RunnableOperation>();

            for (int stageIndexValue = 0;
                stageIndexValue < generationPlan.StagePlanNodes.Count;
                stageIndexValue++)
            {
                StagePlanNode stagePlanNode = generationPlan.StagePlanNodes[stageIndexValue];
                var stageIndex = new StageIndex(stageIndexValue);
                var operationIndices = new OperationIndex[stagePlanNode.OperationPlanNodes.Count];

                for (int localOperationIndex = 0;
                    localOperationIndex < stagePlanNode.OperationPlanNodes.Count;
                    localOperationIndex++)
                {
                    OperationPlanNode operationPlanNode = stagePlanNode.OperationPlanNodes[localOperationIndex];
                    var operationIndex = new OperationIndex(operationRows.Count);
                    operationIndices[localOperationIndex] = operationIndex;

                    operationRows.Add(new RunnableOperation(
                        operationIndex,
                        stageIndex,
                        operationPlanNode,
                        GetFieldIndices(
                            operationPlanNode.OperationContract.RequiredInputs,
                            fieldIndicesByResourceSymbol),
                        GetFieldIndices(
                            operationPlanNode.OperationContract.ProducedOutputs,
                            fieldIndicesByResourceSymbol)));
                }

                stages[stageIndexValue] = new RunnableStage(
                    stageIndex,
                    stagePlanNode,
                    GetFieldIndices(
                        stagePlanNode.StageContract.RequiredInputs,
                        fieldIndicesByResourceSymbol),
                    GetFieldIndices(
                        stagePlanNode.StageContract.ProducedOutputs,
                        fieldIndicesByResourceSymbol),
                    operationIndices);
            }

            operations = operationRows.ToArray();
        }

        private static FieldIndex[] GetFieldIndices(
            IReadOnlyList<ResourceDefinition> resourceDefinitions,
            IReadOnlyDictionary<Symbol, FieldIndex> fieldIndicesByResourceSymbol)
        {
            var fieldIndices = new FieldIndex[resourceDefinitions.Count];

            for (int index = 0; index < resourceDefinitions.Count; index++)
            {
                fieldIndices[index] = fieldIndicesByResourceSymbol[resourceDefinitions[index].Symbol];
            }

            return fieldIndices;
        }

        private sealed class ResourceUsage
        {
            public ResourceUsage(ResourceDefinition resourceDefinition)
            {
                ResourceDefinition = resourceDefinition;
            }

            public ResourceDefinition ResourceDefinition { get; }

            public bool RequiresInput { get; set; }

            public bool ProducesOutput { get; set; }

            public FieldPlanRole ToPlanRole()
            {
                if (RequiresInput && ProducesOutput)
                {
                    return FieldPlanRole.RequiredInputAndProducedOutput;
                }

                return RequiresInput
                    ? FieldPlanRole.RequiredInput
                    : FieldPlanRole.ProducedOutput;
            }
        }

        private readonly struct ResourceFieldBindingSource
        {
            public ResourceFieldBindingSource(
                ResourceDefinition resourceDefinition,
                FieldDefinition fieldDefinition,
                FieldPlanRole planRole)
            {
                ResourceDefinition = resourceDefinition;
                FieldDefinition = fieldDefinition;
                PlanRole = planRole;
            }

            public ResourceDefinition ResourceDefinition { get; }

            public FieldDefinition FieldDefinition { get; }

            public FieldPlanRole PlanRole { get; }
        }
    }
}