namespace ServiceStack.AI;

/// <summary>
/// Binds a PDF data model to the published template in App_Data/pdf that renders it, so the template
/// name lives on the model instead of being repeated as a magic string at every call site.
/// <para>
/// Applied to the classes the Admin UI generates from a template's .ui.json schema, then read by
/// <see cref="PdfRendererExtensions.RenderPdfAsync{T}"/> and
/// <see cref="PdfRendererExtensions.PdfResultAsync{T}"/>.
/// </para>
/// </summary>
/// <example><code>
/// [Pdf("invoice")]                    // renders with App_Data/pdf/invoice.typ
/// public class Invoice { /* ... */ }
/// </code></example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PdfAttribute(string template) : AttributeBase
{
    /// <summary>Published template name, without the .typ (e.g. "invoice")</summary>
    public string Template { get; set; } = template;

    /// <summary>Download name for <see cref="PdfRendererExtensions.PdfResultAsync{T}"/>, default "{Template}.pdf"</summary>
    public string? FileName { get; set; }

    /// <summary>The download name to use when the caller doesn't supply one</summary>
    public string ResolveFileName() => !string.IsNullOrEmpty(FileName)
        ? FileName!
        : Template + ".pdf";
}
