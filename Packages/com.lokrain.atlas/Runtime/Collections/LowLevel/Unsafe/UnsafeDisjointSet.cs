#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Internal unsafe disjoint-set / union-find container with path compression and union by rank.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type owns unmanaged memory directly. It is deliberately not marked as a Unity
    /// <c>NativeContainer</c>. A separate native wrapper or scheduler-owned scratch lease must own Unity safety handles,
    /// job dependencies, and dependency-aware disposal before this crosses Unity job boundaries as an owning container.
    /// </para>
    /// <para>
    /// The disjoint-set root is an implementation detail. If a generation artifact needs stable compact
    /// labels, build those labels in a separate canonicalization pass after all unions are complete.
    /// </para>
    /// <para>
    /// Equal-rank unions select the lower root index as the surviving root for deterministic tie-breaking.
    /// </para>
    /// <para>
    /// This is a single-writer unsafe core. It is not thread-safe and does not provide a parallel writer.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {" + nameof(Length) + "}, ComponentCount = {" + nameof(ComponentCount) + "}")]
    internal unsafe struct UnsafeDisjointSet : IDisposable
    {
        private const string TypeName = nameof(UnsafeDisjointSet);

        [NativeDisableUnsafePtrRestriction]
        private Header* m_Header;

        [NativeDisableUnsafePtrRestriction]
        private int* m_Parents;

        [NativeDisableUnsafePtrRestriction]
        private byte* m_Ranks;

        [NativeDisableUnsafePtrRestriction]
        private int* m_Sizes;

        private Allocator m_AllocatorLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeDisjointSet"/> struct.
        /// </summary>
        /// <param name="length">The number of elements.</param>
        /// <param name="allocator">The allocator used for unmanaged storage.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="length"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is not a valid allocation allocator.
        /// </exception>
        public UnsafeDisjointSet(int length, Allocator allocator)
        {
            ValidateConstructorArguments(length, allocator);

            m_Header = null;
            m_Parents = null;
            m_Ranks = null;
            m_Sizes = null;
            m_AllocatorLabel = allocator;

            try
            {
                m_Header = (Header*)UnsafeUtility.MallocTracked(
                    UnsafeUtility.SizeOf<Header>(),
                    UnsafeUtility.AlignOf<Header>(),
                    allocator,
                    1);

                m_Parents = (int*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<int>(length, nameof(length)),
                    UnsafeUtility.AlignOf<int>(),
                    allocator,
                    1);

                m_Ranks = (byte*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<byte>(length, nameof(length)),
                    UnsafeUtility.AlignOf<byte>(),
                    allocator,
                    1);

                m_Sizes = (int*)UnsafeUtility.MallocTracked(
                    UnsafeCollectionSize.CheckedPositiveElementBytes<int>(length, nameof(length)),
                    UnsafeUtility.AlignOf<int>(),
                    allocator,
                    1);

                m_Header->Length = length;
                m_Header->ComponentCount = length;

                Reset();
            }
            catch
            {
                FreeAllocatedMemory();

                m_Header = null;
                m_Parents = null;
                m_Ranks = null;
                m_Sizes = null;
                m_AllocatorLabel = Allocator.Invalid;

                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this value references allocated unmanaged disjoint-set storage.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Header != null && m_Parents != null && m_Ranks != null && m_Sizes != null;
        }

        /// <summary>
        /// Gets the number of elements in the disjoint set.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->Length;
            }
        }

        /// <summary>
        /// Gets the number of currently distinct components.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        public int ComponentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckCreated();
                CheckHeaderState();
                return m_Header->ComponentCount;
            }
        }

        /// <summary>
        /// Resets the container so every element is its own singleton component.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        public void Reset()
        {
            CheckCreated();
            CheckHeaderDimensions();

            int length = m_Header->Length;

            for (int index = 0; index < length; index++)
            {
                m_Parents[index] = index;
                m_Ranks[index] = 0;
                m_Sizes[index] = 1;
            }

            m_Header->ComponentCount = length;
        }

        /// <summary>
        /// Finds the current root of an element and performs path compression.
        /// </summary>
        /// <param name="index">The element index.</param>
        /// <returns>The current component root.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Find(int index)
        {
            CheckCreated();
            CheckHeaderState();
            CheckIndex(index, m_Header->Length);

            int root = FindRootNoChecks(index);

            while (m_Parents[index] != index)
            {
                int parent = m_Parents[index];
                CheckIndex(parent, m_Header->Length);

                m_Parents[index] = root;
                index = parent;
            }

            return root;
        }

        /// <summary>
        /// Finds the current root of an element without path compression.
        /// </summary>
        /// <param name="index">The element index.</param>
        /// <returns>The current component root.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindNoCompression(int index)
        {
            CheckCreated();
            CheckHeaderState();
            CheckIndex(index, m_Header->Length);

            return FindRootNoChecks(index);
        }

        /// <summary>
        /// Gets the size of the component containing the specified element.
        /// </summary>
        /// <param name="index">The element index.</param>
        /// <returns>The number of elements in the component.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSetSize(int index)
        {
            int root = Find(index);
            CheckRootSize(root);

            return m_Sizes[root];
        }

        /// <summary>
        /// Gets the size of a component by root index.
        /// </summary>
        /// <param name="rootIndex">The root index.</param>
        /// <returns>The number of elements in the component.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="rootIndex"/> is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated, structurally invalid, or
        /// <paramref name="rootIndex"/> is not currently a root.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRootSize(int rootIndex)
        {
            CheckCreated();
            CheckHeaderState();
            CheckIndex(rootIndex, m_Header->Length);
            CheckRoot(rootIndex);
            CheckRootSize(rootIndex);

            return m_Sizes[rootIndex];
        }

        /// <summary>
        /// Gets a value indicating whether the specified element is currently a component root.
        /// </summary>
        /// <param name="index">The element index.</param>
        /// <returns><see langword="true"/> when the element is a root; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRoot(int index)
        {
            CheckCreated();
            CheckHeaderState();
            CheckIndex(index, m_Header->Length);

            int parent = m_Parents[index];
            CheckIndex(parent, m_Header->Length);

            return parent == index;
        }

        /// <summary>
        /// Gets a value indicating whether two elements currently belong to the same component.
        /// </summary>
        /// <param name="left">The first element index.</param>
        /// <param name="right">The second element index.</param>
        /// <returns><see langword="true"/> when both elements are connected; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when either element index is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AreConnected(int left, int right)
        {
            return Find(left) == Find(right);
        }

        /// <summary>
        /// Unions the components containing the specified elements.
        /// </summary>
        /// <param name="left">The first element index.</param>
        /// <param name="right">The second element index.</param>
        /// <returns>
        /// <see langword="true"/> when two previously separate components were merged;
        /// otherwise, <see langword="false"/> when both elements were already in the same component.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when either element index is outside the valid element range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        public bool Union(int left, int right)
        {
            int leftRoot = Find(left);
            int rightRoot = Find(right);

            if (leftRoot == rightRoot)
            {
                return false;
            }

            byte leftRank = m_Ranks[leftRoot];
            byte rightRank = m_Ranks[rightRoot];

            if (leftRank < rightRank)
            {
                Attach(childRoot: leftRoot, parentRoot: rightRoot);
                return true;
            }

            if (leftRank > rightRank)
            {
                Attach(childRoot: rightRoot, parentRoot: leftRoot);
                return true;
            }

            if (rightRoot < leftRoot)
            {
                int swap = leftRoot;
                leftRoot = rightRoot;
                rightRoot = swap;
            }

            Attach(childRoot: rightRoot, parentRoot: leftRoot);

            if (m_Ranks[leftRoot] != byte.MaxValue)
            {
                m_Ranks[leftRoot]++;
            }

            return true;
        }

        /// <summary>
        /// Performs path compression for every element.
        /// </summary>
        /// <remarks>
        /// Call this after all unions when subsequent passes will repeatedly query roots.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the disjoint set is uncreated or structurally invalid.
        /// </exception>
        public void CompressAll()
        {
            CheckCreated();
            CheckHeaderState();

            int length = m_Header->Length;

            for (int index = 0; index < length; index++)
            {
                Find(index);
            }
        }

        /// <summary>
        /// Releases all unmanaged memory owned by this disjoint-set value.
        /// </summary>
        /// <remarks>
        /// Because this is an unsafe core, disposing one value does not invalidate copied values.
        /// A native wrapper or scheduler-owned scratch lease is responsible for Unity safety-handle invalidation
        /// and job dependency ownership.
        /// </remarks>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            FreeAllocatedMemory();

            m_Header = null;
            m_Parents = null;
            m_Ranks = null;
            m_Sizes = null;
            m_AllocatorLabel = Allocator.Invalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRootNoChecks(int index)
        {
            int root = index;
            int guard = 0;
            int length = m_Header->Length;

            while (m_Parents[root] != root)
            {
                root = m_Parents[root];
                CheckIndex(root, length);

                guard++;

                if (guard > length)
                {
                    UnsafeCollectionChecks.ThrowCorruptState(TypeName);
                }
            }

            return root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Attach(int childRoot, int parentRoot)
        {
            CheckRoot(childRoot);
            CheckRoot(parentRoot);

            int childSize = m_Sizes[childRoot];
            int parentSize = m_Sizes[parentRoot];

            if (childSize <= 0 || parentSize <= 0)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }

            m_Parents[childRoot] = parentRoot;
            m_Sizes[parentRoot] = UnsafeCollectionSize.CheckedAdd(
                parentSize,
                childSize,
                TypeName + " component size overflowed.");
            m_Sizes[childRoot] = 0;
            m_Header->ComponentCount--;
        }

        private void FreeAllocatedMemory()
        {
            if (m_Sizes != null)
            {
                UnsafeUtility.FreeTracked(m_Sizes, m_AllocatorLabel);
            }

            if (m_Ranks != null)
            {
                UnsafeUtility.FreeTracked(m_Ranks, m_AllocatorLabel);
            }

            if (m_Parents != null)
            {
                UnsafeUtility.FreeTracked(m_Parents, m_AllocatorLabel);
            }

            if (m_Header != null)
            {
                UnsafeUtility.FreeTracked(m_Header, m_AllocatorLabel);
            }
        }

        private static void ValidateConstructorArguments(int length, Allocator allocator)
        {
            UnsafeCollectionChecks.CheckPositive(length, nameof(length));
            UnsafeCollectionChecks.CheckAllocator(allocator, nameof(allocator));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            UnsafeCollectionChecks.CheckCreated(IsCreated, TypeName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderDimensions()
        {
            UnsafeCollectionChecks.CheckPositive(m_Header->Length, nameof(Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHeaderState()
        {
            CheckHeaderDimensions();

            int length = m_Header->Length;
            int componentCount = m_Header->ComponentCount;

            if (componentCount <= 0 || componentCount > length)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIndex(int index, int length)
        {
            UnsafeCollectionChecks.CheckIndexInRange(index, length, nameof(index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRoot(int rootIndex)
        {
            CheckIndex(rootIndex, m_Header->Length);

            int parentIndex = m_Parents[rootIndex];
            CheckIndex(parentIndex, m_Header->Length);

            if (rootIndex != parentIndex)
            {
                throw new InvalidOperationException(TypeName + " index is not a root.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRootSize(int rootIndex)
        {
            int size = m_Sizes[rootIndex];

            if (size <= 0 || size > m_Header->Length)
            {
                UnsafeCollectionChecks.ThrowCorruptState(TypeName);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public int Length;
            public int ComponentCount;
        }
    }
}