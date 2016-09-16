#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
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
        public static XmlSerializableSerializer Instance = new XmlSerializableSerializer();

        public string SerializeToString<XmlDto>(XmlDto from)
        {
            try
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    using (XmlWriter xw = XmlWriter.Create(ms))
                    {
                        var ser = new XmlSerializerWrapper(from.GetType());
                        ser.WriteObject(xw, from);
                    }
                     
                    ms.Position = 0;
                    using (var reader = new StreamReader(ms))
                    {
                        return reader.ReadToEnd();
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