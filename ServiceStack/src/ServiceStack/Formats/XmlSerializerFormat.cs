using System;
using System.Collections.Concurrent;
using System.IO;
using ServiceStack.Web;
using System.Xml.Serialization;

namespace ServiceStack.Formats;

/// <summary>
/// Use .NET XmlSerializer instead of .NET XML DataContract Serializer
/// </summary>
public class XmlSerializerFormat : IPlugin
{
    private static readonly ConcurrentDictionary<Type, XmlSerializer> SerializerCache = new();

    public static XmlSerializer GetSerializer(Type type) =>
        type != null ? SerializerCache.GetOrAdd(type, t => new XmlSerializer(t)) : null;

    public static void Serialize(IRequest req, object response, Stream stream)
    {
        if (response == null || stream == null || stream == Stream.Null) return;
        var serializer = GetSerializer(response.GetType());
        serializer?.Serialize(stream, response);
    }

    public static object Deserialize(Type type, Stream stream)
    {
        if (type == null || stream == null || stream == Stream.Null) return null;
        var serializer = GetSerializer(type);
        return serializer?.Deserialize(stream);
    }

    public void Register(IAppHost appHost)
    {
        if (appHost == null)
            return;

        appHost.ContentTypes.Register(MimeTypes.Xml, Serialize, Deserialize);
    }
}
