// Runtime/Fields/IAtlasField.cs

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Declares the stable contract for a typed Atlas Field.
    /// </summary>
    /// <typeparam name="TElement">
    /// Unmanaged element type stored by the Field.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Atlas Fields are declared as empty value types that implement this interface.
    /// The Field type provides stable metadata; the Contract table provides canonical
    /// ordering; runtime storage provides allocated native memory.
    /// </para>
    ///
    /// <para>
    /// Implementations should be immutable, allocation-free, and safe to read from
    /// <c>default(TField)</c>. Field declarations must not depend on scene state,
    /// world state, managed services, Unity objects, current frame data, or runtime storage.
    /// </para>
    ///
    /// <para>
    /// This interface is intended for Contract creation, validation, storage allocation,
    /// and typed Field resolution. Burst jobs should receive resolved native containers
    /// rather than Field declarations or Contract interfaces.
    /// </para>
    /// </remarks>
    public interface IAtlasField<TElement>
        where TElement : unmanaged
    {
        /// <summary>
        /// Gets the durable, versioned identity of this Field contract.
        /// </summary>
        /// <remarks>
        /// The stable identifier must remain unchanged across refactors and Contract-table
        /// reordering. Increment its version when the Field contract changes incompatibly.
        /// </remarks>
        StableDataId StableId { get; }

        /// <summary>
        /// Gets the semantic role of this Field in the generated-world contract.
        /// </summary>
        /// <remarks>
        /// Role determines whether the Field is canonical generated-world state, internal support
        /// data, transient scratch memory, downstream payload data, diagnostic data, or externally
        /// owned integration data.
        /// </remarks>
        AtlasFieldRole Role { get; }

        /// <summary>
        /// Gets the native storage kind required for this Field.
        /// </summary>
        /// <remarks>
        /// Storage kind determines the runtime container family used by Atlas, such as fixed
        /// array storage, variable-length list storage, stream storage, or externally owned
        /// storage.
        /// </remarks>
        StorageKind StorageKind { get; }

        /// <summary>
        /// Gets the ownership policy for this Field's storage.
        /// </summary>
        /// <remarks>
        /// Ownership determines which system is responsible for allocation, disposal, and
        /// lifetime enforcement.
        /// </remarks>
        OwnershipPolicy Ownership { get; }

        /// <summary>
        /// Gets the lifetime policy for this Field's storage.
        /// </summary>
        /// <remarks>
        /// Lifetime determines how long allocated storage may remain valid and which allocator
        /// policies are legal for the Field.
        /// </remarks>
        LifetimePolicy Lifetime { get; }

        /// <summary>
        /// Gets the rule used to resolve this Field's runtime length or capacity.
        /// </summary>
        /// <remarks>
        /// Length shape is resolved before jobs are scheduled. Jobs should receive containers
        /// with concrete lengths and should not interpret shape rules directly.
        /// </remarks>
        LengthShape LengthShape { get; }

        /// <summary>
        /// Gets the schema, access, and allocation flags for this Field.
        /// </summary>
        /// <remarks>
        /// Flags describe Field-level behavior such as read/write expectations, clearing
        /// policy, deterministic ordering requirements, and parallel-write eligibility.
        /// </remarks>
        AtlasFieldFlags Flags { get; }

        /// <summary>
        /// Gets the hash participation policy for this Field.
        /// </summary>
        /// <remarks>
        /// Hash participation controls whether the Field contributes to Contract schema
        /// hashes, shape hashes, content hashes, or compatibility hashes.
        /// </remarks>
        HashParticipation HashParticipation { get; }

        /// <summary>
        /// Gets the stable diagnostic name for this Field.
        /// </summary>
        /// <remarks>
        /// Debug names are for diagnostics, editor tooling, validation reports, and tests.
        /// They must not be used as stable identity.
        /// </remarks>
        FixedString64Bytes DebugName { get; }
    }
}