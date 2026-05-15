// Runtime/Core/AtlasConstants.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Core
//
// Purpose
// - Centralize representation limits shared by Atlas runtime metadata.
// - Preserve zero-valid slot, id, version, and hash semantics.
// - Avoid invalid sentinel constants.
// - Keep numeric boundaries explicit and deterministic.
//
// Design notes
// - Field slot zero is valid.
// - StableDataId zero/default is valid.
// - AtlasOperationId zero/default is valid.
// - AtlasStageId zero/default is valid.
// - AtlasPipelineId zero/default is valid.
// - Type hash zero/default is valid.
// - Version zero is valid.
// - No constant in this file represents an invalid id.
// - Missing state must be represented by bool-returning APIs or explicit presence flags.
// - Do not put algorithm tunables here.
// - Do not put map-generation profile defaults here.
// - Do not put job batch sizes here.
// - Do not put sea level, elevation scale, climate constants, or terrain-generation constants here.

namespace Lokrain.Atlas.Core
{
    /// <summary>
    /// Shared representation constants for Atlas runtime metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type owns hard representation boundaries for the current Atlas metadata model. It does
    /// not own world-generation policy, stage tuning, algorithm defaults, map profile values,
    /// presentation settings, or job scheduling heuristics.
    /// </para>
    ///
    /// <para>
    /// The constants in this file intentionally follow the zero-valid production model. Zero is
    /// valid for ids, versions, hashes, and slots. Optional, missing, unresolved, or unsupported
    /// state must be represented by explicit boolean state in the containing type or by a
    /// bool-returning lookup API.
    /// </para>
    /// </remarks>
    public static class AtlasConstants
    {
        /// <summary>
        /// The first supported zero-based field slot.
        /// </summary>
        /// <remarks>
        /// Slot zero is valid. It must not be used as an invalid or missing sentinel.
        /// </remarks>
        public const int FirstFieldSlot = 0;

        /// <summary>
        /// The last supported zero-based field slot.
        /// </summary>
        /// <remarks>
        /// This value is bounded by the current <c>ushort</c>-backed slot representation.
        /// </remarks>
        public const int LastFieldSlot = ushort.MaxValue;

        /// <summary>
        /// The maximum number of field slots representable by <see cref="Fields.AtlasFieldSlot"/>.
        /// </summary>
        /// <remarks>
        /// Because slot zero is valid, the maximum count is <c>ushort.MaxValue + 1</c>.
        /// </remarks>
        public const int MaxFieldSlots = ushort.MaxValue + 1;

        /// <summary>
        /// The first valid operation-local binding index.
        /// </summary>
        /// <remarks>
        /// Binding index zero is valid and represents the first binding in operation ABI order.
        /// </remarks>
        public const int FirstOperationBindingIndex = 0;

        /// <summary>
        /// The first valid stage-local operation index.
        /// </summary>
        /// <remarks>
        /// Operation index zero is valid and represents the first operation in stage ABI order.
        /// </remarks>
        public const int FirstStageOperationIndex = 0;

        /// <summary>
        /// The first valid pipeline-local stage index.
        /// </summary>
        /// <remarks>
        /// Stage index zero is valid and represents the first stage in pipeline ABI order.
        /// </remarks>
        public const int FirstPipelineStageIndex = 0;

        /// <summary>
        /// The first valid contract version.
        /// </summary>
        /// <remarks>
        /// Version zero is valid. It must not be treated as missing or invalid.
        /// </remarks>
        public const ushort FirstContractVersion = 0;

        /// <summary>
        /// The maximum representable contract version.
        /// </summary>
        public const ushort LastContractVersion = ushort.MaxValue;

        /// <summary>
        /// The byte count of a durable identity payload excluding version.
        /// </summary>
        /// <remarks>
        /// Stable Atlas identities use a 128-bit durable identity plus a semantic version.
        /// </remarks>
        public const int StableIdentityByteCount = 16;

        /// <summary>
        /// The byte count of a stable version payload.
        /// </summary>
        public const int StableVersionByteCount = sizeof(ushort);

        /// <summary>
        /// The byte count of a stable identity plus version payload.
        /// </summary>
        public const int StableVersionedIdentityByteCount = StableIdentityByteCount + StableVersionByteCount;

        /// <summary>
        /// The number of bits in the high identity word.
        /// </summary>
        public const int StableIdentityHighBitCount = 64;

        /// <summary>
        /// The number of bits in the low identity word.
        /// </summary>
        public const int StableIdentityLowBitCount = 64;

        /// <summary>
        /// The total number of bits in the durable identity payload excluding version.
        /// </summary>
        public const int StableIdentityBitCount = StableIdentityHighBitCount + StableIdentityLowBitCount;

        /// <summary>
        /// The maximum diagnostic name byte capacity used by FixedString64Bytes-backed names.
        /// </summary>
        /// <remarks>
        /// <c>FixedString64Bytes</c> stores bytes, not UTF-16 characters. Effective character count
        /// depends on UTF-8 encoding.
        /// </remarks>
        public const int FixedString64ByteCapacity = 64;

        /// <summary>
        /// The maximum diagnostic name byte capacity used by FixedString128Bytes-backed names.
        /// </summary>
        /// <remarks>
        /// <c>FixedString128Bytes</c> stores bytes, not UTF-16 characters. Effective character count
        /// depends on UTF-8 encoding.
        /// </remarks>
        public const int FixedString128ByteCapacity = 128;

        /// <summary>
        /// The default deterministic hash seed used by value objects that need a local hash seed.
        /// </summary>
        /// <remarks>
        /// This is not a cryptographic seed and must not be used for stable artifact hashing by
        /// itself. It exists only for deterministic managed <see cref="object.GetHashCode"/> style
        /// implementations in small value objects.
        /// </remarks>
        public const int DeterministicHashSeed = 17;

        /// <summary>
        /// The default deterministic hash multiplier used by value objects that need a local hash multiplier.
        /// </summary>
        /// <remarks>
        /// This is not a cryptographic multiplier and must not be used as the package's artifact
        /// hash algorithm by itself.
        /// </remarks>
        public const int DeterministicHashMultiplier = 397;

        /// <summary>
        /// The non-concrete element size used by default storage-format payloads.
        /// </summary>
        /// <remarks>
        /// This is a semantic concrete-format threshold, not an invalid bit-pattern sentinel.
        /// A concrete storage format must have an element size greater than this value.
        /// </remarks>
        public const int InvalidElementSize = 0;

        /// <summary>
        /// The non-concrete element alignment used by default storage-format payloads.
        /// </summary>
        /// <remarks>
        /// This is a semantic concrete-format threshold, not an invalid bit-pattern sentinel.
        /// A concrete storage format must have an element alignment greater than this value.
        /// </remarks>
        public const int InvalidElementAlignment = 0;
    }
} 