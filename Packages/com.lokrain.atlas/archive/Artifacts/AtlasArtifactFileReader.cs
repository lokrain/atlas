// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactFileReader.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Read already captured AtlasArtifact instances from files.
// - Own artifact file-path convenience only.
// - Delegate deterministic stream deserialization to AtlasArtifactBinaryReader.
// - Keep file I/O separate from binary schema, workspace capture, execution, and debug rendering.

using System;
using System.IO;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Reads managed Atlas artifacts from files.
    /// </summary>
    public static class AtlasArtifactFileReader
    {
        /// <summary>
        /// Reads an artifact from a file.
        /// </summary>
        /// <param name="filePath">Source artifact file path.</param>
        /// <returns>The deserialized artifact.</returns>
        public static AtlasArtifact ReadFromFile(
            string filePath)
        {
            ValidateFilePathOrThrow(filePath);

            using (var stream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            {
                return AtlasArtifactBinaryReader.ReadFromStream(stream);
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
