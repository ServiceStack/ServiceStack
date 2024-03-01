// run node postinstall.js to update to latest version
using System.Globalization;
using Markdig;
using ServiceStack.IO;

namespace MyApp;

public class BlogConfig
{
    public static BlogConfig Instance { get; } = new();
    public string LocalBaseUrl { get; set; }
    public string PublicBaseUrl { get; set; }
    public string? SiteTwitter { get; set; }
    public List<AuthorInfo> Authors { get; set; } = new();
    public string? BlogTitle { get; set; }
    public string? BlogDescription { get; set; }
    public string? BlogEmail { get; set; }
    public string? CopyrightOwner { get; set; }
    public string? BlogImageUrl { get; set; }
}

public class AuthorInfo
{
    public string Name { get; set; }
    public string ProfileUrl { get; set; }
    public string? Bio { get; set; }
    public string? Email { get; set; }
    public string? GitHubUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? ThreadsUrl { get; set; }
    public string? MastodonUrl { get; set; }
}

public class MarkdownBlog(ILogger<MarkdownBlog> log, IWebHostEnvironment env, IVirtualFiles fs)
    : MarkdownPagesBase<MarkdownFileInfo>(log, env, fs)
{
    public override string Id => "posts";
    List<MarkdownFileInfo> Posts { get; set; } = [];

    public List<MarkdownFileInfo> VisiblePosts => Posts.Where(IsVisible).ToList();
    
    public string FallbackProfileUrl { get; set; } = Svg.ToDataUri(Svg.Create(Svg.Body.User, stroke:"none").Replace("fill='currentColor'","fill='#0891b2'"));
    public string FallbackSplashUrl { get; set; } = "https://source.unsplash.com/random/2000x1000/?stationary";

    public BlogConfig Config { get; set; } = new();
    public List<AuthorInfo> Authors { get; set; } = [];

    public Dictionary<string, AuthorInfo> AuthorSlugMap { get; } = new();
    public Dictionary<string, string> TagSlugMap { get; } = new();

    public void GenerateSlugs()
    {
        AuthorSlugMap.Clear();
        TagSlugMap.Clear();
        
        foreach (var author in Authors)
        {
            AuthorSlugMap[author.Name.GenerateSlug()] = author;
        }
        foreach (var post in Posts)
        {
            foreach (var tag in post.Tags.Safe())
            {
                TagSlugMap[tag.GenerateSlug()] = tag;
            }
        }
    }
    
    public string GetAuthorProfileUrl(string? name) => (name != null
            ? Authors.FirstOrDefault(x => x.Name == name)
            : null)?.ProfileUrl
        ?? FallbackProfileUrl;

    public List<MarkdownFileInfo> GetPosts(string? author = null, string? tag = null, int? year = null)
    {
        IEnumerable<MarkdownFileInfo> latestPosts = Posts.Where(IsVisible);
        if (author != null)
            latestPosts = latestPosts.Where(x => x.Author == author);
        if (tag != null)
            latestPosts = latestPosts.Where(x => x.Tags.Contains(tag));
        if (year != null)
            latestPosts = latestPosts.Where(x => x.Date.GetValueOrDefault().Year == year);
        return latestPosts.OrderByDescending(x => x.Date).ToList();
    }

    public string GetPostLink(MarkdownFileInfo post) => $"posts/{post.Slug}";

    public string GetPostsLink() => "posts/";
    public string? GetAuthorLink(string? author) => author != null && Authors.Any(x => x.Name.Equals(author, StringComparison.OrdinalIgnoreCase))
        ? $"posts/author/{author.GenerateSlug()}"
        : null;
    
    public string GetYearLink(int year) => $"posts/year/{year}";
    public string GetTagLink(string tag) => $"posts/tagged/{tag.GenerateSlug()}";
    public string GetDateLabel(DateTime? date) => X.Map(date ?? DateTime.UtcNow, d => d.ToString("MMMM d, yyyy"))!;
    public string GetDateTimestamp(DateTime? date) => X.Map(date ?? DateTime.UtcNow, d => d.ToString("O"))!;

    public AuthorInfo? GetAuthorBySlug(string? slug) => slug != null && AuthorSlugMap.TryGetValue(slug, out var author)
        ? author
        : null;

    public string? GetTagBySlug(string? slug) => slug != null && TagSlugMap.TryGetValue(slug, out var tag)
        ? tag
        : null;

    public string GetSplashImage(MarkdownFileInfo post)
    {
        var splash = post.Image ?? FallbackSplashUrl;
        return splash.StartsWith("https://images.unsplash.com")
            ? splash.LeftPart('?') + "?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=crop&w=420&q=80"
            : splash;
    }

    public MarkdownFileInfo? FindPostBySlug(string name) => Fresh(VisiblePosts.FirstOrDefault(x => x.Slug == name));

    public override MarkdownFileInfo? Load(string path, MarkdownPipeline? pipeline = null)
    {
        var file = VirtualFiles.GetFile(path)
            ?? throw new FileNotFoundException(path.LastRightPart('/'));
        var content = file.ReadAllText();

        var writer = new StringWriter();
        var doc = CreateMarkdownFile(content, writer, pipeline);
        if (doc.Title == null)
        {
            log.LogWarning("No frontmatter found for {VirtualPath}, ignoring...", file.VirtualPath);
            return null;
        }

        doc.Path = file.VirtualPath;
        doc.Slug = file.Name.RightPart('_').LastLeftPart('.');
        doc.FileName = file.Name;
        doc.HtmlFileName = $"{file.Name.RightPart('_').LastLeftPart('.')}.html";
        var datePart = file.Name.LeftPart('_');
        if (!DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out var date))
        {
            log.LogWarning("Could not parse date '{DatePart}', ignoring...", datePart);
            return null;
        }

        doc.WordCount = WordCount(content);
        doc.LineCount = LineCount(content);
        doc.Date = date;
        writer.Flush();
        doc.Preview = writer.ToString();

        return doc;
    }

    public void LoadFrom(string fromDirectory)
    {
        Authors.Clear();
        Posts.Clear();
        var files = VirtualFiles.GetDirectory(fromDirectory).GetAllFiles().ToList();
        log.LogInformation("Found {Count} posts", files.Count);

        var pipeline = CreatePipeline();

        foreach (var file in files)
        {
            if (file.Name == "config.json")
            {
                Config = file.ReadAllText().FromJson<BlogConfig>();
            }
            else if (file.Name == "authors.json")
            {
                Authors = file.ReadAllText().FromJson<List<AuthorInfo>>();
            }
            else if (file.Extension == "md")
            {
                try
                {
                    var doc = Load(file.VirtualPath, pipeline);
                    if (doc == null)
                        continue;

                    Posts.Add(doc);
                }
                catch (Exception e)
                {
                    log.LogError(e, "Couldn't load {VirtualPath}: {Message}", file.VirtualPath, e.Message);
                }
            }
        }

        GenerateSlugs();
    }

    public override List<MarkdownFileBase> GetAll() => 
        VisiblePosts.Map(doc => ToMetaDoc(doc, x => x.Url ??= $"/posts/{x.Slug}"));
}