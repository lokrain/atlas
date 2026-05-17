// Packages/com.lokrain.atlas/Runtime/Operations/IAtlasOperationDefinitionProvider.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Define the minimal lookup contract for resolving durable operation identities to authored operation definitions.
// - Keep operation-definition lookup separate from operation occurrence sequences.
// - Avoid using AtlasOperationSet as a unique operation catalog.
// - Support pipeline/schema builders that should depend on operation definitions without owning them.
//
// Design notes
// - OperationId is durable identity and every bit pattern is valid.
// - Missing lookup state is represented by a bool-returning API.
// - This interface does not imply executability.
// - Executor registration belongs to Lokrain.Atlas.Executors.
// - Operation occurrence ordering belongs to AtlasOperationSet, AtlasStageDefinition, and AtlasPipelineDefinition.

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Resolves durable Atlas operation identities to authored operation definitions.
    /// </summary>
    public interface IAtlasOperationDefinitionProvider
    {
        /// <summary>
        /// Attempts to resolve an operation definition by durable operation identity.
        /// </summary>
        /// <param name="operationId">Stable operation identity to resolve.</param>
        /// <param name="operation">Resolved operation definition when present; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> when the operation definition exists; otherwise, <c>false</c>.</returns>
        bool TryGetOperation(
            AtlasOperationId operationId,
            out AtlasOperationDefinition operation);
    }
}
