// run node postinstall.js to update to latest version
using ServiceStack.IO;

namespace MyApp;

public class MarkdownPages(ILogger<MarkdownPages> log, IWebHostEnvironment env, IVirtualFiles fs)
    : MarkdownPagesBase<MarkdownFileInfo>(log, env, fs)
{
    public override string Id => "pages";

    public virtual string? DefaultMenuIcon { get; set; } =
        "<svg class='h-6 w-6 shrink-0 text-sky-500' fill='none' viewBox='0 0 24 24' stroke-width='1.5' stroke='currentColor' aria-hidden='true'><path stroke-linecap='round' stroke-linejoin='round' d='M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25'></path></svg>";

    List<MarkdownFileInfo> Pages { get; set; } = new();
    public List<MarkdownFileInfo> GetVisiblePages(string? prefix=null, bool allDirectories=false) => prefix == null 
        ? Pages.Where(x => IsVisible(x) && !x.Slug!.Contains('/')).OrderBy(x => x.Order).ThenBy(x => x.Path).ToList()
        : Pages.Where(x => IsVisible(x) && x.Slug!.StartsWith(prefix.WithTrailingSlash()))
            .Where(x => allDirectories || (x.Slug.CountOccurrencesOf('/') == prefix.CountOccurrencesOf('/') + 1))
            .OrderBy(x => x.Order).ThenBy(x => x.Path).ToList();

    public MarkdownFileInfo? GetBySlug(string slug)
    {
        slug = slug.Trim('/');
        return Fresh(Pages.Where(IsVisible).FirstOrDefault(x => x.Slug == slug));
    }

    public Dictionary<string, List<MarkdownMenu>> Sidebars { get; set; } = new();

    public void LoadFrom(string fromDirectory)
    {
        Sidebars.Clear();
        Pages.Clear();
        var files = VirtualFiles.GetDirectory(fromDirectory).GetAllFiles()
            .OrderBy(x => x.VirtualPath)
            .ToList();
        log.LogInformation("Found {Count} pages", files.Count);

        var pipeline = CreatePipeline();

        foreach (var file in files)
        {
            try
            {
                if (file.Extension == "md")
                {
                    var doc = Load(file.VirtualPath, pipeline);
                    if (doc == null)
                        continue;

                    var relativePath = file.VirtualPath[(fromDirectory.Length + 1)..];
                    if (relativePath.IndexOf('/') >= 0)
                    {
                        doc.Slug = relativePath.LastLeftPart('/') + '/' + doc.Slug;
                    }

                    Pages.Add(doc);
                }
                else if (file.Name == "sidebar.json")
                {
                    var virtualPath = file.VirtualPath.Substring(fromDirectory.Length);
                    var folder = virtualPath[..^"sidebar.json".Length].Trim('/');
                    var sidebarJson = file.ReadAllText();
                    var sidebar = sidebarJson.FromJson<List<MarkdownMenu>>();

                    // If first entry is home and icon is not provided or '' use DefaultMenuIcon 
                    var defaultMenu = sidebar.FirstOrDefault();
                    if (defaultMenu?.Link?.Trim('/') == folder && defaultMenu.Icon == null)
                    {
                        defaultMenu.Icon = DefaultMenuIcon;
                    }
                    Sidebars[folder] = sidebar;
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Couldn't load {VirtualPath}: {Message}", file.VirtualPath, e.Message);
            }
        }
        if (Sidebars.Count > 0)
        {
            log.LogInformation("Loaded {Count} sidebars: {Sidebars}", Sidebars.Count, Sidebars.Keys.Join(", "));
        }
    }

    public override List<MarkdownFileBase> GetAll() => Pages.Where(IsVisible).Map(doc => ToMetaDoc(doc, x => x.Url = $"/{x.Slug}"));

    public virtual List<MarkdownMenu> GetSidebar(string folder, MarkdownMenu? defaultMenu=null)
    {
        if (Sidebars.TryGetValue(folder, out var sidebar))
            return sidebar;

        var allPages = GetVisiblePages(folder);
        var allGroups = allPages.Select(x => x.Group)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        sidebar = new List<MarkdownMenu>();
        foreach (var group in allGroups)
        {
            MarkdownMenu? menuItem;
            if (group == null)
            {
                menuItem = defaultMenu ?? new MarkdownMenu {
                    Children = [],
                };
            }
            else
            {
                menuItem = new() {
                    Text = group
                };
            }
            sidebar.Add(menuItem);
        
            foreach (var page in allPages.Where(x => x.Group == group).OrderBy(x => x.Order))
            {
                menuItem.Children ??= [];
                var link = page.Slug!;
                if (link.EndsWith("/index"))
                {
                    link = link.Substring(0, link.Length - "index".Length);
                    // Hide /index from auto Sidebar as it's included in Docs Page Sidebar Header by default
                    if (link.Trim('/') == folder)
                        continue;
                }
                menuItem.Children.Add(new()
                {
                    Text = page.Title!,
                    Link = link,
                });
            }
        }

        return sidebar;
    }
}
