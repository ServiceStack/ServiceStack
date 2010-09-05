using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.ServiceModel.Serialization
{
	public class JsonDataContractDeserializer 
	{
		public static JsonDataContractDeserializer Instance = new JsonDataContractDeserializer();

		public bool UseBcl { get; set; }

		public object Parse(string json, Type returnType)
		{
			if (!UseBcl)
			{
				return JsonSerializer.DeserializeFromString(json, returnType);
			}

			try
			{
				using (var ms = new MemoryStream())
				{
					var bytes = Encoding.UTF8.GetBytes(json);
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
		
		public T Parse<T>(string json)
		{
			if (UseBcl)
			{
				return (T)Parse(json, typeof(T));
			}
			return JsonSerializer.DeserializeFromString<T>(json);
		}

		public T DeserializeFromStream<T>(Stream stream)
		{
			if (UseBcl)
			{
				var serializer = new DataContractJsonSerializer(typeof(T));
				return (T)serializer.ReadObject(stream);				
			}
			return JsonSerializer.DeserializeFromStream<T>(stream);
		}
	}
}