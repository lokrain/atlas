// Packages/com.lokrain.atlas/Tests/Runtime/Operations/AtlasOperationDefinitionTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations.Tests
//
// Purpose
// - Verify operation definitions require explicit semantic roles.
// - Verify operation role metadata is preserved by authored and compiled operation contracts.
// - Prevent fallback to unclassified operation definitions.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Operations.Tests
{
    public sealed class AtlasOperationDefinitionTests
    {
        private static readonly StableDataId FieldId =
            new(0x2100_0000_0000_0001UL, 0x2200_0000_0000_0001UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0x2300_0000_0000_0001UL, 0x2400_0000_0000_0001UL, 1);

        [Test]
        public void Create_WithConcreteRole_PreservesRole()
        {
            var operation = CreateOperation(AtlasOperationRole.CanonicalGeneration);

            Assert.That(operation.Role, Is.EqualTo(AtlasOperationRole.CanonicalGeneration));
            Assert.That(operation.OperationId, Is.EqualTo(OperationId));
            Assert.That(operation.DebugName.ToString(), Is.EqualTo("operations.test"));
        }

        [Test]
        public void Create_WithNoneRole_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateOperation(AtlasOperationRole.None));
        }

        [Test]
        public void Compile_PreservesOperationRole()
        {
            var operation = CreateOperation(AtlasOperationRole.WorkspacePreparation);
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("operations.contracts"),
                AtlasContractFactory.Create<byte>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("operations.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("operations.field")));

            var compiled = AtlasCompiledOperation.Compile(
                0,
                operation,
                contracts);

            Assert.That(compiled.Role, Is.EqualTo(AtlasOperationRole.WorkspacePreparation));
            Assert.That(compiled.OperationId, Is.EqualTo(operation.OperationId));
            Assert.That(compiled.DebugName, Is.EqualTo(operation.DebugName));
        }

        private static AtlasOperationDefinition CreateOperation(
            AtlasOperationRole role)
        {
            return AtlasOperationDefinition.Create(
                OperationId,
                new FixedString64Bytes("operations.test"),
                role,
                AtlasOperationAccess.Create(
                    FieldId,
                    AtlasOperationAccessMode.Write,
                    AtlasOperationAccessFlags.DiscardBeforeWrite |
                    AtlasOperationAccessFlags.RequiresExclusiveWrite,
                    AtlasWriteCoverage.FullLogicalLength,
                    new FixedString64Bytes("operations.field")));
        }
    }
}
