// Packages/com.lokrain.atlas/Runtime/Execution/AtlasExecutionResult.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Represent the managed result of scheduling or completing an Atlas execution pass.
// - Preserve the final JobHandle for asynchronous execution.
// - Report operation counts and first failure location for diagnostics/UI.
// - Keep execution result separate from workspace memory, artifacts, and debug rendering.
//
// Design notes
// - This is managed orchestration metadata.
// - This is not a durable artifact.
// - This does not own native memory.
// - This does not dispose workspace memory.
// - This does not replace compiler diagnostics.
// - This does not inspect job internals after scheduling.
// - A Scheduled result means the job dependency chain was produced successfully.
// - A Completed result means the runner completed the final dependency handle before returning.
// - A Failed result means managed scheduling/dispatch failed before a complete successful chain was returned.

using System;
using Lokrain.Atlas.Operations;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Managed status for an Atlas execution pass.
    /// </summary>
    public enum AtlasExecutionResultStatus : byte
    {
        /// <summary>
        /// No execution result has been produced.
        /// </summary>
        None = 0,

        /// <summary>
        /// Operations were scheduled and the final dependency handle is available.
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// Operations were scheduled and the final dependency handle was completed before returning.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Managed operation dispatch or scheduling failed.
        /// </summary>
        Failed = 3
    }

    /// <summary>
    /// Managed result returned by Atlas operation runners.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasExecutionResult"/> summarizes a single managed execution attempt. It is
    /// intentionally not a replacement for compiler diagnostics, generated-world validation, or
    /// artifact metadata. It answers one narrower question: did the runner successfully dispatch
    /// compiled operation occurrences to registered executors, and what final dependency handle
    /// represents the scheduled work?
    /// </para>
    ///
    /// <para>
    /// A successful scheduled result does not imply that the jobs have completed. The caller must
    /// complete <see cref="FinalDependency"/> or pass it into downstream scheduling. A completed
    /// result means the runner already completed the final dependency before returning.
    /// </para>
    ///
    /// <para>
    /// This type carries managed failure information for editor windows, diagnostics panels, and
    /// orchestration logs. It is not durable artifact state and should not be serialized as world
    /// output.
    /// </para>
    /// </remarks>
    public sealed class AtlasExecutionResult
    {
        private AtlasExecutionResult(
            AtlasExecutionResultStatus status,
            JobHandle finalDependency,
            int scheduledOperationCount,
            int failedFlattenedOperationIndex,
            AtlasOperationId failedOperationId,
            FixedString64Bytes failedOperationName,
            FixedString512Bytes message,
            Exception exception)
        {
            ValidateConstructorArgumentsOrThrow(
                status,
                scheduledOperationCount,
                failedFlattenedOperationIndex,
                exception);

            Status = status;
            FinalDependency = finalDependency;
            ScheduledOperationCount = scheduledOperationCount;
            FailedFlattenedOperationIndex = failedFlattenedOperationIndex;
            FailedOperationId = failedOperationId;
            FailedOperationName = failedOperationName;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Gets the execution result status.
        /// </summary>
        public AtlasExecutionResultStatus Status { get; }

        /// <summary>
        /// Gets the final dependency handle produced by successful scheduling.
        /// </summary>
        public JobHandle FinalDependency { get; }

        /// <summary>
        /// Gets the number of operation occurrences successfully scheduled before this result was produced.
        /// </summary>
        public int ScheduledOperationCount { get; }

        /// <summary>
        /// Gets the flattened operation index that failed, or -1 when there is no failure.
        /// </summary>
        public int FailedFlattenedOperationIndex { get; }

        /// <summary>
        /// Gets the failed operation id, or default when there is no failure.
        /// </summary>
        public AtlasOperationId FailedOperationId { get; }

        /// <summary>
        /// Gets the failed operation diagnostic name, or empty when there is no failure.
        /// </summary>
        public FixedString64Bytes FailedOperationName { get; }

        /// <summary>
        /// Gets the managed result message.
        /// </summary>
        public FixedString512Bytes Message { get; }

        /// <summary>
        /// Gets the managed exception captured during dispatch/scheduling failure, when available.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets whether execution scheduling or completion succeeded.
        /// </summary>
        public bool Succeeded =>
            Status == AtlasExecutionResultStatus.Scheduled ||
            Status == AtlasExecutionResultStatus.Completed;

        /// <summary>
        /// Gets whether execution dispatch or scheduling failed.
        /// </summary>
        public bool Failed => Status == AtlasExecutionResultStatus.Failed;

        /// <summary>
        /// Gets whether the result represents a scheduled but not runner-completed dependency chain.
        /// </summary>
        public bool IsScheduled => Status == AtlasExecutionResultStatus.Scheduled;

        /// <summary>
        /// Gets whether the result represents a runner-completed dependency chain.
        /// </summary>
        public bool IsCompleted => Status == AtlasExecutionResultStatus.Completed;

        /// <summary>
        /// Gets whether the result has a captured managed exception.
        /// </summary>
        public bool HasException => Exception != null;

        /// <summary>
        /// Creates a successful scheduled result.
        /// </summary>
        /// <param name="finalDependency">Final dependency handle for the scheduled operation chain.</param>
        /// <param name="scheduledOperationCount">Number of operation occurrences successfully scheduled.</param>
        /// <returns>A scheduled execution result.</returns>
        public static AtlasExecutionResult Scheduled(
            JobHandle finalDependency,
            int scheduledOperationCount)
        {
            return new AtlasExecutionResult(
                AtlasExecutionResultStatus.Scheduled,
                finalDependency,
                scheduledOperationCount,
                failedFlattenedOperationIndex: -1,
                failedOperationId: default,
                failedOperationName: default,
                message: CreateMessage(
                    $"Atlas execution scheduled '{scheduledOperationCount}' operation(s)."),
                exception: null);
        }

        /// <summary>
        /// Creates a successful completed result.
        /// </summary>
        /// <param name="scheduledOperationCount">Number of operation occurrences successfully scheduled and completed.</param>
        /// <returns>A completed execution result.</returns>
        public static AtlasExecutionResult Completed(
            int scheduledOperationCount)
        {
            return new AtlasExecutionResult(
                AtlasExecutionResultStatus.Completed,
                default,
                scheduledOperationCount,
                failedFlattenedOperationIndex: -1,
                failedOperationId: default,
                failedOperationName: default,
                message: CreateMessage(
                    $"Atlas execution completed '{scheduledOperationCount}' operation(s)."),
                exception: null);
        }

        /// <summary>
        /// Creates a failed result for managed dispatch or scheduling failure.
        /// </summary>
        /// <param name="scheduledOperationCount">Number of operation occurrences successfully scheduled before failure.</param>
        /// <param name="failedFlattenedOperationIndex">Flattened operation index that failed.</param>
        /// <param name="failedOperationId">Failed operation id.</param>
        /// <param name="failedOperationName">Failed operation diagnostic name.</param>
        /// <param name="message">Failure message.</param>
        /// <param name="exception">Captured managed exception, when available.</param>
        /// <returns>A failed execution result.</returns>
        public static AtlasExecutionResult FailedResult(
            int scheduledOperationCount,
            int failedFlattenedOperationIndex,
            AtlasOperationId failedOperationId,
            FixedString64Bytes failedOperationName,
            string message,
            Exception exception = null)
        {
            if (failedFlattenedOperationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failedFlattenedOperationIndex),
                    failedFlattenedOperationIndex,
                    "Failed operation index must be non-negative.");
            }

            return new AtlasExecutionResult(
                AtlasExecutionResultStatus.Failed,
                default,
                scheduledOperationCount,
                failedFlattenedOperationIndex,
                failedOperationId,
                failedOperationName,
                CreateMessage(message),
                exception);
        }

        /// <summary>
        /// Creates a failed result for setup-level failure where no specific operation is responsible.
        /// </summary>
        /// <param name="message">Failure message.</param>
        /// <param name="exception">Captured managed exception, when available.</param>
        /// <returns>A failed execution result.</returns>
        public static AtlasExecutionResult FailedSetup(
            string message,
            Exception exception = null)
        {
            return new AtlasExecutionResult(
                AtlasExecutionResultStatus.Failed,
                default,
                scheduledOperationCount: 0,
                failedFlattenedOperationIndex: -1,
                failedOperationId: default,
                failedOperationName: default,
                CreateMessage(message),
                exception);
        }

        /// <summary>
        /// Throws when this result represents failure.
        /// </summary>
        public void ThrowIfFailed()
        {
            if (!Failed)
            {
                return;
            }

            var message = Message.IsEmpty
                ? "Atlas execution failed."
                : Message.ToString();

            throw new InvalidOperationException(
                message,
                Exception);
        }

        /// <summary>
        /// Completes the final dependency handle when this result is scheduled.
        /// </summary>
        /// <returns>A completed result.</returns>
        /// <remarks>
        /// This method does not mutate the current result. It completes the stored dependency
        /// handle and returns a new completed result. Calling it on a failed result rethrows the
        /// failure through <see cref="ThrowIfFailed"/>.
        /// </remarks>
        public AtlasExecutionResult Complete()
        {
            ThrowIfFailed();

            if (IsCompleted)
            {
                return this;
            }

            FinalDependency.Complete();

            return Completed(ScheduledOperationCount);
        }

        /// <summary>
        /// Returns a diagnostic representation of this result.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public override string ToString()
        {
            if (Failed)
            {
                return
                    $"AtlasExecutionResult(Status={Status}, Scheduled={ScheduledOperationCount}, FailedIndex={FailedFlattenedOperationIndex}, FailedOperation={FailedOperationName}, Message={Message})";
            }

            return
                $"AtlasExecutionResult(Status={Status}, Scheduled={ScheduledOperationCount}, Message={Message})";
        }

        private static FixedString512Bytes CreateMessage(
            string message)
        {
            var fixedMessage = new FixedString512Bytes();

            if (!string.IsNullOrEmpty(message))
            {
                fixedMessage.Append(message);
            }

            return fixedMessage;
        }

        private static void ValidateConstructorArgumentsOrThrow(
            AtlasExecutionResultStatus status,
            int scheduledOperationCount,
            int failedFlattenedOperationIndex,
            Exception exception)
        {
            if (status == AtlasExecutionResultStatus.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Execution result status must be Scheduled, Completed, or Failed.");
            }

            if (scheduledOperationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scheduledOperationCount),
                    scheduledOperationCount,
                    "Scheduled operation count must be non-negative.");
            }

            if (status == AtlasExecutionResultStatus.Failed)
            {
                if (failedFlattenedOperationIndex < -1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(failedFlattenedOperationIndex),
                        failedFlattenedOperationIndex,
                        "Failed operation index must be -1 for setup failures or non-negative for operation failures.");
                }

                return;
            }

            if (failedFlattenedOperationIndex != -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failedFlattenedOperationIndex),
                    failedFlattenedOperationIndex,
                    "Successful execution results must not contain a failed operation index.");
            }

            if (exception != null)
            {
                throw new ArgumentException(
                    "Successful execution results must not contain an exception.",
                    nameof(exception));
            }
        }
    }
}