// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasCompiledBindingCursor.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Represent the deterministic location of one compiled binding occurrence inside a compiled plan.
// - Keep stage/operation/binding index plumbing out of validation policy code.
// - Create diagnostic locations from the cursor and binding metadata.

using System;
using Lokrain.Atlas.Diagnostics;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Deterministic cursor for one compiled binding occurrence inside an <see cref="AtlasCompiledPlan"/>.
    /// </summary>
    internal readonly struct AtlasCompiledBindingCursor :
        IEquatable<AtlasCompiledBindingCursor>
    {
        public AtlasCompiledBindingCursor(
            int stageIndex,
            int operationIndex,
            int bindingIndex)
        {
            ThrowIfNegative(stageIndex, nameof(stageIndex));
            ThrowIfNegative(operationIndex, nameof(operationIndex));
            ThrowIfNegative(bindingIndex, nameof(bindingIndex));

            StageIndex = stageIndex;
            OperationIndex = operationIndex;
            BindingIndex = bindingIndex;
        }

        /// <summary>
        /// Gets the zero-based compiled stage occurrence index.
        /// </summary>
        public int StageIndex { get; }

        /// <summary>
        /// Gets the zero-based operation occurrence index within the stage.
        /// </summary>
        public int OperationIndex { get; }

        /// <summary>
        /// Gets the zero-based binding occurrence index within the operation.
        /// </summary>
        public int BindingIndex { get; }

        /// <summary>
        /// Creates a diagnostic location for the supplied binding at this cursor.
        /// </summary>
        /// <param name="binding">Compiled binding located by this cursor.</param>
        /// <returns>A compiled-binding diagnostic location.</returns>
        public AtlasDiagnosticLocation CreateLocation(
            AtlasCompiledBinding binding)
        {
            return AtlasDiagnosticLocation.CompiledBinding(
                binding.FieldId,
                StageIndex,
                OperationIndex,
                BindingIndex,
                binding.BindingName);
        }

        /// <summary>
        /// Gets whether this cursor equals another cursor.
        /// </summary>
        public bool Equals(
            AtlasCompiledBindingCursor other)
        {
            return StageIndex == other.StageIndex &&
                   OperationIndex == other.OperationIndex &&
                   BindingIndex == other.BindingIndex;
        }

        /// <summary>
        /// Gets whether this cursor equals another object.
        /// </summary>
        public override bool Equals(
            object obj)
        {
            return obj is AtlasCompiledBindingCursor other && Equals(other);
        }

        /// <summary>
        /// Gets a deterministic hash code for this cursor.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ StageIndex;
                hash = (hash * 397) ^ OperationIndex;
                hash = (hash * 397) ^ BindingIndex;
                return hash;
            }
        }

        /// <summary>
        /// Returns a diagnostic representation of this cursor.
        /// </summary>
        public override string ToString()
        {
            return $"AtlasCompiledBindingCursor(Stage={StageIndex}, Operation={OperationIndex}, Binding={BindingIndex})";
        }

        private static void ThrowIfNegative(
            int value,
            string parameterName)
        {
            if (value >= 0)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Compiled binding cursor indices must be non-negative.");
        }
    }
}
