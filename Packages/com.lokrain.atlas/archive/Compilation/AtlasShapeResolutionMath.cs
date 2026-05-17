// Runtime/Compilation/AtlasShapeResolutionMath.cs

using System;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Contracts;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Provides deterministic integer math for resolving runtime field lengths and capacities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type belongs to compilation/resolution code, not jobs. Jobs receive already-resolved
    /// lengths, capacities, native containers, slices, views, or compiled addresses.
    /// </para>
    ///
    /// <para>
    /// Capacity scaling intentionally uses integer ratios rather than floating-point multipliers
    /// so shape resolution is deterministic, hash-stable, and platform-independent.
    /// </para>
    /// </remarks>
    internal static class AtlasShapeResolutionMath
    {
        /// <summary>
        /// Resolves a static length from a scalar or fixed length shape.
        /// </summary>
        /// <param name="shape">The length shape to resolve.</param>
        /// <param name="parameterName">Caller parameter name used by thrown exceptions.</param>
        /// <returns>The resolved static length.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="shape"/> is not a static-length shape.
        /// </exception>
        public static int ResolveStaticLength(
            LengthShape shape,
            string parameterName = null)
        {
            shape.ValidateOrThrow(parameterName);

            if (shape.TryGetStaticLength(out var length))
            {
                return length;
            }

            throw new ArgumentException(
                $"Length shape '{shape}' does not declare a static length.",
                parameterName ?? nameof(shape));
        }

        /// <summary>
        /// Resolves a capacity derived from another field's resolved length or capacity.
        /// </summary>
        /// <param name="sourceCapacity">The already-resolved source length or capacity.</param>
        /// <param name="shape">The derived-capacity shape.</param>
        /// <param name="parameterName">Caller parameter name used by thrown exceptions.</param>
        /// <returns>The resolved capacity.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="sourceCapacity"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="shape"/> is not a derived-capacity shape.
        /// </exception>
        /// <exception cref="OverflowException">
        /// Thrown when the resolved capacity exceeds <see cref="int.MaxValue"/>.
        /// </exception>
        public static int ResolveDerivedCapacity(
            int sourceCapacity,
            LengthShape shape,
            string parameterName = null)
        {
            if (sourceCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceCapacity),
                    sourceCapacity,
                    "Source capacity must be greater than or equal to zero.");
            }

            if (shape.Kind != LengthShapeKind.CapacityFromField)
            {
                throw new ArgumentException(
                    $"Length shape '{shape}' is not a derived-capacity shape.",
                    parameterName ?? nameof(shape));
            }

            shape.ValidateOrThrow(parameterName);

            var scaled = MultiplyAndDivideRoundUp(
                sourceCapacity,
                shape.CapacityMultiplierNumerator,
                shape.CapacityMultiplierDenominator);

            var capacity = checked(scaled + shape.CapacityPadding);

            if (capacity > int.MaxValue)
            {
                throw new OverflowException(
                    $"Resolved capacity '{capacity}' exceeds Int32.MaxValue.");
            }

            return (int)capacity;
        }

        /// <summary>
        /// Resolves a capacity derived from another field's resolved length or capacity using
        /// exact division only.
        /// </summary>
        /// <param name="sourceCapacity">The already-resolved source length or capacity.</param>
        /// <param name="shape">The derived-capacity shape.</param>
        /// <param name="parameterName">Caller parameter name used by thrown exceptions.</param>
        /// <returns>The resolved capacity.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the capacity ratio does not divide exactly.
        /// </exception>
        public static int ResolveDerivedCapacityExact(
            int sourceCapacity,
            LengthShape shape,
            string parameterName = null)
        {
            if (sourceCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceCapacity),
                    sourceCapacity,
                    "Source capacity must be greater than or equal to zero.");
            }

            if (shape.Kind != LengthShapeKind.CapacityFromField)
            {
                throw new ArgumentException(
                    $"Length shape '{shape}' is not a derived-capacity shape.",
                    parameterName ?? nameof(shape));
            }

            shape.ValidateOrThrow(parameterName);

            var scaled = MultiplyAndDivideExact(
                sourceCapacity,
                shape.CapacityMultiplierNumerator,
                shape.CapacityMultiplierDenominator);

            var capacity = checked(scaled + shape.CapacityPadding);

            if (capacity > int.MaxValue)
            {
                throw new OverflowException(
                    $"Resolved capacity '{capacity}' exceeds Int32.MaxValue.");
            }

            return (int)capacity;
        }

        /// <summary>
        /// Multiplies an integer value by a ratio and rounds up.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="numerator">The ratio numerator.</param>
        /// <param name="denominator">The ratio denominator.</param>
        /// <returns>The rounded-up scaled value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long MultiplyAndDivideRoundUp(
            int value,
            int numerator,
            int denominator)
        {
            ValidateRatioInput(value, numerator, denominator);

            var product = checked((long)value * numerator);

            return DivideRoundUp(product, denominator);
        }

        /// <summary>
        /// Multiplies an integer value by a ratio and requires exact division.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="numerator">The ratio numerator.</param>
        /// <param name="denominator">The ratio denominator.</param>
        /// <returns>The exactly scaled value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the product is not exactly divisible by <paramref name="denominator"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long MultiplyAndDivideExact(
            int value,
            int numerator,
            int denominator)
        {
            ValidateRatioInput(value, numerator, denominator);

            var product = checked((long)value * numerator);
            var remainder = product % denominator;

            if (remainder != 0L)
            {
                throw new InvalidOperationException(
                    $"Ratio {numerator}/{denominator} does not divide value '{value}' exactly.");
            }

            return product / denominator;
        }

        /// <summary>
        /// Divides a non-negative value by a positive divisor and rounds up.
        /// </summary>
        /// <param name="value">The non-negative value.</param>
        /// <param name="divisor">The positive divisor.</param>
        /// <returns>The rounded-up quotient.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long DivideRoundUp(
            long value,
            int divisor)
        {
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Value must be greater than or equal to zero.");
            }

            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(divisor),
                    divisor,
                    "Divisor must be greater than zero.");
            }

            return checked((value + divisor - 1L) / divisor);
        }

        private static void ValidateRatioInput(
            int value,
            int numerator,
            int denominator)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Value must be greater than or equal to zero.");
            }

            if (numerator < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(numerator),
                    numerator,
                    "Numerator must be greater than or equal to zero.");
            }

            if (denominator <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(denominator),
                    denominator,
                    "Denominator must be greater than zero.");
            }
        }
    }
}