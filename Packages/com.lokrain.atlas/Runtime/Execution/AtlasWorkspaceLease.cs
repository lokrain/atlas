// Packages/com.lokrain.atlas/Runtime/Execution/AtlasWorkspaceLease.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Own a transferred Atlas workspace together with the JobHandle that protects it.
// - Make asynchronous workflow ownership explicit after AtlasRunWorkflowResult releases workspace state.
// - Complete pending execution before disposing workspace memory.
// - Prevent native workspace ownership from being separated from the scheduled dependency chain.
//
// Design notes
// - This is managed orchestration ownership, not job code.
// - The lease owns workspace disposal only when OwnsWorkspace is true.
// - A pending dependency belongs to the lease after transfer and must be completed before workspace inspection/disposal.
// - Dispose is safe and idempotent.
// - default(JobHandle) is a valid completed dependency representation.

using System;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Owns a transferred Atlas workspace and the dependency required before the workspace is safe to inspect or dispose.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasWorkspaceLease"/> is returned when a workflow result transfers workspace
    /// ownership without forcing scheduled execution to complete first. The lease keeps the workspace
    /// and final dependency together so downstream systems can chain more jobs safely.
    /// </para>
    ///
    /// <para>
    /// Disposing the lease completes any pending dependency before disposing an owned workspace. This
    /// preserves the native-memory lifetime invariant even when callers do not explicitly call
    /// <see cref="Complete"/>.
    /// </para>
    /// </remarks>
    public sealed class AtlasWorkspaceLease : IDisposable
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private AtlasWorkspace _workspace;
        private bool _ownsWorkspace;
        private JobHandle _dependency;
        private bool _hasPendingDependency;
        private byte _state;

        internal AtlasWorkspaceLease(
            AtlasWorkspace workspace,
            bool ownsWorkspace,
            JobHandle dependency,
            bool hasPendingDependency)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (workspace.IsDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasWorkspace),
                    "Atlas workspace lease cannot own a disposed workspace.");
            }

            _workspace = workspace;
            _ownsWorkspace = ownsWorkspace;
            _dependency = hasPendingDependency ? dependency : default;
            _hasPendingDependency = hasPendingDependency;
            _state = AliveState;
        }

        /// <summary>
        /// Gets the leased workspace, or null after ownership is released or the lease is disposed.
        /// </summary>
        public AtlasWorkspace Workspace => _workspace;

        /// <summary>
        /// Gets whether this lease still references a workspace.
        /// </summary>
        public bool HasWorkspace => _workspace != null;

        /// <summary>
        /// Gets whether this lease owns disposal of the attached workspace.
        /// </summary>
        public bool OwnsWorkspace => _ownsWorkspace && _workspace != null;

        /// <summary>
        /// Gets whether this lease has been disposed.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Gets whether the leased workspace is protected by a pending dependency.
        /// </summary>
        public bool HasPendingDependency => _hasPendingDependency;

        /// <summary>
        /// Gets the pending dependency protecting the leased workspace, or default when no dependency is pending.
        /// </summary>
        public JobHandle Dependency => _hasPendingDependency ? _dependency : default;

        /// <summary>
        /// Completes the pending dependency, if any.
        /// </summary>
        public void Complete()
        {
            ThrowIfDisposed();

            if (!_hasPendingDependency)
            {
                return;
            }

            _dependency.Complete();
            _dependency = default;
            _hasPendingDependency = false;
        }

        /// <summary>
        /// Gets the required live workspace.
        /// </summary>
        public AtlasWorkspace GetRequiredWorkspace()
        {
            ThrowIfDisposed();

            if (_workspace == null)
            {
                throw new InvalidOperationException(
                    "Atlas workspace lease does not contain a workspace.");
            }

            if (_workspace.IsDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(AtlasWorkspace),
                    "Atlas workspace lease contains a disposed workspace.");
            }

            return _workspace;
        }

        /// <summary>
        /// Completes pending work and transfers workspace ownership out of this lease.
        /// </summary>
        /// <returns>The leased workspace after its pending dependency has completed.</returns>
        /// <remarks>
        /// After this call, disposing the lease will not dispose the returned workspace. The caller
        /// becomes responsible for disposing it when <see cref="OwnsWorkspace"/> was true before release.
        /// </remarks>
        public AtlasWorkspace CompleteAndReleaseWorkspaceOwnership()
        {
            ThrowIfDisposed();

            if (_workspace == null)
            {
                throw new InvalidOperationException(
                    "Atlas workspace lease does not contain a workspace to release.");
            }

            Complete();

            var released = _workspace;
            _workspace = null;
            _ownsWorkspace = false;

            return released;
        }

        /// <summary>
        /// Throws when this lease has been disposed.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state != DisposedState)
            {
                return;
            }

            throw new ObjectDisposedException(
                nameof(AtlasWorkspaceLease),
                "Atlas workspace lease has been disposed.");
        }

        /// <summary>
        /// Completes pending work and disposes the owned workspace.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            if (_ownsWorkspace && _workspace != null)
            {
                Complete();
                _workspace.Dispose();
            }

            _workspace = null;
            _ownsWorkspace = false;
            _dependency = default;
            _hasPendingDependency = false;
            _state = DisposedState;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a diagnostic representation of this lease.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "AtlasWorkspaceLease(HasWorkspace={0}, OwnsWorkspace={1}, HasPendingDependency={2}, IsDisposed={3})",
                HasWorkspace,
                OwnsWorkspace,
                HasPendingDependency,
                IsDisposed);
        }
    }
}
