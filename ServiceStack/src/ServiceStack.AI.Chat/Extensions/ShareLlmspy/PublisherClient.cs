using System.Net;
using ServiceStack.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

/// <summary>Bounded publisher requests. One immutable account/origin snapshot per operation.</summary>
public sealed class PublisherClient
{
    public const int MaxJsonBytes=3*1024*1024;
    readonly JsonObject config;
    static readonly SocketsHttpHandler Transport=new() {AllowAutoRedirect=false,UseCookies=false,ConnectTimeout=TimeSpan.FromSeconds(5)};
    readonly Func<HttpMessageHandler> handlerFactory;
    readonly bool ownsHandler;
    public string BaseUrl {get;}
    public JsonObject Configuration=>config.Clone();
    public TimeSpan TotalTimeout {get;set;}=TimeSpan.FromSeconds(30);
    public TimeSpan ReadTimeout {get;set;}=TimeSpan.FromSeconds(15);
    /// <summary>Thread, project and media uploads (llms-py uses aiohttp's 5 minute default for these).</summary>
    public TimeSpan UploadTimeout {get;set;}=TimeSpan.FromMinutes(5);
    public PublisherClient(JsonObject configuration,Func<HttpMessageHandler>? handlerFactory=null)
    {
        config=configuration.Clone();BaseUrl=Origin(config);
        ownsHandler=handlerFactory!=null;
        this.handlerFactory=handlerFactory??(()=>Transport);
    }
    public static string Origin(JsonObject config)
    {
        var raw=(config.GetString("baseUrl")??PublisherConfiguration.DefaultOrigin).TrimEnd('/');
        if(!Uri.TryCreate(raw,UriKind.Absolute,out var uri) || uri.Scheme is not ("https" or "http") || uri.Host.Length==0 || uri.UserInfo.Length>0
            || uri.Query.Length>0 || uri.Fragment.Length>0 || uri.Scheme=="http" && !config.GetBool("allowHttp")
            || raw.Contains('\\') || raw[(raw.IndexOf("://",StringComparison.Ordinal)+3)..].Contains('/'))
            throw HttpError.BadRequest("Configure a valid HTTPS publisher origin (allowHttp is required for local tests).");
        return uri.GetLeftPart(UriPartial.Authority);
    }
    public static string PublicReference(string value)
    {
        if(!Regex.IsMatch(value??"",@"\A[A-Za-z0-9_-]{1,100}\z"))throw HttpError.BadRequest("Invalid public recipe reference.");return value;
    }
    public static string ReferenceFromUrl(JsonObject config,string value)
    {
        var origin=Origin(config);
        if(!Uri.TryCreate(value,UriKind.Absolute,out var uri) || uri.GetLeftPart(UriPartial.Authority)!=origin || uri.UserInfo.Length>0 || uri.Query.Length>0 || uri.Fragment.Length>0)
            throw HttpError.BadRequest("Paste a recipe link from the configured publisher host.");
        var match=Regex.Match(uri.AbsolutePath,@"\A/d/([A-Za-z0-9_-]{1,100})(?:\.json|/recipe\.json)?\z");
        if(!match.Success)throw HttpError.BadRequest("Paste a /d/ recipe page or JSON download link.");return match.Groups[1].Value;
    }
    Uri Url(string path)
    {
        if(!path.StartsWith('/') || path.StartsWith("//") || path.Contains('\\') || path.Contains('#') || path.Any(char.IsControl)
            || !Uri.TryCreate(BaseUrl+path,UriKind.Absolute,out var uri) || uri.GetLeftPart(UriPartial.Authority)!=BaseUrl
            || Uri.UnescapeDataString(path.Split('?')[0]).Split('/').Any(x=>x is "." or ".."))
            throw HttpError.BadRequest("Invalid publisher request path.");return uri;
    }
    public Task<JsonNode?> SendAsync(HttpMethod method,string path,JsonNode? body,bool authenticated,CancellationToken token=default)=>
        SendAsync(method,path,body,authenticated,upload:false,token);
    /// <summary>An upload is not a recipe publication: it has no 3 MB request cap and a longer total bound.</summary>
    public async Task<JsonNode?> SendAsync(HttpMethod method,string path,JsonNode? body,bool authenticated,bool upload,CancellationToken token=default)
    {
        using var request=new HttpRequestMessage(method,Url(path));
        if(body!=null) {
            byte[] bytes;
            try{bytes=Encoding.UTF8.GetBytes(body.ToJsonString(ChatJson.Options));}
            catch(Exception e) when(e is ArgumentException or System.Text.Json.JsonException){throw HttpError.BadRequest("Publication must contain valid finite JSON.");}
            if(!upload && bytes.Length>MaxJsonBytes)throw new HttpError(413,"RequestEntityTooLarge","Run a smaller worked example before sharing.");
            request.Content=new ByteArrayContent(bytes);request.Content.Headers.ContentType=new("application/json");
        }
        return await SendRequestAsync(request,authenticated,upload?UploadTimeout:TotalTimeout,token).ConfigAwait();
    }
    public async Task<JsonNode?> SendMultipartAsync(string path,MultipartFormDataContent content,CancellationToken token=default)
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,Url(path)){Content=content};
        return await SendRequestAsync(request,true,UploadTimeout,token).ConfigAwait();
    }
    static void ValidateFinite(System.Text.Json.JsonElement value)
    {
        if(value.ValueKind==System.Text.Json.JsonValueKind.Number && (!value.TryGetDouble(out var number) || !double.IsFinite(number)))
            throw new ArgumentException("Non-finite number");
        if(value.ValueKind==System.Text.Json.JsonValueKind.Object)foreach(var property in value.EnumerateObject())ValidateFinite(property.Value);
        if(value.ValueKind==System.Text.Json.JsonValueKind.Array)foreach(var item in value.EnumerateArray())ValidateFinite(item);
    }
    async Task<JsonNode?> SendRequestAsync(HttpRequestMessage request,bool authenticated,TimeSpan timeout,CancellationToken token)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if(authenticated) {
            var key=config.GetString("apiKey");if(string.IsNullOrEmpty(key))throw HttpError.Unauthorized("Connect publisher account first.");
            request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        }
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(timeout);
        using var client=new HttpClient(handlerFactory(),disposeHandler:ownsHandler){Timeout=Timeout.InfiniteTimeSpan};
        try {
            using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigAwait();
            // A custom host transport must also prohibit redirects; verify its final URI as a
            // defense in depth check. The default transport never follows them.
            if(response.RequestMessage?.RequestUri is { } final && final!=request.RequestUri)throw new HttpError(502,"BadGateway","Publisher redirects are not supported.");
            if(response.Content.Headers.ContentLength>MaxJsonBytes)throw new HttpError(502,"BadGateway","Publisher response exceeds the size limit.");
            await using var stream=await response.Content.ReadAsStreamAsync(deadline.Token).ConfigAwait();using var output=new MemoryStream();var buffer=new byte[65536];
            while(true) {
                using var read=CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);read.CancelAfter(ReadTimeout);
                var count=await stream.ReadAsync(buffer,read.Token).ConfigAwait();if(count==0)break;
                if(output.Length+count>MaxJsonBytes)throw new HttpError(502,"BadGateway","Publisher response exceeds the size limit.");output.Write(buffer,0,count);
            }
            var status=(int)response.StatusCode;
            if(status is 401 or 403)throw HttpError.Unauthorized("Reconnect publisher account.");
            if(status==404)throw HttpError.NotFound("Sharing is unavailable on this publisher, or the recipe was removed.");
            if(status==409)throw new HttpError(409,"Conflict","The public recipe changed. Refresh and review before trying again.");
            if(status==413)throw new HttpError(413,"RequestEntityTooLarge","Run a smaller worked example before sharing.");
            if(status==429)throw new HttpError(429,"TooManyRequests","Publisher is rate limiting requests. Wait a moment and retry.");
            if(status<200 || status>=300)throw new HttpError(502,"BadGateway",$"Publisher request failed (HTTP {status}). Try again later.");
            if(response.Content.Headers.ContentType?.MediaType!="application/json")throw new HttpError(502,"BadGateway","Publisher returned a non-JSON response.");
            try {
                using var document=System.Text.Json.JsonDocument.Parse(output.ToArray());
                ValidateFinite(document.RootElement);
                return JsonNode.Parse(new UTF8Encoding(false,true).GetString(output.ToArray()));
            }
            catch(Exception e) when(e is System.Text.Json.JsonException or DecoderFallbackException or ArgumentException){throw new HttpError(502,"BadGateway","Publisher returned invalid JSON.");}
        }
        catch(Exception e) when(e is HttpRequestException or IOException || e is OperationCanceledException && !token.IsCancellationRequested)
        {throw new HttpError(502,"BadGateway","Could not reach publisher. Retry to recover the same publication.");}
    }
}
