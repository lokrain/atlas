// Packages/com.lokrain.atlas/Tests/Runtime/Generation/Stages/Landmass/Fields/AtlasLandmassFieldCatalogTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Generation.Stages.Landmass.Fields.Tests
//
// Purpose
// - Verify the Landmass stage PrimaryContinent field contract catalog.
// - Protect canonical and stage-transient field roles, storage formats, shape domains, and names.
// - Ensure the route field set is cataloged without assigning stale table-local slots.

using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Generation.Stages.Landmass.Fields.Tests
{
    public sealed class AtlasLandmassFieldCatalogTests
    {
        [Test]
        public void CreateCatalog_ReturnsAllPrimaryContinentContractsInDeterministicOrder()
        {
            var catalog = AtlasLandmassFieldCatalog.CreateCatalog();

            Assert.That(catalog.Name, Is.EqualTo(AtlasLandmassFieldCatalog.CatalogName));
            Assert.That(catalog.Count, Is.EqualTo(AtlasLandmassFieldCatalog.PrimaryContinentFieldCount));

            AssertContract(catalog[0], AtlasLandmassFieldIds.LandMask, AtlasLandmassFieldNames.LandMask, AtlasFieldRole.Canonical, StorageKind.NativeArray, typeof(byte));
            AssertContract(catalog[1], AtlasLandmassFieldIds.OceanMask, AtlasLandmassFieldNames.OceanMask, AtlasFieldRole.Canonical, StorageKind.NativeArray, typeof(byte));
            AssertContract(catalog[2], AtlasLandmassFieldIds.LandLabel, AtlasLandmassFieldNames.LandLabel, AtlasFieldRole.Canonical, StorageKind.NativeArray, typeof(int));
            AssertContract(catalog[3], AtlasLandmassFieldIds.BaseElevation, AtlasLandmassFieldNames.BaseElevation, AtlasFieldRole.Canonical, StorageKind.NativeArray, typeof(int));
            AssertContract(catalog[4], AtlasLandmassFieldIds.ContinentSuitability, AtlasLandmassFieldNames.ContinentSuitability, AtlasFieldRole.StageTransient, StorageKind.NativeArray, typeof(int));
            AssertContract(catalog[5], AtlasLandmassFieldIds.ContinentSuitabilityCutoff, AtlasLandmassFieldNames.ContinentSuitabilityCutoff, AtlasFieldRole.StageTransient, StorageKind.Scalar, typeof(int));
            AssertContract(catalog[6], AtlasLandmassFieldIds.ContinentCandidateMask, AtlasLandmassFieldNames.ContinentCandidateMask, AtlasFieldRole.StageTransient, StorageKind.NativeArray, typeof(byte));
            AssertContract(catalog[7], AtlasLandmassFieldIds.ContinentPrimaryMask, AtlasLandmassFieldNames.ContinentPrimaryMask, AtlasFieldRole.StageTransient, StorageKind.NativeArray, typeof(byte));
            AssertContract(catalog[8], AtlasLandmassFieldIds.ContinentArea, AtlasLandmassFieldNames.ContinentArea, AtlasFieldRole.StageTransient, StorageKind.Scalar, typeof(int));
            AssertContract(catalog[9], AtlasLandmassFieldIds.ContinentGrowthCutoff, AtlasLandmassFieldNames.ContinentGrowthCutoff, AtlasFieldRole.StageTransient, StorageKind.Scalar, typeof(int));
        }

        [Test]
        public void CreatePrimaryContinentContractTable_AssignsFreshSlotsInRouteFieldOrder()
        {
            var table = AtlasLandmassFieldCatalog.CreatePrimaryContinentContractTable();

            Assert.That(table.Name, Is.EqualTo(AtlasLandmassFieldCatalog.PrimaryContinentTableName));
            Assert.That(table.Count, Is.EqualTo(AtlasLandmassFieldCatalog.PrimaryContinentFieldCount));

            for (var i = 0; i < table.Count; i++)
            {
                Assert.That(table[i].HasAssignedSlot, Is.True);
                Assert.That(table[i].Slot.Index, Is.EqualTo(i));
            }

            Assert.That(table[0].StableId, Is.EqualTo(AtlasLandmassFieldIds.LandMask));
            Assert.That(table[4].StableId, Is.EqualTo(AtlasLandmassFieldIds.ContinentSuitability));
            Assert.That(table[9].StableId, Is.EqualTo(AtlasLandmassFieldIds.ContinentGrowthCutoff));
        }

        [Test]
        public void CanonicalFields_AreDefaultArtifactCapturedAndContentHashParticipating()
        {
            var catalog = AtlasLandmassFieldCatalog.CreateCatalog();

            AssertCanonicalArtifactContract(catalog[AtlasLandmassFieldIds.LandMask]);
            AssertCanonicalArtifactContract(catalog[AtlasLandmassFieldIds.OceanMask]);
            AssertCanonicalArtifactContract(catalog[AtlasLandmassFieldIds.LandLabel]);
            AssertCanonicalArtifactContract(catalog[AtlasLandmassFieldIds.BaseElevation]);
        }

        [Test]
        public void StageTransientFields_AreWorkspaceFieldsButNotDefaultArtifactCaptured()
        {
            var catalog = AtlasLandmassFieldCatalog.CreateCatalog();

            AssertStageTransientContract(catalog[AtlasLandmassFieldIds.ContinentSuitability]);
            AssertStageTransientContract(catalog[AtlasLandmassFieldIds.ContinentSuitabilityCutoff]);
            AssertStageTransientContract(catalog[AtlasLandmassFieldIds.ContinentCandidateMask]);
            AssertStageTransientContract(catalog[AtlasLandmassFieldIds.ContinentPrimaryMask]);
            AssertStageTransientContract(catalog[AtlasLandmassFieldIds.ContinentArea]);
            AssertStageTransientContract(catalog[AtlasLandmassFieldIds.ContinentGrowthCutoff]);
        }

        [Test]
        public void MapCellFields_UseMapCellShapeDomainAndResolverName()
        {
            var catalog = AtlasLandmassFieldCatalog.CreateCatalog();

            AssertMapCellField(catalog[AtlasLandmassFieldIds.LandMask]);
            AssertMapCellField(catalog[AtlasLandmassFieldIds.OceanMask]);
            AssertMapCellField(catalog[AtlasLandmassFieldIds.LandLabel]);
            AssertMapCellField(catalog[AtlasLandmassFieldIds.BaseElevation]);
            AssertMapCellField(catalog[AtlasLandmassFieldIds.ContinentSuitability]);
            AssertMapCellField(catalog[AtlasLandmassFieldIds.ContinentCandidateMask]);
            AssertMapCellField(catalog[AtlasLandmassFieldIds.ContinentPrimaryMask]);
        }

        [Test]
        public void ScalarStageTransientFields_UseScalarStorageDomainAndLength()
        {
            var catalog = AtlasLandmassFieldCatalog.CreateCatalog();

            AssertScalarField(catalog[AtlasLandmassFieldIds.ContinentSuitabilityCutoff]);
            AssertScalarField(catalog[AtlasLandmassFieldIds.ContinentArea]);
            AssertScalarField(catalog[AtlasLandmassFieldIds.ContinentGrowthCutoff]);
        }

        [Test]
        public void TypedFieldDeclarations_ValidateSuccessfully()
        {
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.LandMask, byte>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.OceanMask, byte>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.LandLabel, int>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.BaseElevation, int>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.ContinentSuitability, int>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.ContinentSuitabilityCutoff, int>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.ContinentCandidateMask, byte>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.ContinentPrimaryMask, byte>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.ContinentArea, int>();
            AtlasField.ValidateDeclarationOrThrow<AtlasLandmassFields.ContinentGrowthCutoff, int>();
        }

        private static void AssertContract(
            AtlasContract contract,
            StableDataId expectedStableId,
            string expectedDebugName,
            AtlasFieldRole expectedRole,
            StorageKind expectedStorageKind,
            System.Type expectedElementType)
        {
            Assert.That(contract.StableId, Is.EqualTo(expectedStableId));
            Assert.That(contract.DebugName, Is.EqualTo(new FixedString64Bytes(expectedDebugName)));
            Assert.That(contract.Role, Is.EqualTo(expectedRole));
            Assert.That(contract.StorageFormat.Kind, Is.EqualTo(expectedStorageKind));
            Assert.That(contract.Ownership, Is.EqualTo(OwnershipPolicy.AtlasOwned));
            Assert.That(contract.Lifetime, Is.EqualTo(LifetimePolicy.Plan));
            Assert.That(contract.HasAssignedSlot, Is.False);

            if (expectedElementType == typeof(byte))
            {
                Assert.That(contract.StorageFormat.ElementSize, Is.EqualTo(sizeof(byte)));
            }
            else if (expectedElementType == typeof(int))
            {
                Assert.That(contract.StorageFormat.ElementSize, Is.EqualTo(sizeof(int)));
            }
            else
            {
                Assert.Fail($"Unsupported expected element type '{expectedElementType}'.");
            }
        }

        private static void AssertCanonicalArtifactContract(AtlasContract contract)
        {
            Assert.That(contract.Role, Is.EqualTo(AtlasFieldRole.Canonical));
            Assert.That(contract.Role.IsCapturedByDefaultArtifactProfile(), Is.True);
            Assert.That(contract.HashParticipation.HasAll(HashParticipation.Full), Is.True);
        }

        private static void AssertStageTransientContract(AtlasContract contract)
        {
            Assert.That(contract.Role, Is.EqualTo(AtlasFieldRole.StageTransient));
            Assert.That(contract.Role.IsAtlasWorkspaceField(), Is.True);
            Assert.That(contract.Role.IsCapturedByDefaultArtifactProfile(), Is.False);
            Assert.That(contract.HashParticipation.HasAny(HashParticipation.Content), Is.False);
        }

        private static void AssertMapCellField(AtlasContract contract)
        {
            Assert.That(contract.ShapeDomain.Kind, Is.EqualTo(AtlasShapeDomainKind.CellGrid2D));
            Assert.That(contract.ShapeDomain.Name, Is.EqualTo(new FixedString64Bytes(AtlasLandmassFieldNames.MapCells)));
            Assert.That(contract.LengthShape.Kind, Is.EqualTo(LengthShapeKind.QueryCount));
            Assert.That(contract.LengthShape.Name, Is.EqualTo(new FixedString64Bytes(AtlasLandmassFieldNames.MapCells)));
        }

        private static void AssertScalarField(AtlasContract contract)
        {
            Assert.That(contract.StorageFormat.Kind, Is.EqualTo(StorageKind.Scalar));
            Assert.That(contract.ShapeDomain.Kind, Is.EqualTo(AtlasShapeDomainKind.Scalar));
            Assert.That(contract.LengthShape.Kind, Is.EqualTo(LengthShapeKind.Scalar));
            Assert.That(contract.LengthShape.FixedLength, Is.EqualTo(1));
        }
    }
}
