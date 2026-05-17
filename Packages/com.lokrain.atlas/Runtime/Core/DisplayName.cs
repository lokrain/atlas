#nullable enable

using System;
using System.Globalization;
using System.Text;

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Represents validated user-facing text used for editor UI, diagnostics, and documentation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A display name is metadata. It is not identity, not a lookup key, not a symbol, and not part of
    /// deterministic generation semantics.
    /// </para>
    /// <para>
    /// Leading and trailing ordinary spaces are removed during creation. Empty, whitespace-only, control,
    /// formatting, private-use, unassigned, line-separator, paragraph-separator, surrogate, and non-space
    /// whitespace characters are rejected.
    /// </para>
    /// <para>
    /// Accepted display names are normalized to Unicode normalization form C.
    /// </para>
    /// <para>
    /// A non-null <see cref="DisplayName"/> instance is always valid.
    /// </para>
    /// </remarks>
    public sealed class DisplayName : IEquatable<DisplayName>
    {
        /// <summary>
        /// The maximum number of UTF-16 code units allowed in a display name.
        /// </summary>
        public const int MaxLength = 128;

        private DisplayName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the normalized display name value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Creates a validated display name.
        /// </summary>
        /// <param name="value">The display name text.</param>
        /// <returns>A validated display name.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is null, empty after trimming ordinary spaces, too long,
        /// or contains unsupported characters.
        /// </exception>
        public static DisplayName Create(string? value)
        {
            if (!TryNormalize(value, out string? normalizedValue, out string? validationError))
            {
                throw new ArgumentException(validationError, nameof(value));
            }

            return new DisplayName(normalizedValue!);
        }

        /// <summary>
        /// Attempts to create a validated display name.
        /// </summary>
        /// <param name="value">The display name text.</param>
        /// <param name="displayName">The created display name when validation succeeds.</param>
        /// <returns><see langword="true"/> when the display name is valid; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(string? value, out DisplayName? displayName)
        {
            if (!TryNormalize(value, out string? normalizedValue, out _))
            {
                displayName = null;
                return false;
            }

            displayName = new DisplayName(normalizedValue!);
            return true;
        }

        /// <summary>
        /// Determines whether the specified text can create a valid display name.
        /// </summary>
        /// <param name="value">The display name text.</param>
        /// <returns><see langword="true"/> when the text can create a valid display name; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(string? value)
        {
            return TryNormalize(value, out _, out _);
        }

        /// <inheritdoc/>
        public bool Equals(DisplayName? other)
        {
            return other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is DisplayName other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Determines whether two display names are equal.
        /// </summary>
        public static bool operator ==(DisplayName? left, DisplayName? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two display names are not equal.
        /// </summary>
        public static bool operator !=(DisplayName? left, DisplayName? right)
        {
            return !Equals(left, right);
        }

        private static bool TryNormalize(
            string? value,
            out string? normalizedValue,
            out string? validationError)
        {
            normalizedValue = null;
            validationError = null;

            if (value is null)
            {
                validationError = "Display name cannot be null.";
                return false;
            }

            string trimmedValue = value.Trim(' ');

            if (trimmedValue.Length == 0)
            {
                validationError = "Display name cannot be empty or ordinary-space-only.";
                return false;
            }

            string formCValue = trimmedValue.Normalize(NormalizationForm.FormC);

            if (formCValue.Length > MaxLength)
            {
                validationError = $"Display name cannot exceed {MaxLength} UTF-16 code units.";
                return false;
            }

            for (int index = 0; index < formCValue.Length; index++)
            {
                char character = formCValue[index];

                if (char.IsSurrogate(character))
                {
                    validationError = "Display name cannot contain surrogate characters.";
                    return false;
                }

                if (char.IsWhiteSpace(character) && character != ' ')
                {
                    validationError = "Display name cannot contain whitespace characters other than ordinary spaces.";
                    return false;
                }

                if (!IsAllowedCharacter(character))
                {
                    validationError = "Display name cannot contain control, formatting, private-use, unassigned, line-separator, or paragraph-separator characters.";
                    return false;
                }
            }

            normalizedValue = formCValue;
            return true;
        }

        private static bool IsAllowedCharacter(char character)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            return category != UnicodeCategory.Control
                && category != UnicodeCategory.Format
                && category != UnicodeCategory.PrivateUse
                && category != UnicodeCategory.OtherNotAssigned
                && category != UnicodeCategory.LineSeparator
                && category != UnicodeCategory.ParagraphSeparator;
        }
    }
}