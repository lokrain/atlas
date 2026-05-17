#nullable enable

using System;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a compiler-created resolved operation occurrence inside a generation plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation plan node resolves one stage route step to its operation definition, selected operation
    /// implementation definition, and operation contract. It is managed planning data only; it does not contain
    /// executable bindings, runtime state, job data, native containers, ECS systems, Burst function pointers, or
    /// Unity runtime objects.
    /// </para>
    /// <para>
    /// Operation plan nodes are created by the generation plan compiler after resolving request selections through
    /// an accepted generation catalog. Runnable-plan compilation later resolves execution bindings.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationPlanNode"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class OperationPlanNode : IEquatable<OperationPlanNode>
    {
        internal OperationPlanNode(
            StageRouteStepDefinition stageRouteStepDefinition,
            OperationDefinition operationDefinition,
            OperationImplementationDefinition operationImplementationDefinition,
            OperationContract operationContract)
        {
            if (stageRouteStepDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageRouteStepDefinition));
            }

            if (operationDefinition is null)
            {
                throw new ArgumentNullException(nameof(operationDefinition));
            }

            if (operationImplementationDefinition is null)
            {
                throw new ArgumentNullException(nameof(operationImplementationDefinition));
            }

            if (operationContract is null)
            {
                throw new ArgumentNullException(nameof(operationContract));
            }

            if (stageRouteStepDefinition.OperationDefinitionSymbol != operationDefinition.Symbol)
            {
                throw new ArgumentException(
                    $"Stage route step '{stageRouteStepDefinition.Symbol}' references operation definition symbol '{stageRouteStepDefinition.OperationDefinitionSymbol}', but the resolved operation definition is '{operationDefinition.Symbol}'.",
                    nameof(operationDefinition));
            }

            if (!ReferenceEquals(operationImplementationDefinition.OperationDefinition, operationDefinition))
            {
                throw new ArgumentException(
                    $"Operation implementation definition '{operationImplementationDefinition.Symbol}' does not belong to operation definition '{operationDefinition.Symbol}'.",
                    nameof(operationImplementationDefinition));
            }

            if (!ReferenceEquals(operationContract.OperationDefinition, operationDefinition))
            {
                throw new ArgumentException(
                    $"Operation contract for operation definition '{operationContract.OperationDefinition.Symbol}' does not belong to operation definition '{operationDefinition.Symbol}'.",
                    nameof(operationContract));
            }

            StageRouteStepDefinition = stageRouteStepDefinition;
            OperationDefinition = operationDefinition;
            OperationImplementationDefinition = operationImplementationDefinition;
            OperationContract = operationContract;
        }

        /// <summary>
        /// Gets the resolved stage route step definition.
        /// </summary>
        public StageRouteStepDefinition StageRouteStepDefinition { get; }

        /// <summary>
        /// Gets the resolved operation definition.
        /// </summary>
        public OperationDefinition OperationDefinition { get; }

        /// <summary>
        /// Gets the selected operation implementation definition.
        /// </summary>
        public OperationImplementationDefinition OperationImplementationDefinition { get; }

        /// <summary>
        /// Gets the resolved operation contract.
        /// </summary>
        public OperationContract OperationContract { get; }

        /// <inheritdoc/>
        public bool Equals(OperationPlanNode? other)
        {
            return other is not null
                && StageRouteStepDefinition == other.StageRouteStepDefinition
                && OperationDefinition == other.OperationDefinition
                && OperationImplementationDefinition == other.OperationImplementationDefinition
                && OperationContract == other.OperationContract;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationPlanNode other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                StageRouteStepDefinition,
                OperationDefinition,
                OperationImplementationDefinition,
                OperationContract);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(OperationPlanNode)}({nameof(StageRouteStepDefinition)}: {StageRouteStepDefinition.Symbol}, {nameof(OperationDefinition)}: {OperationDefinition.Symbol}, {nameof(OperationImplementationDefinition)}: {OperationImplementationDefinition.Symbol})";
        }

        /// <summary>
        /// Determines whether two operation plan nodes are equal.
        /// </summary>
        public static bool operator ==(OperationPlanNode? left, OperationPlanNode? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation plan nodes are not equal.
        /// </summary>
        public static bool operator !=(OperationPlanNode? left, OperationPlanNode? right)
        {
            return !Equals(left, right);
        }
    }
}