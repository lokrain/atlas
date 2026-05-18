#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Planning
{
    /// <summary>
    /// Describes symbolic intent to create an accepted generation request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation request descriptor is unresolved symbolic input. It selects a generation recipe by symbol,
    /// carries generation-wide run settings, and optionally describes operation implementation overrides by symbol.
    /// </para>
    /// <para>
    /// This type is suitable for user input, editor tooling, importers, serialized descriptors, and higher-level
    /// APIs. It is not an accepted generation request, resolved recipe, generation plan, executable binding,
    /// scheduler binding, runtime state, job data, native container, ECS system, Burst function pointer, or Unity
    /// runtime object.
    /// </para>
    /// <para>
    /// A descriptor validates its own structure. Catalog-dependent satisfiability is established by
    /// <see cref="GenerationRequestResolver"/>.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationRequestDescriptor"/> instance is always a valid symbolic descriptor.
    /// </para>
    /// </remarks>
    public sealed class GenerationRequestDescriptor : IEquatable<GenerationRequestDescriptor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRequestDescriptor"/> class without operation
        /// implementation overrides.
        /// </summary>
        /// <param name="generationRecipeDefinitionSymbol">The selected generation-recipe-definition symbol.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationRecipeDefinitionSymbol"/> or <paramref name="runSettings"/> is null.
        /// </exception>
        public GenerationRequestDescriptor(
            Symbol generationRecipeDefinitionSymbol,
            GenerationRunSettings runSettings)
            : this(
                generationRecipeDefinitionSymbol,
                runSettings,
                Array.Empty<OperationImplementationOverrideDescriptor>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationRequestDescriptor"/> class.
        /// </summary>
        /// <param name="generationRecipeDefinitionSymbol">The selected generation-recipe-definition symbol.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <param name="operationImplementationOverrides">The operation implementation overrides.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generationRecipeDefinitionSymbol"/>, <paramref name="runSettings"/>, or
        /// <paramref name="operationImplementationOverrides"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="operationImplementationOverrides"/> contains null entries or duplicate
        /// stage-route-step-definition symbols.
        /// </exception>
        public GenerationRequestDescriptor(
            Symbol generationRecipeDefinitionSymbol,
            GenerationRunSettings runSettings,
            IEnumerable<OperationImplementationOverrideDescriptor> operationImplementationOverrides)
        {
            if (generationRecipeDefinitionSymbol is null)
            {
                throw new ArgumentNullException(nameof(generationRecipeDefinitionSymbol));
            }

            if (runSettings is null)
            {
                throw new ArgumentNullException(nameof(runSettings));
            }

            if (operationImplementationOverrides is null)
            {
                throw new ArgumentNullException(nameof(operationImplementationOverrides));
            }

            OperationImplementationOverrideDescriptor[] copiedOperationImplementationOverrides =
                CopyOperationImplementationOverrides(operationImplementationOverrides);

            GenerationRecipeDefinitionSymbol = generationRecipeDefinitionSymbol;
            RunSettings = runSettings;
            OperationImplementationOverrides =
                new ReadOnlyCollection<OperationImplementationOverrideDescriptor>(
                    copiedOperationImplementationOverrides);
        }

        /// <summary>
        /// Gets the selected generation-recipe-definition symbol.
        /// </summary>
        public Symbol GenerationRecipeDefinitionSymbol { get; }

        /// <summary>
        /// Gets the generation-wide run settings.
        /// </summary>
        public GenerationRunSettings RunSettings { get; }

        /// <summary>
        /// Gets the operation implementation overrides.
        /// </summary>
        public IReadOnlyList<OperationImplementationOverrideDescriptor> OperationImplementationOverrides { get; }

        /// <summary>
        /// Creates a generation request descriptor from a recipe symbol value without operation implementation
        /// overrides.
        /// </summary>
        /// <param name="generationRecipeDefinitionSymbol">The selected generation-recipe-definition symbol value.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <returns>A validated generation request descriptor.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="generationRecipeDefinitionSymbol"/> is null or not a valid symbol.
        /// </exception>
        public static GenerationRequestDescriptor Create(
            string? generationRecipeDefinitionSymbol,
            GenerationRunSettings runSettings)
        {
            return new(
                CreateSymbol(
                    generationRecipeDefinitionSymbol,
                    nameof(generationRecipeDefinitionSymbol),
                    "Generation recipe definition symbol"),
                runSettings);
        }

        /// <summary>
        /// Creates a generation request descriptor from a recipe symbol value.
        /// </summary>
        /// <param name="generationRecipeDefinitionSymbol">The selected generation-recipe-definition symbol value.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <param name="operationImplementationOverrides">The operation implementation overrides.</param>
        /// <returns>A validated generation request descriptor.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="generationRecipeDefinitionSymbol"/> is null or not a valid symbol.
        /// </exception>
        public static GenerationRequestDescriptor Create(
            string? generationRecipeDefinitionSymbol,
            GenerationRunSettings runSettings,
            IEnumerable<OperationImplementationOverrideDescriptor> operationImplementationOverrides)
        {
            return new(
                CreateSymbol(
                    generationRecipeDefinitionSymbol,
                    nameof(generationRecipeDefinitionSymbol),
                    "Generation recipe definition symbol"),
                runSettings,
                operationImplementationOverrides);
        }

        /// <summary>
        /// Attempts to create a generation request descriptor from a recipe symbol value without operation
        /// implementation overrides.
        /// </summary>
        /// <param name="generationRecipeDefinitionSymbol">The selected generation-recipe-definition symbol value.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <param name="descriptor">The created descriptor when validation succeeds.</param>
        /// <returns>
        /// <see langword="true"/> when the symbol value is valid and <paramref name="runSettings"/> is not null;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            string? generationRecipeDefinitionSymbol,
            GenerationRunSettings? runSettings,
            out GenerationRequestDescriptor? descriptor)
        {
            if (!Symbol.TryCreate(generationRecipeDefinitionSymbol, out Symbol? createdSymbol)
                || runSettings is null)
            {
                descriptor = null;
                return false;
            }

            descriptor = new(createdSymbol!, runSettings);
            return true;
        }

        /// <summary>
        /// Determines whether the specified values can create a valid generation request descriptor.
        /// </summary>
        /// <param name="generationRecipeDefinitionSymbol">The selected generation-recipe-definition symbol value.</param>
        /// <param name="runSettings">The generation-wide run settings.</param>
        /// <returns>
        /// <see langword="true"/> when the values can create a valid descriptor; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool IsValid(
            string? generationRecipeDefinitionSymbol,
            GenerationRunSettings? runSettings)
        {
            return Symbol.IsValid(generationRecipeDefinitionSymbol)
                && runSettings is not null;
        }

        /// <inheritdoc/>
        public bool Equals(GenerationRequestDescriptor? other)
        {
            return other is not null
                && GenerationRecipeDefinitionSymbol == other.GenerationRecipeDefinitionSymbol
                && RunSettings == other.RunSettings
                && SequenceEquals(OperationImplementationOverrides, other.OperationImplementationOverrides);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GenerationRequestDescriptor other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GenerationRecipeDefinitionSymbol);
            hashCode.Add(RunSettings);

            for (int index = 0; index < OperationImplementationOverrides.Count; index++)
            {
                hashCode.Add(OperationImplementationOverrides[index]);
            }

            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationRequestDescriptor)}({nameof(GenerationRecipeDefinitionSymbol)}: {GenerationRecipeDefinitionSymbol}, {nameof(RunSettings)}: {RunSettings}, {nameof(OperationImplementationOverrides)}: {OperationImplementationOverrides.Count})";
        }

        /// <summary>
        /// Determines whether two generation request descriptors are equal.
        /// </summary>
        public static bool operator ==(GenerationRequestDescriptor? left, GenerationRequestDescriptor? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two generation request descriptors are not equal.
        /// </summary>
        public static bool operator !=(GenerationRequestDescriptor? left, GenerationRequestDescriptor? right)
        {
            return !Equals(left, right);
        }

        private static OperationImplementationOverrideDescriptor[] CopyOperationImplementationOverrides(
            IEnumerable<OperationImplementationOverrideDescriptor> operationImplementationOverrides)
        {
            var copiedOverrides = new List<OperationImplementationOverrideDescriptor>();
            var selectedStageRouteStepDefinitionSymbols = new HashSet<Symbol>();

            foreach (OperationImplementationOverrideDescriptor? operationImplementationOverride in operationImplementationOverrides)
            {
                if (operationImplementationOverride is null)
                {
                    throw new ArgumentException(
                        "Operation implementation overrides cannot contain null entries.",
                        nameof(operationImplementationOverrides));
                }

                if (!selectedStageRouteStepDefinitionSymbols.Add(
                    operationImplementationOverride.StageRouteStepDefinitionSymbol))
                {
                    throw new ArgumentException(
                        $"Operation implementation overrides cannot contain duplicate stage-route-step-definition symbol '{operationImplementationOverride.StageRouteStepDefinitionSymbol}'.",
                        nameof(operationImplementationOverrides));
                }

                copiedOverrides.Add(operationImplementationOverride);
            }

            return copiedOverrides.ToArray();
        }

        private static Symbol CreateSymbol(
            string? value,
            string parameterName,
            string description)
        {
            if (!Symbol.TryCreate(value, out Symbol? symbol))
            {
                throw new ArgumentException(
                    $"{description} must be a valid symbol.",
                    parameterName);
            }

            return symbol!;
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