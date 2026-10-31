# ServiceStack.AI.Chat features

Embeddable multi-provider chat application, durable agents, tools, MCP, RAG and published PDF generation.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net8.0;net10.0`; see [ServiceStack.AI.Chat.csproj](ServiceStack.AI.Chat.csproj) for conditional compilation and dependencies.
- Shared UI is generated from llms-py and must be edited upstream. C# hosting, identity, OrmLite storage and inbound MCP have deliberate differences. Filesystem/process tools require explicit host policy. Native FTS and provider behavior vary; no live-provider validation was performed for this catalog.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Server](../ServiceStack.Server/ServiceStack.Server.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Chat hosting and typed APIs | Embed a chat SPA and expose chat/decision APIs to application code. | [ChatFeature.cs](ChatFeature.cs), [ChatFeatureRoutes.cs](ChatFeatureRoutes.cs), [ChatServices.cs](ChatServices.cs), [IChatClient.cs](IChatClient.cs), [CreateDecision.cs](CreateDecision.cs) |
| Provider adapters and streaming | Use OpenAI-compatible, Anthropic and Google providers with streaming translation. | [Providers/OpenAiCompatibleProvider.cs](Providers/OpenAiCompatibleProvider.cs), [Providers/AnthropicProvider.cs](Providers/AnthropicProvider.cs), [Providers/GoogleProvider.cs](Providers/GoogleProvider.cs), [Providers/SseReader.cs](Providers/SseReader.cs) |
| Tool execution and approvals | Discover and invoke tools and opted-in ServiceStack APIs with approval controls. | [Pipeline/ToolRegistry.cs](Pipeline/ToolRegistry.cs), [Pipeline/ToolApproval.cs](Pipeline/ToolApproval.cs), [Pipeline/PluggableApis.cs](Pipeline/PluggableApis.cs), [API_TOOLS.md](API_TOOLS.md) |
| Durable agents and history | Persist runs, checkpoints and canonical message history across execution slices. | [Db/ChatDbDurable.cs](Db/ChatDbDurable.cs), [Db/DurableAgentTables.cs](Db/DurableAgentTables.cs), [Extensions/App/DURABLE_AGENTS.md](Extensions/App/DURABLE_AGENTS.md) |
| Identity and user-owned storage | Integrate host authentication and partition chat data by user. | [Auth/IdentityChatAuth.cs](Auth/IdentityChatAuth.cs), [Db/ChatDb.cs](Db/ChatDb.cs), [SECURITY.md](SECURITY.md) |
| Inbound and outbound MCP | Expose selected tools to external clients and connect to external MCP servers. | [Extensions/Mcp](Extensions/Mcp), [Extensions/McpClient](Extensions/McpClient), [MCP.md](MCP.md), [MCP_CLIENT_USER.md](MCP_CLIENT_USER.md) |
| Gemini RAG and assistants | Import and search documents and configure website assistants with metadata/search integrations. | [Extensions/Gemini](Extensions/Gemini) |
| Projects, Git, skills and workspaces | Organize conversations, select workspace context and use modular assistant tools. | [Extensions/App](Extensions/App), [Extensions/Git](Extensions/Git), [Extensions/Skills](Extensions/Skills), [Pipeline/WorkspaceScope.cs](Pipeline/WorkspaceScope.cs) |
| Media, analytics and publishing | Extend chat with media generation, usage analytics and publishing destinations. | [Providers/Generators](Providers/Generators), [Extensions/Analytics](Extensions/Analytics), [Extensions/ShareStatic](Extensions/ShareStatic), [Extensions/ShareLlmspy](Extensions/ShareLlmspy) |
| PDF Studio and runtime | Publish validated PDF templates and render typed application PDFs. | [PdfFeature.cs](PdfFeature.cs), [PdfPublisher.cs](PdfPublisher.cs), [PdfRenderer.cs](PdfRenderer.cs), [PdfCodeGen.cs](PdfCodeGen.cs), [PDF.md](PDF.md) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [chat/overview.md](../../../../docs.servicestack.net/MyApp/_pages/chat/overview.md)
- [chat/install.md](../../../../docs.servicestack.net/MyApp/_pages/chat/install.md)
- [chat/api.md](../../../../docs.servicestack.net/MyApp/_pages/chat/api.md)
- [chat/providers.md](../../../../docs.servicestack.net/MyApp/_pages/chat/providers.md)
- [chat/tools.md](../../../../docs.servicestack.net/MyApp/_pages/chat/tools.md)
- [chat/api-tools.md](../../../../docs.servicestack.net/MyApp/_pages/chat/api-tools.md)
- [chat/agents.md](../../../../docs.servicestack.net/MyApp/_pages/chat/agents.md)
- [chat/auth.md](../../../../docs.servicestack.net/MyApp/_pages/chat/auth.md)
- [chat/data.md](../../../../docs.servicestack.net/MyApp/_pages/chat/data.md)
- [chat/mcp_server.md](../../../../docs.servicestack.net/MyApp/_pages/chat/mcp_server.md)
- [chat/mcp_client.md](../../../../docs.servicestack.net/MyApp/_pages/chat/mcp_client.md)
- [chat/gemini-rag.md](../../../../docs.servicestack.net/MyApp/_pages/chat/gemini-rag.md)
- [chat/projects.md](../../../../docs.servicestack.net/MyApp/_pages/chat/projects.md)
- [chat/skills.md](../../../../docs.servicestack.net/MyApp/_pages/chat/skills.md)
- [chat/git.md](../../../../docs.servicestack.net/MyApp/_pages/chat/git.md)
- [chat/workspace-explorer.md](../../../../docs.servicestack.net/MyApp/_pages/chat/workspace-explorer.md)
- [chat/media.md](../../../../docs.servicestack.net/MyApp/_pages/chat/media.md)
- [chat/analytics.md](../../../../docs.servicestack.net/MyApp/_pages/chat/analytics.md)
- [chat/publishing.md](../../../../docs.servicestack.net/MyApp/_pages/chat/publishing.md)
- [chat/pdf-studio.md](../../../../docs.servicestack.net/MyApp/_pages/chat/pdf-studio.md)
- [chat/rendering-pdfs.md](../../../../docs.servicestack.net/MyApp/_pages/chat/rendering-pdfs.md)
- [chat/decision-studio.md](../../../../docs.servicestack.net/MyApp/_pages/chat/decision-studio.md)

Detailed local contracts: [README](README.md), [security](SECURITY.md), [API tools](API_TOOLS.md), [MCP server](MCP.md), [MCP client](MCP_CLIENT_USER.md), [durable agents](Extensions/App/DURABLE_AGENTS.md), [PDF](PDF.md).

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.AiTests/AiChatFeatureTests.cs](../../tests/ServiceStack.AiTests/AiChatFeatureTests.cs)
- [ServiceStack.AiTests/AiChatToolsTests.cs](../../tests/ServiceStack.AiTests/AiChatToolsTests.cs)
- [ServiceStack.AiTests/AiChatMigrationAgentsWebTests.cs](../../tests/ServiceStack.AiTests/AiChatMigrationAgentsWebTests.cs)
- [ServiceStack.AiTests/AiChatMigrationGeminiDialectTests.cs](../../tests/ServiceStack.AiTests/AiChatMigrationGeminiDialectTests.cs)
- [ServiceStack.AiTests/AiChatSyncTests.cs](../../tests/ServiceStack.AiTests/AiChatSyncTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
