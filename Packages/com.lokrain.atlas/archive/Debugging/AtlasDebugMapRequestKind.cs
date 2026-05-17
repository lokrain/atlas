// Packages/com.lokrain.atlas/Runtime/Debugging/AtlasDebugMapRequestKind.cs
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
    /// Debug-map visualization mode.
    /// </summary>
    public enum AtlasDebugMapRequestKind : byte
    {
        /// <summary>
        /// No debug-map request kind.
        /// </summary>
        None = 0,

        /// <summary>
        /// Treat a byte field as a boolean mask: zero is off, non-zero is on.
        /// </summary>
        ByteMask = 1,

        /// <summary>
        /// Treat a byte field as a normalized 0..255 ramp.
        /// </summary>
        ByteRamp = 2,

        /// <summary>
        /// Treat an Int32 field as a value ramp using explicit minimum and maximum.
        /// </summary>
        Int32Ramp = 3
    }
}