# ServiceStack feature catalog

This index maps existing capabilities to their owning projects and keeps proposed work separate from accepted improvements. It is a first pass over the 29 projects directly under this `src` directory, not a completed audit of the entire ServiceStack suite.

## Baseline and evidence

- Review date: 2026-10-31.
- ServiceStack repository HEAD: `f24446665f086139356c849c36383692e14dfef5`.
- Documentation repository HEAD: `51d3ae93b07fc965093f420a425cfb921cd8166f`.
- Baselines describe the local checkout, including any pre-existing working-tree edits; published release availability has not been verified.
- Implementation entry points and project metadata were located, with targeted source review for capability boundaries. Related documentation and test files are linked for follow-up; this is not a line-by-line behavioral or security audit.
- Tests were not run: this change adds documentation only. A located test file does not establish passing tests, exhaustive coverage or provider parity.
- Existing `AGENTS.md` files were used as orientation, not authoritative feature/release evidence. Class names, target-framework guards and ownership were checked against the checkout.
- Public docs primarily live in a separate repository. Relative local links work with this checkout layout; the catalog is an internal source map rather than a portable published manual.

## Capability ownership

| Customer task | Primary owners | Related capabilities |
| --- | --- | --- |
| Build and expose DTO services | [Core](ServiceStack/FEATURES.md), [Interfaces](ServiceStack.Interfaces/FEATURES.md) | Routing, filters, validation, authentication, formats, metadata and schemas |
| Query and mutate application data | [Server](ServiceStack.Server/FEATURES.md), [Core](ServiceStack/FEATURES.md) | AutoQuery RDBMS/Data, AutoCrud, CRUD audit events, database administration |
| Call APIs from .NET | [Client](ServiceStack.Client/FEATURES.md), [HttpClient](ServiceStack.HttpClient/FEATURES.md), [gRPC client](ServiceStack.GrpcClient/FEATURES.md) | Typed clients, auth, caching, SSE and gRPC |
| Schedule and operate background work | [Jobs](ServiceStack.Jobs/FEATURES.md), [Server](ServiceStack.Server/FEATURES.md), [Core](ServiceStack/FEATURES.md) | Local SQLite jobs, relational leased jobs, schedules, batches and diagnostics |
| Integrate brokers and real-time delivery | [RabbitMQ](ServiceStack.RabbitMq/FEATURES.md), [Server](ServiceStack.Server/FEATURES.md), [Core](ServiceStack/FEATURES.md) | RabbitMQ, Redis MQ, SSE and distributed server events |
| Integrate ASP.NET Core identity/protocols | [Extensions](ServiceStack.Extensions/FEATURES.md) | Identity auth, administration, gRPC hosting, frontend helpers |
| Embed AI and document workflows | [AI.Chat](ServiceStack.AI.Chat/FEATURES.md), [AI](ServiceStack.AI/FEATURES.md), [Google Cloud](ServiceStack.GoogleCloud/FEATURES.md) | Chat, durable agents, MCP, RAG, tools, PDF, transcription and typed translation |
| Document APIs for external tooling | [Microsoft](ServiceStack.OpenApi.Microsoft/FEATURES.md), [Swashbuckle](ServiceStack.OpenApi.Swashbuckle/FEATURES.md), [older ASP.NET Core adapter](ServiceStack.AspNetCore.OpenApi/FEATURES.md), [Swagger 2](ServiceStack.Api.OpenApi/FEATURES.md) | Separate dependency generations and document pipelines |
| Render web/desktop experiences | [MVC](ServiceStack.Mvc/FEATURES.md), [Razor](ServiceStack.Razor/FEATURES.md), [Desktop](ServiceStack.Desktop/FEATURES.md), [Core](ServiceStack/FEATURES.md) | Razor Pages/SSG, legacy Razor, desktop bindings, Admin/Locode/API UIs |
| Share infrastructure | [Common](ServiceStack.Common/FEATURES.md), [Interfaces](ServiceStack.Interfaces/FEATURES.md) | VFS, #Script, process utilities, settings and contracts |

## Project index

Every project owns `FEATURES.md`, `plan/FEATURE-PROPOSALS.md` and `plan/FEATURE-IMPROVEMENTS.md`. A proposal spanning projects has one canonical owner and links from participants. Root planning files are indexes and process records, not duplicate project backlogs.

| Project | Purpose | Planning |
| --- | --- | --- |
| [ServiceStack](ServiceStack/FEATURES.md) | DTO-first services, hosting, authentication, metadata, generated UIs and cross-cutting plugins. | [Proposals](ServiceStack/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.AI](ServiceStack.AI/FEATURES.md) | Speech transcription and schema-directed natural-language translation building blocks. | [Proposals](ServiceStack.AI/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.AI/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.AI.Chat](ServiceStack.AI.Chat/FEATURES.md) | Embeddable multi-provider chat application, durable agents, tools, MCP, RAG and published PDF generation. | [Proposals](ServiceStack.AI.Chat/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.AI.Chat/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Api.OpenApi](ServiceStack.Api.OpenApi/FEATURES.md) | Classic Swagger/OpenAPI 2 document generation and embedded Swagger UI. | [Proposals](ServiceStack.Api.OpenApi/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Api.OpenApi/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.AspNetCore.OpenApi](ServiceStack.AspNetCore.OpenApi/FEATURES.md) | OpenAPI metadata integration for the older ASP.NET Core/Swashbuckle dependency line. | [Proposals](ServiceStack.AspNetCore.OpenApi/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.AspNetCore.OpenApi/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Authentication.MongoDb](ServiceStack.Authentication.MongoDb/FEATURES.md) | MongoDB-backed ServiceStack user, OAuth-link and API-key repositories. | [Proposals](ServiceStack.Authentication.MongoDb/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Authentication.MongoDb/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Authentication.RavenDb](ServiceStack.Authentication.RavenDb/FEATURES.md) | RavenDB-backed ServiceStack authentication repositories and API keys. | [Proposals](ServiceStack.Authentication.RavenDb/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Authentication.RavenDb/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Caching.Memcached](ServiceStack.Caching.Memcached/FEATURES.md) | Enyim-backed Memcached adapter for ServiceStack caching contracts. | [Proposals](ServiceStack.Caching.Memcached/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Caching.Memcached/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Client](ServiceStack.Client/FEATURES.md) | Typed .NET clients, client DTOs, URL construction, caching, server events and diagnostics. | [Proposals](ServiceStack.Client/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Client/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Common](ServiceStack.Common/FEATURES.md) | Shared VFS, #Script, configuration, process and application utility infrastructure. | [Proposals](ServiceStack.Common/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Common/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Desktop](ServiceStack.Desktop/FEATURES.md) | Embedded desktop assets and Windows-native operations for hosted web desktop applications. | [Proposals](ServiceStack.Desktop/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Desktop/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Extensions](ServiceStack.Extensions/FEATURES.md) | ASP.NET Core Identity, gRPC hosting and application integration helpers. | [Proposals](ServiceStack.Extensions/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Extensions/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.GoogleCloud](ServiceStack.GoogleCloud/FEATURES.md) | Google Cloud Storage virtual files and Google speech transcription. | [Proposals](ServiceStack.GoogleCloud/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.GoogleCloud/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.GrpcClient](ServiceStack.GrpcClient/FEATURES.md) | Typed .NET gRPC transport for ServiceStack services. | [Proposals](ServiceStack.GrpcClient/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.GrpcClient/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.HttpClient](ServiceStack.HttpClient/FEATURES.md) | HttpClient-based typed transport and a cache decorator for ServiceStack JSON services. | [Proposals](ServiceStack.HttpClient/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.HttpClient/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.ImageSharp](ServiceStack.ImageSharp/FEATURES.md) | ImageSharp implementation of the shared image provider. | [Proposals](ServiceStack.ImageSharp/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.ImageSharp/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Interfaces](ServiceStack.Interfaces/FEATURES.md) | Portable DTO contracts, attributes and provider interfaces shared by servers and clients. | [Proposals](ServiceStack.Interfaces/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Interfaces/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Jobs](ServiceStack.Jobs/FEATURES.md) | SQLite-backed background jobs, scheduling and partitioned request analytics. | [Proposals](ServiceStack.Jobs/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Jobs/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Kestrel](ServiceStack.Kestrel/FEATURES.md) | Direct Kestrel self-hosting and compatibility host base classes. | [Proposals](ServiceStack.Kestrel/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Kestrel/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.MsgPack](ServiceStack.MsgPack/FEATURES.md) | MessagePack content negotiation, typed HTTP transport and byte serialization helpers. | [Proposals](ServiceStack.MsgPack/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.MsgPack/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Mvc](ServiceStack.Mvc/FEATURES.md) | MVC/Razor integration, service-aware controllers and Razor static-site generation. | [Proposals](ServiceStack.Mvc/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Mvc/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.NetFramework](ServiceStack.NetFramework/FEATURES.md) | Legacy .NET Framework profiling and SmartThreadPool self-host integration. | [Proposals](ServiceStack.NetFramework/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.NetFramework/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.OpenApi.Microsoft](ServiceStack.OpenApi.Microsoft/FEATURES.md) | ServiceStack schema and operation enrichment for Microsoft ASP.NET Core OpenAPI. | [Proposals](ServiceStack.OpenApi.Microsoft/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.OpenApi.Microsoft/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.OpenApi.Swashbuckle](ServiceStack.OpenApi.Swashbuckle/FEATURES.md) | ServiceStack schema and operation enrichment for the newer Swashbuckle integration. | [Proposals](ServiceStack.OpenApi.Swashbuckle/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.OpenApi.Swashbuckle/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.ProtoBuf](ServiceStack.ProtoBuf/FEATURES.md) | protobuf-net Protocol Buffers content negotiation, typed HTTP transport and byte serialization helpers. | [Proposals](ServiceStack.ProtoBuf/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.ProtoBuf/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.RabbitMq](ServiceStack.RabbitMq/FEATURES.md) | RabbitMQ-backed DTO messaging, producers, consumers and queue provisioning. | [Proposals](ServiceStack.RabbitMq/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.RabbitMq/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Razor](ServiceStack.Razor/FEATURES.md) | Standalone legacy .NET Framework Razor view engine, compilation and HTML helpers. | [Proposals](ServiceStack.Razor/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Razor/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Server](ServiceStack.Server/FEATURES.md) | OrmLite and Redis integrations, AutoQuery/AutoCrud, relational jobs and database administration. | [Proposals](ServiceStack.Server/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Server/plan/FEATURE-IMPROVEMENTS.md) |
| [ServiceStack.Skia](ServiceStack.Skia/FEATURES.md) | SkiaSharp implementation of the shared image provider. | [Proposals](ServiceStack.Skia/plan/FEATURE-PROPOSALS.md) · [Accepted](ServiceStack.Skia/plan/FEATURE-IMPROVEMENTS.md) |

## Scope still to cover

Sibling source trees such as ServiceStack.OrmLite (including dialects), ServiceStack.Redis, ServiceStack.Text, ServiceStack.Blazor, ServiceStack.Aws, ServiceStack.Azure, ServiceStack.Logging and ServiceStack.Stripe have not been inventoried here. Their integrations may be referenced, but that does not constitute coverage of the owning libraries.

Java/Swift and other language clients, JavaScript/Vue/React packages, tools, templates, AI Server, SharpScript and upstream llms-py also require separate inventories. Framework docs include examples, releases, third-party integrations and products outside these 29 projects; unassigned pages are not automatically missing features.

## Planning workflow

1. Check the owning project's FEATURES.md, implementation, tests, public documentation and existing plans before proposing work. Confirm whether the gap is missing implementation, discoverability, documentation, provider support or integration.
2. Add a stable, project-prefixed ID to `plan/FEATURE-PROPOSALS.md`. Include the customer problem, existing baseline, proposed increment, alternatives, evidence, value, effort, confidence, compatibility risks and validation needed. Mark uncertain absence claims explicitly.
3. Rank investigation separately from acceptance. Technical fit is not customer demand. Validate demand with support, discussions, adoption evidence or interviews; do not invent usage/revenue estimates.
4. After explicit maintainer acceptance, move the complete proposal into the owning project's `plan/FEATURE-IMPROVEMENTS.md`, preserving its ID. Record the accepting maintainer, date, decision reference, scope, plan and acceptance criteria. Leave a link in proposals and update the root indexes.
5. Track Accepted → In progress → Implemented → Released separately. Update FEATURES.md only with actual supported behavior; record source evidence and confirmed release availability. Deferred/rejected ideas stay in proposals with a reason.

The user's approval of this documentation exercise is not acceptance of any product proposal. Existing PLAN.md files and historical feature notes remain implementation context; they are not automatically imported as accepted roadmap decisions.

## Maintaining the inventory

- For each capability, retain its customer outcome, owning implementation entry points, configuration location, related docs, important constraints and test evidence. The initial project tables group related APIs; split groups when provider or release differences matter.
- Add an explicit verification date/revision for substantial future reviews. Replace broad test-file pointers with specific scenarios as behavior is audited.
- A published release is confirmed only by a version/tag/release-note cross-check; current source presence alone is insufficient.
- Keep AGENTS.md limited to development guidance and links. Check relative links when files or projects move.
- Preserve detailed existing documents; link or reconcile disagreements instead of silently rewriting historical claims.

See [proposal priorities](plan/FEATURE-PROPOSALS.md), [acceptance index](plan/FEATURE-IMPROVEMENTS.md) and [coverage/reconciliation report](plan/FEATURE-COVERAGE.md).
