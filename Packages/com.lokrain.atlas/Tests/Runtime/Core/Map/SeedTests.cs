#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Core.Map.Tests
{
    public sealed class SeedTests
    {
        [TestCase(0UL)]
        [TestCase(1UL)]
        [TestCase(123UL)]
        [TestCase(ulong.MaxValue)]
        public void Constructor_WithValue_CreatesSeed(ulong value)
        {
            var seed = new Seed(value);

            Assert.That(seed.Value, Is.EqualTo(value));
        }

        [Test]
        public void Zero_ReturnsSeedWithZeroValue()
        {
            Seed seed = Seed.Zero;

            Assert.That(seed.Value, Is.EqualTo(0UL));
        }

        [TestCase("0", 0UL)]
        [TestCase("1", 1UL)]
        [TestCase("123", 123UL)]
        [TestCase("18446744073709551615", ulong.MaxValue)]
        [TestCase("0x0", 0UL)]
        [TestCase("0x1", 1UL)]
        [TestCase("0x7B", 123UL)]
        [TestCase("0X7B", 123UL)]
        [TestCase("0xffffffffffffffff", ulong.MaxValue)]
        public void Parse_WithValidSeedText_ReturnsSeed(
            string value,
            ulong expectedValue)
        {
            Seed seed = Seed.Parse(value);

            Assert.That(seed.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Parse_WithLeadingAndTrailingWhitespace_ReturnsSeed()
        {
            Seed seed = Seed.Parse("  123  ");

            Assert.That(seed.Value, Is.EqualTo(123UL));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-1")]
        [TestCase("+1")]
        [TestCase("1.0")]
        [TestCase("abc")]
        [TestCase("0x")]
        [TestCase("0xg")]
        [TestCase("18446744073709551616")]
        [TestCase("0x10000000000000000")]
        public void Parse_WithInvalidSeedText_ThrowsArgumentException(string? value)
        {
            Assert.Throws<ArgumentException>(() => Seed.Parse(value));
        }

        [TestCase("0", 0UL)]
        [TestCase("123", 123UL)]
        [TestCase("0x7B", 123UL)]
        [TestCase("0X7B", 123UL)]
        [TestCase("18446744073709551615", ulong.MaxValue)]
        public void TryParse_WithValidSeedText_ReturnsTrueAndSeed(
            string value,
            ulong expectedValue)
        {
            bool succeeded = Seed.TryParse(value, out Seed seed);

            Assert.That(succeeded, Is.True);
            Assert.That(seed.Value, Is.EqualTo(expectedValue));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-1")]
        [TestCase("+1")]
        [TestCase("abc")]
        [TestCase("0x")]
        [TestCase("0xg")]
        [TestCase("18446744073709551616")]
        public void TryParse_WithInvalidSeedText_ReturnsFalseAndDefaultSeed(string? value)
        {
            bool succeeded = Seed.TryParse(value, out Seed seed);

            Assert.That(succeeded, Is.False);
            Assert.That(seed.Value, Is.EqualTo(0UL));
        }

        [Test]
        public void Derive_WithNumericSalt_ReturnsDeterministicSeed()
        {
            var seed = new Seed(123UL);

            Seed derived = seed.Derive(456UL);

            Assert.That(derived.Value, Is.EqualTo(10437168881846419345UL));
        }

        [Test]
        public void Derive_WithSameNumericSalt_ReturnsEqualSeeds()
        {
            var seed = new Seed(123UL);

            Seed left = seed.Derive(456UL);
            Seed right = seed.Derive(456UL);

            Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void Derive_WithDifferentNumericSalt_ReturnsDifferentSeeds()
        {
            var seed = new Seed(123UL);

            Seed left = seed.Derive(456UL);
            Seed right = seed.Derive(789UL);

            Assert.That(left, Is.Not.EqualTo(right));
        }

        [Test]
        public void Derive_WithTextSalt_ReturnsDeterministicSeed()
        {
            var seed = new Seed(123UL);

            Seed derived = seed.Derive("continental_landmass");

            Assert.That(derived.Value, Is.EqualTo(11709336295918592680UL));
        }

        [Test]
        public void Derive_WithSameTextSalt_ReturnsEqualSeeds()
        {
            var seed = new Seed(123UL);

            Seed left = seed.Derive("continental_landmass");
            Seed right = seed.Derive("continental_landmass");

            Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void Derive_WithDifferentTextSalt_ReturnsDifferentSeeds()
        {
            var seed = new Seed(123UL);

            Seed left = seed.Derive("continental_landmass");
            Seed right = seed.Derive("base_elevation");

            Assert.That(left, Is.Not.EqualTo(right));
        }

        [Test]
        public void Derive_WithNullTextSalt_ThrowsArgumentNullException()
        {
            var seed = new Seed(123UL);

            Assert.Throws<ArgumentNullException>(() => seed.Derive(null!));
        }

        [TestCase(0UL, "0x0000000000000000")]
        [TestCase(1UL, "0x0000000000000001")]
        [TestCase(123UL, "0x000000000000007B")]
        [TestCase(ulong.MaxValue, "0xFFFFFFFFFFFFFFFF")]
        public void ToHexString_ReturnsSixteenDigitUppercaseHexText(
            ulong value,
            string expectedText)
        {
            var seed = new Seed(value);

            string text = seed.ToHexString();

            Assert.That(text, Is.EqualTo(expectedText));
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            var left = new Seed(123UL);
            var right = new Seed(123UL);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            var left = new Seed(123UL);
            var right = new Seed(456UL);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsDecimalValue()
        {
            var seed = new Seed(123UL);

            string value = seed.ToString();

            Assert.That(value, Is.EqualTo("123"));
        }
    }
}