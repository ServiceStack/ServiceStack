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
                ? ChatJson.TryParseObject(File.ReadAllText(ImportConfigPath)) ?? new JsonObject()
                : new JsonObject();
        }
        catch (Exception e) { throw new Exception($"Could not parse {ImportConfigPath}: {e.Message}", e); }
    }

    List<string> ConfiguredImportRoots()
    {
        var config = GlobalImportConfig();
        return GeminiMetadata.AsList(config.GetObject("gemini")?["importRoots"] ?? config["importRoots"]);
    }
    List<string> TrustedImportRoots() => ConfiguredImportRoots().Select(ResolveImportPath)
        .Concat(Ctx.ResolveAllowedDirectories()).Where(Directory.Exists).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToList();

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
        var roots = TrustedImportRoots();
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
            ["effective"] = new JsonArray(TrustedImportRoots().Select(x => (JsonNode)x).ToArray()),
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
        var roots = TrustedImportRoots();
        var imports = CrawlImportsRoot(UserOf(req));
        var rootInfo = new JsonObject
        {
            ["trusted"] = new JsonArray(ConfiguredImportRoots().Select(ResolveImportPath).Select(x => (JsonNode)x).ToArray()),
            ["allowed"] = new JsonArray(Ctx.ResolveAllowedDirectories().Select(x => (JsonNode)x).ToArray()),
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
        return Task.FromResult<object?>(db.QuerySources(filestoreId, UserOf(req)).ToDtos(x => x.ToDto()));
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
        AssertSourceAllowed(source, req); source.Id = db.InsertSource(source); return source.ToDto();
    }

    async Task<object?> UpdateSourceAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var source = db.GetSource(IdOf(req), UserOf(req)) ?? throw new Exception("Source does not exist");
        var body = await req.GetJsonBodyAsync().ConfigAwait(); var name = (body.GetString("name") ?? source.Name ?? "").Trim();
        if (source.LastRunId != null && db.SavedSourceNameExists(source.FilestoreId, UserOf(req), name, source.Id))
            throw new Exception($"A saved import named '{name}' already exists");
        source.Name = name;
        foreach (var key in new[] { "config", "category", "rules", "include", "exclude", "extract", "chunking", "volatile", "cursor" })
            if (body[key] != null) typeof(ChatSource).GetProperty(char.ToUpperInvariant(key[0]) + key[1..])!.SetValue(source, Json(body[key]));
        if (body.GetString("type") is { } type) source.Type = type;
        if (body.GetString("extractorVer") is { } version) source.ExtractorVer = version;
        if (body.GetString("schedule") is { } schedule) source.Schedule = schedule;
        if (body.GetString("onDelete") is { } onDelete) source.OnDelete = onDelete;
        if (body.TryGetPropertyValue("enabled", out _)) source.Enabled = body.GetBool("enabled");
        AssertSourceAllowed(source, req); db.UpdateSource(source); return source.ToDto();
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

    async Task<object?> RunSourceAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait(); var source = db.GetSource(IdOf(req), UserOf(req)) ?? throw new Exception("Source does not exist");
        AssertSourceAllowed(source, req); var body = await req.GetJsonBodyAsync().ConfigAwait();
        var dryRun = body.TryGetPropertyValue("dryRun", out _) ? body.GetBool("dryRun") : source.LastRunId == null;
        if (!dryRun && db.SavedSourceNameExists(source.FilestoreId, UserOf(req), source.Name ?? "", source.Id))
            throw new Exception($"A saved import named '{source.Name}' already exists");
        var run = new ChatSourceRun { SourceId = source.Id, User = UserOf(req), StartedAt = DateTime.Now,
            Status = dryRun ? "preview" : "running", DryRun = dryRun };
        run.Id = db.InsertSourceRun(run);
        try
        {
            var existing = db.SelectDocuments(new JsonObject { ["filter"] = new JsonObject { ["sourceId"] = source.Id } }, UserOf(req), true);
            var plan = await Task.Run(() => GeminiIngest.BuildPlan(source, existing, body.GetObject("set"),
                warning => Log.LogWarning("{Warning}", warning))).ConfigAwait();
            var summary = plan.Summary(); var refusal = body.GetBool("confirmDeletes") ? null : GeminiIngest.DeleteRefusal(plan, existing.Count);
            if (refusal != null) { summary["deleteRefused"] = refusal; plan.Removed.Clear(); }
            PopulateRun(run, plan, summary); run.CompletedAt = DateTime.Now; run.Status = dryRun ? "preview" : "completed";
            if (!dryRun)
            {
                var applied = await ApplyPlanAsync(plan, source, req).ConfigAwait();
                source.LastRunId = run.Id; source.LastRunAt = DateTime.Now; source.Error = null; db.UpdateSource(source); worker?.Start();
                foreach (var (key, value) in applied) summary[key] = value?.DeepClone();
                var sourceConfig = ChatDtos.ParseJson(source.Config) as JsonObject;
                if (body.GetBool("saveConfig") && source.Type == "folder" && sourceConfig?.GetBool("metadataSpecified") == true
                    && sourceConfig.GetString("path") is { } importPath)
                    await SaveImportMetadataAsync(importPath, ChatDtos.ParseJson(source.Rules) as JsonObject).ConfigAwait();
            }
            db.UpdateSourceRun(run); summary["runId"] = run.Id; summary["dryRun"] = dryRun; return summary;
        }
        catch (Exception e)
        {
            run.Status = "failed"; run.CompletedAt = DateTime.Now; run.Error = ChatJson.ToErrorMessage(e); db.UpdateSourceRun(run); throw;
        }
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
        var queued = 0; var removed = 0;
        foreach (var entry in plan.Added.Concat(plan.Changed).Concat(plan.MetadataOnly))
        {
            var bytes = Encoding.UTF8.GetBytes(entry.Text); var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var ext = Path.GetExtension(entry.SourceKey).TrimStart('.'); if (ext is "html" or "htm") ext = "md"; if (ext.Length == 0) ext = "txt";
            var filename = $"{hash}.{ext}"; var relative = $"{hash[..2]}/{filename}"; var fullPath = Ctx.GetCachePath(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); await File.WriteAllBytesAsync(fullPath, bytes).ConfigAwait();
            var doc = entry.Id != null ? db.GetDocument(entry.Id.Value, UserOf(req))! : new ChatDocument
            {
                FilestoreId = source.FilestoreId, SourceId = source.Id, User = UserOf(req), CreatedAt = DateTime.Now,
            };
            doc.UpdatedAt = DateTime.Now; doc.SourceId = source.Id; doc.SourceKey = entry.SourceKey; doc.SourceEtag = entry.SourceEtag;
            doc.DisplayName = entry.DisplayName; doc.Filename = filename; doc.Url = CacheUrlBase + relative; doc.Hash = hash; doc.Size = bytes.Length;
            doc.MimeType = MimeTypes.GetMimeType(filename); doc.ContentHash = entry.ContentHash; doc.MetadataHash = entry.MetadataHash;
            doc.ExtractorVer = entry.ExtractorVer; doc.TombstonedAt = null; doc.Error = null; doc.UploadedAt = null;
            ApplyMetadata(doc, entry.Metadata);
            if (entry.Id != null) db.UpdateDocument(doc); else { doc.Id = db.InsertDocument(doc); }
            queued++;
        }
        foreach (var doc in plan.Removed)
        {
            if (source.OnDelete == "ignore") continue;
            if (doc.Name != null)
            {
                try { await client.DeleteDocumentAsync(doc.Name).ConfigAwait(); }
                catch (GeminiApiException e) when (e.StatusCode == 404) { }
            }
            if (source.OnDelete == "remove") db.DeleteDocument(doc.Id, UserOf(req));
            else { doc.TombstonedAt = DateTime.Now; doc.Name = null; doc.State = "REMOVED_UPSTREAM"; db.UpdateDocument(doc); }
            removed++;
        }
        return new JsonObject { ["queued"] = queued, ["removedApplied"] = removed };
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
