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

    public class TemplatMarkdownBlock : TemplateBlock
    {
        public override string Name => "markdown";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            var strFragment = (PageStringFragment)fragment.Body[0];

            if (!string.IsNullOrWhiteSpace(fragment.Argument))
            {
                Capture(scope, fragment, strFragment);
            }
            else
            {
                await scope.OutputStream.WriteAsync(MarkdownConfig.Transform(strFragment.Value.Value), cancel);
            }
        }

        private static void Capture(TemplateScopeContext scope, PageBlockFragment fragment, PageStringFragment strFragment)
        {
            var literal = fragment.Argument.AsSpan().AdvancePastWhitespace();

            literal = literal.ParseVarName(out var name);
            var nameString = name.Value();
            scope.PageResult.Args[nameString] = MarkdownConfig.Transform(strFragment.Value.Value).ToRawString();
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