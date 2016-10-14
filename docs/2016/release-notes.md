# [v4.5.0 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.5.0.md)

We've upgraded all ServiceStack packages to **.NET 4.5**, if you were already using ServiceStack in 
.NET 4.5 projects this will be a seamless upgrade like any other release but if your ServiceStack projects 
are instead still on .NET 4.0 this will be a breaking change which will require converting **all** your 
projects to **.NET 4.5 Framework** before upgrading, e.g:

![](http://i.imgur.com/GV8TmAS.png)

You will also need to have .NET 4.5 Framework installed on any deployment Servers that doesn't have it already. 

### Upgraded 3rd Party NuGet packages

Upgrading to .NET 4.5 mean we're able to reference the **latest .NET 4.5 .dlls** in packages with 3rd Party
dependencies including, `Npgsql`, `RabbitMQ.Client` and `ServiceStack.Razor` now references the official 
`Microsoft.AspNet.Razor` NuGet package.

## [.NET Core support for ServiceStack.Redis!](https://github.com/ServiceStack/ServiceStack.Redis/blob/netcore/docs/pages/netcore.md)

In following the
[.NET Core support of our Text and Client libraries](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.62.md#net-core-support-for-servicestackclient) 
in our last release we've extended our support for .NET Core in this release to now also include 
[ServiceStack.Redis](https://github.com/ServiceStack/ServiceStack.Redis)
where we now have .NET Core builds for our [Top 3 popular NuGet packages](https://www.nuget.org/profiles/servicestack).

To make it easy to start using Redis in a .NET Core App we've created a step-by-step guide for 
[getting started with ServiceStack.Redis on .NET Core](https://github.com/ServiceStack/ServiceStack.Redis/blob/netcore/docs/pages/netcore.md) 
in both Windows and Linux.

## [New Xamarin.Forms TechStacks App](https://github.com/ServiceStackApps/TechStacksXamarin)

We've added a new TechStacks Mobile App to our expanding showcase of different ways where ServiceStack
provides a seamless end-to-end Typed API development experience for developing Native Mobile Apps which now includes:

 - [C# iOS/Android Xamarin.Forms TechStacks App](https://github.com/ServiceStackApps/TechStacksXamarin) - **new!**
 - [Swift iOS TechStacks App](https://github.com/ServiceStackApps/TechStacksApp)
 - [Java Android Techstacks App](https://github.com/ServiceStackApps/TechStacksAndroidApp)
 - [Kotlin Android TechStacks App](https://github.com/ServiceStackApps/TechStacksKotlinApp)
 - [C# Xamarin.Android TechStacks Auth Example](https://github.com/ServiceStackApps/TechStacksAuth)

Whilst not as flexibile or performant as native code, [Xamarin.Forms](https://www.xamarin.com/forms) enables 
the most code reuse of all the available options when needing to develop both iOS and Android Apps whilst 
still allowing for customization through styling or custom platform specific renderers. It also benefits from being 
able to use C# and much of the rich cross-platform libraries in .NET.

Despite sharing the majority of UI code between Android and iOS, Xamarin.Forms Apps also adopts the navigation
idioms of each platform to provide a native "look and feel" which we can see by running the 
TechStacks Xamarin.Forms App on iOS and Android:

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/TechStacksXamForms/video_preview.png)](https://www.youtube.com/watch?v=4ghchU3xKs4)

See the [TechStacksXamarin Github project](https://github.com/ServiceStackApps/TechStacksXamarin) 
for more info and access to the source code.

## [AutoQuery Viewer](https://github.com/ServiceStack/Admin) Saved Queries

We've further refined [AutoQuery Viewer](https://github.com/ServiceStack/Admin) and added support for 
Saved Queries where you can save queries under each AutoQuery Service by clicking the **save icon**. 

The saved query will be listed with the name provided and displayed to the right of the save icon, e.g:

[![](http://i.imgur.com/hySw1T9.png)](https://github.com/ServiceStack/Admin)

This makes it easy for everyone to maintain and easily switch between multiple personalized views 
of any [AutoQuery Service](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query).

## Create Live Executable Docs with Gistlyn

In our mission to make [Gistlyn](http://gistlyn.com) an immensely useful and collaborative learning tool for 
exploring any .NET library, we've greatly improved the UX for editing Collections making it easier than ever to 
create "Live documentation" which we believe is the best way to learn about a library, mixing documentation and 
providing a live development experience letting developers try out and explore what they've just learned without 
losing context by switching to their development environment and setting up new projects to match each code sample.

Gistlyn also makes it easy to share C# snippets with colleagues or reporting an issue to library mainteners with 
just a URL or a saved Gist ID which anyone can view in a browser at [gistlyn.com](http://gistlyn.com) or on their 
[Desktop version of Gistlyn](http://gistlyn.com/downloads). 

Here's an example of the new Collection authoring features in action:

[![](http://i.imgur.com/156wYPJ.png)](https://youtu.be/FkdzYsx2lYw)

## The Truly Empty ASP.NET Template

![](http://i.imgur.com/ZCHoJFA.png)

Over the years it's becoming harder and harder to create an Empty ASP.NET VS.NET Template as it 
continues to accumulate more cruft, unused dlls, hidden behavior, hooks into external services and 
other unnecessary bloat. Most of the bloat added since ASP.NET 2.0 for the most part has been unnecessary 
yet most .NET developers end up living with it because it's in the default template and they're 
unsure what each unknown dlls and default configuration does or what unintended behavior it will 
cause down the line if they remove it.

For ServiceStack and other lightweight Web Frameworks this added weight is completely unnecessary
and can be safely removed. 
E.g. [most ServiceStack Apps just needs a few ServiceStack .dlls](https://github.com/ServiceStackApps/Chat#super-lean-front-and-back) 
and a [single Web.config mapping](https://github.com/ServiceStack/ServiceStack/wiki/Create-your-first-webservice#register-servicestack-handler)
to tell ASP.NET to route all calls to ServiceStack. Any other ASP.NET config you would add in 
ServiceStack projects is just to get ASP.NET to disable any conflicting default behavior.

### The Minimal ASP.NET Template we wanted

Out of frustration we've decided to reverse this trend and instead of focusing on what can be added, 
we're focusing on what can be removed whilst still remaining useful for most modern ASP.NET Web Apps. 

With this goal we've reduced the ASP.NET Empty Template down to a single project with
the only external dependency being Roslyn:

![](http://i.imgur.com/jKFga3J.png)

Most dlls have been removed and the 
[Web.config](https://github.com/ServiceStack/ServiceStackVS/blob/master/src/ServiceStackVS/ProjectTemplates/AspNetEmpty/Host/Web.config) 
just contains registration for Roslyn and config for disabling ASP.NET's unwanted default behavior.

The only `.cs` file is an Empty `Global.asax.cs` with an empty placeholder for running custom code on Startup:

```csharp
using System;

namespace WebApplication
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            
        }
    }
}
```

And that's it! `ASP.NET Empty` is a single project empty ASP.NET Web Application with no additional references. 

### Minimal but still Useful

You can then easily [Convert this empty template into a functional ServiceStack Web App](https://github.com/ServiceStack/ServiceStack/wiki/Create-your-first-webservice) by: 

1) Installing [ServiceStack and any other dependency](https://github.com/ServiceStackApps/Todos/blob/master/src/Todos/packages.config) you want to use, e.g:

	PM> Install-Package ServiceStack
	PM> Install-Package ServiceStack.Redis
   
2) Adding the [ASP.NET HTTP Handler mapping](https://github.com/ServiceStackApps/Todos/blob/fdcffd37d4ad49daa82b01b5876a9f308442db8c/src/Todos/Web.config#L34-L39) to route all requests to ServiceStack:

```xml
<system.webServer>
    <validation validateIntegratedModeConfiguration="false"/>
    <handlers>
	    <add path="*" name="ServiceStack.Factory" type="ServiceStack.HttpHandlerFactory, ServiceStack" verb="*" preCondition="integratedMode" resourceType="Unspecified" allowPathInfo="true"/>
    </handlers>
</system.webServer>
```

3) Adding your [ServiceStack AppHost and Services in Global.asax.cs](https://github.com/ServiceStackApps/Todos/blob/master/src/Todos/Global.asax.cs).

That's all that's needed to create a functional Web App, which in this case creates a
[Backbone TODO compatible REST API with a Redis back-end](https://github.com/ServiceStackApps/Todos/) 
which can power all [todomvc.com](http://todomvc.com) Single Page Apps.

## Generating API Keys for Existing Users

Whilst not a feature in ServiceStack, this script is useful if you want to enable ServiceStack's 
[API Key AuthProvider](https://github.com/ServiceStack/ServiceStack/wiki/API-Key-AuthProvider) 
but you have existing users you also want to generate API Keys for.

You can add the script below (which only needs to be run once) to your `AppHost.Configure()` which will 
use the configuration in your registered `ApiKeyAuthProvider` to generate new keys for all existing users 
that don't have keys. 

This example assumes the typical scenario of using an `OrmLiteAuthRepository` to store your Users in an RDBMS: 

```csharp
AfterInitCallbacks.Add(host =>
{
    var authProvider = (ApiKeyAuthProvider)
        AuthenticateService.GetAuthProvider(ApiKeyAuthProvider.Name);
    using (var db = host.TryResolve<IDbConnectionFactory>().Open())
    {
        var userWithKeysIds = db.Column<string>(db.From<ApiKey>()
            .SelectDistinct(x => x.UserAuthId)).Map(int.Parse);

        var userIdsMissingKeys = db.Column<string>(db.From<UserAuth>()
            .Where(x => userWithKeysIds.Count == 0 || !userWithKeysIds.Contains(x.Id))
            .Select(x => x.Id));

        var authRepo = (IManageApiKeys)host.TryResolve<IAuthRepository>();
        foreach (var userId in userIdsMissingKeys)
        {
            var apiKeys = authProvider.GenerateNewApiKeys(userId.ToString());
            authRepo.StoreAll(apiKeys);
        }
    }
});
```

If using another Auth Repository backend you will need to modify this script to fetch the userIds for
all users missing API Keys for the data persistence back-end you're using.

## Other Features

### Auto rewriting of HTTPS Links

ServiceStack now automatically rewrites outgoing links to use `https://` for Requests that were forwarded
by an SSL-terminating Proxy and containing the `X-Forwarded-Proto = https` HTTP Header. 
You can override `AppHost.UseHttps()` to change this behavior.

# [v4.0.62 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md)

We've got another big release with features added across the board, we'll list the main highlights here but 
please see the [v4.0.62 release notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.62.md) 
for the full details.

## [Gistlyn](http://gistlyn.com)

We're excited to announce [gistlyn.com](http://gistlyn.com) to the world - Gistlyn is a C# Gist IDE for creating, running and sharing stand-alone, executable C# snippets! 

Born out of our initiative to improve ServiceStack's documentation, we set out to create the best way for developers to learn and explore different features in ServiceStack, Gistlyn is the result of this  effort which lets you try out and explore C# and .NET libraries using just a modern browser, making it the ideal companion tool for trying out libraries during development or on the go from the comfort of  your iPad or recent Android tablets by going to: http://gistlyn.com

Gistlyn lets you run any valid C# fragments will execute your code on Gistlyn's server, running in an  isolated context where each of the variables defined in the top-level scope can be inspected further. The preview inspector also includes an Expression evaluator that can be used to evaluate C# expressions against the live server session.

### [Gistlyn Collections](http://gistlyn.com/collections)

Gistlyn is also an Open platform where anyone can create [Collections](http://gistlyn.com/collections) - A simple markdown document with info about a feature and links to source code where users can try it out "live" in the browser, with instant feedback so you can quickly preview the results at a glance.

### [OrmLite Interactive Tour](http://gistlyn.com/ormlite)

The [OrmLite Collection](http://gistlyn.com/ormlite) is a great example of this which is now the best way to learn about and try OrmLite with walk through guides that takes you through different OrmLite features. We intend to add more guides like this in future so Gistlyn becomes the best place to learn about and test different features. In the meantime we've added simple TODO examples pre-configured with the necessary Nuget packages to explore OrmLite, Redis and PocoDynamo at: 

  - http://gistlyn.com/ormlite-todo
  - http://gistlyn.com/redis-todo
  - http://gistlyn.com/pocodynamo-todo

### [Add ServiceStack Reference in Gistlyn](http://gistlyn.com/add-servicestack-reference)

One feature that will add a lot of complementary value to your ServiceStack Services is Gistlyn's integrated support for Add ServiceStack Reference feature which will generate a Typed API for your remote ServiceStack Services and let you call them using ServiceStack's typed C# Service Clients and view their results - within seconds!

The easiest way to use this feature is to add the **BaseUrl** for your remote ServiceStack instance to the `?AddServiceStackReference` query string, e.g:

 - http://gistlyn.com?AddServiceStackReference=techstacks.io

Which will create a new Gist with your Typed DTOs attached and an example request using a pre-configured JsonServiceClient and the first GET Request DTO it can find. So without having written any code you can Press play to execute a Typed API Request against your ServiceStack Services.

The URL can be further customized to tell Gistlyn which Request DTO and C# expression it should use and whether to auto run it. This feature should make it easier to collaborate with others about your ServiceStack Services as you can send a url so they can test it out without having the proper developer environment setup. For more advanced scenarios you can easily save a modified script as a gist and send a link to that instead.

### [Gistlyn Snapshots](http://gistlyn.com/snapshots)

Snapshots lets you save the *entire client state* of your current workspace (excluding your login info) 
into a generated url which you can use to revert back in time from when the snapshot was taken or send to someone else who can instantly see and run what you're working on, who'll be able to continue working from the same place you're at.

### Gistlyn's Stateless Architecture

One surprising thing about Gistlyn is that it's entirely stateless where it runs without any kind of backend db persistence. All state is either persisted to Github gists or in your browser's `localStorage`. Not even your Authenticated Github session is retained on the server as it's immediately converted into an encrypted JWT Cookie that is sent with every Ajax request, so redeployments (or even clean server rebuilds) won't lose any of your work or force you to Sign In again until the JWT Token expires.

Gistlyn's Github Repo provides a good example of a modern medium-sized ServiceStack, React + TypeScript App that takes advantage of a number of different ServiceStack Features:

 - [React Desktop Apps](https://github.com/ServiceStackApps/ReactDesktopApps) - 
 tooling for packaging Gistlyn's ASP.NET Web App into a Winforms Desktop and Console App
 - [Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events) - providing real-time 
 Script Status updates and Console logging
 - [TypeScript](https://github.com/ServiceStack/ServiceStack/wiki/TypeScript-Add-ServiceStack-Reference) - enabling end-to-end Typed API requests
 - [Github OAuth](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization#auth-providers) -
 authentication with Github
 - [JWT Auth Provider](https://github.com/ServiceStack/ServiceStack/wiki/JWT-AuthProvider) - enabling both JWT and JWE ecrypted stateless Sessions
 - [HTTP Utils](https://github.com/ServiceStack/ServiceStack/wiki/Http-Utils) - consuming Github's REST API 
 and creating an authenticated HTTP Proxy in [GitHubServices.cs](https://github.com/ServiceStack/Gistlyn/blob/master/src/Gistlyn.ServiceInterface/GitHubServices.cs)

### [Run Gistlyn on your Desktop](http://gistlyn.com/downloads)

Thanks to ServiceStack's [React Desktop Apps](https://github.com/ServiceStackApps/ReactDesktopApps) VS.NET Template Gistlyn is available in a variety of different flavours:

Deployed as an ASP.NET Web Application on both Windows / .NET and Linux / Mono servers at: 

 - [gistlyn.com](http://gistlyn.com) - Ubuntu / Vagrant / Windows 2012 Server VM / IIS / .NET 4.6
 - [mono.gistlyn.com](http://mono.gistlyn.com) - Ubuntu / Docker / mono / nginx / HyperFastCGI

In addition to a running as an ASP.NET Web App, Gistlyn is also available as a self-hosting 
Winforms Desktop or cross-platform OSX/Linux/Windows Console App at: http://gistlyn.com/downloads 

Running Gistlyn on your Desktop lets you take advantage of the full resources of your CPU for faster 
build and response times and as they're run locally they'll be able to access your RDBMS or 
other Networked Servers and Services available from your local Intranet.

## 1st class TypeScript support

TypeScript has become a core part of our overall recommended solution that's integrated into all  ServiceStackVS's React and Aurelia Single Page App VS.NET Templatesoffering a seamless development experience with access to advanced ES6 features like modules, classes and arrow functions whilst still being able to target most web browsers with its down-level ES5 support. 

We've added even deeper integration with TypeScript in this release with several enhancements to the generated TypeScript DTOs which graduates TypeScript to a 1st class supported language that together with the new TypeScript `JsonServiceClient` available in the servicestack-client npm package enables the same productive, typed API development experience available in our other 1st-class supported client platforms, e.g: 

```ts
client.post(request)
    .then(r => { 
        console.log(`New C# Gist was created with id: ${r.gist}`);
    })
    .catch(e => {
        console.log("Failed to create Gist: ", e.responseStatus);
    });
```

Where the `r` param in the returned `then()` Promise callback is typed to Response DTO Type.

### Isomorphic Fetch

The `servicestack-client` is a clean "jQuery-free" implementation based on JavaScript's new Fetch API standard utilizing nodes isomorphic-fetch implementation so it can be used in both JavaScript client web apps as well as node.js server projects.

### ServerEventsClient

In addition to `JsonServiceClient` we've ported most of the JavaScript utils in ss-utils.js including the new `ServerEventsClient` which Gistlyn uses to process real-time Server Events with:

```ts
const channels = ["gist"];
const sse = new ServerEventsClient("/", channels, {
    handlers: {
        onConnect(activeSub:ISseConnect) {
            store.dispatch({ type: 'SSE_CONNECT', activeSub });
            fetch("/session-to-token", {
                method:"POST", credentials:"include"
            });
        },
        ConsoleMessage(m, e) {
            batchLogs.queue({ msg: m.message });
        },
        ScriptExecutionResult(m:ScriptExecutionResult, e) {
            //...
        }
    }
});
```

### .NET Core support for ServiceStack.Client

We're happy to be able to release our initial library support for .NET Core with .NET Core builds for ServiceStack.Client and its dependencies, available in the following NuGet packages:

 - ServiceStack.Client.Core
 - ServiceStack.Text.Core
 - ServiceStack.Interfaces.Core

Until we've completed our transition, we'll be maintaining .NET Core builds in separate NuGet packages containing a `.Core` suffix as seen above. This leaves our existing .NET packages unaffected, whilst letting us increase our release cadence of .NET Core packages until support for .NET Core libraries has stabilized.

We've published a step-by-step guide showing how to Install ServiceStack.Client in a .NET Core App at: https://github.com/ServiceStack/ServiceStack/blob/netcore/docs/pages/netcore.md

### ServiceStack.Text is now Free!

To celebrate our initial release supporting .NET Core, we're now making ServiceStack.Text completely free for commercial or non-commercial use. We've removed all free-quota restrictions and are no longer selling licenses for ServiceStack.Text. By extension this also extends to our client libraries that just depend on ServiceStack.Text, including ServiceStack.Client and ServiceStack.Stripe which are also both free of any technical restrictions.

### Encrypted Service Clients for iOS, Android and OSX

The `EncryptedServiceClient` is now available in Xamarin iOS, Android and OSX packages so your Xamarin Mobile and OSX Desktop Apps are now able to benefit from transparent encrypted service client requests without needing to configure back-end HTTP servers with SSL.

## Last release supporting .NET 4.0

As announced earlier this year in preparation for .NET Core, this will be our last release supporting 
.NET 4.0. Starting from next release all projects will be upgraded to .NET 4.5. Should you need it, 
the .NET 4.0 compatible ServiceStack source code will remain accessible in the `net40` branches of all major ServiceStack Github repositories.

## Aurelia updated to 1.0

To coincide with the v1.0 release of Aurelia the Aurelia VS.NET template has been updated to v1.0 using **bootstrap.native** and is now pre-configured with both the new `servicestack-client` and local `src/dtos.ts` TypeScript Reference that includes an end-to-end Typed DTO integrated example.

## Improved Razor intellisense

We've updated all our ASP.NET Razor VS.NET Templates to use the ideal `Web.config` configuration for editing Razor pages without designer errors in VS.NET 2015. 

## JWT Auth Provider

The `JwtAuthProvider` has added support for specifying multiple fallback AES Auth Keys and RSA Public Keys allowing for smooth key rotations to newer Auth Keys whilst simultaneously being able to verify JWT Tokens signed with a previous key. 

## Multitenancy RDBMS AuthProvider

ServiceStack's `IAuthProvider` has been refactored to use the central and overridable `GetAuthRepository(IRequest)` AppHost factory method where just like ServiceStack's other "Multitenancy-aware" dependencies now lets you dynamically change which AuthProvider should be used based on the incoming request.

This can be used with the new `OrmLiteAuthRepositoryMultitenancy` provider to maintain isolated User Accounts per tenant in all major supported RDBMS.

## OrmLite

OrmLite continues to see improvements with many of the new features in this release contributed by the Community, with special thanks to @shift-evgeny, @OlegNadymov and @bryancrosby for their contributions. There's too many examples to list here, so please check the release notes for OrmLite's new capabilities.

### Dump Utils

To improve their utility in Gistlyn C# gists for quickly dumping and inspecting the contents of an object the `T.PrintDump()` and `T.Dump()` extension methods can now be used on objects with cyclical references where it will display the first-level `ToString()` value of properties that have circular references.

The `Dump()` utils are invaluable when explanatory coding or creating tests as you can quickly see what's in an object without having to set breakpoints and navigate nested properties in VS.NET's Watch window.

### PATCH APIs added to HttpUtils

The same HTTP Utils extension methods for Post and Put now also have `Patch()` equivalents.

## ServiceStack.Redis

Transaction support for Complex Type APIs was added to Redis and we've improved resiliency for dealing with failing Sentinels. The Default Auto Retry Timeout was also increased from 3 to 10 seconds.

## PocoDynamo

PocoDynamo now has Typed API support for DynamoDB Conditional Expressions and C# int and string enums.

## Stripe

The `StripeGateway` now supports sending and Receiving Customer `Metadata`, `BusinessVatId` and returning the Customer's `Currency`.

## Find free Tcp Port

The new `HostContext.FindFreeTcpPort()` lets you find the first free TCP port within a specified port-range which you can use to start your `AppHost` on the first available port:

```csharp
var port = HostContext.FindFreeTcpPort(startingFrom:5000, endingAt:6000);
new AppHost()
    .Init()
    .Start($"http://localhost:{port}/");
```

## Other ServiceStack Features:

 - Named dependency support for Funq's AutoWired APIs was contributed by @donaldgray
 - Compression of `[CacheResponse]` responses can now be disabled with the `NoCompression` property
 - `MakeInternal` option was added to C# / F# and VB.NET Native Types to generate internal Types
 - OnError callback added to ServerEvents and new `isAuthenticated` property now being returned to Clients
 - Can disable the second Total query in AutoQuery with `IncludeTotal = false`
 - New Request/Response filters for Service Gateways requests that are also validated against any Validators

This covers most of the features, please see the [full release notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.62.md) for more details on any of the ones you're interested in.

# [v4.0.60 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md)

v4.0.60 is another jam-packed release starting with exciting new API Key and JWT Auth Providers enabling fast, stateless and centralized Auth Services, a modernized API surface for OrmLite, new GEO capabilities in Redis, Logging for Slack, performance and memory improvements across all ServiceStack and libraries including useful utilities you can reuse to improve performance in your own Apps! 

I'll try highlight the main points but I welcome you to checkout the [full v4.0.60 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#v4060-release-notes) when you can.

## Authentication

Auth Providers that authenticate with each request (i.e. implement `IAuthWithRequest`) no longer persist Users Sessions to the cache, they're just attached to the `IRequest` and only last for the duration of the Request. This should be a transparent change but can be reverted by setting `PersistSession=true`.

### [API Key Auth Provider](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#api-key-auth-provider)

The new `ApiKeyAuthProvider` provides an alternative method for allowing external 3rd Parties access to 
your protected Services without needing to specify a password. API Keys is the preferred approach for 
many well-known public API providers used in system-to-system scenarios for several reasons:

 - **Simple** - It integrates easily with existing HTTP Auth functionality
 - **Independent from Password** - Limits exposure to the much more sensitive master user passwords that 
 should ideally never be stored in plain-text. 
 - **Entropy** - API Keys are typically much more secure than most normal User Passwords. The configurable 
default has **24 bytes** of entropy (Guids have 16 bytes) 
 - **Performance** - Thanks to their much greater entropy and independence from user-chosen passwords,
 API Keys are validated as fast as possible using a datastore Index. 

Like most ServiceStack providers the new API Key Auth Provider is simple to use, integrates seamlessly with
ServiceStack existing Auth model and includes Typed end-to-end client/server support. 

We've modeled it around Stripe API Keys and provides an alternative way to authenticate using an API Key, which can be registered with just:

```csharp
Plugins.Add(new AuthFeature(...,
    new IAuthProvider[] {
        new ApiKeyAuthProvider(AppSettings),
        //...
    }));
```

And can persist API Keys in either of the following Auth Repositories: `OrmLiteAuthRepository`, `RedisAuthRepository`, `DynamoDbAuthRepository` and `InMemoryAuthRepository`.

Just like Stripe, API Keys can be sent in the Username of HTTP Basic Auth or as a HTTP Bearer Token. Example using .NET Service Clients:

```csharp
var client = new JsonServiceClient(baseUrl) {
    Credentials = new NetworkCredential(apiKey, "")
};

var client = new JsonHttpClient(baseUrl) {
    BearerToken = apiKey
};
```

And [HTTP Utils](https://github.com/ServiceStack/ServiceStack/wiki/Http-Utils):

```csharp
var response = baseUrl.CombineWith("/secured").GetStringFromUrl(
    requestFilter: req => req.AddBasicAuth(apiKey, ""));
    
var response = await "https://example.org/secured".GetJsonFromUrlAsync(
    requestFilter: req => req.AddBearerToken(apiKey));
```

### Multiple API Key Types and Environments

API Keys are automatically created when a User is registered, a key is created for each Key **Type** and **Environment**. By default it creates a "secret" API Key for both "live" and "test" environments, you could change this to also create "publishable" API Keys as well with:

```csharp
Plugins.Add(new AuthFeature(...,
    new IAuthProvider[] {
        new ApiKeyAuthProvider(AppSettings) {
            KeyTypes = new[] { "secret", "publishable" },
        }
    });
```

If preferred properties can also be set in [AppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings):

```xml
<add key="apikey.KeyTypes" value="secret,publishable" />
```


### Multitenancy by API Keys 

Thanks to the ServiceStack's trivial support for [Multitenancy](https://github.com/ServiceStack/ServiceStack/wiki/Multitenancy) you can easily change which Database your Services and AutoQuery Services use based on Key Environment by overriding `GetDbConnection()` in your AppHost, e.g:

```csharp
public override IDbConnection GetDbConnection(IRequest req = null)
{
    //If an API Test Key was used return DB connection to TestDb instead: 
    return req.GetApiKey()?.Environment == "test"
        ? TryResolve<IDbConnectionFactory>().OpenDbConnection("TestDb")
        : base.GetDbConnection(req);
}
```

## [JWT Auth Provider](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#jwt-auth-provider)

Even more exciting than the new API Key Provider is the new integrated Auth solution for the popular
[JSON Web Tokens](https://jwt.io/) (JWT) industry standard which is easily enabled by registering
the `JwtAuthProvider` with the `AuthFeature` plugin:

```csharp
Plugins.Add(new AuthFeature(...,
    new IAuthProvider[] {
        new JwtAuthProvider(AppSettings) { AuthKey = AesUtils.CreateKey() },
        new CredentialsAuthProvider(AppSettings),
        //...
    }));
```

## JWT Overview

A nice property of JWT tokens is that they allow for truly stateless authentication where API Keys and user 
credentials can be maintained in a decentralized Auth Service that's kept isolated from the rest of your 
System, making them optimal for use in Microservice architectures.

Being self-contained lends JWT tokens to more scalable, performant and flexible architectures as they don't 
require any I/O or any state to be accessed from App Servers to validate the JWT Tokens, this is unlike all 
other Auth Providers which requires at least a DB, Cache or Network hit to authenticate the user.

A good introduction into JWT is availble from the JWT website: https://jwt.io/introduction/

### Service Client Integration

Just like API Keys JWT Tokens can be sent as a HTTP Bearer Token, since we control the Service Client we're also able to enable high-level functionality like being able to transparently handle when our JWT Token expires in order to fetch a new one. Since JWT Tokens are self-contained they could instead need to retrieved from an externalized central authority service independent from the Service we're talking to. We can support this scenario by handling the `OnAuthenticationRequired` callback where we can call the Auth Service to fetch our new token with:

```csharp
var authClient = JsonServiceClient(centralAuthBaseUrl) {
    Credentials = new NetworkCredential(apiKey, "")
};

var client = new JsonServiceClient(baseUrl);
client.OnAuthenticationRequired = () => {
    client.BearerToken = authClient.Send(new Authenticate()).BearerToken;
};
```

### JWT Signature

JWT Tokens are possible courtesy of the cryptographic signature added to the end of the message that's used 
to Authenticate and Verify that a Message hasn't been tampered with. The JWT standard allows for a number of different Hashing Algorithms although requires at least the **HM256** HMAC SHA-256 to be supported which is the default. The full list of Symmetric HMAC and Asymmetric RSA Algorithms `JwtAuthProvider` supports include:

 - **HM256** - Symmetric HMAC SHA-256 algorithm
 - **HS384** - Symmetric HMAC SHA-384 algorithm
 - **HS512** - Symmetric HMAC SHA-512 algorithm
 - **RS256** - Asymmetric RSA with PKCS#1 padding with SHA-256
 - **RS384** - Asymmetric RSA with PKCS#1 padding with SHA-384
 - **RS512** - Asymmetric RSA with PKCS#1 padding with SHA-512

HMAC is the simplest to use as it lets you use the same AuthKey to Sign and Verify the message. 

But if preferred you can use a RSA Keys to sign and verify tokens by changing the `HashAlgorithm` and 
specifying a RSA Private Key:

```csharp
new JwtAuthProvider(AppSettings) { 
    HashAlgorithm = "RS256",
    PrivateKeyXml = AppSettings.GetString("PrivateKeyXml") 
}
```

### Encrypted JWE Tokens

Something that's not immediately obvious is that while JWT Tokens are signed to prevent tampering and 
verify authenticity, they're not encrypted and can easily be read by decoding the URL-safe Base64 string.
This is a feature of JWT where it allows Client Apps to inspect the User's claims and hide functionality
they don't have access to, it also means that JWT Tokens are debuggable and can be inspected for whenever 
you need to track down unexpected behavior.

But there may be times when you want to embed sensitive information in your JWT Tokens in which case you'll
want to enable Encryption, which can be done with:

```csharp
new JwtAuthProvider(AppSettings) { 
    PrivateKeyXml = AppSettings.GetString("PrivateKeyXml"),
    EncryptPayload = true
}
```

When turning on encryption, tokens are instead created following the [JSON Web Encryption (JWE)](https://tools.ietf.org/html/rfc7516#section-3) standard where they'll be encoded in the 5-part [JWE Compact Serialization](https://tools.ietf.org/html/rfc7516#section-3.1) format.

### Stateless Auth Microservices

One of JWT's most appealing features is its ability to decouple the System that provides User Authentication Services and issues tokens from all the other Systems but are still able provide protected Services although no longer needs access to a User database or Session data store to facilitate it, as sessions can now be embedded in Tokens and its state maintained and sent by clients instead of accessed from each App Server. This is ideal for Microservice architectures where Auth Services can be isolated into a single externalized System.

With this use-case in mind we've decoupled `JwtAuthProvider` in 2 classes:

 - [JwtAuthProviderReader](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Auth/JwtAuthProviderReader.cs) - 
Responsible for validating and creating Authenticated User Sessions from tokens
 - [JwtAuthProvider](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Auth/JwtAuthProvider.cs) -
Inherits `JwtAuthProviderReader` to also be able to Issue, Encrypt and provide access to tokens

#### Services only Validating Tokens

This lets us configure our Microservices that we want to enable Authentication via JWT Tokens down to just:

```csharp
public override void Configure(Container container)
{
    Plugins.Add(new AuthFeature(() => new AuthUserSession(),
        new IAuthProvider[] {
            new JwtAuthProviderReader(AppSettings) {
                HashAlgorithm = "RS256",
                PublicKeyXml = AppSettings.GetString("PublicKeyXml")
            },
        }));
}
```

Which no longer needs access to a [IUserAuthRepository](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization#userauth-persistence---the-iuserauthrepository) or [Sessions](https://github.com/ServiceStack/ServiceStack/wiki/Sessions) since they're populated entirely from JWT Tokens. Whilst you can use the default **HS256** HashAlgorithm, RSA is ideal for this use-case as you can limit access to the **PrivateKey** to only the central Auth Service issuing the tokens and then only distribute the **PublicKey** to each Service which needs to validate them.

### Ajax Clients

Using Cookies is the [recommended way for using JWT Tokens in Web Applications](https://stormpath.com/blog/where-to-store-your-jwts-cookies-vs-html5-web-storage) since the `HttpOnly` Cookie flag will prevent it from being accessible from JavaScript making them immune to XSS attacks whilst the `Secure` flag will ensure that the JWT Token is only ever transmitted over HTTPS.

You can convert your Session into a Token and set the **ss-jwt** Cookie in your web page by sending an Ajax request to `/session-to-token`, e.g:

```javascript
$.post("/session-to-token");
```

Likewise this API lets you convert Sessions created by any of the OAuth providers into a stateless JWT Token.

### Switching existing Sites to JWT

Thanks to the flexibility and benefits of using stateless JWT Tokens, we've upgraded both our Single Page App
http://techstacks.io which uses Twitter and GitHub OAuth to [use JWT with a single Ajax call](https://github.com/ServiceStackApps/TechStacks/blob/78ecd5e390e585c14f616bb27b24e0072b756040/src/TechStacks/TechStacks/js/user/services.js#L30):

```javascript
$.post("/session-to-token");
```

We've also upgraded https://servicestack.net which as it uses normal Username/Password Credentials Authentication 
(i.e. instead of redirects in OAuth), it doesn't need any additional network calls as we can add the `UseTokenCookie`
option as a hidden variable in our FORM request:

```html
<form id="form-login" action="/auth/login">
    <input type="hidden" name="UseTokenCookie" value="true" />
    ...
</form>
```

Which just like `ConvertSessionToToken` adds returns a populated session in the **ss-tok** Cookie so now 
both [techstacks.io](http://techstacks.io) and [servicestack.net](https://servicestack.net) can maintain 
uninterrupted Sessions across multiple redeployments without a persistent Sessions cache.

## [Modernized OrmLite API Surface](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#cleaner-modernized-api-surface)

As [mentioned in the last release](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/release-notes.md#deprecating-legacy-ormlite-apis) we've moved OrmLite's deprecated APIs into the `ServiceStack.OrmLite.Legacy` namespace leaving a clean, modern API surface in OrmLite's default namespace.

This primarily affects the original OrmLite APIs ending with `*Fmt` which were used to provide a familiar API for C# developers based on C#'s `string.Format()`, e.g:

```csharp
var tracks = db.SelectFmt<Track>("Artist = {0} AND Album = {1}", 
    "Nirvana", "Nevermind");
```

Whilst you can continue using the legacy API by adding the `ServiceStack.OrmLite.Legacy` namespace, it's also a good time to consider switching using any of the recommended parameterized APIs below:

```csharp
var tracks = db.Select<Track>(x => x.Artist == "Nirvana" && x.Album == "Nevermind");

var q = db.From<Track>()
    .Where(x => x.Artist == "Nirvana" && x.Album == "Nevermind");
var tracks = db.Select(q);

var tracks = db.Select<Track>("Artist = @artist AND Album = @album", 
    new { artist = "Nirvana", album = "Nevermind" });

var tracks = db.SqlList<Track>(
    "SELECT * FROM Track WHERE Artist = @artist AND Album = @album",
    new { artist = "Nirvana", album = "Nevermind" });
```

### Parameterized by default

The `OrmLiteConfig.UseParameterizeSqlExpressions` option that could be used to disable parameterized 
SqlExpressions and revert to using in-line escaped SQL has been removed in along with all its dependent 
functionality, so now all queries just use db params.

### Improved partial Updates and Inserts APIs

One of the limitations we had with using LINQ Expressions was the [lack of support for assignment expressions](http://stackoverflow.com/a/16847364/85785) which meant we previously needed to do capture which fields you wanted updated in partial updates, e.g:

```csharp
db.UpdateOnly(new Poco { Age = 22 }, onlyFields:p => p.Age, where:p => p.Name == "Justin Bieber");

//increments age by 1
db.UpdateAdd(new Poco { Age = 1 }, onlyFields:p => p.Age, where:p => p.Name == "Justin Bieber");
```

Taking a [leaf from PocoDynamo](https://github.com/ServiceStack/PocoDynamo#updating-an-item-with-pocodynamo) we've added a better API using a lambda expression which now saves us from having to specify which fields to update twice since we're able to infer them from the returned Member Init Expression, e.g:

```csharp
db.UpdateOnly(() => new Poco { Age = 22 }, where: p => p.Name == "Justin Bieber");

//increments age by 1
db.UpdateAdd(() => new Poco { Age = 1 }, where: p => p.Name == "Justin Bieber");
```

With async equivalents also available:

```csharp
await db.UpdateOnlyAsync(() => new Poco { Age = 22 }, where: p => p.Name == "Justin Bieber");
await db.UpdateOnlyAsync(() => new Poco { Age = 1 }, where: p => p.Name == "Justin Bieber");
```

This feature is extended for partial INSERT's as well:

```csharp
db.InsertOnly(() => new Poco { Name = "Justin Bieber", Age = 22 });

await db.InsertOnlyAsync(() => new Poco { Name = "Justin Bieber", Age = 22 });
```

### New ColumnExists API

We've added support for a Typed `ColumnExists` API across all supported RDBMS's which makes it easy to
inspect the state of an RDBMS Table which can be used to determine what modifications you want on it, e.g:

```csharp
db.DropColumn<Poco>(x => x.Ssn);
db.ColumnExists<Poco>(x => x.Ssn); //= false

if (!db.ColumnExists<Poco>(x => x.Age)) //= false
    db.AddColumn<Poco>(x => x.Age);
db.ColumnExists<Poco>(x => x.Age); //= true
```

### New SelectMulti API

Previously the only Typed API available to select data across multiple joined tables was to use a [Custom POCO with all the columns](https://github.com/ServiceStack/ServiceStack.OrmLite#selecting-multiple-columns-across-joined-tables) you want from any of the joined tables, e.g:

```
List<FullCustomerInfo> customers = db.Select<FullCustomerInfo>(
    db.From<Customer>().Join<CustomerAddress>());
```

The new `SelectMulti` API now lets you use your existing POCO's to access results from multiple joined tables by returning them in a Typed Tuple:

```csharp
var q = db.From<Customer>()
    .Join<Customer, CustomerAddress>()
    .Join<Customer, Order>()
    .Where(x => x.CreatedDate >= new DateTime(2016,01,01))
    .And<CustomerAddress>(x => x.Country == "Australia");

var results = db.SelectMulti<Customer, CustomerAddress, Order>(q);

foreach (var tuple in results)
{
    Customer customer = tuple.Item1;
    CustomerAddress custAddress = tuple.Item2;
    Order custOrder = tuple.Item3;
}
```

We've also added support for `Select<dynamic>` providing an alternative way to fetch data from multiple tables, e.g:

```csharp
var q = db.From<Employee>()
    .Join<Department>()
    .Select<Employee, Department>((e, d) => new { e.FirstName, e.LastName, d.Name });
    
List<dynamic> results = db.Select<dynamic>(q);

foreach (dynamic result in results)
{
    string firstName = result.FirstName;
    string lastName = result.LastName;
    string deptName = result.Name;
}
```

### CustomSelect Attribute

The new `[CustomSelect]` can be used to define properties you want populated from a Custom SQL Function or Expression instead of a normal persisted column, e.g:

```csharp
public class Block
{
    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    [CustomSelect("Width * Height")]
    public int Area { get; set; }

    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime CreatedDate { get; set; }

    [CustomSelect("FORMAT(CreatedDate, 'yyyy-MM-dd')")]
    public string DateFormat { get; set; }
}

db.Insert(new Block { Id = 1, Width = 10, Height = 5 });

var block = db.SingleById<Block>(1);

block.Area.Print(); //= 50

block.DateFormat.Print(); //= 2016-06-08
```

## [New Redis GEO Operations](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#new-redis-geo-operations)

The latest [release of Redis 3.2.0](http://antirez.com/news/104) brings it exciting new [GEO capabilities](http://redis.io/commands/geoadd) which will let you store Lat/Long coordinates in Redis and query locations within a specified radius. 

To demonstrate this functionality we've created a new [Redis GEO Live Demo](https://github.com/ServiceStackApps/redis-geo) which lets you click on anywhere in the U.S. to find the list of nearest cities within a given radius:

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-geo/redisgeo-screenshot.png)](http://redisgeo.servicestack.net/)

> Live Demo: http://redisgeo.servicestack.net

## [Slack Logger](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#slack-logger)

The new Slack Logger can be used to send Logging to a custom Slack Channel which is a nice interactive way 
for your development team on Slack to see and discuss logging messages as they come in.

To start using it first download it from NuGet:

    PM> Install-Package ServiceStack.Logging.Slack

Then configure it with the channels you want to log it to, e.g:

```csharp
LogManager.LogFactory = new SlackLogFactory("{GeneratedSlackUrlFromCreatingIncomingWebhook}", 
    debugEnabled:true)
{
    //Alternate default channel than one specified when creating Incoming Webhook.
    DefaultChannel = "other-default-channel",
    //Custom channel for Fatal logs. Warn, Info etc will fallback to DefaultChannel or 
    //channel specified when Incoming Webhook was created.
    FatalChannel = "more-grog-logs",
    //Custom bot username other than default
    BotUsername = "Guybrush Threepwood",
    //Custom channel prefix can be provided to help filter logs from different users or environments. 
    ChannelPrefix = System.Security.Principal.WindowsIdentity.GetCurrent().Name
};

LogManager.LogFactory = new SlackLogFactory(appSettings);
```

Some more usage examples are available in [SlackLogFactoryTests](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Logging.Tests/UnitTests/SlackLogFactoryTests.cs).

## Performance and Memory improvements

Several performance and memory usage improvements were also added across the board in this release where all ServiceStack libraries have now switched to using a ThreadStatic `StringBuilder` Cache where possible to reuse existing `StringBuilder` instances and save on Heap allocations. 

For similar improvements you can also use the new `StringBuilderCache` in your own code where you'd just need to call `Allocate()` to get access to a reset `StringBuilder` instance and call `ReturnAndFree()` when you're done to access the `string` and return the `StringBuilder` to the cache, e.g:

```csharp
public static string ToMd5Hash(this Stream stream)
{
    var hash = MD5.Create().ComputeHash(stream);
    var sb = StringBuilderCache.Allocate();
    for (var i = 0; i < hash.Length; i++)
    {
        sb.Append(hash[i].ToString("x2"));
    }
    return StringBuilderCache.ReturnAndFree(sb);
}
```

There's also a `StringBuilderCacheAlt` for when you need access to 2x StringBuilders at the same time.

### String Parsing

We've switched to new APIs that have the same behavior as the existing `SplitOnFirst()` and `SplitOnLast()` extension methods but save allocating a temporary array:

```csharp
str.LeftPart(':')      == str.SplitOnFirst(':')[0]
str.RightPart(':')     == str.SplitOnFirst(':').Last()
str.LastLeftPart(':')  == str.SplitOnLast(':')[0]
str.LastRightPart(':') == str.SplitOnLast(':').Last()
```

### TypeConstants

We've switched to using the new [TypeConstants](https://github.com/ServiceStack/ServiceStack.Text/blob/master/src/ServiceStack.Text/TypeConstants.cs) which holds static instances of many popular empty collections and `Task<T>` results which you can reuse instead of creating new instances:

```csharp
TypeConstants.EmptyStringArray == new string[0];
TypeConstants.EmptyObjectArray == new object[0];
TypeConstants<CustomType>.EmptyArray == new T[0];
```

### CachedExpressionCompiler

We've added MVC's [CachedExpressionCompiler](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Common/CachedExpressionCompiler.cs) to **ServiceStack.Common** and where possible are now using it in-place of Compiling LINQ expressions directly in all of ServiceStack libraries.

### Object Pools

We've added the Object pooling classes that Roslyn's code-base uses in `ServiceStack.Text.Pools` which lets you create reusable object pools of instances. The available pools include:

 - `ObjectPool<T>`
 - `PooledObject<T>`
 - `SharedPools`
 - `StringBuilderPool`

### Add ServiceStack Reference Wildcards

The `IncludeType` option in all [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) languages now allow specifying a `.*` wildcard suffix on Request DTO's as a shorthand to return all dependent DTOs for that Service:

    IncludeTypes: RequestDto.*

Special thanks to [@donaldgray](https://github.com/donaldgray) for contributing this feature.

### New ServerEventsClient APIs

Use new Typed [GetChannelSubscribers APIs](https://github.com/ServiceStack/ServiceStack/commit/1476e232502f690ba1832600c221ad76c15cfda7) added to [C# ServerEventsClient](https://github.com/ServiceStack/ServiceStack/wiki/C%23-Server-Events-Client) to fetch Channel Subscribers:

```csharp
var clientA = new ServerEventsClient("A");
var channelASubscribers = clientA.GetChannelSubscribers();
var channelASubscribers = await clientA.GetChannelSubscribersAsync();
```

### RegisterServicesInAssembly

Plugins can use the new `RegisterServicesInAssembly()` API to register multiple Services in a specified assembly:

```csharp
appHost.RegisterServicesInAssembly(GetType().Assembly);
```

This summary touches on the the main highlights, more features and further details are available in the [full v4.0.60 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.60.md#v4060-release-notes).

# [v4.0.56 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.56.md)

This is another release jam-packed with some killer features, the release notes are unfortunately quite longer than usual as the new features required more detail to describe what each does and understand how they work. 

We'll list the highlights below to provide a quick overview, but when you can please checkout the full
[v4.0.56 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.56.md) for the finer details of each feature.

## [ServiceStack VS Templates Update](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/servicestackvs/v1.0.22.md)

React Desktop Apps received a major update with much faster Startup times on Windows that now features auto-updating support built-in courtesy of [Squirrel Windows][2] integration! Whilst the OSX App can now be built using Xamarin's free Community Edition :smile:  

We're now all-in with React and TypeScript with both our VS.NET SPA React Templates modernized with TypeScript + JSPM. If you're new to both, please checkout the comprehensive [TypeScript + Redux walk through][1] to get up and running quickly. 

## [New AutoQuery Data](https://github.com/ServiceStack/ServiceStack/wiki/AutoQuery-Data)

AutoQuery Data is an alternative implementation of AutoQuery for RDBMS but supports an Open Provider model which can be implemented to query multiple data source backends. The 3 data source providers available include:

 - **[MemorySource](https://github.com/ServiceStack/ServiceStack/wiki/AutoQuery-Memory)** - for querying static or dynamic in-memory .NET collections, some of the included examples show querying a flat-file `.csv` file and a 3rd Party API that can also the throttled with configurable caching.
 - **[ServiceSource](https://github.com/ServiceStack/ServiceStack/wiki/AutoQuery-Service)** - a step higher than MemorySource where you can decorate the response of existing Services with AutoQuery's rich querying capabilities.
 - **[DynamoDbSource](https://github.com/ServiceStack/ServiceStack/wiki/AutoQuery-DynamoDb)** - adds rich querying capabilities over an AWS DynamoDB Table making it much more productive than if you had to construct the query manually.
 
AutoQuery DynamoDB queries are also self-optimizing where it will transparently construct the most optimal query possible by looking at any Hash Id's, Range Keys and Local Indexes populated in the Request to construct the most optimal DynamoDB **QueryRequest** or **Scan** Operation behind-the-scenes.

And since AutoQuery Services are just normal ServiceStack Services they get to take advantage of ServiceStack's rich ecosystem around Services so with just the single AutoQuery DynamoDB Request DTO below:
 
```csharp
[Route("/rockstar-albums")]
[CacheResponse(Duration = 60, MaxAge = 30)]
public class QueryRockstarAlbums : QueryData<RockstarAlbum>
{
    public int? Id { get; set; }         
    public int? RockstarId { get; set; }
    public string Genre { get; set; }
    public int[] IdBetween { get; set; }
}
```

We've declaratively created a fully-queryable DynamoDB AutoQuery Service that transparently executes the most ideal DynamoDB queries for each request, has it's optimal representation efficiently cached on both Server and clients, whose Typed DTO can be reused as-is on the client to call Services with an end-to-end Typed API using any .NET Service Client, that's also available to external developers in a clean typed API, natively in their preferred language of choice, accessible with just a right-click menu integrated inside VS.NET, Xcode, Android Studio, IntelliJ and Eclipse - serving both PCL Xamarin.iOS/Android as well as native iOS and Android developers by just Adding a ServiceStack Reference to the base URL of a remote ServiceStack Instance - all without needing to write any implementation!

## [HTTP Caching](https://github.com/ServiceStack/ServiceStack/wiki/HTTP-Caching)

HTTP Caching is another big feature we expect to prove extremely valuable which much improves story around HTTP Caching that transparently improves the behavior of existing ToOptimized Cached Responses, provides a typed API to to opt-in to HTTP Client features, introduces a simpler declarative API for enabling both Server and Client Caching of Services and also includes Cache-aware clients that are able to improve the performance and robustness of all existing .NET Service Clients - functionality that's especially valuable to bandwidth-constrained Xamarin.iOS / Xamarin.Android clients offering improved performance and greater resilience.

### [CacheResponse Attribute](https://github.com/ServiceStack/ServiceStack/wiki/CacheResponse-Attribute)

The new `[CacheResponse]` Filter Attribute provides the easiest way to enable both **HTTP Client** and 
**Server Caching** of your Services with a single Attribute declared on your Service class, method implementation
or Request DTO.

### [Cache-aware Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/Cache-Aware-Clients)

You can now create **cache-aware** versions of all .NET Service Clients that respects any caching directives
returned by your Server using the `.WithCache()` extension methods, e.g:

```csharp
IServiceClient client = new JsonServiceClient(baseUrl).WithCache(); 

IServiceClient client = new JsonHttpClient(baseUrl).WithCache();
```

Cache-aware Service Clients can dramatically improve performance by eliminating server requests entirely as well as reducing bandwidth for re-validated requests. They also offer an additional layer of resiliency as re-validated requests that result in Errors will transparently fallback to using pre-existing locally cached responses. For bandwidth-constrained environments like Mobile Apps they can dramatically improve the User Experience and as they're available in all supported PCL client platforms.

@jezzsantos also wrote a comprehensive overview about HTTP Caching in general and goes through the process of how he developed an alternative caching solution within ServiceStack in his epic [Caching Anyone post](http://www.mindkin.co.nz/blog/2016/1/5/caching-anyone).

## [Service Gateway](https://github.com/ServiceStack/ServiceStack/wiki/Service-Gateway)

The new `IServiceGateway` is another valuable capability that despite being trivial to implement on top of ServiceStack's existing message-based architecture, opens up exciting new possibilities for development of loosely-coupled [Modularized Service Architectures](https://github.com/ServiceStack/ServiceStack/wiki/Modularizing-services).

The Service Gateway is available from `base.Gateway` in both sync:

```csharp
public object Any(GetCustomerOrders request)
{
    return new GetCustomerOrders {
        Customer = Gateway.Send(new GetCustomer { Id = request.Id }),
        Orders = Gateway.Send(new QueryOrders { CustomerId = request.Id })
    };
}
```

and async versions:

```csharp
public async Task<GetCustomerOrdersResponse> Any(GetCustomerOrders request)
{
    return new GetCustomerOrdersResponse {
        Customer = await Gateway.SendAsync(new GetCustomer { Id = request.Id }),
        Orders = await Gateway.SendAsync(new QueryOrders { CustomerId = request.Id })
    };
}
```

The benefit of the Gateway is that the same above code will continue to function even if you later decided to split out your Customer and Order subsystems out into different Micro Services.

The Service Gateway also allows plugging in a Discovery Service for your Micro Services where you can happily just send Request DTO's to call Services and the Discovery Service will transparently route it to the most available Service. 

We're extremely fortunate to have @Mac and @rsafier both jump in with Service Discovery solutions straight out-of-the-gate which you can find more about in their GitHub Project home pages:

 - https://github.com/MacLeanElectrical/servicestack-discovery-consul
 - https://github.com/rsafier/ServiceStack.Discovery.Redis

## [Super CSV Support](https://github.com/ServiceStack/ServiceStack/wiki/CSV-Format#csv-deserialization-support)

We've now implemented CSV deserialization support so now all your Services can accept CSV payloads in addition to serializing to .csv. As a tabular data format it's especially useful when your Service accepts Lists of POCO's such as in Auto Batched Requests where it's now the most compact text data format to send them with using either the new `CsvServiceClient` or `.PostCsvToUrl()` HTTP Utils extension method.

A feature that sets ServiceStack's CSV support apart is that it's built on the compact and very fast JSV Format which not only can deserialize a tabular flat file of scalar values at high-speed, it also supports deeply nested object graphs which are encoded in JSV and escaped in a CSV field.

Which opens a number of interesting use-cases as you can now maintain rich code or system data in .csv flat-files to easily query them in AutoQuery Services, making it a great option for structured logging as they're now easily parsable, queryable with AutoQuery Data, analyzed with your favorite Spreadsheet or imported using CSV features or data migration tooling for your preferred RDBMS. 

Given these useful properties we've developed a CSV Request Logger that can be registered with:

```csharp
Plugins.Add(new RequestLogsFeature {
    RequestLogger = new CsvRequestLogger(),
});
```

To store request and error logs into daily logs to the following overridable locations:

- `requestlogs/{year}-{month}/{year}-{month}-{day}.csv`
- `requestlogs/{year}-{month}/{year}-{month}-{day}-errors.csv`

Error logs are also written out into a separate log file as it can be useful to view them in isolation.

## [Virtual FileSystem](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system)

To efficiently support Appending to existing files as needed by the CsvRequestLogger we've added new 
`AppendFile` API's and implementations for Memory and FileSystem Virtual File Providers:

```csharp
interface IVirtualFiles
{
    void AppendFile(string filePath, string textContents);
    void AppendFile(string filePath, Stream stream);
}
```

## [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite)

New `UpdateAdd` API's provides several Typed API's for updating existing values:

```csharp
//Increase everyone's Score by 3 points
db.UpdateAdd(new Person { Score = 3 }, fields: x => x.Score); 

//Remove 5 points from Jackson Score
db.UpdateAdd(new Person { Score = -5 }, x => x.Score, x => 
    where: x.LastName == "Jackson");

//Graduate everyone and increase everyone's Score by 2 points 
var q = db.From<Person>().Update(x => new { x.Points, x.Graduated });
db.UpdateAdd(new Person { Points = 2, Graduated = true }, q);
```

## Deprecating Legacy OrmLite API's

We're going to gracefully deprecate OrmLite's legacy API's by first deprecating them in this release to notify which API's are earmarked to move, then in a future release we'll move the extension methods under the `ServiceStack.OrmLite.Legacy` namespace to move them out of OrmLite's default namespace. 

The deprecated API's include those ending with `*Fmt` which uses C#'s old-style string formatting, e.g:

```csharp
var tracks = db.SelectFmt<Track>("Artist = {0} AND Album = {1}", 
  "Nirvana", 
  "Nevermind");
```

Ideally they should be replaced with the parameterized API's below:

```csharp
var tracks = db.Select<Track>(x => x.Artist == "Nirvana" && x.Album == "Nevermind");

var tracks = db.Select<Track>("Artist = @artist AND Album = @album", 
    new { artist = "Nirvana", album = "Nevermind" });
    
var tracks = db.SqlList<Track>(
    "SELECT * FROM Track WHERE Artist = @artist AND Album = @album",
    new { artist = "Nirvana", album = "Nevermind" });
```

The other API's that have been deprecated are those that inject an `SqlExpression<T>` e.g:

```csharp
var tracks = db.Select<Track>(q => 
    q.Where(x => x.Artist == "Nirvana" && x.Album == "Nevermind"));
```

Which should be changed to passing in the `SqlExpression<T>` by calling `db.From<T>`, e.g:

```csharp
var tracks = db.Select(db.From<Track>() 
    .Where(x => x.Artist == "Nirvana" && x.Album == "Nevermind"));
```

## [PocoDynamo](https://github.com/ServiceStack/PocoDynamo)

ServiceStack's POCO-friendly DynamoDB client has added support for DynamoDB's **UpdateItem** which lets you modify existing attributes. The easiest API to use is to pass in a partially populated POCO with containing any non-default values you want updated:

```csharp
db.UpdateItemNonDefaults(new Customer { Id = customer.Id, Age = 42 });
```

There's also a more flexible API to support each of DynamoDB UpdateItem operations, e.g:

```csharp
db.UpdateItem(customer.Id, 
    put: () => new Customer {
        Nationality = "Australian"
    },
    add: () => new Customer {
        Age = -1
    },
    delete: x => new { x.Name, x.Orders });
```

## [ServiceStack.Redis](https://github.com/ServiceStack/ServiceStack.Redis)

Additional resiliency was added in ServiceStack.Redis which can now handle re-connections for broken TCP connections happening in the middle of processing a Redis Operation.

New API's were added to remove multiple values from a Sorted Set:

```csharp
interface IRedisClient {
    long RemoveItemsFromSortedSet(string setId, List<string> values);
} 

interface IRedisNativeClient {
    long ZRem(string setId, byte[][] values);
}
```

## [ServiceStack IDEA](https://github.com/ServiceStack/ServiceStack.Java)

The ServiceStack IDEA Android Studio plugin was updated to support Android Studio 2.0.

## Community

There were a number of community plugins published in this release, check out their GitHub projects for
more info:

 - [ServiceStack.Discovery.Redis](https://github.com/MacLeanElectrical/servicestack-eventstore)
 - [ServiceStack.SimpleCloudControl](https://github.com/rsafier/ServiceStack.SimpleCloudControl)
 - [ServiceStack.Funq.Quartz](https://github.com/CodeRevver/ServiceStackWithQuartz)
 - [ServiceStack.Discovery.Consul](https://github.com/MacLeanElectrical/servicestack-discovery-consul)
 - [ServiceStack.Discovery.Redis](https://github.com/rsafier/ServiceStack.Discovery.Redis)

## Other Features

 - Changed ServiceStack.Interfaces to **Profile 328** adding support **Windows Phone 8.1**
 - New `IHasStatusDescription` can be added on Exceptions to customize their StatusDescription
 - New `IHasErrorCode` can be used to customize the ErrorCode used, instead of its Exception Type
 - New `AppHost.OnLogError` can be used to override and suppress service error logging


  [1]: https://github.com/ServiceStackApps/typescript-redux
  [2]: https://github.com/Squirrel/Squirrel.Windows


# [v4.0.54 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.54.md)

v4.0.54 is another jam-packed release with a lot of features across the board, we'll list the highlights 
here, for more details about each feature you can checkout the full
[v4.0.54 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.54.md).

**WARNING .NET 4.0 builds will cease after August 1, 2016**

We want to warn everyone that we will be upgrading all packages to .NET 4.5 and stop providing .NET 4.0 
builds after August 1, 2016 now that Microsoft no longer supports them. If you absolutely need supported 
.NET 4.0 builds after this date please leave a comment on [User Voice](https://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/12528912-continue-supporting-net-4-0-projects)

---

## [AutoQuery Viewer](https://github.com/ServiceStack/Admin)

An exciting new plugin available from the **ServiceStack.Admin** NuGet package which provides an instant automatic UI for all your AutoQuery services. As it's super quick to add we've enabled it on a number of existing live demos which you can try out:

- http://github.servicestack.net/ss_admin/
- http://northwind.servicestack.net/ss_admin/
- http://stackapis.servicestack.net/ss_admin/
- http://techstacks.io/ss_admin/

It also ships with a number of productive features out-of-the-box:

 - **Marking up Services** - Use `[AutoQueryViewer]` attribute to mark up look and default behavior of Services
 - **Filter Services** - If you have a lot of Services, this will help quickly find the service you want
 - **Authorized Only Services** - Users only see the AQ Services they're authorized to, which lets you customize the UI for what each user sees
 - **Multiple Conditions** - The UI makes it easy to create complex queries with multiple conditions
 - **Updated in Real-Time** - AQ Services are refreshed and App State is saved as-you-type
 - **Change Content Type** - The short-cut links can be used to access results in your desired format
 - **Customize Columns** - Customize results to only return the columns you're interested in
 - **Sorting and Paging** - Results can be sorted by any column and paged with nav links
 
A quick showcase of some of these features are available on YouTube: https://youtu.be/YejYkCvKsuQ 

## [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query)

A number of new Enhancements were also added to AutoQuery Services:

 - **Parameterized AutoQuery** - AQ Services are now parameterized with Convention Templates converted to use db params
 - **Customizable Fields** - You can now customize which fields you want returned using new **Fields** property
 - **Named Connection** - As part of our new Multitenancy features AQ Services can be easily configured to run on multiple db's 
 - **T4 Templates** - OrmLite's T4 templates now have options for generating AutoQuery Services and named connections

## [Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)

We've added a couple of demos showing how easy it is to create rich, interactive mobile and web apps with Server Events:

## [Xamarin.Android Chat](https://github.com/ServiceStackApps/AndroidXamarinChat)

The new Xamarin.Android demo shows how to use the .NET PCL typed Server Events Client to connect to an 
existing chat.servicestack.net back-end and communicate with existing Ajax web clients. It also shows 
how to use Xamarin.Auth to authenticate with ServiceStack using Twitter and OAuth.

A quick demo is available from: https://www.youtube.com/watch?v=tImAm2LURu0 

### [Networked Time Traveller Shape Creator](https://github.com/ServiceStackApps/typescript-redux#example-9---real-time-networked-time-traveller)

We've given the existing Time Traveller Shape Creator networking capabilities which now let you "Remote Desktop" into and watch other users view the app. This was surprisingly simple to do with Redux, just 1 React Component and 2x 1-line ServiceStack ServerEvent Services.

Live demo at: http://redux.servicestack.net

### Update Channels on Live Subscriptions

You can now update the channels your active SSE subscription is connected to without re-connecting. This is enabled everywhere, in Memory + Redis SSE backends as well as typed API's for .NET and Ajax clients.

## [TypeScript React App](https://github.com/ServiceStackApps/typescript-react-template)

The new TypeScript + React VS.NET Tempalte captures what we believe is the best combination of technologies for developing rich JavaScript apps: TypeScript 1.8, React, JSPM, typings + Gulp - combined together within a single integrated, pre-configured VS.NET template. This tech suite represents our choice stack for developing rich Single Page Apps which we've used to build AutoQuery Viewer and Networked Shape Creator and currently our number #1 choice for new SPA Apps.

## [TypeScript Redux](https://github.com/ServiceStackApps/typescript-redux)

To help developers familiarize themselves with these technologies we've also published an in-depth step-by-step guide for beginners that starts off building the simplest HelloWorld TypeScript React App from scratch then slowly growing with each example explaining how TypeScript, React and Redux can be used to easily create the more complex networked Time Travelling Shape Creator, available at: https://github.com/ServiceStackApps/typescript-redux

## [ss-utils](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library)

To make it easier to use ss-utils in JavaScript projects, we're maintaining copies of ss-utils in npm, JSPM and Definitely Typed registries. We've also added a few new common utils:

 - $.ss.combinePaths
 - $.ss.createPath
 - $.ss.createUrl
 - $.ss.normalizeKey
 - $.ss.normalize
 - $.ss.postJSON
 
## [Customize JSON Responses on-the-fly](https://github.com/ServiceStack/ServiceStack/wiki/Customize-JSON-Responses)

The JSON/JSV responses for all your services can now be customized on-the-fly by your Service consumers so they're able to access your JSON responses in their preferred configuration using the `?jsconfig` modifier, e.g:

    /service?jsconfig=EmitLowercaseUnderscoreNames,ExcludeDefaultValues

It also supports the much shorter Camel Humps notation:

    /service?jsconfig=elun,edv

Most JsConfig config options are supported.

## [Improved support for Multitenancy](https://github.com/ServiceStack/ServiceStack/wiki/Multitenancy)

There are a number of new features and flexibile options available to make Multitenancy easier to support where you can easily change which DB is used at runtime based on an incoming request with a request filter.

We've added a number of examples in the release notes to show how this works.

## [ServiceClient URL Resolvers](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client#serviceclient-url-resolvers)

You can use the new `TypedUrlResolver` and `UrlResolver` delegates available on every .NET Service Client 
to change which url each request is made with.

### [ServiceStack.Discovery.Consul](https://github.com/MacLeanElectrical/servicestack-discovery-consul)

This feature makes it easy to enable high-level discovery and health/failover features as seen in the new **ServiceStack.Discovery.Consul** Community project which maintains an active list of available load-balanced ServiceStack Services as well as auto-registering the Services each instance supports taking care of managing the different endpoints for each Service where all Typed requests can be made with a single Service Client and Consul takes care of routing to the appropriate active endpoint.  

## [Multiple File Uploads](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client#multiple-file-uploads)

The new PostFilesWithRequest API's on every ServiceClient for sending mutliple file uploads with a single Request.

## Local MemoryCacheClient

The new `LocalCache` property gives your Services access to a Local Memory Cache in addition to your registered ICacheClient

## OrmLite

 - New `[EnumAsInt]` attribute can be used as an alternative to `[Flags]` for storing enums as ints in OrmLites but serialized as strings
 - Free-text SQL Expressions are now converted to Parameterized Statements
 - New SelectFields API provides an smart API to reference and return custom fields
 - All `db.Exists()` API's have been optimized to return only a single scalar value
 - Max String column definition on MySql now uses **LONGTEXT**
 
## ServiceStack.Text

 - New `JsConfig.SkipDateTimeConversion` to skip built-in Conversion of DateTime's.
 - New `ISO8601DateOnly` and `ISO8601DateTime` DateHandler formats to emit only the Date or Date and Time 

## [Stripe Gateway](https://github.com/ServiceStack/Stripe)

Support added to support Stripe's unconventional object notation for complex Requests. This feature is used
in the new `CreateStripeAccount` API.

## Minor ServiceStack Features

 - Old Session removed and invalided when generating new session ids for a new AuthRequest
 - New ResourcesResponseFilter, ApiDeclarationFilter and OperationFilter added to SwaggerFeature to modify response
 - `Name` property added to `IHttpFiles` in Response.Files collection
 - `HostType`, `RootDirectoryPath`, `RequestAttributes`, `Ipv4Addresses` and `Ipv6Addresses` added to [?debug=requestinfo](https://github.com/ServiceStack/ServiceStack/wiki/Debugging#request-info)
 - `StaticFileHandler` now has `IVirtualFile` and `IVirtualDirectory` constructor overloads
 - New `StaticContentHandler` for returning custom text or binary responses in `RawHttpHandlers`

And that's a wrap for this release, apologies for the length of the TL;DR. For even more details on each feature please see the release notes: https://servicestack.net/release-notes


# [v4.0.52 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/v4.0.52.md)

We've hope everyone's had a great X-mas holidays and are super-charged for a productive 2016!

To kick things off we're making your ServiceStack Services even more attractive to mobile and Java developers 
with first-class support for JetBrains modern and highly-productive [Kotlin](https://kotlinlang.org/) 
programming language including integration into Android Studio and enhanced support in Android and 
JsonServiceClients.

## [Kotlin](https://kotlinlang.org/) - a better language for Android and the JVM

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/android-studio-splash-kotlin.png)

Whilst Java is the flagship language for the JVM, it's slow evolution, lack of modern features and distasteful
language additions have grown it into an cumbersome language to develop with, as illustrated in the 
[C# 101 LINQ Examples in Java](https://github.com/mythz/java-linq-examples) where it's by far the worst of all 
modern languages compared, making it a poor choice for a functional-style of programming that's especially painful
for Android development which is stuck on Java 7.

### [101 LINQ Examples in Kotlin](https://github.com/mythz/kotlin-linq-examples) vs [Java](https://github.com/mythz/java-linq-examples)

By contrast Kotlin is one of the 
[best modern languages for functional programming](https://github.com/mythz/kotlin-linq-examples) that's 
vastly more expressive, readable, maintainable and safer than Java. As Kotlin is being developed by JetBrains 
it also has great tooling support in **Android Studio**, **IntelliJ** and **Eclipse** and seamlessly integrates 
with existing Java code where projects can **mix-and-match Java and Kotlin** code together within the same application - 
making Kotlin a very attractive and easy choice for Android Development.

## [Kotlin Native Types!](https://github.com/ServiceStack/ServiceStack/wiki/Kotlin-Add-ServiceStack-Reference)

As we expect more Android and Java projects to be written in Kotlin in future we've added first-class 
[Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) 
support for Kotlin with IDE integration in 
[Android Studio](http://developer.android.com/tools/studio/index.html) and 
[IntelliJ IDEA](https://www.jetbrains.com/idea/) where App Devlopers can create and update an end-to-end typed 
API with just a Menu Item click - enabling a highly-productive workflow for consuming ServiceStack Services!

No new IDE plugins were needed to enable Kotlin support which was added to the existing 
[ServiceStack IDEA plugin](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference#servicestack-idea-android-studio-plugin) 
that can Install or Updated to enable Kotlin ServiceStack Reference support in Android Studio or IntelliJ IDEA.

### Installing Kotlin

Kotlin support is enabled in Android Studio by installing the JetBrain's Kotlin plugin in project settings:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/kotlin/install-kotlin-plugins.png)

After it's installed, subsequent Restarts of Android Studio will now load with the **Kotlin** plugin enabled.

#### Configure Project to use Kotlin

After Kotlin is enabled in Android Studio you can configure which projects you want to have Kotlin support
by going to `Tools -> Kotlin -> Configure Kotlin in Project` on the File Menu:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/kotlin/kotlin-configure-project.png)

Configuring a project to support Kotlin just modifies that projects 
[build.gradle](https://github.com/mythz/kotlin-linq-examples/blob/master/src/app/build.gradle), applying the
necessary Android Kotlin plugin and build scripts needed to compile Kotlin files with your project. Once Kotlin
is configured with your project you'll get first-class IDE support for Kotlin `.kt` source files including 
intellisense, integrated compiler analysis and feedback, refactoring and debugging support, etc.

One convenient feature that's invaluable for porting Java code and learning Kotlin is the 
[Converting Java to Kotlin](https://kotlinlang.org/docs/tutorials/kotlin-android.html#converting-java-code-to-kotlin)
Feature which can be triggered by selecting a `.java` class and clicking `Ctrl + Alt + Shift + K` keyboard shortcut
(or using [Find Action](https://kotlinlang.org/docs/tutorials/kotlin-android.html#converting-java-code-to-kotlin)).

### Kotlin Add ServiceStack Reference

To add a ServiceStack Reference, right-click (or press `Ctrl+Alt+Shift+R`) on the **Package folder** in your 
Java sources where you want to add the POJO DTO's. This will bring up the **New >** Items Context Menu where 
you can click on the **ServiceStack Reference...** Menu Item to open the **Add ServiceStack Reference** Dialog: 

![Add ServiceStack Reference Kotlin Context Menu](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/kotlin/package-add-servicestack-reference.png)

The **Add ServiceStack Reference** Dialog will be partially populated with the selected **Package** with the 
package where the Dialog was launched from and the **File Name** defaulting to `dtos.kt` where the generated 
Kotlin DTO's will be added to. All that's missing is the url of the remote ServiceStack instance you wish to 
generate the DTO's for, e.g: `http://techstacks.io`:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/kotlin/kotlin-add-servicestack-reference.png)

Clicking **OK** will add the 
[dtos.kt](https://github.com/ServiceStackApps/TechStacksKotlinApp/blob/master/src/TechStacks/app/src/main/java/servicestack/net/techstackskotlin/dtos.kt)
file to your project and modifies the current Project's **build.gradle** 
file dependencies list with the new **net.servicestack:android** dependency containing the JSON 
ServiceClients which is used together with the remote Servers DTO's to enable its typed Web Services API. If
for some reason you wish to instead add Java DTO's to your project instead of Kotlin, just rename the `dtos.kt` 
file extension to `dtos.java` and it will import Java classes instead.

> As the Module's **build.gradle** file was modified you'll need to click on the **Sync Now** link in the top yellow banner to sync the **build.gradle** changes which will install or remove any modified dependencies.


### Update ServiceStack Reference

Like other Native Type languages, the generated DTO's can be further customized by modifying any of the options available in the header comments:

```
/* Options:
Date: 2015-04-17 15:16:08
Version: 1
BaseUrl: http://techstacks.io

Package: org.layoric.myapplication
GlobalNamespace: techstackdtos
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: java.math.*,java.util.*,net.servicestack.client.*,com.google.gson.annotations.*
*/
...
```

For example the package name can be changed by uncommenting the **Package:** option with the new package name, then either right-click on the file to bring up the file context menu or use Android Studio's **Alt+Enter** keyboard shortcut then click on **Update ServiceStack Reference** to update the DTO's with any modified options:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/kotlin/kotlin-update-servicestack-reference.png)


### JsonServiceClient Usage

Both Java and Kotlin use the same `JsonServiceClient` that you need to initialize with the **BaseUrl** of 
the remote ServiceStack instance you want to access, e.g:

```kotlin
val client = JsonServiceClient("http://techstacks.io")
```

> The JsonServiceClient is made available after the [net.servicestack:android](https://bintray.com/servicestack/maven/ServiceStack.Android/view) package is automatically added to your **build.gradle** when adding a ServiceStack reference.

Typical usage of the Service Client is the same in .NET where you just need to send a populated Request DTO 
and the Service Client will return a populated Response DTO, e.g:

```kotlin
val response:AppOverviewResponse? = client.get(AppOverview())
val allTiers:ArrayList<Option> = response.AllTiers
val topTech:ArrayList<TechnologyInfo> = response.TopTechnologies
```

As Kotlin has proper type inference, the explicit types are unnecessary. Here's a typical example using a populated Request DTO:

```kotlin
var request = GetTechnology()
request.Slug = "servicestack"

val response = client.get(request)
```

### Custom Route Example

When preferred you can also consume Services using a custom route by supplying a string containing the route 
and/or Query String. As no type info is available you'll need to specify the Response DTO class to deserialize 
the response into, e.g:

```kotlin
val response = client.get("/overview", OverviewResponse::class.java)

//Using an Absolute Url:
val response = client.get("http://techstacks.io/overview", OverviewResponse::class.java)
```

### AutoQuery Example Usage

You can also send requests composed of both a Typed DTO and untyped String Map by providing a Hash Map of 
additional args. This is typically used when querying 
[implicit conventions in AutoQuery services](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#implicit-conventions), e.g:

```kotlin
val response = client.get(FindTechnologies(), hashMapOf(Pair("DescriptionContains","framework")))
```

## AndroidServiceClient Async Usage

To make use of Async API's in an Android App (which you'll want to do to keep web service requests off the 
Main UI thread), you'll instead need to use an instance of `AndroidServiceClient` which as it inherits 
`JsonServiceClient` can be used to perform both Sync and Async requests which can be instantiated with:

```kotlin
val client = AndroidServiceClient("http://techstacks.io")
```
To provide an optimal experience for Kotlin and Java 8, we've added 
[SAM overloads](https://kotlinlang.org/docs/reference/java-interop.html#sam-conversions) 
using the new `AsyncSuccess<T>`, `AsyncSuccessVoid` and `AsyncError` interfaces which as they only contain 
a single method are treated like a lambda in Kotlin and Java 8 allowing you to make async requests with just:

```kotlin
client.getAsync(Overview(), AsyncSuccess<OverviewResponse> {
        var topUsers = it.TopUsers
    }, AsyncError {
        it.printStackTrace()
    })
```

Instead of the previously more verbose anonymous AsyncResult interface used in Java 7:

```kotlin
client.getAsync(Overview(), object: AsyncResult<OverviewResponse>() {
    override fun success(response: OverviewResponse?) {
        var topUsers = response!!.TopUsers
    }
    override fun error(ex: Exception?) {
        ex?.printStackTrace()
    }
})
```

### Custom Service Examples

Just like the `JsonServiceClient` Sync examples above there a number of flexible options for executing 
Custom Async Requests, e.g: 

```kotlin
//Relative Url:
client.getAsync("/overview", OverviewResponse::class.java,
    AsyncSuccess<OverviewResponse?> {  
    })

//Absolute Url:
client.getAsync("http://techstacks.io/overview", OverviewResponse::class.java,
    AsyncSuccess<OverviewResponse>() {
    })

//AutoQuery Example:
client.getAsync(FindTechnologies(), hashMapOf(Pair("DescriptionContains", "framework")),
    AsyncSuccess<QueryResponse<Technology>>() {
    })
```

#### Download Raw Image Async Example

Example downloading raw Image bytes and loading it into an Android Image `Bitmap`:

```kotlin
client.getAsync("https://servicestack.net/img/logo.png", {
    val img = BitmapFactory.decodeByteArray(it, 0, it.size);
})
```

#### Send Raw String or byte[] Requests

You can easily get the raw string Response from Request DTO's that return are annotated with `IReturn<string>`, e.g:
 
```java
open class HelloString : IReturn<String> { ... }

var request = HelloString()
request.name = "World"

val response:String? = client.get(request)
```

You can also specify that you want the raw UTF-8 `byte[]` or `String` response instead of a the deserialized 
Response DTO by specifying the Response class you want returned, e.g:

```kotlin
val response:ByteArray = client.get("/hello?Name=World", ByteArray::class.java);
```

### Typed Error Handling

Thanks to Kotlin also using typed Exceptions for error control flow, error handling in Kotlin will be instantly 
familiar to C# devs which also throws a typed `WebServiceException` containing the remote servers structured 
error data:

```kotlin
var request = ThrowType()
request.Type = "NotFound"
request.message = "not here"

try {
    val response = client.post(request)
} catch(webEx: WebServiceException) {
    val status = webEx.responseStatus
    status.message    //= not here
    status.stackTrace //= (Server StackTrace)
}
```

### Async Error Handling

Whilst Async Error handlers can cast into a `WebServiceException` to access the structured error response:

```kotlin
client.postAsync(ThrowError(), AsyncSuccess<ThrowErrorResponse> { },
    AsyncError {
        val webEx = it as WebServiceException

        val status = webEx.responseStatus
        status.message    //= not here
        status.stackTrace //= (Server StackTrace)
    })
```

### JsonServiceClient Error Handlers

To make it easier to generically handle Web Service Exceptions, the Java Service Clients also support static Global Exception handlers by assigning `AndroidServiceClient.GlobalExceptionFilter`, e.g:
```kotlin
AndroidServiceClient.GlobalExceptionFilter = ExceptionFilter { res:HttpURLConnection?, ex ->
}
```

As well as local Exception Filters by specifying a handler for `client.ExceptionFilter`, e.g:
```kotlin
client.ExceptionFilter = ExceptionFilter { res:HttpURLConnection?, ex ->
}
```

See the [Kotlin Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Kotlin-Add-ServiceStack-Reference) 
wiki for more docs on consuming ServiceStack Services from Kotlin.

## Example [TechStacks Android App](https://github.com/ServiceStackApps/TechStacksKotlinApp)

To demonstrate Kotlin Native Types in action we've ported the Java 
[TechStacks Android App](https://github.com/ServiceStackApps/TechStacksAndroidApp) to a native 
Android App written in Kotlin to showcase the responsiveness and easy-of-use of leveraging 
Kotlin Add ServiceStack Reference in Android Projects. 

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-kotlin-app.png)](https://play.google.com/store/apps/details?id=test.servicestack.net.techstackskotlin)

Checkout the [TechStacks Kotlin Android App](https://github.com/ServiceStackApps/TechStacksKotlinApp) 
repository for a nice overview of how it leverages Kotlin Native Types and iOS-inspired Data Binding to easily 
develop services-heavy Mobile Apps.

### Kotlin Android Resources

To help getting started, here are some useful resources helping to develop Android Apps with Kotlin:

 - [Getting started with Android and Kotlin](https://kotlinlang.org/docs/tutorials/kotlin-android.html) <small>_(kotlinlang.org)_</small>
 - [Kotlin for Android Developers](http://www.javaadvent.com/2015/12/kotlin-android.html) <small>_(javaadvent.com)_</small>
 - [Android Development with Kotlin - Jake Wharton](https://www.youtube.com/watch?v=A2LukgT2mKc&feature=youtu.be) <small>_(youtube.com)_</small>


## Improved [Java and Android ServiceClient](https://github.com/ServiceStack/ServiceStack.Java)

### Fewer dependencies

We've continued making improvements to Java and Android Service Clients which now have fewer dependencies
initially triggered by [Google removing Apache HTTP Client](http://developer.android.com/about/versions/marshmallow/android-6.0-changes.html#behavior-apache-http-client)
in Android 6.0. Previously the `AndroidServiceClient` **net.servicestack:android** package contained dependencies
on the pure Java **net.servicestack:client** package as well as the external Apache Client. Both dependencies
have been removed, the **android** package now uses the HTTP classes built into Android and all **client** classes
have been merged into the **android** package. 

The **net.servicestack:client** package continues to be available for pure Java clients and remains 
source-compatible with the `JsonServiceClient` classes in the **android** package.

### Integrated Basic Auth

We've added HTTP Basic Auth support to `JsonServiceClient` following the implementation in .NET Service Client
where you can specify the user's credentials and whether you always want to send Basic Auth with each request by:

```java
client.setCredentials(userName, password);
client.setAlwaysSendBasicAuthHeaders(true);

TestAuthResponse response = client.get(new TestAuth());
```

It also supports processing challenged 401 Auth HTTP responses where it will transparently replay the failed 
request with the Basic Auth Headers:

```java
client.setCredentials(userName, password);

TestAuthResponse response = client.get(new TestAuth());
```

Although this has the additional latency of waiting for a failed 401 response before sending an authenticated request.

### Cookies-enabled Service Client

The `JsonServiceClient` now initializes a `CookieManager` in its constructor to enable any Cookies received to
be added on subsequent requests to allow you to make authenticated requests after authenticating, e.g:

```java
AuthenticateResponse authResponse = client.post(new Authenticate()
    .setProvider("credentials")
    .setUserName(userName)
    .setPassword(password));

TestAuthResponse response = client.get(new TestAuth());
``` 

## [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite)

### Async PostgreSQL Support

The [Npgsql](http://www.npgsql.org) ADO.NET PostgreSQL provider [underwent a major upgrade in v3](http://www.npgsql.org/npgsql-3.0-released/)
where [existing 2.x versions](http://www.npgsql.org/2.2.6-release/) are now considered obsolete. As a 
result we've upgraded 
[ServiceStack.OrmLite.PostgreSQL](https://www.nuget.org/packages/ServiceStack.OrmLite.PostgreSQL/)
to use the latest **v3.0.5** of Npgsql which is only available for .NET 4.5+ projects. We're also distributing
.NET 4.0 builds of OrmLite in the same NuGet package but you'll need to manually reference the **2.x** Npgsql 
dependency which contains .NET 4.0 .dll. 

The primary benefit of upgrading means PostgreSQL now has true Async support where you can now use all of
[OrmLite's Async APIs](https://github.com/ServiceStack/ServiceStack.OrmLite#async-api-overview) with PostgreSQL!
See [ApiPostgreSqlTestsAsync.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLiteV45.Tests/ApiPostgreSqlTestsAsync.cs)
for a number of Async API's in action.

### Parameterized SQL Expressions

When typed SQL Expressions were first added they constructed SQL inline. We've been slow to migrate to using 
parameterized queries which was first enabled for Oracle behind a 
`OrmLiteConfig.UseParameterizeSqlExpressions = true;` flag. We've since carefully maintained separate code-paths
to ensure any migration efforts wouldn't affect existing queries. In the last release we extended support to 
SQL Server under the same flag. 

In this release we've enabled it for every RDBMS provider and have made parameterized queries the default. 

So now typed SQL Expressions will use parameterized queries by default, e.g:

```csharp
db.Select<Person>(x => x.Age > 40);
db.GetLastSql(); //= SELECT "Id", "FirstName", "LastName", "Age" FROM "Person" WHERE ("Age" > @0)
```

For now all supported RDBMS's can still opt-in to revert to in-line SQL with:

```csharp
OrmLiteConfig.UseParameterizeSqlExpressions = false;

db.Select<Person>(x => x.Age > 40);
db.GetLastSql(); //= SELECT "Id", "FirstName", "LastName", "Age" FROM "Person" WHERE ("Age" > 40)
```

However we've deprecated `OrmLiteConfig.UseParameterizeSqlExpressions` as we want to remove the legacy code-paths 
as soon as possible. Our entire test suite passes under either option so we expect this change to be a transparent 
implementation detail not affecting existing behavior. However if you do find any issues with this change please 
[submit them to us](https://github.com/ServiceStack/Issues) as we plan to remove the legacy implementation 
if there aren't any reported issues. In the meantime you can use the above flag to revert to the existing behavior.

More examples showing the difference between in-line and parameterized SQL Expressions can be seen in:

 - [ApiSqlServerTests.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/777084da84dadf972b63b0caef02b7801f63935b/tests/ServiceStack.OrmLite.Tests/ApiSqlServerTests.cs)
 - [ApiSqlServerTests.NonParam.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/777084da84dadf972b63b0caef02b7801f63935b/tests/ServiceStack.OrmLite.Tests/ApiSqlServerTests.NonParam.cs)

### Parameterized Updates

In the migration to Parameterized queries we've also migrated the Update API's, e.g:

```csharp
db.Update(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix" });
db.GetLastSql() //= UPDATE "Person" SET "FirstName"=@FirstName, "LastName"=@LastName WHERE "Id"=@Id
```

### Parameterized ExecuteSql

The new `ExecuteSql()` API makes it easy to execute Custom SQL with parameterized values using an anon object:

```csharp
Db.ExecuteSql("UPDATE page_stats SET fav_count = @favCount WHERE ref_id = @refId and ref_type = 'tech'",
              new { refId = techFav.Key, favCount = techFav.Value });
```

Whilst the Async alternative is useful for non-blocking fire-and-forget RDBMS updates, e.g. TechStacks uses this for 
[updating page view counts](https://github.com/ServiceStackApps/TechStacks/blob/06acc775a65b74610b43878aea4543cbd7a9912c/src/TechStacks/TechStacks.ServiceInterface/TechExtensions.cs#L53):
    
```csharp
Db.ExecuteSqlAsync("UPDATE page_stats SET view_count = view_count + 1 WHERE id = @id", new { id })
```

### LoadSelect Typed Include References

Similar to `LoadSingleById`, there's now a typed API that lets you selectively load references in `LoadSelect` queries, e.g:

```csharp
var customers = db.LoadSelect<Customer>(x => x.Name.StartsWith("A"), 
    include: x => new { x.PrimaryAddress });
```

## Updated [Stripe Gateway](https://github.com/ServiceStack/Stripe)

StripeGateway has been updated to use the latest 2015-10-13 API version thanks to [@jklemmack](https://github.com/jklemmack).
Please refer to [Stripe's API Chengelog](https://stripe.com/docs/upgrades#api-changelog) for the complete list of
API changes. Most of the API Collections now supports paging with the `Limit`, `StartingAfter` and `EndingBefore`
properties added on the Request DTO's.

There were a few source-incompatible breaking changes where `Cards` have been renamed to `Sources`, `Last4` was
renamed to `DynamicLast4` and the collection `Count` is renamed to `TotalCount` so to access the total size of
a collection, instead of:

```csharp
response.Cards.Count
```

You'd now use:

```csharp
response.Sources.TotalCount
```

### Customize urls used with `IUrlFilter`

Request DTO's can customize urls used in Service Clients or any libraries using ServiceStack's typed 
[Reverse Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing) by having 
Request DTO's implement 
[IUrlFilter](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/IUrlFilter.cs).

ServiceStack's [Stripe Gateway](https://github.com/ServiceStack/Stripe) takes advantage of ServiceStack's
typed Routing feature to implement its 
[Open-Ended, Declarative Message-based APIs](https://github.com/ServiceStack/Stripe#open-ended-declarative-message-based-apis)
with minimal effort.

In order to match Stripe's unconventional syntax for specifying arrays on the QueryString of their 3rd party
REST API we use `IUrlFilter` to customize the url that's used. E.g. we need to specify `include[]` in order
for the Stripe API to return any optional fields like **total_count**.

```csharp
[Route("/customers")]
public class GetStripeCustomers : IGet, IReturn<StripeCollection<StripeCustomer>>, IUrlFilter
{
    public GetStripeCustomers() 
    {
        Include = new[] { "total_count" };
    }

    [IgnoreDataMember]
    public string[] Include { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        return Include == null ? absoluteUrl 
            : absoluteUrl.AddQueryParam("include[]", string.Join(",", Include));
    }
}
```

> `[IgnoreDataMember]` is used to hide the property being emitted using the default convention

Which when sending the Request DTO:

```csharp
var response = client.Get(new GetStripeCustomers());
```

Generates and sends the relative url:

    /customers?include[]=total_count

Which has the effect of populating the `TotalCount` property in the typed `StripeCollection<StripeCustomer>` response.

## Web Framework

### Structured Request Binding Errors

Previously type conversion errors would throw a generic `RequestBindingException` to indicate the Request was
malformed. They are now being converted into a structured error so the same error handling logic used to handle
[field validation errors](https://github.com/ServiceStack/ServiceStack/wiki/Validation) can also handle request
binding exceptions, e.g:

```csharp
try
{
    var response = client.Get<RequestBinding>("/errorrequestbinding?Int=string");
}
catch (WebServiceException ex)
{
    ex.ResponseStatus.Message //= Unable to bind 'RequestBinding': Input string was not in a correct format.

    var fieldError = ex.GetFieldErrors()[0];
    fieldError.FieldName //= Int
    fieldError.ErrorCode //= SerializationException
    fieldError.Message   //= 'string' is an Invalid value for 'Int'
}
```
 
Special thanks to [@georgehemmings](https://github.com/georgehemmings) for his contributions to this feature. 
 
### Scalable [Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)

[@Nness](https://github.com/Nness) upgraded our existing Server Events implementation based on manual array 
re-sizing and custom locks to use `ConcurrentDictionary` to 
[solve their scalability issues](https://github.com/ServiceStack/ServiceStack/pull/1014)
they were having at 25-30k concurrent Server Event connections.

The default synchronous `WriteEvent` implementation can be overridden which .NET 4.5 applications can take 
advantage to use asynchronous Write and Flush API's with:

```csharp
Plugins.Add(new ServerEventsFeature
{
    WriteEvent = (res, frame) =>
    {
        var aspRes = (HttpResponseBase)res.OriginalResponse;
        var bytes = frame.ToUtf8Bytes();
        aspRes.OutputStream.WriteAsync(bytes, 0, bytes.Length)
            .Then(_ => aspRes.OutputStream.FlushAsync());
    }
});
```

## [Metadata pages](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-page)

To avoid repetitive noise in each Metadata Operation Page the common `ResposneStatus` DTO's were omitted, if
you prefer they can now be enabled with:

```csharp
this.GetPlugin<MetadataFeature>().ShowResponseStatusInMetadataPages = true;
```

## [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query)

### Querying NULL

You can implicitly query a Column is null by specifying a property with no value, e.g:

    /rockstars?DateDied=
    
Will return all Rockstars where **DateDied IS NULL**.

## Other Changes

 - `ISequenceSource` changed to use longs
 - Add Support for Fluent Validation's `RuleForEach()`
 - Use `AuthFeature.HtmlLogoutRedirect` to specify the browser redirect after logout
 - Change Exception returned by overriding `ResolveResponseException()` in AppHost

---

## [2015 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2015/release-notes.md)


