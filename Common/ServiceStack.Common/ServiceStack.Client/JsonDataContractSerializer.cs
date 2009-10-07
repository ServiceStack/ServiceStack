using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ServiceStack.Client
{
	public class JsonDataContractSerializer
	{
		public static JsonDataContractSerializer Instance = new JsonDataContractSerializer();

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
				throw new SerializationException("JsonDataContractSerializer: Error converting type: " + ex.Message, ex);
			}
		}
	}
}