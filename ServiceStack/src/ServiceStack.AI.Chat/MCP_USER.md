# User Guide: Connecting AI Assistants to ServiceStack via MCP

> **Connect OpenCode, Claude Code, Cursor, and other AI assistants to your local ServiceStack .NET application using the Model Context Protocol (MCP).**

---

## 1. Overview

ServiceStack's built-in MCP server at `/chat/mcp` lets external AI assistants discover, query, and execute your application's ServiceStack APIs using standard typed Request DTOs, validation rules, and authorization gates.

With API Tools and MCP enabled:
- **Read APIs** (`IGet`, `QueryDb<>`, `QueryData<>`) execute immediately.
- **Write / Destructive APIs** automatically trigger a **Two-Phase Confirmation** flow: the server returns a summary and a secure `confirmationToken`, the AI assistant prompts you in chat for confirmation, and upon approval submits the request with the token.

```
┌──────────────────────────┐                  ┌──────────────────────────┐
│   External AI Assistant  │                  │  Local ServiceStack App  │
│  (OpenCode, Cursor, etc) │                  │  (http://localhost:5000) │
└────────────┬─────────────┘                  └────────────┬─────────────┘
             │                                             │
             │   Streamable HTTP (JSON-RPC POST)           │
             │   POST /chat/mcp                            │
             │   Authorization: Bearer ak-your-api-key     │
             ├────────────────────────────────────────────►│
             │                                             │
             │   tools/list: api_search, api_describe,     │
             │               api_call                      │
             │◄────────────────────────────────────────────┤
```

---

## 2. Server Setup (in your .NET App)

Ensure your ServiceStack application has `ChatFeature` registered with `EnableApiTools` and `Mcp` enabled in your `Configure.Ai.cs` or `Program.cs`:

```csharp
using ServiceStack.AI;

public class ConfigureAi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.AddPlugin(new ChatFeature
            {
                // Set RequireAuth = false for open local dev, or true to require API Keys
                RequireAuth = false,

                Tools =
                {
                    EnableApiTools = true,
                },

                ApiTools =
                {
                    // Include the tags or Request DTOs you want exposed
                    IncludeTags = ["CoffeeShop", "todos", "Bookings"],
                },

                Mcp =
                {
                    // Expose the api_tools group to MCP clients (default)
                    ToolGroups = ["api_tools"],
                    
                    // Default mode: Server-enforced two-phase confirmation tokens
                    ApprovalMode = McpApprovalMode.ConfirmationToken,
                }
            });
        });
}
```

Start your app:
```bash
dotnet run
# App listening on http://localhost:5000
```

---

## 3. Configuring OpenCode / omp (Recommended Example)

[OpenCode](https://opencode.ai) (`omp`) is a popular open-source terminal AI coding assistant with native MCP support.

### Option A: Using the `/mcp add` Slash Command (Fastest)

Inside OpenCode / `omp`, run the `/mcp add` command directly in the chat prompt:

```text
/mcp add <name> [--scope project|user] [--url <url> --transport http|sse] [--token <token>] [-- <command...>]
```

**Examples:**

- **Local app without authentication (`RequireAuth = false`):**
  ```text
  /mcp add local-app --url http://localhost:5000/chat/mcp --transport http
  ```

- **Local app with API Key authentication:**
  ```text
  /mcp add local-app --url http://localhost:5000/chat/mcp --transport http --token ak-your-api-key
  ```

- **Project-scoped MCP configuration (stored in current project repo):**
  ```text
  /mcp add local-app --scope project --url http://localhost:5000/chat/mcp --transport http
  ```

### Option B: JSON Configuration File

You can configure MCP globally in `~/.config/opencode/opencode.json` (or `~/.config/omp/omp.json`) or locally in your project repository's `opencode.json`:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "local-app": {
      "type": "remote",
      "url": "http://localhost:5000/chat/mcp",
      "enabled": true,
      "oauth": false,
      "headers": {
        "Authorization": "Bearer ak-example-dev-key"
      }
    }
  }
}
```

> **Note:** If `RequireAuth = false` in your app, you can omit the `headers` / `Authorization` block.

---

## 4. Configuring Other AI Assistants

### Claude (Claude Code CLI & Claude Desktop)

#### Claude Code (CLI)
Add the MCP endpoint with the `claude mcp add` command:

```bash
# Without auth:
claude mcp add --transport http local-app http://localhost:5000/chat/mcp

# With API key:
claude mcp add --transport http local-app http://localhost:5000/chat/mcp \
  --header "Authorization: Bearer ak-example-dev-key"
```

#### Claude Desktop
Add to your `claude_desktop_config.json` (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS or `%APPDATA%\Claude\claude_desktop_config.json` on Windows):

```json
{
  "mcpServers": {
    "local-app": {
      "url": "http://localhost:5000/chat/mcp",
      "headers": {
        "Authorization": "Bearer ak-example-dev-key"
      }
    }
  }
}
```

---

### ChatGPT (Desktop & Developer MCP)

In ChatGPT Desktop (with Developer Mode / MCP enabled) or in your workspace MCP configuration (`~/.chatgpt/mcp.json`):

```json
{
  "mcpServers": {
    "local-app": {
      "url": "http://localhost:5000/chat/mcp",
      "headers": {
        "Authorization": "Bearer ak-example-dev-key"
      }
    }
  }
}
```

---

### Google Antigravity (AGY CLI & Antigravity IDE)

Google Antigravity supports remote HTTP MCP servers natively.

#### Option A: CLI Configuration
Add the server using the `agy` CLI:

```bash
agy mcp add local-app http://localhost:5000/chat/mcp --header "Authorization: Bearer ak-example-dev-key"
```

#### Option B: Workspace or Global Config (`.gemini/config/mcp.json` or `antigravity.json`)
Add `local-app` to your `mcpServers` block:

```json
{
  "mcpServers": {
    "local-app": {
      "url": "http://localhost:5000/chat/mcp",
      "transport": "http",
      "headers": {
        "Authorization": "Bearer ak-example-dev-key"
      }
    }
  }
}
```

---

### Cursor IDE
1. Open **Cursor Settings** $\rightarrow$ **Features** $\rightarrow$ **MCP**.
2. Click **+ Add New MCP Server**.
3. Configure:
   - **Name**: `local-app`
   - **Type**: `Remote` (or `HTTP`)
   - **URL**: `http://localhost:5000/chat/mcp`
   - **Headers**: `{"Authorization": "Bearer ak-example-dev-key"}`

---

### VS Code (GitHub Copilot / Cline / Roo Code)
In your workspace `.vscode/mcp.json` (or extension settings):

```json
{
  "mcpServers": {
    "local-app": {
      "url": "http://localhost:5000/chat/mcp",
      "headers": {
        "Authorization": "Bearer ak-example-dev-key"
      }
    }
  }
}
```

---

## 5. Testing & Verification Walkthrough

Once your assistant is connected, you can interact with your backend using natural language.

### Test 1: Read-Only Discovery & Query (Immediate Execution)

Ask the assistant to query live data:

> **You:** *"What drinks and sizes are available on the coffee shop menu?"*

#### What happens behind the scenes:
1. OpenCode calls `api_search({"query": "coffee menu drinks"})`.
2. Server returns matches: `GetCoffeeShopMenu`, `CreateCoffeeShopOrder`, etc.
3. OpenCode calls `api_describe({"names": ["GetCoffeeShopMenu"]})`.
4. OpenCode calls `api_call({"name": "GetCoffeeShopMenu", "args": {}})`.
5. Because `GetCoffeeShopMenu` implements `IGet` (`ToolSafety.ReadOnly`), the server executes it **immediately** and returns the menu JSON.

> **OpenCode:** *"The menu features Lattes ($4.50), Cappuccinos ($4.00), and Cold Brews ($3.75) available in Tall, Grande, and Venti sizes with Whole, Oat, or Almond milk options."*

---

### Test 2: Mutating Write API (Two-Phase Confirmation Flow)

Ask the assistant to perform a write operation:

> **You:** *"Order two hot Grande Oat Milk Lattes with light vanilla syrup for Sam."*

#### What happens behind the scenes:

1. **Discovery & Validation**:
   - OpenCode inspects `GetCoffeeShopMenu` to resolve `ProductId: 1` (Latte), `Size: "Grande"`, and `Milk: "Oat"`.
2. **Phase 1 (Proposal / Dry Run)**:
   - OpenCode calls `api_call`:
     ```json
     {
       "name": "CreateCoffeeShopOrder",
       "args": {
         "CustomerName": "Sam",
         "Items": [{ "ProductId": 1, "Quantity": 2, "Size": "Grande" }]
       }
     }
     ```
   - The server identifies that `CreateCoffeeShopOrder` is a `Write` operation and detects that no `confirmationToken` was provided.
   - The server creates an HMAC-signed token bound to `CustomerName: "Sam"` and the arguments hash, and returns:
     ```json
     {
       "status": "requires_confirmation",
       "api": "CreateCoffeeShopOrder",
       "safety": "Write",
       "confirmationToken": "mcp_cf_eyJhbGciOiJIUzI1Ni...",
       "summary": "Create validated coffee shop order for Sam",
       "instruction": "Display this summary to the user for explicit confirmation..."
     }
     ```
3. **Conversational Confirmation**:
   - OpenCode reads the `requires_confirmation` response and presents the prompt to you in chat:

   > **OpenCode:** *"I have prepared the order for Sam: 2x Grande Oat Milk Latte ($11.00 total). Would you like me to submit this order?"*

4. **Phase 2 (User Approval & Commit)**:
   - You reply:

   > **You:** *"Yes, place the order."*

   - OpenCode re-invokes `api_call` with the exact same arguments and the `confirmationToken`:
     ```json
     {
       "name": "CreateCoffeeShopOrder",
       "args": {
         "CustomerName": "Sam",
         "Items": [{ "ProductId": 1, "Quantity": 2, "Size": "Grande" }]
       },
       "confirmationToken": "mcp_cf_eyJhbGciOiJIUzI1Ni..."
     }
     ```
   - The server validates the cryptographic signature, verifies that arguments have not been altered, marks the single-use token as consumed, and executes `CreateCoffeeShopOrder` in-process.

   > **OpenCode:** *"Order #1042 has been successfully placed for Sam! Estimated pickup time is 10 minutes."*

---

## 6. Troubleshooting

### 1. HTTP 401 Unauthorized
- **Cause**: `RequireAuth = true` is enabled on `ChatFeature`, but the assistant did not supply a valid Bearer API key.
- **Fix**: Create an API key in your ServiceStack app (or sign in as Admin), and provide `"Authorization": "Bearer ak-..."` in `opencode.json` / client settings. For local development, you can temporarily set `RequireAuth = false`.

### 2. HTTP 405 Method Not Allowed
- **Cause**: Some older MCP clients attempt to open an SSE stream via `GET /chat/mcp`.
- **Fix**: ServiceStack implements the modern **stateless MCP Streamable HTTP (JSON-RPC POST)** transport. Ensure your client is configured for Streamable HTTP or remote HTTP POST.

### 3. "API not found or not available to you"
- **Cause**: The API's tag is not listed in `ApiTools.IncludeTags`, or the user lacks the required Role/Permission.
- **Fix**: Check `IncludeTags` in `ConfigureAi.cs` or decorate your Request DTO with `[Tag("CoffeeShop")]` or `[Tool]`.

### 4. "Arguments have been modified since confirmation was issued"
- **Cause**: The model altered parameter values between the preview step and the final execution step.
- **Fix**: The server's parameter tamper protection detected an argument hash mismatch. The assistant will simply generate a new proposal with the updated values and ask you to confirm the new values.

### 5. The assistant submitted a Write/Destructive API without asking me first

The server-side Two-Phase Confirmation token still guarantees the call cannot be replayed or tampered with — but MCP has **no protocol-level "ask user first" gate**. Whether a client actually pauses on a `requires_confirmation` response is a policy decision made by the *assistant* (or the *client app* hosting it). Some clients (and some models within those clients) will "auto-approve" their own preview turn and immediately re-invoke `api_call` with the token in the same assistant turn, without ever showing you the summary.

If that happens:

#### Step 1 — Switch your MCP client to a pessimistic "Ask for approval" setting

Almost every MCP client ships with a per-tool permission mode. Set it so that **every** `api_call` invocation requires an explicit human click/keypress before it's sent to the server. The token flow then becomes a *server-side* double-check on top of a *client-side* human gate.

Common examples (names change often — check your client's current docs):

- **Claude Code (CLI)**: run `/permissions` and set `api_call` (or the whole `local-app` MCP server) to `ask` instead of `allow`.
- **Cursor**: **Settings → MCP → Auto-run tools** → disable it for `local-app`, or set the server's approval mode to *Ask every time*.
- **VS Code (GitHub Copilot / MCP)**: in `.vscode/mcp.json` (or the MCP settings UI) set `"trust": "ask"` on the server; disable *Auto-approve*.
- **OpenCode / `omp`**: in `opencode.json` set `"permission": { "tool": { "local-app*": "ask" } }`.
- **ZCode**: switch the MCP server's tool permissions to **"Ask before changes"** (or the equivalent per-tool *Ask* setting) rather than *Allow*.
- **Claude Desktop / ChatGPT Desktop / Antigravity**: enable *Confirm before running tools* / *Ask for each tool call* in the client's MCP preferences.

> **Tip:** even on the strictest client setting, some assistants will still auto-approve *read-only* calls (`api_search`, `api_describe`, and `IGet` APIs) so you only get prompted on writes. That's the intended behavior.

#### Step 2 — If it still doesn't ask, give the MCP endpoint an imperative hint on the DTO

Some models ignore polite phrasing in a tool description and interpret their own "preview" turn as user confirmation. You can strengthen the per-DTO hint the MCP client sees by adding a `[Mcp(Description = "...")]` attribute on the Request DTO. It's an **MCP-only** override — the regular `[Description]` (used by OpenAPI, admin UIs, and the internal chat) stays clean, but MCP responses (`api_describe`, the `requires_confirmation` summary) use the stricter wording.

Use imperative, RFC-2119-style language (MUST / WAIT / does NOT count) and be explicit that the assistant's own reasoning is not a substitute for user input:

```csharp
using ServiceStack;
using ServiceStack.AI;

[Tag("CoffeeShop")]
[Description("Submits and charges a coffee shop order. Product names and prices are always resolved from the database.")]
[Mcp(Description =
    """
    Submits and charges a coffee shop order. Product names and prices are always resolved from the database.
    IMPORTANT: Before calling this API you MUST first call PreviewCoffeeShopOrder, present the itemized summary and total price to the human customer verbatim, and WAIT for their explicit natural-language confirmation of both the items and the total in a subsequent user turn. 
    Your own preview or reasoning does NOT count as customer confirmation. Do not place the order on the customer's behalf.   
    """)]
[Tool("the user has finished choosing an order and wants to place or submit it. Always confirm the itemized order and total price with the customer before calling this — never auto-submit",
    Safety = ToolSafety.Write, RequiresApproval = true, Keywords = ["buy", "checkout", "place order"], 
    Prerequisites = [
        nameof(GetCoffeeShopMenu), 
        nameof(PreviewCoffeeShopOrder)], 
    Preview = nameof(PreviewCoffeeShopOrder), 
    FollowUps = [nameof(GetCoffeeShopOrder)], 
    Aliases = ["PlaceCoffeeShopOrder"], 
    Examples = ["{\"customerName\":\"Sam\",\"items\":[{\"productId\":5,\"quantity\":1,\"size\":\"Grande\",\"temperature\":\"Hot\",\"options\":[{\"type\":\"Milks\",\"name\":\"Oat Milk\"}]}]}"])]
[Route("/coffee-shop/orders", "POST")]
public class CreateCoffeeShopOrder : IPost, IReturn<CreateCoffeeShopOrderResponse> { /*...*/ }
```

Guidelines that work well in practice:

- Mention the **name of the preview / read API** the assistant must call first (e.g. `PreviewCoffeeShopOrder`) so the model has a concrete prerequisite.
- Spell out **what** must be confirmed (items, quantity, total price, destination account, etc.) — vague "confirm with the user" is often ignored.
- Explicitly say the assistant's own preview/reasoning **does not** count as user confirmation.
- For destructive APIs, add "this action cannot be undone" — many models are trained to pause on that phrasing.

> **Reality check:** none of this can *force* a non-compliant assistant to pause — MCP is advisory. Combining a strict client permission setting (Step 1) with a firm per-DTO `[Mcp]` hint (Step 2) is the most reliable configuration today. The server-side token continues to guarantee that whatever *is* submitted is exactly what the assistant claimed to preview.

---

## 7. Summary of MCP Configuration Options

| Setting in `ChatFeature.Mcp` | Type              | Default             | Description                                                                            |
|:-----------------------------|:------------------|:--------------------|:---------------------------------------------------------------------------------------|
| `ToolGroups`                 | `List<string>`    | `["api_tools"]`     | Tool groups exposed to external MCP clients.                                           |
| `ApprovalMode`               | `McpApprovalMode` | `ConfirmationToken` | `ConfirmationToken` (Two-Phase prompt), `Reject` (fail-closed), or `DelegateToClient`. |
| `ConfirmationTokenExpiry`    | `TimeSpan`        | `5 minutes`         | Lifetime of generated confirmation tokens.                                             |
| `ServerName`                 | `string`          | App Name            | Name reported in MCP `initialize`.                                                     |
| `Instructions`               | `string`          | Automatic           | System prompt instructions supplied to MCP clients during initialization.              |
