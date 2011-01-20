using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace ServiceStack.ServiceClient.Web
{
	public class DataContractSerializer
	{
		public static DataContractSerializer Instance = new DataContractSerializer();

		public string Parse<XmlDto>(XmlDto from, bool indentXml)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					var dcs = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
					dcs.WriteObject(ms, from);
					return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException(string.Format("Error serializing object of type {0}", from.GetType().FullName), ex);
			}
		}

		public string SerializeToString<XmlDto>(XmlDto from)
		{
			var sb = new StringBuilder();
			try
			{
				using (var writer = new XmlTextWriter(new StringWriter(sb)))
				{
					var dcs = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
					dcs.WriteObject(writer, from);
					return sb.ToString();
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException(string.Format("Error serializing object of type {0}", from.GetType().FullName), ex);
			}
		}

		public string Parse<XmlDto>(XmlDto from)
		{
			return Parse(from, false);
		}
	}
}