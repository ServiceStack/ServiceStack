using Markdig;
using Markdig.Syntax;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Client;

public class MarkdownFileInfo
{
    public string? Path { get; set; }
    public string? FileName { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public DateTime? Date { get; set; }
    public string? Content { get; set; }
    public string? Preview { get; set; }
}

public static class MarkdownUtils
{
    public static Dictionary<string, MarkdownFileInfo> Cache = new();

    public static async Task<ApiResult<MarkdownFileInfo>> LoadDocumentAsync(string name, Func<MarkdownFileInfo, Task<string>> resolverAsync)
    {
        try
        {
            if (Cache.TryGetValue(name, out var cachedDoc))
                return ApiResult.Create(cachedDoc);

            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .Build();
            var writer = new System.IO.StringWriter();
            var renderer = new Markdig.Renderers.HtmlRenderer(writer);
            pipeline.Setup(renderer);

            var doc = new MarkdownFileInfo
            {
                Path = name,
                FileName = $"{name}.md",
            };
            doc.Content = await resolverAsync(doc);

            var document = Markdown.Parse(doc.Content!, pipeline);
            renderer.Render(document);
            var block = document
                .Descendants<Markdig.Extensions.Yaml.YamlFrontMatterBlock>()
                .FirstOrDefault();

            var metaObj = block?
                .Lines // StringLineGroup[]
                .Lines // StringLine[]
                .Select(x => $"{x}\n")
                .ToList()
                .Select(x => x.Replace("---", string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => KeyValuePairs.Create(x.LeftPart(':').Trim(), x.RightPart(':').Trim()))
                .ToObjectDictionary();
            metaObj?.PopulateInstance(doc);

            await writer.FlushAsync();
            doc.Preview = writer.ToString();
            Cache[name] = doc;
            return ApiResult.Create(doc);
        }
        catch (Exception ex)
        {
            return ApiResult.CreateError<MarkdownFileInfo>(ex.AsResponseStatus());
        }
    }

}