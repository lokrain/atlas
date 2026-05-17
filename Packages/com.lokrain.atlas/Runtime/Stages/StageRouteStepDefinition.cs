#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Defines one ordered operation occurrence within a stage route definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage route step definition identifies one authored operation occurrence inside a route. Multiple route
    /// steps may reference the same operation definition symbol, allowing repeated passes, per-step diagnostics,
    /// per-step settings, and per-step implementation selection.
    /// </para>
    /// <para>
    /// The route step symbol is the stable machine-facing identity for this operation occurrence. The display name
    /// is user-facing metadata only and must not be used for lookup, deterministic generation, catalog resolution,
    /// or artifact compatibility.
    /// </para>
    /// <para>
    /// The operation definition symbol is unresolved catalog intent. The generation catalog validates that it
    /// resolves to an operation definition in the same generation schema as the owning route stage.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageRouteStepDefinition"/> instance is always syntactically valid.
    /// Catalog-dependent semantic validity is established by the generation catalog.
    /// </para>
    /// </remarks>
    public sealed class StageRouteStepDefinition : IEquatable<StageRouteStepDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageRouteStepDefinition"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing route-step symbol.</param>
        /// <param name="displayName">The user-facing route-step display name.</param>
        /// <param name="operationDefinitionSymbol">The referenced operation-definition symbol.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/>, <paramref name="displayName"/>, or
        /// <paramref name="operationDefinitionSymbol"/> is null.
        /// </exception>
        public StageRouteStepDefinition(
            Symbol symbol,
            DisplayName displayName,
            Symbol operationDefinitionSymbol)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (operationDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(operationDefinitionSymbol));
            }

            Symbol = symbol;
            DisplayName = displayName;
            OperationDefinitionSymbol = operationDefinitionSymbol;
        }

        /// <summary>
        /// Gets the stable machine-facing route-step symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing route-step display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <summary>
        /// Gets the referenced operation-definition symbol.
        /// </summary>
        public Symbol OperationDefinitionSymbol { get; }

        /// <inheritdoc/>
        public bool Equals(StageRouteStepDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageRouteStepDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageRouteStepDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(OperationDefinitionSymbol)}: {OperationDefinitionSymbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two stage route step definitions are equal.
        /// </summary>
        public static bool operator ==(StageRouteStepDefinition? left, StageRouteStepDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage route step definitions are not equal.
        /// </summary>
        public static bool operator !=(StageRouteStepDefinition? left, StageRouteStepDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}