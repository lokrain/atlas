// Runtime/Compilation/AtlasCompiledOperation.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent one operation occurrence after symbolic Field access has been resolved.
// - Preserve operation occurrence index and operation binding order.
// - Bind a durable operation contract to compiled bindings resolved against an Atlas Contract table.
// - Keep compiled operation metadata separate from concrete job schedulers and native memory.
//
// Design notes
// - This is compilation metadata, not runtime job payload.
// - Repeated operation definitions are valid at stage and pipeline level; occurrence index is therefore essential.
// - Binding order is preserved exactly from AtlasOperationDefinition.
// - Optional bindings may be missing while still occupying their original binding index.
// - This type deliberately uses immutable arrays and linear scans instead of dictionaries.

using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Atlas.Contracts;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Unity.Collections;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Compiled representation of one operation occurrence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An operation definition is a durable contract. An operation occurrence is one concrete
    /// use of that contract in an authored stage or pipeline sequence. Because the same operation
    /// definition may appear multiple times, the occurrence index is part of compiled identity.
    /// </para>
    ///
    /// <para>
    /// This type preserves the operation-local binding order and stores one
    /// <see cref="AtlasCompiledBinding"/> for each source <see cref="AtlasOperationAccess"/>.
    /// Missing optional Fields remain represented as bindings so generated diagnostics, wrappers,
    /// compiled operation hashes, and executor binding tables keep stable indexes.
    /// </para>
    ///
    /// <para>
    /// This type does not own native memory and does not schedule jobs. Later compilation and
    /// execution layers consume compiled bindings to produce concrete scheduler payloads and
    /// workspace memory views.
    /// </para>
    /// </remarks>
    public sealed class AtlasCompiledOperation :
        IReadOnlyList<AtlasCompiledBinding>
    {
        private const int InvalidBindingIndex = -1;

        private readonly AtlasCompiledBinding[] _bindings;

        /// <summary>
        /// Zero-based operation occurrence index inside the source stage or flattened parent sequence.
        /// </summary>
        /// <remarks>
        /// This is not durable operation identity. Durable operation identity belongs to
        /// <see cref="OperationId"/>. The occurrence index distinguishes repeated uses of the same
        /// operation contract.
        /// </remarks>
        public readonly int OperationIndex;

        /// <summary>
        /// Stable, versioned identity of the source operation contract.
        /// </summary>
        public readonly AtlasOperationId OperationId;

        /// <summary>
        /// Stable diagnostic operation name from the source operation definition.
        /// </summary>
        public readonly FixedString64Bytes DebugName;

        private AtlasCompiledOperation(
            int operationIndex,
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            AtlasCompiledBinding[] bindings)
        {
            OperationIndex = operationIndex;
            OperationId = operationId;
            DebugName = debugName;
            _bindings = CopyAndValidateBindings(
                operationIndex,
                operationId,
                debugName,
                bindings);
        }

        /// <summary>
        /// Gets the number of compiled bindings in this operation occurrence.
        /// </summary>
        public int Count => _bindings.Length;

        /// <summary>
        /// Gets whether this compiled operation has no bindings.
        /// </summary>
        /// <remarks>
        /// Concrete compiled operations are required to contain at least one binding, so this
        /// property normally returns <c>false</c>.
        /// </remarks>
        public bool IsEmpty => _bindings.Length == 0;

        /// <summary>
        /// Gets whether at least one present binding reads Field contents.
        /// </summary>
        public bool ReadsContent
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].ReadsContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one present binding writes, appends, consumes, or mutates Field contents.
        /// </summary>
        public bool WritesContent
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].WritesContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one source binding declares a content read, regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentRead
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].DeclaresContentRead)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether at least one source binding declares a content write, regardless of optional resolution.
        /// </summary>
        public bool DeclaresContentWrite
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].DeclaresContentWrite)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled operation contains at least one optional binding.
        /// </summary>
        public bool HasOptionalBinding
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].IsOptional)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled operation contains at least one missing optional binding.
        /// </summary>
        public bool HasMissingOptionalBinding
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].IsMissingOptional)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled operation contains at least one shape-only binding.
        /// </summary>
        public bool HasShapeOnlyBinding
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].IsShapeOnly)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether this compiled operation has at least one present binding that requires content memory.
        /// </summary>
        public bool RequiresContentMemory
        {
            get
            {
                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].RequiresContentMemory)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the number of bindings that resolved to concrete Field Contracts.
        /// </summary>
        public int PresentBindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].IsPresent)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the number of bindings that are absent optional bindings.
        /// </summary>
        public int MissingOptionalBindingCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _bindings.Length; i++)
                {
                    if (_bindings[i].IsMissingOptional)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the compiled binding at a zero-based operation-local binding index.
        /// </summary>
        /// <param name="index">Zero-based compiled binding index.</param>
        /// <returns>The compiled binding at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside this operation's binding range.
        /// </exception>
        public AtlasCompiledBinding this[int index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                return _bindings[index];
            }
        }

        /// <summary>
        /// Compiles one operation occurrence by resolving all access declarations against a Contract table.
        /// </summary>
        /// <param name="operationIndex">Zero-based operation occurrence index.</param>
        /// <param name="operation">Source operation definition.</param>
        /// <param name="contracts">Contract table used for Field resolution.</param>
        /// <returns>A compiled operation occurrence with resolved bindings.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="operation"/> or <paramref name="contracts"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the operation occurrence cannot be compiled.
        /// </exception>
        public static AtlasCompiledOperation Compile(
            int operationIndex,
            AtlasOperationDefinition operation,
            AtlasContractTable contracts)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            ValidateOperationHeaderOrThrow(
                operationIndex,
                operation.OperationId,
                operation.DebugName,
                operation.Count);

            var bindings = new AtlasCompiledBinding[operation.Count];

            for (var i = 0; i < operation.Count; i++)
            {
                bindings[i] = AtlasCompiledBinding.Resolve(
                    i,
                    operation[i],
                    contracts);
            }

            return new AtlasCompiledOperation(
                operationIndex,
                operation.OperationId,
                operation.DebugName,
                bindings);
        }

        /// <summary>
        /// Creates a compiled operation from already resolved bindings.
        /// </summary>
        /// <param name="operationIndex">Zero-based operation occurrence index.</param>
        /// <param name="operationId">Stable, versioned source operation identity.</param>
        /// <param name="debugName">Stable diagnostic operation name.</param>
        /// <param name="bindings">Compiled bindings in operation-local binding order.</param>
        /// <returns>A validated compiled operation occurrence.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="bindings"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the header or bindings are invalid.
        /// </exception>
        public static AtlasCompiledOperation Create(
            int operationIndex,
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            params AtlasCompiledBinding[] bindings)
        {
            return new AtlasCompiledOperation(
                operationIndex,
                operationId,
                debugName,
                bindings);
        }

        /// <summary>
        /// Determines whether this compiled operation contains a binding for the supplied Field id.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to search for.</param>
        /// <returns><c>true</c> when a matching binding exists; otherwise, <c>false</c>.</returns>
        public bool ContainsField(StableDataId fieldId)
        {
            return IndexOf(fieldId) != InvalidBindingIndex;
        }

        /// <summary>
        /// Determines whether this compiled operation contains a binding with the supplied binding name.
        /// </summary>
        /// <param name="bindingName">Diagnostic binding name to search for.</param>
        /// <returns><c>true</c> when a matching binding exists; otherwise, <c>false</c>.</returns>
        public bool ContainsBinding(FixedString64Bytes bindingName)
        {
            return IndexOf(bindingName) != InvalidBindingIndex;
        }

        /// <summary>
        /// Returns the operation-local binding index for the supplied Field id.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to search for.</param>
        /// <returns>The matching binding index, or <c>-1</c> when absent.</returns>
        public int IndexOf(StableDataId fieldId)
        {
            if (!fieldId.IsValid)
            {
                return InvalidBindingIndex;
            }

            for (var i = 0; i < _bindings.Length; i++)
            {
                if (_bindings[i].FieldId == fieldId)
                {
                    return i;
                }
            }

            return InvalidBindingIndex;
        }

        /// <summary>
        /// Returns the operation-local binding index for the supplied binding name.
        /// </summary>
        /// <param name="bindingName">Diagnostic binding name to search for.</param>
        /// <returns>The matching binding index, or <c>-1</c> when absent.</returns>
        public int IndexOf(FixedString64Bytes bindingName)
        {
            if (bindingName.IsEmpty)
            {
                return InvalidBindingIndex;
            }

            for (var i = 0; i < _bindings.Length; i++)
            {
                if (_bindings[i].BindingName.Equals(bindingName))
                {
                    return i;
                }
            }

            return InvalidBindingIndex;
        }

        /// <summary>
        /// Resolves a binding index for the supplied Field id.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to resolve.</param>
        /// <returns>The matching operation-local binding index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fieldId"/> is invalid or not declared by this compiled operation.
        /// </exception>
        public int GetRequiredIndex(StableDataId fieldId)
        {
            var index = IndexOf(fieldId);

            if (index != InvalidBindingIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Compiled Atlas operation '{DebugName}' at occurrence index '{OperationIndex}' does not contain Field id '{fieldId}'.",
                nameof(fieldId));
        }

        /// <summary>
        /// Resolves a binding index for the supplied binding name.
        /// </summary>
        /// <param name="bindingName">Diagnostic binding name to resolve.</param>
        /// <returns>The matching operation-local binding index.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="bindingName"/> is empty or not declared by this compiled operation.
        /// </exception>
        public int GetRequiredIndex(FixedString64Bytes bindingName)
        {
            var index = IndexOf(bindingName);

            if (index != InvalidBindingIndex)
            {
                return index;
            }

            throw new ArgumentException(
                $"Compiled Atlas operation '{DebugName}' at occurrence index '{OperationIndex}' does not contain binding '{bindingName}'.",
                nameof(bindingName));
        }

        /// <summary>
        /// Attempts to resolve a binding by Field id.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to resolve.</param>
        /// <param name="binding">Resolved compiled binding when present; otherwise <see cref="AtlasCompiledBinding.Empty"/>.</param>
        /// <returns><c>true</c> when the binding was found; otherwise, <c>false</c>.</returns>
        public bool TryGetBinding(
            StableDataId fieldId,
            out AtlasCompiledBinding binding)
        {
            var index = IndexOf(fieldId);

            if (index != InvalidBindingIndex)
            {
                binding = _bindings[index];
                return true;
            }

            binding = AtlasCompiledBinding.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a binding by diagnostic binding name.
        /// </summary>
        /// <param name="bindingName">Diagnostic binding name to resolve.</param>
        /// <param name="binding">Resolved compiled binding when present; otherwise <see cref="AtlasCompiledBinding.Empty"/>.</param>
        /// <returns><c>true</c> when the binding was found; otherwise, <c>false</c>.</returns>
        public bool TryGetBinding(
            FixedString64Bytes bindingName,
            out AtlasCompiledBinding binding)
        {
            var index = IndexOf(bindingName);

            if (index != InvalidBindingIndex)
            {
                binding = _bindings[index];
                return true;
            }

            binding = AtlasCompiledBinding.Empty;
            return false;
        }

        /// <summary>
        /// Resolves a binding by Field id.
        /// </summary>
        /// <param name="fieldId">Stable Field identifier to resolve.</param>
        /// <returns>The matching compiled binding.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fieldId"/> is invalid or not declared by this compiled operation.
        /// </exception>
        public AtlasCompiledBinding GetRequiredBinding(StableDataId fieldId)
        {
            return _bindings[GetRequiredIndex(fieldId)];
        }

        /// <summary>
        /// Resolves a binding by diagnostic binding name.
        /// </summary>
        /// <param name="bindingName">Diagnostic binding name to resolve.</param>
        /// <returns>The matching compiled binding.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="bindingName"/> is empty or not declared by this compiled operation.
        /// </exception>
        public AtlasCompiledBinding GetRequiredBinding(FixedString64Bytes bindingName)
        {
            return _bindings[GetRequiredIndex(bindingName)];
        }

        /// <summary>
        /// Copies compiled bindings into a caller-provided destination array.
        /// </summary>
        /// <param name="destination">Destination array receiving bindings in operation binding order.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> is smaller than <see cref="Count"/>.
        /// </exception>
        public void CopyTo(AtlasCompiledBinding[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < _bindings.Length)
            {
                throw new ArgumentException(
                    $"Destination array length '{destination.Length}' is smaller than compiled binding count '{_bindings.Length}'.",
                    nameof(destination));
            }

            Array.Copy(_bindings, destination, _bindings.Length);
        }

        /// <summary>
        /// Copies a compiled binding range into a caller-provided destination array.
        /// </summary>
        /// <param name="sourceIndex">First source binding index in this operation.</param>
        /// <param name="destination">Destination array.</param>
        /// <param name="destinationIndex">First destination index.</param>
        /// <param name="length">Number of bindings to copy.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="destination"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a range argument is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the source or destination range is invalid.
        /// </exception>
        public void CopyTo(
            int sourceIndex,
            AtlasCompiledBinding[] destination,
            int destinationIndex,
            int length)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Source index must be non-negative.");
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), destinationIndex, "Destination index must be non-negative.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
            }

            if (sourceIndex + length > _bindings.Length)
            {
                throw new ArgumentException(
                    "Source range exceeds compiled operation bounds.",
                    nameof(length));
            }

            if (destinationIndex + length > destination.Length)
            {
                throw new ArgumentException(
                    "Destination range exceeds destination array bounds.",
                    nameof(length));
            }

            Array.Copy(_bindings, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Creates a managed copy of this operation's compiled bindings.
        /// </summary>
        /// <returns>A new binding array in operation binding order.</returns>
        public AtlasCompiledBinding[] ToArray()
        {
            var copy = new AtlasCompiledBinding[_bindings.Length];
            Array.Copy(_bindings, copy, _bindings.Length);
            return copy;
        }

        /// <summary>
        /// Gets a managed enumerator over compiled bindings in operation binding order.
        /// </summary>
        /// <returns>An enumerator over compiled bindings.</returns>
        public IEnumerator<AtlasCompiledBinding> GetEnumerator()
        {
            for (var i = 0; i < _bindings.Length; i++)
            {
                yield return _bindings[i];
            }
        }

        /// <summary>
        /// Gets a managed enumerator over compiled bindings in operation binding order.
        /// </summary>
        /// <returns>An enumerator over compiled bindings.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a diagnostic representation of this compiled operation.
        /// </summary>
        /// <returns>A string containing operation name, id, occurrence index, and binding counts.</returns>
        public override string ToString()
        {
            return $"AtlasCompiledOperation(Index={OperationIndex}, Name={DebugName}, Id={OperationId}, Bindings={Count}, Present={PresentBindingCount}, MissingOptional={MissingOptionalBindingCount})";
        }

        private static AtlasCompiledBinding[] CopyAndValidateBindings(
            int operationIndex,
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            AtlasCompiledBinding[] bindings)
        {
            ValidateOperationHeaderOrThrow(
                operationIndex,
                operationId,
                debugName,
                bindings == null ? 0 : bindings.Length);

            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            var copy = new AtlasCompiledBinding[bindings.Length];

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];

                ValidateBindingOrThrow(
                    debugName,
                    binding,
                    i);

                copy[i] = binding;
            }

            ValidateNoDuplicateFields(debugName, copy);
            ValidateNoDuplicateBindingNames(debugName, copy);

            return copy;
        }

        private static void ValidateOperationHeaderOrThrow(
            int operationIndex,
            AtlasOperationId operationId,
            FixedString64Bytes debugName,
            int bindingCount)
        {
            if (operationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(operationIndex),
                    operationIndex,
                    "Compiled operation occurrence index must be non-negative.");
            }

            operationId.ValidateOrThrow(nameof(operationId));

            if (debugName.IsEmpty)
            {
                throw new ArgumentException(
                    "Compiled Atlas operation must have a non-empty debug name.",
                    nameof(debugName));
            }

            if (bindingCount <= 0)
            {
                throw new ArgumentException(
                    $"Compiled Atlas operation '{debugName}' must contain at least one binding.",
                    nameof(bindingCount));
            }
        }

        private static void ValidateBindingOrThrow(
            FixedString64Bytes operationName,
            AtlasCompiledBinding binding,
            int index)
        {
            binding.ValidateOrThrow($"bindings[{index}]");

            if (binding.BindingIndex != index)
            {
                throw new ArgumentException(
                    $"Compiled Atlas operation '{operationName}' contains binding '{binding.BindingName}' with binding index '{binding.BindingIndex}', but expected '{index}'.",
                    nameof(binding));
            }
        }

        private static void ValidateNoDuplicateFields(
            FixedString64Bytes operationName,
            AtlasCompiledBinding[] bindings)
        {
            for (var i = 0; i < bindings.Length; i++)
            {
                var left = bindings[i];

                for (var j = i + 1; j < bindings.Length; j++)
                {
                    var right = bindings[j];

                    if (left.FieldId != right.FieldId)
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Compiled Atlas operation '{operationName}' contains duplicate Field id '{left.FieldId}' at binding indices '{i}' and '{j}'.");
                }
            }
        }

        private static void ValidateNoDuplicateBindingNames(
            FixedString64Bytes operationName,
            AtlasCompiledBinding[] bindings)
        {
            for (var i = 0; i < bindings.Length; i++)
            {
                var left = bindings[i];

                for (var j = i + 1; j < bindings.Length; j++)
                {
                    var right = bindings[j];

                    if (!left.BindingName.Equals(right.BindingName))
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Compiled Atlas operation '{operationName}' contains duplicate binding name '{left.BindingName}' at binding indices '{i}' and '{j}'.");
                }
            }
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if ((uint)index < (uint)_bindings.Length)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Compiled binding index must be between 0 and {_bindings.Length - 1}.");
        }
    }
}