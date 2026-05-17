#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationRequestResolutionErrorTests
    {
        private const string CodeSymbolText =
            "lokrain.atlas.tests.resolution_error.code";

        private const string OtherCodeSymbolText =
            "lokrain.atlas.tests.resolution_error.other_code";

        private const string SubjectSymbolText =
            "lokrain.atlas.tests.resolution_error.subject";

        private const string OtherSubjectSymbolText =
            "lokrain.atlas.tests.resolution_error.other_subject";

        [Test]
        public void Constructor_WithCodeAndMessage_StoresErrorWithoutSubjectSymbol()
        {
            Symbol code = Symbol.Create(CodeSymbolText);

            var error = new GenerationRequestResolutionError(
                code,
                "Recipe was not found.");

            Assert.That(error.Code, Is.SameAs(code));
            Assert.That(error.Message, Is.EqualTo("Recipe was not found."));
            Assert.That(error.SubjectSymbol, Is.Null);
        }

        [Test]
        public void Constructor_WithCodeMessageAndSubjectSymbol_StoresErrorWithSubjectSymbol()
        {
            Symbol code = Symbol.Create(CodeSymbolText);
            Symbol subjectSymbol = Symbol.Create(SubjectSymbolText);

            var error = new GenerationRequestResolutionError(
                code,
                "Recipe was not found.",
                subjectSymbol);

            Assert.That(error.Code, Is.SameAs(code));
            Assert.That(error.Message, Is.EqualTo("Recipe was not found."));
            Assert.That(error.SubjectSymbol, Is.SameAs(subjectSymbol));
        }

        [Test]
        public void Constructor_WithMessageContainingLeadingAndTrailingWhitespace_StoresTrimmedMessage()
        {
            Symbol code = Symbol.Create(CodeSymbolText);

            var error = new GenerationRequestResolutionError(
                code,
                "  Recipe was not found.  ");

            Assert.That(error.Message, Is.EqualTo("Recipe was not found."));
        }

        [Test]
        public void Constructor_WithNullCode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequestResolutionError(
                    null!,
                    "Recipe was not found."));
        }

        [Test]
        public void Constructor_WithNullMessage_ThrowsArgumentNullException()
        {
            Symbol code = Symbol.Create(CodeSymbolText);

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequestResolutionError(
                    code,
                    null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("\n")]
        public void Constructor_WithEmptyOrWhitespaceMessage_ThrowsArgumentException(
            string message)
        {
            Symbol code = Symbol.Create(CodeSymbolText);

            Assert.Throws<ArgumentException>(
                () => new GenerationRequestResolutionError(
                    code,
                    message));
        }

        [Test]
        public void Equals_WithSameCodeMessageAndSubjectSymbol_ReturnsTrue()
        {
            var left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            var right = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithSameCodeAndMessageAndNoSubjectSymbol_ReturnsTrue()
        {
            var left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.");

            var right = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentCode_ReturnsFalse()
        {
            var left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            var right = new GenerationRequestResolutionError(
                Symbol.Create(OtherCodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentMessage_ReturnsFalse()
        {
            var left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            var right = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Implementation was not found.",
                Symbol.Create(SubjectSymbolText));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentSubjectSymbol_ReturnsFalse()
        {
            var left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            var right = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(OtherSubjectSymbolText));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithOneSubjectSymbolMissing_ReturnsFalse()
        {
            var left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            var right = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            var error = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            Assert.That(error.Equals("GenerationRequestResolutionError"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationRequestResolutionError? left = null;
            GenerationRequestResolutionError? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationRequestResolutionError? left = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.");

            GenerationRequestResolutionError? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_WithoutSubjectSymbol_ReturnsErrorText()
        {
            var error = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.");

            string value = error.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRequestResolutionError(Code: lokrain.atlas.tests.resolution_error.code, Message: Recipe was not found.)"));
        }

        [Test]
        public void ToString_WithSubjectSymbol_ReturnsErrorText()
        {
            var error = new GenerationRequestResolutionError(
                Symbol.Create(CodeSymbolText),
                "Recipe was not found.",
                Symbol.Create(SubjectSymbolText));

            string value = error.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRequestResolutionError(Code: lokrain.atlas.tests.resolution_error.code, SubjectSymbol: lokrain.atlas.tests.resolution_error.subject, Message: Recipe was not found.)"));
        }
    }
}