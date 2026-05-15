// Runtime/Contracts/StorageFormat.cs

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Contracts
{
    /// <summary>
    /// Describes the physical unmanaged storage format required by an Atlas Field Contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage format is schema metadata. It combines the container family declared by
    /// <see cref="StorageKind"/> with the unmanaged element layout required to allocate,
    /// validate, reconstruct, and type-check runtime storage.
    /// </para>
    ///
    /// <para>
    /// This type does not own memory. It describes how memory must be allocated or interpreted
    /// by storage systems before jobs receive resolved native containers.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct StorageFormat :
        IEquatable<StorageFormat>
    {
        /// <summary>
        /// Reserved invalid storage format.
        /// </summary>
        public static readonly StorageFormat None = default;

        /// <summary>
        /// Native storage family used by the Field.
        /// </summary>
        public readonly StorageKind Kind;

        /// <summary>
        /// Size in bytes of one unmanaged element.
        /// </summary>
        /// <remarks>
        /// Valid concrete formats must have a positive element size.
        /// </remarks>
        public readonly int ElementSize;

        /// <summary>
        /// Required alignment in bytes for one unmanaged element.
        /// </summary>
        /// <remarks>
        /// Valid concrete formats must have a positive element alignment.
        /// </remarks>
        public readonly int ElementAlignment;

        /// <summary>
        /// Stable runtime hash of the unmanaged element type.
        /// </summary>
        /// <remarks>
        /// This hash is used for validation and diagnostics. It is not durable identity across
        /// deliberate Field-contract migrations. Durable Field identity is represented by
        /// <see cref="StableDataId"/>.
        /// </remarks>
        public readonly ulong ElementTypeHash;

        private StorageFormat(
            StorageKind kind,
            int elementSize,
            int elementAlignment,
            ulong elementTypeHash)
        {
            Kind = kind;
            ElementSize = elementSize;
            ElementAlignment = elementAlignment;
            ElementTypeHash = elementTypeHash;
        }

        /// <summary>
        /// Gets whether this format is valid for a concrete Atlas Field Contract.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind != StorageKind.None &&
                   ElementSize > AtlasConstants.InvalidElementSize &&
                   ElementAlignment > AtlasConstants.InvalidElementAlignment &&
                   ElementTypeHash != 0UL;
        }

        /// <summary>
        /// Gets whether this format represents contiguous element storage.
        /// </summary>
        /// <remarks>
        /// Contiguous storage can usually be exposed as array-like memory for dense jobs.
        /// </remarks>
        public bool IsContiguous
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == StorageKind.Scalar ||
                   Kind == StorageKind.NativeArray ||
                   Kind == StorageKind.NativeList ||
                   Kind == StorageKind.UnsafeList;
        }

        /// <summary>
        /// Gets whether this format requires a fixed resolved length before scheduling.
        /// </summary>
        public bool RequiresFixedLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == StorageKind.Scalar ||
                   Kind == StorageKind.NativeArray ||
                   Kind == StorageKind.Blob;
        }

        /// <summary>
        /// Gets whether this format supports runtime resizing when the Field declaration allows it.
        /// </summary>
        public bool SupportsResize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == StorageKind.NativeList ||
                   Kind == StorageKind.UnsafeList ||
                   Kind == StorageKind.NativeStream ||
                   Kind == StorageKind.NativeParallelHashMap;
        }

        /// <summary>
        /// Creates scalar storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged scalar element type.</typeparam>
        /// <returns>A scalar storage format.</returns>
        public static StorageFormat Scalar<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.Scalar);
        }

        /// <summary>
        /// Creates fixed-length native array storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged array element type.</typeparam>
        /// <returns>A native array storage format.</returns>
        public static StorageFormat NativeArray<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.NativeArray);
        }

        /// <summary>
        /// Creates variable-length native list storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged list element type.</typeparam>
        /// <returns>A native list storage format.</returns>
        public static StorageFormat NativeList<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.NativeList);
        }

        /// <summary>
        /// Creates low-level unsafe list storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged list element type.</typeparam>
        /// <returns>An unsafe list storage format.</returns>
        public static StorageFormat UnsafeList<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.UnsafeList);
        }

        /// <summary>
        /// Creates native stream storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged stream element type.</typeparam>
        /// <returns>A native stream storage format.</returns>
        public static StorageFormat NativeStream<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.NativeStream);
        }

        /// <summary>
        /// Creates native parallel hash-map storage format for unmanaged key and value types.
        /// </summary>
        /// <typeparam name="TKey">Unmanaged key type.</typeparam>
        /// <typeparam name="TValue">Unmanaged value type.</typeparam>
        /// <returns>A native parallel hash-map storage format.</returns>
        /// <remarks>
        /// The element layout represents one key-value pair for validation and capacity planning.
        /// Runtime storage still uses the native hash-map container appropriate for the key and
        /// value types.
        /// </remarks>
        public static StorageFormat NativeParallelHashMap<TKey, TValue>()
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new StorageFormat(
                StorageKind.NativeParallelHashMap,
                ElementPair<TKey, TValue>.Size,
                ElementPair<TKey, TValue>.Alignment,
                ElementPair<TKey, TValue>.TypeHash);
        }

        /// <summary>
        /// Creates immutable blob-style storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged blob element type.</typeparam>
        /// <returns>A blob storage format.</returns>
        public static StorageFormat Blob<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.Blob);
        }

        /// <summary>
        /// Creates externally owned storage format for an unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged external element type.</typeparam>
        /// <returns>An external storage format.</returns>
        public static StorageFormat External<TElement>()
            where TElement : unmanaged
        {
            return Create<TElement>(StorageKind.External);
        }

        /// <summary>
        /// Creates storage format for an unmanaged element type and explicit storage kind.
        /// </summary>
        /// <typeparam name="TElement">Unmanaged element type.</typeparam>
        /// <param name="kind">Storage kind to associate with the element layout.</param>
        /// <returns>A storage format for the supplied kind and element type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="kind"/> is <see cref="StorageKind.None"/>.
        /// </exception>
        public static StorageFormat Create<TElement>(StorageKind kind)
            where TElement : unmanaged
        {
            if (kind == StorageKind.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "Storage kind must be concrete.");
            }

            return new StorageFormat(
                kind,
                UnsafeUtility.SizeOf<TElement>(),
                UnsafeUtility.AlignOf<TElement>(),
                AtlasTypeHash.Of<TElement>());
        }

        /// <summary>
        /// Throws when this storage format is invalid or internally inconsistent.
        /// </summary>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the storage format is invalid.
        /// </exception>
        public void ValidateOrThrow(string parameterName = null)
        {
            if (IsValid)
            {
                return;
            }

            throw new ArgumentException(
                $"Storage format '{this}' is invalid.",
                parameterName ?? nameof(StorageFormat));
        }

        /// <summary>
        /// Throws when this storage format is not compatible with the supplied unmanaged element type.
        /// </summary>
        /// <typeparam name="TElement">Expected unmanaged element type.</typeparam>
        /// <param name="parameterName">Optional parameter name used by the thrown exception.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when size, alignment, or type hash differs from <typeparamref name="TElement"/>.
        /// </exception>
        public void ValidateElementTypeOrThrow<TElement>(string parameterName = null)
            where TElement : unmanaged
        {
            var expected = Create<TElement>(Kind);

            if (ElementSize == expected.ElementSize &&
                ElementAlignment == expected.ElementAlignment &&
                ElementTypeHash == expected.ElementTypeHash)
            {
                return;
            }

            throw new ArgumentException(
                $"Storage format '{this}' is not compatible with element type '{typeof(TElement).FullName}'.",
                parameterName ?? nameof(StorageFormat));
        }

        /// <summary>
        /// Determines whether this format is equal to another format.
        /// </summary>
        /// <param name="other">The format to compare with this format.</param>
        /// <returns><c>true</c> when all storage format fields match; otherwise, <c>false</c>.</returns>
        public bool Equals(StorageFormat other)
        {
            return Kind == other.Kind &&
                   ElementSize == other.ElementSize &&
                   ElementAlignment == other.ElementAlignment &&
                   ElementTypeHash == other.ElementTypeHash;
        }

        /// <summary>
        /// Determines whether this format is equal to an object instance.
        /// </summary>
        /// <param name="obj">The object to compare with this format.</param>
        /// <returns>
        /// <c>true</c> when <paramref name="obj"/> is a <see cref="StorageFormat"/> with matching fields.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is StorageFormat other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code suitable for managed hash containers.
        /// </summary>
        /// <returns>A managed hash code for this storage format.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ ElementSize;
                hash = (hash * 397) ^ ElementAlignment;
                hash = (hash * 397) ^ ElementTypeHash.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this storage format.
        /// </summary>
        /// <returns>A string containing the storage kind, element size, alignment, and type hash.</returns>
        public override string ToString()
        {
            return $"{Kind}(Size={ElementSize}, Align={ElementAlignment}, TypeHash=0x{ElementTypeHash:X16})";
        }

        /// <summary>
        /// Determines whether two storage formats are equal.
        /// </summary>
        /// <param name="left">The first format.</param>
        /// <param name="right">The second format.</param>
        /// <returns><c>true</c> when all storage format fields match; otherwise, <c>false</c>.</returns>
        public static bool operator ==(StorageFormat left, StorageFormat right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two storage formats are not equal.
        /// </summary>
        /// <param name="left">The first format.</param>
        /// <param name="right">The second format.</param>
        /// <returns><c>true</c> when any storage format field differs; otherwise, <c>false</c>.</returns>
        public static bool operator !=(StorageFormat left, StorageFormat right)
        {
            return !left.Equals(right);
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct ElementPair<TKey, TValue>
            where TKey : unmanaged
            where TValue : unmanaged
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public static int Size
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => UnsafeUtility.SizeOf<ElementPair<TKey, TValue>>();
            }

            public static int Alignment
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => UnsafeUtility.AlignOf<ElementPair<TKey, TValue>>();
            }

            public static ulong TypeHash
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => AtlasTypeHash.Of<ElementPair<TKey, TValue>>();
            }
        }
    }
}