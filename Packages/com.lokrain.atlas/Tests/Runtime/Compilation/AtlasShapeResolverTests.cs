// Packages/com.lokrain.atlas/Tests/Runtime/Compilation/AtlasShapeResolverTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation.Tests
//
// Purpose
// - Verify Contract-table-only shape resolution.
// - Verify slot-order preservation.
// - Verify field-relative resolution independent of Contract-table declaration order.
// - Verify derived capacity semantics for fixed and growable storage.
// - Verify unsupported dynamic shape kinds fail explicitly.
// - Verify cyclic field-relative shape dependencies are rejected by the resolver.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation.Tests
{
    public sealed class AtlasShapeResolverTests
    {
        private static readonly StableDataId ScalarFieldId =
            new StableDataId(0x1000_0000_0000_0001UL, 0x2000_0000_0000_0001UL, 1);

        private static readonly StableDataId FixedFieldId =
            new StableDataId(0x1000_0000_0000_0002UL, 0x2000_0000_0000_0002UL, 1);

        private static readonly StableDataId MatchTargetFieldId =
            new StableDataId(0x1000_0000_0000_0003UL, 0x2000_0000_0000_0003UL, 1);

        private static readonly StableDataId MatchSourceFieldId =
            new StableDataId(0x1000_0000_0000_0004UL, 0x2000_0000_0000_0004UL, 1);

        private static readonly StableDataId CapacityTargetFieldId =
            new StableDataId(0x1000_0000_0000_0005UL, 0x2000_0000_0000_0005UL, 1);

        private static readonly StableDataId CapacitySourceFieldId =
            new StableDataId(0x1000_0000_0000_0006UL, 0x2000_0000_0000_0006UL, 1);

        private static readonly StableDataId DynamicFieldId =
            new StableDataId(0x1000_0000_0000_0007UL, 0x2000_0000_0000_0007UL, 1);

        private static readonly StableDataId CycleAFieldId =
            new StableDataId(0x1000_0000_0000_0008UL, 0x2000_0000_0000_0008UL, 1);

        private static readonly StableDataId CycleBFieldId =
            new StableDataId(0x1000_0000_0000_0009UL, 0x2000_0000_0000_0009UL, 1);

        [Test]
        public void Resolve_FixedAndScalarShapes_ReturnsSlotOrderedResolvedShapes()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.fixed.scalar"),
                CreateIntContract(
                    ScalarFieldId,
                    StorageKind.Scalar,
                    LengthShape.Scalar(),
                    "shape.scalar"),
                CreateIntContract(
                    FixedFieldId,
                    StorageKind.NativeArray,
                    LengthShape.Fixed(8),
                    "shape.fixed"));

            var set = AtlasShapeResolver.Resolve(contracts);

            Assert.That(set, Is.Not.Null);
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Contracts, Is.SameAs(contracts));
            Assert.That(set.RequiresMemory, Is.True);
            Assert.That(set.HasCapacitySlack, Is.False);

            Assert.That(set[0].StableId, Is.EqualTo(ScalarFieldId));
            Assert.That(set[0].Slot.Index, Is.EqualTo(0));
            Assert.That(set[0].StorageFormat.Kind, Is.EqualTo(StorageKind.Scalar));
            Assert.That(set[0].Length, Is.EqualTo(1));
            Assert.That(set[0].Capacity, Is.EqualTo(1));
            Assert.That(set[0].ByteLength, Is.EqualTo(contracts[0].StorageFormat.ElementSize));

            Assert.That(set[1].StableId, Is.EqualTo(FixedFieldId));
            Assert.That(set[1].Slot.Index, Is.EqualTo(1));
            Assert.That(set[1].StorageFormat.Kind, Is.EqualTo(StorageKind.NativeArray));
            Assert.That(set[1].Length, Is.EqualTo(8));
            Assert.That(set[1].Capacity, Is.EqualTo(8));
            Assert.That(set[1].ByteLength, Is.EqualTo(contracts[1].StorageFormat.ElementSize * 8L));
            Assert.That(set[1].ByteCapacity, Is.EqualTo(contracts[1].StorageFormat.ElementSize * 8L));
        }

        [Test]
        public void Resolve_MatchFieldLength_SourceDeclaredAfterTarget_ResolvesFromLaterSource()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.match.later.source"),
                CreateIntContract(
                    MatchTargetFieldId,
                    StorageKind.NativeArray,
                    LengthShape.MatchFieldLength(MatchSourceFieldId),
                    "shape.match.target"),
                CreateIntContract(
                    MatchSourceFieldId,
                    StorageKind.NativeArray,
                    LengthShape.Fixed(7),
                    "shape.match.source"));

            var set = AtlasShapeResolver.Resolve(contracts);

            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set[0].StableId, Is.EqualTo(MatchTargetFieldId));
            Assert.That(set[0].Length, Is.EqualTo(7));
            Assert.That(set[0].Capacity, Is.EqualTo(7));

            Assert.That(set[1].StableId, Is.EqualTo(MatchSourceFieldId));
            Assert.That(set[1].Length, Is.EqualTo(7));
            Assert.That(set[1].Capacity, Is.EqualTo(7));
        }

        [Test]
        public void Resolve_CapacityFromField_ForGrowableStorage_UsesZeroLengthAndDerivedCapacity()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.capacity.growable"),
                CreateIntContract(
                    CapacityTargetFieldId,
                    StorageKind.NativeList,
                    LengthShape.CapacityFromField(
                        CapacitySourceFieldId,
                        multiplier: 1.5f,
                        padding: 2),
                    "shape.capacity.target",
                    AtlasFieldFlags.Resizable),
                CreateIntContract(
                    CapacitySourceFieldId,
                    StorageKind.NativeArray,
                    LengthShape.Fixed(10),
                    "shape.capacity.source"));

            var set = AtlasShapeResolver.Resolve(contracts);

            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set[0].StableId, Is.EqualTo(CapacityTargetFieldId));
            Assert.That(set[0].StorageFormat.Kind, Is.EqualTo(StorageKind.NativeList));
            Assert.That(set[0].Length, Is.EqualTo(0));
            Assert.That(set[0].Capacity, Is.EqualTo(17));
            Assert.That(set[0].HasCapacitySlack, Is.True);
            Assert.That(set.HasCapacitySlack, Is.True);
            Assert.That(set[0].ByteLength, Is.EqualTo(0L));
            Assert.That(set[0].ByteCapacity, Is.EqualTo(contracts[0].StorageFormat.ElementSize * 17L));
        }

        [Test]
        public void Resolve_CapacityFromField_ForFixedStorage_UsesDerivedCapacityAsLength()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.capacity.fixed"),
                CreateIntContract(
                    CapacityTargetFieldId,
                    StorageKind.NativeArray,
                    LengthShape.CapacityFromField(
                        CapacitySourceFieldId,
                        multiplier: 2.0f,
                        padding: 3),
                    "shape.capacity.target"),
                CreateIntContract(
                    CapacitySourceFieldId,
                    StorageKind.NativeArray,
                    LengthShape.Fixed(5),
                    "shape.capacity.source"));

            var set = AtlasShapeResolver.Resolve(contracts);

            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set[0].StorageFormat.Kind, Is.EqualTo(StorageKind.NativeArray));
            Assert.That(set[0].Length, Is.EqualTo(13));
            Assert.That(set[0].Capacity, Is.EqualTo(13));
            Assert.That(set[0].HasCapacitySlack, Is.False);
        }

        [Test]
        public void Resolve_QueryCount_ThrowsNotSupported()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.query.unsupported"),
                CreateIntContract(
                    DynamicFieldId,
                    StorageKind.NativeArray,
                    LengthShape.QueryCount(new FixedString64Bytes("ecs.query.units")),
                    "shape.query"));

            var exception = Assert.Throws<NotSupportedException>(
                () => AtlasShapeResolver.Resolve(contracts));

            Assert.That(exception.Message, Does.Contain("cannot be resolved from a Contract table alone"));
            Assert.That(exception.Message, Does.Contain("QueryCount"));
        }

        [Test]
        public void Resolve_ChunkCount_ThrowsNotSupported()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.chunk.unsupported"),
                CreateIntContract(
                    DynamicFieldId,
                    StorageKind.NativeArray,
                    LengthShape.ChunkCount(new FixedString64Bytes("ecs.query.chunks")),
                    "shape.chunk"));

            var exception = Assert.Throws<NotSupportedException>(
                () => AtlasShapeResolver.Resolve(contracts));

            Assert.That(exception.Message, Does.Contain("cannot be resolved from a Contract table alone"));
            Assert.That(exception.Message, Does.Contain("ChunkCount"));
        }

        [Test]
        public void Resolve_PrefixSumPayload_ThrowsNotSupported()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.prefix.unsupported"),
                CreateIntContract(
                    DynamicFieldId,
                    StorageKind.NativeArray,
                    LengthShape.PrefixSumPayload(CapacitySourceFieldId),
                    "shape.prefix.payload"),
                CreateIntContract(
                    CapacitySourceFieldId,
                    StorageKind.NativeArray,
                    LengthShape.Fixed(4),
                    "shape.prefix.source"));

            var exception = Assert.Throws<NotSupportedException>(
                () => AtlasShapeResolver.Resolve(contracts));

            Assert.That(exception.Message, Does.Contain("cannot be resolved from a Contract table alone"));
            Assert.That(exception.Message, Does.Contain("PrefixSumPayload"));
        }

        [Test]
        public void Resolve_External_ThrowsNotSupported()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.external.unsupported"),
                CreateIntContract(
                    DynamicFieldId,
                    StorageKind.External,
                    LengthShape.External(new FixedString64Bytes("external.height.buffer")),
                    "shape.external",
                    flags: AtlasFieldFlags.AllowsExternalAlias,
                    role: AtlasFieldRole.External,
                    ownership: OwnershipPolicy.Borrowed,
                    lifetime: LifetimePolicy.External));

            var exception = Assert.Throws<NotSupportedException>(
                () => AtlasShapeResolver.Resolve(contracts));

            Assert.That(exception.Message, Does.Contain("cannot be resolved from a Contract table alone"));
            Assert.That(exception.Message, Does.Contain("External"));
        }

        [Test]
        public void Resolve_FieldRelativeCycle_ThrowsInvalidOperationException()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.cycle"),
                CreateIntContract(
                    CycleAFieldId,
                    StorageKind.NativeArray,
                    LengthShape.MatchFieldLength(CycleBFieldId),
                    "shape.cycle.a"),
                CreateIntContract(
                    CycleBFieldId,
                    StorageKind.NativeArray,
                    LengthShape.MatchFieldLength(CycleAFieldId),
                    "shape.cycle.b"));

            var exception = Assert.Throws<InvalidOperationException>(
                () => AtlasShapeResolver.Resolve(contracts));

            Assert.That(exception.Message, Does.Contain("cyclic"));
            Assert.That(exception.Message, Does.Contain("shape.cycle"));
        }

        [Test]
        public void CanResolveFromContractTableOnly_ReturnsExpectedSupportMatrix()
        {
            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.Scalar),
                Is.True);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.Fixed),
                Is.True);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.MatchFieldLength),
                Is.True);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.CapacityFromField),
                Is.True);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.QueryCount),
                Is.False);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.ChunkCount),
                Is.False);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.PrefixSumPayload),
                Is.False);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.External),
                Is.False);

            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(LengthShapeKind.None),
                Is.False);
        }

        private static AtlasContract CreateIntContract(
            StableDataId stableId,
            StorageKind storageKind,
            LengthShape lengthShape,
            string debugName,
            AtlasFieldFlags flags = AtlasFieldFlags.None,
            AtlasFieldRole role = AtlasFieldRole.Canonical,
            OwnershipPolicy ownership = OwnershipPolicy.AtlasOwned,
            LifetimePolicy lifetime = LifetimePolicy.Frame)
        {
            return AtlasContractFactory.Create<int>(
                stableId,
                role,
                storageKind,
                ownership,
                lifetime,
                lengthShape,
                flags,
                HashParticipation.Default,
                new FixedString64Bytes(debugName));
        }
    }
}