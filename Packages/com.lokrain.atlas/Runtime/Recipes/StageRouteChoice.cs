#nullable enable

using System;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Recipes
{
    /// <summary>
    /// Represents a resolved stage route choice inside a generation recipe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage route choice binds one stage definition to one selected route definition and its stage contract.
    /// It is recipe metadata only; it does not contain executable bindings, runtime state, job data, native
    /// containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// The stage route definition identifies the selected route for the stage. The stage contract describes what
    /// the stage requires and produces at the planning boundary.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageRouteChoice"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class StageRouteChoice : IEquatable<StageRouteChoice>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageRouteChoice"/> class.
        /// </summary>
        /// <param name="stageDefinition">The resolved stage definition.</param>
        /// <param name="stageRouteDefinition">The selected stage route definition.</param>
        /// <param name="stageContract">The resolved stage contract.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the route definition or contract does not belong to the specified stage definition.
        /// </exception>
        public StageRouteChoice(
            StageDefinition stageDefinition,
            StageRouteDefinition stageRouteDefinition,
            StageContract stageContract)
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

            StageDefinition = stageDefinition;
            StageRouteDefinition = stageRouteDefinition;
            StageContract = stageContract;
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

        /// <inheritdoc/>
        public bool Equals(StageRouteChoice? other)
        {
            return other is not null
                && StageDefinition == other.StageDefinition
                && StageRouteDefinition == other.StageRouteDefinition
                && StageContract == other.StageContract;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageRouteChoice other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                StageDefinition,
                StageRouteDefinition,
                StageContract);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageRouteChoice)}({nameof(StageDefinition)}: {StageDefinition.Symbol}, {nameof(StageRouteDefinition)}: {StageRouteDefinition.Symbol})";
        }

        /// <summary>
        /// Determines whether two stage route choices are equal.
        /// </summary>
        public static bool operator ==(StageRouteChoice? left, StageRouteChoice? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage route choices are not equal.
        /// </summary>
        public static bool operator !=(StageRouteChoice? left, StageRouteChoice? right)
        {
            return !Equals(left, right);
        }
    }
}