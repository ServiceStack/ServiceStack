# API rate limiting with ASP.NET Core

## Goal and scope

Add an opt-in way to attach Microsoft ASP.NET Core named rate-limiting policies to ServiceStack API endpoints. A request DTO can opt in with Microsoft's `[EnableRateLimiting("policy-name")]` or ServiceStack's dependency-free `[RateLimiting("policy-name")]`. An application can also apply one policy to **all operations carrying the same `[Tag]`**, so those APIs consume one shared policy budget. Microsoft middleware remains responsible for acquiring leases, queueing, rejecting requests, and recording its built-in metrics; ServiceStack supplies the operation-to-endpoint mapping.

The primary target is .NET 10. The relevant endpoint and rate-limiter APIs also exist in .NET 8, and ServiceStack already compiles its endpoint mapping under `NET8_0_OR_GREATER`. Include .NET 8 only if the same implementation and integration tests compile and pass without a separate code path. Do not add rate limiting to `net472` or `net6.0`.

The initial release covers HTTP requests executed through ASP.NET Core endpoint routing. It does not promise distributed counters across server instances, background job limits, non-HTTP transports, or transparent enforcement for ServiceStack's legacy request routing. Existing job-queue rate limits are a separate feature.

## Relevant code today

- `AppHostBase.NetCore.cs` maps explicit routes in `MapUserDefinedRoutes()` and invokes `ConfigureOperationEndpoint()` for each route builder. `ServiceStackOptions.RouteHandlerBuilders` can add endpoint conventions after basic endpoint configuration.
- `PredefinedRoutesFeature.cs` maps `/api/{Request}`, autobatch, and generic `{name}.{format}` routes. `AppHostBase.NetCore.cs` also maps explicit `.{format}` routes. Some variants do not currently call `RouteHandlerBuilders`; those gaps matter for enforcement.
- `ServiceMetadata.Add()` gathers request DTO `[Tag]` attributes into `Operation.Tags`. `[Tag]` allows multiple instances, so an operation may have multiple tags.
- `ServiceStackOptions.MapEndpoints()` enables endpoint mapping. Its default `force: true` disables ordinary ServiceStack service routing, which is needed to prevent a limited operation from being reachable through an unmetered legacy path.
- `ServiceStack.Kestrel/AppSelfHostBase.cs` duplicates part of the endpoint mapping logic. A shared helper should avoid different behavior between ASP.NET Core hosts and self-hosting.
- `UseServiceStack()` copies request DTO attributes into endpoint metadata on .NET 8+, including Microsoft rate-limiting attributes if a user applies them. The new feature must define how this interacts with configuration rather than silently stacking conflicting metadata.

## Public API and semantics

Keep policy **definitions** in the application's normal `services.AddRateLimiter(...)` call. Both attributes select a policy by name; neither defines a limiter. `ServiceStack.csproj` already has a `Microsoft.AspNetCore.App` framework reference, so the core integration needs no new package. DTOs compiled in an ASP.NET Core Web project can use Microsoft's attribute directly. A standalone DTO class library needs its own `Microsoft.AspNetCore.App` framework reference to compile that attribute. Add `ServiceStack.RateLimitingAttribute` to `ServiceStack.Interfaces` so shared DTO projects can opt in without that framework reference. Do not add a Microsoft framework reference to `ServiceStack.Interfaces`.

Add tag-wide policy **bindings** to `ServiceStackOptions` (names below are proposed):

```csharp
[Tag("orders")]
[RateLimiting("orders-per-user")] // Or [EnableRateLimiting("orders-per-user")] in ASP.NET Core DTO projects.
public class PlaceOrder : IPost, IReturn<PlaceOrderResponse> { /* ... */ }

[Tag("orders")]
public class ListOrders : IGet, IReturn<ListOrdersResponse> { /* ... */ }
```

Both operations above use `orders-per-user`: the attribute binds `PlaceOrder` directly, while the tag-wide registration binds `ListOrders`. The host still registers that one named policy with Microsoft middleware:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("orders-per-user", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.IsAuthenticated == true &&
                          httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value is { Length: > 0 } userId
                ? "user:" + userId
                : "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});

var app = builder.Build();
app.UseRouting();
app.UseAuthentication(); // Required when the policy reads HttpContext.User.
app.UseRateLimiter();     // After routing; before ServiceStack executes the API.
app.UseAuthorization();
app.UseServiceStack(new AppHost(), options =>
{
    options.MapEndpoints(); // Keep force:true for complete endpoint enforcement.
    options.RateLimitTag("orders", "orders-per-user");
});
```

`[Tag("orders")]` on several request DTOs binds the same named policy to every corresponding endpoint. The policy's partition factory determines whether the budget is global, per user, or otherwise partitioned. ServiceStack must **not** add the operation name to the partition key: doing so would give each operation a separate budget. Distinct tags can intentionally name the same policy and therefore share a budget too. DTOs can use either attribute without any tag registration. The ServiceStack attribute must have the same endpoint behavior as Microsoft's `EnableRateLimitingAttribute`: it selects/replaces the endpoint-specific policy while leaving a configured global limiter in effect.

Proposed binding rules:

1. Either request-DTO attribute wins over a tag binding. A programmatic operation binding is an optional override; if it and an attribute are both present, require the same policy name or fail startup. If both attributes are present, require equal policy names or fail startup, then apply the policy only once.
2. With no explicit binding, exactly one distinct policy name across the operation's matching tags applies. Several matching tags mapped to the *same* policy are valid.
3. If matching tags map to different policy names, fail startup with the operation name and conflicting tags/policies. One ASP.NET Core endpoint can have only one effective named endpoint policy; silent first-match selection would be fragile.
4. No match means no ServiceStack endpoint-specific rate limit. A separately configured ASP.NET Core global limiter still applies.
5. Match tag names exactly (`StringComparer.Ordinal`) to preserve existing `[Tag]` semantics. Reject empty tag/policy names and duplicate registrations with conflicting values.
6. Reject an empty policy name in either attribute. Respect a deliberately applied Microsoft `[DisableRateLimiting]` on a request DTO, but reject a conflicting attribute or programmatic binding at startup. Document that `DisableRateLimiting` also disables the global limiter, so it is a broad opt-out.

The initial API should expose only named-policy attachment. Developers can use any Microsoft fixed-window, sliding-window, token-bucket, concurrency, or custom `IRateLimiterPolicy` implementation without a ServiceStack-specific algorithm abstraction. A programmatic operation-type overload taking `Type` is useful for dynamically registered DTOs, but ordinary DTOs need no operation registration because of the attribute.

## Implementation steps

### 1. Add the portable attribute and resolve both DTO attributes

Add `ServiceStack.RateLimitingAttribute(string policyName)` to `ServiceStack.Interfaces`, with `AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)` and a read-only `PolicyName`. Keep it as plain metadata with no ASP.NET Core type references. In the core project, read it alongside `Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute` and `DisableRateLimitingAttribute` from request types. Add `RateLimitTag(string tag, string policyName)` to `ServiceStackOptions` and optionally `RateLimitOperation<TRequest>(string policyName)` plus a non-generic overload. Store immutable or startup-only dictionaries. Resolve a policy once for each `Operation` at endpoint registration, using its request type attributes, `Operation.Tags`, and any explicit operation binding; do not re-scan reflection on every request. Make conflicts explicit in one resolver, with unit tests.

Policy existence must be validated before serving requests. Inspect the registered Microsoft rate-limiter options after application services are available, or use a startup validation mechanism if their public options API does not expose names reliably. If validation cannot be done cleanly, verify this with an integration test and let the middleware's documented missing-policy startup error surface; do not maintain a second policy registry.

### 2. Apply the Microsoft endpoint convention

Create one internal helper such as `ConfigureRateLimiting(RouteHandlerBuilder, Operation)` and call `builder.RequireRateLimiting(policyName)` after normal operation endpoint configuration. Centralize resolution so the same operation receives the same policy on every HTTP method and route. Put the helper in the core project's `NET8_0_OR_GREATER` section. There is no need for a new NuGet dependency because `ServiceStack.csproj` already references `Microsoft.AspNetCore.App` for modern targets.

Prefer invoking this helper inside `ConfigureOperationEndpoint()` so existing callers in the core project and `PredefinedRoutesFeature` inherit it. `ServiceStack.Kestrel` has its own implementation rather than inheriting the core method; have it call the same shared helper explicitly. Do not rely only on `RouteHandlerBuilders`: current `.{format}` endpoints do not consistently invoke that list.

Endpoint metadata from request DTO attributes is added later by `AddRequestDtoAttributes`, and not on every variant route. Resolve both enable attributes and Microsoft's disable attribute directly from the request type, then apply the same effective policy or opt-out to every route variant. Translate `ServiceStack.RateLimitingAttribute` into the Microsoft endpoint convention; Microsoft middleware will not interpret the ServiceStack attribute by itself. Do not rely only on the existing metadata copy. Avoid contradictory duplicate endpoint metadata when `AddRequestDtoAttributes` runs, and verify final metadata ordering in a live integration test. If metadata ordering makes conflict detection unreliable, add an endpoint convention that validates the final metadata before the app starts accepting requests.

### 3. Close alternate-route gaps

Inventory every public HTTP path that can invoke an `Operation`:

- Explicit `[Route]` endpoints, including each verb and `.{format}` variant.
- `/api/{Request}` and `/api/{Request}[]` autobatch endpoints.
- `/api/{Request}.{format}` and other configured content-format routes.
- Legacy `/json/reply/{Request}`, `/json/oneway/{Request}`, and other handler routes if the host still allows them.

The first three must carry the effective policy. `PredefinedRoutesFeature` currently maps generic `{name}.{format}` endpoints, which cannot receive a different static named policy for each request DTO. When API rate-limiting bindings are enabled, replace or precede these generic routes with operation-specific format endpoints, or use one Microsoft policy whose partition factory safely resolves the operation and its configured limit. Prefer operation-specific endpoint registration for consistency with `RequireRateLimiting`; retain generic routes only for unbound operations if they cannot reach a bound operation. Test route precedence and all supported format/verb combinations.

Require `MapEndpoints(force: true)` when either rate-limiting attribute or a programmatic binding is present. If an application selects `force: false`, `use: false`, or omits endpoint mapping, fail startup with actionable guidance rather than expose a bypass. Confirm that ServiceStack catch-all/fallback handlers cannot execute a bound operation outside the rate-limiter middleware. Requests through `IMessageService`, in-process gateways, and jobs remain outside this HTTP feature.

### 4. Document host setup and rejection behavior

Document that applications call `AddRateLimiter` and `UseRateLimiter`; ServiceStack will not insert middleware implicitly because middleware order is application-owned. Endpoint-specific policies require `UseRouting()` before `UseRateLimiter()`, and authenticated partition keys require `UseAuthentication()` first. Place `UseRateLimiter()` before `UseServiceStack()` so rejection occurs before service execution. A host with `HttpContext.User` unset cannot safely infer a validated ServiceStack session or API key at this middleware stage: examples should use a shared anonymous partition or an ASP.NET Core authenticated principal, never a raw `X-Api-Key` or user-controlled header as an identity key.

Show a recommended `OnRejected` callback that sets HTTP 429, emits `Retry-After` only when the lease supplies `MetadataName.RetryAfter`, and returns a ServiceStack-friendly JSON error body if the response has not started. Preserve a host's existing `OnRejected` callback and `RejectionStatusCode` rather than overriding them. Clarify that Microsoft middleware's partitioned limiters are process-local; multi-node quotas need a separately supplied distributed policy or a gateway.

### 5. Add focused integration coverage

Use the existing endpoint-routing integration-test pattern in `tests/ServiceStack.Extensions.Tests/EndpointRoutingTests.cs` or a dedicated fixture. Keep limits small and windows long so assertions are deterministic. Cover:

1. Two DTOs with `[Tag("orders")]` exhaust **one shared** named-policy budget; a DTO with another tag remains available. Each attribute works without a programmatic operation binding and shares the budget with a tag-bound DTO using the same policy. Verify equal policy names on both attributes are accepted and unequal names fail startup.
2. One DTO with multiple tags mapped to different policies fails startup; identical-policy tags work; an operation-specific binding wins.
3. Two routes and two verbs for the same DTO, explicit `.{format}`, `/api/{Request}`, autobatch, and `/api/{Request}.{format}` all count against the same budget or are unavailable by design. No legacy route executes the service after a 429.
4. A request rejected with 429 never enters the service, request filters, or database layer; an allowed request still gets the normal ServiceStack response.
5. Authentication-based partitioning separates authenticated users and puts anonymous callers in a bounded shared bucket. Verify that a spoofed identity header does not create a new partition.
6. Missing policy registration and incompatible endpoint-routing options fail at startup. Both enable attributes have identical effects; Microsoft's disable attribute and a configured global limiter follow their documented precedence. Compile a `netstandard2.0` DTO project using `[RateLimiting]` with only `ServiceStack.Interfaces` referenced.
7. Compile and run on .NET 10. Run the same fixture on .NET 8 only if no compatibility branch or extra package is needed. Also ensure `net472` and `net6.0` builds are unaffected by conditional compilation.

## Release criteria

The feature is ready when a tagged set of ServiceStack operations shares a Microsoft policy budget across every supported HTTP route, the middleware can reject before service execution, incompatible routing settings fail fast, and sample setup works with the normal ASP.NET Core pipeline. The docs must state the per-process nature of built-in policies and the exact authentication and middleware-order requirements.

## Microsoft references

- [ASP.NET Core rate limiting middleware (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [`EnableRateLimitingAttribute` endpoint-policy behavior](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.ratelimiting.enableratelimitingattribute?view=aspnetcore-10.0)
- [`RequireRateLimiting` endpoint convention](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.ratelimiterendpointconventionbuilderextensions.requireratelimiting?view=aspnetcore-10.0)
- [Rate limiting samples: rejection, `Retry-After`, global and endpoint policies](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit-samples?view=aspnetcore-10.0)

## Application setup and behavior

Register each named policy with `builder.Services.AddRateLimiter(...)`, then place
`app.UseRouting()`, `app.UseAuthentication()` when the policy reads `HttpContext.User`,
`app.UseRateLimiter()`, and `app.UseServiceStack(..., options => options.MapEndpoints())`
in that order. `MapEndpoints()` must keep its default `use: true, force: true`
when an operation has a rate limit. ServiceStack never installs middleware or
changes `RateLimiterOptions.OnRejected` or `RejectionStatusCode` for the host.

The host may set `RejectionStatusCode = StatusCodes.Status429TooManyRequests`.
A host that wants a ServiceStack-friendly rejection body can add this when it
registers its policy (keeping any existing `OnRejected` behavior it needs):

```csharp
options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
options.OnRejected = async (context, cancellationToken) =>
{
    var response = context.HttpContext.Response;
    if (response.HasStarted) return;
    response.StatusCode = StatusCodes.Status429TooManyRequests;
    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds).ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    await response.WriteAsJsonAsync(new
    {
        ResponseStatus = new ResponseStatus
        {
            ErrorCode = "TooManyRequests",
            Message = "Rate limit exceeded",
        },
    }, cancellationToken);
};
```

`MetadataName` is in `System.Threading.RateLimiting`. Do not replace a host's
existing `OnRejected` callback without incorporating its behavior.

`[DisableRateLimiting]` also disables a configured global limiter and is a broad
opt-out. It cannot be combined with an explicit DTO or operation binding. All
built-in ASP.NET Core partitioned limiters keep counters in the current process;
for a shared quota across nodes, supply a distributed policy or use a gateway.
Use a validated ASP.NET Core principal for per-user partitions. A raw API-key or
identity header is not proof of identity at this middleware stage.

The feature covers ASP.NET Core HTTP endpoint routing on .NET 8 and .NET 10.
In-process calls, message transports, jobs, and legacy ServiceStack routing are
outside its scope. The latter is disabled by `MapEndpoints(force: true)` for
rate-limited operations.

## Feature Benefits

ServiceStack APIs can now use ASP.NET Core's named rate-limiting policies to
protect capacity and provide a more predictable experience during traffic
spikes. Add `[RateLimiting("policy-name")]` to a request DTO, use Microsoft's
`[EnableRateLimiting]` attribute, or apply a policy to every API with a shared
`[Tag]`. The portable ServiceStack attribute also lets shared DTO libraries opt
in without taking a dependency on ASP.NET Core.

**One budget for related APIs.** Apply a policy to a tag such as `orders` and
all matching operations draw from the same quota. This makes it easy to protect
an entire feature or customer workflow while keeping unrelated APIs available.
Policies can partition that quota by an authenticated user or another trusted
key, according to the application's needs.

**Use the limiter that fits your service.** Define policies with the familiar
ASP.NET Core rate limiter, including fixed-window, sliding-window, token-bucket,
concurrency, or custom policies. ServiceStack attaches the policy to its HTTP
endpoints, while ASP.NET Core handles permits, queues, rejections, and metrics.

**Consistent protection across API URLs.** The same policy applies to an
operation's explicit routes, `/api` route, supported format URLs, and autobatch
endpoint. When a request exceeds its limit, middleware can return HTTP 429
before ServiceStack runs the service, avoiding unnecessary application work.
Existing applications opt in through their normal ASP.NET Core setup and can
choose the rejection response that best suits their clients.
