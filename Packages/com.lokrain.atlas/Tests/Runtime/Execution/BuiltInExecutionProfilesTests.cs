#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class BuiltInExecutionProfilesTests
    {
        [Test]
        public void All_ReturnsExpectedExecutionProfilesInDeclaredOrder()
        {
            Assert.That(
                BuiltInExecutionProfiles.All,
                Is.EqualTo(new[]
                {
                    BuiltInExecutionProfiles.Default
                }));
        }

        [Test]
        public void All_ReturnsOneExecutionProfile()
        {
            Assert.That(BuiltInExecutionProfiles.All, Has.Count.EqualTo(1));
        }

        [Test]
        public void All_IsReadOnly()
        {
            var executionProfiles = (ICollection<ExecutionProfile>)BuiltInExecutionProfiles.All;

            Assert.That(executionProfiles.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(
                () => executionProfiles.Add(BuiltInExecutionProfiles.Default));
        }

        [Test]
        public void Default_HasExpectedMetadata()
        {
            Assert.That(
                BuiltInExecutionProfiles.Default.Symbol.Value,
                Is.EqualTo("lokrain.atlas.execution.profile.default"));

            Assert.That(
                BuiltInExecutionProfiles.Default.DisplayName.Value,
                Is.EqualTo("Default Execution Profile"));
        }

        [Test]
        public void All_ContainsUniqueSymbols()
        {
            var symbols = new HashSet<string>();

            foreach (ExecutionProfile executionProfile in BuiltInExecutionProfiles.All)
            {
                Assert.That(symbols.Add(executionProfile.Symbol.Value), Is.True);
            }
        }
    }
}
