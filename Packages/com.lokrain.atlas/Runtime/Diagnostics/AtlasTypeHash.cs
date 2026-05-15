// Runtime/Diagnostics/AtlasTypeHash.cs

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Provides deterministic-ish type fingerprints for Atlas Contract validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Atlas type hashes are validation fingerprints, not durable Field identity.
    /// Durable identity belongs to <see cref="StableDataId"/>. Type hashes help detect
    /// accidental Contract, storage, and binding mismatches where the same Field slot
    /// is interpreted as a different unmanaged element type.
    /// </para>
    ///
    /// <para>
    /// The hash input includes the element type's assembly-qualified name, unmanaged size,
    /// and unmanaged alignment. Renaming or moving a type may change this hash. That is
    /// intentional: type hashes are a safety check for the current build, not a migration key.
    /// </para>
    ///
    /// <para>
    /// Do not use this type for replay hashes, network protocol hashes, save-game identity,
    /// content hashes, or cross-version compatibility decisions.
    /// </para>
    /// </remarks>
    public static class AtlasTypeHash
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        /// <summary>
        /// Computes the Atlas validation hash for an unmanaged type.
        /// </summary>
        /// <typeparam name="T">Unmanaged type to fingerprint.</typeparam>
        /// <returns>
        /// A non-zero 64-bit validation hash for the current build's representation of
        /// <typeparamref name="T"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Of<T>()
            where T : unmanaged
        {
            return Cache<T>.Value;
        }

        /// <summary>
        /// Computes the Atlas validation hash for a runtime type and explicit unmanaged layout.
        /// </summary>
        /// <param name="type">Type to fingerprint.</param>
        /// <param name="size">Unmanaged size in bytes.</param>
        /// <param name="alignment">Unmanaged alignment in bytes.</param>
        /// <returns>A non-zero 64-bit validation hash.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="type"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="size"/> or <paramref name="alignment"/> is not positive.
        /// </exception>
        /// <remarks>
        /// This overload is intended for diagnostics, editor tooling, tests, and validation
        /// paths that already have explicit layout information. Generic code should prefer
        /// <see cref="Of{T}"/>.
        /// </remarks>
        public static ulong Of(Type type, int size, int alignment)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    size,
                    "Type size must be positive.");
            }

            if (alignment <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(alignment),
                    alignment,
                    "Type alignment must be positive.");
            }

            var hash = OffsetBasis;

            hash = AddString(hash, type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
            hash = AddInt32(hash, size);
            hash = AddInt32(hash, alignment);

            return EnsureNonZero(Avalanche(hash));
        }

        private static class Cache<T>
            where T : unmanaged
        {
            public static readonly ulong Value = Of(
                typeof(T),
                UnsafeUtility.SizeOf<T>(),
                UnsafeUtility.AlignOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AddByte(ulong hash, byte value)
        {
            unchecked
            {
                hash ^= value;
                hash *= Prime;
                return hash;
            }
        }

        private static ulong AddString(ulong hash, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            for (var i = 0; i < bytes.Length; i++)
            {
                hash = AddByte(hash, bytes[i]);
            }

            return AddByte(hash, 0);
        }

        private static ulong AddInt32(ulong hash, int value)
        {
            unchecked
            {
                hash = AddByte(hash, (byte)value);
                hash = AddByte(hash, (byte)(value >> 8));
                hash = AddByte(hash, (byte)(value >> 16));
                hash = AddByte(hash, (byte)(value >> 24));

                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Avalanche(ulong value)
        {
            unchecked
            {
                value ^= value >> 33;
                value *= 0xff51afd7ed558ccdUL;
                value ^= value >> 33;
                value *= 0xc4ceb9fe1a85ec53UL;
                value ^= value >> 33;

                return value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EnsureNonZero(ulong value)
        {
            return value == 0UL ? 1UL : value;
        }
    }
}