// Packages/com.lokrain.atlas/Runtime/Execution/AtlasExecutionContext.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Bind one compiled Atlas plan to one compatible layout-owned workspace.
// - Provide executor-facing access from compiled bindings to workspace layout entries and slices.
// - Keep FieldId/slot resolution out of jobs.
// - Keep operation execution separate from compilation, memory allocation, artifacts, and debug rendering.
//
// Design notes
// - This context does not own native memory.
// - Disposing the workspace remains the caller's responsibility.
// - Executors use compiled operation bindings to obtain AtlasFieldAddress values and typed NativeSlice views.
// - Jobs should receive those typed views or numeric addresses, not this managed context.
// - Shape-only bindings should use layout/shape metadata access, not memory access.
// - Missing optional bindings are represented explicitly and must not be treated as default slots.
// - This context no longer exposes AtlasFieldMemoryBlock because fields are packed into workspace
//   storage blocks and addressed by AtlasFieldAddress.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Workspaces;
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
    /// <see cref="AtlasCompiledPlan"/> and <see cref="AtlasWorkspace"/> agree on field identity,
    /// storage format, shape domain, and length-shape metadata.
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
        /// Gets the compiled workspace layout.
        /// </summary>
        public AtlasWorkspaceLayout Layout => Workspace.Layout;

        /// <summary>
        /// Gets a resolved shape set reconstructed from the compiled plan and workspace layout.
        /// </summary>
        /// <remarks>
        /// This property is a compatibility bridge for artifact/header code. It allocates managed
        /// metadata and should not be used in hot execution paths.
        /// </remarks>
        public AtlasResolvedShapeSet Shapes => CreateResolvedShapeSet();

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
        /// Gets the number of workspace field entries.
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
        public AtlasCompiledOperation GetRequiredOperation(int flattenedOperationIndex)
        {
            return Plan.GetRequiredFlattenedOperation(flattenedOperationIndex);
        }

        /// <summary>
        /// Attempts to get a compiled operation by flattened operation index.
        /// </summary>
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
        /// Gets the layout entry for a present compiled binding.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(AtlasCompiledBinding binding)
        {
            ValidatePresentBindingOrThrow(binding);

            var entry = Workspace.GetRequiredEntry(binding.Contract.Slot);

            ValidateEntryMatchesBindingOrThrow(
                entry,
                binding);

            return entry;
        }

        /// <summary>
        /// Gets the layout entry for a compiled operation binding by binding index.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(
            AtlasCompiledOperation operation,
            int bindingIndex)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetRequiredEntry(operation[bindingIndex]);
        }

        /// <summary>
        /// Attempts to get the layout entry for a compiled binding.
        /// </summary>
        public bool TryGetEntry(
            AtlasCompiledBinding binding,
            out AtlasWorkspaceLayoutEntry entry)
        {
            binding.ValidateOrThrow(nameof(binding));

            if (!binding.IsPresent)
            {
                entry = default;
                return false;
            }

            if (!Workspace.TryGetEntry(binding.Contract.Slot, out entry))
            {
                entry = default;
                return false;
            }

            ValidateEntryMatchesBindingOrThrow(
                entry,
                binding);

            return true;
        }

        /// <summary>
        /// Gets the physical field address for a present compiled binding.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress(AtlasCompiledBinding binding)
        {
            return GetRequiredEntry(binding).Address;
        }

        /// <summary>
        /// Attempts to get the physical field address for a compiled binding.
        /// </summary>
        public bool TryGetAddress(
            AtlasCompiledBinding binding,
            out AtlasFieldAddress address)
        {
            if (TryGetEntry(binding, out var entry))
            {
                address = entry.Address;
                return true;
            }

            address = default;
            return false;
        }

        /// <summary>
        /// Gets the resolved shape metadata for a present compiled binding.
        /// </summary>
        public AtlasResolvedShape GetRequiredShape(AtlasCompiledBinding binding)
        {
            var entry = GetRequiredEntry(binding);

            return CreateShapeFromEntry(
                binding.Contract,
                entry);
        }

        /// <summary>
        /// Gets the resolved shape metadata for a compiled operation binding by binding index.
        /// </summary>
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
        /// Attempts to get the resolved shape metadata for a compiled binding.
        /// </summary>
        public bool TryGetShape(
            AtlasCompiledBinding binding,
            out AtlasResolvedShape shape)
        {
            if (TryGetEntry(binding, out var entry))
            {
                shape = CreateShapeFromEntry(
                    binding.Contract,
                    entry);
                return true;
            }

            shape = default;
            return false;
        }

        /// <summary>
        /// Gets the allocated byte-capacity slice for a present content binding.
        /// </summary>
        public NativeSlice<byte> GetFieldByteCapacitySlice(AtlasCompiledBinding binding)
        {
            ValidateContentBindingOrThrow(binding);

            var entry = GetRequiredEntry(binding);

            return Workspace.GetFieldByteCapacitySlice(entry.Slot);
        }

        /// <summary>
        /// Gets the allocated byte-capacity slice for a compiled operation binding by binding index.
        /// </summary>
        public NativeSlice<byte> GetFieldByteCapacitySlice(
            AtlasCompiledOperation operation,
            int bindingIndex)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetFieldByteCapacitySlice(operation[bindingIndex]);
        }

        /// <summary>
        /// Gets the logical byte-length slice for a present content binding.
        /// </summary>
        public NativeSlice<byte> GetFieldByteLengthSlice(AtlasCompiledBinding binding)
        {
            ValidateContentBindingOrThrow(binding);

            var entry = GetRequiredEntry(binding);

            return Workspace.GetFieldByteLengthSlice(entry.Slot);
        }

        /// <summary>
        /// Gets a typed capacity slice for a present content binding.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacitySlice<TElement>(
            AtlasCompiledBinding binding)
            where TElement : unmanaged
        {
            ValidateContentBindingOrThrow(binding);

            var entry = GetRequiredEntry(binding);

            return Workspace.GetTypedCapacitySlice<TElement>(entry.Slot);
        }

        /// <summary>
        /// Compatibility alias for <see cref="GetTypedCapacitySlice{TElement}(AtlasCompiledBinding)"/>.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacityArray<TElement>(
            AtlasCompiledBinding binding)
            where TElement : unmanaged
        {
            return GetTypedCapacitySlice<TElement>(binding);
        }

        /// <summary>
        /// Gets a typed capacity slice for a compiled operation binding by binding index.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacitySlice<TElement>(
            AtlasCompiledOperation operation,
            int bindingIndex)
            where TElement : unmanaged
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return GetTypedCapacitySlice<TElement>(operation[bindingIndex]);
        }

        /// <summary>
        /// Compatibility alias for <see cref="GetTypedCapacitySlice{TElement}(AtlasCompiledOperation, int)"/>.
        /// </summary>
        public NativeSlice<TElement> GetTypedCapacityArray<TElement>(
            AtlasCompiledOperation operation,
            int bindingIndex)
            where TElement : unmanaged
        {
            return GetTypedCapacitySlice<TElement>(
                operation,
                bindingIndex);
        }

        /// <summary>
        /// Gets a typed logical-length slice for a present content binding.
        /// </summary>
        public NativeSlice<TElement> GetTypedLengthSlice<TElement>(
            AtlasCompiledBinding binding)
            where TElement : unmanaged
        {
            ValidateContentBindingOrThrow(binding);

            var entry = GetRequiredEntry(binding);

            return Workspace.GetTypedLengthSlice<TElement>(entry.Slot);
        }

        /// <summary>
        /// Gets a typed logical-length slice for a compiled operation binding by binding index.
        /// </summary>
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
        /// Gets a required layout entry by stable field identity.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(StableDataId stableId)
        {
            return Workspace.GetRequiredEntry(stableId);
        }

        /// <summary>
        /// Gets a required layout entry by typed field declaration.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Workspace.GetRequiredEntry<TField, TElement>();
        }

        /// <summary>
        /// Reconstructs resolved shape metadata from the compiled plan and workspace layout.
        /// </summary>
        public AtlasResolvedShapeSet CreateResolvedShapeSet()
        {
            var shapes = new AtlasResolvedShape[Workspace.Count];

            for (var i = 0; i < Workspace.Count; i++)
            {
                shapes[i] = CreateShapeFromEntry(
                    Plan.Contracts[i],
                    Workspace[i]);
            }

            return AtlasResolvedShapeSet.Create(
                Plan.DebugName,
                Plan.Contracts,
                shapes);
        }

        /// <summary>
        /// Returns a stable diagnostic name for this execution context.
        /// </summary>
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
            workspace.Layout.ValidateOrThrow();

            if (plan.Contracts.Count != workspace.Count)
            {
                throw new ArgumentException(
                    $"Compiled plan Contract table contains '{plan.Contracts.Count}' fields, but workspace layout contains '{workspace.Count}' entries.");
            }

            for (var i = 0; i < plan.Contracts.Count; i++)
            {
                ValidateEntryMatchesContractOrThrow(
                    workspace[i],
                    plan.Contracts[i],
                    i);
            }
        }

        private static void ValidatePresentBindingOrThrow(AtlasCompiledBinding binding)
        {
            binding.ValidateOrThrow(nameof(binding));

            if (!binding.IsPresent)
            {
                throw new InvalidOperationException(
                    $"Atlas compiled binding '{binding.BindingName}' is missing and cannot be resolved against workspace memory or shape metadata.");
            }
        }

        private static void ValidateContentBindingOrThrow(AtlasCompiledBinding binding)
        {
            ValidatePresentBindingOrThrow(binding);

            if (!binding.RequiresContentMemory)
            {
                throw new InvalidOperationException(
                    $"Atlas compiled binding '{binding.BindingName}' is shape-only and must not request workspace content memory.");
            }
        }

        private static AtlasResolvedShape CreateShapeFromEntry(
            AtlasContract contract,
            AtlasWorkspaceLayoutEntry entry)
        {
            ValidateEntryMatchesContractOrThrow(
                entry,
                contract,
                entry.Slot.Index);

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

        private static void ValidateEntryMatchesBindingOrThrow(
            AtlasWorkspaceLayoutEntry entry,
            AtlasCompiledBinding binding)
        {
            ValidateEntryMatchesContractOrThrow(
                entry,
                binding.Contract,
                entry.Slot.Index);
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
                    $"Workspace layout entry at index '{index}' has slot '{entry.Slot}'.");
            }

            if (entry.StableId != contract.StableId)
            {
                throw new InvalidOperationException(
                    $"Workspace layout entry '{entry.GetDiagnosticName()}' does not match Contract '{contract.GetDiagnosticName()}'. Entry field id '{entry.StableId}' differs from Contract field id '{contract.StableId}'.");
            }

            if (entry.Slot != contract.Slot)
            {
                throw new InvalidOperationException(
                    $"Workspace layout entry '{entry.GetDiagnosticName()}' does not match Contract '{contract.GetDiagnosticName()}'. Entry slot '{entry.Slot}' differs from Contract slot '{contract.Slot}'.");
            }

            if (entry.Role != contract.Role)
            {
                throw new InvalidOperationException(
                    $"Workspace layout entry '{entry.GetDiagnosticName()}' does not match Contract '{contract.GetDiagnosticName()}'. Entry role '{entry.Role}' differs from Contract role '{contract.Role}'.");
            }

            if (entry.StorageFormat != contract.StorageFormat)
            {
                throw new InvalidOperationException(
                    $"Workspace layout entry '{entry.GetDiagnosticName()}' does not match Contract '{contract.GetDiagnosticName()}'. Entry storage format differs from Contract storage format.");
            }

            if (entry.ShapeDomain != contract.ShapeDomain)
            {
                throw new InvalidOperationException(
                    $"Workspace layout entry '{entry.GetDiagnosticName()}' does not match Contract '{contract.GetDiagnosticName()}'. Entry shape domain differs from Contract shape domain.");
            }

            if (entry.DeclaredShape != contract.LengthShape)
            {
                throw new InvalidOperationException(
                    $"Workspace layout entry '{entry.GetDiagnosticName()}' does not match Contract '{contract.GetDiagnosticName()}'. Entry declared shape differs from Contract length shape.");
            }
        }
    }
}
