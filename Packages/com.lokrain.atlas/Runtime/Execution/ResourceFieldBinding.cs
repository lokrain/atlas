#nullable enable

using System;
using System.Runtime.CompilerServices;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Resources;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Represents one immutable, plan-local binding row that connects a semantic resource definition
    /// to managed field metadata for runnable-plan compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="ResourceFieldBinding"/> is managed runnable-plan metadata. It does not represent storage
    /// allocation, runtime ownership, scheduler behavior, execution state, native handle ownership, job scheduling,
    /// ECS binding, artifact capture execution, or runtime diagnostic capture.
    /// </para>
    /// <para>
    /// Ownership is reference-exact. The <see cref="FieldDefinition"/> supplied to this binding must reference the
    /// exact <see cref="ResourceDefinition"/> instance supplied to this binding. Symbol-equivalent instances from
    /// outside the accepted metadata graph are rejected.
    /// </para>
    /// </remarks>
    public sealed class ResourceFieldBinding : IEquatable<ResourceFieldBinding>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFieldBinding"/> class.
        /// </summary>
        /// <param name="fieldIndex">The dense, plan-local field table index for this binding row.</param>
        /// <param name="resourceDefinition">The semantic resource represented by this binding row.</param>
        /// <param name="fieldDefinition">The managed field metadata mapped to <paramref name="resourceDefinition"/>.</param>
        /// <param name="planRole">The runnable-plan input/output role of the bound field.</param>
        /// <param name="capturePolicy">The capture-intent metadata policy for future infrastructure.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinition"/> or <paramref name="fieldDefinition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an enum parameter is unspecified or unsupported, or when
        /// <paramref name="fieldDefinition"/> does not reference the exact supplied
        /// <paramref name="resourceDefinition"/> instance.
        /// </exception>
        public ResourceFieldBinding(
            FieldIndex fieldIndex,
            ResourceDefinition resourceDefinition,
            FieldDefinition fieldDefinition,
            FieldPlanRole planRole,
            FieldCapturePolicy capturePolicy)
        {
            if (resourceDefinition is null)
            {
                throw new ArgumentNullException(nameof(resourceDefinition));
            }

            if (fieldDefinition is null)
            {
                throw new ArgumentNullException(nameof(fieldDefinition));
            }

            ValidatePlanRole(planRole);
            ValidateCapturePolicy(capturePolicy);
            ValidateReferenceExactResourceOwnership(fieldDefinition, resourceDefinition);

            FieldIndex = fieldIndex;
            ResourceDefinition = resourceDefinition;
            FieldDefinition = fieldDefinition;
            PlanRole = planRole;
            CapturePolicy = capturePolicy;
        }

        /// <summary>
        /// Gets the dense, plan-local field table index for this binding row.
        /// </summary>
        public FieldIndex FieldIndex { get; }

        /// <summary>
        /// Gets the semantic resource represented by this binding row.
        /// </summary>
        public ResourceDefinition ResourceDefinition { get; }

        /// <summary>
        /// Gets the managed field metadata mapped to <see cref="ResourceDefinition"/>.
        /// </summary>
        public FieldDefinition FieldDefinition { get; }

        /// <summary>
        /// Gets the runnable-plan input/output role for this binding.
        /// </summary>
        public FieldPlanRole PlanRole { get; }

        /// <summary>
        /// Gets the capture-intent policy for this binding.
        /// </summary>
        public FieldCapturePolicy CapturePolicy { get; }

        /// <inheritdoc/>
        public bool Equals(ResourceFieldBinding? other)
        {
            return other is not null
                && FieldIndex == other.FieldIndex
                && ReferenceEquals(ResourceDefinition, other.ResourceDefinition)
                && ReferenceEquals(FieldDefinition, other.FieldDefinition)
                && PlanRole == other.PlanRole
                && CapturePolicy == other.CapturePolicy;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ResourceFieldBinding other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(FieldIndex);
            hashCode.Add(RuntimeHelpers.GetHashCode(ResourceDefinition));
            hashCode.Add(RuntimeHelpers.GetHashCode(FieldDefinition));
            hashCode.Add(PlanRole);
            hashCode.Add(CapturePolicy);
            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(ResourceFieldBinding)}(" +
                   $"{nameof(FieldIndex)}: {FieldIndex}, " +
                   $"{nameof(ResourceDefinition)}: {ResourceDefinition.Symbol}, " +
                   $"{nameof(FieldDefinition)}: {FieldDefinition.Symbol}, " +
                   $"{nameof(PlanRole)}: {PlanRole}, " +
                   $"{nameof(CapturePolicy)}: {CapturePolicy})";
        }

        /// <summary>
        /// Determines whether two bindings are equal.
        /// </summary>
        public static bool operator ==(ResourceFieldBinding? left, ResourceFieldBinding? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two bindings are not equal.
        /// </summary>
        public static bool operator !=(ResourceFieldBinding? left, ResourceFieldBinding? right)
        {
            return !Equals(left, right);
        }

        private static void ValidatePlanRole(FieldPlanRole planRole)
        {
            if (planRole != FieldPlanRole.RequiredInput
                && planRole != FieldPlanRole.ProducedOutput
                && planRole != FieldPlanRole.RequiredInputAndProducedOutput)
            {
                throw new ArgumentException(
                    "Resource field binding must specify a supported field plan role.",
                    nameof(planRole));
            }
        }

        private static void ValidateCapturePolicy(FieldCapturePolicy capturePolicy)
        {
            if (capturePolicy != FieldCapturePolicy.DoNotCapture
                && capturePolicy != FieldCapturePolicy.Capture)
            {
                throw new ArgumentException(
                    "Resource field binding must specify a supported field capture policy.",
                    nameof(capturePolicy));
            }
        }

        private static void ValidateReferenceExactResourceOwnership(
            FieldDefinition fieldDefinition,
            ResourceDefinition resourceDefinition)
        {
            if (!ReferenceEquals(fieldDefinition.ResourceDefinition, resourceDefinition))
            {
                throw new ArgumentException(
                    $"Field definition '{fieldDefinition.Symbol}' references resource definition '{fieldDefinition.ResourceDefinition.Symbol}', " +
                    $"but binding requires the exact resource definition instance '{resourceDefinition.Symbol}'.",
                    nameof(fieldDefinition));
            }
        }
    }
}