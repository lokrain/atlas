// Runtime/Pipelines/AtlasPipelineDefinition.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Pipelines
//
// Purpose
// - Define one durable Atlas pipeline contract.
// - Bind stable pipeline identity and diagnostic pipeline name to an ordered stage sequence.
// - Preserve authored stage occurrence order exactly.
// - Keep pipeline contracts separate from compiled plans, workspace memory, jobs, and artifacts.
//
// Design notes
// - A pipeline is a durable semantic route/preset over one ordered stage sequence.
// - Repeated stage definitions are allowed at this layer; route policy and compiler validation
//   may reject them later when a concrete pipeline family requires unique stage identities.
// - Stage occurrence index is meaningful inside the pipeline.
// - Pipeline identity is not stage identity.
// - Pipeline definitions are authoring/compilation metadata, not runtime execution payloads.
// - Jobs must not receive this type.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Stages;
using Unity.Collections;

namespace Lokrain.Atlas.Pipelines
{
    /// <summary>
    /// Defines one durable Atlas pipeline contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pipeline is a named semantic generation route. It owns a stable pipeline id,
    /// a stable diagnostic name, and an authored ordered sequence of stage occurrences.
    /// </para>
    ///
    /// <para>
    /// A pipeline does not own memory. It does not schedule jobs. It does not resolve Field slots.
    /// It does not know concrete native containers. The compiler resolves the pipeline's stages
    /// and operations against the Field Contract table and emits a compiled execution plan.
    /// </para>
    ///
    /// <para>
    /// Stage order is preserved exactly. This type deliberately uses a plain immutable managed
    /// array and linear scans instead of dictionaries. Pipeline definitions are metadata; compiled
    /// plans are the execution structures.
    /// </para>
    /// </remarks>
    public sealed class AtlasPipelineDefinition :
        IReadOnlyList<AtlasStageDefinition>
    {
        private const int InvalidStageIndex = -1;

        private readonly AtlasStageDefinition[] _stages;

        /// <summary>
        /// Stable, versioned identity of this pipeline contract.
        /// </summary>
        public readonly AtlasPipelineId PipelineId;

        /// <summary>
        /// Stable diagnostic pipeline name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        /// <remarks>
        /// Pipeline names are not durable identity. Durable identity belongs to <see cref="PipelineId"/>.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        private AtlasPipelineDefinition(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName,
            AtlasStageDefinition[] stages)
        {
            PipelineId = pipelineId;
            DebugName = debugName;

            ValidateHeaderOrThrow(
                pipelineId,
                debugName);

            _stages = CopyAndValidateStages(
                debugName,
                stages);
        }

        /// <summary>
        /// Gets the number of stage occurrences in this pipeline.
        /// </summary>
        public int Count => _stages.Length;

        /// <summary>
        /// Gets whether this pipeline contains no stage occurrences.
        /// </summary>
        /// <remarks>
        /// Concrete pipeline definitions created by this type are required to contain at least one
        /// stage occurrence, so this property normally returns <c>false</c>.
        /// </remarks>
        public bool IsEmpty => _stages.Length == 0;

        /// <summary>
        /// Gets whether at least one stage occurrence reads Field contents.
        /// </summary>
        public bool ReadsContent
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].ReadsContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one stage occurrence writes, appends, consumes, or mutates Field contents.
        /// </summary>
        public bool WritesContent
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].WritesContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one stage occurrence declares an optional Field binding.
        /// </summary>
        public bool HasOptionalAccess
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].HasOptionalAccess)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one stage occurrence declares a shape-only Field binding.
        /// </summary>
        public bool HasShapeOnlyAccess
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].HasShapeOnlyAccess)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the total number of operation occurrences across all stage occurrences.
        /// </summary>
        public int OperationCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _stages.Length; i++)
                {
                    count += _stages[i].Count;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the stage definition at a zero-based pipeline-local stage occurrence index.
        /// </summary>
        /// <param name="index">Zero-based stage occurrence index.</param>
        /// <returns>The stage definition at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this pipeline's stage range.
        /// </exception>
        public AtlasStageDefinition this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _stages[index];
            }
        }

        /// <summary>
        /// Creates a pipeline definition from explicitly ordered stage occurrences.
        /// </summary>
        /// <param name="pipelineId">Stable, versioned pipeline identity.</param>
        /// <param name="debugName">Stable diagnostic pipeline name.</param>
        /// <param name="stages">Stage occurrences in authored pipeline order.</param>
        /// <returns>A validated pipeline definition.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stages"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the pipeline header is invalid or any stage occurrence is invalid.
        /// </exception>
        public static AtlasPipelineDefinition Create(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName,
            params AtlasStageDefinition[] stages)
        {
            return new AtlasPipelineDefinition(
                pipelineId,
                debugName,
                stages);
        }

        /// <summary>
        /// Determines whether this pipeline contains at least one occurrence of the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to search for.</param>
        /// <returns><c>true</c> when at least one matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool Contains(AtlasStageId stageId)
        {
            return IndexOfFirst(stageId) != InvalidStageIndex;
        }

        /// <summary>
        /// Determines whether this pipeline contains at least one stage occurrence with the supplied debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to search for.</param>
        /// <returns><c>true</c> when at least one matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool ContainsStage(FixedString64Bytes stageName)
        {
            return IndexOfFirstStage(stageName) != InvalidStageIndex;
        }

        /// <summary>
        /// Returns the first stage occurrence index matching the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to search for.</param>
        /// <returns>The first matching stage occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirst(AtlasStageId stageId)
        {

            for (var i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].StageId == stageId)
                {
                    return i;
                }
            }

            return InvalidStageIndex;
        }

        /// <summary>
        /// Returns the first stage occurrence index matching the supplied debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to search for.</param>
        /// <returns>The first matching stage occurrence index, or <c>-1</c> when absent.</returns>
        public int IndexOfFirstStage(FixedString64Bytes stageName)
        {
            if (stageName.IsEmpty)
            {
                return InvalidStageIndex;
            }

            for (var i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].DebugName.Equals(stageName))
                {
                    return i;
                }
            }

            return InvalidStageIndex;
        }

        /// <summary>
        /// Resolves the first stage occurrence index for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <returns>The first matching stage occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageId"/> is invalid or absent from this pipeline.
        /// </exception>
        public int GetRequiredFirstIndex(AtlasStageId stageId)
        {
            var index = IndexOfFirst(stageId);

            if (index != InvalidStageIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Atlas Pipeline definition '{DebugName}' does not contain stage id '{stageId}'.",
                nameof(stageId));
        }

        /// <summary>
        /// Resolves the first stage occurrence index for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <returns>The first matching stage occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageName"/> is empty or absent from this pipeline.
        /// </exception>
        public int GetRequiredFirstStageIndex(FixedString64Bytes stageName)
        {
            var index = IndexOfFirstStage(stageName);

            if (index != InvalidStageIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Atlas Pipeline definition '{DebugName}' does not contain stage debug name '{stageName}'.",
                nameof(stageName));
        }

        /// <summary>
        /// Attempts to resolve the first stage occurrence index for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <param name="index">First matching pipeline-local stage index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstIndex(
            AtlasStageId stageId,
            out int index)
        {
            index = IndexOfFirst(stageId);
            return index != InvalidStageIndex;
        }

        /// <summary>
        /// Attempts to resolve the first stage occurrence index for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <param name="index">First matching pipeline-local stage index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstStageIndex(
            FixedString64Bytes stageName,
            out int index)
        {
            index = IndexOfFirstStage(stageName);
            return index != InvalidStageIndex;
        }

        /// <summary>
        /// Attempts to resolve the first stage occurrence for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <param name="stage">First matching stage definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstStage(
            AtlasStageId stageId,
            out AtlasStageDefinition stage)
        {
            var index = IndexOfFirst(stageId);

            if (index != InvalidStageIndex)
            {
                stage = _stages[index];
                return true;
            }

            stage = null;
            return false;
        }

        /// <summary>
        /// Attempts to resolve the first stage occurrence for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <param name="stage">First matching stage definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstStage(
            FixedString64Bytes stageName,
            out AtlasStageDefinition stage)
        {
            var index = IndexOfFirstStage(stageName);

            if (index != InvalidStageIndex)
            {
                stage = _stages[index];
                return true;
            }

            stage = null;
            return false;
        }

        /// <summary>
        /// Resolves the first stage occurrence for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <returns>The first matching stage definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageId"/> is invalid or absent from this pipeline.
        /// </exception>
        public AtlasStageDefinition GetRequiredFirstStage(AtlasStageId stageId)
        {
            return _stages[GetRequiredFirstIndex(stageId)];
        }

        /// <summary>
        /// Resolves the first stage occurrence for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <returns>The first matching stage definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageName"/> is empty or absent from this pipeline.
        /// </exception>
        public AtlasStageDefinition GetRequiredFirstStage(FixedString64Bytes stageName)
        {
            return _stages[GetRequiredFirstStageIndex(stageName)];
        }

        /// <summary>
        /// Counts stage occurrences matching the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to count.</param>
        /// <returns>The number of matching stage occurrences.</returns>
        public int CountOf(AtlasStageId stageId)
        {

            var count = 0;

            for (var i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].StageId == stageId)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts stage occurrences matching the supplied stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to count.</param>
        /// <returns>The number of matching stage occurrences.</returns>
        public int CountOfStage(FixedString64Bytes stageName)
        {
            if (stageName.IsEmpty)
            {
                return 0;
            }

            var count = 0;

            for (var i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].DebugName.Equals(stageName))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies all pipeline-local stage occurrence indices matching the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is too small.
        /// </exception>
        public int CopyIndices(
            AtlasStageId stageId,
            int[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var count = CountOf(stageId);

            if (destination.Length < count)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than matching stage count '{count}'.",
                    nameof(destination));
            }


            var writeIndex = 0;

            for (var i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].StageId == stageId)
                {
                    destination[writeIndex++] = i;
                }
            }

            return writeIndex;
        }

        /// <summary>
        /// Copies all pipeline-local stage occurrence indices matching the supplied stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to search for.</param>
        /// <param name="destination">Destination index array.</param>
        /// <returns>The number of copied indices.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is too small.
        /// </exception>
        public int CopyStageIndices(
            FixedString64Bytes stageName,
            int[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var count = CountOfStage(stageName);

            if (destination.Length < count)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than matching stage count '{count}'.",
                    nameof(destination));
            }

            if (stageName.IsEmpty)
            {
                return 0;
            }

            var writeIndex = 0;

            for (var i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].DebugName.Equals(stageName))
                {
                    destination[writeIndex++] = i;
                }
            }

            return writeIndex;
        }

        /// <summary>
        /// Creates a new managed array containing all pipeline-local stage occurrence indices matching the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToIndexArray(AtlasStageId stageId)
        {
            var indices = new int[CountOf(stageId)];
            CopyIndices(stageId, indices);
            return indices;
        }

        /// <summary>
        /// Creates a new managed array containing all pipeline-local stage occurrence indices matching the supplied stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to search for.</param>
        /// <returns>A new index array in authored order.</returns>
        public int[] ToStageIndexArray(FixedString64Bytes stageName)
        {
            var indices = new int[CountOfStage(stageName)];
            CopyStageIndices(stageName, indices);
            return indices;
        }

        /// <summary>
        /// Copies stage occurrences from this pipeline into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving stage definitions in pipeline order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is smaller than <see cref="Count"/>.
        /// </exception>
        public void CopyTo(AtlasStageDefinition[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _stages.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than pipeline stage count '{_stages.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_stages, destination, _stages.Length);
        }

        /// <summary>
        /// Copies a pipeline-local stage occurrence range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source index in this pipeline.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of stage occurrences to copy.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a range argument is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the source or destination range is invalid.
        /// </exception>
        public void CopyTo(
            int sourceIndex,
            AtlasStageDefinition[] destination,
            int destinationIndex,
            int length)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Source index must be non-negative.");
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), destinationIndex, "Destination index must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
            }

            if (sourceIndex + length > _stages.Length)
            {
                throw new ArgumentException(
                    "Source range exceeds Pipeline definition bounds.",
                    nameof(length));
            }

            if (destinationIndex + length > destination.Length)
            {
                throw new ArgumentException(
                    "Destination range exceeds destination array bounds.",
                    nameof(length));
            }

            Array.Copy(_stages, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Creates a managed copy of this pipeline's stage occurrences.
        /// </summary>
        /// <returns>A new stage-definition array in pipeline order.</returns>
        public AtlasStageDefinition[] ToArray()
        {
            var copy = new AtlasStageDefinition[_stages.Length];
            Array.Copy(_stages, copy, _stages.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over pipeline-local stage occurrences in authored order.
        /// </summary>
        /// <returns>An enumerator over stage definitions.</returns>
        public IEnumerator<AtlasStageDefinition> GetEnumerator()
        {
            for (var i = 0; i < _stages.Length; i++)
            {
                yield return _stages[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over pipeline-local stage occurrences in authored order.
        /// </summary>
        /// <returns>An enumerator over stage definitions.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this pipeline definition.
        /// </summary>
        /// <returns>A string containing the pipeline name, pipeline id, stage count, and operation count.</returns>
        public override string ToString()
        {
            return $"AtlasPipelineDefinition(Name={GetDiagnosticName(DebugName)}, PipelineId={PipelineId}, StageCount={Count}, OperationCount={OperationCount})";
        }

        private static void ValidateHeaderOrThrow(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName)
        {
            pipelineId.ValidateOrThrow(nameof(pipelineId));

            if (!debugName.IsEmpty)
            {
                return;
            }

            throw new ArgumentException(
                "Atlas Pipeline definition must have a non-empty debug name.",
                nameof(debugName));
        }

        private static AtlasStageDefinition[] CopyAndValidateStages(
            FixedString64Bytes pipelineName,
            AtlasStageDefinition[] stages)
        {
            if (stages == null)
            {
                throw new ArgumentNullException(nameof(stages));
            }

            if (stages.Length == 0)
            {
                throw new ArgumentException(
                    $"Atlas Pipeline definition '{GetDiagnosticName(pipelineName)}' must contain at least one stage occurrence.",
                    nameof(stages));
            }

            var copy = new AtlasStageDefinition[stages.Length];

            for (var i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];

                ValidateStageOrThrow(
                    pipelineName,
                    stage,
                    i);

                copy[i] = stage;
            }

            return copy;
        }

        private static void ValidateStageOrThrow(
            FixedString64Bytes pipelineName,
            AtlasStageDefinition stage,
            int index)
        {
            if (stage == null)
            {
                throw new ArgumentException(
                    $"Atlas Pipeline definition '{GetDiagnosticName(pipelineName)}' contains a null stage occurrence at index '{index}'.");
            }

            stage.StageId.ValidateOrThrow($"stages[{index}].StageId");

            if (stage.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas Pipeline definition '{GetDiagnosticName(pipelineName)}' contains a stage occurrence with an empty debug name at index '{index}'.");
            }

            if (stage.IsEmpty)
            {
                throw new ArgumentException(
                    $"Atlas Pipeline definition '{GetDiagnosticName(pipelineName)}' contains stage occurrence '{stage.DebugName}' with no operation occurrences.");
            }
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_stages.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Pipeline stage index must be between 0 and {_stages.Length - 1}.");
        }

        private static string GetDiagnosticName(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? "<unnamed>"
                : name.ToString();
        }
    }
}