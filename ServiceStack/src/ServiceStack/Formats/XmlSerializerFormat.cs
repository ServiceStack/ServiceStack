using System;
using System.IO;
using ServiceStack.Web;
using System.Xml.Serialization;

namespace ServiceStack.Formats;

/// <summary>
/// Use .NET XmlSerializer instead of .NET XML DataContract Serializer
/// </summary>
public class XmlSerializerFormat : IPlugin
{
    public static void Serialize(IRequest req, object response, Stream stream)
    {
        if (response == null || stream == null) return;
        var serializer = new XmlSerializer(response.GetType());
        serializer.Serialize(stream, response);
    }

    public static object Deserialize(Type type, Stream stream)
    {
        if (type == null || stream == null) return null;
        var serializer = new XmlSerializer(type);
        var obj = serializer.Deserialize(stream);
        return obj;
    }

    public void Register(IAppHost appHost)
    {
        appHost.ContentTypes.Register(MimeTypes.Xml, Serialize, Deserialize);
    }
}
