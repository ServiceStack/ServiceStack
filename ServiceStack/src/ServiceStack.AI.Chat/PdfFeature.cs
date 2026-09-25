using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;

namespace ServiceStack.AI;

/// <summary>
/// Published PDF templates: the typst templates in App_Data/pdf that this App renders PDFs from,
/// e.g. returned by an API or attached to an email.
/// <para>
/// Templates are authored in the AI Chat PDF Designer (per-user) and promoted here by an Admin with
/// AdminPublishPdfTemplate. Rendering itself has no dependency on ChatFeature — only publishing
/// does, since that's what reads the designer's folder.
/// </para>
/// </summary>
public partial class PdfFeature : IPlugin, Model.IHasStringId, IConfigureServices, IPreInitPlugin
{
    public string Id => "pdf";

    /// <summary>Where published templates live. Default: {ContentRoot}/App_Data/pdf</summary>
    public string? PdfPath { get; set; }

    /// <summary>Path to the typst CLI, resolved on Register from $TYPST_PATH or PATH</summary>
    public string? TypstPath { get; set; }

    /// <summary>How long a single typst compile may run before it's killed</summary>
    public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Longer budget for rasterising a preview, which costs more than the PDF itself</summary>
    public TimeSpan PreviewTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Resolution of the &lt;name&gt;.preview.png thumbnails written on publish</summary>
    public int PreviewPpi { get; set; } = 96;

    /// <summary>Concurrent typst processes, so a burst of renders can't saturate the host</summary>
    public int MaxConcurrentRenders { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Largest data JSON a render accepts. It rides typst's argv (--input data=), which the OS caps
    /// well below 128KB once the environment is counted, so this stays conservative.
    /// </summary>
    public int MaxDataBytes { get; set; } = 64 * 1024;

    /// <summary>Validate example/fixture data and generated model contracts before publishing</summary>
    public bool ValidateOnPublish { get; set; } = true;

    /// <summary>Where the Chat UI is mounted, for the Admin UI's Edit link + borrowed JS modules</summary>
    public string ChatRoutePrefix { get; set; } = "/chat";

    /// <summary>
    /// Where the <c>pdf</c> StartupTask generates PDF data models, when its config doesn't say. Set to
    /// override; otherwise resolved on Register to the App's ServiceModel folder + "/Pdf", and null when
    /// no ServiceModel folder was found.
    /// <para>
    /// Their own folder because generated names come from the document's keys and are generic enough
    /// (Item, From, Details) to collide with the App's own types.
    /// </para>
    /// <para>Nothing is ever written outside <see cref="GeneratePdfs"/> — see <see cref="AI.PdfCodeGen"/>.</para>
    /// </summary>
    public string? ModelsPath { get; set; }

    /// <summary>
    /// Namespace generated models are emitted into. Set to override; otherwise the namespace the
    /// App's ServiceModel sources declare + ".Pdf", to match <see cref="ModelsPath"/>.
    /// </summary>
    public string? ModelsNamespace { get; set; }

    /// <summary>Replaceable renderer, also resolvable as IPdfRenderer</summary>
    public IPdfRenderer Renderer { get; set; } = null!;

    /// <summary>False when typst isn't installed: templates can still be listed and unpublished</summary>
    public bool IsAvailable => !string.IsNullOrEmpty(TypstPath);
    
    public PdfCodeGenConfig? PdfCodeGen { get; set; }

    public ILogger Log { get; set; } = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    public string SvgIcon { get; set; } =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path d="M0 0h16v16H0z" fill="none" /><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="M2.8 14.34c1.81-1.25 3.02-3.16 3.91-5.5c.9-2.33 1.86-4.33 1.44-6.63c-.06-.36-.57-.73-.83-.7c-1.02.06-.95 1.21-.85 1.9c.24 1.71 1.56 3.7 2.84 5.56c1.27 1.87 2.32 2.16 3.78 2.26c.5.03 1.25-.14 1.37-.58c.77-2.8-9.02-.54-12.28 2.08c-.4.33-.86 1-.6 1.46c.2.36.87.4 1.23.15h0Z" /></svg>""";

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        // so modules/admin-ui/components/AdminPdf.mjs is discoverable when this plugin is used
        // without ChatFeature, which registers the same assembly
        var assembly = GetType().Assembly;
        if (appHost.Config.EmbeddedResourceBaseTypes.All(x => x.Assembly != assembly))
            appHost.Config.EmbeddedResourceBaseTypes.Add(GetType());
    }

    public void Configure(IServiceCollection services)
    {
        Renderer ??= new PdfRenderer(this);
        services.AddSingleton(this);
        services.AddSingleton(_ => Renderer);
        services.RegisterService<AdminPdfServices>();

        services.ConfigurePlugin<UiFeature>(feature =>
        {
            feature.AddAdminLink(AdminUiFeature.Dynamic, new LinkInfo {
                Id = "pdf",
                Label = "PDF",
                Icon = Svg.ImageSvg(SvgIcon),
                Show = $"role:{RoleNames.Admin}",
            });
            feature.AddAdminComponent("pdf", "AdminPdf");
        });
    }

    public void Register(IAppHost appHost)
    {
        Log = appHost.GetApplicationServices().GetRequiredService<ILogger<PdfFeature>>();

        PdfPath ??= appHost.MapProjectPath("~/App_Data/pdf");
        Directory.CreateDirectory(PdfPath);

        TypstPath ??= Environment.GetEnvironmentVariable("TYPST_PATH") ?? ProcessUtils.FindExePath("typst");
        if (!IsAvailable)
            Log.LogInformation("typst not found in PATH, PDF rendering disabled");
        else
            Log.LogInformation("Using {Typst} to render {Path}", TypstPath, PdfPath);

        // the Admin UI links back to the designer and borrows its JS modules, wherever it's mounted
        if (appHost.GetPlugin<ChatFeature>() is { } chat)
            ChatRoutePrefix = chat.RoutePrefix;

        // both default to a Pdf/ subfolder of the App's ServiceModel, so generated models are
        // namespaced away from the App's own types rather than sitting alongside them
        var serviceModelPath = ModelsPath == null || ModelsNamespace == null
            ? ResolveServiceModelPath(appHost)
            : null;
        ModelsPath ??= serviceModelPath != null
            ? Path.Combine(serviceModelPath, PdfModelsFolder)
            : null;
        if (serviceModelPath != null)
            ModelsNamespace ??= ResolveServiceModelNamespace(serviceModelPath) is { } ns
                ? ns + "." + PdfModelsFolder
                : null;
    }

    /// <summary>Folder + namespace segment generated models default into</summary>
    public const string PdfModelsFolder = "Pdf";

    /// <summary>
    /// The namespace the App's own ServiceModel sources use, since it can't be derived reliably: the
    /// folder is often "ServiceModel" inside a differently-named project (e.g. MyApp.ServiceModel).
    /// </summary>
    string? ResolveServiceModelNamespace(string dir)
    {
        try
        {
            var declared = Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
                .SelectMany(x => File.ReadLines(x).Take(30))
                .Select(line => NamespaceRegex().Match(line))
                .Where(m => m.Success)
                .Select(m => m.Groups["ns"].Value)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
            if (declared != null)
                return declared;

            // an empty folder still names itself in the multi-project layout
            var name = new DirectoryInfo(dir).Name;
            return name.Contains('.') ? name : null;
        }
        catch (Exception e)
        {
            Log.LogDebug(e, "Could not resolve ServiceModel namespace");
            return null;
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\s*namespace\s+(?<ns>[A-Za-z_][A-Za-z0-9_.]*)\s*[;{]?\s*$")]
    private static partial System.Text.RegularExpressions.Regex NamespaceRegex();

    /// <summary>
    /// Finds the App's ServiceModel folder, covering both layouts ServiceStack Apps use: a folder in
    /// the host project, or the sibling MyApp.ServiceModel project `x new` templates create.
    /// </summary>
    string? ResolveServiceModelPath(IAppHost appHost)
    {
        try
        {
            var inProject = appHost.MapProjectPath("~/ServiceModel");
            if (Directory.Exists(inProject))
                return inProject;

            var contentRoot = appHost.MapProjectPath("~/");
            var parent = Directory.GetParent(contentRoot.TrimEnd(Path.DirectorySeparatorChar, '/'));
            if (parent?.Exists != true)
                return null;

            // only when it's unambiguous: a solution with several is not ours to guess between
            var siblings = parent.GetDirectories("*.ServiceModel")
                .Where(x => x.EnumerateFiles("*.csproj").Any())
                .ToList();
            return siblings.Count == 1 ? siblings[0].FullName : null;
        }
        catch (Exception e)
        {
            // a missing or unreadable project layout just means models can't be saved from the UI
            Log.LogDebug(e, "Could not resolve ServiceModel path");
            return null;
        }
    }

    /// <summary>
    /// Generates a typed C# model for every published PDF template, typically registered as a development
    /// StartupTask — the same source the Admin UI's Code view shows.
    /// </summary>
    /// <example><code>
    /// StartupTasks.Register("pdf", () => appHost.GetPlugin&lt;PdfFeature&gt;().GeneratePdf(new() {
    ///     Namespace = "MyApp.ServiceModel.Pdf",
    ///     OutputPath = Path.Combine(contentRootPath, "ServiceModel/Pdf"),
    /// }));
    /// </code></example>
    public PdfCodeGenResult GeneratePdfs(PdfCodeGenConfig? config = null)
    {
        config ??= PdfCodeGen;
        if (config == null)
            throw new InvalidOperationException("PdfCodeGen configuration is not set.");
        var result = new PdfCodeGen(this).Generate(config);
        Log.LogInformation("Generated PDF models in {Path}\n{Log}",
            config.OutputPath ?? ModelsPath, result.GetLog());
        return result;
    }
}
