// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapTargetKind.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Debugging
//
// Purpose
// - Represent one explicit debug-map export request.
// - Select one artifact/workspace field by StableDataId or AtlasFieldSlot.
// - Define the image dimensions, visualization mode, output path, palette, and overwrite policy.
// - Keep debug-map export configuration out of run workflow parameter lists.
//
// Design notes
// - This is managed orchestration data.
// - This is debug/export configuration, not canonical world truth.
// - This does not own workspace memory.
// - This does not write files.
// - This does not render UnityEngine objects.
// - StableDataId zero/default is valid.
// - AtlasFieldSlot zero/default is valid.
// - Target presence is represented by TargetKind, not by invalid sentinel values.
// - default(AtlasDebugMapRequest) is not concrete.
// - Use explicit factory methods instead of partially initialized requests.

namespace Lokrain.Atlas.Debugging
{
    /// <summary>
    /// Debug-map field target selector kind.
    /// </summary>
    public enum AtlasDebugMapTargetKind : byte
    {
        /// <summary>
        /// No field target.
        /// </summary>
        None = 0,

        /// <summary>
        /// Target field is selected by stable field id.
        /// </summary>
        StableId = 1,

        /// <summary>
        /// Target field is selected by Contract-table slot.
        /// </summary>
        Slot = 2
    }
}