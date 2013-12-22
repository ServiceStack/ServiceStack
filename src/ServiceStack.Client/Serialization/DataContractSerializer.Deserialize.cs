using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

namespace ServiceStack.Serialization
{

    public partial class DataContractSerializer
    {
        public object DeserializeFromString(string xml, Type type)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(xml);

#if IOS				
                using (var reader = XmlDictionaryReader.CreateTextReader(bytes, null))
#elif SL5 && !WP
                using (var reader = XmlDictionaryReader.CreateTextReader(bytes, XmlDictionaryReaderQuotas.Max))
#elif WP
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

        public T DeserializeFromString<T>(string xml)
        {
            var type = typeof(T);
            return (T)DeserializeFromString(xml, type);
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
            return (T)serializer.ReadObject(stream);
        }

    }


}
