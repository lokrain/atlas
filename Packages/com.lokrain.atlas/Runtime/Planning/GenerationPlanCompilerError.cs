#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Represents a structured generation-plan compiler error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation plan compiler error describes a catalog-dependent semantic failure encountered while
    /// resolving a generation request. It is returned as compiler output and is not an execution exception,
    /// runtime binding, job error, native container, or Unity object.
    /// </para>
    /// <para>
    /// The error code is a stable machine-facing symbol intended for tests, tooling, logs, and editor UI.
    /// The message is human-facing diagnostic text. The optional subject symbol identifies the primary request
    /// or catalog symbol associated with the error when one exists.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationPlanCompilerError"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationPlanCompilerError : IEquatable<GenerationPlanCompilerError>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationPlanCompilerError"/> class.
        /// </summary>
        /// <param name="code">The stable machine-facing error code.</param>
        /// <param name="message">The human-facing diagnostic message.</param>
        /// <param name="subjectSymbol">The primary related symbol, or null when no single symbol applies.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="code"/> or <paramref name="message"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="message"/> is empty or whitespace-only.
        /// </exception>
        public GenerationPlanCompilerError(
            Symbol code,
            string message,
            Symbol? subjectSymbol = null)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string normalizedMessage = message.Trim();

            if (normalizedMessage.Length == 0)
            {
                throw new ArgumentException(
                    "Generation plan compiler error message cannot be empty or whitespace-only.",
                    nameof(message));
            }

            Code = code;
            Message = normalizedMessage;
            SubjectSymbol = subjectSymbol;
        }

        /// <summary>
        /// Gets the stable machine-facing error code.
        /// </summary>
        public Symbol Code { get; }

        /// <summary>
        /// Gets the human-facing diagnostic message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the primary related symbol, or null when no single symbol applies.
        /// </summary>
        public Symbol? SubjectSymbol { get; }

        /// <inheritdoc/>
        public bool Equals(GenerationPlanCompilerError? other)
        {
            return other is not null
                && Code == other.Code
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SubjectSymbol == other.SubjectSymbol;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationPlanCompilerError other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Code,
                StringComparer.Ordinal.GetHashCode(Message),
                SubjectSymbol);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return SubjectSymbol is null
                ? $"{nameof(GenerationPlanCompilerError)}({nameof(Code)}: {Code}, {nameof(Message)}: {Message})"
                : $"{nameof(GenerationPlanCompilerError)}({nameof(Code)}: {Code}, {nameof(SubjectSymbol)}: {SubjectSymbol}, {nameof(Message)}: {Message})";
        }

        /// <summary>
        /// Determines whether two generation plan compiler errors are equal.
        /// </summary>
        public static bool operator ==(
            GenerationPlanCompilerError? left,
            GenerationPlanCompilerError? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation plan compiler errors are not equal.
        /// </summary>
        public static bool operator !=(
            GenerationPlanCompilerError? left,
            GenerationPlanCompilerError? right)
        {
            return !Equals(left, right);
        }
    }
}