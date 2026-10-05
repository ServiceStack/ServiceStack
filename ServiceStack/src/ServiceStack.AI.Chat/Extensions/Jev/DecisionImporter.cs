using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using static ServiceStack.AI.DecisionRecipeValidator;

namespace ServiceStack.AI;

/// <summary>Unauthenticated portable exports, revalidating each redirect through host download policy.</summary>
public sealed class DecisionImporter(ChatFeature feature,Func<HttpMessageHandler>? handlerFactory=null)
{
    static readonly SocketsHttpHandler Transport=new(){AllowAutoRedirect=false,UseCookies=false,ConnectTimeout=TimeSpan.FromSeconds(5)};
    public TimeSpan TotalTimeout {get;set;}=TimeSpan.FromSeconds(30);
    public TimeSpan ReadTimeout {get;set;}=TimeSpan.FromSeconds(15);
    public static string Url(JsonObject config,JsonNode? value)
    {
        var raw=DecisionRecipeValidator.Text(value,"url",4096).Trim();
        Require(Uri.TryCreate(raw,UriKind.Absolute,out var uri)&&uri.Scheme is "https" or "http"&&uri.Host.Length>0&&uri.UserInfo.Length==0&&!raw.Contains('\\'),"url","Enter an HTTP or HTTPS URL without credentials.");
        foreach(var origin in new[]{config,new JsonObject {["baseUrl"]=PublisherConfiguration.DefaultOrigin}})
            try{var clean=new UriBuilder(uri!){Query="",Fragment=""}.Uri.ToString();var reference=PublisherClient.ReferenceFromUrl(origin,clean);return PublisherClient.Origin(origin)+"/d/"+reference+".json";}catch(HttpError){}
        return raw;
    }
    public async Task<JsonObject> DownloadAsync(string url,CancellationToken token)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TotalTimeout);
        using var client=new HttpClient(handlerFactory?.Invoke()??Transport,disposeHandler:handlerFactory!=null){Timeout=Timeout.InfiniteTimeSpan};
        try{
            for(var attempt=0;attempt<6;attempt++){
                url=Url(new JsonObject(),JsonValue.Create(url));feature.ValidateDownloadUrl?.Invoke(url);
                using var request=new HttpRequestMessage(HttpMethod.Get,url);request.Headers.Accept.ParseAdd("application/json");
                using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token).ConfigureAwait(false);
                if(response.RequestMessage?.RequestUri is { } final&&final!=request.RequestUri)throw new JevValidationException("url","Use a download transport that disables automatic redirects.");
                if((int)response.StatusCode is 301 or 302 or 303 or 307 or 308){Require(response.Headers.Location!=null,"url","The download redirect has no destination.");url=Url(new JsonObject(),JsonValue.Create(new Uri(new Uri(url),response.Headers.Location!).ToString()));continue;}
                Require(response.IsSuccessStatusCode,"url",$"Recipe download failed (HTTP {(int)response.StatusCode}).");
                Require(!(response.Content.Headers.ContentLength>MaxBytes),"url","Keep the recipe JSON under 512 KB.");
                await using var stream=await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);using var output=new MemoryStream();var buffer=new byte[65536];
                while(true){using var read=CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);read.CancelAfter(ReadTimeout);var count=await stream.ReadAsync(buffer,read.Token).ConfigureAwait(false);if(count==0)break;Require(output.Length+count<=MaxBytes,"url","Keep the recipe JSON under 512 KB.");output.Write(buffer,0,count);}
                JsonNode? value;try{value=JsonNode.Parse(new UTF8Encoding(false,true).GetString(output.ToArray()));}catch(Exception e) when(e is System.Text.Json.JsonException or DecoderFallbackException){throw new JevValidationException("url","The URL must return a recipe JSON export.");}
                var document=Validate(value);var disposition=response.Content.Headers.ContentDisposition;var filename=(disposition?.FileNameStar??disposition?.FileName)?.Trim('"')??Uri.UnescapeDataString(new Uri(url).AbsolutePath.Split('/').Last());
                try{filename=JevPaths.Filename(filename);}catch(JevValidationException){filename=JevPaths.DefaultFilename(document.GetString("name")!);}
                return new JsonObject {["document"]=document,["filename"]=filename,["downloadUrl"]=url};
            }
            throw new JevValidationException("url","Too many recipe download redirects.");
        }catch(Exception e) when(e is HttpRequestException or IOException||e is OperationCanceledException&&!token.IsCancellationRequested){throw new JevValidationException("url","Could not download recipe JSON. Check the URL and try again.");}
    }
}
