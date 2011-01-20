using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ServiceStack.ServiceClient.Web
{
	public class BinaryFormatterDeserializer
	{
		public static BinaryFormatterDeserializer Instance = new BinaryFormatterDeserializer();
		readonly BinaryFormatter formatter = new BinaryFormatter();

		public To Deserialize<To>(byte[] bytes)
		{
			var type = typeof(To);
			return (To)Deserialize(bytes, type);
		}

		public object Deserialize(byte[] bytes, Type type)
		{
			try
			{
				if (bytes == null) throw new ArgumentNullException("bytes");

				using (var ms = new MemoryStream(bytes))
				{
					var obj = formatter.Deserialize(ms);
					return obj;
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException("BinaryFormatterDeserializer: Error converting type: " + ex.Message, ex);
			}
		}
	}

}