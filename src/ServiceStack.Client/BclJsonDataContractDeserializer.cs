using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ServiceStack.ServiceClient.Web
{
	public class BclJsonDataContractDeserializer
	{
		public static BclJsonDataContractDeserializer Instance = new BclJsonDataContractDeserializer();

		public object Parse(string json, Type returnType)
		{
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
				throw new SerializationException("BclJsonDataContractDeserializer: Error converting to type: " + ex.Message, ex);
			}
		}

		public To Parse<To>(string json)
		{
			return (To)Parse(json, typeof(To));
		}
	}

	public class JsonSerializer
	{
		public static object DeserializeFromStream(Type type, Stream stream)
		{
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			{
				return DeserializeFromString(reader.ReadToEnd(), type);
			}
		}

		public static void SerializeToStream<T>(T value, Stream stream)
		{
			BclJsonDataContractSerializer.Instance.SerializeToStream(value, stream);
		}

		public static object DeserializeFromString(string value, Type type)
		{
			return value == null
					? null
					: BclJsonDataContractDeserializer.Instance.Parse(value, type);
		}

	}
}