#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class ExecutionProfileSetTests
    {
        [Test]
        public void Constructor_WithValidExecutionProfiles_CreatesExecutionProfileSetInCanonicalSymbolOrder()
        {
            ExecutionProfile diagnosticsProfile = CreateExecutionProfile(
                "diagnostics",
                "Diagnostics Execution Profile");

            ExecutionProfile defaultProfile = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfileSet executionProfileSet = new(
                new[]
                {
                    diagnosticsProfile,
                    defaultProfile
                });

            Assert.That(executionProfileSet.ExecutionProfiles, Has.Count.EqualTo(2));
            Assert.That(executionProfileSet.ExecutionProfiles[0], Is.SameAs(defaultProfile));
            Assert.That(executionProfileSet.ExecutionProfiles[1], Is.SameAs(diagnosticsProfile));
        }

        [Test]
        public void Constructor_WithSameProfilesInDifferentOrder_ProducesSamePublicOrder()
        {
            ExecutionProfile defaultProfile = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfile diagnosticsProfile = CreateExecutionProfile(
                "diagnostics",
                "Diagnostics Execution Profile");

            ExecutionProfileSet first = new(
                new[]
                {
                    defaultProfile,
                    diagnosticsProfile
                });

            ExecutionProfileSet second = new(
                new[]
                {
                    diagnosticsProfile,
                    defaultProfile
                });

            Assert.That(first.ExecutionProfiles, Is.EqualTo(second.ExecutionProfiles));
        }

        [Test]
        public void Constructor_WithEmptyExecutionProfiles_CreatesEmptyExecutionProfileSet()
        {
            ExecutionProfileSet executionProfileSet = new(Array.Empty<ExecutionProfile>());

            Assert.That(executionProfileSet.ExecutionProfiles, Is.Empty);
        }

        [Test]
        public void Constructor_WithNullExecutionProfiles_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ExecutionProfileSet(null!));
        }

        [Test]
        public void Constructor_WithNullEntry_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => new ExecutionProfileSet(
                    new ExecutionProfile?[]
                    {
                        CreateExecutionProfile(
                            "default",
                            "Default Execution Profile"),
                        null
                    }!));
        }

        [Test]
        public void Constructor_WithDuplicateSymbol_ThrowsArgumentException()
        {
            ExecutionProfile first = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfile second = CreateExecutionProfile(
                "default",
                "Duplicate Default Execution Profile");

            Assert.Throws<ArgumentException>(
                () => new ExecutionProfileSet(
                    new[]
                    {
                        first,
                        second
                    }));
        }

        [Test]
        public void Constructor_CopiesExecutionProfiles()
        {
            ExecutionProfile defaultProfile = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfile diagnosticsProfile = CreateExecutionProfile(
                "diagnostics",
                "Diagnostics Execution Profile");

            var executionProfiles = new List<ExecutionProfile>
            {
                defaultProfile
            };

            ExecutionProfileSet executionProfileSet = new(executionProfiles);

            executionProfiles.Add(diagnosticsProfile);

            Assert.That(executionProfileSet.ExecutionProfiles, Has.Count.EqualTo(1));
            Assert.That(executionProfileSet.ExecutionProfiles[0], Is.SameAs(defaultProfile));
        }

        [Test]
        public void ContainsExecutionProfile_WithKnownSymbol_ReturnsTrue()
        {
            ExecutionProfile executionProfile = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfileSet executionProfileSet = new(
                new[]
                {
                    executionProfile
                });

            Assert.That(
                executionProfileSet.ContainsExecutionProfile(executionProfile.Symbol),
                Is.True);
        }

        [Test]
        public void ContainsExecutionProfile_WithUnknownSymbol_ReturnsFalse()
        {
            ExecutionProfileSet executionProfileSet = new(Array.Empty<ExecutionProfile>());

            Assert.That(
                executionProfileSet.ContainsExecutionProfile(
                    Symbol.Create("lokrain.atlas.tests.execution.profile.unknown")),
                Is.False);
        }

        [Test]
        public void GetExecutionProfile_WithKnownSymbol_ReturnsExecutionProfile()
        {
            ExecutionProfile executionProfile = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfileSet executionProfileSet = new(
                new[]
                {
                    executionProfile
                });

            Assert.That(
                executionProfileSet.GetExecutionProfile(executionProfile.Symbol),
                Is.SameAs(executionProfile));
        }

        [Test]
        public void GetExecutionProfile_WithUnknownSymbol_ThrowsKeyNotFoundException()
        {
            ExecutionProfileSet executionProfileSet = new(Array.Empty<ExecutionProfile>());

            Assert.Throws<KeyNotFoundException>(
                () => executionProfileSet.GetExecutionProfile(
                    Symbol.Create("lokrain.atlas.tests.execution.profile.unknown")));
        }

        [Test]
        public void TryGetExecutionProfile_WithKnownSymbol_ReturnsTrueAndExecutionProfile()
        {
            ExecutionProfile executionProfile = CreateExecutionProfile(
                "default",
                "Default Execution Profile");

            ExecutionProfileSet executionProfileSet = new(
                new[]
                {
                    executionProfile
                });

            bool result = executionProfileSet.TryGetExecutionProfile(
                executionProfile.Symbol,
                out ExecutionProfile? resolvedExecutionProfile);

            Assert.That(result, Is.True);
            Assert.That(resolvedExecutionProfile, Is.SameAs(executionProfile));
        }

        [Test]
        public void TryGetExecutionProfile_WithUnknownSymbol_ReturnsFalseAndNull()
        {
            ExecutionProfileSet executionProfileSet = new(Array.Empty<ExecutionProfile>());

            bool result = executionProfileSet.TryGetExecutionProfile(
                Symbol.Create("lokrain.atlas.tests.execution.profile.unknown"),
                out ExecutionProfile? executionProfile);

            Assert.That(result, Is.False);
            Assert.That(executionProfile, Is.Null);
        }

        [Test]
        public void ToString_ReturnsExecutionProfileSetSummary()
        {
            ExecutionProfileSet executionProfileSet = new(
                new[]
                {
                    CreateExecutionProfile(
                        "default",
                        "Default Execution Profile"),
                    CreateExecutionProfile(
                        "diagnostics",
                        "Diagnostics Execution Profile")
                });

            string value = executionProfileSet.ToString();

            Assert.That(value, Is.EqualTo("ExecutionProfileSet(ExecutionProfiles: 2)"));
        }

        private static ExecutionProfile CreateExecutionProfile(
            string name,
            string displayName)
        {
            return new ExecutionProfile(
                Symbol.Create($"lokrain.atlas.tests.execution.profile.{name}"),
                DisplayName.Create(displayName));
        }
    }
}
