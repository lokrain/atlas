#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Schemas
{
    /// <summary>
    /// Provides Atlas-owned generation schema definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built-in generation schemas are stable package-owned catalog definitions. They are not a mutable
    /// registry and they do not perform catalog construction.
    /// </para>
    /// <para>
    /// The schema symbols exposed through these definitions are stable machine-facing contract values.
    /// Display names are user-facing metadata only.
    /// </para>
    /// </remarks>
    public static class BuiltInGenerationSchemas
    {
        private const string WorldSymbolValue = "lokrain.atlas.world";

        /// <summary>
        /// Gets the built-in Atlas world-generation schema definition.
        /// </summary>
        public static GenerationSchemaDefinition World { get; } = new GenerationSchemaDefinition(
            Symbol.Create(WorldSymbolValue),
            DisplayName.Create("Atlas World Generation"));

        private static readonly GenerationSchemaDefinition[] Definitions =
        {
            World
        };

        /// <summary>
        /// Gets all Atlas-owned built-in generation schema definitions.
        /// </summary>
        public static IReadOnlyList<GenerationSchemaDefinition> All { get; } =
            new ReadOnlyCollection<GenerationSchemaDefinition>(Definitions);
    }
}