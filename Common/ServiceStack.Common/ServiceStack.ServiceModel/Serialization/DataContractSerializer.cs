using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ServiceStack.DesignPatterns.Serialization;

namespace ServiceStack.ServiceModel.Serialization
{
	public class DataContractSerializer : IStringSerializer 
	{
		public static DataContractSerializer Instance = new DataContractSerializer();

		public string Parse<XmlDto>(XmlDto from, bool indentXml)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					using (var xw = new XmlTextWriter(ms, Encoding.UTF8))
					{
						if (indentXml)
						{
							xw.Formatting = Formatting.Indented;	
						}

						var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
						serializer.WriteObject(xw, from);
						xw.Flush();
						ms.Seek(0, SeekOrigin.Begin);
						using (var reader = new StreamReader(ms))
						{
							return reader.ReadToEnd();
						}
					}
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

		public void CompressToStream<XmlDto>(XmlDto from, Stream stream)
		{
			using (var deflateStream = new DeflateStream(stream, CompressionMode.Compress))
			using (var xw = new XmlTextWriter(deflateStream, Encoding.UTF8))
			{
				var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
				serializer.WriteObject(xw, from);
				xw.Flush();
			}
		}

		public void SerializeToStream(object obj, Stream stream)
		{
			using (var xw = new XmlTextWriter(stream, Encoding.UTF8))
			{
				var serializer = new System.Runtime.Serialization.DataContractSerializer(obj.GetType());
				serializer.WriteObject(xw, obj);
			}
		}

		public byte[] Compress<XmlDto>(XmlDto from)
		{
			using (var ms = new MemoryStream())
			{
				CompressToStream(from, ms);
				
				return ms.ToArray();
			}
		}
	}
}