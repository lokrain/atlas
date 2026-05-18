#nullable enable

using System;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents one deterministic runnable-plan compilation failure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A runnable-plan compilation error contains a stable machine-readable <see cref="Code"/> and
    /// human-readable diagnostic <see cref="Message"/>. Tooling and tests must use <see cref="Code"/> and
    /// structured context, not message text, as the API contract.
    /// </para>
    /// <para>
    /// Context properties are optional because not every compilation failure has a field, stage, operation, or
    /// subject symbol. They identify where a deterministic managed metadata compilation failure was detected.
    /// </para>
    /// <para>
    /// This type describes managed runnable-plan compilation failures only. It does not describe runtime
    /// execution failures, native allocation failures, scheduler failures, job failures, ECS failures, artifact
    /// capture failures, or runtime diagnostic capture failures.
    /// </para>
    /// </remarks>
    public sealed class RunnablePlanCompilationError : IEquatable<RunnablePlanCompilationError>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunnablePlanCompilationError"/> class.
        /// </summary>
        /// <param name="code">The stable machine-readable error code.</param>
        /// <param name="message">The human-readable diagnostic message.</param>
        /// <param name="subjectSymbol">The relevant subject symbol, when available.</param>
        /// <param name="fieldIndex">The relevant field index, when available.</param>
        /// <param name="stageIndex">The relevant stage index, when available.</param>
        /// <param name="operationIndex">The relevant operation index, when available.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="code"/> is unspecified or unsupported, or when
        /// <paramref name="message"/> is empty or whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        public RunnablePlanCompilationError(
            RunnablePlanCompilationErrorCode code,
            string message,
            Symbol? subjectSymbol = null,
            FieldIndex? fieldIndex = null,
            StageIndex? stageIndex = null,
            OperationIndex? operationIndex = null)
        {
            ValidateCode(code);

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "Runnable-plan compilation error message cannot be empty or whitespace.",
                    nameof(message));
            }

            Code = code;
            Message = message;
            SubjectSymbol = subjectSymbol;
            FieldIndex = fieldIndex;
            StageIndex = stageIndex;
            OperationIndex = operationIndex;
        }

        /// <summary>
        /// Gets the stable machine-readable error code.
        /// </summary>
        public RunnablePlanCompilationErrorCode Code { get; }

        /// <summary>
        /// Gets the human-readable diagnostic message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the relevant subject symbol, when available.
        /// </summary>
        public Symbol? SubjectSymbol { get; }

        /// <summary>
        /// Gets the relevant field index, when available.
        /// </summary>
        public FieldIndex? FieldIndex { get; }

        /// <summary>
        /// Gets the relevant stage index, when available.
        /// </summary>
        public StageIndex? StageIndex { get; }

        /// <summary>
        /// Gets the relevant operation index, when available.
        /// </summary>
        public OperationIndex? OperationIndex { get; }

        /// <inheritdoc/>
        public bool Equals(RunnablePlanCompilationError? other)
        {
            return other is not null
                && Code == other.Code
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SubjectSymbol == other.SubjectSymbol
                && FieldIndex == other.FieldIndex
                && StageIndex == other.StageIndex
                && OperationIndex == other.OperationIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is RunnablePlanCompilationError other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Code,
                Message,
                SubjectSymbol,
                FieldIndex,
                StageIndex,
                OperationIndex);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string text = $"{nameof(RunnablePlanCompilationError)}({nameof(Code)}: {Code}, {nameof(Message)}: {Message}";

            if (SubjectSymbol is not null)
            {
                text += $", {nameof(SubjectSymbol)}: {SubjectSymbol}";
            }

            if (FieldIndex.HasValue)
            {
                text += $", {nameof(FieldIndex)}: {FieldIndex.Value}";
            }

            if (StageIndex.HasValue)
            {
                text += $", {nameof(StageIndex)}: {StageIndex.Value}";
            }

            if (OperationIndex.HasValue)
            {
                text += $", {nameof(OperationIndex)}: {OperationIndex.Value}";
            }

            return text + ")";
        }

        /// <summary>
        /// Determines whether two runnable-plan compilation errors are equal.
        /// </summary>
        public static bool operator ==(RunnablePlanCompilationError? left, RunnablePlanCompilationError? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two runnable-plan compilation errors are not equal.
        /// </summary>
        public static bool operator !=(RunnablePlanCompilationError? left, RunnablePlanCompilationError? right)
        {
            return !Equals(left, right);
        }

        private static void ValidateCode(RunnablePlanCompilationErrorCode code)
        {
            if (code != RunnablePlanCompilationErrorCode.MissingFieldDefinition
                && code != RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch)
            {
                throw new ArgumentException(
                    "Runnable-plan compilation error must specify a supported error code.",
                    nameof(code));
            }
        }
    }
}
