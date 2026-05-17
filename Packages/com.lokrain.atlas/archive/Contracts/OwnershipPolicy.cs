// Runtime/Contracts/OwnershipPolicy.cs

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Defines which system owns allocation, disposal, and mutation authority for Atlas Field storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ownership policy is a storage contract. It determines whether Atlas may allocate memory,
    /// dispose memory, resize containers, clear contents, recycle storage, or merely borrow memory
    /// owned by another system.
    /// </para>
    ///
    /// <para>
    /// Ownership is validated together with <see cref="LifetimePolicy"/>, <see cref="StorageKind"/>,
    /// <see cref="LengthShape"/>, and plan access declarations. Invalid ownership combinations should
    /// be rejected before jobs are scheduled.
    /// </para>
    /// </remarks>
    public enum OwnershipPolicy : byte
    {
        /// <summary>
        /// No ownership policy is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for concrete Contracts and is reserved for default initialization
        /// and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Atlas owns allocation, disposal, and lifetime enforcement for the Field storage.
        /// </summary>
        /// <remarks>
        /// This is the default policy for Contract-driven storage. Atlas may allocate, clear,
        /// resize when allowed by the storage kind, recycle, and dispose memory according to the
        /// declared lifetime.
        /// </remarks>
        AtlasOwned = 1,

        /// <summary>
        /// The scheduling job or producer phase owns the storage for the Field.
        /// </summary>
        /// <remarks>
        /// Job-owned storage is appropriate for temporary outputs created by a producer job and
        /// consumed by a known dependency chain. Atlas may track the storage but must not assume
        /// it can recreate or resize it independently.
        /// </remarks>
        JobOwned = 2,

        /// <summary>
        /// The storage is owned by a system outside Atlas.
        /// </summary>
        /// <remarks>
        /// External-owned storage may be provided by ECS, native systems, engine integration,
        /// gameplay systems, or custom allocators. Atlas may validate and expose the storage to
        /// plans but must not dispose it.
        /// </remarks>
        ExternalOwned = 3,

        /// <summary>
        /// Atlas temporarily borrows storage without taking ownership.
        /// </summary>
        /// <remarks>
        /// Borrowed storage must remain valid for the entire resolved use interval. Atlas must not
        /// allocate, dispose, or resize borrowed storage. Write access requires explicit validation
        /// that the external owner grants exclusive or otherwise safe mutation authority.
        /// </remarks>
        Borrowed = 4,

        /// <summary>
        /// Atlas imports storage from another owner with an explicit lifetime and access contract.
        /// </summary>
        /// <remarks>
        /// Imported storage is stronger than borrowed storage because Atlas is allowed to model the
        /// imported memory as part of a Contract table. The original owner still defines disposal
        /// unless ownership is explicitly transferred by a higher-level integration.
        /// </remarks>
        Imported = 5,

        /// <summary>
        /// Ownership is transferred to Atlas at acquisition time.
        /// </summary>
        /// <remarks>
        /// Adopted storage starts outside Atlas but becomes Atlas-owned once acquired. Validators
        /// must require an explicit adoption path so disposal responsibility is unambiguous.
        /// </remarks>
        Adopted = 6
    }
}