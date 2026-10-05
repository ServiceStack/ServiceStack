using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    static readonly ConcurrentDictionary<string,SemaphoreSlim> ImportLocks=new(StringComparer.Ordinal);
    static readonly ConcurrentDictionary<string,SemaphoreSlim> SourceRunLocks=new(StringComparer.Ordinal);
    string ImportLockKey(string? user)=>Feature.AppData.BasePath+"\0"+user;
    static readonly ConcurrentDictionary<string,SemaphoreSlim> SourceOperationLocks=new(StringComparer.Ordinal);
    async Task<object?> SourceOperationAsync(ChatRequestContext req,Func<ChatRequestContext,Task<object?>> operation)
    {
        // One writer per user's manifests, including read routes which relocate a legacy crawl.
        // This is asynchronous coordination; it never holds a database transaction over HTTP.
        var gate=SourceOperationLocks.GetOrAdd(ImportLockKey(UserOf(req)),_=>new SemaphoreSlim(1,1));
        await gate.WaitAsync(req.Request.RequestAborted).ConfigureAwait(false);
        try{return await operation(req).ConfigureAwait(false);}finally{gate.Release();}
    }
    async Task<string> RequestCrawlRootAsync(ChatRequestContext req,long? sourceId=null,long? storeId=null)
    {
        sourceId??=long.TryParse(req.QueryString("sourceId"),out var id)?id:null;
        storeId??=long.TryParse(req.QueryString("filestoreId"),out var store)?store:null;
        if(sourceId!=null)return (await SelectedCrawlAsync(req,sourceId.Value,storeId).ConfigureAwait(false)).Root;
        return CrawlWorkspace(UserOf(req),req.GetPathParam("name"));
    }
    static readonly HashSet<string> JsonSourceFields=new(["config","category","rules","include","exclude","extract","chunking","volatile","cursor"]);
    static void ApplySourceSettings(ChatSource source,JsonObject value)
    {
        foreach(var field in GeminiImportManifest.SourceFields.Concat(["config","rules","enabled"])) {
            if(!value.ContainsKey(field))continue;
            var property=typeof(ChatSource).GetProperty(char.ToUpperInvariant(field[0])+field[1..])!;
            property.SetValue(source,field=="enabled"?value.GetBool(field):JsonSourceFields.Contains(field)?value[field]?.ToJsonString(ChatJson.Options):value.GetString(field));
        }
    }
    JsonObject EditableSource(ChatSource source,ChatRequestContext req)
    {
        var row=source.ToDto();var config=row.GetObject("config");var manifest=config.GetString("manifestPath");
        if(manifest==null && source.Type=="folder" && config.GetString("path") is { } directory && File.Exists(Path.Combine(directory,ImportManifest)))manifest=Path.Combine(directory,ImportManifest);
        if(manifest!=null) {
            AssertPathAllowed(manifest,req);row=GeminiImportManifest.Load(manifest,row);
            var cfg=ReadImportJson(manifest);var options=new JsonObject();foreach(var field in new[]{"crawl","transforms"})if(cfg.ContainsKey(field))options[field]=cfg[field]?.DeepClone();row["importOptions"]=options;
        }
        if(source.LastRunId!=null || config.GetBool("saved"))row.GetObject("config")!["saved"]=true;
        var edited=new ChatSource();ApplySourceSettings(edited,row);AssertSourceAllowed(edited,req);return row;
    }
    void AssertPathAllowed(string path,ChatRequestContext req)=>AssertSourceAllowed(new ChatSource { Config=new JsonObject { ["path"]=path }.ToJsonString() },req);
    async Task SaveSourceSettingsAsync(ChatSource source,ChatRequestContext req,JsonObject? options)
    {
        AssertSourceAllowed(source,req);var config=ChatJson.TryParseObject(source.Config)??new JsonObject();
        if(config.GetString("manifestPath") is { } existing)AssertPathAllowed(existing,req);
        if(source.Type is not ("folder" or "zip"))throw HttpError.BadRequest("Unknown source type");
        var manifest=await GeminiImportManifest.SaveAsync(source.ToDto(),options).ConfigureAwait(false);
        config["manifestPath"]=manifest;config["metadataSpecified"]=false;config["saved"]=true;source.Config=config.ToJsonString();
    }
    async Task<object?> BrowseImportManifestsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigureAwait(false);var path=req.QueryString("path");
        if(string.IsNullOrEmpty(path)) {path=CrawlImportsRoot(UserOf(req));Directory.CreateDirectory(path);}
        path=GeminiIngest.ResolvePath(path);AssertPathAllowed(path,req);if(!Directory.Exists(path))throw HttpError.NotFound("Directory is unavailable");
        var entries=new JsonArray();
        foreach(var info in new DirectoryInfo(path).EnumerateFileSystemInfos().Where(x=>x is DirectoryInfo || x.Name==ImportManifest).OrderBy(x=>x is DirectoryInfo).ThenBy(x=>x.Name,StringComparer.OrdinalIgnoreCase)) {
            try {AssertPathAllowed(info.FullName,req);}catch(UnauthorizedAccessException){continue;}
            entries.Add(new JsonObject { ["name"]=info.Name,["path"]=info.FullName,["directory"]=info is DirectoryInfo });
        }
        var parent=Path.GetDirectoryName(path);if(parent!=null)try{AssertPathAllowed(parent,req);}catch(UnauthorizedAccessException){parent=null;}
        return new JsonObject { ["path"]=path,["parent"]=parent==path?null:parent,["entries"]=entries };
    }
    async Task<object?> LoadImportManifestAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigureAwait(false);var body=await req.GetJsonBodyAsync().ConfigureAwait(false);var path=body.GetString("path")??throw HttpError.BadRequest("An import.json path is required");AssertPathAllowed(path,req);
        var row=GeminiImportManifest.Load(path);var source=new ChatSource();ApplySourceSettings(source,row);AssertSourceAllowed(source,req);
        var cfg=ReadImportJson(GeminiIngest.ResolvePath(path));var options=new JsonObject();foreach(var field in new[]{"crawl","transforms"})if(cfg.ContainsKey(field))options[field]=cfg[field]?.DeepClone();row["importOptions"]=options;return row;
    }
    async Task<object?> LoadSavedImportAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigureAwait(false);var body=await req.GetJsonBodyAsync().ConfigureAwait(false);
        var source=await RegisterImportManifestAsync(body.GetString("path")??throw HttpError.BadRequest("An import.json path is required"),body.GetLong("filestoreId")??throw HttpError.BadRequest("filestoreId is required"),req).ConfigureAwait(false);
        return EditableSource(source,req);
    }
    async Task<ChatSource> RegisterImportManifestAsync(string path,long storeId,ChatRequestContext req)
    {
        var user=UserOf(req);var gate=ImportLocks.GetOrAdd(ImportLockKey(user),_=>new SemaphoreSlim(1,1));await gate.WaitAsync(req.Request.RequestAborted).ConfigureAwait(false);
        try {
            if(db.GetFilestore(storeId,user)==null)throw HttpError.BadRequest("Filestore does not exist");
            AssertPathAllowed(path,req);var row=GeminiImportManifest.Load(path);var source=new ChatSource();ApplySourceSettings(source,row);AssertSourceAllowed(source,req);
            var original=GeminiIngest.ResolvePath(path);var cfg=ReadImportJson(original);var crawl=!string.IsNullOrEmpty(cfg.GetObject("crawl").GetString("url"));
            if(crawl) {
                AssertPathAllowed(Path.GetDirectoryName(original)!,req);
                path=await GeminiImportWorkspaces.ImportAsync(CrawlImportsRoot(user),original,req.Request.RequestAborted).ConfigureAwait(false);row=GeminiImportManifest.Load(path);ApplySourceSettings(source,row);AssertSourceAllowed(source,req);
                foreach(var alias in GeminiImportWorkspaces.LegacyManifests(CrawlImportsRoot(user),Path.GetDirectoryName(path)!)) {
                    foreach(var prior in db.QueryFilestores(new JsonObject { ["take"]=1000 },user).SelectMany(store=>db.QuerySources(store.Id,user,false))) {
                        var config=ChatJson.TryParseObject(prior.Config);var candidate=config.GetString("manifestPath")??(config.GetString("path") is { } input?Path.Combine(input,ImportManifest):null);
                        if(candidate==null || GeminiIngest.ResolvePath(candidate)!=alias)continue;
                        var relative=Path.GetRelativePath(Path.GetDirectoryName(alias)!,config.GetString("path")??Path.GetDirectoryName(alias)!);
                        config!["path"]=Path.Combine(Path.GetDirectoryName(path)!,relative);config["manifestPath"]=path;config["saved"]=true;prior.Config=config.ToJsonString();db.UpdateSource(prior);
                    }
                    db.RelocateSourceDocuments(alias,path,user);
                    Directory.Delete(Path.GetDirectoryName(alias)!,true);
                }
            }
            var manifest=source.ToDto().GetObject("config").GetString("manifestPath")!;
            foreach(var prior in db.QuerySources(storeId,user,false)) {
                var config=ChatJson.TryParseObject(prior.Config);var candidate=config.GetString("manifestPath")??(prior.Type=="folder" && config.GetString("path") is { } input?Path.Combine(input,ImportManifest):null);
                var same=false;
                if(candidate!=null && crawl)try {AssertPathAllowed(candidate,req);same=GeminiImportWorkspaces.Target(CrawlImportsRoot(user),candidate)==Path.GetDirectoryName(manifest);}catch(ArgumentException){ }catch(UnauthorizedAccessException){ }
                if(candidate==null || GeminiIngest.ResolvePath(candidate)!=manifest && GeminiIngest.ResolvePath(candidate)!=original && !same)continue;
                if(GeminiIngest.ResolvePath(candidate)!=manifest || original!=manifest) {ApplySourceSettings(prior,row);await SaveSourceSettingsAsync(prior,req,null).ConfigureAwait(false);db.UpdateSource(prior);db.RelocateSourceDocuments(candidate,manifest,user);}
                else if(!config.GetBool("saved")) {config!["manifestPath"]=manifest;config["saved"]=true;prior.Config=config.ToJsonString();db.UpdateSource(prior);}
                db.AttachSourceDocuments(prior.Id,storeId,manifest,user);return db.GetSource(prior.Id,user)!;
            }
            if(db.SavedSourceNameExists(storeId,user,source.Name??""))throw HttpError.BadRequest("A saved import with that name already exists");
            source.User=user;source.FilestoreId=storeId;source.Enabled=true;source.OnDelete??="tombstone";source.ExtractorVer??=GeminiIngest.ExtractorVersion;source.CreatedAt=source.UpdatedAt=DateTime.Now;
            await SaveSourceSettingsAsync(source,req,null).ConfigureAwait(false);source.Id=db.InsertSource(source);db.AttachSourceDocuments(source.Id,storeId,manifest,user);return source;
        } finally {gate.Release();}
    }
    async Task<(ChatSource Source,string Root)> SelectedCrawlAsync(ChatRequestContext req,long sourceId,long? storeId=null)
    {
        var source=db.GetSource(sourceId,UserOf(req))??throw HttpError.NotFound("Source does not exist");var row=EditableSource(source,req);
        if(storeId!=null && source.FilestoreId!=storeId)throw HttpError.BadRequest("Saved import does not belong to this filestore");
        if(source.Type!="folder" || string.IsNullOrEmpty(row.GetObject("importOptions").GetObject("crawl").GetString("url")))throw HttpError.BadRequest("Saved import is not a web crawl");
        source=await RegisterImportManifestAsync(row.GetObject("config").GetString("manifestPath")!,source.FilestoreId,req).ConfigureAwait(false);
        var root=Path.GetDirectoryName(ChatJson.ParseObject(source.Config!).GetString("manifestPath"))!;AssertPathAllowed(root,req);return (source,root);
    }
}
