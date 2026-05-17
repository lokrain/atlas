#nullable enable

using System;

namespace Lokrain.Atlas.Core.Map
{
    /// <summary>
    /// Represents a validated logical cell position inside a grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A cell is a terrain-grid concept using horizontal <see cref="X"/> and depth-axis <see cref="Z"/>
    /// coordinates. It does not represent elevation.
    /// </para>
    /// <para>
    /// Cells are created by <see cref="Grid"/> after bounds validation. The default value represents
    /// cell <c>(0, 0)</c>, which is valid for every production grid supported by Atlas.
    /// </para>
    /// </remarks>
    public readonly struct Cell : IEquatable<Cell>
    {
        internal Cell(int x, int z)
        {
            X = x;
            Z = z;
        }

        /// <summary>
        /// Gets the horizontal X coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the horizontal depth-axis Z coordinate.
        /// </summary>
        public int Z { get; }

        /// <summary>
        /// Deconstructs the cell into its coordinate components.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        public void Deconstruct(out int x, out int z)
        {
            x = X;
            z = Z;
        }

        /// <inheritdoc/>
        public bool Equals(Cell other)
        {
            return X == other.X && Z == other.Z;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Cell other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(Cell)}({X}, {Z})";
        }

        /// <summary>
        /// Determines whether two cells are equal.
        /// </summary>
        public static bool operator ==(Cell left, Cell right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two cells are not equal.
        /// </summary>
        public static bool operator !=(Cell left, Cell right)
        {
            return !left.Equals(right);
        }
    }
}