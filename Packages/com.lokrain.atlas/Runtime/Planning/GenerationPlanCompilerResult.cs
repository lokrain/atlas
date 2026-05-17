#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents the result of compiling a generation request into a resolved managed generation plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation plan compiler result contains either a successful resolved generation plan or one or more
    /// structured compiler errors. Compiler errors represent catalog-dependent semantic failures and should be
    /// returned as result data instead of being thrown as normal validation flow.
    /// </para>
    /// <para>
    /// The result is managed planning data only. It does not contain executable bindings, runtime state, job data,
    /// native containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationPlanCompilerResult"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationPlanCompilerResult : IEquatable<GenerationPlanCompilerResult>
    {
        private GenerationPlanCompilerResult(
            GenerationPlan? generationPlan,
            IEnumerable<GenerationPlanCompilerError> errors)
        {
            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            GenerationPlanCompilerError[] copiedErrors = CopyErrors(errors);

            if (generationPlan is null && copiedErrors.Length == 0)
            {
                throw new ArgumentException(
                    "A failed generation plan compiler result must contain at least one error.",
                    nameof(errors));
            }

            if (generationPlan is not null && copiedErrors.Length != 0)
            {
                throw new ArgumentException(
                    "A successful generation plan compiler result cannot contain errors.",
                    nameof(errors));
            }

            GenerationPlan = generationPlan;
            Errors = new ReadOnlyCollection<GenerationPlanCompilerError>(copiedErrors);
        }

        /// <summary>
        /// Gets a value indicating whether compilation succeeded.
        /// </summary>
        public bool Succeeded => GenerationPlan is not null;

        /// <summary>
        /// Gets a value indicating whether compilation failed.
        /// </summary>
        public bool Failed => GenerationPlan is null;

        /// <summary>
        /// Gets the resolved generation plan when compilation succeeded; otherwise, null.
        /// </summary>
        public GenerationPlan? GenerationPlan { get; }

        /// <summary>
        /// Gets the structured compiler errors when compilation failed.
        /// </summary>
        public IReadOnlyList<GenerationPlanCompilerError> Errors { get; }

        internal static GenerationPlanCompilerResult Success(GenerationPlan generationPlan)
        {
            if (generationPlan is null)
            {
                throw new ArgumentNullException(nameof(generationPlan));
            }

            return new(generationPlan, Array.Empty<GenerationPlanCompilerError>());
        }

        internal static GenerationPlanCompilerResult Failure(
            IEnumerable<GenerationPlanCompilerError> errors)
        {
            return new(null, errors);
        }

        /// <inheritdoc/>
        public bool Equals(GenerationPlanCompilerResult? other)
        {
            return other is not null
                && GenerationPlan == other.GenerationPlan
                && SequenceEquals(Errors, other.Errors);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationPlanCompilerResult other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationPlan);

            for (int index = 0; index < Errors.Count; index++)
            {
                hashCode.Add(Errors[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Succeeded
                ? $"{nameof(GenerationPlanCompilerResult)}({nameof(Succeeded)}: true, {nameof(GenerationPlan)}: {GenerationPlan})"
                : $"{nameof(GenerationPlanCompilerResult)}({nameof(Succeeded)}: false, {nameof(Errors)}: {Errors.Count})";
        }

        /// <summary>
        /// Determines whether two generation plan compiler results are equal.
        /// </summary>
        public static bool operator ==(
            GenerationPlanCompilerResult? left,
            GenerationPlanCompilerResult? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation plan compiler results are not equal.
        /// </summary>
        public static bool operator !=(
            GenerationPlanCompilerResult? left,
            GenerationPlanCompilerResult? right)
        {
            return !Equals(left, right);
        }

        private static GenerationPlanCompilerError[] CopyErrors(
            IEnumerable<GenerationPlanCompilerError> errors)
        {
            var copiedErrors = new List<GenerationPlanCompilerError>();

            foreach (GenerationPlanCompilerError? error in errors)
            {
                if (error is null)
                {
                    throw new ArgumentException(
                        "Generation plan compiler errors cannot contain null entries.",
                        nameof(errors));
                }

                copiedErrors.Add(error);
            }

            return copiedErrors.ToArray();
        }

        private static bool SequenceEquals<TValue>(
            IReadOnlyList<TValue> left,
            IReadOnlyList<TValue> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var comparer = EqualityComparer<TValue>.Default;

            for (int index = 0; index < left.Count; index++)
            {
                if (!comparer.Equals(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}