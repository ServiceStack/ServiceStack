using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    readonly GeminiAssistantMinuteLimiter searchLimiter = new();
    readonly GeminiAssistantMinuteLimiter searchClickLimiter = new();
    readonly GeminiAssistantMinuteLimiter searchPageViewLimiter = new();

    void InstallSearchRoutes(ExtensionContext ctx)
    {
        ctx.AddGet("filestores/{id}/searches", ListSearchWidgetsAsync);
        ctx.AddPost("filestores/{id}/searches", CreateSearchWidgetAsync);
        ctx.AddGet("searches/{id}", GetSearchWidgetAsync);
        ctx.AddGet("searches/{id}/analytics", SearchAnalyticsAsync);
        ctx.AddPost("searches/{id}/analytics/clear", ClearSearchAnalyticsAsync);
        ctx.AddGet("searches/{id}/diagnostics", SearchDiagnosticsAsync);
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
        ctx.AddPost("public/searches/{publicId}/clicks", PublicSearchClickAsync, allowAnon: true);
        ctx.AddPost("public/searches/{publicId}/pageviews", PublicSearchPageViewAsync, allowAnon: true);
        ctx.AddGet("public/searches/{publicId}/documents/{documentId}", PublicSearchDocumentAsync, allowAnon: true);
    }

    JsonObject SearchWidgetDto(ChatSearchWidget widget, ChatRequestContext req, long? searchCount = null,
        long? pageViewCount = null)
    {
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        var src = AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/widget.js?g={widget.PublicId}");
        var dto = widget.ToDto(); dto["config"] = config;
        dto["published"] = widget.Enabled && widget.PublishedAt != null;
        dto["searchCount"] = searchCount ?? db.SearchQueryCount(widget.Id);
        dto["pageViewCount"] = pageViewCount ?? db.SearchPageViewCount(widget.Id);
        dto["requestIp"] = GeminiSearchGeo.NormalizeIpAddress(req.Request.RemoteIp);
        dto["scriptUrl"] = src; dto["embedCode"] = $"<script src=\"{src}\" async></script>";
        return dto;
    }

    Task<object?> ListSearchWidgetsAsync(ChatRequestContext req)
    {
        var user = UserOf(req); var storeId = IdOf(req);
        if (db.GetFilestore(storeId, user) == null) return Task.FromResult<object?>(ChatResult.NotFound("File Store does not exist"));
        var widgets = db.QuerySearchWidgets(storeId, user, true);
        var counts = db.SearchQueryCounts(widgets.Select(x => x.Id));
        var pageViews = db.SearchPageViewCounts(widgets.Select(x => x.Id));
        return Task.FromResult<object?>(new JsonArray(widgets
            .Select(x => (JsonNode)SearchWidgetDto(x, req, counts.GetValueOrDefault(x.Id),
                pageViews.GetValueOrDefault(x.Id))).ToArray()));
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

    Task<object?> SearchAnalyticsAsync(ChatRequestContext req)
    {
        var id = IdOf(req); var user = UserOf(req);
        var widget = db.GetSearchWidget(id, user);
        if (widget == null) return Task.FromResult<object?>(ChatResult.NotFound("Search widget does not exist"));
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        db.ClearSearchAnalytics(id, user,
            DateTime.Now.AddDays(-(config.GetObject("analytics").GetInt("retentionDays") ?? 90)));
        var result = db.SearchAnalytics(id, user,
            req.QueryString("groupTake").ToInt(50), req.QueryString("recentTake").ToInt(100));
        if (result == null) return Task.FromResult<object?>(ChatResult.NotFound("Search widget does not exist"));
        var enabled = config.GetObject("analytics").GetBool("enabled");
        result["trafficEnabled"] = enabled;
        result["requestIp"] = GeminiSearchGeo.NormalizeIpAddress(req.Request.RemoteIp);
        result["traffic"] = db.SearchTrafficAnalytics(id, user, req.QueryString("period") ?? "30d",
            req.QueryString("visitorSkip").ToInt(), req.QueryString("visitorTake").ToInt(10));
        return Task.FromResult<object?>(result);
    }

    async Task<object?> ClearSearchAnalyticsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var result = db.ClearSearchAnalytics(IdOf(req), UserOf(req));
        return result != null ? result : ChatResult.NotFound("Search widget does not exist");
    }

    Task<object?> SearchDiagnosticsAsync(ChatRequestContext req)
    {
        var widget = db.GetSearchWidget(IdOf(req), UserOf(req));
        if (widget == null) return Task.FromResult<object?>(ChatResult.NotFound("Search widget does not exist"));
        var store = db.GetFilestore(widget.FilestoreId, widget.User);
        var stats = db.SearchStats(widget.FilestoreId, widget.User);
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        var checks = new JsonArray();
        void Add(string name, string status, string message) => checks.Add(new JsonObject
            { ["name"] = name, ["status"] = status, ["message"] = message });
        Add("Published", widget.Enabled && widget.PublishedAt != null ? "pass" : "fail",
            widget.Enabled && widget.PublishedAt != null ? "The public Search deployment is enabled." : "Publish this Search before embedding it.");
        Add("Public File Store", store?.Visibility == "public" ? "pass" : "fail",
            store?.Visibility == "public" ? "Anonymous read access is enabled." : "The File Store must be public.");
        Add("Search index", stats.Indexed > 0 ? stats.Failed > 0 ? "warn" : "pass" : "fail",
            $"{stats.Indexed} indexed, {stats.Pending} pending, {stats.Failed} failed documents.");
        var origins = GeminiMetadata.AsList(config.GetObject("hosting")?["allowedOrigins"]);
        Add("Allowed origins", origins.Count > 0 ? "pass" : "warn",
            origins.Count > 0 ? $"Restricted to {origins.Count} configured origin(s)." : "All origins are currently allowed.");
        Add("Widget endpoint", widget.Enabled && widget.PublishedAt != null ? "pass" : "fail",
            AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/widget.js?g={widget.PublicId}"));
        return Task.FromResult<object?>(new JsonObject
            { ["ready"] = checks.OfType<JsonObject>().All(x => x.GetString("status") != "fail"), ["checks"] = checks });
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
        var skip=Math.Clamp(req.QueryString("skip").ToInt(),0,1000);
        var ranking=ChatJson.TryParseObject(req.QueryString("ranking"));
        var rows=db.SearchSections(storeId,query,user,take:take+1,ranking:ranking,skip:skip);
        var hasMore=rows.Count>take; rows=rows.Take(take).ToList();
        return Task.FromResult<object?>(new JsonObject{{"query",query},{"hasMore",hasMore},{"nextSkip",skip+rows.Count},{"groups",GroupSearchResults(rows,query,
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
            ["failed"]=stats.Failed,["stale"]=stats.Stale,["sections"]=stats.Sections,["provider"]=stats.Provider,
            ["lastIndexedAt"]=stats.LastIndexedAt,["oldestPendingAt"]=stats.OldestPendingAt,["errors"]=stats.Errors,
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
                ["tooltip"]=config.GetObject("identity").GetString("tooltip"),
                ["behavior"]=config["behavior"]?.DeepClone(),["appearance"]=config["appearance"]?.DeepClone(),
                ["analyticsEnabled"]=config.GetObject("analytics").GetBool("enabled"),
                ["analytics"]=new JsonObject
                {
                    ["respectDoNotTrack"] = config.GetObject("analytics").GetBool("respectDoNotTrack", true),
                    ["requireConsent"] = config.GetObject("analytics").GetBool("requireConsent"),
                },
                ["searchUrl"]=AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/{widget.PublicId}/results"),
                ["clickUrl"]=AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/{widget.PublicId}/clicks"),
                ["analyticsUrl"]=AssistantBaseUrl(req).CombineWith($"ext/gemini/public/searches/{widget.PublicId}/pageviews"),
            };
            var script=$"(()=>{{const CONFIG={publicConfig.ToJsonString(ChatJson.Options)};const SCRIPT=document.currentScript;"
                + $"const MARKDOWN=(()=>{{\n{markdown}\n}})();const mount=()=>{{\n{source}\n}};"
                + "if(document.readyState!=='loading')mount();else addEventListener('DOMContentLoaded',mount,{once:true});})();";
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
        var skip=Math.Clamp(req.QueryString("skip").ToInt(),0,1000);
        if(query.Length<(behavior.GetInt("minChars")??2))return Task.FromResult<object?>(new ChatResult{ContentType=MimeTypes.Json,Headers=headers,Text=new JsonObject{{"query",query},{"groups",new JsonArray()},{"hasMore",false},{"nextSkip",0}}.ToJsonString(ChatJson.Options)});
        var stopwatch = Stopwatch.StartNew();
        var take=behavior.GetInt("maxResults")??30;
        var rows=db.SearchSections(store.Id,query,widget.User,config.GetObject("scope"),take+1,
            config.GetObject("ranking"),skip);
        var hasMore=rows.Count>take; rows=rows.Take(take).ToList();
        var groups=GroupSearchResults(rows,query,
            behavior.GetInt("groupLimit")??8,
            row => AssistantBaseUrl(req).CombineWith(
                $"ext/gemini/public/searches/{widget.PublicId}/documents/{row.DocumentId}"));
        stopwatch.Stop();
        long? searchEventId = null;
        var analytics = config.GetObject("analytics");
        var deniedUserAgents = GeminiMetadata.AsList(analytics["deniedUserAgents"]);
        var deniedIpRanges = GeminiMetadata.AsList(analytics["deniedIpRanges"]);
        var excludedPaths = GeminiMetadata.AsList(analytics["excludedPaths"]);
        var recordAnalytics = !(analytics.GetBool("excludeBots", true) && GeminiSearch.IsBot(req.Request.UserAgent))
            && !GeminiSearch.IsDeniedUserAgent(req.Request.UserAgent, deniedUserAgents)
            && !GeminiSearch.IsDeniedIp(req.Request.RemoteIp, deniedIpRanges)
            && !GeminiSearch.IsExcludedPath(req.Request.Headers["Referer"], excludedPaths);
        try
        {
            if (skip == 0 && recordAnalytics)
            {
                db.ClearSearchAnalytics(widget.Id, widget.User, DateTime.Now.AddDays(
                    -(config.GetObject("analytics").GetInt("retentionDays") ?? 90)));
                searchEventId = db.RecordSearchQuery(widget.Id, query, origin, req.Request.Headers["Referer"],
                    req.Request.UserAgent, rows.Count, groups.Count, (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
            }
        }
        catch (Exception e) { Log.LogWarning(e, "Failed recording Search analytics"); }
        var result=new JsonObject{{"query",query},{"groups",groups},{"hasMore",hasMore},{"nextSkip",skip+rows.Count},
            {"searchEventId",searchEventId}};
        return Task.FromResult<object?>(new ChatResult{ContentType=MimeTypes.Json,Headers=headers,Text=result.ToJsonString(ChatJson.Options)});
    }

    async Task<object?> PublicSearchClickAsync(ChatRequestContext req)
    {
        var (widget, store) = PublicSearch(req.GetPathParam("publicId"));
        if (widget == null || store == null) return ChatResult.NotFound("Search is unavailable");
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        var origin = req.Request.Headers[HttpHeaders.Origin];
        var allowed = GeminiAssistants.OriginAllowed(origin,
            GeminiMetadata.AsList(config.GetObject("hosting")?["allowedOrigins"]));
        var headers = CorsHeaders(origin, allowed);
        if (!allowed) return Error("This website is not allowed to use this Search widget", "OriginNotAllowed", 403, headers);
        var analytics = config.GetObject("analytics");
        if ((analytics.GetBool("excludeBots", true) && GeminiSearch.IsBot(req.Request.UserAgent)) ||
            GeminiSearch.IsDeniedUserAgent(req.Request.UserAgent, GeminiMetadata.AsList(analytics["deniedUserAgents"])) ||
            GeminiSearch.IsDeniedIp(req.Request.RemoteIp, GeminiMetadata.AsList(analytics["deniedIpRanges"])) ||
            GeminiSearch.IsExcludedPath(req.Request.Headers["Referer"], GeminiMetadata.AsList(analytics["excludedPaths"])))
            return new ChatResult { Status = 204, Headers = headers };
        var searchLimit = config.GetObject("hosting").GetInt("requestsPerMinute") ?? 120;
        var clickLimit = Math.Max(searchLimit * 4, 120);
        if (!searchClickLimiter.Allow($"{widget.Id}:{req.Request.RemoteIp ?? "unknown"}", clickLimit))
        {
            headers["Retry-After"] = "60";
            return Error("Too many Search interactions. Please wait a moment and try again.", "RateLimited", 429, headers);
        }
        JsonObject body;
        try { body = await req.GetJsonBodyAsync().ConfigAwait(); }
        catch { return Error("Invalid click event", "ValidationError", 400, headers); }
        var searchEventId = body.GetLong("searchEventId") ?? 0;
        var documentId = body.GetLong("documentId") ?? 0;
        var sectionId = body.GetLong("sectionId");
        var position = body.GetInt("position") ?? 0;
        if (searchEventId <= 0 || documentId <= 0 || position <= 0)
            return Error("Invalid click event", "ValidationError", 400, headers);
        try
        {
            db.RecordSearchClick(widget.Id, store.Id, widget.User, searchEventId, documentId,
                sectionId > 0 ? sectionId : null, position, body.GetString("documentTitle"),
                body.GetString("sourceUrl"), body.GetString("resultType"));
        }
        catch (Exception e) { Log.LogWarning(e, "Failed recording Search result click"); }
        return new ChatResult { Status = 204, Headers = headers };
    }

    async Task<object?> PublicSearchPageViewAsync(ChatRequestContext req)
    {
        var (widget, store) = PublicSearch(req.GetPathParam("publicId"));
        if (widget == null || store == null) return ChatResult.NotFound("Search is unavailable");
        var config = GeminiSearch.NormalizeConfig(ChatJson.TryParseObject(widget.Config));
        var origin = req.Request.Headers[HttpHeaders.Origin];
        var allowed = GeminiAssistants.OriginAllowed(origin,
            GeminiMetadata.AsList(config.GetObject("hosting")?["allowedOrigins"]));
        var headers = CorsHeaders(origin, allowed);
        if (!allowed) return Error("This website is not allowed to use this Search widget", "OriginNotAllowed", 403, headers);
        if (!config.GetObject("analytics").GetBool("enabled"))
            return new ChatResult { Status = 204, Headers = headers };
        var analytics = config.GetObject("analytics");
        if (analytics.GetBool("respectDoNotTrack", true) && req.Request.Headers["DNT"] == "1")
            return new ChatResult { Status = 204, Headers = headers };
        if (analytics.GetBool("excludeBots", true) && GeminiSearch.IsBot(req.Request.UserAgent))
            return new ChatResult { Status = 204, Headers = headers };
        if (GeminiSearch.IsDeniedUserAgent(req.Request.UserAgent,
                GeminiMetadata.AsList(analytics["deniedUserAgents"])))
            return new ChatResult { Status = 204, Headers = headers };
        var ipAddress = GeminiSearchGeo.NormalizeIpAddress(req.Request.RemoteIp);
        if (GeminiSearch.IsDeniedIp(ipAddress, GeminiMetadata.AsList(analytics["deniedIpRanges"])))
            return new ChatResult { Status = 204, Headers = headers };
        var searchLimit = config.GetObject("hosting").GetInt("requestsPerMinute") ?? 120;
        var pageViewLimit = Math.Max(searchLimit * 10, 600);
        if (!searchPageViewLimiter.Allow($"{widget.Id}:{req.Request.RemoteIp ?? "unknown"}", pageViewLimit))
        {
            headers["Retry-After"] = "60";
            return Error("Too many Analytics events. Please wait a moment and try again.", "RateLimited", 429, headers);
        }
        JsonObject body;
        try { body = await req.GetJsonBodyAsync().ConfigAwait(); }
        catch { return Error("Invalid page view", "ValidationError", 400, headers); }
        var pageUrl = (body.GetString("pageUrl") ?? req.Request.Headers["Referer"] ?? "").Trim().SafeSubstring(0, 2000);
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return Error("Invalid page URL", "ValidationError", 400, headers);
        if (GeminiSearch.IsExcludedPath(uri.AbsolutePath, GeminiMetadata.AsList(analytics["excludedPaths"])))
            return new ChatResult { Status = 204, Headers = headers };
        if (string.IsNullOrEmpty(body.GetString("pagePath"))) body["pagePath"] = uri.PathAndQuery;
        body["pageUrl"] = pageUrl;
        GeminiSearchGeo? geo = null;
        if (ipAddress != null && SearchGeoResolver != null)
        {
            try
            {
                geo = await SearchGeoResolver.ResolveAsync(ipAddress, req.Request.RequestAborted).ConfigAwait();
            }
            catch (OperationCanceledException) when (req.Request.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.LogWarning(e, "Failed resolving Search analytics visitor geography");
            }
        }
        var storedIp = analytics.GetBool("anonymizeIp", true) ? GeminiSearch.AnonymizeIp(ipAddress) : ipAddress;
        try
        {
            db.ClearSearchAnalytics(widget.Id, widget.User,
                DateTime.Now.AddDays(-(analytics.GetInt("retentionDays") ?? 90)));
            db.RecordSearchPageView(widget.Id, body, origin, req.Request.UserAgent, storedIp, geo);
        }
        catch (Exception e) { Log.LogWarning(e, "Failed recording Search page view"); }
        return new ChatResult { Status = 204, Headers = headers };
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
