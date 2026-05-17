#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Describes a symbol-based operation implementation override for one recipe route step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation implementation override descriptor is unresolved symbolic input. It is used by request
    /// authoring, editor tooling, importers, and higher-level APIs to override the default implementation selected
    /// by a generation recipe.
    /// </para>
    /// <para>
    /// The route-step symbol identifies the operation occurrence being overridden. The implementation symbol
    /// identifies the requested implementation for that occurrence.
    /// </para>
    /// <para>
    /// This descriptor does not contain catalog definitions, accepted request nodes, resolved plan nodes,
    /// executable bindings, runtime state, job data, native containers, ECS systems, Burst function pointers, or
    /// Unity runtime objects.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationImplementationOverrideDescriptor"/> instance is always a valid symbolic
    /// descriptor. Catalog-dependent satisfiability is established by a request resolver.
    /// </para>
    /// </remarks>
    public sealed class OperationImplementationOverrideDescriptor :
        IEquatable<OperationImplementationOverrideDescriptor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationImplementationOverrideDescriptor"/> class.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageRouteStepDefinitionSymbol"/> or
        /// <paramref name="operationImplementationDefinitionSymbol"/> is null.
        /// </exception>
        public OperationImplementationOverrideDescriptor(
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
        /// Creates an operation implementation override descriptor from symbol values.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol value.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol value.
        /// </param>
        /// <returns>A validated operation implementation override descriptor.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when either symbol value is null or not a valid symbol.
        /// </exception>
        public static OperationImplementationOverrideDescriptor Create(
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
        /// Attempts to create an operation implementation override descriptor from symbol values.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol value.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol value.
        /// </param>
        /// <param name="descriptor">The created descriptor when validation succeeds.</param>
        /// <returns>
        /// <see langword="true"/> when both symbol values are valid; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            string? stageRouteStepDefinitionSymbol,
            string? operationImplementationDefinitionSymbol,
            out OperationImplementationOverrideDescriptor? descriptor)
        {
            if (!Symbol.TryCreate(stageRouteStepDefinitionSymbol, out Symbol? createdStageRouteStepDefinitionSymbol))
            {
                descriptor = null;
                return false;
            }

            if (!Symbol.TryCreate(
                operationImplementationDefinitionSymbol,
                out Symbol? createdOperationImplementationDefinitionSymbol))
            {
                descriptor = null;
                return false;
            }

            descriptor = new(
                createdStageRouteStepDefinitionSymbol!,
                createdOperationImplementationDefinitionSymbol!);

            return true;
        }

        /// <summary>
        /// Determines whether the specified symbol values can create a valid operation implementation override
        /// descriptor.
        /// </summary>
        /// <param name="stageRouteStepDefinitionSymbol">The selected stage-route-step-definition symbol value.</param>
        /// <param name="operationImplementationDefinitionSymbol">
        /// The selected operation-implementation-definition symbol value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when both symbol values can create a valid descriptor; otherwise,
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
        public bool Equals(OperationImplementationOverrideDescriptor? other)
        {
            return other is not null
                && StageRouteStepDefinitionSymbol == other.StageRouteStepDefinitionSymbol
                && OperationImplementationDefinitionSymbol == other.OperationImplementationDefinitionSymbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationImplementationOverrideDescriptor other && Equals(other);
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
            return $"{nameof(OperationImplementationOverrideDescriptor)}({nameof(StageRouteStepDefinitionSymbol)}: {StageRouteStepDefinitionSymbol}, {nameof(OperationImplementationDefinitionSymbol)}: {OperationImplementationDefinitionSymbol})";
        }

        /// <summary>
        /// Determines whether two operation implementation override descriptors are equal.
        /// </summary>
        public static bool operator ==(
            OperationImplementationOverrideDescriptor? left,
            OperationImplementationOverrideDescriptor? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation implementation override descriptors are not equal.
        /// </summary>
        public static bool operator !=(
            OperationImplementationOverrideDescriptor? left,
            OperationImplementationOverrideDescriptor? right)
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