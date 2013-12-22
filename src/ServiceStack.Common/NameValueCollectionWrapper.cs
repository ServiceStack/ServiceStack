using System;
using System.Collections;
using System.Collections.Specialized;
using ServiceStack.Web;

namespace ServiceStack
{
    public class NameValueCollectionWrapper : INameValueCollection
    {
        private readonly NameValueCollection data;

        public NameValueCollectionWrapper(NameValueCollection data)
        {
            this.data = data;
        }

        public IEnumerator GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public object Original
        {
            get { return data; }
        }

        public void Add(string name, string value)
        {
            data.Add(name, value);
        }

        public void Clear()
        {
            data.Clear();
        }

        public void CopyTo(Array dest, int index)
        {
            data.CopyTo(dest, index);
        }

        public string Get(int index)
        {
            return data.Get(index);
        }

        public string Get(string name)
        {
            return data.Get(name);
        }

        public string GetKey(int index)
        {
            return data.GetKey(index);
        }

        public string[] GetValues(string name)
        {
            return data.GetValues(name);
        }

        public bool HasKeys()
        {
            return data.HasKeys();
        }

        public void Remove(string name)
        {
            data.Remove(name);
        }

        public void Set(string name, string value)
        {
            data.Set(name, value);
        }

        public string this[int index]
        {
            get { return data[index]; }
        }

        public string this[string name]
        {
            get { return data[name]; }
            set { data[name] = value; }
        }

        public string[] AllKeys
        {
            get { return data.AllKeys; }
        }

        public int Count
        {
            get { return data.Count; }
        }

        private bool readOnly;
        public bool IsReadOnly
        {
            get { return readOnly; }
            set { readOnly = value; }
        }

        public object SyncRoot
        {
            get { return data; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public static NameValueCollectionWrapper New()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }
    }

    public static class NameValueCollectionWrapperExtensions
    {
        public static NameValueCollectionWrapper InWrapper(this NameValueCollection nvc)
        {
            return new NameValueCollectionWrapper(nvc);
        }

        public static NameValueCollection ToNameValueCollection(this INameValueCollection nvc)
        {
            return (NameValueCollection)nvc.Original;
        }
    }

}