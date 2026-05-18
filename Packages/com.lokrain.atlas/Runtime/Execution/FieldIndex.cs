#nullable enable

using System;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents a zero-based field-binding table position inside runnable metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field index is a dense, plan-local table index. It is not a resource identity, field-definition identity, storage handle, artifact identity, ECS binding, or durable identifier.
    /// </para>
    /// <para>
    /// The value <c>0</c> is valid. Atlas does not reserve a sentinel value for missing field indices.
    /// </para>
    /// <para>
    /// A field index validates only that its value is non-negative. Table owners validate bounds,
    /// uniqueness, and dense ordering invariants such as <c>FieldBindings[i].FieldIndex.Value == i</c>.
    /// </para>
    /// </remarks>
    public readonly struct FieldIndex : IEquatable<FieldIndex>, IComparable<FieldIndex>, IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldIndex"/> struct.
        /// </summary>
        /// <param name="value">The zero-based field-binding table position.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="value"/> is negative.
        /// </exception>
        public FieldIndex(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Field index must be zero or greater.");
            }

            Value = value;
        }

        /// <summary>
        /// Gets the zero-based field-binding table position.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Attempts to create a field index.
        /// </summary>
        /// <param name="value">The zero-based field-binding table position.</param>
        /// <param name="fieldIndex">The created field index when validation succeeds.</param>
        /// <returns><see langword="true"/> when the field index is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(int value, out FieldIndex fieldIndex)
        {
            if (value < 0)
            {
                fieldIndex = default;
                return false;
            }

            fieldIndex = new FieldIndex(value);
            return true;
        }

        /// <inheritdoc/>
        public int CompareTo(FieldIndex other)
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

            if (obj is FieldIndex other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException(
                $"Object must be of type {nameof(FieldIndex)}.",
                nameof(obj));
        }

        /// <inheritdoc/>
        public bool Equals(FieldIndex other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is FieldIndex other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(FieldIndex)}({Value})";
        }

        /// <summary>
        /// Determines whether two field indices are equal.
        /// </summary>
        public static bool operator ==(FieldIndex left, FieldIndex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two field indices are not equal.
        /// </summary>
        public static bool operator !=(FieldIndex left, FieldIndex right)
        {
            return !left.Equals(right);
        }
    }
}
