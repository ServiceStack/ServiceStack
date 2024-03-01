---
draft: true
title: Prerendering Razor Pages
summary: Improving Blog Performance with Prerendering
author: Lucy Bates
tags: [c#, dev, markdown]
image: https://images.unsplash.com/photo-1522526886914-6e8d4fd91399?crop=entropy&fit=crop&h=1000&w=2000
---

Prerendering static content is a popular technique used by [JAMStack](https://jamstack.org) Apps to improve the
performance, reliability and scalability of Web Apps that's able to save unnecessary computation at runtime by 
generating static content at deployment which can be optionally hosted from a CDN for even greater performance.

As such we thought it a valuable technique to include in this template to show how it can be easily achieved
within a Razor Pages Application. Since prerendered content is only updated at deployment, it's primarily only 
useful for static content like this Blog which is powered by the static markdown content in 
[_blog/posts](https://github.com/NetCoreTemplates/vue-mjs/tree/main/MyApp/wwwroot/_blog/posts) whose content
is prerendered to: 

  - [/blog](https://vue-mjs.web-templates.io/blog)

### Parsing Markdown Files

All the functionality to load and render Markdown files is maintained in 
[Configure.Markdown.cs](https://github.com/NetCoreTemplates/vue-mjs/blob/main/MyApp/Configure.Markdown.cs),
most of which is spent populating the POCO below from the content and frontmatter of each Markdown file:  

```csharp
public class MarkdownFileInfo
{
    public string Path { get; set; } = default!;
    public string? Slug { get; set; }
    public string? FileName { get; set; }
    public string? HtmlFileName { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Splash { get; set; }
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime? Date { get; set; }
    public string? Content { get; set; }
    public string? Preview { get; set; }
    public string? HtmlPage { get; set; }
    public int? WordCount { get; set; }
    public int? LineCount { get; set; }
}
```

Which uses the popular [Markdig](https://github.com/xoofx/markdig) library to parse the frontmatter into a
Dictionary that it populates the POCO with using the built-in [Automapping](https://docs.servicestack.net/auto-mapping):

```csharp
var content = VirtualFiles.GetFile(path).ReadAllText();
var document = Markdown.Parse(content, pipeline);
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
    .ConvertTo<MarkdownFileInfo>();
```

Since this is a [Jekyll inspired blog](https://jekyllrb.com/docs/step-by-step/08-blogging/) it derives the **date** and **slug** for each 
post from its file name which has the nice property of maintaining markdown blog posts in chronological order:

```csharp
doc.Slug = file.Name.RightPart('_').LastLeftPart('.');
doc.HtmlFileName = $"{file.Name.RightPart('_').LastLeftPart('.')}.html";

var datePart = file.Name.LeftPart('_');
if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.AdjustToUniversal, out var date))
{
    doc.Date = date;
}
```

The rendering itself is done using Markdig's `HtmlRenderer` which renders the Markdown content into a HTML fragment:  

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter()
    .UseAdvancedExtensions()
    .Build();
var writer = new StringWriter();
var renderer = new Markdig.Renderers.HtmlRenderer(writer);
pipeline.Setup(renderer);
//...

renderer.Render(document);
writer.Flush();
doc.Preview = writer.ToString();
```

At this point we've populated Markdown Blog Posts into a POCO which is the data source used to implement all the blog's functionality. 

We can now start prerendering entire HTML Pages by rendering the markdown inside the 
[Post.cshtml](https://github.com/NetCoreTemplates/vue-mjs/blob/main/MyApp/Pages/Posts/Post.cshtml) Razor Page by populating its PageModel
from the `MarkdownFileInfo` POCO. It also sets a `Static` flag that tells the Razor Page that this page is being statically rendered so 
it can render the appropriate links.

```csharp
var page = razorPages.GetView("/Pages/Posts/Post.cshtml");
var model = new Pages.Posts.PostModel(this) { Static = true }.Populate(doc);
doc.HtmlPage = RenderToHtml(page.View, model);

public string RenderToHtml(IView? page, PageModel model)
{
    using var ms = MemoryStreamFactory.GetStream();
    razorPages.WriteHtmlAsync(ms, page, model).GetAwaiter().GetResult();
    ms.Position = 0;
    var html = Encoding.UTF8.GetString(ms.ReadFullyAsMemory().Span);
    return html;
}
```

The use of `GetResult()` on an async method isn't ideal, but something we have to live with until there's a better way 
to run async code on Startup.

The actual rendering of the Razor Page is done with ServiceStack's `RazorPagesEngine` feature which sets up the necessary 
Http, View and Page contexts to render Razor Pages, registered in ASP.NET Core's IOC at:

```csharp
.ConfigureServices(services => {
    services.AddSingleton<RazorPagesEngine>();
})
```

The process of saving the prerendered content is then simply a matter of saving the rendered Razor Page at the preferred locations,
done for each post and the [/blog](https://vue-mjs.web-templates.io/blog) index page using the
[Posts/Index.cshtml](https://github.com/NetCoreTemplates/vue-mjs/blob/main/MyApp/Pages/Posts/Index.cshtml) Razor Page:

```csharp
foreach (var file in files)
{
    // prerender /blog/{slug}.html
    if (renderTo != null)
    {
        log.InfoFormat("Writing {0}/{1}...", renderTo, doc.HtmlFileName);
        fs.WriteFile($"{renderTo}/{doc.HtmlFileName}", doc.HtmlPage);
    }
}

// prerender /blog/index.html
if (renderTo != null)
{
    log.InfoFormat("Writing {0}/index.html...", renderTo);
    RenderToFile(razorPages.GetView("/Pages/Posts/Index.cshtml").View, 
        new Pages.Posts.IndexModel { Static = true }, $"{renderTo}/index.html");
}
```

### Prerendering Pages Task

Next we need to come up with a solution to run this from the command-line.
[App Tasks](https://docs.servicestack.net/app-tasks) is ideal for this which lets you run one-off tasks within the full context of your App 
but without the overhead of maintaining a separate .exe with duplicated App configuration & logic, instead we can run the .NET App to 
run the specified Tasks then exit before launching its HTTP Server.

To do this we'll register this task with the **prerender** AppTask name:

```csharp
AppTasks.Register("prerender", args => blogPosts.LoadPosts("_blog/posts", renderTo: "blog"));
```

Which we can run now from the command-line with:

```bash
$ dotnet run --AppTasks=prerender
```

To make it more discoverable, this is also registered as an npm script in `package.json`:

```json
{
    "scripts": {
        "prerender": "dotnet run --AppTasks=prerender"
    }
}
```

That can now be run to prerender this blog to `/wwwroot/blog` with: 

```bash
$ npm run prerender
```

### Prerendering at Deployment

To ensure this is always run at deployment it's also added as an MS Build task in **MyApp.csproj**:

```xml
<Target Name="AppTasks" AfterTargets="Build" Condition="$(APP_TASKS) != ''">
    <CallTarget Targets="Prerender" Condition="$(APP_TASKS.Contains('prerender'))" />
</Target>
<Target Name="Prerender">
    <Message Text="Prerender..." />
    <Exec Command="dotnet run --AppTasks=prerender" />
</Target>
```

Configured to run when the .NET App is published in the GitHub Actions deployment task in 
[/.github/workflows/release.yml](https://github.com/NetCoreTemplates/vue-mjs/blob/main/.github/workflows/release.yml):

```yaml
 # Publish .NET Project
 - name: Publish dotnet project
   working-directory: ./MyApp
   run: | 
     dotnet publish -c Release /p:APP_TASKS=prerender
```

Where it's able to control which App Tasks are run at deployment. 

### Pretty URLs for static .html pages

A nicety we can add to serving static `.html` pages is giving them [Pretty URLs](https://en.wikipedia.org/wiki/Clean_URL)
by registering the Plugin: 

```csharp
Plugins.Add(new CleanUrlsFeature());
```

Which allows prerendered pages to be accessed with and without its file extension:

 - [/blog/prerendering](https://vue-mjs.web-templates.io/blog/prerendering)
 - [/blog/prerendering.html](https://vue-mjs.web-templates.io/blog/prerendering.html)

### 