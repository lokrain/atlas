#nullable enable

using System;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class RunnablePlanCompilationResultTests
    {
        [Test]
        public void Success_WithNullRunnablePlan_ThrowsArgumentNullException()
        {
            Assert.That(
                () => RunnablePlanCompilationResult.Success(null!),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("runnablePlan"));
        }

        [Test]
        public void Success_WithRunnablePlan_ReturnsSuccessfulResult()
        {
            RunnablePlan runnablePlan = CreateUninitializedRunnablePlan();

            RunnablePlanCompilationResult result = RunnablePlanCompilationResult.Success(runnablePlan);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RunnablePlan, Is.SameAs(runnablePlan));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Failure_WithNullErrors_ThrowsArgumentNullException()
        {
            Assert.That(
                () => RunnablePlanCompilationResult.Failure(null!),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("errors"));
        }

        [Test]
        public void Failure_WithEmptyErrors_ThrowsArgumentException()
        {
            Assert.That(
                () => RunnablePlanCompilationResult.Failure(
                    Array.Empty<RunnablePlanCompilationError>()),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("errors"));
        }

        [Test]
        public void Failure_WithNullErrorEntry_ThrowsArgumentException()
        {
            Assert.That(
                () => RunnablePlanCompilationResult.Failure(
                    new RunnablePlanCompilationError?[]
                    {
                        CreateError(),
                        null
                    }!),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("errors"));
        }

        [Test]
        public void Failure_WithErrors_ReturnsFailedResult()
        {
            RunnablePlanCompilationError error = CreateError();

            RunnablePlanCompilationResult result = RunnablePlanCompilationResult.Failure(
                new[]
                {
                    error
                });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RunnablePlan, Is.Null);
            Assert.That(result.Errors, Is.EqualTo(new[] { error }));
        }

        [Test]
        public void Failure_CopiesErrors()
        {
            RunnablePlanCompilationError error = CreateError();
            var errors = new[] { error };

            RunnablePlanCompilationResult result = RunnablePlanCompilationResult.Failure(errors);
            errors[0] = new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.ResourceFieldOwnershipMismatch,
                "Different error.");

            Assert.That(result.Errors, Is.EqualTo(new[] { error }));
        }

        [Test]
        public void ToString_ReturnsExpectedSummary()
        {
            RunnablePlanCompilationResult result = RunnablePlanCompilationResult.Failure(
                new[]
                {
                    CreateError()
                });

            Assert.That(
                result.ToString(),
                Is.EqualTo("RunnablePlanCompilationResult(Succeeded: False, Errors: 1)"));
        }

        private static RunnablePlanCompilationError CreateError()
        {
            return new RunnablePlanCompilationError(
                RunnablePlanCompilationErrorCode.MissingFieldDefinition,
                "Missing field definition.");
        }

        private static RunnablePlan CreateUninitializedRunnablePlan()
        {
            return (RunnablePlan)FormatterServices.GetUninitializedObject(typeof(RunnablePlan));
        }
    }
}
