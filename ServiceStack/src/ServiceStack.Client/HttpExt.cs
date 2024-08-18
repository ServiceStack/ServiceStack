using System.Threading.Tasks;

namespace ServiceStack;

public static class HttpExt
{
    public static bool HasNonAscii(string s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            foreach (var c in s)
            {
                if (c > 127)
                    return true;
            }
        }
        return false;
    }

    public static string GetDispositionFileName(string fileName)
    {
        if (!HasNonAscii(fileName))
            return $"filename=\"{fileName}\"";

        var encodedFileName = ClientConfig.EncodeDispositionFileName(fileName);
        return $"filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}";
    }

#if NET6_0_OR_GREATER
    public static async Task<string> SendJsonCallbackAsync<T>(this System.Net.Http.IHttpClientFactory clientFactory, string url, T body)
    {
        using var client = clientFactory.CreateClient(string.Empty);
        return await client.SendJsonCallbackAsync(url, body);
    }

    public static async Task<string> SendJsonCallbackAsync<T>(this System.Net.Http.HttpClient client, string url, T body)
    {
        var msg = ToJsonHttpRequestMessage(url, body);
        var response = await client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public static System.Net.Http.HttpRequestMessage ToJsonHttpRequestMessage<T>(string url, T body)
    {
        var msg = HttpUtils.ToHttpRequestMessage(url);
        var json = ClientConfig.ToJson(body);
        msg.WithHeader(HttpHeaders.Accept, MimeTypes.Json);
        msg.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, MimeTypes.Json);
        return msg;
    }
#endif
    
}