// Packages/com.lokrain.atlas/Runtime/Contracts/IAtlasContractProvider.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Define the minimal lookup contract for resolving durable field identities to authored field contracts.
// - Keep unique contract lookup separate from contract-table slot assignment.
// - Avoid using AtlasContractTable as a unique field-contract catalog.
// - Support pipeline/schema builders that should depend on field contracts without owning catalog storage.
//
// Design notes
// - StableDataId is durable identity and every bit pattern is valid.
// - Missing lookup state is represented by a bool-returning API.
// - This interface does not allocate workspace memory.
// - Contract-table occurrence ordering and slot assignment belong to AtlasContractTable.
// - Catalog implementations must not expose table-local slot state as durable metadata.

using Lokrain.Atlas.Core;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Resolves durable Atlas field identities to authored field contracts.
    /// </summary>
    public interface IAtlasContractProvider
    {
        /// <summary>
        /// Attempts to resolve a contract by durable field identity.
        /// </summary>
        /// <param name="stableId">Stable field identity to resolve. Zero/default is valid.</param>
        /// <param name="contract">Resolved contract when present; otherwise, the default contract payload.</param>
        /// <returns><c>true</c> when the contract was found; otherwise, <c>false</c>.</returns>
        bool TryGetContract(
            StableDataId stableId,
            out AtlasContract contract);
    }
}
