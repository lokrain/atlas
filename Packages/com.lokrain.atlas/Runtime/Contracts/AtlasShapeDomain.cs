// Packages/com.lokrain.atlas/Runtime/Contracts/AtlasShapeDomain.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Contracts
//
// Purpose
// - Represent semantic shape-domain identity for field contracts and resolved shapes.
// - Distinguish equal numeric lengths that belong to different interpretation domains.
// - Preserve zero-valid StableDataId semantics by using domain kind/name as presence metadata.
//
// Design notes
// - This is schema/compilation metadata, not runtime storage.
// - Jobs should not branch on this type in hot loops.
// - Validators, artifact writers, debug exporters, and executor policy use this type.
// - default(AtlasShapeDomain) is a valid value object but not a concrete domain.
// - The all-zero StableDataId remains valid and must not be used as missing state.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Describes the semantic domain used to interpret a field's resolved shape.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasShapeDomain :
        IEquatable<AtlasShapeDomain>
    {
        private const int HashSeed = 17;
        private const int HashMultiplier = 397;

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
        /// Domain names are not field identity. They are stable ABI/debug metadata used by
        /// validation reports, tooling, artifacts, and derived export policy.
        /// </remarks>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Gets the optional source field identity for field-derived domains.
        /// </summary>
        /// <remarks>
        /// Zero/default is valid. Source presence is owned by <see cref="HasSourceField"/>.
        /// </remarks>
        public readonly StableDataId SourceFieldId;

        private readonly byte _hasSourceField;

        private AtlasShapeDomain(
            AtlasShapeDomainKind kind,
            FixedString64Bytes name,
            StableDataId sourceFieldId,
            bool hasSourceField)
        {
            Kind = kind;
            Name = name;
            SourceFieldId = sourceFieldId;
            _hasSourceField = hasSourceField ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Gets whether this value describes a concrete shape domain.
        /// </summary>
        public bool IsConcrete
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind != AtlasShapeDomainKind.None &&
                   !Name.IsEmpty;
        }

        /// <summary>
        /// Gets whether this value is not a concrete shape domain.
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
            get => _hasSourceField != 0;
        }

        /// <summary>
        /// Gets whether this domain is dense grid-like data.
        /// </summary>
        public bool IsDenseGrid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasShapeDomainKind.CellGrid2D ||
                   Kind == AtlasShapeDomainKind.VertexGrid2D ||
                   Kind == AtlasShapeDomainKind.VoxelGrid3D;
        }

        /// <summary>
        /// Gets whether this domain is row-like variable topology data.
        /// </summary>
        public bool IsTopologyRows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == AtlasShapeDomainKind.ComponentSet ||
                   Kind == AtlasShapeDomainKind.GraphNodeSet ||
                   Kind == AtlasShapeDomainKind.GraphEdgeSet;
        }

        /// <summary>
        /// Gets whether this domain is externally owned.
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
        /// Creates a component-set shape domain.
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
        /// Creates an external shape domain.
        /// </summary>
        public static AtlasShapeDomain External(FixedString64Bytes name)
        {
            return Create(
                AtlasShapeDomainKind.External,
                name);
        }

        /// <summary>
        /// Creates a shape domain from explicit metadata.
        /// </summary>
        public static AtlasShapeDomain Create(
            AtlasShapeDomainKind kind,
            FixedString64Bytes name)
        {
            var domain = new AtlasShapeDomain(
                kind,
                name,
                default,
                hasSourceField: false);

            domain.ValidateOrThrow(nameof(domain));
            return domain;
        }

        /// <summary>
        /// Creates a field-derived shape domain from explicit metadata.
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
                hasSourceField: true);

            domain.ValidateOrThrow(nameof(domain));
            return domain;
        }

        /// <summary>
        /// Throws when this value does not describe a concrete shape domain.
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

            SourceFieldId.ValidateOrThrow($"{name}.{nameof(SourceFieldId)}");

            if (HasSourceField)
            {
                return;
            }

            if (Kind == AtlasShapeDomainKind.PrefixSumPayload)
            {
                throw new ArgumentException(
                    "Prefix-sum payload domains must declare a source field.",
                    name);
            }
        }

        /// <summary>
        /// Determines whether this domain has the same kind and name as another domain.
        /// </summary>
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
                   _hasSourceField == other._hasSourceField;
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
        /// Returns a deterministic hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HashSeed;
                hash = (hash * HashMultiplier) ^ (int)Kind;
                hash = (hash * HashMultiplier) ^ Name.GetHashCode();
                hash = (hash * HashMultiplier) ^ SourceFieldId.GetHashCode();
                hash = (hash * HashMultiplier) ^ _hasSourceField;
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this domain.
        /// </summary>
        public override string ToString()
        {
            return HasSourceField
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1} Source={2}",
                    Kind,
                    Name,
                    SourceFieldId)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}",
                    Kind,
                    Name);
        }

        public static bool operator ==(AtlasShapeDomain left, AtlasShapeDomain right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AtlasShapeDomain left, AtlasShapeDomain right)
        {
            return !left.Equals(right);
        }
    }
}