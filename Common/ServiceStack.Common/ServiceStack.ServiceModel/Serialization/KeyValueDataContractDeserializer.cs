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
			var map = new Dictionary<string, string>();

			foreach (var key in nameValues.AllKeys)
			{
				if (key == null)
				{
					//occurs when no value is specified, e.g. 'path/to/page?debug'
					//throw new ArgumentNullException("key", "nameValues: " + nameValues);
					continue;
				}

				var values = nameValues.GetValues(key);
				if (values != null && values.Length > 0)
				{
					map[key] = values[0];
				}
			}
			return Parse(map, returnType);
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