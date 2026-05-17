// Packages/com.lokrain.atlas/Runtime/Diagnostics/AtlasDiagnosticBuffer.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Provide an ordered managed buffer for Atlas diagnostics.
// - Accumulate compiler, validator, editor, and CI diagnostics without throwing.
// - Preserve diagnostic order deterministically.
// - Provide failure/severity queries needed by TryCompile-style APIs.
//
// Design notes
// - This is a managed metadata container.
// - This type must not be passed into Burst jobs.
// - This type intentionally uses List<T>; diagnostic counts are small and tooling-oriented.
// - Diagnostic order is insertion order.
// - This buffer owns diagnostics only, not compiler state, workspace memory, or execution state.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Collections;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Ordered managed accumulator for Atlas diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasDiagnosticBuffer"/> is the central mutable diagnostics container for
    /// non-throwing compiler, validator, editor, and CI flows. It preserves diagnostics in
    /// insertion order and exposes aggregate severity/failure queries.
    /// </para>
    ///
    /// <para>
    /// This container is deliberately managed. Diagnostics are metadata and may carry formatted
    /// messages for tooling. Runtime jobs should receive compact execution payloads, not diagnostic
    /// buffers.
    /// </para>
    /// </remarks>
    public sealed class AtlasDiagnosticBuffer :
        IReadOnlyList<AtlasDiagnostic>
    {
        private const int DefaultCapacity = 16;
        private const int InvalidIndex = -1;

        private readonly List<AtlasDiagnostic> _diagnostics;

        /// <summary>
        /// Creates an empty diagnostic buffer with the default capacity.
        /// </summary>
        public AtlasDiagnosticBuffer()
            : this(DefaultCapacity)
        {
        }

        /// <summary>
        /// Creates an empty diagnostic buffer with a requested initial capacity.
        /// </summary>
        /// <param name="capacity">Initial diagnostic capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="capacity"/> is negative.
        /// </exception>
        public AtlasDiagnosticBuffer(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Atlas diagnostic buffer capacity must be non-negative.");
            }

            _diagnostics = new List<AtlasDiagnostic>(capacity);
        }

        /// <summary>
        /// Gets the number of diagnostics in the buffer.
        /// </summary>
        public int Count => _diagnostics.Count;

        /// <summary>
        /// Gets whether the buffer contains no diagnostics.
        /// </summary>
        public bool IsEmpty => _diagnostics.Count == 0;

        /// <summary>
        /// Gets whether the buffer contains at least one warning diagnostic.
        /// </summary>
        public bool HasWarnings => WarningCount > 0;

        /// <summary>
        /// Gets whether the buffer contains at least one error or fatal diagnostic.
        /// </summary>
        public bool HasFailures => FailureCount > 0;

        /// <summary>
        /// Gets whether the buffer contains at least one fatal diagnostic.
        /// </summary>
        public bool HasFatal => FatalCount > 0;

        /// <summary>
        /// Gets whether the current pass should normally stop.
        /// </summary>
        public bool ShouldStopPass => HasFatal;

        /// <summary>
        /// Gets the highest severity currently present in the buffer.
        /// </summary>
        public AtlasDiagnosticSeverity HighestSeverity
        {
            get
            {
                var highest = AtlasDiagnosticSeverity.None;

                for (var i = 0; i < _diagnostics.Count; i++)
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

                for (var i = 0; i < _diagnostics.Count; i++)
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
        /// Creates a diagnostic buffer with the default capacity.
        /// </summary>
        /// <returns>An empty diagnostic buffer.</returns>
        public static AtlasDiagnosticBuffer Create()
        {
            return new AtlasDiagnosticBuffer();
        }

        /// <summary>
        /// Creates a diagnostic buffer with a requested initial capacity.
        /// </summary>
        /// <param name="capacity">Initial diagnostic capacity.</param>
        /// <returns>An empty diagnostic buffer.</returns>
        public static AtlasDiagnosticBuffer Create(int capacity)
        {
            return new AtlasDiagnosticBuffer(capacity);
        }

        /// <summary>
        /// Removes all diagnostics from the buffer.
        /// </summary>
        public void Clear()
        {
            _diagnostics.Clear();
        }

        /// <summary>
        /// Adds a validated diagnostic to the buffer.
        /// </summary>
        /// <param name="diagnostic">Diagnostic to add.</param>
        /// <returns>The insertion index of the added diagnostic.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="diagnostic"/> is not valid for a produced diagnostic.
        /// </exception>
        public int Add(AtlasDiagnostic diagnostic)
        {
            diagnostic.ValidateOrThrow(nameof(diagnostic));

            var index = _diagnostics.Count;
            _diagnostics.Add(diagnostic);
            return index;
        }

        /// <summary>
        /// Creates and adds a diagnostic to the buffer.
        /// </summary>
        /// <param name="severity">Diagnostic severity.</param>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>The insertion index of the added diagnostic.</returns>
        public int Add(
            AtlasDiagnosticSeverity severity,
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Add(AtlasDiagnostic.Create(
                severity,
                code,
                location,
                message));
        }

        /// <summary>
        /// Creates and adds an informational diagnostic to the buffer.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>The insertion index of the added diagnostic.</returns>
        public int AddInfo(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Add(AtlasDiagnostic.Info(
                code,
                location,
                message));
        }

        /// <summary>
        /// Creates and adds a warning diagnostic to the buffer.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>The insertion index of the added diagnostic.</returns>
        public int AddWarning(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Add(AtlasDiagnostic.Warning(
                code,
                location,
                message));
        }

        /// <summary>
        /// Creates and adds an error diagnostic to the buffer.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>The insertion index of the added diagnostic.</returns>
        public int AddError(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Add(AtlasDiagnostic.Error(
                code,
                location,
                message));
        }

        /// <summary>
        /// Creates and adds a fatal diagnostic to the buffer.
        /// </summary>
        /// <param name="code">Stable diagnostic code.</param>
        /// <param name="location">Diagnostic source location.</param>
        /// <param name="message">Human-readable diagnostic message.</param>
        /// <returns>The insertion index of the added diagnostic.</returns>
        public int AddFatal(
            AtlasDiagnosticCode code,
            AtlasDiagnosticLocation location,
            FixedString512Bytes message)
        {
            return Add(AtlasDiagnostic.Fatal(
                code,
                location,
                message));
        }

        /// <summary>
        /// Adds every diagnostic from another diagnostic sequence.
        /// </summary>
        /// <param name="diagnostics">Diagnostics to append in enumeration order.</param>
        /// <returns>The number of diagnostics added.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="diagnostics"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when any diagnostic is invalid.
        /// </exception>
        public int AddRange(IEnumerable<AtlasDiagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var added = 0;

            foreach (var diagnostic in diagnostics)
            {
                Add(diagnostic);
                added++;
            }

            return added;
        }

        /// <summary>
        /// Returns whether the buffer contains a diagnostic with the specified code.
        /// </summary>
        /// <param name="code">Diagnostic code to search for.</param>
        /// <returns><c>true</c> when a matching diagnostic exists; otherwise <c>false</c>.</returns>
        public bool ContainsCode(AtlasDiagnosticCode code)
        {
            code.ValidateOrThrow(nameof(code));

            return IndexOfFirst(code) != InvalidIndex;
        }

        /// <summary>
        /// Returns the first index of a diagnostic with the specified code.
        /// </summary>
        /// <param name="code">Diagnostic code to search for.</param>
        /// <returns>The first matching index, or <c>-1</c> when no diagnostic matches.</returns>
        public int IndexOfFirst(AtlasDiagnosticCode code)
        {
            code.ValidateOrThrow(nameof(code));

            for (var i = 0; i < _diagnostics.Count; i++)
            {
                if (_diagnostics[i].Code == code)
                {
                    return i;
                }
            }

            return InvalidIndex;
        }

        /// <summary>
        /// Returns the first index of a diagnostic with the specified severity.
        /// </summary>
        /// <param name="severity">Diagnostic severity to search for.</param>
        /// <returns>The first matching index, or <c>-1</c> when no diagnostic matches.</returns>
        public int IndexOfFirst(AtlasDiagnosticSeverity severity)
        {
            if (!severity.IsValid())
            {
                throw new ArgumentException(
                    "Atlas diagnostic severity search requires a valid produced-diagnostic severity.",
                    nameof(severity));
            }

            for (var i = 0; i < _diagnostics.Count; i++)
            {
                if (_diagnostics[i].Severity == severity)
                {
                    return i;
                }
            }

            return InvalidIndex;
        }

        /// <summary>
        /// Returns the first index of an error or fatal diagnostic.
        /// </summary>
        /// <returns>The first failure index, or <c>-1</c> when no failure exists.</returns>
        public int IndexOfFirstFailure()
        {
            for (var i = 0; i < _diagnostics.Count; i++)
            {
                if (_diagnostics[i].IsFailure)
                {
                    return i;
                }
            }

            return InvalidIndex;
        }

        /// <summary>
        /// Attempts to get the first error or fatal diagnostic.
        /// </summary>
        /// <param name="diagnostic">First failure diagnostic when one exists; otherwise <see cref="AtlasDiagnostic.Empty"/>.</param>
        /// <returns><c>true</c> when a failure exists; otherwise <c>false</c>.</returns>
        public bool TryGetFirstFailure(out AtlasDiagnostic diagnostic)
        {
            var index = IndexOfFirstFailure();

            if (index == InvalidIndex)
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
        /// Thrown when the buffer contains no error or fatal diagnostic.
        /// </exception>
        public AtlasDiagnostic GetRequiredFirstFailure()
        {
            if (TryGetFirstFailure(out var diagnostic))
            {
                return diagnostic;
            }

            throw new InvalidOperationException(
                "Atlas diagnostic buffer contains no failure diagnostic.");
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

            for (var i = 0; i < _diagnostics.Count; i++)
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
        /// Thrown when the destination does not have enough available space.
        /// </exception>
        public void CopyTo(
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

            if (destination.Length - destinationIndex < _diagnostics.Count)
            {
                throw new ArgumentException(
                    "Destination array does not have enough available space for Atlas diagnostics.",
                    nameof(destination));
            }

            for (var i = 0; i < _diagnostics.Count; i++)
            {
                destination[destinationIndex + i] = _diagnostics[i];
            }
        }

        /// <summary>
        /// Returns diagnostics as a new array in insertion order.
        /// </summary>
        /// <returns>A new diagnostic array.</returns>
        public AtlasDiagnostic[] ToArray()
        {
            return _diagnostics.ToArray();
        }

        /// <summary>
        /// Returns a multi-line diagnostic report.
        /// </summary>
        /// <returns>A deterministic diagnostic report string.</returns>
        public string ToReportString()
        {
            if (_diagnostics.Count == 0)
            {
                return "Atlas diagnostics: none.";
            }

            var builder = new StringBuilder();

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "Atlas diagnostics: {0} total, {1} info, {2} warning, {3} error, {4} fatal.",
                Count,
                InfoCount,
                WarningCount,
                ErrorCount,
                FatalCount);

            for (var i = 0; i < _diagnostics.Count; i++)
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
        /// Returns the enumerator over diagnostics in insertion order.
        /// </summary>
        /// <returns>A diagnostic enumerator.</returns>
        public IEnumerator<AtlasDiagnostic> GetEnumerator()
        {
            return _diagnostics.GetEnumerator();
        }

        /// <summary>
        /// Returns the non-generic enumerator over diagnostics in insertion order.
        /// </summary>
        /// <returns>A diagnostic enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a compact diagnostic buffer summary.
        /// </summary>
        /// <returns>A compact summary string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasDiagnosticBuffer(Count={0}, Info={1}, Warning={2}, Error={3}, Fatal={4})",
                Count,
                InfoCount,
                WarningCount,
                ErrorCount,
                FatalCount);
        }
    }
}