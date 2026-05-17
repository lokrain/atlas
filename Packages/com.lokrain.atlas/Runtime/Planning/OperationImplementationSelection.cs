#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a request-side selection of an implementation for a stage route step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation implementation selection is unresolved request intent. It stores stable machine-facing
    /// symbols only. It does not contain catalog definitions, resolved plan nodes, execution bindings, job data,
    /// native containers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// The selected route-step-definition symbol identifies which operation occurrence this implementation
    /// selection applies to. The selected operation-implementation-definition symbol identifies the requested
    /// implementation for that route step.
    /// </para>
    /// <para>
    /// The generation plan compiler resolves <see cref="StageRouteStepDefinitionSymbol"/> and
    /// <see cref="OperationImplementationDefinitionSymbol"/> through a generation catalog. It is also responsible
    /// for validating that the selected implementation belongs to the operation definition referenced by the
    /// selected route step.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationImplementationSelection"/> instance is always syntactically valid.
    /// </para>
    /// </remarks>
    public sealed class OperationImplementationSelection : IEquatable<OperationImplementationSelection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationImplementationSelection"/> class.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageRouteStepDefinitionSymbol"/> or
        /// <paramref name="operationImplementationDefinitionSymbol"/> is null.
        /// </exception>
        public OperationImplementationSelection(
            Symbol stageRouteStepDefinitionSymbol,
            Symbol operationImplementationDefinitionSymbol)
        {
            if (stageRouteStepDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(stageRouteStepDefinitionSymbol));
            }

            if (operationImplementationDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(operationImplementationDefinitionSymbol));
            }

            StageRouteStepDefinitionSymbol = stageRouteStepDefinitionSymbol;
            OperationImplementationDefinitionSymbol = operationImplementationDefinitionSymbol;
        }

        /// <summary>
        /// Gets the selected stage-route-step-definition symbol.
        /// </summary>
        public Symbol StageRouteStepDefinitionSymbol { get; }

        /// <summary>
        /// Gets the selected operation-implementation-definition symbol.
        /// </summary>
        public Symbol OperationImplementationDefinitionSymbol { get; }

        /// <summary>
        /// Creates an operation implementation selection from symbol values.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol value.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol value.
        /// </param>
        /// <returns>A validated operation implementation selection.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when either symbol value is null or not a valid symbol.
        /// </exception>
        public static OperationImplementationSelection Create(
            string? stageRouteStepDefinitionSymbol,
            string? operationImplementationDefinitionSymbol)
        {
            return new(
                CreateSymbol(
                    stageRouteStepDefinitionSymbol,
                    nameof(stageRouteStepDefinitionSymbol),
                    "Stage route step definition symbol"),
                CreateSymbol(
                    operationImplementationDefinitionSymbol,
                    nameof(operationImplementationDefinitionSymbol),
                    "Operation implementation definition symbol"));
        }

        /// <summary>
        /// Attempts to create an operation implementation selection from symbol values.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol value.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol value.
        /// </param>
        /// <param name="selection">The created selection when validation succeeds.</param>
        /// <returns>
        /// <see langword="true"/> when both symbol values are valid; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            string? stageRouteStepDefinitionSymbol,
            string? operationImplementationDefinitionSymbol,
            out OperationImplementationSelection? selection)
        {
            if (!Symbol.TryCreate(stageRouteStepDefinitionSymbol, out Symbol? createdStageRouteStepDefinitionSymbol))
            {
                selection = null;
                return false;
            }

            if (!Symbol.TryCreate(
                operationImplementationDefinitionSymbol,
                out Symbol? createdOperationImplementationDefinitionSymbol))
            {
                selection = null;
                return false;
            }

            selection = new(
                createdStageRouteStepDefinitionSymbol!,
                createdOperationImplementationDefinitionSymbol!);

            return true;
        }

        /// <summary>
        /// Determines whether the specified symbol values can create a valid operation implementation selection.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol value.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when both symbol values can create a valid selection; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool IsValid(
            string? stageRouteStepDefinitionSymbol,
            string? operationImplementationDefinitionSymbol)
        {
            return Symbol.IsValid(stageRouteStepDefinitionSymbol)
                && Symbol.IsValid(operationImplementationDefinitionSymbol);
        }

        /// <inheritdoc/>
        public bool Equals(OperationImplementationSelection? other)
        {
            return other is not null
                && StageRouteStepDefinitionSymbol == other.StageRouteStepDefinitionSymbol
                && OperationImplementationDefinitionSymbol == other.OperationImplementationDefinitionSymbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationImplementationSelection other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                StageRouteStepDefinitionSymbol,
                OperationImplementationDefinitionSymbol);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(OperationImplementationSelection)}({nameof(StageRouteStepDefinitionSymbol)}: {StageRouteStepDefinitionSymbol}, {nameof(OperationImplementationDefinitionSymbol)}: {OperationImplementationDefinitionSymbol})";
        }

        /// <summary>
        /// Determines whether two operation implementation selections are equal.
        /// </summary>
        public static bool operator ==(
            OperationImplementationSelection? left,
            OperationImplementationSelection? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation implementation selections are not equal.
        /// </summary>
        public static bool operator !=(
            OperationImplementationSelection? left,
            OperationImplementationSelection? right)
        {
            return !Equals(left, right);
        }

        private static Symbol CreateSymbol(
            string? value,
            string parameterName,
            string description)
        {
            if (!Symbol.TryCreate(value, out Symbol? symbol))
            {
                throw new ArgumentException(
                    $"{description} must be a valid symbol.",
                    parameterName);
            }

            return symbol!;
        }
    }
}