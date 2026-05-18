#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class FieldCapturePolicyTests
    {
        [Test]
        public void Default_ReturnsUnknown()
        {
            FieldCapturePolicy policy = default;

            Assert.That(policy, Is.EqualTo(FieldCapturePolicy.Unknown));
        }

        [Test]
        public void Unknown_HasZeroValue()
        {
            int value = (int)FieldCapturePolicy.Unknown;

            Assert.That(value, Is.EqualTo(0));
        }

        [TestCase(FieldCapturePolicy.DoNotCapture, 1)]
        [TestCase(FieldCapturePolicy.Capture, 2)]
        public void SupportedPolicy_HasStableValue(
            FieldCapturePolicy policy,
            int expectedValue)
        {
            int value = (int)policy;

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void DefinedPolicies_AreExpectedSet()
        {
            var values = Enum.GetValues(typeof(FieldCapturePolicy));

            Assert.That(
                values,
                Is.EqualTo(new[]
                {
                    FieldCapturePolicy.Unknown,
                    FieldCapturePolicy.DoNotCapture,
                    FieldCapturePolicy.Capture
                }));
        }

        [Test]
        public void FieldCapturePolicy_IsNotFlagsEnum()
        {
            bool hasFlagsAttribute = typeof(FieldCapturePolicy).IsDefined(typeof(FlagsAttribute), inherit: false);

            Assert.That(hasFlagsAttribute, Is.False);
        }
    }
}