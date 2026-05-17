#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines a catalog-owned implementation option for a generation operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation implementation definition identifies one selectable implementation for an operation
    /// definition. It describes catalog metadata only; it does not contain executable bindings, delegates,
    /// reflection types, ECS systems, Burst function pointers, job structs, runtime state, job data, or native
    /// containers.
    /// </para>
    /// <para>
    /// Executable implementation binding belongs to the runnable-plan compilation layer. This keeps catalog
    /// definitions independent from Unity runtime execution concerns and allows managed planning to resolve
    /// accepted implementation choices before unmanaged job data is produced.
    /// </para>
    /// <para>
    /// The implementation symbol is the stable machine-facing identity. The display name is user-facing metadata
    /// only and must not be used for lookup, deterministic generation, catalog resolution, or artifact
    /// compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because two implementation definitions with the same symbol
    /// represent the same catalog identity. Operation ownership or metadata conflicts must be handled by catalog
    /// validation, not by treating duplicate symbols as distinct implementation definitions.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationImplementationDefinition"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class OperationImplementationDefinition : IEquatable<OperationImplementationDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationImplementationDefinition"/> class.
        /// </summary>
        /// <param name="operationDefinition">The operation definition implemented by this implementation.</param>
        /// <param name="symbol">The stable machine-facing implementation symbol.</param>
        /// <param name="displayName">The user-facing implementation display name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationDefinition"/>, <paramref name="symbol"/>, or
        /// <paramref name="displayName"/> is null.
        /// </exception>
        public OperationImplementationDefinition(
            OperationDefinition operationDefinition,
            Symbol symbol,
            DisplayName displayName)
        {
            if (operationDefinition is null)
            {
                throw new ArgumentNullException(nameof(operationDefinition));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            OperationDefinition = operationDefinition;
            Symbol = symbol;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the operation definition implemented by this implementation.
        /// </summary>
        public OperationDefinition OperationDefinition { get; }

        /// <summary>
        /// Gets the stable machine-facing implementation symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing implementation display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <inheritdoc/>
        public bool Equals(OperationImplementationDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationImplementationDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(OperationImplementationDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(OperationDefinition)}: {OperationDefinition.Symbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two operation implementation definitions are equal.
        /// </summary>
        public static bool operator ==(
            OperationImplementationDefinition? left,
            OperationImplementationDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation implementation definitions are not equal.
        /// </summary>
        public static bool operator !=(
            OperationImplementationDefinition? left,
            OperationImplementationDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}