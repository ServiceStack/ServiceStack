# Rendering PDFs in your App

`PdfFeature` renders the typst templates published to `App_Data/pdf`. This guide covers the two things
Apps do with them: **return a rendered PDF from an API**, and **attach one to an email**.

Designing and publishing templates is the PDF Studio's job — see the designer docs for that. This
document starts from a template that has already been published.

## Prerequisites

Install the [typst](https://github.com/typst/typst) compiler and put it on your `PATH`
(`cargo install typst-cli`, `brew install typst`, `winget install typst`), then register the plugin:

```csharp
services.AddPlugin(new PdfFeature());
```

Without typst, templates can still be browsed and unpublished — only rendering is disabled
(`PdfFeature.IsAvailable` is `false`). `PdfFeature` works standalone: it has no dependency on
`ChatFeature`, which is only needed to *publish* templates from the designer.

## The workflow

| Step | Where |
| --- | --- |
| 1. Design the template | PDF Studio, `/chat/pdf` |
| 2. Publish it to `App_Data/pdf` | the designer's **Publish** action |
| 3. Generate a C# data model from it | Admin UI, `/admin-ui/pdf` → **Code** |
| 4. Populate the model and render | your App's code |

Your App never builds the template's JSON by hand. Step 3 gives you classes that produce exactly the
shape the template reads.

## Generating the data model

Open **`/admin-ui/pdf`**, select a template, and switch the left pane from **Data** to **Code**. You get
C# classes generated from the template's `.ui.json` schema, falling back to its `.json` example when no
schema was published, decorated for you:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ServiceStack.AI;

namespace MyApp.ServiceModel.Pdf;

[Pdf("invoice")]                          // binds this model to App_Data/pdf/invoice.typ
public class Invoice
{
    [JsonPropertyName("invoice")]
    public InvoiceInfo InvoiceValue { get; set; } = null!;

    [JsonPropertyName("items")]
    public List<Item> Items { get; set; } = new();
    // ...
}
```

**Copy** puts it on the clipboard, for when you'd rather paste it into a model you already own. Nothing
in the Admin UI writes to your project — generating into the source tree is a task you run, in the same
spirit as OrmLite Migrations.

The Code view has a tab per scenario, all generated from the same template so the type names, members
and template name are already this template's:

| Tab | What you get |
| --- | --- |
| **PDF Data Models** | the classes above — the only tab the `pdf` AppTask writes |
| **Rendering a PDF API** | a Service + Request DTO that returns the PDF as a download (use case 1 below) |
| **Sending an Email** | a `[Worker]` Command that renders it and attaches it to your mail (use case 2) |

The two example tabs are stubs to paste into your `ServiceInterface` project, not generated files. Both
include the full object initialiser for the model, with every member the template reads spelled out —
which is the point, since an unset member is omitted from the JSON entirely and typst fails on the
missing key. Fill them in from your own data; that mapping is the part only you can write.

### The `pdf` AppTask

Configure it on the plugin, register the task once, then run it whenever a template changes:

```csharp
services.AddPlugin(new PdfFeature {
    PdfCodeGen = new() {
        Namespace = "MyApp.ServiceModel.Pdf",
        OutputPath = Path.Combine(contentRootPath, "../MyApp.ServiceModel/Pdf"),
    }
});
```

```csharp
AppTasks.Register("pdf", _ => appHost.GetPlugin<PdfFeature>().GeneratePdfs());
```

```bash
$ dotnet run --AppTasks=pdf
```

It walks every template published to `App_Data/pdf` and writes one `.cs` per template, using the same
generator — and the same `PdfCodeGen` config — the Admin UI's Code view shows, so what you read there is
what lands in the file.

| Option | Default | Purpose |
| --- | --- | --- |
| `Namespace` | `PdfFeature.ModelsNamespace` | namespace the models are emitted into |
| `OutputPath` | `PdfFeature.ModelsPath` | folder they're written to, created if missing |
| `Include` | all published | only generate these templates |
| `Exclude` | none | templates to leave alone |
| `ResolveFileName` | `Invoice.cs` | names the file a template generates into |
| `PreserveModified` | `true` | never overwrite a generated file you've edited |
| `Usings` | none | extra usings to emit |
| `Filter` | none | last say over each file: edit its source, or `Skip` it |

Both paths default to a **`Pdf` subfolder of your App's ServiceModel** — `MyApp.ServiceModel/Pdf`, in
the `MyApp.ServiceModel.Pdf` namespace. Generated names come from the document's own keys and are
generic enough (`Item`, `From`, `Details`) to collide with your App's types, so they get their own
folder and namespace rather than sitting alongside them.

**Your edits survive.** Each generated file carries a header hashing everything below it, so the next
run can tell a file it wrote from one you've since taken over — an edited file is skipped and reported,
never clobbered. `Exclude` says the same thing up front, for a model you've adopted wholesale:

```csharp
PdfCodeGen = new() {
    Exclude = ["invoice"],       // hand-tuned, don't regenerate it
    PreserveModified = false,    // and overwrite everything else, edits and all
}
```

> Note: `[Pdf]` is metadata, nothing more. It doesn't create an API, change content negotiation, or
> hook the request pipeline — it only records which template the model renders with, so the template
> name isn't repeated at every call site.

## Use case 1: returning a PDF from an API

Write your own API. Load what you need, map it onto the PDF model, and render:

```csharp
using ServiceStack;
using ServiceStack.AI;
using MyApp.ServiceModel.Pdf;

namespace MyApp.ServiceInterface;

public class InvoiceServices(IPdfRenderer pdf) : Service
{
    public async Task<object> Any(GetUserInvoicePdf request)
    {
        // 1. your own data, in your own shape
        var order = await Db.LoadSingleByIdAsync<Order>(request.OrderId);

        // 2. map it onto the PDF model - this part is yours to write, since only you know
        //    how your tables relate to the document
        var invoice = new Invoice
        {
            InvoiceValue = new InvoiceInfo
            {
                Number = order.InvoiceNo,
                Date = order.OrderDate.ToString("d MMMM yyyy"),
                Currency = "$",
            },
            From = new From { Name = "Acme Pty Ltd", Lines = [..] },
            To = new From { Name = order.ShipName, Lines = [..] },
            Items = order.Details.Map(x => new Item {
                Description = x.ProductName,
                Qty = x.Quantity,
                UnitPrice = x.UnitPrice.ToString("N2"),
            }),
            // ...
        };

        // 3. render it to a result the client downloads as a file
        return await pdf.PdfResultAsync(invoice, $"Invoice-{order.InvoiceNo}.pdf");
    }
}
```

`PdfResultAsync` returns an `HttpResult` with `Content-Type: application/pdf` and the
`Content-Disposition` browsers need to name the download:

```
HTTP/1.1 200 OK
Content-Type: application/pdf
Content-Disposition: attachment; filename="Invoice-INV-2026-042.pdf"
```

Pass `inline: true` to display it in the browser instead of downloading it. Omit the file name to use
the attribute's `FileName`, which defaults to `{Template}.pdf`.

## Use case 2: attaching a PDF to an email

`RenderPdfAsync` gives you the bytes. Nothing else changes about how your App sends mail — add an
attachment to the `SendEmail` DTO and command your App already owns
(`ServiceInterface/EmailServices.cs` in the ServiceStack templates):

```csharp
public class SendEmail
{
    public string To { get; set; }
    public string? ToName { get; set; }
    public string Subject { get; set; }
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
    public int? InvoiceId { get; set; }        // what to render, not the rendered bytes
}
```

```csharp
[Worker("smtp")]
public class SendEmailCommand(ILogger<SendEmailCommand> logger, IBackgroundJobs jobs,
    SmtpConfig config, IDbConnectionFactory dbFactory, IPdfRenderer pdf)
    : AsyncCommand<SendEmail>
{
    protected override async Task RunAsync(SendEmail request, CancellationToken token)
    {
        using var client = new SmtpClient(config.Host, config.Port) { /* ... */ };
        var msg = new MailMessage(/* ... */);

        if (request.InvoiceId != null)
        {
            using var db = await dbFactory.OpenAsync(token);
            var invoice = await db.LoadInvoiceAsync(request.InvoiceId.Value);
            var bytes = await pdf.RenderPdfAsync(invoice, token);
            msg.Attachments.Add(new Attachment(
                new MemoryStream(bytes), "invoice.pdf", MimeTypes.Pdf));
        }

        await client.SendMailAsync(msg, token);
    }
}
```

Then enqueue it as usual:

```csharp
jobs.EnqueueCommand<SendEmailCommand>(new SendEmail {
    To = customer.Email,
    Subject = $"Invoice {invoice.Number}",
    BodyText = "Your invoice is attached.",
    InvoiceId = invoice.Id,
});
```

> Note: render **inside the job**, not in the API that enqueues it. Every render forks a typst
> process, so doing it on the request thread makes your API as slow as the compile. Enqueueing the
> invoice *id* rather than the rendered bytes also keeps the job payload small and lets a retry pick
> up corrected data.

## Reference

### `[Pdf]`

| Member | Purpose |
| --- | --- |
| `Template` | Published template name, without the `.typ` (e.g. `"invoice"`) |
| `FileName` | Default download name, defaults to `{Template}.pdf` |

### Extension methods on `IPdfRenderer`

| Method | Returns |
| --- | --- |
| `RenderPdfAsync(model, token)` | `byte[]` — for emails, storage, or your own result |
| `PdfResultAsync(model, fileName, inline, token)` | `HttpResult` a Service can return directly |

Both read the template name from the model's `[Pdf]`. A model without one throws an `ArgumentException`
naming the type and the attribute to add.

### `IPdfRenderer`

`RenderAsync(name, data)` renders by template name when you don't have a decorated model — the same
call the two extensions above delegate to. `GetTemplateNames()` lists what's published, and
`RenderPngAsync` rasterises a page.

### `PdfFeature`

| Property | Default | Purpose |
| --- | --- | --- |
| `PdfPath` | `~/App_Data/pdf` | where published templates live |
| `TypstPath` | `$TYPST_PATH` or `PATH` | the typst binary |
| `RenderTimeout` | 30s | before a compile is killed |
| `MaxConcurrentRenders` | `ProcessorCount` | bounds forked typst processes |
| `MaxDataBytes` | 64KB | largest data payload — see below |
| `ModelsPath` | `<ServiceModel>/Pdf` | where the `pdf` AppTask generates models |
| `ModelsNamespace` | `<ServiceModel namespace>.Pdf` | namespace those models are emitted into |

## Gotchas

**Unset members disappear rather than becoming null.** The renderer serialises with
`DefaultIgnoreCondition = WhenWritingNull`, so a member you didn't populate is *omitted from the JSON
entirely* and typst fails with `dictionary does not contain key "paymentBank"`. Populate every member
the template reads, and treat that error as "you left something null".

**Your generated model only covers what the schema declares.** If a template's `.ui.json` has drifted
from its `.typ`, the generated model will be missing fields and renders will fail on the first one
typst reaches. When in doubt, compare against the template's `.json` example — the Code view warns
when a template has no schema at all.

**Generated class names are generic.** Names like `Item`, `From` and `Details` come straight from the
data's own keys, which is why models default into their own `Pdf` folder and namespace. Note that a
type in the *enclosing* namespace still wins over a `using`-imported one, so a
`MyApp.ServiceInterface.Item` shadows `MyApp.ServiceModel.Pdf.Item` and needs qualifying.

**`format: "date"` generates `DateTime`.** System.Text.Json writes that as `2026-09-05T00:00:00`, so a
template printing the value raw shows the time component too. Use a `string` for dates you want
formatted for display, and keep `DateTime` for values the template parses itself.

**Templates can require more than their schema implies.** A template doing `data.from.lines.at(3)`
needs at least four lines, and typst reports `array index out of bounds`. Nothing in the schema
expresses that, so check the template when an array-shaped render fails.

**Data rides typst's argv**, which caps `MaxDataBytes` at 64KB. A document with a very large
collection can exceed it and throws an `ArgumentException` before the compile starts.

**Compile errors carry typst's own diagnostics.** `PdfRenderException.Diagnostics` holds the full
stderr and the message is the first line of it, which is usually enough to act on:

```
invoice.typ:71:13: error: dictionary does not contain key "unitPrice"
```
