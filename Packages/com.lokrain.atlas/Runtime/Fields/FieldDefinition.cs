#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Defines managed field metadata for representing a semantic generation resource.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field definition maps one semantic resource definition to managed representation metadata used by
    /// runnable compilation and execution infrastructure.
    /// </para>
    /// <para>
    /// A field definition describes what kind of field is required for a resource. It does not allocate storage,
    /// own native memory, schedule jobs, bind ECS data, capture artifacts, or describe executable operation data.
    /// </para>
    /// <para>
    /// The field symbol is stable machine-facing identity. The display name is user-facing metadata only and
    /// must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="Symbol"/> because a field definition has one field metadata identity.
    /// Resource, shape, value-kind, or metadata conflicts must be handled by field-set or catalog-adjacent validation,
    /// not by treating duplicate symbols as distinct field definitions.
    /// </para>
    /// <para>
    /// A non-null <see cref="FieldDefinition"/> instance is structurally valid. Catalog-dependent semantic
    /// validity is established by the generation catalog and field metadata set. Execution support is established
    /// by runnable compilation.
    /// </para>
    /// </remarks>
    public sealed class FieldDefinition : IEquatable<FieldDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldDefinition"/> class.
        /// </summary>
        /// <param name="resourceDefinition">The semantic resource represented by this field definition.</param>
        /// <param name="symbol">The stable machine-facing field symbol.</param>
        /// <param name="displayName">The user-facing field display name.</param>
        /// <param name="shape">The logical field shape.</param>
        /// <param name="valueKind">The managed scalar value kind.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinition"/>, <paramref name="symbol"/>, or
        /// <paramref name="displayName"/> is null.
        /// </exception>
        public FieldDefinition(
            ResourceDefinition resourceDefinition,
            Symbol symbol,
            DisplayName displayName,
            FieldShape shape,
            FieldValueKind valueKind)
        {
            if (resourceDefinition is null)
            {
                throw new ArgumentNullException(nameof(resourceDefinition));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            ResourceDefinition = resourceDefinition;
            Symbol = symbol;
            DisplayName = displayName;
            Shape = shape;
            ValueKind = valueKind;
        }

        /// <summary>
        /// Gets the semantic resource represented by this field definition.
        /// </summary>
        public ResourceDefinition ResourceDefinition { get; }

        /// <summary>
        /// Gets the stable machine-facing field symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the user-facing field display name.
        /// </summary>
        public DisplayName DisplayName { get; }

        /// <summary>
        /// Gets the logical field shape.
        /// </summary>
        public FieldShape Shape { get; }

        /// <summary>
        /// Gets the managed scalar value kind.
        /// </summary>
        public FieldValueKind ValueKind { get; }

        /// <inheritdoc/>
        public bool Equals(FieldDefinition? other)
        {
            return other is not null && Symbol == other.Symbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is FieldDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(FieldDefinition)}({nameof(Symbol)}: {Symbol}, {nameof(ResourceDefinition)}: {ResourceDefinition.Symbol}, {nameof(Shape)}: {Shape}, {nameof(ValueKind)}: {ValueKind}, {nameof(DisplayName)}: {DisplayName})";
        }

        /// <summary>
        /// Determines whether two field definitions are equal.
        /// </summary>
        public static bool operator ==(FieldDefinition? left, FieldDefinition? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two field definitions are not equal.
        /// </summary>
        public static bool operator !=(FieldDefinition? left, FieldDefinition? right)
        {
            return !Equals(left, right);
        }
    }
}
