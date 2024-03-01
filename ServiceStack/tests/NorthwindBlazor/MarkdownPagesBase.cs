// run node postinstall.js to update to latest version

using System.Text;
using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.CustomContainers;
using ServiceStack.IO;
using ServiceStack.Text;

namespace MyApp;

public class MarkdigConfig
{
    public static MarkdigConfig Instance { get; private set; } = new();
    public Action<MarkdownPipelineBuilder>? ConfigurePipeline { get; set; }
    public Action<ContainerExtensions> ConfigureContainers { get; set; } = x => x.AddBuiltInContainers();

    public static void Set(MarkdigConfig config)
    {
        Instance = config;
    }
}

public class MarkdownFileBase
{
    public string Path { get; set; } = default!;
    public string? Slug { get; set; }
    public string? Layout { get; set; }
    public string? FileName { get; set; }
    public string? HtmlFileName { get; set; }

    /// <summary>
    /// Whether to hide this document in Production
    /// </summary>
    public bool Draft { get; set; }

    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Image { get; set; }
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Date document is published. Documents with future Dates are only shown in Development 
    /// </summary>
    public DateTime? Date { get; set; }

    public string? Content { get; set; }
    public string? Url { get; set; }

    /// <summary>
    /// The rendered HTML of the Markdown
    /// </summary>
    public string? Preview { get; set; }

    public string? HtmlPage { get; set; }
    public int? WordCount { get; set; }
    public int? LineCount { get; set; }
    public string? Group { get; set; }
    public int? Order { get; set; }
    public DocumentMap? DocumentMap { get; set; }

    /// <summary>
    /// Update Markdown File to latest version
    /// </summary>
    /// <param name="newDoc"></param>
    public virtual void Update(MarkdownFileBase newDoc)
    {
        Layout = newDoc.Layout;
        Title = newDoc.Title;
        Summary = newDoc.Summary;
        Draft = newDoc.Draft;
        Image = newDoc.Image;
        Author = newDoc.Author;
        Tags = newDoc.Tags;
        Content = newDoc.Content;
        Url = newDoc.Url;
        Preview = newDoc.Preview;
        HtmlPage = newDoc.HtmlPage;
        WordCount = newDoc.WordCount;
        LineCount = newDoc.LineCount;
        Group = newDoc.Group;
        Order = newDoc.Order;
        DocumentMap = newDoc.DocumentMap;

        if (newDoc.Date != null)
            Date = newDoc.Date;
    }
}

public interface IMarkdownPages
{
    string Id { get; }
    List<MarkdownFileBase> GetAll();
}

public abstract class MarkdownPagesBase<T>(ILogger log, IWebHostEnvironment env, IVirtualFiles fs) : IMarkdownPages
    where T : MarkdownFileBase
{
    public abstract string Id { get; }
    public IVirtualFiles VirtualFiles => fs;

    public virtual MarkdownPipeline CreatePipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseAdvancedExtensions()
            .UseAutoLinkHeadings()
            .UseHeadingsMap()
            .UseCustomContainers(MarkdigConfig.Instance.ConfigureContainers);
        MarkdigConfig.Instance.ConfigurePipeline?.Invoke(builder);

        var pipeline = builder.Build();
        return pipeline;
    }

    public virtual List<T> Fresh(List<T> docs)
    {
        if (docs.IsEmpty())
            return docs;
        foreach (var doc in docs)
        {
            Fresh(doc);
        }

        return docs;
    }

    public virtual T? Fresh(T? doc)
    {
        // Ignore reloading source .md if run in production or as AppTask
        if (doc == null || !env.IsDevelopment() || AppTasks.IsRunAsAppTask())
            return doc;
        var newDoc = Load(doc.Path);
        doc.Update(newDoc);
        return doc;
    }

    public virtual T CreateMarkdownFile(string content, TextWriter writer, MarkdownPipeline? pipeline = null)
    {
        pipeline ??= CreatePipeline();

        var renderer = new Markdig.Renderers.HtmlRenderer(writer);
        pipeline.Setup(renderer);

        var document = Markdown.Parse(content, pipeline);
        renderer.Render(document);

        var block = document
            .Descendants<Markdig.Extensions.Yaml.YamlFrontMatterBlock>()
            .FirstOrDefault();

        var doc = block?
            .Lines // StringLineGroup[]
            .Lines // StringLine[]
            .Select(x => $"{x}\n")
            .ToList()
            .Select(x => x.Replace("---", string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => KeyValuePairs.Create(x.LeftPart(':').Trim(), x.RightPart(':').Trim()))
            .ToObjectDictionary()
            .ConvertTo<T>()
            ?? typeof(T).CreateInstance<T>();

        doc.Tags = doc.Tags.Map(x => x.Trim());
        doc.Content = content;
        doc.DocumentMap = document.GetData(nameof(DocumentMap)) as DocumentMap;

        return doc;
    }

    public virtual T? Load(string path, MarkdownPipeline? pipeline = null)
    {
        var file = fs.GetFile(path)
                   ?? throw new FileNotFoundException(path.LastRightPart('/'));
        var content = file.ReadAllText();

        var writer = new StringWriter();

        var doc = CreateMarkdownFile(content, writer, pipeline);
        doc.Title ??= file.Name;

        doc.Path = file.VirtualPath;
        doc.FileName = file.Name;
        doc.Slug = doc.FileName.WithoutExtension().GenerateSlug();
        doc.Content = content;
        doc.WordCount = WordCount(content);
        doc.LineCount = LineCount(content);
        writer.Flush();
        doc.Preview = writer.ToString();
        doc.Date ??= file.LastModified;

        return doc;
    }

    public virtual bool IsVisible(T doc) => env.IsDevelopment() || !doc.Draft && (doc.Date == null || doc.Date.Value <= DateTime.UtcNow);

    public int WordsPerMin { get; set; } = 225;
    public char[] WordBoundaries { get; set; } = { ' ', '.', '?', '!', '(', ')', '[', ']' };
    public virtual int WordCount(string str) => str.Split(WordBoundaries, StringSplitOptions.RemoveEmptyEntries).Length;
    public virtual int LineCount(string str) => str.CountOccurrencesOf('\n');
    public virtual int MinutesToRead(int? words) => (int)Math.Ceiling((words ?? 1) / (double)WordsPerMin);

    public virtual List<MarkdownFileBase> GetAll() => new();

    public virtual string? StripFrontmatter(string? content)
    {
        if (content == null)
            return null;
        var startPos = content.IndexOf("---", StringComparison.CurrentCulture);
        if (startPos == -1)
            return content;
        var endPos = content.IndexOf("---", startPos + 3, StringComparison.Ordinal);
        if (endPos == -1)
            return content;
        return content.Substring(endPos + 3).Trim();
    }

    public virtual MarkdownFileBase ToMetaDoc(T x, Action<MarkdownFileBase>? fn = null)
    {
        var to = new MarkdownFileBase
        {
            Slug = x.Slug,
            Title = x.Title,
            Summary = x.Summary,
            Date = x.Date,
            Tags = x.Tags,
            Author = x.Author,
            Image = x.Image,
            WordCount = x.WordCount,
            LineCount = x.LineCount,
            Url = x.Url,
            Group = x.Group,
            Order = x.Order,
        };
        fn?.Invoke(to);
        return to;
    }

    /// <summary>
    /// Need to escape '{{' and '}}' template literals when using content inside a Vue template 
    /// </summary>
    public virtual string? SanitizeVueTemplate(string? content)
    {
        if (content == null)
            return null;

        return content
            .Replace("{{", "{‎{")
            .Replace("}}", "}‎}");
    }
}

public class MarkdownIncludes(ILogger<MarkdownIncludes> log, IWebHostEnvironment env, IVirtualFiles fs)
    : MarkdownPagesBase<MarkdownFileInfo>(log, env, fs)
{
    public override string Id => "includes";
    public List<MarkdownFileInfo> Pages { get; } = [];

    public void LoadFrom(string fromDirectory)
    {
        Pages.Clear();
        var files = VirtualFiles.GetDirectory(fromDirectory).GetAllFiles()
            .OrderBy(x => x.VirtualPath)
            .ToList();
        log.LogInformation("Found {Count} includes", files.Count);

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
            }
            catch (Exception e)
            {
                log.LogError(e, "Couldn't load {VirtualPath}: {Message}", file.VirtualPath, e.Message);
            }
        }
    }

    public override List<MarkdownFileBase> GetAll() => Pages.Where(IsVisible).Map(doc => ToMetaDoc(doc, x => x.Url = $"/{x.Slug}"));
}

public struct HeadingInfo(int level, string id, string content)
{
    public int Level { get; } = level;
    public string Id { get; } = id;
    public string Content { get; } = content;
}

/// <summary>
/// An HTML renderer for a <see cref="HeadingBlock"/>.
/// </summary>
/// <seealso cref="HtmlObjectRenderer{TObject}" />
public class AutoLinkHeadingRenderer : HtmlObjectRenderer<HeadingBlock>
{
    private static readonly string[] HeadingTexts = [
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6"
    ];

    public event Action<HeadingBlock>? OnHeading;

    protected override void Write(HtmlRenderer renderer, HeadingBlock obj)
    {
        int index = obj.Level - 1;
        string[] headings = HeadingTexts;
        string headingText = ((uint)index < (uint)headings.Length)
            ? headings[index]
            : $"h{obj.Level}";

        if (renderer.EnableHtmlForBlock)
        {
            renderer.Write('<');
            renderer.Write(headingText);
            renderer.WriteAttributes(obj);
            renderer.Write('>');
        }
        renderer.WriteLeafInline(obj);

        var attrs = obj.TryGetAttributes();
        if (attrs?.Id != null && obj.Level <= 4)
        {
            renderer.Write("<a class=\"header-anchor\" href=\"javascript:;\" onclick=\"location.hash='#");
            renderer.Write(attrs.Id);
            renderer.Write("'\" aria-label=\"Permalink\">&ZeroWidthSpace;</a>");
        }

        if (renderer.EnableHtmlForBlock)
        {
            renderer.Write("</");
            renderer.Write(headingText);
            renderer.WriteLine('>');
        }

        renderer.EnsureLine();
        OnHeading?.Invoke(obj);
    }
}

public class AutoLinkHeadingsExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        renderer.ObjectRenderers.Replace<HeadingRenderer>(new AutoLinkHeadingRenderer());
    }
}

public class FilesCodeBlockRenderer(CodeBlockRenderer? underlyingRenderer = null) : HtmlObjectRenderer<CodeBlock>
{
    private readonly CodeBlockRenderer underlyingRenderer = underlyingRenderer ?? new CodeBlockRenderer();

    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        if (obj is not FencedCodeBlock fencedCodeBlock || obj.Parser is not FencedCodeBlockParser parser)
        {
            underlyingRenderer.Write(renderer, obj);
            return;
        }

        var attributes = obj.TryGetAttributes() ?? new HtmlAttributes();
        var languageMoniker = fencedCodeBlock.Info?.Replace(parser.InfoPrefix!, string.Empty);
        if (string.IsNullOrEmpty(languageMoniker))
        {
            underlyingRenderer.Write(renderer, obj);
            return;
        }

        var txt = GetContent(obj);
        renderer
            .Write("<div")
            .WriteAttributes(attributes)
            .Write(">");

        var dir = ParseFileStructure(txt);
        RenderNode(renderer, dir);
        renderer.WriteLine("</div>");
    }

    private static string GetContent(LeafBlock obj)
    {
        var code = new StringBuilder();
        foreach (var line in obj.Lines.Lines)
        {
            var slice = line.Slice;
            if (slice.Text == null)
                continue;

            var lineText = slice.Text.Substring(slice.Start, slice.Length);
            code.AppendLine();
            code.Append(lineText);
        }

        return code.ToString();
    }

    public class Node
    {
        public List<string> Files { get; set; } = [];
        public Dictionary<string, Node> Directories { get; set; } = new();
    }

    public void RenderNode(HtmlRenderer html, Node model)
    {
        foreach (var (dirName, childNode) in model.Directories)
        {
            html.WriteLine("<div class=\"ml-6\">");
            html.WriteLine("  <div class=\"flex items-center text-base leading-8\">");
            html.WriteLine("    <svg class=\"mr-1 text-slate-600 inline-block select-none align-text-bottom overflow-visible\" aria-hidden=\"true\" focusable=\"false\" role=\"img\" viewBox=\"0 0 12 12\" width=\"12\" height=\"12\" fill=\"currentColor\"><path d=\"M6 8.825c-.2 0-.4-.1-.5-.2l-3.3-3.3c-.3-.3-.3-.8 0-1.1.3-.3.8-.3 1.1 0l2.7 2.7 2.7-2.7c.3-.3.8-.3 1.1 0 .3.3.3.8 0 1.1l-3.2 3.2c-.2.2-.4.3-.6.3Z\"></path></svg>");
            html.WriteLine("    <svg class=\"mr-1 text-sky-500\" aria-hidden=\"true\" focusable=\"false\" role=\"img\" viewBox=\"0 0 16 16\" width=\"16\" height=\"16\" fill=\"currentColor\"><path d=\"M.513 1.513A1.75 1.75 0 0 1 1.75 1h3.5c.55 0 1.07.26 1.4.7l.9 1.2a.25.25 0 0 0 .2.1H13a1 1 0 0 1 1 1v.5H2.75a.75.75 0 0 0 0 1.5h11.978a1 1 0 0 1 .994 1.117L15 13.25A1.75 1.75 0 0 1 13.25 15H1.75A1.75 1.75 0 0 1 0 13.25V2.75c0-.464.184-.91.513-1.237Z\"></path></svg>");
            html.WriteLine("    <span>" + dirName + "</span>");
            html.WriteLine("  </div>");
            RenderNode(html, childNode);
            html.WriteLine("</div>");
        }

        if (model.Files.Count > 0)
        {
            html.WriteLine("<div>");
            foreach (var file in model.Files)
            {
                html.WriteLine("<div class=\"ml-6 flex items-center text-base leading-8\">");
                html.WriteLine("  <svg class=\"mr-1 text-slate-600 inline-block select-none align-text-bottom overflow-visible\" aria-hidden=\"true\" focusable=\"false\" role=\"img\" viewBox=\"0 0 16 16\" width=\"16\" height=\"16\" fill=\"currentColor\"><path d=\"M2 1.75C2 .784 2.784 0 3.75 0h6.586c.464 0 .909.184 1.237.513l2.914 2.914c.329.328.513.773.513 1.237v9.586A1.75 1.75 0 0 1 13.25 16h-9.5A1.75 1.75 0 0 1 2 14.25Zm1.75-.25a.25.25 0 0 0-.25.25v12.5c0 .138.112.25.25.25h9.5a.25.25 0 0 0 .25-.25V6h-2.75A1.75 1.75 0 0 1 9 4.25V1.5Zm6.75.062V4.25c0 .138.112.25.25.25h2.688l-.011-.013-2.914-2.914-.013-.011Z\"></path></svg>");
                html.WriteLine("  <span>" + file + "</span>");
                html.WriteLine("</div>");
            }
            html.WriteLine("</div>");
        }
    }

    public static Node ParseFileStructure(string ascii, int indent = 2)
    {
        var lines = ascii.Trim().Split('\n').Where(x => x.Trim().Length > 0);
        var root = new Node();
        var stack = new Stack<Node>();
        stack.Push(root);

        foreach (var line in lines)
        {
            var depth = line.TakeWhile(char.IsWhiteSpace).Count() / indent;
            var name = line.Trim();
            var isDir = name.StartsWith('/');

            while (stack.Count > depth + 1)
                stack.Pop();

            var parent = stack.Peek();
            if (isDir)
            {
                var dirName = name.Substring(1);
                var dirContents = new Node();
                parent.Directories[dirName] = dirContents;
                stack.Push(dirContents);
            }
            else
            {
                parent.Files.Add(name);
            }
        }
        return root;
    }
}

public class CopyContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    public string Class { get; set; } = "";
    public string BoxClass { get; set; } = "bg-gray-700";
    public string IconClass { get; set; } = "";
    public string TextClass { get; set; } = "text-lg text-white";

    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();
        if (renderer.EnableHtmlForBlock)
        {
            renderer.Write(@$"<div class=""{Class} flex cursor-pointer mb-3"" onclick=""copy(this)"">
                <div class=""flex-grow {BoxClass}"">
                    <div class=""pl-4 py-1 pb-1.5 align-middle {TextClass}"">");
        }

        // We don't escape a CustomContainer
        renderer.WriteChildren(obj);
        if (renderer.EnableHtmlForBlock)
        {
            renderer.WriteLine(@$"</div>
                    </div>
                <div class=""flex"">
                    <div class=""{IconClass} text-white p-1.5 pb-0"">
                        <svg class=""copied w-6 h-6"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"" xmlns=""http://www.w3.org/2000/svg""><path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M5 13l4 4L19 7""></path></svg>
                        <svg class=""nocopy w-6 h-6"" title=""copy"" fill='none' stroke='white' viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='1' d='M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2'></path>
                        </svg>
                    </div>
                </div>
            </div>");
        }
    }
}

public class CustomInfoRenderer : HtmlObjectRenderer<CustomContainer>
{
    public string Title { get; set; } = "TIP";
    public string Class { get; set; } = "tip";

    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();
        if (renderer.EnableHtmlForBlock)
        {
            var title = obj.Arguments ?? obj.Info;
            if (string.IsNullOrEmpty(title))
                title = Title;
            renderer.Write(@$"<div class=""{Class} custom-block"">
            <p class=""custom-block-title"">{title}</p>");
        }

        // We don't escape a CustomContainer
        renderer.WriteChildren(obj);
        if (renderer.EnableHtmlForBlock)
        {
            renderer.WriteLine("</div>");
        }
    }
}

/// <summary>
/// Render HTML-encoded inline contents inside a &gt;pre class="pre"/&lt;
/// </summary>
public class PreContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    public string Class { get; set; } = "pre";

    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();
        if (renderer.EnableHtmlForBlock)
        {
            var attrs = obj.TryGetAttributes();
            if (attrs != null && attrs.Classes.IsEmpty())
            {
                attrs.Classes ??= new();
                attrs.Classes.Add(Class);
            }

            renderer.Write("<pre").WriteAttributes(obj).Write('>');
            renderer.WriteLine();
        }

        if (obj.FirstOrDefault() is LeafBlock leafBlock)
        {
            // There has to be an official API to resolve the original text from a renderer?
            string? FindOriginalText(ContainerBlock? block)
            {
                if (block != null)
                {
                    if (block.FirstOrDefault(x => x is LeafBlock { Lines.Count: > 0 }) is LeafBlock first)
                        return first.Lines.Lines[0].Slice.Text;
                    return FindOriginalText(block.Parent);
                }

                return null;
            }

            var originalSource = leafBlock.Lines.Count > 0
                ? leafBlock.Lines.Lines[0].Slice.Text
                : FindOriginalText(obj.Parent);
            if (originalSource == null)
            {
                HostContext.Resolve<ILogger<PreContainerRenderer>>().LogError("Could not find original Text");
                renderer.WriteLine($"Could not find original Text");
            }
            else
            {
                renderer.WriteEscape(originalSource.AsSpan().Slice(leafBlock.Span.Start, leafBlock.Span.Length));
            }
        }
        else
        {
            renderer.WriteChildren(obj);
        }

        if (renderer.EnableHtmlForBlock)
        {
            renderer.WriteLine("</pre>");
        }
    }
}

public class IncludeContainerInlineRenderer : HtmlObjectRenderer<CustomContainerInline>
{
    protected override void Write(HtmlRenderer renderer, CustomContainerInline obj)
    {
        var include = obj.FirstChild is LiteralInline literalInline
            ? literalInline.Content.AsSpan().RightPart(' ').ToString()
            : null;
        if (string.IsNullOrEmpty(include))
            return;

        renderer.Write("<div").WriteAttributes(obj).Write('>');
        MarkdownFileBase? doc = null;
        if (include.EndsWith(".md"))
        {
            var includes = HostContext.TryResolve<MarkdownIncludes>();
            var pages = HostContext.TryResolve<MarkdownPages>();
            // default relative path to _includes/
            include = include[0] != '/'
                ? "_includes/" + include
                : include.TrimStart('/');

            doc = includes?.Pages.FirstOrDefault(x => x.Path == include);
            if (doc == null && pages != null)
            {
                var prefix = include.LeftPart('/');
                var slug = include.LeftPart('.');
                var allIncludes = pages.GetVisiblePages(prefix, allDirectories: true);
                doc = allIncludes.FirstOrDefault(x => x.Slug == slug);
            }
        }

        if (doc?.Preview != null)
        {
            renderer.WriteLine(doc.Preview!);
        }
        else
        {
            var log = HostContext.Resolve<ILogger<IncludeContainerInlineRenderer>>();
            log.LogError("Could not find: {Include}", include);
            renderer.WriteLine($"Could not find: {include}");
        }

        renderer.Write("</div>");
    }
}

public class YouTubeContainerInlineRenderer : HtmlObjectRenderer<CustomContainerInline>
{
    public string? ContainerClass { get; set; } = "flex justify-center";
    public string? Class { get; set; } = "w-full mx-4 my-4";

    protected override void Write(HtmlRenderer renderer, CustomContainerInline obj)
    {
        var videoId = obj.FirstChild is LiteralInline literalInline
            ? literalInline.Content.AsSpan().RightPart(' ').ToString()
            : null;
        if (string.IsNullOrEmpty(videoId))
            return;

        if (ContainerClass != null) renderer.WriteLine($"<div class=\"{ContainerClass}\">");
        renderer.WriteLine(
            $"<lite-youtube class=\"{Class}\" width=\"560\" height=\"315\" videoid=\"{videoId}\" style=\"background-image:url('https://img.youtube.com/vi/{videoId}/maxresdefault.jpg')\"></lite-youtube>");
        if (ContainerClass != null) renderer.WriteLine("</div>");
    }
}

public class YouTubeContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    public string? ContainerClass { get; set; } // = "flex justify-center";
    public string? Class { get; set; } = "w-full mx-4 my-4";

    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();

        var videoId = (obj.Arguments ?? "").TrimEnd(':');
        if (string.IsNullOrEmpty(videoId))
        {
            renderer.WriteLine("<!-- youtube: Missing YouTube Video Id -->");
            return;
        }

        var title = ((obj.Count > 0 ? obj[0] as ParagraphBlock : null)?.Inline?.FirstChild as LiteralInline)?.Content
            .ToString() ?? "";
        if (ContainerClass != null) renderer.WriteLine($"<div class=\"{ContainerClass}\">");
        renderer.WriteLine(
            $"<lite-youtube class=\"{Class}\" width=\"560\" height=\"315\" videoid=\"{videoId}\" playlabel=\"{title}\" style=\"background-image:url('https://img.youtube.com/vi/{videoId}/maxresdefault.jpg')\"></lite-youtube>");
        if (ContainerClass != null) renderer.WriteLine("</div>");
    }
}

public class CustomCodeBlockRenderers(ContainerExtensions extensions, CodeBlockRenderer? underlyingRenderer = null)
    : HtmlObjectRenderer<CodeBlock>
{
    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        var useRenderer = obj is FencedCodeBlock { Info: not null } f &&
                          extensions.CodeBlocks.TryGetValue(f.Info, out var customRenderer)
            ? customRenderer(underlyingRenderer)
            : underlyingRenderer ?? new CodeBlockRenderer();
        useRenderer.Write(renderer, obj);
    }
}

public class CustomContainerRenderers(ContainerExtensions extensions) : HtmlObjectRenderer<CustomContainer>
{
    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        var useRenderer = obj.Info != null && extensions.BlockContainers.TryGetValue(obj.Info, out var customRenderer)
            ? customRenderer
            : new HtmlCustomContainerRenderer();
        useRenderer.Write(renderer, obj);
    }
}

public class CustomContainerInlineRenderers(ContainerExtensions extensions) : HtmlObjectRenderer<CustomContainerInline>
{
    protected override void Write(HtmlRenderer renderer, CustomContainerInline obj)
    {
        var firstWord = obj.FirstChild is LiteralInline literalInline
            ? literalInline.Content.AsSpan().LeftPart(' ').ToString()
            : null;
        var useRenderer =
            firstWord != null && extensions.InlineContainers.TryGetValue(firstWord, out var customRenderer)
                ? customRenderer
                : new HtmlCustomContainerInlineRenderer();
        useRenderer.Write(renderer, obj);
    }
}

public class ContainerExtensions : IMarkdownExtension
{
    public Dictionary<string, Func<CodeBlockRenderer?, HtmlObjectRenderer<CodeBlock>>> CodeBlocks { get; set; } = new();
    public Dictionary<string, HtmlObjectRenderer<CustomContainer>> BlockContainers { get; set; } = new();
    public Dictionary<string, HtmlObjectRenderer<CustomContainerInline>> InlineContainers { get; set; } = new();

    public void AddCodeBlock(string name, Func<CodeBlockRenderer?, HtmlObjectRenderer<CodeBlock>> fenceCodeBlock) =>
        CodeBlocks[name] = fenceCodeBlock;

    public void AddBlockContainer(string name, HtmlObjectRenderer<CustomContainer> container) =>
        BlockContainers[name] = container;

    public void AddInlineContainer(string name, HtmlObjectRenderer<CustomContainerInline> container) =>
        InlineContainers[name] = container;

    public void AddBuiltInContainers(string[]? exclude = null)
    {
        CodeBlocks = new()
        {
            ["files"] = origRenderer => new FilesCodeBlockRenderer(origRenderer)
        };
        BlockContainers = new()
        {
            ["copy"] = new CopyContainerRenderer
            {
                Class = "not-prose copy cp",
                IconClass = "bg-sky-500",
            },
            ["sh"] = new CopyContainerRenderer
            {
                Class = "not-prose sh-copy cp",
                BoxClass = "bg-gray-800",
                IconClass = "bg-green-600",
                TextClass = "whitespace-pre text-base text-gray-100",
            },
            ["tip"] = new CustomInfoRenderer(),
            ["info"] = new CustomInfoRenderer
            {
                Class = "info",
                Title = "INFO",
            },
            ["warning"] = new CustomInfoRenderer
            {
                Class = "warning",
                Title = "WARNING",
            },
            ["danger"] = new CustomInfoRenderer
            {
                Class = "danger",
                Title = "DANGER",
            },
            ["pre"] = new PreContainerRenderer(),
            ["youtube"] = new YouTubeContainerRenderer(),
        };
        InlineContainers = new()
        {
            ["include"] = new IncludeContainerInlineRenderer(),
            ["youtube"] = new YouTubeContainerInlineRenderer(),
        };

        if (exclude != null)
        {
            foreach (var name in exclude)
            {
                BlockContainers.TryRemove(name, out _);
                InlineContainers.TryRemove(name, out _);
            }
        }
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<CustomContainerParser>())
        {
            // Insert the parser before any other parsers
            pipeline.BlockParsers.Insert(0, new CustomContainerParser());
        }

        // Plug the inline parser for CustomContainerInline
        var inlineParser = pipeline.InlineParsers.Find<EmphasisInlineParser>();
        if (inlineParser != null && !inlineParser.HasEmphasisChar(':'))
        {
            inlineParser.EmphasisDescriptors.Add(new EmphasisDescriptor(':', 2, 2, true));
            inlineParser.TryCreateEmphasisInlineList.Add((emphasisChar, delimiterCount) =>
            {
                if (delimiterCount >= 2 && emphasisChar == ':')
                {
                    return new CustomContainerInline();
                }

                return null;
            });
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            var originalCodeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
            if (originalCodeBlockRenderer != null)
            {
                htmlRenderer.ObjectRenderers.Remove(originalCodeBlockRenderer);
            }

            htmlRenderer.ObjectRenderers.AddIfNotAlready(
                new CustomCodeBlockRenderers(this, originalCodeBlockRenderer));

            if (!htmlRenderer.ObjectRenderers.Contains<CustomContainerRenderers>())
            {
                // Must be inserted before CodeBlockRenderer
                htmlRenderer.ObjectRenderers.Insert(0, new CustomContainerRenderers(this));
            }

            htmlRenderer.ObjectRenderers.TryRemove<HtmlCustomContainerInlineRenderer>();
            // Must be inserted before EmphasisRenderer
            htmlRenderer.ObjectRenderers.Insert(0, new CustomContainerInlineRenderers(this));
        }
    }
}

public class HeadingsMapExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var headingBlockParser = pipeline.BlockParsers.Find<HeadingBlockParser>();
        if (headingBlockParser != null)
        {
            // Install a hook on the HeadingBlockParser when a HeadingBlock is actually processed
            // headingBlockParser.Closed -= HeadingBlockParser_Closed;
            // headingBlockParser.Closed += HeadingBlockParser_Closed;
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer.ObjectRenderers.TryFind<AutoLinkHeadingRenderer>(out var customHeader))
        {
            customHeader.OnHeading += OnHeading;
        }
    }

    private void OnHeading(HeadingBlock headingBlock)
    {
        if (headingBlock.Parent is not MarkdownDocument document)
            return;

        if (document.GetData(nameof(DocumentMap)) is not DocumentMap docMap)
        {
            docMap = new();
            document.SetData(nameof(DocumentMap), docMap);
        }

        var text = headingBlock.Inline?.FirstChild is LiteralInline literalInline
            ? literalInline.ToString()
            : null;
        var attrs = headingBlock.TryGetAttributes();

        if (!string.IsNullOrEmpty(text) && attrs?.Id != null)
        {
            if (headingBlock.Level == 2)
            {
                docMap.Headings.Add(new MarkdownMenu
                {
                    Text = text,
                    Link = $"#{attrs.Id}",
                });
            }
            else if (headingBlock.Level == 3)
            {
                var lastHeading = docMap.Headings.LastOrDefault();
                if (lastHeading != null)
                {
                    lastHeading.Children ??= new();
                    lastHeading.Children.Add(new MarkdownMenuItem
                    {
                        Text = text,
                        Link = $"#{attrs.Id}",
                    });
                }
            }
        }
    }
}

public static class MarkdigExtensions
{
    /// <summary>
    /// Uses the auto-identifier extension.
    /// </summary>
    public static MarkdownPipelineBuilder UseAutoLinkHeadings(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new AutoLinkHeadingsExtension());
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseHeadingsMap(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new HeadingsMapExtension());
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseCustomContainers(this MarkdownPipelineBuilder pipeline,
        Action<ContainerExtensions>? configure = null)
    {
        var ext = new ContainerExtensions();
        configure?.Invoke(ext);
        pipeline.Extensions.AddIfNotAlready(ext);
        return pipeline;
    }
}

public class DocumentMap
{
    public List<MarkdownMenu> Headings { get; } = new();
}

public class MarkdownMenu
{
    public string? Icon { get; set; }
    public string? Text { get; set; }
    public string? Link { get; set; }
    public List<MarkdownMenuItem>? Children { get; set; }
}

public class MarkdownMenuItem
{
    public string Text { get; set; }
    public string Link { get; set; }
}