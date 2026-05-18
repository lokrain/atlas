#nullable enable

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Specifies whether a resource field binding is marked for future capture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field capture policy is managed runnable-plan metadata. It records capture intent for a field binding
    /// and does not capture artifacts, allocate storage, own native memory, create field handles, schedule jobs,
    /// bind ECS data, or capture runtime diagnostics.
    /// </para>
    /// <para>
    /// Artifact capture, diagnostic capture, storage snapshots, serialization, and capture scheduling belong to
    /// future execution infrastructure.
    /// </para>
    /// </remarks>
    public enum FieldCapturePolicy
    {
        /// <summary>
        /// No field capture policy has been specified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The field binding is not marked for capture.
        /// </summary>
        DoNotCapture = 1,

        /// <summary>
        /// The field binding is marked for future capture.
        /// </summary>
        Capture = 2
    }
}