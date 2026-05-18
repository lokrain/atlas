#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Planning;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents one immutable stage row in managed runnable-plan metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A runnable stage binds one compiler-created <see cref="StagePlanNode"/> occurrence to dense plan-local
    /// field and operation table indices. Required input and produced output field indices follow the source stage
    /// contract order. Operation indices follow the source stage route operation order.
    /// </para>
    /// <para>
    /// This type is managed metadata only. It does not execute stages, allocate storage, own native memory,
    /// schedule jobs, describe scratch memory, bind ECS data, capture artifacts, or capture runtime diagnostics.
    /// </para>
    /// </remarks>
    public sealed class RunnableStage : IEquatable<RunnableStage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunnableStage"/> class.
        /// </summary>
        /// <param name="stageIndex">The dense, plan-local stage table index.</param>
        /// <param name="stagePlanNode">The source managed stage plan node.</param>
        /// <param name="requiredInputFieldIndices">The required input field indices in stage-contract order.</param>
        /// <param name="producedOutputFieldIndices">The produced output field indices in stage-contract order.</param>
        /// <param name="operationIndices">The operation indices owned by this stage in route order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stagePlanNode"/>, <paramref name="requiredInputFieldIndices"/>,
        /// <paramref name="producedOutputFieldIndices"/>, or <paramref name="operationIndices"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when index counts do not match the source stage plan node, or when an index sequence contains
        /// duplicates.
        /// </exception>
        public RunnableStage(
            StageIndex stageIndex,
            StagePlanNode stagePlanNode,
            IEnumerable<FieldIndex> requiredInputFieldIndices,
            IEnumerable<FieldIndex> producedOutputFieldIndices,
            IEnumerable<OperationIndex> operationIndices)
        {
            if (stagePlanNode is null)
            {
                throw new ArgumentNullException(nameof(stagePlanNode));
            }

            if (requiredInputFieldIndices is null)
            {
                throw new ArgumentNullException(nameof(requiredInputFieldIndices));
            }

            if (producedOutputFieldIndices is null)
            {
                throw new ArgumentNullException(nameof(producedOutputFieldIndices));
            }

            if (operationIndices is null)
            {
                throw new ArgumentNullException(nameof(operationIndices));
            }

            FieldIndex[] copiedRequiredInputFieldIndices = CopyFieldIndices(
                requiredInputFieldIndices,
                nameof(requiredInputFieldIndices),
                "Required input field indices");

            FieldIndex[] copiedProducedOutputFieldIndices = CopyFieldIndices(
                producedOutputFieldIndices,
                nameof(producedOutputFieldIndices),
                "Produced output field indices");

            OperationIndex[] copiedOperationIndices = CopyOperationIndices(operationIndices);

            ValidateCount(
                copiedRequiredInputFieldIndices.Length,
                stagePlanNode.StageContract.RequiredInputs.Count,
                nameof(requiredInputFieldIndices),
                "required input field indices");

            ValidateCount(
                copiedProducedOutputFieldIndices.Length,
                stagePlanNode.StageContract.ProducedOutputs.Count,
                nameof(producedOutputFieldIndices),
                "produced output field indices");

            ValidateCount(
                copiedOperationIndices.Length,
                stagePlanNode.OperationPlanNodes.Count,
                nameof(operationIndices),
                "operation indices");

            StageIndex = stageIndex;
            StagePlanNode = stagePlanNode;
            RequiredInputFieldIndices = new ReadOnlyCollection<FieldIndex>(copiedRequiredInputFieldIndices);
            ProducedOutputFieldIndices = new ReadOnlyCollection<FieldIndex>(copiedProducedOutputFieldIndices);
            OperationIndices = new ReadOnlyCollection<OperationIndex>(copiedOperationIndices);
        }

        /// <summary>
        /// Gets the dense, plan-local stage table index.
        /// </summary>
        public StageIndex StageIndex { get; }

        /// <summary>
        /// Gets the source managed stage plan node.
        /// </summary>
        public StagePlanNode StagePlanNode { get; }

        /// <summary>
        /// Gets the required input field indices in stage-contract order.
        /// </summary>
        public IReadOnlyList<FieldIndex> RequiredInputFieldIndices { get; }

        /// <summary>
        /// Gets the produced output field indices in stage-contract order.
        /// </summary>
        public IReadOnlyList<FieldIndex> ProducedOutputFieldIndices { get; }

        /// <summary>
        /// Gets the operation indices owned by this stage in route order.
        /// </summary>
        public IReadOnlyList<OperationIndex> OperationIndices { get; }

        /// <inheritdoc/>
        public bool Equals(RunnableStage? other)
        {
            return other is not null
                && StageIndex == other.StageIndex
                && ReferenceEquals(StagePlanNode, other.StagePlanNode)
                && SequenceEquals(RequiredInputFieldIndices, other.RequiredInputFieldIndices)
                && SequenceEquals(ProducedOutputFieldIndices, other.ProducedOutputFieldIndices)
                && SequenceEquals(OperationIndices, other.OperationIndices);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is RunnableStage other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(StageIndex);
            hashCode.Add(RuntimeHelpers.GetHashCode(StagePlanNode));

            AddSequenceHash(ref hashCode, RequiredInputFieldIndices);
            AddSequenceHash(ref hashCode, ProducedOutputFieldIndices);
            AddSequenceHash(ref hashCode, OperationIndices);

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(RunnableStage)}(" +
                   $"{nameof(StageIndex)}: {StageIndex}, " +
                   $"{nameof(StagePlanNode)}: {StagePlanNode.StageDefinition.Symbol}, " +
                   $"{nameof(RequiredInputFieldIndices)}: {RequiredInputFieldIndices.Count}, " +
                   $"{nameof(ProducedOutputFieldIndices)}: {ProducedOutputFieldIndices.Count}, " +
                   $"{nameof(OperationIndices)}: {OperationIndices.Count})";
        }

        /// <summary>
        /// Determines whether two runnable stages are equal.
        /// </summary>
        public static bool operator ==(RunnableStage? left, RunnableStage? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two runnable stages are not equal.
        /// </summary>
        public static bool operator !=(RunnableStage? left, RunnableStage? right)
        {
            return !Equals(left, right);
        }

        private static FieldIndex[] CopyFieldIndices(
            IEnumerable<FieldIndex> fieldIndices,
            string parameterName,
            string description)
        {
            var copiedFieldIndices = new List<FieldIndex>();
            var uniqueFieldIndices = new HashSet<FieldIndex>();

            foreach (FieldIndex fieldIndex in fieldIndices)
            {
                if (!uniqueFieldIndices.Add(fieldIndex))
                {
                    throw new ArgumentException(
                        $"{description} cannot contain duplicate field index '{fieldIndex}'.",
                        parameterName);
                }

                copiedFieldIndices.Add(fieldIndex);
            }

            return copiedFieldIndices.ToArray();
        }

        private static OperationIndex[] CopyOperationIndices(IEnumerable<OperationIndex> operationIndices)
        {
            var copiedOperationIndices = new List<OperationIndex>();
            var uniqueOperationIndices = new HashSet<OperationIndex>();

            foreach (OperationIndex operationIndex in operationIndices)
            {
                if (!uniqueOperationIndices.Add(operationIndex))
                {
                    throw new ArgumentException(
                        $"Operation indices cannot contain duplicate operation index '{operationIndex}'.",
                        nameof(operationIndices));
                }

                copiedOperationIndices.Add(operationIndex);
            }

            return copiedOperationIndices.ToArray();
        }

        private static void ValidateCount(
            int actualCount,
            int expectedCount,
            string parameterName,
            string description)
        {
            if (actualCount != expectedCount)
            {
                throw new ArgumentException(
                    $"Runnable stage requires {expectedCount} {description}, but {actualCount} were provided.",
                    parameterName);
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
