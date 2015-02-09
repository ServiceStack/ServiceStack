#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
using System;
using System.Collections.Generic;
using ServiceStack.Web;

namespace ServiceStack.Serialization
{
    public class KeyValueDataContractDeserializer
    {
        public static KeyValueDataContractDeserializer Instance = new KeyValueDataContractDeserializer();

        public object Parse(INameValueCollection nameValues, Type returnType)
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