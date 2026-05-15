// Packages/com.lokrain.atlas/Runtime/Execution/AtlasRunRequest.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Represent one managed Atlas run request.
// - Bind authored pipeline metadata, Contract table, executor registry, workspace allocation policy,
//   artifact output policy, and optional debug-map export policy.
// - Keep production run options explicit without making the workflow signature grow unbounded.
//
// Design notes
// - This is orchestration input, not execution state.
// - This type does not compile a plan.
// - This type does not allocate workspace memory.
// - This type does not schedule jobs.
// - This type does not capture or write artifacts.
// - This type does not render or write debug maps.
// - Artifact capture and debug-map export require completed execution to avoid racing scheduled jobs.
// - Debug maps are derived from captured artifacts, not directly from live workspace memory.

using System;
using System.Globalization;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Debugging;
using Lokrain.Atlas.Executors;
using Lokrain.Atlas.Pipelines;
using Unity.Collections;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Managed input contract for one Atlas run workflow invocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasRunRequest"/> is the production orchestration boundary for a full run:
    /// authored pipeline plus Contract table, executor registry, workspace allocation policy,
    /// artifact capture/write policy, and optional debug-map export request.
    /// </para>
    ///
    /// <para>
    /// The request deliberately does not execute anything. The run workflow consumes this object,
    /// then performs compilation, shape resolution, workspace allocation, context creation,
    /// operation execution, artifact capture, artifact writing, and debug-map writing in that order.
    /// </para>
    ///
    /// <para>
    /// Artifact capture and debug-map export require <see cref="CompleteExecution"/> because artifact
    /// capture copies workspace memory. Copying workspace bytes while jobs are still scheduled would
    /// create an invalid race at the orchestration boundary.
    /// </para>
    /// </remarks>
    public sealed class AtlasRunRequest
    {
        private AtlasRunRequest(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasOperationExecutorRegistry executors,
            Allocator workspaceAllocator,
            NativeArrayOptions workspaceAllocationOptions,
            bool completeExecution,
            bool captureArtifact,
            bool computeArtifactContentHashes,
            string artifactFilePath,
            bool overwriteArtifactFile,
            AtlasDebugMapRequest debugMap)
        {
            ValidateConstructorArgumentsOrThrow(
                pipeline,
                contracts,
                executors,
                workspaceAllocator,
                workspaceAllocationOptions,
                completeExecution,
                captureArtifact,
                computeArtifactContentHashes,
                artifactFilePath,
                debugMap);

            Pipeline = pipeline;
            Contracts = contracts;
            Executors = executors;
            WorkspaceAllocator = workspaceAllocator;
            WorkspaceAllocationOptions = workspaceAllocationOptions;
            CompleteExecution = completeExecution;
            CaptureArtifact = captureArtifact;
            ComputeArtifactContentHashes = computeArtifactContentHashes;
            ArtifactFilePath = artifactFilePath;
            OverwriteArtifactFile = overwriteArtifactFile;
            DebugMap = debugMap;
        }

        /// <summary>
        /// Gets the authored pipeline definition to compile and execute.
        /// </summary>
        public AtlasPipelineDefinition Pipeline { get; }

        /// <summary>
        /// Gets the Contract table used by the compiler and workspace allocator.
        /// </summary>
        public AtlasContractTable Contracts { get; }

        /// <summary>
        /// Gets the executor registry used by the operation runner.
        /// </summary>
        public AtlasOperationExecutorRegistry Executors { get; }

        /// <summary>
        /// Gets the Unity allocator used for workspace-owned native memory.
        /// </summary>
        public Allocator WorkspaceAllocator { get; }

        /// <summary>
        /// Gets the initialization policy used for workspace-owned native arrays.
        /// </summary>
        public NativeArrayOptions WorkspaceAllocationOptions { get; }

        /// <summary>
        /// Gets whether the workflow must complete scheduled execution before returning.
        /// </summary>
        public bool CompleteExecution { get; }

        /// <summary>
        /// Gets whether the workflow must capture a durable managed artifact after execution.
        /// </summary>
        public bool CaptureArtifact { get; }

        /// <summary>
        /// Gets whether artifact capture should compute deterministic content hashes.
        /// </summary>
        public bool ComputeArtifactContentHashes { get; }

        /// <summary>
        /// Gets the optional artifact output file path.
        /// </summary>
        public string ArtifactFilePath { get; }

        /// <summary>
        /// Gets whether an existing artifact output file may be overwritten.
        /// </summary>
        public bool OverwriteArtifactFile { get; }

        /// <summary>
        /// Gets the optional debug-map export request.
        /// </summary>
        public AtlasDebugMapRequest DebugMap { get; }

        /// <summary>
        /// Gets whether this request writes an artifact file.
        /// </summary>
        public bool HasArtifactFilePath => HasPath(ArtifactFilePath);

        /// <summary>
        /// Gets whether this request writes a selected debug-map image.
        /// </summary>
        public bool HasDebugMap => DebugMap != null;

        /// <summary>
        /// Gets whether the workflow must capture an artifact for retained output, artifact file
        /// writing, or debug-map generation.
        /// </summary>
        public bool RequiresArtifactCapture =>
            CaptureArtifact ||
            HasArtifactFilePath ||
            HasDebugMap;

        /// <summary>
        /// Creates a validated Atlas run request.
        /// </summary>
        public static AtlasRunRequest Create(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasOperationExecutorRegistry executors,
            Allocator workspaceAllocator,
            NativeArrayOptions workspaceAllocationOptions = NativeArrayOptions.ClearMemory,
            bool completeExecution = true,
            bool captureArtifact = true,
            bool computeArtifactContentHashes = true,
            string artifactFilePath = null,
            bool overwriteArtifactFile = true,
            AtlasDebugMapRequest debugMap = null)
        {
            return new AtlasRunRequest(
                pipeline,
                contracts,
                executors,
                workspaceAllocator,
                workspaceAllocationOptions,
                completeExecution,
                captureArtifact,
                computeArtifactContentHashes,
                artifactFilePath,
                overwriteArtifactFile,
                debugMap);
        }

        /// <summary>
        /// Creates an equivalent request with an artifact output file path.
        /// </summary>
        public AtlasRunRequest WithArtifactFilePath(
            string artifactFilePath,
            bool overwrite = true)
        {
            return new AtlasRunRequest(
                Pipeline,
                Contracts,
                Executors,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                captureArtifact: true,
                ComputeArtifactContentHashes,
                artifactFilePath,
                overwrite,
                DebugMap);
        }

        /// <summary>
        /// Creates an equivalent request without artifact file output.
        /// </summary>
        public AtlasRunRequest WithoutArtifactFilePath()
        {
            return new AtlasRunRequest(
                Pipeline,
                Contracts,
                Executors,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                CaptureArtifact,
                ComputeArtifactContentHashes,
                artifactFilePath: null,
                overwriteArtifactFile: true,
                DebugMap);
        }

        /// <summary>
        /// Creates an equivalent request with a debug-map export request.
        /// </summary>
        public AtlasRunRequest WithDebugMap(
            AtlasDebugMapRequest debugMap)
        {
            return new AtlasRunRequest(
                Pipeline,
                Contracts,
                Executors,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                captureArtifact: true,
                ComputeArtifactContentHashes,
                ArtifactFilePath,
                OverwriteArtifactFile,
                debugMap);
        }

        /// <summary>
        /// Creates an equivalent request without debug-map output.
        /// </summary>
        public AtlasRunRequest WithoutDebugMap()
        {
            return new AtlasRunRequest(
                Pipeline,
                Contracts,
                Executors,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                CaptureArtifact,
                ComputeArtifactContentHashes,
                ArtifactFilePath,
                OverwriteArtifactFile,
                debugMap: null);
        }

        /// <summary>
        /// Creates an equivalent request that does not capture artifacts, write artifact files, or
        /// export debug maps.
        /// </summary>
        public AtlasRunRequest WithoutArtifactCapture()
        {
            return new AtlasRunRequest(
                Pipeline,
                Contracts,
                Executors,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                captureArtifact: false,
                computeArtifactContentHashes: false,
                artifactFilePath: null,
                overwriteArtifactFile: true,
                debugMap: null);
        }

        /// <summary>
        /// Validates this request.
        /// </summary>
        public void ValidateOrThrow()
        {
            ValidateConstructorArgumentsOrThrow(
                Pipeline,
                Contracts,
                Executors,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                CaptureArtifact,
                ComputeArtifactContentHashes,
                ArtifactFilePath,
                DebugMap);
        }

        /// <summary>
        /// Returns a diagnostic representation of this request.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasRunRequest(Pipeline={0}, Contracts={1}, Executors={2}, Allocator={3}, AllocationOptions={4}, CompleteExecution={5}, CaptureArtifact={6}, ArtifactFile='{7}', DebugMap={8})",
                GetPipelineDiagnosticName(Pipeline),
                GetContractTableDiagnosticName(Contracts),
                Executors?.Count ?? 0,
                WorkspaceAllocator,
                WorkspaceAllocationOptions,
                CompleteExecution,
                CaptureArtifact,
                HasArtifactFilePath ? ArtifactFilePath : "<none>",
                HasDebugMap ? DebugMap.ToString() : "<none>");
        }

        private static void ValidateConstructorArgumentsOrThrow(
            AtlasPipelineDefinition pipeline,
            AtlasContractTable contracts,
            AtlasOperationExecutorRegistry executors,
            Allocator workspaceAllocator,
            NativeArrayOptions workspaceAllocationOptions,
            bool completeExecution,
            bool captureArtifact,
            bool computeArtifactContentHashes,
            string artifactFilePath,
            AtlasDebugMapRequest debugMap)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            ValidateAllocatorOrThrow(workspaceAllocator);
            ValidateAllocationOptionsOrThrow(workspaceAllocationOptions);

            if (artifactFilePath != null && !HasPath(artifactFilePath))
            {
                throw new ArgumentException(
                    "Artifact output file path must not be empty or whitespace when supplied.",
                    nameof(artifactFilePath));
            }

            debugMap?.ValidateOrThrow();

            var hasArtifactFilePath = HasPath(artifactFilePath);
            var hasDebugMap = debugMap != null;

            if (computeArtifactContentHashes && !captureArtifact)
            {
                throw new ArgumentException(
                    "Artifact content hashes cannot be requested when artifact capture is disabled.",
                    nameof(computeArtifactContentHashes));
            }

            if (hasArtifactFilePath && !captureArtifact)
            {
                throw new ArgumentException(
                    "Artifact file output requires artifact capture.",
                    nameof(artifactFilePath));
            }

            if (hasDebugMap && !captureArtifact)
            {
                throw new ArgumentException(
                    "Debug-map export requires artifact capture because debug maps are derived from captured artifact payloads.",
                    nameof(debugMap));
            }

            if (!completeExecution && captureArtifact)
            {
                throw new ArgumentException(
                    "Artifact capture requires completed execution. Set completeExecution to true or disable artifact capture.",
                    nameof(completeExecution));
            }
        }

        private static void ValidateAllocatorOrThrow(
            Allocator allocator)
        {
            if (allocator == Allocator.None ||
                allocator == Allocator.Invalid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(allocator),
                    allocator,
                    "Atlas run requests require a concrete Unity allocator for workspace-owned memory.");
            }
        }

        private static void ValidateAllocationOptionsOrThrow(
            NativeArrayOptions options)
        {
            if (options == NativeArrayOptions.ClearMemory ||
                options == NativeArrayOptions.UninitializedMemory)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(options),
                options,
                "Atlas workspace allocation options must be ClearMemory or UninitializedMemory.");
        }

        private static bool HasPath(
            string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath);
        }

        private static string GetPipelineDiagnosticName(
            AtlasPipelineDefinition pipeline)
        {
            if (pipeline == null)
            {
                return "<null>";
            }

            return pipeline.DebugName.IsEmpty
                ? "<unnamed-pipeline>"
                : pipeline.DebugName.ToString();
        }

        private static string GetContractTableDiagnosticName(
            AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                return "<null>";
            }

            return contracts.Name.IsEmpty
                ? "<unnamed-contract-table>"
                : contracts.Name.ToString();
        }
    }
}