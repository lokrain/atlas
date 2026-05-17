#nullable enable

using System;

namespace Lokrain.Atlas.Core.Map
{
    /// <summary>
    /// Represents a validated flattened cell index inside a grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A cell index is a terrain-grid concept owned by <see cref="Grid"/>. It is not a collection index
    /// abstraction and deliberately does not use the name <c>Index</c> to avoid ambiguity with
    /// <see cref="System.Index"/>.
    /// </para>
    /// <para>
    /// Cell indices are created by <see cref="Grid"/> after bounds validation. The default value represents
    /// index <c>0</c>, which is valid for every production grid supported by Atlas.
    /// </para>
    /// </remarks>
    public readonly struct CellIndex : IEquatable<CellIndex>
    {
        internal CellIndex(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the flattened zero-based cell index.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Converts the cell index to its integer value.
        /// </summary>
        /// <param name="cellIndex">The cell index to convert.</param>
        public static explicit operator int(CellIndex cellIndex)
        {
            return cellIndex.Value;
        }

        /// <inheritdoc/>
        public bool Equals(CellIndex other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is CellIndex other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(CellIndex)}({Value})";
        }

        /// <summary>
        /// Determines whether two cell indices are equal.
        /// </summary>
        public static bool operator ==(CellIndex left, CellIndex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two cell indices are not equal.
        /// </summary>
        public static bool operator !=(CellIndex left, CellIndex right)
        {
            return !left.Equals(right);
        }
    }
}