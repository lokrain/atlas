// Packages/com.lokrain.atlas/Runtime/Compilation/IAtlasCompiledBindingVisitor.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Define the allocation-free visitor contract used by compiled-plan binding traversal.
// - Keep traversal mechanics separate from validation policy implementation.

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Visits compiled binding occurrences in deterministic compiled-plan order.
    /// </summary>
    internal interface IAtlasCompiledBindingVisitor
    {
        /// <summary>
        /// Visits one compiled binding occurrence.
        /// </summary>
        /// <param name="operation">Compiled operation that owns the binding.</param>
        /// <param name="binding">Compiled binding occurrence.</param>
        /// <param name="cursor">Deterministic compiled-plan cursor for the binding.</param>
        void VisitBinding(
            AtlasCompiledOperation operation,
            AtlasCompiledBinding binding,
            AtlasCompiledBindingCursor cursor);
    }
}
