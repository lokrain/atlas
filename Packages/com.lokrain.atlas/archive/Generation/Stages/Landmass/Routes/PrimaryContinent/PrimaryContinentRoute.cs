// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Routes/PrimaryContinent/PrimaryContinentRoute.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent
//
// Purpose
// - Define the PrimaryContinent route operation sequence for the Landmass stage.
// - Keep route occurrence order under the owning stage instead of under individual operations.
// - Publish operation arrays without exposing mutable shared state.

using Lokrain.Atlas.Generation.Operations.CompleteContinentArea;
using Lokrain.Atlas.Generation.Operations.ComposeBaseElevation;
using Lokrain.Atlas.Generation.Operations.EvaluateContinentSuitability;
using Lokrain.Atlas.Generation.Operations.FormContinentCandidate;
using Lokrain.Atlas.Generation.Operations.PreserveMainContinent;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Routes.PrimaryContinent
{
    /// <summary>
    /// Route contract for the Landmass stage PrimaryContinent route.
    /// </summary>
    public static class PrimaryContinentRoute
    {

        /// <summary>
        /// Stable diagnostic route name.
        /// </summary>
        public static readonly FixedString64Bytes RouteName =
            new(AtlasLandmassSchemaNames.PrimaryContinentRoute);

        /// <summary>
        /// Diagnostic route operation catalog name.
        /// </summary>
        public static readonly FixedString64Bytes OperationCatalogName =
            new("catalog.landmass.primary_continent.operations");

        /// <summary>
        /// Diagnostic operation-set name for this route.
        /// </summary>
        public static readonly FixedString64Bytes OperationSetName =
            new("operations.landmass.primary_continent");

        /// <summary>
        /// Number of operation occurrences required by this route.
        /// </summary>
        public const int OperationCount = 5;

        /// <summary>
        /// Creates the unique operation catalog used by this route.
        /// </summary>
        public static AtlasOperationCatalog CreateOperationCatalog()
        {
            return AtlasOperationCatalog.Create(
                OperationCatalogName,
                CreateOperations());
        }

        /// <summary>
        /// Creates this route's operation definitions in deterministic route order.
        /// </summary>
        public static AtlasOperationDefinition[] CreateOperations()
        {
            return new[]
            {
                EvaluateContinentSuitabilityOperation.CreateDefinition(),
                FormContinentCandidateOperation.CreateDefinition(),
                PreserveMainContinentOperation.CreateDefinition(),
                CompleteContinentAreaOperation.CreateDefinition(),
                ComposeBaseElevationOperation.CreateDefinition()
            };
        }

        /// <summary>
        /// Creates this route's operation ids in deterministic route order.
        /// </summary>
        public static AtlasOperationId[] CreateOperationIds()
        {
            return new[]
            {
                EvaluateContinentSuitabilityOperation.Id,
                FormContinentCandidateOperation.Id,
                PreserveMainContinentOperation.Id,
                CompleteContinentAreaOperation.Id,
                ComposeBaseElevationOperation.Id
            };
        }

        /// <summary>
        /// Creates the ordered operation occurrence set for this route.
        /// </summary>
        public static AtlasOperationSet CreateOperationSet()
        {
            return AtlasOperationSet.Create(
                OperationSetName,
                CreateOperations());
        }
    }
}
