#nullable enable

using System;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class HttpFile : IHttpFile
{
    public string Name { get; set; }
    public string FileName { get; set; }
    public long ContentLength { get; set; }
    public string ContentType { get; set; }
    public virtual Stream InputStream { get; set; }
}

#if NET6_0_OR_GREATER

public class HttpFileContent : HttpFile
{
    System.Net.Http.HttpContent content;
    public HttpFileContent(System.Net.Http.HttpContent content)
    {
        this.content = content;
        this.ContentType = content.Headers.ContentType?.MediaType ?? MimeTypes.Binary;
        if (content.Headers.ContentLength != null)
            this.ContentLength = content.Headers.ContentLength.Value;
        
        var contentDisposition = content.Headers.ContentDisposition;
        if (contentDisposition != null)
        {
            Name = contentDisposition.Name;
            FileName = contentDisposition.FileName;
            if (contentDisposition.Size != null)
                ContentLength = contentDisposition.Size.Value;
        }
    }

    public override Stream InputStream 
    { 
        get => base.InputStream ??= content.ReadAsStream(); 
        set => base.InputStream = value; 
    }
}


#endif

public static class HttpFileUtils
{
    public static HttpFile ToHttpFile(IHttpFile file, Action<HttpFile>? configure = null)
    {
        var to = new HttpFile
        {
            Name = file.Name,
            FileName = file.FileName,
            ContentLength = file.ContentLength,
            ContentType = file.ContentType,
            InputStream = file.InputStream,
        };

        configure?.Invoke(to);

        return to;
    }
}
