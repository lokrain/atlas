// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasWorkspaceLayout.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Store the immutable compiled memory layout for one Atlas workspace.
// - Preserve canonical field-slot ordering and physical storage-block ordering.
// - Validate field-address containment, block coverage, byte ranges, and non-overlap.
// - Give AtlasWorkspace an allocation recipe without exposing Contracts or shape-resolution sets.
//
// Design notes
// - This is memory-layout output, not authored Contract metadata.
// - This type does not own native memory.
// - This type does not allocate or dispose Unity native containers.
// - This type does not know operations, stages, routes, schedulers, jobs, JobHandles, or artifacts.
// - Entries are stored in canonical field-slot order.
// - Storage blocks are stored in physical block-index order.
// - Slot zero, StableDataId zero, block index zero, byte offset zero, and type-hash zero are valid.
// - Absence is represented by bool-returning lookup APIs, not sentinel ids.
// - Lookups are intentionally linear; layout size is compiler metadata scale and deterministic
//   array order is more important than dictionary machinery here.
// - GetHashCode is deterministic and does not use System.HashCode.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Unity.Collections;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Immutable compiled memory layout for an Atlas workspace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWorkspaceLayout"/> is produced after semantic shape resolution and before
    /// workspace allocation. It defines which physical storage blocks must be allocated and how
    /// each canonical field slot maps to a block-relative byte address.
    /// </para>
    ///
    /// <para>
    /// This type deliberately does not retain authored Contracts, operation Contracts, stage
    /// Contracts, route Contracts, execution plans, native containers, job handles, artifact
    /// builders, or editor state. It is the stable memory-layout product consumed by workspace
    /// allocation and later execution-plan binding.
    /// </para>
    /// </remarks>
    public sealed class AtlasWorkspaceLayout :
        IReadOnlyList<AtlasWorkspaceLayoutEntry>
    {
        private readonly AtlasWorkspaceLayoutEntry[] _entries;
        private readonly AtlasStorageBlockPlan[] _storageBlocks;

        /// <summary>
        /// Diagnostic layout name used by exceptions, reports, tooling, and tests.
        /// </summary>
        public readonly FixedString64Bytes Name;

        private AtlasWorkspaceLayout(
            FixedString64Bytes name,
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks)
        {
            Name = name;
            _entries = BuildEntryArray(entries);
            _storageBlocks = BuildStorageBlockArray(storageBlocks);

            ValidateLayoutOrThrow(
                _entries,
                _storageBlocks,
                GetDiagnosticName());
        }

        /// <summary>
        /// Gets the number of field layout entries.
        /// </summary>
        public int Count => _entries.Length;

        /// <summary>
        /// Gets the number of planned physical storage blocks.
        /// </summary>
        public int StorageBlockCount => _storageBlocks.Length;

        /// <summary>
        /// Gets whether this layout contains no field entries and no storage blocks.
        /// </summary>
        public bool IsEmpty => _entries.Length == 0 && _storageBlocks.Length == 0;

        /// <summary>
        /// Gets whether at least one storage block requires non-zero native memory.
        /// </summary>
        public bool RequiresMemory
        {
            get
            {
                for (var i = 0; i < _storageBlocks.Length; i++)
                {
                    if (_storageBlocks[i].RequiresMemory)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one field entry has capacity greater than logical length.
        /// </summary>
        public bool HasCapacitySlack
        {
            get
            {
                for (var i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].HasCapacitySlack)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the total logical byte length across all field entries.
        /// </summary>
        public long TotalFieldByteLength
        {
            get
            {
                long total = 0L;

                for (var i = 0; i < _entries.Length; i++)
                {
                    total = checked(total + _entries[i].ByteLength);
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the total allocated byte capacity across all field entries.
        /// </summary>
        public long TotalFieldByteCapacity
        {
            get
            {
                long total = 0L;

                for (var i = 0; i < _entries.Length; i++)
                {
                    total = checked(total + _entries[i].ByteCapacity);
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the total used byte length across all physical storage blocks.
        /// </summary>
        public long TotalBlockUsedByteLength
        {
            get
            {
                long total = 0L;

                for (var i = 0; i < _storageBlocks.Length; i++)
                {
                    total = checked(total + _storageBlocks[i].UsedByteLength);
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the total byte capacity that the workspace must allocate.
        /// </summary>
        public long TotalBlockByteCapacity
        {
            get
            {
                long total = 0L;

                for (var i = 0; i < _storageBlocks.Length; i++)
                {
                    total = checked(total + _storageBlocks[i].ByteCapacity);
                }

                return total;
            }
        }

        /// <summary>
        /// Gets a layout entry by canonical slot index.
        /// </summary>
        public AtlasWorkspaceLayoutEntry this[int index]
        {
            get
            {
                ThrowIfEntryIndexOutOfRange(index);
                return _entries[index];
            }
        }

        /// <summary>
        /// Gets a layout entry by canonical field slot.
        /// </summary>
        public AtlasWorkspaceLayoutEntry this[AtlasFieldSlot slot] => GetRequiredEntry(slot);

        /// <summary>
        /// Creates a workspace layout.
        /// </summary>
        public static AtlasWorkspaceLayout Create(
            FixedString64Bytes name,
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks)
        {
            return new AtlasWorkspaceLayout(
                name,
                entries,
                storageBlocks);
        }

        /// <summary>
        /// Creates a workspace layout with the default diagnostic name.
        /// </summary>
        public static AtlasWorkspaceLayout Create(
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks)
        {
            return new AtlasWorkspaceLayout(
                default,
                entries,
                storageBlocks);
        }

        /// <summary>
        /// Determines whether this layout contains an entry for the supplied slot.
        /// </summary>
        public bool Contains(AtlasFieldSlot slot)
        {
            return TryGetEntry(
                slot,
                out _);
        }

        /// <summary>
        /// Determines whether this layout contains an entry for the supplied stable field id.
        /// </summary>
        public bool Contains(StableDataId stableId)
        {
            return TryGetEntry(
                stableId,
                out _);
        }

        /// <summary>
        /// Determines whether this layout contains an entry for the supplied typed field declaration.
        /// </summary>
        public bool Contains<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return Contains(AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to get a layout entry by canonical field slot.
        /// </summary>
        public bool TryGetEntry(
            AtlasFieldSlot slot,
            out AtlasWorkspaceLayoutEntry entry)
        {
            var index = slot.Index;

            if (index >= 0 &&
                index < _entries.Length &&
                _entries[index].Slot == slot)
            {
                entry = _entries[index];
                return true;
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a layout entry by stable field id.
        /// </summary>
        public bool TryGetEntry(
            StableDataId stableId,
            out AtlasWorkspaceLayoutEntry entry)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].StableId == stableId)
                {
                    entry = _entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a layout entry by typed field declaration.
        /// </summary>
        public bool TryGetEntry<TField, TElement>(
            out AtlasWorkspaceLayoutEntry entry)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetEntry(
                AtlasField.StableId<TField, TElement>(),
                out entry);
        }

        /// <summary>
        /// Gets a required layout entry by canonical field slot.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(AtlasFieldSlot slot)
        {
            if (TryGetEntry(slot, out var entry))
            {
                return entry;
            }

            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas workspace layout '{0}' does not contain slot '{1}'.",
                    GetDiagnosticName(),
                    slot));
        }

        /// <summary>
        /// Gets a required layout entry by stable field id.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry(StableDataId stableId)
        {
            if (TryGetEntry(stableId, out var entry))
            {
                return entry;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas workspace layout '{0}' does not contain field id '{1}'.",
                    GetDiagnosticName(),
                    stableId),
                nameof(stableId));
        }

        /// <summary>
        /// Gets a required layout entry by typed field declaration.
        /// </summary>
        public AtlasWorkspaceLayoutEntry GetRequiredEntry<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredEntry(
                AtlasField.StableId<TField, TElement>());
        }

        /// <summary>
        /// Attempts to get a physical field address by canonical field slot.
        /// </summary>
        public bool TryGetAddress(
            AtlasFieldSlot slot,
            out AtlasFieldAddress address)
        {
            if (TryGetEntry(slot, out var entry))
            {
                address = entry.Address;
                return true;
            }

            address = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a physical field address by stable field id.
        /// </summary>
        public bool TryGetAddress(
            StableDataId stableId,
            out AtlasFieldAddress address)
        {
            if (TryGetEntry(stableId, out var entry))
            {
                address = entry.Address;
                return true;
            }

            address = default;
            return false;
        }

        /// <summary>
        /// Attempts to get a physical field address by typed field declaration.
        /// </summary>
        public bool TryGetAddress<TField, TElement>(
            out AtlasFieldAddress address)
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return TryGetAddress(
                AtlasField.StableId<TField, TElement>(),
                out address);
        }

        /// <summary>
        /// Gets a required physical field address by canonical field slot.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress(AtlasFieldSlot slot)
        {
            return GetRequiredEntry(slot).Address;
        }

        /// <summary>
        /// Gets a required physical field address by stable field id.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress(StableDataId stableId)
        {
            return GetRequiredEntry(stableId).Address;
        }

        /// <summary>
        /// Gets a required physical field address by typed field declaration.
        /// </summary>
        public AtlasFieldAddress GetRequiredAddress<TField, TElement>()
            where TField : struct, IAtlasField<TElement>
            where TElement : unmanaged
        {
            return GetRequiredEntry<TField, TElement>().Address;
        }

        /// <summary>
        /// Attempts to get a planned physical storage block.
        /// </summary>
        public bool TryGetStorageBlockPlan(
            int blockIndex,
            out AtlasStorageBlockPlan blockPlan)
        {
            if (blockIndex >= 0 &&
                blockIndex < _storageBlocks.Length &&
                _storageBlocks[blockIndex].BlockIndex == blockIndex)
            {
                blockPlan = _storageBlocks[blockIndex];
                return true;
            }

            blockPlan = default;
            return false;
        }

        /// <summary>
        /// Gets a required planned physical storage block.
        /// </summary>
        public AtlasStorageBlockPlan GetRequiredStorageBlockPlan(int blockIndex)
        {
            if (TryGetStorageBlockPlan(blockIndex, out var blockPlan))
            {
                return blockPlan;
            }

            throw new ArgumentOutOfRangeException(
                nameof(blockIndex),
                blockIndex,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Atlas workspace layout '{0}' does not contain storage block '{1}'.",
                    GetDiagnosticName(),
                    blockIndex));
        }

        /// <summary>
        /// Copies field layout entries into a caller-provided array.
        /// </summary>
        public void CopyEntriesTo(AtlasWorkspaceLayoutEntry[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _entries.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Destination length {0} is smaller than layout entry count {1}.",
                        destination.Length,
                        _entries.Length),
                    nameof(destination));
            }

            Array.Copy(
                _entries,
                destination,
                _entries.Length);
        }

        /// <summary>
        /// Copies planned physical storage blocks into a caller-provided array.
        /// </summary>
        public void CopyStorageBlockPlansTo(AtlasStorageBlockPlan[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _storageBlocks.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Destination length {0} is smaller than storage block count {1}.",
                        destination.Length,
                        _storageBlocks.Length),
                    nameof(destination));
            }

            Array.Copy(
                _storageBlocks,
                destination,
                _storageBlocks.Length);
        }

        /// <summary>
        /// Creates a defensive copy of all field layout entries.
        /// </summary>
        public AtlasWorkspaceLayoutEntry[] ToEntryArray()
        {
            var copy = new AtlasWorkspaceLayoutEntry[_entries.Length];

            Array.Copy(
                _entries,
                copy,
                _entries.Length);

            return copy;
        }

        /// <summary>
        /// Creates a defensive copy of all planned physical storage blocks.
        /// </summary>
        public AtlasStorageBlockPlan[] ToStorageBlockPlanArray()
        {
            var copy = new AtlasStorageBlockPlan[_storageBlocks.Length];

            Array.Copy(
                _storageBlocks,
                copy,
                _storageBlocks.Length);

            return copy;
        }

        /// <summary>
        /// Validates this layout's internal consistency.
        /// </summary>
        public void ValidateOrThrow()
        {
            ValidateLayoutOrThrow(
                _entries,
                _storageBlocks,
                GetDiagnosticName());
        }

        /// <summary>
        /// Gets a stable managed diagnostic name.
        /// </summary>
        public string GetDiagnosticName()
        {
            if (!Name.IsEmpty)
            {
                return Name.ToString();
            }

            return "atlas-workspace-layout";
        }

        /// <summary>
        /// Returns an enumerator over field layout entries in canonical slot order.
        /// </summary>
        public IEnumerator<AtlasWorkspaceLayoutEntry> GetEnumerator()
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                yield return _entries[i];
            }
        }

        /// <summary>
        /// Returns an enumerator over field layout entries in canonical slot order.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a deterministic managed hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = AtlasConstants.DeterministicHashSeed;
                hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ Name.GetHashCode();

                for (var i = 0; i < _entries.Length; i++)
                {
                    hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _entries[i].GetHashCode();
                }

                for (var i = 0; i < _storageBlocks.Length; i++)
                {
                    hash = (hash * AtlasConstants.DeterministicHashMultiplier) ^ _storageBlocks[i].GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns an invariant diagnostic representation.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasWorkspaceLayout(Name={0}, Entries={1}, StorageBlocks={2}, UsedBytes={3}, CapacityBytes={4})",
                GetDiagnosticName(),
                Count,
                StorageBlockCount,
                TotalBlockUsedByteLength,
                TotalBlockByteCapacity);
        }

        private static AtlasWorkspaceLayoutEntry[] BuildEntryArray(AtlasWorkspaceLayoutEntry[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Length > AtlasConstants.MaxFieldSlots)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entries),
                    entries.Length,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Workspace layout entry count must not exceed {0}.",
                        AtlasConstants.MaxFieldSlots));
            }

            var result = new AtlasWorkspaceLayoutEntry[entries.Length];

            Array.Copy(
                entries,
                result,
                entries.Length);

            return result;
        }

        private static AtlasStorageBlockPlan[] BuildStorageBlockArray(AtlasStorageBlockPlan[] storageBlocks)
        {
            if (storageBlocks == null)
            {
                throw new ArgumentNullException(nameof(storageBlocks));
            }

            var result = new AtlasStorageBlockPlan[storageBlocks.Length];

            Array.Copy(
                storageBlocks,
                result,
                storageBlocks.Length);

            return result;
        }

        private static void ValidateLayoutOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks,
            string diagnosticName)
        {
            ValidateEmptyLayoutOrThrow(
                entries,
                storageBlocks,
                diagnosticName);

            ValidateEntryOrderAndUniquenessOrThrow(
                entries,
                diagnosticName);

            ValidateStorageBlockOrderOrThrow(
                storageBlocks,
                diagnosticName);

            ValidateEntryBlockBindingsOrThrow(
                entries,
                storageBlocks,
                diagnosticName);

            ValidateBlockAddressCountsOrThrow(
                entries,
                storageBlocks,
                diagnosticName);

            ValidateBlockUsedLengthsOrThrow(
                entries,
                storageBlocks,
                diagnosticName);

            ValidateNoOverlappingAddressRangesOrThrow(
                entries,
                diagnosticName);
        }

        private static void ValidateEmptyLayoutOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks,
            string diagnosticName)
        {
            if (entries.Length == 0 && storageBlocks.Length == 0)
            {
                return;
            }

            if (entries.Length == 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace layout '{0}' has storage blocks but no field entries.",
                        diagnosticName),
                    nameof(entries));
            }

            if (storageBlocks.Length == 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas workspace layout '{0}' has field entries but no storage blocks.",
                        diagnosticName),
                    nameof(storageBlocks));
            }
        }

        private static void ValidateEntryOrderAndUniquenessOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            string diagnosticName)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                entry.ValidateBoundOrThrow(nameof(entries));

                if (entry.Slot.Index != i)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' entry at index {1} has slot {2}. Entries must be stored in canonical slot order.",
                            diagnosticName,
                            i,
                            entry.Slot),
                        nameof(entries));
                }

                for (var j = 0; j < i; j++)
                {
                    if (entries[j].StableId == entry.StableId)
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Atlas workspace layout '{0}' contains duplicate stable field id '{1}' at slots {2} and {3}.",
                                diagnosticName,
                                entry.StableId,
                                entries[j].Slot,
                                entry.Slot),
                            nameof(entries));
                    }
                }
            }
        }

        private static void ValidateStorageBlockOrderOrThrow(
            AtlasStorageBlockPlan[] storageBlocks,
            string diagnosticName)
        {
            for (var i = 0; i < storageBlocks.Length; i++)
            {
                var blockPlan = storageBlocks[i];

                blockPlan.ValidatePlannedOrThrow(nameof(storageBlocks));

                if (blockPlan.BlockIndex != i)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' storage block at array index {1} has block index {2}. Blocks must be stored in physical block order.",
                            diagnosticName,
                            i,
                            blockPlan.BlockIndex),
                        nameof(storageBlocks));
                }
            }
        }

        private static void ValidateEntryBlockBindingsOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks,
            string diagnosticName)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var address = entry.Address;

                if (address.BlockIndex < 0 ||
                    address.BlockIndex >= storageBlocks.Length)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' entry slot {1} references missing storage block {2}.",
                            diagnosticName,
                            entry.Slot,
                            address.BlockIndex),
                        nameof(entries));
                }

                var blockPlan = storageBlocks[address.BlockIndex];

                if (blockPlan.RequiredAlignment < address.ElementAlignment)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' storage block {1} alignment {2} is lower than entry slot {3} element alignment {4}.",
                            diagnosticName,
                            blockPlan.BlockIndex,
                            blockPlan.RequiredAlignment,
                            entry.Slot,
                            address.ElementAlignment),
                        nameof(storageBlocks));
                }

                if (!blockPlan.ContainsByteRange(
                        address.ByteOffset,
                        address.ByteCapacity))
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' entry slot {1} byte range [{2}, {3}) is outside storage block {4} capacity {5}.",
                            diagnosticName,
                            entry.Slot,
                            address.ByteOffset,
                            address.CapacityEndByteOffsetExclusive,
                            blockPlan.BlockIndex,
                            blockPlan.ByteCapacity),
                        nameof(entries));
                }
            }
        }

        private static void ValidateBlockAddressCountsOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks,
            string diagnosticName)
        {
            for (var blockIndex = 0; blockIndex < storageBlocks.Length; blockIndex++)
            {
                var count = 0;

                for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    if (entries[entryIndex].BlockIndex == blockIndex)
                    {
                        count++;
                    }
                }

                if (count != storageBlocks[blockIndex].FieldAddressCount)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' storage block {1} declares {2} field addresses but {3} entries reference it.",
                            diagnosticName,
                            blockIndex,
                            storageBlocks[blockIndex].FieldAddressCount,
                            count),
                        nameof(storageBlocks));
                }
            }
        }

        private static void ValidateBlockUsedLengthsOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            AtlasStorageBlockPlan[] storageBlocks,
            string diagnosticName)
        {
            for (var blockIndex = 0; blockIndex < storageBlocks.Length; blockIndex++)
            {
                var expectedUsedByteLength = 0L;

                for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    var entry = entries[entryIndex];

                    if (entry.BlockIndex != blockIndex ||
                        entry.ByteCapacity == 0L)
                    {
                        continue;
                    }

                    if (entry.Address.CapacityEndByteOffsetExclusive > expectedUsedByteLength)
                    {
                        expectedUsedByteLength = entry.Address.CapacityEndByteOffsetExclusive;
                    }
                }

                if (storageBlocks[blockIndex].UsedByteLength != expectedUsedByteLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Atlas workspace layout '{0}' storage block {1} used byte length is {2}, but address ranges require {3}.",
                            diagnosticName,
                            blockIndex,
                            storageBlocks[blockIndex].UsedByteLength,
                            expectedUsedByteLength),
                        nameof(storageBlocks));
                }
            }
        }

        private static void ValidateNoOverlappingAddressRangesOrThrow(
            AtlasWorkspaceLayoutEntry[] entries,
            string diagnosticName)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                var left = entries[i];

                if (left.ByteCapacity == 0L)
                {
                    continue;
                }

                var leftStart = left.ByteOffset;
                var leftEnd = left.Address.CapacityEndByteOffsetExclusive;

                for (var j = i + 1; j < entries.Length; j++)
                {
                    var right = entries[j];

                    if (right.ByteCapacity == 0L ||
                        right.BlockIndex != left.BlockIndex)
                    {
                        continue;
                    }

                    var rightStart = right.ByteOffset;
                    var rightEnd = right.Address.CapacityEndByteOffsetExclusive;

                    if (leftStart < rightEnd &&
                        rightStart < leftEnd)
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Atlas workspace layout '{0}' has overlapping byte ranges in storage block {1}: slot {2} [{3}, {4}) overlaps slot {5} [{6}, {7}).",
                                diagnosticName,
                                left.BlockIndex,
                                left.Slot,
                                leftStart,
                                leftEnd,
                                right.Slot,
                                rightStart,
                                rightEnd),
                            nameof(entries));
                    }
                }
            }
        }

        private void ThrowIfEntryIndexOutOfRange(int index)
        {
            if (index >= 0 && index < _entries.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Entry index must be between 0 and {0}.",
                    _entries.Length - 1));
        }
    }
}