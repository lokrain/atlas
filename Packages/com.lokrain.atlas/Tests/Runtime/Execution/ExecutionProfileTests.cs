#nullable enable

using System;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class ExecutionProfileTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesExecutionProfile()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.execution.profile.default");
            DisplayName displayName = DisplayName.Create("Default Execution Profile");

            ExecutionProfile executionProfile = new(symbol, displayName);

            Assert.That(executionProfile.Symbol, Is.SameAs(symbol));
            Assert.That(executionProfile.DisplayName, Is.SameAs(displayName));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            DisplayName displayName = DisplayName.Create("Default Execution Profile");

            Assert.Throws<ArgumentNullException>(
                () => new ExecutionProfile(
                    null!,
                    displayName));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.execution.profile.default");

            Assert.Throws<ArgumentNullException>(
                () => new ExecutionProfile(
                    symbol,
                    null!));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            ExecutionProfile left = new(
                Symbol.Create("lokrain.atlas.tests.execution.profile.default"),
                DisplayName.Create("Default Execution Profile"));

            ExecutionProfile right = new(
                Symbol.Create("lokrain.atlas.tests.execution.profile.default"),
                DisplayName.Create("Different Display Name"));

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            ExecutionProfile left = new(
                Symbol.Create("lokrain.atlas.tests.execution.profile.default"),
                DisplayName.Create("Default Execution Profile"));

            ExecutionProfile right = new(
                Symbol.Create("lokrain.atlas.tests.execution.profile.diagnostics"),
                DisplayName.Create("Diagnostics Execution Profile"));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            ExecutionProfile executionProfile = CreateExecutionProfile();

            Assert.That(executionProfile.Equals("ExecutionProfile"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            ExecutionProfile? left = null;
            ExecutionProfile? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            ExecutionProfile? left = CreateExecutionProfile();
            ExecutionProfile? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsExecutionProfileSummary()
        {
            ExecutionProfile executionProfile = CreateExecutionProfile();

            string value = executionProfile.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "ExecutionProfile(Symbol: lokrain.atlas.tests.execution.profile.default, DisplayName: Default Execution Profile)"));
        }

        private static ExecutionProfile CreateExecutionProfile()
        {
            return new ExecutionProfile(
                Symbol.Create("lokrain.atlas.tests.execution.profile.default"),
                DisplayName.Create("Default Execution Profile"));
        }
    }
}
