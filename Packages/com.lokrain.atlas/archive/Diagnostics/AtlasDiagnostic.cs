// Packages/com.lokrain.atlas/Runtime/Diagnostics/AtlasDiagnostic.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Define one immutable Atlas diagnostic report.
// - Combine severity, stable diagnostic code, source location, and message.
// - Provide a stable payload for compiler, validator, editor, CI, and test output.
// - Keep diagnostics separate from exceptions and runtime execution payloads.
//
// Design notes
// - Diagnostic identity belongs to AtlasDiagnosticCode.
// - Failure semantics belong to AtlasDiagnosticSeverity.
// - Source context belongs to AtlasDiagnosticLocation.
// - Human-readable text belongs to Message and may evolve without changing the diagnostic code.
// - This type is metadata-only and should not be passed into Burst jobs.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Immutable Atlas diagnostic report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDiagnostic"/> is the basic report unit produced by validators,
    /// compilers, editor tooling, and CI checks. It is not an exception and does not own
    /// control flow by itself.
    /// </para>
    ///
    /// <para>
    /// The diagnostic code is the stable machine identity. The message is human-readable
    /// explanation and may be improved over time without changing the code, as long as the
    /// underlying diagnostic meaning does not change.
    /// </para>
    ///
    /// <para>
    /// The default value is the empty diagnostic. Produced diagnostics must have a valid
    /// severity, valid code, valid location, and non-empty message.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasDiagnostic :
        IEquatable<AtlasDiagnostic>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        /// <summary>
        /// Reserved empty diagnostic.
        /// </summary>
        public static readonly AtlasDiagnostic Empty = default;

        /// <summary>
        /// Diagnostic severity.
        /// </summary>
        public readonly AtlasDiagnosticSeverity Severity;

        /// <summary>
        /// Stable diagnostic code.
        /// </summary>
        public readonly AtlasDiagnosticCode Code;

        /// <summary>
        /// Diagnostic source location.
        /// </summary>
        public readonly AtlasDiagnosticLocation Location;

        /// <summary>
        /// Human-readable diagnostic message.
        /// </summary>
        /// <remarks>
        /// The message is not diagnostic identity. It is allowed to change for clarity without
        /// changing <see cref="Code"/>.
        /// </remarks>
        public readonly FixedString512Bytes Message;

        private AtlasDiagnostic(
            AtlasDiagnosticSeverity severity,
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            Severity = severity;
            Code = code;
            Location = location;
            Message = message;
        }

        /// <summary>
        /// Gets whether this value is the reserved empty diagnostic.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Severity == AtlasDiagnosticSeverity.None &&
                   Code == AtlasDiagnosticCode.Empty &&
                   Location == AtlasDiagnosticLocation.Empty &&
                   Message.IsEmpty;
        }

        /// <summary>
        /// Gets whether this value is valid for a produced diagnostic.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Severity.IsValid() &&
                   Code.IsValid &&
                   Location.IsValid &&
                   !Message.IsEmpty;
        }

        /// <summary>
        /// Gets whether this diagnostic represents a failed operation.
        /// </summary>
        public bool IsFailure
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Severity.IsFailure();
        }

        /// <summary>
        /// Gets whether this diagnostic represents a non-failing report.
        /// </summary>
        public bool IsNonFailure
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Severity.IsNonFailure();
        }

        /// <summary>
        /// Gets whether this diagnostic should normally stop the current validation or compilation pass.
        /// </summary>
        public bool ShouldStopPass
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Severity.ShouldStopPass();
        }

        /// <summary>
        /// Creates a validated diagnostic.
        /// </summary>
        /// <param name="severity">Diagnostic severity.</param>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>A validated diagnostic.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when severity, code, location, or message is not valid for a produced diagnostic.
        /// </exception>
        public static AtlasDiagnostic Create(
            AtlasDiagnosticSeverity severity,
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            var diagnostic = new AtlasDiagnostic(
                severity,
                code,
                location,
                message);

            diagnostic.ValidateOrThrow(nameof(diagnostic));
            return diagnostic;
        }

        /// <summary>
        /// Creates an informational diagnostic.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>A validated informational diagnostic.</returns>
        public static AtlasDiagnostic Info(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Create(
                AtlasDiagnosticSeverity.Info,
                code,
                location,
                message);
        }

        /// <summary>
        /// Creates a warning diagnostic.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>A validated warning diagnostic.</returns>
        public static AtlasDiagnostic Warning(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Create(
                AtlasDiagnosticSeverity.Warning,
                code,
                location,
                message);
        }

        /// <summary>
        /// Creates an error diagnostic.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>A validated error diagnostic.</returns>
        public static AtlasDiagnostic Error(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Create(
                AtlasDiagnosticSeverity.Error,
                code,
                location,
                message);
        }

        /// <summary>
        /// Creates a fatal diagnostic.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>A validated fatal diagnostic.</returns>
        public static AtlasDiagnostic Fatal(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Create(
                AtlasDiagnosticSeverity.Fatal,
                code,
                location,
                message);
        }

        /// <summary>
        /// Creates a copy of this diagnostic with a replacement severity.
        /// </summary>
        /// <param name="severity">Replacement severity.</param>
        /// <returns>A validated diagnostic.</returns>
        public AtlasDiagnostic WithSeverity(AtlasDiagnosticSeverity severity)
        {
            return Create(
                severity,
                Code,
                Location,
                Message);
        }

        /// <summary>
        /// Creates a copy of this diagnostic with a replacement code.
        /// </summary>
        /// <param name="code">Replacement diagnostic code.</param>
        /// <returns>A validated diagnostic.</returns>
        public AtlasDiagnostic WithCode(AtlasDiagnosticCode code)
        {
            return Create(
                Severity,
                code,
                Location,
                Message);
        }

        /// <summary>
        /// Creates a copy of this diagnostic with a replacement location.
        /// </summary>
        /// <param name="location">Replacement diagnostic location.</param>
        /// <returns>A validated diagnostic.</returns>
        public AtlasDiagnostic WithLocation(AtlasDiagnosticLocation location)
        {
            return Create(
                Severity,
                Code,
                location,
                Message);
        }

        /// <summary>
        /// Creates a copy of this diagnostic with a replacement message.
        /// </summary>
        /// <param name="message">Replacement diagnostic message.</param>
        /// <returns>A validated diagnostic.</returns>
        public AtlasDiagnostic WithMessage(FixedString512Bytes message)
        {
            return Create(
                Severity,
                Code,
                Location,
                message);
        }

        /// <summary>
        /// Throws when this diagnostic is not valid for a produced diagnostic.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when severity, code, location, or message is not valid.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            if (!Severity.IsValid())
            {
                throw new ArgumentException(
                    "Atlas diagnostic requires a valid produced-diagnostic severity.",
                    parameterName ?? nameof(AtlasDiagnostic));
            }

            if (!Code.IsValid)
            {
                throw new ArgumentException(
                    "Atlas diagnostic requires a valid diagnostic code.",
                    parameterName ?? nameof(AtlasDiagnostic));
            }

            if (!Location.IsValid)
            {
                throw new ArgumentException(
                    "Atlas diagnostic requires a valid diagnostic location.",
                    parameterName ?? nameof(AtlasDiagnostic));
            }

            if (Message.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas diagnostic requires a non-empty message.",
                    parameterName ?? nameof(AtlasDiagnostic));
            }
        }

        /// <summary>
        /// Determines whether this diagnostic equals another diagnostic.
        /// </summary>
        /// <param name="other">Diagnostic to compare against.</param>
        /// <returns>
        /// <c>true</c> when severity, code, location, and message match; otherwise <c>false</c>.
        /// </returns>
        public bool Equals(AtlasDiagnostic other)
        {
            return Severity == other.Severity &&
                   Code == other.Code &&
                   Location == other.Location &&
                   Message.Equals(other.Message);
        }

        /// <summary>
        /// Determines whether this diagnostic equals an object instance.
        /// </summary>
        /// <param name="obj">Object instance to compare against.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasDiagnostic"/> with
        /// the same payload; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasDiagnostic other && Equals(other);
        }

        /// <summary>
        /// Returns a managed hash code for this diagnostic.
        /// </summary>
        /// <returns>A managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;

                hash = (hash * HashMultiplier) ^ Severity.GetHashCode();
                hash = (hash * HashMultiplier) ^ Code.GetHashCode();
                hash = (hash * HashMultiplier) ^ Location.GetHashCode();
                hash = (hash * HashMultiplier) ^ Message.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a human-readable diagnostic line.
        /// </summary>
        /// <returns>A diagnostic line suitable for logs, editor output, and test failure messages.</returns>
        public override string ToString()
        {
            return IsValid
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1}: {2} at {3}",
                    Severity,
                    Code,
                    Message.ToString(),
                    Location.ToString())
                : "<empty diagnostic>";
        }

        /// <summary>
        /// Determines whether two diagnostics are equal.
        /// </summary>
        public static bool operator ==(
            AtlasDiagnostic left,
            AtlasDiagnostic right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two diagnostics are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasDiagnostic left,
            AtlasDiagnostic right)
        {
            return !left.Equals(right);
        }
    }
}