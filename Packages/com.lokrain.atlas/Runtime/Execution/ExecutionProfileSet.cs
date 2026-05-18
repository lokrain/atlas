#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents an immutable accepted set of managed execution profiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An execution profile set owns managed execution-policy identities indexed by profile symbol.
    /// </para>
    /// <para>
    /// The set validates metadata-level uniqueness. It does not allocate storage, own native memory,
    /// schedule jobs, bind ECS data, capture artifacts, or describe executable operation data.
    /// </para>
    /// <para>
    /// Public enumeration order is deterministic. Execution profiles are exposed in ordinal profile-symbol order.
    /// Private dictionaries are lookup indexes only and never define public order.
    /// </para>
    /// <para>
    /// Each accepted execution profile must have a unique profile symbol.
    /// </para>
    /// <para>
    /// A non-null <see cref="ExecutionProfileSet"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class ExecutionProfileSet
    {
        private readonly Dictionary<Symbol, ExecutionProfile> _executionProfilesBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileSet"/> class.
        /// </summary>
        /// <param name="executionProfiles">The execution profiles owned by the set.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="executionProfiles"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="executionProfiles"/> contains null entries or duplicate profile symbols.
        /// </exception>
        public ExecutionProfileSet(IEnumerable<ExecutionProfile> executionProfiles)
        {
            if (executionProfiles is null)
            {
                throw new ArgumentNullException(nameof(executionProfiles));
            }

            ExecutionProfile[] copiedExecutionProfiles = CopyExecutionProfiles(executionProfiles);
            Array.Sort(copiedExecutionProfiles, static (left, right) => left.Symbol.CompareTo(right.Symbol));

            _executionProfilesBySymbol = CreateIndex(copiedExecutionProfiles);
            ExecutionProfiles = new ReadOnlyCollection<ExecutionProfile>(copiedExecutionProfiles);
        }

        /// <summary>
        /// Gets the execution profiles owned by the set in ordinal profile-symbol order.
        /// </summary>
        public IReadOnlyList<ExecutionProfile> ExecutionProfiles { get; }

        /// <summary>
        /// Determines whether the set contains an execution profile with the specified profile symbol.
        /// </summary>
        /// <param name="executionProfileSymbol">The profile symbol to find.</param>
        /// <returns><see langword="true"/> when the execution profile exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="executionProfileSymbol"/> is null.
        /// </exception>
        public bool ContainsExecutionProfile(Symbol executionProfileSymbol)
        {
            if (executionProfileSymbol is null)
            {
                throw new ArgumentNullException(nameof(executionProfileSymbol));
            }

            return _executionProfilesBySymbol.ContainsKey(executionProfileSymbol);
        }

        /// <summary>
        /// Gets the execution profile with the specified profile symbol.
        /// </summary>
        /// <param name="executionProfileSymbol">The profile symbol to find.</param>
        /// <returns>The matching execution profile.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="executionProfileSymbol"/> is null.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no execution profile has the specified symbol.
        /// </exception>
        public ExecutionProfile GetExecutionProfile(Symbol executionProfileSymbol)
        {
            if (executionProfileSymbol is null)
            {
                throw new ArgumentNullException(nameof(executionProfileSymbol));
            }

            if (!_executionProfilesBySymbol.TryGetValue(executionProfileSymbol, out ExecutionProfile executionProfile))
            {
                throw new KeyNotFoundException($"Execution profile '{executionProfileSymbol}' was not found.");
            }

            return executionProfile;
        }

        /// <summary>
        /// Attempts to get the execution profile with the specified profile symbol.
        /// </summary>
        /// <param name="executionProfileSymbol">The profile symbol to find.</param>
        /// <param name="executionProfile">The matching execution profile when found.</param>
        /// <returns><see langword="true"/> when the execution profile exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="executionProfileSymbol"/> is null.
        /// </exception>
        public bool TryGetExecutionProfile(
            Symbol executionProfileSymbol,
            out ExecutionProfile? executionProfile)
        {
            if (executionProfileSymbol is null)
            {
                throw new ArgumentNullException(nameof(executionProfileSymbol));
            }

            return _executionProfilesBySymbol.TryGetValue(executionProfileSymbol, out executionProfile);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(ExecutionProfileSet)}({nameof(ExecutionProfiles)}: {ExecutionProfiles.Count})";
        }

        private static ExecutionProfile[] CopyExecutionProfiles(IEnumerable<ExecutionProfile> executionProfiles)
        {
            var copiedExecutionProfiles = new List<ExecutionProfile>();
            var executionProfileSymbols = new HashSet<Symbol>();

            foreach (ExecutionProfile? executionProfile in executionProfiles)
            {
                if (executionProfile is null)
                {
                    throw new ArgumentException(
                        "Execution profiles cannot contain null entries.",
                        nameof(executionProfiles));
                }

                if (!executionProfileSymbols.Add(executionProfile.Symbol))
                {
                    throw new ArgumentException(
                        $"Execution profiles cannot contain duplicate profile symbol '{executionProfile.Symbol}'.",
                        nameof(executionProfiles));
                }

                copiedExecutionProfiles.Add(executionProfile);
            }

            return copiedExecutionProfiles.ToArray();
        }

        private static Dictionary<Symbol, ExecutionProfile> CreateIndex(
            IEnumerable<ExecutionProfile> executionProfiles)
        {
            var index = new Dictionary<Symbol, ExecutionProfile>();

            foreach (ExecutionProfile executionProfile in executionProfiles)
            {
                index.Add(executionProfile.Symbol, executionProfile);
            }

            return index;
        }
    }
}
