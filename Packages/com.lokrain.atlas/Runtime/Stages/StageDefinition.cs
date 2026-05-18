#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Defines a generation stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage definition identifies one authored generation stage within a generation schema. It is managed
    /// definition metadata only; it does not define route ordering, operation membership, execution behavior,
    /// runtime bindings, or job data.
    /// </para>
    /// <para>
    /// The stage symbol is the stable machine-facing identity. The display name is user-facing metadata only
    /// and must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because two stage definitions with the same symbol represent
    /// the same catalog identity. Schema, kind, or metadata conflicts must be handled by catalog validation,
    /// not by treating duplicate symbols as distinct definitions.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageDefinition"/> instance is always syntactically valid.
    /// Catalog-dependent semantic validity is established by the generation catalog.
    /// </para>
    /// </remarks>
    public sealed class StageDefinition : IEquatable<StageDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageDefinition"/> class.
        /// </summary>
        /// <param name="generationSchema">The generation schema that owns this stage definition.</param>
        /// <param name="stageKind">The stage kind.</param>
        /// <param name="symbol">The stable machine-facing stage symbol.</param>
        /// <param name="displayName">The user-facing stage display name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationSchema"/>, <paramref name="stageKind"/>,
        /// <paramref name="symbol"/>, or <paramref name="displayName"/> is null.
        /// </exception>
        public StageDefinition(
            GenerationSchemaDefinition generationSchema,
            StageKind stageKind,
            Symbol symbol,
            DisplayName displayName)
        {
            if (generationSchema is null)
            {
                throw new ArgumentNullException(nameof(generationSchema));
            }

            if (stageKind is null)
            {
                throw new ArgumentNullException(nameof(stageKind));
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
            StageKind = stageKind;
            Symbol = symbol;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the generation schema that owns this stage definition.
        /// </summary>
        public GenerationSchemaDefinition GenerationSchema { get; }

        /// <summary>
        /// Gets the stage kind.
        /// </summary>
        public StageKind StageKind { get; }

        /// <summary>
        /// Gets the stable machine-facing stage symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing stage display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <inheritdoc/>
        public bool Equals(StageDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(StageKind)}: {StageKind}, {nameof(GenerationSchema)}: {GenerationSchema.Symbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two stage definitions are equal.
        /// </summary>
        public static bool operator ==(StageDefinition? left, StageDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage definitions are not equal.
        /// </summary>
        public static bool operator !=(StageDefinition? left, StageDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}