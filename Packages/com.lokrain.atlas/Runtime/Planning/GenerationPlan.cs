#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a compiler-created resolved managed generation plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation plan contains accepted Core inputs and resolved managed plan nodes created by the generation
    /// plan compiler. It does not contain executable bindings, runtime state, job data, native containers,
    /// ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Stage plan node order is compiler-created managed execution order. The order must satisfy stage contract
    /// dependencies: every required stage input must be available from a previous stage output.
    /// </para>
    /// <para>
    /// Runnable-plan compilation later converts this managed plan into execution-ready metadata and resolved
    /// runtime bindings. Jobs later receive only raw unmanaged data and native containers.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationPlan"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationPlan : IEquatable<GenerationPlan>
    {
        internal GenerationPlan(
            GenerationSchemaDefinition generationSchemaDefinition,
            Grid grid,
            Seed seed,
            IEnumerable<StagePlanNode> stagePlanNodes)
        {
            if (generationSchemaDefinition is null)
            {
                throw new ArgumentNullException(nameof(generationSchemaDefinition));
            }

            if (grid is null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (stagePlanNodes is null)
            {
                throw new ArgumentNullException(nameof(stagePlanNodes));
            }

            StagePlanNode[] copiedStagePlanNodes = CopyStagePlanNodes(
                generationSchemaDefinition,
                stagePlanNodes);

            ValidateStagePlanNodeOrder(copiedStagePlanNodes);

            GenerationSchemaDefinition = generationSchemaDefinition;
            Grid = grid;
            Seed = seed;
            StagePlanNodes = new ReadOnlyCollection<StagePlanNode>(copiedStagePlanNodes);
        }

        /// <summary>
        /// Gets the resolved generation schema definition.
        /// </summary>
        public GenerationSchemaDefinition GenerationSchemaDefinition { get; }

        /// <summary>
        /// Gets the accepted generation grid.
        /// </summary>
        public Grid Grid { get; }

        /// <summary>
        /// Gets the accepted generation seed.
        /// </summary>
        public Seed Seed { get; }

        /// <summary>
        /// Gets the resolved managed stage plan nodes in compiler-created managed execution order.
        /// </summary>
        public IReadOnlyList<StagePlanNode> StagePlanNodes { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationPlan? other)
        {
            return other is not null
                && GenerationSchemaDefinition == other.GenerationSchemaDefinition
                && Grid == other.Grid
                && Seed == other.Seed
                && SequenceEquals(StagePlanNodes, other.StagePlanNodes);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationPlan other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationSchemaDefinition);
            hashCode.Add(Grid);
            hashCode.Add(Seed);

            for (int index = 0; index < StagePlanNodes.Count; index++)
            {
                hashCode.Add(StagePlanNodes[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationPlan)}({nameof(GenerationSchemaDefinition)}: {GenerationSchemaDefinition.Symbol}, {nameof(Grid)}: {Grid}, {nameof(Seed)}: {Seed}, {nameof(StagePlanNodes)}: {StagePlanNodes.Count})";
        }

        /// <summary>
        /// Determines whether two generation plans are equal.
        /// </summary>
        public static bool operator ==(GenerationPlan? left, GenerationPlan? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation plans are not equal.
        /// </summary>
        public static bool operator !=(GenerationPlan? left, GenerationPlan? right)
        {
            return !Equals(left, right);
        }

        private static StagePlanNode[] CopyStagePlanNodes(
            GenerationSchemaDefinition generationSchemaDefinition,
            IEnumerable<StagePlanNode> stagePlanNodes)
        {
            var copiedStagePlanNodes = new List<StagePlanNode>();
            var stageDefinitionSymbols = new HashSet<Symbol>();
            var stageRouteDefinitionSymbols = new HashSet<Symbol>();

            foreach (StagePlanNode? stagePlanNode in stagePlanNodes)
            {
                if (stagePlanNode is null)
                {
                    throw new ArgumentException(
                        "Stage plan nodes cannot contain null entries.",
                        nameof(stagePlanNodes));
                }

                if (!ReferenceEquals(
                    stagePlanNode.StageDefinition.GenerationSchema,
                    generationSchemaDefinition))
                {
                    throw new ArgumentException(
                        $"Stage plan node for stage definition '{stagePlanNode.StageDefinition.Symbol}' belongs to generation schema '{stagePlanNode.StageDefinition.GenerationSchema.Symbol}', but the generation plan belongs to generation schema '{generationSchemaDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!stageDefinitionSymbols.Add(stagePlanNode.StageDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage plan nodes cannot contain duplicate stage-definition symbol '{stagePlanNode.StageDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                if (!stageRouteDefinitionSymbols.Add(stagePlanNode.StageRouteDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage plan nodes cannot contain duplicate stage-route-definition symbol '{stagePlanNode.StageRouteDefinition.Symbol}'.",
                        nameof(stagePlanNodes));
                }

                copiedStagePlanNodes.Add(stagePlanNode);
            }

            if (copiedStagePlanNodes.Count == 0)
            {
                throw new ArgumentException(
                    "Generation plan must contain at least one stage plan node.",
                    nameof(stagePlanNodes));
            }

            return copiedStagePlanNodes.ToArray();
        }

        private static void ValidateStagePlanNodeOrder(
            IReadOnlyList<StagePlanNode> stagePlanNodes)
        {
            var availableSymbols = new HashSet<Symbol>();

            for (int index = 0; index < stagePlanNodes.Count; index++)
            {
                StagePlanNode stagePlanNode = stagePlanNodes[index];

                foreach (Symbol requiredInputSymbol in stagePlanNode.StageContract.RequiredInputSymbols)
                {
                    if (!availableSymbols.Contains(requiredInputSymbol))
                    {
                        throw new ArgumentException(
                            $"Stage plan node at index {index} for stage definition '{stagePlanNode.StageDefinition.Symbol}' requires input symbol '{requiredInputSymbol}', but that symbol is not available from previous stage outputs.",
                            nameof(stagePlanNodes));
                    }
                }

                foreach (Symbol producedOutputSymbol in stagePlanNode.StageContract.ProducedOutputSymbols)
                {
                    availableSymbols.Add(producedOutputSymbol);
                }
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
    }
}