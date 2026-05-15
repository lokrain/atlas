// Runtime/Contracts/LifetimePolicy.cs

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Defines the intended validity interval for Atlas Field storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifetime policy describes how long storage may remain valid after acquisition. It is
    /// validated together with ownership, storage kind, allocator policy, dependency tracking,
    /// and plan access. A Field lifetime must be long enough for every scheduled job that
    /// receives the resolved storage.
    /// </para>
    ///
    /// <para>
    /// Lifetime does not by itself define ownership. A Field may be frame-lifetime and
    /// Atlas-owned, frame-lifetime and borrowed, or persistent and externally owned. Disposal
    /// authority is defined by <see cref="OwnershipPolicy"/>.
    /// </para>
    /// </remarks>
    public enum LifetimePolicy : byte
    {
        /// <summary>
        /// No lifetime policy is declared.
        /// </summary>
        /// <remarks>
        /// This value is invalid for concrete Contracts and is reserved for default
        /// initialization and validation failure paths.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Storage is valid only for a single immediate invocation.
        /// </summary>
        /// <remarks>
        /// Invocation lifetime is appropriate for transient data that is resolved, consumed, and
        /// released within a tightly controlled scheduling call. Validators must reject usages
        /// where the storage can outlive the invocation through asynchronous job dependencies.
        /// </remarks>
        Invocation = 1,

        /// <summary>
        /// Storage is valid for one Atlas frame.
        /// </summary>
        /// <remarks>
        /// Frame lifetime is the default policy for dense job inputs and outputs that are rebuilt
        /// every simulation frame. Disposal or recycling must occur only after all dependent jobs
        /// that use the frame storage have completed.
        /// </remarks>
        Frame = 2,

        /// <summary>
        /// Storage is valid for one compiled or scheduled plan execution.
        /// </summary>
        /// <remarks>
        /// Plan lifetime is appropriate for intermediate Fields that exist only while executing
        /// a known plan graph. It is shorter than world or scene lifetime but may span multiple
        /// jobs and dependency edges.
        /// </remarks>
        Plan = 3,

        /// <summary>
        /// Storage is valid for the lifetime of a simulation world or equivalent runtime context.
        /// </summary>
        /// <remarks>
        /// World lifetime is appropriate for reusable persistent working sets, stable lookup data,
        /// and Fields that are shared across many frames within the same world.
        /// </remarks>
        World = 4,

        /// <summary>
        /// Storage is valid for the lifetime of a loaded scene or equivalent content scope.
        /// </summary>
        /// <remarks>
        /// Scene lifetime is appropriate for data that is derived from scene content and can be
        /// released when that content scope is unloaded.
        /// </remarks>
        Scene = 5,

        /// <summary>
        /// Storage is valid until explicitly disposed by its owner.
        /// </summary>
        /// <remarks>
        /// Persistent lifetime requires explicit disposal and should be used sparingly. Validators
        /// should require ownership and allocator policies that make disposal responsibility clear.
        /// </remarks>
        Persistent = 6,

        /// <summary>
        /// Storage validity is controlled by an external owner.
        /// </summary>
        /// <remarks>
        /// External lifetime is appropriate for borrowed, imported, or externally owned memory.
        /// Atlas may validate that the lifetime was declared, but the external owner is responsible
        /// for ensuring the memory remains valid while jobs use it.
        /// </remarks>
        External = 7
    }
}