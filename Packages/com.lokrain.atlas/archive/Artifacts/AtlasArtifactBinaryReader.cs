// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactBinaryReader.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Read AtlasArtifact instances from deterministic binary streams.
// - Own artifact binary schema deserialization only.
// - Preserve artifact header, field table, shape-domain metadata, payload byte lengths, and payload bytes.
// - Keep stream deserialization separate from file I/O, workspace capture, execution, and debug rendering.
//
// Design notes
// - The source stream remains open after reading.
// - Binary input is explicitly little-endian and matches AtlasArtifactBinaryWriter schema version 2.
// - Strings are read as UTF-8 byte length followed by bytes.
// - Payload bytes are read exactly as serialized by AtlasArtifactBinaryWriter.
// - Corrupt/truncated streams fail with InvalidDataException rather than producing partial artifacts.

using System;
using System.IO;
using System.Text;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Pipelines;
using Unity.Collections;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Reads managed Atlas artifacts from deterministic binary streams.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasArtifactBinaryReader"/> owns only the durable stream format. It does not
    /// open files, allocate workspace memory, schedule jobs, or render debug output.
    /// </para>
    ///
    /// <para>
    /// The reader leaves the supplied stream open. Callers own stream lifetime.
    /// </para>
    /// </remarks>
    public static class AtlasArtifactBinaryReader
    {
        private const int MaximumStringByteLength = 1024 * 1024;

        private static readonly Encoding Utf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        /// <summary>
        /// Reads an artifact from a deterministic binary stream.
        /// </summary>
        /// <param name="stream">Source stream. The stream remains open after reading.</param>
        /// <returns>The deserialized artifact.</returns>
        public static AtlasArtifact ReadFromStream(
            Stream stream)
        {
            ValidateStreamOrThrow(stream);

            ReadAndValidateFilePreamble(stream);

            var header = ReadHeader(stream);
            var fields = ReadFieldTable(stream, header.FieldCount);
            var payload = ReadPayload(stream);

            return AtlasArtifact.Create(
                header,
                fields,
                payload);
        }

        private static void ReadAndValidateFilePreamble(
            Stream stream)
        {
            var marker = ReadUInt32(stream);

            if (marker != AtlasArtifactBinaryWriter.WriterFormatMarker)
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream has writer marker '0x{marker:X8}', but expected '0x{AtlasArtifactBinaryWriter.WriterFormatMarker:X8}'.");
            }

            var schemaVersion = ReadUInt16(stream);

            if (schemaVersion != AtlasArtifactBinaryWriter.WriterSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream has writer schema version '{schemaVersion}', but expected '{AtlasArtifactBinaryWriter.WriterSchemaVersion}'.");
            }

            var reserved = ReadUInt16(stream);

            if (reserved != 0)
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream has non-zero reserved preamble value '{reserved}'.");
            }
        }

        private static AtlasArtifactHeader ReadHeader(
            Stream stream)
        {
            var magic = ReadUInt64(stream);
            var formatMajorVersion = ReadUInt16(stream);
            var formatMinorVersion = ReadUInt16(stream);
            var headerVersion = ReadUInt16(stream);
            var reserved = ReadUInt16(stream);

            if (reserved != 0)
            {
                throw new InvalidDataException(
                    $"Atlas artifact header has non-zero reserved value '{reserved}'.");
            }

            var pipelineId = ReadPipelineId(stream);
            var pipelineName = ReadFixedString64(stream, nameof(AtlasArtifactHeader.PipelineName));

            var contractCount = ReadInt32(stream);
            var fieldCount = ReadInt32(stream);
            var stageCount = ReadInt32(stream);
            var operationCount = ReadInt32(stream);
            var bindingCount = ReadInt32(stream);
            var presentBindingCount = ReadInt32(stream);
            var missingOptionalBindingCount = ReadInt32(stream);

            var totalByteLength = ReadInt64(stream);
            var totalByteCapacity = ReadInt64(stream);

            var contractHash = ReadUInt64(stream);
            var shapeHash = ReadUInt64(stream);
            var hasContentHash = ReadBool(stream);
            var contentHash = ReadUInt64(stream);

            ValidateCurrentHeaderSchemaOrThrow(
                magic,
                formatMajorVersion,
                formatMinorVersion,
                headerVersion);

            return AtlasArtifactHeader.CreateFromSerialized(
                pipelineId,
                pipelineName,
                contractCount,
                fieldCount,
                stageCount,
                operationCount,
                bindingCount,
                presentBindingCount,
                missingOptionalBindingCount,
                totalByteLength,
                totalByteCapacity,
                contractHash,
                shapeHash,
                contentHash,
                hasContentHash);
        }

        private static AtlasArtifactField[] ReadFieldTable(
            Stream stream,
            int expectedFieldCount)
        {
            if (expectedFieldCount < 0)
            {
                throw new InvalidDataException(
                    $"Atlas artifact header declares negative field count '{expectedFieldCount}'.");
            }

            var fieldCount = ReadInt32(stream);

            if (fieldCount != expectedFieldCount)
            {
                throw new InvalidDataException(
                    $"Atlas artifact field table declares field count '{fieldCount}', but header declares '{expectedFieldCount}'.");
            }

            var fields = new AtlasArtifactField[fieldCount];

            for (var i = 0; i < fieldCount; i++)
            {
                fields[i] = ReadField(stream);
            }

            return fields;
        }

        private static AtlasArtifactField ReadField(
            Stream stream)
        {
            var fieldIndex = ReadInt32(stream);
            var stableId = ReadStableDataId(stream);
            var slot = ReadFieldSlot(stream);
            var role = ReadEnum<AtlasFieldRole>(ReadInt32(stream), nameof(AtlasFieldRole));
            var storageFormat = ReadStorageFormat(stream);
            var shapeDomain = ReadShapeDomain(stream);
            var declaredShape = ReadLengthShape(stream);
            var debugName = ReadFixedString64(stream, nameof(AtlasArtifactField.DebugName));

            var length = ReadInt32(stream);
            var capacity = ReadInt32(stream);
            var byteLength = ReadInt64(stream);
            var byteCapacity = ReadInt64(stream);
            var payloadByteLength = ReadInt64(stream);
            var byteOffset = ReadInt64(stream);

            var hasContentHash = ReadBool(stream);
            var contentHash = ReadUInt64(stream);

            return AtlasArtifactField.CreateFromSerialized(
                fieldIndex,
                stableId,
                slot,
                role,
                storageFormat,
                shapeDomain,
                declaredShape,
                debugName,
                length,
                capacity,
                byteLength,
                byteCapacity,
                payloadByteLength,
                byteOffset,
                contentHash,
                hasContentHash);
        }

        private static byte[] ReadPayload(
            Stream stream)
        {
            var payloadByteLength = ReadInt64(stream);

            if (payloadByteLength < 0)
            {
                throw new InvalidDataException(
                    $"Atlas artifact payload byte length must be non-negative, but was '{payloadByteLength}'.");
            }

            if (payloadByteLength > int.MaxValue)
            {
                throw new InvalidDataException(
                    $"Atlas artifact payload byte length '{payloadByteLength}' exceeds the supported managed payload size '{int.MaxValue}'.");
            }

            var payload = new byte[(int)payloadByteLength];
            ReadExactly(stream, payload, payload.Length);
            return payload;
        }

        private static StorageFormat ReadStorageFormat(
            Stream stream)
        {
            var kind = ReadEnum<StorageKind>(ReadInt32(stream), nameof(StorageKind));
            var elementSize = ReadInt32(stream);
            var elementAlignment = ReadInt32(stream);
            var elementTypeHash = ReadUInt64(stream);

            return StorageFormat.CreateFromSerialized(
                kind,
                elementSize,
                elementAlignment,
                elementTypeHash);
        }

        private static AtlasShapeDomain ReadShapeDomain(
            Stream stream)
        {
            var kind = ReadEnum<AtlasShapeDomainKind>(ReadInt32(stream), nameof(AtlasShapeDomainKind));
            var name = ReadFixedString64(stream, nameof(AtlasShapeDomain.Name));
            var hasSourceField = ReadBool(stream);
            var sourceFieldId = ReadStableDataId(stream);

            return hasSourceField
                ? AtlasShapeDomain.CreateDerived(kind, name, sourceFieldId)
                : AtlasShapeDomain.Create(kind, name);
        }

        private static LengthShape ReadLengthShape(
            Stream stream)
        {
            var kind = ReadEnum<LengthShapeKind>(ReadInt32(stream), nameof(LengthShapeKind));
            var fixedLength = ReadInt32(stream);
            var sourceFieldId = ReadStableDataId(stream);
            var name = ReadFixedString64AllowingEmpty(stream);
            var capacityMultiplierNumerator = ReadInt32(stream);
            var capacityMultiplierDenominator = ReadInt32(stream);
            var capacityPadding = ReadInt32(stream);

            switch (kind)
            {
                case LengthShapeKind.Scalar:
                    return LengthShape.Scalar();

                case LengthShapeKind.Fixed:
                    return LengthShape.Fixed(fixedLength);

                case LengthShapeKind.MatchFieldLength:
                    return LengthShape.MatchFieldLength(sourceFieldId);

                case LengthShapeKind.QueryCount:
                    return LengthShape.QueryCount(name);

                case LengthShapeKind.ChunkCount:
                    return LengthShape.ChunkCount(name);

                case LengthShapeKind.CapacityFromField:
                    return LengthShape.CapacityFromField(
                        sourceFieldId,
                        capacityMultiplierNumerator,
                        capacityMultiplierDenominator,
                        capacityPadding);

                case LengthShapeKind.PrefixSumPayload:
                    return LengthShape.PrefixSumPayload(sourceFieldId);

                case LengthShapeKind.External:
                    return LengthShape.External(name);

                default:
                    throw new InvalidDataException(
                        $"Atlas artifact stream contains unsupported length-shape kind '{kind}'.");
            }
        }

        private static StableDataId ReadStableDataId(
            Stream stream)
        {
            return new StableDataId(
                ReadUInt64(stream),
                ReadUInt64(stream),
                ReadUInt16(stream));
        }

        private static AtlasPipelineId ReadPipelineId(
            Stream stream)
        {
            return new AtlasPipelineId(
                ReadUInt64(stream),
                ReadUInt64(stream),
                ReadUInt16(stream));
        }

        private static AtlasFieldSlot ReadFieldSlot(
            Stream stream)
        {
            return AtlasFieldSlot.FromIndex(ReadInt32(stream));
        }

        private static FixedString64Bytes ReadFixedString64(
            Stream stream,
            string fieldName)
        {
            var value = ReadUtf8String(stream);

            if (value == null)
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream contains a null fixed string for '{fieldName}'.");
            }

            return new FixedString64Bytes(value);
        }

        private static FixedString64Bytes ReadFixedString64AllowingEmpty(
            Stream stream)
        {
            var value = ReadUtf8String(stream);

            if (value == null)
            {
                throw new InvalidDataException(
                    "Atlas artifact stream contains a null fixed string where an empty fixed string was expected.");
            }

            return new FixedString64Bytes(value);
        }

        private static string ReadUtf8String(
            Stream stream)
        {
            var length = ReadInt32(stream);

            if (length == -1)
            {
                return null;
            }

            if (length < 0)
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream contains invalid UTF-8 string byte length '{length}'.");
            }

            if (length > MaximumStringByteLength)
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream contains UTF-8 string byte length '{length}', which exceeds maximum '{MaximumStringByteLength}'.");
            }

            if (length == 0)
            {
                return string.Empty;
            }

            var bytes = new byte[length];
            ReadExactly(stream, bytes, bytes.Length);

            try
            {
                return Utf8.GetString(bytes, 0, bytes.Length);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException(
                    "Atlas artifact stream contains invalid UTF-8 string bytes.",
                    exception);
            }
        }

        private static bool ReadBool(
            Stream stream)
        {
            var value = ReadByte(stream);

            if (value == 0)
            {
                return false;
            }

            if (value == 1)
            {
                return true;
            }

            throw new InvalidDataException(
                $"Atlas artifact stream contains invalid boolean byte '{value}'.");
        }

        private static ushort ReadUInt16(
            Stream stream)
        {
            var b0 = ReadByte(stream);
            var b1 = ReadByte(stream);

            return (ushort)(b0 | (b1 << 8));
        }

        private static uint ReadUInt32(
            Stream stream)
        {
            var b0 = ReadByte(stream);
            var b1 = ReadByte(stream);
            var b2 = ReadByte(stream);
            var b3 = ReadByte(stream);

            return (uint)b0 |
                   ((uint)b1 << 8) |
                   ((uint)b2 << 16) |
                   ((uint)b3 << 24);
        }

        private static ulong ReadUInt64(
            Stream stream)
        {
            var low = ReadUInt32(stream);
            var high = ReadUInt32(stream);

            return low | ((ulong)high << 32);
        }

        private static int ReadInt32(
            Stream stream)
        {
            return unchecked((int)ReadUInt32(stream));
        }

        private static long ReadInt64(
            Stream stream)
        {
            return unchecked((long)ReadUInt64(stream));
        }

        private static int ReadByte(
            Stream stream)
        {
            var value = stream.ReadByte();

            if (value < 0)
            {
                throw new InvalidDataException(
                    "Atlas artifact stream ended before the expected byte was available.");
            }

            return value;
        }

        private static void ReadExactly(
            Stream stream,
            byte[] buffer,
            int length)
        {
            var offset = 0;

            while (offset < length)
            {
                var read = stream.Read(
                    buffer,
                    offset,
                    length - offset);

                if (read <= 0)
                {
                    throw new InvalidDataException(
                        "Atlas artifact stream ended before the expected byte range was available.");
                }

                offset += read;
            }
        }

        private static TEnum ReadEnum<TEnum>(
            int value,
            string enumName)
            where TEnum : struct
        {
            var enumType = typeof(TEnum);
            var enumValue = Enum.ToObject(enumType, value);

            if (!Enum.IsDefined(enumType, enumValue))
            {
                throw new InvalidDataException(
                    $"Atlas artifact stream contains unknown {enumName} value '{value}'.");
            }

            return (TEnum)enumValue;
        }

        private static void ValidateCurrentHeaderSchemaOrThrow(
            ulong magic,
            ushort formatMajorVersion,
            ushort formatMinorVersion,
            ushort headerVersion)
        {
            if (magic != AtlasArtifactHeader.MagicValue)
            {
                throw new InvalidDataException(
                    $"Atlas artifact header has magic value '0x{magic:X16}', but expected '0x{AtlasArtifactHeader.MagicValue:X16}'.");
            }

            if (formatMajorVersion != AtlasArtifactHeader.CurrentFormatMajorVersion ||
                formatMinorVersion != AtlasArtifactHeader.CurrentFormatMinorVersion ||
                headerVersion != AtlasArtifactHeader.CurrentHeaderVersion)
            {
                throw new InvalidDataException(
                    $"Atlas artifact header version is '{formatMajorVersion}.{formatMinorVersion}/{headerVersion}', but expected '{AtlasArtifactHeader.CurrentFormatMajorVersion}.{AtlasArtifactHeader.CurrentFormatMinorVersion}/{AtlasArtifactHeader.CurrentHeaderVersion}'.");
            }
        }

        private static void ValidateStreamOrThrow(
            Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(
                    "Source stream must be readable.",
                    nameof(stream));
            }
        }
    }
}
