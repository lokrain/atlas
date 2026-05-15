// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasShapeResolver.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Resolve Contract-table LengthShape metadata into concrete AtlasResolvedShape rows.
// - Produce one resolved shape per Contract-table slot.
// - Resolve only shapes that can be computed from the Contract table itself.
// - Reject resolver-input-dependent shapes until explicit resolver context exists.
//
// Supported by this resolver:
// - Scalar
// - Fixed
// - MatchFieldLength
// - CapacityFromField
//
// Explicitly rejected here:
// - QueryCount
// - ChunkCount
// - PrefixSumPayload
// - External
//
// Design notes
// - This is a managed compiler pass.
// - This type does not allocate workspace memory.
// - This type does not calculate byte offsets.
// - This type does not schedule jobs.
// - This type does not use FieldId lookup in jobs.
// - Field-relative shapes are resolved recursively, so source order in the Contract table does not matter.
// - Cyclic field-relative shape dependencies are rejected deterministically.

using System;
using System.Globalization;
using Lokrain.Atlas.Contracts;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Resolves symbolic Atlas Contract-table length shapes into concrete runtime shape metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasShapeResolver"/> is the compiler pass between validated Contract metadata
    /// and later memory-layout construction. It turns <see cref="LengthShape"/> rules into concrete
    /// logical lengths, capacities, and byte sizes represented by <see cref="AtlasResolvedShape"/>.
    /// </para>
    ///
    /// <para>
    /// This resolver intentionally supports only shape rules that are computable from the
    /// <see cref="AtlasContractTable"/> itself. Query counts, chunk counts, prefix-sum payloads,
    /// and externally provided lengths need explicit resolver inputs and are therefore rejected
    /// here instead of guessed.
    /// </para>
    ///
    /// <para>
    /// Shape resolution is recursive for field-relative dependencies. Contract-table order is not
    /// required to place source fields before dependent fields. Cycles are rejected with a stable
    /// diagnostic exception.
    /// </para>
    /// </remarks>
    public static class AtlasShapeResolver
    {
        private const byte Unvisited = 0;
        private const byte Visiting = 1;
        private const byte Resolved = 2;

        /// <summary>
        /// Resolves all Contract-table shapes using the Contract table's diagnostic name.
        /// </summary>
        /// <param name="contracts">Contract table whose field shapes should be resolved.</param>
        /// <returns>A validated resolved shape set in canonical Contract-table slot order.</returns>
        public static AtlasResolvedShapeSet Resolve(
            AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            return Resolve(
                contracts.Name,
                contracts);
        }

        /// <summary>
        /// Resolves all Contract-table shapes using an explicit diagnostic shape-set name.
        /// </summary>
        /// <param name="name">Diagnostic name for the resolved shape set.</param>
        /// <param name="contracts">Contract table whose field shapes should be resolved.</param>
        /// <returns>A validated resolved shape set in canonical Contract-table slot order.</returns>
        public static AtlasResolvedShapeSet Resolve(
            FixedString64Bytes name,
            AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var shapes = ResolveToArray(contracts);

            return AtlasResolvedShapeSet.Create(
                name,
                contracts,
                shapes);
        }

        /// <summary>
        /// Resolves shapes for the Contract table referenced by a compiled plan.
        /// </summary>
        /// <param name="plan">Compiled plan whose Contract table should be resolved.</param>
        /// <returns>A validated resolved shape set in canonical Contract-table slot order.</returns>
        public static AtlasResolvedShapeSet Resolve(
            AtlasCompiledPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (plan.Contracts == null)
            {
                throw new ArgumentException(
                    "Compiled plan does not reference a Contract table.",
                    nameof(plan));
            }

            return Resolve(
                plan.DebugName,
                plan.Contracts);
        }

        /// <summary>
        /// Resolves all Contract-table shapes into a managed array in canonical slot order.
        /// </summary>
        /// <param name="contracts">Contract table whose field shapes should be resolved.</param>
        /// <returns>A new array containing one resolved shape per Contract-table slot.</returns>
        public static AtlasResolvedShape[] ResolveToArray(
            AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var shapes = new AtlasResolvedShape[contracts.Count];
            var states = new byte[contracts.Count];

            for (var i = 0; i < contracts.Count; i++)
            {
                ResolveIndex(
                    contracts,
                    i,
                    shapes,
                    states);
            }

            return shapes;
        }

        /// <summary>
        /// Returns whether the supplied shape kind can be resolved from a Contract table alone.
        /// </summary>
        /// <param name="kind">Shape kind to inspect.</param>
        /// <returns><c>true</c> for Contract-table-only shape rules; otherwise, <c>false</c>.</returns>
        public static bool CanResolveFromContractTableOnly(
            LengthShapeKind kind)
        {
            switch (kind)
            {
                case LengthShapeKind.Scalar:
                case LengthShapeKind.Fixed:
                case LengthShapeKind.MatchFieldLength:
                case LengthShapeKind.CapacityFromField:
                    return true;

                default:
                    return false;
            }
        }

        private static AtlasResolvedShape ResolveIndex(
            AtlasContractTable contracts,
            int index,
            AtlasResolvedShape[] shapes,
            byte[] states)
        {
            switch (states[index])
            {
                case Resolved:
                    return shapes[index];

                case Visiting:
                    throw new InvalidOperationException(
                        CreateCycleMessage(contracts, index));
            }

            states[index] = Visiting;

            var contract = contracts[index];
            contract.ValidateTableReadyOrThrow($"contracts[{index}]");

            var resolved = ResolveContract(
                contracts,
                contract,
                shapes,
                states);

            resolved.ValidateOrThrow($"shapes[{index}]");

            shapes[index] = resolved;
            states[index] = Resolved;

            return resolved;
        }

        private static AtlasResolvedShape ResolveContract(
            AtlasContractTable contracts,
            AtlasContract contract,
            AtlasResolvedShape[] shapes,
            byte[] states)
        {
            var shape = contract.LengthShape;

            switch (shape.Kind)
            {
                case LengthShapeKind.Scalar:
                    return AtlasResolvedShape.Create(
                        contract,
                        length: 1,
                        capacity: 1);

                case LengthShapeKind.Fixed:
                    return AtlasResolvedShape.Create(
                        contract,
                        length: shape.FixedLength,
                        capacity: shape.FixedLength);

                case LengthShapeKind.MatchFieldLength:
                    return ResolveMatchFieldLength(
                        contracts,
                        contract,
                        shapes,
                        states);

                case LengthShapeKind.CapacityFromField:
                    return ResolveCapacityFromField(
                        contracts,
                        contract,
                        shapes,
                        states);

                case LengthShapeKind.QueryCount:
                case LengthShapeKind.ChunkCount:
                case LengthShapeKind.PrefixSumPayload:
                case LengthShapeKind.External:
                    throw new NotSupportedException(
                        CreateUnsupportedShapeMessage(contract));

                default:
                    throw new ArgumentException(
                        $"Contract '{contract.GetDiagnosticName()}' declares unsupported length-shape kind '{shape.Kind}'.",
                        nameof(contract));
            }
        }

        private static AtlasResolvedShape ResolveMatchFieldLength(
            AtlasContractTable contracts,
            AtlasContract contract,
            AtlasResolvedShape[] shapes,
            byte[] states)
        {
            var source = ResolveSourceShape(
                contracts,
                contract,
                shapes,
                states);

            return AtlasResolvedShape.Create(
                contract,
                length: source.Length,
                capacity: source.Length);
        }

        private static AtlasResolvedShape ResolveCapacityFromField(
            AtlasContractTable contracts,
            AtlasContract contract,
            AtlasResolvedShape[] shapes,
            byte[] states)
        {
            var source = ResolveSourceShape(
                contracts,
                contract,
                shapes,
                states);

            var capacity = ComputeDerivedCapacity(
                contract,
                source);

            var length = contract.StorageFormat.RequiresFixedLength
                ? capacity
                : 0;

            return AtlasResolvedShape.Create(
                contract,
                length,
                capacity);
        }

        private static AtlasResolvedShape ResolveSourceShape(
            AtlasContractTable contracts,
            AtlasContract target,
            AtlasResolvedShape[] shapes,
            byte[] states)
        {
            var sourceId = target.LengthShape.SourceFieldId;

            if (!contracts.TryGetSlot(sourceId, out var sourceSlot))
            {
                throw new ArgumentException(
                    $"Contract '{target.GetDiagnosticName()}' depends on source field id '{sourceId}', but that field is not present in Contract table '{GetContractTableDiagnosticName(contracts)}'.",
                    nameof(target));
            }

            return ResolveIndex(
                contracts,
                sourceSlot.Index,
                shapes,
                states);
        }

        private static int ComputeDerivedCapacity(
            AtlasContract target,
            AtlasResolvedShape source)
        {
            var shape = target.LengthShape;
            var multiplier = shape.CapacityMultiplier;

            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
            {
                throw new ArgumentException(
                    $"Contract '{target.GetDiagnosticName()}' has non-finite capacity multiplier '{multiplier}'.",
                    nameof(target));
            }

            if (multiplier < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(target),
                    multiplier,
                    $"Contract '{target.GetDiagnosticName()}' has negative capacity multiplier.");
            }

            if (shape.CapacityPadding < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(target),
                    shape.CapacityPadding,
                    $"Contract '{target.GetDiagnosticName()}' has negative capacity padding.");
            }

            var sourceCapacity = source.Capacity;
            var scaled = Math.Ceiling(sourceCapacity * (double)multiplier);
            var derived = scaled + shape.CapacityPadding;

            if (derived > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' derived capacity '{1}' from source field '{2}', exceeding Int32.MaxValue.",
                        target.GetDiagnosticName(),
                        derived,
                        source.GetDiagnosticName()));
            }

            if (derived < 0.0d)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' derived negative capacity '{1}' from source field '{2}'.",
                        target.GetDiagnosticName(),
                        derived,
                        source.GetDiagnosticName()));
            }

            return checked((int)derived);
        }

        private static string CreateUnsupportedShapeMessage(
            AtlasContract contract)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Contract '{0}' declares length shape '{1}', which cannot be resolved from a Contract table alone. " +
                "Provide an explicit resolver context before resolving QueryCount, ChunkCount, PrefixSumPayload, or External shapes.",
                contract.GetDiagnosticName(),
                contract.LengthShape);
        }

        private static string CreateCycleMessage(
            AtlasContractTable contracts,
            int index)
        {
            var contract = contracts[index];

            return string.Format(
                CultureInfo.InvariantCulture,
                "Contract table '{0}' contains a cyclic field-relative length-shape dependency involving contract '{1}' at slot '{2}'.",
                GetContractTableDiagnosticName(contracts),
                contract.GetDiagnosticName(),
                contract.Slot);
        }

        private static string GetContractTableDiagnosticName(AtlasContractTable contracts)
        {
            if (contracts != null && !contracts.Name.IsEmpty)
            {
                return contracts.Name.ToString();
            }

            return "<unnamed-contract-table>";
        }
    }
}