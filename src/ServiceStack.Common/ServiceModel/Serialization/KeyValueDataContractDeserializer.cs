#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ServiceStack.ServiceModel.Serialization
{
    public class KeyValueDataContractDeserializer
    {
        public static KeyValueDataContractDeserializer Instance = new KeyValueDataContractDeserializer();

        public object Parse(NameValueCollection nameValues, Type returnType)
        {
            return Parse(nameValues.ToDictionary(), returnType);
        }

        readonly Dictionary<Type, StringMapTypeDeserializer> typeStringMapSerializerMap
            = new Dictionary<Type, StringMapTypeDeserializer>();

        public object Parse(IDictionary<string, string> keyValuePairs, Type returnType)
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

            return stringMapTypeDeserializer.CreateFromMap(keyValuePairs);
        }

        public To Parse<To>(IDictionary<string, string> keyValuePairs)
        {
            return (To)Parse(keyValuePairs, typeof(To));
        }
    }
}
#endif