using System.Reflection;
using System.Text;

namespace ServiceStack.AI;

/// <summary>
/// What the <c>pdf</c> StartupTask generates and where it puts it.
/// <para>
/// Set on <see cref="PdfFeature.PdfCodeGen"/>, so the Admin UI's Code view can show the same source the
/// task writes rather than asking for a namespace it would only get wrong.
/// </para>
/// </summary>
/// <example><code>
/// services.AddPlugin(new PdfFeature {
///     PdfCodeGen = new() {
///         Namespace = "MyApp.ServiceModel.Pdf",
///         OutputPath = Path.Combine(contentRootPath, "ServiceModel/Pdf"),
///         Exclude = ["invoice"],   // hand-tuned, leave it alone
///     }
/// });
///
/// StartupTasks.Register("pdf", () => appHost.GetPlugin&lt;PdfFeature&gt;().GeneratePdfs());
/// </code></example>
public class PdfCodeGenConfig
{
    /// <summary>
    /// Namespace the generated models are emitted into. Defaults to <see cref="PdfFeature.ModelsNamespace"/>,
    /// i.e. the App's ServiceModel namespace + ".Pdf".
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Folder the generated .cs files are written to, created if it doesn't exist. Defaults to
    /// <see cref="PdfFeature.ModelsPath"/>, i.e. the App's ServiceModel folder + "/Pdf".
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>Only generate these published templates. Empty (default) generates all of them.</summary>
    public List<string> Include { get; set; } = [];

    /// <summary>
    /// Templates to leave alone, e.g. one whose model you've since taken ownership of. Removing a generated
    /// file's marker already preserves it when <see cref="PreserveModified"/> is enabled — this says so up
    /// front, and also covers templates you never wanted a model for.
    /// </summary>
    public List<string> Exclude { get; set; } = [];

    /// <summary>Names the file a template is generated into. Default: "invoice" → "Invoice.cs"</summary>
    public Func<string, string>? ResolveFileName { get; set; }

    /// <summary>
    /// Preserve any existing file without the generated marker. Remove the opening
    /// <c>&lt;auto-generated&gt;</c> comment from a generated file to take ownership of it. Set false to
    /// overwrite unmarked files as well.
    /// </summary>
    public bool PreserveModified { get; set; } = true;

    /// <summary>Extra usings to emit, on top of the ones the generated types need</summary>
    public List<string> Usings { get; set; } = [];

    /// <summary>
    /// Last say over each file before it's written: change the source, the file name, or set
    /// <see cref="PdfCodeGenFile.Skip"/> to drop it.
    /// </summary>
    public Action<PdfCodeGenFile>? Filter { get; set; }
}

/// <summary>One template's generated model, before it's written</summary>
public class PdfCodeGenFile
{
    /// <summary>Published template this was generated from, e.g. "invoice"</summary>
    public string Template { get; set; } = null!;

    /// <summary>Root class name, e.g. "Invoice"</summary>
    public string? TypeName { get; set; }

    /// <summary>File name within <see cref="PdfCodeGenConfig.OutputPath"/>, e.g. "Invoice.cs"</summary>
    public string FileName { get; set; } = null!;

    /// <summary>Absolute path this will be written to</summary>
    public string Path { get; set; } = null!;

    /// <summary>Namespace the generated C# types are emitted into</summary>
    public string? Namespace { get; set; }

    /// <summary>The generated C#, without its header</summary>
    public string Source { get; set; } = null!;

    /// <summary>The types the source was emitted from, for anything else that needs to read the shape</summary>
    public JsonTypes.JsonTypesModel Model { get; set; } = null!;

    /// <summary>Set in a <see cref="PdfCodeGenConfig.Filter"/> to leave this template ungenerated</summary>
    public bool Skip { get; set; }

    /// <summary>Why it was skipped, for the task's log</summary>
    public string? SkipReason { get; set; }
}

/// <summary>What a <see cref="PdfCodeGen"/> run did, so the StartupTask can report it</summary>
public class PdfCodeGenResult
{
    /// <summary>Files written, either new or regenerated</summary>
    public List<string> Generated { get; set; } = [];

    /// <summary>Files already identical to what would have been written</summary>
    public List<string> Unchanged { get; set; } = [];

    /// <summary>Templates left alone: excluded, filtered out, or a model that's been edited by hand</summary>
    public List<string> Skipped { get; set; } = [];

    /// <summary>Templates that couldn't be generated, keyed by name</summary>
    public Dictionary<string, string> Errors { get; set; } = new();

    public string GetLog()
    {
        var sb = new StringBuilder();
        Generated.Each(x => sb.AppendLine($"  generated {x}"));
        Unchanged.Each(x => sb.AppendLine($"  unchanged {x}"));
        Skipped.Each(x => sb.AppendLine($"  skipped   {x}"));
        Errors.Each(x => sb.AppendLine($"  FAILED    {x.Key}: {x.Value}"));
        return sb.ToString();
    }
}

/// <summary>
/// Generates a typed C# model for every template published to App_Data/pdf, so App code can populate a DTO
/// and hand it to <see cref="PdfRendererExtensions.RenderPdfAsync{T}"/> instead of hand-building JSON.
/// <para>
/// Registered as a development StartupTask so model changes are synchronized whenever the App restarts.
/// </para>
/// </summary>
public class PdfCodeGen(PdfFeature feature)
{
    /// <summary>Marks a file as generator-owned. Remove this line to preserve the file on future runs.</summary>
    public const string HeaderPrefix = "// <auto-generated>";
    public const string HeaderSuffix = "// </auto-generated>";
    const string LegacyHeaderPrefix = "// <auto-generated hash=\"";

    private Dictionary<string, HashSet<string>>? templateNamespaces;

    public PdfCodeGenResult Generate(PdfCodeGenConfig config)
    {
        var result = new PdfCodeGenResult();

        var outputPath = config.OutputPath ?? feature.ModelsPath
            ?? throw new ArgumentException(
                "No output folder was found to generate into. Set PdfCodeGenConfig.OutputPath or " +
                "PdfFeature.ModelsPath to the folder generated PDF models should be written to.",
                nameof(config.OutputPath));

        var include = new HashSet<string>(config.Include, StringComparer.OrdinalIgnoreCase);
        var exclude = new HashSet<string>(config.Exclude, StringComparer.OrdinalIgnoreCase);
        var templates = new PdfPublisher(feature).GetPublishedNames();

        var files = new List<PdfCodeGenFile>();
        foreach (var template in templates)
        {
            if (include.Count > 0 && !include.Contains(template))
                continue;
            if (exclude.Contains(template))
            {
                result.Skipped.Add($"{template} (excluded)");
                continue;
            }

            try
            {
                files.Add(CreateFile(template, config, outputPath));
            }
            catch (Exception e)
            {
                result.Errors[template] = e.Message;
            }
        }

        if (files.Count > 0)
            Directory.CreateDirectory(outputPath);

        var written = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            config.Filter?.Invoke(file);
            if (file.Skip)
            {
                result.Skipped.Add($"{file.Template}{(file.SkipReason != null ? $" ({file.SkipReason})" : "")}");
                continue;
            }

            // two templates whose schemas share a title land on the same file — say so rather than let the
            // second silently replace the first
            if (written.TryGetValue(file.FileName, out var owner))
            {
                result.Errors[file.Template] =
                    $"'{file.FileName}' was already generated from '{owner}'. Give one of them its own file " +
                    $"with PdfCodeGenConfig.ResolveFileName.";
                continue;
            }
            written[file.FileName] = file.Template;

            try
            {
                var content = WithHeader(file);
                if (File.Exists(file.Path))
                {
                    var existing = File.ReadAllText(file.Path).Replace("\r\n", "\n");
                    if (existing == content)
                    {
                        result.Unchanged.Add(file.FileName);
                        continue;
                    }
                    if (config.PreserveModified && !CanOverwrite(existing, out var reason))
                    {
                        result.Skipped.Add($"{file.Template} ({file.FileName} {reason})");
                        continue;
                    }
                }
                File.WriteAllText(file.Path, content);
                result.Generated.Add(file.FileName);
            }
            catch (Exception e)
            {
                result.Errors[file.Template] = e.Message;
            }
        }

        return result;
    }

    /// <summary>
    /// Generates one template's model without touching the file system — how the Admin UI shows the source
    /// for copy/paste, so what you see there is what the StartupTask writes here.
    /// <para>
    /// <paramref name="data"/> and <paramref name="schema"/> override the template's own .json/.ui.json,
    /// which is how the UI previews edits it hasn't published yet.
    /// </para>
    /// </summary>
    public PdfCodeGenFile CreateFile(string template, PdfCodeGenConfig config, string? outputPath = null,
        JsonNode? data = null, JsonNode? schema = null)
    {
        var model = BuildModel(template, data, schema);
        var typeName = model.RootTypeName;
        var fileName = config.ResolveFileName?.Invoke(template)
            ?? (typeName ?? JsonTypes.Pascal(template)) + ".cs";

        outputPath ??= config.OutputPath ?? feature.ModelsPath;
        var csharpOptions = CSharpOptions(template, config);
        return new PdfCodeGenFile
        {
            Template = template,
            TypeName = typeName,
            FileName = fileName,
            Path = outputPath != null ? System.IO.Path.Combine(outputPath, fileName) : fileName,
            Namespace = csharpOptions.Namespace,
            Source = JsonTypes.ToCSharp(model, csharpOptions),
            Model = model,
        };
    }

    JsonTypes.CSharpOptions CSharpOptions(string template, PdfCodeGenConfig config)
    {
        var attributeNamespace = ResolveAttributeNamespace(template);
        return new JsonTypes.CSharpOptions
        {
            Namespace = attributeNamespace ?? config.Namespace ?? feature.ModelsNamespace,
            // [Pdf] binds the model to its template, so App code never repeats the template name.
            // Preserve a per-template namespace override so subsequent generations remain deterministic.
            Usings = ["ServiceStack.AI", ..config.Usings],
            RootAttributes = [attributeNamespace != null
                ? $"Pdf(\"{template}\", Namespace = \"{attributeNamespace}\")"
                : $"Pdf(\"{template}\")"],
        };
    }

    /// <summary>
    /// Finds namespace overrides on the App's already-loaded [Pdf] models. Only assemblies referencing
    /// this feature are inspected, and discovery is cached for the lifetime of this generation run.
    /// </summary>
    string? ResolveAttributeNamespace(string template)
    {
        templateNamespaces ??= FindTemplateNamespaces();
        if (!templateNamespaces.TryGetValue(template, out var namespaces) || namespaces.Count == 0)
            return null;
        if (namespaces.Count > 1)
            throw new InvalidOperationException(
                $"Multiple [Pdf(\"{template}\")] models specify different namespaces: " +
                string.Join(", ", namespaces.OrderBy(x => x, StringComparer.Ordinal)));
        return namespaces.First();
    }

    static Dictionary<string, HashSet<string>> FindTemplateNamespaces()
    {
        var to = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var pdfAssembly = typeof(PdfAttribute).Assembly;
        var pdfAssemblyName = pdfAssembly.GetName().Name;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || !ReferencesPdfAssembly(assembly, pdfAssembly, pdfAssemblyName))
                continue;

            foreach (var type in GetLoadableTypes(assembly))
            {
                var attr = type.GetCustomAttribute<PdfAttribute>(inherit: false);
                if (attr == null || string.IsNullOrWhiteSpace(attr.Template) ||
                    string.IsNullOrWhiteSpace(attr.Namespace))
                    continue;

                if (!to.TryGetValue(attr.Template, out var namespaces))
                    to[attr.Template] = namespaces = new HashSet<string>(StringComparer.Ordinal);
                namespaces.Add(attr.Namespace!);
            }
        }
        return to;
    }

    static bool ReferencesPdfAssembly(Assembly assembly, Assembly pdfAssembly, string? pdfAssemblyName)
    {
        if (assembly == pdfAssembly)
            return true;
        try
        {
            return assembly.GetReferencedAssemblies().Any(x => x.Name == pdfAssemblyName);
        }
        catch
        {
            return false;
        }
    }

    static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(x => x != null)!;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// The template's schema and example data. The schema wins, but the example is still read so a string
    /// <c>format</c> the data disagrees with doesn't produce a model that can't parse it.
    /// </summary>
    JsonTypes.JsonTypesModel BuildModel(string template, JsonNode? data = null, JsonNode? schema = null)
    {
        var name = template + CoreToolsExtension.SchemaSuffix;
        schema ??= ReadJson(template + CoreToolsExtension.SchemaSuffix);
        data ??= ReadJson(template + ".json");
        return JsonTypes.BuildModel(name, data, schema);
    }

    JsonNode? ReadJson(string fileName)
    {
        var path = System.IO.Path.Combine(feature.PdfPath!, fileName);
        return File.Exists(path) ? ChatJson.Parse(File.ReadAllText(path)) : null;
    }

    // ── Header ──

    /// <summary>
    /// A header naming what wrote the file. Its opening marker explicitly indicates that the file remains
    /// generator-owned; removing that marker transfers ownership to the developer.
    /// </summary>
    static string WithHeader(PdfCodeGenFile file)
    {
        // the <auto-generated> marker opts the file out of analysers, which also switches its nullable
        // context off — so the annotations the generator emits need turning back on explicitly
        var body = "#nullable enable\n\n" + file.Source.Replace("\r\n", "\n");
        return HeaderPrefix + "\n"
            + $"//     Generated from App_Data/pdf/{file.Template}{CoreToolsExtension.SchemaSuffix}"
            + " during development startup.\n"
            + "//     Remove this <auto-generated> line to preserve this file on future runs.\n"
            + HeaderSuffix + "\n"
            + body;
    }

    /// <summary>
    /// Whether this run may replace what's already there. The marker is an explicit ownership flag:
    /// marked files belong to the generator, while unmarked files belong to the developer.
    /// </summary>
    static bool CanOverwrite(string content, out string reason)
    {
        reason = "";
        // Also accepts the former <auto-generated hash="..."> marker so existing generated files migrate
        // naturally to the simpler ownership marker on their next run.
        if (!content.StartsWith(HeaderPrefix + "\n", StringComparison.Ordinal) &&
            !content.StartsWith(LegacyHeaderPrefix, StringComparison.Ordinal))
        {
            reason = "has no <auto-generated> marker";
            return false;
        }

        return true;
    }
}
