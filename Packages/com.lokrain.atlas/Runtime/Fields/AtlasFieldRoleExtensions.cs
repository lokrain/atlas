// Packages/com.lokrain.atlas/Runtime/Fields/AtlasFieldRoleExtensions.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Fields
//
// Purpose
// - Centralize semantic field-role policy checks.
// - Keep artifact-capture and validation code from duplicating role-specific decisions.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Provides allocation-free policy helpers for <see cref="AtlasFieldRole"/>.
    /// </summary>
    public static class AtlasFieldRoleExtensions
    {
        /// <summary>
        /// Gets whether fields with this role are captured by the default artifact capture path.
        /// </summary>
        /// <remarks>
        /// The current default capture path preserves existing canonical, payload, and diagnostic
        /// behavior, while explicitly excluding stage-transient fields. Operation scratch is not a
        /// field role and therefore cannot be captured through this policy.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCapturedByDefaultArtifactProfile(
            this AtlasFieldRole role)
        {
            return role == AtlasFieldRole.Canonical ||
                   role == AtlasFieldRole.Payload ||
                   role == AtlasFieldRole.Diagnostic;
        }

        /// <summary>
        /// Gets whether fields with this role are allocated in Atlas workspace-owned storage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAtlasWorkspaceField(
            this AtlasFieldRole role)
        {
            return role == AtlasFieldRole.Canonical ||
                   role == AtlasFieldRole.Payload ||
                   role == AtlasFieldRole.Diagnostic ||
                   role == AtlasFieldRole.StageTransient;
        }
    }
}
