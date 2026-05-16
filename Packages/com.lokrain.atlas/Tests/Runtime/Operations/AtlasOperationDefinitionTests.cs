// Packages/com.lokrain.atlas/Tests/Runtime/Operations/AtlasOperationCatalogTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations.Tests
//
// Purpose
// - Verify AtlasOperationCatalog is a unique definition catalog, not an occurrence sequence.
// - Verify operation-id and debug-name lookup behavior.
// - Verify operation occurrence sequences can be created from cataloged operation ids.
// - Verify default/zero operation ids remain valid when explicitly cataloged.

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Operations.Tests
{
    public sealed class AtlasOperationCatalogTests
    {
        private static readonly StableDataId FieldIdA =
            new StableDataId(0x4100_0000_0000_0001UL, 0x4200_0000_0000_0001UL, 1);

        private static readonly StableDataId FieldIdB =
            new StableDataId(0x4100_0000_0000_0002UL, 0x4200_0000_0000_0002UL, 1);

        private static readonly AtlasOperationId OperationIdA =
            new AtlasOperationId(0x4300_0000_0000_0001UL, 0x4400_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId OperationIdB =
            new AtlasOperationId(0x4300_0000_0000_0002UL, 0x4400_0000_0000_0002UL, 1);

        [Test]
        public void Create_WithUniqueDefinitions_PreservesCatalogOrder()
        {
            var operationA = CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a");
            var operationB = CreateOperation(OperationIdB, FieldIdB, "catalog.operation.b");

            var catalog = AtlasOperationCatalog.Create(
                new FixedString64Bytes("catalog.test"),
                operationA,
                operationB);

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.IsEmpty, Is.False);
            Assert.That(catalog[0], Is.SameAs(operationA));
            Assert.That(catalog[1], Is.SameAs(operationB));
            Assert.That(catalog.Name.ToString(), Is.EqualTo("catalog.test"));
        }

        [Test]
        public void Create_WithDuplicateOperationId_ThrowsArgumentException()
        {
            var left = CreateOperation(OperationIdA, FieldIdA, "catalog.operation.left");
            var right = CreateOperation(OperationIdA, FieldIdB, "catalog.operation.right");

            Assert.Throws<ArgumentException>(() =>
                AtlasOperationCatalog.Create(
                    new FixedString64Bytes("catalog.duplicates"),
                    left,
                    right));
        }

        [Test]
        public void Create_WithDuplicateDebugName_ThrowsArgumentException()
        {
            var left = CreateOperation(OperationIdA, FieldIdA, "catalog.operation.duplicate");
            var right = CreateOperation(OperationIdB, FieldIdB, "catalog.operation.duplicate");

            Assert.Throws<ArgumentException>(() =>
                AtlasOperationCatalog.Create(
                    new FixedString64Bytes("catalog.duplicates"),
                    left,
                    right));
        }

        [Test]
        public void Create_WithNullDefinition_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                AtlasOperationCatalog.Create(
                    new FixedString64Bytes("catalog.null"),
                    CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a"),
                    null));
        }

        [Test]
        public void Create_WithZeroOperationId_PreservesDefinition()
        {
            var operation = CreateOperation(AtlasOperationId.Zero, FieldIdA, "catalog.operation.zero");

            var catalog = AtlasOperationCatalog.Create(
                new FixedString64Bytes("catalog.zero"),
                operation);

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.Contains(AtlasOperationId.Zero), Is.True);
            Assert.That(catalog[AtlasOperationId.Zero], Is.SameAs(operation));
        }

        [Test]
        public void TryGetOperation_ByOperationId_ReturnsExpectedDefinition()
        {
            var operationA = CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a");
            var operationB = CreateOperation(OperationIdB, FieldIdB, "catalog.operation.b");
            var catalog = AtlasOperationCatalog.Create(operationA, operationB);

            var found = catalog.TryGetOperation(OperationIdB, out var operation);

            Assert.That(found, Is.True);
            Assert.That(operation, Is.SameAs(operationB));
        }

        [Test]
        public void TryGetOperation_ByDebugName_ReturnsExpectedDefinition()
        {
            var operationA = CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a");
            var operationB = CreateOperation(OperationIdB, FieldIdB, "catalog.operation.b");
            var catalog = AtlasOperationCatalog.Create(operationA, operationB);

            var found = catalog.TryGetOperation(
                new FixedString64Bytes("catalog.operation.a"),
                out var operation);

            Assert.That(found, Is.True);
            Assert.That(operation, Is.SameAs(operationA));
        }

        [Test]
        public void TryGetOperation_MissingOperationId_ReturnsFalseAndNullOperation()
        {
            var catalog = AtlasOperationCatalog.Create(
                CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a"));

            var found = catalog.TryGetOperation(OperationIdB, out var operation);

            Assert.That(found, Is.False);
            Assert.That(operation, Is.Null);
        }

        [Test]
        public void GetRequiredOperation_MissingOperationId_ThrowsKeyNotFoundException()
        {
            var catalog = AtlasOperationCatalog.Create(
                CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a"));

            Assert.Throws<KeyNotFoundException>(() =>
                catalog.GetRequiredOperation(OperationIdB));
        }

        [Test]
        public void CreateOperationSet_FromCatalogIds_PreservesRequestedOccurrenceOrderAndAllowsRepeats()
        {
            var operationA = CreateOperation(OperationIdA, FieldIdA, "catalog.operation.a");
            var operationB = CreateOperation(OperationIdB, FieldIdB, "catalog.operation.b");
            var catalog = AtlasOperationCatalog.Create(operationA, operationB);

            var set = catalog.CreateOperationSet(
                new FixedString64Bytes("catalog.sequence"),
                OperationIdB,
                OperationIdA,
                OperationIdB);

            Assert.That(set.Count, Is.EqualTo(3));
            Assert.That(set[0], Is.SameAs(operationB));
            Assert.That(set[1], Is.SameAs(operationA));
            Assert.That(set[2], Is.SameAs(operationB));
            Assert.That(set.CountOf(OperationIdB), Is.EqualTo(2));
        }

        private static AtlasOperationDefinition CreateOperation(
            AtlasOperationId operationId,
            StableDataId fieldId,
            string debugName)
        {
            return AtlasOperationDefinition.Create(
                operationId,
                new FixedString64Bytes(debugName),
                AtlasOperationRole.CanonicalGeneration,
                AtlasOperationAccess.Create(
                    fieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullLogicalLength,
                    new FixedString64Bytes(debugName + ".field")));
        }
    }
}
