// Packages/com.lokrain.atlas/Runtime/Operations/AtlasOperationDefinition.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Operations
//
// Purpose
// - Define one durable Atlas operation contract.
// - Preserve operation identity, semantic role, diagnostic name, and symbolic field access.
// - Keep operation definitions independent from executors, jobs, native memory, and runtime storage.
//
// Design notes
// - OperationId is durable identity.
// - Role is semantic classification.
// - DebugName is diagnostic ABI metadata.
// - Access declaration order is operation ABI and must not be sorted.
// - WriteCoverage is required for write-capable bindings.
// - Jobs must not receive this type.

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
        /// Semantic operation role used by validation, diagnostics, tooling, and policy.
        /// </summary>
        public readonly AtlasOperationRole Role;

        /// <summary>
        /// Stable diagnostic name used by validation reports, editor tooling, generated docs, and tests.
        /// </summary>
        public readonly FixedString64Bytes DebugName;

        private AtlasOperationDefinition(
            AtlasOperationId operationId,
            AtlasOperationRole role,
            FixedString64Bytes debugName,
            AtlasOperationAccess[] accesses)
        {
            OperationId = operationId;
            Role = role;
            DebugName = debugName;

            ValidateHeaderOrThrow(
                operationId,
                role,
                debugName);

            _accesses = CopyAndValidateAccesses(accesses);
            _accessIndexByFieldId = BuildFieldLookup(_accesses);
            _accessIndexByBindingName = BuildBindingNameLookup(_accesses);
        }

        /// <summary>
        /// Gets the number of field access declarations.
        /// </summary>
        public int Count => _accesses.Length;

        /// <summary>
        /// Gets whether this operation declares no field access.
        /// </summary>
        public bool IsEmpty => _accesses.Length == 0;

        /// <summary>
        /// Gets whether this operation reads at least one content binding.
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
        /// Gets whether this operation writes, appends, consumes, or mutates at least one content binding.
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
        /// Gets whether this operation fully writes at least one logical content binding.
        /// </summary>
        public bool WritesFullLogicalContent
        {
            get
            {
                for (var i = 0; i < _accesses.Length; i++)
                {
                    if (_accesses[i].WritesFullLogicalContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this operation declares at least one partial content write.
        /// </summary>
        public bool HasPartialContentWrite
        {
            get
            {
                for (var i = 0; i < _accesses.Length; i++)
                {
                    if (_accesses[i].WritesPartialContent)
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
        public AtlasOperationAccess this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _accesses[index];
            }
        }

        /// <summary>
        /// Creates an operation definition from explicitly ordered field access declarations.
        /// </summary>
        public static AtlasOperationDefinition Create(
            AtlasOperationId operationId,
            AtlasOperationRole role,
            FixedString64Bytes debugName,
            params AtlasOperationAccess[] accesses)
        {
            return new AtlasOperationDefinition(
                operationId,
                role,
                debugName,
                accesses);
        }

        /// <summary>
        /// Determines whether this operation declares access to the supplied field identifier.
        /// </summary>
        public bool ContainsField(StableDataId fieldId)
        {
            return fieldId.IsValid &&
                   _accessIndexByFieldId.ContainsKey(fieldId);
        }

        /// <summary>
        /// Determines whether this operation declares an access binding with the supplied binding name.
        /// </summary>
        public bool ContainsBinding(FixedString64Bytes bindingName)
        {
            return !bindingName.IsEmpty &&
                   _accessIndexByBindingName.ContainsKey(bindingName);
        }

        /// <summary>
        /// Attempts to resolve a field identifier to its operation access declaration.
        /// </summary>
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
        /// Resolves a field identifier to its operation access declaration.
        /// </summary>
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

            Array.Copy(
                _accesses,
                destination,
                _accesses.Length);
        }

        /// <summary>
        /// Creates a managed copy of operation access declarations in canonical binding order.
        /// </summary>
        public AtlasOperationAccess[] ToArray()
        {
            var copy = new AtlasOperationAccess[_accesses.Length];

            Array.Copy(
                _accesses,
                copy,
                _accesses.Length);

            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over access declarations in operation binding order.
        /// </summary>
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
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this operation definition.
        /// </summary>
        public override string ToString()
        {
            return $"AtlasOperationDefinition(Name={DebugName}, Id={OperationId}, Role={Role}, Accesses={Count})";
        }

        private static void ValidateHeaderOrThrow(
            AtlasOperationId operationId,
            AtlasOperationRole role,
            FixedString64Bytes debugName)
        {
            operationId.ValidateOrThrow(nameof(operationId));

            if (role == AtlasOperationRole.None)
            {
                throw new ArgumentException(
                    "Atlas operation definition must declare a concrete semantic role.",
                    nameof(role));
            }

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
                    "Use a single access declaration with the correct access mode and write coverage instead of duplicate bindings.");
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
                    if (access.WriteCoverage != AtlasWriteCoverage.None)
                    {
                        throw new ArgumentException(
                            $"Atlas operation access '{access.BindingName}' declares write coverage '{access.WriteCoverage}' but does not write content.");
                    }

                    continue;
                }

                if (!access.WriteCoverage.IsConcreteWriteCoverage())
                {
                    throw new ArgumentException(
                        $"Atlas operation access '{access.BindingName}' writes Field '{access.FieldId}' without concrete write coverage.");
                }

                if (access.Mode == AtlasOperationAccessMode.Write &&
                    access.WriteCoverage.MakesFullLogicalContentAvailable() &&
                    !access.Flags.HasAny(AtlasOperationAccessFlags.DiscardBeforeWrite))
                {
                    throw new ArgumentException(
                        $"Atlas operation access '{access.BindingName}' fully writes Field '{access.FieldId}' without declaring discard-before-write.");
                }

                if (access.Mode == AtlasOperationAccessMode.ReadWrite &&
                    !access.Flags.HasAny(AtlasOperationAccessFlags.PreserveExistingContent))
                {
                    throw new ArgumentException(
                        $"Atlas operation access '{access.BindingName}' uses read-write access without preserving existing content.");
                }
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