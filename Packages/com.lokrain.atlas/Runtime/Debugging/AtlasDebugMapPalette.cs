// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapPalette.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Define deterministic RGBA colors for Atlas debug-map image generation.
// - Keep debug visualization color policy separate from canonical generation fields.
// - Avoid UnityEngine.Color/Color32 dependency in runtime core debug data.
//
// Design notes
// - This is debug/presentation-support data, not canonical world truth.
// - This does not own workspace memory.
// - This does not write files.
// - This does not allocate native memory.
// - This does not depend on UnityEngine.
// - Colors are stored as explicit byte RGBA.
// - default(AtlasDebugColor32) is valid transparent black.
// - default(AtlasDebugMapPalette) is usable but visually sparse; prefer ProductionDefault.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// Deterministic 8-bit RGBA debug color.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugColor32"/> intentionally avoids <c>UnityEngine.Color32</c>. Debug-map
    /// generation should be usable by artifact tooling, editor windows, tests, and future
    /// non-Unity export paths without pulling UnityEngine types into the data model.
    /// </para>
    /// </remarks>
    public readonly struct AtlasDebugColor32 :
        IEquatable<AtlasDebugColor32>
    {
        /// <summary>
        /// Red channel.
        /// </summary>
        public readonly byte R;

        /// <summary>
        /// Green channel.
        /// </summary>
        public readonly byte G;

        /// <summary>
        /// Blue channel.
        /// </summary>
        public readonly byte B;

        /// <summary>
        /// Alpha channel.
        /// </summary>
        public readonly byte A;

        /// <summary>
        /// Creates an opaque RGB debug color.
        /// </summary>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        public AtlasDebugColor32(
            byte r,
            byte g,
            byte b)
            : this(r, g, b, 255)
        {
        }

        /// <summary>
        /// Creates an RGBA debug color.
        /// </summary>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        /// <param name="a">Alpha channel.</param>
        public AtlasDebugColor32(
            byte r,
            byte g,
            byte b,
            byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Gets transparent black.
        /// </summary>
        public static AtlasDebugColor32 TransparentBlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0, 0, 0, 0);
        }

        /// <summary>
        /// Gets opaque black.
        /// </summary>
        public static AtlasDebugColor32 Black
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0, 0, 0, 255);
        }

        /// <summary>
        /// Gets opaque white.
        /// </summary>
        public static AtlasDebugColor32 White
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(255, 255, 255, 255);
        }

        /// <summary>
        /// Gets opaque magenta used for missing/invalid debug samples.
        /// </summary>
        public static AtlasDebugColor32 Magenta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(255, 0, 255, 255);
        }

        /// <summary>
        /// Gets whether this color is fully transparent.
        /// </summary>
        public bool IsTransparent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => A == 0;
        }

        /// <summary>
        /// Packs this color as little-endian RGBA.
        /// </summary>
        /// <returns>Packed RGBA value.</returns>
        public uint ToRgba32()
        {
            return (uint)R |
                   ((uint)G << 8) |
                   ((uint)B << 16) |
                   ((uint)A << 24);
        }

        /// <summary>
        /// Packs this color as little-endian BGRA.
        /// </summary>
        /// <returns>Packed BGRA value.</returns>
        public uint ToBgra32()
        {
            return (uint)B |
                   ((uint)G << 8) |
                   ((uint)R << 16) |
                   ((uint)A << 24);
        }

        /// <summary>
        /// Linearly interpolates two colors using an 8-bit interpolation amount.
        /// </summary>
        /// <param name="from">Color at t=0.</param>
        /// <param name="to">Color at t=255.</param>
        /// <param name="t">Interpolation amount in [0, 255].</param>
        /// <returns>Interpolated color.</returns>
        public static AtlasDebugColor32 Lerp(
            AtlasDebugColor32 from,
            AtlasDebugColor32 to,
            byte t)
        {
            return new AtlasDebugColor32(
                LerpByte(from.R, to.R, t),
                LerpByte(from.G, to.G, t),
                LerpByte(from.B, to.B, t),
                LerpByte(from.A, to.A, t));
        }

        /// <summary>
        /// Multiplies RGB channels by a brightness factor while preserving alpha.
        /// </summary>
        /// <param name="brightnessQ8">Brightness in Q8, where 256 means unchanged.</param>
        /// <returns>Brightness-adjusted color.</returns>
        public AtlasDebugColor32 WithBrightnessQ8(
            int brightnessQ8)
        {
            return new AtlasDebugColor32(
                ScaleByteQ8(R, brightnessQ8),
                ScaleByteQ8(G, brightnessQ8),
                ScaleByteQ8(B, brightnessQ8),
                A);
        }

        /// <summary>
        /// Returns a copy with a replaced alpha channel.
        /// </summary>
        /// <param name="alpha">New alpha channel.</param>
        /// <returns>Color with replaced alpha.</returns>
        public AtlasDebugColor32 WithAlpha(
            byte alpha)
        {
            return new AtlasDebugColor32(
                R,
                G,
                B,
                alpha);
        }

        /// <summary>
        /// Determines whether this color equals another color.
        /// </summary>
        /// <param name="other">Color to compare.</param>
        /// <returns><c>true</c> when all channels match.</returns>
        public bool Equals(
            AtlasDebugColor32 other)
        {
            return R == other.R &&
                   G == other.G &&
                   B == other.B &&
                   A == other.A;
        }

        /// <summary>
        /// Determines whether this color equals another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal debug color.</returns>
        public override bool Equals(
            object obj)
        {
            return obj is AtlasDebugColor32 other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        /// <returns>A managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ R;
                hash = (hash * 397) ^ G;
                hash = (hash * 397) ^ B;
                hash = (hash * 397) ^ A;
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic color string.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "RGBA({0}, {1}, {2}, {3})",
                R,
                G,
                B,
                A);
        }

        /// <summary>
        /// Compares two colors for equality.
        /// </summary>
        public static bool operator ==(
            AtlasDebugColor32 left,
            AtlasDebugColor32 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two colors for inequality.
        /// </summary>
        public static bool operator !=(
            AtlasDebugColor32 left,
            AtlasDebugColor32 right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte LerpByte(
            byte from,
            byte to,
            byte t)
        {
            var value = from + (((to - from) * t + 127) / 255);
            return ClampByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ScaleByteQ8(
            byte value,
            int brightnessQ8)
        {
            var scaled = (value * brightnessQ8 + 128) >> 8;
            return ClampByte(scaled);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ClampByte(
            int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            if (value >= 255)
            {
                return 255;
            }

            return (byte)value;
        }
    }

    /// <summary>
    /// Color policy for Atlas debug-map rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugMapPalette"/> is not canonical generation data. It is a deterministic
    /// visual mapping policy used by debug-map writers to turn generated fields into visible image
    /// pixels.
    /// </para>
    /// </remarks>
    public readonly struct AtlasDebugMapPalette :
        IEquatable<AtlasDebugMapPalette>
    {
        /// <summary>
        /// Color used when no valid sample exists.
        /// </summary>
        public readonly AtlasDebugColor32 Missing;

        /// <summary>
        /// Background color used for empty pixels.
        /// </summary>
        public readonly AtlasDebugColor32 Background;

        /// <summary>
        /// Low elevation or low normalized-value color.
        /// </summary>
        public readonly AtlasDebugColor32 Low;

        /// <summary>
        /// Mid elevation or mid normalized-value color.
        /// </summary>
        public readonly AtlasDebugColor32 Mid;

        /// <summary>
        /// High elevation or high normalized-value color.
        /// </summary>
        public readonly AtlasDebugColor32 High;

        /// <summary>
        /// Water mask color.
        /// </summary>
        public readonly AtlasDebugColor32 Water;

        /// <summary>
        /// Land mask color.
        /// </summary>
        public readonly AtlasDebugColor32 Land;

        /// <summary>
        /// Coast/edge highlight color.
        /// </summary>
        public readonly AtlasDebugColor32 Coast;

        /// <summary>
        /// Mountain/high-relief highlight color.
        /// </summary>
        public readonly AtlasDebugColor32 Mountain;

        /// <summary>
        /// River/water-flow highlight color.
        /// </summary>
        public readonly AtlasDebugColor32 River;

        /// <summary>
        /// Warning/diagnostic highlight color.
        /// </summary>
        public readonly AtlasDebugColor32 Warning;

        /// <summary>
        /// Error/invalid highlight color.
        /// </summary>
        public readonly AtlasDebugColor32 Error;

        /// <summary>
        /// Creates a debug-map palette.
        /// </summary>
        public AtlasDebugMapPalette(
            AtlasDebugColor32 missing,
            AtlasDebugColor32 background,
            AtlasDebugColor32 low,
            AtlasDebugColor32 mid,
            AtlasDebugColor32 high,
            AtlasDebugColor32 water,
            AtlasDebugColor32 land,
            AtlasDebugColor32 coast,
            AtlasDebugColor32 mountain,
            AtlasDebugColor32 river,
            AtlasDebugColor32 warning,
            AtlasDebugColor32 error)
        {
            Missing = missing;
            Background = background;
            Low = low;
            Mid = mid;
            High = high;
            Water = water;
            Land = land;
            Coast = coast;
            Mountain = mountain;
            River = river;
            Warning = warning;
            Error = error;
        }

        /// <summary>
        /// Gets the production default Atlas debug-map palette.
        /// </summary>
        public static AtlasDebugMapPalette ProductionDefault
        {
            get
            {
                return new AtlasDebugMapPalette(
                    missing: AtlasDebugColor32.Magenta,
                    background: new AtlasDebugColor32(10, 12, 16, 255),
                    low: new AtlasDebugColor32(28, 56, 72, 255),
                    mid: new AtlasDebugColor32(70, 120, 72, 255),
                    high: new AtlasDebugColor32(230, 232, 220, 255),
                    water: new AtlasDebugColor32(24, 76, 150, 255),
                    land: new AtlasDebugColor32(92, 142, 72, 255),
                    coast: new AtlasDebugColor32(232, 214, 150, 255),
                    mountain: new AtlasDebugColor32(170, 170, 165, 255),
                    river: new AtlasDebugColor32(60, 155, 220, 255),
                    warning: new AtlasDebugColor32(255, 190, 70, 255),
                    error: new AtlasDebugColor32(255, 40, 80, 255));
            }
        }

        /// <summary>
        /// Gets a grayscale palette.
        /// </summary>
        public static AtlasDebugMapPalette Grayscale
        {
            get
            {
                return new AtlasDebugMapPalette(
                    missing: AtlasDebugColor32.Magenta,
                    background: AtlasDebugColor32.Black,
                    low: AtlasDebugColor32.Black,
                    mid: new AtlasDebugColor32(128, 128, 128, 255),
                    high: AtlasDebugColor32.White,
                    water: new AtlasDebugColor32(64, 64, 64, 255),
                    land: new AtlasDebugColor32(192, 192, 192, 255),
                    coast: AtlasDebugColor32.White,
                    mountain: AtlasDebugColor32.White,
                    river: new AtlasDebugColor32(160, 160, 160, 255),
                    warning: new AtlasDebugColor32(220, 220, 220, 255),
                    error: AtlasDebugColor32.White);
            }
        }

        /// <summary>
        /// Maps a boolean mask to land/background colors.
        /// </summary>
        /// <param name="isSet">Whether the mask is set.</param>
        /// <returns>Land color when set; otherwise, background color.</returns>
        public AtlasDebugColor32 Mask(
            bool isSet)
        {
            return isSet
                ? Land
                : Background;
        }

        /// <summary>
        /// Maps a land/water mask to land/water colors.
        /// </summary>
        /// <param name="isLand">Whether the sample is land.</param>
        /// <returns>Land color when true; otherwise, water color.</returns>
        public AtlasDebugColor32 LandWater(
            bool isLand)
        {
            return isLand
                ? Land
                : Water;
        }

        /// <summary>
        /// Maps a normalized byte value to a low/mid/high ramp.
        /// </summary>
        /// <param name="value">Normalized value in [0, 255].</param>
        /// <returns>Ramp color.</returns>
        public AtlasDebugColor32 Ramp(
            byte value)
        {
            if (value < 128)
            {
                return AtlasDebugColor32.Lerp(
                    Low,
                    Mid,
                    (byte)(value << 1));
            }

            return AtlasDebugColor32.Lerp(
                Mid,
                High,
                (byte)((value - 128) << 1));
        }

        /// <summary>
        /// Maps a signed integer value range to a low/mid/high ramp.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="minimum">Minimum expected value.</param>
        /// <param name="maximum">Maximum expected value.</param>
        /// <returns>Ramp color.</returns>
        public AtlasDebugColor32 Ramp(
            int value,
            int minimum,
            int maximum)
        {
            return Ramp(
                NormalizeToByte(
                    value,
                    minimum,
                    maximum));
        }

        /// <summary>
        /// Maps a signed long value range to a low/mid/high ramp.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="minimum">Minimum expected value.</param>
        /// <param name="maximum">Maximum expected value.</param>
        /// <returns>Ramp color.</returns>
        public AtlasDebugColor32 Ramp(
            long value,
            long minimum,
            long maximum)
        {
            return Ramp(
                NormalizeToByte(
                    value,
                    minimum,
                    maximum));
        }

        /// <summary>
        /// Maps a signed value into a centered blue/land/high ramp.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="negativeMagnitude">Magnitude treated as full negative.</param>
        /// <param name="positiveMagnitude">Magnitude treated as full positive.</param>
        /// <returns>Centered ramp color.</returns>
        public AtlasDebugColor32 SignedRamp(
            int value,
            int negativeMagnitude,
            int positiveMagnitude)
        {
            if (negativeMagnitude <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(negativeMagnitude),
                    negativeMagnitude,
                    "Negative magnitude must be positive.");
            }

            if (positiveMagnitude <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(positiveMagnitude),
                    positiveMagnitude,
                    "Positive magnitude must be positive.");
            }

            if (value < 0)
            {
                var t = NormalizeMagnitudeToByte(
                    -value,
                    negativeMagnitude);

                return AtlasDebugColor32.Lerp(
                    Mid,
                    Water,
                    t);
            }

            return AtlasDebugColor32.Lerp(
                Mid,
                High,
                NormalizeMagnitudeToByte(value, positiveMagnitude));
        }

        /// <summary>
        /// Determines whether this palette equals another palette.
        /// </summary>
        /// <param name="other">Palette to compare.</param>
        /// <returns><c>true</c> when all colors match.</returns>
        public bool Equals(
            AtlasDebugMapPalette other)
        {
            return Missing == other.Missing &&
                   Background == other.Background &&
                   Low == other.Low &&
                   Mid == other.Mid &&
                   High == other.High &&
                   Water == other.Water &&
                   Land == other.Land &&
                   Coast == other.Coast &&
                   Mountain == other.Mountain &&
                   River == other.River &&
                   Warning == other.Warning &&
                   Error == other.Error;
        }

        /// <summary>
        /// Determines whether this palette equals another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal palette.</returns>
        public override bool Equals(
            object obj)
        {
            return obj is AtlasDebugMapPalette other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        /// <returns>A managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ Missing.GetHashCode();
                hash = (hash * 397) ^ Background.GetHashCode();
                hash = (hash * 397) ^ Low.GetHashCode();
                hash = (hash * 397) ^ Mid.GetHashCode();
                hash = (hash * 397) ^ High.GetHashCode();
                hash = (hash * 397) ^ Water.GetHashCode();
                hash = (hash * 397) ^ Land.GetHashCode();
                hash = (hash * 397) ^ Coast.GetHashCode();
                hash = (hash * 397) ^ Mountain.GetHashCode();
                hash = (hash * 397) ^ River.GetHashCode();
                hash = (hash * 397) ^ Warning.GetHashCode();
                hash = (hash * 397) ^ Error.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic string.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasDebugMapPalette(Land={0}, Water={1}, Low={2}, Mid={3}, High={4})",
                Land,
                Water,
                Low,
                Mid,
                High);
        }

        /// <summary>
        /// Compares two palettes for equality.
        /// </summary>
        public static bool operator ==(
            AtlasDebugMapPalette left,
            AtlasDebugMapPalette right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two palettes for inequality.
        /// </summary>
        public static bool operator !=(
            AtlasDebugMapPalette left,
            AtlasDebugMapPalette right)
        {
            return !left.Equals(right);
        }

        private static byte NormalizeToByte(
            int value,
            int minimum,
            int maximum)
        {
            if (maximum <= minimum)
            {
                throw new ArgumentException(
                    "Maximum must be greater than minimum.");
            }

            if (value <= minimum)
            {
                return 0;
            }

            if (value >= maximum)
            {
                return 255;
            }

            var numerator = (long)(value - minimum) * 255L;
            var denominator = maximum - minimum;

            return (byte)((numerator + denominator / 2L) / denominator);
        }

        private static byte NormalizeToByte(
            long value,
            long minimum,
            long maximum)
        {
            if (maximum <= minimum)
            {
                throw new ArgumentException(
                    "Maximum must be greater than minimum.");
            }

            if (value <= minimum)
            {
                return 0;
            }

            if (value >= maximum)
            {
                return 255;
            }

            var numerator = (value - minimum) * 255L;
            var denominator = maximum - minimum;

            return (byte)((numerator + denominator / 2L) / denominator);
        }

        private static byte NormalizeMagnitudeToByte(
            int value,
            int magnitude)
        {
            if (value <= 0)
            {
                return 0;
            }

            if (value >= magnitude)
            {
                return 255;
            }

            return (byte)(((long)value * 255L + magnitude / 2L) / magnitude);
        }
    }
}