using System;
using ServiceStack.Text;

namespace ServiceStack.Caching.Memcached
{
    [Serializable]
    public class MemcachedValueWrapper
    {
        public Type ValueType { get; set; }
        public string JsonString { get; set; }

        [NonSerialized] private object _value;

        public object Value
        {
            get
            {
                if (_value == null && !string.IsNullOrEmpty(JsonString))
                {
                    try
                    {
                        _value = ValueType != null
                            ? JsonSerializer.DeserializeFromString(JsonString, ValueType)
                            : JsonSerializer.DeserializeFromString<object>(JsonString);
                    }
                    catch
                    {
                        try
                        {
                            _value = JsonSerializer.DeserializeFromString<object>(JsonString);
                        }
                        catch
                        {
                            _value = JsonString;
                        }
                    }
                }
                return _value;
            }
        }

        public MemcachedValueWrapper() {}

        public MemcachedValueWrapper(object value)
        {
            while (value is MemcachedValueWrapper inner)
            {
                value = inner.Value;
            }
            if (value == null) return;
            ValueType = value.GetType();
            _value = value;
            JsonString = value.ToJson();
        }
    }
}