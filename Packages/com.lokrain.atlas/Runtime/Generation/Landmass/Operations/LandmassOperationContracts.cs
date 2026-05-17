#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Generation.Landmass;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Resources;

namespace Lokrain.Atlas.Generation.Landmass.Operations
{
    /// <summary>
    /// Provides Atlas-owned landmass operation contracts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Landmass operation contracts describe the semantic resources required and produced by built-in landmass
    /// operations at the catalog/planning boundary. They are not field definitions, artifact definitions,
    /// implementation bindings, runtime identifiers, job data, native containers, ECS systems, or Unity runtime
    /// objects.
    /// </para>
    /// <para>
    /// Operation contracts use resource definitions, not raw resource symbols. Resource definitions describe what
    /// semantic values exist in the managed generation graph. Field and artifact layers may later map these
    /// resources to concrete storage definitions.
    /// </para>
    /// </remarks>
    public static class LandmassOperationContracts
    {
        /// <summary>
        /// Gets the built-in evaluate continent suitability operation contract.
        /// </summary>
        public static OperationContract EvaluateContinentSuitability { get; } = new(
            LandmassOperationDefinitions.EvaluateContinentSuitability,
            Array.Empty<ResourceDefinition>(),
            new[]
            {
                LandmassResourceDefinitions.ContinentSuitability
            });

        /// <summary>
        /// Gets the built-in form continent candidate operation contract.
        /// </summary>
        public static OperationContract FormContinentCandidate { get; } = new(
            LandmassOperationDefinitions.FormContinentCandidate,
            new[]
            {
                LandmassResourceDefinitions.ContinentSuitability
            },
            new[]
            {
                LandmassResourceDefinitions.ContinentCandidate
            });

        /// <summary>
        /// Gets the built-in extract main continent operation contract.
        /// </summary>
        public static OperationContract ExtractMainContinent { get; } = new(
            LandmassOperationDefinitions.ExtractMainContinent,
            new[]
            {
                LandmassResourceDefinitions.ContinentCandidate
            },
            new[]
            {
                LandmassResourceDefinitions.MainContinent
            });

        /// <summary>
        /// Gets the built-in complete continent area operation contract.
        /// </summary>
        public static OperationContract CompleteContinentArea { get; } = new(
            LandmassOperationDefinitions.CompleteContinentArea,
            new[]
            {
                LandmassResourceDefinitions.MainContinent
            },
            new[]
            {
                LandmassResourceDefinitions.ContinentalLandmassArea
            });

        /// <summary>
        /// Gets the built-in compose base elevation operation contract.
        /// </summary>
        public static OperationContract ComposeBaseElevation { get; } = new(
            LandmassOperationDefinitions.ComposeBaseElevation,
            new[]
            {
                LandmassResourceDefinitions.ContinentalLandmassArea
            },
            new[]
            {
                LandmassResourceDefinitions.BaseElevation
            });

        private static readonly OperationContract[] OperationContracts =
        {
            EvaluateContinentSuitability,
            FormContinentCandidate,
            ExtractMainContinent,
            CompleteContinentArea,
            ComposeBaseElevation
        };

        /// <summary>
        /// Gets all Atlas-owned built-in landmass operation contracts.
        /// </summary>
        public static IReadOnlyList<OperationContract> All { get; } =
            new ReadOnlyCollection<OperationContract>(OperationContracts);
    }
}