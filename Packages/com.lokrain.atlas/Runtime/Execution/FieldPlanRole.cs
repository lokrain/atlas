#nullable enable

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Specifies whether a field binding is required as plan input, produced as plan output, or both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field plan role is managed runnable-plan metadata. It classifies semantic field flow at the plan
    /// boundary and does not describe storage lifetime, allocation ownership, scheduler behavior, job
    /// dependencies, artifact capture, ECS binding, or runtime diagnostics.
    /// </para>
    /// </remarks>
    public enum FieldPlanRole
    {
        /// <summary>
        /// No plan role has been specified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The field is required before plan execution and is not produced by the plan.
        /// </summary>
        RequiredInput = 1,

        /// <summary>
        /// The field is produced by plan execution and is not required before the plan starts.
        /// </summary>
        ProducedOutput = 2,

        /// <summary>
        /// The field is required before plan execution and is also produced by plan execution.
        /// </summary>
        RequiredInputAndProducedOutput = 3
    }
}
