#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned landmass stage kinds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass stage kinds are stable catalog-facing categories used by built-in landmass stage definitions.
    /// They are not stage definitions, route definitions, execution bindings, runtime identifiers, job data,
    /// native containers, ECS systems, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Stage kind symbols are stable machine-facing contract values.
    /// </para>
    /// </remarks>
    public static class LandmassStageKinds
    {
        private const string ContinentalLandmassSymbolValue =
            "lokrain.atlas.landmass.stage_kind.continental_landmass";

        /// <summary>
        /// Gets the built-in continental landmass stage kind.
        /// </summary>
        public static StageKind ContinentalLandmass { get; } =
            StageKind.Create(ContinentalLandmassSymbolValue);

        private static readonly StageKind[] StageKinds =
        {
            ContinentalLandmass
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass stage kinds.
        /// </summary>
        public static IReadOnlyList<StageKind> All { get; } =
            new ReadOnlyCollection<StageKind>(StageKinds);
    }
}