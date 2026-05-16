// Packages/com.lokrain.atlas/Runtime/Fields/AtlasFieldRole.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields
//
// Purpose
// - Define why a field exists in the Atlas data contract.
// - Keep canonical, payload, diagnostic, stage-transient, and external storage explicit.
// - Prevent operation scratch from being modeled as a field role.

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Defines the semantic role of an Atlas field in the generated-world contract.
    /// </summary>
    /// <remarks>
    /// Field role answers why the field exists. It is separate from storage kind, ownership,
    /// lifetime, access mode, and hash policy. Operation scratch is intentionally absent because
    /// scratch memory is scheduler-owned temporary native storage, not a field contract.
    /// </remarks>
    public enum AtlasFieldRole : byte
    {
        /// <summary>
        /// No field role is declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// Durable generated-world state that defines canonical map truth.
        /// </summary>
        Canonical = 1,

        /// <summary>
        /// Derived output prepared for a downstream consumer outside canonical generation truth.
        /// </summary>
        Payload = 2,

        /// <summary>
        /// Explicit validation, inspection, profiling, test, or tooling data.
        /// </summary>
        Diagnostic = 3,

        /// <summary>
        /// Workspace data produced by one operation and consumed by another operation in the same stage.
        /// </summary>
        StageTransient = 4,

        /// <summary>
        /// Data owned by an external system but declared in Atlas for validation and binding.
        /// </summary>
        External = 5
    }
}
