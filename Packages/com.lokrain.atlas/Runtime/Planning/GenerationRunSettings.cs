#nullable enable

using System;
using Lokrain.Atlas.Core.Map;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents generation-wide settings for one generation run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generation run settings contain validated inputs that apply to the whole generation invocation, not to one
    /// recipe, stage, route, operation, or implementation.
    /// </para>
    /// <para>
    /// The grid defines the spatial domain and memory shape for generated data. The seed defines the root
    /// deterministic input for generation. Recipe-specific algorithm settings belong in separate setting
    /// descriptors later.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationRunSettings"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationRunSettings : IEquatable<GenerationRunSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRunSettings"/> class.
        /// </summary>
        /// <param name="grid">The accepted generation grid.</param>
        /// <param name="seed">The accepted generation seed.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="grid"/> is null.
        /// </exception>
        public GenerationRunSettings(
            Grid grid,
            Seed seed)
        {
            if (grid is null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            Grid = grid;
            Seed = seed;
        } 
        
        /// <summary>
        /// Gets the accepted generation grid.
        /// </summary>
        public Grid Grid { get; }

        /// <summary>
        /// Gets the accepted generation seed.
        /// </summary>
        public Seed Seed { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationRunSettings? other)
        {
            return other is not null
                && Grid == other.Grid
                && Seed == other.Seed;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationRunSettings other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Grid, Seed);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationRunSettings)}({nameof(Grid)}: {Grid}, {nameof(Seed)}: {Seed})";
        }

        /// <summary>
        /// Determines whether two generation run settings are equal.
        /// </summary>
        public static bool operator ==(GenerationRunSettings? left, GenerationRunSettings? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation run settings are not equal.
        /// </summary>
        public static bool operator !=(GenerationRunSettings? left, GenerationRunSettings? right)
        {
            return !Equals(left, right);
        }
    }
}