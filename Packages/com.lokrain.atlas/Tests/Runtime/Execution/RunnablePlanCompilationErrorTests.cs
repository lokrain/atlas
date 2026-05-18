#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class RunnablePlanCompilationErrorTests
    {
        [Test]
        public void Constructor_WithUnknownCode_ThrowsArgumentException()
        {
            Assert.That(
                () => new RunnablePlanCompilationError(
                    RunnablePlanCompilationErrorCode.Unknown,
                    "Message."),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("code"));
        }

        [Test]
        public void Constructor_WithUnsupportedCode_ThrowsArgumentException()
        {
            Assert.That(
                () => new RunnablePlanCompilationError(
                    (RunnablePlanCompilationErrorCode)99,
                    "Message."),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("code"));
        }

        [Test]
        public void Constructor_WithNullMessage_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new RunnablePlanCompilationError(
                    RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                    message: null!),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("message"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithEmptyOrWhiteSpaceMessage_ThrowsArgumentException(string message)
        {
            Assert.That(
                () => new RunnablePlanCompilationError(
                    RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                    message),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("message"));
        }

        [Test]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            Symbol subjectSymbol = Symbol.Create("atlas.resource.base_elevation");
            var fieldIndex = new FieldIndex(1);
            var stageIndex = new StageIndex(2);
            var operationIndex = new OperationIndex(3);

            var error = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch,
                "Resource ownership mismatch.",
                subjectSymbol,
                fieldIndex,
                stageIndex,
                operationIndex);

            Assert.That(error.Code, Is.EqualTo(RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch));
            Assert.That(error.Message, Is.EqualTo("Resource ownership mismatch."));
            Assert.That(error.SubjectSymbol, Is.SameAs(subjectSymbol));
            Assert.That(error.FieldIndex, Is.EqualTo(fieldIndex));
            Assert.That(error.StageIndex, Is.EqualTo(stageIndex));
            Assert.That(error.OperationIndex, Is.EqualTo(operationIndex));
        }

        [Test]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            Symbol subjectSymbol = Symbol.Create("atlas.resource.base_elevation");

            var left = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                "Missing field definition.",
                subjectSymbol,
                new FieldIndex(1),
                new StageIndex(2),
                new OperationIndex(3));

            var right = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                "Missing field definition.",
                subjectSymbol,
                new FieldIndex(1),
                new StageIndex(2),
                new OperationIndex(3));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentCode_ReturnsFalse()
        {
            var left = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                "Message.");

            var right = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch,
                "Message.");

            Assert.That(left.Equals(right), Is.False);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            var error = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                "Message.");

            Assert.That(error.Equals(null), Is.False);
            Assert.That(error == null, Is.False);
            Assert.That(error != null, Is.True);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            RunnablePlanCompilationError? left = null;
            RunnablePlanCompilationError? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void ToString_ContainsHighSignalContext()
        {
            Symbol subjectSymbol = Symbol.Create("atlas.resource.base_elevation");

            var error = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                "Missing field definition.",
                subjectSymbol,
                new FieldIndex(1),
                new StageIndex(2),
                new OperationIndex(3));

            string text = error.ToString();

            Assert.That(text, Does.Contain(nameof(RunnablePlanCompilationError)));
            Assert.That(text, Does.Contain(RunnablePlanCompilationErrorCode.MissingFieldDefinition.ToString()));
            Assert.That(text, Does.Contain("Missing field definition."));
            Assert.That(text, Does.Contain(subjectSymbol.ToString()));
            Assert.That(text, Does.Contain("FieldIndex(1)"));
            Assert.That(text, Does.Contain("StageIndex(2)"));
            Assert.That(text, Does.Contain("OperationIndex(3)"));
        }
    }
}
