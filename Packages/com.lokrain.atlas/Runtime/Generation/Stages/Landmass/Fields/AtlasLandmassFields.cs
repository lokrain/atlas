// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFields.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Fields
//
// Purpose
// - Declare typed field contracts for the Landmass stage PrimaryContinent route.
// - Keep canonical output fields and stage-transient cross-operation fields explicit.
// - Preserve field metadata independently from contract-table slot assignment and workspace storage.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Fields
{
    /// <summary>
    /// Typed field declarations for the Landmass stage.
    /// </summary>
    /// <remarks>
    /// These declarations are schema metadata only. They do not allocate workspace memory and must
    /// not be passed to jobs. Operation executors resolve compiled bindings into typed native views.
    /// </remarks>
    public static partial class AtlasLandmassFields
    {
        private static readonly AtlasFieldFlags DenseProducedMapFieldFlags =
            AtlasFieldFlags.DiscardBeforeWrite |
            AtlasFieldFlags.AllowsUninitializedMemory |
            AtlasFieldFlags.AllowsParallelWrite;

        private static readonly AtlasFieldFlags ProducedScalarFlags =
            AtlasFieldFlags.DiscardBeforeWrite |
            AtlasFieldFlags.AllowsUninitializedMemory;

        private static readonly AtlasShapeDomain MapCellsDomain =
            AtlasShapeDomain.CellGrid2D(new FixedString64Bytes(AtlasLandmassFieldNames.MapCells));

        private static readonly LengthShape MapCellsLength =
            LengthShape.QueryCount(new FixedString64Bytes(AtlasLandmassFieldNames.MapCells));
    }
}
