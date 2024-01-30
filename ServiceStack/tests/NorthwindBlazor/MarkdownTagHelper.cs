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
            if (include.EndsWith(".md"))
            {
                var markdown = HostContext.Resolve<MarkdownPages>();
                // default relative path to _includes/
                include = include[0] != '/'
                    ? "_includes/" + include
                    : include.TrimStart('/');
                var prefix = include.LeftPart('/');
                var slug = include.LeftPart('.');
                var allIncludes = markdown.GetVisiblePages(prefix, allDirectories: true);
                var doc = allIncludes.FirstOrDefault(x => x.Slug == slug);
                if (doc != null)
                {
                    renderer.WriteLine(doc.Preview!);
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