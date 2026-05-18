#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Resources;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Defines the semantic input and output resource contract for a catalog-owned generation stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage contract describes what a stage requires and produces at the catalog/planning boundary.
    /// It is compatibility metadata only; it does not define field storage, artifact storage, operation routing,
    /// execution behavior, runtime bindings, job data, or native containers.
    /// </para>
    /// <para>
    /// Required inputs and produced outputs are semantic resource definitions. They answer what values are
    /// required or produced by the stage, not how those values are stored or executed.
    /// </para>
    /// <para>
    /// Required inputs and produced outputs may overlap. An overlap represents read-modify-write behavior for
    /// a semantic resource.
    /// </para>
    /// <para>
    /// Equality is based on <see cref="StageDefinition"/> because a stage has one contract identity. Contract
    /// content conflicts must be handled by catalog validation, not by treating duplicate stage contracts as
    /// distinct identities.
    /// </para>
    /// <para>
    /// A non-null <see cref="StageContract"/> instance is always syntactically valid.
    /// Catalog-dependent semantic validity is established by the generation catalog.
    /// </para>
    /// </remarks>
    public sealed class StageContract : IEquatable<StageContract>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StageContract"/> class.
        /// </summary>
        /// <param name="stageDefinition">The stage definition described by this contract.</param>
        /// <param name="requiredInputs">The semantic resources required by the stage.</param>
        /// <param name="producedOutputs">The semantic resources produced by the stage.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageDefinition"/>, <paramref name="requiredInputs"/>,
        /// or <paramref name="producedOutputs"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when either resource collection contains null entries or duplicate resources, when a resource
        /// belongs to a different generation schema than the stage, or when both collections are empty.
        /// </exception>
        public StageContract(
            StageDefinition stageDefinition,
            IEnumerable<ResourceDefinition> requiredInputs,
            IEnumerable<ResourceDefinition> producedOutputs)
        {
            if (stageDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageDefinition));
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
                stageDefinition,
                nameof(requiredInputs),
                "Stage contract required inputs");

            ResourceDefinition[] copiedProducedOutputs = CopyResourceDefinitions(
                producedOutputs,
                stageDefinition,
                nameof(producedOutputs),
                "Stage contract produced outputs");

            if (copiedRequiredInputs.Length == 0 && copiedProducedOutputs.Length == 0)
            {
                throw new ArgumentException(
                    "Stage contract must contain at least one required input resource or produced output resource.",
                    nameof(producedOutputs));
            }

            StageDefinition = stageDefinition;
            RequiredInputs = new ReadOnlyCollection<ResourceDefinition>(copiedRequiredInputs);
            ProducedOutputs = new ReadOnlyCollection<ResourceDefinition>(copiedProducedOutputs);
        }

        /// <summary>
        /// Gets the stage definition described by this contract.
        /// </summary>
        public StageDefinition StageDefinition { get; }

        /// <summary>
        /// Gets the semantic resources required before the stage can execute.
        /// </summary>
        public IReadOnlyList<ResourceDefinition> RequiredInputs { get; }

        /// <summary>
        /// Gets the semantic resources produced by the stage.
        /// </summary>
        public IReadOnlyList<ResourceDefinition> ProducedOutputs { get; }

        /// <inheritdoc/>
        public bool Equals(StageContract? other)
        {
            return other is not null && StageDefinition == other.StageDefinition;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is StageContract other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StageDefinition.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(StageContract)}({nameof(StageDefinition)}: {StageDefinition.Symbol}, {nameof(RequiredInputs)}: {RequiredInputs.Count}, {nameof(ProducedOutputs)}: {ProducedOutputs.Count})";
        }

        /// <summary>
        /// Determines whether two stage contracts are equal.
        /// </summary>
        public static bool operator ==(StageContract? left, StageContract? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two stage contracts are not equal.
        /// </summary>
        public static bool operator !=(StageContract? left, StageContract? right)
        {
            return !Equals(left, right);
        }

        private static ResourceDefinition[] CopyResourceDefinitions(
            IEnumerable<ResourceDefinition> resourceDefinitions,
            StageDefinition stageDefinition,
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

                if (resourceDefinition.GenerationSchema != stageDefinition.GenerationSchema)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain resource '{resourceDefinition.Symbol}' from generation schema '{resourceDefinition.GenerationSchema.Symbol}' because stage '{stageDefinition.Symbol}' belongs to generation schema '{stageDefinition.GenerationSchema.Symbol}'.",
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