# PDF Templates: design with AI, render from your App

Every App eventually needs to produce a real PDF — an invoice, a quote, a statement, a packing slip, a
certificate. The usual options are all unpleasant: HTML-to-PDF that renders differently in every headless
browser, a reporting designer that only runs on Windows, or a templating library whose layout model is a
stack of tables you maintain by hand.

`ServiceStack.AI.Chat` takes a different route. Documents are [typst](https://typst.app) templates —
plain text, versionable, and compiled by a single fast binary — and you **don't write them by hand**.
You describe the document you want (or paste a screenshot of one), the AI writes the typst, and you watch
the PDF update live as it does. When it looks right you **publish** it, and your App gets a **typed C#
model** generated from it, so populating a document is `new Invoice { ... }` rather than hand-building JSON
and hoping the keys match.

![The three surfaces: PDF Studio, Admin UI, your App](img/pdf/hero-overview.png)

> 📸 **`hero-overview.png`** — a three-panel banner: PDF Studio with a rendered invoice, the Admin UI
> gallery of published templates, and a snippet of C# calling `PdfResultAsync`. This is the doc's hero
> image, so favour a clean wide crop over detail.

## How it fits together

| Step | Where | What you get |
| --- | --- | --- |
| 1. Design the template | **PDF Studio** — `/chat/pdf` | a `.typ` template + its `.json` example data |
| 2. Publish it | the Studio's **Publish** button | a flat, self-contained copy in `App_Data/pdf` |
| 3. Generate a data model | **Admin UI** — `/admin-ui/pdf` → **Code** | a typed C# class bound to the template |
| 4. Render it | your App's code | PDF bytes, or an `HttpResult` a Service returns |

Steps 1 and 2 are dev-time and interactive. Steps 3 and 4 are what ships: at runtime your App only ever
touches `App_Data/pdf` and `IPdfRenderer`, with no dependency on the Chat UI or on any AI provider.

## Two plugins, deliberately separate

| Plugin | Provides | Needs |
| --- | --- | --- |
| `ChatFeature` (the `pdf` extension) | PDF Studio: authoring, AI editing, live preview | an LLM provider, typst |
| `PdfFeature` | rendering published templates, the Admin UI, code generation | typst |

`PdfFeature` stands alone. You can design templates on a dev machine with `ChatFeature` installed and
deploy an App that has only `PdfFeature` — production never needs the designer, an API key, or a model.

### Prerequisites

Install the typst compiler and put it on `PATH`:

```bash
cargo install typst-cli    # or: brew install typst / winget install typst
```

Then register the plugin:

```csharp
services.AddPlugin(new PdfFeature());
```

Without typst, templates can still be browsed and unpublished — only rendering is disabled and
`PdfFeature.IsAvailable` is `false`. The Studio disables itself entirely.

---

# Part 1 — Design a template with AI

Open **PDF Studio** at `/chat/pdf`. Your first visit seeds your workspace with a worked `invoice` example
(`invoice.typ`, `invoice.json`, `invoice.ui.json`) plus the shared `lib.typ`, so there's something
compiling in front of you before you type anything.

![PDF Studio with the invoice example open](img/pdf/studio-overview.png)

> 📸 **`studio-overview.png`** — the full Studio: file explorer on the left, `invoice.typ` in the editor,
> rendered invoice in the preview pane, AI prompt box at the bottom. This is the "what am I looking at"
> shot, so keep every region visible even if the text is small.

Templates are stored **per user** under `App_Data/chat/user/<user>/pdf`, so everyone experiments in their
own workspace and nothing reaches your App until it's published.

## Ask for what you want

The prompt box takes plain English. The model gets the current template, the data files it reads, and any
partials it includes — then returns the complete updated files, which are compiled immediately. If the
compile fails, the model gets one shot at fixing its own output before you ever see it.

> *"Add a 'Paid' watermark across the page when the balance is zero"*
>
> *"Move the totals into a right-aligned box under the line items, and show tax as a separate row"*
>
> *"Make this look like a shipping label: 4x6 inches, big barcode area, no margins"*

![The AI prompt and its result, side by side](img/pdf/studio-ai-edit.png)

> 📸 **`studio-ai-edit.png`** — a before/after of one prompt. Capture the prompt text you typed and the
> changed preview; the diff in the editor is a bonus. Pick a visually obvious change (a watermark, a colour
> scheme) so the difference reads at thumbnail size.

### Build a template from a screenshot

The prompt accepts image attachments — up to 8 screenshots, photos or rasterised PDF pages. Paste in a
picture of the document you're replacing and ask for it back as a typst template. This is by far the
fastest way to start: an existing invoice becomes a working template in one round trip.

![Attaching a screenshot of an existing document](img/pdf/studio-attach-image.png)

> 📸 **`studio-attach-image.png`** — the prompt box with an attached image thumbnail and a prompt like
> "rebuild this as a typst template", next to the resulting render. Use a document you're happy to publish
> — no real customer data.

### Undo is a button, not a prayer

Every AI edit is applied to the buffers, not committed behind your back. **Restore the previous contents**
puts back what was there, and the editor keeps unsaved changes marked, so you can iterate without fear.

## Data lives next to the template

A template reads its data from a sibling JSON file:

```typst
#let data = json("invoice.json")
```

That file is both the example the preview renders with *and* the shape your C# model is generated from.
Keep it flat — top-level scalars, and arrays of flat objects where you need a list. The AI is instructed to
do the same, because flat data produces far better typed classes and forms.

### The `.ui.json` schema

Alongside `invoice.json` you can generate `invoice.ui.json` — a JSON Schema describing that data. It's
optional but worth having, because it's what turns a guess into a fact:

| Without a schema | With a schema |
| --- | --- |
| every string is `string` | `format: date` → `DateTime`, `format: uuid` → `Guid` |
| every number is `double` | `multipleOf: 0.01` → `decimal` |
| everything is nullable | `required` → non-nullable members |
| no documentation | `description` → XML doc comments |
| free-text fields | `enum` → a real C# enum |

The Studio generates it for you from the data, and rebuilds it on demand after you change the data's shape.

![Generating the .ui.json schema](img/pdf/studio-generate-schema.png)

> 📸 **`studio-generate-schema.png`** — the generate/rebuild schema action with `invoice.ui.json` open
> beside `invoice.json`, so the correspondence between a field and its schema entry is visible.

## The shared `lib.typ`

Every template imports `lib.typ`, which holds the styles and helpers your documents share — fonts, colours,
a `money()` formatter, header and footer blocks. Change it once and every template picks it up.
`lib.preview.typ` is a small document that renders the library itself, so you can see what's in it.

---

# Part 2 — Publish

When a template is ready, hit **Publish** in the Studio's preview toolbar. (The button only appears for
admins, on a real template — not on `lib.typ`.)

![The Publish button in the Studio toolbar](img/pdf/studio-publish.png)

> 📸 **`studio-publish.png`** — a tight crop of the preview toolbar showing **Publish** and the eye icon
> that opens the template in the Admin UI, with the "Published invoice" toast if you can catch it.

Publishing copies the template out of your personal workspace into the App's shared `App_Data/pdf` folder,
and does rather more than a file copy:

- **It follows the template's references.** Data files, `#include`d partials, `#image()` assets and
  `lib.typ` all come along, up to 8 levels deep.
- **It flattens them.** A template authored at `reports/quote.typ` is published as `quote.typ` with its
  companions beside it and its paths rewritten. Published templates are always flat, so rendering never
  depends on your folder layout.
- **It smoke-tests the result.** The flattened template is compiled to produce the gallery thumbnail. If it
  doesn't compile once published, the publish is rolled back and you keep the version that worked.
- **It records who published what** in `.published.json`, which is how the Admin UI can link back to the
  document in the designer.
- **It won't silently take over someone else's name.** Publishing over a template someone else published
  asks first.

---

# Part 3 — The PDF Admin UI

`/admin-ui/pdf` is where published templates live. It's an admin-only page for browsing what your App can
render, exercising templates against real data, and getting the code to use them.

## Browse the gallery

Published templates are shown as thumbnails — the preview rendered at publish time — with search and
sorting by name, modified date or size.

![The published templates gallery](img/pdf/admin-gallery.png)

> 📸 **`admin-gallery.png`** — the landing state of `/admin-ui/pdf` with several published templates so the
> grid reads as a gallery. Publish 4–6 visually distinct templates first (invoice, receipt, label, report);
> one lonely thumbnail undersells it.

The same picker is available from the **Open** button once you have a template selected, so you can move
between templates without going back.

## Run a template against real data

Selecting a template opens a two-pane workspace: the document's data on the left, the rendered PDF on the
right. Edit the data and the preview re-renders as you type.

![The template workspace: data and live preview](img/pdf/admin-workspace.png)

> 📸 **`admin-workspace.png`** — the full two-pane view with the **Data** tab active and a rendered invoice
> beside it. The headline shot for this section; make sure both panes have real content.

The data pane has three tabs:

### Form

The `.ui.json` schema rendered as an editable form — labelled fields, date pickers, dropdowns for enums,
add/remove rows for line items. This is how you exercise a template without touching JSON, and it's a
faithful preview of what a schema-driven UI over the same document would look like.

![Editing document data as a form](img/pdf/admin-tab-form.png)

> 📸 **`admin-tab-form.png`** — the **Form** tab with nested groups expanded and an array of line items
> visible, so the add/remove row controls are in frame.

### Data

The raw JSON, syntax-highlighted and editable, for when you want to paste a payload straight in. Invalid
JSON is reported inline rather than blanking the preview. **Reset** puts back the published example.

![Editing document data as JSON](img/pdf/admin-tab-data.png)

> 📸 **`admin-tab-data.png`** — the **Data** tab mid-edit. If you can catch the preview showing the edited
> value, that sells the live re-render better than a static shot.

### Code

The code generation tools — covered in [Part 4](#part-4--code-generation).

## The preview pane

Rendered with pdf.js, so it's the real PDF, not an approximation:

| Control | Does |
| --- | --- |
| **Render** | recompile with the current data (also runs automatically as you edit) |
| **− / + / Fit** | zoom out, in, or scale to the pane width |
| page count | how many pages the current data produced |
| **Download** | save the rendered PDF |

## Edit and Unpublish

**Edit** takes you back to the template in PDF Studio — and if it isn't in your workspace (say a colleague
published it), it copies the published files in first, so you can pick up someone else's template and keep
working. **Unpublish** removes it from `App_Data/pdf`.

![The template toolbar](img/pdf/admin-toolbar.png)

> 📸 **`admin-toolbar.png`** — a crop of the header showing the template name, "Published by … · 2 hours
> ago", and the Open / Edit / Unpublish buttons.

---

# Part 4 — Code generation

This is what turns a published template into something your App can use without ever writing a JSON key by
hand. The **Code** tab has three sub-tabs, all generated from *this* template — real type names, real
members, real template name.

![The Code tab's three sub-tabs](img/pdf/admin-tab-code.png)

> 📸 **`admin-tab-code.png`** — the **Code** tab with the tab strip visible and the generated model
> showing. Capture the `[Pdf("invoice")]` attribute and a couple of properties, plus the copy icon in the
> code block's corner.

## PDF Data Models

A C# class per type in the document, generated from the `.ui.json` schema (falling back to the example data
when there isn't one). Structurally identical objects collapse into a single type, recursive schemas
generate recursive classes, and awkward keys keep their wire name:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ServiceStack.AI;

namespace MyApp.ServiceModel.Pdf;

public class InvoiceDetails
{
    [JsonPropertyName("number")]
    public string Number { get; set; } = null!;

    /// <summary>The date payment is due.</summary>
    [JsonPropertyName("due")]
    public DateTime Due { get; set; }
    // …
}

public class LineItem
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("qty")]
    public int Qty { get; set; }

    /// <summary>The price charged for one unit of this item.</summary>
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }
}

/// <summary>Details, parties, line items, tax, and notes for an invoice.</summary>
[Pdf("invoice")]
public class Invoice
{
    [JsonPropertyName("items")]
    public List<LineItem> Items { get; set; } = new();
    // …
}
```

Note `[Pdf("invoice")]` on the root class. It binds the model to the template that renders it, so the
template name is never repeated at a call site. It's metadata and nothing else — it doesn't create an API,
change content negotiation, or touch the request pipeline.

### Generate them into your project

Copying from the UI is fine for one model. For the whole set, configure the generator on the plugin and
register an AppTask — the same arrangement as OrmLite Migrations, where writing into your source tree is
something you ask for rather than something a running App does:

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

Then, whenever a template changes:

```bash
$ dotnet run --AppTasks=pdf
```

```
Generated PDF models in /home/me/src/MyApp/MyApp.ServiceModel/Pdf
  generated Invoice.cs
  unchanged Receipt.cs
  skipped   quote (Quote.cs was edited by hand)
```

![Running the pdf AppTask](img/pdf/apptask-run.png)

> 📸 **`apptask-run.png`** — a terminal running `dotnet run --AppTasks=pdf` with a mix of
> generated/unchanged/skipped lines. Publish 3+ templates and hand-edit one first so all three outcomes
> appear.

Both paths default to a `Pdf` subfolder of your App's ServiceModel — `MyApp.ServiceModel/Pdf`, in the
`MyApp.ServiceModel.Pdf` namespace. Generated names come from the document's own keys and are generic
enough (`Item`, `From`, `Details`) to collide with your App's types, which is why they get their own folder
and namespace.

Because the UI and the task run the *same* generator with the *same* config, what you read in the Code tab
is exactly what lands in the file.

### Your edits survive

Each generated file carries a header hashing everything below it:

```csharp
// <auto-generated hash="eeda7d80ab51ceaa">
//     Generated from App_Data/pdf/invoice.ui.json by `dotnet run --AppTasks=pdf`.
//     Safe to edit — the next run leaves a modified file alone instead of overwriting it.
// </auto-generated>
#nullable enable
```

So the next run can tell a file it wrote from one you've since taken over. An edited file is skipped and
reported, never clobbered — and neither is a hand-written file that happens to share the name. `Exclude`
says the same thing up front, for a model you've adopted wholesale:

```csharp
PdfCodeGen = new() {
    Exclude = ["invoice"],       // hand-tuned, don't regenerate it
    PreserveModified = false,    // and overwrite everything else, edits and all
}
```

## Rendering a PDF API

A complete Service that returns the PDF as a download, with the model's object initialiser written out in
full — every member the template reads, so you can see the whole shape and fill it in from your own data:

```csharp
public class InvoicePdfServices(IPdfRenderer pdf) : Service
{
    public async Task<object> Any(GetInvoicePdf request)
    {
        // 1. Load your own data, in your own shape
        // var order = await Db.LoadSingleByIdAsync<Order>(request.Id);

        // 2. Map it onto the PDF model…
        var invoice = new Invoice
        {
            Items =
            [
                new LineItem { Description = "", Qty = 0, Rate = 0m },
            ],
            // …
        };

        // 3. Return it as a download
        return await pdf.PdfResultAsync(invoice, $"invoice-{request.Id}.pdf");
    }
}
```

## Sending an Email

The same mapping, wired into a background Command that attaches the rendered bytes to an email — including
the `IDbConnectionFactory` you'll want for loading the document, and the `EnqueueCommand` call to kick it
off.

![The example code tabs](img/pdf/admin-code-examples.png)

> 📸 **`admin-code-examples.png`** — the **Rendering a PDF API** tab (or a two-up with **Sending an
> Email**), scrolled to show the generated object initialiser. That's the part people don't expect to be
> generated, so make it the focus.

---

# Part 5 — End to end

Here's the whole thing, from a published `invoice` template to an App that serves it and mails it.

### 1. Generate the models

```bash
$ dotnet run --AppTasks=pdf
```

### 2. Return it from an API

```csharp
using ServiceStack;
using ServiceStack.AI;
using MyApp.ServiceModel.Pdf;

namespace MyApp.ServiceInterface;

[Route("/orders/{Id}/invoice")]
public class GetOrderInvoice : IGet, IReturn<byte[]>
{
    public int Id { get; set; }
}

public class InvoiceServices(IPdfRenderer pdf) : Service
{
    public async Task<object> Any(GetOrderInvoice request)
    {
        var order = await Db.LoadSingleByIdAsync<Order>(request.Id);

        var invoice = new Invoice
        {
            InvoiceValue = new InvoiceDetails
            {
                Number = order.InvoiceNo,
                Date = order.OrderDate.ToString("d MMMM yyyy"),
                Due = order.DueDate,
                Currency = "$",
            },
            From = new From { Name = "Acme Pty Ltd", Lines = ["123 Trade St", "Sydney NSW 2000"] },
            To = new From { Name = order.ShipName, Lines = [order.ShipAddress, order.ShipCity] },
            Items = order.Details.Map(x => new LineItem {
                Description = x.ProductName,
                Qty = x.Quantity,
                Rate = x.UnitPrice,
            }),
            TaxRate = 0.10m,
            Notes = "Payment due within 30 days.",
        };

        return await pdf.PdfResultAsync(invoice, $"Invoice-{order.InvoiceNo}.pdf");
    }
}
```

`PdfResultAsync` returns an `HttpResult` with the headers a browser needs to name the download:

```
HTTP/1.1 200 OK
Content-Type: application/pdf
Content-Disposition: attachment; filename="Invoice-INV-2026-042.pdf"
```

Pass `inline: true` to display it in the browser instead. Omit the file name to use the attribute's
`FileName`, which defaults to `{Template}.pdf`.

![The rendered PDF served from your API](img/pdf/end-to-end-download.png)

> 📸 **`end-to-end-download.png`** — a browser at `/orders/1/invoice` showing the PDF inline (use
> `inline: true` for the shot), with the URL bar visible so it's clearly your App's own API.

### 3. Or attach it to an email

```csharp
[Worker("smtp")]
public class SendInvoiceEmailCommand(IPdfRenderer pdf, SmtpConfig config,
    IDbConnectionFactory dbFactory) : AsyncCommand<SendInvoiceEmail>
{
    protected override async Task RunAsync(SendInvoiceEmail request, CancellationToken token)
    {
        using var db = await dbFactory.OpenAsync(token: token);
        var order = await db.LoadSingleByIdAsync<Order>(request.OrderId, token: token);

        var invoice = MapToInvoice(order);
        var pdfBytes = await pdf.RenderPdfAsync(invoice, token);

        using var client = new SmtpClient(config.Host, config.Port) { /* … */ };
        using var msg = new MailMessage(config.FromEmail, request.To) {
            Subject = request.Subject,
            Body = request.BodyText,
        };
        msg.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "invoice.pdf", MimeTypes.Pdf));

        await client.SendMailAsync(msg, token);
    }
}
```

Queue it as you would any Command:

```csharp
jobs.EnqueueCommand<SendInvoiceEmailCommand>(new SendInvoiceEmail {
    To = order.Email,
    Subject = $"Your invoice {order.InvoiceNo}",
    OrderId = order.Id,
});
```

Keep the job argument to the **id**, not the rendered bytes — a job argument is persisted, and a PDF in the
jobs table is a bad time.

---

# Reference

## `PdfFeature`

| Property | Default | Purpose |
| --- | --- | --- |
| `PdfPath` | `~/App_Data/pdf` | where published templates live |
| `TypstPath` | `$TYPST_PATH` or `PATH` | the typst binary |
| `RenderTimeout` | 30s | before a compile is killed |
| `PreviewTimeout` | 60s | longer budget for rasterising a thumbnail |
| `PreviewPpi` | 96 | resolution of gallery thumbnails |
| `MaxConcurrentRenders` | `ProcessorCount` | bounds forked typst processes |
| `MaxDataBytes` | 64KB | largest data payload a render accepts |
| `PdfCodeGen` | `null` | code generation config — see below |
| `ModelsPath` | `<ServiceModel>/Pdf` | where models generate, when `PdfCodeGen` doesn't say |
| `ModelsNamespace` | `<ServiceModel namespace>.Pdf` | the namespace they're emitted into |
| `Renderer` | `PdfRenderer` | swap in your own `IPdfRenderer` |

## `PdfCodeGenConfig`

| Option | Default | Purpose |
| --- | --- | --- |
| `Namespace` | `ModelsNamespace` | namespace the models are emitted into |
| `OutputPath` | `ModelsPath` | folder they're written to, created if missing |
| `Include` | all published | only generate these templates |
| `Exclude` | none | templates to leave alone |
| `ResolveFileName` | `Invoice.cs` | names the file a template generates into |
| `PreserveModified` | `true` | never overwrite a generated file you've edited |
| `Usings` | none | extra usings to emit |
| `Filter` | none | last say over each file: edit its source, or `Skip` it |

## Rendering

| Call | Returns |
| --- | --- |
| `pdf.RenderPdfAsync(model, token)` | PDF bytes for a `[Pdf]`-decorated model |
| `pdf.PdfResultAsync(model, fileName, inline, token)` | an `HttpResult` a Service can return directly |
| `pdf.RenderAsync(name, data, token)` | render by template name, when you have no model |
| `pdf.RenderPngAsync(name, …)` | rasterise a page, e.g. for a thumbnail |
| `pdf.GetTemplateNames()` | what's currently published |

## `[Pdf]`

| Property | Purpose |
| --- | --- |
| `Template` | published template name, without the `.typ` |
| `FileName` | default download name, defaults to `{Template}.pdf` |

---

# Gotchas

**Unset members disappear rather than becoming null.** The renderer serialises with
`DefaultIgnoreCondition = WhenWritingNull`, so a member you didn't populate is *omitted from the JSON
entirely* and typst fails with `dictionary does not contain key "paymentBank"`. Populate every member the
template reads — which is exactly why the generated examples spell out the full initialiser.

**Your model only covers what the schema declares.** If a template's `.ui.json` has drifted from its
`.typ`, the model will be missing fields and renders fail on the first one typst reaches. The Code tab
warns when a template has no schema at all.

**Generated class names are generic.** `Item`, `From` and `Details` come straight from the data's own keys,
which is why models default into their own folder and namespace.

**Data rides typst's command line.** It's passed as `--input data=<json>`, which the OS caps well below
128KB once the environment is counted — hence the conservative 64KB `MaxDataBytes`. Documents with
hundreds of line items should paginate, not grow one payload.

**`lib.typ` is shared.** Publishing a template republishes the library it imports, which can affect other
published templates. The publish response says so when it happens.

# Security notes

- The Admin UI and every `Admin*Pdf*` API require the **Admin** role.
- Nothing in the Admin UI writes to your source tree. Generating models is an AppTask *you* run.
- Published names are flat and validated — a template name can't contain separators, `..` or an absolute
  path, so nothing escapes `App_Data/pdf`.
- Designer workspaces are per-user and path-checked; publishing is the only way anything reaches the App's
  shared folder, and it's admin-only.
- typst compiles are sandboxed to their own directory, time-limited by `RenderTimeout`, and bounded by
  `MaxConcurrentRenders` so a burst can't saturate the host.

---

## Screenshot checklist

| File | Shows |
| --- | --- |
| `hero-overview.png` | three-panel banner: Studio, Admin UI, C# |
| `studio-overview.png` | the whole PDF Studio with the invoice example |
| `studio-ai-edit.png` | one AI prompt, before and after |
| `studio-attach-image.png` | building a template from an attached screenshot |
| `studio-generate-schema.png` | generating `invoice.ui.json` from the data |
| `studio-publish.png` | the Publish button and its toast |
| `admin-gallery.png` | the published templates gallery |
| `admin-workspace.png` | data pane + live PDF preview |
| `admin-tab-form.png` | the Form tab editing nested data |
| `admin-tab-data.png` | the Data tab editing raw JSON |
| `admin-tab-code.png` | the Code tab and its three sub-tabs |
| `admin-code-examples.png` | a generated example with its object initialiser |
| `admin-toolbar.png` | template header: published-by, Open / Edit / Unpublish |
| `apptask-run.png` | `dotnet run --AppTasks=pdf` output |
| `end-to-end-download.png` | the PDF served from your own API |

Put them in `img/pdf/` next to this document. Shoot in **light theme** at a consistent window width
(1440px works well) so the set looks like one sitting, and publish a handful of visually distinct templates
before taking the gallery shot.
