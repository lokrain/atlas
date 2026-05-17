// Packages/com.lokrain.atlas/Runtime/Diagnostics/AtlasDiagnosticCode.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Define stable diagnostic code identity for Atlas validation and compilation reports.
// - Avoid one giant enum that must own every future diagnostic forever.
// - Provide compact packed representation for logs, editor tooling, tests, and CI.
// - Keep diagnostic code separate from severity, location, and human-readable message.
//
// Design notes
// - Diagnostic code identity is domain + numeric code.
// - Domain identifies the Atlas subsystem that owns the diagnostic.
// - Number identifies the stable diagnostic inside that subsystem.
// - Code zero is reserved for default/empty values.
// - This type is metadata only and must not be passed into Burst jobs as part of execution payloads.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Stable diagnostic domain for Atlas diagnostic codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The domain identifies the subsystem that owns a diagnostic code. It is not a severity,
    /// not a location, and not a category of runtime data.
    /// </para>
    ///
    /// <para>
    /// Numeric values are part of the public diagnostic contract. Do not reorder existing
    /// values after release. Add new domains only when a subsystem needs its own diagnostic
    /// ownership space.
    /// </para>
    /// </remarks>
    public enum AtlasDiagnosticDomain : byte
    {
        /// <summary>
        /// No diagnostic domain.
        /// </summary>
        None = 0,

        /// <summary>
        /// Core value-object and package invariant diagnostics.
        /// </summary>
        Core = 1,

        /// <summary>
        /// Field declaration diagnostics.
        /// </summary>
        Fields = 2,

        /// <summary>
        /// Contract and Contract-table diagnostics.
        /// </summary>
        Contracts = 3,

        /// <summary>
        /// Operation definition and operation access diagnostics.
        /// </summary>
        Operations = 4,

        /// <summary>
        /// Stage definition diagnostics.
        /// </summary>
        Stages = 5,

        /// <summary>
        /// Pipeline definition diagnostics.
        /// </summary>
        Pipelines = 6,

        /// <summary>
        /// Compilation and compiled metadata diagnostics.
        /// </summary>
        Compilation = 7,

        /// <summary>
        /// Validation-pass diagnostics that are not owned by a narrower subsystem.
        /// </summary>
        Validation = 8,

        /// <summary>
        /// Workspace, memory resolution, and execution diagnostics.
        /// </summary>
        Execution = 9,

        /// <summary>
        /// Durable artifact diagnostics.
        /// </summary>
        Artifacts = 10,

        /// <summary>
        /// Diagnostic-system self-validation diagnostics.
        /// </summary>
        Diagnostics = 11
    }

    /// <summary>
    /// Stable diagnostic code for Atlas reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDiagnosticCode"/> is a compact stable identity for a diagnostic.
    /// It answers what happened, not where it happened and not how severe it is.
    /// </para>
    ///
    /// <para>
    /// The packed representation reserves the high 8 bits for <see cref="AtlasDiagnosticDomain"/>
    /// and the low 24 bits for the subsystem-owned code number.
    /// </para>
    ///
    /// <para>
    /// Human-readable messages may change for clarity. Diagnostic codes should not change
    /// unless the underlying invariant or diagnostic meaning changes.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasDiagnosticCode :
        IEquatable<AtlasDiagnosticCode>,
        IComparable<AtlasDiagnosticCode>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 31;
        private const uint DomainShift = 24;
        private const uint NumberMask = 0x00FFFFFFU;

        /// <summary>
        /// Maximum diagnostic number inside one diagnostic domain.
        /// </summary>
        public const uint MaxNumber = NumberMask;

        /// <summary>
        /// Reserved empty diagnostic code.
        /// </summary>
        public static readonly AtlasDiagnosticCode Empty = default;

        /// <summary>
        /// Diagnostic subsystem that owns the code.
        /// </summary>
        public readonly AtlasDiagnosticDomain Domain;

        /// <summary>
        /// Stable diagnostic number inside <see cref="Domain"/>.
        /// </summary>
        /// <remarks>
        /// Zero is reserved. Valid diagnostic codes use values in the range 1..16777215.
        /// </remarks>
        public readonly uint Number;

        /// <summary>
        /// Creates a diagnostic code from explicit domain and number components.
        /// </summary>
        /// <param name="domain">Subsystem domain that owns the diagnostic.</param>
        /// <param name="number">Stable diagnostic number inside the domain.</param>
        public AtlasDiagnosticCode(
            AtlasDiagnosticDomain domain,
            uint number)
        {
            Domain = domain;
            Number = number;
        }

        /// <summary>
        /// Gets whether this value is the reserved empty diagnostic code.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Domain == AtlasDiagnosticDomain.None && Number == 0U;
        }

        /// <summary>
        /// Gets whether this value is valid for a produced diagnostic.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Domain.IsValid() && Number > 0U && Number <= MaxNumber;
        }

        /// <summary>
        /// Gets the compact packed representation of this diagnostic code.
        /// </summary>
        /// <remarks>
        /// The high 8 bits contain <see cref="Domain"/> and the low 24 bits contain
        /// <see cref="Number"/>. The empty diagnostic code packs to zero.
        /// </remarks>
        public uint Packed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((uint)Domain << (int)DomainShift) | (Number & NumberMask);
        }

        /// <summary>
        /// Creates a valid diagnostic code.
        /// </summary>
        /// <param name="domain">Subsystem domain that owns the diagnostic.</param>
        /// <param name="number">Stable diagnostic number inside the domain.</param>
        /// <returns>A validated diagnostic code.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="domain"/> is not valid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="number"/> is zero or exceeds <see cref="MaxNumber"/>.
        /// </exception>
        public static AtlasDiagnosticCode Create(
            AtlasDiagnosticDomain domain,
            uint number)
        {
            if (!domain.IsValid())
            {
                throw new ArgumentException(
                    "Atlas diagnostic code requires a valid diagnostic domain.",
                    nameof(domain));
            }

            if (number == 0U || number > MaxNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    number,
                    $"Atlas diagnostic code number must be in range 1..{MaxNumber.ToString(CultureInfo.InvariantCulture)}.");
            }

            return new AtlasDiagnosticCode(
                domain,
                number);
        }

        /// <summary>
        /// Creates a diagnostic code from its compact packed representation.
        /// </summary>
        /// <param name="packed">Packed diagnostic code.</param>
        /// <returns>A validated diagnostic code.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="packed"/> does not encode a valid diagnostic code.
        /// </exception>
        public static AtlasDiagnosticCode FromPacked(uint packed)
        {
            var domain = (AtlasDiagnosticDomain)(packed >> (int)DomainShift);
            var number = packed & NumberMask;

            var code = new AtlasDiagnosticCode(
                domain,
                number);

            code.ValidateOrThrow(nameof(packed));
            return code;
        }

        /// <summary>
        /// Creates a diagnostic code with the same domain and a different number.
        /// </summary>
        /// <param name="number">Replacement diagnostic number.</param>
        /// <returns>A validated diagnostic code.</returns>
        public AtlasDiagnosticCode WithNumber(uint number)
        {
            return Create(
                Domain,
                number);
        }

        /// <summary>
        /// Creates a diagnostic code with the same number and a different domain.
        /// </summary>
        /// <param name="domain">Replacement diagnostic domain.</param>
        /// <returns>A validated diagnostic code.</returns>
        public AtlasDiagnosticCode WithDomain(AtlasDiagnosticDomain domain)
        {
            return Create(
                domain,
                Number);
        }

        /// <summary>
        /// Throws when this diagnostic code is not valid for a produced diagnostic.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the diagnostic code has no valid domain or no valid number.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            if (IsValid)
            {
                return;
            }

            throw new ArgumentException(
                "Atlas diagnostic code must have a valid domain and a non-zero diagnostic number.",
                parameterName ?? nameof(AtlasDiagnosticCode));
        }

        /// <summary>
        /// Determines whether this code equals another diagnostic code.
        /// </summary>
        /// <param name="other">Diagnostic code to compare against.</param>
        /// <returns>
        /// <c>true</c> when domain and number match; otherwise <c>false</c>.
        /// </returns>
        public bool Equals(AtlasDiagnosticCode other)
        {
            return Domain == other.Domain &&
                   Number == other.Number;
        }

        /// <summary>
        /// Determines whether this code equals an object instance.
        /// </summary>
        /// <param name="obj">Object instance to compare against.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is an <see cref="AtlasDiagnosticCode"/>
        /// with the same domain and number; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is AtlasDiagnosticCode other && Equals(other);
        }

        /// <summary>
        /// Returns a managed hash code for this diagnostic code.
        /// </summary>
        /// <returns>A managed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;

                hash = (hash * HashMultiplier) + Domain.GetHashCode();
                hash = (hash * HashMultiplier) + Number.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Compares diagnostic codes using canonical diagnostic order.
        /// </summary>
        /// <param name="other">Diagnostic code to compare against.</param>
        /// <returns>
        /// A negative value when this code sorts before <paramref name="other"/>,
        /// zero when both codes are equal, and a positive value when this code sorts after it.
        /// </returns>
        public int CompareTo(AtlasDiagnosticCode other)
        {
            var domainComparison = Domain.CompareTo(other.Domain);

            return domainComparison != 0
                ? domainComparison
                : Number.CompareTo(other.Number);
        }

        /// <summary>
        /// Returns a stable human-readable diagnostic code label.
        /// </summary>
        /// <returns>A stable label in the form <c>ATL-DOMAIN-000001</c>.</returns>
        public override string ToString()
        {
            return IsValid
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "ATL-{0}-{1:D6}",
                    Domain.ToShortCode(),
                    Number)
                : "ATL-EMPTY-000000";
        }

        /// <summary>
        /// Determines whether two diagnostic codes are equal.
        /// </summary>
        public static bool operator ==(
            AtlasDiagnosticCode left,
            AtlasDiagnosticCode right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two diagnostic codes are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasDiagnosticCode left,
            AtlasDiagnosticCode right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left diagnostic code sorts before the right diagnostic code.
        /// </summary>
        public static bool operator <(
            AtlasDiagnosticCode left,
            AtlasDiagnosticCode right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left diagnostic code sorts after the right diagnostic code.
        /// </summary>
        public static bool operator >(
            AtlasDiagnosticCode left,
            AtlasDiagnosticCode right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left diagnostic code sorts before or equal to the right diagnostic code.
        /// </summary>
        public static bool operator <=(
            AtlasDiagnosticCode left,
            AtlasDiagnosticCode right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left diagnostic code sorts after or equal to the right diagnostic code.
        /// </summary>
        public static bool operator >=(
            AtlasDiagnosticCode left,
            AtlasDiagnosticCode right)
        {
            return left.CompareTo(right) >= 0;
        }
    }

    /// <summary>
    /// Extension helpers for <see cref="AtlasDiagnosticDomain"/>.
    /// </summary>
    public static class AtlasDiagnosticDomainExtensions
    {
        /// <summary>
        /// Returns whether the domain is valid for a produced diagnostic.
        /// </summary>
        /// <param name="domain">Domain to test.</param>
        /// <returns><c>true</c> when the domain is a defined non-empty Atlas diagnostic domain.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this AtlasDiagnosticDomain domain)
        {
            return domain >= AtlasDiagnosticDomain.Core &&
                   domain <= AtlasDiagnosticDomain.Diagnostics;
        }

        /// <summary>
        /// Returns the stable short label used in diagnostic code formatting.
        /// </summary>
        /// <param name="domain">Domain to format.</param>
        /// <returns>A stable short diagnostic domain label.</returns>
        public static string ToShortCode(this AtlasDiagnosticDomain domain)
        {
            return domain switch
            {
                AtlasDiagnosticDomain.Core => "CORE",
                AtlasDiagnosticDomain.Fields => "FIELD",
                AtlasDiagnosticDomain.Contracts => "CONTRACT",
                AtlasDiagnosticDomain.Operations => "OP",
                AtlasDiagnosticDomain.Stages => "STAGE",
                AtlasDiagnosticDomain.Pipelines => "PIPE",
                AtlasDiagnosticDomain.Compilation => "COMP",
                AtlasDiagnosticDomain.Validation => "VALID",
                AtlasDiagnosticDomain.Execution => "EXEC",
                AtlasDiagnosticDomain.Artifacts => "ART",
                AtlasDiagnosticDomain.Diagnostics => "DIAG",
                _ => "NONE"
            };
        }
    }
}