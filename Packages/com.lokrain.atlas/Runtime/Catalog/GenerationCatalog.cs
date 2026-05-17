#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;

namespace Lokrain.Atlas.Catalog
{
    /// <summary>
    /// Represents an immutable accepted generation catalog snapshot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generation catalog owns validated schema, stage, route, route-step, operation, implementation, and
    /// contract definitions used by managed planning. It is not a mutable registry, builder, Unity asset,
    /// execution container, ECS world object, runtime binding table, job scheduler, or native data owner.
    /// </para>
    /// <para>
    /// Definitions are indexed by stable machine-facing symbols. Display names are user-facing metadata only
    /// and must not be used for lookup, deterministic generation, catalog resolution, or artifact compatibility.
    /// </para>
    /// <para>
    /// Referenced owner definitions must be present in the same catalog instance. Routes must be compatible with
    /// their stage contracts through their ordered operation contracts before the catalog is accepted.
    /// </para>
    /// <para>
    /// A non-null <see cref="GenerationCatalog"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class GenerationCatalog
    {
        private readonly Dictionary<Symbol, GenerationSchemaDefinition> _generationSchemaDefinitionsBySymbol;
        private readonly Dictionary<Symbol, StageDefinition> _stageDefinitionsBySymbol;
        private readonly Dictionary<Symbol, StageRouteDefinition> _stageRouteDefinitionsBySymbol;
        private readonly Dictionary<Symbol, StageRouteStepDefinition> _stageRouteStepDefinitionsBySymbol;
        private readonly Dictionary<Symbol, StageContract> _stageContractsByStageDefinitionSymbol;
        private readonly Dictionary<Symbol, OperationDefinition> _operationDefinitionsBySymbol;
        private readonly Dictionary<Symbol, OperationImplementationDefinition> _operationImplementationDefinitionsBySymbol;
        private readonly Dictionary<Symbol, OperationContract> _operationContractsByOperationDefinitionSymbol;

        internal GenerationCatalog(
            IEnumerable<GenerationSchemaDefinition> generationSchemaDefinitions,
            IEnumerable<StageDefinition> stageDefinitions,
            IEnumerable<StageRouteDefinition> stageRouteDefinitions,
            IEnumerable<StageContract> stageContracts,
            IEnumerable<OperationDefinition> operationDefinitions,
            IEnumerable<OperationImplementationDefinition> operationImplementationDefinitions,
            IEnumerable<OperationContract> operationContracts)
        {
            GenerationSchemaDefinition[] copiedGenerationSchemaDefinitions = CopyUniqueBySymbol(
                generationSchemaDefinitions,
                definition => definition.Symbol,
                nameof(generationSchemaDefinitions),
                "Generation schema definitions");

            if (copiedGenerationSchemaDefinitions.Length == 0)
            {
                throw new ArgumentException(
                    "Generation catalog must contain at least one generation schema definition.",
                    nameof(generationSchemaDefinitions));
            }

            StageDefinition[] copiedStageDefinitions = CopyUniqueBySymbol(
                stageDefinitions,
                definition => definition.Symbol,
                nameof(stageDefinitions),
                "Stage definitions");

            StageRouteDefinition[] copiedStageRouteDefinitions = CopyUniqueBySymbol(
                stageRouteDefinitions,
                definition => definition.Symbol,
                nameof(stageRouteDefinitions),
                "Stage route definitions");

            StageRouteStepDefinition[] copiedStageRouteStepDefinitions = CopyUniqueStageRouteStepDefinitions(
                copiedStageRouteDefinitions,
                nameof(stageRouteDefinitions));

            StageContract[] copiedStageContracts = CopyUniqueContracts(
                stageContracts,
                contract => contract.StageDefinition.Symbol,
                nameof(stageContracts),
                "Stage contracts");

            OperationDefinition[] copiedOperationDefinitions = CopyUniqueBySymbol(
                operationDefinitions,
                definition => definition.Symbol,
                nameof(operationDefinitions),
                "Operation definitions");

            OperationImplementationDefinition[] copiedOperationImplementationDefinitions = CopyUniqueBySymbol(
                operationImplementationDefinitions,
                definition => definition.Symbol,
                nameof(operationImplementationDefinitions),
                "Operation implementation definitions");

            OperationContract[] copiedOperationContracts = CopyUniqueContracts(
                operationContracts,
                contract => contract.OperationDefinition.Symbol,
                nameof(operationContracts),
                "Operation contracts");

            _generationSchemaDefinitionsBySymbol = CreateIndex(
                copiedGenerationSchemaDefinitions,
                definition => definition.Symbol);

            _stageDefinitionsBySymbol = CreateIndex(
                copiedStageDefinitions,
                definition => definition.Symbol);

            _stageRouteDefinitionsBySymbol = CreateIndex(
                copiedStageRouteDefinitions,
                definition => definition.Symbol);

            _stageRouteStepDefinitionsBySymbol = CreateIndex(
                copiedStageRouteStepDefinitions,
                definition => definition.Symbol);

            _stageContractsByStageDefinitionSymbol = CreateIndex(
                copiedStageContracts,
                contract => contract.StageDefinition.Symbol);

            _operationDefinitionsBySymbol = CreateIndex(
                copiedOperationDefinitions,
                definition => definition.Symbol);

            _operationImplementationDefinitionsBySymbol = CreateIndex(
                copiedOperationImplementationDefinitions,
                definition => definition.Symbol);

            _operationContractsByOperationDefinitionSymbol = CreateIndex(
                copiedOperationContracts,
                contract => contract.OperationDefinition.Symbol);

            Dictionary<Symbol, int> stageRouteCountsByStageDefinitionSymbol = CreateCountIndex(
                copiedStageRouteDefinitions,
                definition => definition.StageDefinition.Symbol);

            Dictionary<Symbol, int> operationImplementationCountsByOperationDefinitionSymbol = CreateCountIndex(
                copiedOperationImplementationDefinitions,
                definition => definition.OperationDefinition.Symbol);

            ValidateStageContracts(
                copiedStageContracts,
                _stageDefinitionsBySymbol,
                nameof(stageContracts));

            ValidateOperationContracts(
                copiedOperationContracts,
                _operationDefinitionsBySymbol,
                nameof(operationContracts));

            ValidateStageDefinitions(
                copiedStageDefinitions,
                _generationSchemaDefinitionsBySymbol,
                _stageContractsByStageDefinitionSymbol,
                stageRouteCountsByStageDefinitionSymbol,
                nameof(stageDefinitions));

            ValidateOperationDefinitions(
                copiedOperationDefinitions,
                _generationSchemaDefinitionsBySymbol,
                _operationContractsByOperationDefinitionSymbol,
                operationImplementationCountsByOperationDefinitionSymbol,
                nameof(operationDefinitions));

            ValidateOperationImplementationDefinitions(
                copiedOperationImplementationDefinitions,
                _operationDefinitionsBySymbol,
                nameof(operationImplementationDefinitions));

            ValidateStageRouteDefinitions(
                copiedStageRouteDefinitions,
                _stageDefinitionsBySymbol,
                _operationDefinitionsBySymbol,
                _stageContractsByStageDefinitionSymbol,
                _operationContractsByOperationDefinitionSymbol,
                nameof(stageRouteDefinitions));

            GenerationSchemaDefinitions = new ReadOnlyCollection<GenerationSchemaDefinition>(
                copiedGenerationSchemaDefinitions);

            StageDefinitions = new ReadOnlyCollection<StageDefinition>(
                copiedStageDefinitions);

            StageRouteDefinitions = new ReadOnlyCollection<StageRouteDefinition>(
                copiedStageRouteDefinitions);

            StageRouteStepDefinitions = new ReadOnlyCollection<StageRouteStepDefinition>(
                copiedStageRouteStepDefinitions);

            StageContracts = new ReadOnlyCollection<StageContract>(
                copiedStageContracts);

            OperationDefinitions = new ReadOnlyCollection<OperationDefinition>(
                copiedOperationDefinitions);

            OperationImplementationDefinitions = new ReadOnlyCollection<OperationImplementationDefinition>(
                copiedOperationImplementationDefinitions);

            OperationContracts = new ReadOnlyCollection<OperationContract>(
                copiedOperationContracts);
        }

        /// <summary>
        /// Gets the generation schema definitions owned by the catalog.
        /// </summary>
        public IReadOnlyList<GenerationSchemaDefinition> GenerationSchemaDefinitions { get; }

        /// <summary>
        /// Gets the stage definitions owned by the catalog.
        /// </summary>
        public IReadOnlyList<StageDefinition> StageDefinitions { get; }

        /// <summary>
        /// Gets the stage route definitions owned by the catalog.
        /// </summary>
        public IReadOnlyList<StageRouteDefinition> StageRouteDefinitions { get; }

        /// <summary>
        /// Gets the stage route step definitions owned by the catalog.
        /// </summary>
        public IReadOnlyList<StageRouteStepDefinition> StageRouteStepDefinitions { get; }

        /// <summary>
        /// Gets the stage contracts owned by the catalog.
        /// </summary>
        public IReadOnlyList<StageContract> StageContracts { get; }

        /// <summary>
        /// Gets the operation definitions owned by the catalog.
        /// </summary>
        public IReadOnlyList<OperationDefinition> OperationDefinitions { get; }

        /// <summary>
        /// Gets the operation implementation definitions owned by the catalog.
        /// </summary>
        public IReadOnlyList<OperationImplementationDefinition> OperationImplementationDefinitions { get; }

        /// <summary>
        /// Gets the operation contracts owned by the catalog.
        /// </summary>
        public IReadOnlyList<OperationContract> OperationContracts { get; }

        /// <summary>
        /// Determines whether the catalog contains a generation schema definition with the specified symbol.
        /// </summary>
        public bool ContainsGenerationSchemaDefinition(Symbol generationSchemaDefinitionSymbol)
        {
            return ContainsKey(
                _generationSchemaDefinitionsBySymbol,
                generationSchemaDefinitionSymbol,
                nameof(generationSchemaDefinitionSymbol));
        }

        /// <summary>
        /// Gets the generation schema definition with the specified symbol.
        /// </summary>
        public GenerationSchemaDefinition GetGenerationSchemaDefinition(Symbol generationSchemaDefinitionSymbol)
        {
            return GetRequired(
                _generationSchemaDefinitionsBySymbol,
                generationSchemaDefinitionSymbol,
                nameof(generationSchemaDefinitionSymbol),
                "Generation schema definition");
        }

        /// <summary>
        /// Attempts to get the generation schema definition with the specified symbol.
        /// </summary>
        public bool TryGetGenerationSchemaDefinition(
            Symbol generationSchemaDefinitionSymbol,
            out GenerationSchemaDefinition? generationSchemaDefinition)
        {
            return TryGet(
                _generationSchemaDefinitionsBySymbol,
                generationSchemaDefinitionSymbol,
                nameof(generationSchemaDefinitionSymbol),
                out generationSchemaDefinition);
        }

        /// <summary>
        /// Determines whether the catalog contains a stage definition with the specified symbol.
        /// </summary>
        public bool ContainsStageDefinition(Symbol stageDefinitionSymbol)
        {
            return ContainsKey(
                _stageDefinitionsBySymbol,
                stageDefinitionSymbol,
                nameof(stageDefinitionSymbol));
        }

        /// <summary>
        /// Gets the stage definition with the specified symbol.
        /// </summary>
        public StageDefinition GetStageDefinition(Symbol stageDefinitionSymbol)
        {
            return GetRequired(
                _stageDefinitionsBySymbol,
                stageDefinitionSymbol,
                nameof(stageDefinitionSymbol),
                "Stage definition");
        }

        /// <summary>
        /// Attempts to get the stage definition with the specified symbol.
        /// </summary>
        public bool TryGetStageDefinition(Symbol stageDefinitionSymbol, out StageDefinition? stageDefinition)
        {
            return TryGet(
                _stageDefinitionsBySymbol,
                stageDefinitionSymbol,
                nameof(stageDefinitionSymbol),
                out stageDefinition);
        }

        /// <summary>
        /// Determines whether the catalog contains a stage route definition with the specified symbol.
        /// </summary>
        public bool ContainsStageRouteDefinition(Symbol stageRouteDefinitionSymbol)
        {
            return ContainsKey(
                _stageRouteDefinitionsBySymbol,
                stageRouteDefinitionSymbol,
                nameof(stageRouteDefinitionSymbol));
        }

        /// <summary>
        /// Gets the stage route definition with the specified symbol.
        /// </summary>
        public StageRouteDefinition GetStageRouteDefinition(Symbol stageRouteDefinitionSymbol)
        {
            return GetRequired(
                _stageRouteDefinitionsBySymbol,
                stageRouteDefinitionSymbol,
                nameof(stageRouteDefinitionSymbol),
                "Stage route definition");
        }

        /// <summary>
        /// Attempts to get the stage route definition with the specified symbol.
        /// </summary>
        public bool TryGetStageRouteDefinition(
            Symbol stageRouteDefinitionSymbol,
            out StageRouteDefinition? stageRouteDefinition)
        {
            return TryGet(
                _stageRouteDefinitionsBySymbol,
                stageRouteDefinitionSymbol,
                nameof(stageRouteDefinitionSymbol),
                out stageRouteDefinition);
        }

        /// <summary>
        /// Determines whether the catalog contains a stage route step definition with the specified symbol.
        /// </summary>
        public bool ContainsStageRouteStepDefinition(Symbol stageRouteStepDefinitionSymbol)
        {
            return ContainsKey(
                _stageRouteStepDefinitionsBySymbol,
                stageRouteStepDefinitionSymbol,
                nameof(stageRouteStepDefinitionSymbol));
        }

        /// <summary>
        /// Gets the stage route step definition with the specified symbol.
        /// </summary>
        public StageRouteStepDefinition GetStageRouteStepDefinition(Symbol stageRouteStepDefinitionSymbol)
        {
            return GetRequired(
                _stageRouteStepDefinitionsBySymbol,
                stageRouteStepDefinitionSymbol,
                nameof(stageRouteStepDefinitionSymbol),
                "Stage route step definition");
        }

        /// <summary>
        /// Attempts to get the stage route step definition with the specified symbol.
        /// </summary>
        public bool TryGetStageRouteStepDefinition(
            Symbol stageRouteStepDefinitionSymbol,
            out StageRouteStepDefinition? stageRouteStepDefinition)
        {
            return TryGet(
                _stageRouteStepDefinitionsBySymbol,
                stageRouteStepDefinitionSymbol,
                nameof(stageRouteStepDefinitionSymbol),
                out stageRouteStepDefinition);
        }

        /// <summary>
        /// Determines whether the catalog contains a stage contract for the specified stage-definition symbol.
        /// </summary>
        public bool ContainsStageContract(Symbol stageDefinitionSymbol)
        {
            return ContainsKey(
                _stageContractsByStageDefinitionSymbol,
                stageDefinitionSymbol,
                nameof(stageDefinitionSymbol));
        }

        /// <summary>
        /// Gets the stage contract for the specified stage-definition symbol.
        /// </summary>
        public StageContract GetStageContract(Symbol stageDefinitionSymbol)
        {
            return GetRequired(
                _stageContractsByStageDefinitionSymbol,
                stageDefinitionSymbol,
                nameof(stageDefinitionSymbol),
                "Stage contract");
        }

        /// <summary>
        /// Attempts to get the stage contract for the specified stage-definition symbol.
        /// </summary>
        public bool TryGetStageContract(Symbol stageDefinitionSymbol, out StageContract? stageContract)
        {
            return TryGet(
                _stageContractsByStageDefinitionSymbol,
                stageDefinitionSymbol,
                nameof(stageDefinitionSymbol),
                out stageContract);
        }

        /// <summary>
        /// Determines whether the catalog contains an operation definition with the specified symbol.
        /// </summary>
        public bool ContainsOperationDefinition(Symbol operationDefinitionSymbol)
        {
            return ContainsKey(
                _operationDefinitionsBySymbol,
                operationDefinitionSymbol,
                nameof(operationDefinitionSymbol));
        }

        /// <summary>
        /// Gets the operation definition with the specified symbol.
        /// </summary>
        public OperationDefinition GetOperationDefinition(Symbol operationDefinitionSymbol)
        {
            return GetRequired(
                _operationDefinitionsBySymbol,
                operationDefinitionSymbol,
                nameof(operationDefinitionSymbol),
                "Operation definition");
        }

        /// <summary>
        /// Attempts to get the operation definition with the specified symbol.
        /// </summary>
        public bool TryGetOperationDefinition(
            Symbol operationDefinitionSymbol,
            out OperationDefinition? operationDefinition)
        {
            return TryGet(
                _operationDefinitionsBySymbol,
                operationDefinitionSymbol,
                nameof(operationDefinitionSymbol),
                out operationDefinition);
        }

        /// <summary>
        /// Determines whether the catalog contains an operation implementation definition with the specified symbol.
        /// </summary>
        public bool ContainsOperationImplementationDefinition(Symbol operationImplementationDefinitionSymbol)
        {
            return ContainsKey(
                _operationImplementationDefinitionsBySymbol,
                operationImplementationDefinitionSymbol,
                nameof(operationImplementationDefinitionSymbol));
        }

        /// <summary>
        /// Gets the operation implementation definition with the specified symbol.
        /// </summary>
        public OperationImplementationDefinition GetOperationImplementationDefinition(
            Symbol operationImplementationDefinitionSymbol)
        {
            return GetRequired(
                _operationImplementationDefinitionsBySymbol,
                operationImplementationDefinitionSymbol,
                nameof(operationImplementationDefinitionSymbol),
                "Operation implementation definition");
        }

        /// <summary>
        /// Attempts to get the operation implementation definition with the specified symbol.
        /// </summary>
        public bool TryGetOperationImplementationDefinition(
            Symbol operationImplementationDefinitionSymbol,
            out OperationImplementationDefinition? operationImplementationDefinition)
        {
            return TryGet(
                _operationImplementationDefinitionsBySymbol,
                operationImplementationDefinitionSymbol,
                nameof(operationImplementationDefinitionSymbol),
                out operationImplementationDefinition);
        }

        /// <summary>
        /// Determines whether the catalog contains an operation contract for the specified operation-definition symbol.
        /// </summary>
        public bool ContainsOperationContract(Symbol operationDefinitionSymbol)
        {
            return ContainsKey(
                _operationContractsByOperationDefinitionSymbol,
                operationDefinitionSymbol,
                nameof(operationDefinitionSymbol));
        }

        /// <summary>
        /// Gets the operation contract for the specified operation-definition symbol.
        /// </summary>
        public OperationContract GetOperationContract(Symbol operationDefinitionSymbol)
        {
            return GetRequired(
                _operationContractsByOperationDefinitionSymbol,
                operationDefinitionSymbol,
                nameof(operationDefinitionSymbol),
                "Operation contract");
        }

        /// <summary>
        /// Attempts to get the operation contract for the specified operation-definition symbol.
        /// </summary>
        public bool TryGetOperationContract(
            Symbol operationDefinitionSymbol,
            out OperationContract? operationContract)
        {
            return TryGet(
                _operationContractsByOperationDefinitionSymbol,
                operationDefinitionSymbol,
                nameof(operationDefinitionSymbol),
                out operationContract);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(GenerationCatalog)}({nameof(GenerationSchemaDefinitions)}: {GenerationSchemaDefinitions.Count}, {nameof(StageDefinitions)}: {StageDefinitions.Count}, {nameof(StageRouteDefinitions)}: {StageRouteDefinitions.Count}, {nameof(StageRouteStepDefinitions)}: {StageRouteStepDefinitions.Count}, {nameof(OperationDefinitions)}: {OperationDefinitions.Count}, {nameof(OperationImplementationDefinitions)}: {OperationImplementationDefinitions.Count})";
        }

        private static TDefinition[] CopyUniqueBySymbol<TDefinition>(
            IEnumerable<TDefinition> definitions,
            Func<TDefinition, Symbol> getSymbol,
            string parameterName,
            string description)
            where TDefinition : class
        {
            if (definitions is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copiedDefinitions = new List<TDefinition>();
            var uniqueSymbols = new HashSet<Symbol>();

            foreach (TDefinition? definition in definitions)
            {
                if (definition is null)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain null entries.",
                        parameterName);
                }

                Symbol symbol = getSymbol(definition);

                if (!uniqueSymbols.Add(symbol))
                {
                    throw new ArgumentException(
                        $"{description} cannot contain duplicate symbol '{symbol}'.",
                        parameterName);
                }

                copiedDefinitions.Add(definition);
            }

            return copiedDefinitions.ToArray();
        }

        private static StageRouteStepDefinition[] CopyUniqueStageRouteStepDefinitions(
            IEnumerable<StageRouteDefinition> stageRouteDefinitions,
            string parameterName)
        {
            var copiedDefinitions = new List<StageRouteStepDefinition>();
            var uniqueRouteStepSymbols = new HashSet<Symbol>();

            foreach (StageRouteDefinition stageRouteDefinition in stageRouteDefinitions)
            {
                foreach (StageRouteStepDefinition stageRouteStepDefinition in stageRouteDefinition.StageRouteStepDefinitions)
                {
                    if (!uniqueRouteStepSymbols.Add(stageRouteStepDefinition.Symbol))
                    {
                        throw new ArgumentException(
                            $"Stage route step definitions cannot contain duplicate route-step symbol '{stageRouteStepDefinition.Symbol}' across the catalog.",
                            parameterName);
                    }

                    copiedDefinitions.Add(stageRouteStepDefinition);
                }
            }

            return copiedDefinitions.ToArray();
        }

        private static TContract[] CopyUniqueContracts<TContract>(
            IEnumerable<TContract> contracts,
            Func<TContract, Symbol> getOwnerSymbol,
            string parameterName,
            string description)
            where TContract : class
        {
            if (contracts is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copiedContracts = new List<TContract>();
            var uniqueOwnerSymbols = new HashSet<Symbol>();

            foreach (TContract? contract in contracts)
            {
                if (contract is null)
                {
                    throw new ArgumentException(
                        $"{description} cannot contain null entries.",
                        parameterName);
                }

                Symbol ownerSymbol = getOwnerSymbol(contract);

                if (!uniqueOwnerSymbols.Add(ownerSymbol))
                {
                    throw new ArgumentException(
                        $"{description} cannot contain more than one entry for owner symbol '{ownerSymbol}'.",
                        parameterName);
                }

                copiedContracts.Add(contract);
            }

            return copiedContracts.ToArray();
        }

        private static Dictionary<Symbol, TDefinition> CreateIndex<TDefinition>(
            IEnumerable<TDefinition> definitions,
            Func<TDefinition, Symbol> getSymbol)
            where TDefinition : class
        {
            var index = new Dictionary<Symbol, TDefinition>();

            foreach (TDefinition definition in definitions)
            {
                index.Add(getSymbol(definition), definition);
            }

            return index;
        }

        private static Dictionary<Symbol, int> CreateCountIndex<TDefinition>(
            IEnumerable<TDefinition> definitions,
            Func<TDefinition, Symbol> getSymbol)
            where TDefinition : class
        {
            var counts = new Dictionary<Symbol, int>();

            foreach (TDefinition definition in definitions)
            {
                Symbol symbol = getSymbol(definition);

                if (counts.TryGetValue(symbol, out int count))
                {
                    counts[symbol] = count + 1;
                }
                else
                {
                    counts.Add(symbol, 1);
                }
            }

            return counts;
        }

        private static bool ContainsKey<TDefinition>(
            Dictionary<Symbol, TDefinition> definitionsBySymbol,
            Symbol symbol,
            string parameterName)
            where TDefinition : class
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return definitionsBySymbol.ContainsKey(symbol);
        }

        private static TDefinition GetRequired<TDefinition>(
            Dictionary<Symbol, TDefinition> definitionsBySymbol,
            Symbol symbol,
            string parameterName,
            string description)
            where TDefinition : class
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!definitionsBySymbol.TryGetValue(symbol, out TDefinition definition))
            {
                throw new KeyNotFoundException($"{description} '{symbol}' was not found in the generation catalog.");
            }

            return definition;
        }

        private static bool TryGet<TDefinition>(
            Dictionary<Symbol, TDefinition> definitionsBySymbol,
            Symbol symbol,
            string parameterName,
            out TDefinition? definition)
            where TDefinition : class
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return definitionsBySymbol.TryGetValue(symbol, out definition);
        }

        private static void ValidateStageDefinitions(
            IEnumerable<StageDefinition> stageDefinitions,
            Dictionary<Symbol, GenerationSchemaDefinition> generationSchemaDefinitionsBySymbol,
            Dictionary<Symbol, StageContract> stageContractsByStageDefinitionSymbol,
            Dictionary<Symbol, int> stageRouteCountsByStageDefinitionSymbol,
            string parameterName)
        {
            foreach (StageDefinition stageDefinition in stageDefinitions)
            {
                ValidateGenerationSchemaReference(
                    stageDefinition.GenerationSchema,
                    generationSchemaDefinitionsBySymbol,
                    parameterName,
                    $"Stage definition '{stageDefinition.Symbol}'");

                if (!stageContractsByStageDefinitionSymbol.ContainsKey(stageDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage definition '{stageDefinition.Symbol}' must have exactly one stage contract.",
                        parameterName);
                }

                if (!stageRouteCountsByStageDefinitionSymbol.ContainsKey(stageDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Stage definition '{stageDefinition.Symbol}' must have at least one stage route definition.",
                        parameterName);
                }
            }
        }

        private static void ValidateOperationDefinitions(
            IEnumerable<OperationDefinition> operationDefinitions,
            Dictionary<Symbol, GenerationSchemaDefinition> generationSchemaDefinitionsBySymbol,
            Dictionary<Symbol, OperationContract> operationContractsByOperationDefinitionSymbol,
            Dictionary<Symbol, int> operationImplementationCountsByOperationDefinitionSymbol,
            string parameterName)
        {
            foreach (OperationDefinition operationDefinition in operationDefinitions)
            {
                ValidateGenerationSchemaReference(
                    operationDefinition.GenerationSchema,
                    generationSchemaDefinitionsBySymbol,
                    parameterName,
                    $"Operation definition '{operationDefinition.Symbol}'");

                if (!operationContractsByOperationDefinitionSymbol.ContainsKey(operationDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Operation definition '{operationDefinition.Symbol}' must have exactly one operation contract.",
                        parameterName);
                }

                if (!operationImplementationCountsByOperationDefinitionSymbol.ContainsKey(operationDefinition.Symbol))
                {
                    throw new ArgumentException(
                        $"Operation definition '{operationDefinition.Symbol}' must have at least one operation implementation definition.",
                        parameterName);
                }
            }
        }

        private static void ValidateStageRouteDefinitions(
            IEnumerable<StageRouteDefinition> stageRouteDefinitions,
            Dictionary<Symbol, StageDefinition> stageDefinitionsBySymbol,
            Dictionary<Symbol, OperationDefinition> operationDefinitionsBySymbol,
            Dictionary<Symbol, StageContract> stageContractsByStageDefinitionSymbol,
            Dictionary<Symbol, OperationContract> operationContractsByOperationDefinitionSymbol,
            string parameterName)
        {
            foreach (StageRouteDefinition stageRouteDefinition in stageRouteDefinitions)
            {
                ValidateStageDefinitionReference(
                    stageRouteDefinition.StageDefinition,
                    stageDefinitionsBySymbol,
                    parameterName,
                    $"Stage route definition '{stageRouteDefinition.Symbol}'");

                foreach (StageRouteStepDefinition stageRouteStepDefinition in stageRouteDefinition.StageRouteStepDefinitions)
                {
                    if (!operationDefinitionsBySymbol.TryGetValue(
                        stageRouteStepDefinition.OperationDefinitionSymbol,
                        out OperationDefinition operationDefinition))
                    {
                        throw new ArgumentException(
                            $"Stage route definition '{stageRouteDefinition.Symbol}' contains route step '{stageRouteStepDefinition.Symbol}' referencing operation definition symbol '{stageRouteStepDefinition.OperationDefinitionSymbol}', but that operation definition is not present in this catalog.",
                            parameterName);
                    }

                    if (!ReferenceEquals(
                        stageRouteDefinition.StageDefinition.GenerationSchema,
                        operationDefinition.GenerationSchema))
                    {
                        throw new ArgumentException(
                            $"Stage route definition '{stageRouteDefinition.Symbol}' contains route step '{stageRouteStepDefinition.Symbol}' referencing operation definition '{operationDefinition.Symbol}' from generation schema '{operationDefinition.GenerationSchema.Symbol}', but the route stage belongs to generation schema '{stageRouteDefinition.StageDefinition.GenerationSchema.Symbol}'.",
                            parameterName);
                    }
                }

                ValidateStageRouteContractCompatibility(
                    stageRouteDefinition,
                    operationDefinitionsBySymbol,
                    stageContractsByStageDefinitionSymbol,
                    operationContractsByOperationDefinitionSymbol,
                    parameterName);
            }
        }

        private static void ValidateStageContracts(
            IEnumerable<StageContract> stageContracts,
            Dictionary<Symbol, StageDefinition> stageDefinitionsBySymbol,
            string parameterName)
        {
            foreach (StageContract stageContract in stageContracts)
            {
                ValidateStageDefinitionReference(
                    stageContract.StageDefinition,
                    stageDefinitionsBySymbol,
                    parameterName,
                    $"Stage contract for stage definition '{stageContract.StageDefinition.Symbol}'");
            }
        }

        private static void ValidateOperationImplementationDefinitions(
            IEnumerable<OperationImplementationDefinition> operationImplementationDefinitions,
            Dictionary<Symbol, OperationDefinition> operationDefinitionsBySymbol,
            string parameterName)
        {
            foreach (OperationImplementationDefinition operationImplementationDefinition in operationImplementationDefinitions)
            {
                ValidateOperationDefinitionReference(
                    operationImplementationDefinition.OperationDefinition,
                    operationDefinitionsBySymbol,
                    parameterName,
                    $"Operation implementation definition '{operationImplementationDefinition.Symbol}'");
            }
        }

        private static void ValidateOperationContracts(
            IEnumerable<OperationContract> operationContracts,
            Dictionary<Symbol, OperationDefinition> operationDefinitionsBySymbol,
            string parameterName)
        {
            foreach (OperationContract operationContract in operationContracts)
            {
                ValidateOperationDefinitionReference(
                    operationContract.OperationDefinition,
                    operationDefinitionsBySymbol,
                    parameterName,
                    $"Operation contract for operation definition '{operationContract.OperationDefinition.Symbol}'");
            }
        }

        private static void ValidateStageRouteContractCompatibility(
            StageRouteDefinition stageRouteDefinition,
            Dictionary<Symbol, OperationDefinition> operationDefinitionsBySymbol,
            Dictionary<Symbol, StageContract> stageContractsByStageDefinitionSymbol,
            Dictionary<Symbol, OperationContract> operationContractsByOperationDefinitionSymbol,
            string parameterName)
        {
            StageContract stageContract = stageContractsByStageDefinitionSymbol[stageRouteDefinition.StageDefinition.Symbol];

            var availableSymbols = new HashSet<Symbol>();

            foreach (Symbol requiredInputSymbol in stageContract.RequiredInputSymbols)
            {
                availableSymbols.Add(requiredInputSymbol);
            }

            foreach (StageRouteStepDefinition stageRouteStepDefinition in stageRouteDefinition.StageRouteStepDefinitions)
            {
                OperationDefinition operationDefinition = operationDefinitionsBySymbol[
                    stageRouteStepDefinition.OperationDefinitionSymbol];

                OperationContract operationContract = operationContractsByOperationDefinitionSymbol[
                    operationDefinition.Symbol];

                foreach (Symbol requiredInputSymbol in operationContract.RequiredInputSymbols)
                {
                    if (!availableSymbols.Contains(requiredInputSymbol))
                    {
                        throw new ArgumentException(
                            $"Stage route definition '{stageRouteDefinition.Symbol}' contains route step '{stageRouteStepDefinition.Symbol}' for operation definition '{operationDefinition.Symbol}', but required input symbol '{requiredInputSymbol}' is not available from the stage contract inputs or previous route steps.",
                            parameterName);
                    }
                }

                foreach (Symbol producedOutputSymbol in operationContract.ProducedOutputSymbols)
                {
                    availableSymbols.Add(producedOutputSymbol);
                }
            }

            foreach (Symbol producedOutputSymbol in stageContract.ProducedOutputSymbols)
            {
                if (!availableSymbols.Contains(producedOutputSymbol))
                {
                    throw new ArgumentException(
                        $"Stage route definition '{stageRouteDefinition.Symbol}' does not produce required stage output symbol '{producedOutputSymbol}' for stage definition '{stageRouteDefinition.StageDefinition.Symbol}'.",
                        parameterName);
                }
            }
        }

        private static void ValidateGenerationSchemaReference(
            GenerationSchemaDefinition generationSchemaDefinition,
            Dictionary<Symbol, GenerationSchemaDefinition> generationSchemaDefinitionsBySymbol,
            string parameterName,
            string ownerDescription)
        {
            if (!generationSchemaDefinitionsBySymbol.TryGetValue(
                    generationSchemaDefinition.Symbol,
                    out GenerationSchemaDefinition catalogGenerationSchemaDefinition)
                || !ReferenceEquals(catalogGenerationSchemaDefinition, generationSchemaDefinition))
            {
                throw new ArgumentException(
                    $"{ownerDescription} references a generation schema definition that is not present in this catalog.",
                    parameterName);
            }
        }

        private static void ValidateStageDefinitionReference(
            StageDefinition stageDefinition,
            Dictionary<Symbol, StageDefinition> stageDefinitionsBySymbol,
            string parameterName,
            string ownerDescription)
        {
            if (!stageDefinitionsBySymbol.TryGetValue(
                    stageDefinition.Symbol,
                    out StageDefinition catalogStageDefinition)
                || !ReferenceEquals(catalogStageDefinition, stageDefinition))
            {
                throw new ArgumentException(
                    $"{ownerDescription} references a stage definition that is not present in this catalog.",
                    parameterName);
            }
        }

        private static void ValidateOperationDefinitionReference(
            OperationDefinition operationDefinition,
            Dictionary<Symbol, OperationDefinition> operationDefinitionsBySymbol,
            string parameterName,
            string ownerDescription)
        {
            if (!operationDefinitionsBySymbol.TryGetValue(
                    operationDefinition.Symbol,
                    out OperationDefinition catalogOperationDefinition)
                || !ReferenceEquals(catalogOperationDefinition, operationDefinition))
            {
                throw new ArgumentException(
                    $"{ownerDescription} references an operation definition that is not present in this catalog.",
                    parameterName);
            }
        }
    }
}