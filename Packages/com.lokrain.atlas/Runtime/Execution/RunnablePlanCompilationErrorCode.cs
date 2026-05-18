#nullable enable

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Defines stable machine-readable error codes for runnable-plan compilation failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These codes identify deterministic compile-time validation failures while converting accepted
    /// managed semantic planning metadata into managed runnable metadata.
    /// </para>
    /// <para>
    /// Error codes are part of the compiler contract and should remain stable over time for tooling,
    /// automated validation, CI baselines, and user-facing diagnostic mapping. Human-readable diagnostic
    /// messages are not the contract.
    /// </para>
    /// <para>
    /// This enumeration is strictly for managed compilation outcomes. It does not represent runtime execution
    /// failures, native allocation failures, scheduler failures, job failures, ECS failures, artifact capture
    /// failures, or runtime diagnostic capture failures.
    /// </para>
    /// </remarks>
    public enum RunnablePlanCompilationErrorCode
    {
        /// <summary>
        /// No error code has been specified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A required field definition for a used resource was not found in the field definition set.
        /// </summary>
        MissingFieldDefinition = 1,

        /// <summary>
        /// A resolved field definition does not reference the exact expected resource definition instance.
        /// </summary>
        ResourceFieldOwnershipMismatch = 2
    }
}
