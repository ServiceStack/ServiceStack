using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Per-user publisher grants, with public origin defaults and atomic, isolated persistence.</summary>
public sealed class PublisherConfiguration(ExtensionContext ctx)
{
    public const string DefaultOrigin="https://ai.llmspy.org";
    public const string RegisterPath="/embed/register.html?domain=llmspy.org";
    static readonly ConcurrentDictionary<string,object> Locks=new(StringComparer.Ordinal);
    string PathOf(string? user)=>Path.Combine(ctx.GetUserPath(user),"share_llmspy","config.json");
    string LegacyPathOf(string? user)=>Path.Combine(ctx.GetUserPath(user),"publish","config.json");
    JsonObject ReadFor(string? user)=>Read(File.Exists(PathOf(user))?PathOf(user):LegacyPathOf(user));
    void ClearLegacy(string? user) { var path=LegacyPathOf(user);if(File.Exists(path))File.Delete(path); }
    object Gate(string? user)=>Locks.GetOrAdd(PathOf(user),_=>new object());
    static JsonObject Read(string path)
    {
        if(!File.Exists(path))return new JsonObject();
        try{return JsonNode.Parse(File.ReadAllText(path)) as JsonObject??throw new ArgumentException("Publisher config must be an object");}
        catch(System.Text.Json.JsonException e){throw new ArgumentException("Invalid publisher configuration",e);}
    }
    public JsonObject Get(string? user,bool obscure=true)
    {
        var own=ReadFor(user);var result=new JsonObject();
        if(!string.IsNullOrEmpty(user) && PathOf(user)!=PathOf(null)) {
            var defaults=ReadFor(null);
            foreach(var field in new[]{"baseUrl","allowHttp"})if(defaults.ContainsKey(field))result[field]=defaults[field]?.DeepClone();
        }
        result["apiKey"]=null;result["userName"]=null;result["userId"]=null;
        foreach(var pair in own)result[pair.Key]=pair.Value?.DeepClone();
        result["baseUrl"]??=DefaultOrigin;
        result["registerUrl"]??=result.GetString("baseUrl")!.TrimEnd('/')+RegisterPath;
        if(obscure && result.GetString("apiKey") is {Length:>0} key)result["apiKey"]=Mask(key);
        return result;
    }
    public static string Mask(string key)=>key[..Math.Min(3,key.Length)]+"******"+key[Math.Max(0,key.Length-4)..];
    public static string AccountIdentity(JsonObject config)=>PublisherClient.Origin(config)+"\0"+(config["userId"]?.ToJsonString()??config.GetString("userName")??"")+"\0"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(config.GetString("apiKey")??""))).ToLowerInvariant();
    static readonly HashSet<string> Writable=new(["apiKey","userName","userId","baseUrl","allowHttp","registerUrl"],StringComparer.Ordinal);
    public JsonObject Save(string? user,JsonObject patch)
    {
        lock(Gate(user)) {
            if(patch.Any(x=>!Writable.Contains(x.Key)))throw HttpError.BadRequest("Unknown publisher configuration field");
            foreach(var field in new[]{"apiKey","userName","baseUrl","registerUrl"})
                if(patch[field] is { } value && (value is not JsonValue scalar || !scalar.TryGetValue<string>(out _)))throw HttpError.BadRequest(field+" must be a string");
            if(patch["allowHttp"] is { } allow && (allow is not JsonValue boolean || !boolean.TryGetValue<bool>(out _)))throw HttpError.BadRequest("allowHttp must be a boolean");
            if(patch["userId"] is { } id && (id is not JsonValue identityValue || !identityValue.TryGetValue<string>(out _) && !identityValue.TryGetValue<long>(out _)))throw HttpError.BadRequest("userId must be a string or integer");
            var current=Get(user,false);var key=current.GetString("apiKey");
            foreach(var pair in patch) {
                if(pair.Key=="apiKey" && (string.IsNullOrEmpty(pair.Value?.GetValue<string>()) || key!=null && pair.Value?.GetValue<string>()==Mask(key)))continue;
                current[pair.Key]=pair.Value?.DeepClone();
            }
            if(current.GetString("apiKey") is { } grant && (grant.Length>16384 || grant.Any(char.IsWhiteSpace) || grant.Any(char.IsControl)))throw HttpError.BadRequest("Invalid publisher API key");
            PublisherClient.Origin(current);
            var before=Get(user,false);
            if(before.GetString("baseUrl")!=current.GetString("baseUrl") && !patch.ContainsKey("registerUrl"))current["registerUrl"]=current.GetString("baseUrl")!.TrimEnd('/')+RegisterPath;
            if(current.GetString("registerUrl") is { } registration && (!Uri.TryCreate(registration,UriKind.Absolute,out var url) || url.UserInfo.Length>0 || url.GetLeftPart(UriPartial.Authority)!=PublisherClient.Origin(current)))throw HttpError.BadRequest("Registration must use the configured publisher origin");
            if(AccountIdentity(before)!=AccountIdentity(current))current.Remove("avatars");
            Write(PathOf(user),current);ClearLegacy(user);return Get(user);
        }
    }
    public JsonObject Disconnect(string? user)
    {
        lock(Gate(user)){var path=PathOf(user);if(File.Exists(path))File.Delete(path);ClearLegacy(user);return Get(user);}
    }
    public bool SaveAvatar(string? user,JsonObject captured,string profile,string publishedUrl)
    {
        lock(Gate(user)) {
            var current=Get(user,false);if(AccountIdentity(current)!=AccountIdentity(captured))return false;
            var avatars=current.GetObject("avatars")??new JsonObject();avatars[profile]=publishedUrl;current["avatars"]=avatars;Write(PathOf(user),current);ClearLegacy(user);return true;
        }
    }
    static void Write(string path,JsonObject value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,value.ToJsonString(ChatJson.Indented));File.Move(temporary,path,true);}finally{File.Delete(temporary);}
    }
}
