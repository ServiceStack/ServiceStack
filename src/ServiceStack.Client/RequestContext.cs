using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ServiceStack
{
    public class RequestContext
    {
        public static readonly RequestContext Instance = new RequestContext();

#if SL5 || ANDROID || __IOS__ || PCL
        [ThreadStatic] private static IDictionary _items;
#endif

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
#if !(SL5 || ANDROID || __IOS__ || PCL)
                return GetItems() ?? (System.Web.HttpContext.Current != null
                    ? System.Web.HttpContext.Current.Items
                    : CreateItems());
#else
                return GetItems() ?? CreateItems();
#endif
            }
            set
            {
                CreateItems(value);
            }
        }

        private const string _key = "__Request.Items";

        private IDictionary GetItems()
        {
#if !(SL5 || ANDROID || __IOS__ || PCL)
            return System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(_key) as IDictionary;
#else
            return items;
#endif
        }

        private IDictionary CreateItems(IDictionary items=null)
        {
#if !(SL5 || ANDROID || __IOS__ || PCL)
            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(_key, items ?? (items = new ConcurrentDictionary<object, object>()));
#else
            _items = items ?? (items = new Dictionary<object, object>());
#endif
            return items;
        }

        public T GetOrCreate<T>(Func<T> createFn)
        {
            if (Items.Contains(typeof(T).Name))
                return (T)Items[typeof(T).Name];

            return (T) (Items[typeof(T).Name] = createFn());
        }

        public void EndRequest()
        {
#if !(SL5 || ANDROID || __IOS__ || PCL)
            System.Runtime.Remoting.Messaging.CallContext.FreeNamedDataSlot(_key);
#else
            _items = null;
#endif
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