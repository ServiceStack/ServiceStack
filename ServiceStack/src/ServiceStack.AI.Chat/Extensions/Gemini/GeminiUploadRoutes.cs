using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    sealed record UploadPart(string Key, string DisplayName, byte[] Content, string MimeType, string? Category,
        JsonObject? Metadata = null);

    async Task<object?> QueueManualUploadsAsync(ChatRequestContext req, long filestoreId, string? user, string? queryCategory)
    {
        var form = req.Request.FormData;
        string? Field(string name) => form[name] ?? req.QueryString(name);
        var category = Field("category") ?? queryCategory;
        var sourceUrl = Field("sourceUrl");
        ValidateSourceUrlTemplate(sourceUrl);
        var metadata = new JsonObject
        {
            ["docType"] = Field("docType"), ["status"] = Field("status"), ["locale"] = Field("locale"),
            ["product"] = Field("product"), ["versions"] = new JsonArray(SplitValues(Field("versions")).Select(x => (JsonNode)x).ToArray()),
            ["tags"] = new JsonArray(SplitValues(Field("tags")).Select(x => (JsonNode)x).ToArray()),
        };
        if (!string.IsNullOrEmpty(sourceUrl)) metadata["sourceUrl"] = sourceUrl;
        var files = req.Request.Files.Where(x => x.Name?.StartsWith("file") == true).ToList();
        if (files.Count == 0) files = req.Request.Files.ToList();
        var parts = new List<UploadPart>();
        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.FileName)) continue;
            using var ms = new MemoryStream(); await file.InputStream.CopyToAsync(ms).ConfigAwait();
            var filename = file.FileName.LastRightPart('/').LastRightPart('\\'); var bytes = ms.ToArray();
            if (filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                parts.AddRange(ExpandZip(bytes, category, metadata));
            else
                parts.Add(new UploadPart(filename, filename, bytes, MimeTypes.GetMimeType(filename), category));
        }

        var ids = new List<long>();
        foreach (var part in parts)
        {
            var partMetadata = part.Metadata?.Clone() ?? metadata.Clone();
            if (sourceUrl != null)
            {
                var expandedUrl = GeminiIngest.ExpandTemplate(sourceUrl,
                    GeminiIngest.TemplateValues(part.Key, part.Category, part.DisplayName),
                    warning => Log.LogWarning("{SourceKey}: {Warning}", part.Key, warning));
                if (expandedUrl == null)
                    partMetadata.Remove("sourceUrl");
                else
                    partMetadata["sourceUrl"] = expandedUrl;
            }
            ids.Add(await QueueManualDocumentAsync(filestoreId, user, part, partMetadata).ConfigAwait());
        }
        worker?.Start();
        searchWorker?.Start();
        var docs = ids.Count == 0 ? [] : db.QueryDocuments(new JsonObject
            { ["ids_in"] = string.Join(',', ids), ["take"] = ids.Count }, user);
        return docs.ToDtos(ToClientDto);
    }

    static List<string> SplitValues(string? value) => string.IsNullOrWhiteSpace(value) ? []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();

    static void ValidateSourceUrlTemplate(string? template)
        => GeminiIngest.ValidateTemplate(template);

    List<UploadPart> ExpandZip(byte[] content, string? baseCategory, JsonObject? overrideMetadata = null)
    {
        using var ms = new MemoryStream(content); using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var allEntries = archive.Entries.Where(x => !string.IsNullOrEmpty(x.Name) && !x.FullName.Contains("__MACOSX/")).ToList();
        var entries = allEntries.Where(x => !GeminiIngest.MatchesAny(x.FullName.Replace('\\', '/'), GeminiIngest.DefaultExcludes)).ToList();
        var firstSegments = entries.Select(x => x.FullName.Replace('\\', '/').Split('/')[0]).Distinct().ToList();
        var stripWrapper = firstSegments.Count == 1 && entries.All(x => x.FullName.Contains('/'));
        string Normalize(string value) { var key = value.Replace('\\', '/').TrimStart('/'); return stripWrapper && key.Contains('/') ? key[(key.IndexOf('/') + 1)..] : key; }
        var manifests = new Dictionary<string, JsonObject>();
        foreach (var entry in allEntries.Where(x => Normalize(x.FullName) is "import.json" || Normalize(x.FullName).EndsWith("/import.json")))
        {
            var key = Normalize(entry.FullName); try { using var reader = new StreamReader(entry.Open(), Encoding.UTF8); manifests[Path.GetDirectoryName(key)?.Replace('\\', '/') ?? ""] = ChatJson.TryParseObject(reader.ReadToEnd()) ?? new JsonObject(); }
            catch (Exception e) { throw new ArgumentException($"Invalid {key}: {e.Message}", e); }
        }
        var ret = new List<UploadPart>();
        foreach (var entry in entries)
        {
            var key = Normalize(entry.FullName);
            using var input = entry.Open(); using var output = new MemoryStream(); input.CopyTo(output); var bytes = output.ToArray();
            var ext = Path.GetExtension(key).TrimStart('.').ToLowerInvariant(); var displayName = Path.GetFileName(key);
            var extracted = GeminiIngest.Extract(bytes, key, new JsonObject { ["minWords"] = 0 });
            if (extracted.Skip == null)
            {
                bytes = Encoding.UTF8.GetBytes(extracted.Text!);
                if (ext is "html" or "htm") { displayName = Path.ChangeExtension(displayName, ".md"); key = Path.ChangeExtension(key, ".md"); }
            }
            else if (ext is not ("pdf" or "docx" or "pptx" or "xlsx"))
                continue;
            var directory = key.Contains('/') ? key[..key.LastIndexOf('/')] : "";
            var inherited = new JsonObject(); var current = "";
            foreach (var part in new string?[] { null }.Concat(directory.Split('/', StringSplitOptions.RemoveEmptyEntries)))
            {
                if (part != null) current = current.Length == 0 ? part : current + "/" + part;
                if (manifests.TryGetValue(current, out var manifest)) inherited = MergeImportMetadata(inherited, manifest.GetObject("metadata"));
            }
            var derived = GeminiIngest.DeriveMetadata(key, inherited, extracted.Frontmatter, null, overrideMetadata).Metadata ?? new JsonObject();
            var category = string.Join('/', new[] { baseCategory?.Trim('/'), directory }.Where(x => !string.IsNullOrEmpty(x)));
            ret.Add(new UploadPart(key, extracted.Frontmatter.GetString("title") ?? displayName, bytes,
                MimeTypes.GetMimeType(displayName), category, derived));
        }
        return ret;
    }

    async Task<long> QueueManualDocumentAsync(long filestoreId, string? user, UploadPart part, JsonObject metadata)
    {
        var hash = Convert.ToHexString(SHA256.HashData(part.Content)).ToLowerInvariant();
        var ext = Path.GetExtension(part.DisplayName).TrimStart('.'); if (ext.Length == 0) ext = "bin";
        var saveFilename = $"{hash}.{ext}"; var relative = $"{hash[..2]}/{saveFilename}"; var fullPath = Ctx.GetCachePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); await File.WriteAllBytesAsync(fullPath, part.Content).ConfigAwait();
        var info = new JsonObject { ["date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ["url"] = CacheUrlBase + relative,
            ["size"] = part.Content.Length, ["type"] = part.MimeType, ["name"] = part.DisplayName };
        await File.WriteAllTextAsync(Path.ChangeExtension(fullPath, null) + ".info.json", info.ToJsonString(ChatJson.Options)).ConfigAwait();
        var doc = db.FindDocumentBySourceKey(filestoreId, null, part.Key, user) ?? new ChatDocument
        {
            FilestoreId = filestoreId, User = user, CreatedAt = DateTime.Now, SourceKey = part.Key,
        };
        doc.UpdatedAt = DateTime.Now; doc.Filename = saveFilename; doc.Url = CacheUrlBase + relative; doc.Hash = hash;
        doc.Size = part.Content.Length; doc.DisplayName = part.DisplayName; doc.MimeType = part.MimeType; doc.Category = part.Category;
        doc.DocType = metadata.GetString("docType"); doc.Status = metadata.GetString("status"); doc.Locale = metadata.GetString("locale");
        doc.Product = metadata.GetString("product"); doc.Versions = Json(metadata["versions"]); doc.Tags = Json(metadata["tags"]);
        doc.SourceUrl = metadata.GetString("sourceUrl"); doc.ContentHash = GeminiIngest.ContentHash(Encoding.UTF8.GetString(part.Content));
        doc.Error = null; doc.StartedAt = null; doc.UploadedAt = null; doc.TombstonedAt = null;
        db.SetSearchDesired(doc);
        if (doc.Id == 0) { doc.Id = db.InsertDocument(doc); return doc.Id; }
        db.UpdateDocument(doc); return doc.Id;
    }
}
