#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;

namespace Lokrain.Atlas.Resources
{
    /// <summary>
    /// Defines a catalog-owned semantic generation resource.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A resource definition identifies a semantic value that can be required or produced by stages and
    /// operations at the managed catalog/planning boundary.
    /// </para>
    /// <para>
    /// A resource definition is not field storage, not artifact storage, not a native container, not a
    /// scheduler binding, not job data, and not an execution allocation description.
    /// </para>
    /// <para>
    /// Resource definitions intentionally come before field definitions. A resource answers what semantic
    /// value exists in the generation graph. A later field definition answers how that value is represented
    /// for a specific execution profile.
    /// </para>
    /// <para>
    /// The symbol is stable machine-facing identity. The display name is user-facing metadata only and must
    /// not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because a resource definition has one catalog identity.
    /// </para>
    /// <para>
    /// A non-null <see cref="ResourceDefinition"/> instance is always syntactically valid.
    /// Catalog-dependent semantic validity is established by the generation catalog.
    /// </para>
    /// </remarks>
    public sealed class ResourceDefinition : IEquatable<ResourceDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDefinition"/> class.
        /// </summary>
        /// <param name="symbol">The stable machine-facing resource symbol.</param>
        /// <param name="displayName">The user-facing resource display name.</param>
        /// <param name="generationSchema">The generation schema that owns this resource definition.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/>, <paramref name="displayName"/>, or
        /// <paramref name="generationSchema"/> is null.
        /// </exception>
        public ResourceDefinition(
            Symbol symbol,
            DisplayName displayName,
            GenerationSchemaDefinition generationSchema)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (generationSchema is null)
            {
                throw new ArgumentNullException(nameof(generationSchema));
            }

            Symbol = symbol;
            DisplayName = displayName;
            GenerationSchema = generationSchema;
        }

        /// <summary>
        /// Gets the stable machine-facing resource symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing resource display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <summary>
        /// Gets the generation schema that owns this resource definition.
        /// </summary>
        public GenerationSchemaDefinition GenerationSchema { get; }

        /// <inheritdoc/>
        public bool Equals(ResourceDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ResourceDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(ResourceDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(GenerationSchema)}: {GenerationSchema.Symbol}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two resource definitions are equal.
        /// </summary>
        public static bool operator ==(ResourceDefinition? left, ResourceDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two resource definitions are not equal.
        /// </summary>
        public static bool operator !=(ResourceDefinition? left, ResourceDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}