using System;
using System.Collections;

namespace ServiceStack.Web
{
    public interface INameValueCollection : ICollection
    {
        object Original { get; }

        string this[int index] { get; }

        string this[string name] { get; set; }

        string[] AllKeys { get; }

        void Add(string name, string value);

        void Clear();

        string Get(int index);

        string Get(string name);

        string GetKey(int index);

        string[] GetValues(string name);

        bool HasKeys();

        void Remove(string name);

        void Set(string name, string value);
    }
}