// Runtime/Compilation/AtlasCompiledPlan.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one complete compiled Atlas pipeline contract.
// - Preserve pipeline-local stage order, stage-local operation order, and operation-local binding order.
// - Bind symbolic pipeline/stage/operation Field access to a concrete Atlas Contract table.
// - Provide the canonical metadata boundary before executable scheduler planning and workspace memory resolution.
//
// Design notes
// - This is compilation metadata, not runtime job payload.
// - Repeated stage definitions are valid at this layer; occurrence index is therefore essential.
// - Repeated operation definitions are valid inside stages; operation occurrence index remains meaningful.
// - This type deliberately uses immutable arrays and linear scans instead of dictionaries.
// - Jobs must not receive this type.
// - Runtime execution should consume lower-level scheduler payloads derived from this plan.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Compiled representation of one complete Atlas pipeline definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A compiled plan is the durable metadata result of resolving a pipeline definition against
    /// a Field Contract table. It preserves authored pipeline order exactly and records resolved
    /// Field bindings for every operation access declaration.
    /// </para>
    ///
    /// <para>
    /// This type does not own native memory, does not schedule jobs, and does not contain concrete
    /// scheduler delegates. The next layer should transform this plan into executable scheduler
    /// entries and workspace memory bindings.
    /// </para>
    ///
    /// <para>
    /// The plan intentionally keeps the Contract table reference. The Contract table is the meaning
    /// catalog used for resolution, while the compiled stages and operations preserve occurrence
    /// structure and resolved binding metadata.
    /// </para>
    /// </remarks>
    public sealed class AtlasCompiledPlan :
        IReadOnlyList<AtlasCompiledStage>
    {
        private const int InvalidStageIndex = -1;
        private const int InvalidFlattenedOperationIndex = -1;

        private readonly AtlasCompiledStage[] _stages;

        /// <summary>
        /// Stable, versioned identity of the source pipeline contract.
        /// </summary>
        public readonly AtlasPipelineId PipelineId;

        /// <summary>
        /// Stable diagnostic pipeline name from the source pipeline definition.
        /// </summary>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Contract table used to resolve this plan.
        /// </summary>
        /// <remarks>
        /// The Contract table owns Field meaning. The compiled plan owns resolved pipeline
        /// structure over that table.
        /// </remarks>
        public readonly AtlasContractTable Contracts;

        private AtlasCompiledPlan(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName,
            AtlasContractTable contracts,
            AtlasCompiledStage[] stages)
        {
            PipelineId = pipelineId;
            DebugName = debugName;
            Contracts = contracts;

            _stages = CopyAndValidateStages(
                pipelineId,
                debugName,
                contracts,
                stages);
        }

        /// <summary>
        /// Gets the number of compiled stage occurrences in this plan.
        /// </summary>
        public int Count => _stages.Length;

        /// <summary>
        /// Gets whether this plan contains no compiled stage occurrences.
        /// </summary>
        /// <remarks>
        /// Concrete compiled plans are required to contain at least one stage occurrence, so this
        /// property normally returns <c>false</c>.
        /// </remarks>
        public bool IsEmpty => _stages.Length == 0;

        /// <summary>
        /// Gets the number of compiled stage occurrences in this plan.
        /// </summary>
        public int StageCount => _stages.Length;

        /// <summary>
        /// Gets the total number of compiled operation occurrences across all stages.
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
        /// Gets the total number of compiled bindings across all operation occurrences.
        /// </summary>
        public int BindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _stages.Length; i++)
                {
                    count += _stages[i].BindingCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of bindings that resolved to concrete Field Contracts.
        /// </summary>
        public int PresentBindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _stages.Length; i++)
                {
                    count += _stages[i].PresentBindingCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of absent optional bindings.
        /// </summary>
        public int MissingOptionalBindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _stages.Length; i++)
                {
                    count += _stages[i].MissingOptionalBindingCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets whether at least one present binding reads Field contents.
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
        /// Gets whether at least one present binding writes, appends, consumes, or mutates Field contents.
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
        /// Gets whether at least one source binding declares a content read, regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentRead
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].DeclaresContentRead)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one source binding declares a content write, regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentWrite
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].DeclaresContentWrite)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this plan contains at least one optional binding.
        /// </summary>
        public bool HasOptionalBinding
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].HasOptionalBinding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this plan contains at least one missing optional binding.
        /// </summary>
        public bool HasMissingOptionalBinding
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].HasMissingOptionalBinding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this plan contains at least one shape-only binding.
        /// </summary>
        public bool HasShapeOnlyBinding
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].HasShapeOnlyBinding)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this plan has at least one present binding that requires content memory.
        /// </summary>
        public bool RequiresContentMemory
        {
            get
            {
                for (var i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].RequiresContentMemory)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the compiled stage at a zero-based pipeline-local stage occurrence index.
        /// </summary>
        /// <param name="index">Zero-based pipeline-local stage occurrence index.</param>
        /// <returns>The compiled stage at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this plan's stage range.
        /// </exception>
        public AtlasCompiledStage this[int index]
        {
            get
            {
                ThrowIfStageIndexOutOfRange(index);
                return _stages[index];
            }
        }

        /// <summary>
        /// Compiles a complete pipeline definition against a Contract table.
        /// </summary>
        /// <param name="pipeline">Source pipeline definition.</param>
        /// <param name="contracts">Contract table used for Field resolution.</param>
        /// <returns>A compiled plan with resolved stage, operation, and binding metadata.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="pipeline"/> or <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the pipeline cannot be compiled against the supplied Contract table.
        /// </exception>
        public static AtlasCompiledPlan Compile(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            ValidatePlanHeaderOrThrow(
                pipeline.PipelineId,
                pipeline.DebugName,
                contracts,
                pipeline.Count);

            var stages = new AtlasCompiledStage[pipeline.Count];

            for (var i = 0; i < pipeline.Count; i++)
            {
                stages[i] = AtlasCompiledStage.Compile(
                    i,
                    pipeline[i],
                    contracts);
            }

            return new AtlasCompiledPlan(
                pipeline.PipelineId,
                pipeline.DebugName,
                contracts,
                stages);
        }

        /// <summary>
        /// Creates a compiled plan from already compiled stages.
        /// </summary>
        /// <param name="pipelineId">Stable, versioned source pipeline identity.</param>
        /// <param name="debugName">Stable diagnostic pipeline name.</param>
        /// <param name="contracts">Contract table used for Field resolution.</param>
        /// <param name="stages">Compiled stages in pipeline-local stage order.</param>
        /// <returns>A validated compiled plan.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contracts"/> or <paramref name="stages"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the header or stages are invalid.
        /// </exception>
        public static AtlasCompiledPlan Create(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName,
            AtlasContractTable contracts,
            params AtlasCompiledStage[] stages)
        {
            return new AtlasCompiledPlan(
                pipelineId,
                debugName,
                contracts,
                stages);
        }

        /// <summary>
        /// Determines whether this compiled plan contains at least one occurrence of the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to search for.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool Contains(AtlasStageId stageId)
        {
            return IndexOfFirst(stageId) != InvalidStageIndex;
        }

        /// <summary>
        /// Determines whether this compiled plan contains at least one stage occurrence with the supplied debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to search for.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool ContainsStage(FixedString64Bytes stageName)
        {
            return IndexOfFirstStage(stageName) != InvalidStageIndex;
        }

        /// <summary>
        /// Returns the first pipeline-local stage occurrence index matching the supplied stage id.
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
        /// Returns the first pipeline-local stage occurrence index matching the supplied stage debug name.
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
        /// Resolves the first pipeline-local stage occurrence index for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <returns>The first matching stage occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageId"/> is invalid or absent from this plan.
        /// </exception>
        public int GetRequiredFirstIndex(AtlasStageId stageId)
        {
            var index = IndexOfFirst(stageId);

            if (index != InvalidStageIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Compiled Atlas plan '{DebugName}' does not contain stage id '{stageId}'.",
                nameof(stageId));
        }

        /// <summary>
        /// Resolves the first pipeline-local stage occurrence index for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <returns>The first matching stage occurrence index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageName"/> is empty or absent from this plan.
        /// </exception>
        public int GetRequiredFirstStageIndex(FixedString64Bytes stageName)
        {
            var index = IndexOfFirstStage(stageName);

            if (index != InvalidStageIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Compiled Atlas plan '{DebugName}' does not contain stage debug name '{stageName}'.",
                nameof(stageName));
        }

        /// <summary>
        /// Attempts to resolve the first pipeline-local stage occurrence index for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <param name="index">First matching stage index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstIndex(
            AtlasStageId stageId,
            out int index)
        {
            index = IndexOfFirst(stageId);
            return index != InvalidStageIndex;
        }

        /// <summary>
        /// Attempts to resolve the first pipeline-local stage occurrence index for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <param name="index">First matching stage index when present; otherwise <c>-1</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstStageIndex(
            FixedString64Bytes stageName,
            out int index)
        {
            index = IndexOfFirstStage(stageName);
            return index != InvalidStageIndex;
        }

        /// <summary>
        /// Attempts to resolve the first pipeline-local stage occurrence for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <param name="stage">First matching compiled stage when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstStage(
            AtlasStageId stageId,
            out AtlasCompiledStage stage)
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
        /// Attempts to resolve the first pipeline-local stage occurrence for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <param name="stage">First matching compiled stage when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when a matching stage occurrence exists; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstStage(
            FixedString64Bytes stageName,
            out AtlasCompiledStage stage)
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
        /// Resolves the first pipeline-local stage occurrence for a stage id.
        /// </summary>
        /// <param name="stageId">Stable, versioned stage id to resolve.</param>
        /// <returns>The first matching compiled stage.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageId"/> is invalid or absent from this plan.
        /// </exception>
        public AtlasCompiledStage GetRequiredFirstStage(AtlasStageId stageId)
        {
            return _stages[GetRequiredFirstIndex(stageId)];
        }

        /// <summary>
        /// Resolves the first pipeline-local stage occurrence for a stage debug name.
        /// </summary>
        /// <param name="stageName">Diagnostic stage name to resolve.</param>
        /// <returns>The first matching compiled stage.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stageName"/> is empty or absent from this plan.
        /// </exception>
        public AtlasCompiledStage GetRequiredFirstStage(FixedString64Bytes stageName)
        {
            return _stages[GetRequiredFirstStageIndex(stageName)];
        }

        /// <summary>
        /// Counts pipeline-local stage occurrences matching the supplied stage id.
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
        /// Counts pipeline-local stage occurrences matching the supplied stage debug name.
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
        /// Attempts to resolve a flattened operation occurrence by global operation index.
        /// </summary>
        /// <param name="flattenedOperationIndex">Zero-based operation index across all stages.</param>
        /// <param name="stageIndex">Resolved pipeline-local stage index when present; otherwise <c>-1</c>.</param>
        /// <param name="operationIndex">Resolved stage-local operation index when present; otherwise <c>-1</c>.</param>
        /// <param name="operation">Resolved compiled operation when present; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> when the flattened operation index was resolved; otherwise, <c>false</c>.</returns>
        public bool TryGetFlattenedOperation(
            int flattenedOperationIndex,
            out int stageIndex,
            out int operationIndex,
            out AtlasCompiledOperation operation)
        {
            if (flattenedOperationIndex < 0)
            {
                stageIndex = InvalidStageIndex;
                operationIndex = InvalidFlattenedOperationIndex;
                operation = null;
                return false;
            }

            var remaining = flattenedOperationIndex;

            for (var i = 0; i < _stages.Length; i++)
            {
                var stage = _stages[i];

                if (remaining < stage.Count)
                {
                    stageIndex = i;
                    operationIndex = remaining;
                    operation = stage[remaining];
                    return true;
                }

                remaining -= stage.Count;
            }

            stageIndex = InvalidStageIndex;
            operationIndex = InvalidFlattenedOperationIndex;
            operation = null;
            return false;
        }

        /// <summary>
        /// Resolves a flattened operation occurrence by global operation index.
        /// </summary>
        /// <param name="flattenedOperationIndex">Zero-based operation index across all stages.</param>
        /// <returns>The resolved compiled operation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="flattenedOperationIndex"/> is outside the flattened operation range.
        /// </exception>
        public AtlasCompiledOperation GetRequiredFlattenedOperation(int flattenedOperationIndex)
        {
            if (TryGetFlattenedOperation(
                    flattenedOperationIndex,
                    out _,
                    out _,
                    out var operation))
            {
                return operation;
            }

            throw new ArgumentOutOfRangeException(
                nameof(flattenedOperationIndex),
                flattenedOperationIndex,
                $"Flattened operation index must be between 0 and {OperationCount - 1}.");
        }

        /// <summary>
        /// Copies compiled stage occurrences into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving compiled stages in pipeline-local order.</param>
        public void CopyTo(AtlasCompiledStage[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _stages.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than compiled stage count '{_stages.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_stages, destination, _stages.Length);
        }

        /// <summary>
        /// Copies a compiled stage range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source stage index in this plan.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of stage occurrences to copy.</param>
        public void CopyTo(
            int sourceIndex,
            AtlasCompiledStage[] destination,
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
                    "Source range exceeds compiled plan bounds.",
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
        /// Creates a managed copy of this plan's compiled stage occurrences.
        /// </summary>
        /// <returns>A new compiled-stage array in pipeline-local stage order.</returns>
        public AtlasCompiledStage[] ToArray()
        {
            var copy = new AtlasCompiledStage[_stages.Length];
            Array.Copy(_stages, copy, _stages.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over compiled stage occurrences in pipeline-local order.
        /// </summary>
        /// <returns>An enumerator over compiled stages.</returns>
        public IEnumerator<AtlasCompiledStage> GetEnumerator()
        {
            for (var i = 0; i < _stages.Length; i++)
            {
                yield return _stages[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over compiled stage occurrences in pipeline-local order.
        /// </summary>
        /// <returns>An enumerator over compiled stages.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this compiled plan.
        /// </summary>
        /// <returns>A string containing pipeline name, id, stage count, operation count, and binding counts.</returns>
        public override string ToString()
        {
            return $"AtlasCompiledPlan(Name={DebugName}, Id={PipelineId}, Stages={StageCount}, Operations={OperationCount}, Bindings={BindingCount}, Present={PresentBindingCount}, MissingOptional={MissingOptionalBindingCount})";
        }

        private static AtlasCompiledStage[] CopyAndValidateStages(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName,
            AtlasContractTable contracts,
            AtlasCompiledStage[] stages)
        {
            ValidatePlanHeaderOrThrow(
                pipelineId,
                debugName,
                contracts,
                stages == null ? 0 : stages.Length);

            if (stages == null)
            {
                throw new ArgumentNullException(nameof(stages));
            }

            var copy = new AtlasCompiledStage[stages.Length];

            for (var i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];

                ValidateStageOrThrow(
                    debugName,
                    stage,
                    i);

                copy[i] = stage;
            }

            return copy;
        }

        private static void ValidatePlanHeaderOrThrow(
            AtlasPipelineId pipelineId,
            FixedString64Bytes debugName,
            AtlasContractTable contracts,
            int stageCount)
        {
            pipelineId.ValidateOrThrow(nameof(pipelineId));

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Compiled Atlas plan must have a non-empty debug name.",
                    nameof(debugName));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (contracts.Count == 0)
            {
                throw new ArgumentException(
                    $"Compiled Atlas plan '{debugName}' requires a non-empty Contract table.",
                    nameof(contracts));
            }

            if (stageCount <= 0)
            {
                throw new ArgumentException(
                    $"Compiled Atlas plan '{debugName}' must contain at least one stage occurrence.",
                    nameof(stageCount));
            }
        }

        private static void ValidateStageOrThrow(
            FixedString64Bytes pipelineName,
            AtlasCompiledStage stage,
            int index)
        {
            if (stage == null)
            {
                throw new ArgumentException(
                    $"Compiled Atlas plan '{pipelineName}' contains a null stage occurrence at index '{index}'.",
                    nameof(stage));
            }

            if (stage.StageIndex != index)
            {
                throw new ArgumentException(
                    $"Compiled Atlas plan '{pipelineName}' contains stage '{stage.DebugName}' with stage index '{stage.StageIndex}', but expected '{index}'.",
                    nameof(stage));
            }

            stage.StageId.ValidateOrThrow($"stages[{index}].StageId");

            if (stage.DebugName.IsEmpty)
            {
                throw new ArgumentException(
                    $"Compiled Atlas plan '{pipelineName}' contains a stage occurrence with an empty debug name at index '{index}'.",
                    nameof(stage));
            }

            if (stage.IsEmpty)
            {
                throw new ArgumentException(
                    $"Compiled Atlas plan '{pipelineName}' contains stage occurrence '{stage.DebugName}' with no compiled operation occurrences.",
                    nameof(stage));
            }
        }

        private void ThrowIfStageIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_stages.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Compiled stage index must be between 0 and {_stages.Length - 1}.");
        }
    }
}