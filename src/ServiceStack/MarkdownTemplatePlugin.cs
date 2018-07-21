using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack
{
    public class MarkdownTemplatePlugin : ITemplatePlugin
    {
        public bool RegisterPageFormat { get; set; } = true;

        public void Register(TemplateContext context)
        {
            if (RegisterPageFormat)
                context.PageFormats.Add(new MarkdownPageFormat());
            
            context.FilterTransformers["markdown"] = MarkdownPageFormat.TransformToHtml;
            
            context.TemplateFilters.Add(new MarkdownTemplateFilter());

            TemplateConfig.DontEvaluateBlocksNamed.Add("markdown");
            
            context.TemplateBlocks.Add(new TemplatMarkdownBlock());
        }
    }
    
    public class MarkdownTemplateFilter : TemplateFilter
    {
        public IRawString markdown(string markdown) => markdown != null 
            ? MarkdownConfig.Transform(markdown).ToRawString() 
            : RawString.Empty;
    }

    /// <summary>
    /// Converts markdown contents to HTML using the configured MarkdownConfig.Transformer.
    /// If a variable name is specified the HTML output is captured and saved instead. 
    ///
    /// Usages: {{#markdown}} ## The Heading {{/markdown}}
    ///         {{#markdown content}} ## The Heading {{/markdown}} HTML: {{content}}
    /// </summary>
    public class TemplatMarkdownBlock : TemplateBlock
    {
        public override string Name => "markdown";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var strFragment = (PageStringFragment)block.Body[0];

            if (!block.Argument.IsNullOrWhiteSpace())
            {
                Capture(scope, block, strFragment);
            }
            else
            {
                await scope.OutputStream.WriteAsync(MarkdownConfig.Transform(strFragment.ValueString), token);
            }
        }

        private static void Capture(TemplateScopeContext scope, PageBlockFragment block, PageStringFragment strFragment)
        {
            var literal = block.Argument.AdvancePastWhitespace();

            literal = literal.ParseVarName(out var name);
            var nameString = name.ToString();
            scope.PageResult.Args[nameString] = MarkdownConfig.Transform(strFragment.ValueString).ToRawString();
        }
    }

    public class MarkdownPageFormat : PageFormat
    {
        public MarkdownPageFormat()
        {
            Extension = "md";
            ContentType = MimeTypes.MarkdownText;
        }

        public static async Task<Stream> TransformToHtml(Stream markdownStream)
        {
            var md = await markdownStream.ReadToEndAsync();
            var html = MarkdownConfig.Transformer.Transform(md);
            return MemoryStreamFactory.GetStream(html.ToUtf8Bytes());
        }
    }
}