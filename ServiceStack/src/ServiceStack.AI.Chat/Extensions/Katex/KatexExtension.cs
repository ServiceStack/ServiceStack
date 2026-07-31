namespace ServiceStack.AI;

/// <summary>LaTeX rendering (port of extensions/katex): UI-only importmap + stylesheet</summary>
public class KatexExtension() : ChatExtension("katex")
{
    public override void Install(ExtensionContext ctx)
    {
        ctx.AddImportMaps(new() { ["katex"] = $"{ctx.ExtPrefix}/katex.min.mjs" });
        ctx.AddIndexFooter($"""<link rel="stylesheet" href="{ctx.ExtPrefix}/katex.min.css">""");
    }
}
