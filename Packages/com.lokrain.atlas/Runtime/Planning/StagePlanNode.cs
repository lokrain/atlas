#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a compiler-created resolved stage occurrence inside a generation plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage plan node resolves one selected stage route to its stage definition, stage contract, and ordered
    /// operation plan nodes.
    /// </para>
    /// <para>
    /// The stage definition identifies the semantic generation stage. The stage route definition identifies the
    /// selected ordered route for satisfying that stage. The stage contract describes the semantic resource flow
    /// for the stage. The operation plan nodes describe the resolved operation occurrences for the selected route.
    /// </para>
    /// <para>
    /// Operation plan node order must match the selected route-step order exactly. Missing, extra, null, or
    /// out-of-order operation plan nodes are invalid.
    /// </para>
    /// <para>
    /// This type is managed planning data only. It does not contain executable metadata, scheduler bindings,
    /// runtime state, job data, native containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Stage plan nodes are created by the generation plan compiler after resolving request selections through
    /// an accepted generation catalog. Runnable-plan compilation later resolves execution metadata.
    /// </para>
    /// <para>
    /// A non-null <see cref="StagePlanNode"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class StagePlanNode : IEquatable<StagePlanNode>
    {
        internal StagePlanNode(
            StageDefinition stageDefinition,
            StageRouteDefinition stageRouteDefinition,
            StageContract stageContract,
            IEnumerable<OperationPlanNode> operationPlanNodes)
        {
            if (stageDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageDefinition));
            }

            if (stageRouteDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageRouteDefinition));
            }

            if (stageContract is null)
            {
                throw new ArgumentNullException(nameof(stageContract));
            }

            if (operationPlanNodes is null)
            {
                throw new ArgumentNullException(nameof(operationPlanNodes));
            }

            if (!ReferenceEquals(stageRouteDefinition.StageDefinition, stageDefinition))
            {
                throw new ArgumentException(
                    $"Stage route definition '{stageRouteDefinition.Symbol}' does not belong to stage definition '{stageDefinition.Symbol}'.",
                    nameof(stageRouteDefinition));
            }

            if (!ReferenceEquals(stageContract.StageDefinition, stageDefinition))
            {
                throw new ArgumentException(
                    $"Stage contract for stage definition '{stageContract.StageDefinition.Symbol}' does not belong to stage definition '{stageDefinition.Symbol}'.",
                    nameof(stageContract));
            }

            OperationPlanNode[] copiedOperationPlanNodes = CopyOperationPlanNodes(
                stageRouteDefinition,
                operationPlanNodes);

            StageDefinition = stageDefinition;
            StageRouteDefinition = stageRouteDefinition;
            StageContract = stageContract;
            OperationPlanNodes = new ReadOnlyCollection<OperationPlanNode>(copiedOperationPlanNodes);
        }

        /// <summary>
        /// Gets the resolved stage definition.
        /// </summary>
        public StageDefinition StageDefinition { get; }

        /// <summary>
        /// Gets the selected stage route definition.
        /// </summary>
        public StageRouteDefinition StageRouteDefinition { get; }

        /// <summary>
        /// Gets the resolved stage contract.
        /// </summary>
        public StageContract StageContract { get; }

        /// <summary>
        /// Gets the ordered resolved operation plan nodes for the selected route.
        /// </summary>
        public IReadOnlyList<OperationPlanNode> OperationPlanNodes { get; }

        /// <inheritdoc/>
        public bool Equals(StagePlanNode? other)
        {
            return other is not null
                && StageDefinition == other.StageDefinition
                && StageRouteDefinition == other.StageRouteDefinition
                && StageContract == other.StageContract
                && SequenceEquals(OperationPlanNodes, other.OperationPlanNodes);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StagePlanNode other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(StageDefinition);
            hashCode.Add(StageRouteDefinition);
            hashCode.Add(StageContract);

            for (int index = 0; index < OperationPlanNodes.Count; index++)
            {
                hashCode.Add(OperationPlanNodes[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StagePlanNode)}({nameof(StageDefinition)}: {StageDefinition.Symbol}, {nameof(StageRouteDefinition)}: {StageRouteDefinition.Symbol}, {nameof(OperationPlanNodes)}: {OperationPlanNodes.Count})";
        }

        /// <summary>
        /// Determines whether two stage plan nodes are equal.
        /// </summary>
        public static bool operator ==(StagePlanNode? left, StagePlanNode? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage plan nodes are not equal.
        /// </summary>
        public static bool operator !=(StagePlanNode? left, StagePlanNode? right)
        {
            return !Equals(left, right);
        }

        private static OperationPlanNode[] CopyOperationPlanNodes(
            StageRouteDefinition stageRouteDefinition,
            IEnumerable<OperationPlanNode> operationPlanNodes)
        {
            var copiedOperationPlanNodes = new List<OperationPlanNode>();

            foreach (OperationPlanNode? operationPlanNode in operationPlanNodes)
            {
                if (operationPlanNode is null)
                {
                    throw new ArgumentException(
                        "Operation plan nodes cannot contain null entries.",
                        nameof(operationPlanNodes));
                }

                copiedOperationPlanNodes.Add(operationPlanNode);
            }

            if (copiedOperationPlanNodes.Count != stageRouteDefinition.StageRouteStepDefinitions.Count)
            {
                throw new ArgumentException(
                    $"Stage route definition '{stageRouteDefinition.Symbol}' requires {stageRouteDefinition.StageRouteStepDefinitions.Count} operation plan nodes, but {copiedOperationPlanNodes.Count} were provided.",
                    nameof(operationPlanNodes));
            }

            for (int index = 0; index < copiedOperationPlanNodes.Count; index++)
            {
                StageRouteStepDefinition expectedRouteStepDefinition =
                    stageRouteDefinition.StageRouteStepDefinitions[index];

                OperationPlanNode operationPlanNode = copiedOperationPlanNodes[index];

                if (!ReferenceEquals(
                    operationPlanNode.StageRouteStepDefinition,
                    expectedRouteStepDefinition))
                {
                    throw new ArgumentException(
                        $"Operation plan node at index {index} resolves route step '{operationPlanNode.StageRouteStepDefinition.Symbol}', but stage route definition '{stageRouteDefinition.Symbol}' requires route step '{expectedRouteStepDefinition.Symbol}'.",
                        nameof(operationPlanNodes));
                }
            }

            return copiedOperationPlanNodes.ToArray();
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