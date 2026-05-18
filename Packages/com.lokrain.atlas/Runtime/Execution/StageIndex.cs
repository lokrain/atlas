#nullable enable

using System;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents a zero-based stage table position inside runnable metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage index is a dense, plan-local table index. It is not a stage identity, authored route-step identity, scheduler node identity, ECS binding, or durable identifier.
    /// </para>
    /// <para>
    /// The value <c>0</c> is valid. Atlas does not reserve a sentinel value for missing stage indices.
    /// </para>
    /// <para>
    /// A stage index validates only that its value is non-negative. Table owners validate bounds,
    /// uniqueness, and dense ordering invariants such as <c>Stages[i].StageIndex.Value == i</c>.
    /// </para>
    /// </remarks>
    public readonly struct StageIndex : IEquatable<StageIndex>, IComparable<StageIndex>, IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageIndex"/> struct.
        /// </summary>
        /// <param name="value">The zero-based stage table position.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="value"/> is negative.
        /// </exception>
        public StageIndex(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Stage index must be zero or greater.");
            }

            Value = value;
        }

        /// <summary>
        /// Gets the zero-based stage table position.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Attempts to create a stage index.
        /// </summary>
        /// <param name="value">The zero-based stage table position.</param>
        /// <param name="stageIndex">The created stage index when validation succeeds.</param>
        /// <returns><see langword="true"/> when the stage index is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(int value, out StageIndex stageIndex)
        {
            if (value < 0)
            {
                stageIndex = default;
                return false;
            }

            stageIndex = new StageIndex(value);
            return true;
        }

        /// <inheritdoc/>
        public int CompareTo(StageIndex other)
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

            if (obj is StageIndex other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException(
                $"Object must be of type {nameof(StageIndex)}.",
                nameof(obj));
        }

        /// <inheritdoc/>
        public bool Equals(StageIndex other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageIndex other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageIndex)}({Value})";
        }

        /// <summary>
        /// Determines whether two stage indices are equal.
        /// </summary>
        public static bool operator ==(StageIndex left, StageIndex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two stage indices are not equal.
        /// </summary>
        public static bool operator !=(StageIndex left, StageIndex right)
        {
            return !left.Equals(right);
        }
    }
}
