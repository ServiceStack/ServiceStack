using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common
{
    public class HostContext
    {
        public static readonly HostContext Instance = new HostContext();

        [ThreadStatic] 
		private static IDictionary items; //Thread Specific
        
		/// <summary>
		/// Gets a list of items for this request. 
		/// </summary>
		/// <remarks>This list will be cleared on every request and is specific to the original thread that is handling the request.
		/// If a handler uses additional threads, this data will not be available on those threads.
		/// </remarks>
		public virtual IDictionary Items
        {
            get
            {
                return items ?? (HttpContext.Current != null
                    ? HttpContext.Current.Items
                    : items = new Dictionary<object, object>());
            }
            set { items = value; }
        }

        public T GetOrCreate<T>(Func<T> createFn)
        {
            if (Items.Contains(typeof(T).Name))
                return (T)Items[typeof(T).Name];

            return (T) (Items[typeof(T).Name] = createFn());
        }

        public void EndRequest()
        {
            items = null;
        }

        /// <summary>
        /// Track any IDisposable's to dispose of at the end of the request in IAppHost.OnEndRequest()
        /// </summary>
        /// <param name="instance"></param>
        public void TrackDisposable(IDisposable instance)
        {
            if (instance == null) return;
            if (instance is IService) return; //IService's are already disposed right after they've been executed

            DispsableTracker dispsableTracker = null;
            if (!Items.Contains(DispsableTracker.HashId))
                Items[DispsableTracker.HashId] = dispsableTracker = new DispsableTracker();
            if (dispsableTracker == null)
                dispsableTracker = (DispsableTracker) Items[DispsableTracker.HashId];
            dispsableTracker.Add(instance);
        }
    }

    public class DispsableTracker : IDisposable
    {
        public const string HashId = "__disposables";

        List<WeakReference> disposables = new List<WeakReference>();

        public void Add(IDisposable instance)
        {
            disposables.Add(new WeakReference(instance));
        }

        public void Dispose()
        {
            foreach (var wr in disposables)
            {
                var disposable = (IDisposable)wr.Target;
                if (wr.IsAlive)
                    disposable.Dispose();
            }
        }
    }
}