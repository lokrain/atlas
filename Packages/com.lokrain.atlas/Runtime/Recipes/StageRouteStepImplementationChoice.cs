#nullable enable

using System;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Recipes
{
    /// <summary>
    /// Represents an accepted implementation choice for one stage route step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage route step implementation choice binds one route-step occurrence to one operation definition,
    /// one operation contract, and one selected operation implementation definition.
    /// </para>
    /// <para>
    /// The route step identifies the operation occurrence within a route. The operation definition identifies
    /// the semantic operation referenced by that occurrence. The operation contract describes the semantic
    /// resource flow for that operation. The implementation definition identifies the selected implementation
    /// option for that operation.
    /// </para>
    /// <para>
    /// This type is managed recipe and request-selection metadata only. It does not contain executable bindings,
    /// scheduler bindings, runtime state, job data, native containers, ECS systems, Burst function pointers, or
    /// Unity runtime objects.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageRouteStepImplementationChoice"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class StageRouteStepImplementationChoice : IEquatable<StageRouteStepImplementationChoice>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageRouteStepImplementationChoice"/> class.
        /// </summary>
        /// <param name="stageRouteStepDefinition">The resolved stage route step definition.</param>
        /// <param name="operationDefinition">The resolved operation definition referenced by the route step.</param>
        /// <param name="operationContract">The resolved operation contract.</param>
        /// <param name="operationImplementationDefinition">The selected operation implementation definition.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the route step, operation definition, operation contract, and implementation definition
        /// do not describe the same operation.
        /// </exception>
        public StageRouteStepImplementationChoice(
            StageRouteStepDefinition stageRouteStepDefinition,
            OperationDefinition operationDefinition,
            OperationContract operationContract,
            OperationImplementationDefinition operationImplementationDefinition)
        {
            if (stageRouteStepDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageRouteStepDefinition));
            }

            if (operationDefinition is null)
            {
                throw new ArgumentNullException(nameof(operationDefinition));
            }

            if (operationContract is null)
            {
                throw new ArgumentNullException(nameof(operationContract));
            }

            if (operationImplementationDefinition is null)
            {
                throw new ArgumentNullException(nameof(operationImplementationDefinition));
            }

            if (stageRouteStepDefinition.OperationDefinitionSymbol != operationDefinition.Symbol)
            {
                throw new ArgumentException(
                    $"Stage route step '{stageRouteStepDefinition.Symbol}' references operation definition symbol '{stageRouteStepDefinition.OperationDefinitionSymbol}', but the resolved operation definition is '{operationDefinition.Symbol}'.",
                    nameof(operationDefinition));
            }

            if (!ReferenceEquals(operationContract.OperationDefinition, operationDefinition))
            {
                throw new ArgumentException(
                    $"Operation contract for operation definition '{operationContract.OperationDefinition.Symbol}' does not belong to operation definition '{operationDefinition.Symbol}'.",
                    nameof(operationContract));
            }

            if (!ReferenceEquals(operationImplementationDefinition.OperationDefinition, operationDefinition))
            {
                throw new ArgumentException(
                    $"Operation implementation definition '{operationImplementationDefinition.Symbol}' does not belong to operation definition '{operationDefinition.Symbol}'.",
                    nameof(operationImplementationDefinition));
            }

            StageRouteStepDefinition = stageRouteStepDefinition;
            OperationDefinition = operationDefinition;
            OperationContract = operationContract;
            OperationImplementationDefinition = operationImplementationDefinition;
        }

        /// <summary>
        /// Gets the resolved stage route step definition.
        /// </summary>
        public StageRouteStepDefinition StageRouteStepDefinition { get; }

        /// <summary>
        /// Gets the resolved operation definition referenced by the route step.
        /// </summary>
        public OperationDefinition OperationDefinition { get; }

        /// <summary>
        /// Gets the resolved operation contract.
        /// </summary>
        public OperationContract OperationContract { get; }

        /// <summary>
        /// Gets the selected operation implementation definition.
        /// </summary>
        public OperationImplementationDefinition OperationImplementationDefinition { get; }

        /// <inheritdoc/>
        public bool Equals(StageRouteStepImplementationChoice? other)
        {
            return other is not null
                && StageRouteStepDefinition == other.StageRouteStepDefinition
                && OperationDefinition == other.OperationDefinition
                && OperationContract == other.OperationContract
                && OperationImplementationDefinition == other.OperationImplementationDefinition;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageRouteStepImplementationChoice other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                StageRouteStepDefinition,
                OperationDefinition,
                OperationContract,
                OperationImplementationDefinition);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageRouteStepImplementationChoice)}({nameof(StageRouteStepDefinition)}: {StageRouteStepDefinition.Symbol}, {nameof(OperationDefinition)}: {OperationDefinition.Symbol}, {nameof(OperationImplementationDefinition)}: {OperationImplementationDefinition.Symbol})";
        }

        /// <summary>
        /// Determines whether two stage route step implementation choices are equal.
        /// </summary>
        public static bool operator ==(
            StageRouteStepImplementationChoice? left,
            StageRouteStepImplementationChoice? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage route step implementation choices are not equal.
        /// </summary>
        public static bool operator !=(
            StageRouteStepImplementationChoice? left,
            StageRouteStepImplementationChoice? right)
        {
            return !Equals(left, right);
        }
    }
}