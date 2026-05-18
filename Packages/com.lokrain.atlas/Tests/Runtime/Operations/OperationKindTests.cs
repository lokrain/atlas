#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Operations.Tests
{
    public sealed class OperationKindTests
    {
        [Test]
        public void Constructor_WithValidSymbol_CreatesOperationKind()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.operation_kind.landmass");

            var operationKind = new OperationKind(symbol);

            Assert.That(operationKind.Symbol, Is.SameAs(symbol));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new OperationKind(null!));
        }

        [TestCase("lokrain.atlas.tests.operation_kind.landmass")]
        [TestCase("lokrain.atlas.tests.operation_kind.climate")]
        public void Create_WithValidSymbol_ReturnsOperationKind(string symbol)
        {
            OperationKind operationKind = OperationKind.Create(symbol);

            Assert.That(operationKind.Symbol.Value, Is.EqualTo(symbol));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Landmass")]
        [TestCase("0lokrain.atlas.tests.operation_kind.landmass")]
        [TestCase("lokrain atlas tests operation kind landmass")]
        public void Create_WithInvalidSymbol_ThrowsArgumentException(string? symbol)
        {
            Assert.Throws<ArgumentException>(
                () => OperationKind.Create(symbol));
        }

        [TestCase("lokrain.atlas.tests.operation_kind.landmass")]
        [TestCase("lokrain.atlas.tests.operation_kind.climate")]
        public void TryCreate_WithValidSymbol_ReturnsTrueAndOperationKind(string symbol)
        {
            bool succeeded = OperationKind.TryCreate(symbol, out OperationKind? operationKind);

            Assert.That(succeeded, Is.True);
            Assert.That(operationKind, Is.Not.Null);
            Assert.That(operationKind!.Symbol.Value, Is.EqualTo(symbol));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Landmass")]
        [TestCase("lokrain atlas tests operation kind landmass")]
        public void TryCreate_WithInvalidSymbol_ReturnsFalseAndNull(string? symbol)
        {
            bool succeeded = OperationKind.TryCreate(symbol, out OperationKind? operationKind);

            Assert.That(succeeded, Is.False);
            Assert.That(operationKind, Is.Null);
        }

        [TestCase("lokrain.atlas.tests.operation_kind.landmass")]
        [TestCase("lokrain.atlas.tests.operation_kind.climate")]
        public void IsValid_WithValidSymbol_ReturnsTrue(string symbol)
        {
            bool isValid = OperationKind.IsValid(symbol);

            Assert.That(isValid, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Landmass")]
        [TestCase("lokrain atlas tests operation kind landmass")]
        public void IsValid_WithInvalidSymbol_ReturnsFalse(string? symbol)
        {
            bool isValid = OperationKind.IsValid(symbol);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            OperationKind left = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");
            OperationKind right = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            OperationKind left = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");
            OperationKind right = OperationKind.Create("lokrain.atlas.tests.operation_kind.climate");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            OperationKind operationKind = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");

            Assert.That(operationKind.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            OperationKind operationKind = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");

            Assert.That(operationKind.Equals("OperationKind"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            OperationKind? left = null;
            OperationKind? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            OperationKind? left = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");
            OperationKind? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsSymbolValue()
        {
            OperationKind operationKind = OperationKind.Create("lokrain.atlas.tests.operation_kind.landmass");

            string value = operationKind.ToString();

            Assert.That(value, Is.EqualTo("lokrain.atlas.tests.operation_kind.landmass"));
        }
    }
}