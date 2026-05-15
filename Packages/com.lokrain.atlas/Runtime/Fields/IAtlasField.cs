// Packages/com.lokrain.atlas/Runtime/Fields/IAtlasField.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields
//
// Purpose
// - Declare the stable metadata contract for a typed Atlas field.
// - Keep field declarations allocation-free, deterministic, and readable from default(TField).
// - Carry semantic identity, storage policy, ownership policy, lifetime policy, length shape,
//   shape domain, hash policy, flags, and diagnostic name into contract construction.
//
// Design notes
// - Field declarations are schema metadata.
// - Field declarations do not own runtime storage.
// - Field declarations do not resolve workspace memory.
// - Field declarations do not schedule jobs.
// - Field declarations do not read scene, world, frame, GameObject, ScriptableObject, or UnityEngine state.
// - StableDataId default/zero may be valid only if StableDataId itself allows it; missing state must not be inferred from zero.
// - ShapeDomain describes what resolved length/capacity means.
// - LengthShape describes how length/capacity is resolved.
// - StorageFormat is derived from TElement by contract construction, not declared here.
// - Burst jobs should receive compiled/resolved native containers, not field declarations.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Declares the stable metadata contract for a typed Atlas field.
    /// </summary>
    /// <typeparam name="TElement">
    /// Unmanaged element type stored by the field.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Atlas fields are declared as immutable value types that implement this interface.
    /// The field type provides stable schema metadata; the contract table provides canonical
    /// slot ordering; the compiler resolves length/capacity; the workspace owns native storage.
    /// </para>
    ///
    /// <para>
    /// Implementations must be allocation-free and safe to read from <c>default(TField)</c>.
    /// Field declarations must not depend on scene state, managed services, Unity objects,
    /// current frame data, runtime storage, or mutable static state.
    /// </para>
    ///
    /// <para>
    /// This interface is intended for contract creation, validation, storage allocation policy,
    /// shape resolution, artifact metadata, and typed field access validation. Jobs should receive
    /// resolved native containers or numeric execution payloads.
    /// </para>
    /// </remarks>
    public interface IAtlasField<TElement>
        where TElement : unmanaged
    {
        /// <summary>
        /// Gets the durable, versioned identity of this field contract.
        /// </summary>
        /// <remarks>
        /// The stable identifier must remain unchanged across refactors and contract-table
        /// reordering. Increment its version when the field contract changes incompatibly.
        /// </remarks>
        StableDataId StableId { get; }

        /// <summary>
        /// Gets the semantic role of this field in the generated-world contract.
        /// </summary>
        /// <remarks>
        /// Role determines whether the field is canonical generated-world state, internal support
        /// data, transient scratch memory, downstream payload data, diagnostic data, or externally
        /// owned integration data.
        /// </remarks>
        AtlasFieldRole Role { get; }

        /// <summary>
        /// Gets the native storage kind required for this field.
        /// </summary>
        /// <remarks>
        /// Storage kind determines the runtime container family used by Atlas, such as fixed
        /// array storage, variable-length list storage, stream storage, or externally owned storage.
        /// </remarks>
        StorageKind StorageKind { get; }

        /// <summary>
        /// Gets the ownership policy for this field's storage.
        /// </summary>
        /// <remarks>
        /// Ownership determines which system is responsible for allocation, disposal, and lifetime
        /// enforcement.
        /// </remarks>
        OwnershipPolicy Ownership { get; }

        /// <summary>
        /// Gets the lifetime policy for this field's storage.
        /// </summary>
        /// <remarks>
        /// Lifetime determines how long allocated storage may remain valid and which allocator
        /// policies are legal for the field.
        /// </remarks>
        LifetimePolicy Lifetime { get; }

        /// <summary>
        /// Gets the semantic domain used to interpret this field's resolved shape.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Shape domain answers what resolved elements mean: cell-grid samples, vertex-grid samples,
        /// chunk rows, component rows, graph nodes, graph edges, record-stream entries, external rows,
        /// and similar domain identities.
        /// </para>
        ///
        /// <para>
        /// This is intentionally separate from <see cref="LengthShape"/>. Length shape answers how
        /// length/capacity is resolved. Shape domain answers how that resolved length/capacity must
        /// be interpreted by validators, artifacts, debug exporters, and operation policy.
        /// </para>
        /// </remarks>
        AtlasShapeDomain ShapeDomain { get; }

        /// <summary>
        /// Gets the rule used to resolve this field's runtime length or capacity.
        /// </summary>
        /// <remarks>
        /// Length shape is resolved before jobs are scheduled. Jobs should receive containers with
        /// concrete lengths and should not interpret shape rules directly.
        /// </remarks>
        LengthShape LengthShape { get; }

        /// <summary>
        /// Gets the schema, access, and allocation flags for this field.
        /// </summary>
        /// <remarks>
        /// Flags describe field-level behavior such as clearing policy, deterministic ordering
        /// requirements, aliasing constraints, and parallel-write eligibility.
        /// </remarks>
        AtlasFieldFlags Flags { get; }

        /// <summary>
        /// Gets the hash participation policy for this field.
        /// </summary>
        /// <remarks>
        /// Hash participation controls whether the field contributes to contract schema hashes,
        /// shape hashes, content hashes, compatibility hashes, or artifact hashes.
        /// </remarks>
        HashParticipation HashParticipation { get; }

        /// <summary>
        /// Gets the stable diagnostic name for this field.
        /// </summary>
        /// <remarks>
        /// Debug names are for diagnostics, editor tooling, validation reports, artifact metadata,
        /// and tests. They must not be used as stable identity.
        /// </remarks>
        FixedString64Bytes DebugName { get; }
    }
}