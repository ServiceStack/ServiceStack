# MCP Two-Phase Confirmation Architecture & Implementation Plan

> **Specification & Design Guide**: Server-Enforced Two-Phase "Dry-Run & Confirmation Token" Approval Pattern for ServiceStack AI.Chat MCP Server.

---

## 1. Overview & Problem Statement

### The Problem
When external AI assistants (e.g., Cursor, Claude Code, OpenCode, VS Code) interact with ServiceStack tools over Model Context Protocol (MCP) Streamable HTTP, mutating operations (`ToolSafety.Write` and `ToolSafety.Destructive` or APIs marked with `[Tool(RequiresApproval = true)]`) cannot render ServiceStack's rich interactive Vue/HTML approval forms.

Currently, hosts choose between two extremes:
1. `RejectToolsRequiringApproval = true` (Default): Fails closed and completely blocks the AI assistant from executing write operations.
2. `RejectToolsRequiringApproval = false`: Blindly executes write operations without server-side verification, delegating safety entirely to whether the external client chooses to prompt the user.

#### The Solution: Two-Phase Confirmation Token Pattern
The **Two-Phase Confirmation Token Pattern** is the de facto industry standard used by enterprise MCP servers (e.g., Stripe, GitHub, Cloudflare, Kubernetes) for stateless HTTP transports. 

It guarantees safety on the server while allowing conversational AI assistants to naturally present human-in-the-loop confirmations within their existing chat interface:
1. **Phase 1 (Dry-Run / Proposal):** Calling a mutating API without a valid confirmation token returns a structured `requires_confirmation` response containing a summary, argument payload, and a cryptographically signed, short-lived `confirmationToken`.
2. **Conversation Turn:** The AI assistant displays the action summary and parameters to the user in chat (e.g., *"Would you like me to submit this order for Sam ($11.00)?"*).
3. **Phase 2 (Verified Execution):** When the user approves, the assistant re-invokes the tool with the `confirmationToken`. The server cryptographically validates the token against the user identity, API name, and argument hash before executing the mutation in-process.

> [!IMPORTANT]
> **Preservation of Built-in AI.Chat Approval System**:
> This confirmation token system is scoped specifically to the external **MCP Server boundary (`/chat/mcp`)**.
> The built-in **AI.Chat Assistant approval pipeline** (`ApiToolApprovalCoordinator`, interactive `ApiApprovalForm.mjs`, user argument edits via `effectiveArgs`, thread pausing, and continuation) operates independently and remains 100% untouched.

### 1.1 Coexistence of Dual Approval Systems

ServiceStack maintains two distinct, non-interfering approval pipelines optimized for their respective user interaction models:

```
                                  ┌────────────────────────────────────────────────────────┐
                                  │               ServiceStack AI Capability               │
                                  └───────────────────────────┬────────────────────────────┘
                                                              │
                             ┌────────────────────────────────┴────────────────────────────────┐
                             │                                                                 │
                             ▼                                                                 ▼
             ┌───────────────────────────────┐                                 ┌───────────────────────────────┐
             │       BUILT-IN AI.CHAT        │                                 │     EXTERNAL MCP ASSISTANTS   │
             │   (Browser UI at /chat)       │                                 │  (Cursor, Claude Code, etc.)  │
             └───────────────┬───────────────┘                                 └───────────────┬───────────────┘
                             │                                                                 │
                     [ChatOrchestrator]                                                 [McpExtension]
                             │                                                                 │
                 [tool.ApprovalHandler]                                                [api_call Tool]
                             │                                                                 │
                             ▼                                                                 ▼
             ┌───────────────────────────────┐                                 ┌───────────────────────────────┐
             │   Interactive Form Approval   │                                 │    Two-Phase Token Approval   │
             │                               │                                 │                               │
             │ • Durable Thread Pauses       │                                 │ • Stateless JSON-RPC POST     │
             │ • Renders ApiApprovalForm.mjs │                                 │ • returns requires_confirm    │
             │ • User edits effectiveArgs    │                                 │ • Signed confirmationToken    │
             │ • Resumes thread on submit    │                                 │ • Re-invoke to execute        │
             └───────────────┬───────────────┘                                 └───────────────┬───────────────┘
                             │                                                                 │
                             └────────────────────────────────┬────────────────────────────────┘
                                                              │
                                                              ▼
                                              ┌───────────────────────────────┐
                                              │      Service Gateway API      │
                                              │  (Execute validated DTO)      │
                                              └───────────────────────────────┘
```

1. **Path A: Built-in Interactive AI.Chat UI (`/chat`)**:
   - Uses `ChatOrchestrator` and `ApiToolApprovalCoordinator`.
   - Pauses the thread durably in the database (`ChatToolApprovalBatch` and `ChatToolApproval`).
   - Renders the Vue/HTML `ApiApprovalForm.mjs` modal.
   - Allows users to interactively inspect and modify request parameters (`effectiveArgs`) before execution.
   - When the user clicks "Approve", the server executes the modified arguments and resumes the thread stream seamlessly.

2. **Path B: External MCP Streamable HTTP (`/chat/mcp`)**:
   - Uses `McpExtension` (stateless JSON-RPC POST).
   - Generates and verifies HMAC-SHA256 `confirmationToken` strings across conversational turns.
   - Requires zero client-side UI infrastructure while guaranteeing server-side validation.

```
┌─────────────────┐             ┌─────────────────────────┐             ┌───────────────┐
│   AI Assistant  │             │   ServiceStack MCP      │             │     User      │
│  (Cursor/Claude)│             │      (/chat/mcp)        │             │  (Chat/CLI)   │
└────────┬────────┘             └────────────┬────────────┘             └───────┬───────┘
         │                                   │                                  │
         │ 1. api_call(CreateOrder, args)    │                                  │
         │    [No confirmation token]        │                                  │
         ├──────────────────────────────────►│                                  │
         │                                   │ 2. Evaluate Safety (Write)       │
         │                                   │    Generate Confirmation Token   │
         │ 3. status: requires_confirmation  │                                  │
         │    confirmationToken: "mcp_cf_.." │                                  │
         │    summary: "Create Order..."     │                                  │
         │◄──────────────────────────────────┤                                  │
         │                                   │                                  │
         │ 4. "Please confirm: Would you like to create an order for Sam?"      │
         ├─────────────────────────────────────────────────────────────────────►│
         │                                   │                                  │
         │ 5. "Yes, proceed"                 │                                  │
         │◄─────────────────────────────────────────────────────────────────────┤
         │                                   │                                  │
         │ 6. api_call(CreateOrder, args,    │                                  │
         │             confirmationToken)    │                                  │
         ├──────────────────────────────────►│                                  │
         │                                   │ 7. Validate Signature, Expiry,   │
         │                                   │    User Identity, & Payload Hash │
         │                                   │    Execute in-process Gateway    │
         │ 8. status: success                │                                  │
         │    response: { OrderId: 1042 }    │                                  │
         │◄──────────────────────────────────┤                                  │
         │                                   │                                  │
         │ 9. "Order #1042 has been created" │                                  │
         ├─────────────────────────────────────────────────────────────────────►│
```

---

## 2. Safety Classification & Trigger Conditions

Confirmation tokens are **only required** for operations that alter state or require human verification. Read operations execute immediately.

### Automatic Safety Determination

| API / DTO Classification                                 | HTTP Verb     | Inferred `ToolSafety`   | Confirmation Token Required?  |
|:---------------------------------------------------------|:--------------|:------------------------|:------------------------------|
| Implements `IGet`                                        | `GET`         | `ReadOnly`              | **No** (Executes immediately) |
| Inherits `QueryBase`, `QueryDb<>`, `QueryData<>`         | `GET`         | `ReadOnly`              | **No** (Executes immediately) |
| Route with single verb `"GET"`, `"HEAD"`, `"OPTIONS"`    | `GET`         | `ReadOnly`              | **No** (Executes immediately) |
| Implements `ICreateDb<>`, `ISaveDb<>`, `IPost`           | `POST`        | `Write`                 | **Yes**                       |
| Implements `IUpdateDb<>`, `IPut`, `IPatchDb<>`, `IPatch` | `PUT`/`PATCH` | `Write`                 | **Yes**                       |
| Implements `IDeleteDb<>`, `IDelete`                      | `DELETE`      | `Destructive`           | **Yes**                       |
| Ambiguous / multiple verbs without `[Tool]`              | `POST`        | `Write` (Safe fallback) | **Yes**                       |
| Explicit `[Tool(Safety = ToolSafety.ReadOnly)]`          | *Any*         | `ReadOnly`              | **No**                        |
| Explicit `[Tool(RequiresApproval = true)]`               | *Any*         | *Any*                   | **Yes**                       |

---

## 3. Technical Design of Confirmation Tokens

### 3.1 Token Structure & Cryptography

Confirmation tokens are stateless, tamper-proof, HMAC-SHA256 signed tokens prefixed with `mcp_cf_`:

```text
mcp_cf_{Base64Url(Header)}.{Base64Url(Payload)}.{Base64Url(Signature)}
```

#### Token Header
```json
{
  "alg": "HS256",
  "typ": "MCP+CONFIRM"
}
```

#### Token Payload
```json
{
  "sub": "user_or_apikey_id",
  "tool": "api_call",
  "target": "CreateCoffeeShopOrder",
  "args_hash": "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e",
  "iat": 1723974600,
  "exp": 1723974900,
  "jti": "d3b07384-d113-49d6-848e-d98c39e2467d"
}
```

### 3.2 Payload Fields & Security Invariants

| Field       | Type     | Purpose                                                                       | Security Guarantee                                                                                                                                                           |
|:------------|:---------|:------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `sub`       | `string` | Current authenticated caller (`req.UserName` or API Key ID).                  | Prevents user $A$ from confirming an action generated by or intended for user $B$.                                                                                           |
| `tool`      | `string` | Invoked MCP tool name (e.g., `api_call`).                                     | Binds the token to the specific tool invocation.                                                                                                                             |
| `target`    | `string` | Target API Request DTO name (e.g., `CreateCoffeeShopOrder`).                  | Prevents substituting a destructive operation for an innocuous one.                                                                                                          |
| `args_hash` | `string` | SHA-256 hash of canonicalized JSON argument object.                           | **Parameter Tamper Protection**: Prevents prompt injection or model hallucination from altering parameters (e.g. changing quantity or recipient) between preview and commit. |
| `iat`       | `long`   | Epoch timestamp of token creation.                                            | Tracks token issue time.                                                                                                                                                     |
| `exp`       | `long`   | Expiration timestamp (`iat + Config.ConfirmationTokenExpiry`, default 5 min). | Limits attack window. Expired tokens are rejected.                                                                                                                           |
| `jti`       | `string` | Unique cryptographically random UUID.                                         | **Replay Protection**: Single-use token. Once executed, `jti` is marked consumed.                                                                                            |

### 3.3 Argument Canonicalization & Hash Generation

To prevent JSON property re-ordering from invalidating signatures, argument JSON is normalized prior to hashing:

```csharp
public static string ComputeArgumentsHash(JsonObject? args)
{
    if (args == null || args.Count == 0)
        return "";
    
    // Sort keys alphabetically, format consistently without whitespace
    var sorted = SortJsonObject(args);
    var canonicalJson = sorted.ToJson();
    return canonicalJson.Sha256();
}

private static JsonObject SortJsonObject(JsonObject obj)
{
    var sorted = new JsonObject();
    foreach (var kvp in obj.OrderBy(k => k.Key, StringComparer.Ordinal))
    {
        if (kvp.Value is JsonObject childObj)
            sorted[kvp.Key] = SortJsonObject(childObj);
        else
            sorted[kvp.Key] = kvp.Value?.DeepClone();
    }
    return sorted;
}
```

### 3.4 Single-Use Replay Protection

When a token is successfully verified and executed, its `jti` is placed in an in-memory sliding cache (or `ICacheClient`) with a TTL equal to the token's remaining lifetime:

```csharp
var cacheKey = $"urn:mcp:used:{payload.Jti}";
if (Cache.Get<bool>(cacheKey))
    throw new McpException(InvalidParams, "Confirmation token has already been used.");

Cache.Set(cacheKey, true, TimeSpan.FromSeconds(payload.Exp - DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
```

---

## 4. MCP Protocol & Schema Changes

### 4.1 Schema Updates for `api_call`

The `api_call` input schema is extended to accept an optional `confirmationToken`:

```json
{
  "name": "api_call",
  "description": "Calls a ServiceStack API as the current user. For write/destructive APIs, call once to receive a confirmationToken, then re-call with the token once confirmed.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "name": {
        "type": "string",
        "description": "Exact Request DTO name of the API to call"
      },
      "args": {
        "type": "object",
        "description": "Request arguments matching the API schema"
      },
      "confirmationToken": {
        "type": "string",
        "description": "Confirmation token returned by a previous requires_confirmation response for write/destructive operations."
      }
    },
    "required": ["name", "args"]
  }
}
```

### 4.2 Phase 1: `requires_confirmation` Response Payload

When a mutating API is called without a token, the MCP server returns a structured response:

```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"status\":\"requires_confirmation\",\"api\":\"CreateCoffeeShopOrder\",\"safety\":\"Write\",\"confirmationToken\":\"mcp_cf_eyJhbGciOiJIUzI1NiIsInR5cCI...\",\"expiresInSeconds\":300,\"summary\":\"Create validated coffee shop order for Sam: 2 items\",\"args\":{\"CustomerName\":\"Sam\",\"Items\":[{\"ProductId\":7,\"Quantity\":2}]},\"instruction\":\"Display this summary to the user for explicit confirmation. When approved, call api_call again with the same arguments and confirmationToken: 'mcp_cf_eyJhbGciOiJIUzI1NiIsInR5cCI...'\"}"
    }
  ],
  "structuredContent": {
    "status": "requires_confirmation",
    "api": "CreateCoffeeShopOrder",
    "safety": "Write",
    "confirmationToken": "mcp_cf_eyJhbGciOiJIUzI1NiIsInR5cCI...",
    "expiresInSeconds": 300,
    "summary": "Create validated coffee shop order for Sam: 2 items",
    "args": {
      "CustomerName": "Sam",
      "Items": [
        {
          "ProductId": 7,
          "Quantity": 2
        }
      ]
    },
    "instruction": "Display this summary to the user for explicit confirmation. When approved, call api_call again with the same arguments and confirmationToken: 'mcp_cf_eyJhbGciOiJIUzI1NiIsInR5cCI...'"
  }
}
```

### 4.3 Server Instructions in `initialize`

During MCP `initialize`, the server advertises its confirmation workflow in `instructions`:

```text
Use API Tools (api_search, api_describe, api_call) to interact with the application.
Read-only APIs execute immediately.
When calling Write or Destructive APIs via api_call, the server returns a 'requires_confirmation' status with a confirmationToken.
You MUST present the summary and arguments to the user to obtain explicit confirmation.
Once confirmed, re-invoke api_call passing the exact same arguments along with the provided confirmationToken.
```

---

## 5. Implementation Architecture

### 5.1 Configuration Options in `McpExtension`

```csharp
public enum McpApprovalMode
{
    /// <summary>
    /// Enforce Two-Phase Dry-Run & Confirmation Token on mutating APIs (Recommended).
    /// </summary>
    ConfirmationToken,

    /// <summary>
    /// Fail-closed: Reject all tools requiring approval with an error code.
    /// </summary>
    Reject,

    /// <summary>
    /// Delegate approval to MCP Client native confirmation dialogs (no server token checks).
    /// </summary>
    DelegateToClient,
}

public class McpExtension : ChatExtension
{
    /// <summary>
    /// Approval mode for operations requiring verification. Defaults to ConfirmationToken.
    /// </summary>
    public McpApprovalMode ApprovalMode { get; set; } = McpApprovalMode.ConfirmationToken;

    /// <summary>
    /// How long a generated confirmation token remains valid. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan ConfirmationTokenExpiry { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Signing key used for HMAC-SHA256 token signing. Defaults to HostContext.Config.AdminAuthSecret or AppHost key.
    /// </summary>
    public string? SigningSecret { get; set; }
}
```

### 5.2 Token Manager Implementation: `McpConfirmationTokenManager`

```csharp
public class McpConfirmationTokenManager
{
    private readonly byte[] secretBytes;
    private readonly TimeSpan expiry;
    private readonly ICacheClient cache;

    public McpConfirmationTokenManager(string secret, TimeSpan expiry, ICacheClient cache)
    {
        this.secretBytes = Encoding.UTF8.GetBytes(secret);
        this.expiry = expiry;
        this.cache = cache;
    }

    public string CreateToken(string user, string toolName, string targetApi, JsonObject? args)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new McpTokenPayload
        {
            Sub = user ?? "anonymous",
            Tool = toolName,
            Target = targetApi,
            ArgsHash = ComputeArgumentsHash(args),
            Iat = now.ToUnixTimeSeconds(),
            Exp = now.Add(expiry).ToUnixTimeSeconds(),
            Jti = Guid.NewGuid().ToString("N")
        };

        var headerJson = "{\"alg\":\"HS256\",\"typ\":\"MCP+CONFIRM\"}".ToBase64Url();
        var payloadJson = JsonSerializer.Serialize(payload).ToBase64Url();
        var unsigned = $"{headerJson}.{payloadJson}";
        var signature = Sign(unsigned, secretBytes).ToBase64Url();

        return $"mcp_cf_{unsigned}.{signature}";
    }

    public TokenValidationResult ValidateToken(
        string tokenString, string user, string toolName, string targetApi, JsonObject? currentArgs)
    {
        if (string.IsNullOrEmpty(tokenString) || !tokenString.StartsWith("mcp_cf_"))
            return TokenValidationResult.Failed("Invalid token format.");

        var raw = tokenString.Substring("mcp_cf_".Length);
        var parts = raw.Split('.');
        if (parts.Length != 3)
            return TokenValidationResult.Failed("Malformed confirmation token.");

        var unsigned = $"{parts[0]}.{parts[1]}";
        var expectedSig = Sign(unsigned, secretBytes).ToBase64Url();
        if (!CryptographicEquals(parts[2], expectedSig))
            return TokenValidationResult.Failed("Invalid token signature.");

        var payload = JsonSerializer.Deserialize<McpTokenPayload>(parts[1].FromBase64UrlString());
        if (payload == null)
            return TokenValidationResult.Failed("Invalid token payload.");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now > payload.Exp)
            return TokenValidationResult.Failed("Confirmation token has expired.");

        if (!string.Equals(payload.Sub, user ?? "anonymous", StringComparison.Ordinal))
            return TokenValidationResult.Failed("User mismatch for confirmation token.");

        if (!string.Equals(payload.Tool, toolName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payload.Target, targetApi, StringComparison.OrdinalIgnoreCase))
            return TokenValidationResult.Failed("Target operation mismatch for confirmation token.");

        var currentHash = ComputeArgumentsHash(currentArgs);
        if (!string.Equals(payload.ArgsHash, currentHash, StringComparison.Ordinal))
            return TokenValidationResult.Failed("Arguments have changed since confirmation was issued.");

        // Check & enforce single-use
        var cacheKey = $"urn:mcp:used:{payload.Jti}";
        if (cache.Get<bool>(cacheKey))
            return TokenValidationResult.Failed("Confirmation token has already been used.");

        cache.Set(cacheKey, true, TimeSpan.FromSeconds(Math.Max(1, payload.Exp - now)));
        return TokenValidationResult.Success();
    }
}
```

### 5.3 Execution Interception in `ApiToolsExtension.cs` / `McpExtension.cs`

In `CallToolAsync` for `api_call`:

```csharp
var apiName = args.GetString("name");
var apiArgs = args.GetObject("args") ?? new JsonObject();
var confirmationToken = args.GetString("confirmationToken");

var apiTool = registry.GetTool(apiName, req.Request);
if (apiTool == null)
    return ApiNotFoundError(apiName);

var requiresApproval = apiTool.Safety == ToolSafety.Write || 
                       apiTool.Safety == ToolSafety.Destructive || 
                       apiTool.RequiresApproval;

if (requiresApproval && approvalMode == McpApprovalMode.ConfirmationToken)
{
    if (string.IsNullOrEmpty(confirmationToken))
    {
        // Phase 1: Return structured confirmation request
        var token = tokenManager.CreateToken(req.UserName, "api_call", apiName, apiArgs);
        var summary = apiTool.Summary ?? $"Execute {apiName}";

        return CreateRequiresConfirmationResponse(
            api: apiName,
            safety: apiTool.Safety.ToString(),
            token: token,
            expirySeconds: (int)ConfirmationTokenExpiry.TotalSeconds,
            summary: summary,
            args: apiArgs);
    }

    // Phase 2: Validate token before execution
    var validation = tokenManager.ValidateToken(
        confirmationToken, req.UserName, "api_call", apiName, apiArgs);

    if (!validation.IsValid)
    {
        return CreateTokenErrorResponse(validation.ErrorMessage);
    }
}

// Token is valid or not required -> Execute normally via Service Gateway
return await ExecuteApiAsync(apiTool, apiArgs, req);
```

---

## 6. End-to-End Walkthrough: Placing a Coffee Order

### 1. User Prompt to Cursor / Claude Code
> *"Order 2 hot Grande Oat Milk Lattes for Sam."*

### 2. Assistant Discovers & Describes APIs
- Assistant calls `api_search({"query":"order latte coffee"})`.
- Assistant calls `api_describe({"names":["GetCoffeeShopMenu", "CreateCoffeeShopOrder"]})`.
- Assistant calls `api_call({"name":"GetCoffeeShopMenu","args":{}})` to resolve product IDs (`ProductId: 7`).

### 3. Assistant Proposes Write Operation
The assistant calls `api_call`:
```json
{
  "name": "api_call",
  "arguments": {
    "name": "CreateCoffeeShopOrder",
    "args": {
      "CustomerName": "Sam",
      "Items": [{ "ProductId": 7, "Quantity": 2, "Size": "Grande" }]
    }
  }
}
```

### 4. Server Returns `requires_confirmation`
```json
{
  "status": "requires_confirmation",
  "api": "CreateCoffeeShopOrder",
  "safety": "Write",
  "confirmationToken": "mcp_cf_eyJhbGciOiJIUzI1NiIsInR5cCI6Ik1DUCtDT05GSVJNIiw...",
  "expiresInSeconds": 300,
  "summary": "Submits a validated coffee shop order for Sam",
  "args": {
    "CustomerName": "Sam",
    "Items": [{ "ProductId": 7, "Quantity": 2, "Size": "Grande" }]
  },
  "instruction": "Display this summary to the user for explicit confirmation. When approved, call api_call again with the same arguments and confirmationToken."
}
```

### 5. Assistant Asks User in Chat
> **AI Assistant:** *"I've prepared your order:*
> - *Customer: Sam*
> - *Items: 2x Grande Oat Milk Latte*
> 
> *Please confirm: Would you like me to submit this order?"*

### 6. User Confirms
> **User:** *"Yes, place the order."*

### 7. Assistant Submits Confirmed Call
```json
{
  "name": "api_call",
  "arguments": {
    "name": "CreateCoffeeShopOrder",
    "args": {
      "CustomerName": "Sam",
      "Items": [{ "ProductId": 7, "Quantity": 2, "Size": "Grande" }]
    },
    "confirmationToken": "mcp_cf_eyJhbGciOiJIUzI1NiIsInR5cCI6Ik1DUCtDT05GSVJNIiw..."
  }
}
```

### 8. Server Validates & Executes
- Checks HMAC signature $\rightarrow$ Valid.
- Checks expiration ($< 5$ mins) $\rightarrow$ Valid.
- Checks user identity $\rightarrow$ Valid.
- Checks arguments hash against payload $\rightarrow$ Match.
- Checks single-use cache $\rightarrow$ Valid; marks `jti` as used.
- Executes `CreateCoffeeShopOrder` via Service Gateway.
- Returns `{ "status": "success", "response": { "OrderId": 1042, "Total": 11.00 } }`.

### 9. Assistant Delivers Final Result
> **AI Assistant:** *"Order #1042 has been successfully placed for Sam ($11.00)."*

---

## 7. Security Analysis & Threat Model

| Threat / Attack Vector                                                                        | Mitigation in Two-Phase Confirmation Design                                                                                  |
|:----------------------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------|
| **Argument Tampering / Injection**<br>*(e.g., User confirms $10, model alters args to $1000)* | The token payload embeds `args_hash` (SHA-256 of canonical arguments). Any change to parameters invalidates the token check. |
| **Token Replay Attack**<br>*(e.g., Resending the token to execute duplicate orders)*          | Every token has a unique `jti` stored in cache upon execution. Replays fail with `"token already used"`.                     |
| **Cross-User Token Theft**<br>*(e.g., User B intercepts User A's token)*                      | Token embeds `sub` (username / API key ID). Validated against active caller identity.                                        |
| **Cross-Action Token Swap**<br>*(e.g., Using a confirmed menu token for order deletion)*      | Token embeds `tool` and `target` API name. Validated before execution.                                                       |
| **Stale Confirmation**<br>*(e.g., Executing a token hours later when state changed)*          | Short 5-minute TTL (`exp`). Expired tokens are rejected.                                                                     |
| **Model Fabrication**<br>*(e.g., Model hallucinates a fake token)*                            | HMAC-SHA256 signature signed by server secret key. Cannot be forged without server secret.                                   |

---

## 8. Summary of Benefits

1. **Universal Client Support:** Works seamlessly across all MCP clients (Claude Code, Cursor, OpenCode, VS Code, Zed) without requiring specialized client-side UI extensions.
2. **Zero Breaking Changes:** Read-only APIs (`IGet`, `QueryBase`, `QueryDb<>`, `QueryData<>`) continue executing immediately with zero added latency.
3. **Guaranteed Server Safety:** The server remains the ultimate enforcement boundary; bypassing client permission dialogs cannot bypass server-side token validation.
4. **Natural User Experience:** AI assistants present clean, formatted natural language confirmation questions directly within the conversation stream.
