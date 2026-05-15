// Runtime/Contracts/LengthShape.cs

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Defines the rule used to resolve a Field's runtime length or capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Length shape is schema metadata. It describes how storage size should be resolved before
    /// jobs are scheduled. Jobs should receive already-resolved native containers and must not
    /// interpret length-shape rules directly.
    /// </para>
    ///
    /// <para>
    /// Shape resolution is context-dependent. Query-based shapes require a resolver that knows
    /// the relevant ECS query, authoring set, simulation set, or external data source. Field-
    /// relative shapes require the source Field to exist in the same validated Contract table.
    /// </para>
    /// </remarks>
    public enum LengthShapeKind : byte
    {
        /// <summary>
        /// No length-shape rule is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for concrete Contracts and is reserved for default
        /// initialization and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The Field contains exactly one logical element.
        /// </summary>
        Scalar = 1,

        /// <summary>
        /// The Field has a fixed length declared in the Contract.
        /// </summary>
        Fixed = 2,

        /// <summary>
        /// The Field length must match another Field's resolved length.
        /// </summary>
        MatchFieldLength = 3,

        /// <summary>
        /// The Field length is resolved from a named entity-query count or equivalent data set.
        /// </summary>
        QueryCount = 4,

        /// <summary>
        /// The Field length is resolved from a named chunk count or equivalent partition count.
        /// </summary>
        ChunkCount = 5,

        /// <summary>
        /// The Field capacity is derived from another Field's resolved length or capacity.
        /// </summary>
        CapacityFromField = 6,

        /// <summary>
        /// The Field length is resolved from prefix-sum metadata.
        /// </summary>
        PrefixSumPayload = 7,

        /// <summary>
        /// The Field length is provided by an external owner or integration layer.
        /// </summary>
        External = 8
    }

    /// <summary>
    /// Describes how Atlas resolves Field length or capacity before scheduling jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is immutable and blittable. It can be stored in Contract tables, validation
    /// reports, and unmanaged metadata buffers.
    /// </para>
    ///
    /// <para>
    /// Not every field participates in every shape kind. Callers should use the named factory
    /// methods instead of constructing values manually.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LengthShape :
        IEquatable<LengthShape>
    {
        /// <summary>
        /// Reserved invalid length shape.
        /// </summary>
        public static readonly LengthShape None = default;

        /// <summary>
        /// The kind of shape rule represented by this value.
        /// </summary>
        public readonly LengthShapeKind Kind;

        /// <summary>
        /// Fixed length, scalar length, or minimum derived capacity depending on <see cref="Kind"/>.
        /// </summary>
        public readonly int FixedLength;

        /// <summary>
        /// Stable identifier of the source Field for Field-relative shape rules.
        /// </summary>
        public readonly StableDataId SourceFieldId;

        /// <summary>
        /// Stable diagnostic/query name used by query-based or external shape rules.
        /// </summary>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Capacity multiplier used by derived-capacity rules.
        /// </summary>
        public readonly float CapacityMultiplier;

        /// <summary>
        /// Extra capacity added after multiplier-based capacity resolution.
        /// </summary>
        public readonly int CapacityPadding;

        private LengthShape(
            LengthShapeKind kind,
            int fixedLength,
            StableDataId sourceFieldId,
            FixedString64Bytes name,
            float capacityMultiplier,
            int capacityPadding)
        {
            Kind = kind;
            FixedLength = fixedLength;
            SourceFieldId = sourceFieldId;
            Name = name;
            CapacityMultiplier = capacityMultiplier;
            CapacityPadding = capacityPadding;
        }

        /// <summary>
        /// Gets whether this shape is valid for a concrete Field Contract.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind != LengthShapeKind.None;
        }

        /// <summary>
        /// Gets whether this shape must resolve to exactly one logical element.
        /// </summary>
        public bool IsScalar
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == LengthShapeKind.Scalar;
        }

        /// <summary>
        /// Gets whether this shape has an explicit fixed length.
        /// </summary>
        public bool IsFixed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == LengthShapeKind.Fixed || Kind == LengthShapeKind.Scalar;
        }

        /// <summary>
        /// Gets whether this shape depends on another Atlas Field.
        /// </summary>
        public bool DependsOnField
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == LengthShapeKind.MatchFieldLength ||
                   Kind == LengthShapeKind.CapacityFromField ||
                   Kind == LengthShapeKind.PrefixSumPayload;
        }

        /// <summary>
        /// Gets whether this shape depends on a named resolver source.
        /// </summary>
        public bool DependsOnName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == LengthShapeKind.QueryCount ||
                   Kind == LengthShapeKind.ChunkCount ||
                   Kind == LengthShapeKind.External;
        }

        /// <summary>
        /// Creates a scalar shape with exactly one logical element.
        /// </summary>
        /// <returns>A scalar length shape.</returns>
        public static LengthShape Scalar()
        {
            return new LengthShape(
                LengthShapeKind.Scalar,
                fixedLength: 1,
                sourceFieldId: StableDataId.Empty,
                name: default,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a fixed-length shape.
        /// </summary>
        /// <param name="length">The exact runtime length required by the Field.</param>
        /// <returns>A fixed-length shape.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="length"/> is negative.
        /// </exception>
        public static LengthShape Fixed(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Fixed length must be greater than or equal to zero.");
            }

            return new LengthShape(
                LengthShapeKind.Fixed,
                fixedLength: length,
                sourceFieldId: StableDataId.Empty,
                name: default,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape whose length matches another Field's resolved length.
        /// </summary>
        /// <param name="sourceFieldId">Stable identifier of the source Field.</param>
        /// <returns>A Field-relative length shape.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sourceFieldId"/> is invalid.
        /// </exception>
        public static LengthShape MatchFieldLength(StableDataId sourceFieldId)
        {
            sourceFieldId.ValidateOrThrow(nameof(sourceFieldId));

            return new LengthShape(
                LengthShapeKind.MatchFieldLength,
                fixedLength: AtlasConstants.UnresolvedLength,
                sourceFieldId: sourceFieldId,
                name: default,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape whose length matches another typed Field's resolved length.
        /// </summary>
        /// <typeparam name="TField">Source Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the source Field.</typeparam>
        /// <returns>A Field-relative length shape.</returns>
        public static LengthShape MatchFieldLength<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return MatchFieldLength(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Creates a shape resolved from a named query count or equivalent data set.
        /// </summary>
        /// <param name="queryName">Stable resolver name for the query or data set.</param>
        /// <returns>A query-count length shape.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="queryName"/> is empty.
        /// </exception>
        public static LengthShape QueryCount(FixedString64Bytes queryName)
        {
            ThrowIfEmptyName(queryName, nameof(queryName));

            return new LengthShape(
                LengthShapeKind.QueryCount,
                fixedLength: AtlasConstants.UnresolvedLength,
                sourceFieldId: StableDataId.Empty,
                name: queryName,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape resolved from a named chunk count or equivalent partition count.
        /// </summary>
        /// <param name="queryName">Stable resolver name for the query or partitioned data set.</param>
        /// <returns>A chunk-count length shape.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="queryName"/> is empty.
        /// </exception>
        public static LengthShape ChunkCount(FixedString64Bytes queryName)
        {
            ThrowIfEmptyName(queryName, nameof(queryName));

            return new LengthShape(
                LengthShapeKind.ChunkCount,
                fixedLength: AtlasConstants.UnresolvedLength,
                sourceFieldId: StableDataId.Empty,
                name: queryName,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a capacity shape derived from another Field's resolved length or capacity.
        /// </summary>
        /// <param name="sourceFieldId">Stable identifier of the source Field.</param>
        /// <param name="multiplier">Multiplier applied to the source size.</param>
        /// <param name="padding">Additional capacity added after multiplication.</param>
        /// <returns>A Field-relative capacity shape.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sourceFieldId"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="multiplier"/> is negative or <paramref name="padding"/> is negative.
        /// </exception>
        public static LengthShape CapacityFromField(
            StableDataId sourceFieldId,
            float multiplier = 1.0f,
            int padding = 0)
        {
            sourceFieldId.ValidateOrThrow(nameof(sourceFieldId));
            ThrowIfInvalidCapacityArguments(multiplier, padding);

            return new LengthShape(
                LengthShapeKind.CapacityFromField,
                fixedLength: AtlasConstants.UnresolvedLength,
                sourceFieldId: sourceFieldId,
                name: default,
                capacityMultiplier: multiplier,
                capacityPadding: padding);
        }

        /// <summary>
        /// Creates a capacity shape derived from another typed Field's resolved length or capacity.
        /// </summary>
        /// <typeparam name="TField">Source Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the source Field.</typeparam>
        /// <param name="multiplier">Multiplier applied to the source size.</param>
        /// <param name="padding">Additional capacity added after multiplication.</param>
        /// <returns>A Field-relative capacity shape.</returns>
        public static LengthShape CapacityFromField<TField, TElement>(
            float multiplier = 1.0f,
            int padding = 0)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return CapacityFromField(
                AtlasField.StableId<TField, TElement>(),
                multiplier,
                padding);
        }

        /// <summary>
        /// Creates a shape whose payload length is resolved from prefix-sum metadata.
        /// </summary>
        /// <param name="prefixFieldId">Stable identifier of the prefix metadata Field.</param>
        /// <returns>A prefix-sum payload shape.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="prefixFieldId"/> is invalid.
        /// </exception>
        public static LengthShape PrefixSumPayload(StableDataId prefixFieldId)
        {
            prefixFieldId.ValidateOrThrow(nameof(prefixFieldId));

            return new LengthShape(
                LengthShapeKind.PrefixSumPayload,
                fixedLength: AtlasConstants.UnresolvedLength,
                sourceFieldId: prefixFieldId,
                name: default,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape whose payload length is resolved from a typed prefix metadata Field.
        /// </summary>
        /// <typeparam name="TField">Prefix metadata Field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the prefix metadata Field.</typeparam>
        /// <returns>A prefix-sum payload shape.</returns>
        public static LengthShape PrefixSumPayload<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return PrefixSumPayload(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Creates a shape whose length is provided by an external resolver.
        /// </summary>
        /// <param name="name">Stable resolver name for the external length source.</param>
        /// <returns>An externally resolved length shape.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> is empty.
        /// </exception>
        public static LengthShape External(FixedString64Bytes name)
        {
            ThrowIfEmptyName(name, nameof(name));

            return new LengthShape(
                LengthShapeKind.External,
                fixedLength: AtlasConstants.UnresolvedLength,
                sourceFieldId: StableDataId.Empty,
                name: name,
                capacityMultiplier: 1.0f,
                capacityPadding: 0);
        }

        /// <summary>
        /// Throws when this shape is not valid for a concrete Contract.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the shape is invalid or internally inconsistent.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(LengthShape);

            switch (Kind)
            {
                case LengthShapeKind.Scalar:
                    if (FixedLength == 1)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.Fixed:
                    if (FixedLength >= 0)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.MatchFieldLength:
                case LengthShapeKind.PrefixSumPayload:
                    if (SourceFieldId.IsValid)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.QueryCount:
                case LengthShapeKind.ChunkCount:
                case LengthShapeKind.External:
                    if (!Name.IsEmpty)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.CapacityFromField:
                    if (SourceFieldId.IsValid &&
                        CapacityMultiplier >= 0.0f &&
                        CapacityPadding >= 0)
                    {
                        return;
                    }

                    break;
            }

            throw new ArgumentException(
                $"Length shape '{this}' is invalid or internally inconsistent.",
                name);
        }

        /// <summary>
        /// Determines whether this shape is equal to another shape.
        /// </summary>
        /// <param name="other">The shape to compare with this shape.</param>
        /// <returns><c>true</c> when all shape fields match; otherwise, <c>false</c>.</returns>
        public bool Equals(LengthShape other)
        {
            return Kind == other.Kind &&
                   FixedLength == other.FixedLength &&
                   SourceFieldId == other.SourceFieldId &&
                   Name.Equals(other.Name) &&
                   CapacityMultiplier.Equals(other.CapacityMultiplier) &&
                   CapacityPadding == other.CapacityPadding;
        }

        /// <summary>
        /// Determines whether this shape is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this shape.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is a <see cref="LengthShape"/> with matching fields.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is LengthShape other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code for this shape.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ FixedLength;
                hash = (hash * 397) ^ SourceFieldId.GetHashCode();
                hash = (hash * 397) ^ Name.GetHashCode();
                hash = (hash * 397) ^ CapacityMultiplier.GetHashCode();
                hash = (hash * 397) ^ CapacityPadding;
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this length shape.
        /// </summary>
        /// <returns>A string describing the shape kind and relevant shape arguments.</returns>
        public override string ToString()
        {
            switch (Kind)
            {
                case LengthShapeKind.Scalar:
                    return "Scalar";

                case LengthShapeKind.Fixed:
                    return $"Fixed({FixedLength})";

                case LengthShapeKind.MatchFieldLength:
                    return $"MatchFieldLength({SourceFieldId})";

                case LengthShapeKind.QueryCount:
                    return $"QueryCount({Name})";

                case LengthShapeKind.ChunkCount:
                    return $"ChunkCount({Name})";

                case LengthShapeKind.CapacityFromField:
                    return $"CapacityFromField({SourceFieldId}, x{CapacityMultiplier}, +{CapacityPadding})";

                case LengthShapeKind.PrefixSumPayload:
                    return $"PrefixSumPayload({SourceFieldId})";

                case LengthShapeKind.External:
                    return $"External({Name})";

                default:
                    return "None";
            }
        }

        /// <summary>
        /// Determines whether two length shapes are equal.
        /// </summary>
        /// <param name="left">The first shape.</param>
        /// <param name="right">The second shape.</param>
        /// <returns><c>true</c> when all shape fields match; otherwise, <c>false</c>.</returns>
        public static bool operator ==(LengthShape left, LengthShape right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two length shapes are not equal.
        /// </summary>
        /// <param name="left">The first shape.</param>
        /// <param name="right">The second shape.</param>
        /// <returns><c>true</c> when any shape field differs; otherwise, <c>false</c>.</returns>
        public static bool operator !=(LengthShape left, LengthShape right)
        {
            return !left.Equals(right);
        }

        private static void ThrowIfEmptyName(FixedString64Bytes value, string parameterName)
        {
            if (!value.IsEmpty)
            {
                return;
            }

            throw new ArgumentException(
                "Length-shape resolver name must not be empty.",
                parameterName);
        }

        private static void ThrowIfInvalidCapacityArguments(float multiplier, int padding)
        {
            if (multiplier < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplier),
                    multiplier,
                    "Capacity multiplier must be greater than or equal to zero.");
            }

            if (padding < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(padding),
                    padding,
                    "Capacity padding must be greater than or equal to zero.");
            }
        }
    }
}