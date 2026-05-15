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
    /// <see cref="AtlasArtifact"/> is the first durable managed output container. It captures
    /// artifact metadata in <see cref="Header"/>, per-field layout in the field table, and a
    /// managed payload byte copy detached from workspace-owned native memory.
    /// </para>
    ///
    /// <para>
    /// This type intentionally does not own native memory and does not reference the workspace after
    /// creation. Once an artifact is created, the source <see cref="AtlasWorkspace"/> may be
    /// disposed without invalidating the artifact payload.
    /// </para>
    ///
    /// <para>
    /// The first artifact layout is strict: field payloads are written contiguously in field-table
    /// order, and each field row's <see cref="AtlasArtifactField.ByteOffset"/> must match the
    /// current payload cursor. This keeps artifact writing, hashing, and debug inspection
    /// deterministic.
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
        /// Gets the managed payload byte count.
        /// </summary>
        public int PayloadByteCount => _payload.Length;

        /// <summary>
        /// Gets the managed payload byte count as a long.
        /// </summary>
        public long PayloadByteLength => _payload.Length;

        /// <summary>
        /// Gets the artifact field row at the supplied field-table index.
        /// </summary>
        /// <param name="index">Field-table index.</param>
        /// <returns>The artifact field row.</returns>
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
        /// <param name="slot">Contract-table slot.</param>
        /// <returns>The artifact field row.</returns>
        public AtlasArtifactField this[AtlasFieldSlot slot] =>
            GetRequiredField(slot);

        /// <summary>
        /// Gets the artifact field row for the supplied stable field id.
        /// </summary>
        /// <param name="stableId">Stable field id.</param>
        /// <returns>The artifact field row.</returns>
        public AtlasArtifactField this[StableDataId stableId] =>
            GetRequiredField(stableId);

        /// <summary>
        /// Creates an artifact from explicit header, field table, and payload bytes.
        /// </summary>
        /// <param name="header">Artifact header.</param>
        /// <param name="fields">Artifact field table.</param>
        /// <param name="payload">Artifact payload bytes.</param>
        /// <returns>An immutable managed artifact.</returns>
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
        /// <param name="context">Execution context whose workspace will be copied.</param>
        /// <returns>An immutable managed artifact with field and aggregate content hashes.</returns>
        /// <remarks>
        /// The caller must ensure all jobs writing workspace memory have completed before calling
        /// this method. This method copies the current workspace bytes synchronously.
        /// </remarks>
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
        /// <param name="context">Execution context whose workspace will be copied.</param>
        /// <param name="computeContentHashes">Whether to compute field and aggregate payload hashes.</param>
        /// <returns>An immutable managed artifact.</returns>
        /// <remarks>
        /// The caller must ensure all jobs writing workspace memory have completed before calling
        /// this method. This method copies the current workspace bytes synchronously.
        /// </remarks>
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
        /// Captures a managed artifact snapshot from a compiled plan and compatible workspace.
        /// </summary>
        /// <param name="plan">Compiled plan that produced the workspace contents.</param>
        /// <param name="workspace">Workspace whose bytes should be copied.</param>
        /// <param name="computeContentHashes">Whether to compute field and aggregate payload hashes.</param>
        /// <returns>An immutable managed artifact.</returns>
        /// <remarks>
        /// The caller must ensure all jobs writing workspace memory have completed before calling
        /// this method. This method copies the current workspace bytes synchronously.
        /// </remarks>
        public static AtlasArtifact Capture(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace,
            bool computeContentHashes = true)
        {
            ValidateCaptureInputsOrThrow(
                plan,
                workspace);

            var payloadLength = checked((int)workspace.TotalByteCapacity);
            var payload = new byte[payloadLength];
            var fields = new AtlasArtifactField[workspace.Count];

            long byteOffset = 0L;

            for (var i = 0; i < workspace.Count; i++)
            {
                var block = workspace[i];
                var bytes = block.GetByteCapacityArray();

                var fieldByteOffset = byteOffset;
                var fieldByteCapacity = checked((int)block.ByteCapacity);

                for (var j = 0; j < fieldByteCapacity; j++)
                {
                    payload[checked((int)fieldByteOffset + j)] = bytes[j];
                }

                if (computeContentHashes)
                {
                    var fieldHash = ComputeHash(
                        payload,
                        checked((int)fieldByteOffset),
                        fieldByteCapacity);

                    fields[i] = AtlasArtifactField.Create(
                        block.Contract,
                        block.Shape,
                        fieldByteOffset,
                        fieldHash);
                }
                else
                {
                    fields[i] = AtlasArtifactField.Create(
                        block.Contract,
                        block.Shape,
                        fieldByteOffset);
                }

                byteOffset = checked(byteOffset + block.ByteCapacity);
            }

            var header = computeContentHashes
                ? AtlasArtifactHeader.Create(
                    plan,
                    workspace.Shapes,
                    ComputeHash(payload, 0, payload.Length))
                : AtlasArtifactHeader.Create(
                    plan,
                    workspace.Shapes);

            return new AtlasArtifact(
                header,
                fields,
                payload);
        }

        /// <summary>
        /// Attempts to get an artifact field by field-table index.
        /// </summary>
        /// <param name="index">Field-table index.</param>
        /// <param name="field">Resolved field row on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the index exists.</returns>
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
        /// <param name="slot">Contract-table slot.</param>
        /// <param name="field">Resolved field row on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the slot exists.</returns>
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
        /// <param name="stableId">Stable field id.</param>
        /// <param name="field">Resolved field row on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the field exists.</returns>
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
        /// <param name="index">Field-table index.</param>
        /// <returns>The artifact field row.</returns>
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
        /// <param name="slot">Contract-table slot.</param>
        /// <returns>The artifact field row.</returns>
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
        /// <param name="stableId">Stable field id.</param>
        /// <returns>The artifact field row.</returns>
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
        /// <returns>A new payload byte array.</returns>
        public byte[] GetPayloadCopy()
        {
            return CopyPayload(_payload);
        }

        /// <summary>
        /// Creates a managed copy of one field's payload bytes.
        /// </summary>
        /// <param name="fieldIndex">Field-table index.</param>
        /// <returns>A new byte array containing the field payload bytes.</returns>
        public byte[] GetFieldPayloadCopy(
            int fieldIndex)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(fieldIndex));
        }

        /// <summary>
        /// Creates a managed copy of one field's payload bytes.
        /// </summary>
        /// <param name="slot">Contract-table slot.</param>
        /// <returns>A new byte array containing the field payload bytes.</returns>
        public byte[] GetFieldPayloadCopy(
            AtlasFieldSlot slot)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(slot));
        }

        /// <summary>
        /// Creates a managed copy of one field's payload bytes.
        /// </summary>
        /// <param name="stableId">Stable field id.</param>
        /// <returns>A new byte array containing the field payload bytes.</returns>
        public byte[] GetFieldPayloadCopy(
            StableDataId stableId)
        {
            return GetFieldPayloadCopy(
                GetRequiredField(stableId));
        }

        /// <summary>
        /// Creates a managed copy of one field's payload bytes.
        /// </summary>
        /// <param name="field">Artifact field row.</param>
        /// <returns>A new byte array containing the field payload bytes.</returns>
        public byte[] GetFieldPayloadCopy(
            AtlasArtifactField field)
        {
            field.ValidateOrThrow(nameof(field));
            ValidateFieldRangeInsidePayloadOrThrow(
                field,
                _payload.Length);

            var length = checked((int)field.ByteCapacity);
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
        /// <param name="destination">Destination byte array.</param>
        /// <param name="destinationIndex">Destination start index.</param>
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
        /// Copies one field payload into a caller-provided destination array.
        /// </summary>
        /// <param name="field">Artifact field row.</param>
        /// <param name="destination">Destination byte array.</param>
        /// <param name="destinationIndex">Destination start index.</param>
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

            var length = checked((int)field.ByteCapacity);

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
        /// <returns>An artifact field enumerator.</returns>
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
        /// <returns>An artifact field enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic artifact string.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            var contentHashText = Header.HasContentHash
                ? FormatHex(Header.ContentHash)
                : "<absent>";

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasArtifact(Pipeline={0}, Fields={1}, PayloadBytes={2}, ContentHash={3})",
                Header.PipelineName,
                FieldCount,
                PayloadByteCount,
                contentHashText);
        }

        private static void ValidateCaptureInputsOrThrow(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace)
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

            if (workspace.Contracts == null)
            {
                throw new ArgumentException(
                    "Workspace does not reference a Contract table.",
                    nameof(workspace));
            }

            if (workspace.Shapes == null)
            {
                throw new ArgumentException(
                    "Workspace does not reference a resolved shape set.",
                    nameof(workspace));
            }

            if (plan.Contracts.Count != workspace.Contracts.Count ||
                plan.Contracts.Count != workspace.Count ||
                plan.Contracts.Count != workspace.Shapes.Count)
            {
                throw new ArgumentException(
                    "Compiled plan, workspace, and resolved shape set field counts do not match.");
            }

            if (workspace.TotalByteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    $"Atlas artifact payload byte capacity '{workspace.TotalByteCapacity}' exceeds managed byte array length capacity.");
            }

            for (var i = 0; i < workspace.Count; i++)
            {
                var planContract = plan.Contracts[i];
                var block = workspace[i];

                if (planContract != block.Contract)
                {
                    throw new ArgumentException(
                        $"Workspace block at index '{i}' does not match compiled plan Contract '{planContract.GetDiagnosticName()}'.",
                        nameof(workspace));
                }
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

            if (header.TotalByteCapacity != payload.Length)
            {
                throw new ArgumentException(
                    $"Artifact header total byte capacity '{header.TotalByteCapacity}' does not match payload length '{payload.Length}'.",
                    nameof(payload));
            }

            long cursor = 0L;
            long totalByteLength = 0L;
            long totalByteCapacity = 0L;

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

                cursor = checked(cursor + field.ByteCapacity);
                totalByteLength = checked(totalByteLength + field.ByteLength);
                totalByteCapacity = checked(totalByteCapacity + field.ByteCapacity);
            }

            if (cursor != payload.Length)
            {
                throw new ArgumentException(
                    $"Artifact field table describes '{cursor}' payload bytes, but payload length is '{payload.Length}'.",
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

            if (field.ByteCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    $"Artifact field '{field.DebugName}' byte capacity '{field.ByteCapacity}' exceeds managed array length capacity.");
            }

            var start = checked((int)field.ByteOffset);
            var length = checked((int)field.ByteCapacity);
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