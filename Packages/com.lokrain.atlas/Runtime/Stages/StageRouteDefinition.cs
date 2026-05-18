#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Defines an ordered route of operation occurrences for a generation stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage route definition belongs to one stage definition and describes the ordered route steps that
    /// compose that route. It is managed definition metadata only; it does not contain operation implementations,
    /// runtime bindings, execution state, job data, or native containers.
    /// </para>
    /// <para>
    /// Route steps are ordered operation occurrences. Multiple route steps may reference the same operation
    /// definition symbol, allowing repeated passes while preserving stable per-step identity.
    /// </para>
    /// <para>
    /// The route symbol is the stable machine-facing route identity. The display name is user-facing metadata only
    /// and must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because two route definitions with the same symbol represent the
    /// same catalog identity. Stage, step sequence, or metadata conflicts must be handled by catalog validation,
    /// not by treating duplicate symbols as distinct routes.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageRouteDefinition"/> instance is always syntactically valid.
    /// Catalog-dependent semantic validity is established by the generation catalog.
    /// </para>
    /// </remarks>
    public sealed class StageRouteDefinition : IEquatable<StageRouteDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageRouteDefinition"/> class.
        /// </summary>
        /// <param name="stageDefinition">The stage definition that owns this route.</param>
        /// <param name="symbol">The stable machine-facing route symbol.</param>
        /// <param name="displayName">The user-facing route display name.</param>
        /// <param name="stageRouteStepDefinitions">The ordered route-step definitions that compose this route.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageDefinition"/>, <paramref name="symbol"/>,
        /// <paramref name="displayName"/>, or <paramref name="stageRouteStepDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageRouteStepDefinitions"/> is empty, contains null entries,
        /// or contains duplicate route-step symbols.
        /// </exception>
        public StageRouteDefinition(
            StageDefinition stageDefinition,
            Symbol symbol,
            DisplayName displayName,
            IEnumerable<StageRouteStepDefinition> stageRouteStepDefinitions)
        {
            if (stageDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageDefinition));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (stageRouteStepDefinitions is null)
            {
                throw new ArgumentNullException(nameof(stageRouteStepDefinitions));
            }

            StageRouteStepDefinition[] copiedStageRouteStepDefinitions =
                CopyStageRouteStepDefinitions(stageRouteStepDefinitions);

            StageDefinition = stageDefinition;
            Symbol = symbol;
            DisplayName = displayName;
            StageRouteStepDefinitions = new ReadOnlyCollection<StageRouteStepDefinition>(
                copiedStageRouteStepDefinitions);
        }

        /// <summary>
        /// Gets the stage definition that owns this route.
        /// </summary>
        public StageDefinition StageDefinition { get; }

        /// <summary>
        /// Gets the stable machine-facing route symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing route display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <summary>
        /// Gets the ordered route-step definitions that compose this route.
        /// </summary>
        public IReadOnlyList<StageRouteStepDefinition> StageRouteStepDefinitions { get; }

        /// <inheritdoc/>
        public bool Equals(StageRouteDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageRouteDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageRouteDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(StageDefinition)}: {StageDefinition.Symbol}, {nameof(StageRouteStepDefinitions)}: {StageRouteStepDefinitions.Count}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two stage route definitions are equal.
        /// </summary>
        public static bool operator ==(StageRouteDefinition? left, StageRouteDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage route definitions are not equal.
        /// </summary>
        public static bool operator !=(StageRouteDefinition? left, StageRouteDefinition? right)
        {
            return !Equals(left, right);
        }

        private static StageRouteStepDefinition[] CopyStageRouteStepDefinitions(
            IEnumerable<StageRouteStepDefinition> stageRouteStepDefinitions)
        {
            var copiedDefinitions = new List<StageRouteStepDefinition>();
            var uniqueRouteStepSymbols = new HashSet<Symbol>();

            foreach (StageRouteStepDefinition? stageRouteStepDefinition in stageRouteStepDefinitions)
            {
                if (stageRouteStepDefinition is null)
                {
                    throw new ArgumentException(
                        "Stage route step definitions cannot contain null entries.",
                        nameof(stageRouteStepDefinitions));
                }

                if (!uniqueRouteStepSymbols.Add(stageRouteStepDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage route step definitions cannot contain duplicate route-step symbol '{stageRouteStepDefinition.Symbol}'.",
                        nameof(stageRouteStepDefinitions));
                }

                copiedDefinitions.Add(stageRouteStepDefinition);
            }

            if (copiedDefinitions.Count == 0)
            {
                throw new ArgumentException(
                    "Stage route must contain at least one route step definition.",
                    nameof(stageRouteStepDefinitions));
            }

            return copiedDefinitions.ToArray();
        }
    }
}