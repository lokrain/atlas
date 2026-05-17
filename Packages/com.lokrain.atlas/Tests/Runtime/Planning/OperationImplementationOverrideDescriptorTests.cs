#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class OperationImplementationOverrideDescriptorTests
    {
        private const string StageRouteStepSymbolText =
            "lokrain.atlas.tests.route_step.extract_main_continent";

        private const string ImplementationSymbolText =
            "lokrain.atlas.tests.implementation.extract_main_continent.default";

        [Test]
        public void Constructor_WithSymbols_StoresSymbols()
        {
            Symbol stageRouteStepSymbol = Symbol.Create(StageRouteStepSymbolText);
            Symbol implementationSymbol = Symbol.Create(ImplementationSymbolText);

            var descriptor = new OperationImplementationOverrideDescriptor(
                stageRouteStepSymbol,
                implementationSymbol);

            Assert.That(descriptor.StageRouteStepDefinitionSymbol, Is.SameAs(stageRouteStepSymbol));
            Assert.That(descriptor.OperationImplementationDefinitionSymbol, Is.SameAs(implementationSymbol));
        }

        [Test]
        public void Constructor_WithNullStageRouteStepDefinitionSymbol_ThrowsArgumentNullException()
        {
            Symbol implementationSymbol = Symbol.Create(ImplementationSymbolText);

            Assert.Throws<ArgumentNullException>(
                () => new OperationImplementationOverrideDescriptor(null!, implementationSymbol));
        }

        [Test]
        public void Constructor_WithNullOperationImplementationDefinitionSymbol_ThrowsArgumentNullException()
        {
            Symbol stageRouteStepSymbol = Symbol.Create(StageRouteStepSymbolText);

            Assert.Throws<ArgumentNullException>(
                () => new OperationImplementationOverrideDescriptor(stageRouteStepSymbol, null!));
        }

        [Test]
        public void Create_WithValidSymbolValues_ReturnsDescriptor()
        {
            OperationImplementationOverrideDescriptor descriptor =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    ImplementationSymbolText);

            Assert.That(
                descriptor.StageRouteStepDefinitionSymbol,
                Is.EqualTo(Symbol.Create(StageRouteStepSymbolText)));

            Assert.That(
                descriptor.OperationImplementationDefinitionSymbol,
                Is.EqualTo(Symbol.Create(ImplementationSymbolText)));
        }

        [TestCase(null, ImplementationSymbolText)]
        [TestCase("", ImplementationSymbolText)]
        [TestCase("Invalid.Symbol", ImplementationSymbolText)]
        [TestCase(StageRouteStepSymbolText, null)]
        [TestCase(StageRouteStepSymbolText, "")]
        [TestCase(StageRouteStepSymbolText, "Invalid.Symbol")]
        public void Create_WithInvalidSymbolValues_ThrowsArgumentException(
            string? stageRouteStepSymbol,
            string? implementationSymbol)
        {
            Assert.Throws<ArgumentException>(
                () => OperationImplementationOverrideDescriptor.Create(
                    stageRouteStepSymbol,
                    implementationSymbol));
        }

        [Test]
        public void TryCreate_WithValidSymbolValues_ReturnsTrueAndDescriptor()
        {
            bool succeeded = OperationImplementationOverrideDescriptor.TryCreate(
                StageRouteStepSymbolText,
                ImplementationSymbolText,
                out OperationImplementationOverrideDescriptor? descriptor);

            Assert.That(succeeded, Is.True);
            Assert.That(descriptor, Is.Not.Null);

            Assert.That(
                descriptor!.StageRouteStepDefinitionSymbol,
                Is.EqualTo(Symbol.Create(StageRouteStepSymbolText)));

            Assert.That(
                descriptor.OperationImplementationDefinitionSymbol,
                Is.EqualTo(Symbol.Create(ImplementationSymbolText)));
        }

        [TestCase(null, ImplementationSymbolText)]
        [TestCase("", ImplementationSymbolText)]
        [TestCase("Invalid.Symbol", ImplementationSymbolText)]
        [TestCase(StageRouteStepSymbolText, null)]
        [TestCase(StageRouteStepSymbolText, "")]
        [TestCase(StageRouteStepSymbolText, "Invalid.Symbol")]
        public void TryCreate_WithInvalidSymbolValues_ReturnsFalseAndNull(
            string? stageRouteStepSymbol,
            string? implementationSymbol)
        {
            bool succeeded = OperationImplementationOverrideDescriptor.TryCreate(
                stageRouteStepSymbol,
                implementationSymbol,
                out OperationImplementationOverrideDescriptor? descriptor);

            Assert.That(succeeded, Is.False);
            Assert.That(descriptor, Is.Null);
        }

        [TestCase(StageRouteStepSymbolText, ImplementationSymbolText)]
        public void IsValid_WithValidSymbolValues_ReturnsTrue(
            string stageRouteStepSymbol,
            string implementationSymbol)
        {
            bool isValid = OperationImplementationOverrideDescriptor.IsValid(
                stageRouteStepSymbol,
                implementationSymbol);

            Assert.That(isValid, Is.True);
        }

        [TestCase(null, ImplementationSymbolText)]
        [TestCase("", ImplementationSymbolText)]
        [TestCase("Invalid.Symbol", ImplementationSymbolText)]
        [TestCase(StageRouteStepSymbolText, null)]
        [TestCase(StageRouteStepSymbolText, "")]
        [TestCase(StageRouteStepSymbolText, "Invalid.Symbol")]
        public void IsValid_WithInvalidSymbolValues_ReturnsFalse(
            string? stageRouteStepSymbol,
            string? implementationSymbol)
        {
            bool isValid = OperationImplementationOverrideDescriptor.IsValid(
                stageRouteStepSymbol,
                implementationSymbol);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Equals_WithSameSymbols_ReturnsTrue()
        {
            OperationImplementationOverrideDescriptor left =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    ImplementationSymbolText);

            OperationImplementationOverrideDescriptor right =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    ImplementationSymbolText);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentStageRouteStepDefinitionSymbol_ReturnsFalse()
        {
            OperationImplementationOverrideDescriptor left =
                OperationImplementationOverrideDescriptor.Create(
                    "lokrain.atlas.tests.route_step.first",
                    ImplementationSymbolText);

            OperationImplementationOverrideDescriptor right =
                OperationImplementationOverrideDescriptor.Create(
                    "lokrain.atlas.tests.route_step.second",
                    ImplementationSymbolText);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentOperationImplementationDefinitionSymbol_ReturnsFalse()
        {
            OperationImplementationOverrideDescriptor left =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    "lokrain.atlas.tests.implementation.first");

            OperationImplementationOverrideDescriptor right =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    "lokrain.atlas.tests.implementation.second");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            OperationImplementationOverrideDescriptor descriptor =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    ImplementationSymbolText);

            Assert.That(descriptor.Equals("OperationImplementationOverrideDescriptor"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            OperationImplementationOverrideDescriptor? left = null;
            OperationImplementationOverrideDescriptor? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            OperationImplementationOverrideDescriptor? left =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    ImplementationSymbolText);

            OperationImplementationOverrideDescriptor? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsDescriptorText()
        {
            OperationImplementationOverrideDescriptor descriptor =
                OperationImplementationOverrideDescriptor.Create(
                    StageRouteStepSymbolText,
                    ImplementationSymbolText);

            string value = descriptor.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "OperationImplementationOverrideDescriptor(StageRouteStepDefinitionSymbol: lokrain.atlas.tests.route_step.extract_main_continent, OperationImplementationDefinitionSymbol: lokrain.atlas.tests.implementation.extract_main_continent.default)"));
        }
    }
}