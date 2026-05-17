#nullable enable

using System;
using System.Globalization;

namespace Lokrain.Atlas.Core.Map
{
    /// <summary>
    /// Represents a deterministic generation seed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A seed is a stable numeric input for deterministic generation. The value <c>0</c> is valid.
    /// </para>
    /// <para>
    /// Seed randomization belongs to request normalization, editor tooling, or caller code. This type does
    /// not create random seeds.
    /// </para>
    /// <para>
    /// Text seed derivation hashes UTF-16 code units with a package-owned stable hash. It does not use
    /// <see cref="string.GetHashCode"/>.
    /// </para>
    /// </remarks>
    public readonly struct Seed : IEquatable<Seed>
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private const ulong NumericSaltDomain = 0x243F6A8885A308D3UL;
        private const ulong TextSaltDomain = 0x13198A2E03707344UL;

        /// <summary>
        /// Initializes a new instance of the <see cref="Seed"/> struct.
        /// </summary>
        /// <param name="value">The seed value.</param>
        public Seed(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the zero seed.
        /// </summary>
        public static Seed Zero { get; } = new Seed(0UL);

        /// <summary>
        /// Gets the numeric seed value.
        /// </summary>
        public ulong Value { get; }

        /// <summary>
        /// Parses a seed from decimal text or hexadecimal text prefixed with <c>0x</c>.
        /// </summary>
        /// <param name="value">The seed text.</param>
        /// <returns>The parsed seed.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is null, empty, whitespace, or not a valid unsigned 64-bit integer.
        /// </exception>
        public static Seed Parse(string? value)
        {
            if (!TryParse(value, out Seed seed))
            {
                throw new ArgumentException(
                    "Seed must be a valid unsigned 64-bit integer in decimal form or hexadecimal form prefixed with '0x'.",
                    nameof(value));
            }

            return seed;
        }

        /// <summary>
        /// Attempts to parse a seed from decimal text or hexadecimal text prefixed with <c>0x</c>.
        /// </summary>
        /// <param name="value">The seed text.</param>
        /// <param name="seed">The parsed seed when parsing succeeds.</param>
        /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? value, out Seed seed)
        {
            seed = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalizedValue = value.Trim();

            bool isHexadecimal = normalizedValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            string numericValue = isHexadecimal ? normalizedValue.Substring(2) : normalizedValue;

            if (numericValue.Length == 0)
            {
                return false;
            }

            NumberStyles style = isHexadecimal ? NumberStyles.AllowHexSpecifier : NumberStyles.None;

            if (!ulong.TryParse(numericValue, style, CultureInfo.InvariantCulture, out ulong parsedValue))
            {
                return false;
            }

            seed = new Seed(parsedValue);
            return true;
        }

        /// <summary>
        /// Derives a deterministic child seed from this seed and the specified numeric salt.
        /// </summary>
        /// <param name="salt">The numeric derivation salt.</param>
        /// <returns>The derived seed.</returns>
        public Seed Derive(ulong salt)
        {
            return new Seed(Mix(Value ^ NumericSaltDomain ^ Mix(salt)));
        }

        /// <summary>
        /// Derives a deterministic child seed from this seed and the specified text salt.
        /// </summary>
        /// <param name="salt">The text derivation salt.</param>
        /// <returns>The derived seed.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="salt"/> is null.
        /// </exception>
        public Seed Derive(string salt)
        {
            if (salt is null)
            {
                throw new ArgumentNullException(nameof(salt));
            }

            return new Seed(Mix(Value ^ TextSaltDomain ^ GetStableHash(salt)));
        }

        /// <summary>
        /// Converts this seed to hexadecimal text prefixed with <c>0x</c>.
        /// </summary>
        /// <returns>The hexadecimal seed text.</returns>
        public string ToHexString()
        {
            return "0x" + Value.ToString("X16", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool Equals(Seed other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Seed other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines whether two seeds are equal.
        /// </summary>
        public static bool operator ==(Seed left, Seed right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two seeds are not equal.
        /// </summary>
        public static bool operator !=(Seed left, Seed right)
        {
            return !left.Equals(right);
        }

        private static ulong GetStableHash(string value)
        {
            ulong hash = OffsetBasis;

            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= Prime;
            }

            return hash;
        }

        private static ulong Mix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}