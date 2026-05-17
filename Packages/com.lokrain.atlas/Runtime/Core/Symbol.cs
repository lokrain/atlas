#nullable enable

using System;

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Represents a validated stable machine-facing token used by catalog, request, and planning code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A symbol is a syntax primitive for stable authored and compiled vocabulary. It is not identity,
    /// not kind, not display metadata, and not a numeric runtime identifier.
    /// </para>
    /// <para>
    /// A symbol consists of one or more dot-separated segments. Each segment starts with a lowercase
    /// ASCII letter, ends with a lowercase ASCII letter or digit, and may contain lowercase ASCII letters,
    /// digits, underscores, or hyphens between those boundaries.
    /// </para>
    /// <para>
    /// A non-null <see cref="Symbol"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class Symbol : IEquatable<Symbol>, IComparable<Symbol>
    {
        /// <summary>
        /// The maximum number of characters allowed in a symbol.
        /// </summary>
        public const int MaxLength = 128;

        private Symbol(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the validated symbol value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Creates a validated symbol.
        /// </summary>
        /// <param name="value">The symbol text.</param>
        /// <returns>A validated symbol.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is null, empty, too long, or does not match the symbol grammar.
        /// </exception>
        public static Symbol Create(string? value)
        {
            string? validationError = GetValidationError(value);

            if (validationError is not null)
            {
                throw new ArgumentException(validationError, nameof(value));
            }

            return new Symbol(value!);
        }

        /// <summary>
        /// Attempts to create a validated symbol.
        /// </summary>
        /// <param name="value">The symbol text.</param>
        /// <param name="symbol">The created symbol when validation succeeds.</param>
        /// <returns><see langword="true"/> when the symbol is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(string? value, out Symbol? symbol)
        {
            if (GetValidationError(value) is not null)
            {
                symbol = null;
                return false;
            }

            symbol = new Symbol(value!);
            return true;
        }

        /// <summary>
        /// Determines whether the specified text is a valid symbol.
        /// </summary>
        /// <param name="value">The symbol text.</param>
        /// <returns><see langword="true"/> when the text is a valid symbol; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(string? value)
        {
            return GetValidationError(value) is null;
        }

        /// <inheritdoc/>
        public bool Equals(Symbol? other)
        {
            return other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Symbol other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <inheritdoc/>
        public int CompareTo(Symbol? other)
        {
            return other is null
                ? 1
                : string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Determines whether two symbols are equal.
        /// </summary>
        public static bool operator ==(Symbol? left, Symbol? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two symbols are not equal.
        /// </summary>
        public static bool operator !=(Symbol? left, Symbol? right)
        {
            return !Equals(left, right);
        }

        private static string? GetValidationError(string? value)
        {
            if (value is null)
            {
                return "Symbol cannot be null.";
            }

            if (value.Length == 0)
            {
                return "Symbol cannot be empty.";
            }

            if (value.Length > MaxLength)
            {
                return $"Symbol cannot exceed {MaxLength} characters.";
            }

            if (!IsLowercaseAsciiLetter(value[0]))
            {
                return "Symbol must start with a lowercase ASCII letter.";
            }

            char previousCharacter = value[0];

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];

                if (character == '.')
                {
                    if (!IsAsciiLetterOrDigit(previousCharacter))
                    {
                        return "Symbol segments must end with a lowercase ASCII letter or digit.";
                    }

                    if (index == value.Length - 1)
                    {
                        return "Symbol cannot end with '.'.";
                    }

                    char nextCharacter = value[index + 1];

                    if (!IsLowercaseAsciiLetter(nextCharacter))
                    {
                        return "Each symbol segment must start with a lowercase ASCII letter.";
                    }

                    previousCharacter = character;
                    continue;
                }

                if (!IsSegmentCharacter(character))
                {
                    return "Symbol may contain only lowercase ASCII letters, digits, '.', '_', or '-'.";
                }

                previousCharacter = character;
            }

            if (!IsAsciiLetterOrDigit(previousCharacter))
            {
                return "Symbol must end with a lowercase ASCII letter or digit.";
            }

            return null;
        }

        private static bool IsSegmentCharacter(char character)
        {
            return IsAsciiLetterOrDigit(character)
                || character == '_'
                || character == '-';
        }

        private static bool IsAsciiLetterOrDigit(char character)
        {
            return IsLowercaseAsciiLetter(character) || IsAsciiDigit(character);
        }

        private static bool IsLowercaseAsciiLetter(char character)
        {
            return character is >= 'a' and <= 'z';
        }

        private static bool IsAsciiDigit(char character)
        {
            return character is >= '0' and <= '9';
        }
    }
}