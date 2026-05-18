#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents the deterministic result of managed runnable-plan compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A successful result contains a non-null <see cref="RunnablePlan"/> and no errors. A failed result contains
    /// no runnable plan and at least one deterministic structured error. Failed compilation never returns a partial
    /// runnable plan.
    /// </para>
    /// <para>
    /// This type represents managed compilation outcome only. It does not execute operations, allocate native
    /// memory, schedule jobs, bind ECS data, capture artifacts, or capture runtime diagnostics.
    /// </para>
    /// </remarks>
    public sealed class RunnablePlanCompilationResult
    {
        private RunnablePlanCompilationResult(
            RunnablePlan? runnablePlan,
            IReadOnlyList<RunnablePlanCompilationError> errors)
        {
            RunnablePlan = runnablePlan;
            Errors = errors;
            Succeeded = runnablePlan is not null;
        }

        /// <summary>
        /// Gets a value indicating whether compilation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the compiled runnable plan when compilation succeeded.
        /// </summary>
        public RunnablePlan? RunnablePlan { get; }

        /// <summary>
        /// Gets the deterministic structured compilation errors when compilation failed.
        /// </summary>
        public IReadOnlyList<RunnablePlanCompilationError> Errors { get; }

        /// <summary>
        /// Creates a successful runnable-plan compilation result.
        /// </summary>
        /// <param name="runnablePlan">The compiled runnable plan.</param>
        /// <returns>A successful compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="runnablePlan"/> is <see langword="null"/>.
        /// </exception>
        public static RunnablePlanCompilationResult Success(RunnablePlan runnablePlan)
        {
            if (runnablePlan is null)
            {
                throw new ArgumentNullException(nameof(runnablePlan));
            }

            return new RunnablePlanCompilationResult(
                runnablePlan,
                Array.Empty<RunnablePlanCompilationError>());
        }

        /// <summary>
        /// Creates a failed runnable-plan compilation result.
        /// </summary>
        /// <param name="errors">The deterministic structured compilation errors.</param>
        /// <returns>A failed compilation result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="errors"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="errors"/> is empty or contains null entries.
        /// </exception>
        public static RunnablePlanCompilationResult Failure(
            IEnumerable<RunnablePlanCompilationError> errors)
        {
            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            RunnablePlanCompilationError[] copiedErrors = CopyErrors(errors);

            if (copiedErrors.Length == 0)
            {
                throw new ArgumentException(
                    "Failed runnable-plan compilation result must contain at least one error.",
                    nameof(errors));
            }

            return new RunnablePlanCompilationResult(
                runnablePlan: null,
                new ReadOnlyCollection<RunnablePlanCompilationError>(copiedErrors));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(RunnablePlanCompilationResult)}({nameof(Succeeded)}: {Succeeded}, {nameof(Errors)}: {Errors.Count})";
        }

        private static RunnablePlanCompilationError[] CopyErrors(
            IEnumerable<RunnablePlanCompilationError> errors)
        {
            var copiedErrors = new List<RunnablePlanCompilationError>();

            foreach (RunnablePlanCompilationError? error in errors)
            {
                if (error is null)
                {
                    throw new ArgumentException(
                        "Runnable-plan compilation errors cannot contain null entries.",
                        nameof(errors));
                }

                copiedErrors.Add(error);
            }

            return copiedErrors.ToArray();
        }
    }
}
