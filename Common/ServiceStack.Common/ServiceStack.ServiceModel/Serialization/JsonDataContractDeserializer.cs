using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ServiceStack.ServiceModel.Serialization
{
	public class JsonDataContractDeserializer 
	{
		public static JsonDataContractDeserializer Instance = new JsonDataContractDeserializer();

		public object Parse(string json, Type returnType)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					var bytes = Encoding.Unicode.GetBytes(json);
					ms.Write(bytes, 0, bytes.Length);
					ms.Position = 0;
					var serializer = new DataContractJsonSerializer(returnType);
					return serializer.ReadObject(ms);
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException("JsonDataContractDeserializer: Error converting to type: " + ex.Message, ex);
			}
		}
		
		public To Parse<To>(string json)
		{
			return (To)Parse(json, typeof(To));
		}
	}
}