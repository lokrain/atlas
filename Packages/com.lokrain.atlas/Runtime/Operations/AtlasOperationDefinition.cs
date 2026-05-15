// Runtime/Operations/AtlasOperationDefinition.cs

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Unity.Collections;

namespace Lokrain.Atlas.Operations
{
    /// <summary>
    /// Defines one durable Atlas operation contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation definition declares stable operation identity, diagnostic name, and the
    /// symbolic Field access requirements needed by the operation. It does not contain executable
    /// code, job structs, delegates, native memory, or resolved Field slots.
    /// </para>
    ///
    /// <para>
    /// The compiler resolves <see cref="AtlasOperationAccess"/> declarations against an ordered
    /// Contract table and produces concrete execution bindings. Jobs should receive resolved typed
    /// memory and numeric parameters, never operation definitions or symbolic Field identities.
    /// </para>
    ///
    /// <para>
    /// Access order is preserved. The compiler may use declaration order to build deterministic
    /// binding tables, diagnostics, generated wrappers, and hash input. Do not sort operation
    /// access declarations after construction.
    /// </para>
    /// </remarks>
    public sealed class AtlasOperationDefinition :
        IReadOnlyList<AtlasOperationAccess>
    {
        private readonly AtlasOperationAccess[] _accesses;
        private readonly Dictionary<StableDataId, int> _accessIndexByFieldId;
        private readonly Dictionary<FixedString64Bytes, int> _accessIndexByBindingName;

        /// <summary>
        /// Stable, versioned identity of this operation contract.
        /// </summary>
        public readonly AtlasOperationId OperationId;

        /// <summary>
        /// Stable diagnostic name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        /// <remarks>
        /// Operation names are not durable identity. Durable identity belongs to <see cref="OperationId"/>.
        /// </remarks>
        public readonly FixedString64Bytes DebugName;

        private AtlasOperationDefinition(
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            AtlasOperationAccess[] accesses)
        {
            OperationId = operationId;
            DebugName = debugName;

            ValidateHeaderOrThrow(operationId, debugName);

            _accesses = CopyAndValidateAccesses(accesses);
            _accessIndexByFieldId = BuildFieldLookup(_accesses);
            _accessIndexByBindingName = BuildBindingNameLookup(_accesses);
        }

        /// <summary>
        /// Gets the number of Field access declarations in this operation.
        /// </summary>
        public int Count => _accesses.Length;

        /// <summary>
        /// Gets whether this operation declares no Field access.
        /// </summary>
        public bool IsEmpty => _accesses.Length == 0;

        /// <summary>
        /// Gets whether this operation reads at least one Field content binding.
        /// </summary>
        public bool ReadsContent
        {
            get
            {
                for (var i = 0; i < _accesses.Length; i++)
                {
                    if (_accesses[i].ReadsContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this operation writes, appends, consumes, or mutates at least one Field binding.
        /// </summary>
        public bool WritesContent
        {
            get
            {
                for (var i = 0; i < _accesses.Length; i++)
                {
                    if (_accesses[i].WritesContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this operation declares at least one optional binding.
        /// </summary>
        public bool HasOptionalAccess
        {
            get
            {
                for (var i = 0; i < _accesses.Length; i++)
                {
                    if (_accesses[i].IsOptional)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this operation declares at least one shape-only binding.
        /// </summary>
        public bool HasShapeOnlyAccess
        {
            get
            {
                for (var i = 0; i < _accesses.Length; i++)
                {
                    if (_accesses[i].IsShapeOnly)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the access declaration at a zero-based operation binding index.
        /// </summary>
        /// <param name="index">Zero-based access declaration index.</param>
        /// <returns>The access declaration at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this operation's access range.
        /// </exception>
        public AtlasOperationAccess this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _accesses[index];
            }
        }

        /// <summary>
        /// Creates an operation definition from explicitly ordered Field access declarations.
        /// </summary>
        /// <param name="operationId">Stable, versioned operation identity.</param>
        /// <param name="debugName">Stable diagnostic operation name.</param>
        /// <param name="accesses">Access declarations in canonical operation binding order.</param>
        /// <returns>A validated operation definition.</returns>
        public static AtlasOperationDefinition Create(
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            params AtlasOperationAccess[] accesses)
        {
            return new AtlasOperationDefinition(
                operationId,
                debugName,
                accesses);
        }

        /// <summary>
        /// Determines whether this operation declares access to the supplied Field identifier.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to search for.</param>
        /// <returns><c>true</c> when this operation declares access to the Field; otherwise, <c>false</c>.</returns>
        public bool ContainsField(StableDataId fieldId)
        {
            return fieldId.IsValid &&
                   _accessIndexByFieldId.ContainsKey(fieldId);
        }

        /// <summary>
        /// Determines whether this operation declares an access binding with the supplied binding name.
        /// </summary>
        /// <param name="bindingName">Binding name to search for.</param>
        /// <returns><c>true</c> when this operation contains the binding name; otherwise, <c>false</c>.</returns>
        public bool ContainsBinding(FixedString64Bytes bindingName)
        {
            return !bindingName.IsEmpty &&
                   _accessIndexByBindingName.ContainsKey(bindingName);
        }

        /// <summary>
        /// Attempts to resolve a Field identifier to its operation access declaration.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to resolve.</param>
        /// <param name="access">
        /// Resolved operation access declaration when present; otherwise, <see cref="AtlasOperationAccess.Empty"/>.
        /// </param>
        /// <returns><c>true</c> when the Field access declaration was found; otherwise, <c>false</c>.</returns>
        public bool TryGetAccess(
            StableDataId fieldId,
            out AtlasOperationAccess access)
        {
            if (fieldId.IsValid &&
                _accessIndexByFieldId.TryGetValue(fieldId, out var index))
            {
                access = _accesses[index];
                return true;
            }

            access = AtlasOperationAccess.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a binding name to its operation access declaration.
        /// </summary>
        /// <param name="bindingName">Binding name to resolve.</param>
        /// <param name="access">
        /// Resolved operation access declaration when present; otherwise, <see cref="AtlasOperationAccess.Empty"/>.
        /// </param>
        /// <returns><c>true</c> when the binding was found; otherwise, <c>false</c>.</returns>
        public bool TryGetAccess(
            FixedString64Bytes bindingName,
            out AtlasOperationAccess access)
        {
            if (!bindingName.IsEmpty &&
                _accessIndexByBindingName.TryGetValue(bindingName, out var index))
            {
                access = _accesses[index];
                return true;
            }

            access = AtlasOperationAccess.Empty;
            return false;
        }

        /// <summary>
        /// Resolves a Field identifier to its operation access declaration.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to resolve.</param>
        /// <returns>The operation access declaration for <paramref name="fieldId"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the Field identifier is invalid or not declared by this operation.
        /// </exception>
        public AtlasOperationAccess GetRequiredAccess(StableDataId fieldId)
        {
            if (TryGetAccess(fieldId, out var access))
            {
                return access;
            }

            throw new ArgumentException(
                $"Atlas operation '{DebugName}' does not declare access to Field id '{fieldId}'.",
                nameof(fieldId));
        }

        /// <summary>
        /// Resolves a binding name to its operation access declaration.
        /// </summary>
        /// <param name="bindingName">Binding name to resolve.</param>
        /// <returns>The operation access declaration for <paramref name="bindingName"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the binding name is empty or not declared by this operation.
        /// </exception>
        public AtlasOperationAccess GetRequiredAccess(FixedString64Bytes bindingName)
        {
            if (TryGetAccess(bindingName, out var access))
            {
                return access;
            }

            throw new ArgumentException(
                $"Atlas operation '{DebugName}' does not declare binding '{bindingName}'.",
                nameof(bindingName));
        }

        /// <summary>
        /// Copies access declarations into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving access declarations in operation order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is smaller than <see cref="Count"/>.
        /// </exception>
        public void CopyTo(AtlasOperationAccess[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _accesses.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than operation access count '{_accesses.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_accesses, destination, _accesses.Length);
        }

        /// <summary>
        /// Creates a managed copy of operation access declarations in canonical binding order.
        /// </summary>
        /// <returns>A new access declaration array.</returns>
        public AtlasOperationAccess[] ToArray()
        {
            var copy = new AtlasOperationAccess[_accesses.Length];
            Array.Copy(_accesses, copy, _accesses.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over access declarations in operation binding order.
        /// </summary>
        /// <returns>An enumerator over access declarations.</returns>
        public IEnumerator<AtlasOperationAccess> GetEnumerator()
        {
            for (var i = 0; i < _accesses.Length; i++)
            {
                yield return _accesses[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over access declarations in operation binding order.
        /// </summary>
        /// <returns>An enumerator over access declarations.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this operation definition.
        /// </summary>
        /// <returns>A string containing operation name, id, and access count.</returns>
        public override string ToString()
        {
            return $"AtlasOperationDefinition(Name={DebugName}, Id={OperationId}, Accesses={Count})";
        }

        private static void ValidateHeaderOrThrow(
            AtlasOperationId operationId,
            FixedString64Bytes debugName)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            if (!debugName.IsEmpty)
            {
                return;
            }

            throw new ArgumentException(
                "Atlas operation definition must declare a non-empty debug name.",
                nameof(debugName));
        }

        private static AtlasOperationAccess[] CopyAndValidateAccesses(AtlasOperationAccess[] accesses)
        {
            if (accesses == null)
            {
                throw new ArgumentNullException(nameof(accesses));
            }

            if (accesses.Length == 0)
            {
                throw new ArgumentException(
                    "Atlas operation definition must declare at least one Field access.",
                    nameof(accesses));
            }

            var copy = new AtlasOperationAccess[accesses.Length];

            for (var i = 0; i < accesses.Length; i++)
            {
                var access = accesses[i];
                access.ValidateOrThrow($"accesses[{i}]");
                copy[i] = access;
            }

            ValidateNoDuplicateFields(copy);
            ValidateNoDuplicateBindingNames(copy);
            ValidateNoAmbiguousWritePolicy(copy);

            return copy;
        }

        private static Dictionary<StableDataId, int> BuildFieldLookup(AtlasOperationAccess[] accesses)
        {
            var lookup = new Dictionary<StableDataId, int>(accesses.Length);

            for (var i = 0; i < accesses.Length; i++)
            {
                lookup.Add(accesses[i].FieldId, i);
            }

            return lookup;
        }

        private static Dictionary<FixedString64Bytes, int> BuildBindingNameLookup(AtlasOperationAccess[] accesses)
        {
            var lookup = new Dictionary<FixedString64Bytes, int>(accesses.Length);

            for (var i = 0; i < accesses.Length; i++)
            {
                lookup.Add(accesses[i].BindingName, i);
            }

            return lookup;
        }

        private static void ValidateNoDuplicateFields(AtlasOperationAccess[] accesses)
        {
            var seen = new HashSet<StableDataId>();

            for (var i = 0; i < accesses.Length; i++)
            {
                var access = accesses[i];

                if (seen.Add(access.FieldId))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas operation declares Field id '{access.FieldId}' more than once. " +
                    "Use a single access declaration with the correct access mode, such as ReadWrite, instead of duplicate bindings.");
            }
        }

        private static void ValidateNoDuplicateBindingNames(AtlasOperationAccess[] accesses)
        {
            var seen = new HashSet<FixedString64Bytes>();

            for (var i = 0; i < accesses.Length; i++)
            {
                var access = accesses[i];

                if (seen.Add(access.BindingName))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas operation declares duplicate binding name '{access.BindingName}'. " +
                    "Binding names are diagnostic, but duplicates make generated wrappers and validation reports ambiguous.");
            }
        }

        private static void ValidateNoAmbiguousWritePolicy(AtlasOperationAccess[] accesses)
        {
            for (var i = 0; i < accesses.Length; i++)
            {
                var access = accesses[i];

                if (!access.WritesContent)
                {
                    continue;
                }

                if (access.Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite) ||
                    access.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent) ||
                    access.Mode == AtlasOperationAccessMode.Append ||
                    access.Mode == AtlasOperationAccessMode.Consume)
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Atlas operation access '{access.BindingName}' writes Field '{access.FieldId}' without declaring write-content policy. " +
                    $"Use {nameof(AtlasOperationAccessFlags.DiscardBeforeWrite)} for full overwrite, " +
                    $"{nameof(AtlasOperationAccessFlags.PreserveExistingContent)} for read-modify-write, or an append/consume mode where appropriate.");
            }
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_accesses.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Operation access index must be between 0 and {_accesses.Length - 1}.");
        }
    }
}