using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.ServiceModel.Serialization
{
	public class KeyValueDataContractDeserializer 
	{
		private readonly ILog log = LogManager.GetLogger(typeof(KeyValueDataContractDeserializer));

		public static KeyValueDataContractDeserializer Instance = new KeyValueDataContractDeserializer();

		public object Parse(NameValueCollection nameValues, Type returnType)
		{
			var map = new Dictionary<string, string>();
			foreach (var key in nameValues.AllKeys)
			{
				var values = nameValues.GetValues(key);
				if (values != null && values.Length > 0)
				{
					map[key] = values[0];
				}
			}
			return Parse(map, returnType);
		}

		public object Parse(IDictionary<string, string> keyValuePairs, Type returnType)
		{
			try
			{
				var result = Activator.CreateInstance(returnType);
				foreach (var pair in keyValuePairs)
				{
					var propertyName = pair.Key;
					var propertyTextValue = pair.Value;
					var propertyInfo = returnType.GetProperty(propertyName);
					if (propertyInfo == null)
					{
						log.WarnFormat("Property '{0}' does not exist on type '{1}'", propertyName, returnType.FullName);
						continue;
					}
					var value = TypeSerializer.DeserializeFromString(propertyTextValue, propertyInfo.PropertyType);
					if (value == null)
					{
						log.WarnFormat("Could not create instance of '{0}' with text value '{1}' for property '{2}'", 
						               propertyName, propertyInfo.PropertyType.FullName);
						continue;
					}
					ReflectionUtils.SetProperty(result, propertyInfo, value);
				}
				return result;
			}
			catch (Exception ex)
			{
				throw new SerializationException("KeyValueDataContractDeserializer: Error converting to type: " + ex.Message, ex);
			}
		}

		public To Parse<To>(IDictionary<string, string> keyValuePairs)
		{
			return (To)Parse(keyValuePairs, typeof(To));
		}
	}
}