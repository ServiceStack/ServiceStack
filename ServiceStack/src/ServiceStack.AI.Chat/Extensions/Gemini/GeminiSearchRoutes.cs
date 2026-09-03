using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    readonly GeminiAssistantMinuteLimiter searchLimiter = new();

    void InstallSearchRoutes(ExtensionContext ctx)
    {
        ctx.AddGet("filestores/{id}/searches", ListSearchWidgetsAsync);
        ctx.AddPost("filestores/{id}/searches", CreateSearchWidgetAsync);
        ctx.AddGet("searches/{id}", GetSearchWidgetAsync);
        ctx.AddPut("searches/{id}", UpdateSearchWidgetAsync);
        ctx.AddDelete("searches/{id}", ArchiveSearchWidgetAsync);
        ctx.AddPost("searches/{id}/restore", RestoreSearchWidgetAsync);
        ctx.AddDelete("searches/{id}/permanent", DeleteSearchWidgetAsync);
        ctx.AddGet("filestores/{id}/search", SearchFilestoreAsync, allowAnon: true);
        ctx.AddGet("filestores/{id}/search-documents/{documentId}", SearchDocumentAsync);
        ctx.AddGet("filestores/{id}/search-index", SearchIndexStatusAsync, allowAnon: true);
        ctx.AddPost("filestores/{id}/search-index/rebuild", RebuildSearchIndexAsync);
        ctx.AddGet("public/searches/widget.js", PublicSearchScriptAsync, allowAnon: true);
        ctx.AddGet("public/searches/{publicId}/results", PublicSearchResultsAsync, allowAnon: true);
        ctx.AddGet("public/searches/{publicId}/documents/{documentId}", PublicSearchDocumentAsync, allowAnon: true);
    }

    JsonObject SearchWidgetDto(ChatSearchWidget widget, ChatRequestContext req)
    {
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        var src = AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/widget.js?g={widget.PublicId}");
        var dto = widget.ToDto(); dto["config"] = config;
        dto["published"] = widget.Enabled && widget.PublishedAt != null;
        dto["scriptUrl"] = src; dto["embedCode"] = $"<script src=\"{src}\" async></script>";
        return dto;
    }

    Task<object?> ListSearchWidgetsAsync(ChatRequestContext req)
    {
        var user = UserOf(req); var storeId = IdOf(req);
        if (db.GetFilestore(storeId, user) == null) return Task.FromResult<object?>(ChatResult.NotFound("File Store does not exist"));
        return Task.FromResult<object?>(new JsonArray(db.QuerySearchWidgets(storeId, user, true)
            .Select(x => (JsonNode)SearchWidgetDto(x, req)).ToArray()));
    }

    async Task<object?> CreateSearchWidgetAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var user = UserOf(req); var storeId = IdOf(req);
        if (db.GetFilestore(storeId, user) == null) return ChatResult.NotFound("File Store does not exist");
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var name = (body.GetString("name") ?? "").Trim().SafeSubstring(0, 200);
        if (name.Length == 0) return Error("Name is required", "ValidationError", 400);
        if (db.SearchWidgetNameExists(storeId, name, user)) return Error($"A Search widget named '{name}' already exists", "AlreadyExists", 409);
        JsonObject config; try { config = GeminiSearch.ValidateConfig(body.GetObject("config")); }
        catch (ArgumentException e) { return Error(e.Message, "ValidationError", 400); }
        var now = DateTime.Now; var publish = body.GetBool("published");
        var widget = new ChatSearchWidget
        {
            FilestoreId=storeId, User=user, CreatedAt=now, UpdatedAt=now, Name=name,
            PublicId=GeminiSearch.NewPublicId(), Enabled=true, PublishedAt=publish ? now : null,
            Config=config.ToJsonString(ChatJson.Options),
        };
        widget.Id = db.InsertSearchWidget(widget); if (publish) PublishFilestore(storeId, user);
        return SearchWidgetDto(widget, req);
    }

    Task<object?> GetSearchWidgetAsync(ChatRequestContext req)
    {
        var widget = db.GetSearchWidget(IdOf(req), UserOf(req));
        return Task.FromResult<object?>(widget == null ? ChatResult.NotFound("Search widget does not exist") : SearchWidgetDto(widget, req));
    }

    async Task<object?> UpdateSearchWidgetAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var user = UserOf(req); var widget = db.GetSearchWidget(IdOf(req), user);
        if (widget == null) return ChatResult.NotFound("Search widget does not exist");
        if (!widget.Enabled) return Error("Restore this Search widget before editing or publishing it", "SearchArchived", 409);
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var name = (body.GetString("name") ?? widget.Name ?? "").Trim().SafeSubstring(0, 200);
        if (name.Length == 0) return Error("Name is required", "ValidationError", 400);
        if (db.SearchWidgetNameExists(widget.FilestoreId, name, user, widget.Id)) return Error($"A Search widget named '{name}' already exists", "AlreadyExists", 409);
        JsonObject config; try { config = GeminiSearch.ValidateConfig(body.GetObject("config") ?? ChatJson.TryParseObject(widget.Config)); }
        catch (ArgumentException e) { return Error(e.Message, "ValidationError", 400); }
        var published = body.TryGetPropertyValue("published", out _) ? body.GetBool("published") : widget.PublishedAt != null;
        widget.Name=name; widget.Config=config.ToJsonString(ChatJson.Options);
        widget.PublishedAt=published ? widget.PublishedAt ?? DateTime.Now : null;
        if (body.GetBool("regeneratePublicId")) widget.PublicId=GeminiSearch.NewPublicId();
        db.UpdateSearchWidget(widget); if (published) PublishFilestore(widget.FilestoreId, user);
        return SearchWidgetDto(widget, req);
    }

    async Task<object?> ArchiveSearchWidgetAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        return db.ArchiveSearchWidget(IdOf(req), UserOf(req)) ? new JsonObject { ["archived"] = true } : ChatResult.NotFound("Search widget does not exist");
    }

    async Task<object?> RestoreSearchWidgetAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        try { var widget=db.RestoreSearchWidget(IdOf(req),UserOf(req)); return widget==null ? ChatResult.NotFound("Search widget does not exist") : SearchWidgetDto(widget,req); }
        catch (InvalidOperationException e) { return Error(e.Message,"AlreadyExists",409); }
    }

    async Task<object?> DeleteSearchWidgetAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var widget=db.GetSearchWidget(IdOf(req),UserOf(req));
        if (widget==null) return ChatResult.NotFound("Search widget does not exist");
        var body=await req.GetJsonBodyAsync().ConfigAwait();
        try { db.DeleteSearchWidget(widget.Id,UserOf(req),body.GetString("confirm")); return new JsonObject { ["deleted"] = true }; }
        catch (ArgumentException e) { return Error(e.Message,"ConfirmationRequired",400); }
    }

    static JsonArray SearchParts(string? value, string query)
    {
        value ??= ""; var tokens=Regex.Matches(query,@"[\p{L}\p{N}_]+").Select(x=>Regex.Escape(x.Value)).Take(10).ToArray();
        if(tokens.Length==0) return new JsonArray(new JsonObject{{"text",value},{"match",false}});
        var split=Regex.Split(value,$"({string.Join('|',tokens)})",RegexOptions.IgnoreCase);
        var wanted=new Regex($"^(?:{string.Join('|',tokens)})$",RegexOptions.IgnoreCase);
        return new JsonArray(split.Where(x=>x.Length>0).Select(x=>(JsonNode)new JsonObject{{"text",x},{"match",wanted.IsMatch(x)}}).ToArray());
    }

    static bool IsCachedMarkdown(string? url) => url?.StartsWith(CacheUrlBase, StringComparison.Ordinal) == true
        && Regex.IsMatch(url, @"\.md(?:#|$)", RegexOptions.IgnoreCase);

    JsonArray GroupSearchResults(List<ChatSearchResult> rows, string query, int groupLimit = 8,
        Func<ChatSearchResult, string?>? previewUrl = null)
    {
        var groups=new Dictionary<long,JsonObject>(); var ordered=new JsonArray();
        foreach(var row in rows)
        {
            var cached = row.Url?.StartsWith(CacheUrlBase, StringComparison.Ordinal) == true;
            var url = cached ? null : row.Url;
            var documentPreviewUrl = IsCachedMarkdown(row.Url) ? previewUrl?.Invoke(row) : null;
            if(!groups.TryGetValue(row.DocumentId,out var group))
            {
                group=new JsonObject{{"documentId",row.DocumentId},{"title",row.DocumentTitle??"Document"},{"url",url},
                    {"previewUrl",documentPreviewUrl},{"items",new JsonArray()}};
                groups[row.DocumentId]=group; ordered.Add(group);
            }
            var items=group.GetArray("items")!; if(items.Count>=groupLimit) continue;
            var title=row.Heading??row.DocumentTitle??"Document"; var snippet=row.Snippet??row.Content??"";
            items.Add(new JsonObject
            {
                ["id"]=row.Id,["type"]=row.Kind??"content",["title"]=title,["titleParts"]=SearchParts(title,query),
                ["snippet"]=snippet,["snippetParts"]=SearchParts(snippet,query),["url"]=url,
                ["previewUrl"]=documentPreviewUrl,["anchor"]=row.Anchor,["score"]=row.Score,
            });
        }
        return ordered;
    }

    Task<object?> SearchFilestoreAsync(ChatRequestContext req)
    {
        var user=UserOf(req); var storeId=IdOf(req);
        if(db.GetFilestore(storeId,user)==null) return Task.FromResult<object?>(ChatResult.NotFound("File Store does not exist"));
        var query=req.QueryString("q")??""; var take=Math.Clamp(req.QueryString("take").ToInt(30),1,100);
        var rows=db.SearchSections(storeId,query,user,take:take);
        return Task.FromResult<object?>(new JsonObject{{"query",query},{"groups",GroupSearchResults(rows,query,
            previewUrl: row => AssistantBaseUrl(req).CombineWith(
                $"ext/gemini/filestores/{storeId}/search-documents/{row.DocumentId}"))}});
    }

    async Task<JsonObject?> MarkdownDocumentPayloadAsync(ChatDocument document)
    {
        if (document.Url?.StartsWith(CacheUrlBase, StringComparison.Ordinal) != true)
            return null;
        var filename = document.Filename ?? document.SourceKey ?? document.DisplayName ?? "document.md";
        if (!Regex.IsMatch(filename, @"\.md$", RegexOptions.IgnoreCase)
            && !Regex.IsMatch(document.Url, @"\.md(?:#|$)", RegexOptions.IgnoreCase)) return null;
        var relative = document.Url[CacheUrlBase.Length..].Split('#')[0];
        var path = Ctx.GetCachePath(relative);
        if (!File.Exists(path)) return null;
        var bytes = await File.ReadAllBytesAsync(path).ConfigAwait();
        var extracted = GeminiIngest.Extract(bytes, filename, new JsonObject { ["minWords"] = 0 });
        if (extracted.Skip != null) return null;
        var title = extracted.Frontmatter.GetString("title") ?? document.DisplayName ?? document.SourceKey ?? "Document";
        title = Regex.Replace(title, @"\.(?:md|mdx|markdown)$", "", RegexOptions.IgnoreCase);
        return new JsonObject { ["title"] = title, ["markdown"] = extracted.Text ?? "" };
    }

    async Task<object?> SearchDocumentAsync(ChatRequestContext req)
    {
        var user = UserOf(req); var storeId = IdOf(req);
        var document = db.GetDocument(req.GetPathParam("documentId").ToLong(), user);
        if (document == null || document.FilestoreId != storeId) return ChatResult.NotFound("Document does not exist");
        var payload = await MarkdownDocumentPayloadAsync(document).ConfigAwait();
        if (payload == null) return ChatResult.NotFound("Markdown preview is unavailable");
        return payload;
    }

    Task<object?> SearchIndexStatusAsync(ChatRequestContext req)
    {
        var user=UserOf(req); var storeId=IdOf(req);
        if(db.GetFilestore(storeId,user)==null) return Task.FromResult<object?>(ChatResult.NotFound("File Store does not exist"));
        var stats=db.SearchStats(storeId,user);
        return Task.FromResult<object?>(new JsonObject
        {
            ["documents"]=stats.Documents,["indexed"]=stats.Indexed,["pending"]=stats.Pending,
            ["failed"]=stats.Failed,["sections"]=stats.Sections,["provider"]=stats.Provider,
            ["worker"]=searchWorker?.Status()??new JsonObject{{"running",false}},
        });
    }

    async Task<object?> RebuildSearchIndexAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var user=UserOf(req); var storeId=IdOf(req);
        if(db.GetFilestore(storeId,user)==null) return ChatResult.NotFound("File Store does not exist");
        var docs=db.QueryAllDocuments(storeId,user).ToList(); foreach(var doc in docs){db.SetSearchDesired(doc,true);db.UpdateDocument(doc);}
        searchWorker?.Start(); return new JsonObject{{"queued",docs.Count},{"worker",searchWorker?.Status()}};
    }

    (ChatSearchWidget? Widget,ChatFilestore? Store) PublicSearch(string publicId)
    {
        var widget=db.GetPublicSearchWidget(publicId); var store=widget==null?null:db.GetFilestore(widget.FilestoreId,widget.User);
        return store?.Visibility=="public"?(widget,store):(null,null);
    }

    Task<object?> PublicSearchScriptAsync(ChatRequestContext req)
    {
        var headers=new Dictionary<string,string>{{HttpHeaders.CacheControl,"no-cache"},{"X-Content-Type-Options","nosniff"}};
        var (widget,_)=PublicSearch(req.QueryString("g")??"");
        if(widget==null) return Task.FromResult<object?>(new ChatResult{ContentType="application/javascript",Headers=headers,Text="console.error(\"Gemini Search widget failed to load: Search is unavailable (404)\");"});
        try
        {
            var source=Ctx.GetBundledText("search-widget.js")??throw new FileNotFoundException("search-widget.js is not embedded");
            string markdown;
            try { markdown = BundledMarkdownSource(); }
            catch (Exception e)
            {
                Log.LogWarning(e, "Failed embedding the bundled Marked renderer for Search");
                markdown = "console.warn(\"Gemini Search Markdown renderer is unavailable; using plain text.\");return null;";
            }
            var config=GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
            var publicConfig=new JsonObject
            {
                ["searchId"]=widget.PublicId,["title"]=config.GetObject("identity").GetString("title"),
                ["placeholder"]=config.GetObject("identity").GetString("placeholder"),["emptyText"]=config.GetObject("identity").GetString("emptyText"),
                ["behavior"]=config["behavior"]?.DeepClone(),["appearance"]=config["appearance"]?.DeepClone(),
                ["searchUrl"]=AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/{widget.PublicId}/results"),
            };
            var script=$"(()=>{{const CONFIG={publicConfig.ToJsonString(ChatJson.Options)};const SCRIPT=document.currentScript;"
                + $"const MARKDOWN=(()=>{{\n{markdown}\n}})();const mount=()=>{{\n{source}\n}};"
                + "if(document.body)mount();else addEventListener('DOMContentLoaded',mount,{once:true});})();";
            return Task.FromResult<object?>(new ChatResult{ContentType="application/javascript",Headers=headers,Text=script});
        }
        catch(Exception e){Log.LogError(e,"Failed generating Gemini Search widget script");return Task.FromResult<object?>(new ChatResult{ContentType="application/javascript",Headers=headers,Text="console.error(\"Gemini Search widget failed to load. Check the server logs for details.\");"});}
    }

    Task<object?> PublicSearchResultsAsync(ChatRequestContext req)
    {
        var (widget,store)=PublicSearch(req.GetPathParam("publicId")); if(widget==null||store==null)return Task.FromResult<object?>(ChatResult.NotFound("Search is unavailable"));
        var config=GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config)); var origin=req.Request.Headers[HttpHeaders.Origin];
        var allowed=GeminiAssistants.OriginAllowed(origin,GeminiMetadata.AsList(config.GetObject("hosting")?["allowedOrigins"])); var headers=CorsHeaders(origin,allowed);
        if(!allowed)return Task.FromResult<object?>(Error("This website is not allowed to use this Search widget","OriginNotAllowed",403,headers));
        var limit=config.GetObject("hosting").GetInt("requestsPerMinute")??120;
        if(!searchLimiter.Allow($"{widget.Id}:{req.Request.RemoteIp??"unknown"}",limit)){headers["Retry-After"]="60";return Task.FromResult<object?>(Error("Too many searches. Please wait a moment and try again.","RateLimited",429,headers));}
        var query=(req.QueryString("q")??"").Trim().SafeSubstring(0,200); var behavior=config.GetObject("behavior")!;
        if(query.Length<(behavior.GetInt("minChars")??2))return Task.FromResult<object?>(new ChatResult{ContentType=MimeTypes.Json,Headers=headers,Text=new JsonObject{{"query",query},{"groups",new JsonArray()}}.ToJsonString(ChatJson.Options)});
        var rows=db.SearchSections(store.Id,query,widget.User,config.GetObject("scope"),behavior.GetInt("maxResults")??30);
        var result=new JsonObject{{"query",query},{"groups",GroupSearchResults(rows,query,
            behavior.GetInt("groupLimit")??8,
            row => AssistantBaseUrl(req).CombineWith(
                $"ext/gemini/public/searches/{widget.PublicId}/documents/{row.DocumentId}"))}};
        return Task.FromResult<object?>(new ChatResult{ContentType=MimeTypes.Json,Headers=headers,Text=result.ToJsonString(ChatJson.Options)});
    }

    async Task<object?> PublicSearchDocumentAsync(ChatRequestContext req)
    {
        var (widget, store) = PublicSearch(req.GetPathParam("publicId"));
        if (widget == null || store == null) return ChatResult.NotFound("Search is unavailable");
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        var origin = req.Request.Headers[HttpHeaders.Origin];
        var allowed = GeminiAssistants.OriginAllowed(origin,
            GeminiMetadata.AsList(config.GetObject("hosting")?["allowedOrigins"]));
        var headers = CorsHeaders(origin, allowed);
        if (!allowed) return Error("This website is not allowed to use this Search widget", "OriginNotAllowed", 403, headers);
        var document = db.GetDocument(req.GetPathParam("documentId").ToLong(), widget.User);
        if (document == null || document.FilestoreId != store.Id)
            return Error("Document does not exist", "NotFound", 404, headers);
        var payload = await MarkdownDocumentPayloadAsync(document).ConfigAwait();
        if (payload == null) return Error("Markdown preview is unavailable", "NotFound", 404, headers);
        return new ChatResult { ContentType=MimeTypes.Json, Headers=headers,
            Text=payload.ToJsonString(ChatJson.Options) };
    }
}
