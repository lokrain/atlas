// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFields.BaseElevation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Fields
//
// Purpose
// - Declare typed field contracts for the Landmass stage PrimaryContinent route.
// - Keep canonical output fields and stage-transient cross-operation fields explicit.
// - Preserve field metadata independently from contract-table slot assignment and workspace storage.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Fields
{
    public static partial class AtlasLandmassFields
    {
        /// <summary>
        /// Canonical Q16.16 base elevation for every map cell.
        /// </summary>
        public readonly struct BaseElevation : IAtlasField<int>
        {
            public StableDataId StableId => AtlasLandmassFieldIds.BaseElevation;
            public AtlasFieldRole Role => AtlasFieldRole.Canonical;
            public StorageKind StorageKind => StorageKind.NativeArray;
            public OwnershipPolicy Ownership => OwnershipPolicy.AtlasOwned;
            public LifetimePolicy Lifetime => LifetimePolicy.Plan;
            public AtlasShapeDomain ShapeDomain => MapCellsDomain;
            public LengthShape LengthShape => MapCellsLength;
            public AtlasFieldFlags Flags => DenseProducedMapFieldFlags;
            public HashParticipation HashParticipation => HashParticipation.Full;
            public FixedString64Bytes DebugName => new FixedString64Bytes(AtlasLandmassFieldNames.BaseElevation);
        }
    }
}
