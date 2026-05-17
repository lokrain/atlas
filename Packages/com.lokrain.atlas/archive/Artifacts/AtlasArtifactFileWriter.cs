// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactFileWriter.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Write already captured AtlasArtifact instances to files.
// - Own artifact file-path and directory convenience only.
// - Delegate deterministic stream serialization to AtlasArtifactBinaryWriter.
// - Keep file I/O separate from binary schema, workspace capture, execution, and debug rendering.

using System;
using System.IO;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Writes managed Atlas artifacts to files.
    /// </summary>
    public static class AtlasArtifactFileWriter
    {
        /// <summary>
        /// Writes an artifact to a file.
        /// </summary>
        /// <param name="artifact">Artifact to write.</param>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="overwrite">Whether an existing file may be overwritten.</param>
        public static void WriteToFile(
            AtlasArtifact artifact,
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
                AtlasArtifactBinaryWriter.WriteToStream(
                    artifact,
                    stream);
            }
        }

        private static void ValidateFilePathOrThrow(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Artifact file path must not be null, empty, or whitespace.",
                    nameof(filePath));
            }
        }
    }
}
