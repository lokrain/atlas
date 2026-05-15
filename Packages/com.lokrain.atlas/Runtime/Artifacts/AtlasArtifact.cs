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
// - Serialize logical content by default while preserving capacity metadata.
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
// - Artifact capture writes logical field bytes by default, not capacity slack.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Immutable managed Atlas artifact envelope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasArtifact"/> captures artifact metadata in <see cref="Header"/>,
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
        private const ulong FnvOffsetBasis64 = 14695981039346656037UL;
        private const ulong FnvPrime64 = 1099511628211UL;

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
        /// Captures a managed artifact snapshot from a completed execution context.
        /// </summary>
        public static AtlasArtifact Capture(
            AtlasExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Capture(
                context.Plan,
                context.Workspace,
                computeContentHashes: true);
        }

        /// <summary>
        /// Captures a managed artifact snapshot from a completed execution context.
        /// </summary>
        public static AtlasArtifact Capture(
            AtlasExecutionContext context,
            bool computeContentHashes)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Capture(
                context.Plan,
                context.Workspace,
                computeContentHashes);
        }

        /// <summary>
        /// Captures a logical-content managed artifact snapshot from a compiled plan and compatible workspace.
        /// </summary>
        /// <remarks>
        /// The caller must ensure all jobs writing workspace memory have completed before calling
        /// this method. This method synchronously copies each field's logical byte length and does
        /// not serialize capacity slack.
        /// </remarks>
        public static AtlasArtifact Capture(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace,
            bool computeContentHashes = true)
        {
            ValidateCaptureInputsOrThrow(
                plan,
                workspace,
                captureCapacityPayload: false);

            var shapes = CreateShapeSet(
                plan,
                workspace);

            var payloadLength = checked((int)workspace.TotalByteLength);
            var payload = new byte[payloadLength];
            var fields = new AtlasArtifactField[workspace.Count];

            long byteOffset = 0L;

            for (var i = 0; i < workspace.Count; i++)
            {
                var entry = workspace[i];
                var contract = plan.Contracts[i];
                var shape = shapes[i];
                var bytes = workspace.GetFieldByteLengthSlice(entry.Slot);

                var fieldByteOffset = byteOffset;
                var fieldByteLength = checked((int)entry.ByteLength);

                CopyNativeBytesToManagedPayload(
                    bytes,
                    payload,
                    fieldByteOffset,
                    fieldByteLength);

                if (computeContentHashes)
                {
                    var fieldHash = ComputeHash(
                        payload,
                        checked((int)fieldByteOffset),
                        fieldByteLength);

                    fields[i] = AtlasArtifactField.CreateLogicalPayload(
                        contract,
                        shape,
                        fieldByteOffset,
                        fieldHash);
                }
                else
                {
                    fields[i] = AtlasArtifactField.CreateLogicalPayload(
                        contract,
                        shape,
                        fieldByteOffset);
                }

                byteOffset = checked(byteOffset + entry.ByteLength);
            }

            var header = computeContentHashes
                ? AtlasArtifactHeader.Create(
                    plan,
                    shapes,
                    ComputeHash(payload, 0, payload.Length))
                : AtlasArtifactHeader.Create(
                    plan,
                    shapes);

            return new AtlasArtifact(
                header,
                fields,
                payload);
        }

        /// <summary>
        /// Captures a managed artifact snapshot that includes full capacity bytes.
        /// </summary>
        /// <remarks>
        /// This is an explicit capacity snapshot API. Durable production artifact capture should
        /// normally use <see cref="Capture(AtlasCompiledPlan, AtlasWorkspace, bool)"/> so slack bytes
        /// do not affect payload bytes or content hashes.
        /// </remarks>
        public static AtlasArtifact CaptureCapacitySnapshot(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace,
            bool computeContentHashes = true)
        {
            ValidateCaptureInputsOrThrow(
                plan,
                workspace,
                captureCapacityPayload: true);

            var shapes = CreateShapeSet(
                plan,
                workspace);

            var payloadLength = checked((int)workspace.TotalByteCapacity);
            var payload = new byte[payloadLength];
            var fields = new AtlasArtifactField[workspace.Count];

            long byteOffset = 0L;

            for (var i = 0; i < workspace.Count; i++)
            {
                var entry = workspace[i];
                var contract = plan.Contracts[i];
                var shape = shapes[i];
                var bytes = workspace.GetFieldByteCapacitySlice(entry.Slot);

                var fieldByteOffset = byteOffset;
                var fieldByteCapacity = checked((int)entry.ByteCapacity);

                CopyNativeBytesToManagedPayload(
                    bytes,
                    payload,
                    fieldByteOffset,
                    fieldByteCapacity);

                if (computeContentHashes)
                {
                    var fieldHash = ComputeHash(
                        payload,
                        checked((int)fieldByteOffset),
                        fieldByteCapacity);

                    fields[i] = AtlasArtifactField.Create(
                        contract,
                        shape,
                        fieldByteOffset,
                        fieldHash);
                }
                else
                {
                    fields[i] = AtlasArtifactField.Create(
                        contract,
                        shape,
                        fieldByteOffset);
                }

                byteOffset = checked(byteOffset + entry.ByteCapacity);
            }

            var header = computeContentHashes
                ? AtlasArtifactHeader.Create(
                    plan,
                    shapes,
                    ComputeHash(payload, 0, payload.Length))
                : AtlasArtifactHeader.Create(
                    plan,
                    shapes);

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
        public AtlasArtifactField GetRequiredField(
            int index)
        {
            if (TryGetField(index, out var field))
            {
                return field;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Artifact field index must be between 0 and {_fields.Length - 1}.");
        }

        /// <summary>
        /// Gets a required artifact field by Contract-table slot.
        /// </summary>
        public AtlasArtifactField GetRequiredField(
            AtlasFieldSlot slot)
        {
            if (TryGetField(slot, out var field))
            {
                return field;
            }

            throw new ArgumentException(
                $"Atlas artifact does not contain field slot '{slot}'.",
                nameof(slot));
        }

        /// <summary>
        /// Gets a required artifact field by stable field id.
        /// </summary>
        public AtlasArtifactField GetRequiredField(
            StableDataId stableId)
        {
            if (TryGetField(stableId, out var field))
            {
                return field;
            }

            throw new ArgumentException(
                $"Atlas artifact does not contain stable field id '{stableId}'.",
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
        public byte[] GetFieldPayloadCopy(
            int fieldIndex)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(fieldIndex));
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(
            AtlasFieldSlot slot)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(slot));
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(
            StableDataId stableId)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(stableId));
        }

        /// <summary>
        /// Creates a managed copy of one field's serialized payload bytes.
        /// </summary>
        public byte[] GetFieldPayloadCopy(
            AtlasArtifactField field)
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
        public byte[] GetFieldLogicalPayloadCopy(
            AtlasArtifactField field)
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
                    $"Destination has '{destination.Length - destinationIndex}' available bytes, but artifact payload requires '{_payload.Length}'.",
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
                    $"Destination has '{destination.Length - destinationIndex}' available bytes, but field payload requires '{length}'.",
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

        private static void ValidateCaptureInputsOrThrow(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace,
            bool captureCapacityPayload)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            workspace.ThrowIfDisposed();

            if (plan.Contracts == null)
            {
                throw new ArgumentException(
                    "Compiled plan does not reference a Contract table.",
                    nameof(plan));
            }

            if (plan.Contracts.Count != workspace.Count)
            {
                throw new ArgumentException(
                    "Compiled plan and workspace layout field counts do not match.");
            }

            var payloadBytes = captureCapacityPayload
                ? workspace.TotalByteCapacity
                : workspace.TotalByteLength;

            if (payloadBytes > int.MaxValue)
            {
                throw new OverflowException(
                    $"Atlas artifact payload byte length '{payloadBytes}' exceeds managed byte array length capacity.");
            }

            for (var i = 0; i < workspace.Count; i++)
            {
                var contract = plan.Contracts[i];
                var entry = workspace[i];

                ValidateEntryMatchesContractOrThrow(
                    entry,
                    contract,
                    i);
            }
        }

        private static AtlasResolvedShapeSet CreateShapeSet(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace)
        {
            var shapes = new AtlasResolvedShape[workspace.Count];

            for (var i = 0; i < workspace.Count; i++)
            {
                var entry = workspace[i];

                ValidateEntryMatchesContractOrThrow(
                    entry,
                    plan.Contracts[i],
                    i);

                shapes[i] = AtlasResolvedShape.Create(
                    entry.StableId,
                    entry.Slot,
                    entry.Role,
                    entry.StorageFormat,
                    entry.ShapeDomain,
                    entry.DeclaredShape,
                    entry.DebugName,
                    entry.Length,
                    entry.Capacity);
            }

            return AtlasResolvedShapeSet.Create(
                plan.DebugName,
                plan.Contracts,
                shapes);
        }

        private static void ValidateEntryMatchesContractOrThrow(
            Lokrain.Atlas.Workspaces.AtlasWorkspaceLayoutEntry entry,
            Lokrain.Atlas.Contracts.AtlasContract contract,
            int index)
        {
            contract.ValidateTableReadyOrThrow(nameof(contract));
            entry.ValidateBoundOrThrow(nameof(entry));

            if (entry.Slot.Index != index ||
                entry.StableId != contract.StableId ||
                entry.Slot != contract.Slot ||
                entry.Role != contract.Role ||
                entry.StorageFormat != contract.StorageFormat ||
                entry.ShapeDomain != contract.ShapeDomain ||
                entry.DeclaredShape != contract.LengthShape)
            {
                throw new ArgumentException(
                    $"Workspace layout entry at index '{index}' does not match compiled plan Contract '{contract.GetDiagnosticName()}'.",
                    nameof(entry));
            }
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
                    $"Artifact header field count '{header.FieldCount}' does not match field table length '{fields.Length}'.",
                    nameof(fields));
            }

            long cursor = 0L;
            long totalByteLength = 0L;
            long totalByteCapacity = 0L;
            long totalPayloadByteLength = 0L;

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                field.ValidateOrThrow($"fields[{i}]");

                if (field.FieldIndex != i)
                {
                    throw new ArgumentException(
                        $"Artifact field row at index '{i}' has FieldIndex '{field.FieldIndex}'. Field table indices must be contiguous and ordered.",
                        nameof(fields));
                }

                if (field.ByteOffset != cursor)
                {
                    throw new ArgumentException(
                        $"Artifact field '{field.DebugName}' has byte offset '{field.ByteOffset}', but expected contiguous offset '{cursor}'.",
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
                    $"Artifact field table describes '{cursor}' serialized payload bytes, but payload length is '{payload.Length}'.",
                    nameof(fields));
            }

            if (totalPayloadByteLength != payload.Length)
            {
                throw new ArgumentException(
                    $"Artifact field table payload byte length '{totalPayloadByteLength}' does not match payload length '{payload.Length}'.",
                    nameof(fields));
            }

            if (header.TotalByteLength != totalByteLength)
            {
                throw new ArgumentException(
                    $"Artifact header total byte length '{header.TotalByteLength}' does not match field table logical byte length '{totalByteLength}'.",
                    nameof(header));
            }

            if (header.TotalByteCapacity != totalByteCapacity)
            {
                throw new ArgumentException(
                    $"Artifact header total byte capacity '{header.TotalByteCapacity}' does not match field table byte capacity '{totalByteCapacity}'.",
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
                    $"Artifact field '{field.DebugName}' byte offset '{field.ByteOffset}' exceeds managed array index capacity.");
            }

            if (field.PayloadByteLength > int.MaxValue)
            {
                throw new OverflowException(
                    $"Artifact field '{field.DebugName}' payload byte length '{field.PayloadByteLength}' exceeds managed array length capacity.");
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
                    $"Artifact field '{field.DebugName}' byte range [{field.ByteOffset}, {field.ByteEndOffset}) is outside payload length '{payloadLength}'.");
            }
        }

        private static void CopyNativeBytesToManagedPayload(
            Unity.Collections.NativeSlice<byte> source,
            byte[] destination,
            long destinationOffset,
            int length)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destinationOffset < 0L || destinationOffset > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(destinationOffset),
                    destinationOffset,
                    "Destination offset must fit inside a managed byte array.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Copy length must be non-negative.");
            }

            var offset = checked((int)destinationOffset);

            if (offset + length > destination.Length)
            {
                throw new ArgumentException(
                    "Native source copy range exceeds managed destination payload length.",
                    nameof(destination));
            }

            for (var i = 0; i < length; i++)
            {
                destination[offset + i] = source[i];
            }
        }

        private static AtlasArtifactField[] CopyFields(
            AtlasArtifactField[] fields)
        {
            var copy = new AtlasArtifactField[fields.Length];

            Array.Copy(
                fields,
                copy,
                fields.Length);

            return copy;
        }

        private static byte[] CopyPayload(
            byte[] payload)
        {
            var copy = new byte[payload.Length];

            Array.Copy(
                payload,
                copy,
                payload.Length);

            return copy;
        }

        private static ulong ComputeHash(
            byte[] bytes,
            int offset,
            int length)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (offset + length > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Hash range exceeds byte array length.");
            }

            var hash = FnvOffsetBasis64;

            for (var i = 0; i < length; i++)
            {
                unchecked
                {
                    hash ^= bytes[offset + i];
                    hash *= FnvPrime64;
                }
            }

            return hash;
        }

        private void ThrowIfFieldIndexOutOfRange(
            int index)
        {
            if ((uint)index < (uint)_fields.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Artifact field index must be between 0 and {_fields.Length - 1}.");
        }

        private static string FormatHex(
            ulong value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "0x{0:X16}",
                value);
        }
    }
}