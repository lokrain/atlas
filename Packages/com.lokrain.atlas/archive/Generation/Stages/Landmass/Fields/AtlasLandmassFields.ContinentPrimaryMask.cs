// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFields.ContinentPrimaryMask.cs
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
        /// Stage-transient byte preserved primary-continent mask.
        /// </summary>
        public readonly struct ContinentPrimaryMask : IAtlasField<byte>
        {
            public StableDataId StableId => AtlasLandmassFieldIds.ContinentPrimaryMask;
            public AtlasFieldRole Role => AtlasFieldRole.StageTransient;
            public StorageKind StorageKind => StorageKind.NativeArray;
            public OwnershipPolicy Ownership => OwnershipPolicy.AtlasOwned;
            public LifetimePolicy Lifetime => LifetimePolicy.Plan;
            public AtlasShapeDomain ShapeDomain => MapCellsDomain;
            public LengthShape LengthShape => MapCellsLength;
            public AtlasFieldFlags Flags => DenseProducedMapFieldFlags;
            public HashParticipation HashParticipation => HashParticipation.Default;
            public FixedString64Bytes DebugName => new FixedString64Bytes(AtlasLandmassFieldNames.ContinentPrimaryMask);
        }
    }
}
