#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Represents an immutable accepted set of managed field definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A field definition set owns managed field metadata indexed by field symbol and by represented resource
    /// definition symbol.
    /// </para>
    /// <para>
    /// The set validates metadata-level uniqueness and rejects unspecified enum defaults. It does not validate
    /// catalog ownership, allocate storage, own native memory, schedule jobs, bind ECS data, or describe executable
    /// operation data.
    /// </para>
    /// <para>
    /// Each accepted field definition must have a unique field symbol and must represent a unique resource
    /// definition symbol.
    /// </para>
    /// <para>
    /// Public enumeration order is deterministic. Field definitions are exposed in ordinal field-symbol order.
    /// Private dictionaries are lookup indexes only and never define public order.
    /// </para>
    /// <para>
    /// A non-null <see cref="FieldDefinitionSet"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class FieldDefinitionSet
    {
        private readonly Dictionary<Symbol, FieldDefinition> _fieldDefinitionsBySymbol;
        private readonly Dictionary<Symbol, FieldDefinition> _fieldDefinitionsByResourceDefinitionSymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldDefinitionSet"/> class.
        /// </summary>
        /// <param name="fieldDefinitions">The field definitions owned by the set.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fieldDefinitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fieldDefinitions"/> contains null entries, duplicate field symbols,
        /// duplicate resource definition symbols, or unspecified enum defaults.
        /// </exception>
        public FieldDefinitionSet(IEnumerable<FieldDefinition> fieldDefinitions)
        {
            if (fieldDefinitions is null)
            {
                throw new ArgumentNullException(nameof(fieldDefinitions));
            }

            FieldDefinition[] copiedFieldDefinitions = CopyFieldDefinitions(fieldDefinitions);
            Array.Sort(copiedFieldDefinitions, static (left, right) => left.Symbol.CompareTo(right.Symbol));

            _fieldDefinitionsBySymbol = CreateIndex(
                copiedFieldDefinitions,
                static fieldDefinition => fieldDefinition.Symbol);

            _fieldDefinitionsByResourceDefinitionSymbol = CreateIndex(
                copiedFieldDefinitions,
                static fieldDefinition => fieldDefinition.ResourceDefinition.Symbol);

            FieldDefinitions = new ReadOnlyCollection<FieldDefinition>(copiedFieldDefinitions);
        }

        /// <summary>
        /// Gets the field definitions owned by the set in ordinal field-symbol order.
        /// </summary>
        public IReadOnlyList<FieldDefinition> FieldDefinitions { get; }

        /// <summary>
        /// Determines whether the set contains a field definition with the specified field symbol.
        /// </summary>
        /// <param name="fieldDefinitionSymbol">The field symbol to find.</param>
        /// <returns><see langword="true"/> when the field definition exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fieldDefinitionSymbol"/> is null.
        /// </exception>
        public bool ContainsFieldDefinition(Symbol fieldDefinitionSymbol)
        {
            return ContainsKey(
                _fieldDefinitionsBySymbol,
                fieldDefinitionSymbol,
                nameof(fieldDefinitionSymbol));
        }

        /// <summary>
        /// Gets the field definition with the specified field symbol.
        /// </summary>
        /// <param name="fieldDefinitionSymbol">The field symbol to find.</param>
        /// <returns>The matching field definition.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fieldDefinitionSymbol"/> is null.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no field definition has the specified symbol.
        /// </exception>
        public FieldDefinition GetFieldDefinition(Symbol fieldDefinitionSymbol)
        {
            return GetRequired(
                _fieldDefinitionsBySymbol,
                fieldDefinitionSymbol,
                nameof(fieldDefinitionSymbol),
                "Field definition");
        }

        /// <summary>
        /// Attempts to get the field definition with the specified field symbol.
        /// </summary>
        /// <param name="fieldDefinitionSymbol">The field symbol to find.</param>
        /// <param name="fieldDefinition">The matching field definition when found.</param>
        /// <returns><see langword="true"/> when the field definition exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fieldDefinitionSymbol"/> is null.
        /// </exception>
        public bool TryGetFieldDefinition(
            Symbol fieldDefinitionSymbol,
            out FieldDefinition? fieldDefinition)
        {
            return TryGet(
                _fieldDefinitionsBySymbol,
                fieldDefinitionSymbol,
                nameof(fieldDefinitionSymbol),
                out fieldDefinition);
        }

        /// <summary>
        /// Determines whether the set contains a field definition for the specified resource definition symbol.
        /// </summary>
        /// <param name="resourceDefinitionSymbol">The resource definition symbol to find.</param>
        /// <returns><see langword="true"/> when a field definition exists for the resource; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinitionSymbol"/> is null.
        /// </exception>
        public bool ContainsFieldDefinitionForResourceDefinition(Symbol resourceDefinitionSymbol)
        {
            return ContainsKey(
                _fieldDefinitionsByResourceDefinitionSymbol,
                resourceDefinitionSymbol,
                nameof(resourceDefinitionSymbol));
        }

        /// <summary>
        /// Gets the field definition for the specified resource definition symbol.
        /// </summary>
        /// <param name="resourceDefinitionSymbol">The resource definition symbol to find.</param>
        /// <returns>The matching field definition.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinitionSymbol"/> is null.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no field definition represents the specified resource definition symbol.
        /// </exception>
        public FieldDefinition GetFieldDefinitionForResourceDefinition(Symbol resourceDefinitionSymbol)
        {
            return GetRequired(
                _fieldDefinitionsByResourceDefinitionSymbol,
                resourceDefinitionSymbol,
                nameof(resourceDefinitionSymbol),
                "Field definition");
        }

        /// <summary>
        /// Attempts to get the field definition for the specified resource definition symbol.
        /// </summary>
        /// <param name="resourceDefinitionSymbol">The resource definition symbol to find.</param>
        /// <param name="fieldDefinition">The matching field definition when found.</param>
        /// <returns><see langword="true"/> when a field definition exists for the resource; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resourceDefinitionSymbol"/> is null.
        /// </exception>
        public bool TryGetFieldDefinitionForResourceDefinition(
            Symbol resourceDefinitionSymbol,
            out FieldDefinition? fieldDefinition)
        {
            return TryGet(
                _fieldDefinitionsByResourceDefinitionSymbol,
                resourceDefinitionSymbol,
                nameof(resourceDefinitionSymbol),
                out fieldDefinition);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(FieldDefinitionSet)}({nameof(FieldDefinitions)}: {FieldDefinitions.Count})";
        }

        private static FieldDefinition[] CopyFieldDefinitions(IEnumerable<FieldDefinition> fieldDefinitions)
        {
            var copiedFieldDefinitions = new List<FieldDefinition>();
            var fieldDefinitionSymbols = new HashSet<Symbol>();
            var resourceDefinitionSymbols = new HashSet<Symbol>();

            foreach (FieldDefinition? fieldDefinition in fieldDefinitions)
            {
                if (fieldDefinition is null)
                {
                    throw new ArgumentException(
                        "Field definitions cannot contain null entries.",
                        nameof(fieldDefinitions));
                }

                ValidateFieldShape(fieldDefinition.Shape);
                ValidateFieldValueKind(fieldDefinition.ValueKind);

                if (!fieldDefinitionSymbols.Add(fieldDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Field definitions cannot contain duplicate field symbol '{fieldDefinition.Symbol}'.",
                        nameof(fieldDefinitions));
                }

                if (!resourceDefinitionSymbols.Add(fieldDefinition.ResourceDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Field definitions cannot contain duplicate resource definition symbol '{fieldDefinition.ResourceDefinition.Symbol}'.",
                        nameof(fieldDefinitions));
                }

                copiedFieldDefinitions.Add(fieldDefinition);
            }

            return copiedFieldDefinitions.ToArray();
        }

        private static void ValidateFieldShape(FieldShape shape)
        {
            if (shape == FieldShape.Unknown || !Enum.IsDefined(typeof(FieldShape), shape))
            {
                throw new ArgumentException(
                    "Field definitions cannot contain a field with an unspecified or unsupported field shape.",
                    nameof(shape));
            }
        }

        private static void ValidateFieldValueKind(FieldValueKind valueKind)
        {
            if (valueKind == FieldValueKind.Unknown || !Enum.IsDefined(typeof(FieldValueKind), valueKind))
            {
                throw new ArgumentException(
                    "Field definitions cannot contain a field with an unspecified or unsupported field value kind.",
                    nameof(valueKind));
            }
        }

        private static Dictionary<Symbol, FieldDefinition> CreateIndex(
            IEnumerable<FieldDefinition> fieldDefinitions,
            Func<FieldDefinition, Symbol> getSymbol)
        {
            var index = new Dictionary<Symbol, FieldDefinition>();

            foreach (FieldDefinition fieldDefinition in fieldDefinitions)
            {
                index.Add(getSymbol(fieldDefinition), fieldDefinition);
            }

            return index;
        }

        private static bool ContainsKey(
            Dictionary<Symbol, FieldDefinition> fieldDefinitionsBySymbol,
            Symbol symbol,
            string parameterName)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return fieldDefinitionsBySymbol.ContainsKey(symbol);
        }

        private static FieldDefinition GetRequired(
            Dictionary<Symbol, FieldDefinition> fieldDefinitionsBySymbol,
            Symbol symbol,
            string parameterName,
            string description)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!fieldDefinitionsBySymbol.TryGetValue(symbol, out FieldDefinition fieldDefinition))
            {
                throw new KeyNotFoundException($"{description} '{symbol}' was not found.");
            }

            return fieldDefinition;
        }

        private static bool TryGet(
            Dictionary<Symbol, FieldDefinition> fieldDefinitionsBySymbol,
            Symbol symbol,
            string parameterName,
            out FieldDefinition? fieldDefinition)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return fieldDefinitionsBySymbol.TryGetValue(symbol, out fieldDefinition);
        }
    }
}
