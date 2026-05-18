#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Non-owning unsafe mutable 2D grid view over row-major unmanaged memory.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <remarks>
    /// <para>
    /// This type does not allocate, dispose, resize, or own memory. The caller must guarantee that the
    /// backing memory remains valid for the full lifetime of the view.
    /// </para>
    /// <para>
    /// Logical storage is row-major. A logical coordinate maps to <c>logicalIndex = y * Width + x</c>.
    /// Backing storage may include row padding. A logical coordinate maps to
    /// <c>backingIndex = y * Stride + x</c>.
    /// </para>
    /// <para>
    /// This is low-level memory infrastructure. The <c>height</c> and <c>y</c> terms describe generic
    /// two-dimensional memory and must not leak into Atlas map-domain APIs that use depth and z.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Width = {" + nameof(Width) + "}, Height = {" + nameof(Height) + "}, Stride = {" + nameof(Stride) + "}")]
    internal unsafe readonly struct UnsafeGridView<T>
        where T : unmanaged
    {
        private const string TypeName = nameof(UnsafeGridView<T>);

        private readonly byte m_IsCreated;

        [NativeDisableUnsafePtrRestriction]
        private readonly T* m_Items;

        private readonly int m_Width;
        private readonly int m_Height;
        private readonly int m_Stride;
        private readonly int m_Length;
        private readonly int m_BackingLength;

        /// <summary>
        /// Initializes a new tightly packed row-major grid view.
        /// </summary>
        /// <param name="items">The pointer to the first backing element.</param>
        /// <param name="width">The logical grid width.</param>
        /// <param name="height">The logical grid height.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="items"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the logical or backing length cannot be represented as an <see cref="int"/>.
        /// </exception>
        public UnsafeGridView(T* items, int width, int height)
            : this(items, width, height, width)
        {
        }

        /// <summary>
        /// Initializes a new row-major grid view with an explicit row stride.
        /// </summary>
        /// <param name="items">The pointer to the first backing element.</param>
        /// <param name="width">The logical grid width.</param>
        /// <param name="height">The logical grid height.</param>
        /// <param name="stride">The backing row stride in elements. Must be greater than or equal to <paramref name="width"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="items"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="width"/>, <paramref name="height"/>, or <paramref name="stride"/> is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the logical or backing length cannot be represented as an <see cref="int"/>.
        /// </exception>
        public UnsafeGridView(T* items, int width, int height, int stride)
        {
            ValidateConstructorArguments(items, width, height, stride);

            m_IsCreated = 1;
            m_Items = items;
            m_Width = width;
            m_Height = height;
            m_Stride = stride;
            m_Length = UnsafeCollectionSize.CheckedGridLength(width, height);
            m_BackingLength = UnsafeCollectionSize.CheckedBackingLength(width, height, stride);
        }

        /// <summary>
        /// Gets a value indicating whether this value was explicitly constructed as a grid view.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsCreated != 0;
        }

        /// <summary>
        /// Gets the logical grid width.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_Width;
            }
        }

        /// <summary>
        /// Gets the logical grid height.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_Height;
            }
        }

        /// <summary>
        /// Gets the backing row stride in elements.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public int Stride
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_Stride;
            }
        }

        /// <summary>
        /// Gets the logical element count.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_Length;
            }
        }

        /// <summary>
        /// Gets the minimum number of backing elements required by this view.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public int BackingLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_BackingLength;
            }
        }

        /// <summary>
        /// Determines whether the specified coordinate is inside the logical grid.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns><see langword="true"/> when the coordinate is inside the grid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int x, int y)
        {
            CheckCreated();
            return (uint)x < (uint)m_Width && (uint)y < (uint)m_Height;
        }

        /// <summary>
        /// Determines whether the specified logical linear index is inside the logical grid.
        /// </summary>
        /// <param name="logicalIndex">The logical linear index.</param>
        /// <returns><see langword="true"/> when the index is inside the grid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsLogicalIndex(int logicalIndex)
        {
            CheckCreated();
            return (uint)logicalIndex < (uint)m_Length;
        }

        /// <summary>
        /// Converts a coordinate into a logical linear index.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The logical index equal to <c>y * Width + x</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinate is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToLogicalIndex(int x, int y)
        {
            CheckCreated();
            CheckCoordinates(x, y, m_Width, m_Height);

            return (y * m_Width) + x;
        }

        /// <summary>
        /// Converts a coordinate into a backing memory index.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The backing index equal to <c>y * Stride + x</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinate is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToBackingIndex(int x, int y)
        {
            CheckCreated();
            CheckCoordinates(x, y, m_Width, m_Height);

            return (y * m_Stride) + x;
        }

        /// <summary>
        /// Converts a logical linear index into coordinates.
        /// </summary>
        /// <param name="logicalIndex">The logical linear index.</param>
        /// <param name="x">The returned x coordinate.</param>
        /// <param name="y">The returned y coordinate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="logicalIndex"/> is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToCoordinates(int logicalIndex, out int x, out int y)
        {
            CheckCreated();
            CheckLogicalIndex(logicalIndex, m_Length);

            y = logicalIndex / m_Width;
            x = logicalIndex - (y * m_Width);
        }

        /// <summary>
        /// Converts a logical linear index into a backing memory index.
        /// </summary>
        /// <param name="logicalIndex">The logical linear index.</param>
        /// <returns>The backing memory index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="logicalIndex"/> is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LogicalToBackingIndex(int logicalIndex)
        {
            ToCoordinates(logicalIndex, out int x, out int y);
            return (y * m_Stride) + x;
        }

        /// <summary>
        /// Reads an element by coordinate.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The element value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinate is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read(int x, int y)
        {
            CheckCreated();
            CheckCoordinates(x, y, m_Width, m_Height);

            return m_Items[(y * m_Stride) + x];
        }

        /// <summary>
        /// Reads an element by logical linear index.
        /// </summary>
        /// <param name="logicalIndex">The logical linear index.</param>
        /// <returns>The element value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="logicalIndex"/> is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadLogical(int logicalIndex)
        {
            int backingIndex = LogicalToBackingIndex(logicalIndex);
            return m_Items[backingIndex];
        }

        /// <summary>
        /// Writes an element by coordinate.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="value">The element value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinate is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int x, int y, T value)
        {
            CheckCreated();
            CheckCoordinates(x, y, m_Width, m_Height);

            m_Items[(y * m_Stride) + x] = value;
        }

        /// <summary>
        /// Writes an element by logical linear index.
        /// </summary>
        /// <param name="logicalIndex">The logical linear index.</param>
        /// <param name="value">The element value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="logicalIndex"/> is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogical(int logicalIndex, T value)
        {
            int backingIndex = LogicalToBackingIndex(logicalIndex);
            m_Items[backingIndex] = value;
        }

        /// <summary>
        /// Gets a mutable reference to an element by coordinate.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>A reference to the element.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the coordinate is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int x, int y)
        {
            CheckCreated();
            CheckCoordinates(x, y, m_Width, m_Height);

            return ref *(m_Items + ((y * m_Stride) + x));
        }

        /// <summary>
        /// Gets a mutable reference to an element by logical linear index.
        /// </summary>
        /// <param name="logicalIndex">The logical linear index.</param>
        /// <returns>A reference to the element.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="logicalIndex"/> is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAtLogical(int logicalIndex)
        {
            int backingIndex = LogicalToBackingIndex(logicalIndex);
            return ref *(m_Items + backingIndex);
        }

        /// <summary>
        /// Gets a non-owning mutable row view.
        /// </summary>
        /// <param name="y">The row index.</param>
        /// <returns>A row view.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="y"/> is outside the logical grid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeGridRow<T> GetRow(int y)
        {
            CheckCreated();
            CheckRowIndex(y, m_Height);

            return new UnsafeGridRow<T>(m_Items + (y * m_Stride), m_Width);
        }

        /// <summary>
        /// Fills every logical grid element with the specified value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public void Fill(T value)
        {
            CheckCreated();

            for (int y = 0; y < m_Height; y++)
            {
                T* row = m_Items + (y * m_Stride);

                for (int x = 0; x < m_Width; x++)
                {
                    row[x] = value;
                }
            }
        }

        /// <summary>
        /// Clears every logical grid element to the default value.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the view is uncreated.
        /// </exception>
        public void Clear()
        {
            Fill(default);
        }

        private static void ValidateConstructorArguments(T* items, int width, int height, int stride)
        {
            if (items == null)
            {
                throw new ArgumentNullException(
                    nameof(items),
                    "UnsafeGridView items pointer must not be null.");
            }

            UnsafeCollectionChecks.CheckPositive(width, nameof(width));
            UnsafeCollectionChecks.CheckPositive(height, nameof(height));

            if (stride < width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stride),
                    stride,
                    "Stride must be greater than or equal to width.");
            }

            UnsafeCollectionSize.CheckedGridLength(width, height);
            UnsafeCollectionSize.CheckedBackingLength(width, height, stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCoordinates(int x, int y, int width, int height)
        {
            if ((uint)x >= (uint)width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    x,
                    "X coordinate is outside the valid range.");
            }

            if ((uint)y >= (uint)height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(y),
                    y,
                    "Y coordinate is outside the valid range.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckLogicalIndex(int logicalIndex, int length)
        {
            UnsafeCollectionChecks.CheckIndexInRange(logicalIndex, length, nameof(logicalIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckRowIndex(int y, int height)
        {
            UnsafeCollectionChecks.CheckIndexInRange(y, height, nameof(y));
        }
    }

    /// <summary>
    /// Non-owning unsafe mutable row view over contiguous unmanaged memory.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <remarks>
    /// This type does not own memory and must not outlive the backing storage that created it.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {" + nameof(Length) + "}")]
    internal unsafe readonly struct UnsafeGridRow<T>
        where T : unmanaged
    {
        private const string TypeName = nameof(UnsafeGridRow<T>);

        private readonly byte m_IsCreated;

        [NativeDisableUnsafePtrRestriction]
        private readonly T* m_Items;

        private readonly int m_Length;

        /// <summary>
        /// Initializes a new non-owning row view.
        /// </summary>
        /// <param name="items">The pointer to the first row element.</param>
        /// <param name="length">The row length.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="items"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="length"/> is less than or equal to zero.
        /// </exception>
        public UnsafeGridRow(T* items, int length)
        {
            ValidateConstructorArguments(items, length);

            m_IsCreated = 1;
            m_Items = items;
            m_Length = length;
        }

        /// <summary>
        /// Gets a value indicating whether this value was explicitly constructed as a row view.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsCreated != 0;
        }

        /// <summary>
        /// Gets the row length.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                return m_Length;
            }
        }

        /// <summary>
        /// Determines whether the specified row-relative index is inside the row.
        /// </summary>
        /// <param name="x">The row-relative index.</param>
        /// <returns><see langword="true"/> when the index is inside the row; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int x)
        {
            CheckCreated();
            return (uint)x < (uint)m_Length;
        }

        /// <summary>
        /// Reads an element by row-relative index.
        /// </summary>
        /// <param name="x">The row-relative index.</param>
        /// <returns>The element value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="x"/> is outside the row.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read(int x)
        {
            CheckCreated();
            CheckIndex(x, m_Length);

            return m_Items[x];
        }

        /// <summary>
        /// Writes an element by row-relative index.
        /// </summary>
        /// <param name="x">The row-relative index.</param>
        /// <param name="value">The element value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="x"/> is outside the row.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int x, T value)
        {
            CheckCreated();
            CheckIndex(x, m_Length);

            m_Items[x] = value;
        }

        /// <summary>
        /// Gets a mutable reference to an element by row-relative index.
        /// </summary>
        /// <param name="x">The row-relative index.</param>
        /// <returns>A reference to the element.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="x"/> is outside the row.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int x)
        {
            CheckCreated();
            CheckIndex(x, m_Length);

            return ref *(m_Items + x);
        }

        /// <summary>
        /// Fills the row with the specified value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        public void Fill(T value)
        {
            CheckCreated();

            for (int x = 0; x < m_Length; x++)
            {
                m_Items[x] = value;
            }
        }

        /// <summary>
        /// Clears the row to default values.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the row is uncreated.
        /// </exception>
        public void Clear()
        {
            Fill(default);
        }

        private static void ValidateConstructorArguments(T* items, int length)
        {
            if (items == null)
            {
                throw new ArgumentNullException(
                    nameof(items),
                    "UnsafeGridRow items pointer must not be null.");
            }

            UnsafeCollectionChecks.CheckPositive(length, nameof(length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIndex(int x, int length)
        {
            UnsafeCollectionChecks.CheckIndexInRange(x, length, nameof(x));
        }
    }
}