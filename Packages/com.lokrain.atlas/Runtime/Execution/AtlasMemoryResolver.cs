// Packages/com.lokrain.atlas/Runtime/Execution/AtlasMemoryResolver.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Validate whether resolved Atlas field shapes can be compiled into a workspace layout.
// - Compile Contract-table or compiled-plan shapes into AtlasWorkspaceLayout.
// - Allocate workspace-owned native memory from AtlasWorkspaceLayout.
// - Keep allocation policy separate from operation scheduling, artifacts, and debug rendering.
//
// Design notes
// - The catalog owns meaning.
// - The compiler owns resolution.
// - The workspace owns memory.
// - Execution owns JobHandle flow.
// - Jobs own only numeric computation.
// - Artifacts own durable output.
// - This type is a migration facade over AtlasWorkspaceLayoutCompiler.
// - It does not schedule operations.
// - It does not expose FieldId lookup to jobs.
// - It does not write artifacts.
// - It does not render debug output.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Workspaces;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Creates workspace-owned native memory from validated Atlas compilation metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasMemoryResolver"/> is retained as a source-compatible migration facade.
    /// The canonical memory product is now <see cref="AtlasWorkspaceLayout"/>, produced by
    /// <see cref="AtlasWorkspaceLayoutCompiler"/> and consumed by <see cref="AtlasWorkspace"/>.
    /// </para>
    ///
    /// <para>
    /// New code should prefer the explicit path:
    /// resolve shapes, compile workspace layout, then allocate workspace from that layout.
    /// </para>
    /// </remarks>
    public static class AtlasMemoryResolver
    {
        /// <summary>
        /// Resolves field shapes from a compiled plan, compiles a workspace layout, and allocates a workspace.
        /// </summary>
        public static AtlasWorkspace CreateWorkspace(
            AtlasCompiledPlan plan,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
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

            var shapes = AtlasShapeResolver.Resolve(plan);

            return CreateWorkspace(
                shapes,
                allocator,
                options);
        }

        /// <summary>
        /// Compiles a workspace layout from an already resolved shape set and allocates a workspace.
        /// </summary>
        public static AtlasWorkspace CreateWorkspace(
            AtlasResolvedShapeSet shapes,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            ValidateAllocatorOrThrow(allocator);
            ValidateAllocatableOrThrow(shapes);

            var layout = AtlasWorkspaceLayoutCompiler.Compile(shapes);

            return AtlasWorkspace.Create(
                layout,
                allocator,
                options);
        }

        /// <summary>
        /// Resolves field shapes from a Contract table, compiles a layout, and allocates a workspace.
        /// </summary>
        public static AtlasWorkspace CreateWorkspace(
            AtlasContractTable contracts,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var shapes = AtlasShapeResolver.Resolve(contracts);

            return CreateWorkspace(
                shapes,
                allocator,
                options);
        }

        /// <summary>
        /// Resolves field shapes from a compiled plan and compiles a workspace layout without allocating memory.
        /// </summary>
        public static AtlasWorkspaceLayout CompileLayout(AtlasCompiledPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return CompileLayout(AtlasShapeResolver.Resolve(plan));
        }

        /// <summary>
        /// Compiles a workspace layout from an already resolved shape set without allocating memory.
        /// </summary>
        public static AtlasWorkspaceLayout CompileLayout(AtlasResolvedShapeSet shapes)
        {
            ValidateAllocatableOrThrow(shapes);

            return AtlasWorkspaceLayoutCompiler.Compile(shapes);
        }

        /// <summary>
        /// Resolves field shapes from a Contract table and compiles a workspace layout without allocating memory.
        /// </summary>
        public static AtlasWorkspaceLayout CompileLayout(AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            return CompileLayout(AtlasShapeResolver.Resolve(contracts));
        }

        /// <summary>
        /// Returns whether the supplied resolved shape set can be compiled by the current workspace layout model.
        /// </summary>
        public static bool CanAllocate(AtlasResolvedShapeSet shapes)
        {
            return AtlasWorkspaceLayoutCompiler.CanCompile(shapes);
        }

        /// <summary>
        /// Validates that a resolved shape set can be compiled by the current workspace layout model.
        /// </summary>
        public static void ValidateAllocatableOrThrow(AtlasResolvedShapeSet shapes)
        {
            AtlasWorkspaceLayoutCompiler.ValidateCompilableOrThrow(shapes);
        }

        private static void ValidateAllocatorOrThrow(Allocator allocator)
        {
            if (allocator == Allocator.None ||
                allocator == Allocator.Invalid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(allocator),
                    allocator,
                    "Atlas workspace requires a concrete Unity allocator.");
            }
        }
    }
}
