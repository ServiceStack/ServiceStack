using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ServiceStack.ServiceClient.Web
{
	public class BinaryFormatterSerializer
	{
		public static BinaryFormatterSerializer Instance = new BinaryFormatterSerializer();
		readonly BinaryFormatter formatter = new BinaryFormatter();

		public byte[] Serialize<TSerializable>(TSerializable graph, bool indentXml)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					formatter.Serialize(ms, graph);
					ms.Flush();
					return ms.ToArray();
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException(
					string.Format("Error serializing object of type {0}", graph.GetType().FullName), ex);
			}
		}

		public byte[] Serialize<TSerializable>(TSerializable graph)
		{
			return Serialize(graph, false);
		}
	}
}