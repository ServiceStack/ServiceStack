using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using ServiceStack.DesignPatterns.Serialization;

namespace ServiceStack.ServiceModel.Serialization
{

	public class DataContractDeserializer : IStringDeserializer
	{

		/// <summary>
		/// Default MaxStringContentLength is 8k, and throws an exception when reached
		/// </summary>
		private readonly XmlDictionaryReaderQuotas quotas;

		public static DataContractDeserializer Instance 
			= new DataContractDeserializer(new XmlDictionaryReaderQuotas {
				MaxStringContentLength = 1024 * 1024,
			});

		public DataContractDeserializer(XmlDictionaryReaderQuotas quotas)
		{
			this.quotas = quotas;
		}

		public object Parse(string xml, Type type)
		{
			try
			{
				var bytes = Encoding.UTF8.GetBytes(xml);
				
				using (var reader = XmlDictionaryReader.CreateTextReader(bytes, this.quotas))
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

		public T Parse<T>(string xml)
		{
			var type = typeof(T);
			return (T)Parse(xml, type);
		}

		public T DeserializeFromStream<T>(Stream stream)
		{
			var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
			return (T)serializer.ReadObject(stream);
		}

	}


}
