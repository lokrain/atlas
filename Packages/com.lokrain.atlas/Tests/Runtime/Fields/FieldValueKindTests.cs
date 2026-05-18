#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Fields.Tests
{
    public sealed class FieldValueKindTests
    {
        [Test]
        public void Default_ReturnsUnknown()
        {
            FieldValueKind kind = default;

            Assert.That(kind, Is.EqualTo(FieldValueKind.Unknown));
        }

        [Test]
        public void Unknown_HasZeroValue()
        {
            int value = (int)FieldValueKind.Unknown;

            Assert.That(value, Is.EqualTo(0));
        }

        [TestCase(FieldValueKind.Boolean, 1)]
        [TestCase(FieldValueKind.Int32, 2)]
        [TestCase(FieldValueKind.UInt32, 3)]
        [TestCase(FieldValueKind.Single, 4)]
        [TestCase(FieldValueKind.Double, 5)]
        public void SupportedKind_HasStableValue(FieldValueKind kind, int expectedValue)
        {
            int value = (int)kind;

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void DefinedKinds_AreExpectedSet()
        {
            var values = (FieldValueKind[])Enum.GetValues(typeof(FieldValueKind));

            Assert.That(
                values,
                Is.EqualTo(new[]
                {
                    FieldValueKind.Unknown,
                    FieldValueKind.Boolean,
                    FieldValueKind.Int32,
                    FieldValueKind.UInt32,
                    FieldValueKind.Single,
                    FieldValueKind.Double
                }));
        }
    }
}
