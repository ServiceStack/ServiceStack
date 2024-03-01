---
title: ServiceStack Endpoint Routing
summary: ServiceStack .NET 8 is now more integrated then ever with support for ASP.NET Core Endpoint Routing and IOC    
tags: [servicestack,.net8,apis]
image: https://images.unsplash.com/photo-1510022151265-1bb84d406531?crop=entropy&fit=crop&h=1000&w=2000
author: Lucy Bates
---

In an effort to reduce friction and improve integration with ASP.NET Core Apps, we've continued the trend from last year
for embracing ASP.NET Core's built-in features and conventions which saw the latest ServiceStack v8 release converting 
all its newest .NET 8 templates to adopt [ASP.NET Core Identity Auth](https://docs.servicestack.net/auth/identity-auth).

This is a departure from building upon our own platform-agnostic abstractions which allowed the same ServiceStack code-base
to run on both .NET Core and .NET Framework. Our focus going forward will be to instead adopt De facto standards and conventions
of the latest .NET platform which also means ServiceStack's new value-added features are only available in the latest **.NET 8+** release.

### ServiceStack Middleware

Whilst ServiceStack integrates into ASP.NET Core Apps as custom middleware into ASP.NET Core's HTTP Request Pipeline,
it invokes its own black-box of functionality from there, implemented using its own suite of overlapping features.

Whilst this allows ServiceStack to have full control over how to implement its features, it's not as integrated as it could be,
with there being limits on what ServiceStack Functionality could be reused within external ASP .NET Core MVC Controllers, Razor Pages, etc.
and inhibited the ability to apply application-wide authorization policies across an Application entire surface area,
using and configuring different JSON Serialization implementations.

### Areas for tighter integration

The major areas we've identified that would benefit from tighter integration with ASP.NET Core include:

 - [Funq IOC Container](https://docs.servicestack.net/ioc)
 - [ServiceStack Routing](https://docs.servicestack.net/routing) and [Request Pipeline](https://docs.servicestack.net/order-of-operations)
 - [ServiceStack.Text JSON Serializer](https://docs.servicestack.net/json-format)

### ServiceStack v8.1 is fully integrated!

We're happy to announce the latest release of ServiceStack v8.1 now supports utilizing the optimal ASP.NET Core's 
standardized features to reimplement all these key areas - fostering seamless integration and greater reuse which
you can learn about below:

- [ASP.NET Core Identity Auth](https://docs.servicestack.net/auth/identity-auth)
- [ASP.NET Core IOC](https://docs.servicestack.net/releases/v8_01#asp.net-core-ioc)
- [Endpoint Routing](https://docs.servicestack.net/releases/v8_01#endpoint-routing)
- [System.Text.Json APIs](https://docs.servicestack.net/releases/v8_01#system.text.json)
- [Open API v3 and Swagger UI](https://docs.servicestack.net/releases/v8_01#openapi-v3)
- [ASP.NET Core Identity Auth Admin UI](https://docs.servicestack.net/releases/v8_01#asp.net-core-identity-auth-admin-ui)
- [JWT Identity Auth](https://docs.servicestack.net/releases/v8_01#jwt-identity-auth)

Better yet, this new behavior is enabled by default in all of ServiceStack's new ASP .NET Identity Auth .NET 8 templates!

### Migrating to ASP.NET Core Endpoints

To assist ServiceStack users in upgrading their existing projects we've created a migration guide walking through
the steps required to adopt these new defaults:

:::youtube RaDHkk4tfdU
Upgrade your APIs to use ASP.NET Core Endpoints
:::

### ASP .NET Core IOC

The primary limitation of ServiceStack using its own Funq IOC is that any dependencies registered in Funq are not injected
into Razor Pages, Blazor Components, MVC Controllers, etc. 

That's why our [Modular Startup](https://docs.servicestack.net/modular-startup) configurations recommend utilizing
custom `IHostingStartup` configurations to register application dependencies in ASP .NET Core's IOC where they can be 
injected into both ServiceStack Services and ASP.NET Core's external components, e.g:

```csharp
[assembly: HostingStartup(typeof(MyApp.ConfigureDb))]

namespace MyApp;

public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(
                context.Configuration.GetConnectionString("DefaultConnection"),
                SqliteDialect.Provider));
        });
}
```

But there were fundamental restrictions on what could be registered in ASP .NET Core's IOC as everything needed to be 
registered before AspNetCore's `WebApplication` was built and before ServiceStack's AppHost could be initialized, 
which prohibited being able to register any dependencies created by the AppHost including Services, AutoGen Services, 
Validators and internal functionality like App Settings, Virtual File System and Caching providers, etc.

## Switch to use ASP .NET Core IOC

To enable ServiceStack to switch to using ASP .NET Core's IOC you'll need to move registration of all dependencies and
Services to before the WebApplication is built by calling the `AddServiceStack()` extension method with the Assemblies
where your ServiceStack Services are located, e.g:

```csharp
builder.Services.AddServiceStack(typeof(MyServices).Assembly);

var app = builder.Build();

//...
app.UseServiceStack(new AppHost());
```

Which now registers all ServiceStack dependencies in ASP .NET Core's IOC, including all ServiceStack Services prior to
the AppHost being initialized which no longer needs to specify the Assemblies where ServiceStack Services are created
and no longer needs to use Funq as all dependencies should now be registered in ASP .NET Core's IOC.

### Registering Dependencies and Plugins

Additionally ASP.NET Core's IOC requirement for all dependencies needing to be registered before the WebApplication is 
built means you'll no longer be able to register any dependencies or plugins in ServiceStack's `AppHost.Configure()` method.

```csharp
public class AppHost() : AppHostBase("MyApp"), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Register IOC Dependencies and ServiceStack Plugins
        });

    public override void Configure()
    {
        // DO NOT REGISTER ANY PLUGINS OR DEPENDENCIES HERE
    }
}
```

Instead anything that needs to register dependencies in ASP.NET Core IOC should now use the `IServiceCollection` extension methods:

 - Use `IServiceCollection.Add*` APIs to register dependencies
 - Use `IServiceCollection.AddPlugin` API to register ServiceStack Plugins
 - Use `IServiceCollection.RegisterService*` APIs to dynamically register ServiceStack Services in external Assemblies

This can be done whenever you have access to `IServiceCollection`, either in `Program.cs`:

```csharp
builder.Services.AddPlugin(new AdminDatabaseFeature());
```

Or in any Modular Startup `IHostingStartup` configuration class, e.g:

```csharp
public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(
                context.Configuration.GetConnectionString("DefaultConnection"),
                SqliteDialect.Provider));
            
            // Enable Audit History
            services.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.GetRequiredService<IDbConnectionFactory>()));
            
            // Enable AutoQuery RDBMS APIs
            services.AddPlugin(new AutoQueryFeature {
                 MaxLimit = 1000,
            });

            // Enable AutoQuery Data APIs
            services.AddPlugin(new AutoQueryDataFeature());
            
            // Enable built-in Database Admin UI at /admin-ui/database
            services.AddPlugin(new AdminDatabaseFeature());
        })
        .ConfigureAppHost(appHost => {
            appHost.Resolve<ICrudEvents>().InitSchema();
        });
}
```

The `ConfigureAppHost()` extension method can continue to be used to execute any startup logic that requires access to 
registered dependencies.

### Authoring ServiceStack Plugins

To enable ServiceStack Plugins to support both Funq and ASP .NET Core IOC, any dependencies and Services a plugin needs
should be registered in the `IConfigureServices.Configure(IServiceCollection)` method as seen in the refactored
[ServerEventsFeature.cs](https://github.com/ServiceStack/ServiceStack/blob/main/ServiceStack/src/ServiceStack/ServerEventsFeature.cs)
plugin, e.g:

```csharp
public class ServerEventsFeature : IPlugin, IConfigureServices
{
    //...
    public void Configure(IServiceCollection services)
    {
        if (!services.Exists<IServerEvents>())
        {
            services.AddSingleton<IServerEvents>(new MemoryServerEvents
            {
                IdleTimeout = IdleTimeout,
                HouseKeepingInterval = HouseKeepingInterval,
                OnSubscribeAsync = OnSubscribeAsync,
                OnUnsubscribeAsync = OnUnsubscribeAsync,
                OnUpdateAsync = OnUpdateAsync,
                NotifyChannelOfSubscriptions = NotifyChannelOfSubscriptions,
                Serialize = Serialize,
                OnError = OnError,
            });
        }
        
        if (UnRegisterPath != null)
            services.RegisterService<ServerEventsUnRegisterService>(UnRegisterPath);

        if (SubscribersPath != null)
            services.RegisterService<ServerEventsSubscribersService>(SubscribersPath);
    }

    public void Register(IAppHost appHost)
    {
        //...
    }
}
```

#### All Plugins refactored to support ASP .NET Core IOC

All of ServiceStack's Plugins have been refactored to make use of `IConfigureServices` which supports registering in both 
Funq and ASP.NET Core's IOC when enabled.  

#### Funq IOC implements IServiceCollection and IServiceProvider interfaces

To enable this Funq now implements both `IServiceCollection` and`IServiceProvider` interfaces to enable 100% source-code 
compatibility for registering and resolving dependencies with either IOC, which we now recommend using over Funq's
native Registration and Resolution APIs to simplify migration efforts to ASP.NET Core's IOC in future.

## Dependency Injection

The primary difference between the IOC's is that ASP.NET Core's IOC does not support property injection by default, 
which will require you to refactor your ServiceStack Services to use constructor injection of dependencies, although
this has become a lot more pleasant with C# 12's [Primary Constructors](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/primary-constructors)
which now requires a lot less boilerplate to define, assign and access dependencies, e.g:

```csharp
public class TechStackServices(IAutoQueryDb autoQuery) : Service
{
    public async Task<object> Any(QueryTechStacks request)
    {
        using var db = autoQuery.GetDb(request, base.Request);
        var q = autoQuery.CreateQuery(request, Request, db);
        return await autoQuery.ExecuteAsync(request, q, db);
    }
}
```

This has become our preferred approach for injecting dependencies in ServiceStack Services which have all been refactored
to use constructor injection utilizing primary constructors in order to support both IOC's.

To make migrations easier we've also added support for property injection convention in **ServiceStack Services** using 
ASP.NET Core's IOC where you can add the `[FromServices]` attribute to any public properties you want to be injected, e.g:

```csharp
public class TechStackServices : Service
{
    [FromServices]
    public required IAutoQueryDb AutoQuery { get; set; }

    [FromServices]
    public MyDependency? OptionalDependency { get; set; }
}
```

This feature can be useful for Services wanting to access optional dependencies that may or may not be registered. 

:::info NOTE
`[FromServices]` is only supported in ServiceStack Services (i.e. not other dependencies)
:::

### Built-in ServiceStack Dependencies

This integration now makes it effortless to inject and utilize optional ServiceStack features like
[AutoQuery](https://docs.servicestack.net/autoquery/) and [Server Events](https://docs.servicestack.net/server-events)
in other parts of ASP.NET Core inc. Blazor Components, Razor Pages, MVC Controllers, Minimal APIs, etc.

Whilst the Built-in ServiceStack features that are registered by default and immediately available to be injected, include:
 - `IVirtualFiles` - Read/Write [Virtual File System](https://docs.servicestack.net/virtual-file-system), defaults to `FileSystemVirtualFiles` at `ContentRootPath`
 - `IVirtualPathProvider` - Multi Virtual File System configured to scan multiple read only sources, inc `WebRootPath`, In Memory and Embedded Resource files  
 - `ICacheClient` and `ICacheClientAsync` - In Memory Cache, or distributed Redis cache if [ServiceStack.Redis](https://docs.servicestack.net/redis/) is configured
 - `IAppSettings` - Multiple [AppSettings](https://docs.servicestack.net/appsettings) configuration sources

With ASP.NET Core's IOC now deeply integrated we moved onto the next area of integration: API Integration and Endpoint Routing.

## Endpoint Routing

Whilst ASP.NET Core's middleware is a flexible way to compose and execute different middleware in a HTTP Request pipeline,
each middleware is effectively their own island of functionality that's able to handle HTTP Requests in which ever way
they see fit.

In particular ServiceStack's middleware would execute its own [Request Pipeline](https://docs.servicestack.net/order-of-operations) 
which would execute ServiceStack API's registered at user-defined routes with its own [ServiceStack Routing](https://docs.servicestack.net/routing).

We're happy to announce that ServiceStack **.NET 8** Apps support an entirely new and integrated way to run all of ServiceStack 
requests including all APIs, metadata and built-in UIs with support for 
[ASP.NET Core Endpoint Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing) -
enabled by calling the `MapEndpoints()` extension method when configuring ServiceStack, e.g:

```csharp
app.UseServiceStack(new AppHost(), options => {
    options.MapEndpoints();
});
```

Which configures ServiceStack APIs to be registered and executed along-side Minimal APIs, Razor Pages, SignalR, MVC 
and Web API Controllers, etc, utilizing the same routing, metadata and execution pipeline.

#### View ServiceStack APIs along-side ASP.NET Core APIs

Amongst other benefits, this integration is evident in endpoint metadata explorers like the `Swashbuckle` library 
which can now show ServiceStack APIs in its Swagger UI along-side other ASP.NET Core APIs in ServiceStack's new
[Open API v3](/posts/openapi-v3) support.

### Routing

Using Endpoint Routing also means using ASP.NET Core's Routing System which now lets you use ASP.NET Core's
[Route constraints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-constraints)
for defining user-defined routes for your ServiceStack APIs, e.g:

```csharp
[Route("/users/{Id:int}")]
[Route("/users/{UserName:string}")]
public class GetUser : IGet, IReturn<User>
{
    public int? Id { get; set; }
    public int? UserName { get; set; }
}
```

For the most part ServiceStack Routing implements a subset of ASP.NET Core's Routing features so your existing user-defined 
routes should continue to work as expected. 

### Wildcard Routes

The only incompatibility we found was when using wildcard paths which in ServiceStack Routing would use an '*' suffix, e.g:
`[Route("/wildcard/{Path*}")]` which will need to change to use a ASP.NET Core's Routing prefix, e.g: 

```csharp
[Route("/wildcard/{*Path}")]
[Route("/wildcard/{**Path}")]
public class GetFile : IGet, IReturn<byte[]>
{
    public string Path { get; set; }
}
```

#### ServiceStack Routing Compatibility

To improve compatibility with ASP.NET Core's Routing, ServiceStack's Routing (when not using Endpoint Routing) now 
supports parsing ASP.NET Core's Route Constraints but as they're inert you would need to continue to use
[Custom Route Rules](https://docs.servicestack.net/routing#custom-rules) to distinguish between different routes matching 
the same path at different specificity:

```csharp
[Route("/users/{Id:int}", Matches = "**/{int}")]
[Route("/users/{UserName:string}")]
public class GetUser : IGet, IReturn<User>
{
    public int? Id { get; set; }
    public int? UserName { get; set; }
}
```

It also supports defining Wildcard Routes using ASP.NET Core's syntax which we now recommend using instead 
for compatibility when switching to use Endpoint Routing:

```csharp
[Route("/wildcard/{*Path}")]
[Route("/wildcard/{**Path}")]
public class GetFile : IGet, IReturn<byte[]>
{
    public string Path { get; set; }
}
```

### Primary HTTP Method

Another difference is that an API will only register its Endpoint Route for its [primary HTTP Method](https://docs.servicestack.net/api-design#all-apis-have-a-preferred-default-method),
if you want an API to be registered for multiple HTTP Methods you can specify them in the `Route` attribute, e.g:

```csharp
[Route("/users/{Id:int}", "GET,POST")]
public class GetUser : IGet, IReturn<User>
{
    public required int Id { get; set; }
}
```

As such we recommend using the IVerb `IGet`, `IPost`, `IPut`, `IPatch`, `IDelete` interface markers to specify the primary HTTP Method
for an API. This isn't needed for [AutoQuery Services](https://docs.servicestack.net/autoquery/) which are implicitly configured
to use their optimal HTTP Method.

If no HTTP Method is specified, the Primary HTTP Method defaults to HTTP **POST**.

### Authorization

Using Endpoint Routing also means ServiceStack's APIs are authorized the same way, where ServiceStack's 
[Declarative Validation attributes](https://docs.servicestack.net/auth/#declarative-validation-attributes) are converted
into ASP.NET Core's `[Authorize]` attribute to secure the endpoint:

```csharp
[ValidateIsAuthenticated]
[ValidateIsAdmin]
[ValidateHasRole(role)]
[ValidateHasClaim(type,value)]
[ValidateHasScope(scope)]
public class Secured {}
```

#### Authorize Attribute on ServiceStack APIs

Alternatively you can now use ASP.NET Core's `[Authorize]` attribute directly to secure ServiceStack APIs should
you need more fine-grained Authorization:

```csharp
[Authorize(Roles = "RequiredRole")]
[Authorize(Policy = "RequiredPolicy")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
public class Secured {}
```

#### Configuring Authentication Schemes

ServiceStack will default to using the major Authentication Schemes configured for your App to secure the APIs endpoint with, 
this can be overridden to specify which Authentication Schemes to use to restrict ServiceStack APIs by default, e.g:

```csharp
app.UseServiceStack(new AppHost(), options => {
    options.AuthenticationSchemes = "Identity.Application,Bearer";
    options.MapEndpoints();
});
```

### Hidden ServiceStack Endpoints

Whilst ServiceStack Requests are registered and executed as endpoints, most of them are marked with
`builder.ExcludeFromDescription()` to hide them from polluting metadata and API Explorers like Swagger UI and 
[API Explorer](https://docs.servicestack.net/api-explorer).

To also hide your ServiceStack APIs you can use `[ExcludeMetadata]` attribute to hide them from all metadata services
or use `[Exclude(Feature.ApiExplorer)]` to just hide them from API Explorer UIs:

```csharp
[ExcludeMetadata]
[Exclude(Feature.ApiExplorer)]
public class HiddenRequest {}
```

### Content Negotiation

An example of these hidden routes is the support for invoking and returning ServiceStack APIs in different Content Types
via hidden Endpoint Routes mapped with the format `/api/{Request}.{format}`, e.g:

- [/api/QueryBookings](https://blazor-vue.web-templates.io/api/QueryBookings)
- [/api/QueryBookings.jsonl](https://blazor-vue.web-templates.io/api/QueryBookings.jsonl)
- [/api/QueryBookings.csv](https://blazor-vue.web-templates.io/api/QueryBookings.csv)
- [/api/QueryBookings.xml](https://blazor-vue.web-templates.io/api/QueryBookings.xml)
- [/api/QueryBookings.html](https://blazor-vue.web-templates.io/api/QueryBookings.html)

#### Query String Format

That continues to support specifying the Mime Type via the `?format` query string, e.g:
 
- [/api/QueryBookings?format=jsonl](https://blazor-vue.web-templates.io/api/QueryBookings?format=jsonl)
- [/api/QueryBookings?format=csv](https://blazor-vue.web-templates.io/api/QueryBookings?format=csv)

### Predefined Routes

Endpoints are only created for the newer `/api/{Request}` [pre-defined routes](https://docs.servicestack.net/routing#pre-defined-routes),
which should be easier to use with less conflicts now that ServiceStack APIs are executed along-side other endpoint routes 
APIs which can share the same `/api` base path with non-conflicting routes, e.g: `app.MapGet("/api/minimal-api")`.

As a result clients configured to use the older `/json/reply/{Request}` pre-defined route will need to be configured
to use the newer `/api` base path.

No change is required for C#/.NET clients using the recommended `JsonApiClient` JSON Service Client which is already
configured to use the newer `/api` base path.

```csharp
var client = new JsonApiClient(baseUri);
```

Older .NET clients can be configured to use the newer `/api` pre-defined routes with:

```csharp
var client = new JsonServiceClient(baseUri) {
    UseBasePath = "/api"
};
var client = new JsonHttpClient(baseUri) {
    UseBasePath = "/api"
};
```

To further solidify that `/api` as the preferred pre-defined route we've also **updated all generic service clients** of
other languages to use `/api` base path by default:

#### JavaScript/TypeScript

```ts
const client = new JsonServiceClient(baseUrl)
```

#### Dart

```dart
var client = ClientFactory.api(baseUrl);
```

#### Java/Kotlin

```java
JsonServiceClient client = new JsonServiceClient(baseUrl);
```

#### Python

```python
client = JsonServiceClient(baseUrl)
```

#### PHP

```php
$client = new JsonServiceClient(baseUrl);
```

### Revert to Legacy Predefined Routes

You can unset the base path to revert back to using the older `/json/reply/{Request}` pre-defined route, e.g:

#### JavaScript/TypeScript

```ts
client.basePath = null;
```

#### Dart

```dart
var client = ClientFactory.create(baseUrl);
```

#### Java/Kotlin

```java
client.setBasePath();
```

#### Python

```python
client.set_base_path()
```

#### PHP

```php
$client->setBasePath();
```

### Customize Endpoint Mapping

You can register a RouteHandlerBuilders to customize how ServiceStack APIs endpoints are registered which is also
what ServiceStack uses to annotate its API endpoints to enable its new [Open API v3](/posts/openapi-v3) support:

```csharp
options.RouteHandlerBuilders.Add((builder, operation, method, route) =>
{
    builder.WithOpenApi(op => { ... });
});
```

### Endpoint Routing Compatibility Levels

The default behavior of `MapEndpoints()` is the strictest and recommended configuration that we want future ServiceStack Apps to use,
however if you're migrating existing App's you may want to relax these defaults to improve compatibility with existing behavior.

The configurable defaults for mapping endpoints are:

```csharp
app.UseServiceStack(new AppHost(), options => {
    options.MapEndpoints(use:true, force:true, useSystemJson:UseSystemJson.Always);
});
```

- `use` - Whether to use registered endpoints for executing ServiceStack APIs
- `force` - Whether to only allow APIs to be executed through endpoints
- `useSystemJson` - Whether to use System.Text.Json for JSON API Serialization

So you could for instance register endpoints and not `use` them, where they'll be visible in endpoint API explorers like
[Swagger UI](https://docs.servicestack.net/releases/v8_01#openapi-v3) but continue to execute in ServiceStack's Request Pipeline.

`force` disables fallback execution of ServiceStack Requests through ServiceStack's Request Pipeline for requests that
don't match registered endpoints. You may need to disable this if you have clients calling ServiceStack APIs through
multiple HTTP Methods, as only the primary HTTP Method is registered as an endpoint.

When enabled `force` ensures the only ServiceStack Requests that are not executed through registered endpoints are
`IAppHost.CatchAllHandlers` and `IAppHost.FallbackHandler` handlers.

`useSystemJson` is a new feature that lets you specify when to use `System.Text.Json` for JSON API Serialization, which
is our next exciting feature to standardize on using  
[ASP.NET Core's fast async System.Text.Json](https://docs.servicestack.net/releases/v8_01#system.text.json) Serializer.

## Endpoint Routing Everywhere

Whilst the compatibility levels of Endpoint Routing can be relaxed, we recommend new projects use the strictest and most
integrated defaults that's now configured on all [ASP.NET Core Identity Auth .NET 8 Projects](/start).

For additional testing we've also upgraded many of our existing .NET Example Applications, which are now all running with 
our latest recommended Endpoint Routing configuration:

 - [BlazorDiffusionVue](https://github.com/NetCoreApps/BlazorDiffusionVue)
 - [BlazorDiffusionAuto](https://github.com/NetCoreApps/BlazorDiffusionAuto)
 - [TalentBlazor](https://github.com/NetCoreApps/TalentBlazor)
 - [TechStacks](https://github.com/NetCoreApps/TechStacks)
 - [Validation](https://github.com/NetCoreApps/Validation)
 - [NorthwindAuto](https://github.com/NetCoreApps/NorthwindAuto)
 - [FileBlazor](https://github.com/NetCoreApps/FileBlazor)
 - [Chinook](https://github.com/NetCoreApps/Chinook)
 - [Chat](https://github.com/NetCoreApps/Chat)
