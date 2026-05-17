#nullable enable

using System;

namespace Lokrain.Atlas.Core.Map
{
    /// <summary>
    /// Represents validated terrain-grid dimensions and owns coordinate/index conversion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A grid uses <see cref="Width"/> for the horizontal X dimension and <see cref="Depth"/> for the
    /// horizontal Z dimension. The term height is intentionally avoided because Atlas uses height for elevation.
    /// </para>
    /// <para>
    /// A non-null <see cref="Grid"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class Grid : IEquatable<Grid>
    {
        /// <summary>
        /// The minimum supported grid width.
        /// </summary>
        public const int MinWidth = 256;

        /// <summary>
        /// The maximum supported grid width.
        /// </summary>
        public const int MaxWidth = 4096;

        /// <summary>
        /// The minimum supported grid depth.
        /// </summary>
        public const int MinDepth = 256;

        /// <summary>
        /// The maximum supported grid depth.
        /// </summary>
        public const int MaxDepth = 4096;

        /// <summary>
        /// Initializes a new instance of the <see cref="Grid"/> class.
        /// </summary>
        /// <param name="width">The grid width along the X axis.</param>
        /// <param name="depth">The grid depth along the Z axis.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="width"/> or <paramref name="depth"/> is outside the supported range.
        /// </exception>
        public Grid(int width, int depth)
        {
            ValidateWidth(width);
            ValidateDepth(depth);

            Width = width;
            Depth = depth;
            CellCount = width * depth;
            LastIndexValue = CellCount - 1;
        }

        /// <summary>
        /// Gets the grid width along the X axis.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the grid depth along the Z axis.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Gets the total number of cells in the grid.
        /// </summary>
        public int CellCount { get; }

        /// <summary>
        /// Gets the last valid flattened zero-based cell index.
        /// </summary>
        public int LastIndexValue { get; }

        /// <summary>
        /// Gets a validated cell for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <returns>The validated cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinates are outside the grid.
        /// </exception>
        public Cell this[int x, int z] => GetCell(x, z);

        /// <summary>
        /// Gets the cell represented by the specified flattened index.
        /// </summary>
        /// <param name="index">The flattened zero-based cell index.</param>
        /// <returns>The validated cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the grid.
        /// </exception>
        public Cell this[int index] => GetCell(index);

        /// <summary>
        /// Gets the cell represented by the specified cell index.
        /// </summary>
        /// <param name="index">The cell index.</param>
        /// <returns>The validated cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the grid.
        /// </exception>
        public Cell this[CellIndex index] => GetCell(index);

        /// <summary>
        /// Determines whether the specified coordinates are inside the grid.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <returns><see langword="true"/> when the coordinates are inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool Contains(int x, int z)
        {
            return x >= 0 && x < Width && z >= 0 && z < Depth;
        }

        /// <summary>
        /// Determines whether the specified cell is inside the grid.
        /// </summary>
        /// <param name="cell">The cell to validate.</param>
        /// <returns><see langword="true"/> when the cell is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool Contains(Cell cell)
        {
            return Contains(cell.X, cell.Z);
        }

        /// <summary>
        /// Determines whether the specified flattened index is inside the grid.
        /// </summary>
        /// <param name="index">The flattened zero-based cell index.</param>
        /// <returns><see langword="true"/> when the index is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool Contains(int index)
        {
            return index >= 0 && index < CellCount;
        }

        /// <summary>
        /// Determines whether the specified cell index is inside the grid.
        /// </summary>
        /// <param name="index">The cell index to validate.</param>
        /// <returns><see langword="true"/> when the cell index is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool Contains(CellIndex index)
        {
            return Contains(index.Value);
        }

        /// <summary>
        /// Gets a validated cell for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <returns>The validated cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinates are outside the grid.
        /// </exception>
        public Cell GetCell(int x, int z)
        {
            ValidateCoordinates(x, z);
            return new Cell(x, z);
        }

        /// <summary>
        /// Attempts to get a validated cell for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <param name="cell">The validated cell when the coordinates are inside the grid.</param>
        /// <returns><see langword="true"/> when the coordinates are inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetCell(int x, int z, out Cell cell)
        {
            if (!Contains(x, z))
            {
                cell = default;
                return false;
            }

            cell = new Cell(x, z);
            return true;
        }

        /// <summary>
        /// Gets the cell represented by the specified flattened index.
        /// </summary>
        /// <param name="index">The flattened zero-based cell index.</param>
        /// <returns>The validated cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the grid.
        /// </exception>
        public Cell GetCell(int index)
        {
            ValidateIndex(index);
            GetCellComponentsUnchecked(index, out int x, out int z);
            return new Cell(x, z);
        }

        /// <summary>
        /// Gets the cell represented by the specified cell index.
        /// </summary>
        /// <param name="index">The cell index.</param>
        /// <returns>The validated cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the grid.
        /// </exception>
        public Cell GetCell(CellIndex index)
        {
            return GetCell(index.Value);
        }

        /// <summary>
        /// Attempts to get the cell represented by the specified flattened index.
        /// </summary>
        /// <param name="index">The flattened zero-based cell index.</param>
        /// <param name="cell">The validated cell when the index is inside the grid.</param>
        /// <returns><see langword="true"/> when the index is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetCell(int index, out Cell cell)
        {
            if (!Contains(index))
            {
                cell = default;
                return false;
            }

            GetCellComponentsUnchecked(index, out int x, out int z);
            cell = new Cell(x, z);
            return true;
        }

        /// <summary>
        /// Attempts to get the cell represented by the specified cell index.
        /// </summary>
        /// <param name="index">The cell index.</param>
        /// <param name="cell">The validated cell when the index is inside the grid.</param>
        /// <returns><see langword="true"/> when the index is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetCell(CellIndex index, out Cell cell)
        {
            return TryGetCell(index.Value, out cell);
        }

        /// <summary>
        /// Gets the validated cell index for the specified cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <returns>The validated cell index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cell"/> is outside the grid.
        /// </exception>
        public CellIndex GetIndex(Cell cell)
        {
            return GetIndex(cell.X, cell.Z);
        }

        /// <summary>
        /// Gets the validated cell index for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <returns>The validated cell index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinates are outside the grid.
        /// </exception>
        public CellIndex GetIndex(int x, int z)
        {
            ValidateCoordinates(x, z);
            return new CellIndex(GetIndexValueUnchecked(x, z));
        }

        /// <summary>
        /// Attempts to get the validated cell index for the specified cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <param name="index">The validated cell index when the cell is inside the grid.</param>
        /// <returns><see langword="true"/> when the cell is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetIndex(Cell cell, out CellIndex index)
        {
            return TryGetIndex(cell.X, cell.Z, out index);
        }

        /// <summary>
        /// Attempts to get the validated cell index for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <param name="index">The validated cell index when the coordinates are inside the grid.</param>
        /// <returns><see langword="true"/> when the coordinates are inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetIndex(int x, int z, out CellIndex index)
        {
            if (!Contains(x, z))
            {
                index = default;
                return false;
            }

            index = new CellIndex(GetIndexValueUnchecked(x, z));
            return true;
        }

        /// <summary>
        /// Gets the flattened index value for the specified cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <returns>The flattened zero-based cell index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cell"/> is outside the grid.
        /// </exception>
        public int GetIndexValue(Cell cell)
        {
            return GetIndexValue(cell.X, cell.Z);
        }

        /// <summary>
        /// Gets the flattened index value for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <returns>The flattened zero-based cell index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinates are outside the grid.
        /// </exception>
        public int GetIndexValue(int x, int z)
        {
            ValidateCoordinates(x, z);
            return GetIndexValueUnchecked(x, z);
        }

        /// <summary>
        /// Attempts to get the flattened index value for the specified cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <param name="index">The flattened zero-based cell index when the cell is inside the grid.</param>
        /// <returns><see langword="true"/> when the cell is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetIndexValue(Cell cell, out int index)
        {
            return TryGetIndexValue(cell.X, cell.Z, out index);
        }

        /// <summary>
        /// Attempts to get the flattened index value for the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <param name="index">The flattened zero-based cell index when the coordinates are inside the grid.</param>
        /// <returns><see langword="true"/> when the coordinates are inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetIndexValue(int x, int z, out int index)
        {
            if (!Contains(x, z))
            {
                index = default;
                return false;
            }

            index = GetIndexValueUnchecked(x, z);
            return true;
        }

        /// <summary>
        /// Gets the coordinate components represented by the specified flattened index.
        /// </summary>
        /// <param name="index">The flattened zero-based cell index.</param>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the grid.
        /// </exception>
        public void GetCellComponents(int index, out int x, out int z)
        {
            ValidateIndex(index);
            GetCellComponentsUnchecked(index, out x, out z);
        }

        /// <summary>
        /// Gets the coordinate components represented by the specified cell index.
        /// </summary>
        /// <param name="index">The cell index.</param>
        /// <param name="x">The horizontal X coordinate.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the grid.
        /// </exception>
        public void GetCellComponents(CellIndex index, out int x, out int z)
        {
            GetCellComponents(index.Value, out x, out z);
        }

        /// <summary>
        /// Attempts to get the coordinate components represented by the specified flattened index.
        /// </summary>
        /// <param name="index">The flattened zero-based cell index.</param>
        /// <param name="x">The horizontal X coordinate when the index is inside the grid.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate when the index is inside the grid.</param>
        /// <returns><see langword="true"/> when the index is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetCellComponents(int index, out int x, out int z)
        {
            if (!Contains(index))
            {
                x = default;
                z = default;
                return false;
            }

            GetCellComponentsUnchecked(index, out x, out z);
            return true;
        }

        /// <summary>
        /// Attempts to get the coordinate components represented by the specified cell index.
        /// </summary>
        /// <param name="index">The cell index.</param>
        /// <param name="x">The horizontal X coordinate when the index is inside the grid.</param>
        /// <param name="z">The horizontal depth-axis Z coordinate when the index is inside the grid.</param>
        /// <returns><see langword="true"/> when the index is inside the grid; otherwise, <see langword="false"/>.</returns>
        public bool TryGetCellComponents(CellIndex index, out int x, out int z)
        {
            return TryGetCellComponents(index.Value, out x, out z);
        }

        /// <inheritdoc/>
        public bool Equals(Grid? other)
        {
            return other is not null && Width == other.Width && Depth == other.Depth;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Grid other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Depth);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(Grid)}({nameof(Width)}: {Width}, {nameof(Depth)}: {Depth})";
        }

        /// <summary>
        /// Determines whether two grids are equal.
        /// </summary>
        public static bool operator ==(Grid? left, Grid? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two grids are not equal.
        /// </summary>
        public static bool operator !=(Grid? left, Grid? right)
        {
            return !Equals(left, right);
        }

        private static void ValidateWidth(int width)
        {
            if (width < MinWidth || width > MaxWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    $"Grid width must be between {MinWidth} and {MaxWidth}.");
            }
        }

        private static void ValidateDepth(int depth)
        {
            if (depth < MinDepth || depth > MaxDepth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth),
                    depth,
                    $"Grid depth must be between {MinDepth} and {MaxDepth}.");
            }
        }

        private void ValidateCoordinates(int x, int z)
        {
            if (x < 0 || x >= Width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    x,
                    $"Cell X coordinate must be between 0 and {Width - 1} for grid {Width}x{Depth}.");
            }

            if (z < 0 || z >= Depth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(z),
                    z,
                    $"Cell Z coordinate must be between 0 and {Depth - 1} for grid {Width}x{Depth}.");
            }
        }

        private void ValidateIndex(int index)
        {
            if (!Contains(index))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"Cell index must be between 0 and {LastIndexValue} for grid {Width}x{Depth}.");
            }
        }

        private int GetIndexValueUnchecked(int x, int z)
        {
            return z * Width + x;
        }

        private void GetCellComponentsUnchecked(int index, out int x, out int z)
        {
            x = index % Width;
            z = index / Width;
        }
    }
}