#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Defines managed execution-policy identity for runnable generation compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An execution profile is managed metadata that identifies a reusable execution-policy variant. It can be
    /// used by runnable compilation to select storage policy, capture policy, diagnostic policy, scheduler policy,
    /// and implementation variants.
    /// </para>
    /// <para>
    /// This type does not allocate storage, own native memory, schedule jobs, bind ECS data, capture artifacts,
    /// or describe executable operation data.
    /// </para>
    /// <para>
    /// The profile symbol is stable machine-facing identity. The display name is user-facing metadata only and
    /// must not be used for lookup compatibility outside explicit symbol-based profile selection.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because an execution profile has one policy identity. Policy
    /// conflicts must be handled by profile-set, runnable-compiler, or catalog-adjacent validation, not by treating
    /// duplicate symbols as distinct execution profiles.
    /// </para>
    /// <para>
    /// A non-null <see cref="ExecutionProfile"/> instance is structurally valid. Execution support is established
    /// by runnable compilation.
    /// </para>
    /// </remarks>
    public sealed class ExecutionProfile : IEquatable<ExecutionProfile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfile"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing profile symbol.</param>
        /// <param name="displayName">The user-facing profile display name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/> or <paramref name="displayName"/> is null.
        /// </exception>
        public ExecutionProfile(
            Symbol symbol,
            DisplayName displayName)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            Symbol = symbol;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the stable machine-facing profile symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing profile display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <inheritdoc/>
        public bool Equals(ExecutionProfile? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ExecutionProfile other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(ExecutionProfile)}({nameof(Symbol)}: {Symbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two execution profiles are equal.
        /// </summary>
        public static bool operator ==(ExecutionProfile? left, ExecutionProfile? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two execution profiles are not equal.
        /// </summary>
        public static bool operator !=(ExecutionProfile? left, ExecutionProfile? right)
        {
            return !Equals(left, right);
        }
    }
}
