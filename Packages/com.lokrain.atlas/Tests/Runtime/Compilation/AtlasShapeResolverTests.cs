// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasShapeResolver.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Resolve contract-table LengthShape metadata into concrete AtlasResolvedShape rows.
// - Produce one resolved shape per contract-table slot.
// - Resolve only shapes that can be computed from the contract table itself.
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
// - This type does not use field identity lookup in jobs.
// - Field-relative shapes are resolved recursively, so source order in the contract table does not matter.
// - Cyclic field-relative shape dependencies are rejected deterministically.
// - Capacity scaling uses integer ratios only; no float shape metadata participates in resolution.

using System;
using System.Globalization;
using Lokrain.Atlas.Contracts;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Resolves symbolic Atlas contract-table length shapes into concrete runtime shape metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasShapeResolver"/> is the compiler pass between validated contract metadata
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
        /// Resolves all contract-table shapes using the contract table's diagnostic name.
        /// </summary>
        /// <param name="contracts">Contract table whose field shapes should be resolved.</param>
        /// <returns>A validated resolved shape set in canonical contract-table slot order.</returns>
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
        /// Resolves all contract-table shapes using an explicit diagnostic shape-set name.
        /// </summary>
        /// <param name="name">Diagnostic name for the resolved shape set.</param>
        /// <param name="contracts">Contract table whose field shapes should be resolved.</param>
        /// <returns>A validated resolved shape set in canonical contract-table slot order.</returns>
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
        /// Resolves shapes for the contract table referenced by a compiled plan.
        /// </summary>
        /// <param name="plan">Compiled plan whose contract table should be resolved.</param>
        /// <returns>A validated resolved shape set in canonical contract-table slot order.</returns>
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
                    "Compiled plan does not reference a contract table.",
                    nameof(plan));
            }

            return Resolve(
                plan.DebugName,
                plan.Contracts);
        }

        /// <summary>
        /// Resolves all contract-table shapes into a managed array in canonical slot order.
        /// </summary>
        /// <param name="contracts">Contract table whose field shapes should be resolved.</param>
        /// <returns>A new array containing one resolved shape per contract-table slot.</returns>
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
        /// Returns whether the supplied shape kind can be resolved from a contract table alone.
        /// </summary>
        /// <param name="kind">Shape kind to inspect.</param>
        /// <returns><c>true</c> for contract-table-only shape rules; otherwise, <c>false</c>.</returns>
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
            contract.ValidateTableReadyOrThrow(nameof(contract));

            var shape = contract.LengthShape;
            shape.ValidateOrThrow(nameof(contract));

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

                case LengthShapeKind.None:
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

            var capacity = ResolveCapacityFromField(
                source.Capacity,
                contract.LengthShape,
                contract.GetDiagnosticName(),
                source.GetDiagnosticName());

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
                    $"Contract '{target.GetDiagnosticName()}' depends on source field id '{sourceId}', but that field is not present in contract table '{GetContractTableDiagnosticName(contracts)}'.",
                    nameof(target));
            }

            return ResolveIndex(
                contracts,
                sourceSlot.Index,
                shapes,
                states);
        }

        private static int ResolveCapacityFromField(
            int sourceCapacity,
            LengthShape shape,
            string targetDiagnosticName,
            string sourceDiagnosticName)
        {
            if (sourceCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceCapacity),
                    sourceCapacity,
                    "Source capacity must be greater than or equal to zero.");
            }

            if (shape.Kind != LengthShapeKind.CapacityFromField)
            {
                throw new ArgumentException(
                    $"Length shape '{shape}' is not a capacity-from-field shape.",
                    nameof(shape));
            }

            shape.ValidateOrThrow(nameof(shape));

            var numerator = shape.CapacityMultiplierNumerator;
            var denominator = shape.CapacityMultiplierDenominator;
            var padding = shape.CapacityPadding;

            var product = checked((long)sourceCapacity * numerator);
            var scaled = DivideRoundUp(
                product,
                denominator);

            var resolvedCapacity = checked(scaled + padding);

            if (resolvedCapacity > int.MaxValue)
            {
                throw new OverflowException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' derived capacity '{1}' from source field '{2}', exceeding Int32.MaxValue.",
                        targetDiagnosticName,
                        resolvedCapacity,
                        sourceDiagnosticName));
            }

            return (int)resolvedCapacity;
        }

        private static long DivideRoundUp(
            long value,
            int divisor)
        {
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Value must be greater than or equal to zero.");
            }

            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(divisor),
                    divisor,
                    "Divisor must be greater than zero.");
            }

            return checked((value + divisor - 1L) / divisor);
        }

        private static string CreateUnsupportedShapeMessage(
            AtlasContract contract)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Contract '{0}' declares length shape '{1}', which cannot be resolved from a contract table alone. " +
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

        private static string GetContractTableDiagnosticName(
            AtlasContractTable contracts)
        {
            if (contracts != null && !contracts.Name.IsEmpty)
            {
                return contracts.Name.ToString();
            }

            return "<unnamed-contract-table>";
        }
    }
}