// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapExport.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Provide a thin high-level vertical-slice export façade for Atlas debug output.
// - Capture completed workspace contents into an AtlasArtifact.
// - Optionally write the artifact to disk.
// - Write one selected artifact field as a debug TGA image.
// - Keep debug export orchestration separate from canonical generation, execution, and artifact internals.
//
// Design notes
// - This is debug/export orchestration, not canonical world truth.
// - This does not schedule operations.
// - This does not allocate workspace memory.
// - This does not dispose workspace memory.
// - This does not render UnityEngine objects.
// - This does not depend on UnityEngine.Texture2D or ImageConversion.
// - Callers must complete execution before capture, unless they explicitly pass completeIfScheduled.
// - The artifact remains the durable output; the TGA is a human-visible debug derivative.

using System;
using Lokrain.Atlas.Artifacts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// High-level debug-map export helpers for completed Atlas execution output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDebugMapExport"/> is the temporary but production-shaped vertical-slice
    /// façade for getting visible output quickly:
    /// completed execution context -> managed artifact snapshot -> optional artifact file ->
    /// selected debug field TGA.
    /// </para>
    ///
    /// <para>
    /// This type intentionally does not know operation semantics. It only exports already-produced
    /// field payloads from an artifact. Selection is by stable field id or Contract-table slot.
    /// </para>
    ///
    /// <para>
    /// The debug TGA is not canonical data. The artifact is the durable output. The TGA is a
    /// derived view intended for diagnostics, editor tooling, and quick visual validation.
    /// </para>
    /// </remarks>
    public static class AtlasDebugMapExport
    {
        /// <summary>
        /// Captures a completed execution context, optionally writes the artifact, and writes a byte-mask field as a TGA.
        /// </summary>
        /// <param name="context">Completed execution context to capture.</param>
        /// <param name="stableId">Stable id of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportCompletedByteMask(
            AtlasExecutionContext context,
            StableDataId stableId,
            int width,
            int height,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            var artifact = CaptureAndMaybeWriteArtifact(
                context,
                artifactFilePath,
                overwrite,
                computeContentHashes);

            var image = AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                artifact,
                stableId,
                width,
                height,
                debugTgaFilePath,
                palette,
                overwrite);

            return new AtlasDebugMapExportResult(
                artifact,
                image,
                artifactFilePath,
                debugTgaFilePath);
        }

        /// <summary>
        /// Captures a completed execution context, optionally writes the artifact, and writes a byte-mask field as a TGA.
        /// </summary>
        /// <param name="context">Completed execution context to capture.</param>
        /// <param name="slot">Slot of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportCompletedByteMask(
            AtlasExecutionContext context,
            AtlasFieldSlot slot,
            int width,
            int height,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            var artifact = CaptureAndMaybeWriteArtifact(
                context,
                artifactFilePath,
                overwrite,
                computeContentHashes);

            var image = AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                artifact,
                slot,
                width,
                height,
                debugTgaFilePath,
                palette,
                overwrite);

            return new AtlasDebugMapExportResult(
                artifact,
                image,
                artifactFilePath,
                debugTgaFilePath);
        }

        /// <summary>
        /// Captures a completed execution context, optionally writes the artifact, and writes a byte-ramp field as a TGA.
        /// </summary>
        /// <param name="context">Completed execution context to capture.</param>
        /// <param name="stableId">Stable id of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportCompletedByteRamp(
            AtlasExecutionContext context,
            StableDataId stableId,
            int width,
            int height,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            var artifact = CaptureAndMaybeWriteArtifact(
                context,
                artifactFilePath,
                overwrite,
                computeContentHashes);

            var image = AtlasDebugMapWriter.WriteArtifactByteRampToTgaFile(
                artifact,
                stableId,
                width,
                height,
                debugTgaFilePath,
                palette,
                overwrite);

            return new AtlasDebugMapExportResult(
                artifact,
                image,
                artifactFilePath,
                debugTgaFilePath);
        }

        /// <summary>
        /// Captures a completed execution context, optionally writes the artifact, and writes a byte-ramp field as a TGA.
        /// </summary>
        /// <param name="context">Completed execution context to capture.</param>
        /// <param name="slot">Slot of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportCompletedByteRamp(
            AtlasExecutionContext context,
            AtlasFieldSlot slot,
            int width,
            int height,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            var artifact = CaptureAndMaybeWriteArtifact(
                context,
                artifactFilePath,
                overwrite,
                computeContentHashes);

            var image = AtlasDebugMapWriter.WriteArtifactByteRampToTgaFile(
                artifact,
                slot,
                width,
                height,
                debugTgaFilePath,
                palette,
                overwrite);

            return new AtlasDebugMapExportResult(
                artifact,
                image,
                artifactFilePath,
                debugTgaFilePath);
        }

        /// <summary>
        /// Captures a completed execution context, optionally writes the artifact, and writes an Int32-ramp field as a TGA.
        /// </summary>
        /// <param name="context">Completed execution context to capture.</param>
        /// <param name="stableId">Stable id of the Int32 field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="minimum">Minimum value mapped to palette low.</param>
        /// <param name="maximum">Maximum value mapped to palette high.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportCompletedInt32Ramp(
            AtlasExecutionContext context,
            StableDataId stableId,
            int width,
            int height,
            int minimum,
            int maximum,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            var artifact = CaptureAndMaybeWriteArtifact(
                context,
                artifactFilePath,
                overwrite,
                computeContentHashes);

            var image = AtlasDebugMapWriter.WriteArtifactInt32RampToTgaFile(
                artifact,
                stableId,
                width,
                height,
                minimum,
                maximum,
                debugTgaFilePath,
                palette,
                overwrite);

            return new AtlasDebugMapExportResult(
                artifact,
                image,
                artifactFilePath,
                debugTgaFilePath);
        }

        /// <summary>
        /// Captures a completed execution context, optionally writes the artifact, and writes an Int32-ramp field as a TGA.
        /// </summary>
        /// <param name="context">Completed execution context to capture.</param>
        /// <param name="slot">Slot of the Int32 field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="minimum">Minimum value mapped to palette low.</param>
        /// <param name="maximum">Maximum value mapped to palette high.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportCompletedInt32Ramp(
            AtlasExecutionContext context,
            AtlasFieldSlot slot,
            int width,
            int height,
            int minimum,
            int maximum,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            var artifact = CaptureAndMaybeWriteArtifact(
                context,
                artifactFilePath,
                overwrite,
                computeContentHashes);

            var image = AtlasDebugMapWriter.WriteArtifactInt32RampToTgaFile(
                artifact,
                slot,
                width,
                height,
                minimum,
                maximum,
                debugTgaFilePath,
                palette,
                overwrite);

            return new AtlasDebugMapExportResult(
                artifact,
                image,
                artifactFilePath,
                debugTgaFilePath);
        }

        /// <summary>
        /// Completes a scheduled execution result when requested, captures the context, optionally writes the artifact, and writes a byte-mask TGA.
        /// </summary>
        /// <param name="context">Execution context to capture after completion validation.</param>
        /// <param name="executionResult">Execution result returned by the operation runner.</param>
        /// <param name="stableId">Stable id of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="completeIfScheduled">Whether a scheduled result should be completed before capture.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportByteMaskFromExecutionResult(
            AtlasExecutionContext context,
            AtlasExecutionResult executionResult,
            StableDataId stableId,
            int width,
            int height,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool completeIfScheduled,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            ValidateExecutionResultReadyForCaptureOrThrow(
                executionResult,
                completeIfScheduled);

            return ExportCompletedByteMask(
                context,
                stableId,
                width,
                height,
                debugTgaFilePath,
                artifactFilePath,
                palette,
                overwrite,
                computeContentHashes);
        }

        /// <summary>
        /// Completes a scheduled execution result when requested, captures the context, optionally writes the artifact, and writes a byte-mask TGA.
        /// </summary>
        /// <param name="context">Execution context to capture after completion validation.</param>
        /// <param name="executionResult">Execution result returned by the operation runner.</param>
        /// <param name="slot">Slot of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="artifactFilePath">Optional artifact path. Pass null or empty to skip artifact file writing.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="completeIfScheduled">Whether a scheduled result should be completed before capture.</param>
        /// <param name="overwrite">Whether existing files may be overwritten.</param>
        /// <param name="computeContentHashes">Whether artifact capture computes content hashes.</param>
        /// <returns>The captured artifact and debug image.</returns>
        public static AtlasDebugMapExportResult ExportByteMaskFromExecutionResult(
            AtlasExecutionContext context,
            AtlasExecutionResult executionResult,
            AtlasFieldSlot slot,
            int width,
            int height,
            string debugTgaFilePath,
            string artifactFilePath,
            AtlasDebugMapPalette palette,
            bool completeIfScheduled,
            bool overwrite = true,
            bool computeContentHashes = true)
        {
            ValidateExecutionResultReadyForCaptureOrThrow(
                executionResult,
                completeIfScheduled);

            return ExportCompletedByteMask(
                context,
                slot,
                width,
                height,
                debugTgaFilePath,
                artifactFilePath,
                palette,
                overwrite,
                computeContentHashes);
        }

        /// <summary>
        /// Writes an already captured artifact byte-mask field to TGA without recapturing workspace memory.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="stableId">Stable id of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether an existing TGA may be overwritten.</param>
        /// <returns>The written debug image.</returns>
        public static AtlasDebugMapImage ExportArtifactByteMask(
            AtlasArtifact artifact,
            StableDataId stableId,
            int width,
            int height,
            string debugTgaFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            return AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                artifact,
                stableId,
                width,
                height,
                debugTgaFilePath,
                palette,
                overwrite);
        }

        /// <summary>
        /// Writes an already captured artifact byte-mask field to TGA without recapturing workspace memory.
        /// </summary>
        /// <param name="artifact">Source artifact.</param>
        /// <param name="slot">Slot of the byte field to visualize.</param>
        /// <param name="width">Debug image width in pixels.</param>
        /// <param name="height">Debug image height in pixels.</param>
        /// <param name="debugTgaFilePath">Destination TGA path.</param>
        /// <param name="palette">Debug-map palette.</param>
        /// <param name="overwrite">Whether an existing TGA may be overwritten.</param>
        /// <returns>The written debug image.</returns>
        public static AtlasDebugMapImage ExportArtifactByteMask(
            AtlasArtifact artifact,
            AtlasFieldSlot slot,
            int width,
            int height,
            string debugTgaFilePath,
            AtlasDebugMapPalette palette,
            bool overwrite = true)
        {
            return AtlasDebugMapWriter.WriteArtifactByteMaskToTgaFile(
                artifact,
                slot,
                width,
                height,
                debugTgaFilePath,
                palette,
                overwrite);
        }

        private static AtlasArtifact CaptureAndMaybeWriteArtifact(
            AtlasExecutionContext context,
            string artifactFilePath,
            bool overwrite,
            bool computeContentHashes)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var artifact = AtlasArtifactCapture.Capture(
                context,
                computeContentHashes);

            if (!string.IsNullOrWhiteSpace(artifactFilePath))
            {
                AtlasArtifactFileWriter.WriteToFile(
                    artifact,
                    artifactFilePath,
                    overwrite);
            }

            return artifact;
        }

        private static void ValidateExecutionResultReadyForCaptureOrThrow(
            AtlasExecutionResult executionResult,
            bool completeIfScheduled)
        {
            if (executionResult == null)
            {
                throw new ArgumentNullException(nameof(executionResult));
            }

            executionResult.ThrowIfFailed();

            if (executionResult.IsCompleted)
            {
                return;
            }

            if (!executionResult.IsScheduled)
            {
                throw new InvalidOperationException(
                    $"Atlas execution result status '{executionResult.Status}' is not ready for debug export.");
            }

            if (!completeIfScheduled)
            {
                throw new InvalidOperationException(
                    "Atlas execution result is scheduled but not completed. Complete the final dependency before debug export, or pass completeIfScheduled: true.");
            }

            executionResult.FinalDependency.Complete();
        }
    }

    /// <summary>
    /// Result of one high-level Atlas debug-map export operation.
    /// </summary>
    public sealed class AtlasDebugMapExportResult
    {
        /// <summary>
        /// Captured artifact used by the debug export.
        /// </summary>
        public readonly AtlasArtifact Artifact;

        /// <summary>
        /// Debug image written by the export.
        /// </summary>
        public readonly AtlasDebugMapImage Image;

        /// <summary>
        /// Artifact path written by the export, or null/empty when artifact file writing was skipped.
        /// </summary>
        public readonly string ArtifactFilePath;

        /// <summary>
        /// Debug TGA path written by the export.
        /// </summary>
        public readonly string DebugTgaFilePath;

        /// <summary>
        /// Creates a debug-map export result.
        /// </summary>
        /// <param name="artifact">Captured artifact.</param>
        /// <param name="image">Written debug image.</param>
        /// <param name="artifactFilePath">Artifact path, or null/empty when skipped.</param>
        /// <param name="debugTgaFilePath">Debug TGA path.</param>
        public AtlasDebugMapExportResult(
            AtlasArtifact artifact,
            AtlasDebugMapImage image,
            string artifactFilePath,
            string debugTgaFilePath)
        {
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            Image = image ?? throw new ArgumentNullException(nameof(image));
            ArtifactFilePath = artifactFilePath;
            DebugTgaFilePath = debugTgaFilePath;

            if (string.IsNullOrWhiteSpace(debugTgaFilePath))
            {
                throw new ArgumentException(
                    "Debug TGA file path must not be null, empty, or whitespace.",
                    nameof(debugTgaFilePath));
            }
        }

        /// <summary>
        /// Gets whether an artifact file path was supplied.
        /// </summary>
        public bool WroteArtifactFile => !string.IsNullOrWhiteSpace(ArtifactFilePath);

        /// <summary>
        /// Gets whether a debug TGA file path was supplied.
        /// </summary>
        public bool WroteDebugTgaFile => !string.IsNullOrWhiteSpace(DebugTgaFilePath);

        /// <summary>
        /// Returns a diagnostic export result string.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return
                $"AtlasDebugMapExportResult(ArtifactFields={Artifact.FieldCount}, Image={Image.Width}x{Image.Height}, ArtifactFile='{ArtifactFilePath}', DebugTgaFile='{DebugTgaFilePath}')";
        }
    }
}