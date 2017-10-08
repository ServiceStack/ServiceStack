#if NETSTANDARD2_0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using ServiceStack.Web;

namespace ServiceStack.NetCore
{
    public class NetCoreHeadersCollection : INameValueCollection
    {
        IHeaderDictionary original;

        public NetCoreHeadersCollection(IHeaderDictionary original)
        {
            this.original = original;
        }

        #region ICollection implementation
        public int Count => original.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => original;

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var pair in original)
                yield return pair.Key;
        }
        #endregion

        public object Original => original;

        public string this[int index] => Get(index);

        public string this[string name] 
        {
            get { return original[name]; }

            set { throw new NotSupportedException(); }
        }

        public string[] AllKeys => original.Keys.ToArray();

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
            return name != null ? (string)original[name] : null;
        }

        public string GetKey(int index)
        {
            return AllKeys[index];
        }
        
        public string[] GetValues(string name)
        {
            return original[name];
        }

        public bool HasKeys()
        {
            return original.Count > 0;
        }

        public void Remove(string name)
        {
            throw new NotSupportedException();
        }

        public void Set(string key, string value)
        {
            throw new NotSupportedException();
        }
    } 
}

#endif
