#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Generation.Landmass
{
    /// <summary>
    /// Provides Atlas-owned landmass stage contracts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass stage contracts describe the semantic resources required and produced by built-in landmass
    /// stages at the catalog/planning boundary. They are not field definitions, artifact definitions, execution
    /// bindings, runtime identifiers, job data, native containers, ECS systems, or Unity runtime objects.
    /// </para>
    /// <para>
    /// Stage contracts use resource definitions, not raw resource symbols. Resource definitions describe what
    /// semantic values exist in the managed generation graph. Field and artifact layers may later map these
    /// resources to concrete storage definitions.
    /// </para>
    /// </remarks>
    public static class LandmassStageContracts
    {
        /// <summary>
        /// Gets the built-in continental landmass stage contract.
        /// </summary>
        public static StageContract ContinentalLandmass { get; } = new(
            LandmassStageDefinitions.ContinentalLandmass,
            Array.Empty<ResourceDefinition>(),
            new[]
            {
                LandmassResourceDefinitions.ContinentalLandmassArea,
                LandmassResourceDefinitions.BaseElevation
            });

        private static readonly StageContract[] StageContracts =
        {
            ContinentalLandmass
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass stage contracts.
        /// </summary>
        public static IReadOnlyList<StageContract> All { get; } =
            new ReadOnlyCollection<StageContract>(StageContracts);
    }
}