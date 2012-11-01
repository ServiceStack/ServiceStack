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
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
        private readonly XmlDictionaryReaderQuotas quotas;
#endif

        public static DataContractDeserializer Instance 
            = new DataContractDeserializer(
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
                new XmlDictionaryReaderQuotas { MaxStringContentLength = 1024 * 1024, }
#endif
                );

        public DataContractDeserializer(
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
            XmlDictionaryReaderQuotas quotas=null
#endif
            )
        {
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
            this.quotas = quotas;
#endif
        }

        public object Parse(string xml, Type type)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(xml);

#if MONOTOUCH				
                using (var reader = XmlDictionaryReader.CreateTextReader(bytes, null))
#elif SILVERLIGHT && !WINDOWS_PHONE
                using (var reader = XmlDictionaryReader.CreateTextReader(bytes, XmlDictionaryReaderQuotas.Max))
#elif WINDOWS_PHONE
                using (var reader = XmlDictionaryReader.CreateBinaryReader(bytes, XmlDictionaryReaderQuotas.Max))
#else
                using (var reader = XmlDictionaryReader.CreateTextReader(bytes, this.quotas))
#endif
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
