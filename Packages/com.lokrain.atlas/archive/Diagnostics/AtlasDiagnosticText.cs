// Packages/com.lokrain.atlas/Runtime/Diagnostics/AtlasDiagnosticText.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Centralize deterministic diagnostic text conversion.
// - Remove repeated FixedString64Bytes and FixedString512Bytes truncation helpers from validators.
// - Preserve stable diagnostic payload limits without making message text diagnostic identity.
//
// Design notes
// - Diagnostic codes own machine identity. Text owns human explanation.
// - These helpers intentionally truncate managed strings before constructing FixedString payloads.
// - Truncation is by managed char count, matching the package's existing behavior.
// - This type is managed tooling/validation infrastructure and is not a Burst/job payload.

using Unity.Collections;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Provides deterministic diagnostic text conversion helpers.
    /// </summary>
    public static class AtlasDiagnosticText
    {
        /// <summary>
        /// Maximum managed character count used before constructing <see cref="FixedString64Bytes"/> values.
        /// </summary>
        public const int FixedString64CharacterLimit = 63;

        /// <summary>
        /// Maximum managed character count used before constructing <see cref="FixedString512Bytes"/> values.
        /// </summary>
        public const int FixedString512CharacterLimit = 511;

        /// <summary>
        /// Fallback text used when a produced diagnostic receives an empty message.
        /// </summary>
        public const string EmptyDiagnosticMessage = "<empty diagnostic message>";

        /// <summary>
        /// Fallback text used when diagnostic display names are absent.
        /// </summary>
        public const string UnnamedDiagnosticName = "<unnamed>";

        /// <summary>
        /// Converts a managed string into a bounded diagnostic name.
        /// </summary>
        public static FixedString64Bytes Name64(string value)
        {
            return string.IsNullOrEmpty(value)
                ? default
                : new FixedString64Bytes(Truncate(value, FixedString64CharacterLimit));
        }

        /// <summary>
        /// Converts a fixed diagnostic name to managed display text.
        /// </summary>
        public static string Name(FixedString64Bytes name)
        {
            return name.IsEmpty
                ? UnnamedDiagnosticName
                : name.ToString();
        }

        /// <summary>
        /// Converts a managed string into a bounded diagnostic message.
        /// </summary>
        public static FixedString512Bytes Message(string value)
        {
            return string.IsNullOrEmpty(value)
                ? new FixedString512Bytes(EmptyDiagnosticMessage)
                : new FixedString512Bytes(Truncate(value, FixedString512CharacterLimit));
        }

        /// <summary>
        /// Truncates a managed string to the supplied maximum character count.
        /// </summary>
        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }
}
