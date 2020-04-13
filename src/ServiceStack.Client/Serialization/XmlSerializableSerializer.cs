using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ServiceStack.Text;

namespace ServiceStack.Serialization
{
    public partial class XmlSerializableSerializer : IStringSerializer
    {
        public static XmlWriterSettings XmlWriterSettings { get; set; } = new XmlWriterSettings {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        
        public static XmlSerializableSerializer Instance = new XmlSerializableSerializer();

        public string SerializeToString<XmlDto>(XmlDto from)
        {
            try
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    using (XmlWriter xw = XmlWriter.Create(ms, XmlWriterSettings))
                    {
                        var ser = new XmlSerializerWrapper(from.GetType());
                        ser.WriteObject(xw, from);
                    }

                    return ms.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Error serializing object of type {from.GetType().FullName}", ex);
            }
        }
        
        public void SerializeToStream(object obj, Stream stream)
        {
            using (var xw = XmlWriter.Create(stream, XmlWriterSettings))
            {
                var ser = new XmlSerializerWrapper(obj.GetType());
                ser.WriteObject(xw, obj);
            }
        }
    }
}