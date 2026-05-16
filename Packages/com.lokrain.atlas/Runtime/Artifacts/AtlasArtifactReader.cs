// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactReader.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Provide a thin compatibility facade for artifact stream/file reading.
// - Delegate deterministic binary deserialization to AtlasArtifactBinaryReader.
// - Delegate file-path handling to AtlasArtifactFileReader.
//
// Design notes
// - This class intentionally contains no binary schema code.
// - New code should prefer AtlasArtifactBinaryReader and AtlasArtifactFileReader directly.

using System.IO;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Thin facade over artifact binary stream and file reading.
    /// </summary>
    public static class AtlasArtifactReader
    {
        /// <summary>
        /// Reads an artifact from a stream.
        /// </summary>
        public static AtlasArtifact ReadFromStream(
            Stream stream)
        {
            return AtlasArtifactBinaryReader.ReadFromStream(stream);
        }

        /// <summary>
        /// Reads an artifact from a file.
        /// </summary>
        public static AtlasArtifact ReadFromFile(
            string filePath)
        {
            return AtlasArtifactFileReader.ReadFromFile(filePath);
        }
    }
}
