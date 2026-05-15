// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasCompilationResult.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent the result of a non-throwing Atlas compilation pass.
// - Carry the compiled plan when compilation succeeds.
// - Carry immutable diagnostic snapshots for editor, CI, and validation tooling.
// - Make TryCompile-style APIs honest without exposing mutable diagnostic buffers.
//
// Design notes
// - This is managed metadata, not Burst/job payload.
// - Success may contain warnings and informational diagnostics.
// - Failure must contain at least one error or fatal diagnostic.
// - Failed results do not expose a usable compiled plan.
// - Diagnostics are copied into a private array to prevent accidental external mutation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Immutable externally visible result of an Atlas compilation pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasCompilationResult"/> is the result contract for non-throwing compiler
    /// entry points. It separates control flow from exceptions by returning either a usable
    /// compiled plan or a deterministic diagnostic report.
    /// </para>
    ///
    /// <para>
    /// A successful result may still contain warnings. A failed result must contain at least one
    /// error or fatal diagnostic and does not expose a usable <see cref="AtlasCompiledPlan"/>.
    /// </para>
    ///
    /// <para>
    /// The result snapshots diagnostics into a private array. The mutable
    /// <see cref="AtlasDiagnosticBuffer"/> remains an internal accumulation tool for compiler
    /// and validator passes.
    /// </para>
    /// </remarks>
    public sealed class AtlasCompilationResult :
        IReadOnlyList<AtlasDiagnostic>
    {
        private static readonly AtlasDiagnostic[] NoDiagnostics = Array.Empty<AtlasDiagnostic>();

        private readonly AtlasDiagnostic[] _diagnostics;

        private AtlasCompilationResult(
            AtlasCompiledPlan plan,
            AtlasDiagnostic[] diagnostics)
        {
            Plan = plan;
            _diagnostics = diagnostics ?? NoDiagnostics;
        }

        /// <summary>
        /// Gets the compiled plan when compilation succeeded.
        /// </summary>
        /// <remarks>
        /// This value is <c>null</c> for failed results. Use <see cref="GetRequiredPlan"/> when
        /// the caller wants exception-based access to a successful plan.
        /// </remarks>
        public AtlasCompiledPlan Plan { get; }

        /// <summary>
        /// Gets the number of diagnostics captured by the result.
        /// </summary>
        public int Count => _diagnostics.Length;

        /// <summary>
        /// Gets whether the result contains no diagnostics.
        /// </summary>
        public bool IsEmpty => _diagnostics.Length == 0;

        /// <summary>
        /// Gets whether compilation produced a usable compiled plan.
        /// </summary>
        public bool Succeeded => Plan != null && !HasFailures;

        /// <summary>
        /// Gets whether compilation failed.
        /// </summary>
        public bool Failed => !Succeeded;

        /// <summary>
        /// Gets whether the result contains a non-null compiled plan.
        /// </summary>
        public bool HasPlan => Plan != null;

        /// <summary>
        /// Gets whether the result contains at least one diagnostic.
        /// </summary>
        public bool HasDiagnostics => _diagnostics.Length > 0;

        /// <summary>
        /// Gets whether the result contains at least one warning diagnostic.
        /// </summary>
        public bool HasWarnings => WarningCount > 0;

        /// <summary>
        /// Gets whether the result contains at least one error or fatal diagnostic.
        /// </summary>
        public bool HasFailures => FailureCount > 0;

        /// <summary>
        /// Gets whether the result contains at least one fatal diagnostic.
        /// </summary>
        public bool HasFatal => FatalCount > 0;

        /// <summary>
        /// Gets whether the compilation pass should normally stop.
        /// </summary>
        public bool ShouldStopPass => HasFatal;

        /// <summary>
        /// Gets the highest diagnostic severity captured by the result.
        /// </summary>
        public AtlasDiagnosticSeverity HighestSeverity
        {
            get
            {
                var highest = AtlasDiagnosticSeverity.None;

                for (var i = 0; i < _diagnostics.Length; i++)
                {
                    var severity = _diagnostics[i].Severity;

                    if (severity > highest)
                    {
                        highest = severity;
                    }
                }

                return highest;
            }
        }

        /// <summary>
        /// Gets the number of informational diagnostics.
        /// </summary>
        public int InfoCount => CountSeverity(AtlasDiagnosticSeverity.Info);

        /// <summary>
        /// Gets the number of warning diagnostics.
        /// </summary>
        public int WarningCount => CountSeverity(AtlasDiagnosticSeverity.Warning);

        /// <summary>
        /// Gets the number of error diagnostics.
        /// </summary>
        public int ErrorCount => CountSeverity(AtlasDiagnosticSeverity.Error);

        /// <summary>
        /// Gets the number of fatal diagnostics.
        /// </summary>
        public int FatalCount => CountSeverity(AtlasDiagnosticSeverity.Fatal);

        /// <summary>
        /// Gets the number of error and fatal diagnostics.
        /// </summary>
        public int FailureCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _diagnostics.Length; i++)
                {
                    if (_diagnostics[i].IsFailure)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the diagnostic at the specified insertion-order index.
        /// </summary>
        /// <param name="index">Diagnostic index.</param>
        /// <returns>The diagnostic at <paramref name="index"/>.</returns>
        public AtlasDiagnostic this[int index] => _diagnostics[index];

        /// <summary>
        /// Creates a successful compilation result without diagnostics.
        /// </summary>
        /// <param name="plan">Compiled plan produced by compilation.</param>
        /// <returns>A successful compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="plan"/> is <c>null</c>.
        /// </exception>
        public static AtlasCompilationResult Success(AtlasCompiledPlan plan)
        {
            return Success(
                plan,
                NoDiagnostics);
        }

        /// <summary>
        /// Creates a successful compilation result with diagnostics.
        /// </summary>
        /// <param name="plan">Compiled plan produced by compilation.</param>
        /// <param name="diagnostics">Diagnostics produced during compilation.</param>
        /// <returns>A successful compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="plan"/> or <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when diagnostics contain an error or fatal diagnostic.
        /// </exception>
        public static AtlasCompilationResult Success(
            AtlasCompiledPlan plan,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            return Success(
                plan,
                diagnostics.ToArray());
        }

        /// <summary>
        /// Creates a successful compilation result with diagnostics.
        /// </summary>
        /// <param name="plan">Compiled plan produced by compilation.</param>
        /// <param name="diagnostics">Diagnostics produced during compilation.</param>
        /// <returns>A successful compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="plan"/> or <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when diagnostics contain an error or fatal diagnostic.
        /// </exception>
        public static AtlasCompilationResult Success(
            AtlasCompiledPlan plan,
            IEnumerable<AtlasDiagnostic> diagnostics)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var snapshot = CopyDiagnostics(diagnostics);

            if (ContainsFailure(snapshot))
            {
                throw new ArgumentException(
                    "A successful Atlas compilation result cannot contain error or fatal diagnostics.",
                    nameof(diagnostics));
            }

            return new AtlasCompilationResult(
                plan,
                snapshot);
        }

        /// <summary>
        /// Creates a failed compilation result.
        /// </summary>
        /// <param name="diagnostics">Diagnostics produced during failed compilation.</param>
        /// <returns>A failed compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when diagnostics contain no error or fatal diagnostic.
        /// </exception>
        public static AtlasCompilationResult Failure(AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            return Failure(diagnostics.ToArray());
        }

        /// <summary>
        /// Creates a failed compilation result.
        /// </summary>
        /// <param name="diagnostics">Diagnostics produced during failed compilation.</param>
        /// <returns>A failed compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when diagnostics contain no error or fatal diagnostic.
        /// </exception>
        public static AtlasCompilationResult Failure(IEnumerable<AtlasDiagnostic> diagnostics)
        {
            var snapshot = CopyDiagnostics(diagnostics);

            if (!ContainsFailure(snapshot))
            {
                throw new ArgumentException(
                    "A failed Atlas compilation result requires at least one error or fatal diagnostic.",
                    nameof(diagnostics));
            }

            return new AtlasCompilationResult(
                null,
                snapshot);
        }

        /// <summary>
        /// Creates a result from a plan and diagnostics using normal compilation semantics.
        /// </summary>
        /// <param name="plan">Compiled plan, or <c>null</c> when compilation did not produce one.</param>
        /// <param name="diagnostics">Diagnostics produced during compilation.</param>
        /// <returns>
        /// A successful result when <paramref name="plan"/> is non-null and diagnostics contain no
        /// failures; otherwise a failed result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when no plan exists and diagnostics contain no failure.
        /// </exception>
        public static AtlasCompilationResult From(
            AtlasCompiledPlan plan,
            AtlasDiagnosticBuffer diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            return From(
                plan,
                diagnostics.ToArray());
        }

        /// <summary>
        /// Creates a result from a plan and diagnostics using normal compilation semantics.
        /// </summary>
        /// <param name="plan">Compiled plan, or <c>null</c> when compilation did not produce one.</param>
        /// <param name="diagnostics">Diagnostics produced during compilation.</param>
        /// <returns>
        /// A successful result when <paramref name="plan"/> is non-null and diagnostics contain no
        /// failures; otherwise a failed result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when no plan exists and diagnostics contain no failure.
        /// </exception>
        public static AtlasCompilationResult From(
            AtlasCompiledPlan plan,
            IEnumerable<AtlasDiagnostic> diagnostics)
        {
            var snapshot = CopyDiagnostics(diagnostics);

            if (ContainsFailure(snapshot))
            {
                return new AtlasCompilationResult(
                    null,
                    snapshot);
            }

            if (plan == null)
            {
                throw new ArgumentException(
                    "Atlas compilation result requires a compiled plan when diagnostics contain no failures.",
                    nameof(plan));
            }

            return new AtlasCompilationResult(
                plan,
                snapshot);
        }

        /// <summary>
        /// Returns the compiled plan or throws when compilation failed.
        /// </summary>
        /// <returns>The compiled plan.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the result does not contain a successful compiled plan.
        /// </exception>
        public AtlasCompiledPlan GetRequiredPlan()
        {
            if (Succeeded)
            {
                return Plan;
            }

            throw new InvalidOperationException(
                ToReportString());
        }

        /// <summary>
        /// Throws when this result represents failed compilation.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation failed.
        /// </exception>
        public void ThrowIfFailed()
        {
            if (Succeeded)
            {
                return;
            }

            throw new InvalidOperationException(
                ToReportString());
        }

        /// <summary>
        /// Returns whether the result contains a diagnostic with the specified code.
        /// </summary>
        /// <param name="code">Diagnostic code to search for.</param>
        /// <returns><c>true</c> when a matching diagnostic exists; otherwise <c>false</c>.</returns>
        public bool ContainsCode(AtlasDiagnosticCode code)
        {
            code.ValidateOrThrow(nameof(code));

            return IndexOfFirst(code) != -1;
        }

        /// <summary>
        /// Returns the first index of a diagnostic with the specified code.
        /// </summary>
        /// <param name="code">Diagnostic code to search for.</param>
        /// <returns>The first matching diagnostic index, or <c>-1</c> when no diagnostic matches.</returns>
        public int IndexOfFirst(AtlasDiagnosticCode code)
        {
            code.ValidateOrThrow(nameof(code));

            for (var i = 0; i < _diagnostics.Length; i++)
            {
                if (_diagnostics[i].Code == code)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the first index of a diagnostic with the specified severity.
        /// </summary>
        /// <param name="severity">Diagnostic severity to search for.</param>
        /// <returns>The first matching diagnostic index, or <c>-1</c> when no diagnostic matches.</returns>
        public int IndexOfFirst(AtlasDiagnosticSeverity severity)
        {
            if (!severity.IsValid())
            {
                throw new ArgumentException(
                    "Atlas diagnostic severity search requires a valid produced-diagnostic severity.",
                    nameof(severity));
            }

            for (var i = 0; i < _diagnostics.Length; i++)
            {
                if (_diagnostics[i].Severity == severity)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the first index of an error or fatal diagnostic.
        /// </summary>
        /// <returns>The first failure index, or <c>-1</c> when no failure exists.</returns>
        public int IndexOfFirstFailure()
        {
            for (var i = 0; i < _diagnostics.Length; i++)
            {
                if (_diagnostics[i].IsFailure)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Attempts to get the first error or fatal diagnostic.
        /// </summary>
        /// <param name="diagnostic">
        /// First failure diagnostic when one exists; otherwise <see cref="AtlasDiagnostic.Empty"/>.
        /// </param>
        /// <returns><c>true</c> when a failure exists; otherwise <c>false</c>.</returns>
        public bool TryGetFirstFailure(out AtlasDiagnostic diagnostic)
        {
            var index = IndexOfFirstFailure();

            if (index < 0)
            {
                diagnostic = AtlasDiagnostic.Empty;
                return false;
            }

            diagnostic = _diagnostics[index];
            return true;
        }

        /// <summary>
        /// Gets the first error or fatal diagnostic.
        /// </summary>
        /// <returns>The first failure diagnostic.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the result contains no error or fatal diagnostic.
        /// </exception>
        public AtlasDiagnostic GetRequiredFirstFailure()
        {
            if (TryGetFirstFailure(out var diagnostic))
            {
                return diagnostic;
            }

            throw new InvalidOperationException(
                "Atlas compilation result contains no failure diagnostic.");
        }

        /// <summary>
        /// Counts diagnostics with a specific severity.
        /// </summary>
        /// <param name="severity">Severity to count.</param>
        /// <returns>Number of diagnostics with the requested severity.</returns>
        public int CountSeverity(AtlasDiagnosticSeverity severity)
        {
            if (!severity.IsValid())
            {
                throw new ArgumentException(
                    "Atlas diagnostic severity count requires a valid produced-diagnostic severity.",
                    nameof(severity));
            }

            var count = 0;

            for (var i = 0; i < _diagnostics.Length; i++)
            {
                if (_diagnostics[i].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies diagnostics into an existing array.
        /// </summary>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="destinationIndex"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when destination does not have enough available space.
        /// </exception>
        public void CopyDiagnosticsTo(
            AtlasDiagnostic[] destination,
            int destinationIndex = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(destinationIndex),
                    destinationIndex,
                    "Destination index must be non-negative.");
            }

            if (destination.Length - destinationIndex < _diagnostics.Length)
            {
                throw new ArgumentException(
                    "Destination array does not have enough available space for Atlas compilation diagnostics.",
                    nameof(destination));
            }

            for (var i = 0; i < _diagnostics.Length; i++)
            {
                destination[destinationIndex + i] = _diagnostics[i];
            }
        }

        /// <summary>
        /// Returns diagnostics as a new array in insertion order.
        /// </summary>
        /// <returns>A new diagnostic array.</returns>
        public AtlasDiagnostic[] ToDiagnosticArray()
        {
            var copy = new AtlasDiagnostic[_diagnostics.Length];

            for (var i = 0; i < _diagnostics.Length; i++)
            {
                copy[i] = _diagnostics[i];
            }

            return copy;
        }

        /// <summary>
        /// Returns a multi-line compilation result report.
        /// </summary>
        /// <returns>A deterministic compilation report string.</returns>
        public string ToReportString()
        {
            var builder = new StringBuilder();

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "Atlas compilation {0}: {1} diagnostics, {2} info, {3} warning, {4} error, {5} fatal.",
                Succeeded ? "succeeded" : "failed",
                Count,
                InfoCount,
                WarningCount,
                ErrorCount,
                FatalCount);

            if (Plan != null)
            {
                builder.AppendLine();
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "Plan: {0}",
                    Plan.ToString());
            }

            for (var i = 0; i < _diagnostics.Length; i++)
            {
                builder.AppendLine();
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "[{0}] {1}",
                    i,
                    _diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns diagnostics in insertion order.
        /// </summary>
        /// <returns>A diagnostic enumerator.</returns>
        public IEnumerator<AtlasDiagnostic> GetEnumerator()
        {
            for (var i = 0; i < _diagnostics.Length; i++)
            {
                yield return _diagnostics[i];
            }
        }

        /// <summary>
        /// Returns diagnostics in insertion order.
        /// </summary>
        /// <returns>A diagnostic enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a compact compilation result summary.
        /// </summary>
        /// <returns>A compact result summary.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasCompilationResult(Succeeded={0}, Diagnostics={1}, Info={2}, Warning={3}, Error={4}, Fatal={5})",
                Succeeded,
                Count,
                InfoCount,
                WarningCount,
                ErrorCount,
                FatalCount);
        }

        private static AtlasDiagnostic[] CopyDiagnostics(IEnumerable<AtlasDiagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var list = new List<AtlasDiagnostic>();

            foreach (var diagnostic in diagnostics)
            {
                diagnostic.ValidateOrThrow(nameof(diagnostics));
                list.Add(diagnostic);
            }

            if (list.Count == 0)
            {
                return NoDiagnostics;
            }

            return list.ToArray();
        }

        private static bool ContainsFailure(AtlasDiagnostic[] diagnostics)
        {
            for (var i = 0; i < diagnostics.Length; i++)
            {
                if (diagnostics[i].IsFailure)
                {
                    return true;
                }
            }

            return false;
        }
    }
}