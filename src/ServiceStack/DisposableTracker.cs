using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack
{
    /// <summary>
    /// Tracks created <see cref="IDisposable"></see> objects.
    /// Used by <see cref="RequestContext"></see> to track <see cref="Funq.Container"></see> created IDisposable instances.
    /// These instances are tracked and disposed at the end of a request.
    /// </summary>
#if !NETSTANDARD1_6
    [Serializable]
#endif
    public class DisposableTracker : IDisposable
    {
        public const string HashId = "__disposables";

        private readonly List<WeakReference> disposables = new List<WeakReference>();

        /// <summary>
        /// Adds disposable to the tracker
        /// </summary>
        public void Add(IDisposable instance)
        {
            disposables.Add(new WeakReference(instance));
        }

        /// <summary>
        /// Dispose all disposables in the tracker.
        /// If disposable is still alive alose <see cref="HostContext"></see>.Release() is called to release the object.
        /// </summary>
        public void Dispose()
        {
            foreach (var wr in disposables)
            {
                var disposable = (IDisposable)wr.Target;
                if (!wr.IsAlive) continue;

                HostContext.Release(disposable);
            }
        }
    }
}