#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Serialization;

namespace ServiceStack.ServiceModel.Serialization
{
    public class XmlSerializableDeserializer : IStringDeserializer
    {
        public static XmlSerializableDeserializer Instance = new XmlSerializableDeserializer();

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
                    var serializer = new System.Xml.Serialization.XmlSerializer(type);
                    return serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(string.Format("Error serializing object of type {0}", type.FullName), ex);
            }
        }

        public To Parse<To>(TextReader from)
        {
            var type = typeof(To);
            try
            {
                using (from)
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(type);
                    return (To)serializer.Deserialize(from);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(string.Format("Error serializing object of type {0}", type.FullName), ex);
            }
        }

        public To Parse<To>(Stream from)
        {
            var type = typeof(To);
            try
            {
                using (var reader = XmlDictionaryReader.CreateTextReader(from, new XmlDictionaryReaderQuotas()))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(type);
                    return (To)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(string.Format("Error serializing object of type {0}", type.FullName), ex);
            }
        }
    }
}
#endif
