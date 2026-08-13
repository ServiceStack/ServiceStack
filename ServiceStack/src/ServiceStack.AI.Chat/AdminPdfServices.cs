using System.Text.Json.Nodes;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>A typst template published to App_Data/pdf</summary>
public class PublishedPdfTemplate
{
    public string Name { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long Size { get; set; }
    public DateTime Modified { get; set; }
    public bool HasData { get; set; }
    public bool HasSchema { get; set; }
    public bool HasPreview { get; set; }
    /// <summary>API url of the thumbnail, cache-busted by its mtime</summary>
    public string? PreviewUrl { get; set; }
    public string? PublishedBy { get; set; }
    /// <summary>Path in the designer this was published from, for the Edit link</summary>
    public string? Source { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? EditUrl { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminPdfTemplates : IGet, IReturn<AdminPdfTemplatesResponse> {}

public class AdminPdfTemplatesResponse
{
    public string PdfPath { get; set; } = null!;
    public bool TypstAvailable { get; set; }
    /// <summary>Where the Chat UI is mounted, which is also where the Admin UI borrows its JS from</summary>
    public string ChatBaseUrl { get; set; } = null!;
    public List<PublishedPdfTemplate> Results { get; set; } = [];
    /// <summary>Where the `pdf` AppTask generates models, shown so the Code view can point at it</summary>
    public string? ModelsPath { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminGetPdfTemplate : IGet, IReturn<AdminGetPdfTemplateResponse>
{
    public string Name { get; set; } = null!;
    public bool? IncludeSource { get; set; }
}

public class AdminGetPdfTemplateResponse
{
    public PublishedPdfTemplate Template { get; set; } = null!;
    /// <summary>Raw &lt;name&gt;.json, so arbitrary shapes round-trip untouched</summary>
    public string? Data { get; set; }
    /// <summary>Raw &lt;name&gt;.ui.json the Admin UI renders its form from</summary>
    public string? Schema { get; set; }
    public string? Source { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminRenderPdfTemplate : IPost, IReturn<byte[]>
{
    public string Name { get; set; } = null!;
    /// <summary>JSON the template renders with. Omitted renders the published .json sidecar.</summary>
    public string? Data { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminPdfTemplatePreview : IGet, IReturn<byte[]>
{
    public string Name { get; set; } = null!;
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminDeletePdfTemplate : IPost, IReturn<AdminDeletePdfTemplateResponse>
{
    public string Name { get; set; } = null!;
}

public class AdminDeletePdfTemplateResponse
{
    public List<string> Deleted { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminPublishPdfTemplate : IPost, IReturn<AdminPublishPdfTemplateResponse>
{
    /// <summary>Template path in the publishing user's PDF Designer folder, e.g. "reports/quote.typ"</summary>
    public string Path { get; set; } = null!;
    /// <summary>Published name, defaulting to the template's own stem</summary>
    public string? Name { get; set; }
    /// <summary>Overwrite a name published by someone else, or from a different template</summary>
    public bool? Overwrite { get; set; }
    /// <summary>Publish the shared lib.typ along with it (default true)</summary>
    public bool? PublishLib { get; set; }
}

public class AdminPublishPdfTemplateResponse
{
    public PublishedPdfTemplate? Template { get; set; }
    public List<string> Files { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public bool LibUpdated { get; set; }
    public string? Revision { get; set; }
    public PdfContractValidation? Validation { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminPdfTemplateVersions : IGet, IReturn<AdminPdfTemplateVersionsResponse>
{
    public string Name { get; set; } = null!;
}

public class AdminPdfTemplateVersionsResponse
{
    public string? CurrentRevision { get; set; }
    public List<PdfPublisher.Revision> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminRollbackPdfTemplate : IPost, IReturn<AdminRollbackPdfTemplateResponse>
{
    public string Name { get; set; } = null!;
    public string Revision { get; set; } = null!;
}

public class AdminRollbackPdfTemplateResponse
{
    public PublishedPdfTemplate Template { get; set; } = null!;
    public PdfPublisher.Revision Revision { get; set; } = null!;
    public List<string> Warnings { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminEditPdfTemplate : IPost, IReturn<AdminEditPdfTemplateResponse>
{
    public string Name { get; set; } = null!;
}

public class AdminEditPdfTemplateResponse
{
    public string EditUrl { get; set; } = null!;
    public List<string> Copied { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// The C# data model for a template, generated from its .ui.json schema so App code can populate a typed
/// DTO instead of hand-building the template's JSON.
/// <para>
/// Read-only: this is the same <see cref="PdfCodeGen"/> the <c>pdf</c> AppTask runs, so what the Admin UI
/// shows for copy/paste is exactly what <c>dotnet run --AppTasks=pdf</c> writes into the project.
/// </para>
/// </summary>
[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminPdfTemplateTypes : IPost, IReturn<AdminPdfTemplateTypesResponse>
{
    public string Name { get; set; } = null!;
    /// <summary>Data JSON being edited in the UI. Omitted uses the published &lt;name&gt;.json.</summary>
    public string? Data { get; set; }
    /// <summary>Schema JSON being edited in the UI. Omitted uses the published &lt;name&gt;.ui.json.</summary>
    public string? Schema { get; set; }
}

public class AdminPdfTemplateTypesResponse
{
    /// <summary>Root class name, e.g. "Invoice"</summary>
    public string? TypeName { get; set; }
    public string Source { get; set; } = null!;
    /// <summary>Copy/paste stubs for using the model, one per tab in the Code view</summary>
    public List<PdfCodeGenExample> Examples { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// Admin APIs behind /admin-ui/pdf: browse the templates published to App_Data/pdf, render them with
/// arbitrary data, unpublish them, and publish new ones out of the Chat PDF Designer.
/// </summary>
public partial class AdminPdfServices(PdfFeature feature, IPdfRenderer renderer) : Service
{
    string ResolveTemplate(string name, bool mustExist = true)
    {
        var stem = (name ?? "").EndsWith(".typ", StringComparison.OrdinalIgnoreCase)
            ? name[..^".typ".Length]
            : name ?? "";
        if (PdfRenderer.ReservedNames.Contains(stem, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"'{stem}' is the shared library, not a template");
        return renderer.ResolvePath(name, ".typ", mustExist);
    }

    PdfFeature AssertFeature()
    {
        RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
        return feature;
    }

    PdfFeature AssertTypst()
    {
        var to = AssertFeature();
        if (!to.IsAvailable)
            throw new Exception("typst is not installed - install it from https://typst.app to render PDFs");
        return to;
    }

    PdfPublisher Publisher => new(feature);

    public object Any(AdminPdfTemplates request)
    {
        AssertFeature();
        var publisher = Publisher;
        var manifest = publisher.GetManifest();
        return new AdminPdfTemplatesResponse
        {
            PdfPath = feature.PdfPath!,
            TypstAvailable = feature.IsAvailable,
            ChatBaseUrl = feature.ChatRoutePrefix,
            Results = publisher.GetPublishedNames().Map(x => ToTemplate(x, manifest)),
            // only when the App has an AppTask to run: without a config, GeneratePdfs() has nowhere to write
            ModelsPath = feature.PdfCodeGen != null
                ? feature.PdfCodeGen.OutputPath ?? feature.ModelsPath
                : null,
        };
    }

    public object Any(AdminGetPdfTemplate request)
    {
        AssertFeature();
        var typPath = ResolveTemplate(request.Name);
        var name = System.IO.Path.GetFileNameWithoutExtension(typPath);

        return new AdminGetPdfTemplateResponse
        {
            Template = ToTemplate(name, Publisher.GetManifest()),
            Data = ReadIfExists(name + ".json"),
            Schema = ReadIfExists(name + CoreToolsExtension.SchemaSuffix),
            Source = request.IncludeSource == true ? File.ReadAllText(typPath) : null,
        };
    }

    public object Any(AdminPdfTemplateVersions request)
    {
        AssertFeature();
        ResolveTemplate(request.Name);
        var publisher = Publisher;
        var entry = publisher.GetManifest().GetObject(request.Name);
        return new AdminPdfTemplateVersionsResponse
        {
            CurrentRevision = entry?.GetString("currentRevision"),
            Results = publisher.GetRevisions(request.Name),
        };
    }

    public object Any(AdminRollbackPdfTemplate request)
    {
        AssertFeature();
        ResolveTemplate(request.Name);
        var publisher = Publisher;
        var user = HostContext.GetPlugin<ChatFeature>()?.ChatAuth.GetUserName(Request!)
            ?? Request?.GetClaimsPrincipal()?.Identity?.Name;
        var (revision, warnings) = publisher.Rollback(request.Name, request.Revision, user);
        return new AdminRollbackPdfTemplateResponse
        {
            Template = ToTemplate(request.Name, publisher.GetManifest()),
            Revision = revision,
            Warnings = warnings,
        };
    }

    public async Task<object> Any(AdminRenderPdfTemplate request)
    {
        AssertTypst();
        ResolveTemplate(request.Name);
        var pdf = await renderer.RenderAsync(request.Name, request.Data, token: Request.RequestAborted)
            .ConfigAwait();
        return new HttpResult(pdf, MimeTypes.Pdf)
        {
            Headers = { [HttpHeaders.CacheControl] = "no-store" },
        };
    }

    public object Any(AdminPdfTemplatePreview request)
    {
        AssertFeature();
        var path = renderer.ResolvePath(request.Name, ".preview.png", mustExist: false);
        if (!File.Exists(path))
            throw HttpError.NotFound($"'{request.Name}' has no preview");

        var lastModified = File.GetLastWriteTimeUtc(path);
        return new HttpResult(new FileInfo(path), MimeTypes.ImagePng)
        {
            LastModified = lastModified,
            ETag = $"\"{lastModified.Ticks:x}\"",
            Headers = { [HttpHeaders.CacheControl] = "private, max-age=0, must-revalidate" },
        };
    }

    public object Any(AdminDeletePdfTemplate request)
    {
        AssertFeature();
        ResolveTemplate(request.Name); // validates the name + that it exists
        var name = System.IO.Path.GetFileNameWithoutExtension(request.Name);
        if (!Publisher.GetPublishedNames().Contains(name, StringComparer.OrdinalIgnoreCase))
            throw HttpError.NotFound($"'{name}' is not a published PDF template");
        return new AdminDeletePdfTemplateResponse
        {
            Deleted = Publisher.Unpublish(name),
        };
    }

    public object Any(AdminEditPdfTemplate request)
    {
        AssertFeature();
        ResolveTemplate(request.Name);
        var name = System.IO.Path.GetFileNameWithoutExtension(request.Name);
        var publisher = Publisher;
        var manifest = publisher.GetManifest();
        var entry = manifest.GetObject(name);
        var source = entry.GetString("source");
        var editUrl = $"{feature.ChatRoutePrefix}/pdf?template={(source ?? name + ".typ").UrlEncode()}";

        var chat = HostContext.GetPlugin<ChatFeature>();
        if (chat == null)
            return new AdminEditPdfTemplateResponse { EditUrl = editUrl };

        var user = chat.ChatAuth.GetUserName(Request);
        var userRoot = chat.AppData.GetUserFilePath(user, "pdf");
        Directory.CreateDirectory(userRoot);

        var files = PdfPublisher.GetManifestFiles(entry);
        var copied = new List<string>();
        var sourcePath = source ?? name + ".typ";
        var sourceStem = System.IO.Path.GetFileName(sourcePath).LeftPart('.');
        var sourceDir = PdfPublisher.GetParentDir(sourcePath);

        foreach (var flatFile in files)
        {
            // Skip thumbnails - they don't belong in the designer
            if (flatFile.EndsWith(".preview.png", StringComparison.OrdinalIgnoreCase))
                continue;

            // Reverse-flatten: map the published flat filename back to its original relative path
            string targetRel;
            if (flatFile.Equals(PdfExtension.LibName, StringComparison.OrdinalIgnoreCase))
            {
                targetRel = PdfExtension.LibName;
            }
            else if (flatFile.Equals(name + ".typ", StringComparison.OrdinalIgnoreCase))
            {
                targetRel = sourcePath;
            }
            else if (flatFile.StartsWith(name + ".", StringComparison.Ordinal))
            {
                // e.g. "invoice.json" → stem suffix is ".json", target is "reports/quote.json"
                // e.g. "invoice.logo.png" → stem suffix is ".logo.png"
                var suffix = flatFile[name.Length..]; // e.g. ".json" or ".logo.png"
                var stemSuffix = sourceStem + suffix; // e.g. "quote.json" or "quote.logo.png"

                // If the suffix starts with the source stem's extension pattern (e.g. .json, .ui.json),
                // it's a companion; otherwise it's a prefixed asset - strip the name prefix
                if (flatFile.StartsWith(name + "." + sourceStem + ".", StringComparison.Ordinal)
                    || suffix.StartsWith("." + sourceStem + ".", StringComparison.Ordinal))
                {
                    // Prefixed asset: "invoice.logo.png" → "logo.png" in source dir
                    var assetName = flatFile[(name.Length + 1)..];
                    targetRel = sourceDir.Length > 0 ? $"{sourceDir}/{assetName}" : assetName;
                }
                else
                {
                    // Stem companion: "invoice.json" → "quote.json" in source dir
                    targetRel = sourceDir.Length > 0 ? $"{sourceDir}/{stemSuffix}" : stemSuffix;
                }
            }
            else
            {
                // Unexpected file in the manifest - place it as-is in the source dir
                targetRel = sourceDir.Length > 0 ? $"{sourceDir}/{flatFile}" : flatFile;
            }

            var targetPath = System.IO.Path.Combine(userRoot, targetRel.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!PdfExtension.IsSafePath(userRoot, targetPath))
                continue;
            if (File.Exists(targetPath))
                continue;

            var srcPath = System.IO.Path.Combine(publisher.PdfPath, flatFile);
            if (!File.Exists(srcPath))
                continue;

            var targetDir = System.IO.Path.GetDirectoryName(targetPath);
            if (targetDir != null)
                Directory.CreateDirectory(targetDir);

            File.Copy(srcPath, targetPath, overwrite: false);
            copied.Add(targetRel);
        }

        return new AdminEditPdfTemplateResponse
        {
            EditUrl = editUrl,
            Copied = copied,
        };
    }

    public async Task<object> Any(AdminPublishPdfTemplate request)
    {
        AssertTypst();
        var publisher = Publisher;

        // publishing is the one part that needs the designer, so it asks for ChatFeature lazily
        var chat = HostContext.GetPlugin<ChatFeature>()
            ?? throw new Exception("Publishing requires the AI Chat PDF Designer (ChatFeature)");
        var user = chat.ChatAuth.GetUserName(Request);
        var srcRoot = chat.AppData.GetUserFilePath(user, "pdf");

        var relPath = (request.Path ?? "").Replace('\\', '/').TrimStart('/');
        if (!relPath.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .typ templates can be published");
        if (PdfExtension.IsLibraryArtifact(relPath))
        {
            var status = new ResponseStatus("LibraryArtifact",
                $"'{relPath}' is a library artifact and cannot be published as a PDF document");
            return new HttpResult(new AdminPublishPdfTemplateResponse { ResponseStatus = status },
                System.Net.HttpStatusCode.BadRequest);
        }
        PdfExtension.Resolve(srcRoot, relPath, mustExist: true);

        var name = request.Name ?? System.IO.Path.GetFileName(relPath).LeftPart('.');
        renderer.ResolvePath(name, ".typ", mustExist: false); // validates the published name

        var validation = feature.ValidateOnPublish
            ? new PdfContractValidator(feature).Validate(srcRoot, relPath, name)
            : new PdfContractValidation();
        if (!validation.IsValid)
            return ContractFailure(validation);

        // republishing your own template is silent; taking over someone else's needs a nudge
        if (request.Overwrite != true && publisher.GetOwner(name) is { } owner)
        {
            var ownerUser = owner.GetString("user");
            var ownerSource = owner.GetString("source");
            if (ownerUser != user || ownerSource != relPath)
            {
                // 409 + who owns it, so the Publish button can word its confirmation
                var conflict = new ResponseStatus("AlreadyExists",
                    $"'{name}' was published by {ownerUser ?? "someone else"} from " +
                    $"'{ownerSource ?? "an unknown template"}'")
                {
                    Meta = new Dictionary<string, string>
                    {
                        ["name"] = name,
                        ["user"] = ownerUser ?? "",
                        ["source"] = ownerSource ?? "",
                        ["publishedAt"] = owner.GetString("publishedAt") ?? "",
                    },
                };
                return new HttpResult(new AdminPublishPdfTemplateResponse { ResponseStatus = conflict },
                    System.Net.HttpStatusCode.Conflict);
            }
        }

        // whatever this publish is about to overwrite, so a republish that turns out not to compile
        // leaves the previously working template in place rather than a broken one
        var snapshot = publisher.Snapshot(name);
        PdfPublisher.PublishResult result;
        try
        {
            result = publisher.Publish(srcRoot, relPath, name, user, request.PublishLib ?? true);
        }
        catch (Exception)
        {
            publisher.Restore(name, snapshot);
            throw;
        }

        // rendering the thumbnail doubles as a smoke test that the flattened template still compiles
        try
        {
            var png = await renderer.RenderPngAsync(name, token: Request.RequestAborted).ConfigAwait();
            await File.WriteAllBytesAsync(System.IO.Path.Combine(publisher.PdfPath, name + ".preview.png"), png)
                .ConfigAwait();
            result.Files.AddIfNotExists(name + ".preview.png");
        }
        catch (TimeoutException e)
        {
            // a slow preview shouldn't fail a publish, it just means no thumbnail
            result.Warnings.Add(e.Message);
        }
        catch (Exception e)
        {
            publisher.Restore(name, snapshot); // never leave a broken template published
            throw new PdfRenderException(
                $"'{name}' does not compile once published: {e.Message}")
            {
                Diagnostics = (e as PdfRenderException)?.Diagnostics,
            };
        }

        // A template.fixture.<name>.json companion is a named contract fixture. It must satisfy the schema
        // above and compile through the flattened, published template before the publish can commit.
        foreach (var fixture in validation.Fixtures)
        {
            var fileName = $"{name}.fixture.{fixture}.json";
            var fixturePath = System.IO.Path.Combine(publisher.PdfPath, fileName);
            if (!File.Exists(fixturePath))
                continue;
            try
            {
                await renderer.RenderAsync(name, await File.ReadAllTextAsync(fixturePath, Request.RequestAborted),
                    token: Request.RequestAborted).ConfigAwait();
            }
            catch (Exception e)
            {
                publisher.Restore(name, snapshot);
                validation.Issues.Add(new PdfContractIssue
                {
                    Code = "FixtureCompile",
                    Fixture = fixture,
                    Message = $"Fixture '{fixture}' does not compile after publishing: {e.Message}",
                });
                return ContractFailure(validation);
            }
        }

        PdfPublisher.Revision revision;
        try
        {
            revision = publisher.SaveRevision(name, user, relPath, result.Files);
        }
        catch
        {
            publisher.Restore(name, snapshot);
            throw;
        }

        return new AdminPublishPdfTemplateResponse
        {
            Template = ToTemplate(name, publisher.GetManifest()),
            Files = result.Files,
            Warnings = result.Warnings,
            LibUpdated = result.LibUpdated,
            Revision = revision.Id,
            Validation = validation,
        };
    }

    static HttpResult ContractFailure(PdfContractValidation validation)
    {
        var first = validation.Issues.First(x => x.Severity == "error");
        var status = new ResponseStatus("PdfContractValidation", first.Message)
        {
            Errors = validation.Issues.Select(x => new ResponseError
            {
                ErrorCode = x.Code,
                FieldName = x.Path ?? "",
                Message = x.Message,
                Meta = new Dictionary<string, string>
                {
                    ["severity"] = x.Severity,
                    ["fixture"] = x.Fixture ?? "",
                },
            }).ToList(),
        };
        var response = new AdminPublishPdfTemplateResponse
        {
            Validation = validation,
            ResponseStatus = status,
        };
        return new HttpResult(response, System.Net.HttpStatusCode.BadRequest);
    }

    public object Any(AdminPdfTemplateTypes request)
    {
        AssertFeature();
        ResolveTemplate(request.Name); // the model must belong to a template that exists

        // the App's own codegen config, so the source shown here is namespaced exactly like the one the
        // AppTask writes - nothing for the UI to ask for or get wrong
        var config = feature.PdfCodeGen ?? new PdfCodeGenConfig();

        // the UI sends what's currently in its editors, so the source tracks unsaved edits; omitted, the
        // generator reads the template's own companions, which is what the AppTask does
        var file = new PdfCodeGen(feature).CreateFile(request.Name, config, outputPath: "",
            data: ParseJson(request.Data), schema: ParseJson(request.Schema));

        return new AdminPdfTemplateTypesResponse
        {
            TypeName = file.TypeName,
            Source = file.Source,
            Examples = PdfExamples.Create(file, config.Namespace ?? feature.ModelsNamespace),
        };
    }

    // ── Helpers ──

    /// <summary>Editor content the UI hasn't finished typing yet is null, not an error</summary>
    static JsonNode? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return ChatJson.Parse(json!);
        }
        catch (System.Text.Json.JsonException e)
        {
            throw new ArgumentException($"Invalid JSON: {e.Message}");
        }
    }

    string? ReadIfExists(string fileName)
    {
        var path = System.IO.Path.Combine(feature.PdfPath!, fileName);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    PublishedPdfTemplate ToTemplate(string name, JsonObject manifest)
    {
        var root = feature.PdfPath!;
        var typPath = System.IO.Path.Combine(root, name + ".typ");
        var info = new FileInfo(typPath);
        var previewPath = System.IO.Path.Combine(root, name + ".preview.png");
        var hasPreview = File.Exists(previewPath);
        var entry = manifest.GetObject(name);
        var source = entry.GetString("source");

        return new PublishedPdfTemplate
        {
            Name = name,
            FileName = name + ".typ",
            Size = info.Exists ? info.Length : 0,
            Modified = info.Exists ? info.LastWriteTimeUtc : default,
            HasData = File.Exists(System.IO.Path.Combine(root, name + ".json")),
            HasSchema = File.Exists(System.IO.Path.Combine(root, name + CoreToolsExtension.SchemaSuffix)),
            HasPreview = hasPreview,
            // the mtime keeps a republished thumbnail from being served from cache
            PreviewUrl = hasPreview
                ? $"/api/{nameof(AdminPdfTemplatePreview)}?name={name.UrlEncode()}" +
                  $"&v={File.GetLastWriteTimeUtc(previewPath).Ticks}"
                : null,
            PublishedBy = entry.GetString("user"),
            Source = source,
            PublishedAt = entry.GetString("publishedAt").TryParseDate(),
            EditUrl = $"{feature.ChatRoutePrefix}/pdf?template={(source ?? name + ".typ").UrlEncode()}",
        };
    }
}

internal static class AdminPdfUtils
{
    public static DateTime? TryParseDate(this string? value) =>
        DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
            ? date
            : null;
}
