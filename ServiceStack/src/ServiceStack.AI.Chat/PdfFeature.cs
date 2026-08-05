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

    /// <summary>Where the Chat UI is mounted, for the Admin UI's Edit link + borrowed JS modules</summary>
    public string ChatRoutePrefix { get; set; } = "/chat";

    /// <summary>
    /// Where the <c>pdf</c> AppTask generates PDF data models, when its config doesn't say. Set to
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
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path d="M0 0h16v16H0z" fill="none" /><path fill="currentColor" d="M13.156 9.211c-.213-.21-.686-.321-1.406-.331a12 12 0 0 0-1.69.124c-.276-.159-.561-.333-.784-.542c-.601-.561-1.103-1.34-1.415-2.197c.02-.08.038-.15.054-.222c0 0 .339-1.923.249-2.573a.7.7 0 0 0-.044-.184l-.029-.076c-.092-.212-.273-.437-.556-.425l-.171-.005c-.316 0-.573.161-.64.403c-.205.757.007 1.889.39 3.355l-.098.239c-.275.67-.619 1.345-.923 1.94l-.04.077c-.32.626-.61 1.157-.873 1.607l-.271.144c-.02.01-.485.257-.594.323c-.926.553-1.539 1.18-1.641 1.678c-.032.159-.008.362.156.456l.263.132a.8.8 0 0 0 .357.086c.659 0 1.425-.821 2.48-2.662a25 25 0 0 1 3.819-.908c.926.521 2.065.883 2.783.883q.193 0 .327-.036a.56.56 0 0 0 .325-.222c.139-.21.168-.499.13-.795a.53.53 0 0 0-.157-.271zM3.307 12.72c.12-.329.596-.979 1.3-1.556c.044-.036.153-.138.253-.233c-.736 1.174-1.229 1.642-1.553 1.788zm4.169-9.6c.212 0 .333.534.343 1.035s-.107.853-.252 1.113c-.12-.385-.179-.992-.179-1.389c0 0-.009-.759.088-.759M6.232 9.961q.222-.395.458-.839c.383-.724.624-1.29.804-1.755a5.8 5.8 0 0 0 1.328 1.649q.098.083.207.166c-1.066.211-1.987.467-2.798.779zm6.72-.06c-.065.041-.251.064-.37.064c-.386 0-.864-.176-1.533-.464q.386-.029.705-.029c.387 0 .502-.002.88.095s.383.293.318.333z" /><path fill="currentColor" d="M14.341 3.579c-.347-.473-.831-1.027-1.362-1.558S11.894 1.006 11.421.659C10.615.068 10.224 0 10 0H2.25C1.561 0 1 .561 1 1.25v13.5c0 .689.561 1.25 1.25 1.25h11.5c.689 0 1.25-.561 1.25-1.25V5c0-.224-.068-.615-.659-1.421m-2.07-.85c.48.48.856.912 1.134 1.271h-2.406V1.595c.359.278.792.654 1.271 1.134zM14 14.75c0 .136-.114.25-.25.25H2.25a.253.253 0 0 1-.25-.25V1.25c0-.135.115-.25.25-.25H10v3.5a.5.5 0 0 0 .5.5H14z" /></svg>""";

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
    /// Generates a typed C# model for every published PDF template, to register as an AppTask and run with
    /// <c>dotnet run --AppTasks=pdf</c> — the same source the Admin UI's Code view shows.
    /// </summary>
    /// <example><code>
    /// AppTasks.Register("pdf", _ => appHost.GetPlugin&lt;PdfFeature&gt;().GeneratePdf(new() {
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
