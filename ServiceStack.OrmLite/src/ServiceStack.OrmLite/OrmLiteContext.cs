using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
#endif
using System.Threading;

namespace ServiceStack.OrmLite
{
    public class OrmLiteContext
    {
        public static readonly OrmLiteContext Instance = new OrmLiteContext();

        /// <summary>
        /// Tell ServiceStack to use ThreadStatic Items Collection for Context Scoped items.
        /// Warning: ThreadStatic Items aren't pinned to the same request in async services which callback on different threads.
        /// </summary>
        public static bool UseThreadStatic = false;

        [ThreadStatic]
        public static IDictionary ContextItems;

#if !NETFRAMEWORK
        AsyncLocal<IDictionary> localContextItems = new();
#endif

        /// <summary>
        /// Gets a list of items for this context. 
        /// </summary>
        public virtual IDictionary Items
        {
            get => GetItems() ?? (CreateItems());
            set => CreateItems(value);
        }

        private const string _key = "__OrmLite.Items";

        private IDictionary GetItems()
        {
#if !NETFRAMEWORK
            if (UseThreadStatic)
                return ContextItems;

            return localContextItems.Value;
#else
            try
            {
                if (UseThreadStatic)
                    return ContextItems;

                return CallContext.LogicalGetData(_key) as IDictionary;
            }
            catch (NotImplementedException)
            {
                //Fixed in Mono master: https://github.com/mono/mono/pull/817
                return CallContext.GetData(_key) as IDictionary;
            }
#endif
        }

        private IDictionary CreateItems(IDictionary items = null)
        {
#if !NETFRAMEWORK
                if (UseThreadStatic)
                {
                    ContextItems = items ??= new Dictionary<object, object>();
                }
                else
                {
                    localContextItems.Value = items ??= new ConcurrentDictionary<object, object>();
                }
#else                
            try
            {
                if (UseThreadStatic)
                {
                    ContextItems = items ??= new Dictionary<object, object>();
                }
                else
                {
                    CallContext.LogicalSetData(_key, items ??= new ConcurrentDictionary<object, object>());
                }
            }
            catch (NotImplementedException)
            {
                //Fixed in Mono master: https://github.com/mono/mono/pull/817
                CallContext.SetData(_key, items ??= new ConcurrentDictionary<object, object>());
            }
#endif
            return items;
        }

        public void ClearItems()
        {
            if (UseThreadStatic)
            {
                ContextItems = new Dictionary<object, object>();
            }
            else
            {
#if !NETFRAMEWORK
                localContextItems.Value = new ConcurrentDictionary<object, object>();                
#else                
                CallContext.FreeNamedDataSlot(_key);
#endif
            }
        }

        public T GetOrCreate<T>(Func<T> createFn)
        {
            if (Items.Contains(typeof(T).Name))
                return (T)Items[typeof(T).Name];

            return (T)(Items[typeof(T).Name] = createFn());
        }

        internal static void SetItem<T>(string key, T value)
        {
            if (Equals(value, default(T)))
            {
                Instance.Items.Remove(key);
            }
            else
            {
                Instance.Items[key] = value;
            }
        }

        public static OrmLiteState CreateNewState()
        {
            var state = new OrmLiteState();
            OrmLiteState = state;
            return state;
        }

        public static OrmLiteState GetOrCreateState()
        {
            return OrmLiteState
                ?? CreateNewState();
        }

        public static OrmLiteState OrmLiteState
        {
            get
            {
                if (Instance.Items.Contains("OrmLiteState"))
                    return Instance.Items["OrmLiteState"] as OrmLiteState;

                return null;
            }
            set => SetItem("OrmLiteState", value);
        }

        //Only used when using OrmLite API's against a native IDbConnection (i.e. not from DbFactory) 
        internal static IDbTransaction TSTransaction
        {
            get
            {
                var state = OrmLiteState;
                return state?.TSTransaction;
            }
            set => GetOrCreateState().TSTransaction = value;
        }
    }

    public class OrmLiteState
    {
        private static long Counter;
        public long Id;

        public OrmLiteState()
        {
            Id = Interlocked.Increment(ref Counter);
        }

        public IDbTransaction TSTransaction;
        public IOrmLiteResultsFilter ResultsFilter;

        public override string ToString()
        {
            return $"State Id: {Id}";
        }
    }
}
