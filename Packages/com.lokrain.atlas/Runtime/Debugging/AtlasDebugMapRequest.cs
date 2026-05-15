// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapRequest.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Represent one explicit debug-map export request.
// - Select one artifact/workspace field by StableDataId or AtlasFieldSlot.
// - Define the image dimensions, visualization mode, output path, palette, and overwrite policy.
// - Keep debug-map export configuration out of run workflow parameter lists.
//
// Design notes
// - This is managed orchestration data.
// - This is debug/export configuration, not canonical world truth.
// - This does not own workspace memory.
// - This does not write files.
// - This does not render UnityEngine objects.
// - StableDataId zero/default is valid.
// - AtlasFieldSlot zero/default is valid.
// - Target presence is represented by TargetKind, not by invalid sentinel values.
// - default(AtlasDebugMapRequest) is not concrete.
// - Use explicit factory methods instead of partially initialized requests.

using System;
using System.Globalization;
using Lokrain.Atlas.Artifacts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// Explicit request to export one Atlas debug-map image.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugMapRequest"/> is the production orchestration boundary between a run
    /// workflow and debug-map export. It makes field selection, visualization mode, dimensions,
    /// palette, path, and overwrite policy explicit without mixing them into workflow call
    /// signatures as loose optional parameters.
    /// </para>
    ///
    /// <para>
    /// The request selects exactly one field, either by <see cref="StableId"/> or by
    /// <see cref="Slot"/>. Both zero/default stable ids and zero/default slots are valid, so target
    /// presence is represented by <see cref="TargetKind"/>.
    /// </para>
    /// </remarks>
    public sealed class AtlasDebugMapRequest
    {
        private AtlasDebugMapRequest(
            AtlasDebugMapRequestKind kind,
            AtlasDebugMapTargetKind targetKind,
            StableDataId stableId,
            AtlasFieldSlot slot,
            int width,
            int height,
            int minimum,
            int maximum,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite)
        {
            ValidateConstructorArgumentsOrThrow(
                kind,
                targetKind,
                stableId,
                slot,
                width,
                height,
                minimum,
                maximum,
                filePath);

            Kind = kind;
            TargetKind = targetKind;
            StableId = stableId;
            Slot = slot;
            Width = width;
            Height = height;
            Minimum = minimum;
            Maximum = maximum;
            FilePath = filePath;
            Palette = palette;
            Overwrite = overwrite;
        }

        /// <summary>
        /// Gets the requested visualization mode.
        /// </summary>
        public AtlasDebugMapRequestKind Kind { get; }

        /// <summary>
        /// Gets the field target selector kind.
        /// </summary>
        public AtlasDebugMapTargetKind TargetKind { get; }

        /// <summary>
        /// Gets the target stable field id when <see cref="TargetKind"/> is <see cref="AtlasDebugMapTargetKind.StableId"/>.
        /// </summary>
        public StableDataId StableId { get; }

        /// <summary>
        /// Gets the target field slot when <see cref="TargetKind"/> is <see cref="AtlasDebugMapTargetKind.Slot"/>.
        /// </summary>
        public AtlasFieldSlot Slot { get; }

        /// <summary>
        /// Gets the debug image width in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the debug image height in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the minimum value for Int32 ramp requests.
        /// </summary>
        public int Minimum { get; }

        /// <summary>
        /// Gets the maximum value for Int32 ramp requests.
        /// </summary>
        public int Maximum { get; }

        /// <summary>
        /// Gets the destination TGA file path.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the debug color palette.
        /// </summary>
        public AtlasDebugMapPalette Palette { get; }

        /// <summary>
        /// Gets whether an existing output file may be overwritten.
        /// </summary>
        public bool Overwrite { get; }

        /// <summary>
        /// Gets whether this request targets a field by stable id.
        /// </summary>
        public bool TargetsStableId => TargetKind == AtlasDebugMapTargetKind.StableId;

        /// <summary>
        /// Gets whether this request targets a field by slot.
        /// </summary>
        public bool TargetsSlot => TargetKind == AtlasDebugMapTargetKind.Slot;

        /// <summary>
        /// Gets the requested pixel count.
        /// </summary>
        public int PixelCount => checked(Width * Height);

        /// <summary>
        /// Creates a byte-mask debug-map request targeting a stable field id.
        /// </summary>
        public static AtlasDebugMapRequest ByteMask(
            StableDataId stableId,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return new AtlasDebugMapRequest(
                AtlasDebugMapRequestKind.ByteMask,
                AtlasDebugMapTargetKind.StableId,
                stableId,
                default,
                width,
                height,
                minimum: 0,
                maximum: 0,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Creates a byte-mask debug-map request targeting a field slot.
        /// </summary>
        public static AtlasDebugMapRequest ByteMask(
            AtlasFieldSlot slot,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            slot.ValidateOrThrow(nameof(slot));

            return new AtlasDebugMapRequest(
                AtlasDebugMapRequestKind.ByteMask,
                AtlasDebugMapTargetKind.Slot,
                default,
                slot,
                width,
                height,
                minimum: 0,
                maximum: 0,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Creates a byte-ramp debug-map request targeting a stable field id.
        /// </summary>
        public static AtlasDebugMapRequest ByteRamp(
            StableDataId stableId,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return new AtlasDebugMapRequest(
                AtlasDebugMapRequestKind.ByteRamp,
                AtlasDebugMapTargetKind.StableId,
                stableId,
                default,
                width,
                height,
                minimum: 0,
                maximum: 255,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Creates a byte-ramp debug-map request targeting a field slot.
        /// </summary>
        public static AtlasDebugMapRequest ByteRamp(
            AtlasFieldSlot slot,
            int width,
            int height,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            slot.ValidateOrThrow(nameof(slot));

            return new AtlasDebugMapRequest(
                AtlasDebugMapRequestKind.ByteRamp,
                AtlasDebugMapTargetKind.Slot,
                default,
                slot,
                width,
                height,
                minimum: 0,
                maximum: 255,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Creates an Int32-ramp debug-map request targeting a stable field id.
        /// </summary>
        public static AtlasDebugMapRequest Int32Ramp(
            StableDataId stableId,
            int width,
            int height,
            int minimum,
            int maximum,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            return new AtlasDebugMapRequest(
                AtlasDebugMapRequestKind.Int32Ramp,
                AtlasDebugMapTargetKind.StableId,
                stableId,
                default,
                width,
                height,
                minimum,
                maximum,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Creates an Int32-ramp debug-map request targeting a field slot.
        /// </summary>
        public static AtlasDebugMapRequest Int32Ramp(
            AtlasFieldSlot slot,
            int width,
            int height,
            int minimum,
            int maximum,
            string filePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            slot.ValidateOrThrow(nameof(slot));

            return new AtlasDebugMapRequest(
                AtlasDebugMapRequestKind.Int32Ramp,
                AtlasDebugMapTargetKind.Slot,
                default,
                slot,
                width,
                height,
                minimum,
                maximum,
                filePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Validates this request.
        /// </summary>
        public void ValidateOrThrow()
        {
            ValidateConstructorArgumentsOrThrow(
                Kind,
                TargetKind,
                StableId,
                Slot,
                Width,
                Height,
                Minimum,
                Maximum,
                FilePath);
        }

        /// <summary>
        /// Writes the requested debug map from an already captured artifact.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <returns>The written debug-map image.</returns>
        public AtlasDebugMapImage ExportFromArtifact(
            AtlasArtifact artifact)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            ValidateOrThrow();

            switch (Kind)
            {
                case AtlasDebugMapRequestKind.ByteMask:
                    return TargetsStableId
                        ? AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                            artifact,
                            StableId,
                            Width,
                            Height,
                            FilePath,
                            Palette,
                            Overwrite)
                        : AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                            artifact,
                            Slot,
                            Width,
                            Height,
                            FilePath,
                            Palette,
                            Overwrite);

                case AtlasDebugMapRequestKind.ByteRamp:
                    return TargetsStableId
                        ? AtlasDebugMapWriter.WriteArtifactByteRampToTgaFile(
                            artifact,
                            StableId,
                            Width,
                            Height,
                            FilePath,
                            Palette,
                            Overwrite)
                        : AtlasDebugMapWriter.WriteArtifactByteRampToTgaFile(
                            artifact,
                            Slot,
                            Width,
                            Height,
                            FilePath,
                            Palette,
                            Overwrite);

                case AtlasDebugMapRequestKind.Int32Ramp:
                    return TargetsStableId
                        ? AtlasDebugMapWriter.WriteArtifactInt32RampToTgaFile(
                            artifact,
                            StableId,
                            Width,
                            Height,
                            Minimum,
                            Maximum,
                            FilePath,
                            Palette,
                            Overwrite)
                        : AtlasDebugMapWriter.WriteArtifactInt32RampToTgaFile(
                            artifact,
                            Slot,
                            Width,
                            Height,
                            Minimum,
                            Maximum,
                            FilePath,
                            Palette,
                            Overwrite);

                default:
                    throw new InvalidOperationException(
                        $"Unsupported debug-map request kind '{Kind}'.");
            }
        }

        /// <summary>
        /// Returns a diagnostic request string.
        /// </summary>
        public override string ToString()
        {
            var target = TargetsStableId
                ? StableId.ToString()
                : Slot.ToString();

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasDebugMapRequest(Kind={0}, TargetKind={1}, Target={2}, Size={3}x{4}, File='{5}')",
                Kind,
                TargetKind,
                target,
                Width,
                Height,
                FilePath);
        }

        private static void ValidateConstructorArgumentsOrThrow(
            AtlasDebugMapRequestKind kind,
            AtlasDebugMapTargetKind targetKind,
            StableDataId stableId,
            AtlasFieldSlot slot,
            int width,
            int height,
            int minimum,
            int maximum,
            string filePath)
        {
            if (kind == AtlasDebugMapRequestKind.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "Debug-map request kind must be concrete.");
            }

            if (targetKind == AtlasDebugMapTargetKind.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetKind),
                    targetKind,
                    "Debug-map request target kind must be concrete.");
            }

            if (targetKind != AtlasDebugMapTargetKind.StableId &&
                targetKind != AtlasDebugMapTargetKind.Slot)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetKind),
                    targetKind,
                    "Unsupported debug-map target kind.");
            }

            stableId.ValidateOrThrow(nameof(stableId));
            slot.ValidateOrThrow(nameof(slot));

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

            if (kind == AtlasDebugMapRequestKind.Int32Ramp &&
                maximum <= minimum)
            {
                throw new ArgumentException(
                    "Int32 debug-map ramp maximum must be greater than minimum.");
            }

            if (kind != AtlasDebugMapRequestKind.Int32Ramp &&
                minimum != 0)
            {
                throw new ArgumentException(
                    "Only Int32 debug-map ramp requests may use a non-zero minimum.");
            }

            if (kind == AtlasDebugMapRequestKind.ByteMask &&
                maximum != 0)
            {
                throw new ArgumentException(
                    "Byte-mask debug-map requests must use maximum zero.");
            }

            if (kind == AtlasDebugMapRequestKind.ByteRamp &&
                maximum != 255)
            {
                throw new ArgumentException(
                    "Byte-ramp debug-map requests must use maximum 255.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Debug-map output file path must not be null, empty, or whitespace.",
                    nameof(filePath));
            }
        }
    }
}