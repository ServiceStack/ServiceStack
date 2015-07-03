using System;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Serialization
{
    public partial class DataContractSerializer
    {
        public object DeserializeFromString(string xml, Type type)
        {
            try
            {
                return XmlSerializer.DeserializeFromString(xml, type);
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
