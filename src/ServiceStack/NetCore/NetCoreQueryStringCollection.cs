#if NETSTANDARD2_0

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ServiceStack.NetCore
{
    public class NetCoreQueryStringCollection : NameValueCollection
    {
        readonly IQueryCollection originalQuery;
        public NetCoreQueryStringCollection(IQueryCollection originalQuery) => this.originalQuery = originalQuery;

        public override int Count => originalQuery.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => originalQuery;
        
        public override IEnumerator GetEnumerator() => originalQuery.GetEnumerator();
        public object Original => originalQuery;
        
        public override string[] AllKeys => originalQuery.Keys.ToArray();

        public override string Get(int index) => Get(GetKey(index));
        public override string Get(string name) => originalQuery[name];
        public override string GetKey(int index) => AllKeys[index];
        public override string[] GetValues(string name) => originalQuery[name];
        public new bool HasKeys() => originalQuery.Count > 0;
        public override string ToString() => string.Join("&", originalQuery.Select(pair => pair.Key + "=" + pair.Value.ToString()));

        public override void Add(string name, string value) => throw new NotSupportedException();
        public override void Clear() => throw new NotSupportedException();
        public override void Remove(string name) => throw new NotSupportedException();
        public override void Set(string key, string value) => throw new NotSupportedException();
    } 
}

#endif
