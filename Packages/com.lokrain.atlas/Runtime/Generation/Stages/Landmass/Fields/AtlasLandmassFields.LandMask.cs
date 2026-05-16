// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFields.LandMask.cs
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
        /// Canonical byte land mask. Values are <c>0 = not land</c>, <c>1 = land</c>.
        /// </summary>
        public readonly struct LandMask : IAtlasField<byte>
        {
            public StableDataId StableId => AtlasLandmassFieldIds.LandMask;
            public AtlasFieldRole Role => AtlasFieldRole.Canonical;
            public StorageKind StorageKind => StorageKind.NativeArray;
            public OwnershipPolicy Ownership => OwnershipPolicy.AtlasOwned;
            public LifetimePolicy Lifetime => LifetimePolicy.Plan;
            public AtlasShapeDomain ShapeDomain => MapCellsDomain;
            public LengthShape LengthShape => MapCellsLength;
            public AtlasFieldFlags Flags => DenseProducedMapFieldFlags;
            public HashParticipation HashParticipation => HashParticipation.Full;
            public FixedString64Bytes DebugName => new FixedString64Bytes(AtlasLandmassFieldNames.LandMask);
        }
    }
}
