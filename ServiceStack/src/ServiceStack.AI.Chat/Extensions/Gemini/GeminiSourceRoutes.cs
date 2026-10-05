using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    async Task AssertWriteAsync(ChatRequestContext req, string? requiredRole = null)
    {
        if (!Ctx.IsAuthEnabled) return;
        Ctx.AssertUserName(req.Request);
        var role = requiredRole ?? writeRole;
        if (string.IsNullOrEmpty(role) || Ctx.IsAdmin(req.Request)) return;
        var info = await Feature.ChatAuth.GetAuthInfoAsync(req.Request).ConfigAwait();
        var roles = info?.GetArray("roles")?.Select(x => x?.GetValue<string>()).Where(x => x != null).Cast<string>() ?? [];
        if (!roles.Contains(role) && !roles.Contains("Admin"))
            throw HttpError.Forbidden($"Requires the '{role}' role");
    }

    string ImportConfigPath => Path.Combine(Ctx.GetUserPath("default"), "config.json");

    JsonObject GlobalImportConfig()
    {
        try
        {
            return File.Exists(ImportConfigPath)
                ? JsonNode.Parse(File.ReadAllText(ImportConfigPath)) as JsonObject ?? throw new ArgumentException("Import config must contain a JSON object")
                : new JsonObject();
        }
        catch (Exception e) { throw new Exception($"Could not parse {ImportConfigPath}: {e.Message}", e); }
    }

    List<string> ConfiguredImportRoots()
    {
        var config = GlobalImportConfig();
        return GeminiMetadata.AsList(config.GetObject("gemini")?["importRoots"] ?? config["importRoots"]);
    }
    /// <summary>Configured import roots plus the caller's own allowed directories, never another user's.</summary>
    List<string> TrustedImportRoots(string? user) => ConfiguredImportRoots().Select(ResolveImportPath)
        .Concat(Ctx.ResolveAllowedDirectories(user)).Where(Directory.Exists).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToList();

    string ResolveImportPath(string path)
    {
        if (path.StartsWith('$')) return Ctx.ResolveDirectory(path) ?? "";
        return GeminiIngest.ResolvePath(path);
    }

    void AssertSourceAllowed(ChatSource source, ChatRequestContext req)
    {
        var config = ChatDtos.ParseJson(source.Config) as JsonObject;
        var path = config?.GetString("path");
        if (string.IsNullOrEmpty(path) || Ctx.IsAdmin(req.Request)) return;
        var imports = CrawlImportsRoot(UserOf(req));
        if (GeminiIngest.WithinRoots(ResolveImportPath(path), [imports])) return;
        var roots = TrustedImportRoots(UserOf(req));
        if (roots.Count == 0)
            throw new UnauthorizedAccessException("No trusted import folders are configured");
        if (!GeminiIngest.WithinRoots(ResolveImportPath(path), roots))
            throw new UnauthorizedAccessException($"'{ResolveImportPath(path)}' is outside the folders you may import from. Allowed: {string.Join(", ", roots)}");
    }

    Task<object?> GetImportRootsAsync(ChatRequestContext req)
    {
        var raw = ConfiguredImportRoots();
        var home = GeminiIngest.ResolvePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        return Task.FromResult<object?>(new JsonObject
        {
            ["path"] = ImportConfigPath, ["configured"] = GlobalImportConfig().GetObject("gemini")?.ContainsKey("importRoots") == true
                || GlobalImportConfig().ContainsKey("importRoots"),
            ["isAdmin"] = Ctx.IsAdmin(req.Request),
            ["roots"] = new JsonArray(raw.Select(value =>
            {
                var resolved = ResolveImportPath(value);
                return (JsonNode)new JsonObject { ["value"] = value, ["resolved"] = resolved,
                    ["exists"] = Directory.Exists(resolved), ["broad"] = resolved == Path.GetPathRoot(resolved) || resolved == home };
            }).ToArray()),
            ["effective"] = new JsonArray(TrustedImportRoots(UserOf(req)).Select(x => (JsonNode)x).ToArray()),
        });
    }

    async Task<object?> SaveImportRootsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req, "Admin").ConfigAwait();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var roots = GeminiMetadata.AsList(body["roots"]).Select(x => x.Trim().TrimEnd(Path.DirectorySeparatorChar))
            .Where(x => x.Length > 0).Distinct().ToList();
        var config = GlobalImportConfig(); var gemini = config.GetObject("gemini") ?? new JsonObject();
        gemini["importRoots"] = new JsonArray(roots.Select(x => (JsonNode)x).ToArray()); config["gemini"] = gemini;
        config.Remove("importRoots"); Directory.CreateDirectory(Path.GetDirectoryName(ImportConfigPath)!);
        var temp = ImportConfigPath + ".tmp";
        await File.WriteAllTextAsync(temp, config.ToJsonString(ChatJson.Indented) + "\n").ConfigAwait();
        File.Move(temp, ImportConfigPath, true);
        return await GetImportRootsAsync(req).ConfigAwait();
    }

    Task<object?> SourceTypesAsync(ChatRequestContext req)
    {
        var roots = TrustedImportRoots(UserOf(req));
        var imports = CrawlImportsRoot(UserOf(req));
        var rootInfo = new JsonObject
        {
            ["trusted"] = new JsonArray(ConfiguredImportRoots().Select(ResolveImportPath).Select(x => (JsonNode)x).ToArray()),
            ["allowed"] = new JsonArray(Ctx.ResolveAllowedDirectories(UserOf(req)).Select(x => (JsonNode)x).ToArray()),
            ["imports"] = new JsonArray(imports),
            ["all"] = new JsonArray(roots.Append(imports).Distinct().OrderBy(x => x).Select(x => (JsonNode)x).ToArray()),
        };
        return Task.FromResult<object?>(new JsonArray(
            new JsonObject { ["type"] = "folder", ["available"] = true, ["roots"] = rootInfo,
                ["unrestricted"] = Ctx.IsAdmin(req.Request) },
            new JsonObject { ["type"] = "zip", ["available"] = true }));
    }

    Task<object?> QuerySourcesAsync(ChatRequestContext req)
    {
        if (!long.TryParse(req.QueryString("filestoreId"), out var filestoreId))
            throw new ArgumentException("filestoreId is required");
        return Task.FromResult<object?>(db.QuerySources(filestoreId, UserOf(req)).ToDtos(x => { try { return EditableSource(x, req); } catch (Exception e) { var dto=x.ToDto();dto["error"]=ChatJson.ToErrorMessage(e);return dto; } }));
    }

    static string? Json(JsonNode? node) => node?.ToJsonString(ChatJson.Options);

    async Task<object?> CreateSourceAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var filestoreId = body.GetLong("filestoreId") ?? throw new ArgumentException("filestoreId is required");
        if (db.GetFilestore(filestoreId, UserOf(req)) == null) throw new Exception("Filestore does not exist");
        var type = body.GetString("type") ?? throw new ArgumentException("type is required");
        if (type is not ("folder" or "zip")) throw new ArgumentException($"Unknown source type '{type}'");
        var name = (body.GetString("name") ?? "").Trim();
        if (db.SavedSourceNameExists(filestoreId, UserOf(req), name)) throw new Exception($"A saved import named '{name}' already exists");
        var now = DateTime.Now; var source = new ChatSource
        {
            FilestoreId = filestoreId, User = UserOf(req), CreatedAt = now, UpdatedAt = now,
            Name = name, Type = type, Enabled = body.TryGetPropertyValue("enabled", out _) ? body.GetBool("enabled") : true,
            Config = Json(body["config"]), Category = Json(body["category"]), Rules = Json(body["rules"]),
            Include = Json(body["include"]), Exclude = Json(body["exclude"]), Extract = Json(body["extract"]),
            Chunking = Json(body["chunking"]), Volatile = Json(body["volatile"]),
            ExtractorVer = body.GetString("extractorVer") ?? GeminiIngest.ExtractorVersion,
            Schedule = body.GetString("schedule"), OnDelete = body.GetString("onDelete") ?? "tombstone",
            Cursor = Json(body["cursor"]),
        };
        AssertSourceAllowed(source, req);
        if (body.GetBool("saveConfig")) await SaveSourceSettingsAsync(source,req,body.GetObject("importOptions")).ConfigureAwait(false);
        source.Id = db.InsertSource(source);
        if (body.GetBool("saveConfig")) db.AttachSourceDocuments(source.Id,source.FilestoreId,ChatJson.ParseObject(source.Config!).GetString("manifestPath")!,UserOf(req));
        return source.ToDto();
    }

    async Task<object?> UpdateSourceAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigureAwait(false);
        var source=db.GetSource(IdOf(req),UserOf(req)) ?? throw HttpError.NotFound("Source does not exist");
        var body=await req.GetJsonBodyAsync().ConfigureAwait(false);var save=body.GetBool("saveConfig");
        var current=save?EditableSource(source,req):source.ToDto();
        if(save && current.GetObject("importOptions").GetObject("crawl").GetString("url") is { Length:>0 }) {
            var previous=current.GetObject("config")!.Clone();
            source=await RegisterImportManifestAsync(previous.GetString("manifestPath")!,source.FilestoreId,req).ConfigureAwait(false);
            current=EditableSource(source,req);
            if(previous.GetString("manifestPath")!=current.GetObject("config").GetString("manifestPath") && body.GetObject("config") is { } patch) {
                var root=Path.GetDirectoryName(previous.GetString("manifestPath"))!;var input=GeminiIngest.ResolvePath(patch.GetString("path")??previous.GetString("path")!);
                if(!GeminiIngest.WithinRoots(input,[root]))throw HttpError.BadRequest("The crawl input folder must be inside its manifest workspace");
                patch["manifestPath"]=current.GetObject("config").GetString("manifestPath");patch["path"]=Path.Combine(Path.GetDirectoryName(patch.GetString("manifestPath"))!,Path.GetRelativePath(root,input));
            }
        }
        ApplySourceSettings(source,current);ApplySourceSettings(source,body);source.Name=(source.Name??"").Trim();
        if((source.LastRunId!=null || ChatJson.TryParseObject(source.Config).GetBool("saved")) && db.SavedSourceNameExists(source.FilestoreId,UserOf(req),source.Name,source.Id))throw HttpError.BadRequest("A saved import with that name already exists");
        AssertSourceAllowed(source,req);
        if(save) {
            if(body.ContainsKey("rules")) {var config=ChatJson.TryParseObject(source.Config)??new JsonObject();config["metadataSpecified"]=true;source.Config=config.ToJsonString();}
            await SaveSourceSettingsAsync(source,req,body.GetObject("importOptions")).ConfigureAwait(false);
        }
        db.UpdateSource(source);return save?EditableSource(source,req):source.ToDto();
    }

    async Task<object?> DeleteSourceAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        if (db.GetSource(IdOf(req), UserOf(req)) == null) throw new Exception("Source does not exist");
        db.DeleteSource(IdOf(req), UserOf(req), detachDocuments: req.QueryString("documents") != "remove");
        return new JsonObject();
    }

    Task<object?> SourceRunsAsync(ChatRequestContext req) =>
        Task.FromResult<object?>(db.QuerySourceRuns(IdOf(req), UserOf(req)).ToDtos(x => x.ToDto()));

    async Task<object?> RunSourceAsync(ChatRequestContext req) {
        await AssertWriteAsync(req).ConfigureAwait(false);
        var source=db.GetSource(IdOf(req),UserOf(req)) ?? throw HttpError.NotFound("Source does not exist");
        var body=await req.GetJsonBodyAsync().ConfigureAwait(false);
        var result=await RunSourcePipelineAsync(source,req,body).ConfigureAwait(false);
        if(!result.GetBool("dryRun") && body.GetBool("saveConfig")) {
            source=db.GetSource(source.Id,UserOf(req))!;ApplySourceSettings(source,EditableSource(source,req));await SaveSourceSettingsAsync(source,req,null).ConfigureAwait(false);db.UpdateSource(source);
        }
        return result;
    }
    async Task<JsonObject> RunSourcePipelineAsync(ChatSource source,ChatRequestContext req,JsonObject body,bool refreshCrawl=false,bool startUploads=true)
    {
        var gate=SourceRunLocks.GetOrAdd(ImportLockKey(UserOf(req))+"\0"+source.Id,_=>new SemaphoreSlim(1,1));await gate.WaitAsync(req.Request.RequestAborted).ConfigureAwait(false);
        try {
            source=db.GetSource(source.Id,UserOf(req)) ?? throw HttpError.NotFound("Source does not exist");
            var row=EditableSource(source,req);
            var manifest=row.GetObject("config").GetString("manifestPath");
            if(manifest!=null && row.GetObject("importOptions").GetObject("crawl").GetString("url") is { Length:>0 }) {
                source=await RegisterImportManifestAsync(manifest,source.FilestoreId,req).ConfigureAwait(false);row=EditableSource(source,req);manifest=row.GetObject("config").GetString("manifestPath");
            }
            ApplySourceSettings(source,row);AssertSourceAllowed(source,req);
            if(refreshCrawl && manifest!=null && row.GetObject("importOptions").GetObject("crawl") is { } crawl && crawl.GetString("url") is { Length:>0 }) {
                var root=Path.GetDirectoryName(manifest)!;AssertPathAllowed(root,req);
                await CrawlSiteAsync(crawl,UserOf(req),root,req.Request.RequestAborted).ConfigureAwait(false);
                await ApplyTransformsAsync(root,row.GetObject("importOptions").GetArray("transforms")??new JsonArray()).ConfigureAwait(false);
            }
            var dryRun=body.ContainsKey("dryRun")?body.GetBool("dryRun"):source.LastRunId==null;
            if(!dryRun && db.SavedSourceNameExists(source.FilestoreId,UserOf(req),source.Name??"",source.Id))throw HttpError.BadRequest("A saved import with that name already exists");
            var run=new ChatSourceRun { SourceId=source.Id,User=UserOf(req),StartedAt=DateTime.Now,Status=dryRun?"preview":"running",DryRun=dryRun };run.Id=db.InsertSourceRun(run);
            try {
                var existing=db.SelectDocuments(new JsonObject { ["filter"]=new JsonObject { ["sourceId"]=source.Id } },UserOf(req),true);
                var plan=await Task.Run(()=>GeminiIngest.BuildPlan(source,existing,body.GetObject("set"),warning=>Log.LogWarning("{Warning}",warning)),req.Request.RequestAborted).ConfigureAwait(false);
                var candidates=new JsonArray();
                if(body.ContainsKey("addAllSourceDocuments") && !body.GetBool("addAllSourceDocuments")) {
                    var selected=body.GetArray("sourceDocuments")?.Select(x=>x!.GetValue<string>()).ToHashSet()??[];
                    foreach(var entry in plan.Added.ToArray()) {var key=source.Id+":"+entry.SourceKey;if(selected.Contains(key))continue;var candidate=entry.ToDto();candidate["key"]=key;candidate["sourceId"]=source.Id;candidate["sourceName"]=source.Name;candidates.Add(candidate);plan.Added.Remove(entry);}
                }
                var summary=plan.Summary();summary["newSourceDocuments"]=candidates;summary["newSourceCount"]=candidates.Count;
                summary["pendingUploads"]=plan.Unchanged.Count(x=>existing.Any(d=>d.SourceKey==x.SourceKey && d.UploadedAt==null && d.Error==null));
                summary["sourceChanges"]=new JsonArray(plan.Added.Concat(plan.Changed).Concat(plan.MetadataOnly).Select(x=>(JsonNode)JsonValue.Create(x.SourceKey)!).Concat(plan.Removed.Select(x=>(JsonNode)JsonValue.Create(x.SourceKey??x.DisplayName??"Document")!)).ToArray());
                var refusal=body.GetBool("confirmDeletes")?null:GeminiIngest.DeleteRefusal(plan,existing.Count);if(refusal!=null){summary["deleteRefused"]=refusal;plan.Removed.Clear();}
                if(!dryRun) {
                    var applied=await ApplyPlanAsync(plan,source,req).ConfigureAwait(false);foreach(var pair in applied)summary[pair.Key]=pair.Value?.DeepClone();
                    source.LastRunId=run.Id;source.LastRunAt=DateTime.Now;source.Error=string.Join("; ",summary.GetArray("deleteErrors")?.OfType<JsonObject>().Select(x=>x.GetString("error"))??[]);if(source.Error.Length==0)source.Error=null;db.UpdateSource(source);run.Error=source.Error;
                    if(startUploads){worker?.Start();searchWorker?.Start();}
                    summary["uploadTotal"]=(summary.GetInt("queued")??0)+(summary.GetInt("pendingUploads")??0);
                }
                PopulateRun(run,plan,summary);run.CompletedAt=DateTime.Now;run.Status=dryRun?"preview":"completed";db.UpdateSourceRun(run);
                summary["runId"]=run.Id;summary["sourceId"]=source.Id;summary["dryRun"]=dryRun;return summary;
            } catch(Exception e) {run.Status="failed";run.CompletedAt=DateTime.Now;run.Error=ChatJson.ToErrorMessage(e);db.UpdateSourceRun(run);throw;}
        } finally {gate.Release();}
    }

    static void PopulateRun(ChatSourceRun run, GeminiIngestPlan plan, JsonObject summary)
    {
        run.Discovered = plan.Discovered; run.Added = plan.Added.Count; run.Changed = plan.Changed.Count;
        run.MetadataOnly = plan.MetadataOnly.Count; run.Unchanged = plan.Unchanged.Count; run.Removed = plan.Removed.Count;
        run.Skipped = plan.Skipped.Count; run.Failed = plan.Failed.Count; run.Bytes = plan.Bytes;
        run.Plan = summary.ToJsonString(ChatJson.Options);
    }

    async Task<JsonObject> ApplyPlanAsync(GeminiIngestPlan plan, ChatSource source, ChatRequestContext req)
    {
        var queued = 0; var removed = 0; var deleteErrors = new JsonArray();
        foreach (var entry in plan.Added.Concat(plan.Changed).Concat(plan.MetadataOnly))
        {
            var bytes = Encoding.UTF8.GetBytes(entry.Text); var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var ext = Path.GetExtension(entry.SourceKey).TrimStart('.'); if (GeminiIngest.IsHtmlExtension(ext)) ext = "md"; if (ext.Length == 0) ext = "txt";
            var filename = $"{hash}.{ext}"; var relative = $"{hash[..2]}/{filename}"; var fullPath = Ctx.GetCachePath(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); await File.WriteAllBytesAsync(fullPath, bytes).ConfigAwait();
            var doc = entry.Id != null ? db.GetDocument(entry.Id.Value, UserOf(req))! : new ChatDocument
            {
                FilestoreId = source.FilestoreId, SourceId = source.Id, User = UserOf(req), CreatedAt = DateTime.Now,
            };
            doc.UpdatedAt = DateTime.Now; doc.SourceId = source.Id; doc.SourceManifestPath = ChatJson.TryParseObject(source.Config).GetString("manifestPath"); doc.SourceKey = entry.SourceKey; doc.SourceEtag = entry.SourceEtag;
            doc.DisplayName = entry.DisplayName; doc.Filename = filename; doc.Url = CacheUrlBase + relative; doc.Hash = hash; doc.Size = bytes.Length;
            doc.MimeType = MimeTypes.GetMimeType(filename); doc.ContentHash = entry.ContentHash; doc.MetadataHash = entry.MetadataHash;
            doc.ExtractorVer = entry.ExtractorVer; doc.TombstonedAt = null; doc.Error = null; doc.UploadedAt = null;
            ApplyMetadata(doc, entry.Metadata);
            db.SetSearchDesired(doc);
            if (entry.Id != null) db.UpdateDocument(doc); else { doc.Id = db.InsertDocument(doc); }
            queued++;
        }
        foreach (var doc in plan.Removed)
        {
            if (source.OnDelete == "ignore") continue;
            try
            {
                // Removing the row also removes superseded copies awaiting cleanup, which would
                // otherwise be orphaned in Gemini once their PendingDeleteNames receipt is gone.
                if (source.OnDelete == "remove") await RemoveDocumentAsync(doc, UserOf(req), req.Request.RequestAborted).ConfigAwait();
                else {
                    if (doc.Name != null)
                    {
                        try { await client.DeleteDocumentAsync(doc.Name, req.Request.RequestAborted).ConfigAwait(); }
                        catch (GeminiApiException e) when (e.StatusCode is 404 or 410) { }
                    }
                    doc.TombstonedAt = DateTime.Now; doc.Name = null; doc.State = "REMOVED_UPSTREAM"; doc.Error = null;
                    db.UpdateDocument(doc); db.RemoveSearchDocument(doc.Id);
                }
                removed++;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                var error = ChatJson.ToErrorMessage(e);
                // Keep the active row and remote name; the next import can retry this removal.
                doc.Error = error; db.UpdateDocument(doc);
                deleteErrors.Add(new JsonObject { ["id"] = doc.Id, ["displayName"] = doc.DisplayName, ["error"] = error });
            }
        }
        return new JsonObject { ["queued"] = queued, ["removedApplied"] = removed, ["deleteErrors"] = deleteErrors };
    }

    static void ApplyMetadata(ChatDocument doc, JsonObject metadata)
    {
        doc.Category = metadata.GetString("category") is { Length: > 0 } category ? category : null;
        doc.CategoryPath = Json(metadata["categoryPath"]);
        doc.DocType = metadata.GetString("docType"); doc.Status = metadata.GetString("status"); doc.Locale = metadata.GetString("locale");
        doc.Product = metadata.GetString("product"); doc.Versions = Json(metadata["versions"]); doc.Tags = Json(metadata["tags"]);
        doc.SourceUrl = metadata.GetString("sourceUrl"); doc.SourceUpdatedAt = metadata.GetLong("sourceUpdatedAt");
    }
}
