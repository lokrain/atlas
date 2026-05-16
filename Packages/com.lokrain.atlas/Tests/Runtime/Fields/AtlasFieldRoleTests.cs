// Packages/com.lokrain.atlas/Tests/Runtime/Fields/AtlasFieldRoleTests.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields.Tests
//
// Purpose
// - Verify field-role policy helpers preserve artifact and workspace lifetime boundaries.

using NUnit.Framework;

namespace Lokrain.Atlas.Fields.Tests
{
    public sealed class AtlasFieldRoleTests
    {
        [Test]
        public void IsCapturedByDefaultArtifactProfile_StageTransient_ReturnsFalse()
        {
            Assert.That(
                AtlasFieldRole.StageTransient.IsCapturedByDefaultArtifactProfile(),
                Is.False);
        }

        [Test]
        public void IsCapturedByDefaultArtifactProfile_CanonicalPayloadAndDiagnostic_ReturnsTrue()
        {
            Assert.That(AtlasFieldRole.Canonical.IsCapturedByDefaultArtifactProfile(), Is.True);
            Assert.That(AtlasFieldRole.Payload.IsCapturedByDefaultArtifactProfile(), Is.True);
            Assert.That(AtlasFieldRole.Diagnostic.IsCapturedByDefaultArtifactProfile(), Is.True);
        }

        [Test]
        public void IsAtlasWorkspaceField_StageTransient_ReturnsTrue()
        {
            Assert.That(
                AtlasFieldRole.StageTransient.IsAtlasWorkspaceField(),
                Is.True);
        }

        [Test]
        public void IsAtlasWorkspaceField_External_ReturnsFalse()
        {
            Assert.That(
                AtlasFieldRole.External.IsAtlasWorkspaceField(),
                Is.False);
        }
    }
}
