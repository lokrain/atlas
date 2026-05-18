#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents the result of resolving a symbolic generation request descriptor into an accepted generation
    /// request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation request resolution result contains either an accepted generation request or one or more
    /// structured resolution errors. Resolution errors describe why a symbolic descriptor could not be satisfied
    /// by a generation catalog.
    /// </para>
    /// <para>
    /// A successful result contains exactly one accepted generation request and no errors. A failed result contains
    /// no generation request and at least one structured error.
    /// </para>
    /// <para>
    /// This type is managed planning data only. It does not contain executable metadata, scheduler bindings,
    /// runtime state, job data, native containers, ECS systems, Burst function pointers, or Unity runtime objects.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationRequestResolutionResult"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationRequestResolutionResult : IEquatable<GenerationRequestResolutionResult>
    {
        private GenerationRequestResolutionResult(
            GenerationRequest? generationRequest,
            IEnumerable<GenerationRequestResolutionError> errors)
        {
            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            GenerationRequestResolutionError[] copiedErrors = CopyErrors(errors);

            if (generationRequest is null && copiedErrors.Length == 0)
            {
                throw new ArgumentException(
                    "A failed generation request resolution result must contain at least one error.",
                    nameof(errors));
            }

            if (generationRequest is not null && copiedErrors.Length != 0)
            {
                throw new ArgumentException(
                    "A successful generation request resolution result cannot contain errors.",
                    nameof(errors));
            }

            GenerationRequest = generationRequest;
            Errors = new ReadOnlyCollection<GenerationRequestResolutionError>(copiedErrors);
        }

        /// <summary>
        /// Gets a value indicating whether resolution succeeded.
        /// </summary>
        public bool Succeeded => GenerationRequest is not null;

        /// <summary>
        /// Gets a value indicating whether resolution failed.
        /// </summary>
        public bool Failed => GenerationRequest is null;

        /// <summary>
        /// Gets the accepted generation request when resolution succeeded; otherwise, null.
        /// </summary>
        public GenerationRequest? GenerationRequest { get; }

        /// <summary>
        /// Gets the structured resolution errors when resolution failed.
        /// </summary>
        public IReadOnlyList<GenerationRequestResolutionError> Errors { get; }

        internal static GenerationRequestResolutionResult Success(GenerationRequest generationRequest)
        {
            if (generationRequest is null)
            {
                throw new ArgumentNullException(nameof(generationRequest));
            }

            return new(generationRequest, Array.Empty<GenerationRequestResolutionError>());
        }

        internal static GenerationRequestResolutionResult Failure(
            IEnumerable<GenerationRequestResolutionError> errors)
        {
            return new(null, errors);
        }

        /// <inheritdoc/>
        public bool Equals(GenerationRequestResolutionResult? other)
        {
            return other is not null
                && GenerationRequest == other.GenerationRequest
                && SequenceEquals(Errors, other.Errors);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationRequestResolutionResult other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationRequest);

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
                ? $"{nameof(GenerationRequestResolutionResult)}({nameof(Succeeded)}: true, {nameof(GenerationRequest)}: {GenerationRequest})"
                : $"{nameof(GenerationRequestResolutionResult)}({nameof(Succeeded)}: false, {nameof(Errors)}: {Errors.Count})";
        }

        /// <summary>
        /// Determines whether two generation request resolution results are equal.
        /// </summary>
        public static bool operator ==(
            GenerationRequestResolutionResult? left,
            GenerationRequestResolutionResult? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation request resolution results are not equal.
        /// </summary>
        public static bool operator !=(
            GenerationRequestResolutionResult? left,
            GenerationRequestResolutionResult? right)
        {
            return !Equals(left, right);
        }

        private static GenerationRequestResolutionError[] CopyErrors(
            IEnumerable<GenerationRequestResolutionError> errors)
        {
            var copiedErrors = new List<GenerationRequestResolutionError>();

            foreach (GenerationRequestResolutionError? error in errors)
            {
                if (error is null)
                {
                    throw new ArgumentException(
                        "Generation request resolution errors cannot contain null entries.",
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