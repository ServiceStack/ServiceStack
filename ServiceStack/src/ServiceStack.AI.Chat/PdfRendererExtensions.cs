using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Renders <see cref="PdfAttribute"/>-decorated data models, so App code never repeats a template name.
/// <para>
/// These are extension methods because <see cref="IPdfRenderer"/> intentionally accepts only JSON while
/// this typed convenience layer owns model serialization and [Pdf] template discovery.
/// </para>
/// </summary>
public static class PdfRendererExtensions
{
    /// <summary>
    /// The <see cref="PdfAttribute"/> a model renders with. Looks at the runtime type first so a model
    /// passed as <c>object</c> still resolves, since <see cref="PdfAttribute"/> isn't inherited.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown with the fix, since a missing [Pdf] is the likely first error</exception>
    public static PdfAttribute AssertPdfAttribute(this Type modelType)
    {
        if (modelType == null)
            throw new ArgumentNullException(nameof(modelType));

        var attr = modelType.GetCustomAttribute<PdfAttribute>(inherit: false);
        if (attr == null)
            throw new ArgumentException(
                $"{modelType.Name} has no [Pdf] attribute naming the template it renders with. " +
                $"Add [Pdf(\"<template>\")] to {modelType.Name}, where <template> is a published " +
                $"template in App_Data/pdf (e.g. [Pdf(\"invoice\")] for invoice.typ).");

        if (string.IsNullOrEmpty(attr.Template))
            throw new ArgumentException($"[Pdf] on {modelType.Name} has an empty template name");

        return attr;
    }

    /// <summary>The [Pdf] of a model's runtime type, falling back to the type it was declared as</summary>
    static PdfAttribute AssertPdfAttribute<T>(T model)
    {
        var runtimeType = model?.GetType();
        if (runtimeType != null && runtimeType != typeof(T) &&
            runtimeType.GetCustomAttribute<PdfAttribute>(inherit: false) != null)
            return runtimeType.AssertPdfAttribute();

        return (runtimeType ?? typeof(T)).AssertPdfAttribute();
    }

    /// <summary>
    /// Render a [Pdf]-decorated model to PDF bytes, e.g. to attach to an email.
    /// The model is serialised to the camelCase JSON the template reads through <c>--input data=</c>.
    /// </summary>
    /// <exception cref="PdfRenderException">typst is missing, or the template failed to compile</exception>
    public static Task<byte[]> RenderPdfAsync<T>(this IPdfRenderer renderer, T model,
        CancellationToken token = default)
        => RenderPdfAsync(renderer, model, options: null, token);

    /// <summary>Render a [Pdf]-decorated model with optional template-defined rendering context.</summary>
    public static Task<byte[]> RenderPdfAsync<T>(this IPdfRenderer renderer, T model,
        PdfRenderOptions? options, CancellationToken token = default)
    {
        if (renderer == null)
            throw new ArgumentNullException(nameof(renderer));

        var attr = AssertPdfAttribute(model);
        return renderer.RenderAsync(attr.Template, model != null ? ChatJson.Serialize(model) : null, options, token);
    }

    /// <summary>Render a [Pdf]-decorated model directly into a writable stream.</summary>
    public static Task RenderPdfAsync<T>(this IPdfRenderer renderer, T model, Stream output,
        PdfRenderOptions? options = null, CancellationToken token = default)
    {
        if (renderer == null) throw new ArgumentNullException(nameof(renderer));
        if (output == null) throw new ArgumentNullException(nameof(output));
        var attr = AssertPdfAttribute(model);
        return renderer.RenderToStreamAsync(attr.Template, output, model != null ? ChatJson.Serialize(model) : null,
            options, token);
    }

    /// <summary>
    /// Render a [Pdf]-decorated model to a result a Service can return directly, with the
    /// Content-Disposition browsers need to name the download.
    /// </summary>
    /// <param name="fileName">Overrides the attribute's FileName, which defaults to "{Template}.pdf"</param>
    /// <param name="inline">True to display in the browser rather than download it</param>
    public static async Task<HttpResult> PdfResultAsync<T>(this IPdfRenderer renderer, T model,
        string? fileName = null, bool inline = false, CancellationToken token = default)
        => await PdfResultAsync(renderer, model, options: null, fileName, inline, token).ConfigAwait();

    /// <summary>Render a [Pdf]-decorated model as an HTTP result with optional rendering context.</summary>
    public static async Task<HttpResult> PdfResultAsync<T>(this IPdfRenderer renderer, T model,
        PdfRenderOptions? options, string? fileName = null, bool inline = false,
        CancellationToken token = default)
    {
        if (renderer == null)
            throw new ArgumentNullException(nameof(renderer));

        var attr = AssertPdfAttribute(model);
        var pdf = await renderer.RenderAsync(attr.Template, model != null ? ChatJson.Serialize(model) : null,
            options, token).ConfigAwait();

        var disposition = inline ? "inline" : "attachment";
        var name = !string.IsNullOrEmpty(fileName) ? fileName! : attr.ResolveFileName();

        return new HttpResult(pdf, MimeTypes.Pdf)
        {
            Headers =
            {
                [HttpHeaders.ContentDisposition] = $"{disposition}; {HttpExt.GetDispositionFileName(name)}",
            },
        };
    }
}
