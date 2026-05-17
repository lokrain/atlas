#nullable enable

using System;
using System.Text;
using NUnit.Framework;

namespace Lokrain.Atlas.Core.Tests
{
    public sealed class DisplayNameTests
    {
        [TestCase("A")]
        [TestCase("Atlas")]
        [TestCase("Primary Continental Landmass")]
        [TestCase("Extract Main Continent")]
        [TestCase("Name With  Multiple  Spaces")]
        [TestCase("Café")]
        [TestCase("世界")]
        public void Create_WithValidDisplayName_ReturnsDisplayName(string value)
        {
            DisplayName displayName = DisplayName.Create(value);

            Assert.That(displayName.Value, Is.EqualTo(value));
        }

        [Test]
        public void Create_WithLeadingAndTrailingOrdinarySpaces_TrimsOrdinarySpaces()
        {
            DisplayName displayName = DisplayName.Create("  Atlas Display Name  ");

            Assert.That(displayName.Value, Is.EqualTo("Atlas Display Name"));
        }

        [Test]
        public void Create_WithOnlyOrdinarySpaces_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => DisplayName.Create("   "));
        }

        [Test]
        public void Create_WithDecomposedUnicode_NormalizesToFormC()
        {
            string decomposedValue = "Cafe\u0301";

            DisplayName displayName = DisplayName.Create(decomposedValue);

            Assert.That(displayName.Value, Is.EqualTo("Café"));
            Assert.That(displayName.Value.IsNormalized(NormalizationForm.FormC), Is.True);
        }

        [Test]
        public void Create_WithMaximumLengthDisplayName_ReturnsDisplayName()
        {
            string value = new string('A', DisplayName.MaxLength);

            DisplayName displayName = DisplayName.Create(value);

            Assert.That(displayName.Value, Is.EqualTo(value));
            Assert.That(displayName.Value.Length, Is.EqualTo(DisplayName.MaxLength));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("\t")]
        [TestCase("\n")]
        [TestCase("Atlas\tName")]
        [TestCase("Atlas\nName")]
        [TestCase("Atlas\rName")]
        [TestCase("Atlas\u00A0Name")]
        [TestCase("Atlas\u2028Name")]
        [TestCase("Atlas\u2029Name")]
        [TestCase("Atlas\u200DName")]
        [TestCase("Atlas\uE000Name")]
        public void Create_WithInvalidDisplayName_ThrowsArgumentException(string? value)
        {
            Assert.Throws<ArgumentException>(() => DisplayName.Create(value));
        }

        [Test]
        public void Create_WithSurrogateCharacter_ThrowsArgumentException()
        {
            string value = CreateValueWithSurrogateCharacter();

            Assert.Throws<ArgumentException>(() => DisplayName.Create(value));
        }

        [Test]
        public void Create_WithTooLongDisplayName_ThrowsArgumentException()
        {
            string value = new string('A', DisplayName.MaxLength + 1);

            Assert.Throws<ArgumentException>(() => DisplayName.Create(value));
        }

        [TestCase("Atlas")]
        [TestCase("Primary Continental Landmass")]
        [TestCase("Café")]
        public void TryCreate_WithValidDisplayName_ReturnsTrueAndDisplayName(string value)
        {
            bool succeeded = DisplayName.TryCreate(value, out DisplayName? displayName);

            Assert.That(succeeded, Is.True);
            Assert.That(displayName, Is.Not.Null);
            Assert.That(displayName!.Value, Is.EqualTo(value));
        }

        [Test]
        public void TryCreate_WithLeadingAndTrailingOrdinarySpaces_ReturnsTrimmedDisplayName()
        {
            bool succeeded = DisplayName.TryCreate("  Atlas  ", out DisplayName? displayName);

            Assert.That(succeeded, Is.True);
            Assert.That(displayName, Is.Not.Null);
            Assert.That(displayName!.Value, Is.EqualTo("Atlas"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Atlas\tName")]
        [TestCase("Atlas\u200DName")]
        public void TryCreate_WithInvalidDisplayName_ReturnsFalseAndNull(string? value)
        {
            bool succeeded = DisplayName.TryCreate(value, out DisplayName? displayName);

            Assert.That(succeeded, Is.False);
            Assert.That(displayName, Is.Null);
        }

        [Test]
        public void TryCreate_WithSurrogateCharacter_ReturnsFalseAndNull()
        {
            string value = CreateValueWithSurrogateCharacter();

            bool succeeded = DisplayName.TryCreate(value, out DisplayName? displayName);

            Assert.That(succeeded, Is.False);
            Assert.That(displayName, Is.Null);
        }

        [TestCase("Atlas")]
        [TestCase("Primary Continental Landmass")]
        [TestCase("Café")]
        public void IsValid_WithValidDisplayName_ReturnsTrue(string value)
        {
            bool isValid = DisplayName.IsValid(value);

            Assert.That(isValid, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Atlas\tName")]
        [TestCase("Atlas\u200DName")]
        public void IsValid_WithInvalidDisplayName_ReturnsFalse(string? value)
        {
            bool isValid = DisplayName.IsValid(value);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValid_WithSurrogateCharacter_ReturnsFalse()
        {
            string value = CreateValueWithSurrogateCharacter();

            bool isValid = DisplayName.IsValid(value);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            DisplayName left = DisplayName.Create("Atlas");
            DisplayName right = DisplayName.Create("Atlas");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            DisplayName left = DisplayName.Create("Atlas");
            DisplayName right = DisplayName.Create("Landmass");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentNormalizationEquivalentInput_ReturnsTrue()
        {
            DisplayName left = DisplayName.Create("Café");
            DisplayName right = DisplayName.Create("Cafe\u0301");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            DisplayName? left = null;
            DisplayName? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            DisplayName? left = DisplayName.Create("Atlas");
            DisplayName? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsValue()
        {
            DisplayName displayName = DisplayName.Create("Atlas");

            string value = displayName.ToString();

            Assert.That(value, Is.EqualTo("Atlas"));
        }

        private static string CreateValueWithSurrogateCharacter()
        {
            return new string(new[]
            {
                'A',
                't',
                'l',
                'a',
                's',
                '\uD800',
                'N',
                'a',
                'm',
                'e'
            });
        }
    }
}