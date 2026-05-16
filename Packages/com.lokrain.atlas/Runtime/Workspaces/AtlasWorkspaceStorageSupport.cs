// Packages/com.lokrain.atlas/Runtime/Workspaces/AtlasWorkspaceStorageSupport.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Workspaces
//
// Purpose
// - Define the storage families supported by the current Atlas workspace byte-block backend.
// - Provide one authoritative support predicate for workspace layout, allocation, and tests.
// - Reject declared-but-unimplemented storage families before workspace allocation.
// - Keep storage support policy separate from Contracts vocabulary and artifact serialization.
//
// Design notes
// - StorageKind is broader than the current workspace backend.
// - The current backend packs fixed contiguous Atlas-owned Scalar and NativeArray fields into one byte block.
// - Growable, stream, hash-map, blob, and external storage require dedicated binding models.
// - Unsupported storage must fail deterministically at layout compilation, not deep inside execution.

using System;
using System.Globalization;
using Lokrain.Atlas.Contracts;
using Unity.Collections;

namespace Lokrain.Atlas.Workspaces
{
    /// <summary>
    /// Defines the storage families supported by the current Atlas workspace byte-block backend.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Atlas contract vocabulary intentionally contains more storage families than the first
    /// workspace backend can allocate. This type is the explicit fence between declared schema
    /// vocabulary and executable workspace storage. The current backend supports only fixed
    /// contiguous Atlas-owned <see cref="StorageKind.Scalar"/> and
    /// <see cref="StorageKind.NativeArray"/> storage.
    /// </para>
    ///
    /// <para>
    /// Unsupported storage kinds are not invalid contract vocabulary. They are unsupported by this
    /// physical workspace backend until a dedicated allocation or external binding model exists.
    /// </para>
    /// </remarks>
    public static class AtlasWorkspaceStorageSupport
    {
        /// <summary>
        /// Returns whether the current workspace byte-block backend can allocate the supplied storage kind.
        /// </summary>
        /// <param name="storageKind">Storage kind to inspect.</param>
        /// <returns><c>true</c> when the workspace backend can allocate the storage kind.</returns>
        public static bool SupportsStorageKind(
            StorageKind storageKind)
        {
            return storageKind == StorageKind.Scalar ||
                   storageKind == StorageKind.NativeArray;
        }

        /// <summary>
        /// Returns whether the supplied storage kind represents externally bound storage.
        /// </summary>
        /// <param name="storageKind">Storage kind to inspect.</param>
        /// <returns><c>true</c> when the storage kind requires an explicit external binding model.</returns>
        public static bool RequiresExternalBinding(
            StorageKind storageKind)
        {
            return storageKind == StorageKind.External;
        }

        /// <summary>
        /// Returns whether the supplied storage format can be represented by the current workspace backend.
        /// </summary>
        /// <param name="storageFormat">Storage format to inspect.</param>
        /// <returns><c>true</c> when the storage format is concrete and supported.</returns>
        public static bool SupportsStorageFormat(
            StorageFormat storageFormat)
        {
            return storageFormat.IsValid &&
                   SupportsStorageKind(storageFormat.Kind);
        }

        /// <summary>
        /// Throws when the supplied storage kind is not supported by the current workspace backend.
        /// </summary>
        /// <param name="storageKind">Storage kind to validate.</param>
        /// <param name="fieldName">Optional diagnostic field name.</param>
        public static void ValidateStorageKindOrThrow(
            StorageKind storageKind,
            FixedString64Bytes fieldName)
        {
            if (SupportsStorageKind(storageKind))
            {
                return;
            }

            throw new NotSupportedException(
                CreateUnsupportedStorageKindMessage(
                    storageKind,
                    fieldName));
        }

        /// <summary>
        /// Throws when the supplied storage format is not supported by the current workspace backend.
        /// </summary>
        /// <param name="storageFormat">Storage format to validate.</param>
        /// <param name="fieldName">Optional diagnostic field name.</param>
        /// <param name="parameterName">Optional parameter name used for non-concrete format errors.</param>
        public static void ValidateStorageFormatOrThrow(
            StorageFormat storageFormat,
            FixedString64Bytes fieldName,
            string parameterName = null)
        {
            storageFormat.ValidateOrThrow(parameterName);
            ValidateStorageKindOrThrow(
                storageFormat.Kind,
                fieldName);
        }

        /// <summary>
        /// Creates a deterministic diagnostic message for unsupported workspace storage.
        /// </summary>
        public static string CreateUnsupportedStorageKindMessage(
            StorageKind storageKind,
            FixedString64Bytes fieldName)
        {
            if (RequiresExternalBinding(storageKind))
            {
                return CreateExternalStorageUnsupportedMessage(fieldName);
            }

            if (fieldName.IsEmpty)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Workspace byte-block backend cannot allocate storage kind '{0}'. Current workspace storage supports only Scalar and NativeArray storage. Growable, stream, hash-map, and blob storage require dedicated physical binding models.",
                    storageKind);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Workspace byte-block backend cannot allocate field '{0}' with storage kind '{1}'. Current workspace storage supports only Scalar and NativeArray storage. Growable, stream, hash-map, and blob storage require dedicated physical binding models.",
                fieldName,
                storageKind);
        }

        private static string CreateExternalStorageUnsupportedMessage(
            FixedString64Bytes fieldName)
        {
            if (fieldName.IsEmpty)
            {
                return "Workspace byte-block backend cannot allocate or execute storage kind 'External'. External storage is valid contract vocabulary, but executable workspace use requires an explicit external binding model that defines lifetime, ownership, job-safety, and artifact-capture policy.";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Workspace byte-block backend cannot allocate or execute field '{0}' with storage kind 'External'. External storage is valid contract vocabulary, but executable workspace use requires an explicit external binding model that defines lifetime, ownership, job-safety, and artifact-capture policy.",
                fieldName);
        }
    }
}
