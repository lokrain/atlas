#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents an unresolved generation request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation request contains accepted request inputs and symbol-based selections. It does not contain
    /// catalog definitions, resolved plan nodes, execution bindings, job data, native containers, ECS systems,
    /// Unity objects, or runtime execution state.
    /// </para>
    /// <para>
    /// Stage route selections are preserved in request insertion order for deterministic diagnostics. The
    /// generation plan compiler resolves the selected symbols through a generation catalog and determines the
    /// valid managed plan structure.
    /// </para>
    /// <para>
    /// Operation implementation selections are keyed by stage-route-step-definition symbol. The compiler validates
    /// that each selected route step exists, belongs to a selected route, resolves to an operation definition, and
    /// uses an implementation belonging to that operation definition.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationRequest"/> instance is always syntactically valid. Catalog-dependent
    /// semantic validity is established by the generation plan compiler.
    /// </para>
    /// </remarks>
    public sealed class GenerationRequest : IEquatable<GenerationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRequest"/> class.
        /// </summary>
        /// <param name="generationSchemaDefinitionSymbol">The selected generation-schema-definition symbol.</param>
        /// <param name="grid">The accepted generation grid.</param>
        /// <param name="seed">The accepted generation seed.</param>
        /// <param name="stageRouteSelections">The selected stage routes.</param>
        /// <param name="operationImplementationSelections">The selected route-step operation implementations.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationSchemaDefinitionSymbol"/>, <paramref name="grid"/>,
        /// <paramref name="stageRouteSelections"/>, or <paramref name="operationImplementationSelections"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageRouteSelections"/> is empty, when either selection collection contains
        /// null entries, or when duplicate stage, route, or route-step implementation selections are present.
        /// </exception>
        public GenerationRequest(
            Symbol generationSchemaDefinitionSymbol,
            Grid grid,
            Seed seed,
            IEnumerable<StageRouteSelection> stageRouteSelections,
            IEnumerable<OperationImplementationSelection> operationImplementationSelections)
        {
            if (generationSchemaDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(generationSchemaDefinitionSymbol));
            }

            if (grid is null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (stageRouteSelections is null)
            {
                throw new ArgumentNullException(nameof(stageRouteSelections));
            }

            if (operationImplementationSelections is null)
            {
                throw new ArgumentNullException(nameof(operationImplementationSelections));
            }

            StageRouteSelection[] copiedStageRouteSelections =
                CopyStageRouteSelections(stageRouteSelections);

            OperationImplementationSelection[] copiedOperationImplementationSelections =
                CopyOperationImplementationSelections(operationImplementationSelections);

            GenerationSchemaDefinitionSymbol = generationSchemaDefinitionSymbol;
            Grid = grid;
            Seed = seed;
            StageRouteSelections = new ReadOnlyCollection<StageRouteSelection>(
                copiedStageRouteSelections);
            OperationImplementationSelections = new ReadOnlyCollection<OperationImplementationSelection>(
                copiedOperationImplementationSelections);
        }

        /// <summary>
        /// Gets the selected generation-schema-definition symbol.
        /// </summary>
        public Symbol GenerationSchemaDefinitionSymbol { get; }

        /// <summary>
        /// Gets the accepted generation grid.
        /// </summary>
        public Grid Grid { get; }

        /// <summary>
        /// Gets the accepted generation seed.
        /// </summary>
        public Seed Seed { get; }

        /// <summary>
        /// Gets the selected stage routes.
        /// </summary>
        public IReadOnlyList<StageRouteSelection> StageRouteSelections { get; }

        /// <summary>
        /// Gets the selected route-step operation implementations.
        /// </summary>
        public IReadOnlyList<OperationImplementationSelection> OperationImplementationSelections { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationRequest? other)
        {
            return other is not null
                && GenerationSchemaDefinitionSymbol == other.GenerationSchemaDefinitionSymbol
                && Grid == other.Grid
                && Seed == other.Seed
                && SequenceEquals(StageRouteSelections, other.StageRouteSelections)
                && SequenceEquals(OperationImplementationSelections, other.OperationImplementationSelections);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationRequest other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationSchemaDefinitionSymbol);
            hashCode.Add(Grid);
            hashCode.Add(Seed);

            for (int index = 0; index < StageRouteSelections.Count; index++)
            {
                hashCode.Add(StageRouteSelections[index]);
            }

            for (int index = 0; index < OperationImplementationSelections.Count; index++)
            {
                hashCode.Add(OperationImplementationSelections[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationRequest)}({nameof(GenerationSchemaDefinitionSymbol)}: {GenerationSchemaDefinitionSymbol}, {nameof(Grid)}: {Grid}, {nameof(Seed)}: {Seed}, {nameof(StageRouteSelections)}: {StageRouteSelections.Count}, {nameof(OperationImplementationSelections)}: {OperationImplementationSelections.Count})";
        }

        /// <summary>
        /// Determines whether two generation requests are equal.
        /// </summary>
        public static bool operator ==(GenerationRequest? left, GenerationRequest? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation requests are not equal.
        /// </summary>
        public static bool operator !=(GenerationRequest? left, GenerationRequest? right)
        {
            return !Equals(left, right);
        }

        private static StageRouteSelection[] CopyStageRouteSelections(
            IEnumerable<StageRouteSelection> stageRouteSelections)
        {
            var copiedSelections = new List<StageRouteSelection>();
            var selectedStageDefinitionSymbols = new HashSet<Symbol>();
            var selectedStageRouteDefinitionSymbols = new HashSet<Symbol>();

            foreach (StageRouteSelection? selection in stageRouteSelections)
            {
                if (selection is null)
                {
                    throw new ArgumentException(
                        "Stage route selections cannot contain null entries.",
                        nameof(stageRouteSelections));
                }

                if (!selectedStageDefinitionSymbols.Add(selection.StageDefinitionSymbol))
                {
                    throw new ArgumentException(
                        $"Stage route selections cannot contain duplicate stage-definition symbol '{selection.StageDefinitionSymbol}'.",
                        nameof(stageRouteSelections));
                }

                if (!selectedStageRouteDefinitionSymbols.Add(selection.StageRouteDefinitionSymbol))
                {
                    throw new ArgumentException(
                        $"Stage route selections cannot contain duplicate stage-route-definition symbol '{selection.StageRouteDefinitionSymbol}'.",
                        nameof(stageRouteSelections));
                }

                copiedSelections.Add(selection);
            }

            if (copiedSelections.Count == 0)
            {
                throw new ArgumentException(
                    "Generation request must contain at least one stage route selection.",
                    nameof(stageRouteSelections));
            }

            return copiedSelections.ToArray();
        }

        private static OperationImplementationSelection[] CopyOperationImplementationSelections(
            IEnumerable<OperationImplementationSelection> operationImplementationSelections)
        {
            var copiedSelections = new List<OperationImplementationSelection>();
            var selectedStageRouteStepDefinitionSymbols = new HashSet<Symbol>();

            foreach (OperationImplementationSelection? selection in operationImplementationSelections)
            {
                if (selection is null)
                {
                    throw new ArgumentException(
                        "Operation implementation selections cannot contain null entries.",
                        nameof(operationImplementationSelections));
                }

                if (!selectedStageRouteStepDefinitionSymbols.Add(selection.StageRouteStepDefinitionSymbol))
                {
                    throw new ArgumentException(
                        $"Operation implementation selections cannot contain duplicate stage-route-step-definition symbol '{selection.StageRouteStepDefinitionSymbol}'.",
                        nameof(operationImplementationSelections));
                }

                copiedSelections.Add(selection);
            }

            return copiedSelections.ToArray();
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