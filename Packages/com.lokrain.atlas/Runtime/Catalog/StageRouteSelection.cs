#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a request-side selection of a stage definition and one of its route definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage route selection is unresolved request intent. It stores stable machine-facing symbols only.
    /// It does not contain catalog definitions, resolved plan nodes, execution bindings, job data, native
    /// containers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// The generation plan compiler resolves <see cref="StageDefinitionSymbol"/> and
    /// <see cref="StageRouteDefinitionSymbol"/> through a generation catalog. It is also responsible for
    /// validating that the selected route belongs to the selected stage.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageRouteSelection"/> instance is always syntactically valid.
    /// </para>
    /// </remarks>
    public sealed class StageRouteSelection : IEquatable<StageRouteSelection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageRouteSelection"/> class.
        /// </summary>
        /// <param name="stageDefinitionSymbol">The selected stage-definition symbol.</param>
        /// <param name="stageRouteDefinitionSymbol">The selected stage-route-definition symbol.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageDefinitionSymbol"/> or
        /// <paramref name="stageRouteDefinitionSymbol"/> is null.
        /// </exception>
        public StageRouteSelection(
            Symbol stageDefinitionSymbol,
            Symbol stageRouteDefinitionSymbol)
        {
            if (stageDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(stageDefinitionSymbol));
            }

            if (stageRouteDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(stageRouteDefinitionSymbol));
            }

            StageDefinitionSymbol = stageDefinitionSymbol;
            StageRouteDefinitionSymbol = stageRouteDefinitionSymbol;
        }

        /// <summary>
        /// Gets the selected stage-definition symbol.
        /// </summary>
        public Symbol StageDefinitionSymbol { get; }

        /// <summary>
        /// Gets the selected stage-route-definition symbol.
        /// </summary>
        public Symbol StageRouteDefinitionSymbol { get; }

        /// <summary>
        /// Creates a stage route selection from symbol values.
        /// </summary>
        /// <param name="stageDefinitionSymbol">The selected stage-definition symbol value.</param>
        /// <param name="stageRouteDefinitionSymbol">The selected stage-route-definition symbol value.</param>
        /// <returns>A validated stage route selection.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when either symbol value is null or not a valid symbol.
        /// </exception>
        public static StageRouteSelection Create(
            string? stageDefinitionSymbol,
            string? stageRouteDefinitionSymbol)
        {
            return new(
                CreateSymbol(
                    stageDefinitionSymbol,
                    nameof(stageDefinitionSymbol),
                    "Stage definition symbol"),
                CreateSymbol(
                    stageRouteDefinitionSymbol,
                    nameof(stageRouteDefinitionSymbol),
                    "Stage route definition symbol"));
        }

        /// <summary>
        /// Attempts to create a stage route selection from symbol values.
        /// </summary>
        /// <param name="stageDefinitionSymbol">The selected stage-definition symbol value.</param>
        /// <param name="stageRouteDefinitionSymbol">The selected stage-route-definition symbol value.</param>
        /// <param name="selection">The created selection when validation succeeds.</param>
        /// <returns>
        /// <see langword="true"/> when both symbol values are valid; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            string? stageDefinitionSymbol,
            string? stageRouteDefinitionSymbol,
            out StageRouteSelection? selection)
        {
            if (!Symbol.TryCreate(stageDefinitionSymbol, out Symbol? createdStageDefinitionSymbol))
            {
                selection = null;
                return false;
            }

            if (!Symbol.TryCreate(stageRouteDefinitionSymbol, out Symbol? createdStageRouteDefinitionSymbol))
            {
                selection = null;
                return false;
            }

            selection = new(
                createdStageDefinitionSymbol!,
                createdStageRouteDefinitionSymbol!);

            return true;
        }

        /// <summary>
        /// Determines whether the specified symbol values can create a valid stage route selection.
        /// </summary>
        /// <param name="stageDefinitionSymbol">The selected stage-definition symbol value.</param>
        /// <param name="stageRouteDefinitionSymbol">The selected stage-route-definition symbol value.</param>
        /// <returns>
        /// <see langword="true"/> when both symbol values can create a valid selection; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool IsValid(
            string? stageDefinitionSymbol,
            string? stageRouteDefinitionSymbol)
        {
            return Symbol.IsValid(stageDefinitionSymbol)
                && Symbol.IsValid(stageRouteDefinitionSymbol);
        }

        /// <inheritdoc/>
        public bool Equals(StageRouteSelection? other)
        {
            return other is not null
                && StageDefinitionSymbol == other.StageDefinitionSymbol
                && StageRouteDefinitionSymbol == other.StageRouteDefinitionSymbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageRouteSelection other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                StageDefinitionSymbol,
                StageRouteDefinitionSymbol);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageRouteSelection)}({nameof(StageDefinitionSymbol)}: {StageDefinitionSymbol}, {nameof(StageRouteDefinitionSymbol)}: {StageRouteDefinitionSymbol})";
        }

        /// <summary>
        /// Determines whether two stage route selections are equal.
        /// </summary>
        public static bool operator ==(StageRouteSelection? left, StageRouteSelection? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage route selections are not equal.
        /// </summary>
        public static bool operator !=(StageRouteSelection? left, StageRouteSelection? right)
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