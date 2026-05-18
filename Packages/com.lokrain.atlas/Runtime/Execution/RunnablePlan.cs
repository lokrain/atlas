#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Planning;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents immutable managed runnable metadata compiled from a generation plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A runnable plan contains deterministic field, stage, and operation tables. The tables are dense and
    /// zero-based: <c>FieldBindings[i].FieldIndex.Value == i</c>, <c>Stages[i].StageIndex.Value == i</c>, and
    /// <c>Operations[i].OperationIndex.Value == i</c>.
    /// </para>
    /// <para>
    /// This type is managed metadata only. It does not allocate storage, own native memory, create field handles,
    /// schedule jobs, describe scratch memory, bind ECS data, capture artifacts, or capture runtime diagnostics.
    /// </para>
    /// </remarks>
    public sealed class RunnablePlan : IEquatable<RunnablePlan>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunnablePlan"/> class.
        /// </summary>
        /// <param name="generationPlan">The source managed semantic generation plan.</param>
        /// <param name="executionProfile">The selected managed execution-profile metadata.</param>
        /// <param name="fieldBindings">The dense field-binding table.</param>
        /// <param name="stages">The dense stage table.</param>
        /// <param name="operations">The dense operation table.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a table contains null entries, non-dense indices, out-of-range references, or source-plan
        /// references inconsistent with the generation plan.
        /// </exception>
        public RunnablePlan(
            GenerationPlan generationPlan,
            ExecutionProfile executionProfile,
            IEnumerable<ResourceFieldBinding> fieldBindings,
            IEnumerable<RunnableStage> stages,
            IEnumerable<RunnableOperation> operations)
        {
            if (generationPlan is null)
            {
                throw new ArgumentNullException(nameof(generationPlan));
            }

            if (executionProfile is null)
            {
                throw new ArgumentNullException(nameof(executionProfile));
            }

            if (fieldBindings is null)
            {
                throw new ArgumentNullException(nameof(fieldBindings));
            }

            if (stages is null)
            {
                throw new ArgumentNullException(nameof(stages));
            }

            if (operations is null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            ResourceFieldBinding[] copiedFieldBindings = CopyFieldBindings(fieldBindings);
            RunnableStage[] copiedStages = CopyStages(stages);
            RunnableOperation[] copiedOperations = CopyOperations(operations);

            ValidateDenseFieldBindings(copiedFieldBindings);
            ValidateDenseStages(copiedStages);
            ValidateDenseOperations(copiedOperations);
            ValidateStagePlanNodeClosure(generationPlan, copiedStages);
            ValidateStageOperationClosure(copiedStages, copiedOperations);
            ValidateFieldIndexReferences(copiedFieldBindings, copiedStages, copiedOperations);

            GenerationPlan = generationPlan;
            ExecutionProfile = executionProfile;
            FieldBindings = new ReadOnlyCollection<ResourceFieldBinding>(copiedFieldBindings);
            Stages = new ReadOnlyCollection<RunnableStage>(copiedStages);
            Operations = new ReadOnlyCollection<RunnableOperation>(copiedOperations);
        }

        /// <summary>
        /// Gets the source managed semantic generation plan.
        /// </summary>
        public GenerationPlan GenerationPlan { get; }

        /// <summary>
        /// Gets the selected managed execution-profile metadata.
        /// </summary>
        public ExecutionProfile ExecutionProfile { get; }

        /// <summary>
        /// Gets the dense field-binding table.
        /// </summary>
        public IReadOnlyList<ResourceFieldBinding> FieldBindings { get; }

        /// <summary>
        /// Gets the dense stage table.
        /// </summary>
        public IReadOnlyList<RunnableStage> Stages { get; }

        /// <summary>
        /// Gets the dense operation table.
        /// </summary>
        public IReadOnlyList<RunnableOperation> Operations { get; }

        /// <inheritdoc/>
        public bool Equals(RunnablePlan? other)
        {
            return other is not null
                && ReferenceEquals(GenerationPlan, other.GenerationPlan)
                && ReferenceEquals(ExecutionProfile, other.ExecutionProfile)
                && SequenceEquals(FieldBindings, other.FieldBindings)
                && SequenceEquals(Stages, other.Stages)
                && SequenceEquals(Operations, other.Operations);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is RunnablePlan other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(RuntimeHelpers.GetHashCode(GenerationPlan));
            hashCode.Add(RuntimeHelpers.GetHashCode(ExecutionProfile));
            AddSequenceHash(ref hashCode, FieldBindings);
            AddSequenceHash(ref hashCode, Stages);
            AddSequenceHash(ref hashCode, Operations);
            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(RunnablePlan)}(" +
                   $"{nameof(GenerationPlan)}: {GenerationPlan.GenerationRecipeDefinition.Symbol}, " +
                   $"{nameof(ExecutionProfile)}: {ExecutionProfile.Symbol}, " +
                   $"{nameof(FieldBindings)}: {FieldBindings.Count}, " +
                   $"{nameof(Stages)}: {Stages.Count}, " +
                   $"{nameof(Operations)}: {Operations.Count})";
        }

        /// <summary>
        /// Determines whether two runnable plans are equal.
        /// </summary>
        public static bool operator ==(RunnablePlan? left, RunnablePlan? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two runnable plans are not equal.
        /// </summary>
        public static bool operator !=(RunnablePlan? left, RunnablePlan? right)
        {
            return !Equals(left, right);
        }

        private static ResourceFieldBinding[] CopyFieldBindings(
            IEnumerable<ResourceFieldBinding> fieldBindings)
        {
            var copiedFieldBindings = new List<ResourceFieldBinding>();

            foreach (ResourceFieldBinding? fieldBinding in fieldBindings)
            {
                if (fieldBinding is null)
                {
                    throw new ArgumentException(
                        "Field bindings cannot contain null entries.",
                        nameof(fieldBindings));
                }

                copiedFieldBindings.Add(fieldBinding);
            }

            return copiedFieldBindings.ToArray();
        }

        private static RunnableStage[] CopyStages(IEnumerable<RunnableStage> stages)
        {
            var copiedStages = new List<RunnableStage>();

            foreach (RunnableStage? stage in stages)
            {
                if (stage is null)
                {
                    throw new ArgumentException(
                        "Runnable stages cannot contain null entries.",
                        nameof(stages));
                }

                copiedStages.Add(stage);
            }

            return copiedStages.ToArray();
        }

        private static RunnableOperation[] CopyOperations(IEnumerable<RunnableOperation> operations)
        {
            var copiedOperations = new List<RunnableOperation>();

            foreach (RunnableOperation? operation in operations)
            {
                if (operation is null)
                {
                    throw new ArgumentException(
                        "Runnable operations cannot contain null entries.",
                        nameof(operations));
                }

                copiedOperations.Add(operation);
            }

            return copiedOperations.ToArray();
        }

        private static void ValidateDenseFieldBindings(IReadOnlyList<ResourceFieldBinding> fieldBindings)
        {
            for (int index = 0; index < fieldBindings.Count; index++)
            {
                if (fieldBindings[index].FieldIndex.Value != index)
                {
                    throw new ArgumentException(
                        $"Field binding at table index {index} has field index {fieldBindings[index].FieldIndex.Value}.",
                        nameof(fieldBindings));
                }
            }
        }

        private static void ValidateDenseStages(IReadOnlyList<RunnableStage> stages)
        {
            for (int index = 0; index < stages.Count; index++)
            {
                if (stages[index].StageIndex.Value != index)
                {
                    throw new ArgumentException(
                        $"Runnable stage at table index {index} has stage index {stages[index].StageIndex.Value}.",
                        nameof(stages));
                }
            }
        }

        private static void ValidateDenseOperations(IReadOnlyList<RunnableOperation> operations)
        {
            for (int index = 0; index < operations.Count; index++)
            {
                if (operations[index].OperationIndex.Value != index)
                {
                    throw new ArgumentException(
                        $"Runnable operation at table index {index} has operation index {operations[index].OperationIndex.Value}.",
                        nameof(operations));
                }
            }
        }

        private static void ValidateStagePlanNodeClosure(
            GenerationPlan generationPlan,
            IReadOnlyList<RunnableStage> stages)
        {
            if (stages.Count != generationPlan.StagePlanNodes.Count)
            {
                throw new ArgumentException(
                    $"Runnable plan requires {generationPlan.StagePlanNodes.Count} stage rows, but {stages.Count} were provided.",
                    nameof(stages));
            }

            for (int index = 0; index < stages.Count; index++)
            {
                if (!ReferenceEquals(stages[index].StagePlanNode, generationPlan.StagePlanNodes[index]))
                {
                    throw new ArgumentException(
                        $"Runnable stage at index {index} does not reference the generation plan stage node at the same index.",
                        nameof(stages));
                }
            }
        }

        private static void ValidateStageOperationClosure(
            IReadOnlyList<RunnableStage> stages,
            IReadOnlyList<RunnableOperation> operations)
        {
            var referencedOperationIndices = new HashSet<OperationIndex>();

            for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            {
                RunnableStage stage = stages[stageIndex];

                for (int localOperationIndex = 0;
                    localOperationIndex < stage.OperationIndices.Count;
                    localOperationIndex++)
                {
                    OperationIndex operationIndex = stage.OperationIndices[localOperationIndex];

                    if (operationIndex.Value < 0 || operationIndex.Value >= operations.Count)
                    {
                        throw new ArgumentException(
                            $"Runnable stage '{stage.StageIndex}' references out-of-range operation index '{operationIndex}'.",
                            nameof(stages));
                    }

                    if (!referencedOperationIndices.Add(operationIndex))
                    {
                        throw new ArgumentException(
                            $"Runnable operation index '{operationIndex}' is referenced by more than one stage operation closure.",
                            nameof(stages));
                    }

                    RunnableOperation operation = operations[operationIndex.Value];

                    if (operation.StageIndex != stage.StageIndex)
                    {
                        throw new ArgumentException(
                            $"Runnable operation '{operation.OperationIndex}' belongs to stage index '{operation.StageIndex}', but stage '{stage.StageIndex}' references it.",
                            nameof(operations));
                    }

                    if (!ReferenceEquals(
                        operation.OperationPlanNode,
                        stage.StagePlanNode.OperationPlanNodes[localOperationIndex]))
                    {
                        throw new ArgumentException(
                            $"Runnable operation '{operation.OperationIndex}' does not reference the expected operation plan node for stage '{stage.StageIndex}'.",
                            nameof(operations));
                    }
                }
            }

            if (referencedOperationIndices.Count != operations.Count)
            {
                throw new ArgumentException(
                    "Runnable operation table contains operations not referenced by the stage operation closures.",
                    nameof(operations));
            }
        }

        private static void ValidateFieldIndexReferences(
            IReadOnlyList<ResourceFieldBinding> fieldBindings,
            IReadOnlyList<RunnableStage> stages,
            IReadOnlyList<RunnableOperation> operations)
        {
            for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            {
                ValidateFieldIndexReferences(
                    fieldBindings,
                    stages[stageIndex].RequiredInputFieldIndices,
                    nameof(stages));

                ValidateFieldIndexReferences(
                    fieldBindings,
                    stages[stageIndex].ProducedOutputFieldIndices,
                    nameof(stages));
            }

            for (int operationIndex = 0; operationIndex < operations.Count; operationIndex++)
            {
                ValidateFieldIndexReferences(
                    fieldBindings,
                    operations[operationIndex].RequiredInputFieldIndices,
                    nameof(operations));

                ValidateFieldIndexReferences(
                    fieldBindings,
                    operations[operationIndex].ProducedOutputFieldIndices,
                    nameof(operations));
            }
        }

        private static void ValidateFieldIndexReferences(
            IReadOnlyList<ResourceFieldBinding> fieldBindings,
            IReadOnlyList<FieldIndex> fieldIndices,
            string parameterName)
        {
            for (int index = 0; index < fieldIndices.Count; index++)
            {
                FieldIndex fieldIndex = fieldIndices[index];

                if (fieldIndex.Value < 0 || fieldIndex.Value >= fieldBindings.Count)
                {
                    throw new ArgumentException(
                        $"Runnable metadata references out-of-range field index '{fieldIndex}'.",
                        parameterName);
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

        private static void AddSequenceHash<TValue>(
            ref HashCode hashCode,
            IReadOnlyList<TValue> values)
        {
            for (int index = 0; index < values.Count; index++)
            {
                hashCode.Add(values[index]);
            }
        }
    }
}
