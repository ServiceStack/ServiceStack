// run node postinstall.js to update to latest version
using Markdig;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MyApp;

[HtmlTargetElement("markdown", TagStructure = TagStructure.NormalOrSelfClosing)]
[HtmlTargetElement(Attributes = "markdown")]
public class MarkdownTagHelper : TagHelper
{
    public ModelExpression? Content { get; set; }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (output.TagName == "markdown")
        {
            output.TagName = null;
        }
        output.Attributes.RemoveAll("markdown");

        var content = await GetContent(output);
        
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var writer = new StringWriter();
        var cls = output.Attributes["class"]?.Value ?? "prose";
        await writer.WriteAsync($"<div class=\"{cls}\">");
        var renderer = new Markdig.Renderers.HtmlRenderer(writer);
        pipeline.Setup(renderer);

        var include = output.Attributes["include"]?.Value?.ToString() ?? "";
        if (!string.IsNullOrEmpty(include))
        {
            MarkdownFileBase? doc = null;
            if (include.EndsWith(".md"))
            {
                var includes = HostContext.TryResolve<MarkdownIncludes>();
                var pages = HostContext.TryResolve<MarkdownPages>();
                // default relative path to _includes/
                include = include[0] != '/'
                    ? "_includes/" + include
                    : include.TrimStart('/');
                
                doc = includes.Pages.FirstOrDefault(x => x.Path == include);
                if (doc == null && pages != null)
                {
                    var prefix = include.LeftPart('/');
                    var slug = include.LeftPart('.');
                    var allIncludes = pages.GetVisiblePages(prefix, allDirectories: true);
                    doc = allIncludes.FirstOrDefault(x => x.Slug == slug);
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
            }
            else
            {
                renderer.WriteLine($"Could not find: {include}");
            }
        }
        else
        {
            var document = Markdown.Parse(content ?? "", pipeline);
            renderer.Render(document);
        }

        await writer.WriteAsync("</div>");
        await writer.FlushAsync();
        var html = writer.ToString();

        output.Content.SetHtmlContent(html ?? "");
    }

    private async Task<string?> GetContent(TagHelperOutput output)
    {
        if (Content == null)
            return (await output.GetChildContentAsync()).GetContent();

        return Content.Model?.ToString();
    }
}