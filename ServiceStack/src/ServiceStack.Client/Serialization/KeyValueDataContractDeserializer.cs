using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ServiceStack.Serialization
{
    public class KeyValueDataContractDeserializer
    {
        public static KeyValueDataContractDeserializer Instance = new();

        readonly Dictionary<Type, StringMapTypeDeserializer> typeStringMapSerializerMap = new();

        public object Parse(IDictionary<string, string> keyValuePairs, Type returnType)
        {
            return GetOrAddStringMapTypeDeserializer(returnType)
                .CreateFromMap(keyValuePairs);
        }

        public object Parse(NameValueCollection nameValues, Type returnType)
        {
            return GetOrAddStringMapTypeDeserializer(returnType)
                .CreateFromMap(nameValues);
        }

        public object Populate(object instance, NameValueCollection nameValues, Type returnType)
        {
            return GetOrAddStringMapTypeDeserializer(returnType)
                .PopulateFromMap(instance, nameValues);
        }

        private StringMapTypeDeserializer GetOrAddStringMapTypeDeserializer(Type returnType)
        {
            StringMapTypeDeserializer stringMapTypeDeserializer;
            lock (typeStringMapSerializerMap)
            {
                if (!typeStringMapSerializerMap.TryGetValue(returnType, out stringMapTypeDeserializer))
                {
                    stringMapTypeDeserializer = new StringMapTypeDeserializer(returnType);
                    typeStringMapSerializerMap.Add(returnType, stringMapTypeDeserializer);
                }
            }

            return stringMapTypeDeserializer;
        }

        public To Parse<To>(IDictionary<string, string> keyValuePairs)
        {
            return (To)Parse(keyValuePairs, typeof(To));
        }
    }
}