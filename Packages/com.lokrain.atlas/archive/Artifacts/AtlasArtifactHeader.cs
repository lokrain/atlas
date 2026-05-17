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
// - ContractHash includes field identity, storage schema, ownership, lifetime, shape domain,
//   declared length shape, flags, and hash-participation policy.
// - ShapeHash includes resolved field identity, storage schema, shape domain, declared length shape,
//   resolved logical length, resolved capacity, byte length, and byte capacity.

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
    /// The content hash is optional. Because zero is a valid hash value, content-hash presence is
    /// represented by <see cref="HasContentHash"/>.
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
        public static AtlasArtifactHeader Create(AtlasExecutionContext context)
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
        /// Creates an artifact header from a compiled plan and the fields selected for capture.
        /// </summary>
        /// <remarks>
        /// The compiled plan remains the source of pipeline, operation, binding, and contract-count
        /// metadata. The supplied field table defines which workspace fields are serialized by the
        /// artifact profile. This allows default capture to exclude stage-transient fields while
        /// preserving the source plan contract count.
        /// </remarks>
        public static AtlasArtifactHeader Create(
            AtlasCompiledPlan plan,
            AtlasArtifactField[] fields)
        {
            return CreateCore(
                plan,
                fields,
                contentHash: 0UL,
                hasContentHash: false);
        }

        /// <summary>
        /// Creates an artifact header from a compiled plan, selected artifact fields, and content hash.
        /// </summary>
        public static AtlasArtifactHeader Create(
            AtlasCompiledPlan plan,
            AtlasArtifactField[] fields,
            ulong contentHash)
        {
            return CreateCore(
                plan,
                fields,
                contentHash,
                hasContentHash: true);
        }

        /// <summary>
        /// Creates an artifact header from serialized artifact metadata.
        /// </summary>
        internal static AtlasArtifactHeader CreateFromSerialized(
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
            return new AtlasArtifactHeader(
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

        /// <summary>
        /// Validates that this header is concrete and internally consistent.
        /// </summary>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasArtifactHeader);

            if (!IsConcrete)
            {
                throw new ArgumentException(
                    "Atlas artifact header is not concrete.",
                    name);
            }

            if (Magic != MagicValue)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas artifact header has magic value '{0}', but expected '{1}'.",
                        FormatHex(Magic),
                        FormatHex(MagicValue)),
                    name);
            }

            if (!IsCurrentFormat)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas artifact header has unsupported format version '{0}.{1}' and header version '{2}'.",
                        FormatMajorVersion,
                        FormatMinorVersion,
                        HeaderVersion),
                    name);
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
        public AtlasArtifactHeader WithContentHash(ulong contentHash)
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
        public bool Equals(AtlasArtifactHeader other)
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
        public override bool Equals(object obj)
        {
            return obj is AtlasArtifactHeader other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic hash code for this header.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _state;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _contentHashState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldULong(Magic);
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
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(TotalByteLength);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldLong(TotalByteCapacity);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldULong(ContractHash);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldULong(ShapeHash);
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ FoldULong(ContentHash);
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic string for this header.
        /// </summary>
        public override string ToString()
        {
            if (!IsConcrete)
            {
                return "AtlasArtifactHeader(<empty>)";
            }

            var contentHashText = HasContentHash
                ? FormatHex(ContentHash)
                : "<absent>";

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasArtifactHeader(Pipeline={0}, Format={1}.{2}, Header={3}, Fields={4}, Operations={5}, Bytes={6}/{7}, ContractHash={8}, ShapeHash={9}, ContentHash={10})",
                PipelineName,
                FormatMajorVersion,
                FormatMinorVersion,
                HeaderVersion,
                FieldCount,
                OperationCount,
                TotalByteLength,
                TotalByteCapacity,
                FormatHex(ContractHash),
                FormatHex(ShapeHash),
                contentHashText);
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

        private static AtlasArtifactHeader CreateCore(
            AtlasCompiledPlan plan,
            AtlasArtifactField[] fields,
            ulong contentHash,
            bool hasContentHash)
        {
            ValidatePlanAndArtifactFieldsOrThrow(
                plan,
                fields);

            var totalByteLength = 0L;
            var totalByteCapacity = 0L;

            for (var i = 0; i < fields.Length; i++)
            {
                totalByteLength = checked(totalByteLength + fields[i].ByteLength);
                totalByteCapacity = checked(totalByteCapacity + fields[i].ByteCapacity);
            }

            return new AtlasArtifactHeader(
                plan.PipelineId,
                plan.DebugName,
                plan.Contracts.Count,
                fields.Length,
                plan.StageCount,
                plan.OperationCount,
                plan.BindingCount,
                plan.PresentBindingCount,
                plan.MissingOptionalBindingCount,
                totalByteLength,
                totalByteCapacity,
                ComputeContractHash(plan.Contracts),
                ComputeArtifactFieldShapeHash(fields),
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
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Compiled plan Contract table contains {0} fields, but shape Contract table contains {1} fields.",
                        plan.Contracts.Count,
                        shapes.Contracts.Count),
                    nameof(shapes));
            }

            if (plan.Contracts.Count != shapes.Count)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Compiled plan Contract table contains {0} fields, but shape set contains {1} shapes.",
                        plan.Contracts.Count,
                        shapes.Count),
                    nameof(shapes));
            }

            for (var i = 0; i < plan.Contracts.Count; i++)
            {
                var contract = plan.Contracts[i];
                var shape = shapes[i];

                contract.ValidateTableReadyOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "plan.Contracts[{0}]",
                        i));

                shape.ValidateOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "shapes[{0}]",
                        i));

                if (shape.StableId != contract.StableId ||
                    shape.Slot != contract.Slot ||
                    shape.Role != contract.Role ||
                    shape.StorageFormat != contract.StorageFormat ||
                    shape.ShapeDomain != contract.ShapeDomain ||
                    shape.DeclaredShape != contract.LengthShape)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Shape at index {0} does not match compiled plan Contract '{1}'.",
                            i,
                            contract.GetDiagnosticName()),
                        nameof(shapes));
                }
            }
        }

        private static void ValidatePlanAndArtifactFieldsOrThrow(
            AtlasCompiledPlan plan,
            AtlasArtifactField[] fields)
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

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var seenSlots = new bool[plan.Contracts.Count];

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
                            "Artifact field row at index {0} has FieldIndex {1}. Field rows must be contiguous and ordered.",
                            i,
                            field.FieldIndex),
                        nameof(fields));
                }

                var contractIndex = field.Slot.Index;

                if (contractIndex < 0 ||
                    contractIndex >= plan.Contracts.Count)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Artifact field '{0}' references slot '{1}', but the compiled plan has {2} contracts.",
                            field.DebugName,
                            field.Slot,
                            plan.Contracts.Count),
                        nameof(fields));
                }

                if (seenSlots[contractIndex])
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Artifact field table contains duplicate slot '{0}'.",
                            field.Slot),
                        nameof(fields));
                }

                seenSlots[contractIndex] = true;

                var contract = plan.Contracts[contractIndex];

                contract.ValidateTableReadyOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "plan.Contracts[{0}]",
                        contractIndex));

                if (field.StableId != contract.StableId ||
                    field.Slot != contract.Slot ||
                    field.Role != contract.Role ||
                    field.StorageFormat != contract.StorageFormat ||
                    field.ShapeDomain != contract.ShapeDomain ||
                    field.DeclaredShape != contract.LengthShape ||
                    !field.DebugName.Equals(contract.DebugName))
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Artifact field row {0} does not match compiled plan Contract '{1}'.",
                            i,
                            contract.GetDiagnosticName()),
                        nameof(fields));
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
                throw new ArgumentOutOfRangeException(
                    nameof(contractCount),
                    contractCount,
                    "Contract count must be non-negative.");
            }

            if (fieldCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldCount),
                    fieldCount,
                    "Field count must be non-negative.");
            }

            if (fieldCount > contractCount)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Field count {0} must not exceed source Contract count {1}.",
                        fieldCount,
                        contractCount));
            }

            if (stageCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageCount),
                    stageCount,
                    "Stage count must be non-negative.");
            }

            if (operationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(operationCount),
                    operationCount,
                    "Operation count must be non-negative.");
            }

            if (bindingCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bindingCount),
                    bindingCount,
                    "Binding count must be non-negative.");
            }

            if (presentBindingCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentBindingCount),
                    presentBindingCount,
                    "Present binding count must be non-negative.");
            }

            if (missingOptionalBindingCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(missingOptionalBindingCount),
                    missingOptionalBindingCount,
                    "Missing optional binding count must be non-negative.");
            }

            if (presentBindingCount + missingOptionalBindingCount > bindingCount)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Present binding count {0} plus missing optional binding count {1} exceeds binding count {2}.",
                        presentBindingCount,
                        missingOptionalBindingCount,
                        bindingCount));
            }

            if (totalByteLength < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalByteLength),
                    totalByteLength,
                    "Total byte length must be non-negative.");
            }

            if (totalByteCapacity < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalByteCapacity),
                    totalByteCapacity,
                    "Total byte capacity must be non-negative.");
            }

            if (totalByteLength > totalByteCapacity)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Total byte length {0} exceeds total byte capacity {1}.",
                        totalByteLength,
                        totalByteCapacity));
            }
        }

        private static ulong ComputeContractHash(AtlasContractTable contracts)
        {
            var hash = FnvOffsetBasis64;

            AppendByte(ref hash, 0x43);
            AppendInt(ref hash, contracts.Count);

            for (var i = 0; i < contracts.Count; i++)
            {
                var contract = contracts[i];

                contract.ValidateTableReadyOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "contracts[{0}]",
                        i));

                AppendStableDataId(ref hash, contract.StableId);
                AppendInt(ref hash, contract.Slot.Index);
                AppendInt(ref hash, (int)contract.Role);
                AppendStorageFormat(ref hash, contract.StorageFormat);
                AppendInt(ref hash, (int)contract.Ownership);
                AppendInt(ref hash, (int)contract.Lifetime);
                AppendShapeDomain(ref hash, contract.ShapeDomain);
                AppendLengthShape(ref hash, contract.LengthShape);
                AppendUInt(ref hash, (uint)contract.Flags);
                AppendInt(ref hash, (int)contract.HashParticipation);
            }

            return hash;
        }

        private static ulong ComputeShapeHash(AtlasResolvedShapeSet shapes)
        {
            var hash = FnvOffsetBasis64;

            AppendByte(ref hash, 0x53);
            AppendInt(ref hash, shapes.Count);

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];

                shape.ValidateOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "shapes[{0}]",
                        i));

                AppendStableDataId(ref hash, shape.StableId);
                AppendInt(ref hash, shape.Slot.Index);
                AppendInt(ref hash, (int)shape.Role);
                AppendStorageFormat(ref hash, shape.StorageFormat);
                AppendShapeDomain(ref hash, shape.ShapeDomain);
                AppendLengthShape(ref hash, shape.DeclaredShape);
                AppendInt(ref hash, shape.Length);
                AppendInt(ref hash, shape.Capacity);
                AppendLong(ref hash, shape.ByteLength);
                AppendLong(ref hash, shape.ByteCapacity);
            }

            return hash;
        }

        private static ulong ComputeArtifactFieldShapeHash(AtlasArtifactField[] fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var hash = FnvOffsetBasis64;

            AppendByte(ref hash, 0x53);
            AppendInt(ref hash, fields.Length);

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                field.ValidateOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "fields[{0}]",
                        i));

                AppendStableDataId(ref hash, field.StableId);
                AppendInt(ref hash, field.Slot.Index);
                AppendInt(ref hash, (int)field.Role);
                AppendStorageFormat(ref hash, field.StorageFormat);
                AppendShapeDomain(ref hash, field.ShapeDomain);
                AppendLengthShape(ref hash, field.DeclaredShape);
                AppendInt(ref hash, field.Length);
                AppendInt(ref hash, field.Capacity);
                AppendLong(ref hash, field.ByteLength);
                AppendLong(ref hash, field.ByteCapacity);
            }

            return hash;
        }

        private static void AppendStorageFormat(
            ref ulong hash,
            StorageFormat storageFormat)
        {
            storageFormat.ValidateOrThrow(nameof(storageFormat));

            AppendInt(ref hash, (int)storageFormat.Kind);
            AppendInt(ref hash, storageFormat.ElementSize);
            AppendInt(ref hash, storageFormat.ElementAlignment);
            AppendULong(ref hash, storageFormat.ElementTypeHash);
        }

        private static void AppendShapeDomain(
            ref ulong hash,
            AtlasShapeDomain domain)
        {
            domain.ValidateOrThrow(nameof(domain));

            AppendInt(ref hash, (int)domain.Kind);
            AppendFixedString64(ref hash, domain.Name);
            AppendByte(ref hash, domain.HasSourceField ? (byte)1 : (byte)0);
            AppendStableDataId(ref hash, domain.SourceFieldId);
        }

        private static void AppendLengthShape(
            ref ulong hash,
            LengthShape shape)
        {
            shape.ValidateOrThrow(nameof(shape));

            AppendInt(ref hash, (int)shape.Kind);
            AppendInt(ref hash, shape.FixedLength);
            AppendStableDataId(ref hash, shape.SourceFieldId);
            AppendFixedString64(ref hash, shape.Name);
            AppendInt(ref hash, shape.CapacityMultiplierNumerator);
            AppendInt(ref hash, shape.CapacityMultiplierDenominator);
            AppendInt(ref hash, shape.CapacityPadding);
        }

        private static void AppendStableDataId(
            ref ulong hash,
            StableDataId stableId)
        {
            stableId.ValidateOrThrow(nameof(stableId));

            AppendULong(ref hash, stableId.High);
            AppendULong(ref hash, stableId.Low);
            AppendUShort(ref hash, stableId.Version);
        }

        private static void AppendFixedString64(
            ref ulong hash,
            FixedString64Bytes value)
        {
            AppendInt(ref hash, value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                AppendByte(ref hash, value[i]);
            }
        }

        private static void AppendLong(
            ref ulong hash,
            long value)
        {
            AppendULong(
                ref hash,
                unchecked((ulong)value));
        }

        private static void AppendInt(
            ref ulong hash,
            int value)
        {
            AppendUInt(
                ref hash,
                unchecked((uint)value));
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

        private static int FoldLong(long value)
        {
            return FoldULong(
                unchecked((ulong)value));
        }

        private static int FoldULong(ulong value)
        {
            unchecked
            {
                return (int)(value ^ (value >> 32));
            }
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