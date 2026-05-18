#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Planning;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents one immutable operation row in managed runnable-plan metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A runnable operation binds one compiler-created <see cref="OperationPlanNode"/> occurrence to dense
    /// plan-local table indices. Required input field indices follow the source operation contract required-input
    /// order. Produced output field indices follow the source operation contract produced-output order.
    /// </para>
    /// <para>
    /// This type is managed metadata only. It does not execute operations, allocate storage, own native memory,
    /// create field handles, schedule jobs, describe scratch memory, bind ECS data, capture artifacts, or capture
    /// runtime diagnostics.
    /// </para>
    /// </remarks>
    public sealed class RunnableOperation : IEquatable<RunnableOperation>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunnableOperation"/> class.
        /// </summary>
        /// <param name="operationIndex">The dense, plan-local operation table index.</param>
        /// <param name="stageIndex">The dense, plan-local stage table index that owns this operation.</param>
        /// <param name="operationPlanNode">The source managed operation plan node.</param>
        /// <param name="requiredInputFieldIndices">The required input field indices in operation-contract order.</param>
        /// <param name="producedOutputFieldIndices">The produced output field indices in operation-contract order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationPlanNode"/>, <paramref name="requiredInputFieldIndices"/>, or
        /// <paramref name="producedOutputFieldIndices"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when field-index counts do not match the source operation contract, or when a field-index
        /// sequence contains duplicates.
        /// </exception>
        public RunnableOperation(
            OperationIndex operationIndex,
            StageIndex stageIndex,
            OperationPlanNode operationPlanNode,
            IEnumerable<FieldIndex> requiredInputFieldIndices,
            IEnumerable<FieldIndex> producedOutputFieldIndices)
        {
            if (operationPlanNode is null)
            {
                throw new ArgumentNullException(nameof(operationPlanNode));
            }

            if (requiredInputFieldIndices is null)
            {
                throw new ArgumentNullException(nameof(requiredInputFieldIndices));
            }

            if (producedOutputFieldIndices is null)
            {
                throw new ArgumentNullException(nameof(producedOutputFieldIndices));
            }

            FieldIndex[] copiedRequiredInputFieldIndices = CopyFieldIndices(
                requiredInputFieldIndices,
                nameof(requiredInputFieldIndices),
                "Required input field indices");

            FieldIndex[] copiedProducedOutputFieldIndices = CopyFieldIndices(
                producedOutputFieldIndices,
                nameof(producedOutputFieldIndices),
                "Produced output field indices");

            ValidateFieldIndexCount(
                copiedRequiredInputFieldIndices,
                operationPlanNode.OperationContract.RequiredInputs.Count,
                nameof(requiredInputFieldIndices),
                "required input");

            ValidateFieldIndexCount(
                copiedProducedOutputFieldIndices,
                operationPlanNode.OperationContract.ProducedOutputs.Count,
                nameof(producedOutputFieldIndices),
                "produced output");

            OperationIndex = operationIndex;
            StageIndex = stageIndex;
            OperationPlanNode = operationPlanNode;
            RequiredInputFieldIndices = new ReadOnlyCollection<FieldIndex>(copiedRequiredInputFieldIndices);
            ProducedOutputFieldIndices = new ReadOnlyCollection<FieldIndex>(copiedProducedOutputFieldIndices);
        }

        /// <summary>
        /// Gets the dense, plan-local operation table index.
        /// </summary>
        public OperationIndex OperationIndex { get; }

        /// <summary>
        /// Gets the dense, plan-local stage table index that owns this operation.
        /// </summary>
        public StageIndex StageIndex { get; }

        /// <summary>
        /// Gets the source managed operation plan node.
        /// </summary>
        public OperationPlanNode OperationPlanNode { get; }

        /// <summary>
        /// Gets the required input field indices in operation-contract order.
        /// </summary>
        public IReadOnlyList<FieldIndex> RequiredInputFieldIndices { get; }

        /// <summary>
        /// Gets the produced output field indices in operation-contract order.
        /// </summary>
        public IReadOnlyList<FieldIndex> ProducedOutputFieldIndices { get; }

        /// <inheritdoc/>
        public bool Equals(RunnableOperation? other)
        {
            return other is not null
                && OperationIndex == other.OperationIndex
                && StageIndex == other.StageIndex
                && ReferenceEquals(OperationPlanNode, other.OperationPlanNode)
                && SequenceEquals(RequiredInputFieldIndices, other.RequiredInputFieldIndices)
                && SequenceEquals(ProducedOutputFieldIndices, other.ProducedOutputFieldIndices);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is RunnableOperation other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(OperationIndex);
            hashCode.Add(StageIndex);
            hashCode.Add(RuntimeHelpers.GetHashCode(OperationPlanNode));

            AddSequenceHash(ref hashCode, RequiredInputFieldIndices);
            AddSequenceHash(ref hashCode, ProducedOutputFieldIndices);

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(RunnableOperation)}(" +
                   $"{nameof(OperationIndex)}: {OperationIndex}, " +
                   $"{nameof(StageIndex)}: {StageIndex}, " +
                   $"{nameof(OperationPlanNode)}: {OperationPlanNode.OperationDefinition.Symbol}, " +
                   $"{nameof(RequiredInputFieldIndices)}: {RequiredInputFieldIndices.Count}, " +
                   $"{nameof(ProducedOutputFieldIndices)}: {ProducedOutputFieldIndices.Count})";
        }

        /// <summary>
        /// Determines whether two runnable operations are equal.
        /// </summary>
        public static bool operator ==(RunnableOperation? left, RunnableOperation? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two runnable operations are not equal.
        /// </summary>
        public static bool operator !=(RunnableOperation? left, RunnableOperation? right)
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

        private static void ValidateFieldIndexCount(
            IReadOnlyList<FieldIndex> fieldIndices,
            int expectedCount,
            string parameterName,
            string description)
        {
            if (fieldIndices.Count != expectedCount)
            {
                throw new ArgumentException(
                    $"Runnable operation requires {expectedCount} {description} field indices, but {fieldIndices.Count} were provided.",
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
