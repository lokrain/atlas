#nullable enable

using Lokrain.Atlas.Fields;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides the accepted managed field definition set for Atlas-owned landmass generation metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The landmass field definition set validates and indexes built-in landmass field definitions by field
    /// symbol and represented resource definition symbol.
    /// </para>
    /// <para>
    /// This type provides managed metadata only. It does not allocate storage, own native memory, schedule jobs,
    /// bind ECS data, capture artifacts, or describe executable operation data.
    /// </para>
    /// <para>
    /// Catalog ownership and execution support are established outside this type.
    /// </para>
    /// </remarks>
    public static class LandmassFieldDefinitionSet
    {
        /// <summary>
        /// Gets the accepted field definition set for Atlas-owned landmass generation metadata.
        /// </summary>
        public static FieldDefinitionSet Default { get; } =
            new(LandmassFieldDefinitions.All);
    }
}
