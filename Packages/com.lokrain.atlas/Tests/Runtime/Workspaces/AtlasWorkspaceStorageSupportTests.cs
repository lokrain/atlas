// Packages/com.lokrain.atlas/Tests/Runtime/Workspaces/AtlasWorkspaceStorageSupportTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces.Tests
//
// Purpose
// - Verify the current workspace byte-block backend has an explicit storage support fence.
// - Verify unsupported storage vocabulary is rejected before workspace allocation.
// - Prevent declared-but-unimplemented storage families from being packed as raw byte ranges.

using System;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Workspaces.Tests
{
    public sealed class AtlasWorkspaceStorageSupportTests
    {
        private static readonly StableDataId FieldId =
            new(0xA100_0000_0000_0001UL, 0xA200_0000_0000_0001UL, 1);

        [Test]
        public void SupportsStorageKind_Scalar_ReturnsTrue()
        {
            Assert.That(
                AtlasWorkspaceStorageSupport.SupportsStorageKind(StorageKind.Scalar),
                Is.True);
        }

        [Test]
        public void SupportsStorageKind_NativeArray_ReturnsTrue()
        {
            Assert.That(
                AtlasWorkspaceStorageSupport.SupportsStorageKind(StorageKind.NativeArray),
                Is.True);
        }

        [Test]
        public void RequiresExternalBinding_External_ReturnsTrue()
        {
            Assert.That(
                AtlasWorkspaceStorageSupport.RequiresExternalBinding(StorageKind.External),
                Is.True);
        }

        [TestCase(StorageKind.Scalar)]
        [TestCase(StorageKind.NativeArray)]
        [TestCase(StorageKind.NativeList)]
        [TestCase(StorageKind.Blob)]
        public void RequiresExternalBinding_NonExternalStorageKind_ReturnsFalse(
            StorageKind storageKind)
        {
            Assert.That(
                AtlasWorkspaceStorageSupport.RequiresExternalBinding(storageKind),
                Is.False);
        }

        [TestCase(StorageKind.None)]
        [TestCase(StorageKind.NativeList)]
        [TestCase(StorageKind.UnsafeList)]
        [TestCase(StorageKind.NativeStream)]
        [TestCase(StorageKind.NativeParallelHashMap)]
        [TestCase(StorageKind.Blob)]
        [TestCase(StorageKind.External)]
        public void SupportsStorageKind_UnsupportedStorageKind_ReturnsFalse(
            StorageKind storageKind)
        {
            Assert.That(
                AtlasWorkspaceStorageSupport.SupportsStorageKind(storageKind),
                Is.False);
        }

        [Test]
        public void SupportsStorageFormat_ConcreteNativeArray_ReturnsTrue()
        {
            var storageFormat = StorageFormat.Create<int>(StorageKind.NativeArray);

            Assert.That(
                AtlasWorkspaceStorageSupport.SupportsStorageFormat(storageFormat),
                Is.True);
        }

        [Test]
        public void SupportsStorageFormat_NonConcreteFormat_ReturnsFalse()
        {
            Assert.That(
                AtlasWorkspaceStorageSupport.SupportsStorageFormat(default),
                Is.False);
        }

        [Test]
        public void ValidateStorageKindOrThrow_External_ThrowsExplicitExternalBindingMessage()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
                AtlasWorkspaceStorageSupport.ValidateStorageKindOrThrow(
                    StorageKind.External,
                    new FixedString64Bytes("workspace.external.field")));

            Assert.That(exception.Message, Does.Contain("workspace.external.field"));
            Assert.That(exception.Message, Does.Contain(StorageKind.External.ToString()));
            Assert.That(exception.Message, Does.Contain("valid contract vocabulary"));
            Assert.That(exception.Message, Does.Contain("explicit external binding model"));
        }

        [Test]
        public void ValidateStorageFormatOrThrow_External_ThrowsExplicitExternalBindingMessage()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
                AtlasWorkspaceStorageSupport.ValidateStorageFormatOrThrow(
                    StorageFormat.External<int>(),
                    new FixedString64Bytes("workspace.external.field")));

            Assert.That(exception.Message, Does.Contain("workspace.external.field"));
            Assert.That(exception.Message, Does.Contain(StorageKind.External.ToString()));
            Assert.That(exception.Message, Does.Contain("explicit external binding model"));
        }

        [TestCase(StorageKind.NativeList)]
        [TestCase(StorageKind.UnsafeList)]
        [TestCase(StorageKind.NativeStream)]
        [TestCase(StorageKind.NativeParallelHashMap)]
        [TestCase(StorageKind.Blob)]
        public void ValidateCompilableOrThrow_UnsupportedAtlasOwnedStorageKind_ThrowsNotSupportedException(
            StorageKind storageKind)
        {
            var shapes = CreateResolvedShapes(storageKind);

            var exception = Assert.Throws<NotSupportedException>(() =>
                AtlasWorkspaceLayoutCompiler.ValidateCompilableOrThrow(shapes));

            Assert.That(exception.Message, Does.Contain(storageKind.ToString()));
            Assert.That(exception.Message, Does.Contain("supports only Scalar and NativeArray"));
        }

        [Test]
        public void CanCompile_UnsupportedAtlasOwnedStorageKind_ReturnsFalse()
        {
            var shapes = CreateResolvedShapes(StorageKind.NativeList);

            Assert.That(
                AtlasWorkspaceLayoutCompiler.CanCompile(shapes),
                Is.False);
        }

        [Test]
        public void ValidateCompilableOrThrow_ExternalStorageShape_ThrowsExplicitExternalBindingMessage()
        {
            var shapes = CreateExternalResolvedShapes();

            var exception = Assert.Throws<NotSupportedException>(() =>
                AtlasWorkspaceLayoutCompiler.ValidateCompilableOrThrow(shapes));

            Assert.That(exception.Message, Does.Contain("workspace.external.field"));
            Assert.That(exception.Message, Does.Contain(StorageKind.External.ToString()));
            Assert.That(exception.Message, Does.Contain("explicit external binding model"));
            Assert.That(exception.Message, Does.Not.Contain("only supports Atlas-owned memory"));
        }

        [Test]
        public void CanCompile_ExternalStorageShape_ReturnsFalse()
        {
            var shapes = CreateExternalResolvedShapes();

            Assert.That(
                AtlasWorkspaceLayoutCompiler.CanCompile(shapes),
                Is.False);
        }

        [Test]
        public void Compile_SupportedNativeArrayStorageKind_ReturnsLayout()
        {
            var shapes = CreateResolvedShapes(StorageKind.NativeArray);

            var layout = AtlasWorkspaceLayoutCompiler.Compile(shapes);

            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.Count, Is.EqualTo(1));
            Assert.That(layout.StorageBlockCount, Is.EqualTo(1));
        }

        [Test]
        public void Compile_SupportedScalarStorageKind_ReturnsLayout()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("workspace.storage.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.Scalar,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.Scalar(new FixedString64Bytes("workspace.scalar")),
                    LengthShape.Scalar(),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("workspace.scalar.field")));

            var shapes = AtlasShapeResolver.Resolve(contracts);
            var layout = AtlasWorkspaceLayoutCompiler.Compile(shapes);

            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.Count, Is.EqualTo(1));
            Assert.That(layout.StorageBlockCount, Is.EqualTo(1));
        }

        [Test]
        public void ValidateCompilableOrThrow_NonAtlasOwnedStorage_ThrowsNotSupportedExceptionBeforeAllocation()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("workspace.storage.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    StorageKind.NativeArray,
                    OwnershipPolicy.Borrowed,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("workspace.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("workspace.borrowed.field")));

            var shapes = AtlasShapeResolver.Resolve(contracts);

            var exception = Assert.Throws<NotSupportedException>(() =>
                AtlasWorkspaceLayoutCompiler.ValidateCompilableOrThrow(shapes));

            Assert.That(exception.Message, Does.Contain(OwnershipPolicy.Borrowed.ToString()));
            Assert.That(exception.Message, Does.Contain("only supports Atlas-owned memory"));
        }

        private static AtlasResolvedShapeSet CreateExternalResolvedShapes()
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("workspace.storage.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.External,
                    StorageKind.External,
                    OwnershipPolicy.ExternalOwned,
                    LifetimePolicy.External,
                    AtlasShapeDomain.External(new FixedString64Bytes("workspace.external")),
                    LengthShape.External(new FixedString64Bytes("workspace.external.length")),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("workspace.external.field")));

            var contract = contracts[0];

            var shape = AtlasResolvedShape.Create(
                contract,
                length: 4,
                capacity: 4);

            return AtlasResolvedShapeSet.Create(
                contracts,
                shape);
        }

        private static AtlasResolvedShapeSet CreateResolvedShapes(
            StorageKind storageKind)
        {
            var contracts = AtlasContractTable.Create(
                new FixedString64Bytes("workspace.storage.contracts"),
                AtlasContractFactory.Create<int>(
                    FieldId,
                    AtlasFieldRole.Canonical,
                    storageKind,
                    OwnershipPolicy.AtlasOwned,
                    LifetimePolicy.Frame,
                    AtlasShapeDomain.LinearRows(new FixedString64Bytes("workspace.rows")),
                    LengthShape.Fixed(4),
                    AtlasFieldFlags.None,
                    HashParticipation.Default,
                    new FixedString64Bytes("workspace.field")));

            return AtlasShapeResolver.Resolve(contracts);
        }
    }
}
