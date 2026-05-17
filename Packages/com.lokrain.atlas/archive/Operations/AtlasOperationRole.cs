// Packages/com.lokrain.atlas/Runtime/Operations/AtlasOperationRole.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Define semantic operation categories for validation, diagnostics, tooling, and policy.
// - Keep durable operation identity separate from operation meaning.
// - Avoid using C# type names, folder names, or debug names as operation semantics.

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines the semantic role of an Atlas operation definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operation id is durable identity. Operation role is semantic classification. Validators,
    /// editor tooling, generated documentation, profiling, diagnostics, and workflow policy use
    /// the role to reason about operation purpose without hard-coding operation ids.
    /// </para>
    /// </remarks>
    public enum AtlasOperationRole : byte
    {
        /// <summary>
        /// No semantic operation role is declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// Initializes deterministic run, seed, dimensions, policy, or request metadata.
        /// </summary>
        Initialization = 1,

        /// <summary>
        /// Clears, initializes, invalidates, or prepares workspace-owned storage.
        /// </summary>
        WorkspacePreparation = 2,

        /// <summary>
        /// Resolves shape, count, partition, layout, or domain metadata.
        /// </summary>
        ShapeResolution = 3,

        /// <summary>
        /// Produces canonical generated-world data.
        /// </summary>
        CanonicalGeneration = 4,

        /// <summary>
        /// Produces transient support data used by canonical generation.
        /// </summary>
        SupportGeneration = 5,

        /// <summary>
        /// Performs graph, component, topology, distance, routing, or connectivity work.
        /// </summary>
        TopologyProcessing = 6,

        /// <summary>
        /// Performs deterministic simulation over generated state.
        /// </summary>
        Simulation = 7,

        /// <summary>
        /// Validates generated data and emits diagnostics or quality metrics.
        /// </summary>
        Validation = 8,

        /// <summary>
        /// Produces durable artifact data or artifact-ready payloads.
        /// </summary>
        ArtifactProduction = 9,

        /// <summary>
        /// Produces downstream presentation payloads without becoming canonical generation truth.
        /// </summary>
        PresentationPayload = 10,

        /// <summary>
        /// Produces deterministic physics payloads without creating Unity physics runtime objects.
        /// </summary>
        PhysicsPayload = 11,

        /// <summary>
        /// Produces deterministic navigation payloads without creating Unity navigation runtime objects.
        /// </summary>
        NavigationPayload = 12,

        /// <summary>
        /// Imports, exports, synchronizes, or fences externally owned storage.
        /// </summary>
        ExternalInterop = 13,

        /// <summary>
        /// Emits debug, inspection, visualization, or tooling-facing data.
        /// </summary>
        Debugging = 14
    }
}