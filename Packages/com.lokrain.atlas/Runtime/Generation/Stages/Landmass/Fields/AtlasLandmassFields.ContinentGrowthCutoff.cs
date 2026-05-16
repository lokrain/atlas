// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFields.ContinentGrowthCutoff.cs
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
        /// Stage-transient Q16.16 growth cutoff scalar selected during CompleteContinentArea.
        /// </summary>
        public readonly struct ContinentGrowthCutoff : IAtlasField<int>
        {
            public StableDataId StableId => AtlasLandmassFieldIds.ContinentGrowthCutoff;
            public AtlasFieldRole Role => AtlasFieldRole.StageTransient;
            public StorageKind StorageKind => StorageKind.Scalar;
            public OwnershipPolicy Ownership => OwnershipPolicy.AtlasOwned;
            public LifetimePolicy Lifetime => LifetimePolicy.Plan;
            public AtlasShapeDomain ShapeDomain => AtlasShapeDomain.Scalar(new FixedString64Bytes(AtlasLandmassFieldNames.ContinentGrowthCutoff));
            public LengthShape LengthShape => LengthShape.Scalar();
            public AtlasFieldFlags Flags => ProducedScalarFlags;
            public HashParticipation HashParticipation => HashParticipation.Default;
            public FixedString64Bytes DebugName => new FixedString64Bytes(AtlasLandmassFieldNames.ContinentGrowthCutoff);
        }
    }
}
