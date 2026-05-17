// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasShapeDomain.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Represent the semantic identity of a resolved field shape.
// - Distinguish equal numeric lengths that belong to different interpretation domains.
// - Preserve zero-valid StableDataId semantics by using explicit source-field presence.
// - Provide stable metadata for contracts, resolved shapes, artifacts, debug-map export, and validators.
//
// Design notes
// - This is schema/compilation metadata, not runtime storage.
// - This type does not own memory.
// - This type does not describe byte format.
// - This type does not describe allocator ownership.
// - Jobs should receive resolved numeric shape/layout data, not branch on this type in hot loops.
// - default(AtlasShapeDomain) is a valid value object, but not a concrete domain.
// - StableDataId default/zero is valid and must not represent "missing" by itself.
// - Source-field presence is represented by HasSourceField.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Describes the semantic domain used to interpret a field's resolved length and capacity.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasShapeDomain :
        IEquatable<AtlasShapeDomain>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

        private const byte NoSourceField = 0;
        private const byte HasSourceFieldValue = 1;

        /// <summary>
        /// Default non-concrete domain payload.
        /// </summary>
        public static readonly AtlasShapeDomain None = default;

        /// <summary>
        /// Gets the domain family.
        /// </summary>
        public readonly AtlasShapeDomainKind Kind;

        /// <summary>
        /// Gets the stable diagnostic domain name.
        /// </summary>
        /// <remarks>
        /// This name is ABI/debug metadata. It is not field identity and must not replace
        /// <see cref="StableDataId"/>.
        /// </remarks>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Gets the optional source field id for field-derived domains.
        /// </summary>
        /// <remarks>
        /// Zero/default is a valid stable id. Source presence is represented by
        /// <see cref="HasSourceField"/>.
        /// </remarks>
        public readonly StableDataId SourceFieldId;

        private readonly byte _sourceFieldPresence;

        private AtlasShapeDomain(
            AtlasShapeDomainKind kind,
            FixedString64Bytes name,
            StableDataId sourceFieldId,
            byte sourceFieldPresence)
        {
            Kind = kind;
            Name = name;
            SourceFieldId = sourceFieldId;
            _sourceFieldPresence = sourceFieldPresence;
        }

        /// <summary>
        /// Gets whether this value describes a concrete shape domain.
        /// </summary>
        public bool IsConcrete
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind != AtlasShapeDomainKind.None &&
                   !Name.IsEmpty &&
                   IsSourceFieldPresenceValid();
        }

        /// <summary>
        /// Gets whether this value does not describe a concrete shape domain.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsConcrete;
        }

        /// <summary>
        /// Gets whether this domain is derived from another field.
        /// </summary>
        public bool HasSourceField
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sourceFieldPresence == HasSourceFieldValue;
        }

        /// <summary>
        /// Gets whether this domain represents dense grid data.
        /// </summary>
        public bool IsDenseGrid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasShapeDomainKind.CellGrid2D ||
                   Kind == AtlasShapeDomainKind.VertexGrid2D ||
                   Kind == AtlasShapeDomainKind.VoxelGrid3D;
        }

        /// <summary>
        /// Gets whether this domain represents topology row data.
        /// </summary>
        public bool IsTopologyRows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasShapeDomainKind.ComponentSet ||
                   Kind == AtlasShapeDomainKind.GraphNodeSet ||
                   Kind == AtlasShapeDomainKind.GraphEdgeSet;
        }

        /// <summary>
        /// Gets whether this domain represents variable-length or compacted payload data.
        /// </summary>
        public bool IsVariablePayload
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasShapeDomainKind.RecordStream ||
                   Kind == AtlasShapeDomainKind.PrefixSumPayload;
        }

        /// <summary>
        /// Gets whether this domain represents externally owned rows.
        /// </summary>
        public bool IsExternal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasShapeDomainKind.External;
        }

        /// <summary>
        /// Creates a scalar shape domain.
        /// </summary>
        public static AtlasShapeDomain Scalar(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.Scalar,
                name.IsEmpty ? new FixedString64Bytes("scalar") : name);
        }

        /// <summary>
        /// Creates a fixed-vector shape domain.
        /// </summary>
        public static AtlasShapeDomain FixedVector(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.FixedVector,
                name);
        }

        /// <summary>
        /// Creates a linear-row shape domain.
        /// </summary>
        public static AtlasShapeDomain LinearRows(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.LinearRows,
                name);
        }

        /// <summary>
        /// Creates a dense 2D cell-grid shape domain.
        /// </summary>
        public static AtlasShapeDomain CellGrid2D(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.CellGrid2D,
                name.IsEmpty ? new FixedString64Bytes("cell-grid-2d") : name);
        }

        /// <summary>
        /// Creates a dense 2D vertex-grid shape domain.
        /// </summary>
        public static AtlasShapeDomain VertexGrid2D(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.VertexGrid2D,
                name.IsEmpty ? new FixedString64Bytes("vertex-grid-2d") : name);
        }

        /// <summary>
        /// Creates a dense 3D voxel-grid shape domain.
        /// </summary>
        public static AtlasShapeDomain VoxelGrid3D(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.VoxelGrid3D,
                name.IsEmpty ? new FixedString64Bytes("voxel-grid-3d") : name);
        }

        /// <summary>
        /// Creates a chunk-set shape domain.
        /// </summary>
        public static AtlasShapeDomain ChunkSet(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.ChunkSet,
                name.IsEmpty ? new FixedString64Bytes("chunk-set") : name);
        }

        /// <summary>
        /// Creates an entity-set shape domain.
        /// </summary>
        public static AtlasShapeDomain EntitySet(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.EntitySet,
                name);
        }

        /// <summary>
        /// Creates a connected-component-set shape domain.
        /// </summary>
        public static AtlasShapeDomain ComponentSet(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.ComponentSet,
                name.IsEmpty ? new FixedString64Bytes("component-set") : name);
        }

        /// <summary>
        /// Creates a graph-node-set shape domain.
        /// </summary>
        public static AtlasShapeDomain GraphNodeSet(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.GraphNodeSet,
                name.IsEmpty ? new FixedString64Bytes("graph-node-set") : name);
        }

        /// <summary>
        /// Creates a graph-edge-set shape domain.
        /// </summary>
        public static AtlasShapeDomain GraphEdgeSet(FixedString64Bytes name = default)
        {
            return Create(
                AtlasShapeDomainKind.GraphEdgeSet,
                name.IsEmpty ? new FixedString64Bytes("graph-edge-set") : name);
        }

        /// <summary>
        /// Creates a record-stream shape domain.
        /// </summary>
        public static AtlasShapeDomain RecordStream(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.RecordStream,
                name);
        }

        /// <summary>
        /// Creates a prefix-sum payload domain derived from another field.
        /// </summary>
        public static AtlasShapeDomain PrefixSumPayload(
            StableDataId sourceFieldId,
            FixedString64Bytes name)
        {
            return CreateDerived(
                AtlasShapeDomainKind.PrefixSumPayload,
                name,
                sourceFieldId);
        }

        /// <summary>
        /// Creates a sparse-index-set shape domain.
        /// </summary>
        public static AtlasShapeDomain SparseIndexSet(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.SparseIndexSet,
                name);
        }

        /// <summary>
        /// Creates an external shape domain.
        /// </summary>
        public static AtlasShapeDomain External(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.External,
                name);
        }

        /// <summary>
        /// Creates a concrete shape domain from explicit metadata.
        /// </summary>
        public static AtlasShapeDomain Create(
            AtlasShapeDomainKind kind,
            FixedString64Bytes name)
        {
            var domain = new AtlasShapeDomain(
                kind,
                name,
                default,
                NoSourceField);

            domain.ValidateOrThrow(nameof(domain));
            return domain;
        }

        /// <summary>
        /// Creates a concrete field-derived shape domain from explicit metadata.
        /// </summary>
        public static AtlasShapeDomain CreateDerived(
            AtlasShapeDomainKind kind,
            FixedString64Bytes name,
            StableDataId sourceFieldId)
        {
            var domain = new AtlasShapeDomain(
                kind,
                name,
                sourceFieldId,
                HasSourceFieldValue);

            domain.ValidateOrThrow(nameof(domain));
            return domain;
        }

        /// <summary>
        /// Throws when this value is not a concrete shape domain.
        /// </summary>
        public void ValidateOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasShapeDomain);

            if (Kind == AtlasShapeDomainKind.None)
            {
                throw new ArgumentException(
                    "Atlas shape domain must declare a concrete domain kind.",
                    name);
            }

            if (Name.IsEmpty)
            {
                throw new ArgumentException(
                    "Atlas shape domain must declare a non-empty diagnostic name.",
                    name);
            }

            if (!IsSourceFieldPresenceValid())
            {
                throw new ArgumentException(
                    $"Atlas shape domain '{Name}' has invalid source-field presence state '{_sourceFieldPresence}'.",
                    name);
            }

            SourceFieldId.ValidateOrThrow($"{name}.{nameof(SourceFieldId)}");

            if (Kind == AtlasShapeDomainKind.PrefixSumPayload && !HasSourceField)
            {
                throw new ArgumentException(
                    $"Atlas shape domain '{Name}' is a prefix-sum payload domain but does not declare a source field.",
                    name);
            }

            if (Kind != AtlasShapeDomainKind.PrefixSumPayload &&
                Kind != AtlasShapeDomainKind.SparseIndexSet &&
                HasSourceField)
            {
                throw new ArgumentException(
                    $"Atlas shape domain '{Name}' declares a source field, but domain kind '{Kind}' is not source-field-derived.",
                    name);
            }
        }

        /// <summary>
        /// Determines whether this domain has the same domain identity as another domain.
        /// </summary>
        /// <remarks>
        /// This intentionally excludes source field identity. Use full equality when the source field
        /// must also match.
        /// </remarks>
        public bool HasSameDomainIdentityAs(AtlasShapeDomain other)
        {
            return Kind == other.Kind &&
                   Name.Equals(other.Name);
        }

        /// <summary>
        /// Determines whether this domain equals another domain.
        /// </summary>
        public bool Equals(AtlasShapeDomain other)
        {
            return Kind == other.Kind &&
                   Name.Equals(other.Name) &&
                   SourceFieldId == other.SourceFieldId &&
                   _sourceFieldPresence == other._sourceFieldPresence;
        }

        /// <summary>
        /// Determines whether this domain equals another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasShapeDomain other &&
                   Equals(other);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ (int)Kind;
                hash = (hash * HashMultiplier) ^ Name.GetHashCode();
                hash = (hash * HashMultiplier) ^ SourceFieldId.GetHashCode();
                hash = (hash * HashMultiplier) ^ _sourceFieldPresence;
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this shape domain.
        /// </summary>
        public override string ToString()
        {
            if (Kind == AtlasShapeDomainKind.None && Name.IsEmpty)
            {
                return "AtlasShapeDomain(None)";
            }

            return HasSourceField
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "AtlasShapeDomain(Kind={0}, Name={1}, SourceFieldId={2})",
                    Kind,
                    Name,
                    SourceFieldId)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "AtlasShapeDomain(Kind={0}, Name={1})",
                    Kind,
                    Name);
        }

        /// <summary>
        /// Determines whether two shape domains are equal.
        /// </summary>
        public static bool operator ==(AtlasShapeDomain left, AtlasShapeDomain right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two shape domains are not equal.
        /// </summary>
        public static bool operator !=(AtlasShapeDomain left, AtlasShapeDomain right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSourceFieldPresenceValid()
        {
            return _sourceFieldPresence == NoSourceField ||
                   _sourceFieldPresence == HasSourceFieldValue;
        }
    }
}