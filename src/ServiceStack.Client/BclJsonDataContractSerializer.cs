using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ServiceStack.ServiceClient.Web
{
	public class BclJsonDataContractSerializer
	{
		public static BclJsonDataContractSerializer Instance = new BclJsonDataContractSerializer();

		public string Parse(object obj)
		{
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
				throw new SerializationException("BclJsonDataContractSerializer: Error converting type: " + ex.Message, ex);
			}
		}

		public void SerializeToStream<T>(T value, Stream stream)
		{
			var serializer = new DataContractJsonSerializer(typeof(T));
			serializer.WriteObject(stream, value);
		}

	}
}