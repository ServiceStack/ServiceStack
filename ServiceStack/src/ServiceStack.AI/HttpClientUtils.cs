using System.Net.Http;
using System.Net.Http.Headers;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.AI;

internal static class HttpClientUtils
{
    internal static HttpContent ToHttpContent(this IVirtualFile file)
    {
        var fileContents = file.GetContents();
        
#if NET6_0_OR_GREATER
        HttpContent? httpContent = fileContents is ReadOnlyMemory<byte> romBytes
            ? new ReadOnlyMemoryContent(romBytes)
            : fileContents is string str
                ? new StringContent(str)
                : fileContents is ReadOnlyMemory<char> romChars
                    ? new ReadOnlyMemoryContent(romChars.ToUtf8())
                    : fileContents is byte[] bytes
                        ? new ByteArrayContent(bytes, 0, bytes.Length)
                        : null;

        if (httpContent != null)
            return httpContent;

        using var stream = fileContents as Stream ?? file.OpenRead();
        return new ReadOnlyMemoryContent(stream.ReadFullyAsMemory());
#else
        HttpContent? httpContent = fileContents is string str
                ? new StringContent(str)
                : fileContents is byte[] bytes
                        ? new ByteArrayContent(bytes, 0, bytes.Length)
                        : null;

        if (httpContent != null)
            return httpContent;

        var stream = fileContents as Stream ?? file.OpenRead();
        return new StreamContent(stream);
#endif
    }
    
    internal static MultipartFormDataContent AddFile(this MultipartFormDataContent content, string fieldName, IVirtualFile file, string? mimeType=null)
    {
        content.Add(file.ToHttpContent()
            .AddFileInfo(fieldName: fieldName, fileName: file.Name, mimeType: mimeType));
        return content;
    }

    internal static HttpContent AddFileInfo(this HttpContent content, string fieldName, string fileName, string? mimeType=null)
    {
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
            Name = fieldName,
            FileName = fileName,
        };
        return content;
    }

    internal static MultipartFormDataContent AddParam(this MultipartFormDataContent content, string key, string value)
    {
        content.Add(new StringContent(value), $"\"{key}\"");
        return content;
    }
}
