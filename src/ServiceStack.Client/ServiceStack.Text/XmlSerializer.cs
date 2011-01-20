using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace ServiceStack.Text
{
	public class XmlSerializer
	{
		private readonly XmlDictionaryReaderQuotas quotas;

		public static XmlSerializer Instance 
			= new XmlSerializer(new XmlDictionaryReaderQuotas {
				MaxStringContentLength = 1024 * 1024,
			});

		public XmlSerializer(XmlDictionaryReaderQuotas quotas)
		{
			this.quotas = quotas;
		}

		private static object Deserialize(string xml, Type type, XmlDictionaryReaderQuotas quotas)
		{
			try
			{
				var bytes = Encoding.UTF8.GetBytes(xml);
				
				using (var reader = XmlDictionaryReader.CreateTextReader(bytes, quotas))
				{
					var serializer = new System.Runtime.Serialization.DataContractSerializer(type);
					return serializer.ReadObject(reader);
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException("DeserializeDataContract: Error converting type: " + ex.Message, ex);
			}
		}

		public static object DeserializeFromString(string xml, Type type)
		{
			return Deserialize(xml, type, Instance.quotas);
		}

		public static T DeserializeFromString<T>(string xml)
		{
			var type = typeof(T);
			return (T)Deserialize(xml, type, Instance.quotas);
		}

		public static T DeserializeFromReader<T>(TextReader reader)
		{
			return DeserializeFromString<T>(reader.ReadToEnd());
		}

		public static T DeserializeFromStream<T>(Stream stream)
		{
			var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));

			return (T)serializer.ReadObject(stream);
		}

		public static object DeserializeFromStream(Type type, Stream stream)
		{
			var serializer = new System.Runtime.Serialization.DataContractSerializer(type);
			return serializer.ReadObject(stream);
		}

		public static string SerializeToString<T>(T from)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					using (var xw = new XmlTextWriter(ms, Encoding.UTF8))
					{
						if (false)
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

		public static void SerializeToWriter<T>(T value, TextWriter writer)
		{
			try
			{
				using (var xw = new XmlTextWriter(writer))
				{
					var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
					serializer.WriteObject(xw, value);
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException(string.Format("Error serializing object of type {0}", typeof(T).FullName), ex);
			}
		}

		public static void CompressToStream<TXmlDto>(TXmlDto from, Stream stream)
		{
			using (var deflateStream = new DeflateStream(stream, CompressionMode.Compress))
			using (var xw = new XmlTextWriter(deflateStream, Encoding.UTF8))
			{
				var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
				serializer.WriteObject(xw, from);
				xw.Flush();
			}
		}

		public static void SerializeToStream(object obj, Stream stream)
		{
			using (var xw = new XmlTextWriter(stream, Encoding.UTF8))
			{
				var serializer = new System.Runtime.Serialization.DataContractSerializer(obj.GetType());
				serializer.WriteObject(xw, obj);
			}
		}

		public static byte[] Compress<TXmlDto>(TXmlDto from)
		{
			using (var ms = new MemoryStream())
			{
				CompressToStream(from, ms);

				return ms.ToArray();
			}
		}		
	}
}