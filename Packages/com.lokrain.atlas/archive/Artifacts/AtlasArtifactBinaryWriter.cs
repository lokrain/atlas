// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactBinaryWriter.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Write AtlasArtifact instances to deterministic binary streams.
// - Own artifact binary schema serialization only.
// - Preserve artifact header, field table, shape-domain metadata, payload byte lengths, and payload bytes.
// - Keep stream serialization separate from file I/O, workspace capture, execution, and debug rendering.
//
// Design notes
// - The destination stream remains open after writing.
// - Binary output is explicitly little-endian.
// - Strings are written as UTF-8 byte length followed by bytes.
// - Payload bytes are written exactly as stored by AtlasArtifact.
// - Writer schema version is bumped because field rows include ShapeDomain and PayloadByteLength.
// - Logical artifacts and capacity snapshots are both supported by field.PayloadByteLength.

using System;
using System.IO;
using System.Text;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Pipelines;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Writes managed Atlas artifacts to deterministic binary streams.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasArtifactBinaryWriter"/> owns only the durable stream format. It does not
    /// create files, capture workspace memory, complete job handles, or render debug output.
    /// </para>
    ///
    /// <para>
    /// The writer leaves the supplied stream open. Callers own stream lifetime.
    /// </para>
    /// </remarks>
    public static class AtlasArtifactBinaryWriter
    {
        /// <summary>
        /// Binary writer format marker. ASCII "ATF1" in little-endian order.
        /// </summary>
        public const uint WriterFormatMarker = 0x41544631U;

        /// <summary>
        /// Binary writer schema version.
        /// </summary>
        /// <remarks>
        /// Version 2 includes field-table ShapeDomain metadata and field.PayloadByteLength.
        /// </remarks>
        public const ushort WriterSchemaVersion = 2;

        private static readonly Encoding Utf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        /// <summary>
        /// Writes an artifact to a deterministic binary stream.
        /// </summary>
        /// <param name="artifact">Artifact to write.</param>
        /// <param name="stream">Destination stream. The stream remains open after writing.</param>
        public static void WriteToStream(
            AtlasArtifact artifact,
            Stream stream)
        {
            ValidateArtifactAndStreamOrThrow(
                artifact,
                stream);

            WriteFilePreamble(stream);
            WriteHeader(stream, artifact.Header);
            WriteFieldTable(stream, artifact);
            WritePayload(stream, artifact);
        }

        private static void WriteFilePreamble(
            Stream stream)
        {
            WriteUInt32(stream, WriterFormatMarker);
            WriteUInt16(stream, WriterSchemaVersion);
            WriteUInt16(stream, 0);
        }

        private static void WriteHeader(
            Stream stream,
            AtlasArtifactHeader header)
        {
            header.ValidateOrThrow(nameof(header));

            WriteUInt64(stream, header.Magic);
            WriteUInt16(stream, header.FormatMajorVersion);
            WriteUInt16(stream, header.FormatMinorVersion);
            WriteUInt16(stream, header.HeaderVersion);
            WriteUInt16(stream, 0);

            WritePipelineId(stream, header.PipelineId);
            WriteFixedString64(stream, header.PipelineName);

            WriteInt32(stream, header.ContractCount);
            WriteInt32(stream, header.FieldCount);
            WriteInt32(stream, header.StageCount);
            WriteInt32(stream, header.OperationCount);
            WriteInt32(stream, header.BindingCount);
            WriteInt32(stream, header.PresentBindingCount);
            WriteInt32(stream, header.MissingOptionalBindingCount);

            WriteInt64(stream, header.TotalByteLength);
            WriteInt64(stream, header.TotalByteCapacity);

            WriteUInt64(stream, header.ContractHash);
            WriteUInt64(stream, header.ShapeHash);
            WriteBool(stream, header.HasContentHash);
            WriteUInt64(stream, header.ContentHash);
        }

        private static void WriteFieldTable(
            Stream stream,
            AtlasArtifact artifact)
        {
            WriteInt32(stream, artifact.FieldCount);

            for (var i = 0; i < artifact.FieldCount; i++)
            {
                WriteField(
                    stream,
                    artifact[i]);
            }
        }

        private static void WriteField(
            Stream stream,
            AtlasArtifactField field)
        {
            field.ValidateOrThrow(nameof(field));

            WriteInt32(stream, field.FieldIndex);
            WriteStableDataId(stream, field.StableId);
            WriteFieldSlot(stream, field.Slot);
            WriteInt32(stream, (int)field.Role);
            WriteStorageFormat(stream, field.StorageFormat);
            WriteShapeDomain(stream, field.ShapeDomain);
            WriteLengthShape(stream, field.DeclaredShape);
            WriteFixedString64(stream, field.DebugName);

            WriteInt32(stream, field.Length);
            WriteInt32(stream, field.Capacity);
            WriteInt64(stream, field.ByteLength);
            WriteInt64(stream, field.ByteCapacity);
            WriteInt64(stream, field.PayloadByteLength);
            WriteInt64(stream, field.ByteOffset);

            WriteBool(stream, field.HasContentHash);
            WriteUInt64(stream, field.ContentHash);
        }

        private static void WritePayload(
            Stream stream,
            AtlasArtifact artifact)
        {
            var payload = artifact.GetPayloadCopy();

            WriteInt64(stream, payload.LongLength);

            if (payload.Length > 0)
            {
                stream.Write(
                    payload,
                    0,
                    payload.Length);
            }
        }

        private static void WriteStableDataId(
            Stream stream,
            StableDataId id)
        {
            WriteUInt64(stream, id.High);
            WriteUInt64(stream, id.Low);
            WriteUInt16(stream, id.Version);
        }

        private static void WritePipelineId(
            Stream stream,
            AtlasPipelineId id)
        {
            WriteUInt64(stream, id.High);
            WriteUInt64(stream, id.Low);
            WriteUInt16(stream, id.Version);
        }

        private static void WriteFieldSlot(
            Stream stream,
            AtlasFieldSlot slot)
        {
            slot.ValidateOrThrow(nameof(slot));
            WriteInt32(stream, slot.Index);
        }

        private static void WriteStorageFormat(
            Stream stream,
            StorageFormat format)
        {
            format.ValidateOrThrow(nameof(format));

            WriteInt32(stream, (int)format.Kind);
            WriteInt32(stream, format.ElementSize);
            WriteInt32(stream, format.ElementAlignment);
            WriteUInt64(stream, format.ElementTypeHash);
        }

        private static void WriteShapeDomain(
            Stream stream,
            AtlasShapeDomain domain)
        {
            domain.ValidateOrThrow(nameof(domain));

            WriteInt32(stream, (int)domain.Kind);
            WriteFixedString64(stream, domain.Name);
            WriteBool(stream, domain.HasSourceField);
            WriteStableDataId(stream, domain.SourceFieldId);
        }

        private static void WriteLengthShape(
            Stream stream,
            LengthShape shape)
        {
            shape.ValidateOrThrow(nameof(shape));

            WriteInt32(stream, (int)shape.Kind);
            WriteInt32(stream, shape.FixedLength);
            WriteStableDataId(stream, shape.SourceFieldId);
            WriteFixedString64(stream, shape.Name);
            WriteInt32(stream, shape.CapacityMultiplierNumerator);
            WriteInt32(stream, shape.CapacityMultiplierDenominator);
            WriteInt32(stream, shape.CapacityPadding);
        }

        private static void WriteFixedString64(
            Stream stream,
            Unity.Collections.FixedString64Bytes value)
        {
            WriteUtf8String(
                stream,
                value.ToString());
        }

        private static void WriteUtf8String(
            Stream stream,
            string value)
        {
            if (value == null)
            {
                WriteInt32(stream, -1);
                return;
            }

            var bytes = Utf8.GetBytes(value);

            WriteInt32(stream, bytes.Length);

            if (bytes.Length > 0)
            {
                stream.Write(
                    bytes,
                    0,
                    bytes.Length);
            }
        }

        private static void WriteBool(
            Stream stream,
            bool value)
        {
            stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        private static void WriteUInt16(
            Stream stream,
            ushort value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        private static void WriteUInt32(
            Stream stream,
            uint value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        private static void WriteUInt64(
            Stream stream,
            ulong value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 32));
            stream.WriteByte((byte)(value >> 40));
            stream.WriteByte((byte)(value >> 48));
            stream.WriteByte((byte)(value >> 56));
        }

        private static void WriteInt32(
            Stream stream,
            int value)
        {
            WriteUInt32(
                stream,
                unchecked((uint)value));
        }

        private static void WriteInt64(
            Stream stream,
            long value)
        {
            WriteUInt64(
                stream,
                unchecked((ulong)value));
        }

        private static void ValidateArtifactAndStreamOrThrow(
            AtlasArtifact artifact,
            Stream stream)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            artifact.ValidateOrThrow();

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
    }
}
