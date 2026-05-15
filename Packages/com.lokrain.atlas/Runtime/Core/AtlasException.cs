// Runtime/Core/AtlasException.cs

using System;
using System.Runtime.Serialization;

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Base exception type for unrecoverable Atlas runtime failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Atlas validation APIs should prefer structured validation reports when callers are
    /// expected to inspect and recover from errors. This exception is intended for invalid
    /// package usage, failed invariants, corrupted Contract state, invalid storage access,
    /// and other failures where continuing would hide a programming error.
    /// </para>
    ///
    /// <para>
    /// Argument validation should still use standard .NET exception types such as
    /// <see cref="ArgumentNullException"/>, <see cref="ArgumentOutOfRangeException"/>,
    /// and <see cref="ArgumentException"/> when the failure is local to a method argument.
    /// </para>
    /// </remarks>
    [Serializable]
    public class AtlasException : Exception
    {
        /// <summary>
        /// Creates a new Atlas exception with the default diagnostic message.
        /// </summary>
        public AtlasException()
            : base("An Atlas runtime failure occurred.")
        {
        }

        /// <summary>
        /// Creates a new Atlas exception with a diagnostic message.
        /// </summary>
        /// <param name="message">Human-readable diagnostic message describing the failure.</param>
        public AtlasException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new Atlas exception with a diagnostic message and causal exception.
        /// </summary>
        /// <param name="message">Human-readable diagnostic message describing the failure.</param>
        /// <param name="innerException">The exception that caused this Atlas failure.</param>
        public AtlasException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Restores an Atlas exception from serialized exception state.
        /// </summary>
        /// <param name="info">Serialized exception data.</param>
        /// <param name="context">Serialization context.</param>
        protected AtlasException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}