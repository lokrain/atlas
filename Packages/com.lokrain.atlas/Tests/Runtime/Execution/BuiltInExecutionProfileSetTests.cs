#nullable enable

using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class BuiltInExecutionProfileSetTests
    {
        [Test]
        public void Default_ReturnsExecutionProfileSet()
        {
            ExecutionProfileSet executionProfileSet = BuiltInExecutionProfileSet.Default;

            Assert.That(executionProfileSet, Is.Not.Null);
        }

        [Test]
        public void Default_ContainsAllBuiltInExecutionProfiles()
        {
            ExecutionProfileSet executionProfileSet = BuiltInExecutionProfileSet.Default;

            Assert.That(
                executionProfileSet.ExecutionProfiles,
                Is.EqualTo(BuiltInExecutionProfiles.All));
        }

        [Test]
        public void Default_ContainsDefaultExecutionProfile()
        {
            ExecutionProfileSet executionProfileSet = BuiltInExecutionProfileSet.Default;

            Assert.That(
                executionProfileSet.ContainsExecutionProfile(
                    BuiltInExecutionProfiles.Default.Symbol),
                Is.True);

            Assert.That(
                executionProfileSet.GetExecutionProfile(
                    BuiltInExecutionProfiles.Default.Symbol),
                Is.SameAs(BuiltInExecutionProfiles.Default));
        }

        [Test]
        public void Default_ToString_ReturnsExpectedSummary()
        {
            string value = BuiltInExecutionProfileSet.Default.ToString();

            Assert.That(value, Is.EqualTo("ExecutionProfileSet(ExecutionProfiles: 1)"));
        }
    }
}
