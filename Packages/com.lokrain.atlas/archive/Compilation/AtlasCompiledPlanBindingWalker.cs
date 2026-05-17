// Packages/com.lokrain.atlas/Runtime/Compilation/AtlasCompiledPlanBindingWalker.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Compilation
//
// Purpose
// - Traverse compiled plans in deterministic stage/operation/binding order.
// - Centralize compiled-plan traversal so validators do not own nested iteration mechanics.
// - Preserve allocation-free visitor dispatch for managed compiler validation paths.

using System;

namespace Lokrain.Atlas.Compilation
{
    /// <summary>
    /// Deterministically walks compiled binding occurrences inside an <see cref="AtlasCompiledPlan"/>.
    /// </summary>
    internal static class AtlasCompiledPlanBindingWalker
    {
        /// <summary>
        /// Visits every compiled binding occurrence in stable stage/operation/binding order.
        /// </summary>
        /// <typeparam name="TVisitor">Concrete value-type visitor.</typeparam>
        /// <param name="plan">Compiled plan to walk.</param>
        /// <param name="visitor">Visitor that receives each binding occurrence.</param>
        public static void VisitBindings<TVisitor>(
            AtlasCompiledPlan plan,
            ref TVisitor visitor)
            where TVisitor : struct, IAtlasCompiledBindingVisitor
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            for (var stageIndex = 0; stageIndex < plan.Count; stageIndex++)
            {
                VisitStage(
                    plan[stageIndex],
                    stageIndex,
                    ref visitor);
            }
        }

        private static void VisitStage<TVisitor>(
            AtlasCompiledStage stage,
            int stageIndex,
            ref TVisitor visitor)
            where TVisitor : struct, IAtlasCompiledBindingVisitor
        {
            for (var operationIndex = 0; operationIndex < stage.Count; operationIndex++)
            {
                VisitOperation(
                    stage[operationIndex],
                    stageIndex,
                    operationIndex,
                    ref visitor);
            }
        }

        private static void VisitOperation<TVisitor>(
            AtlasCompiledOperation operation,
            int stageIndex,
            int operationIndex,
            ref TVisitor visitor)
            where TVisitor : struct, IAtlasCompiledBindingVisitor
        {
            for (var bindingIndex = 0; bindingIndex < operation.Count; bindingIndex++)
            {
                visitor.VisitBinding(
                    operation,
                    operation[bindingIndex],
                    new AtlasCompiledBindingCursor(
                        stageIndex,
                        operationIndex,
                        bindingIndex));
            }
        }
    }
}
