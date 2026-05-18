#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class RunnablePlanCompilationErrorCodeTests
    {
        [Test]
        public void Default_ReturnsUnknown()
        {
            RunnablePlanCompilationErrorCode code = default;

            Assert.That(code, Is.EqualTo(RunnablePlanCompilationErrorCode.Unknown));
        }

        [Test]
        public void Unknown_HasZeroValue()
        {
            int value = (int)RunnablePlanCompilationErrorCode.Unknown;

            Assert.That(value, Is.EqualTo(0));
        }

        [TestCase(RunnablePlanCompilationErrorCode.MissingFieldDefinition, 1)]
        [TestCase(RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch, 2)]
        public void SupportedCode_HasStableValue(
            RunnablePlanCompilationErrorCode code,
            int expectedValue)
        {
            int value = (int)code;

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void DefinedCodes_AreExpectedSet()
        {
            var values = (RunnablePlanCompilationErrorCode[])Enum.GetValues(
                typeof(RunnablePlanCompilationErrorCode));

            Assert.That(
                values,
                Is.EqualTo(new[]
                {
                    RunnablePlanCompilationErrorCode.Unknown,
                    RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                    RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch
                }));
        }

        [Test]
        public void RunnablePlanCompilationErrorCode_IsNotFlagsEnum()
        {
            bool hasFlagsAttribute = typeof(RunnablePlanCompilationErrorCode).IsDefined(
                typeof(FlagsAttribute),
                inherit: false);

            Assert.That(hasFlagsAttribute, Is.False);
        }
    }
}
