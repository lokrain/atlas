// Packages/com.lokrain.atlas/Tests/Runtime/Contracts/AtlasContractCatalogTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts.Tests
//
// Purpose
// - Verify AtlasContractCatalog is a unique contract catalog, not a slotted contract table.
// - Verify stable-id and debug-name lookup behavior.
// - Verify contract tables can be created from cataloged stable ids.
// - Verify default/zero StableDataId remains valid when explicitly cataloged.

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts.Tests
{
    public sealed class AtlasContractCatalogTests
    {
        private static readonly StableDataId FieldIdA =
            new(0x5100_0000_0000_0001UL, 0x5200_0000_0000_0001UL, 1);

        private static readonly StableDataId FieldIdB =
            new(0x5100_0000_0000_0002UL, 0x5200_0000_0000_0002UL, 1);

        private static readonly StableDataId FieldIdAOtherVersion =
            new(0x5100_0000_0000_0001UL, 0x5200_0000_0000_0001UL, 2);

        [Test]
        public void Create_WithUniqueContracts_PreservesCatalogOrderAndStripsSlots()
        {
            var contractA = CreateContract(FieldIdA, "catalog.field.a").WithSlot(9);
            var contractB = CreateContract(FieldIdB, "catalog.field.b");

            var catalog = AtlasContractCatalog.Create(
                new FixedString64Bytes("catalog.test"),
                contractA,
                contractB);

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.IsEmpty, Is.False);
            Assert.That(catalog[0].StableId, Is.EqualTo(FieldIdA));
            Assert.That(catalog[1].StableId, Is.EqualTo(FieldIdB));
            Assert.That(catalog[0].HasAssignedSlot, Is.False);
            Assert.That(catalog[1].HasAssignedSlot, Is.False);
            Assert.That(catalog.Name.ToString(), Is.EqualTo("catalog.test"));
        }

        [Test]
        public void Create_WithDuplicateStableId_ThrowsArgumentException()
        {
            var left = CreateContract(FieldIdA, "catalog.field.left");
            var right = CreateContract(FieldIdA, "catalog.field.right");

            Assert.Throws<ArgumentException>(() =>
                AtlasContractCatalog.Create(
                    new FixedString64Bytes("catalog.duplicates"),
                    left,
                    right));
        }

        [Test]
        public void Create_WithDuplicateDurableIdentityVersion_ThrowsArgumentException()
        {
            var left = CreateContract(FieldIdA, "catalog.field.left");
            var right = CreateContract(FieldIdAOtherVersion, "catalog.field.right");

            Assert.Throws<ArgumentException>(() =>
                AtlasContractCatalog.Create(
                    new FixedString64Bytes("catalog.versions"),
                    left,
                    right));
        }

        [Test]
        public void Create_WithDuplicateDebugName_ThrowsArgumentException()
        {
            var left = CreateContract(FieldIdA, "catalog.field.duplicate");
            var right = CreateContract(FieldIdB, "catalog.field.duplicate");

            Assert.Throws<ArgumentException>(() =>
                AtlasContractCatalog.Create(
                    new FixedString64Bytes("catalog.duplicates"),
                    left,
                    right));
        }

        [Test]
        public void Create_WithDefaultContract_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                AtlasContractCatalog.Create(
                    new FixedString64Bytes("catalog.invalid"),
                    CreateContract(FieldIdA, "catalog.field.a"),
                    default));
        }

        [Test]
        public void Create_WithZeroStableId_PreservesContract()
        {
            var contract = CreateContract(StableDataId.Zero, "catalog.field.zero");

            var catalog = AtlasContractCatalog.Create(
                new FixedString64Bytes("catalog.zero"),
                contract);

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.Contains(StableDataId.Zero), Is.True);
            Assert.That(catalog[StableDataId.Zero].StableId, Is.EqualTo(StableDataId.Zero));
        }

        [Test]
        public void TryGetContract_ByStableId_ReturnsExpectedContract()
        {
            var contractA = CreateContract(FieldIdA, "catalog.field.a");
            var contractB = CreateContract(FieldIdB, "catalog.field.b");
            var catalog = AtlasContractCatalog.Create(contractA, contractB);

            var found = catalog.TryGetContract(FieldIdB, out var contract);

            Assert.That(found, Is.True);
            Assert.That(contract.StableId, Is.EqualTo(FieldIdB));
            Assert.That(contract.DebugName, Is.EqualTo(contractB.DebugName));
        }

        [Test]
        public void TryGetContract_ByDebugName_ReturnsExpectedContract()
        {
            var contractA = CreateContract(FieldIdA, "catalog.field.a");
            var contractB = CreateContract(FieldIdB, "catalog.field.b");
            var catalog = AtlasContractCatalog.Create(contractA, contractB);

            var found = catalog.TryGetContract(
                new FixedString64Bytes("catalog.field.a"),
                out var contract);

            Assert.That(found, Is.True);
            Assert.That(contract.StableId, Is.EqualTo(FieldIdA));
        }

        [Test]
        public void TryGetContract_MissingStableId_ReturnsFalseAndDefaultContractPayload()
        {
            var catalog = AtlasContractCatalog.Create(
                CreateContract(FieldIdA, "catalog.field.a"));

            var found = catalog.TryGetContract(FieldIdB, out var contract);

            Assert.That(found, Is.False);
            Assert.That(contract.IsValid, Is.False);
        }

        [Test]
        public void GetRequiredContract_MissingStableId_ThrowsKeyNotFoundException()
        {
            var catalog = AtlasContractCatalog.Create(
                CreateContract(FieldIdA, "catalog.field.a"));

            Assert.Throws<KeyNotFoundException>(() =>
                catalog.GetRequiredContract(FieldIdB));
        }

        [Test]
        public void ProviderInterface_ByStableId_ReturnsExpectedContract()
        {
            var catalog = AtlasContractCatalog.Create(
                CreateContract(FieldIdA, "catalog.field.a"));
            IAtlasContractProvider provider = catalog;

            var found = provider.TryGetContract(FieldIdA, out var contract);

            Assert.That(found, Is.True);
            Assert.That(contract.StableId, Is.EqualTo(FieldIdA));
        }

        [Test]
        public void CreateContractTable_FromCatalogIds_AssignsFreshCanonicalSlotsInRequestedOrder()
        {
            var contractA = CreateContract(FieldIdA, "catalog.field.a").WithSlot(12);
            var contractB = CreateContract(FieldIdB, "catalog.field.b").WithSlot(7);
            var catalog = AtlasContractCatalog.Create(contractA, contractB);

            var table = catalog.CreateContractTable(
                new FixedString64Bytes("catalog.table"),
                FieldIdB,
                FieldIdA);

            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table[0].StableId, Is.EqualTo(FieldIdB));
            Assert.That(table[0].Slot.Index, Is.EqualTo(0));
            Assert.That(table[0].HasAssignedSlot, Is.True);
            Assert.That(table[1].StableId, Is.EqualTo(FieldIdA));
            Assert.That(table[1].Slot.Index, Is.EqualTo(1));
            Assert.That(table[1].HasAssignedSlot, Is.True);
        }

        [Test]
        public void CreateContractTable_WithRepeatedFieldId_ThrowsArgumentException()
        {
            var catalog = AtlasContractCatalog.Create(
                CreateContract(FieldIdA, "catalog.field.a"));

            Assert.Throws<ArgumentException>(() =>
                catalog.CreateContractTable(
                    new FixedString64Bytes("catalog.repeated"),
                    FieldIdA,
                    FieldIdA));
        }

        private static AtlasContract CreateContract(
            StableDataId stableId,
            string debugName)
        {
            return AtlasContractFactory.Create<byte>(
                stableId,
                AtlasFieldRole.Canonical,
                StorageKind.NativeArray,
                OwnershipPolicy.AtlasOwned,
                LifetimePolicy.Frame,
                AtlasShapeDomain.LinearRows(new FixedString64Bytes("catalog.rows")),
                LengthShape.Fixed(4),
                AtlasFieldFlags.None,
                HashParticipation.Default,
                new FixedString64Bytes(debugName));
        }
    }
}
