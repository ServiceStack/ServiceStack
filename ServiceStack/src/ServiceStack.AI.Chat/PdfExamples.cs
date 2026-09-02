namespace ServiceStack.AI;

/// <summary>A copy/paste stub for one way of using a template's generated model</summary>
public class PdfCodeGenExample
{
    /// <summary>Stable id the UI keys its tab on, e.g. "api"</summary>
    public string Id { get; set; } = null!;

    /// <summary>Tab label, e.g. "Rendering a PDF API"</summary>
    public string Label { get; set; } = null!;

    /// <summary>One line under the tabs saying where this belongs</summary>
    public string? Notes { get; set; }

    public string Source { get; set; } = null!;
}

/// <summary>
/// Worked examples for a template's generated model: an API that returns the PDF, and a command that
/// emails it. Generated rather than written down so the type names, members and template name are this
/// template's, which is the difference between a snippet you read and one you paste.
/// <para>
/// Stubs only — nothing here is written to your project. The <c>pdf</c> StartupTask generates the data models
/// these build on; what to do with them is yours.
/// </para>
/// </summary>
public static class PdfExamples
{
    /// <summary>Both examples for a generated model, or none when its root isn't a class</summary>
    public static List<PdfCodeGenExample> Create(PdfCodeGenFile file, string? modelsNamespace)
    {
        // a template whose document is an array or a scalar has no model to populate, so there's nothing
        // worth showing beyond the types themselves
        if (file.TypeName == null)
            return [];

        var ctx = new Context(file, modelsNamespace);
        return [ApiExample(ctx), EmailExample(ctx)];
    }

    /// <summary>The names every example derives from one template</summary>
    class Context(PdfCodeGenFile file, string? modelsNamespace)
    {
        public readonly string Template = file.Template;
        public readonly string Type = file.TypeName!;
        /// <summary>Local the mapped model is assigned to, e.g. "invoice"</summary>
        public readonly string Var = JsonTypes.Camel(file.TypeName);
        public readonly string? ModelsNamespace = modelsNamespace;
        public readonly string? Namespace = ServiceNamespace(modelsNamespace);

        /// <summary>The model initialiser, indented to sit in a method body</summary>
        public string Initializer(string indent) => JsonTypes.ToCSharpInitializer(
            file.Model, file.Model.Root, indent);
    }

    /// <summary>
    /// Where a ServiceStack App puts its Services, inferred from where its PDF models go:
    /// MyApp.ServiceModel.Pdf → MyApp.ServiceInterface. Null when there's nothing to infer from, in which
    /// case the example simply omits its namespace.
    /// </summary>
    static string? ServiceNamespace(string? modelsNamespace)
    {
        if (string.IsNullOrEmpty(modelsNamespace))
            return null;

        var ns = modelsNamespace!;
        foreach (var suffix in new[] { "." + PdfFeature.PdfModelsFolder, ".ServiceModel" })
        {
            if (ns.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                ns = ns[..^suffix.Length];
        }
        return ns.Length > 0 ? ns + ".ServiceInterface" : null;
    }

    static string Header(Context ctx, params string[] usings)
    {
        var lines = new List<string>(usings);
        if (ctx.ModelsNamespace != null && !lines.Contains(ctx.ModelsNamespace))
            lines.Add(ctx.ModelsNamespace);

        var sb = new System.Text.StringBuilder();
        lines.Each(x => sb.Append("using ").Append(x).AppendLine(";"));
        if (ctx.Namespace != null)
            sb.AppendLine().Append("namespace ").Append(ctx.Namespace).AppendLine(";");
        return sb.ToString();
    }

    // ── Returning a PDF from an API ──

    static PdfCodeGenExample ApiExample(Context ctx) => new()
    {
        Id = "api",
        Label = "Rendering a PDF API",
        Notes = "Your own API, in your ServiceInterface project",
        Source = Header(ctx, "ServiceStack", "ServiceStack.AI") + $$"""

            [Route("/{{ctx.Template}}/{Id}/pdf")]
            public class Get{{ctx.Type}}Pdf : IGet, IReturn<byte[]>
            {
                public int Id { get; set; }
            }

            public class {{ctx.Type}}PdfServices(IPdfRenderer pdf) : Service
            {
                public async Task<object> Any(Get{{ctx.Type}}Pdf request)
                {
                    // 1. Load your own data, in your own shape
                    // var order = await Db.LoadSingleByIdAsync<Order>(request.Id);

                    // 2. Map it onto the PDF model. Only you know how your tables relate to the document,
                    //    so this part is yours to write — but populate every member: one you leave unset is
                    //    omitted from the JSON entirely and typst fails on the missing key.
                    var {{ctx.Var}} = {{ctx.Initializer("        ")}};

                    // 3. Return it as a download. [Pdf("{{ctx.Template}}")] on {{ctx.Type}} picks the
                    //    template, so the name isn't repeated here. inline:true shows it in the browser.
                    return await pdf.PdfResultAsync({{ctx.Var}}, $"{{ctx.Template}}-{request.Id}.pdf");
                }
            }

            """,
    };

    // ── Attaching a PDF to an email ──

    static PdfCodeGenExample EmailExample(Context ctx) => new()
    {
        Id = "email",
        Label = "Sending an Email",
        Notes = "Rendering a PDF returns bytes - how your App sends mail doesn't change. "
            + "Example using SmtpConfig from ServiceInterface/EmailServices.cs.",
        Source = Header(ctx, "System.Net", "System.Net.Mail", "ServiceStack", "ServiceStack.AI",
            "ServiceStack.Data", "ServiceStack.OrmLite") + $$"""

            /// <summary>
            /// What to render, never the rendered bytes: a job argument is persisted, so keep it to the id
            /// the command can load the document from.
            /// </summary>
            public class Send{{ctx.Type}}Email
            {
                public string To { get; set; } = null!;
                public string? ToName { get; set; }
                public string Subject { get; set; } = null!;
                public string? BodyText { get; set; }
                public int {{ctx.Type}}Id { get; set; }
            }

            [Worker("smtp")]
            public class Send{{ctx.Type}}EmailCommand(IPdfRenderer pdf, SmtpConfig config,
                IDbConnectionFactory dbFactory) : AsyncCommand<Send{{ctx.Type}}Email>
            {
                protected override async Task RunAsync(Send{{ctx.Type}}Email request, CancellationToken token)
                {
                    // 1. Load your own data, in your own shape
                    // using var db = await dbFactory.OpenAsync(token: token);
                    // var order = await db.LoadSingleByIdAsync<Order>(request.{{ctx.Type}}Id, token: token);

                    // 2. Map it onto the PDF model, exactly as the API example does
                    var {{ctx.Var}} = {{ctx.Initializer("        ")}};

                    // 3. RenderPdfAsync returns the bytes; [Pdf("{{ctx.Template}}")] picks the template
                    var pdfBytes = await pdf.RenderPdfAsync({{ctx.Var}}, token);

                    // 4. Attach them to the mail your App already sends
                    using var client = new SmtpClient(config.Host, config.Port)
                    {
                        Credentials = new NetworkCredential(config.UserName, config.Password),
                        EnableSsl = true,
                    };
                    using var msg = new MailMessage(config.FromEmail, request.To)
                    {
                        Subject = request.Subject,
                        Body = request.BodyText,
                    };
                    msg.Attachments.Add(new Attachment(
                        new MemoryStream(pdfBytes), "{{ctx.Template}}.pdf", MimeTypes.Pdf));

                    await client.SendMailAsync(msg, token);
                }
            }

            /* Queue it from an API or Command with IBackgroundJobs:

                jobs.EnqueueCommand<Send{{ctx.Type}}EmailCommand>(new Send{{ctx.Type}}Email {
                    To = order.Email,
                    Subject = "Your {{ctx.Template}}",
                    {{ctx.Type}}Id = order.Id,
                });
            */

            """,
    };
}
