using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ServiceStack.Common
{
    public class HostContext
    {
        public static readonly HostContext Instance = new HostContext();

        [ThreadStatic] 
		public static IDictionary _items; //Thread Specific
        
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
                return _items ?? (HttpContext.Current != null
                    ? HttpContext.Current.Items
                    : _items = new Dictionary<object, object>());
            }
            set { _items = value; }
        }

        public T GetOrCreate<T>(Func<T> createFn)
        {
            if (Items.Contains(typeof(T).Name))
                return (T)Items[typeof(T).Name];

            return (T) (Items[typeof(T).Name] = createFn());
        }

        public void EndRequest()
        {
            _items = null;
        }
    }
}