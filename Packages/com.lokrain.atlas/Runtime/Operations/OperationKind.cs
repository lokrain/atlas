#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Identifies a category of generation operation within a generation catalog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation kind is a domain-specific symbolic identity used to group compatible operation definitions,
    /// implementation definitions, request selections, and resolved plan nodes.
    /// </para>
    /// <para>
    /// The wrapped symbol is the stable machine-facing value. Operation kind names are not display names,
    /// numeric runtime identifiers, execution handles, or serialized job data.
    /// </para>
    /// <para>
    /// An operation kind is intentionally not an enum. Catalog extensions can define additional operation kinds
    /// without changing Atlas source code.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationKind"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class OperationKind : IEquatable<OperationKind>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationKind"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing operation-kind symbol.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/> is null.
        /// </exception>
        public OperationKind(Symbol symbol)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            Symbol = symbol;
        }

        /// <summary>
        /// Gets the stable machine-facing operation-kind symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Creates an operation kind from a symbol value.
        /// </summary>
        /// <param name="symbol">The stable machine-facing operation-kind symbol value.</param>
        /// <returns>A validated operation kind.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="symbol"/> is not a valid symbol value.
        /// </exception>
        public static OperationKind Create(string? symbol)
        {
            return new OperationKind(Symbol.Create(symbol));
        }

        /// <summary>
        /// Attempts to create an operation kind from a symbol value.
        /// </summary>
        /// <param name="symbol">The stable machine-facing operation-kind symbol value.</param>
        /// <param name="operationKind">The created operation kind when validation succeeds.</param>
        /// <returns><see langword="true"/> when the operation kind is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(string? symbol, out OperationKind? operationKind)
        {
            if (!Symbol.TryCreate(symbol, out Symbol? createdSymbol))
            {
                operationKind = null;
                return false;
            }

            operationKind = new OperationKind(createdSymbol!);
            return true;
        }

        /// <summary>
        /// Determines whether the specified text can create a valid operation kind.
        /// </summary>
        /// <param name="symbol">The stable machine-facing operation-kind symbol value.</param>
        /// <returns><see langword="true"/> when the text can create a valid operation kind; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(string? symbol)
        {
            return Symbol.IsValid(symbol);
        }

        /// <inheritdoc/>
        public bool Equals(OperationKind? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationKind other && Equals(other);
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
        /// Determines whether two operation kinds are equal.
        /// </summary>
        public static bool operator ==(OperationKind? left, OperationKind? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation kinds are not equal.
        /// </summary>
        public static bool operator !=(OperationKind? left, OperationKind? right)
        {
            return !Equals(left, right);
        }
    }
}