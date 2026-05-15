// Packages/com.lokrain.atlas/Runtime/Execution/AtlasExecutionContext.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Bind one compiled Atlas plan to one compatible workspace.
// - Provide executor-facing access from compiled bindings to workspace-owned memory.
// - Keep FieldId/slot resolution out of jobs.
// - Keep operation execution separate from compilation, memory allocation, artifacts, and debug rendering.
//
// Design notes
// - This context does not own native memory.
// - Disposing the workspace remains the caller's responsibility.
// - Executors may use compiled operation bindings to obtain typed NativeArray/NativeSlice views.
// - Jobs should receive those typed views, not this managed context.
// - Shape-only bindings should use shape access, not memory access.
// - Missing optional bindings are represented explicitly and must not be treated as default slots.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Non-owning execution context binding a compiled plan to workspace-owned memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasExecutionContext"/> is the managed executor-facing boundary after
    /// compilation and workspace allocation. It validates that the supplied
    /// <see cref="AtlasCompiledPlan"/> and <see cref="AtlasWorkspace"/> agree on the same field
    /// contract shape, then exposes accessors from <see cref="AtlasCompiledBinding"/> to
    /// workspace-owned blocks and typed native views.
    /// </para>
    ///
    /// <para>
    /// This type does not allocate or dispose native memory. It does not schedule jobs by itself.
    /// Concrete operation executors should use this context to resolve compiled bindings into
    /// typed views, then pass those views into Burst-compatible jobs.
    /// </para>
    /// </remarks>
    public sealed class AtlasExecutionContext
    {
        /// <summary>
        /// Compiled plan being executed.
        /// </summary>
        public readonly AtlasCompiledPlan Plan;

        /// <summary>
        /// Workspace providing native memory for the compiled plan.
        /// </summary>
        public readonly AtlasWorkspace Workspace;

        private AtlasExecutionContext(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace)
        {
            ValidateCompatibleOrThrow(
                plan,
                workspace);

            Plan = plan;
            Workspace = workspace;
        }

        /// <summary>
        /// Gets the Contract table used by the compiled plan.
        /// </summary>
        public AtlasContractTable Contracts => Plan.Contracts;

        /// <summary>
        /// Gets the resolved workspace shape set.
        /// </summary>
        public AtlasResolvedShapeSet Shapes => Workspace.Shapes;

        /// <summary>
        /// Gets the number of compiled stage occurrences.
        /// </summary>
        public int StageCount => Plan.StageCount;

        /// <summary>
        /// Gets the flattened operation occurrence count.
        /// </summary>
        public int OperationCount => Plan.OperationCount;

        /// <summary>
        /// Gets the total compiled binding count.
        /// </summary>
        public int BindingCount => Plan.BindingCount;

        /// <summary>
        /// Gets the number of workspace field blocks.
        /// </summary>
        public int FieldCount => Workspace.Count;

        /// <summary>
        /// Gets the total workspace logical byte length.
        /// </summary>
        public long TotalByteLength => Workspace.TotalByteLength;

        /// <summary>
        /// Gets the total workspace byte capacity.
        /// </summary>
        public long TotalByteCapacity => Workspace.TotalByteCapacity;

        /// <summary>
        /// Creates a non-owning execution context for an already compiled plan and allocated workspace.
        /// </summary>
        /// <param name="plan">Compiled plan.</param>
        /// <param name="workspace">Compatible workspace.</param>
        /// <returns>A validated execution context.</returns>
        public static AtlasExecutionContext Create(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace)
        {
            return new AtlasExecutionContext(
                plan,
                workspace);
        }

        /// <summary>
        /// Creates a non-owning execution context from a successful compilation result and allocated workspace.
        /// </summary>
        /// <param name="compilation">Compilation result containing a successful compiled plan.</param>
        /// <param name="workspace">Compatible workspace.</param>
        /// <returns>A validated execution context.</returns>
        public static AtlasExecutionContext Create(
            AtlasCompilationResult compilation,
            AtlasWorkspace workspace)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return Create(
                compilation.GetRequiredPlan(),
                workspace);
        }

        /// <summary>
        /// Gets a compiled operation by flattened operation index.
        /// </summary>
        /// <param name="flattenedOperationIndex">Zero-based operation index across all stages.</param>
        /// <returns>The compiled operation.</returns>
        public AtlasCompiledOperation GetRequiredOperation(
            int flattenedOperationIndex)
        {
            return Plan.GetRequiredFlattenedOperation(flattenedOperationIndex);
        }

        /// <summary>
        /// Attempts to get a compiled operation by flattened operation index.
        /// </summary>
        /// <param name="flattenedOperationIndex">Zero-based operation index across all stages.</param>
        /// <param name="stageIndex">Resolved stage index when present; otherwise, -1.</param>
        /// <param name="operationIndex">Resolved stage-local operation index when present; otherwise, -1.</param>
        /// <param name="operation">Resolved operation when present; otherwise, null.</param>
        /// <returns><c>true</c> when the operation exists.</returns>
        public bool TryGetOperation(
            int flattenedOperationIndex,
            out int stageIndex,
            out int operationIndex,
            out AtlasCompiledOperation operation)
        {
            return Plan.TryGetFlattenedOperation(
                flattenedOperationIndex,
                out stageIndex,
                out operationIndex,
                out operation);
        }

        /// <summary>
        /// Gets the workspace block for a present compiled binding that requires content memory.
        /// </summary>
        /// <param name="binding">Compiled binding.</param>
        /// <returns>The matching workspace field memory block.</returns>
        public AtlasFieldMemoryBlock GetRequiredBlock(
            AtlasCompiledBinding binding)
        {
            ValidateContentBindingOrThrow(binding);

            var block = Workspace.GetRequiredBlock(binding.Contract.Slot);

            ValidateBlockMatchesBindingOrThrow(
                block,
                binding);

            return block;
        }

        /// <summary>
        /// Gets the workspace block for a compiled operation binding by binding index.
        /// </summary>
        /// <param name="operation">Compiled operation.</param>
        /// <param name="bindingIndex">Operation-local binding index.</param>
        /// <returns>The matching workspace field memory block.</returns>
        public AtlasFieldMemoryBlock GetRequiredBlock(
            AtlasCompiledOperation operation,
            int bindingIndex)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetRequiredBlock(operation[bindingIndex]);
        }

        /// <summary>
        /// Attempts to get the workspace block for a compiled binding.
        /// </summary>
        /// <param name="binding">Compiled binding.</param>
        /// <param name="block">Resolved block on success; otherwise, null.</param>
        /// <returns>
        /// <c>true</c> when the binding is present and requires content memory; otherwise,
        /// <c>false</c> for missing optional or shape-only bindings.
        /// </returns>
        public bool TryGetBlock(
            AtlasCompiledBinding binding,
            out AtlasFieldMemoryBlock block)
        {
            binding.ValidateOrThrow(nameof(binding));

            if (!binding.IsPresent ||
                !binding.RequiresContentMemory)
            {
                block = null;
                return false;
            }

            if (!Workspace.TryGetBlock(binding.Contract.Slot, out block))
            {
                block = null;
                return false;
            }

            ValidateBlockMatchesBindingOrThrow(
                block,
                binding);

            return true;
        }

        /// <summary>
        /// Gets the resolved shape for a present compiled binding.
        /// </summary>
        /// <param name="binding">Compiled binding.</param>
        /// <returns>The matching resolved shape.</returns>
        public AtlasResolvedShape GetRequiredShape(
            AtlasCompiledBinding binding)
        {
            ValidatePresentBindingOrThrow(binding);

            var shape = Shapes.GetRequiredShape(binding.Contract.Slot);

            if (shape.StableId != binding.Contract.StableId)
            {
                throw new InvalidOperationException(
                    $"Atlas execution context shape mismatch for binding '{binding.BindingName}'. Shape stable id '{shape.StableId}' does not match binding field id '{binding.Contract.StableId}'.");
            }

            return shape;
        }

        /// <summary>
        /// Gets the resolved shape for a compiled operation binding by binding index.
        /// </summary>
        /// <param name="operation">Compiled operation.</param>
        /// <param name="bindingIndex">Operation-local binding index.</param>
        /// <returns>The matching resolved shape.</returns>
        public AtlasResolvedShape GetRequiredShape(
            AtlasCompiledOperation operation,
            int bindingIndex)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetRequiredShape(operation[bindingIndex]);
        }

        /// <summary>
        /// Attempts to get the resolved shape for a compiled binding.
        /// </summary>
        /// <param name="binding">Compiled binding.</param>
        /// <param name="shape">Resolved shape on success; otherwise, default payload.</param>
        /// <returns><c>true</c> when the binding is present; otherwise, <c>false</c>.</returns>
        public bool TryGetShape(
            AtlasCompiledBinding binding,
            out AtlasResolvedShape shape)
        {
            binding.ValidateOrThrow(nameof(binding));

            if (!binding.IsPresent)
            {
                shape = default;
                return false;
            }

            if (!Shapes.TryGetShape(binding.Contract.Slot, out shape))
            {
                shape = default;
                return false;
            }

            if (shape.StableId != binding.Contract.StableId)
            {
                throw new InvalidOperationException(
                    $"Atlas execution context shape mismatch for binding '{binding.BindingName}'. Shape stable id '{shape.StableId}' does not match binding field id '{binding.Contract.StableId}'.");
            }

            return true;
        }

        /// <summary>
        /// Gets a typed capacity view for a present content binding.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="binding">Compiled binding.</param>
        /// <returns>A NativeArray view over the full resolved field capacity.</returns>
        public NativeArray<TElement> GetTypedCapacityArray<TElement>(
            AtlasCompiledBinding binding)
            where TElement : unmanaged
        {
            return GetRequiredBlock(binding)
                .GetTypedCapacityArray<TElement>();
        }

        /// <summary>
        /// Gets a typed capacity view for a compiled operation binding by binding index.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="operation">Compiled operation.</param>
        /// <param name="bindingIndex">Operation-local binding index.</param>
        /// <returns>A NativeArray view over the full resolved field capacity.</returns>
        public NativeArray<TElement> GetTypedCapacityArray<TElement>(
            AtlasCompiledOperation operation,
            int bindingIndex)
            where TElement : unmanaged
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetTypedCapacityArray<TElement>(operation[bindingIndex]);
        }

        /// <summary>
        /// Gets a typed logical-length view for a present content binding.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="binding">Compiled binding.</param>
        /// <returns>A NativeSlice view over the resolved logical field length.</returns>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            AtlasCompiledBinding binding)
            where TElement : unmanaged
        {
            return GetRequiredBlock(binding)
                .GetTypedLengthSlice<TElement>();
        }

        /// <summary>
        /// Gets a typed logical-length view for a compiled operation binding by binding index.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <param name="operation">Compiled operation.</param>
        /// <param name="bindingIndex">Operation-local binding index.</param>
        /// <returns>A NativeSlice view over the resolved logical field length.</returns>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            AtlasCompiledOperation operation,
            int bindingIndex)
            where TElement : unmanaged
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetTypedLengthSlice<TElement>(operation[bindingIndex]);
        }

        /// <summary>
        /// Gets a required field memory block by stable field identity.
        /// </summary>
        /// <param name="stableId">Stable field id.</param>
        /// <returns>The matching workspace field memory block.</returns>
        /// <remarks>
        /// Prefer compiled binding access inside operation executors. This method exists for
        /// orchestration, diagnostics, artifact writing, and debug-map export boundaries.
        /// </remarks>
        public AtlasFieldMemoryBlock GetRequiredBlock(
            StableDataId stableId)
        {
            return Workspace.GetRequiredBlock(stableId);
        }

        /// <summary>
        /// Gets a required field memory block by typed field declaration.
        /// </summary>
        /// <typeparam name="TField">Typed field declaration.</typeparam>
        /// <typeparam name="TElement">Expected unmanaged field element type.</typeparam>
        /// <returns>The matching workspace field memory block.</returns>
        /// <remarks>
        /// Prefer compiled binding access inside operation executors. This method exists for
        /// orchestration, diagnostics, artifact writing, and debug-map export boundaries.
        /// </remarks>
        public AtlasFieldMemoryBlock GetRequiredBlock<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Workspace.GetRequiredBlock<TField, TElement>();
        }

        /// <summary>
        /// Returns a stable diagnostic name for this execution context.
        /// </summary>
        /// <returns>The compiled pipeline debug name when present; otherwise, the workspace name.</returns>
        public string GetDiagnosticName()
        {
            if (!Plan.DebugName.IsEmpty)
            {
                return Plan.DebugName.ToString();
            }

            return Workspace.GetDiagnosticName();
        }

        /// <summary>
        /// Returns a diagnostic representation of this context.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            return
                $"AtlasExecutionContext(Name={GetDiagnosticName()}, Stages={StageCount}, Operations={OperationCount}, Bindings={BindingCount}, Fields={FieldCount}, ByteCapacity={TotalByteCapacity})";
        }

        private static void ValidateCompatibleOrThrow(
            AtlasCompiledPlan plan,
            AtlasWorkspace workspace)
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

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            workspace.ThrowIfDisposed();

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

            ValidateCompatibleContractTablesOrThrow(
                plan.Contracts,
                workspace.Contracts);

            ValidateWorkspaceShapesMatchPlanOrThrow(
                plan.Contracts,
                workspace.Shapes);
        }

        private static void ValidateCompatibleContractTablesOrThrow(
            AtlasContractTable planContracts,
            AtlasContractTable workspaceContracts)
        {
            if (planContracts.Count != workspaceContracts.Count)
            {
                throw new ArgumentException(
                    $"Compiled plan Contract table contains '{planContracts.Count}' fields, but workspace Contract table contains '{workspaceContracts.Count}' fields.");
            }

            for (var i = 0; i < planContracts.Count; i++)
            {
                var planContract = planContracts[i];
                var workspaceContract = workspaceContracts[i];

                if (planContract != workspaceContract)
                {
                    throw new ArgumentException(
                        $"Compiled plan Contract table does not match workspace Contract table at slot '{i}'. Plan field '{planContract.GetDiagnosticName()}' does not match workspace field '{workspaceContract.GetDiagnosticName()}'.");
                }
            }
        }

        private static void ValidateWorkspaceShapesMatchPlanOrThrow(
            AtlasContractTable planContracts,
            AtlasResolvedShapeSet shapes)
        {
            if (shapes.Count != planContracts.Count)
            {
                throw new ArgumentException(
                    $"Workspace shape set contains '{shapes.Count}' shapes, but compiled plan Contract table contains '{planContracts.Count}' contracts.");
            }

            for (var i = 0; i < shapes.Count; i++)
            {
                var contract = planContracts[i];
                var shape = shapes[i];

                shape.ValidateOrThrow($"shapes[{i}]");

                if (shape.StableId != contract.StableId ||
                    shape.Slot != contract.Slot ||
                    shape.Role != contract.Role ||
                    shape.StorageFormat != contract.StorageFormat ||
                    shape.DeclaredShape != contract.LengthShape)
                {
                    throw new ArgumentException(
                        $"Workspace shape at slot '{i}' does not match compiled plan Contract field '{contract.GetDiagnosticName()}'.");
                }
            }
        }

        private static void ValidatePresentBindingOrThrow(
            AtlasCompiledBinding binding)
        {
            binding.ValidateOrThrow(nameof(binding));

            if (!binding.IsPresent)
            {
                throw new InvalidOperationException(
                    $"Atlas compiled binding '{binding.BindingName}' is missing and cannot be resolved against workspace memory or shape metadata.");
            }
        }

        private static void ValidateContentBindingOrThrow(
            AtlasCompiledBinding binding)
        {
            ValidatePresentBindingOrThrow(binding);

            if (!binding.RequiresContentMemory)
            {
                throw new InvalidOperationException(
                    $"Atlas compiled binding '{binding.BindingName}' is shape-only and must not request workspace content memory.");
            }
        }

        private static void ValidateBlockMatchesBindingOrThrow(
            AtlasFieldMemoryBlock block,
            AtlasCompiledBinding binding)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (block.StableId != binding.Contract.StableId)
            {
                throw new InvalidOperationException(
                    $"Atlas workspace block '{block.GetDiagnosticName()}' does not match compiled binding '{binding.BindingName}'. Block field id '{block.StableId}' differs from binding field id '{binding.Contract.StableId}'.");
            }

            if (block.Slot != binding.Contract.Slot)
            {
                throw new InvalidOperationException(
                    $"Atlas workspace block '{block.GetDiagnosticName()}' does not match compiled binding '{binding.BindingName}'. Block slot '{block.Slot}' differs from binding slot '{binding.Contract.Slot}'.");
            }

            if (block.StorageFormat != binding.Contract.StorageFormat)
            {
                throw new InvalidOperationException(
                    $"Atlas workspace block '{block.GetDiagnosticName()}' does not match compiled binding '{binding.BindingName}'. Block storage format differs from binding storage format.");
            }
        }
    }
}