// Packages/com.lokrain.atlas/Tests/Runtime/Compilation/AtlasShapeResolverTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation.Tests
//
// Purpose
// - Verify AtlasShapeResolver resolves Contract-table-only shapes deterministically.
// - Verify resolved shapes preserve Contract identity, slot, storage format, and shape domain.
// - Verify field-relative shapes resolve regardless of Contract-table source order.
// - Verify resolver-input-dependent shapes are rejected until explicit resolver context exists.
// - Verify invalid field-relative and shape-domain metadata fails at the earliest owning boundary.

using System;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Pipelines;
using Lokrain.Atlas.Stages;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation.Tests
{
    public sealed class AtlasShapeResolverTests
    {
        private static readonly StableDataId ScalarFieldId =
            new(0xA100_0000_0000_0001UL, 0xB100_0000_0000_0001UL, 1);

        private static readonly StableDataId FixedFieldId =
            new(0xA100_0000_0000_0002UL, 0xB100_0000_0000_0002UL, 1);

        private static readonly StableDataId MatchedFieldId =
            new(0xA100_0000_0000_0003UL, 0xB100_0000_0000_0003UL, 1);

        private static readonly StableDataId CapacityFieldId =
            new(0xA100_0000_0000_0004UL, 0xB100_0000_0000_0004UL, 1);

        private static readonly StableDataId MissingSourceFieldId =
            new(0xA100_0000_0000_0005UL, 0xB100_0000_0000_0005UL, 1);

        private static readonly StableDataId CycleLeftFieldId =
            new(0xA100_0000_0000_0006UL, 0xB100_0000_0000_0006UL, 1);

        private static readonly StableDataId CycleRightFieldId =
            new(0xA100_0000_0000_0007UL, 0xB100_0000_0000_0007UL, 1);

        private static readonly StableDataId PrefixSourceFieldId =
            new(0xA100_0000_0000_0008UL, 0xB100_0000_0000_0008UL, 1);

        private static readonly AtlasOperationId OperationId =
            new(0xC100_0000_0000_0001UL, 0xD100_0000_0000_0001UL, 1);

        private static readonly AtlasStageId StageId =
            new(0xE100_0000_0000_0001UL, 0xF100_0000_0000_0001UL, 1);

        private static readonly AtlasPipelineId PipelineId =
            new(0x1100_0000_0000_0001UL, 0x1200_0000_0000_0001UL, 1);

        [Test]
        public void Resolve_ScalarContract_ResolvesLengthAndCapacityOneAndPreservesMetadata()
        {
            var contracts = CreateContractTable(
                CreateScalarContract(
                    ScalarFieldId,
                    "shape.scalar"));

            var shapes = AtlasShapeResolver.Resolve(contracts);

            Assert.That(shapes.Count, Is.EqualTo(1));
            Assert.That(shapes.Name.ToString(), Is.EqualTo("shape.tests.contracts"));
            Assert.That(shapes.Contracts, Is.SameAs(contracts));

            var shape = shapes[0];

            Assert.That(shape.IsResolved, Is.True);
            Assert.That(shape.StableId, Is.EqualTo(ScalarFieldId));
            Assert.That(shape.Slot.Index, Is.EqualTo(0));
            Assert.That(shape.Role, Is.EqualTo(AtlasFieldRole.Canonical));
            Assert.That(shape.StorageFormat.Kind, Is.EqualTo(StorageKind.Scalar));
            Assert.That(shape.ShapeDomain.Kind, Is.EqualTo(AtlasShapeDomainKind.Scalar));
            Assert.That(shape.DeclaredShape.Kind, Is.EqualTo(LengthShapeKind.Scalar));
            Assert.That(shape.DebugName.ToString(), Is.EqualTo("shape.scalar"));
            Assert.That(shape.Length, Is.EqualTo(1));
            Assert.That(shape.Capacity, Is.EqualTo(1));
            Assert.That(shape.ByteLength, Is.EqualTo(sizeof(int)));
            Assert.That(shape.ByteCapacity, Is.EqualTo(sizeof(int)));
            Assert.That(shape.HasCapacitySlack, Is.False);
        }

        [Test]
        public void Resolve_FixedContract_ResolvesLengthCapacityAndByteCounts()
        {
            var contracts = CreateContractTable(
                CreateNativeArrayContract(
                    FixedFieldId,
                    "shape.fixed",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.rows")),
                    LengthShape.Fixed(7)));

            var shapes = AtlasShapeResolver.Resolve(contracts);

            Assert.That(shapes.Count, Is.EqualTo(1));

            var shape = shapes[0];

            Assert.That(shape.StableId, Is.EqualTo(FixedFieldId));
            Assert.That(shape.Slot.Index, Is.EqualTo(0));
            Assert.That(shape.ShapeDomain.Kind, Is.EqualTo(AtlasShapeDomainKind.LinearRows));
            Assert.That(shape.DeclaredShape.Kind, Is.EqualTo(LengthShapeKind.Fixed));
            Assert.That(shape.Length, Is.EqualTo(7));
            Assert.That(shape.Capacity, Is.EqualTo(7));
            Assert.That(shape.ByteLength, Is.EqualTo(7 * sizeof(int)));
            Assert.That(shape.ByteCapacity, Is.EqualTo(7 * sizeof(int)));
        }

        [Test]
        public void Resolve_MatchFieldLength_SourceOrderDoesNotMatter()
        {
            var contracts = CreateContractTable(
                CreateNativeArrayContract(
                    MatchedFieldId,
                    "shape.matched",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.matched.rows")),
                    LengthShape.MatchFieldLength(FixedFieldId)),
                CreateNativeArrayContract(
                    FixedFieldId,
                    "shape.source",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.source.rows")),
                    LengthShape.Fixed(11)));

            var shapes = AtlasShapeResolver.Resolve(contracts);

            var source = shapes.GetRequiredShape(FixedFieldId);
            var matched = shapes.GetRequiredShape(MatchedFieldId);

            Assert.That(source.Length, Is.EqualTo(11));
            Assert.That(source.Capacity, Is.EqualTo(11));
            Assert.That(matched.Length, Is.EqualTo(source.Length));
            Assert.That(matched.Capacity, Is.EqualTo(source.Length));
            Assert.That(matched.DeclaredShape.Kind, Is.EqualTo(LengthShapeKind.MatchFieldLength));
            Assert.That(matched.Slot.Index, Is.EqualTo(0));
            Assert.That(source.Slot.Index, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_CapacityFromField_AppliesCeilingMultiplierAndPadding()
        {
            var contracts = CreateContractTable(
                CreateNativeArrayContract(
                    FixedFieldId,
                    "shape.source",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.source.rows")),
                    LengthShape.Fixed(5)),
                CreateNativeArrayContract(
                    CapacityFieldId,
                    "shape.capacity",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.capacity.rows")),
                    LengthShape.CapacityFromField(
                        FixedFieldId,
                        multiplierNumerator: 3,
                        multiplierDenominator: 2,
                        padding: 4)));

            var shapes = AtlasShapeResolver.Resolve(contracts);

            var capacity = shapes.GetRequiredShape(CapacityFieldId);

            Assert.That(capacity.DeclaredShape.Kind, Is.EqualTo(LengthShapeKind.CapacityFromField));
            Assert.That(capacity.Length, Is.EqualTo(12));
            Assert.That(capacity.Capacity, Is.EqualTo(12));
            Assert.That(capacity.ByteLength, Is.EqualTo(12 * sizeof(int)));
            Assert.That(capacity.ByteCapacity, Is.EqualTo(12 * sizeof(int)));
        }

        [Test]
        public void CreateContractTable_MissingFieldRelativeSource_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateContractTable(
                    CreateNativeArrayContract(
                        MatchedFieldId,
                        "shape.missing.source.target",
                        AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.target.rows")),
                        LengthShape.MatchFieldLength(MissingSourceFieldId))));
        }

        [Test]
        public void Resolve_CyclicFieldRelativeDependency_ThrowsInvalidOperationException()
        {
            var contracts = CreateContractTable(
                CreateNativeArrayContract(
                    CycleLeftFieldId,
                    "shape.cycle.left",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.cycle.left.rows")),
                    LengthShape.MatchFieldLength(CycleRightFieldId)),
                CreateNativeArrayContract(
                    CycleRightFieldId,
                    "shape.cycle.right",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.cycle.right.rows")),
                    LengthShape.MatchFieldLength(CycleLeftFieldId)));

            Assert.Throws<InvalidOperationException>(() =>
                AtlasShapeResolver.Resolve(contracts));
        }

        [TestCase(LengthShapeKind.QueryCount)]
        [TestCase(LengthShapeKind.ChunkCount)]
        [TestCase(LengthShapeKind.PrefixSumPayload)]
        [TestCase(LengthShapeKind.External)]
        public void Resolve_ResolverInputDependentShape_ThrowsNotSupportedException(
            LengthShapeKind kind)
        {
            var contracts = CreateUnsupportedResolverInputContractTable(kind);

            Assert.Throws<NotSupportedException>(() =>
                AtlasShapeResolver.Resolve(contracts));
        }

        [TestCase(LengthShapeKind.Scalar, true)]
        [TestCase(LengthShapeKind.Fixed, true)]
        [TestCase(LengthShapeKind.MatchFieldLength, true)]
        [TestCase(LengthShapeKind.CapacityFromField, true)]
        [TestCase(LengthShapeKind.QueryCount, false)]
        [TestCase(LengthShapeKind.ChunkCount, false)]
        [TestCase(LengthShapeKind.PrefixSumPayload, false)]
        [TestCase(LengthShapeKind.External, false)]
        [TestCase(LengthShapeKind.None, false)]
        public void CanResolveFromContractTableOnly_ReturnsExpectedValue(
            LengthShapeKind kind,
            bool expected)
        {
            Assert.That(
                AtlasShapeResolver.CanResolveFromContractTableOnly(kind),
                Is.EqualTo(expected));
        }

        [Test]
        public void ValidateResolvableFromContractTableOnlyOrThrow_ResolvableTable_DoesNotThrow()
        {
            var contracts = CreateContractTable(
                CreateScalarContract(
                    ScalarFieldId,
                    "shape.scalar"),
                CreateNativeArrayContract(
                    FixedFieldId,
                    "shape.fixed",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.rows")),
                    LengthShape.Fixed(4)));

            Assert.DoesNotThrow(() =>
                AtlasShapeResolver.ValidateResolvableFromContractTableOnlyOrThrow(contracts));
        }

        [Test]
        public void ValidateResolvableFromContractTableOnlyOrThrow_UnsupportedTable_ThrowsNotSupportedException()
        {
            var contracts = CreateContractTable(
                CreateNativeArrayContract(
                    FixedFieldId,
                    "shape.query",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.query.rows")),
                    LengthShape.QueryCount(new FixedString64Bytes("shape.query.count"))));

            Assert.Throws<NotSupportedException>(() =>
                AtlasShapeResolver.ValidateResolvableFromContractTableOnlyOrThrow(contracts));
        }

        [Test]
        public void Resolve_CompiledPlan_UsesPlanContractTable()
        {
            var contracts = CreateContractTable(
                CreateNativeArrayContract(
                    FixedFieldId,
                    "shape.fixed",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.rows")),
                    LengthShape.Fixed(9)));

            var operation = AtlasOperationDefinition.Create(
                OperationId,
                new FixedString64Bytes("shape.operation"),
                AtlasOperationRole.Validation,
                AtlasOperationAccess.Create(
                    FixedFieldId,
                    AtlasOperationAccessMode.Read,
                    AtlasOperationAccessFlags.None,
                    AtlasWriteCoverage.None,
                    new FixedString64Bytes("shape.fixed")));

            var stage = AtlasStageDefinition.Create(
                StageId,
                new FixedString64Bytes("shape.stage"),
                operation);

            var pipeline = AtlasPipelineDefinition.Create(
                PipelineId,
                new FixedString64Bytes("shape.pipeline"),
                stage);

            var plan = AtlasPlanCompiler.Compile(
                pipeline,
                contracts);

            var shapes = AtlasShapeResolver.Resolve(plan);

            Assert.That(shapes.Contracts, Is.SameAs(contracts));
            Assert.That(shapes.Name, Is.EqualTo(plan.DebugName));
            Assert.That(shapes.Count, Is.EqualTo(1));
            Assert.That(shapes.GetRequiredShape(FixedFieldId).Length, Is.EqualTo(9));
        }

        [Test]
        public void CreateContract_ScalarLengthWithLinearRowsDomain_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateNativeArrayContract(
                    ScalarFieldId,
                    "shape.scalar.bad.domain",
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.rows")),
                    LengthShape.Scalar()));
        }

        private static AtlasContractTable CreateContractTable(
            params AtlasContract[] contracts)
        {
            return AtlasContractTable.Create(
                new FixedString64Bytes("shape.tests.contracts"),
                contracts);
        }

        private static AtlasContract CreateScalarContract(
            StableDataId fieldId,
            string debugName)
        {
            return AtlasContractFactory.Create<int>(
                fieldId,
                AtlasFieldRole.Canonical,
                StorageKind.Scalar,
                OwnershipPolicy.AtlasOwned,
                LifetimePolicy.Frame,
                AtlasShapeDomain.Scalar(new FixedString64Bytes("shape.scalar.domain")),
                LengthShape.Scalar(),
                AtlasFieldFlags.None,
                HashParticipation.Default,
                new FixedString64Bytes(debugName));
        }

        private static AtlasContract CreateNativeArrayContract(
            StableDataId fieldId,
            string debugName,
            AtlasShapeDomain shapeDomain,
            LengthShape lengthShape)
        {
            return AtlasContractFactory.Create<int>(
                fieldId,
                AtlasFieldRole.Canonical,
                StorageKind.NativeArray,
                OwnershipPolicy.AtlasOwned,
                LifetimePolicy.Frame,
                shapeDomain,
                lengthShape,
                AtlasFieldFlags.None,
                HashParticipation.Default,
                new FixedString64Bytes(debugName));
        }

        private static AtlasContractTable CreateUnsupportedResolverInputContractTable(
            LengthShapeKind kind)
        {
            if (kind == LengthShapeKind.PrefixSumPayload)
            {
                return CreateContractTable(
                    CreateNativeArrayContract(
                        PrefixSourceFieldId,
                        "shape.prefix.source",
                        AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.prefix.source.rows")),
                        LengthShape.Fixed(4)),
                    CreateUnsupportedResolverInputContract(kind));
            }

            return CreateContractTable(
                CreateUnsupportedResolverInputContract(kind));
        }

        private static AtlasContract CreateUnsupportedResolverInputContract(
            LengthShapeKind kind)
        {
            switch (kind)
            {
                case LengthShapeKind.QueryCount:
                    return CreateNativeArrayContract(
                        FixedFieldId,
                        "shape.query",
                        AtlasShapeDomain.LinearRows(new FixedString64Bytes("shape.query.rows")),
                        LengthShape.QueryCount(new FixedString64Bytes("shape.query.count")));

                case LengthShapeKind.ChunkCount:
                    return CreateNativeArrayContract(
                        FixedFieldId,
                        "shape.chunk",
                        AtlasShapeDomain.ChunkSet(new FixedString64Bytes("shape.chunk.set")),
                        LengthShape.ChunkCount(new FixedString64Bytes("shape.chunk.count")));

                case LengthShapeKind.PrefixSumPayload:
                    return AtlasContractFactory.Create<int>(
                        FixedFieldId,
                        AtlasFieldRole.Payload,
                        StorageKind.NativeArray,
                        OwnershipPolicy.AtlasOwned,
                        LifetimePolicy.Frame,
                        AtlasShapeDomain.PrefixSumPayload(
                            PrefixSourceFieldId,
                            new FixedString64Bytes("shape.prefix.payload")),
                        LengthShape.PrefixSumPayload(PrefixSourceFieldId),
                        AtlasFieldFlags.None,
                        HashParticipation.Default,
                        new FixedString64Bytes("shape.prefix.payload"));

                case LengthShapeKind.External:
                    return AtlasContractFactory.Create<int>(
                        FixedFieldId,
                        AtlasFieldRole.External,
                        StorageKind.External,
                        OwnershipPolicy.ExternalOwned,
                        LifetimePolicy.External,
                        AtlasShapeDomain.External(new FixedString64Bytes("shape.external")),
                        LengthShape.External(new FixedString64Bytes("shape.external.length")),
                        AtlasFieldFlags.None,
                        HashParticipation.Default,
                        new FixedString64Bytes("shape.external"));

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Unsupported resolver-input-dependent length-shape kind.");
            }
        }
    }
}
