// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapImage.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Represent one managed RGBA debug-map image.
// - Own a detached managed byte buffer suitable for PNG/TGA/debug export.
// - Provide deterministic pixel access without depending on UnityEngine.
//
// Design notes
// - This is debug visualization data, not canonical world truth.
// - This owns managed RGBA bytes.
// - This does not own workspace memory.
// - This does not reference NativeArray, JobHandle, Texture2D, Sprite, GameObject, or renderer state.
// - Pixel byte layout is RGBA8.
// - Pixel origin is top-left for file/debug-view convention.
// - Debug-map writers can encode this image to disk without touching generation code.

using System;
using System.Globalization;

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// Managed RGBA debug-map image.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugMapImage"/> is a small managed image container used by Atlas debug
    /// export and editor tooling. It deliberately stores raw RGBA bytes instead of UnityEngine
    /// types so canonical generation, artifact tooling, and headless diagnostics remain independent
    /// of Unity rendering APIs.
    /// </para>
    ///
    /// <para>
    /// The image owns its byte buffer. Construction and copy APIs defensively copy input buffers so
    /// callers cannot mutate image contents behind the type boundary.
    /// </para>
    /// </remarks>
    public sealed class AtlasDebugMapImage
    {
        /// <summary>
        /// Number of bytes per RGBA8 pixel.
        /// </summary>
        public const int BytesPerPixel = 4;

        private readonly byte[] _rgba;

        private AtlasDebugMapImage(
            int width,
            int height,
            byte[] rgba,
            bool copy)
        {
            ValidateDimensionsOrThrow(
                width,
                height);

            ValidateBufferOrThrow(
                width,
                height,
                rgba);

            Width = width;
            Height = height;
            _rgba = copy
                ? CopyBytes(rgba)
                : rgba;
        }

        /// <summary>
        /// Gets the image width in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the image height in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the number of pixels.
        /// </summary>
        public int PixelCount => checked(Width * Height);

        /// <summary>
        /// Gets the number of RGBA bytes.
        /// </summary>
        public int ByteCount => _rgba.Length;

        /// <summary>
        /// Gets whether the image has no pixels.
        /// </summary>
        public bool IsEmpty => _rgba.Length == 0;

        /// <summary>
        /// Creates an image filled with transparent black.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <returns>A new managed debug-map image.</returns>
        public static AtlasDebugMapImage Create(
            int width,
            int height)
        {
            ValidateDimensionsOrThrow(
                width,
                height);

            return new AtlasDebugMapImage(
                width,
                height,
                new byte[checked(width * height * BytesPerPixel)],
                copy: false);
        }

        /// <summary>
        /// Creates an image filled with one color.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="color">Fill color.</param>
        /// <returns>A new managed debug-map image.</returns>
        public static AtlasDebugMapImage CreateFilled(
            int width,
            int height,
            AtlasDebugColor32 color)
        {
            var image = Create(
                width,
                height);

            image.Fill(color);

            return image;
        }

        /// <summary>
        /// Creates an image from an existing RGBA byte buffer.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="rgba">RGBA byte buffer.</param>
        /// <returns>A new managed debug-map image with copied bytes.</returns>
        public static AtlasDebugMapImage FromRgbaCopy(
            int width,
            int height,
            byte[] rgba)
        {
            return new AtlasDebugMapImage(
                width,
                height,
                rgba,
                copy: true);
        }

        /// <summary>
        /// Creates a debug image from a byte mask.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="mask">Mask bytes, one byte per pixel.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <returns>A new managed debug-map image.</returns>
        /// <remarks>
        /// A zero mask sample maps to background; any non-zero sample maps to land.
        /// </remarks>
        public static AtlasDebugMapImage FromMaskBytes(
            int width,
            int height,
            byte[] mask,
            AtlasDebugMapPalette palette)
        {
            ValidateDimensionsOrThrow(
                width,
                height);

            ValidateScalarBufferOrThrow(
                width,
                height,
                mask,
                nameof(mask));

            var image = Create(
                width,
                height);

            for (var i = 0; i < mask.Length; i++)
            {
                image.SetPixelByIndex(
                    i,
                    palette.Mask(mask[i] != 0));
            }

            return image;
        }

        /// <summary>
        /// Creates a debug image from signed integer samples using a palette ramp.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="values">Value buffer, one value per pixel.</param>
        /// <param name="minimum">Minimum value mapped to palette low.</param>
        /// <param name="maximum">Maximum value mapped to palette high.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <returns>A new managed debug-map image.</returns>
        public static AtlasDebugMapImage FromInt32Ramp(
            int width,
            int height,
            int[] values,
            int minimum,
            int maximum,
            AtlasDebugMapPalette palette)
        {
            ValidateDimensionsOrThrow(
                width,
                height);

            ValidateScalarBufferOrThrow(
                width,
                height,
                values,
                nameof(values));

            var image = Create(
                width,
                height);

            for (var i = 0; i < values.Length; i++)
            {
                image.SetPixelByIndex(
                    i,
                    palette.Ramp(values[i], minimum, maximum));
            }

            return image;
        }

        /// <summary>
        /// Creates a debug image from unsigned byte samples using a palette ramp.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="values">Value buffer, one value per pixel.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <returns>A new managed debug-map image.</returns>
        public static AtlasDebugMapImage FromByteRamp(
            int width,
            int height,
            byte[] values,
            AtlasDebugMapPalette palette)
        {
            ValidateDimensionsOrThrow(
                width,
                height);

            ValidateScalarBufferOrThrow(
                width,
                height,
                values,
                nameof(values));

            var image = Create(
                width,
                height);

            for (var i = 0; i < values.Length; i++)
            {
                image.SetPixelByIndex(
                    i,
                    palette.Ramp(values[i]));
            }

            return image;
        }

        /// <summary>
        /// Gets a pixel color by coordinate.
        /// </summary>
        /// <param name="x">Pixel x coordinate.</param>
        /// <param name="y">Pixel y coordinate.</param>
        /// <returns>The pixel color.</returns>
        public AtlasDebugColor32 GetPixel(
            int x,
            int y)
        {
            return GetPixelByIndex(
                GetPixelIndex(
                    x,
                    y));
        }

        /// <summary>
        /// Gets a pixel color by linear pixel index.
        /// </summary>
        /// <param name="pixelIndex">Linear pixel index.</param>
        /// <returns>The pixel color.</returns>
        public AtlasDebugColor32 GetPixelByIndex(
            int pixelIndex)
        {
            var byteIndex = GetByteIndexFromPixelIndex(pixelIndex);

            return new AtlasDebugColor32(
                _rgba[byteIndex],
                _rgba[byteIndex + 1],
                _rgba[byteIndex + 2],
                _rgba[byteIndex + 3]);
        }

        /// <summary>
        /// Sets a pixel color by coordinate.
        /// </summary>
        /// <param name="x">Pixel x coordinate.</param>
        /// <param name="y">Pixel y coordinate.</param>
        /// <param name="color">Pixel color.</param>
        public void SetPixel(
            int x,
            int y,
            AtlasDebugColor32 color)
        {
            SetPixelByIndex(
                GetPixelIndex(x, y),
                color);
        }

        /// <summary>
        /// Sets a pixel color by linear pixel index.
        /// </summary>
        /// <param name="pixelIndex">Linear pixel index.</param>
        /// <param name="color">Pixel color.</param>
        public void SetPixelByIndex(
            int pixelIndex,
            AtlasDebugColor32 color)
        {
            var byteIndex = GetByteIndexFromPixelIndex(pixelIndex);

            _rgba[byteIndex] = color.R;
            _rgba[byteIndex + 1] = color.G;
            _rgba[byteIndex + 2] = color.B;
            _rgba[byteIndex + 3] = color.A;
        }

        /// <summary>
        /// Fills the whole image with a color.
        /// </summary>
        /// <param name="color">Fill color.</param>
        public void Fill(
            AtlasDebugColor32 color)
        {
            for (var i = 0; i < _rgba.Length; i += BytesPerPixel)
            {
                _rgba[i] = color.R;
                _rgba[i + 1] = color.G;
                _rgba[i + 2] = color.B;
                _rgba[i + 3] = color.A;
            }
        }

        /// <summary>
        /// Creates a managed copy of the RGBA byte buffer.
        /// </summary>
        /// <returns>A new RGBA byte array.</returns>
        public byte[] GetRgbaCopy()
        {
            return CopyBytes(_rgba);
        }

        /// <summary>
        /// Copies the RGBA bytes into a destination array.
        /// </summary>
        /// <param name="destination">Destination byte array.</param>
        /// <param name="destinationIndex">Destination start index.</param>
        public void CopyRgbaTo(
            byte[] destination,
            int destinationIndex = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(destinationIndex),
                    destinationIndex,
                    "Destination index must be non-negative.");
            }

            if (destination.Length - destinationIndex < _rgba.Length)
            {
                throw new ArgumentException(
                    $"Destination has '{destination.Length - destinationIndex}' available bytes, but image requires '{_rgba.Length}'.",
                    nameof(destination));
            }

            Array.Copy(
                _rgba,
                0,
                destination,
                destinationIndex,
                _rgba.Length);
        }

        /// <summary>
        /// Creates a vertically flipped RGBA byte copy.
        /// </summary>
        /// <returns>A new RGBA byte array with rows in bottom-up order.</returns>
        /// <remarks>
        /// Some image encoders or GPU upload paths use bottom-left origin. The image itself remains
        /// top-left origin; this method only changes the returned copy.
        /// </remarks>
        public byte[] GetVerticallyFlippedRgbaCopy()
        {
            var copy = new byte[_rgba.Length];
            var rowByteCount = checked(Width * BytesPerPixel);

            for (var y = 0; y < Height; y++)
            {
                var sourceOffset = y * rowByteCount;
                var destinationOffset = (Height - 1 - y) * rowByteCount;

                Array.Copy(
                    _rgba,
                    sourceOffset,
                    copy,
                    destinationOffset,
                    rowByteCount);
            }

            return copy;
        }

        /// <summary>
        /// Gets the linear pixel index for a coordinate.
        /// </summary>
        /// <param name="x">Pixel x coordinate.</param>
        /// <param name="y">Pixel y coordinate.</param>
        /// <returns>Linear pixel index.</returns>
        public int GetPixelIndex(
            int x,
            int y)
        {
            if ((uint)x >= (uint)Width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    x,
                    $"Pixel x coordinate must be between 0 and {Width - 1}.");
            }

            if ((uint)y >= (uint)Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(y),
                    y,
                    $"Pixel y coordinate must be between 0 and {Height - 1}.");
            }

            return checked(y * Width + x);
        }

        /// <summary>
        /// Validates this image.
        /// </summary>
        public void ValidateOrThrow()
        {
            ValidateDimensionsOrThrow(
                Width,
                Height);

            ValidateBufferOrThrow(
                Width,
                Height,
                _rgba);
        }

        /// <summary>
        /// Returns a diagnostic image string.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasDebugMapImage({0}x{1}, Bytes={2})",
                Width,
                Height,
                ByteCount);
        }

        private int GetByteIndexFromPixelIndex(
            int pixelIndex)
        {
            if ((uint)pixelIndex >= (uint)PixelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pixelIndex),
                    pixelIndex,
                    $"Pixel index must be between 0 and {PixelCount - 1}.");
            }

            return checked(pixelIndex * BytesPerPixel);
        }

        private static void ValidateDimensionsOrThrow(
            int width,
            int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Debug-map image width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "Debug-map image height must be positive.");
            }

            _ = checked(width * height * BytesPerPixel);
        }

        private static void ValidateBufferOrThrow(
            int width,
            int height,
            byte[] rgba)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }

            var expectedLength = checked(width * height * BytesPerPixel);

            if (rgba.Length != expectedLength)
            {
                throw new ArgumentException(
                    $"RGBA buffer length '{rgba.Length}' does not match expected length '{expectedLength}' for image '{width}x{height}'.",
                    nameof(rgba));
            }
        }

        private static void ValidateScalarBufferOrThrow<T>(
            int width,
            int height,
            T[] values,
            string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var expectedLength = checked(width * height);

            if (values.Length != expectedLength)
            {
                throw new ArgumentException(
                    $"Scalar buffer length '{values.Length}' does not match expected pixel count '{expectedLength}' for image '{width}x{height}'.",
                    parameterName);
            }
        }

        private static byte[] CopyBytes(
            byte[] bytes)
        {
            var copy = new byte[bytes.Length];

            Array.Copy(
                bytes,
                copy,
                bytes.Length);

            return copy;
        }
    }
}