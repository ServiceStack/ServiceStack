using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceModel.Serialization
{
	public class JsonDataContractDeserializer 
	{
		public static JsonDataContractDeserializer Instance = new JsonDataContractDeserializer();

		public ITextSerializer TextSerializer { get; set; }

		public bool UseBcl { get; set; }

		public object DeserializeFromString(string json, Type returnType)
		{
			if (TextSerializer != null)
				return TextSerializer.DeserializeFromString(json, returnType);

			if (!UseBcl)
				return JsonSerializer.DeserializeFromString(json, returnType);

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

		public T DeserializeFromString<T>(string json)
		{
			if (TextSerializer != null)
				return TextSerializer.DeserializeFromString<T>(json);

			if (UseBcl)
				return (T)DeserializeFromString(json, typeof(T));

			return JsonSerializer.DeserializeFromString<T>(json);
		}

		public T DeserializeFromStream<T>(Stream stream)
		{
			if (TextSerializer != null)
				return TextSerializer.DeserializeFromStream<T>(stream);

			if (UseBcl)
			{
				var serializer = new DataContractJsonSerializer(typeof(T));
				return (T)serializer.ReadObject(stream);				
			}
			return JsonSerializer.DeserializeFromStream<T>(stream);
		}

		public object DeserializeFromStream(Type type, Stream stream)
		{
			if (TextSerializer != null)
				return TextSerializer.DeserializeFromStream(type, stream);

			if (UseBcl)
			{
				var serializer = new DataContractJsonSerializer(type);
				return serializer.ReadObject(stream);
			}
			return JsonSerializer.DeserializeFromStream(type, stream);
		}
	}
}