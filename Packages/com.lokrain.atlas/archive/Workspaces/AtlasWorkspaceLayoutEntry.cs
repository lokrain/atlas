// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasWorkspaceLayoutEntry.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Represent one canonical field row inside a compiled Atlas workspace layout.
// - Bind stable field identity and Contract-table slot to a physical workspace field address.
// - Preserve resolved length, capacity, byte size, and shape-domain facts needed by workspace users.
// - Keep workspace allocation independent from authored Contracts and semantic compiler products.
//
// Design notes
// - This is workspace-layout metadata, not authored Contract metadata.
// - This type does not own memory.
// - This type does not allocate or dispose native containers.
// - This type does not know operations, stages, routes, schedulers, jobs, artifacts, or JobHandles.
// - default(AtlasWorkspaceLayoutEntry) is unbound for unmanaged/Burst compatibility.
// - StableDataId zero, slot zero, block index zero, byte offset zero, and type-hash zero are valid.
// - Missing/unbound state is represented by an explicit binding-state byte, not sentinel ids.
// - The embedded AtlasFieldAddress is the physical memory binding used by execution.
// - ShapeDomain and DeclaredShape are retained as resolved layout metadata for validation,
//   diagnostics, address lookup, artifact policy selection, and downstream view construction.
// - Declared shape constrains resolved logical length during shape resolution.
// - Declared shape does not force physical capacity to equal logical length.
// - Scalar storage requires length 1 and capacity 1.
// - NativeArray storage may have capacity greater than logical length.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Compilation;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Canonical workspace-layout row for one Atlas field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWorkspaceLayoutEntry"/> is produced by workspace-layout compilation after
    /// semantic shape resolution. It is the bridge between field identity and physical memory:
    /// a stable field id and Contract-table slot identify the field, while
    /// <see cref="Address"/> tells the workspace and execution systems where its storage lives.
    /// </para>
    ///
    /// <para>
    /// This type deliberately does not carry authored Contracts, operation Contracts, stage
    /// Contracts, pipeline Contracts, schedulers, native containers, job handles, or artifact
    /// writer state. It is compiler/runtime metadata used to allocate and bind workspace memory.
    /// </para>
    ///
    /// <para>
    /// The default value is allowed only as an unbound unmanaged payload. Bound entries are created
    /// through factory methods and are valid after construction.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct AtlasWorkspaceLayoutEntry :
        IEquatable<AtlasWorkspaceLayoutEntry>,
        IComparable<AtlasWorkspaceLayoutEntry>
    {
        private const byte UnboundState = 0;
        private const byte BoundState = 1;

        private readonly byte _bindingState;

        /// <summary>
        /// Durable field identity copied from the resolved shape.
        /// </summary>
        /// <remarks>
        /// Zero/default is valid and must not be treated as missing.
        /// </remarks>
        public readonly StableDataId StableId;

        /// <summary>
        /// Canonical Contract-table slot copied from the resolved shape and address.
        /// </summary>
        /// <remarks>
        /// Slot zero/default is valid and must not be treated as missing.
        /// </remarks>
        public readonly AtlasFieldSlot Slot;

        /// <summary>
        /// Semantic field role copied from resolved field metadata.
        /// </summary>
        public readonly AtlasFieldRole Role;

        /// <summary>
        /// Concrete unmanaged storage format used by this layout entry.
        /// </summary>
        public readonly StorageFormat StorageFormat;

        /// <summary>
        /// Resolved semantic shape domain used to interpret length and capacity.
        /// </summary>
        public readonly AtlasShapeDomain ShapeDomain;

        /// <summary>
        /// Original symbolic declared shape copied from resolved shape metadata.
        /// </summary>
        public readonly LengthShape DeclaredShape;

        /// <summary>
        /// Stable diagnostic field name copied from resolved shape metadata.
        /// </summary>
        /// <remarks>
        /// This is fixed unmanaged diagnostic metadata, not a managed string and not job input.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        /// <summary>
        /// Physical workspace address for this field.
        /// </summary>
        public readonly AtlasFieldAddress Address;

        private AtlasWorkspaceLayoutEntry(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            FixedString64Bytes debugName,
            AtlasFieldAddress address)
        {
            StableId = stableId;
            Slot = slot;
            Role = role;
            StorageFormat = storageFormat;
            ShapeDomain = shapeDomain;
            DeclaredShape = declaredShape;
            DebugName = debugName;
            Address = address;
            _bindingState = BoundState;
        }

        /// <summary>
        /// Gets whether this entry has been bound by workspace-layout compilation.
        /// </summary>
        public bool IsBound
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bindingState == BoundState;
        }

        /// <summary>
        /// Gets whether this entry is the default unbound unmanaged payload.
        /// </summary>
        public bool IsUnbound
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bindingState != BoundState;
        }

        /// <summary>
        /// Gets the declared storage kind.
        /// </summary>
        public StorageKind StorageKind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StorageFormat.Kind;
        }

        /// <summary>
        /// Gets the resolved logical element length.
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Address.Length;
        }

        /// <summary>
        /// Gets the resolved element capacity.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Address.Capacity;
        }

        /// <summary>
        /// Gets the resolved logical byte length.
        /// </summary>
        public long ByteLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Address.ByteLength;
        }

        /// <summary>
        /// Gets the resolved byte capacity.
        /// </summary>
        public long ByteCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Address.ByteCapacity;
        }

        /// <summary>
        /// Gets whether this entry requires non-zero workspace memory.
        /// </summary>
        public bool RequiresMemory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsBound && Address.RequiresMemory;
        }

        /// <summary>
        /// Gets whether capacity exceeds logical length.
        /// </summary>
        public bool HasCapacitySlack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsBound && Address.HasCapacitySlack;
        }

        /// <summary>
        /// Gets whether this entry describes dense-grid data.
        /// </summary>
        public bool IsDenseGrid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsBound && ShapeDomain.IsDenseGrid;
        }

        /// <summary>
        /// Gets whether this entry describes variable-payload data.
        /// </summary>
        public bool IsVariablePayload
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsBound && ShapeDomain.IsVariablePayload;
        }

        /// <summary>
        /// Gets the physical storage block index.
        /// </summary>
        public int BlockIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Address.BlockIndex;
        }

        /// <summary>
        /// Gets the block-relative byte offset.
        /// </summary>
        public long ByteOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Address.ByteOffset;
        }

        /// <summary>
        /// Creates a bound layout entry from resolved shape metadata and physical field address.
        /// </summary>
        public static AtlasWorkspaceLayoutEntry Create(
            AtlasResolvedShape shape,
            AtlasFieldAddress address)
        {
            shape.ValidateOrThrow(nameof(shape));

            return Create(
                shape.StableId,
                shape.Slot,
                shape.Role,
                shape.StorageFormat,
                shape.ShapeDomain,
                shape.DeclaredShape,
                shape.DebugName,
                address);
        }

        /// <summary>
        /// Creates a bound layout entry.
        /// </summary>
        public static AtlasWorkspaceLayoutEntry Create(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            FixedString64Bytes debugName,
            AtlasFieldAddress address)
        {
            ValidateInputsOrThrow(
                stableId,
                slot,
                role,
                storageFormat,
                shapeDomain,
                declaredShape,
                address);

            return new AtlasWorkspaceLayoutEntry(
                stableId,
                slot,
                role,
                storageFormat,
                shapeDomain,
                declaredShape,
                debugName,
                address);
        }

        /// <summary>
        /// Returns a copy of this entry assigned to a different physical field address.
        /// </summary>
        public AtlasWorkspaceLayoutEntry WithAddress(AtlasFieldAddress address)
        {
            ValidateBoundOrThrow();

            return Create(
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                DebugName,
                address);
        }

        /// <summary>
        /// Returns whether this entry describes the supplied stable field id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchesStableId(StableDataId stableId)
        {
            return IsBound && StableId == stableId;
        }

        /// <summary>
        /// Returns whether this entry describes the supplied canonical field slot.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchesSlot(AtlasFieldSlot slot)
        {
            return IsBound && Slot == slot;
        }

        /// <summary>
        /// Returns whether the supplied logical element index is inside this entry's logical range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsLogicalElementIndex(int elementIndex)
        {
            return IsBound && Address.ContainsLogicalElementIndex(elementIndex);
        }

        /// <summary>
        /// Returns whether the supplied element index is inside this entry's allocated capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsCapacityElementIndex(int elementIndex)
        {
            return IsBound && Address.ContainsCapacityElementIndex(elementIndex);
        }

        /// <summary>
        /// Computes the block-relative byte offset of a logical element.
        /// </summary>
        public long GetLogicalElementByteOffsetChecked(int elementIndex)
        {
            ValidateBoundOrThrow();

            return Address.GetLogicalElementByteOffsetChecked(elementIndex);
        }

        /// <summary>
        /// Computes the block-relative byte offset of an allocated-capacity element.
        /// </summary>
        public long GetCapacityElementByteOffsetChecked(int elementIndex)
        {
            ValidateBoundOrThrow();

            return Address.GetCapacityElementByteOffsetChecked(elementIndex);
        }

        /// <summary>
        /// Returns whether a block-relative byte range is inside this entry's logical byte range.
        /// </summary>
        public bool ContainsLogicalBlockByteRange(
            long blockByteOffset,
            long byteCount)
        {
            return IsBound &&
                   Address.ContainsLogicalBlockByteRange(
                       blockByteOffset,
                       byteCount);
        }

        /// <summary>
        /// Returns whether a block-relative byte range is inside this entry's allocated capacity.
        /// </summary>
        public bool ContainsCapacityBlockByteRange(
            long blockByteOffset,
            long byteCount)
        {
            return IsBound &&
                   Address.ContainsCapacityBlockByteRange(
                       blockByteOffset,
                       byteCount);
        }

        /// <summary>
        /// Validates that this value is a bound, internally consistent layout entry.
        /// </summary>
        public void ValidateBoundOrThrow(string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasWorkspaceLayoutEntry);

            if (IsUnbound)
            {
                throw new ArgumentException(
                    "Atlas workspace layout entry is unbound.",
                    name);
            }

            ValidateInputsOrThrow(
                StableId,
                Slot,
                Role,
                StorageFormat,
                ShapeDomain,
                DeclaredShape,
                Address,
                name);
        }

        /// <summary>
        /// Returns a stable diagnostic field name.
        /// </summary>
        public string GetDiagnosticName()
        {
            if (!DebugName.IsEmpty)
            {
                return DebugName.ToString();
            }

            return StableId.ToString();
        }

        /// <summary>
        /// Determines whether this layout entry equals another layout entry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AtlasWorkspaceLayoutEntry other)
        {
            return _bindingState == other._bindingState &&
                   StableId == other.StableId &&
                   Slot == other.Slot &&
                   Role == other.Role &&
                   StorageFormat == other.StorageFormat &&
                   ShapeDomain == other.ShapeDomain &&
                   DeclaredShape == other.DeclaredShape &&
                   DebugName.Equals(other.DebugName) &&
                   Address == other.Address;
        }

        /// <summary>
        /// Determines whether this layout entry equals an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AtlasWorkspaceLayoutEntry other &&
                   Equals(other);
        }

        /// <summary>
        /// Compares this entry with another entry using canonical slot order first.
        /// </summary>
        public int CompareTo(AtlasWorkspaceLayoutEntry other)
        {
            var boundComparison = _bindingState.CompareTo(other._bindingState);
            if (boundComparison != 0)
            {
                return boundComparison;
            }

            var slotComparison = Slot.Index.CompareTo(other.Slot.Index);
            if (slotComparison != 0)
            {
                return slotComparison;
            }

            var addressComparison = Address.CompareTo(other.Address);
            if (addressComparison != 0)
            {
                return addressComparison;
            }

            return CompareStableId(
                StableId,
                other.StableId);
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _bindingState;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ StableId.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Slot.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ (int)Role;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ StorageFormat.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ ShapeDomain.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ DeclaredShape.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ DebugName.GetHashCode();
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Address.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns an invariant diagnostic representation.
        /// </summary>
        public override string ToString()
        {
            if (IsUnbound)
            {
                return "AtlasWorkspaceLayoutEntry(Unbound)";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasWorkspaceLayoutEntry(Slot={0}, StableId={1}, Role={2}, Storage={3}, Block={4}, Offset={5}, Length={6}, Capacity={7}, Bytes={8}/{9})",
                Slot,
                StableId,
                Role,
                StorageKind,
                BlockIndex,
                ByteOffset,
                Length,
                Capacity,
                ByteLength,
                ByteCapacity);
        }

        /// <summary>
        /// Determines whether two layout entries are equal.
        /// </summary>
        public static bool operator ==(
            AtlasWorkspaceLayoutEntry left,
            AtlasWorkspaceLayoutEntry right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two layout entries are not equal.
        /// </summary>
        public static bool operator !=(
            AtlasWorkspaceLayoutEntry left,
            AtlasWorkspaceLayoutEntry right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the left entry sorts before the right entry.
        /// </summary>
        public static bool operator <(
            AtlasWorkspaceLayoutEntry left,
            AtlasWorkspaceLayoutEntry right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left entry sorts after the right entry.
        /// </summary>
        public static bool operator >(
            AtlasWorkspaceLayoutEntry left,
            AtlasWorkspaceLayoutEntry right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left entry sorts before or equal to the right entry.
        /// </summary>
        public static bool operator <=(
            AtlasWorkspaceLayoutEntry left,
            AtlasWorkspaceLayoutEntry right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left entry sorts after or equal to the right entry.
        /// </summary>
        public static bool operator >=(
            AtlasWorkspaceLayoutEntry left,
            AtlasWorkspaceLayoutEntry right)
        {
            return left.CompareTo(right) >= 0;
        }

        private static void ValidateInputsOrThrow(
            StableDataId stableId,
            AtlasFieldSlot slot,
            AtlasFieldRole role,
            StorageFormat storageFormat,
            AtlasShapeDomain shapeDomain,
            LengthShape declaredShape,
            AtlasFieldAddress address,
            string parameterName = null)
        {
            var name = parameterName ?? nameof(AtlasWorkspaceLayoutEntry);

            stableId.ValidateOrThrow(name);
            slot.ValidateOrThrow(name);
            storageFormat.ValidateOrThrow(name);
            shapeDomain.ValidateOrThrow(name);
            declaredShape.ValidateOrThrow(name);
            address.ValidateBoundOrThrow(name);

            if (role == AtlasFieldRole.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(role),
                    role,
                    "Workspace layout entries must have a concrete field role.");
            }

            if (address.Slot != slot)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry slot {0} does not match address slot {1}.",
                        slot,
                        address.Slot),
                    name);
            }

            if (address.StorageFormat != storageFormat)
            {
                throw new ArgumentException(
                    "Workspace layout entry storage format does not match address storage format.",
                    name);
            }

            if (storageFormat.Kind == StorageKind.Scalar &&
                (address.Length != 1 || address.Capacity != 1))
            {
                throw new ArgumentException(
                    "Scalar workspace layout entries must have length 1 and capacity 1.",
                    name);
            }
        }

        private static int CompareStableId(
            StableDataId left,
            StableDataId right)
        {
            var highComparison = left.High.CompareTo(right.High);
            if (highComparison != 0)
            {
                return highComparison;
            }

            var lowComparison = left.Low.CompareTo(right.Low);
            if (lowComparison != 0)
            {
                return lowComparison;
            }

            return left.Version.CompareTo(right.Version);
        }
    }
}