#nullable enable
using System;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack;

public class StaticContent
{
    public ReadOnlyMemory<byte> Data { get; }
    public string MimeType { get; }

    public static StaticContent? CreateFromDataUri(string dataUri)
    {
        //data:image/jpg;base64,
        if (dataUri.StartsWith("data:"))
        {
            try
            {
                var uri = dataUri.AsSpan().Slice("data:".Length);
                var mimeType = uri.LeftPart(',');
                var isBase64 = false;
                if (mimeType.EndsWith(";base64"))
                {
                    isBase64 = true;
                    mimeType = mimeType.Slice(0, mimeType.Length - ";base64".Length);
                }
                    
                var fromMimeType = uri.Substring(mimeType.Length + 1);
                if (mimeType.Length < 100)
                {
                    var data = fromMimeType.RightPart(',');
                    if (isBase64)
                    {
                        var bytes = Convert.FromBase64String(data);
                        return new StaticContent(bytes, mimeType.ToString());
                    }
                    else
                    {
                        var decoded = data.UrlDecode();
                        var bytes = decoded.ToUtf8Bytes();
                        return new StaticContent(bytes, mimeType.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger(typeof(StaticContent)).Warn($"Could not CreateFromDataUri: {dataUri.SafeSubstring(50)}...", e);
            }
        }
        return null;
    }
    
    public StaticContent(ReadOnlyMemory<byte> data, string mimeType)
    {
        Data = data;
        MimeType = mimeType;
    }
}