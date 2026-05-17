// Packages/com.lokrain.atlas/Runtime/Diagnostics/AtlasDiagnosticSeverity.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Define the stable severity scale for Atlas diagnostics.
// - Separate diagnostic severity from diagnostic code, location, and message.
// - Provide small helpers for compiler, validator, editor, and CI consumers.
//
// Design notes
// - Severity is intentionally not tied to exceptions.
// - None is reserved for default/empty diagnostics.
// - Error and Fatal are failures.
// - Warning is reportable but does not make an operation fail by itself.
// - Fatal is reserved for diagnostics that should normally stop the current validation/compilation pass.

using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Stable severity level for Atlas diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Severity answers how serious a diagnostic is. It does not identify what happened.
    /// Stable identity belongs to a diagnostic code, and precise source context belongs to
    /// a diagnostic location.
    /// </para>
    ///
    /// <para>
    /// The numeric values are part of the public diagnostic contract. Do not reorder existing
    /// values after release. Additive expansion is allowed only when downstream tooling can
    /// preserve old behavior.
    /// </para>
    /// </remarks>
    public enum AtlasDiagnosticSeverity : byte
    {
        /// <summary>
        /// No diagnostic severity.
        /// </summary>
        /// <remarks>
        /// Used by default values, empty diagnostics, and uninitialized containers. A produced
        /// diagnostic should not use this severity.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Informational diagnostic.
        /// </summary>
        /// <remarks>
        /// Indicates useful contextual information that does not represent a problem.
        /// </remarks>
        Info = 1,

        /// <summary>
        /// Warning diagnostic.
        /// </summary>
        /// <remarks>
        /// Indicates suspicious, deprecated, risky, or potentially unintended input that does
        /// not make compilation or validation fail by itself.
        /// </remarks>
        Warning = 2,

        /// <summary>
        /// Error diagnostic.
        /// </summary>
        /// <remarks>
        /// Indicates invalid input, invalid compiled metadata, or an invariant violation that
        /// prevents the requested operation from succeeding.
        /// </remarks>
        Error = 3,

        /// <summary>
        /// Fatal diagnostic.
        /// </summary>
        /// <remarks>
        /// Indicates an unrecoverable validation or compilation problem where the current pass
        /// should normally stop instead of accumulating additional diagnostics.
        /// </remarks>
        Fatal = 4
    }

    /// <summary>
    /// Extension helpers for <see cref="AtlasDiagnosticSeverity"/>.
    /// </summary>
    public static class AtlasDiagnosticSeverityExtensions
    {
        /// <summary>
        /// Returns whether the severity is a valid produced-diagnostic severity.
        /// </summary>
        /// <param name="severity">Severity to test.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="severity"/> is <see cref="AtlasDiagnosticSeverity.Info"/>,
        /// <see cref="AtlasDiagnosticSeverity.Warning"/>, <see cref="AtlasDiagnosticSeverity.Error"/>,
        /// or <see cref="AtlasDiagnosticSeverity.Fatal"/>; otherwise <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this AtlasDiagnosticSeverity severity)
        {
            return severity >= AtlasDiagnosticSeverity.Info &&
                   severity <= AtlasDiagnosticSeverity.Fatal;
        }

        /// <summary>
        /// Returns whether the severity represents a failed operation.
        /// </summary>
        /// <param name="severity">Severity to test.</param>
        /// <returns>
        /// <c>true</c> for <see cref="AtlasDiagnosticSeverity.Error"/> and
        /// <see cref="AtlasDiagnosticSeverity.Fatal"/>; otherwise <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFailure(this AtlasDiagnosticSeverity severity)
        {
            return severity >= AtlasDiagnosticSeverity.Error &&
                   severity <= AtlasDiagnosticSeverity.Fatal;
        }

        /// <summary>
        /// Returns whether the severity represents a non-failing produced diagnostic.
        /// </summary>
        /// <param name="severity">Severity to test.</param>
        /// <returns>
        /// <c>true</c> for <see cref="AtlasDiagnosticSeverity.Info"/> and
        /// <see cref="AtlasDiagnosticSeverity.Warning"/>; otherwise <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNonFailure(this AtlasDiagnosticSeverity severity)
        {
            return severity == AtlasDiagnosticSeverity.Info ||
                   severity == AtlasDiagnosticSeverity.Warning;
        }

        /// <summary>
        /// Returns whether the severity should normally stop the current validation or compilation pass.
        /// </summary>
        /// <param name="severity">Severity to test.</param>
        /// <returns>
        /// <c>true</c> only for <see cref="AtlasDiagnosticSeverity.Fatal"/>; otherwise <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldStopPass(this AtlasDiagnosticSeverity severity)
        {
            return severity == AtlasDiagnosticSeverity.Fatal;
        }
    }
}