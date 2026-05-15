// Runtime/Core/AtlasConstants.cs

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Defines package-wide constants used by Atlas Contracts, storage, validation, and diagnostics.
    /// </summary>
    /// <remarks>
    /// Constants in this type are part of the public Atlas contract. Values should only change
    /// when the package intentionally changes compatibility behavior or reserved sentinel values.
    /// </remarks>
    public static class AtlasConstants
    {
        /// <summary>
        /// Unity package name used by package manifests, diagnostics, and editor tooling.
        /// </summary>
        public const string PackageName = "com.lokrain.atlas";

        /// <summary>
        /// Runtime assembly name used by assembly definitions and diagnostics.
        /// </summary>
        public const string RuntimeAssemblyName = "Lokrain.Atlas";

        /// <summary>
        /// Root namespace used by all public runtime APIs.
        /// </summary>
        public const string RootNamespace = "Lokrain.Atlas";

        /// <summary>
        /// Reserved slot value representing an unresolved, missing, or invalid Field slot.
        /// </summary>
        /// <remarks>
        /// Valid Contract-table slots must never use this value.
        /// </remarks>
        public const ushort InvalidFieldSlot = ushort.MaxValue;

        /// <summary>
        /// First valid Contract-table slot.
        /// </summary>
        public const ushort FirstFieldSlot = 0;

        /// <summary>
        /// Last valid Contract-table slot.
        /// </summary>
        /// <remarks>
        /// <see cref="InvalidFieldSlot"/> is reserved as a sentinel, so the last usable slot is
        /// one less than the maximum value of <see cref="ushort"/>.
        /// </remarks>
        public const ushort LastFieldSlot = InvalidFieldSlot - 1;

        /// <summary>
        /// Maximum number of Field Contracts that can be represented by the default slot type.
        /// </summary>
        /// <remarks>
        /// Atlas uses <see cref="ushort"/> slots to keep Field handles compact and Burst-friendly.
        /// One value is reserved for <see cref="InvalidFieldSlot"/>.
        /// </remarks>
        public const int MaxFieldSlots = ushort.MaxValue;

        /// <summary>
        /// Reserved length value representing an unresolved runtime shape.
        /// </summary>
        /// <remarks>
        /// Valid resolved lengths must be greater than or equal to zero.
        /// </remarks>
        public const int UnresolvedLength = -1;

        /// <summary>
        /// Reserved element size value representing an unknown or invalid storage format.
        /// </summary>
        /// <remarks>
        /// Valid unmanaged element formats must have a positive element size.
        /// </remarks>
        public const int InvalidElementSize = 0;

        /// <summary>
        /// Reserved element alignment value representing an unknown or invalid storage format.
        /// </summary>
        /// <remarks>
        /// Valid unmanaged element formats must have a positive element alignment.
        /// </remarks>
        public const int InvalidElementAlignment = 0;

        /// <summary>
        /// Default semantic contract version for newly declared stable Field identifiers.
        /// </summary>
        /// <remarks>
        /// Version zero is reserved by <see cref="StableDataId"/>. New Field declarations should
        /// normally start at this value and increment only for incompatible Field contract changes.
        /// </remarks>
        public const ushort InitialFieldVersion = 1;
    }
}