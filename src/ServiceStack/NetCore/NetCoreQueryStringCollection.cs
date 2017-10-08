#if NETSTANDARD2_0

using System;
using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Http;
using ServiceStack.Web;

namespace ServiceStack.NetCore
{
    public class NetCoreQueryStringCollection : INameValueCollection
    {
        IQueryCollection originalQuery;

        public NetCoreQueryStringCollection(IQueryCollection originalQuery)
        {
            this.originalQuery = originalQuery;
        }

        #region ICollection implementation
        public int Count => originalQuery.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => originalQuery;

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            foreach(var pair in originalQuery)
                yield return pair.Key;
        }
        #endregion

        public object Original => originalQuery;

        public string this[int index] => Get(index);

        public string this[string name] 
        {
            get { return originalQuery[name]; }

            set { throw new NotSupportedException(); }
        }

        public string[] AllKeys => originalQuery.Keys.ToArray();

        public void Add(string name, string value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public string Get(int index)
        {
            return Get(GetKey(index));
        }

        public string Get(string name)
        {
            return originalQuery[name];
        }

        public string GetKey(int index)
        {
            return AllKeys[index];
        }
        
        public string[] GetValues(string name)
        {
            return originalQuery[name];
        }

        public bool HasKeys()
        {
            return originalQuery.Count > 0;
        }

        public void Remove(string name)
        {
            throw new NotSupportedException();
        }

        public void Set(string key, string value)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return String.Join("&", originalQuery.Select(pair => pair.Key + "=" + pair.Value.ToString()));
        } 
    } 
}

#endif
