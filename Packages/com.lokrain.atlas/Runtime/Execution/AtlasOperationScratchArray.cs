// Packages/com.lokrain.atlas/Runtime/Execution/AtlasOperationScratchArray.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Execution
//
// Purpose
// - Own one operation-scratch NativeArray allocation.
// - Expose resolved native storage to operation schedulers and jobs without field identity.
// - Support immediate disposal for unscheduled failure paths.
// - Support dependency-aware disposal through NativeArray.Dispose(JobHandle).
//
// Design notes
// - Operation scratch is not a field and must not have a StableDataId.
// - Operation scratch is private to one operation scheduler/job graph.
// - Jobs may receive the NativeArray value exposed by this lease.
// - Schedulers must chain Dispose(JobHandle) into the returned operation dependency.
// - Once dependency-aware disposal is scheduled, this lease is considered disposed immediately.
// - The returned JobHandle is the only authority that keeps scheduled scratch disposal in the graph.

using System;
using System.Globalization;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Atlas.Execution
{
    /// <summary>
    /// Owns one operation-local scratch <see cref="NativeArray{T}"/> allocation.
    /// </summary>
    /// <typeparam name="TElement">Scratch element type.</typeparam>
    /// <remarks>
    /// <para>
    /// This type is a scheduler/executor utility. It intentionally carries no Atlas field identity,
    /// no binding name, and no artifact policy. A scheduler should allocate scratch, schedule jobs
    /// that use <see cref="Array"/>, then call <see cref="Dispose(JobHandle)"/> and return the
    /// resulting dependency.
    /// </para>
    /// </remarks>
    public sealed class AtlasOperationScratchArray<TElement> : IDisposable
        where TElement : unmanaged
    {
        private const byte AliveState = 1;
        private const byte DisposedState = 2;

        private NativeArray<TElement> _array;
        private byte _state;

        internal AtlasOperationScratchArray(
            int length,
            Allocator allocator,
            NativeArrayOptions options)
        {
            ValidateLengthOrThrow(length);
            AtlasOperationScratchAllocator.ValidateScratchAllocatorOrThrow(
                allocator,
                nameof(allocator));
            ValidateNativeArrayOptionsOrThrow(options);

            Length = length;
            Allocator = allocator;
            _array = length == 0
                ? default
                : new NativeArray<TElement>(
                    length,
                    allocator,
                    options);
            _state = AliveState;
        }

        /// <summary>
        /// Gets the originally requested scratch element length.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the allocator used for this scratch allocation.
        /// </summary>
        public Allocator Allocator { get; }

        /// <summary>
        /// Gets whether this scratch lease has been disposed or scheduled for disposal.
        /// </summary>
        public bool IsDisposed => _state == DisposedState;

        /// <summary>
        /// Gets whether this lease currently references a created native allocation.
        /// </summary>
        public bool IsCreated => _state == AliveState && _array.IsCreated;

        /// <summary>
        /// Gets the native scratch array value for scheduler/job construction.
        /// </summary>
        /// <remarks>
        /// This property returns a native container value. The caller must not use the returned value
        /// after this lease has been disposed or after dependency-aware disposal has been scheduled.
        /// </remarks>
        public NativeArray<TElement> Array
        {
            get
            {
                ThrowIfDisposed();
                return _array;
            }
        }

        /// <summary>
        /// Gets the created native array or throws when this lease has no backing allocation.
        /// </summary>
        public NativeArray<TElement> GetRequiredArray()
        {
            ThrowIfDisposed();

            if (!_array.IsCreated)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Atlas operation scratch array of element type '{0}' has no created native allocation. Zero-length scratch arrays do not allocate storage.",
                        typeof(TElement).FullName));
            }

            return _array;
        }

        /// <summary>
        /// Schedules scratch disposal after the supplied dependency and returns the disposal dependency.
        /// </summary>
        public JobHandle Dispose(JobHandle dependency)
        {
            if (_state == DisposedState)
            {
                return dependency;
            }

            var array = _array;
            _array = default;
            _state = DisposedState;
            GC.SuppressFinalize(this);

            return array.IsCreated
                ? array.Dispose(dependency)
                : dependency;
        }

        /// <summary>
        /// Disposes scratch immediately. Use only when no scheduled job can still access this scratch array.
        /// </summary>
        public void Dispose()
        {
            if (_state == DisposedState)
            {
                return;
            }

            if (_array.IsCreated)
            {
                _array.Dispose();
            }

            _array = default;
            _state = DisposedState;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throws when this lease has been disposed or scheduled for disposal.
        /// </summary>
        public void ThrowIfDisposed()
        {
            if (_state != DisposedState)
            {
                return;
            }

            throw new ObjectDisposedException(
                nameof(AtlasOperationScratchArray<TElement>),
                "Atlas operation scratch array has been disposed or scheduled for dependency-aware disposal.");
        }

        /// <summary>
        /// Returns a diagnostic representation of this scratch allocation.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasOperationScratchArray<{0}>(Length={1}, Allocator={2}, IsCreated={3}, IsDisposed={4})",
                typeof(TElement).Name,
                Length,
                Allocator,
                IsCreated,
                IsDisposed);
        }

        private static void ValidateLengthOrThrow(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Operation scratch array length must be zero or positive.");
            }
        }

        private static void ValidateNativeArrayOptionsOrThrow(NativeArrayOptions options)
        {
            if (options == NativeArrayOptions.ClearMemory ||
                options == NativeArrayOptions.UninitializedMemory)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(options),
                options,
                "Operation scratch NativeArrayOptions must be ClearMemory or UninitializedMemory.");
        }
    }
}
