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
        private static readonly Encoding Encoding = Encoding.UTF8;// new UTF8Encoding(true);

        /// <summary>
        /// Default MaxStringContentLength is 8k, and throws an exception when reached
        /// </summary>
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
        private readonly XmlDictionaryReaderQuotas quotas;
#endif

        public static DataContractSerializer Instance
            = new DataContractSerializer(
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
new XmlDictionaryReaderQuotas { MaxStringContentLength = 1024 * 1024, }
#endif
);

        public DataContractSerializer(
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
XmlDictionaryReaderQuotas quotas = null
#endif
)
        {
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            this.quotas = quotas;
#endif
        }

        public string Parse<XmlDto>(XmlDto from, bool indentXml)
        {
            try
            {
                if (Equals(@from, default(XmlDto))) return null;
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
                    var xw = new XmlTextWriter(ms, Encoding); 
                    if (indentXml)
                    {
                        xw.Formatting = Formatting.Indented;	
                    }

                    serializer.WriteObject(xw, from);
                    xw.Flush();
#else
                    serializer.WriteObject(ms, from);
#endif

                    ms.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(ms);
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(string.Format("Error serializing object of type {0}", from.GetType().FullName), ex);
            }
        }

        public string SerializeToString<XmlDto>(XmlDto from)
        {
            return Parse(from, false);
        }


        public void SerializeToStream(object obj, Stream stream)
        {
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            using (var xw = new XmlTextWriter(stream, Encoding))
            {
                var serializer = new System.Runtime.Serialization.DataContractSerializer(obj.GetType());
                serializer.WriteObject(xw, obj);
            }
#else
            var serializer = new System.Runtime.Serialization.DataContractSerializer(obj.GetType());
            serializer.WriteObject(stream, obj);
#endif
        }

#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
        public void CompressToStream<XmlDto>(XmlDto from, Stream stream)
        {
            using (var deflateStream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Compress))
            using (var xw = new XmlTextWriter(deflateStream, Encoding))
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
#endif

    }
}
