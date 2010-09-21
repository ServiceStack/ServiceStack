using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.DesignPatterns.Serialization;

namespace SilverlightStack.Serialization
{
	public class DataContractDeserializer : IStringDeserializer
	{
		public static DataContractDeserializer Instance = new DataContractDeserializer();

		public To Parse<To>(string xml)
		{
			var type = typeof(To);
			return (To)Parse(xml, type);
		}

		public object Parse(string xml, Type type)
		{
			try
			{
				if (string.IsNullOrEmpty(xml)) throw new ArgumentNullException("xml");

				var bytes = Encoding.UTF8.GetBytes(xml);
				using (var ms = new MemoryStream(bytes))
				{
					var dcs = new System.Runtime.Serialization.DataContractSerializer(type);
					return dcs.ReadObject(ms);
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException("DeserializeDataContract: Error converting type: " + ex.Message, ex);
			}
		}
	}
}