// Runtime/Diagnostics/AtlasTypeHash.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Diagnostics
//
// Purpose
// - Provide deterministic diagnostic/runtime hashes for unmanaged CLR element types.
// - Support storage-format compatibility checks without relying on runtime string hash codes.
// - Keep type hashing separate from durable Atlas contract identity.
//
// Design notes
// - This is not durable field identity.
// - Durable field identity belongs to StableDataId and production catalogs.
// - Type hash zero is valid.
// - Do not treat zero as invalid.
// - Do not use string.GetHashCode or System.HashCode for deterministic metadata.
// - This hash is intended for diagnostics, validation, tests, and local compatibility checks.
// - It is not a cryptographic hash.
// - It is not an artifact hash.
// - It is not a stable migration identity across deliberate type or assembly changes.
// - This code may allocate when reading managed Type metadata. It is not a hot-loop/job API.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lokrain.Atlas.Diagnostics
{
    /// <summary>
    /// Provides deterministic diagnostic hashes for CLR types used by Atlas metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasTypeHash"/> exists to make runtime metadata checks deterministic. It is useful
    /// for verifying that a resolved storage format still matches the unmanaged element type expected
    /// by an operation, contract, storage allocator, or test.
    /// </para>
    ///
    /// <para>
    /// This hash must not be treated as durable field identity. Durable field identity is represented
    /// by stable Atlas ids such as <c>StableDataId</c>, <c>AtlasOperationId</c>, <c>AtlasStageId</c>,
    /// and <c>AtlasPipelineId</c>.
    /// </para>
    ///
    /// <para>
    /// The hash is based on deterministic FNV-1a folding over stable diagnostic type text. It does
    /// not use <see cref="string.GetHashCode"/> or <see cref="HashCode"/> because those APIs are not
    /// appropriate for deterministic package metadata.
    /// </para>
    ///
    /// <para>
    /// The returned value may theoretically be zero. Zero is a valid hash value and must not be used
    /// as an invalid sentinel.
    /// </para>
    /// </remarks>
    public static class AtlasTypeHash
    {
        private const ulong FnvOffsetBasis64 = 14695981039346656037UL;
        private const ulong FnvPrime64 = 1099511628211UL;

        private const byte TypePrefix = 0x54;
        private const byte AssemblyPrefix = 0x41;
        private const byte Separator = 0x1F;
        private const byte NullMarker = 0x00;

        /// <summary>
        /// Computes a deterministic diagnostic hash for an unmanaged type.
        /// </summary>
        /// <typeparam name="T">The unmanaged type to hash.</typeparam>
        /// <returns>A deterministic 64-bit diagnostic type hash.</returns>
        /// <remarks>
        /// This method uses managed type metadata and is intended for setup, validation, diagnostics,
        /// editor tooling, and tests. Do not call it from Burst jobs or hot loops.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Of<T>()
            where T : unmanaged
        {
            return Of(typeof(T));
        }

        /// <summary>
        /// Computes a deterministic diagnostic hash for a CLR type.
        /// </summary>
        /// <param name="type">The CLR type to hash.</param>
        /// <returns>A deterministic 64-bit diagnostic type hash.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <para>
        /// This method intentionally hashes text derived from managed type metadata. It is not a
        /// runtime job API and should not be used in performance-sensitive generation kernels.
        /// </para>
        ///
        /// <para>
        /// Assembly simple name is included to reduce collisions between identically named types
        /// from different assemblies. Assembly version is intentionally not included because package
        /// and Unity assembly version metadata may change for reasons unrelated to element layout.
        /// </para>
        /// </remarks>
        public static ulong Of(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var hash = FnvOffsetBasis64;

            AppendByte(ref hash, TypePrefix);
            AppendTypeName(ref hash, type);
            AppendByte(ref hash, Separator);
            AppendByte(ref hash, AssemblyPrefix);
            AppendAssemblyName(ref hash, type);

            return hash;
        }

        /// <summary>
        /// Appends a deterministic type hash into an existing FNV-1a accumulator.
        /// </summary>
        /// <typeparam name="T">The unmanaged type to hash.</typeparam>
        /// <param name="hash">The accumulator to update.</param>
        /// <remarks>
        /// This is useful when building composite diagnostic hashes without allocating intermediate
        /// hash containers. It is still setup/validation code, not a hot-loop job API.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Append<T>(ref ulong hash)
            where T : unmanaged
        {
            Append(ref hash, typeof(T));
        }

        /// <summary>
        /// Appends a deterministic type hash into an existing FNV-1a accumulator.
        /// </summary>
        /// <param name="hash">The accumulator to update.</param>
        /// <param name="type">The CLR type to hash.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <c>null</c>.</exception>
        public static void Append(ref ulong hash, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            AppendByte(ref hash, TypePrefix);
            AppendTypeName(ref hash, type);
            AppendByte(ref hash, Separator);
            AppendByte(ref hash, AssemblyPrefix);
            AppendAssemblyName(ref hash, type);
        }

        /// <summary>
        /// Returns a deterministic diagnostic text representation of a type hash.
        /// </summary>
        /// <param name="hash">The hash to format. Zero is valid.</param>
        /// <returns>A stable hexadecimal diagnostic string.</returns>
        public static string Format(ulong hash)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "0x{0:X16}",
                hash);
        }

        private static void AppendTypeName(ref ulong hash, Type type)
        {
            var fullName = type.FullName;

            if (!string.IsNullOrEmpty(fullName))
            {
                AppendString(ref hash, fullName);
                return;
            }

            var name = type.Name;

            if (!string.IsNullOrEmpty(name))
            {
                AppendString(ref hash, name);
                return;
            }

            AppendByte(ref hash, NullMarker);
        }

        private static void AppendAssemblyName(ref ulong hash, Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;

            if (!string.IsNullOrEmpty(assemblyName))
            {
                AppendString(ref hash, assemblyName);
                return;
            }

            AppendByte(ref hash, NullMarker);
        }

        private static void AppendString(ref ulong hash, string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];

                AppendByte(ref hash, (byte)character);
                AppendByte(ref hash, (byte)(character >> 8));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendByte(ref ulong hash, byte value)
        {
            hash ^= value;
            hash *= FnvPrime64;
        }
    }
}