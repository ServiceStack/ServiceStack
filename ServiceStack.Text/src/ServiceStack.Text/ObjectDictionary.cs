using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack
{
    /// <summary>
    /// UX friendly alternative alias of Dictionary&lt;string, object&gt;
    /// </summary>
    public class ObjectDictionary : Dictionary<string, object>
    {
        public ObjectDictionary() { }
        public ObjectDictionary(int capacity) : base(capacity) { }
        public ObjectDictionary(IEqualityComparer<string> comparer) : base(comparer) { }
        public ObjectDictionary(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) { }
        public ObjectDictionary(IDictionary<string, object> dictionary) : base(dictionary) { }
        public ObjectDictionary(IDictionary<string, object> dictionary, IEqualityComparer<string> comparer) : base(dictionary, comparer) { }
        protected ObjectDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// UX friendly alternative alias of Dictionary&lt;string, string&gt;
    /// </summary>
    public class StringDictionary : Dictionary<string, string>
    {
        public StringDictionary() { }
        public StringDictionary(int capacity) : base(capacity) { }
        public StringDictionary(IEqualityComparer<string> comparer) : base(comparer) { }
        public StringDictionary(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) { }
        public StringDictionary(IDictionary<string, string> dictionary) : base(dictionary) { }
        public StringDictionary(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) : base(dictionary, comparer) { }
        protected StringDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// UX friendly alternative alias of List&lt;KeyValuePair&lt;string, object&gt;gt;
    /// </summary>
    public class KeyValuePairs : List<KeyValuePair<string, object>>
    {
        public KeyValuePairs() { }
        public KeyValuePairs(int capacity) : base(capacity) { }
        public KeyValuePairs(IEnumerable<KeyValuePair<string, object>> collection) : base(collection) { }
        
        public static KeyValuePair<string,object> Create(string key, object value) => 
            new KeyValuePair<string, object>(key, value);
    }

    /// <summary>
    /// UX friendly alternative alias of List&lt;KeyValuePair&lt;string, string&gt;gt;
    /// </summary>
    public class KeyValueStrings : List<KeyValuePair<string, string>>
    {
        public KeyValueStrings() { }
        public KeyValueStrings(int capacity) : base(capacity) { }
        public KeyValueStrings(IEnumerable<KeyValuePair<string, string>> collection) : base(collection) { }
        
        public static KeyValuePair<string,string> Create(string key, string value) => 
            new KeyValuePair<string,string>(key, value);
    }
}