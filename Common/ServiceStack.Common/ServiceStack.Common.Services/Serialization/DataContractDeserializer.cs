using System;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using ServiceStack.Common.DesignPatterns.Serialization;

namespace ServiceStack.Common.Services.Serialization
{
    public class DataContractDeserializer : IXmlDeserializer
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
                var bytes = Encoding.UTF8.GetBytes(xml);
                using (var reader = XmlDictionaryReader.CreateTextReader(bytes, new XmlDictionaryReaderQuotas()))
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
    }
}