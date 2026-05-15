// Packages/com.lokrain.atlas/Runtime/Diagnostics/AtlasDiagnosticLocation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Define stable diagnostic source locations for Atlas reports.
// - Represent Field, Contract, Operation, Stage, Pipeline, compilation, execution, and artifact locations.
// - Preserve authored/compiled sequence indices without requiring workspace or scheduler data.
// - Keep location separate from severity, diagnostic code, and human-readable message.
//
// Design notes
// - Location is metadata-only.
// - Location does not own or resolve Field memory.
// - Indices are sequence-local occurrence indices, not durable identities.
// - Stable ids identify durable contracts when available.
// - Debug names are diagnostic context only and must not be used as identity.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Stable kind of Atlas diagnostic location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Location kind identifies what the diagnostic is attached to. It is not the diagnostic
    /// subsystem that owns the code. For example, a compilation diagnostic may point at a Field,
    /// an operation binding, or the whole compiled plan.
    /// </para>
    ///
    /// <para>
    /// Numeric values are part of the public diagnostic contract. Do not reorder existing
    /// values after release.
    /// </para>
    /// </remarks>
    public enum AtlasDiagnosticLocationKind : byte
    {
        /// <summary>
        /// No diagnostic location.
        /// </summary>
        None = 0,

        /// <summary>
        /// Whole package or pass-level location.
        /// </summary>
        Package = 1,

        /// <summary>
        /// Field declaration location.
        /// </summary>
        Field = 2,

        /// <summary>
        /// Contract or Contract-table location.
        /// </summary>
        Contract = 3,

        /// <summary>
        /// Operation definition location.
        /// </summary>
        Operation = 4,

        /// <summary>
        /// Operation access or binding declaration location.
        /// </summary>
        OperationAccess = 5,

        /// <summary>
        /// Stage definition location.
        /// </summary>
        Stage = 6,

        /// <summary>
        /// Pipeline definition location.
        /// </summary>
        Pipeline = 7,

        /// <summary>
        /// Whole compiled plan location.
        /// </summary>
        CompiledPlan = 8,

        /// <summary>
        /// Compiled stage occurrence location.
        /// </summary>
        CompiledStage = 9,

        /// <summary>
        /// Compiled operation occurrence location.
        /// </summary>
        CompiledOperation = 10,

        /// <summary>
        /// Compiled binding occurrence location.
        /// </summary>
        CompiledBinding = 11,

        /// <summary>
        /// Workspace or memory-resolution location.
        /// </summary>
        Workspace = 12,

        /// <summary>
        /// Runtime execution location.
        /// </summary>
        Execution = 13,

        /// <summary>
        /// Durable artifact location.
        /// </summary>
        Artifact = 14
    }

    /// <summary>
    /// Stable diagnostic source location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDiagnosticLocation"/> gives editor tooling, CI output, and tests enough
    /// context to point a diagnostic at the relevant Atlas contract object or compiled occurrence.
    /// </para>
    ///
    /// <para>
    /// Stable ids identify durable objects such as Fields, operations, stages, and pipelines.
    /// Sequence indices identify authored or compiled occurrences. This distinction matters because
    /// repeated stages and repeated operations are valid metadata.
    /// </para>
    ///
    /// <para>
    /// The default value is the empty location and is valid only for empty/default diagnostics.
    /// Produced diagnostics should use a valid non-empty location.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasDiagnosticLocation :
        IEquatable<AtlasDiagnosticLocation>
    {
        /// <summary>
        /// Index value used when a location has no occurrence index for a given dimension.
        /// </summary>
        public const int NoIndex = -1;

        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Reserved empty diagnostic location.
        /// </summary>
        public static readonly AtlasDiagnosticLocation Empty = default;

        /// <summary>
        /// Kind of object or occurrence identified by this location.
        /// </summary>
        public readonly AtlasDiagnosticLocationKind Kind;

        /// <summary>
        /// Stable durable subject id when one exists.
        /// </summary>
        /// <remarks>
        /// For a Field or Contract diagnostic this is normally the Field stable id.
        /// For an operation, stage, or pipeline diagnostic this is normally the corresponding
        /// operation, stage, or pipeline stable identity converted to <see cref="StableDataId"/>.
        /// </remarks>
        public readonly StableDataId StableId;

        /// <summary>
        /// Pipeline-local stage occurrence index, or <see cref="NoIndex"/> when not applicable.
        /// </summary>
        public readonly int StageIndex;

        /// <summary>
        /// Stage-local operation occurrence index, or <see cref="NoIndex"/> when not applicable.
        /// </summary>
        public readonly int OperationIndex;

        /// <summary>
        /// Operation-local binding occurrence index, or <see cref="NoIndex"/> when not applicable.
        /// </summary>
        public readonly int BindingIndex;

        /// <summary>
        /// Optional diagnostic name for editor and log output.
        /// </summary>
        /// <remarks>
        /// Debug name is context only. It is not identity and must not be used for lookup or
        /// durable comparison.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        private AtlasDiagnosticLocation(
            AtlasDiagnosticLocationKind kind,
            StableDataId stableId,
            int stageIndex,
            int operationIndex,
            int bindingIndex,
            FixedString64Bytes debugName)
        {
            Kind = kind;
            StableId = stableId;
            StageIndex = stageIndex;
            OperationIndex = operationIndex;
            BindingIndex = bindingIndex;
            DebugName = debugName;
        }

        /// <summary>
        /// Gets whether this value is the reserved empty location.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasDiagnosticLocationKind.None &&
                   StableId == default &&
                   StageIndex == 0 &&
                   OperationIndex == 0 &&
                   BindingIndex == 0 &&
                   DebugName.IsEmpty;
        }

        /// <summary>
        /// Gets whether this value is valid for a produced diagnostic.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind.IsValid() &&
                   StageIndex >= NoIndex &&
                   OperationIndex >= NoIndex &&
                   BindingIndex >= NoIndex;
        }

        /// <summary>
        /// Gets whether this location has a durable stable subject id.
        /// </summary>
        public bool HasStableId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StableId != default;
        }

        /// <summary>
        /// Gets whether this location has a stage occurrence index.
        /// </summary>
        public bool HasStageIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StageIndex >= 0;
        }

        /// <summary>
        /// Gets whether this location has an operation occurrence index.
        /// </summary>
        public bool HasOperationIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => OperationIndex >= 0;
        }

        /// <summary>
        /// Gets whether this location has a binding occurrence index.
        /// </summary>
        public bool HasBindingIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BindingIndex >= 0;
        }

        /// <summary>
        /// Creates a package-level diagnostic location.
        /// </summary>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated package-level diagnostic location.</returns>
        public static AtlasDiagnosticLocation Package(FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Package,
                default,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a Field diagnostic location.
        /// </summary>
        /// <param name="fieldId">Stable Field id.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated Field diagnostic location.</returns>
        public static AtlasDiagnosticLocation Field(
            StableDataId fieldId,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Field,
                fieldId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a Contract diagnostic location.
        /// </summary>
        /// <param name="fieldId">Stable Field id owned by the Contract.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated Contract diagnostic location.</returns>
        public static AtlasDiagnosticLocation Contract(
            StableDataId fieldId,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Contract,
                fieldId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates an operation-definition diagnostic location.
        /// </summary>
        /// <param name="operationId">Stable operation id converted to <see cref="StableDataId"/> by the caller.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated operation diagnostic location.</returns>
        public static AtlasDiagnosticLocation Operation(
            StableDataId operationId,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Operation,
                operationId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates an operation-access diagnostic location.
        /// </summary>
        /// <param name="fieldId">Stable Field id referenced by the access.</param>
        /// <param name="bindingIndex">Operation-local binding index.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated operation-access diagnostic location.</returns>
        public static AtlasDiagnosticLocation OperationAccess(
            StableDataId fieldId,
            int bindingIndex,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.OperationAccess,
                fieldId,
                NoIndex,
                NoIndex,
                bindingIndex,
                debugName);
        }

        /// <summary>
        /// Creates a stage-definition diagnostic location.
        /// </summary>
        /// <param name="stageId">Stable stage id converted to <see cref="StableDataId"/> by the caller.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated stage diagnostic location.</returns>
        public static AtlasDiagnosticLocation Stage(
            StableDataId stageId,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Stage,
                stageId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a pipeline-definition diagnostic location.
        /// </summary>
        /// <param name="pipelineId">Stable pipeline id converted to <see cref="StableDataId"/> by the caller.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated pipeline diagnostic location.</returns>
        public static AtlasDiagnosticLocation Pipeline(
            StableDataId pipelineId,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Pipeline,
                pipelineId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a compiled-plan diagnostic location.
        /// </summary>
        /// <param name="pipelineId">Stable pipeline id converted to <see cref="StableDataId"/> by the caller.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated compiled-plan diagnostic location.</returns>
        public static AtlasDiagnosticLocation CompiledPlan(
            StableDataId pipelineId,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.CompiledPlan,
                pipelineId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a compiled-stage occurrence diagnostic location.
        /// </summary>
        /// <param name="stageId">Stable stage id converted to <see cref="StableDataId"/> by the caller.</param>
        /// <param name="stageIndex">Pipeline-local stage occurrence index.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated compiled-stage diagnostic location.</returns>
        public static AtlasDiagnosticLocation CompiledStage(
            StableDataId stageId,
            int stageIndex,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.CompiledStage,
                stageId,
                stageIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a compiled-operation occurrence diagnostic location.
        /// </summary>
        /// <param name="operationId">Stable operation id converted to <see cref="StableDataId"/> by the caller.</param>
        /// <param name="stageIndex">Pipeline-local stage occurrence index.</param>
        /// <param name="operationIndex">Stage-local operation occurrence index.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated compiled-operation diagnostic location.</returns>
        public static AtlasDiagnosticLocation CompiledOperation(
            StableDataId operationId,
            int stageIndex,
            int operationIndex,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.CompiledOperation,
                operationId,
                stageIndex,
                operationIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a compiled-binding occurrence diagnostic location.
        /// </summary>
        /// <param name="fieldId">Stable Field id referenced by the compiled binding.</param>
        /// <param name="stageIndex">Pipeline-local stage occurrence index.</param>
        /// <param name="operationIndex">Stage-local operation occurrence index.</param>
        /// <param name="bindingIndex">Operation-local binding occurrence index.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated compiled-binding diagnostic location.</returns>
        public static AtlasDiagnosticLocation CompiledBinding(
            StableDataId fieldId,
            int stageIndex,
            int operationIndex,
            int bindingIndex,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.CompiledBinding,
                fieldId,
                stageIndex,
                operationIndex,
                bindingIndex,
                debugName);
        }

        /// <summary>
        /// Creates a workspace diagnostic location.
        /// </summary>
        /// <param name="fieldId">Optional stable Field id associated with the workspace issue.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated workspace diagnostic location.</returns>
        public static AtlasDiagnosticLocation Workspace(
            StableDataId fieldId = default,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Workspace,
                fieldId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates an execution diagnostic location.
        /// </summary>
        /// <param name="stableId">Optional stable id associated with the execution issue.</param>
        /// <param name="stageIndex">Pipeline-local stage occurrence index, or <see cref="NoIndex"/>.</param>
        /// <param name="operationIndex">Stage-local operation occurrence index, or <see cref="NoIndex"/>.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated execution diagnostic location.</returns>
        public static AtlasDiagnosticLocation Execution(
            StableDataId stableId = default,
            int stageIndex = NoIndex,
            int operationIndex = NoIndex,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Execution,
                stableId,
                stageIndex,
                operationIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates an artifact diagnostic location.
        /// </summary>
        /// <param name="stableId">Optional stable id associated with the artifact issue.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated artifact diagnostic location.</returns>
        public static AtlasDiagnosticLocation Artifact(
            StableDataId stableId = default,
            FixedString64Bytes debugName = default)
        {
            return Create(
                AtlasDiagnosticLocationKind.Artifact,
                stableId,
                NoIndex,
                NoIndex,
                NoIndex,
                debugName);
        }

        /// <summary>
        /// Creates a validated diagnostic location.
        /// </summary>
        /// <param name="kind">Location kind.</param>
        /// <param name="stableId">Optional stable subject id.</param>
        /// <param name="stageIndex">Stage occurrence index, or <see cref="NoIndex"/>.</param>
        /// <param name="operationIndex">Operation occurrence index, or <see cref="NoIndex"/>.</param>
        /// <param name="bindingIndex">Binding occurrence index, or <see cref="NoIndex"/>.</param>
        /// <param name="debugName">Optional diagnostic name.</param>
        /// <returns>A validated diagnostic location.</returns>
        public static AtlasDiagnosticLocation Create(
            AtlasDiagnosticLocationKind kind,
            StableDataId stableId,
            int stageIndex,
            int operationIndex,
            int bindingIndex,
            FixedString64Bytes debugName = default)
        {
            var location = new AtlasDiagnosticLocation(
                kind,
                stableId,
                stageIndex,
                operationIndex,
                bindingIndex,
                debugName);

            location.ValidateOrThrow(nameof(location));
            return location;
        }

        /// <summary>
        /// Creates a copy of this location with a replacement stable subject id.
        /// </summary>
        /// <param name="stableId">Replacement stable subject id.</param>
        /// <returns>A validated diagnostic location.</returns>
        public AtlasDiagnosticLocation WithStableId(StableDataId stableId)
        {
            return Create(
                Kind,
                stableId,
                StageIndex,
                OperationIndex,
                BindingIndex,
                DebugName);
        }

        /// <summary>
        /// Creates a copy of this location with replacement occurrence indices.
        /// </summary>
        /// <param name="stageIndex">Replacement stage occurrence index.</param>
        /// <param name="operationIndex">Replacement operation occurrence index.</param>
        /// <param name="bindingIndex">Replacement binding occurrence index.</param>
        /// <returns>A validated diagnostic location.</returns>
        public AtlasDiagnosticLocation WithIndices(
            int stageIndex,
            int operationIndex = NoIndex,
            int bindingIndex = NoIndex)
        {
            return Create(
                Kind,
                StableId,
                stageIndex,
                operationIndex,
                bindingIndex,
                DebugName);
        }

        /// <summary>
        /// Creates a copy of this location with a replacement diagnostic debug name.
        /// </summary>
        /// <param name="debugName">Replacement diagnostic debug name.</param>
        /// <returns>A validated diagnostic location.</returns>
        public AtlasDiagnosticLocation WithDebugName(FixedString64Bytes debugName)
        {
            return Create(
                Kind,
                StableId,
                StageIndex,
                OperationIndex,
                BindingIndex,
                debugName);
        }

        /// <summary>
        /// Throws when this location is not valid for a produced diagnostic.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the location kind is invalid or any occurrence index is below <see cref="NoIndex"/>.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            if (IsValid)
            {
                return;
            }

            throw new ArgumentException(
                "Atlas diagnostic location must have a valid location kind and occurrence indices greater than or equal to -1.",
                parameterName ?? nameof(AtlasDiagnosticLocation));
        }

        /// <summary>
        /// Determines whether this location equals another location.
        /// </summary>
        /// <param name="other">Location to compare against.</param>
        /// <returns>
        /// <c>true</c> when kind, stable id, indices, and debug name match; otherwise <c>false</c>.
        /// </returns>
        public bool Equals(AtlasDiagnosticLocation other)
        {
            return Kind == other.Kind &&
                   StableId == other.StableId &&
                   StageIndex == other.StageIndex &&
                   OperationIndex == other.OperationIndex &&
                   BindingIndex == other.BindingIndex &&
                   DebugName.Equals(other.DebugName);
        }

        /// <summary>
        /// Determines whether this location equals an object instance.
        /// </summary>
        /// <param name="obj">Object instance to compare against.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasDiagnosticLocation"/>
        /// with the same payload; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasDiagnosticLocation other && Equals(other);
        }

        /// <summary>
        /// Returns a managed hash code for this diagnostic location.
        /// </summary>
        /// <returns>A managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;

                hash = (hash * HashMultiplier) ^ Kind.GetHashCode();
                hash = (hash * HashMultiplier) ^ StableId.GetHashCode();
                hash = (hash * HashMultiplier) ^ StageIndex;
                hash = (hash * HashMultiplier) ^ OperationIndex;
                hash = (hash * HashMultiplier) ^ BindingIndex;
                hash = (hash * HashMultiplier) ^ DebugName.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a human-readable diagnostic location label.
        /// </summary>
        /// <returns>A diagnostic location label suitable for logs and editor output.</returns>
        public override string ToString()
        {
            if (IsEmpty)
            {
                return "<no location>";
            }

            var name = DebugName.IsEmpty
                ? string.Empty
                : string.Format(
                    CultureInfo.InvariantCulture,
                    " '{0}'",
                    DebugName.ToString());

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1} [stage={2}, operation={3}, binding={4}]",
                Kind,
                name,
                StageIndex,
                OperationIndex,
                BindingIndex);
        }

        /// <summary>
        /// Determines whether two locations are equal.
        /// </summary>
        public static bool operator ==(
            AtlasDiagnosticLocation left,
            AtlasDiagnosticLocation right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two locations are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasDiagnosticLocation left,
            AtlasDiagnosticLocation right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Extension helpers for <see cref="AtlasDiagnosticLocationKind"/>.
    /// </summary>
    public static class AtlasDiagnosticLocationKindExtensions
    {
        /// <summary>
        /// Returns whether the location kind is valid for a produced diagnostic.
        /// </summary>
        /// <param name="kind">Location kind to test.</param>
        /// <returns><c>true</c> when the location kind is a defined non-empty Atlas location kind.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this AtlasDiagnosticLocationKind kind)
        {
            return kind >= AtlasDiagnosticLocationKind.Package &&
                   kind <= AtlasDiagnosticLocationKind.Artifact;
        }
    }
}