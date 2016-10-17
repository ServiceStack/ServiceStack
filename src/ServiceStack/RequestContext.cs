﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

#if !NETSTANDARD1_6
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

        /// <summary>
        /// Tell ServiceStack to use ThreadStatic Items Collection for RequestScoped items.
        /// Warning: ThreadStatic Items aren't pinned to the same request in async services which callback on different threads.
        /// </summary>
        public static bool UseThreadStatic;

        [ThreadStatic]
        public static IDictionary RequestItems;

#if NETSTANDARD1_6
        public static AsyncLocal<IDictionary> AsyncRequestItems = new AsyncLocal<IDictionary>();
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
                return GetItems() ?? CreateItems();
            }
            set
            {
                CreateItems(value);
            }
        }

        private const string _key = "__Request.Items";

        private IDictionary GetItems()
        {
#if !NETSTANDARD1_6
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
#if !NETSTANDARD1_6
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
#if !NETSTANDARD1_6
            if (UseThreadStatic)
                Items = null;
            else
                CallContext.FreeNamedDataSlot(_key);
#else
            AsyncRequestItems.Value = null;
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
            var disposables = ctxItems[DisposableTracker.HashId] as DisposableTracker;

            if (disposables != null)
            {
                disposables.Dispose();
                ctxItems.Remove(DisposableTracker.HashId);
                return true;
            }

            return false;
        }
    }
}