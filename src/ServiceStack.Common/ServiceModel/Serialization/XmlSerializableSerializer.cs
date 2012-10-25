#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.ServiceModel.Support;

namespace ServiceStack.ServiceModel.Serialization
{
    public class XmlSerializableSerializer : IStringSerializer 
    {
        public static XmlSerializableSerializer Instance = new XmlSerializableSerializer();

        public string Parse<XmlDto>(XmlDto from)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    using (XmlWriter xw = new XmlTextWriter(ms, Encoding.UTF8))
                    {
                        var ser = new XmlSerializerWrapper(from.GetType());
                        ser.WriteObject(xw, from);
                        xw.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(string.Format("Error serializing object of type {0}", from.GetType().FullName), ex);
            }
        }
    }
}
#endif