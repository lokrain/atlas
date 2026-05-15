// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapWriter.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Write Atlas debug-map images to deterministic image files or streams.
// - Provide convenience helpers for byte masks, byte ramps, Int32 ramps, and artifact fields.
// - Keep debug image export separate from canonical generation, artifacts, execution, and Unity rendering.
//
// Design notes
// - This is debug/export tooling, not canonical world truth.
// - This does not own workspace memory.
// - This does not schedule jobs.
// - This does not complete JobHandles.
// - This does not depend on UnityEngine.Texture2D or ImageConversion.
// - TGA is used first because it is trivial, deterministic, dependency-free, and readable by common tools.
// - TGA output is uncompressed 32-bit BGRA with top-left origin.
// - Source AtlasDebugMapImage remains RGBA8 top-left origin.

using System;
using System.IO;
using Lokrain.Atlas.Artifacts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// Writes Atlas debug-map images to deterministic image output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugMapWriter"/> is intentionally dependency-free. It writes uncompressed
    /// TGA directly instead of using Unity image encoders. This keeps debug export available in
    /// runtime tooling, editor tooling, tests, and headless execution without introducing a
    /// UnityEngine rendering dependency.
    /// </para>
    ///
    /// <para>
    /// The writer can write an already-built <see cref="AtlasDebugMapImage"/> or build common
    /// debug images from byte masks, byte ramps, Int32 ramps, and serialized artifact field
    /// payloads.
    /// </para>
    /// </remarks>
    public static class AtlasDebugMapWriter
    {
        private const byte TgaImageTypeUncompressedTrueColor = 2;
        private const byte TgaPixelDepthBgra32 = 32;
        private const byte TgaImageDescriptorTopLeftWithAlpha8 = 0x28;

        /// <summary>
        /// Writes an RGBA debug-map image to an uncompressed 32-bit TGA file.
        /// </summary>
        /// <param name="image">Debug-map image.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        public static void WriteTgaToFile(
            AtlasDebugMapImage image,
            string filePath,
            bool overwrite = true)
        {
            ValidateFilePathOrThrow(filePath);

            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var mode = overwrite
                ? FileMode.Create
                : FileMode.CreateNew;

            using (var stream = new FileStream(
                       filePath,
                       mode,
                       FileAccess.Write,
                       FileShare.None))
            {
                WriteTgaToStream(
                    image,
                    stream);
            }
        }

        /// <summary>
        /// Writes an RGBA debug-map image to an uncompressed 32-bit TGA stream.
        /// </summary>
        /// <param name="image">Debug-map image.</param>
        /// <param name="stream">Destination stream.</param>
        /// <remarks>
        /// The stream remains open after writing.
        /// </remarks>
        public static void WriteTgaToStream(
            AtlasDebugMapImage image,
            Stream stream)
        {
            ValidateImageAndStreamOrThrow(
                image,
                stream);

            WriteTgaHeader(
                stream,
                image.Width,
                image.Height);

            WriteTgaBgraPayload(
                stream,
                image);
        }

        /// <summary>
        /// Builds a debug image from a byte mask and writes it to a TGA file.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="mask">Mask buffer, one byte per pixel.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteMaskBytesToTgaFile(
            int width,
            int height,
            byte[] mask,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var image = AtlasDebugMapImage.FromMaskBytes(
                width,
                height,
                mask,
                palette);

            WriteTgaToFile(
                image,
                filePath,
                overwrite);

            return image;
        }

        /// <summary>
        /// Builds a debug image from byte ramp samples and writes it to a TGA file.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="values">Value buffer, one byte per pixel.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteByteRampToTgaFile(
            int width,
            int height,
            byte[] values,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var image = AtlasDebugMapImage.FromByteRamp(
                width,
                height,
                values,
                palette);

            WriteTgaToFile(
                image,
                filePath,
                overwrite);

            return image;
        }

        /// <summary>
        /// Builds a debug image from Int32 ramp samples and writes it to a TGA file.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="values">Value buffer, one Int32 per pixel.</param>
        /// <param name="minimum">Minimum value mapped to palette low.</param>
        /// <param name="maximum">Maximum value mapped to palette high.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteInt32RampToTgaFile(
            int width,
            int height,
            int[] values,
            int minimum,
            int maximum,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var image = AtlasDebugMapImage.FromInt32Ramp(
                width,
                height,
                values,
                minimum,
                maximum,
                palette);

            WriteTgaToFile(
                image,
                filePath,
                overwrite);

            return image;
        }

        /// <summary>
        /// Builds a mask debug image from an artifact byte field and writes it to a TGA file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="stableId">Stable field id.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteArtifactByteMaskToTgaFile(
            AtlasArtifact artifact,
            StableDataId stableId,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var field = GetRequiredArtifactField(
                artifact,
                stableId);

            var bytes = GetLogicalFieldPayloadCopy(
                artifact,
                field);

            ValidateByteScalarFieldForPixelsOrThrow(
                field,
                width,
                height);

            return WriteMaskBytesToTgaFile(
                width,
                height,
                bytes,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Builds a mask debug image from an artifact byte field and writes it to a TGA file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="slot">Field slot.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteArtifactByteMaskToTgaFile(
            AtlasArtifact artifact,
            AtlasFieldSlot slot,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var field = GetRequiredArtifactField(
                artifact,
                slot);

            var bytes = GetLogicalFieldPayloadCopy(
                artifact,
                field);

            ValidateByteScalarFieldForPixelsOrThrow(
                field,
                width,
                height);

            return WriteMaskBytesToTgaFile(
                width,
                height,
                bytes,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Builds a byte-ramp debug image from an artifact byte field and writes it to a TGA file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="stableId">Stable field id.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteArtifactByteRampToTgaFile(
            AtlasArtifact artifact,
            StableDataId stableId,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var field = GetRequiredArtifactField(
                artifact,
                stableId);

            var bytes = GetLogicalFieldPayloadCopy(
                artifact,
                field);

            ValidateByteScalarFieldForPixelsOrThrow(
                field,
                width,
                height);

            return WriteByteRampToTgaFile(
                width,
                height,
                bytes,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Builds a byte-ramp debug image from an artifact byte field and writes it to a TGA file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="slot">Field slot.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteArtifactByteRampToTgaFile(
            AtlasArtifact artifact,
            AtlasFieldSlot slot,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var field = GetRequiredArtifactField(
                artifact,
                slot);

            var bytes = GetLogicalFieldPayloadCopy(
                artifact,
                field);

            ValidateByteScalarFieldForPixelsOrThrow(
                field,
                width,
                height);

            return WriteByteRampToTgaFile(
                width,
                height,
                bytes,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Builds an Int32-ramp debug image from an artifact Int32 field and writes it to a TGA file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="stableId">Stable field id.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="minimum">Minimum value mapped to palette low.</param>
        /// <param name="maximum">Maximum value mapped to palette high.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteArtifactInt32RampToTgaFile(
            AtlasArtifact artifact,
            StableDataId stableId,
            int width,
            int height,
            int minimum,
            int maximum,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var field = GetRequiredArtifactField(
                artifact,
                stableId);

            var bytes = GetLogicalFieldPayloadCopy(
                artifact,
                field);

            ValidateInt32ScalarFieldForPixelsOrThrow(
                field,
                width,
                height);

            return WriteInt32RampToTgaFile(
                width,
                height,
                DecodeInt32LittleEndian(bytes),
                minimum,
                maximum,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Builds an Int32-ramp debug image from an artifact Int32 field and writes it to a TGA file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="slot">Field slot.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="minimum">Minimum value mapped to palette low.</param>
        /// <param name="maximum">Maximum value mapped to palette high.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="palette">Debug color palette.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        /// <returns>The image that was written.</returns>
        public static AtlasDebugMapImage WriteArtifactInt32RampToTgaFile(
            AtlasArtifact artifact,
            AtlasFieldSlot slot,
            int width,
            int height,
            int minimum,
            int maximum,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            var field = GetRequiredArtifactField(
                artifact,
                slot);

            var bytes = GetLogicalFieldPayloadCopy(
                artifact,
                field);

            ValidateInt32ScalarFieldForPixelsOrThrow(
                field,
                width,
                height);

            return WriteInt32RampToTgaFile(
                width,
                height,
                DecodeInt32LittleEndian(bytes),
                minimum,
                maximum,
                filePath,
                palette,
                overwrite);
        }

        private static void WriteTgaHeader(
            Stream stream,
            int width,
            int height)
        {
            if (width > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    $"TGA width must be <= {ushort.MaxValue}.");
            }

            if (height > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    $"TGA height must be <= {ushort.MaxValue}.");
            }

            stream.WriteByte(0);                                      // ID length
            stream.WriteByte(0);                                      // color map type
            stream.WriteByte(TgaImageTypeUncompressedTrueColor);       // image type

            WriteUInt16(stream, 0);                                    // color map first entry
            WriteUInt16(stream, 0);                                    // color map length
            stream.WriteByte(0);                                      // color map entry size

            WriteUInt16(stream, 0);                                    // x origin
            WriteUInt16(stream, 0);                                    // y origin
            WriteUInt16(stream, checked((ushort)width));               // width
            WriteUInt16(stream, checked((ushort)height));              // height
            stream.WriteByte(TgaPixelDepthBgra32);                     // pixel depth
            stream.WriteByte(TgaImageDescriptorTopLeftWithAlpha8);     // 8 alpha bits + top-left origin
        }

        private static void WriteTgaBgraPayload(
            Stream stream,
            AtlasDebugMapImage image)
        {
            var rgba = image.GetRgbaCopy();

            for (var i = 0; i < rgba.Length; i += AtlasDebugMapImage.BytesPerPixel)
            {
                var r = rgba[i];
                var g = rgba[i + 1];
                var b = rgba[i + 2];
                var a = rgba[i + 3];

                stream.WriteByte(b);
                stream.WriteByte(g);
                stream.WriteByte(r);
                stream.WriteByte(a);
            }
        }

        private static AtlasArtifactField GetRequiredArtifactField(
            AtlasArtifact artifact,
            StableDataId stableId)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            return artifact.GetRequiredField(stableId);
        }

        private static AtlasArtifactField GetRequiredArtifactField(
            AtlasArtifact artifact,
            AtlasFieldSlot slot)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            return artifact.GetRequiredField(slot);
        }

        private static byte[] GetLogicalFieldPayloadCopy(
            AtlasArtifact artifact,
            AtlasArtifactField field)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            field.ValidateOrThrow(nameof(field));

            if (field.ByteLength > int.MaxValue)
            {
                throw new OverflowException(
                    $"Debug field '{field.DebugName}' logical byte length '{field.ByteLength}' exceeds managed array length capacity.");
            }

            var capacityBytes = artifact.GetFieldPayloadCopy(field);
            var logicalLength = checked((int)field.ByteLength);

            if (capacityBytes.Length == logicalLength)
            {
                return capacityBytes;
            }

            var logicalBytes = new byte[logicalLength];

            if (logicalLength > 0)
            {
                Array.Copy(
                    capacityBytes,
                    0,
                    logicalBytes,
                    0,
                    logicalLength);
            }

            return logicalBytes;
        }

        private static void ValidateByteScalarFieldForPixelsOrThrow(
            AtlasArtifactField field,
            int width,
            int height)
        {
            field.ValidateOrThrow(nameof(field));
            ValidateDebugDimensionsOrThrow(width, height);

            if (field.StorageFormat.ElementSize != 1)
            {
                throw new ArgumentException(
                    $"Debug field '{field.DebugName}' has element size '{field.StorageFormat.ElementSize}', but byte debug maps require element size 1.",
                    nameof(field));
            }

            var expectedPixels = checked(width * height);

            if (field.Length != expectedPixels ||
                field.ByteLength != expectedPixels)
            {
                throw new ArgumentException(
                    $"Debug field '{field.DebugName}' has length '{field.Length}' and byte length '{field.ByteLength}', but image '{width}x{height}' requires '{expectedPixels}' byte samples.",
                    nameof(field));
            }
        }

        private static void ValidateInt32ScalarFieldForPixelsOrThrow(
            AtlasArtifactField field,
            int width,
            int height)
        {
            field.ValidateOrThrow(nameof(field));
            ValidateDebugDimensionsOrThrow(width, height);

            if (field.StorageFormat.ElementSize != 4)
            {
                throw new ArgumentException(
                    $"Debug field '{field.DebugName}' has element size '{field.StorageFormat.ElementSize}', but Int32 debug maps require element size 4.",
                    nameof(field));
            }

            var expectedPixels = checked(width * height);
            var expectedBytes = checked(expectedPixels * 4);

            if (field.Length != expectedPixels ||
                field.ByteLength != expectedBytes)
            {
                throw new ArgumentException(
                    $"Debug field '{field.DebugName}' has length '{field.Length}' and byte length '{field.ByteLength}', but image '{width}x{height}' requires '{expectedPixels}' Int32 samples and '{expectedBytes}' bytes.",
                    nameof(field));
            }
        }

        private static int[] DecodeInt32LittleEndian(
            byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length % 4 != 0)
            {
                throw new ArgumentException(
                    $"Int32 byte buffer length '{bytes.Length}' is not divisible by 4.",
                    nameof(bytes));
            }

            var values = new int[bytes.Length / 4];

            for (var i = 0; i < values.Length; i++)
            {
                var offset = i * 4;

                values[i] =
                    bytes[offset] |
                    (bytes[offset + 1] << 8) |
                    (bytes[offset + 2] << 16) |
                    (bytes[offset + 3] << 24);
            }

            return values;
        }

        private static void WriteUInt16(
            Stream stream,
            ushort value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        private static void ValidateImageAndStreamOrThrow(
            AtlasDebugMapImage image,
            Stream stream)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            image.ValidateOrThrow();

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException(
                    "Destination stream must be writable.",
                    nameof(stream));
            }
        }

        private static void ValidateFilePathOrThrow(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Debug-map file path must not be null, empty, or whitespace.",
                    nameof(filePath));
            }
        }

        private static void ValidateDebugDimensionsOrThrow(
            int width,
            int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Debug-map width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "Debug-map height must be positive.");
            }

            _ = checked(width * height);
        }
    }
}