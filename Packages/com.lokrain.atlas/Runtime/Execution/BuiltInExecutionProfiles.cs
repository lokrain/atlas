#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Provides Atlas-owned built-in execution profiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built-in execution profiles are stable package-owned managed metadata definitions. They identify reusable
    /// execution-policy variants for runnable compilation.
    /// </para>
    /// <para>
    /// Execution profiles do not allocate storage, own native memory, schedule jobs, bind ECS data, capture
    /// artifacts, or describe executable operation data.
    /// </para>
    /// <para>
    /// The profile symbols exposed through these definitions are stable machine-facing contract values.
    /// Display names are user-facing metadata only.
    /// </para>
    /// </remarks>
    public static class BuiltInExecutionProfiles
    {
        private const string DefaultSymbolValue = "lokrain.atlas.execution.profile.default";

        /// <summary>
        /// Gets the default Atlas execution profile.
        /// </summary>
        public static ExecutionProfile Default { get; } = new(
            Symbol.Create(DefaultSymbolValue),
            DisplayName.Create("Default Execution Profile"));

        private static readonly ExecutionProfile[] Profiles =
        {
            Default
        };

        /// <summary>
        /// Gets all Atlas-owned built-in execution profiles in declared order.
        /// </summary>
        public static IReadOnlyList<ExecutionProfile> All { get; } =
            new ReadOnlyCollection<ExecutionProfile>(Profiles);
    }
}
