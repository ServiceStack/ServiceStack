using System;
using System.IO;
using ServiceStack.Text;
using ServiceStack.Text.Json;
using ServiceStack.Web;

namespace ServiceStack.Formats;

public class JsonlFormat : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Jsonl;
    
    public void Register(IAppHost appHost)
    {
        //Register the 'text/csv' content-type and serializers (format is inferred from the last part of the content-type)
        appHost.ContentTypes.Register(MimeTypes.Jsonl,
            SerializeToStream, JsonlSerializer.DeserializeFromStream);
    }

    public void SerializeToStream(IRequest req, object request, Stream stream)
    {
        switch (request)
        {
            case string str:
                stream.Write(str);
                break;
            case byte[] bytes:
                stream.Write(bytes, 0, bytes.Length);
                break;
            case Stream s:
                s.WriteTo(stream);
                break;
            case ReadOnlyMemory<char> roms:
                MemoryProvider.Instance.Write(stream, roms);
                break;
            case null:
                break;
            default:
                JsonlSerializer.SerializeToStream(request, stream);
                break;
        }
    }
}
