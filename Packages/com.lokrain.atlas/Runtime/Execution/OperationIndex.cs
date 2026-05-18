#nullable enable

using System;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents a zero-based operation table position inside runnable metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A operation index is a dense, plan-local table index. It is not an operation identity, operation symbol, graph node identity, scheduler handle, ECS binding, or durable identifier.
    /// </para>
    /// <para>
    /// The value <c>0</c> is valid. Atlas does not reserve a sentinel value for missing operation indices.
    /// </para>
    /// <para>
    /// A operation index validates only that its value is non-negative. Table owners validate bounds,
    /// uniqueness, and dense ordering invariants such as <c>Operations[i].OperationIndex.Value == i</c>.
    /// </para>
    /// </remarks>
    public readonly struct OperationIndex : IEquatable<OperationIndex>, IComparable<OperationIndex>, IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationIndex"/> struct.
        /// </summary>
        /// <param name="value">The zero-based operation table position.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="value"/> is negative.
        /// </exception>
        public OperationIndex(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Operation index must be zero or greater.");
            }

            Value = value;
        }

        /// <summary>
        /// Gets the zero-based operation table position.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Attempts to create a operation index.
        /// </summary>
        /// <param name="value">The zero-based operation table position.</param>
        /// <param name="operationIndex">The created operation index when validation succeeds.</param>
        /// <returns><see langword="true"/> when the operation index is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(int value, out OperationIndex operationIndex)
        {
            if (value < 0)
            {
                operationIndex = default;
                return false;
            }

            operationIndex = new OperationIndex(value);
            return true;
        }

        /// <inheritdoc/>
        public int CompareTo(OperationIndex other)
        {
            return Value.CompareTo(other.Value);
        }

        /// <inheritdoc/>
        public int CompareTo(object? obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is OperationIndex other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException(
                $"Object must be of type {nameof(OperationIndex)}.",
                nameof(obj));
        }

        /// <inheritdoc/>
        public bool Equals(OperationIndex other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationIndex other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(OperationIndex)}({Value})";
        }

        /// <summary>
        /// Determines whether two operation indices are equal.
        /// </summary>
        public static bool operator ==(OperationIndex left, OperationIndex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two operation indices are not equal.
        /// </summary>
        public static bool operator !=(OperationIndex left, OperationIndex right)
        {
            return !left.Equals(right);
        }
    }
}
