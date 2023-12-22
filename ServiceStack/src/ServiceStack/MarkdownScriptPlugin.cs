using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack;

[Obsolete("Use MarkdownScriptPlugin")]
public class MarkdownTemplatePlugin : MarkdownScriptPlugin {} 
    
public class MarkdownScriptPlugin : IScriptPlugin
{
    public bool RegisterPageFormat { get; set; } = true;

    public void Register(ScriptContext context)
    {
        if (RegisterPageFormat)
            context.PageFormats.Add(new MarkdownPageFormat());
            
        context.FilterTransformers["markdown"] = MarkdownPageFormat.TransformToHtml;
            
        context.ScriptMethods.Add(new MarkdownScriptMethods());

        context.ScriptBlocks.Add(new MarkdownScriptBlock());
    }
}
    
[Obsolete("Use MarkdownScriptMethods")]
public class MarkdownTemplateFilter : MarkdownScriptMethods {}
    
public class MarkdownScriptMethods : ScriptMethods
{
    public IRawString markdown(string markdown) => markdown != null 
        ? MarkdownConfig.Transform(markdown).ToRawString() 
        : RawString.Empty;
}
    
[Obsolete("Use MarkdownScriptBlock")]
public class TemplateMarkdownBlock : MarkdownScriptBlock {}

/// <summary>
/// Converts markdown contents to HTML using the configured MarkdownConfig.Transformer.
/// If a variable name is specified the HTML output is captured and saved instead. 
///
/// Usages: {{#markdown}} ## The Heading {{/markdown}}
///         {{#markdown content}} ## The Heading {{/markdown}} HTML: {{content}}
/// </summary>
public class MarkdownScriptBlock : ScriptBlock
{
    public override string Name => "markdown";
    public override ScriptLanguage Body => ScriptVerbatim.Language;

    public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
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

    private static void Capture(ScriptScopeContext scope, PageBlockFragment block, PageStringFragment strFragment)
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