// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifact.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Represent one immutable managed Atlas artifact envelope.
// - Own a managed copy of serialized field payload bytes.
// - Preserve artifact header and field table metadata.
// - Provide durable field-payload lookup by field index, slot, or stable id.
//
// Design notes
// - This is durable output data.
// - This owns managed payload bytes, not workspace native memory.
// - This does not own or dispose AtlasWorkspace.
// - This does not expose mutable payload storage.
// - This does not contain JobHandle.
// - This does not render debug output.
// - Field payload layout is contiguous, deterministic, and field-table ordered.
// - Payload byte offsets are artifact-payload-relative.
// - Content hash zero is valid; hash presence is explicit in header/field metadata.
// - Header.TotalByteLength is logical content size metadata.
// - Header.TotalByteCapacity is allocation capacity metadata.
// - PayloadByteLength is the actual serialized artifact payload byte count.
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Immutable managed Atlas artifact envelope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasArtifact"/> stores artifact metadata in <see cref="Header"/>,
    /// per-field layout in the field table, and a managed payload byte copy detached from
    /// workspace-owned native memory.
    /// </para>
    ///
    /// <para>
    /// The artifact payload is serialized according to each field row's
    /// <see cref="AtlasArtifactField.PayloadByteLength"/>. Logical artifact capture writes
    /// <see cref="AtlasArtifactField.ByteLength"/> bytes for each field and preserves
    /// <see cref="AtlasArtifactField.ByteCapacity"/> as metadata only.
    /// </para>
    ///
    /// <para>
    /// Existing manually constructed artifacts may still use capacity payload rows. Validation
    /// accepts either logical or capacity payload rows as long as offsets and aggregate metadata are
    /// internally consistent.
    /// </para>
    /// </remarks>
    public sealed class AtlasArtifact :
        IReadOnlyList<AtlasArtifactField>
    {
        private readonly AtlasArtifactField[] _fields;
        private readonly byte[] _payload;

        private AtlasArtifact(
            AtlasArtifactHeader header,
            AtlasArtifactField[] fields,
            byte[] payload)
        {
            ValidateInputsOrThrow(
                header,
                fields,
                payload);

            Header = header;
            _fields = CopyFields(fields);
            _payload = CopyPayload(payload);
        }

        /// <summary>
        /// Gets the artifact header.
        /// </summary>
        public AtlasArtifactHeader Header { get; }

        /// <summary>
        /// Gets the number of artifact field rows.
        /// </summary>
        public int Count => _fields.Length;

        /// <summary>
        /// Gets the number of artifact field rows.
        /// </summary>
        public int FieldCount => _fields.Length;

        /// <summary>
        /// Gets whether this artifact contains no fields.
        /// </summary>
        public bool IsEmpty => _fields.Length == 0;

        /// <summary>
        /// Gets the managed serialized payload byte count.
        /// </summary>
        public int PayloadByteCount => _payload.Length;

        /// <summary>
        /// Gets the managed serialized payload byte count as a long.
        /// </summary>
        public long PayloadByteLength => _payload.Length;

        /// <summary>
        /// Gets whether this artifact serializes only logical field content.
        /// </summary>
        public bool SerializesLogicalContent
        {
            get
            {
                for (var i = 0; i < _fields.Length; i++)
                {
                    if (!_fields[i].SerializesLogicalContent)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets whether this artifact serializes full allocated capacity for every field.
        /// </summary>
        public bool SerializesCapacity
        {
            get
            {
                for (var i = 0; i < _fields.Length; i++)
                {
                    if (!_fields[i].SerializesCapacity)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the artifact field row at the supplied field-table index.
        /// </summary>
        public AtlasArtifactField this[int index]
        {
            get
            {
                ThrowIfFieldIndexOutOfRange(index);
                return _fields[index];
            }
        }

        /// <summary>
        /// Gets the artifact field row at the supplied Contract-table slot.
        /// </summary>
        public AtlasArtifactField this[AtlasFieldSlot slot] =>
            GetRequiredField(slot);

        /// <summary>
        /// Gets the artifact field row for the supplied stable field id.
        /// </summary>
        public AtlasArtifactField this[StableDataId stableId] =>
            GetRequiredField(stableId);

        /// <summary>
        /// Creates an artifact from explicit header, field table, and payload bytes.
        /// </summary>
        public static AtlasArtifact Create(
            AtlasArtifactHeader header,
            AtlasArtifactField[] fields,
            byte[] payload)
        {
            return new AtlasArtifact(
                header,
                fields,
                payload);
        }

        /// <summary>
        /// Attempts to get an artifact field by field-table index.
        /// </summary>
        public bool TryGetField(
            int index,
            out AtlasArtifactField field)
        {
            if ((uint)index < (uint)_fields.Length)
            {
                field = _fields[index];
                return true;
            }

            field = default;
            return false;
        }

        /// <summary>
        /// Attempts to get an artifact field by Contract-table slot.
        /// </summary>
        public bool TryGetField(
            AtlasFieldSlot slot,
            out AtlasArtifactField field)
        {
            var index = slot.Index;

            if ((uint)index < (uint)_fields.Length &&
                _fields[index].Slot == slot)
            {
                field = _fields[index];
                return true;
            }

            for (var i = 0; i < _fields.Length; i++)
            {
                if (_fields[i].Slot == slot)
                {
                    field = _fields[i];
                    return true;
                }
            }

            field = default;
            return false;
        }

        /// <summary>
        /// Attempts to get an artifact field by stable field id.
        /// </summary>
        public bool TryGetField(
            StableDataId stableId,
            out AtlasArtifactField field)
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                if (_fields[i].StableId == stableId)
                {
                    field = _fields[i];
                    return true;
                }
            }

            field = default;
            return false;
        }

        /// <summary>
        /// Gets a required artifact field by field-table index.
        /// </summary>
        public AtlasArtifactField GetRequiredField(int index)
        {
            if (TryGetField(index, out var field))
            {
                return field;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Artifact field index must be between 0 and {0}.",
                    _fields.Length - 1));
        }

        /// <summary>
        /// Gets a required artifact field by Contract-table slot.
        /// </summary>
        public AtlasArtifactField GetRequiredField(AtlasFieldSlot slot)
        {
            if (TryGetField(slot, out var field))
            {
                return field;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas artifact does not contain field slot '{0}'.",
                    slot),
                nameof(slot));
        }

        /// <summary>
        /// Gets a required artifact field by stable field id.
        /// </summary>
        public AtlasArtifactField GetRequiredField(StableDataId stableId)
        {
            if (TryGetField(stableId, out var field))
            {
                return field;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas artifact does not contain stable field id '{0}'.",
                    stableId),
                nameof(stableId));
        }

        /// <summary>
        /// Creates a managed copy of the complete artifact payload.
        /// </summary>
        public byte[] GetPayloadCopy()
        {
            return CopyPayload(_payload);
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(int fieldIndex)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(fieldIndex));
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(AtlasFieldSlot slot)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(slot));
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(StableDataId stableId)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(stableId));
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(AtlasArtifactField field)
        {
            field.ValidateOrThrow(nameof(field));

            ValidateFieldRangeInsidePayloadOrThrow(
                field,
                _payload.Length);

            var length = checked((int)field.PayloadByteLength);
            var copy = new byte[length];

            Array.Copy(
                _payload,
                checked((int)field.ByteOffset),
                copy,
                0,
                length);

            return copy;
        }

        /// <summary>
        /// Creates a managed copy of one field's logical-content payload bytes.
        /// </summary>
        /// <remarks>
        /// This requires the artifact field to serialize at least logical byte length. Production
        /// logical artifacts satisfy this exactly; capacity snapshots also satisfy it because their
        /// payload includes the logical prefix.
        /// </remarks>
        public byte[] GetFieldLogicalPayloadCopy(AtlasArtifactField field)
        {
            field.ValidateOrThrow(nameof(field));

            ValidateFieldRangeInsidePayloadOrThrow(
                field,
                _payload.Length);

            var length = checked((int)field.ByteLength);
            var copy = new byte[length];

            Array.Copy(
                _payload,
                checked((int)field.ByteOffset),
                copy,
                0,
                length);

            return copy;
        }

        /// <summary>
        /// Copies the complete artifact payload into a caller-provided destination array.
        /// </summary>
        public void CopyPayloadTo(
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

            if (destination.Length - destinationIndex < _payload.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Destination has {0} available bytes, but artifact payload requires {1}.",
                        destination.Length - destinationIndex,
                        _payload.Length),
                    nameof(destination));
            }

            Array.Copy(
                _payload,
                0,
                destination,
                destinationIndex,
                _payload.Length);
        }

        /// <summary>
        /// Copies one field's serialized payload into a caller-provided destination array.
        /// </summary>
        public void CopyFieldPayloadTo(
            AtlasArtifactField field,
            byte[] destination,
            int destinationIndex = 0)
        {
            field.ValidateOrThrow(nameof(field));

            ValidateFieldRangeInsidePayloadOrThrow(
                field,
                _payload.Length);

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

            var length = checked((int)field.PayloadByteLength);

            if (destination.Length - destinationIndex < length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Destination has {0} available bytes, but field payload requires {1}.",
                        destination.Length - destinationIndex,
                        length),
                    nameof(destination));
            }

            Array.Copy(
                _payload,
                checked((int)field.ByteOffset),
                destination,
                destinationIndex,
                length);
        }

        /// <summary>
        /// Validates this artifact envelope.
        /// </summary>
        public void ValidateOrThrow()
        {
            ValidateInputsOrThrow(
                Header,
                _fields,
                _payload);
        }

        /// <summary>
        /// Gets an enumerator over artifact fields in field-table order.
        /// </summary>
        public IEnumerator<AtlasArtifactField> GetEnumerator()
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                yield return _fields[i];
            }
        }

        /// <summary>
        /// Gets an enumerator over artifact fields in field-table order.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic artifact string.
        /// </summary>
        public override string ToString()
        {
            var contentHashText = Header.HasContentHash
                ? FormatHex(Header.ContentHash)
                : "<absent>";

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasArtifact(Pipeline={0}, Fields={1}, PayloadBytes={2}, ContentBytes={3}, CapacityBytes={4}, ContentHash={5})",
                Header.PipelineName,
                FieldCount,
                PayloadByteCount,
                Header.TotalByteLength,
                Header.TotalByteCapacity,
                contentHashText);
        }

        private static void ValidateInputsOrThrow(
            AtlasArtifactHeader header,
            AtlasArtifactField[] fields,
            byte[] payload)
        {
            header.ValidateOrThrow(nameof(header));

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (header.FieldCount != fields.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact header field count {0} does not match field table length {1}.",
                        header.FieldCount,
                        fields.Length),
                    nameof(fields));
            }

            long cursor = 0L;
            long totalByteLength = 0L;
            long totalByteCapacity = 0L;
            long totalPayloadByteLength = 0L;

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                field.ValidateOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "fields[{0}]",
                        i));

                if (field.FieldIndex != i)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Artifact field row at index {0} has FieldIndex {1}. Field table indices must be contiguous and ordered.",
                            i,
                            field.FieldIndex),
                        nameof(fields));
                }

                if (field.ByteOffset != cursor)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Artifact field '{0}' has byte offset {1}, but expected contiguous offset {2}.",
                            field.DebugName,
                            field.ByteOffset,
                            cursor),
                        nameof(fields));
                }

                ValidateFieldRangeInsidePayloadOrThrow(
                    field,
                    payload.Length);

                cursor = checked(cursor + field.PayloadByteLength);
                totalPayloadByteLength = checked(totalPayloadByteLength + field.PayloadByteLength);
                totalByteLength = checked(totalByteLength + field.ByteLength);
                totalByteCapacity = checked(totalByteCapacity + field.ByteCapacity);
            }

            if (cursor != payload.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field table describes {0} serialized payload bytes, but payload length is {1}.",
                        cursor,
                        payload.Length),
                    nameof(fields));
            }

            if (totalPayloadByteLength != payload.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field table payload byte length {0} does not match payload length {1}.",
                        totalPayloadByteLength,
                        payload.Length),
                    nameof(fields));
            }

            if (header.TotalByteLength != totalByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact header total byte length {0} does not match field table logical byte length {1}.",
                        header.TotalByteLength,
                        totalByteLength),
                    nameof(header));
            }

            if (header.TotalByteCapacity != totalByteCapacity)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact header total byte capacity {0} does not match field table byte capacity {1}.",
                        header.TotalByteCapacity,
                        totalByteCapacity),
                    nameof(header));
            }
        }

        private static void ValidateFieldRangeInsidePayloadOrThrow(
            AtlasArtifactField field,
            int payloadLength)
        {
            if (field.ByteOffset > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field '{0}' byte offset {1} exceeds managed array index capacity.",
                        field.DebugName,
                        field.ByteOffset));
            }

            if (field.PayloadByteLength > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field '{0}' payload byte length {1} exceeds managed array length capacity.",
                        field.DebugName,
                        field.PayloadByteLength));
            }

            var start = checked((int)field.ByteOffset);
            var length = checked((int)field.PayloadByteLength);
            var end = checked(start + length);

            if (start < 0 ||
                length < 0 ||
                end > payloadLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(field),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Artifact field '{0}' byte range [{1}, {2}) is outside payload length {3}.",
                        field.DebugName,
                        field.ByteOffset,
                        field.ByteEndOffset,
                        payloadLength));
            }
        }

        private static AtlasArtifactField[] CopyFields(AtlasArtifactField[] fields)
        {
            var copy = new AtlasArtifactField[fields.Length];

            Array.Copy(
                fields,
                copy,
                fields.Length);

            return copy;
        }

        private static byte[] CopyPayload(byte[] payload)
        {
            var copy = new byte[payload.Length];

            Array.Copy(
                payload,
                copy,
                payload.Length);

            return copy;
        }

        private void ThrowIfFieldIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_fields.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Artifact field index must be between 0 and {0}.",
                    _fields.Length - 1));
        }

        private static string FormatHex(ulong value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "0x{0:X16}",
                value);
        }
    }
}