using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ServiceStack.Common
{
    public class HostContext
    {
        public static HostContext Instance = new HostContext();

        [ThreadStatic] public static IDictionary items;
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
    }
}