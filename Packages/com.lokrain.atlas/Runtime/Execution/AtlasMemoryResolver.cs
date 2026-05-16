// Packages/com.lokrain.atlas/Runtime/Execution/AtlasMemoryResolver.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Provide a migration facade from compiled semantic facts to layout-owned workspace memory.
// - Compile Contract-table or compiled-plan shapes into AtlasWorkspaceLayout.
// - Allocate workspace-owned native memory from AtlasWorkspaceLayout.
// - Keep allocation policy separate from operation scheduling, artifacts, jobs, and debug rendering.
//
// Design notes
// - The catalog owns meaning.
// - The compiler owns resolution.
// - The workspace owns memory.
// - Execution owns JobHandle flow.
// - Jobs own only numeric computation.
// - Artifacts own durable output.
// - This type is not the canonical memory product.
// - The canonical memory product is AtlasWorkspaceLayout.
// - This type does not schedule operations.
// - This type does not expose FieldId lookup to jobs.
// - This type does not write artifacts.
// - This type does not render debug output.
// - New code should prefer the explicit path:
//   ContractTable / CompiledPlan -> AtlasShapeResolver -> AtlasWorkspaceLayoutCompiler -> AtlasWorkspace.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Workspaces;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Migration facade for creating layout-owned workspace memory from Atlas compilation metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasMemoryResolver"/> is retained to reduce call-site churn while the package
    /// moves from direct shape-set allocation to layout-owned allocation.
    /// </para>
    ///
    /// <para>
    /// The canonical pipeline is:
    /// <c>AtlasShapeResolver</c> resolves semantic shape facts,
    /// <c>AtlasWorkspaceLayoutCompiler</c> compiles memory layout facts, and
    /// <c>AtlasWorkspace</c> allocates concrete native memory from that layout.
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

            return CreateWorkspace(
                AtlasShapeResolver.Resolve(plan),
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

            return AtlasWorkspace.Create(
                CompileLayout(shapes),
                allocator,
                options);
        }

        /// <summary>
        /// Resolves field shapes from a Contract table, compiles a workspace layout, and allocates a workspace.
        /// </summary>
        public static AtlasWorkspace CreateWorkspace(
            AtlasContractTable contracts,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            ValidateAllocatorOrThrow(allocator);

            return AtlasWorkspace.Create(
                CompileLayout(contracts),
                allocator,
                options);
        }

        /// <summary>
        /// Creates a non-owning execution context from a compiled plan and a newly allocated workspace.
        /// </summary>
        /// <remarks>
        /// The returned context does not own the workspace lifetime. The caller must dispose
        /// <see cref="AtlasExecutionContext.Workspace"/>.
        /// </remarks>
        public static AtlasExecutionContext CreateExecutionContext(
            AtlasCompiledPlan plan,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return AtlasExecutionContext.Create(
                plan,
                CreateWorkspace(
                    plan,
                    allocator,
                    options));
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

            if (plan.Contracts == null)
            {
                throw new ArgumentException(
                    "Compiled plan does not reference a Contract table.",
                    nameof(plan));
            }

            return CompileLayout(
                AtlasShapeResolver.Resolve(plan));
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

            return CompileLayout(
                AtlasShapeResolver.Resolve(contracts));
        }

        /// <summary>
        /// Returns whether the supplied resolved shape set can be compiled by the current workspace layout model.
        /// </summary>
        public static bool CanAllocate(AtlasResolvedShapeSet shapes)
        {
            return AtlasWorkspaceLayoutCompiler.CanCompile(shapes);
        }

        /// <summary>
        /// Returns whether shapes resolved from the supplied Contract table can be compiled by the current workspace layout model.
        /// </summary>
        public static bool CanAllocate(AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                return false;
            }

            try
            {
                return CanAllocate(
                    AtlasShapeResolver.Resolve(contracts));
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns whether shapes resolved from the supplied compiled plan can be compiled by the current workspace layout model.
        /// </summary>
        public static bool CanAllocate(AtlasCompiledPlan plan)
        {
            if (plan == null || plan.Contracts == null)
            {
                return false;
            }

            try
            {
                return CanAllocate(
                    AtlasShapeResolver.Resolve(plan));
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a resolved shape set can be compiled by the current workspace layout model.
        /// </summary>
        public static void ValidateAllocatableOrThrow(AtlasResolvedShapeSet shapes)
        {
            AtlasWorkspaceLayoutCompiler.ValidateCompilableOrThrow(shapes);
        }

        /// <summary>
        /// Validates that shapes resolved from a Contract table can be compiled by the current workspace layout model.
        /// </summary>
        public static void ValidateAllocatableOrThrow(AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            ValidateAllocatableOrThrow(
                AtlasShapeResolver.Resolve(contracts));
        }

        /// <summary>
        /// Validates that shapes resolved from a compiled plan can be compiled by the current workspace layout model.
        /// </summary>
        public static void ValidateAllocatableOrThrow(AtlasCompiledPlan plan)
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

            ValidateAllocatableOrThrow(
                AtlasShapeResolver.Resolve(plan));
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