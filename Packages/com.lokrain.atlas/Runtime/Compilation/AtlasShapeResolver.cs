// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasShapeResolver.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Resolve contract-table LengthShape metadata into concrete AtlasResolvedShape rows.
// - Produce one resolved shape per Contract-table slot.
// - Preserve Contract ShapeDomain metadata through resolved compiler metadata.
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
// - This type does not create artifacts.
// - Field-relative shapes are resolved recursively, so source order in the Contract table does not matter.
// - Cyclic field-relative shape dependencies are rejected deterministically.
// - CapacityFromField preserves integer-rational capacity derivation.
// - ShapeDomain is copied from AtlasContract into AtlasResolvedShape.
// - LengthShape decides resolved length/capacity.
// - ShapeDomain decides what resolved length/capacity means.
// - Capacity may exceed logical length in later layout products, but this resolver only emits
//   capacity slack when the declared LengthShape explicitly resolves capacity slack.

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
    /// <see cref="AtlasShapeResolver"/> is the Contract-table-only shape resolver. It handles
    /// length rules that are fully determined by the validated <see cref="AtlasContractTable"/>.
    /// Query, chunk, prefix-sum, and external shapes are intentionally rejected because they require
    /// explicit resolver context.
    /// </para>
    ///
    /// <para>
    /// This resolver produces compiler metadata only. It does not allocate memory, define storage
    /// block layout, schedule jobs, expose native containers, or build artifacts.
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
        public static AtlasResolvedShapeSet Resolve(AtlasContractTable contracts)
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
        public static AtlasResolvedShapeSet Resolve(AtlasCompiledPlan plan)
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
        public static AtlasResolvedShape[] ResolveToArray(AtlasContractTable contracts)
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
        public static bool CanResolveFromContractTableOnly(LengthShapeKind kind)
        {
            return kind switch
            {
                LengthShapeKind.Scalar or LengthShapeKind.Fixed or LengthShapeKind.MatchFieldLength or LengthShapeKind.CapacityFromField => true,
                _ => false,
            };
        }

        /// <summary>
        /// Returns whether the supplied Contract can be resolved from a Contract table alone.
        /// </summary>
        public static bool CanResolveFromContractTableOnly(AtlasContract contract)
        {
            return contract.IsTableReady &&
                   CanResolveFromContractTableOnly(contract.LengthShape.Kind);
        }

        /// <summary>
        /// Validates that all shapes in the supplied Contract table can be resolved by this resolver.
        /// </summary>
        public static void ValidateResolvableFromContractTableOnlyOrThrow(AtlasContractTable contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            for (var i = 0; i < contracts.Count; i++)
            {
                var contract = contracts[i];

                contract.ValidateTableReadyOrThrow(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "contracts[{0}]",
                        i));

                if (!CanResolveFromContractTableOnly(contract.LengthShape.Kind))
                {
                    throw new NotSupportedException(
                        CreateUnsupportedShapeMessage(contract));
                }
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
                        CreateCycleMessage(
                            contracts,
                            index));
            }

            states[index] = Visiting;

            var contract = contracts[index];

            contract.ValidateTableReadyOrThrow(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "contracts[{0}]",
                    index));

            var resolved = ResolveContract(
                contracts,
                contract,
                shapes,
                states);

            resolved.ValidateOrThrow(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "shapes[{0}]",
                    index));

            if (resolved.Slot.Index != index)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Resolved shape '{0}' has slot '{1}', but resolver index is '{2}'.",
                        resolved.GetDiagnosticName(),
                        resolved.Slot,
                        index));
            }

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
                    ValidateScalarDomainOrThrow(contract);

                    return AtlasResolvedShape.Create(
                        contract,
                        length: 1,
                        capacity: 1);

                case LengthShapeKind.Fixed:
                    ValidateFixedDomainOrThrow(contract);

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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Contract '{0}' declares unsupported length-shape kind '{1}'.",
                            contract.GetDiagnosticName(),
                            shape.Kind),
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

            ValidateFieldRelativeDomainOrThrow(
                contract,
                source);

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

            ValidateFieldRelativeDomainOrThrow(
                contract,
                source);

            var capacity = ResolveDerivedCapacityValue(
                source.Capacity,
                contract.LengthShape,
                contract.GetDiagnosticName(),
                source.GetDiagnosticName());

            return AtlasResolvedShape.Create(
                contract,
                length: capacity,
                capacity: capacity);
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
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' depends on source field id '{1}', but that field is not present in Contract table '{2}'.",
                        target.GetDiagnosticName(),
                        sourceId,
                        GetContractTableDiagnosticName(contracts)),
                    nameof(target));
            }

            if (sourceSlot.Index < 0 ||
                sourceSlot.Index >= contracts.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceSlot),
                    sourceSlot,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' resolved source slot '{1}' outside Contract table '{2}'.",
                        target.GetDiagnosticName(),
                        sourceSlot,
                        GetContractTableDiagnosticName(contracts)));
            }

            return ResolveIndex(
                contracts,
                sourceSlot.Index,
                shapes,
                states);
        }

        private static int ResolveDerivedCapacityValue(
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
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Length shape '{0}' is not a capacity-from-field shape.",
                        shape),
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

        private static void ValidateScalarDomainOrThrow(AtlasContract contract)
        {
            if (contract.ShapeDomain.Kind == AtlasShapeDomainKind.Scalar ||
                contract.ShapeDomain.Kind == AtlasShapeDomainKind.FixedVector)
            {
                return;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Contract '{0}' declares scalar length shape with incompatible shape domain '{1}'.",
                    contract.GetDiagnosticName(),
                    contract.ShapeDomain.Kind),
                nameof(contract));
        }

        private static void ValidateFixedDomainOrThrow(AtlasContract contract)
        {
            if (contract.ShapeDomain.Kind != AtlasShapeDomainKind.External &&
                contract.ShapeDomain.Kind != AtlasShapeDomainKind.PrefixSumPayload)
            {
                return;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Contract '{0}' declares fixed length shape with resolver-input-dependent shape domain '{1}'.",
                    contract.GetDiagnosticName(),
                    contract.ShapeDomain.Kind),
                nameof(contract));
        }

        private static void ValidateFieldRelativeDomainOrThrow(
            AtlasContract target,
            AtlasResolvedShape source)
        {
            if (target.ShapeDomain.Kind == AtlasShapeDomainKind.External)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' declares field-relative length shape with external shape domain.",
                        target.GetDiagnosticName()),
                    nameof(target));
            }

            if (target.ShapeDomain.Kind == AtlasShapeDomainKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' declares field-relative length shape '{1}', but prefix-sum payload domains require explicit prefix-sum resolver context.",
                        target.GetDiagnosticName(),
                        target.LengthShape.Kind),
                    nameof(target));
            }

            if (target.ShapeDomain.HasSourceField &&
                target.ShapeDomain.SourceFieldId != source.StableId)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Contract '{0}' declares shape-domain source field '{1}', but resolved length source is '{2}'.",
                        target.GetDiagnosticName(),
                        target.ShapeDomain.SourceFieldId,
                        source.StableId),
                    nameof(target));
            }
        }

        private static string CreateUnsupportedShapeMessage(AtlasContract contract)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Contract '{0}' declares length shape '{1}' with shape domain '{2}', which cannot be resolved from a Contract table alone. Provide an explicit resolver context before resolving QueryCount, ChunkCount, PrefixSumPayload, or External shapes.",
                contract.GetDiagnosticName(),
                contract.LengthShape,
                contract.ShapeDomain);
        }

        private static string CreateCycleMessage(
            AtlasContractTable contracts,
            int index)
        {
            var contract = contracts[index];

            return string.Format(
                CultureInfo.InvariantCulture,
                "Contract table '{0}' contains a cyclic field-relative length-shape dependency involving Contract '{1}' at slot '{2}'.",
                GetContractTableDiagnosticName(contracts),
                contract.GetDiagnosticName(),
                contract.Slot);
        }

        private static string GetContractTableDiagnosticName(AtlasContractTable contracts)
        {
            if (contracts != null &&
                !contracts.Name.IsEmpty)
            {
                return contracts.Name.ToString();
            }

            return "<unnamed-contract-table>";
        }
    }
}