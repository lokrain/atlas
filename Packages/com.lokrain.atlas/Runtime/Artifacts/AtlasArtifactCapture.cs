// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactCapture.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Capture completed workspace data into immutable managed Atlas artifacts.
// - Keep workspace-copy, shape reconstruction, and content-hash policy out of AtlasArtifact.
// - Provide explicit logical-content capture and explicit capacity-snapshot capture.
// - Validate compiled-plan/workspace compatibility before copying native bytes.
//
// Design notes
// - This is a managed artifact-capture boundary, not durable artifact data.
// - The caller must ensure all jobs writing workspace memory have completed before capture.
// - Logical capture writes each field's logical byte length and does not serialize capacity slack.
// - Capacity snapshots are explicit diagnostic/durable snapshots and must not be confused with
//   canonical logical artifact capture.
// - This class does not write files and does not render debug output.

using System;
using System.Globalization;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Workspaces;
using Unity.Collections;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Captures managed artifact snapshots from completed Atlas workspaces.
    /// </summary>
    public static class AtlasArtifactCapture
    {
        private const ulong FnvOffsetBasis64 = 14695981039346656037UL;
        private const ulong FnvPrime64 = 1099511628211UL;

        /// <summary>
        /// Captures a logical-content managed artifact snapshot from a completed execution context.
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
        /// Captures a logical-content managed artifact snapshot from a completed execution context.
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

            var capturedFieldCount = CountDefaultCapturedFields(plan);
            var payloadLength = checked((int)CalculateDefaultCapturedLogicalPayloadLength(plan, workspace));
            var payload = new byte[payloadLength];
            var fields = new AtlasArtifactField[capturedFieldCount];

            long byteOffset = 0L;
            var capturedFieldIndex = 0;

            for (var i = 0; i < workspace.Count; i++)
            {
                var entry = workspace[i];
                var contract = plan.Contracts[i];

                if (!contract.Role.IsCapturedByDefaultArtifactProfile())
                {
                    continue;
                }

                var shape = CreateShape(entry);
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

                    fields[capturedFieldIndex] = AtlasArtifactField.CreateLogicalPayload(
                        capturedFieldIndex,
                        shape,
                        fieldByteOffset,
                        fieldHash);
                }
                else
                {
                    fields[capturedFieldIndex] = AtlasArtifactField.CreateLogicalPayload(
                        capturedFieldIndex,
                        shape,
                        fieldByteOffset);
                }

                byteOffset = checked(byteOffset + entry.ByteLength);
                capturedFieldIndex++;
            }

            var header = computeContentHashes
                ? AtlasArtifactHeader.Create(
                    plan,
                    fields,
                    ComputeHash(payload, 0, payload.Length))
                : AtlasArtifactHeader.Create(
                    plan,
                    fields);

            return AtlasArtifact.Create(
                header,
                fields,
                payload);
        }

        /// <summary>
        /// Captures a managed artifact snapshot that serializes full allocated field capacity.
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

            var payloadLength = checked((int)workspace.TotalFieldByteCapacity);
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

            return AtlasArtifact.Create(
                header,
                fields,
                payload);
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
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Compiled plan Contract table contains {0} fields, but workspace layout contains {1} entries.",
                        plan.Contracts.Count,
                        workspace.Count),
                    nameof(workspace));
            }

            var payloadBytes = captureCapacityPayload
                ? workspace.TotalFieldByteCapacity
                : CalculateDefaultCapturedLogicalPayloadLength(plan, workspace);

            if (payloadBytes > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas artifact payload byte length {0} exceeds managed byte array length capacity.",
                        payloadBytes));
            }

            for (var i = 0; i < workspace.Count; i++)
            {
                ValidateEntryMatchesContractOrThrow(
                    workspace[i],
                    plan.Contracts[i],
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

                shapes[i] = CreateShape(entry);
            }

            return AtlasResolvedShapeSet.Create(
                plan.DebugName,
                plan.Contracts,
                shapes);
        }

        private static int CountDefaultCapturedFields(
            AtlasCompiledPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (plan.Contracts == null)
            {
                throw new ArgumentException(
                    "Compiled plan does not reference a Contract table.",
                    nameof(plan));
            }

            var count = 0;

            for (var i = 0; i < plan.Contracts.Count; i++)
            {
                if (plan.Contracts[i].Role.IsCapturedByDefaultArtifactProfile())
                {
                    count++;
                }
            }

            return count;
        }

        private static long CalculateDefaultCapturedLogicalPayloadLength(
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

            if (plan.Contracts == null)
            {
                throw new ArgumentException(
                    "Compiled plan does not reference a Contract table.",
                    nameof(plan));
            }

            if (plan.Contracts.Count != workspace.Count)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Compiled plan Contract table contains {0} fields, but workspace layout contains {1} entries.",
                        plan.Contracts.Count,
                        workspace.Count),
                    nameof(workspace));
            }

            var total = 0L;

            for (var i = 0; i < workspace.Count; i++)
            {
                if (plan.Contracts[i].Role.IsCapturedByDefaultArtifactProfile())
                {
                    total = checked(total + workspace[i].ByteLength);
                }
            }

            return total;
        }

        private static AtlasResolvedShape CreateShape(
            AtlasWorkspaceLayoutEntry entry)
        {
            entry.ValidateBoundOrThrow(nameof(entry));

            return AtlasResolvedShape.Create(
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

        private static void ValidateEntryMatchesContractOrThrow(
            AtlasWorkspaceLayoutEntry entry,
            AtlasContract contract,
            int index)
        {
            contract.ValidateTableReadyOrThrow(nameof(contract));
            entry.ValidateBoundOrThrow(nameof(entry));

            if (entry.Slot.Index != index)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} has slot {1}.",
                        index,
                        entry.Slot),
                    nameof(entry));
            }

            if (entry.StableId != contract.StableId)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} has stable id {1}, but Contract '{2}' has stable id {3}.",
                        index,
                        entry.StableId,
                        contract.GetDiagnosticName(),
                        contract.StableId),
                    nameof(entry));
            }

            if (entry.Slot != contract.Slot)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} has slot {1}, but Contract '{2}' has slot {3}.",
                        index,
                        entry.Slot,
                        contract.GetDiagnosticName(),
                        contract.Slot),
                    nameof(entry));
            }

            if (entry.Role != contract.Role)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} has role {1}, but Contract '{2}' has role {3}.",
                        index,
                        entry.Role,
                        contract.GetDiagnosticName(),
                        contract.Role),
                    nameof(entry));
            }

            if (entry.StorageFormat != contract.StorageFormat)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} storage format does not match Contract '{1}'.",
                        index,
                        contract.GetDiagnosticName()),
                    nameof(entry));
            }

            if (entry.ShapeDomain != contract.ShapeDomain)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} shape domain does not match Contract '{1}'.",
                        index,
                        contract.GetDiagnosticName()),
                    nameof(entry));
            }

            if (entry.DeclaredShape != contract.LengthShape)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry at index {0} declared shape does not match Contract '{1}'.",
                        index,
                        contract.GetDiagnosticName()),
                    nameof(entry));
            }
        }

        private static void CopyNativeBytesToManagedPayload(
            NativeSlice<byte> source,
            byte[] destination,
            long destinationOffset,
            int length)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destinationOffset < 0L ||
                destinationOffset > int.MaxValue)
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
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    offset,
                    "Hash offset must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Hash length must be non-negative.");
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
    }
}
