# ServiceStack PDF support

> This document is an implementation reference for AI assistants and developers working with PDF support
> in `ServiceStack.AI.Chat`. Treat the behavior, paths, limits and API contracts documented here as the
> supported behavior. Do not infer general PDF manipulation features from the presence of a PDF renderer;
> see [Supported scope](#supported-scope) for the explicit boundary.

ServiceStack produces PDFs from [Typst](https://typst.app) templates. PDF Studio can author those templates
with AI assistance, but templates are plain-text `.typ` files and can also be edited manually. Published
templates render in a .NET App through `IPdfRenderer`; production rendering does not call an LLM and does
not require `ChatFeature`.

The normal artifact set for a document named `invoice` is:

| File | Purpose |
| --- | --- |
| `invoice.typ` | Typst source and layout |
| `invoice.json` | example data used by previews and as code-generation fallback |
| `invoice.ui.json` | optional JSON Schema used by forms and typed C# generation |
| `invoice.*` assets | images and other document-specific resources |
| `lib.typ` | shared Typst helpers and styles |

## Quick start

1. Install `typst` and ensure the executable is on `PATH`, or set `TYPST_PATH`.
2. Register `services.AddPlugin(new PdfFeature());`.
3. With `ChatFeature` installed, open `/chat/pdf`, create or edit a template, and publish it.
4. Open `/admin-ui/pdf` to test the published template with form or JSON data.
5. Configure `PdfCodeGen`, run `dotnet run --AppTasks=pdf`, and populate the generated `[Pdf]` model.
6. Return `await pdf.PdfResultAsync(model)` from a Service, or call `RenderPdfAsync` for PDF bytes.

For rendering without the designer, place a valid published artifact set directly in `App_Data/pdf` and
install only `PdfFeature`.

## Architecture and lifecycle

| Step | Where | What you get |
| --- | --- | --- |
| 1. Design the template | **PDF Studio** — `/chat/pdf` | a `.typ` template + its `.json` example data |
| 2. Publish it | the Studio's **Publish** button | a flat runtime copy in `App_Data/pdf` |
| 3. Generate a data model | **Admin UI** — `/admin-ui/pdf` → **Code** | a typed C# class bound to the template |
| 4. Render it | your App's code | PDF bytes, or an `HttpResult` a Service returns |

Steps 1 and 2 are dev-time and interactive. Steps 3 and 4 are what ships: at runtime your App only ever
touches `App_Data/pdf` and `IPdfRenderer`, with no dependency on the Chat UI or on any AI provider.

### Two plugins, deliberately separate

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

### Build a template from a screenshot

The prompt accepts image attachments — up to 8 screenshots, photos or rasterised PDF pages. Paste in a
picture of the document you're replacing and ask for it back as a typst template. This is by far the
fastest way to start: an existing invoice becomes a working template in one round trip.

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

## The shared `lib.typ`

Every template imports `lib.typ`, which holds the styles and helpers your documents share — fonts, colours,
a `money()` formatter, header and footer blocks. Change it once and every template picks it up.
`lib.preview.typ` is a small document that renders the library itself, so you can see what's in it.

---

# Part 2 — Publish

When a template is ready, hit **Publish** in the Studio's preview toolbar. (The button only appears for
admins, on a real template — not on `lib.typ`.)

Publishing copies the template out of your personal workspace into the App's shared `App_Data/pdf` folder,
and does rather more than a file copy:

- **It follows the template's references.** Data files, `#include`d partials, `#image()` assets and
  `lib.typ` all come along, up to 8 levels deep.
- **It flattens them.** A template authored at `reports/quote.typ` is published as `quote.typ` with its
  companions beside it and its paths rewritten. Published templates are always flat, so rendering never
  depends on your folder layout.
- **It smoke-tests the result.** The flattened template is compiled to produce the gallery thumbnail. If it
  doesn't compile once published, the publish is rolled back and you keep the version that worked.
- **It validates the data contract.** The example and named fixtures are checked against `.ui.json`, model
  generation is exercised, statically visible `data.*` paths are inspected, and every fixture is compiled
  through the flattened template. Contract errors abort the publish and preserve the live revision.
- **It records who published what** in `.published.json`, which is how the Admin UI can link back to the
  document in the designer.
- **It won't silently take over someone else's name.** Publishing over a template someone else published
  asks first.
- **It keeps immutable revisions.** Every successful publish is copied to
  `App_Data/pdf/.versions/<template>/<revision>` with the files produced by publishing, the thumbnail when
  available, and publishing metadata. The hidden revision folders don't appear as renderable templates
  and require no database.

### Version history and rollback

Open a published template in `/admin-ui/pdf` and choose **History** to see who published each revision and
when. **Restore** replaces the live files with that snapshot. A restore never rewrites or deletes history:
it creates a new revision whose metadata points back to the revision it restored, leaving a complete audit
trail and making the rollback itself reversible.

Revisions are intentionally retained when a template is unpublished, so an administrator can republish the
same name without losing its history. Back up `.versions` together with the rest of `App_Data/pdf`. If a
revision contains the shared `lib.typ`, restoring it may affect other templates and the Admin UI reports a
warning.

### Contract fixtures

Add sibling files named `<template>.fixture.<name>.json` to exercise important payload shapes. For example:

```text
invoice.typ
invoice.json
invoice.ui.json
invoice.fixture.empty.json
invoice.fixture.long.json
invoice.fixture.international.json
```

On publish, `invoice.json` and every fixture are validated against `invoice.ui.json`. The validator enforces
the schema subset generated by PDF Studio: types, properties, required members, `additionalProperties`,
array items and bounds, enums/constants, numeric and string bounds, patterns, `date`, `date-time`, `uuid`,
local `$ref`, and `allOf`/`anyOf`/`oneOf`. Unsupported schema keywords produce warnings rather than a false
claim that they were enforced.

After the files are flattened, each fixture is sent through the real Typst renderer. A schema-valid fixture
that triggers a template error therefore still blocks publishing. Validation results contain severity,
code, JSON path, fixture name and message. `PdfFeature.ValidateOnPublish = false` disables contract checks;
the default is `true`.

---

# Part 3 — The PDF Admin UI

`/admin-ui/pdf` is where published templates live. It's an admin-only page for browsing what your App can
render, exercising templates against real data, and getting the code to use them.

## Browse the gallery

Published templates are shown as thumbnails — the preview rendered at publish time — with search and
sorting by name, modified date or size.

The same picker is available from the **Open** button once you have a template selected, so you can move
between templates without going back.

## Run a template against real data

Selecting a template opens a two-pane workspace: the document's data on the left, the rendered PDF on the
right. Edit the data and the preview re-renders as you type.

The data pane has three tabs:

### Form

The `.ui.json` schema rendered as an editable form — labelled fields, date pickers, dropdowns for enums,
add/remove rows for line items. This is how you exercise a template without touching JSON, and it's a
faithful preview of what a schema-driven UI over the same document would look like.

### Data

The raw JSON, syntax-highlighted and editable, for when you want to paste a payload straight in. Invalid
JSON is reported inline rather than blanking the preview. **Reset** puts back the published example.

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

---

# Part 4 — Code generation

This is what turns a published template into something your App can use without ever writing a JSON key by
hand. The **Code** tab has three sub-tabs, all generated from *this* template — real type names, real
members, real template name.

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

## Published filesystem layout

```text
App_Data/pdf/
├── .published.json
├── .versions/
│   └── invoice/
│       └── <revision>/
│           ├── revision.json
│           ├── invoice.typ
│           ├── invoice.json
│           ├── invoice.ui.json
│           └── invoice.preview.png
├── fonts/
├── lib.typ
├── invoice.typ
├── invoice.json
├── invoice.ui.json
└── invoice.preview.png
```

The files at the root are the live revision used by `IPdfRenderer`. `.versions` contains immutable
snapshots and `revision.json` metadata. `.published.json` records live ownership, source location, files and
current revision. A revision directory is not a renderable template.

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
| `ValidateOnPublish` | `true` | validate examples, fixtures and model generation before publishing |
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

## Admin APIs

All these request DTOs require the Admin role and are excluded from ordinary metadata pages.

| Request | Method | Purpose |
| --- | --- | --- |
| `AdminPdfTemplates` | GET | list live published templates and feature status |
| `AdminGetPdfTemplate` | GET | get one template's example data, schema and optional source |
| `AdminRenderPdfTemplate` | POST | render a live template with arbitrary JSON |
| `AdminPdfTemplatePreview` | GET | return the stored gallery thumbnail |
| `AdminPublishPdfTemplate` | POST | publish a Studio template and create an immutable revision |
| `AdminPdfTemplateVersions` | GET | list immutable revisions and the current revision ID |
| `AdminRollbackPdfTemplate` | POST | restore a revision and record a new rollback revision |
| `AdminEditPdfTemplate` | POST | copy missing published files into the current user's Studio workspace |
| `AdminDeletePdfTemplate` | POST | unpublish live files while retaining revision history |
| `AdminPdfTemplateTypes` | POST | generate model source and usage examples without writing source files |

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
128KB once the environment is counted — hence the conservative 64KB `MaxDataBytes`. Pagination controls
layout but does not reduce the JSON payload size. For data beyond this limit, split the document into
multiple renders or replace `IPdfRenderer` with an implementation that transports data differently.

**`lib.typ` is shared.** Publishing a template republishes the library it imports, which can affect other
published templates. The publish response says so when it happens.

# Supported scope

Built-in support includes:

- Authoring `.typ` templates and companion files in per-user PDF Studio workspaces.
- AI edits from text prompts, screenshots and rasterised PDF pages.
- Typst compilation to PDF with local images, partials, data files, fonts and Typst packages.
- Publishing a flattened runtime artifact set to `App_Data/pdf`; local referenced files are copied, while
  external Typst packages remain external dependencies.
- Publish-time contract validation of example data and explicitly named fixtures.
- Immutable filesystem revisions, revision history and rollback.
- Admin preview and download through pdf.js, with form and raw JSON test-data editors.
- JSON Schema-assisted C# model generation.
- Rendering to `byte[]`, or an `HttpResult` with inline/download content disposition.
- PNG rendering of one selected page, primarily for gallery thumbnails.

The built-in implementation does **not** provide PDF merging, splitting, page reordering, encryption,
password protection, digital signatures, timestamping, PDF/A conversion or validation, AcroForm filling,
form flattening, tagged-PDF validation, OCR, PDF text extraction, or streaming output. Use a separate PDF
library before or after rendering, or replace/wrap `IPdfRenderer`, when an application needs those features.

PDF import in Studio is a visual reconstruction aid, not a structural PDF importer. The browser rasterises
at most the first four pages for the model; it does not preserve source fonts, text objects, forms,
annotations, metadata or accessibility structure.

# Security and privacy

- The Admin UI and every `Admin*Pdf*` API require the **Admin** role.
- Nothing in the Admin UI writes to the application's source tree. Only the explicitly registered `pdf`
  AppTask generates model source files.
- Published names are flat and validated: names cannot contain path separators, traversal segments or
  absolute paths.
- Designer workspaces are per-user and path-checked. Publishing is the admin-only boundary that copies
  files into the shared runtime folder.
- Typst receives `--root` restricted to the relevant PDF directory. Compiles are also time-limited and
  concurrency-limited. This file-access restriction is not an operating-system process sandbox; deploy
  untrusted template compilation inside an appropriately isolated container or worker.
- Images and PDF pages attached to an AI prompt are sent to the configured model provider as image data.
  Do not attach secrets, personal information or regulated documents unless that provider and deployment
  are approved for the data.
- Runtime document data is passed as a process argument. Avoid secrets that must not be visible to local
  process-inspection tools, or provide a custom renderer with a different transport.
- Back up `.published.json`, `.versions` and all live files together. Revisions contain the example data
  and assets present at publish time and may therefore contain sensitive information.

# Deployment

## Pin Typst and fonts

PDF layout can change when the Typst version or installed fonts change. Pin the Typst CLI version in every
environment, deploy the same font files, and validate representative documents before upgrading either.
At startup, `PdfFeature.TypstPath` resolves from `TYPST_PATH` and then `PATH`.

The renderer passes `App_Data/pdf/fonts` to Typst as a font directory when it exists. Include that folder
in deployments when templates depend on non-system fonts; do not assume a developer workstation's fonts
exist in a container.

## Container example

This example supplies a pinned Typst binary to an ASP.NET runtime image. Add the application's normal
publish and copy stages around it, and replace the example version with the version validated by the App.

```dockerfile
FROM rust:1-bookworm AS typst
ARG TYPST_VERSION
RUN test -n "$TYPST_VERSION" \
    && cargo install typst-cli --version "$TYPST_VERSION" --locked

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=typst /usr/local/cargo/bin/typst /usr/local/bin/typst
COPY ./publish/ ./
COPY ./App_Data/pdf/ ./App_Data/pdf/
ENV TYPST_PATH=/usr/local/bin/typst
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

For a different target framework, use the matching ASP.NET runtime image. If native fonts are installed
with the operating-system package manager, pin those package versions as well.

# Validation and CI

There is no built-in visual-regression or PDF conformance command. The following checks are appropriate:

1. Keep `.typ`, `.json`, `.ui.json`, assets and `.published.json` in deployment backups or source control
   according to the application's release policy.
2. Run `dotnet run --AppTasks=pdf` and review generated/unchanged/skipped output after schema changes.
3. Run application tests that construct every generated root model and call `RenderPdfAsync`.
4. Include empty, typical, long-text, maximum-list and international-character fixtures.
5. Pin Typst and fonts, render fixtures in CI, and compare page count or approved raster baselines when
   layout stability is important.

A simple compile check for published templates can use each template's example JSON fallback:

```bash
mkdir -p /tmp/pdf-smoke
for template in App_Data/pdf/*.typ; do
  case "$template" in */lib.typ|*/lib.preview.typ) continue ;; esac
  typst compile --root App_Data/pdf "$template" "/tmp/pdf-smoke/$(basename "${template%.typ}").pdf"
done
```

This checks compilation only. It does not prove that arbitrary runtime payloads satisfy the template or
that the rendered layout is visually correct.

# Troubleshooting

| Symptom | Likely cause | Action |
| --- | --- | --- |
| PDF Studio is absent or disabled | `typst` was not found when `ChatFeature` loaded | install Typst, set `TYPST_PATH`, and restart |
| Templates list but Render is unavailable | `PdfFeature.IsAvailable` is false | verify the service account can execute `TypstPath` |
| `dictionary does not contain key` | a model property was null/omitted, or schema/template drifted | populate every referenced member and align `.typ`, `.json` and `.ui.json` |
| `Data is too large` | serialized input exceeded `MaxDataBytes` or command-line limits | reduce/split data or use a custom renderer transport |
| A font differs or is missing | environments have different font installations | deploy the font under `App_Data/pdf/fonts` and pin it |
| Template works in Studio but fails after publishing | a reference was not collected or could not be rewritten | inspect publish warnings and use literal local references supported by the publisher |
| Another template changed after publish/rollback | shared `lib.typ` changed | inspect the publish warning and restore or republish the intended library |
| Code model is missing a property | `.ui.json` does not declare it | rebuild/edit the schema, regenerate models, and update mappings |
| Edited generated file is not updated | `PreserveModified` defaults to `true` | adopt the file intentionally, exclude it, or set `PreserveModified = false` |
| History is empty for an older template | it predates filesystem versioning | publish it once to create its first immutable revision |

# Guidance for AI assistants

When answering questions or proposing code based on this document:

1. Distinguish the three storage contexts: per-user Studio files, live published files, and immutable
   published revisions. Runtime rendering reads live published files only.
2. Distinguish authoring dependencies from runtime dependencies. AI and `ChatFeature` are optional at
   runtime; Typst and `PdfFeature` are required by the default renderer.
3. Prefer generated `[Pdf]` models for application code, but explain that they do not validate the model
   against every key read by the Typst source.
4. Never claim a successful compile proves visual correctness or schema compatibility.
5. Do not suggest editing `.versions`; revisions are immutable implementation artifacts. Restore them
   through the Admin API/UI.
6. Do not describe PDF import as editable PDF conversion. It supplies rasterised reference pages to a
   vision-capable model, which creates a new Typst approximation.
7. Do not invent support for signing, encryption, forms, PDF/A, OCR, merging or streaming. Recommend a
   separate PDF library or a custom `IPdfRenderer` pipeline.
8. Mention the shared `lib.typ` blast radius when a publish or rollback changes library behavior.
9. Preserve cancellation tokens in render calls and avoid persisting PDF bytes in background-job request
   DTOs; persist an identifier and render inside the job instead.
