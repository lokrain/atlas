// Packages/com.lokrain.atlas/Runtime/Artifacts/AtlasArtifactHeader.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Artifacts
//
// Purpose
// - Represent the durable file-level metadata header for one Atlas artifact.
// - Preserve pipeline identity, contract/shape hashes, field counts, operation counts, and byte sizes.
// - Keep artifact metadata separate from workspace memory, execution scheduling, and debug rendering.
//
// Design notes
// - This is durable artifact metadata.
// - This does not own native memory.
// - This does not reference workspace-owned buffers.
// - This does not contain a JobHandle.
// - This does not contain managed exceptions.
// - This does not include timestamps by default because deterministic artifacts need stable identity.
// - ContentHash zero is valid; content-hash presence is represented explicitly.
// - default(AtlasArtifactHeader) is not a concrete artifact header.
// - Missing/unwritten header state is represented by IsConcrete, not by magic value checks alone.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Execution;
using Lokrain.Atlas.Pipelines;
using Unity.Collections;

namespace Lokrain.Atlas.Artifacts
{
    /// <summary>
    /// Durable metadata header for one Atlas artifact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasArtifactHeader"/> is the file-level artifact identity block. It captures
    /// the compiled pipeline identity, compiled-plan counts, resolved workspace size, and stable
    /// schema/shape hashes needed by artifact readers, tooling, diagnostics, and future cache keys.
    /// </para>
    ///
    /// <para>
    /// This header intentionally does not contain timestamps, editor session identifiers, machine
    /// names, Unity object references, scene objects, renderer state, or workspace-owned native
    /// containers. Those would make canonical generated artifacts less stable and would blur the
    /// boundary between generation output and presentation/debug tooling.
    /// </para>
    ///
    /// <para>
    /// The content hash is optional because early vertical slices may write artifacts before final
    /// byte-level field hashing is implemented. Because zero is a valid hash value, content hash
    /// presence is represented by <see cref="HasContentHash"/>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasArtifactHeader :
        IEquatable<AtlasArtifactHeader>
    {
        private const byte NonConcreteState = 0;
        private const byte ConcreteState = 1;

        private const byte ContentHashAbsent = 0;
        private const byte ContentHashPresent = 1;

        private const ulong FnvOffsetBasis64 = 14695981039346656037UL;
        private const ulong FnvPrime64 = 1099511628211UL;

        /// <summary>
        /// ASCII payload for "ATLASART" encoded as a stable big-endian diagnostic constant.
        /// </summary>
        public const ulong MagicValue = 0x41544C4153415254UL;

        /// <summary>
        /// Current major artifact format version.
        /// </summary>
        public const ushort CurrentFormatMajorVersion = 1;

        /// <summary>
        /// Current minor artifact format version.
        /// </summary>
        public const ushort CurrentFormatMinorVersion = 0;

        /// <summary>
        /// Current header schema version.
        /// </summary>
        public const ushort CurrentHeaderVersion = 1;

        private readonly byte _state;
        private readonly byte _contentHashState;

        /// <summary>
        /// Artifact magic value.
        /// </summary>
        public readonly ulong Magic;

        /// <summary>
        /// Artifact format major version.
        /// </summary>
        public readonly ushort FormatMajorVersion;

        /// <summary>
        /// Artifact format minor version.
        /// </summary>
        public readonly ushort FormatMinorVersion;

        /// <summary>
        /// Header schema version.
        /// </summary>
        public readonly ushort HeaderVersion;

        /// <summary>
        /// Stable identity of the compiled pipeline that produced the artifact.
        /// </summary>
        public readonly AtlasPipelineId PipelineId;

        /// <summary>
        /// Stable diagnostic pipeline name.
        /// </summary>
        /// <remarks>
        /// This is not durable identity. Durable identity is <see cref="PipelineId"/>.
        /// </remarks>
        public readonly FixedString64Bytes PipelineName;

        /// <summary>
        /// Number of field contracts in the source Contract table.
        /// </summary>
        public readonly int ContractCount;

        /// <summary>
        /// Number of resolved fields represented by this artifact shape.
        /// </summary>
        public readonly int FieldCount;

        /// <summary>
        /// Number of compiled stage occurrences in the source plan.
        /// </summary>
        public readonly int StageCount;

        /// <summary>
        /// Number of compiled operation occurrences in the source plan.
        /// </summary>
        public readonly int OperationCount;

        /// <summary>
        /// Number of compiled operation bindings in the source plan.
        /// </summary>
        public readonly int BindingCount;

        /// <summary>
        /// Number of compiled operation bindings that resolved to concrete field contracts.
        /// </summary>
        public readonly int PresentBindingCount;

        /// <summary>
        /// Number of optional compiled operation bindings that were absent.
        /// </summary>
        public readonly int MissingOptionalBindingCount;

        /// <summary>
        /// Total logical byte length across all resolved artifact fields.
        /// </summary>
        public readonly long TotalByteLength;

        /// <summary>
        /// Total byte capacity across all resolved artifact fields.
        /// </summary>
        public readonly long TotalByteCapacity;

        /// <summary>
        /// Deterministic hash of field contract schema metadata.
        /// </summary>
        /// <remarks>
        /// This is a local Atlas artifact metadata hash, not a cryptographic hash.
        /// </remarks>
        public readonly ulong ContractHash;

        /// <summary>
        /// Deterministic hash of resolved field shape metadata.
        /// </summary>
        /// <remarks>
        /// This is a local Atlas artifact metadata hash, not a cryptographic hash.
        /// </remarks>
        public readonly ulong ShapeHash;

        /// <summary>
        /// Optional deterministic hash of artifact field contents.
        /// </summary>
        /// <remarks>
        /// Zero is valid. Use <see cref="HasContentHash"/> to determine whether this value is present.
        /// </remarks>
        public readonly ulong ContentHash;

        private AtlasArtifactHeader(
            AtlasPipelineId pipelineId,
            FixedString64Bytes pipelineName,
            int contractCount,
            int fieldCount,
            int stageCount,
            int operationCount,
            int bindingCount,
            int presentBindingCount,
            int missingOptionalBindingCount,
            long totalByteLength,
            long totalByteCapacity,
            ulong contractHash,
            ulong shapeHash,
            ulong contentHash,
            bool hasContentHash)
        {
            ValidateCountsOrThrow(
                contractCount,
                fieldCount,
                stageCount,
                operationCount,
                bindingCount,
                presentBindingCount,
                missingOptionalBindingCount,
                totalByteLength,
                totalByteCapacity);

            Magic = MagicValue;
            FormatMajorVersion = CurrentFormatMajorVersion;
            FormatMinorVersion = CurrentFormatMinorVersion;
            HeaderVersion = CurrentHeaderVersion;
            PipelineId = pipelineId;
            PipelineName = pipelineName;
            ContractCount = contractCount;
            FieldCount = fieldCount;
            StageCount = stageCount;
            OperationCount = operationCount;
            BindingCount = bindingCount;
            PresentBindingCount = presentBindingCount;
            MissingOptionalBindingCount = missingOptionalBindingCount;
            TotalByteLength = totalByteLength;
            TotalByteCapacity = totalByteCapacity;
            ContractHash = contractHash;
            ShapeHash = shapeHash;
            ContentHash = contentHash;
            _contentHashState = hasContentHash ? ContentHashPresent : ContentHashAbsent;
            _state = ConcreteState;
        }

        /// <summary>
        /// Gets whether this value represents a concrete artifact header.
        /// </summary>
        public bool IsConcrete
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state == ConcreteState;
        }

        /// <summary>
        /// Gets whether this value does not represent a concrete artifact header.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state != ConcreteState;
        }

        /// <summary>
        /// Gets whether <see cref="ContentHash"/> is present.
        /// </summary>
        public bool HasContentHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _contentHashState == ContentHashPresent;
        }

        /// <summary>
        /// Gets whether the header has the current artifact format version.
        /// </summary>
        public bool IsCurrentFormat
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FormatMajorVersion == CurrentFormatMajorVersion &&
                   FormatMinorVersion == CurrentFormatMinorVersion &&
                   HeaderVersion == CurrentHeaderVersion;
        }

        /// <summary>
        /// Creates an artifact header from a validated execution context.
        /// </summary>
        /// <param name="context">Execution context whose plan and workspace define the artifact shape.</param>
        /// <returns>A concrete artifact header without a content hash.</returns>
        public static AtlasArtifactHeader Create(
            AtlasExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Create(
                context.Plan,
                context.Shapes);
        }

        /// <summary>
        /// Creates an artifact header from a validated execution context and content hash.
        /// </summary>
        /// <param name="context">Execution context whose plan and workspace define the artifact shape.</param>
        /// <param name="contentHash">Deterministic content hash. Zero is valid.</param>
        /// <returns>A concrete artifact header with a content hash.</returns>
        public static AtlasArtifactHeader Create(
            AtlasExecutionContext context,
            ulong contentHash)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Create(
                context.Plan,
                context.Shapes,
                contentHash);
        }

        /// <summary>
        /// Creates an artifact header from a compiled plan and resolved shape set.
        /// </summary>
        /// <param name="plan">Compiled plan that produced the artifact.</param>
        /// <param name="shapes">Resolved shape set represented by the artifact.</param>
        /// <returns>A concrete artifact header without a content hash.</returns>
        public static AtlasArtifactHeader Create(
            AtlasCompiledPlan plan,
            AtlasResolvedShapeSet shapes)
        {
            return CreateCore(
                plan,
                shapes,
                contentHash: 0UL,
                hasContentHash: false);
        }

        /// <summary>
        /// Creates an artifact header from a compiled plan, resolved shape set, and content hash.
        /// </summary>
        /// <param name="plan">Compiled plan that produced the artifact.</param>
        /// <param name="shapes">Resolved shape set represented by the artifact.</param>
        /// <param name="contentHash">Deterministic content hash. Zero is valid.</param>
        /// <returns>A concrete artifact header with a content hash.</returns>
        public static AtlasArtifactHeader Create(
            AtlasCompiledPlan plan,
            AtlasResolvedShapeSet shapes,
            ulong contentHash)
        {
            return CreateCore(
                plan,
                shapes,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Validates that this header is concrete and internally consistent.
        /// </summary>
        /// <param name="parameterName">Parameter name used by thrown exceptions.</param>
        public void ValidateOrThrow(
            string parameterName = null)
        {
            if (!IsConcrete)
            {
                throw new ArgumentException(
                    "Atlas artifact header is not concrete.",
                    parameterName ?? nameof(AtlasArtifactHeader));
            }

            if (Magic != MagicValue)
            {
                throw new ArgumentException(
                    $"Atlas artifact header has magic value '{FormatHex(Magic)}', but expected '{FormatHex(MagicValue)}'.",
                    parameterName ?? nameof(AtlasArtifactHeader));
            }

            if (!IsCurrentFormat)
            {
                throw new ArgumentException(
                    $"Atlas artifact header has unsupported format version '{FormatMajorVersion}.{FormatMinorVersion}' and header version '{HeaderVersion}'.",
                    parameterName ?? nameof(AtlasArtifactHeader));
            }

            ValidateCountsOrThrow(
                ContractCount,
                FieldCount,
                StageCount,
                OperationCount,
                BindingCount,
                PresentBindingCount,
                MissingOptionalBindingCount,
                TotalByteLength,
                TotalByteCapacity);
        }

        /// <summary>
        /// Returns a copy of this header with a content hash.
        /// </summary>
        /// <param name="contentHash">Deterministic content hash. Zero is valid.</param>
        /// <returns>A concrete artifact header with the supplied content hash.</returns>
        public AtlasArtifactHeader WithContentHash(
            ulong contentHash)
        {
            ValidateOrThrow(nameof(AtlasArtifactHeader));

            return new AtlasArtifactHeader(
                PipelineId,
                PipelineName,
                ContractCount,
                FieldCount,
                StageCount,
                OperationCount,
                BindingCount,
                PresentBindingCount,
                MissingOptionalBindingCount,
                TotalByteLength,
                TotalByteCapacity,
                ContractHash,
                ShapeHash,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Determines whether this header equals another header.
        /// </summary>
        /// <param name="other">Header to compare with this header.</param>
        /// <returns><c>true</c> when both headers contain identical metadata.</returns>
        public bool Equals(
            AtlasArtifactHeader other)
        {
            return _state == other._state &&
                   _contentHashState == other._contentHashState &&
                   Magic == other.Magic &&
                   FormatMajorVersion == other.FormatMajorVersion &&
                   FormatMinorVersion == other.FormatMinorVersion &&
                   HeaderVersion == other.HeaderVersion &&
                   PipelineId == other.PipelineId &&
                   PipelineName.Equals(other.PipelineName) &&
                   ContractCount == other.ContractCount &&
                   FieldCount == other.FieldCount &&
                   StageCount == other.StageCount &&
                   OperationCount == other.OperationCount &&
                   BindingCount == other.BindingCount &&
                   PresentBindingCount == other.PresentBindingCount &&
                   MissingOptionalBindingCount == other.MissingOptionalBindingCount &&
                   TotalByteLength == other.TotalByteLength &&
                   TotalByteCapacity == other.TotalByteCapacity &&
                   ContractHash == other.ContractHash &&
                   ShapeHash == other.ShapeHash &&
                   ContentHash == other.ContentHash;
        }

        /// <summary>
        /// Determines whether this header equals another object.
        /// </summary>
        /// <param name="obj">Object to compare with this header.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an equal header.</returns>
        public override bool Equals(
            object obj)
        {
            return obj is AtlasArtifactHeader other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this header.
        /// </summary>
        /// <returns>A deterministic managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _state;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _contentHashState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Magic.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FormatMajorVersion;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FormatMinorVersion;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ HeaderVersion;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ PipelineId.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ PipelineName.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ContractCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FieldCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ StageCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ OperationCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ BindingCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ PresentBindingCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ MissingOptionalBindingCount;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ TotalByteLength.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ TotalByteCapacity.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ContractHash.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ShapeHash.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ContentHash.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic string for this header.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            if (!IsConcrete)
            {
                return "AtlasArtifactHeader(<empty>)";
            }

            var contentHashText = HasContentHash
                ? FormatHex(ContentHash)
                : "<absent>";

            return
                $"AtlasArtifactHeader(Pipeline={PipelineName}, Format={FormatMajorVersion}.{FormatMinorVersion}, Header={HeaderVersion}, Fields={FieldCount}, Operations={OperationCount}, Bytes={TotalByteLength}/{TotalByteCapacity}, ContractHash={FormatHex(ContractHash)}, ShapeHash={FormatHex(ShapeHash)}, ContentHash={contentHashText})";
        }

        /// <summary>
        /// Compares two headers for equality.
        /// </summary>
        public static bool operator ==(
            AtlasArtifactHeader left,
            AtlasArtifactHeader right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two headers for inequality.
        /// </summary>
        public static bool operator !=(
            AtlasArtifactHeader left,
            AtlasArtifactHeader right)
        {
            return !left.Equals(right);
        }

        private static AtlasArtifactHeader CreateCore(
            AtlasCompiledPlan plan,
            AtlasResolvedShapeSet shapes,
            ulong contentHash,
            bool hasContentHash)
        {
            ValidatePlanAndShapesOrThrow(
                plan,
                shapes);

            return new AtlasArtifactHeader(
                plan.PipelineId,
                plan.DebugName,
                plan.Contracts.Count,
                shapes.Count,
                plan.StageCount,
                plan.OperationCount,
                plan.BindingCount,
                plan.PresentBindingCount,
                plan.MissingOptionalBindingCount,
                shapes.TotalByteLength,
                shapes.TotalByteCapacity,
                ComputeContractHash(plan.Contracts),
                ComputeShapeHash(shapes),
                contentHash,
                hasContentHash);
        }

        private static void ValidatePlanAndShapesOrThrow(
            AtlasCompiledPlan plan,
            AtlasResolvedShapeSet shapes)
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

            if (shapes == null)
            {
                throw new ArgumentNullException(nameof(shapes));
            }

            shapes.ValidateOrThrow(nameof(shapes));

            if (shapes.Contracts == null)
            {
                throw new ArgumentException(
                    "Resolved shape set does not reference a Contract table.",
                    nameof(shapes));
            }

            if (plan.Contracts.Count != shapes.Contracts.Count)
            {
                throw new ArgumentException(
                    $"Compiled plan Contract table contains '{plan.Contracts.Count}' fields, but shape Contract table contains '{shapes.Contracts.Count}' fields.",
                    nameof(shapes));
            }

            if (plan.Contracts.Count != shapes.Count)
            {
                throw new ArgumentException(
                    $"Compiled plan Contract table contains '{plan.Contracts.Count}' fields, but shape set contains '{shapes.Count}' shapes.",
                    nameof(shapes));
            }

            for (var i = 0; i < plan.Contracts.Count; i++)
            {
                var contract = plan.Contracts[i];
                var shape = shapes[i];

                contract.ValidateTableReadyOrThrow($"plan.Contracts[{i}]");
                shape.ValidateOrThrow($"shapes[{i}]");

                if (shape.StableId != contract.StableId ||
                    shape.Slot != contract.Slot ||
                    shape.Role != contract.Role ||
                    shape.StorageFormat != contract.StorageFormat ||
                    shape.DeclaredShape != contract.LengthShape)
                {
                    throw new ArgumentException(
                        $"Shape at index '{i}' does not match compiled plan Contract '{contract.GetDiagnosticName()}'.",
                        nameof(shapes));
                }
            }
        }

        private static void ValidateCountsOrThrow(
            int contractCount,
            int fieldCount,
            int stageCount,
            int operationCount,
            int bindingCount,
            int presentBindingCount,
            int missingOptionalBindingCount,
            long totalByteLength,
            long totalByteCapacity)
        {
            if (contractCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contractCount));
            }

            if (fieldCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldCount));
            }

            if (stageCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageCount));
            }

            if (operationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(operationCount));
            }

            if (bindingCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingCount));
            }

            if (presentBindingCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(presentBindingCount));
            }

            if (missingOptionalBindingCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(missingOptionalBindingCount));
            }

            if (presentBindingCount + missingOptionalBindingCount > bindingCount)
            {
                throw new ArgumentException(
                    $"Present binding count '{presentBindingCount}' plus missing optional binding count '{missingOptionalBindingCount}' exceeds binding count '{bindingCount}'.");
            }

            if (totalByteLength < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(totalByteLength));
            }

            if (totalByteCapacity < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(totalByteCapacity));
            }

            if (totalByteLength > totalByteCapacity)
            {
                throw new ArgumentException(
                    $"Total byte length '{totalByteLength}' exceeds total byte capacity '{totalByteCapacity}'.");
            }
        }

        private static ulong ComputeContractHash(
            AtlasContractTable contracts)
        {
            var hash = FnvOffsetBasis64;

            AppendByte(ref hash, 0x43);
            AppendInt(ref hash, contracts.Count);

            for (var i = 0; i < contracts.Count; i++)
            {
                var contract = contracts[i];
                contract.ValidateTableReadyOrThrow($"contracts[{i}]");

                AppendStableDataId(ref hash, contract.StableId);
                AppendInt(ref hash, contract.Slot.Index);
                AppendInt(ref hash, (int)contract.Role);
                AppendInt(ref hash, (int)contract.StorageFormat.Kind);
                AppendInt(ref hash, contract.StorageFormat.ElementSize);
                AppendInt(ref hash, contract.StorageFormat.ElementAlignment);
                AppendULong(ref hash, contract.StorageFormat.ElementTypeHash);
                AppendInt(ref hash, (int)contract.Ownership);
                AppendInt(ref hash, (int)contract.Lifetime);
                AppendLengthShape(ref hash, contract.LengthShape);
                AppendUInt(ref hash, (uint)contract.Flags);
                AppendInt(ref hash, (int)contract.HashParticipation);
            }

            return hash;
        }

        private static ulong ComputeShapeHash(
            AtlasResolvedShapeSet shapes)
        {
            var hash = FnvOffsetBasis64;

            AppendByte(ref hash, 0x53);
            AppendInt(ref hash, shapes.Count);

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                shape.ValidateOrThrow($"shapes[{i}]");

                AppendStableDataId(ref hash, shape.StableId);
                AppendInt(ref hash, shape.Slot.Index);
                AppendInt(ref hash, (int)shape.Role);
                AppendInt(ref hash, (int)shape.StorageFormat.Kind);
                AppendInt(ref hash, shape.StorageFormat.ElementSize);
                AppendInt(ref hash, shape.StorageFormat.ElementAlignment);
                AppendULong(ref hash, shape.StorageFormat.ElementTypeHash);
                AppendLengthShape(ref hash, shape.DeclaredShape);
                AppendInt(ref hash, shape.Length);
                AppendInt(ref hash, shape.Capacity);
                AppendLong(ref hash, shape.ByteLength);
                AppendLong(ref hash, shape.ByteCapacity);
            }

            return hash;
        }

        private static void AppendLengthShape(
            ref ulong hash,
            LengthShape shape)
        {
            shape.ValidateOrThrow(nameof(shape));

            AppendInt(ref hash, (int)shape.Kind);
            AppendInt(ref hash, shape.FixedLength);
            AppendStableDataId(ref hash, shape.SourceFieldId);
            AppendInt(ref hash, BitConverter.SingleToInt32Bits(shape.CapacityMultiplier));
            AppendInt(ref hash, shape.CapacityPadding);
        }

        private static void AppendStableDataId(
            ref ulong hash,
            StableDataId stableId)
        {
            AppendULong(ref hash, stableId.High);
            AppendULong(ref hash, stableId.Low);
            AppendUShort(ref hash, stableId.Version);
        }

        private static void AppendLong(
            ref ulong hash,
            long value)
        {
            AppendULong(ref hash, unchecked((ulong)value));
        }

        private static void AppendInt(
            ref ulong hash,
            int value)
        {
            AppendUInt(ref hash, unchecked((uint)value));
        }

        private static void AppendUShort(
            ref ulong hash,
            ushort value)
        {
            AppendByte(ref hash, (byte)value);
            AppendByte(ref hash, (byte)(value >> 8));
        }

        private static void AppendUInt(
            ref ulong hash,
            uint value)
        {
            AppendByte(ref hash, (byte)value);
            AppendByte(ref hash, (byte)(value >> 8));
            AppendByte(ref hash, (byte)(value >> 16));
            AppendByte(ref hash, (byte)(value >> 24));
        }

        private static void AppendULong(
            ref ulong hash,
            ulong value)
        {
            AppendByte(ref hash, (byte)value);
            AppendByte(ref hash, (byte)(value >> 8));
            AppendByte(ref hash, (byte)(value >> 16));
            AppendByte(ref hash, (byte)(value >> 24));
            AppendByte(ref hash, (byte)(value >> 32));
            AppendByte(ref hash, (byte)(value >> 40));
            AppendByte(ref hash, (byte)(value >> 48));
            AppendByte(ref hash, (byte)(value >> 56));
        }

        private static void AppendByte(
            ref ulong hash,
            byte value)
        {
            unchecked
            {
                hash ^= value;
                hash *= FnvPrime64;
            }
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