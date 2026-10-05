using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Portable import.json settings; store IDs and runtime ownership never enter the manifest.</summary>
public static class GeminiImportManifest
{
    public const string Filename="import.json";
    public static readonly string[] SourceFields=["name","type","category","extract","chunking","volatile","onDelete","extractorVer"];
    public static JsonArray Patterns(JsonNode? value)
    {
        if(value==null)return new JsonArray();
        if(value is JsonValue text && text.TryGetValue<string>(out var input))return new JsonArray(input.Replace('\n',',').Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(x=>(JsonNode)JsonValue.Create(x.Replace('\\','/'))!).ToArray());
        if(value is not JsonArray array || array.Any(x=>x is not JsonValue item || !item.TryGetValue<string>(out var pattern) || string.IsNullOrWhiteSpace(pattern)))throw new ArgumentException("Include, exclude and ignore must contain file or folder patterns");
        return new JsonArray(array.Select(x=>(JsonNode)JsonValue.Create(x!.GetValue<string>().Replace('\\','/'))!).ToArray());
    }
    public static JsonObject Read(string path)
    {
        if(!File.Exists(path))return new JsonObject();
        try {
            var value=JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new ArgumentException("import.json must contain a JSON object");
            Validate(value);return value;
        } catch(System.Text.Json.JsonException e) {throw new ArgumentException("Invalid import.json: "+e.Message,e);}
    }
    public static void Validate(JsonObject value)
    {
        if(value.ContainsKey("version") && (value["version"] is not JsonValue version || !version.TryGetValue<int>(out var number) || number!=1))throw new ArgumentException("Unsupported import.json version");
        foreach(var field in new[]{"source","metadata","crawl"})if(value[field]!=null && value[field] is not JsonObject)throw new ArgumentException("import.json "+field+" must be an object");
        foreach(var field in new[]{"include","exclude","ignore"})Patterns(value[field]);
        foreach(var field in new[]{"name","type","onDelete","extractorVer"})
            if(value.GetObject("source")?[field] is { } scalar && (scalar is not JsonValue text || !text.TryGetValue<string>(out _)))throw new ArgumentException("Source "+field+" must be a string");
        foreach(var field in new[]{"category","extract","chunking"})
            if(value.GetObject("source")?[field] is { } obj && obj is not JsonObject)throw new ArgumentException("Source "+field+" must be an object");
        foreach(var field in new[]{"url","name"})
            if(value.GetObject("crawl")?[field] is { } scalar && (scalar is not JsonValue text || !text.TryGetValue<string>(out _)))throw new ArgumentException("Crawl "+field+" must be a string");
        if(value.GetObject("source")?["volatile"] is { } patterns && (patterns is not JsonArray array || array.Any(x=>x is not JsonValue text || !text.TryGetValue<string>(out _))))throw new ArgumentException("Source volatile must be an array of regex strings");
        var settings=value.GetObject("source");
        if(settings!=null) {
            if(settings["config"]!=null && settings["config"] is not JsonObject)throw new ArgumentException("import.json source config must be an object");
            if(settings.GetString("type") is { } type && type is not ("folder" or "zip"))throw new ArgumentException("Unknown source type");
            foreach(var field in new[]{"include","exclude","ignore"})Patterns(settings.GetObject("config")?[field]);
            if(settings.GetObject("config")?["path"] is { } path && (path is not JsonValue text || !text.TryGetValue<string>(out _)))throw new ArgumentException("Source path must be a string");
        }
        var metadata=value.GetObject("metadata");
        if(metadata!=null && (metadata["defaults"]!=null && metadata["defaults"] is not JsonObject || metadata["rules"]!=null && metadata["rules"] is not JsonArray))throw new ArgumentException("Invalid import.json metadata defaults or rules");
        var crawl=value.GetObject("crawl");
        if(crawl!=null) {
            if(crawl["rules"]!=null && crawl["rules"] is not JsonArray)throw new ArgumentException("Crawl rules must be an array");
            GeminiExtension.ValidateCrawlRules(crawl.GetArray("rules"));
        }
        if(value["transforms"]!=null && value["transforms"] is not JsonArray)throw new ArgumentException("Regex transforms must be an array");
        if(value.GetArray("transforms") is { } transforms)GeminiExtension.ValidateTransforms(transforms);
    }
    public static JsonObject Load(string path,JsonObject? fallback=null)
    {
        path=GeminiIngest.ResolvePath(path);
        if(Path.GetFileName(path)!=Filename || !File.Exists(path))throw new ArgumentException("Select an existing import.json");
        var cfg=Read(path);var settings=cfg.GetObject("source")??new JsonObject();var row=fallback?.Clone()??new JsonObject();
        if(cfg.ContainsKey("source"))foreach(var key in SourceFields)row.Remove(key);
        foreach(var key in SourceFields)if(settings.ContainsKey(key))row[key]=settings[key]?.DeepClone();
        row["type"]??="folder";if(row.GetString("type") is not ("folder" or "zip"))throw new ArgumentException("Unknown source type");
        var config=settings.GetObject("config")?.Clone()??(!cfg.ContainsKey("source")?fallback.GetObject("config")?.Clone():null)??new JsonObject();
        var folder=Path.GetDirectoryName(path)!;
        var input=config.GetString("path");config["path"]=GeminiIngest.ResolvePath(Path.Combine(folder,string.IsNullOrEmpty(input)?".":input));config["manifestPath"]=path;config["metadataSpecified"]=false;
        foreach(var key in new[]{"include","exclude","ignore"})config[key]=Patterns(config.ContainsKey(key)?config[key]:cfg[key]);
        row["config"]=config;row["rules"]=cfg.GetObject("metadata")?.Clone()??new JsonObject { ["defaults"]=new JsonObject(),["rules"]=new JsonArray() };
        if(!settings.ContainsKey("category") && row.GetObject("rules").GetObject("defaults")?.ContainsKey("category")==true) {var category=row.GetObject("category")?.Clone()??new JsonObject();category["prefix"]=row.GetObject("rules").GetObject("defaults")!["category"]?.DeepClone();row["category"]=category;}
        if(string.IsNullOrEmpty(row.GetString("name")))row["name"]="Import "+Path.GetFileName(config.GetString("path"));
        return row;
    }
    public static async Task<string> SaveAsync(JsonObject source,JsonObject? options=null)
    {
        var config=source.GetObject("config")?.Clone()??throw new ArgumentException("Source config is required");
        var folder=GeminiIngest.ResolvePath(config.GetString("path")??throw new ArgumentException("Source path is required"));
        var manifestDir=source.GetString("type")=="folder"?folder:Path.GetDirectoryName(folder)!;
        var path=GeminiIngest.ResolvePath(config.GetString("manifestPath")??Path.Combine(manifestDir,Filename));
        var cfg=Read(path);config.Remove("manifestPath");config.Remove("metadataSpecified");config.Remove("saved");config["path"]=Path.GetRelativePath(Path.GetDirectoryName(path)!,folder).Replace('\\','/');
        var settings=new JsonObject();foreach(var key in SourceFields)if(source.ContainsKey(key))settings[key]=source[key]?.DeepClone();settings["config"]=config;
        cfg["version"]=1;cfg["source"]=settings;
        if(options!=null)foreach(var key in new[]{"crawl","transforms"})if(options.ContainsKey(key))cfg[key]=options[key]?.DeepClone();
        if(source.GetObject("config").GetBool("metadataSpecified") || !cfg.ContainsKey("metadata"))cfg["metadata"]=source.GetObject("rules")?.Clone()??new JsonObject { ["defaults"]=new JsonObject(),["rules"]=new JsonArray() };
        Validate(cfg);await WriteAsync(path,cfg).ConfigureAwait(false);return path;
    }
    public static async Task WriteAsync(string path,JsonObject value)
    {
        Validate(value);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {await File.WriteAllTextAsync(temporary,value.ToJsonString(ChatJson.Indented)+"\n").ConfigureAwait(false);File.Move(temporary,path,true);}finally{File.Delete(temporary);}
    }
    public static bool Ignored(string key,IEnumerable<string> patterns)=>patterns.Any(p=>GeminiIngest.GlobMatch(key,p) || key.StartsWith(p.TrimEnd('/')+"/",StringComparison.Ordinal) || GeminiIngest.GlobMatch(key+"/x",p.TrimEnd('/')+"/**"));
}
