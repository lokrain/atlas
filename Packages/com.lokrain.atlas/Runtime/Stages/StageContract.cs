#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Stages
{
    /// <summary>
    /// Defines the symbolic input and output contract for a catalog-owned generation stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A stage contract describes what a stage requires and produces at the catalog/planning boundary.
    /// It is compatibility metadata only; it does not define field storage, artifact storage, operation routing,
    /// execution behavior, runtime bindings, job data, or native containers.
    /// </para>
    /// <para>
    /// Required input symbols and produced output symbols are stable machine-facing contract values. They are
    /// intentionally represented as symbols at this layer so later field, artifact, catalog, and runnable-plan
    /// layers can resolve them without coupling stage metadata to execution storage.
    /// </para>
    /// <para>
    /// Required input symbols and produced output symbols may overlap. An overlap represents read-modify-write
    /// behavior for a symbolic contract value.
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
        /// <param name="requiredInputSymbols">The symbolic inputs required before the stage can execute.</param>
        /// <param name="producedOutputSymbols">The symbolic outputs produced by the stage.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stageDefinition"/>, <paramref name="requiredInputSymbols"/>,
        /// or <paramref name="producedOutputSymbols"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when either symbol collection contains null entries or duplicate symbols, or when both
        /// collections are empty.
        /// </exception>
        public StageContract(
            StageDefinition stageDefinition,
            IEnumerable<Symbol> requiredInputSymbols,
            IEnumerable<Symbol> producedOutputSymbols)
        {
            if (stageDefinition is null)
            {
                throw new ArgumentNullException(nameof(stageDefinition));
            }

            if (requiredInputSymbols is null)
            {
                throw new ArgumentNullException(nameof(requiredInputSymbols));
            }

            if (producedOutputSymbols is null)
            {
                throw new ArgumentNullException(nameof(producedOutputSymbols));
            }

            Symbol[] copiedRequiredInputSymbols = CopySymbols(
                requiredInputSymbols,
                nameof(requiredInputSymbols),
                "Stage contract required input symbols");

            Symbol[] copiedProducedOutputSymbols = CopySymbols(
                producedOutputSymbols,
                nameof(producedOutputSymbols),
                "Stage contract produced output symbols");

            if (copiedRequiredInputSymbols.Length == 0 && copiedProducedOutputSymbols.Length == 0)
            {
                throw new ArgumentException(
                    "Stage contract must contain at least one required input symbol or produced output symbol.",
                    nameof(producedOutputSymbols));
            }

            StageDefinition = stageDefinition;
            RequiredInputSymbols = new ReadOnlyCollection<Symbol>(copiedRequiredInputSymbols);
            ProducedOutputSymbols = new ReadOnlyCollection<Symbol>(copiedProducedOutputSymbols);
        }

        /// <summary>
        /// Gets the stage definition described by this contract.
        /// </summary>
        public StageDefinition StageDefinition { get; }

        /// <summary>
        /// Gets the symbolic inputs required before the stage can execute.
        /// </summary>
        public IReadOnlyList<Symbol> RequiredInputSymbols { get; }

        /// <summary>
        /// Gets the symbolic outputs produced by the stage.
        /// </summary>
        public IReadOnlyList<Symbol> ProducedOutputSymbols { get; }

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
            return $"{nameof(StageContract)}({nameof(StageDefinition)}: {StageDefinition.Symbol}, {nameof(RequiredInputSymbols)}: {RequiredInputSymbols.Count}, {nameof(ProducedOutputSymbols)}: {ProducedOutputSymbols.Count})";
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

        private static Symbol[] CopySymbols(
            IEnumerable<Symbol> symbols,
            string parameterName,
            string description)
        {
            var copiedSymbols = new List<Symbol>();
            var uniqueSymbols = new HashSet<Symbol>();

            foreach (Symbol? symbol in symbols)
            {
                if (symbol is null)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain null entries.",
                        parameterName);
                }

                if (!uniqueSymbols.Add(symbol))
                {
                    throw new ArgumentException(
                        $"{description} cannot contain duplicate symbol '{symbol}'.",
                        parameterName);
                }

                copiedSymbols.Add(symbol);
            }

            return copiedSymbols.ToArray();
        }
    }
}