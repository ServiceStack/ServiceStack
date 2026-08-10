# ServiceStack API Tools for AI Assistants

ServiceStack API Tools let an AI assistant discover, understand, and call an application's existing ServiceStack APIs. Applications do not need to create a separate set of AI-specific endpoints. The same typed Request DTOs, validation rules, authorization requirements, service implementations, and response DTOs used by human clients become available to AI agents.

API Tools are used by the built-in AI.Chat interface and can also be exposed to external assistants through the built-in MCP server described in [MCP.md](MCP.md).

## Purpose

An application may contain hundreds of APIs. Publishing every API as an individual model tool would consume a large context window before the user asks a question. API Tools instead expose three stable tools:

- `api_search` finds relevant APIs using compact metadata.
- `api_describe` loads complete schemas only for APIs the assistant intends to use.
- `api_call` invokes one selected API as the current user.

This search, describe, call sequence keeps the initial tool context small while allowing the assistant to use the application's full opted-in API surface.

## Assistant operating procedure

When API Tools are available, follow this sequence:

1. Call `api_search` using words from the user's request. Do not guess an API name.
2. Call `api_describe` for the likely APIs, including prerequisite, preview, and final write APIs when applicable.
3. Read `inputSchema`, required properties, validation metadata, examples, safety, prerequisites, preview, and follow-ups.
4. Call prerequisite lookup APIs to resolve current IDs, allowed values, prices, or state. Never invent database IDs.
5. Prefer a declared `preview` API before a write. Use its normalized response as the basis for the final request.
6. Call `api_call` with the exact API name and an `args` object matching the described schema.
7. If approval is requested, wait for the user. Do not claim the operation completed until the approved call returns success.
8. Use declared follow-up APIs when the user needs the created resource, status, or next workflow step.

Search again with broader user vocabulary or an available tag when no API matches.

## Enabling API Tools

```csharp
services.AddPlugin(new ChatFeature
{
    Tools =
    {
        EnableApiTools = true,
    },
    ApiTools =
    {
        IncludeTags = ["CoffeeShop"],
        IncludeTypes = ["GetSpecialReport"],
        ExcludeTypes = ["InternalMaintenance"],
        DefaultTake = 25,
        MaxTake = 100,
        MaxResultLength = 32 * 1024,
    },
});
```

An API is exposed when any of these conditions applies:

- Its Request DTO has `[Tool]`.
- Its `[Tag]` is listed in `ApiTools.IncludeTags`.
- Its Request DTO name is listed in `ApiTools.IncludeTypes`.

`ExcludeTypes` always wins. APIs excluded from ServiceStack metadata or restricted with `[Restrict]` are not exposed.

## Describing an AI-ready API

Existing ServiceStack metadata remains the source of truth. Add `[Tool]` only for agent-specific selection and workflow hints.

```csharp
[Tag("CoffeeShop")]
[Description("Submits a validated coffee shop order")]
[Tool(
    "the user has finished choosing an order and wants to place it",
    Safety = ToolSafety.Write,
    RequiresApproval = true,
    Keywords = ["buy", "checkout", "place order"],
    Prerequisites = [nameof(GetCoffeeShopMenu)],
    Preview = nameof(PreviewCoffeeShopOrder),
    FollowUps = [nameof(GetCoffeeShopOrder)],
    Aliases = ["PlaceCoffeeShopOrder"],
    Examples = ["""{"customerName":"Sam","items":[{"productId":7,"quantity":2}]}"""])]
[Route("/coffee-shop/orders", "POST")]
public class CreateCoffeeShopOrder : IPost, IReturn<CreateCoffeeShopOrderResponse>
{
    [Description("Name to put on the order")]
    [ValidateNotEmpty]
    public string CustomerName { get; set; } = "";

    [Description("Final order items")]
    [ValidateNotEmpty]
    public List<OrderItemRequest> Items { get; set; } = [];
}
```

Useful metadata includes:

- `[Description]`, `[Notes]`, and `[ApiMember]` for API and property meaning.
- `[Validate*]`, required fields, enums, and `[ApiAllowableValues]` for valid input.
- `[Tag]` for API grouping and bulk exposure.
- `[Input]`, `[Ref]`, and other UI metadata for schema-driven approval forms and lookups.
- `IReturn<T>` or the registered response type for the output schema.

### `ToolAttribute` features

| Property | Meaning |
| --- | --- |
| `WhenToUse` | User situation in which the assistant should select the API. The positional constructor argument sets this value. |
| `Name` | Stable tool-facing API name. Defaults to the Request DTO name. |
| `Keywords` | Additional user vocabulary used by search. |
| `Aliases` | Alternative names that participate in search and resolve in `api_call`. |
| `Examples` | Realistic JSON request examples returned by `api_describe`. |
| `Prerequisites` | APIs normally called before this API. |
| `Preview` | Read-only API that validates or prices the proposed operation. |
| `FollowUps` | APIs commonly useful after success. |
| `Safety` | `Auto`, `ReadOnly`, `Write`, or `Destructive`. |
| `RequiresApproval` | Requires human approval even if the operation would otherwise run unattended. |
| `Fields` | Default response field projection for query APIs. |
| `Take` | Default row limit for query APIs. |
| `Group` | Tool group used to enable or disable related APIs together; defaults to the first API tag. |
| `Exclude` | Prevents exposure. |

## Tool contracts

### `api_search`

Input:

```json
{
  "query": "place coffee order",
  "tag": "CoffeeShop",
  "take": 20
}
```

Search considers API names, split CamelCase names, aliases, keywords, tags, when-to-use text, descriptions, and routes. Minor name typos are tolerated. Results are filtered to APIs the current caller can access.

Success returns structured API summaries:

```json
{
  "status": "success",
  "count": 2,
  "apis": [
    {
      "name": "CreateCoffeeShopOrder",
      "request": "CreateCoffeeShopOrder",
      "summary": "Submits a validated coffee shop order",
      "tags": ["CoffeeShop"],
      "safety": "write",
      "method": "POST",
      "route": "/coffee-shop/orders"
    }
  ],
  "next": "Call api_describe with the names of the APIs you intend to use"
}
```

A no-match result includes `availableTags`, `suggestedApis`, and a recovery instruction.

### `api_describe`

Input:

```json
{
  "names": [
    "GetCoffeeShopMenu",
    "PreviewCoffeeShopOrder",
    "CreateCoffeeShopOrder"
  ]
}
```

Each returned API includes:

- The complete ServiceStack metadata schema at the root, retained for schema-form compatibility.
- `inputSchema`, a copy of the request schema.
- `outputSchema` when the response type is known.
- `tool.name`, `tool.safety`, `tool.requiresApproval`, `tool.whenToUse`, and examples.
- `prerequisites`, `preview`, and `followUps` when declared.
- Route, method, validation, descriptions, and UI metadata.

If an API does not exist or is unavailable to the caller, its entry contains an error instead of revealing inaccessible schema.

### `api_call`

Input:

```json
{
  "name": "PreviewCoffeeShopOrder",
  "args": {
    "CustomerName": "Sam",
    "Items": [
      {
        "ProductId": 7,
        "Quantity": 2,
        "Size": "Grande",
        "Temperature": "Hot",
        "Options": [
          { "Type": "Milks", "Name": "Oat Milk" },
          { "Type": "Syrups", "Name": "Vanilla Syrup", "Quantity": "light" }
        ]
      }
    ]
  }
}
```

Successful calls return:

```json
{
  "status": "success",
  "api": "PreviewCoffeeShopOrder",
  "request": {},
  "response": {},
  "truncated": false,
  "next": ["CreateCoffeeShopOrder"]
}
```

The request is deserialized into the real Request DTO and sent through ServiceStack's in-process Service Gateway. DTO validation, service logic, and database behavior are therefore shared with ordinary API clients.

Unknown argument fields are rejected before execution, including a nearest-field suggestion when possible. Standard ServiceStack validation remains authoritative for required values, ranges, and business rules.

## Authorization and caller identity

API Tools require an HTTP request to act on behalf of. They do not execute as an unrestricted application service account.

Discovery and execution enforce the caller's:

- Authentication requirement
- API-key requirement
- Required and any-of roles
- Required and any-of permissions
- Required claims
- Required scopes

An inaccessible API is omitted from search, cannot be described, and cannot be called. The in-process gateway executes using the same request context after access is asserted.

## Safety and approvals

`ToolSafety.Auto` is inferred conservatively:

- `GET`, `HEAD`, and `OPTIONS` become `ReadOnly`.
- `DELETE` becomes `Destructive`.
- `POST`, `PUT`, `PATCH`, unknown verbs, and ambiguous handlers become `Write`.

Set safety explicitly when HTTP semantics do not describe the real consequence.

In interactive AI.Chat:

- Read-only APIs execute immediately unless `RequiresApproval` is set.
- Write and destructive APIs pause before execution.
- The user sees a schema-generated form containing the proposed Request DTO.
- The user may edit arguments, approve, or reject.
- Approved calls are executed durably and the result is returned to the assistant.
- If no approval coordinator is available, approval-requiring model calls fail closed.

Approval is a pre-execution decision. An assistant must not report success merely because it proposed a call.

MCP has a separate policy because an external client cannot display AI.Chat's approval form. See `RejectToolsRequiringApproval` in [MCP.md](MCP.md).

## Context and result limits

For `QueryBase` requests, API Tools apply a default `Take`, cap it at `MaxTake`, and optionally apply `[Tool(Fields)]` when the assistant did not specify fields. `MaxResultLength` truncates oversized serialized results.

Assistants should still narrow queries themselves:

- Filter before querying.
- Select only necessary fields.
- Request small pages.
- Summarize or aggregate in purpose-built APIs instead of retrieving every row.
- Treat `truncated: true` as a signal to make a narrower call, not as complete data.

## Complete CoffeeShop workflow

For “Order two grande hot oat milk lattes with light vanilla syrup for Sam”:

1. `api_search({"query":"order oat milk latte vanilla syrup"})`
2. `api_describe` the menu, preview, and create APIs.
3. `api_call(GetCoffeeShopMenu)` to resolve Latte to the current product ID and verify choices.
4. `api_call(PreviewCoffeeShopOrder)` to validate and price the normalized order.
5. `api_call(CreateCoffeeShopOrder)` with the validated arguments.
6. In AI.Chat, wait for the editable approval form and user approval.
7. Report the returned order number only after the approved call succeeds.

This pattern generalizes to purchasing, bookings, ticket creation, deployments, messaging, and other workflows where current data must be resolved before a consequential operation.

## Failure guidance for assistants

- **No match:** broaden the vocabulary, remove the tag, or use `availableTags` and `suggestedApis`.
- **Not found or unavailable:** do not retry by guessing; search as the current user.
- **Unknown field:** correct it using the suggestion and the described schema.
- **Validation error:** preserve the user's intent, repair only invalid values, and preview again.
- **Approval required:** wait for the user in AI.Chat; in MCP, follow the server's configured approval policy.
- **Truncated result:** add filters, fields, or pagination.
- **No authenticated request:** API Tools cannot safely act in that execution context.

Never fabricate a successful mutation, identifier, total, or status when a tool call failed or remains pending approval.
