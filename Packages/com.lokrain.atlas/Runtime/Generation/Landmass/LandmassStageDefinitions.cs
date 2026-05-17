#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned landmass stage definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass stage definitions are stable catalog-owned definitions for built-in landmass generation.
    /// They are not route definitions, operation definitions, execution bindings, runtime identifiers, job data,
    /// native containers, ECS systems, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Stage definition symbols are stable machine-facing catalog identity values. Display names are user-facing
    /// metadata only.
    /// </para>
    /// </remarks>
    public static class LandmassStageDefinitions
    {
        private const string ContinentalLandmassSymbolValue =
            "lokrain.atlas.landmass.stage.continental_landmass";

        /// <summary>
        /// Gets the built-in continental landmass stage definition.
        /// </summary>
        public static StageDefinition ContinentalLandmass { get; } = new(
            BuiltInGenerationSchemas.World,
            LandmassStageKinds.ContinentalLandmass,
            Symbol.Create(ContinentalLandmassSymbolValue),
            DisplayName.Create("Continental Landmass"));

        private static readonly StageDefinition[] StageDefinitions =
        {
            ContinentalLandmass
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass stage definitions.
        /// </summary>
        public static IReadOnlyList<StageDefinition> All { get; } =
            new ReadOnlyCollection<StageDefinition>(StageDefinitions);
    }
}