#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Resources;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines the semantic input and output resource contract for a catalog-owned generation operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation contract describes what an operation requires and produces at the catalog/planning boundary.
    /// It is compatibility metadata only; it does not define field storage, artifact storage, implementation
    /// binding, execution behavior, runtime state, job data, or native containers.
    /// </para>
    /// <para>
    /// Required inputs and produced outputs are semantic resource definitions. They answer what values are
    /// required or produced by the operation, not how those values are stored or executed.
    /// </para>
    /// <para>
    /// Required inputs and produced outputs may overlap. An overlap represents read-modify-write behavior for
    /// a semantic resource.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="OperationDefinition"/> because an operation has one contract identity.
    /// Contract content conflicts must be handled by catalog validation, not by treating duplicate operation
    /// contracts as distinct identities.
    /// </para>
    /// <para>
    /// A non-null <see cref="OperationContract"/> instance is always syntactically valid.
    /// Catalog-dependent semantic validity is established by the generation catalog.
    /// </para>
    /// </remarks>
    public sealed class OperationContract : IEquatable<OperationContract>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationContract"/> class.
        /// </summary>
        /// <param name="operationDefinition">The operation definition described by this contract.</param>
        /// <param name="requiredInputs">The semantic resources required before the operation can execute.</param>
        /// <param name="producedOutputs">The semantic resources produced by the operation.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operationDefinition"/>, <paramref name="requiredInputs"/>,
        /// or <paramref name="producedOutputs"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when either resource collection contains null entries or duplicate resources, when a resource
        /// belongs to a different generation schema than the operation, or when both collections are empty.
        /// </exception>
        public OperationContract(
            OperationDefinition operationDefinition,
            IEnumerable<ResourceDefinition> requiredInputs,
            IEnumerable<ResourceDefinition> producedOutputs)
        {
            if (operationDefinition is null)
            {
                throw new ArgumentNullException(nameof(operationDefinition));
            }

            if (requiredInputs is null)
            {
                throw new ArgumentNullException(nameof(requiredInputs));
            }

            if (producedOutputs is null)
            {
                throw new ArgumentNullException(nameof(producedOutputs));
            }

            ResourceDefinition[] copiedRequiredInputs = CopyResourceDefinitions(
                requiredInputs,
                operationDefinition,
                nameof(requiredInputs),
                "Operation contract required inputs");

            ResourceDefinition[] copiedProducedOutputs = CopyResourceDefinitions(
                producedOutputs,
                operationDefinition,
                nameof(producedOutputs),
                "Operation contract produced outputs");

            if (copiedRequiredInputs.Length == 0 && copiedProducedOutputs.Length == 0)
            {
                throw new ArgumentException(
                    "Operation contract must contain at least one required input resource or produced output resource.",
                    nameof(producedOutputs));
            }

            OperationDefinition = operationDefinition;
            RequiredInputs = new ReadOnlyCollection<ResourceDefinition>(copiedRequiredInputs);
            ProducedOutputs = new ReadOnlyCollection<ResourceDefinition>(copiedProducedOutputs);
        }

        /// <summary>
        /// Gets the operation definition described by this contract.
        /// </summary>
        public OperationDefinition OperationDefinition { get; }

        /// <summary>
        /// Gets the semantic resources required before the operation can execute.
        /// </summary>
        public IReadOnlyList<ResourceDefinition> RequiredInputs { get; }

        /// <summary>
        /// Gets the semantic resources produced by the operation.
        /// </summary>
        public IReadOnlyList<ResourceDefinition> ProducedOutputs { get; }

        /// <inheritdoc/>
        public bool Equals(OperationContract? other)
        {
            return other is not null && OperationDefinition == other.OperationDefinition;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is OperationContract other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return OperationDefinition.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(OperationContract)}({nameof(OperationDefinition)}: {OperationDefinition.Symbol}, {nameof(RequiredInputs)}: {RequiredInputs.Count}, {nameof(ProducedOutputs)}: {ProducedOutputs.Count})";
        }

        /// <summary>
        /// Determines whether two operation contracts are equal.
        /// </summary>
        public static bool operator ==(OperationContract? left, OperationContract? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two operation contracts are not equal.
        /// </summary>
        public static bool operator !=(OperationContract? left, OperationContract? right)
        {
            return !Equals(left, right);
        }

        private static ResourceDefinition[] CopyResourceDefinitions(
            IEnumerable<ResourceDefinition> resourceDefinitions,
            OperationDefinition operationDefinition,
            string parameterName,
            string description)
        {
            var copiedResourceDefinitions = new List<ResourceDefinition>();
            var uniqueResourceDefinitions = new HashSet<ResourceDefinition>();

            foreach (ResourceDefinition? resourceDefinition in resourceDefinitions)
            {
                if (resourceDefinition is null)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain null entries.",
                        parameterName);
                }

                if (resourceDefinition.GenerationSchema != operationDefinition.GenerationSchema)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain resource '{resourceDefinition.Symbol}' from generation schema '{resourceDefinition.GenerationSchema.Symbol}' because operation '{operationDefinition.Symbol}' belongs to generation schema '{operationDefinition.GenerationSchema.Symbol}'.",
                        parameterName);
                }

                if (!uniqueResourceDefinitions.Add(resourceDefinition))
                {
                    throw new ArgumentException(
                        $"{description} cannot contain duplicate resource '{resourceDefinition.Symbol}'.",
                        parameterName);
                }

                copiedResourceDefinitions.Add(resourceDefinition);
            }

            return copiedResourceDefinitions.ToArray();
        }
    }
}