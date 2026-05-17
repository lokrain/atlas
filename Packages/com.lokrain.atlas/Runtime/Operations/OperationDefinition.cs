#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines a catalog-owned generation operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation definition identifies one authored generation operation within a generation schema.
    /// It describes catalog metadata only; it does not define stage route membership, implementation binding,
    /// execution behavior, runtime state, job data, or native containers.
    /// </para>
    /// <para>
    /// Stage route membership is owned by stage route definitions. Operation definitions are schema-owned so
    /// routes can compose, validate, and reuse compatible operations without making the operation itself
    /// stage-owned.
    /// </para>
    /// <para>
    /// The operation symbol is the stable machine-facing identity. The display name is user-facing metadata only
    /// and must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because two operation definitions with the same symbol represent
    /// the same catalog identity. Schema, kind, or metadata conflicts must be handled by catalog validation,
    /// not by treating duplicate symbols as distinct definitions.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationDefinition"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class OperationDefinition : IEquatable<OperationDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationDefinition"/> class.
        /// </summary>
        /// <param name="generationSchema">The generation schema that owns this operation definition.</param>
        /// <param name="operationKind">The operation kind.</param>
        /// <param name="symbol">The stable machine-facing operation symbol.</param>
        /// <param name="displayName">The user-facing operation display name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationSchema"/>, <paramref name="operationKind"/>,
        /// <paramref name="symbol"/>, or <paramref name="displayName"/> is null.
        /// </exception>
        public OperationDefinition(
            GenerationSchemaDefinition generationSchema,
            OperationKind operationKind,
            Symbol symbol,
            DisplayName displayName)
        {
            if (generationSchema is null)
            {
                throw new ArgumentNullException(nameof(generationSchema));
            }

            if (operationKind is null)
            {
                throw new ArgumentNullException(nameof(operationKind));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            GenerationSchema = generationSchema;
            OperationKind = operationKind;
            Symbol = symbol;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the generation schema that owns this operation definition.
        /// </summary>
        public GenerationSchemaDefinition GenerationSchema { get; }

        /// <summary>
        /// Gets the operation kind.
        /// </summary>
        public OperationKind OperationKind { get; }

        /// <summary>
        /// Gets the stable machine-facing operation symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing operation display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <inheritdoc/>
        public bool Equals(OperationDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(OperationDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(OperationKind)}: {OperationKind}, {nameof(GenerationSchema)}: {GenerationSchema.Symbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two operation definitions are equal.
        /// </summary>
        public static bool operator ==(OperationDefinition? left, OperationDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation definitions are not equal.
        /// </summary>
        public static bool operator !=(OperationDefinition? left, OperationDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}