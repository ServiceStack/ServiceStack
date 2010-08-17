using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ServiceStack.Text;

namespace ServiceStack.ServiceModel.Serialization
{
	public class JsonDataContractSerializer 
	{
		public static JsonDataContractSerializer Instance = new JsonDataContractSerializer();

		public bool UseBcl { get; set; }

		public string Parse<T>(T obj)
		{
			if (!UseBcl)
			{
				return JsonSerializer.SerializeToString(obj);
			}

			if (obj == null) return null;
			var type = obj.GetType();
			try
			{
				using (var ms = new MemoryStream())
				{
					var serializer = new DataContractJsonSerializer(type);
					serializer.WriteObject(ms, obj);
					ms.Position = 0;
					using (var sr = new StreamReader(ms))
					{
						return sr.ReadToEnd();
					}
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException("JsonDataContractSerializer: Error converting type: " + ex.Message, ex);
			}
		}

		public void SerializeToStream<T>(T obj, Stream stream)
		{
			if (UseBcl)
			{
				var serializer = new DataContractJsonSerializer(obj.GetType());
				serializer.WriteObject(stream, obj);
			}
			else
			{
				JsonSerializer.SerializeToStream(obj, stream);
			}
		}
	}
}