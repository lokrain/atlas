#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Identifies a category of generation stage within a generation catalog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage kind is a domain-specific symbolic identity used to group compatible stage definitions,
    /// route definitions, request selections, and resolved plan nodes.
    /// </para>
    /// <para>
    /// The wrapped symbol is the stable machine-facing value. Stage kind names are not display names,
    /// numeric runtime identifiers, execution handles, or serialized job data.
    /// </para>
    /// <para>
    /// A stage kind is intentionally not an enum. Catalog extensions can define additional stage kinds
    /// without changing Atlas source code.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageKind"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class StageKind : IEquatable<StageKind>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageKind"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing stage-kind symbol.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/> is null.
        /// </exception>
        public StageKind(Symbol symbol)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            Symbol = symbol;
        }

        /// <summary>
        /// Gets the stable machine-facing stage-kind symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Creates a stage kind from a symbol value.
        /// </summary>
        /// <param name="symbol">The stable machine-facing stage-kind symbol value.</param>
        /// <returns>A validated stage kind.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="symbol"/> is not a valid symbol value.
        /// </exception>
        public static StageKind Create(string? symbol)
        {
            return new StageKind(Symbol.Create(symbol));
        }

        /// <summary>
        /// Attempts to create a stage kind from a symbol value.
        /// </summary>
        /// <param name="symbol">The stable machine-facing stage-kind symbol value.</param>
        /// <param name="stageKind">The created stage kind when validation succeeds.</param>
        /// <returns><see langword="true"/> when the stage kind is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(string? symbol, out StageKind? stageKind)
        {
            if (!Symbol.TryCreate(symbol, out Symbol? createdSymbol))
            {
                stageKind = null;
                return false;
            }

            stageKind = new StageKind(createdSymbol!);
            return true;
        }

        /// <summary>
        /// Determines whether the specified text can create a valid stage kind.
        /// </summary>
        /// <param name="symbol">The stable machine-facing stage-kind symbol value.</param>
        /// <returns><see langword="true"/> when the text can create a valid stage kind; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(string? symbol)
        {
            return Symbol.IsValid(symbol);
        }

        /// <inheritdoc/>
        public bool Equals(StageKind? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageKind other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Symbol.ToString();
        }

        /// <summary>
        /// Determines whether two stage kinds are equal.
        /// </summary>
        public static bool operator ==(StageKind? left, StageKind? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage kinds are not equal.
        /// </summary>
        public static bool operator !=(StageKind? left, StageKind? right)
        {
            return !Equals(left, right);
        }
    }
}