#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Core.Tests
{
    public sealed class SymbolTests
    {
        [TestCase("a")]
        [TestCase("a0")]
        [TestCase("abc")]
        [TestCase("abc_def")]
        [TestCase("abc-def")]
        [TestCase("abc_def-123")]
        [TestCase("a.b")]
        [TestCase("lokrain.atlas.landmass.operation.extract_main_continent")]
        public void Create_WithValidSymbol_ReturnsSymbol(string value)
        {
            Symbol symbol = Symbol.Create(value);

            Assert.That(symbol.Value, Is.EqualTo(value));
        }

        [Test]
        public void Create_WithMaximumLengthSymbol_ReturnsSymbol()
        {
            string value = "a" + new string('b', Symbol.MaxLength - 1);

            Symbol symbol = Symbol.Create(value);

            Assert.That(symbol.Value, Is.EqualTo(value));
            Assert.That(symbol.Value.Length, Is.EqualTo(Symbol.MaxLength));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("A")]
        [TestCase("0a")]
        [TestCase("_a")]
        [TestCase("-a")]
        [TestCase(".a")]
        [TestCase("a.")]
        [TestCase("a..b")]
        [TestCase("a._b")]
        [TestCase("a.-b")]
        [TestCase("a.0b")]
        [TestCase("a_")]
        [TestCase("a-")]
        [TestCase("a_.b")]
        [TestCase("a-.b")]
        [TestCase("a b")]
        [TestCase("a/b")]
        [TestCase("a:b")]
        [TestCase("aBb")]
        [TestCase("aáb")]
        [TestCase("a\nb")]
        [TestCase("a\tb")]
        public void Create_WithInvalidSymbol_ThrowsArgumentException(string? value)
        {
            Assert.Throws<ArgumentException>(() => Symbol.Create(value));
        }

        [Test]
        public void Create_WithTooLongSymbol_ThrowsArgumentException()
        {
            string value = "a" + new string('b', Symbol.MaxLength);

            Assert.Throws<ArgumentException>(() => Symbol.Create(value));
        }

        [TestCase("a")]
        [TestCase("a.b")]
        [TestCase("abc_def-123")]
        public void TryCreate_WithValidSymbol_ReturnsTrueAndSymbol(string value)
        {
            bool succeeded = Symbol.TryCreate(value, out Symbol? symbol);

            Assert.That(succeeded, Is.True);
            Assert.That(symbol, Is.Not.Null);
            Assert.That(symbol!.Value, Is.EqualTo(value));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("A")]
        [TestCase("a.")]
        [TestCase("a b")]
        public void TryCreate_WithInvalidSymbol_ReturnsFalseAndNull(string? value)
        {
            bool succeeded = Symbol.TryCreate(value, out Symbol? symbol);

            Assert.That(succeeded, Is.False);
            Assert.That(symbol, Is.Null);
        }

        [TestCase("a")]
        [TestCase("a.b")]
        [TestCase("abc_def-123")]
        public void IsValid_WithValidSymbol_ReturnsTrue(string value)
        {
            bool isValid = Symbol.IsValid(value);

            Assert.That(isValid, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("A")]
        [TestCase("a.")]
        [TestCase("a b")]
        public void IsValid_WithInvalidSymbol_ReturnsFalse(string? value)
        {
            bool isValid = Symbol.IsValid(value);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            Symbol left = Symbol.Create("lokrain.atlas.symbol");
            Symbol right = Symbol.Create("lokrain.atlas.symbol");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            Symbol left = Symbol.Create("lokrain.atlas.symbol_a");
            Symbol right = Symbol.Create("lokrain.atlas.symbol_b");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            Symbol? left = null;
            Symbol? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            Symbol? left = Symbol.Create("lokrain.atlas.symbol");
            Symbol? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void CompareTo_WithLowerOrdinalValue_ReturnsNegativeValue()
        {
            Symbol left = Symbol.Create("a");
            Symbol right = Symbol.Create("b");

            Assert.That(left.CompareTo(right), Is.LessThan(0));
        }

        [Test]
        public void CompareTo_WithEqualValue_ReturnsZero()
        {
            Symbol left = Symbol.Create("lokrain.atlas.symbol");
            Symbol right = Symbol.Create("lokrain.atlas.symbol");

            Assert.That(left.CompareTo(right), Is.EqualTo(0));
        }

        [Test]
        public void CompareTo_WithGreaterOrdinalValue_ReturnsPositiveValue()
        {
            Symbol left = Symbol.Create("b");
            Symbol right = Symbol.Create("a");

            Assert.That(left.CompareTo(right), Is.GreaterThan(0));
        }

        [Test]
        public void CompareTo_WithNull_ReturnsPositiveValue()
        {
            Symbol symbol = Symbol.Create("a");

            Assert.That(symbol.CompareTo(null), Is.GreaterThan(0));
        }

        [Test]
        public void ToString_ReturnsValue()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.symbol");

            string value = symbol.ToString();

            Assert.That(value, Is.EqualTo("lokrain.atlas.symbol"));
        }
    }
}