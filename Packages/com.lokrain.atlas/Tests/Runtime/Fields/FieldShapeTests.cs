#nullable enable

using System;
using NUnit.Framework;

namespace Lokrain.Atlas.Fields.Tests
{
    public sealed class FieldShapeTests
    {
        [Test]
        public void Default_ReturnsUnknown()
        {
            FieldShape shape = default;

            Assert.That(shape, Is.EqualTo(FieldShape.Unknown));
        }

        [Test]
        public void Unknown_HasZeroValue()
        {
            int value = (int)FieldShape.Unknown;

            Assert.That(value, Is.EqualTo(0));
        }

        [TestCase(FieldShape.Scalar, 1)]
        [TestCase(FieldShape.Grid, 2)]
        public void SupportedShape_HasStableValue(FieldShape shape, int expectedValue)
        {
            int value = (int)shape;

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void DefinedShapes_AreExpectedSet()
        {
            var values = (FieldShape[])Enum.GetValues(typeof(FieldShape));

            Assert.That(
                values,
                Is.EqualTo(new[]
                {
                    FieldShape.Unknown,
                    FieldShape.Scalar,
                    FieldShape.Grid
                }));
        }
    }
}
