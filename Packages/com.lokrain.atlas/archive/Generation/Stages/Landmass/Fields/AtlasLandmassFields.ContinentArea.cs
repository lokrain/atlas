// Packages/com.lokrain.atlas/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFields.ContinentArea.cs
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
        /// Stage-transient scalar area count for the preserved or completed primary continent.
        /// </summary>
        public readonly struct ContinentArea : IAtlasField<int>
        {
            public StableDataId StableId => AtlasLandmassFieldIds.ContinentArea;
            public AtlasFieldRole Role => AtlasFieldRole.StageTransient;
            public StorageKind StorageKind => StorageKind.Scalar;
            public OwnershipPolicy Ownership => OwnershipPolicy.AtlasOwned;
            public LifetimePolicy Lifetime => LifetimePolicy.Plan;
            public AtlasShapeDomain ShapeDomain => AtlasShapeDomain.Scalar(new FixedString64Bytes(AtlasLandmassFieldNames.ContinentArea));
            public LengthShape LengthShape => LengthShape.Scalar();
            public AtlasFieldFlags Flags => ProducedScalarFlags;
            public HashParticipation HashParticipation => HashParticipation.Default;
            public FixedString64Bytes DebugName => new FixedString64Bytes(AtlasLandmassFieldNames.ContinentArea);
        }
    }
}
