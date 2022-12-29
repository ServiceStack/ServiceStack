#if !LITE
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ServiceStack.Text;

namespace ServiceStack.Serialization
{
    public partial class DataContractSerializer : IStringSerializer 
    {
        /// <summary>
        /// Default MaxStringContentLength is 8k, and throws an exception when reached
        /// </summary>
        private readonly XmlDictionaryReaderQuotas quotas;

        public static DataContractSerializer Instance
            = new DataContractSerializer(new XmlDictionaryReaderQuotas { MaxStringContentLength = 1024 * 1024 });

        public DataContractSerializer(XmlDictionaryReaderQuotas quotas = null)
        {
            this.quotas = quotas;
        }

        public string Parse<XmlDto>(XmlDto from, bool indentXml)
        {
            try
            {
                if (Equals(@from, default(XmlDto))) return null;
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
                    var xw = new XmlTextWriter(ms, JsConfig.UTF8Encoding);
                    if (indentXml)
                    {
                        xw.Formatting = Formatting.Indented;
                    }

                    serializer.WriteObject(xw, from);
                    xw.Flush();

                    return ms.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Error serializing object of type {@from.GetType().FullName}", ex);
            }
        }

        public string SerializeToString<XmlDto>(XmlDto from)
        {
            return Parse(from, false);
        }

        public void SerializeToStream(object obj, Stream stream)
        {
            using (var xw = new XmlTextWriter(stream, JsConfig.UTF8Encoding))
            {
                var serializer = new System.Runtime.Serialization.DataContractSerializer(obj.GetType());
                serializer.WriteObject(xw, obj);
            }
        }

        public void CompressToStream<XmlDto>(XmlDto from, Stream stream)
        {
            using (var deflateStream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Compress))
            using (var xw = new XmlTextWriter(deflateStream, JsConfig.UTF8Encoding))
            {
                var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
                serializer.WriteObject(xw, from);
                xw.Flush();
            }
        }

        public byte[] Compress<XmlDto>(XmlDto from)
        {
            using (var ms = new MemoryStream()) //only use MS with .NET's incompat Compression classes
            {
                CompressToStream(from, ms);

                return ms.ToArray();
            }
        }

    }
}
#endif