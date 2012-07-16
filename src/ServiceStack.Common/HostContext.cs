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

        [ThreadStatic] protected static IDictionary items;
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

        public void EndRequest()
        {
            if (items != null)
            {
                items.Values.OfType<IDisposable>().Dispose();
            }
            items = null;
        }
    }
}