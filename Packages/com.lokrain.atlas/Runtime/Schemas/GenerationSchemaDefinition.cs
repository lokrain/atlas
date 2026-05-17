#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Schemas
{
    /// <summary>
    /// Defines a generation schema boundary owned by a generation catalog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation schema definition identifies a named generation domain, such as a built-in world-generation
    /// schema or a package-provided extension schema.
    /// </para>
    /// <para>
    /// The schema symbol is the stable machine-facing identity. The display name is user-facing metadata only
    /// and must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because two schema definitions with the same symbol represent
    /// the same catalog identity. Metadata differences must be handled as catalog duplicate/conflict validation,
    /// not as distinct schema identities.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationSchemaDefinition"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationSchemaDefinition : IEquatable<GenerationSchemaDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationSchemaDefinition"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing schema symbol.</param>
        /// <param name="displayName">The user-facing schema display name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/> or <paramref name="displayName"/> is null.
        /// </exception>
        public GenerationSchemaDefinition(Symbol symbol, DisplayName displayName)
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
        /// Gets the stable machine-facing schema symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing schema display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationSchemaDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationSchemaDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationSchemaDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two generation schema definitions are equal.
        /// </summary>
        public static bool operator ==(GenerationSchemaDefinition? left, GenerationSchemaDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation schema definitions are not equal.
        /// </summary>
        public static bool operator !=(GenerationSchemaDefinition? left, GenerationSchemaDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}