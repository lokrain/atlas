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
    /// Describes how Atlas resolves field length or capacity before scheduling jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is immutable and blittable. It can be stored in contract tables, validation
    /// reports, and unmanaged metadata buffers.
    /// </para>
    ///
    /// <para>
    /// This type is not a resolved shape. It does not use an unresolved length sentinel.
    /// Non-fixed shape kinds keep <see cref="FixedLength"/> at zero because that field is not
    /// meaningful for those kinds.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LengthShape :
        IEquatable<LengthShape>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Reserved invalid length shape.
        /// </summary>
        public static readonly LengthShape None = default;

        /// <summary>
        /// The kind of shape rule represented by this value.
        /// </summary>
        public readonly LengthShapeKind Kind;

        /// <summary>
        /// Fixed length for <see cref="LengthShapeKind.Fixed"/> or scalar length for
        /// <see cref="LengthShapeKind.Scalar"/>.
        /// </summary>
        /// <remarks>
        /// For non-fixed shape kinds this value is intentionally zero and must not be interpreted
        /// as unresolved state. Resolution state belongs to the resolved-shape layer.
        /// </remarks>
        public readonly int FixedLength;

        /// <summary>
        /// Stable identifier of the source field for field-relative shape rules.
        /// </summary>
        public readonly StableDataId SourceFieldId;

        /// <summary>
        /// Stable diagnostic/query name used by query-based or external shape rules.
        /// </summary>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Numerator of the deterministic capacity multiplier used by derived-capacity rules.
        /// </summary>
        public readonly int CapacityMultiplierNumerator;

        /// <summary>
        /// Denominator of the deterministic capacity multiplier used by derived-capacity rules.
        /// </summary>
        public readonly int CapacityMultiplierDenominator;

        /// <summary>
        /// Extra capacity added after ratio-based capacity resolution.
        /// </summary>
        public readonly int CapacityPadding;

        private LengthShape(
            LengthShapeKind kind,
            int fixedLength,
            StableDataId sourceFieldId,
            FixedString64Bytes name,
            int capacityMultiplierNumerator,
            int capacityMultiplierDenominator,
            int capacityPadding)
        {
            Kind = kind;
            FixedLength = fixedLength;
            SourceFieldId = sourceFieldId;
            Name = name;
            CapacityMultiplierNumerator = capacityMultiplierNumerator;
            CapacityMultiplierDenominator = capacityMultiplierDenominator;
            CapacityPadding = capacityPadding;
        }

        /// <summary>
        /// Gets whether this shape is valid for a concrete field contract.
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
        /// Gets whether this shape declares a static length directly.
        /// </summary>
        public bool HasStaticLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == LengthShapeKind.Fixed || Kind == LengthShapeKind.Scalar;
        }

        /// <summary>
        /// Gets whether this shape has an explicit fixed length.
        /// </summary>
        public bool IsFixed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HasStaticLength;
        }

        /// <summary>
        /// Gets whether this shape depends on another Atlas field.
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
        /// Gets whether this shape derives capacity from another field.
        /// </summary>
        public bool IsDerivedCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == LengthShapeKind.CapacityFromField;
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
                sourceFieldId: default,
                name: default,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a fixed-length shape.
        /// </summary>
        /// <param name="length">The exact runtime length required by the field.</param>
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
                sourceFieldId: default,
                name: default,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape whose length matches another field's resolved length.
        /// </summary>
        /// <param name="sourceFieldId">Stable identifier of the source field.</param>
        /// <returns>A field-relative length shape.</returns>
        public static LengthShape MatchFieldLength(StableDataId sourceFieldId)
        {
            sourceFieldId.ValidateOrThrow(nameof(sourceFieldId));

            return new LengthShape(
                LengthShapeKind.MatchFieldLength,
                fixedLength: 0,
                sourceFieldId: sourceFieldId,
                name: default,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape whose length matches another typed field's resolved length.
        /// </summary>
        /// <typeparam name="TField">Source field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the source field.</typeparam>
        /// <returns>A field-relative length shape.</returns>
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
        public static LengthShape QueryCount(FixedString64Bytes queryName)
        {
            ThrowIfEmptyName(queryName, nameof(queryName));

            return new LengthShape(
                LengthShapeKind.QueryCount,
                fixedLength: 0,
                sourceFieldId: default,
                name: queryName,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape resolved from a named chunk count or equivalent partition count.
        /// </summary>
        /// <param name="queryName">Stable resolver name for the query or partitioned data set.</param>
        /// <returns>A chunk-count length shape.</returns>
        public static LengthShape ChunkCount(FixedString64Bytes queryName)
        {
            ThrowIfEmptyName(queryName, nameof(queryName));

            return new LengthShape(
                LengthShapeKind.ChunkCount,
                fixedLength: 0,
                sourceFieldId: default,
                name: queryName,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a capacity shape derived from another field's resolved length or capacity.
        /// </summary>
        /// <param name="sourceFieldId">Stable identifier of the source field.</param>
        /// <param name="multiplierNumerator">Numerator of the deterministic capacity multiplier.</param>
        /// <param name="multiplierDenominator">Denominator of the deterministic capacity multiplier.</param>
        /// <param name="padding">Additional capacity added after ratio-based resolution.</param>
        /// <returns>A field-relative capacity shape.</returns>
        public static LengthShape CapacityFromField(
            StableDataId sourceFieldId,
            int multiplierNumerator = 1,
            int multiplierDenominator = 1,
            int padding = 0)
        {
            sourceFieldId.ValidateOrThrow(nameof(sourceFieldId));
            ThrowIfInvalidCapacityArguments(
                multiplierNumerator,
                multiplierDenominator,
                padding);

            return new LengthShape(
                LengthShapeKind.CapacityFromField,
                fixedLength: 0,
                sourceFieldId: sourceFieldId,
                name: default,
                capacityMultiplierNumerator: multiplierNumerator,
                capacityMultiplierDenominator: multiplierDenominator,
                capacityPadding: padding);
        }

        /// <summary>
        /// Creates a capacity shape derived from another typed field's resolved length or capacity.
        /// </summary>
        /// <typeparam name="TField">Source field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the source field.</typeparam>
        /// <param name="multiplierNumerator">Numerator of the deterministic capacity multiplier.</param>
        /// <param name="multiplierDenominator">Denominator of the deterministic capacity multiplier.</param>
        /// <param name="padding">Additional capacity added after ratio-based resolution.</param>
        /// <returns>A field-relative capacity shape.</returns>
        public static LengthShape CapacityFromField<TField, TElement>(
            int multiplierNumerator = 1,
            int multiplierDenominator = 1,
            int padding = 0)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return CapacityFromField(
                AtlasField.StableId<TField, TElement>(),
                multiplierNumerator,
                multiplierDenominator,
                padding);
        }

        /// <summary>
        /// Creates a shape whose payload length is resolved from prefix-sum metadata.
        /// </summary>
        /// <param name="prefixFieldId">Stable identifier of the prefix metadata field.</param>
        /// <returns>A prefix-sum payload shape.</returns>
        public static LengthShape PrefixSumPayload(StableDataId prefixFieldId)
        {
            prefixFieldId.ValidateOrThrow(nameof(prefixFieldId));

            return new LengthShape(
                LengthShapeKind.PrefixSumPayload,
                fixedLength: 0,
                sourceFieldId: prefixFieldId,
                name: default,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Creates a shape whose payload length is resolved from a typed prefix metadata field.
        /// </summary>
        /// <typeparam name="TField">Prefix metadata field declaration type.</typeparam>
        /// <typeparam name="TElement">Unmanaged element type stored by the prefix metadata field.</typeparam>
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
        public static LengthShape External(FixedString64Bytes name)
        {
            ThrowIfEmptyName(name, nameof(name));

            return new LengthShape(
                LengthShapeKind.External,
                fixedLength: 0,
                sourceFieldId: default,
                name: name,
                capacityMultiplierNumerator: 1,
                capacityMultiplierDenominator: 1,
                capacityPadding: 0);
        }

        /// <summary>
        /// Attempts to read the static length declared by this shape.
        /// </summary>
        /// <param name="length">The declared static length when this method returns <c>true</c>.</param>
        /// <returns><c>true</c> for scalar and fixed shapes; otherwise, <c>false</c>.</returns>
        public bool TryGetStaticLength(out int length)
        {
            if (HasStaticLength)
            {
                length = FixedLength;
                return true;
            }

            length = 0;
            return false;
        }

        /// <summary>
        /// Throws when this shape is not valid for a concrete contract.
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
                    if (FixedLength == 1 &&
                        HasDefaultCapacityRatio() &&
                        CapacityPadding == 0)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.Fixed:
                    if (FixedLength >= 0 &&
                        HasDefaultCapacityRatio() &&
                        CapacityPadding == 0)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.MatchFieldLength:
                case LengthShapeKind.PrefixSumPayload:
                    if (SourceFieldId.IsValid &&
                        FixedLength == 0 &&
                        HasDefaultCapacityRatio() &&
                        CapacityPadding == 0)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.QueryCount:
                case LengthShapeKind.ChunkCount:
                case LengthShapeKind.External:
                    if (!Name.IsEmpty &&
                        FixedLength == 0 &&
                        HasDefaultCapacityRatio() &&
                        CapacityPadding == 0)
                    {
                        return;
                    }

                    break;

                case LengthShapeKind.CapacityFromField:
                    if (SourceFieldId.IsValid &&
                        FixedLength == 0 &&
                        CapacityMultiplierNumerator >= 0 &&
                        CapacityMultiplierDenominator > 0 &&
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
                   CapacityMultiplierNumerator == other.CapacityMultiplierNumerator &&
                   CapacityMultiplierDenominator == other.CapacityMultiplierDenominator &&
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
        /// Returns a deterministic hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A deterministic managed hash code for this shape.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ (int)Kind;
                hash = (hash * HashMultiplier) ^ FixedLength;
                hash = (hash * HashMultiplier) ^ SourceFieldId.GetHashCode();
                hash = (hash * HashMultiplier) ^ Name.GetHashCode();
                hash = (hash * HashMultiplier) ^ CapacityMultiplierNumerator;
                hash = (hash * HashMultiplier) ^ CapacityMultiplierDenominator;
                hash = (hash * HashMultiplier) ^ CapacityPadding;
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
                    return $"CapacityFromField({SourceFieldId}, x{CapacityMultiplierNumerator}/{CapacityMultiplierDenominator}, +{CapacityPadding})";

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
        public static bool operator ==(LengthShape left, LengthShape right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two length shapes are not equal.
        /// </summary>
        public static bool operator !=(LengthShape left, LengthShape right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasDefaultCapacityRatio()
        {
            return CapacityMultiplierNumerator == 1 &&
                   CapacityMultiplierDenominator == 1;
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

        private static void ThrowIfInvalidCapacityArguments(
            int multiplierNumerator,
            int multiplierDenominator,
            int padding)
        {
            if (multiplierNumerator < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplierNumerator),
                    multiplierNumerator,
                    "Capacity multiplier numerator must be greater than or equal to zero.");
            }

            if (multiplierDenominator <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplierDenominator),
                    multiplierDenominator,
                    "Capacity multiplier denominator must be greater than zero.");
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