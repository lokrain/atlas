#nullable enable

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Provides the accepted built-in execution profile set for Atlas-owned execution metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The built-in execution profile set validates and indexes Atlas-owned execution profiles by profile symbol.
    /// </para>
    /// <para>
    /// This type provides managed metadata only. It does not allocate storage, own native memory, schedule jobs,
    /// bind ECS data, capture artifacts, or describe executable operation data.
    /// </para>
    /// <para>
    /// Runnable compilation and execution support are established outside this type.
    /// </para>
    /// </remarks>
    public static class BuiltInExecutionProfileSet
    {
        /// <summary>
        /// Gets the accepted built-in execution profile set for Atlas-owned execution metadata.
        /// </summary>
        public static ExecutionProfileSet Default { get; } =
            new(BuiltInExecutionProfiles.All);
    }
}
