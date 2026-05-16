// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapExporter.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Execute Atlas debug-map export requests against captured artifacts.
// - Keep AtlasDebugMapRequest as request data only.
// - Keep run workflow independent from debug-map rendering and TGA writer details.
// - Keep artifact lookup and visualization dispatch out of request value objects.
//
// Design notes
// - This is managed debug/export orchestration, not canonical world truth.
// - This does not own workspace memory.
// - This does not schedule jobs.
// - This does not complete JobHandles.
// - This does not depend on UnityEngine.Texture2D or ImageConversion.
// - Source artifact payloads are immutable managed copies.
// - File encoding remains delegated to AtlasDebugMapWriter.

using System;
using Lokrain.Atlas.Artifacts;

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// Executes debug-map export requests against captured Atlas artifacts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugMapExporter"/> is the orchestration boundary between request metadata,
    /// captured artifact data, debug image construction, and file writing. It deliberately keeps
    /// <see cref="AtlasDebugMapRequest"/> as immutable request data.
    /// </para>
    /// </remarks>
    public static class AtlasDebugMapExporter
    {
        /// <summary>
        /// Exports a debug-map image from an already captured artifact and writes the configured file.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="request">Debug-map request.</param>
        /// <returns>The written debug-map image.</returns>
        public static AtlasDebugMapImage ExportFromArtifact(
            AtlasArtifact artifact,
            AtlasDebugMapRequest request)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.ValidateOrThrow();

            return request.Kind switch
            {
                AtlasDebugMapRequestKind.ByteMask => ExportByteMaskFromArtifact(
                                        artifact,
                                        request),
                AtlasDebugMapRequestKind.ByteRamp => ExportByteRampFromArtifact(
                                        artifact,
                                        request),
                AtlasDebugMapRequestKind.Int32Ramp => ExportInt32RampFromArtifact(
                                        artifact,
                                        request),
                _ => throw new InvalidOperationException(
                                        $"Unsupported debug-map request kind '{request.Kind}'."),
            };

        }

        private static AtlasDebugMapImage ExportByteMaskFromArtifact(
            AtlasArtifact artifact,
            AtlasDebugMapRequest request)
        {
            return request.TargetsStableId
                ? AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                    artifact,
                    request.StableId,
                    request.Width,
                    request.Height,
                    request.FilePath,
                    request.Palette,
                    request.Overwrite)
                : AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                    artifact,
                    request.Slot,
                    request.Width,
                    request.Height,
                    request.FilePath,
                    request.Palette,
                    request.Overwrite);
        }

        private static AtlasDebugMapImage ExportByteRampFromArtifact(
            AtlasArtifact artifact,
            AtlasDebugMapRequest request)
        {
            return request.TargetsStableId
                ? AtlasDebugMapWriter.WriteArtifactByteRampToTgaFile(
                    artifact,
                    request.StableId,
                    request.Width,
                    request.Height,
                    request.FilePath,
                    request.Palette,
                    request.Overwrite)
                : AtlasDebugMapWriter.WriteArtifactByteRampToTgaFile(
                    artifact,
                    request.Slot,
                    request.Width,
                    request.Height,
                    request.FilePath,
                    request.Palette,
                    request.Overwrite);
        }

        private static AtlasDebugMapImage ExportInt32RampFromArtifact(
            AtlasArtifact artifact,
            AtlasDebugMapRequest request)
        {
            return request.TargetsStableId
                ? AtlasDebugMapWriter.WriteArtifactInt32RampToTgaFile(
                    artifact,
                    request.StableId,
                    request.Width,
                    request.Height,
                    request.Minimum,
                    request.Maximum,
                    request.FilePath,
                    request.Palette,
                    request.Overwrite)
                : AtlasDebugMapWriter.WriteArtifactInt32RampToTgaFile(
                    artifact,
                    request.Slot,
                    request.Width,
                    request.Height,
                    request.Minimum,
                    request.Maximum,
                    request.FilePath,
                    request.Palette,
                    request.Overwrite);
        }
    }
}
