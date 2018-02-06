using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

#if !NETSTANDARD2_0
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace ServiceStack
{
    /// <summary>
    /// Abstraction to provide a context per request.
    /// in spnet.web its equivalent to <see cref="System.Web.HttpContext"></see>.Current.Items falls back to CallContext
    /// </summary>
    public class RequestContext
    {
        public static readonly RequestContext Instance = new RequestContext();

#if !NETSTANDARD2_0
        /// <summary>
        /// Tell ServiceStack to use ThreadStatic Items Collection for RequestScoped items.
        /// Warning: ThreadStatic Items aren't pinned to the same request in async services which callback on different threads.
        /// </summary>
        public static bool UseThreadStatic;

        [ThreadStatic]
        public static IDictionary RequestItems;
#else
        public static AsyncLocal<IDictionary> AsyncRequestItems = new AsyncLocal<IDictionary>();
#endif

        /// <summary>
        /// Start a new Request context, everything deeper in Async pipeline will get this new RequestContext dictionary.
        /// </summary>
        public void StartRequestContext()
        {
            // This fixes problems if the RequestContext.Instance.Items was touched on startup or outside of request context.
            // It would turn it into a static dictionary instead flooding request with each-others values.
            // This can already happen if I register a Funq.Container Request Scope type and Resolve it on startup.
            CreateItems();
        }

        /// <summary>
        /// Gets a list of items for this request. 
        /// </summary>
        /// <remarks>This list will be cleared on every request and is specific to the original thread that is handling the request.
        /// If a handler uses additional threads, this data will not be available on those threads.
        /// </remarks>
        public virtual IDictionary Items
        {
            get => GetItems() ?? CreateItems();
            set => CreateItems(value);
        }

        private const string _key = "__Request.Items";

        private IDictionary GetItems()
        {
#if !NETSTANDARD2_0
            try
            {
                if (UseThreadStatic)
                    return RequestItems;

                //Don't init CallContext on Main Thread which inits copies in Request threads
                if (!ServiceStackHost.IsReady())
                    return new Dictionary<object, object>();

                if (System.Web.HttpContext.Current != null)
                    return System.Web.HttpContext.Current.Items;

                return CallContext.LogicalGetData(_key) as IDictionary;
            }
            catch (NotImplementedException)
            {
                //Fixed in Mono master: https://github.com/mono/mono/pull/817
                return CallContext.GetData(_key) as IDictionary;
            }
#else
            return AsyncRequestItems.Value;
#endif
        }

        private IDictionary CreateItems(IDictionary items = null)
        {
#if !NETSTANDARD2_0
            try
            {
                if (UseThreadStatic)
                {
                    RequestItems = items ?? (items = new Dictionary<object, object>());
                }
                else
                {
                    CallContext.LogicalSetData(_key, items ?? (items = new ConcurrentDictionary<object, object>()));
                }
            }
            catch (NotImplementedException)
            {
                //Fixed in Mono master: https://github.com/mono/mono/pull/817
                CallContext.SetData(_key, items ?? (items = new ConcurrentDictionary<object, object>()));
            }
            return items;
#else
            return AsyncRequestItems.Value = items ?? new Dictionary<object, object>();
#endif
        }

        public T GetOrCreate<T>(Func<T> createFn)
        {
            if (Items.Contains(typeof(T).Name))
                return (T)Items[typeof(T).Name];

            return (T)(Items[typeof(T).Name] = createFn());
        }

        public void EndRequest()
        {
#if !NETSTANDARD2_0
            if (UseThreadStatic)
                Items = null;
            else
                CallContext.FreeNamedDataSlot(_key);
#else
            //setting to AsyncLocal.Value to null does not really null it
            //possible bug in .NET Core
            AsyncRequestItems.Value?.Clear();
#endif
        }

        /// <summary>
        /// Track any IDisposable's to dispose of at the end of the request in IAppHost.OnEndRequest()
        /// </summary>
        /// <param name="instance"></param>
        public void TrackDisposable(IDisposable instance)
        {
            if (!ServiceStackHost.IsReady()) return;
            if (instance == null) return;
            if (instance is IService) return; //IService's are already disposed right after they've been executed

            DisposableTracker dispsableTracker = null;
            if (!Items.Contains(DisposableTracker.HashId))
                Items[DisposableTracker.HashId] = dispsableTracker = new DisposableTracker();
            if (dispsableTracker == null)
                dispsableTracker = (DisposableTracker)Items[DisposableTracker.HashId];
            dispsableTracker.Add(instance);
        }

        /// <summary>
        /// Release currently registered dependencies for this request
        /// </summary>
        /// <returns>true if any dependencies were released</returns>
        public bool ReleaseDisposables()
        {
            if (!ServiceStackHost.IsReady()) return false;
            if (!ServiceStackHost.Instance.Config.DisposeDependenciesAfterUse) return false;

            var ctxItems = Instance.Items;

            if (ctxItems[DisposableTracker.HashId] is DisposableTracker disposables)
            {
                disposables.Dispose();
                ctxItems.Remove(DisposableTracker.HashId);
                return true;
            }

            return false;
        }
    }
}