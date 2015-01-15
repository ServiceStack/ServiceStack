## Go to [2015 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2015/release-notes.md)

---

# v4.0.35 Release Notes

We're ending 2014 with a short release cycle primarily focused on a polished and fixes Release, ready for before everyone gets back at work - re-energized for a Happy New 2015 work year :)

## New [TechStacks](http://techstacks.io) LiveDemo!

We've been gradually refining our modern [AngularJS](https://github.com/ServiceStack/ServiceStackVS/blob/master/angular-spa.md) and [React](https://github.com/ServiceStackApps/Chat-React) Single Page App VS.NET templates which represents what we believe to be the optimal formula for developing future .NET-based JS Apps - utilizing a best-of-breed node.js, npm, bower, grunt/gulp build system.

To this end we're developing new Single Page Apps alongside to further refine these VS.NET templates and demonstrate their potential in using the pre-configured Grunt tasks to manage the full iterative client/server building, optimization and deployment dev workflows. 

We're happy to be able to preview the latest Live Demo built on the **AngularJS App** VS.NET template in: http://techstacks.io 

[![TechStacks](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/techstacks/screenshots/techstacks.png)](http://techstacks.io)

TechStacks is a modern [AngularJS](https://angularjs.org/) CRUD App that lets you Browse and Add Technology Stacks of popular StartUps. After Signing in you can add your own TechStacks and favorite technologies to create a personalized custom 'feed' to view Websites and Apps built with your favorite programming languages and technologies.

TechStacks is based on a [Bootstrap template](http://getbootstrap.com) with client-side features:

 - HTML5 Routing to enable pretty urls, also supports full page reloads and back button support
 - Same Services supporting both human-readable Slugs or int primary keys
 - Responsive design supporting iPad Landscape and Portrait modes
 - Preloading and background data fetching to reduce flicker and maximize responsiveness
 - [Disqus](https://disqus.com/) commenting system
 - [Chosen](http://harvesthq.github.io/chosen/) for UX-friendly multi combo boxes

and some of TechStacks back-end features include: 

 - [Twitter and GitHub OAuth Providers](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization)
 - Substitutable [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) RDBMS [PostgreSQL and Sqlite](https://github.com/ServiceStackApps/TechStacks/blob/875e78910e43d2230f0925b71d5990497216511e/src/TechStacks/TechStacks/AppHost.cs#L49-L56) back-ends
 - [Auto Query](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) for automatic services of RDBMS tables
 - [RDBMS Sessions and In Memory Caching](https://github.com/ServiceStack/ServiceStack/wiki/Caching)
 - [Smart Razor Views](http://razor.servicestack.net)
 - [Fluent Validation](https://github.com/ServiceStack/ServiceStack/wiki/Validation)

TechStacks is a good example of the experience you can get running a packaged ServiceStack/AngularJS App on modest hardware - [techstacks.io](http://techstacks.io) is currently running on a single **m1.small** AWS EC2 instance and **db.t1.micro** RDS PostgreSQL instance that hosts all [Live Demos](https://github.com/ServiceStackApps/LiveDemos).

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-client-layout.png" align="right" hspace="30" />

### [View the Source](https://github.com/ServiceStackApps/TechStacks)

Checkout the [Source Code for TechStacks](https://github.com/ServiceStackApps/TechStacks) for the full details to see how it's built. The project also includes an [example client layout](https://github.com/ServiceStackApps/TechStacks/tree/master/src/TechStacks/TechStacks/js) for structuring larger AngularJS projects in an extensible layout files and folder structure:

### HTML5 Routing and Full-page reloads

One of the disadvantages of Single Page Apps is having to resort to hash-style `#!` url suffix hacks to prevent JavaScript apps from making full-page reloads. By utilizing [AngularJS's HTML5 mode](https://docs.angularjs.org/guide/$location#html5-mode) we can take advantage of modern browsers support for HTML5 History API to retain the optimal pretty urls (we'd have if this were a server generated website) whilst still retaining the responsiveness of JS Apps which are able to load just the minimum content required, i.e. instead of waiting for the full page rendering of Server generated pages and their resource dependencies to be loaded again.

ServiceStack has great support for these modern-style SPA's which lets you specify a fallback handler for **HTML page requests** with un-matched routes to return the same `/default.cshtml` home page so AngularJS is able to handle the request and perform the same client-side routing it would've had the url been navigated from within the App - using the [AppHost configuration below](https://github.com/ServiceStackApps/TechStacks/blob/41efa5d8add1c4b0bdd449d6507878f2c8387bbc/src/TechStacks/TechStacks/AppHost.cs#L41):

```csharp
base.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/default.cshtml");
```

This lets you re-use pretty client-side routes like:

 - http://techstacks.io/tech/servicestack

And allow deep-link support for full round-trip requests (i.e. outside of AngularJS) - where since `/tech/servicestack` doesn't match any custom Server routes, ServiceStack instead responds with the above `/default.cshtml` Razor View. At which point AngularJS takes over and navigates to the internal route mapping that matches `/tech/servicestack`.

> To get the latest AngularJS and React.js App templates download the latest [ServiceStackVS VS.NET Extension](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

## ServerEvents now supports Multiple Channels per subscription

To ensure each Client only ever needs 1 ServerEvents subscription, subscriptions now support subscribing to multiple channels. Multi Channel Support is fully implemented in all [JavaScript ServerEvents](https://github.com/ServiceStack/ServiceStack/wiki/JavaScript-Server-Events-Client) and [C#/.NET ServerEvents](https://github.com/ServiceStack/ServiceStack/wiki/C%23-Server-Events-Client) Clients as well as both [back-end InMemory](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events) and [Redis ServerEvents](https://github.com/ServiceStack/ServiceStack/wiki/Redis-Server-Events) providers.

The API remains similar to the previous Single Channel Routes where in addition to subscribing to a single channel:

    /event-stream?channel=Home

Clients can also subscribe to multiple channels:

    /event-stream?channel=Home,Work,Play

> If preferred, clients can also use the more readable **?channels=** plural variable name

And the above example again using the [C#/.NET ServerEvents Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-Server-Events-Client):

```csharp
var client = new ServerEventsClient(BaseUri, "Home");

var client = new ServerEventsClient(BaseUri, "Home", "Work", "Play");
```

Multi-Channel subscriptions works conceptually similar to having multiple "single channel" subscriptions where multiple Join/Leave/Message events are fired for events occurring in each channel. For more details on this checkout the [multi-channel ServerEvents tests](https://github.com/ServiceStack/ServiceStack/blob/42d08dee1f4945f1a7be29ac234ce1250e04de9b/tests/ServiceStack.WebHost.Endpoints.Tests/ServerEventTests.cs#L781).

### Chat Apps now support multiple Chat Rooms

With this feature, we can now create Chat Apps that support multiple Chat Rooms using only a single ServerEvents subscription: 

[![React Multi-Channel Chat](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/chat-react-multichannels.png)](http://react-chat.servicestack.net/?channels=home,work,play)

> Multi-Channel React Chat preview

Surprisingly it only took a small amount of code to add support for multiple chat rooms in all the different Chat Apps which now all support Multiple Chat rooms:

 - Upgrade [jQuery Chat Client/Server](https://github.com/ServiceStackApps/Chat/commit/f23bb912791425abcba1bc724cd86cb4ab8cac82) to support multiple Chat Rooms
 - Upgrade [React Chat Client](https://github.com/ServiceStackApps/Chat-React/commit/8969ce9c291d88f63d84500b3bb281c3b1f451c7) to support multiple Chat Rooms
 - Upgrade [React Chat Server](https://github.com/ServiceStackApps/Chat-React/commit/cae43b6923771b02c28726dcfa4927d8490275ee) to support multiple Chat Rooms
 - Upgrade [ServiceStack.Gap Chat Client/Server](https://github.com/ServiceStack/ServiceStack.Gap/commit/6ee72d81fcf7cd73573b686400500d7516f312b9) to support multiple Chat Rooms

Should you want to run the previous "Single Room" Chat Apps, they're available in the **single-channel** branches:

 - [jQuery Chat](https://github.com/ServiceStackApps/Chat/tree/single-channel)
 - [React Chat](https://github.com/ServiceStackApps/Chat-React/tree/singe-channel)

> Multi-Channel support is mostly backwards compatible where all Chat Apps can be run as-is when upgraded to use the latest ServiceStack v4.0.35+ - but it does require upgrading both v4.0.35 Client and Server libraries together.

## Minor Changes and Fixes

Rest of this release was focused on minor features, changes and fixes:

### Framework Changes 

 - Custom HTTP Handlers now execute Global Request Filters [709fb73](https://github.com/ServiceStack/ServiceStack/commit/709fb73c1450f13ba6449eed9101e588775c3d9d)
 - Static Default html pages (e.g. default.html) are served directly from root instead of being redirected to static file - Behavior is now in-line with `default.cshtml` in Razor Support [5b5d7fa](https://github.com/ServiceStack/ServiceStack/commit/5b5d7fa66bbd3d4237ede8a5bc9054354dfa7b2c)
 - StaticFileHandler HTTP Handler is now re-usable `VirtualNode` for returning Static Files [8571ecd](https://github.com/ServiceStack/ServiceStack/commit/8571ecd6f7e244ee152e959e6759f6a1ee82fe4d)
 - Original C#/.NET Exception is now accessible as `InnerException` in wrapped `HttpError` [42d5976](https://github.com/ServiceStack/ServiceStack/commit/42d59767fd8ea9414470cbedbac8b2bae308e9e8)
 - Added overridable `IDbConnectionFactory` and `IRedisClientsManager` properties in `Service` base class [c18215b](https://github.com/ServiceStack/ServiceStack/commit/c18215b58a7a71f9537f8614ce42acf91beaee3b)
 - Add `.woff2` to `Config.AllowFileExtensions` white-list [aa1e93a](https://github.com/ServiceStack/ServiceStack/commit/aa1e93adcea85216aac807cad4bdbe8f71ff2f52)
 - Changed all methods in MVC ServiceStackController base class to protected to prevent MVC Controller Factories from assuming their MVC Actions [eff11c](https://github.com/ServiceStack/ServiceStack/commit/eff11c8992df78b18b07cc0137d27ea1e2d7eb47)
 - Added Remove Plugin and Debug Link API's [9002d48](https://github.com/ServiceStack/ServiceStack/commit/9002d4827c43dd91e02b298a3b5a56e6e376963a)
 - Added Retry logic on Concurrent Update collisions in `OrmLiteCacheClient` [aa6d62c](https://github.com/ServiceStack/ServiceStack/commit/aa6d62ca23ebef30eb3727f3894d214d320843b0)
 - Added Runtime Attribute Filter example [355365b](https://github.com/ServiceStack/ServiceStack/commit/355365bbfc45e1309fa2d91fcbc1856e874a9676)
 - Add support for implicit querying of enums in AutoQuery [b5d2477](https://github.com/ServiceStack/ServiceStack/commit/b5d2477c581152168f43017a355cbcae9dccbefb)
 - Handle Retry Exceptions during on `ServerEventsClient` reconnections [7833cd8](https://github.com/ServiceStack/ServiceStack/commit/7833cd8c25e0eb4dc10cd0e0033d2d156393625a)
 - Added `AppHost.GetCurrentRequest()` to allow different AppHosts to return the current HttpContext [7cbadda](https://github.com/ServiceStack/ServiceStack/commit/7cbadda18f5666a4c24a0e49fa1af740afd0fec4)
 - Fixed NRE during max pool-size overflow handling in `RedisManagerPool` [c94eedd](https://github.com/ServiceStack/ServiceStack.Redis/commit/c94eedd2e3467a418b290209fdf52b01c0516855)

### Auth Changes

 - `IAuthRepository.CreateOrMergeAuthSession()` now returns the merged `IUserAuthDetails` [f2383ff](https://github.com/ServiceStack/ServiceStack/commit/f2383fffd390d58d2da55dd47eb2b68110066c51)
   - `OnRegistered()` callback now fired for successful first-time OAuth requests (in addition to `/register` Service)
 - Added `AppHost.OnSaveSession()` to allow custom logic whenever a User Session is saved to the Cache [002a4eb](https://github.com/ServiceStack/ServiceStack/commit/002a4ebf9ea75e922554148ffa2581be05e2c359)
 - New `Dictionary<string,string> Meta` added to allow custom Auth params on `Authenticate` during Authentication [4d339c1](https://github.com/ServiceStack/ServiceStack/commit/4d339c190bf086e2639c3373792b9f4547e0851b)
 - New `Config.AddRedirectParamsToQueryString` option added to change redirect params to be added to QueryString instead of hash `#` params [fea60fa](https://github.com/ServiceStack/ServiceStack/commit/fea60fa37000ff7603dc15a31b53150d72bae131)
 - `NHibernateUserAuthRepository.GetCurrentSession()` is now overridable to customize NH Session Initialization [7249c9a](https://github.com/ServiceStack/ServiceStack/commit/7249c9af8191ec1bdf7b95db0bba607fe5015dc8)

### MQ Changes

 - Added `QueueNames.IsTempQueue()` API to determine if a MQ name is a Temp Queue even when custom naming conventions are used [c3ee3d0](https://github.com/ServiceStack/ServiceStack/commit/c3ee3d037ec5676a05e0852ea90d0d75f0d25787)
 - Pass `IMessageHandler` in custom MQ Error Handlers so Nak's can be sent from same client that received the message [3be2e3f](https://github.com/ServiceStack/ServiceStack/commit/3be2e3f9d3e9340c4993f8eabc8805c2b1325b18)

### OrmLite Changes

 - `SqlProc` no longer disposes `IDbCommand` before returning it [9e71480](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/9e714808079aa08f8b7b90766ba308279532c08f)
 - Fixed `SingleAsync` API to call correct internal API [b23410](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/b234105065b38df5eb18449e2fb8d5173458c269)
 - Added support new Multi-Column OrderBy Descending API's [33292ef](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/33292ef67ec09cbe005afc2cba1f7c417da4434c)
 - Add support for `ConvertToList<T>` to handle Scalars as well [4290229](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/4290229cd50ae6475a3edffc198bbdc87cc54539)

### ServiceStack.Text Changes

 - Add support for Dates in `yyyyMMdd` format [a752f2a](https://github.com/ServiceStack/ServiceStack.Text/commit/a752f2af70f165398899e92b2775daa0d870ff57)
   - Add New `DateTimeSerializer.OnParseErrorFn` fallback can be used to handle unknown Date Formats
 - Added convenient `Task.Success()` and `Task.Error()` extension methods for non-generic `Task` [b17866a](https://github.com/ServiceStack/ServiceStack.Text/commit/b17866a3b46e3e6c699c20b7f33ef3738fdffd46)
 - PCL version of `GetPublicProperties()` now only return instance (non-static) properties [dbe1f83](https://github.com/ServiceStack/ServiceStack.Text/commit/dbe1f8349600ba47e2c4aaaa49c4759198a6ac1f)

### Dependencies Updated

 - FacebookAuthProvider upgraded to use v2.0 of Facebook's API
 - Swagger UI updated latest version
 - Memcached updated to 0.57
 - FluentNHibernate to 2.0.1.0

# v4.0.34 Release Notes

## [Add TypeScript Reference!](https://github.com/ServiceStack/ServiceStack/wiki/TypeScript-Add-ServiceStack-Reference)

The next typed client supported in [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) is [TypeScript](http://www.typescriptlang.org/)!

![Add TypeScript Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/add-typescript-reference.png)

[TypeScript](http://www.typescriptlang.org/) is a superset of JavaScript that enhances it with an optional type system for annotating JavaScript source code - bringing many of the code-analysis, insights and tooling benefits that we get to enjoy developing in a typed language like C#/VS.NET. We're excited to also be able to bring these benefits to TypeScript Client Apps consuming ServiceStack Services! 

### TypeScript DTO Interface Declarations

The TypeScript Native Types feature takes a non-invasive approach in that they're made available in the form of a `.d.ts` [TypeScript declaration file](http://www.typescriptlang.org/Handbook#writing-dts-files). TypeScript declarations are pure static type annotations, i.e. they don't generate any code or otherwise have any effect on runtime behavior. This makes them useful as a non-invasive drop-in into existing JavaScript code where it's used to provide type annotations and intelli-sense on existing JavaScript objects, letting you continue using your existing data types and Ajax libraries.

### TypeScript Reference Example

Lets walk through a simple example to see how we can use ServiceStack's TypeScript DTO annotations in our JavaScript clients. Firstly we'll need to add a **TypeScript Reference** to the remote ServiceStack Service by **right-clicking** on your project and clicking on `Add > TypeScript Reference...` (as seen in the above screenshot).

This will import the remote Services dtos into your local project which ends up looking similar to:

```typescript
/* Options:
Date: 2014-12-08 17:24:02
Version: 1
BaseUrl: http://api.example.com

GlobalNamespace: dtos
//MakePropertiesOptional: True
//AddServiceStackTypes: True
//AddResponseStatus: False
*/

declare module dtos
{
    // @Route("/hello")
    // @Route("/hello/{Name}")
    interface Hello extends IReturn<HelloResponse>
    {
        // @Required()
        name:string;
        title?:string;
    }

    interface HelloResponse
    {
        result?:string;
    }

    interface IReturn<T> {}
    ...
}
```

Initially the single TypeScript module that contains all the DTO definitions will default to the C#/.NET `ServiceModel` namespace, but this can be made more readable in client apps by uncommenting in the header properties:

`GlobalNamespace: dtos` 

Looking at the types we'll notice the DTO's are just interface type definitions with any .NET attributes added in comments using AtScript's proposed [meta-data annotations format](https://docs.google.com/document/d/11YUzC-1d0V1-Q3V0fQ7KSit97HnZoKVygDxpWzEYW0U/mobilebasic?viewopt=127). This lets you view helpful documentation about your DTO's like the different custom routes available for each Request DTO.

By default DTO properties are optional but can be made a required field by annotating the .NET property with the `[Required]` attribute or by uncommenting `MakePropertiesOptional: False` in the header comments which instead defaults all properties as required.

Property names always reflect to match the remote servers JSON Serialization configuration, i.e. will use **camelCase** properties when the `AppHost` is configured with:

```csharp
JsConfig.EmitCamelCaseNames = true;
```

### Referencing TypeScript DTO's


Once added to your project, use VS.NET's JavaScript Doc Comments to reference the TypeScript definitions in your `.ts` scripts. The example below shows how to use the above TypeScript definitions to create a typed Request/Response utilizing jQuery's Ajax API to fire off a new Ajax request on every keystroke:

```html
/// <reference path="MyApis.dtos.d.ts"/>
...

<input type="text" id="txtHello" data-keyup="sayHello" /> 
<div id="result"></div>

<script>
$(document).bindHandlers({
    sayHello: function () {
        var request: dtos.Hello = {};
        request.title = "Dr";
        request.name = this.value;
        
        $.getJSON(createUrl("/hello", request), request, 
            function (r: dtos.HelloResponse) {
                $("#result").html(r.result);
            });
    }
});

function createUrl(path: string, params: any): string {
    for (var key in params) {
        path += path.indexOf('?') < 0 ? "?" : "&";
        path += key + "=" + encodeURIComponent(params[key]);
    }
    return path;
}
</script>
```

Here we're just using a simple inline `createUrl()` function to show how we're creating the url for the **GET** HTTP Request by appending all Request DTO properties to the QueryString, resulting in a HTTP GET Request that looks like:

    /hello?title=Dr&name=World

There's also a new `$.ss.createUrl()` API in [ss-utils.js](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library) which also handles .NET Route definitions where it will populate any variables in the `/path/{info}` instead of adding them to the `?QueryString`, e.g:

```typescript
$(document).bindHandlers({
    sayHello: function () {
        var request: dtos.Hello = {};
        request.title = "Dr";
        request.name = this.value;
        
        $.getJSON($.ss.createUrl("/hello/{Name}", request), request, 
            function (r: dtos.HelloResponse) {
                $("#result").html(r.result);
            });
    }
});
```

Which results in a HTTP GET request with the expected Url:

    /hello/World?title=Dr

### [ss-utils.d.ts](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/js/ss-utils.d.ts)

To coincide with the new TypeScript Reference support, we've also included a TypeScript declaration file for [ss-utils.js](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library). that's also available as an embedded resource in `ServiceStack.dll` at [/js/ss-utils.d.ts](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/js/ss-utils.d.ts).

### [Upgrade ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project)

To take advantage of **Add TypeScript Reference** feature, [Upgrade or Install ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project) VS.NET Extension: 

[![VS.NET Gallery Download](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/vsgallery-download.png)](http://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

> If you already have ServiceStackVS installed, uninstall it first from **Tools -> Extensions and Updates...**  then in the Extensions dialog find **ServiceStackVS -> Uninstall**.

## Auto Batched Requests

One of the best ways to improve performance, efficiency and reduce latency is to minimize the number of network requests required, which is one of the reasons we've always encouraged [Coarse-grained API designs](https://github.com/ServiceStack/ServiceStack/wiki/Why-Servicestack#servicestack-encourages-development-of-message-style-re-usable-and-batch-full-web-services) - which also lend themselves to better encapsulation and re-use. 

Another common use-case that can be improved are clients making multiple requests to the same API, but due to the lack of a better alternative batched API or control over the server implementation, will default to making multiple N+1 web service requests - thanks to ServiceStack's [message-based design](https://github.com/ServiceStack/ServiceStack/wiki/Advantages-of-message-based-web-services), that better alternative now exists :)

### All Services now support Batching!

With the introduction of **Auto Batched Requests** in this release, all ServiceStack Services now include implicit support for batching, automatically, without any additional effort - where multiple requests of the same type can be sent together in a single HTTP Request.

This is now enabled in all [.NET Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) via the new `SendAll()` and `SendAllOneWay()` API's, e.g:

```csharp
var client = new JsonServiceClient(BaseUrl);
var requests = new[]
{
    new Request { Id = 1, Name = "Foo" },
    new Request { Id = 2, Name = "Bar" },
    new Request { Id = 3, Name = "Baz" },
};

List<Response> responses = client.SendAll(requests);
```

The API works as you would expect where multiple requests can be sent together and the Service Client will return a list of all responses in the same order as the requests were sent. 

And on the back-end, your Services are none the wiser, remaining focused on handling a single Request DTO. In the case below the Service does some work then stores the response in Redis before returning it:

```csharp
public class MyServices : Service
{
    public object Any(Request request)
    {
        var response = DoWork(request);
        Redis.Store(response);
        return response;
    }
}
```

### Request Execution Flow

From the Service's point of view nothing changes. Request DTO's still get executed one at a time, through all existing filters just as if they we're sent on their own. They're just delivered together within a single HTTP Request, in this case POST'ed as JSON to the `/json/reply/Request[]` [pre-defined route](https://github.com/ServiceStack/ServiceStack/wiki/Routing#pre-defined-routes):

![Auto Batched Requests](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/auto-batched-requests.png)

### Custom Batched Requests Implementations

If a client was previously calling the same API 100 times, the existing overhead of 100 HTTP Requests would be reduced to just **1 HTTP Request** when batched. Although the above Service would still be calling Redis 100 times to store each Response.

If later this API has become really hot and you want to improve it even further, you can later add a custom implementation that accepts a `Request[]` and it will only get called once, with access to all the Request DTO's together. In this case we can use a custom implementation and take advantage of Redis's own batched API's and reduce this further to 1 Redis operation:

```csharp
public class MyServices : Service
{
    public object Any(Request request)
    {
        var response = DoWork(request);
        Redis.Store(response);
        return response;
    }
    
    public object Any(Request[] requests)
    {
        var responses = requests.Map(DoWork);
        Redis.StoreAll(responses);
        return responses;
    }
}
```

So with this custom implementation we've gone from **100 HTTP Requests + 100 Redis Operations** to **1 HTTP Request + 1 Redis Operation**.

Another scenario where you may consider using a **Custom Batched Implementation** is if you wanted to execute all requests within a single RDBMS transaction, which with [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) would look something like:

```csharp
public class MyServices : Service
{
	public object Any(Request request)
	{
	    var response = DoWork(request);
	    Db.Insert(request);
	    return response;
	}
	
	public object Any(Request[] requests)
	{
	    using (var trans = Db.OpenTransaction())
	    {
	        var responses = requests.Map(x => Any(x));	

	        trans.Commit();
	        return responses;
	    }
	}
}
```

Just like with normal Batched Requests, Custom Batched implementations are still executed one at a time through all request/response filters, taking advantage of any existing logic/validation. If you instead only wanted multiple Requests to be treated as a single Request through the entire pipeline you can create a new Request DTO that inherits from `List<TRequest>` which then gets treated as a normal Request DTO e, g:

```csharp
public class Requests : List<Request> {}

public class MyServices : Service
{
	...
    public object Any(Requests requests)
    {
        var responses = requests.Map(DoWork);
        Redis.StoreAll(responses);
        return responses;
    }
}
```

More examples of Auto Batched Requests and its behavior can be found in the [ReplyAllTests suite](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/ReplyAllTests.cs).

## New [ReactJS App Template](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project)!

The new ServiceStackVS **ReactJS App** template shares the same approach for developing modern Single Page Apps in VS.NET as the existing [AngularJS App](https://github.com/ServiceStack/ServiceStackVS/blob/master/angular-spa.md) template by leveraging the **node.js** ecosystem for managing all aspects of Client App development utilizing the best-in-class libraries:

 - [npm](https://www.npmjs.org/) to manage node.js dependencies (bower, grunt, gulp)
 - [Bower](http://bower.io/) for managing client dependencies (angular, jquery, bootstrap, etc)
 - [Grunt](http://gruntjs.com/) as the primary task runner for server, client packaging and deployments
 - [Gulp](http://gulpjs.com/) used by Grunt to do the heavy-lifting bundling and minification

The templates conveniently pre-configures the above libraries into a working out-of-the-box solution, including high-level grunt tasks to take care of the full-dev-cycle of **building**, **packaging** and **deploying** your app:

 - **[01-run-tests](https://github.com/ServiceStack/ServiceStackVS/blob/angular-spa-template/angular-spa.md#01-run-tests)** - Runs Karma JavaScript Unit Tests
 - **[02-package-server](https://github.com/ServiceStack/ServiceStackVS/blob/angular-spa-template/angular-spa.md#02-package-server)** - Uses msbuild to build the application and copies server artefacts to `/wwwroot`
 - **[03-package-client](https://github.com/ServiceStack/ServiceStackVS/blob/angular-spa-template/angular-spa.md#03-package-client)** - Optimizes and packages the client artefacts for deployment in `/wwwroot`
 - **[04-deploy-app](https://github.com/ServiceStack/ServiceStackVS/blob/angular-spa-template/angular-spa.md#04-deploy-app)** - Uses MS WebDeploy and `/wwwroot_buld/publish/config.json` to deploy app to specified server

## [React Chat](https://github.com/ServiceStackApps/Chat-React)

To help to get started with the **ReactJS App** VS.NET template and learn React, we've rewritten [ServiceStack Chat](https://github.com/ServiceStackApps/Chat) ServerEvents demo using [React](http://facebook.github.io/react/):

[![React Chat](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/chat-react.png)](http://react-chat.servicestack.net)

> Live Demo: http://react-chat.servicestack.net

### [Intro to developing apps with React and Reflux](https://github.com/ServiceStackApps/Chat-React)

The [React Chat GitHub Repository](https://github.com/ServiceStackApps/Chat-React) also includes a guide of Facebook's [React](http://facebook.github.io/react/) library and [Flux pattern](http://facebook.github.io/react/docs/flux-overview.html) and walks through how to use them to put together a React-based App - making use of the scripts in the ServiceStackVS template along the way to optimize, package and deploy React Chat.

## [Redis](https://github.com/ServiceStack/ServiceStack.Redis)

### Heartbeats enabled on [RedisPubSubServer](https://github.com/ServiceStack/ServiceStack.Redis#new-managed-pubsub-server)

`RedisPubServer` now maintains periodic heartbeats with Redis and will auto-reconnect when it detects a connection has dropped. Heartbeats can be disabled by setting `HeartbeatInterval=null`.

The new heartbeat support should also improve resiliency in components powered by RedisPubServer, including `RedisMqServer` and `RedisServerEvents`.

### Updated [RedisManagerPool](https://github.com/ServiceStack/ServiceStack.Redis#redismanagerpool) Pooling Behavior

`RedisManagerPool` is our new streamlined version of `PooledRedisClientManager` with a simplified API courtesy of the configuration moving into the [Redis Connection String](https://github.com/ServiceStack/ServiceStack.Redis#redis-connection-strings). As an new and alternative Pooled Client Manager we've taken the opportunity to tweak the pooling behavior so that any connections required after the maximum Pool size has been reached will be created and disposed outside of the Pool. 

This is different to `PooledRedisClientManager` which imposes a maximum connection limit and when its maximum pool size has been reached will instead block on any new connection requests until the next RedisClient is released back into the pool. If no client became available within `PoolTimeout`, a Pool Timeout exception will be thrown. 

By not being restricted to a maximum pool size, the new pooling behavior in `RedisManagerPool` lets it maintain a smaller connection pool size at the cost of potentially having a higher opened/closed connection count.

### Redis HTTP [Request Logger](https://github.com/ServiceStack/ServiceStack/wiki/Request-logger)

A new `RedisRequestLogger` is available in the **ServiceStack.Server** NuGet package to provide an alternative back-end for the [Request Logs Feature](https://github.com/ServiceStack/ServiceStack/wiki/Request-logger) to log HTTP Requests with Redis so they remain available after AppDomain restarts.

`RequestLogsFeature` can be configured to use the new `RedisRequestLogger` with:

```csharp
Plugins.Add(new RequestLogsFeature {
    RequestLogger = new RedisRequestLogger(
	    container.Resolve<IRedisClientsManager>(), capacity:1000)
});
```

## [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite)

### [Multiple Self References](https://github.com/ServiceStack/ServiceStack.OrmLite#multiple-self-references)

Supports for multiple Self References of the same type has been added where if there are multiple fields containing `[References]` to the same type, OrmLite will fallback to matching properties based on `{PropertyName}Id` property naming convention, e.g:

```csharp
public class Customer
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }

    [References(typeof(CustomerAddress))]
    public int? HomeAddressId { get; set; }

    [References(typeof(CustomerAddress))]
    public int? WorkAddressId { get; set; }

    [Reference]
    public CustomerAddress HomeAddress { get; set; }

    [Reference]
    public CustomerAddress WorkAddress { get; set; }
}
```

> Self References now also support mixing and matching of property names and DB Alias naming conventions

### Support for CROSS JOIN's

New `CrossJoin` API's were added to `SqlExpression` that works similarly to other JOIN's, e.g:

```csharp
var q = db.From<TableA>()
          .CrossJoin<TableB>()
          .OrderBy<TableB>(x => x.Id);

var results = db.Select<CrossJoinResult>(q);
```

### OpenDbConnectionString()

A new `OpenDbConnectionString` API was added to open adhoc connection strings using the same `OrmLiteConnectionFactory` and `DialectProvider` instance. This makes creating Multi Tenant DB Factories a little easier, e.g:

```csharp
public class MultiTenantDbFactory : IDbConnectionFactory
{
    private readonly IDbConnectionFactory dbFactory;

    public MultiTenantDbFactory(IDbConnectionFactory dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public IDbConnection OpenDbConnection()
    {
        var tenantId = RequestContext.Instance.Items["TenantId"] as string;
        return tenantId != null
            ? dbFactory.OpenDbConnectionString(GetConnectionString(tenantId))
            : dbFactory.OpenDbConnection();
    }

    public IDbConnection CreateDbConnection()
    {
        return dbFactory.CreateDbConnection();
    }
}
```

A complete Multi Tenant OrmLite example can be found in [MultiTennantAppHostTests.cs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/MultiTennantAppHostTests.cs)

### Oracle Provider returns to form

Thanks to [@TroyDycavinu-AI](https://github.com/TroyDycavinu-AI) efforts, the [Oracle OrmLite Provider](http://nuget.org/packages/ServiceStack.OrmLite.Oracle) test suite is back in the green

## [Razor](http://razor.servicestack.net/)

### RenderToAction()

The new `RenderToAction()` method lets you execute a Service in a Razor View and include it's rendered partial view using just a relative Url:

```csharp
@Html.RenderAction("/products/1")
```
It also takes an optional view name if you want a different view than the default:

```csharp
@Html.RenderAction("/products/1", "CustomProductView")
```

An alternative approach to include another Services View is with `Html.Partial()` specifying which view and model you want to render:

```csharp
@Html.Partial("GetProduct", 
    base.ExecuteService<ProductService>(s => s.Any(new GetProduct { Id = 1 })))
```
 
Where `ExecuteService` is a shorthand wrapper around using `ResolveService<T>` in a `using` block:

```csharp
@{
   Response response = null;
   using (var service = base.ResolveService<ProductService>())
   {
       response = service.Any(new GetProduct { Id = 1 });
   }
}
@Html.Partial("GetProduct", response)
```

### Relative Content Partials

In addition to Shared Razor Views in `/Views` you can now include Partials relative to the containing Razor Content Page:

```csharp
@Html.Partial("LocalPartial", model)
@Html.Partial("SubDir/NestedPartial", model)
```

## [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query)

### Unlimited Custom Table Joins

AutoQuery now supports joining any number of tables together by annotating the Request DTO with multiple `IJoin<>` interface markers e.g:

```csharp
public class MyQuery : QueryBase<Table>, 
	IJoin<T1,T2>,
	IJoin<T2,T3>,
	IJoin<T3,T4>,
	//... 
{
}
```

## [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference)

 - DTO Interfaces are now included in all C#, F#, VB.NET and TypeScript Native Type providers.
 - F# developers can specify `GlobalNamespace` in their generated `.dtos.fs`


## [Authentication](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization)

The `RavenDbUserAuthRepository` now supports specialization so it can be used to persist extended custom `UserAuth` and `UserAuthDetails` types, e.g:

```csharp
public class CustomRavenDbAuthRepository 
    : RavenDbUserAuthRepository<MyUserAuth, MyUserAuthDetails>
{
	//...
}
```

### [Sessions](https://github.com/ServiceStack/ServiceStack/wiki/Sessions)

If needed, Sessions can be injected or modified in ASP.NET hosts with:

```csharp
HttpContext.Current.Items[ServiceExtensions.RequestItemsSessionKey] =
    new AuthUserSession { ... };
```

New Session API's added:

 - `IRequest.RemoveSession(sessionId)` - Remove the Session 
 - `IRequest.GetSessionTimeToLive()` - Time remaining before current Session expires
 - `ICacheClient.GetSessionTimeToLive(id)` - Time remaining before specified Session expires



## Other Features

 - SOAP no longer emits the UTF8 BOM by default, overridable in `Config.XmlWriterSettings`
 - CORS Support added in Server Events raw HTTP Handlers
 - C# Server Events Client auto restarts when Heartbeat has elapsed the Servers `IdleTimeoutMs`
 - Added [workaround for supporting Mono with Redis SSL](https://github.com/mono/mono/pull/1399)


## Breaking Changes

### PCL NuGet Packages Merged

The **ServiceStack.Client.Pcl** and **ServiceStack.Stripe.Pcl** NuGet packages have been merged into the main **ServiceStack.Client** and **ServiceStack.Stripe** NuGet packages and as a result will no longer receive future updates. If you're using them please update your NuGet references.

### Refactored Redis Client API's

`IRedisClient.GetTimeToLive()` now returns a `TimeSpan?` will will return:
  - `null` if no key exists
  - `TimeSpan.MaxValue` if there is no expiry set on the key
  - or a `TimeSpan` value with the time remaining before the key is set to expire

 `KeepAliveRetryAfterMs` has been renamed to `WaitBeforeNextRestart` in `RedisPubSubServer` and the classes that use it: `RedisServerEvents` and `RedisMqServer`.

# v4.0.33 Release Notes

## OrmLite now supports Async!

Another [major feature request](http://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/6217167-provider-async-support-for-ormlite) 
is ticked off in this release with the new **Async support available in OrmLite!**

A quick overview of the new Async API's added can be seen in the class diagram below:

![OrmLite Async APIs](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/ormlite/OrmLiteApiAsync.png) 

Basically most of OrmLite public API's now have async equivalents of the same name and an additional conventional `*Async` suffix. 
The Async API's also take an optional `CancellationToken` making converting sync code trivial, where you just need to
add the `Async` suffix and **await** keyword, as can be seen in the 
[Customer Orders UseCase upgrade to Async diff](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/c1ce6f0eac99133fc232b263c26c42379d4c5f48)
, e.g:

Sync:

```csharp
db.Insert(new Employee { Id = 1, Name = "Employee 1" });
db.Save(product1, product2);
var customer = db.Single<Customer>(new { customer.Email }); 
```

Async:

```csharp
await db.InsertAsync(new Employee { Id = 1, Name = "Employee 1" });
await db.SaveAsync(product1, product2);
var customer = await db.SingleAsync<Customer>(new { customer.Email });
```

> Effectively the only Data Access API's that doesn't have async equivalents are `*Lazy` APIs yielding a lazy 
> sequence (incompatible with async) as well as **Schema** DDL API's which are typically not used at runtime.

For a quick preview of many of the new Async API's in action, checkout 
[ApiSqlServerTestsAsync.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLiteV45.Tests/ApiSqlServerTestsAsync.cs).

### Async RDBMS Providers

Currently only a limited number of RDBMS providers offer async API's which are only available in their **.NET 4.5** builds, which at this time are only:

  - [SQL Server .NET 4.5+](https://www.nuget.org/packages/ServiceStack.OrmLite.SqlServer)
  - [MySQL .NET 4.5+](https://www.nuget.org/packages/ServiceStack.OrmLite.MySql)

We've also added a 
[.NET 4.5 build for Sqlite](https://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite.Mono) 
as it's a common use-case to swapout to use Sqlite's in-memory provider for faster tests. 
But as Sqlite doesn't provide async API's under-the-hood we fallback to *pseudo async* support where we just wrap its synchronous responses in `Task` results. 

Regardless of whether the RDBMS provider offers Async API's, you still can use the same OrmLite async API's with all providers,
where the same Async OrmLite API's can also be used in DB Providers that doesn't natively support Async (i.e. Sqlite):

 - [ApiSqlServerTestsAsync.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLiteV45.Tests/ApiSqlServerTestsAsync.cs)
 - [ApiMySqlTestsAsync.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLiteV45.Tests/ApiMySqlTestsAsync.cs)
 - [ApiSqliteTestsAsync.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLiteV45.Tests/ApiSqliteTestsAsync.cs)

Only when these Async API's are run on an RDBMS provider with native async support (i.e. .NET 4.5 SqlServer or MySql) will you benefit from true 
non-blocking Async I/O, otherwise it fallsback to *pseudo async* support, i.e. synchronous I/O datasets wrapped in `Task` Results.

### Multiple Self References

OrmLite's [POCO Reference conventions](https://github.com/ServiceStack/ServiceStack.OrmLite#reference-conventions) 
has been expanded to include support for multiple Self References. 

The example below shows a customer with multiple `CustomerAddress` references which are able to be matched with 
the `{PropertyReference}Id` naming convention, e.g:

```csharp
public class Customer
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }

    [References(typeof(CustomerAddress))]
    public int? HomeAddressId { get; set; }

    [References(typeof(CustomerAddress))]
    public int? WorkAddressId { get; set; }

    [Reference]
    public CustomerAddress HomeAddress { get; set; }

    [Reference]
    public CustomerAddress WorkAddress { get; set; }
}
```

Once defined, it can be saved and loaded via OrmLite's normal Reference and Select API's, e.g:

```csharp
var customer = new Customer
{
    Name = "Z Customer",
    HomeAddress = new CustomerAddress {
        Address = "1 Home Street",
        Country = "US"
    },
    WorkAddress = new CustomerAddress {
        Address = "2 Work Road",
        Country = "UK"
    },
};

db.Save(customer, references:true);

var c = db.LoadSelect<Customer>(q => q.Name == "Z Customer");
c.WorkAddress.Address.Print(); // 2 Work Road

var ukAddress = db.Single<CustomerAddress>(q => q.Country == "UK");
ukAddress.Address.Print();     // 2 Work Road
```

## [ServiceStack.Redis SSL Support](https://github.com/ServiceStack/ServiceStack/wiki/Secure-SSL-Redis-connections-to-Azure-Redis)

The [most requested feature for ServiceStack.Redis](http://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/6093693-support-ssl-connection-to-redis-instances-hosted-a) 
has also been realized in this release with **ServiceStack.Redis** now supporting **SSL connections** making it suitable for accessing 
remote Redis server instances over a **secure SSL connection**.

![Azure Redis Cache](https://github.com/ServiceStack/Assets/raw/master/img/wikis/redis/azure-redis-instance.png)

### Redis Use Cases

Redis is normally used as a back-end datastore whose access is typically limited to Internal networks or authorized networks protected via firewalls. 
The new SSL Support in the Redis Client also enables secure access to a redis-server instance over the Internet and public networks as well, 
a scenario that's been recently popularized by Cloud hosting environments like Azure Redis Cache.

### [Connecting to Azure Redis](https://github.com/ServiceStack/ServiceStack/wiki/Secure-SSL-Redis-connections-to-Azure-Redis)

As connecting to [Azure Redis Cache](http://azure.microsoft.com/en-us/services/cache/) via SSL was the primary use-case for this feature, 
we've added a new 
[Getting connected to Azure Redis via SSL](https://github.com/ServiceStack/ServiceStack/wiki/Secure-SSL-Redis-connections-to-Azure-Redis) 
to help you get started.

### Redis Connection Strings

Redis Connection strings have been expanded to support the more versatile URI format which is now able to capture most of Redis Client settings in a 
single connection string (akin to DB Connection strings).

Redis Connection Strings supports multiple URI-like formats, from a simple **hostname** or **IP Address and port** pair to a fully-qualified **URL** 
with multiple options specified on the QueryString. 

Some examples of supported formats:

    localhost
    127.0.0.1:6379
    redis://localhost:6379
    password@localhost:6379
    clientid:password@localhost:6379
    redis://clientid:password@localhost:6380?ssl=true&db=1

> More examples can be seen in 
[ConfigTests.cs](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/tests/ServiceStack.Redis.Tests/ConfigTests.cs)

Any additional configuration can be specified as QueryString parameters. The full list of options that can be specified include:

<table>
    <tr>
        <td><b>Ssl</b></td>
        <td>bool</td>
        <td>If this is an SSL connection</td>
    </tr>
    <tr>
        <td><b>Db</b></td>
        <td>int</td>
        <td>The Redis DB this connection should be set to</td>
    </tr>
    <tr>
        <td><b>Client</b></td>
        <td>string</td>
        <td>A text alias to specify for this connection for analytic purposes</td>
    </tr>
    <tr>
        <td><b>Password</b></td>
        <td>string</td>
        <td>UrlEncoded version of the Password for this connection</td>
    </tr>
    <tr>
        <td><b>ConnectTimeout</b></td>
        <td>int</td>
        <td>Timeout in ms for making a TCP Socket connection</td>
    </tr>
    <tr>
        <td><b>SendTimeout</b></td>
        <td>int</td>
        <td>Timeout in ms for making a synchronous TCP Socket Send</td>
    </tr>
    <tr>
        <td><b>ReceiveTimeout</b></td>
        <td>int</td>
        <td>Timeout in ms for waiting for a synchronous TCP Socket Receive</td>
    </tr>
    <tr>
        <td><b>IdleTimeOutSecs</b></td>
        <td>int</td>
        <td>Timeout in Seconds for an Idle connection to be considered active</td>
    </tr>
    <tr>
        <td><b>NamespacePrefix</b></td>
        <td>string</td>
        <td>Use a custom prefix for ServiceStack.Redis internal index colletions</td>
    </tr>
</table>

### New `RedisManagerPool` Client Manager

With the introduction of Redis URI Connection Strings we've been able to simplify and streamline the existing `PooledRedisClientManager` 
implementation that's been extracted out into clients manager called `RedisManagerPool`. 
In addition to removing all above options on the Client Manager itself, we've also removed readonly connection strings so the configuration is 
much simpler and more aligned with the common use-case.

In most cases, `PooledRedisClientManager` is substitutable with `RedisManagerPool` e.g:

```csharp
container.Register<IRedisClientsManager>(c => 
    new RedisManagerPool(redisConnectionString));
```

### New Generic API's for calling Custom Redis commands

Most of the time when waiting to use a new [Redis Command](http://redis.io/commands) you'll need to wait for an updated version of 
**ServiceStack.Redis** to add support for the new commands likewise there are times when the Redis Client doesn't offer every permutation 
that redis-server supports. 

With the new `Custom` and `RawCommand` API's on `IRedisClient` and `IRedisNativeClient` you can now use the RedisClient to send your own 
custom commands that can call adhoc Redis commands:

```csharp
public interface IRedisClient
{
    ...
    RedisText Custom(params object[] cmdWithArgs);
}

public interface IRedisNativeClient
{
    ...
    RedisData RawCommand(params object[] cmdWithArgs);
    RedisData RawCommand(params byte[][] cmdWithBinaryArgs);
}
```

These API's return Custom Results in the generic data structures below:

```csharp
public class RedisText
{
    public string Text { get; set; }
    public List<RedisText> Children { get; set; }
}

public class RedisData
{
    public byte[] Data { get; set; }
    public List<RedisData> Children { get; set; } 
}
```

These Custom API's take a flexible `object[]` arguments which accepts any serializable value e.g. 
`byte[]`, `string`, `int` as well as any user-defined Complex Types which are transparently serialized 
as JSON and send across the wire as UTF-8 bytes. 

```csharp
var ret = Redis.Custom("SET", "foo", 1);          // ret.Text = "OK"

byte[] cmdSet = Commands.Set;
ret = Redis.Custom(cmdSet, "bar", "b");           // ret.Text = "OK"

ret = Redis.Custom("GET", "foo");                 // ret.Text = "1"
```

There are also 
[convenient extension methods](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/src/ServiceStack.Redis/RedisDataExtensions.cs) 
on `RedisData` and `RedisText` that make it easy to access structured data, e.g:

```csharp
var ret = Redis.Custom(Commands.Keys, "*");
var keys = ret.GetResults();                      // keys = ["foo", "bar"]

ret = Redis.Custom(Commands.MGet, "foo", "bar");
var values = ret.GetResults();                    // values = ["1", "b"]

Enum.GetNames(typeof(DayOfWeek)).ToList()
    .ForEach(x => Redis.Custom(Commands.RPush, "DaysOfWeek", x));
ret = Redis.Custom(Commands.LRange, "DaysOfWeek", 1, -2);
var weekDays = ret.GetResults();      

weekDays.PrintDump(); // ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
```

and some more examples using Complex Types with the Custom API's:

```csharp
var ret = Redis.Custom(Commands.Set, "foo", new Poco { Name = "Bar" }); // ret.Text = "OK"

ret = Redis.Custom(Commands.Get, "foo");          // ret.Text =  {"Name":"Bar"}
Poco dto = ret.GetResult<Poco>();

dto.Name.Print(); // Bar
```

### New Config, Role and Client commands

A number of New API's added in this can be seen below:

```csharp
public interface IRedisClient
{
    ...
    RedisText GetServerRoleInfo();
    string GetConfig(string item);
    void SetConfig(string item, string value);
    void SaveConfig();
    void ResetInfoStats();

    string GetClient();
    void SetClient(string name);
    void KillClient(string address);
    long KillClients(string fromAddress = null, 
        string withId = null, RedisClientType? ofType = null, bool? skipMe = null);
    List<Dictionary<string, string>> GetClientsInfo();
    void PauseAllClients(TimeSpan duration);
}

public interface IRedisNativeClient
{
    ...
    void ConfigRewrite();
    RedisText Role();
    string ClientGetName();
    void ClientSetName(string client);
    void ClientKill(string host);
    long ClientKill(string addr = null, string id = null, string type = null, string skipMe = null);
    byte[] ClientList();
    void ClientPause(int timeOutMs);    
}
```

## [New VB.NET Add ServiceStack Reference!](https://github.com/ServiceStack/ServiceStack/wiki/VB.Net-Add-ServiceStack-Reference)

This release also adds [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) 
support for the last remaining major .NET language with the new first-class support for 
[VB.NET Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/VB.Net-Add-ServiceStack-Reference)! 

This now allows any C#, F# or VB.NET client project to be able generate and end-to-end typed API for your services just by providing the 
url of your remote ServiceStack instance, directly from within VS.NET!

![Add ServiceStack Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/add-service-ref-flow.png)

After clicking OK, the servers DTO's and **ServiceStack.Client** NuGet package are added to the project, providing an instant typed API: 

![Calling a ServiceStack Service from VB.NET](https://github.com/ServiceStack/Assets/raw/master/img/apps/StackApis/call-service-vb.png)

Thanks to the close semantics between the C# and VB.NET languages, we're able to add support for all C# 
[customization options in VB.NET](https://github.com/ServiceStack/ServiceStack/wiki/VB.Net-Add-ServiceStack-Reference#dto-customization-options) as well.

Much of the new VB.NET NativeTypes provider is thanks to the efforts of [@KevinHoward](https://github.com/KevinHoward).

### [Upgrade ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project)

To take advantage of VB.NET Add ServiceStack Reference feature, 
[Upgrade or Install ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project) VS.NET Extension. 
If you already have **ServiceStackVS** installed, uninstall it first from `Tools -> Extensions and Updates... -> ServiceStackVS -> Uninstall`.

## Simplified UX for all languages

Our first iteration of **Add ServiceStack Reference** for C# used a **T4 Template** to make it easy for clients to view and modify all 
Customization options available and to be able to auto-generate the Server DTO's by modifying and saving (or re-running) the T4 template. 

As F# projects didn't support T4 Templates, when we added support for 
[F# Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/FSharp-Add-ServiceStack-Reference) 
we had to skip the T4 template and add the server-generated DTO's source file directly to the project. 

By skipping the T4 Template we pleasantly discovered we ended up with a nicer, simplified and more user-friendly UX, with less moving parts for 
the default use case of generating client DTO's based on the 
[Default Server Configuration](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#change-default-server-configuration).

### Improving Single Generated Source File Story

We've since decided to embrace and provide a better development experience around a single source file approach and use it consistently in all 
C#, F# and VB.NET projects - now resulting simpler **Add ServiceStack Reference** UX for all client projects.

### Update ServiceStack Reference Context Menu Item

With the latest **ServiceStackVS** you can now update the Server DTO's in all projects by clicking on `Update ServiceStack Reference` 
on the context-menu, e.g:

![Update ServiceStack Reference](https://github.com/ServiceStack/Assets/raw/master/img/servicestackvs/servicestack%20reference/updateref-vbnet.gif)

### Updating and Customizing Generated Types

To [customize the generated DTO's](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#dto-customization-options) 
on the client you can just uncomment the option you want to change directly in the **header comments** and hit save. 
**ServiceStackVS** automatically watches for any changes to the generated dto source files (i.e. ending with `.dtos.cs`) and will automatically 
send the uncommented options to the remote server referenced by the `BaseUrl` and replace the existing file with the updated DTOs instantly!

Taking the example below, once we uncomment the `MakePartial` option and save the file, **ServiceStackVS** automatically sends a new request 
to the remote ServiceStack instance, passing in the `?MakePartial=False` option when requesting updated DTO's:

```csharp
/* Options:
Date: 2014-10-21 00:44:24
Version: 1
BaseUrl: http://stackapis.servicestack.net

MakePartial: False
//MakeVirtual: True
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/
```

After saving you'll be able to notice the DTO's are updated instantly with the `Date: *` changing to reflect the current time and the new 
generated DTO's no longer containing `partial` classes.

### ServiceStack.Text

New `JsConfig<T>.OnDeserializing` and dynamic `ShouldSerialize(string field)` customization options were added to ServiceStack's JSON and JSV 
Text serializers by [@pavelsavara](https://twitter.com/pavelsavara). An example of these new customization options in action is visible below:

```csharp
[DataContract]
public class CustomSerializedPoco
{
    [IgnoreDataMember]
    public HashSet<string> hasAttribute;

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int A { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public int? B { get; set; }

    public bool? ShouldSerialize(string fieldName)
    {
        return hasAttribute == null 
            ? null
            :  hasAttribute.Contains(fieldName);
    }

    public object OnDeserializing(string fieldName, object value)
    {
        if (hasAttribute == null)
            hasAttribute = new HashSet<string>();
        hasAttribute.Add(fieldName);
        return value;
    }
}
```

This change makes it possible to create dynamic POCO's that behave in a similar way that dynamic languages can, 
e.g. After deserialization you can detect which fields were deserialized by inspecting the `hasAttribute` collection.

The `ShouldSerialize` API, closely follows the existing `ShouldSerialize{X}` convention but instead allows for a single API 
to handle all serializable properties. 

The API returns a `bool?` which has the following meaning: 
 - `true` - Should be emitted  
 - `false` - Should not be emitted 
 - `null` - Use default behavior

This allows us to implement a custom type that can support full round-trip when the field on the original JSON payload allowing use to 
implement a custom type with similar functionality to `IExtensibleDataObject` which allows survival and forwarding of unknown properties, but for JSON.

## [RabbitMQ](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ)

RabbitMQ Server and Client now have optional `PublishMessageFilter` and `GetMessageFilter` callbacks which can be used to intercept
outgoing and incoming messages, the `IBasicProperties.Type` is also pre-populated with the Type name of the message body that was published, e.g:

```csharp
var mqServer = new RabbitMqServer("localhost") 
{
    PublishMessageFilter = (queueName, properties, msg) => {
        properties.AppId = "app:{0}".Fmt(queueName);
    },
    GetMessageFilter = (queueName, basicMsg) => {
        var props = basicMsg.BasicProperties;
        receivedMsgType = props.Type; //automatically added by RabbitMqProducer
        receivedMsgApp = props.AppId;
    }
};

using (var mqClient = mqServer.CreateMessageQueueClient())
{
    mqClient.Publish(new Hello { Name = "Bugs Bunny" });
}

receivedMsgApp.Print();   // app:mq:Hello.In
receivedMsgType.Print();  // Hello
```

## Other Minor Changes

 - ServerEvents Server now echoes heartbeat messages back through to the listening connection, 
   `ServerEventsClient` only fires the `OnHeartbeat` callback when it's receives the echoed `cmd.Heartbeat` command message
 -  Request binding for `Path` and `QueryString` variables are added to DTO's with Request DTO's providing their own [custom body deserialization](https://github.com/ServiceStack/ServiceStack/wiki/Serialization-deserialization) by implementing `IRequiresRequestStream`
 - New `IAppHost.OnDisposeCallbacks` available allowing **Plugins** to register callbacks when `AppHost` is disposed
 - `Config.UseHttpsLinks` now modifies generated BaseUrl of all links to use `https`
 - The `ResponseStatus` on Custom DTO's are now preserved when thrown inside a custom `HttpError` response
 - Equality members added to `[Route]`, `[Authenticate]`, `[RequiredRole]` and `[RequiredPermission]` attributes
 - `ToOptimizedResultUsingCache` no longer double-encodes raw `string` responses
 - `MvcHtmlString` was moved to `ServiceStack.Html` namespace
 - New `StaticFileHandler.ResponseFilter` added to be able to modify custom headers returned on static files
 - Many of OrmLite's static Extension method classes were renamed into a more logical grouping. 
   These changes are source compatible for typical usage of OrmLite API's, i.e. referenced as extension methods

## Breaking changes

### Added new .NET 4.5 Builds

In preparation for introducing Async API's we've added new **.NET 4.5 builds** for the following packages:

  - ServiceStack.OrmLite
  - ServiceStack.OrmLite.Sqlite.Mono
  - ServiceStack.OrmLite.SqlServer
  - ServiceStack.OrmLite.MySql
  - ServiceStack.Server

When adding ServiceStack NuGet Packages to a **.NET 4.5** project you will now get these newer .NET 4.5 builds instead.
The additional builds means you could potentially run into issues if mixing .NET v4.0 and v4.5 builds as all dependencies 
need to reference the same build version.

Should you need to, the easiest way to fix any versioning issues is to make sure all projects use the same .NET Framework version 
(e.g. .NET 4.5) and then just uninstall and re-install the ServiceStack NuGet packages.

### Removed ThreadStatic OrmLite Configuration

We've also removed our existing ThreadStatic config variables (used to temporarily override global configuration).
Most per-connection state is now stored on the connection e.g. `CommandTimeout` was previously overridden with:

```csharp
var hold = OrmLiteConfig.TSCommandTimeout;
try {
	OrmLiteConfig.TSCommandTimeout = 60;
	db.Select(...);
} finally {
	OrmLiteConfig.TSCommandTimeout = hold;
}
```

Is now set directly on the connection (and only applies to that connection), e.g:

```csharp
using (var db = DbFactory.Open())
{
	db.SetCommandTimeout(60);
	db.Select(...);
}
```

Likewise if you ever need to access the current `OrmLiteConfig.DialectProvider`, it should now be retrieved from the `IDbConnection`, i.e:

```csharp
db.GetDialectProvider();
``` 

and if you ever need to access the underlying ADO.NET `IDbConnection` or `IDbCommand` you can access them via the following APIs:

```csharp
IDbConnection adoDb = db.ToDbConnection();
IDbCommand adoDbCmd = dmCmd.ToDbCommand();
```

### IReturnVoid now returns void

All `IReturnVoid` API's on Service Clients have been changed to return `void` instead of 
`HttpWebResponse` which needed to be explicitly disposed by the callee. 

To access the `HttpWebResponse`, Request DTO's can be changed to `IReturn<HttpWebResponse>` 

```csharp
public class EmptyResponse : IReturn<HttpWebResponse> { ... }
```

Alternatively the Response can be specified on the call-site with:

```csharp
HttpWebResponse response = client.Get<HttpWebResponse>(new EmptyResponse());
```

# v4.0.32 Release Notes

## FSharp Add ServiceStack Reference!

We're happy to announce that the next language supported by [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) is F#!

For a quick overview, [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) is a simple solution to WCF's Add Service Reference that provides a flexible alternative to sharing your compiled server DTO's assembly with clients. Now F# clients can easily add a reference to a remote ServiceStack instance and update typed DTO's directly from within VS.NET - reducing the burden and effort required to consume ServiceStack Services. 

### Example Usage

The easiest way to Add a ServiceStack reference to your project is to right-click on your project to bring up [ServiceStackVS's](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project) **Add ServiceStack Reference** context-menu item. This opens a dialog where you can add the url of the ServiceStack instance you want to typed DTO's for, as well as the name of the T4 template that's added to your project.

[![Add ServiceStack Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/add-service-ref-flow.png)](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/add-service-ref-flow.png)

After clicking OK, the servers DTO's and [ServiceStack.Client](https://www.nuget.org/packages/ServiceStack.Client) NuGet package are added to the project, providing an instant typed API:

[![Calling ServiceStack Service with FSharp](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/fsharp-add-servicestack-reference.png)](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/fsharp-add-servicestack-reference.png)

### Updating a ServiceStack Reference

Updating a ServiceStack reference works intuitively where you can right-click on the DTO's you want to update and click **Update ServiceStack Reference** on the context menu:

[![Calling ServiceStack Service with FSharp](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/fsharp-update-servicestack-reference.png)](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/fsharp-update-servicestack-reference.png)

### F# Client Example

Just like with C#, F# Native Types can be used in ServiceStack's [Generic Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) providing and end-to-end Typed API whose PCL support also allows F# to be used in [mobile clients apps](https://github.com/ServiceStackApps/HelloMobile) as well. The basic [stackapis.servicestack.net](http://stackapis.servicestack.net/) Services example in F# looks like:

```fsharp
let client = new JsonServiceClient("http://stackapis.servicestack.net")
let response = client.Get(new SearchQuestions(
    Tags = new List<string>([ "redis"; "ormlite" ])))        

TypeSerializer.PrintDump(response)
```

## FSharp Native Types Notes 

Add ServiceStack Reference in FSharp projects works a little different to C# projects which [utilizes VS.NET's built-in T4 support](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference#add-servicestack-reference) to provide customization of DTO's on the client that auto-runs the T4 template implicitly on **Save** or explicitly with the **Run Custom Tool** context menu item.

Since there's no support for T4 templates in F# Projects, clicking **Add ServiceStack Reference** skips the T4 template and just adds the generated F# Types using the server defaults. This ends up providing a simpler experience for F# clients in the default case. Customization of default behavior can still be done on the Server:

### Change Default Server Configuration

The above defaults are also overridable on the ServiceStack Server by modifying the default config on the `NativeTypesFeature` Plugin, e.g:

```csharp
var typesConfig = this.GetPlugin<NativeTypesFeature>().MetadataTypesConfig;
typesConfig.AddDataContractAttributes = false;
...
```

## F# Native Types Constraints

As the ordering constraint in F# conflicted with the ordering of types by C# namespaces, the cleanest approach was to add all DTO's under a single namespace. By default the namespace used will be the base **ServiceModel** namespace which is overridable with the `GlobalNamespace` Config:

```csharp
typesConfig.GlobalNamespace = "Client.Namespace";
```

This does mean that each DTO type name needs to be unique which is a best-practice that's now a requirement in order to make use of F# native types. Another semantic difference is that any C# partial classes are converted into top-level classes in F#.  

For more documentation about F# Native Types including info on each of the config options supported checkout the [F# Add ServiceStack Reference wiki](https://github.com/ServiceStack/ServiceStack/wiki/FSharp-Add-ServiceStack-Reference).

### [Upgrade ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project)

To take advantage of this F# Add ServiceStack Reference [Upgrade or Install ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project) VS.NET Extension. If you already have **ServiceStackVS** installed, uninstall it first from `Tools -> Extensions and Updates... -> ServiceStackVS -> Uninstall`.

### Nested classes and Enums

Support for Enums and Nested classes are now supported on both C# and F# Native Types. 

## Improved integration with MVC and ASP.NET

We've improved integration for making use of ServiceStack components in existing ASP.NET MVC and WebForms Web Applications. The internals of `ServiceStackController` have been rewritten to share a common code-base with the new WebForms `ServiceStackPage` WebForms base page, both provide easy access to the same clean, high-performance components found in ServiceStack's `Service` base class, directly from within your MVC Controllers and WebForm pages.

This is an outline of the API's found in MVC's `ServiceStackController` and WebForms `ServiceStackPage`:

```csharp
public class ServiceStackController : Controller
{
    //...
    IServiceStackProvider ServiceStackProvider { get; set; }
    IAppSettings AppSettings { get; set; }
    IHttpRequest ServiceStackRequest { get; set; }
    IHttpResponse ServiceStackResponse { get; set; }
    ICacheClient Cache { get; set; }
    IDbConnection Db { get; set; }
    IRedisClient Redis { get; set; }
    IMessageFactory MessageFactory { get; set; }
    IMessageProducer MessageProducer { get; set; }
    ISessionFactory SessionFactory { get; set; }
    ISession SessionBag { get; set; }
    bool IsAuthenticated { get; set; }

    T TryResolve<T>();
    T ResolveService<T>();
    object Execute(object requestDto);
    object ForwardRequestToServiceStack(IRequest request=null);
    IAuthSession GetSession(bool reload = true);
    TUserSession SessionAs<TUserSession>();
    void ClearSession();
    void PublishMessage<T>(T message);
}
```

### Use ServiceStack Authentication

One benefit of integration with ServiceStack is to be able to make use of ServiceStack's simple and flexible [Authentication Providers](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization) which require minimal configuration and supports a number of different [Session Providers](https://github.com/ServiceStack/ServiceStack/wiki/Caching) and persistent [Data Store back-ends](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization#userauth-persistence---the-iuserauthrepository) to make it easy to integrate with an existing environment.

### New MVC and WebForms Examples

To illustrate the seamless integration with ServiceStack, we've created 2 new authentication-enabled example websites:

 - **ASP.NET MVC** Live Demo: [mvc.servicestack.net](http://mvc.servicestack.net/) and [source code](https://github.com/ServiceStack/Test/tree/master/src/Mvc)
 - **ASP.NET WebForms** Live Demo: [webforms.servicestack.net](http://webforms.servicestack.net/) and [source code](https://github.com/ServiceStack/Test/tree/master/src/WebForms)

![MVC with ServiceStack Authentication](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/mvc-integration.png)

### Integrating with ServiceStack from MVC or WebForms

We'll go through the MVC example to showcase the different ways you can integrate with ServiceStack from an external Web Framework. 

#### Using ResolveService to call Services directly

The `Login` Action is a standard MVC Action handling HTML Form input accepting 3 parameters, a `userName`, `password` as well as a relative `redirect` url to redirect to when authentication is successful. Login uses the `ResolveService<TService>` API which just resolves an auto-wired instance of the ServiceStack `AuthenticateService` from the IOC and injects the current HTTP Request context, which we then use to call a method on the Service directly:

```csharp
public ActionResult Login(string userName, string password, string redirect=null)
{
    if (ModelState.IsValid)
    {
        try
        {
            using (var authService = ResolveService<AuthenticateService>())
            {
                var response = authService.Authenticate(new Authenticate {
                    provider = CredentialsAuthProvider.Name,
                    UserName = userName,
                    Password = password,
                    RememberMe = true,
                });

                // add ASP.NET auth cookie
                FormsAuthentication.SetAuthCookie(userName, true);

                return Redirect(string.IsNullOrEmpty(redirect) ? "/" : redirect);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
    }

    return View("Index", GetViewModel());
}
```

> Since the above example calls the Service method directly any exceptions raised by the Service implementation are thrown and caught as normal.

#### Using Execute to process Request DTO's

The `Logout()` MVC Action uses ServiceStack's `Execute()` API which can call the desired ServiceStack Service with just a populated Request DTO:

```csharp
public ActionResult Logout()
{
    Execute(new Authenticate { provider = "logout" });
    FormsAuthentication.SignOut(); 

    return Redirect("/");
}
```

#### Using ForwardRequestToServiceStack to proxy HTTP Requests

The `ForwardingController` handles OAuth callbacks that have been configured to callback to `/auth/*` route which is handled by MVC as ServiceStack is mounted at and only configured to handle `/api` requests. 

Instead of creating new OAuth Applications with each provider to use the new `/api/auth/*` callback url so ServiceStack can handle the OAuth callback, we can use just use the new `ForwardRequestToServiceStack()` which just forwards the incoming HTTP Request from MVC to ServiceStack to process, effectively acting as a proxy:

```csharp
routes.MapRoute("Forwarding", "auth/{*pathinfo}", 
    new { controller = "Forwarding", action = "Index" });
...

public class ForwardingController : ServiceStackController
{
    public ActionResult Index()
    {
        var response = ForwardRequestToServiceStack();
        if (ServiceStackResponse.IsClosed) return new EmptyResult();

        string redirectUrl;
        var httpResult = response as IHttpResult;
        if (httpResult != null && httpResult.Headers.TryGetValue(HttpHeaders.Location, out redirectUrl))
            return Redirect(redirectUrl);

        return Redirect("/");
    }
}
```

The `Execute()` and `ForwardRequestToServiceStack()` are high-level API's that call into ServiceStack's internal Request pipeline, executing any Action Filters and also converts any exceptions into a populated serializable Response DTO with a populated `ResponseStatus` as would be returned to Service Clients.

### Authentication Attributes

Since we're using ServiceStack for Authentication, we're also able to re-use ServiceStack's Authentication Attribute Filters directly on MVC Controllers and WebForm Pages just as if they were ServiceStack Services, e.g:

```csharp
[Authenticate]
public class AuthOnlyController : ServiceStackController 
{
    public ActionResult Index()
    {
        return View(SessionAs<CustomUserSession>());
    }         
}
```

The above controller hanldes the [mvc.servicestack.net/AuthOnly](http://mvc.servicestack.net/AuthOnly) route which only allows access to Authorized users. If a user is not authenticated they're automatically redirected to [/?redirect=/AuthOnly#f=Unauthorized](http://mvc.servicestack.net/?redirect=%2fAuthOnly#f=Unauthorized) to prompt the user to login, after successfully logging in it will redirect back to the original `/AuthOnly` url.

### Required Role or Permission

The `[RequiredRole]` and `[RequiredPermission]` attributes work similar to the `[Authentication]` attribute except they also assert that the user is a member of the specified role:

```csharp
[RequiredRole("TheRole")]
public class RequiresRoleController : ServiceStackController 
{
    public ActionResult Index()
    {
        return View(SessionAs<CustomUserSession>());
    }
}
```

The above Controller handles the [/RequiresRole](http://mvc.servicestack.net/RequiresRole) Route and will only grant access if the Authenticated User is also a member of the **TheRole**.

### Calling ServiceStack Services Directly

The simplest way to consume ServiceStack Services requiring the least effort and moving parts is to call them directly: 

#### Using ServiceStack OAuth in MVC

Integrating with ServiceStack's OAuth providers requires the least effort as they're linkable directly in the format `/api/auth/{provider}` which is handled by ServiceStack's OAuth Service who initiates the Authentication process by redirecting to the selected OAuth provider:

![MVC OAuth with HTML](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/mvc-auth.png)

#### Calling ServiceStack with Ajax in MVC

Posting HTML Forms directly to ServiceStack Services isn't that much more effort, Start with a plain HTML Form with field names that match with the Services property names:

![MVC Register with HTML](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/mvc-register.png)

We can then use ServiceStack's built-in [ss-utils.js JavaScript Libraray](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library) to take care of Ajaxifying, auto-binding and submitting the form via Ajax. It also has built-in support for [Bootstrap Forms Field Validation conventions](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library#bootstrap-forms) to automatically bind errors to the appropriate fields. The only custom code required is to bind the form is then:

```javascript
$("#form-register").bindForm({
    success: function (r) { location.href = '/'; }
});
```

In this case we've added a success callback to redirect to the home page if the registration was successful which will either be authenticated with the newly registered user if **Auto Login** was checked, otherwise you can use the login form to Sign in as the newly registered user.

## [Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)

There are new custom hooks on `ServerEventsFeature` to allow for further customization and deeper introspection of ServiceStack's Server Events:

  - `OnInit` - Invoked when clients first connect to the `event-stream`, can be used to add additional HTTP Headers back to the client
  - `OnPublish` - Fired after each message is published with the active Response and the raw message that was published

### Add Authentication support to .NET ServerEvents Client

There are new explicit `Authenticate` and `AuthenticateAsync` API's which can be used to authenticate the ServerEvents ServiceClient which now **shares cookies** with the WebRequest that connects to the `/event-stream` so authenticating with the Server Events ServiceClient will also authenticate the `/event-stream` HTTP Connection:

```csharp
client.Authenticate(new Authenticate {
    provider = CredentialsAuthProvider.Name,
    UserName = "user",
    Password = "pass",
    RememberMe = true,
});

client.Start();
```

Which is also equivalent to:

```csharp
client.ServiceClient.Post(new Authenticate {
    provider = CredentialsAuthProvider.Name,
    UserName = "user",
    Password = "pass",
    RememberMe = true,
});
```

### Limiting Server Events to Authenticated Clients Only

There's a new `LimitToAuthenticatedUsers` option in `ServerEventsFeature` to limit access to authenticated clients only:

```csharp
Plugins.Add(new ServerEventsFeature {
    LimitToAuthenticatedUsers = true,
});
```

When enabled it will return a `401 Unauthorized` for non-authenticated clients.

### JavaScript ServerEvents Client

The [Server Events JavaScript Client](https://github.com/ServiceStack/ServiceStack/wiki/JavaScript-Server-Events-Client) now auto-reconnects when a heartbeat request fails by calling the new `$.ss.reconnectServerEvents()` API.

## Funq IOC

Funq now supports Lazy dependencies where you can `Func<T>` factories to delay the resolution of dependencies to only when the Service is needed. The factories also support Multiple lazy arguments (up to 3 args):

```csharp
container.RegisterAutoWiredAs<Foo, IFoo>();
container.RegisterAutoWiredAs<Bar, IBar>();
container.RegisterAutoWiredAs<Baz, IBaz>();

container.RegisterAutoWired<Dependency>();

public class Dependency
{
    Func<IFoo> ctorFoo;
    public Dependency(Func<IFoo> ctorFoo)
    {
        this.ctorFoo = ctorFoo;
    }

    public Func<IFoo> Foo { get; set; }

    public Func<IFoo, IBar> FooBar { get; set; }

    public Func<IFoo, IBar, IBaz> FooBarBaz { get; set; }

    public object Execute()
    {
        int total = 0;
        var foo = ctorFoo();
        var bar = FooBar(foo);
        var baz = FooBarBaz(foo, bar);
        return bax.Execute();
    }
}
```

An alternative approach to resolving lazy dependency is to use a Lazy Property which is our preferred approach since allows the call-site to be transparent and retain a clean API, e.g:

```csharp
public virtual IDbConnectionFactory DbFactory { get; set; }

IDbConnection db;
public virtual IDbConnection Db
{
    get { return db ?? (db = DbFactory.OpenDbConnection()); }
}
```

This technique is used in the built-in [Repository Base](https://github.com/ServiceStack/ServiceStack/blob/8dcbbdb7dbe20fd3201cde100370564e8577a019/src/ServiceStack/ILogic.cs#L38) and [Logic Base](https://github.com/ServiceStack/ServiceStack/blob/8dcbbdb7dbe20fd3201cde100370564e8577a019/src/ServiceStack/ILogic.cs#L55) base classes which your dependencies can inherit from to enable lazy access to common ServiceStack providers.

## App Settings

### New Providers

There's a new `EnvironmentVariableSettings` AppSettings provider to source configuration from Environment variables as well as a new `MultiAppSettings` AppSettings provider that enables reading configuration from multiple configuration sources.

With these new providers we can setup a cascading configuration that first checks Environment variables, then looks in a local `~/appsettings.txt` plain-text file before falling back to `Web.config`: 

```csharp
AppSettings = new MultiAppSettings(
    new EnvironmentVariableSettings(),
    new TextFileSettings("~/appsettings.txt".MapHostAbsolutePath()),
    new AppSettings());
```

### New Apis

New `GetAllKeys()` and `Exists()` were added to all `IAppSettings` providers:

```csharp
public interface IAppSettings
{
    List<string> GetAllKeys(); 
    bool Exists(string key);
    //...
}
```

This makes it easy to scan and retrieve all related keys, e.g:

```csharp
var devKeys = appSettings.GetAllKeys().Where(x => x.Matches("Dev.*"));
```

## [Session](https://github.com/ServiceStack/ServiceStack/wiki/Sessions)

### Use HTTP Headers to Send Session Cookies

You can now make a Session-enabled request with HTTP Headers instead of Cookies. The Session HTTP Headers have a `X-` prefix before the Session Id, i.e: `X-ss-id`, `X-ss-pid` and `X-ss-opts`

### New API's for Session Bag

New API's for storing and retrieving POCO's in a session bag without specifying a key (key defaults to Type Name):

```csharp
SessionBag.Set(unAuthInfo);
var unAuthInfo = SessionBag.Get<UnAuthInfo>();
```

New `GetSessionBag()` extension methods on `IRequest` and `IServiceBase` make it easier to access the users dynamic Session Bag from outside of a Service. E.g the example below shows how to copy any Anonymous User Session info into the Users typed Session when they Sign In:

```csharp
public class CustomUserSession : AuthUserSession
{
    [DataMember]
    public string UnAuthInfo { get; set; }

    public override void OnAuthenticated(IServiceBase authService, IAuthSession session, 
        IAuthTokens tokens, Dictionary<string, string> authInfo)
    {
        UnAuthInfo = authService.GetSessionBag().Get<UnAuthInfo>();
    }
}
```

## OrmLite

### New OrderBy API's for Joined Tables

You can now Order By fields from Joined Tables in an SqlExpression:

```csharp
var q = db.From<TableA>()
    .Join<TableB>()
    .OrderBy<TableB>(x => x.Name);
```

### Count Queries

 - New `RowCount()` API added to return the number of rows in a query
 - Aggregate `Count()` queries that return more than a single row are summed together

### SelectInto Mapping

 - You no longer need to repeat `[Alias]` attributes when projecting results into different types with `db.SelectInto<T>` API's as fields are now mapped by POCO Property names

## ServiceStack.Text

 - Default delimiter of `ParseKeyValueText` changed from `:` to ` ` (space)
 - New `string.Matches()` extension method to perform Glob-style matches
 - `GetGenericType()` extension has been renamed to the more appropriate `FirstGenericType()`
 - The `__type` info for late-bound types is no longer affected by white-space

## Other

 - Metadata detail pages now include any types referenced in the Request and Response DTOs 
 - Rabbit MQ Clients no longer auto-declare Server Named Queues `amq.*`

## Community

### New [ServiceStack Succinctly](http://www.agile-code.com/blog/servicestack-succinctly-free-e-book/) Free e-book!

[ServiceStack Succinctly](http://www.agile-code.com/blog/servicestack-succinctly-free-e-book/) is a new free e-book by [Zoran Maksimovic](https://twitter.com/zoranmax) which runs through the basics of creating and implementing a simple order management system with ServiceStack:

<a href="http://www.agile-code.com/blog/servicestack-succinctly-free-e-book/"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/community/servicestack-succinctly.png" width="299" align="right" hspace="20" alt="Free ServiceStack Succinctly e-book"></a>

### Table of Contents

 1. ServiceStack Overview
 2. ServiceStack Basics
 3. Order Management System
 4. Solution Configuration
 5. Service Implementation
 6. Pagination
 7. Authentication
 8. Caching
 9. Logging
 10. Profiling
 11. Extending ServiceStack
 12. Documenting Web Services

# v4.0.31 Release Notes

The most requested feature since our last release was to expand our last releases support for [Server Sent Events](https://github.com/ServiceStackApps/Chat#server-sent-events) with both a scale-out **Redis ServerEvents back-end** for use in load-balanced App Servers scenarios as well as a **typed C# ServerEvents Client** - we're happy to announce we've been able to deliver both features in this release!

#### Major features in this release

  - Server Events
    - [Redis ServerEvents](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#redis-serverevents)
    - [C# ServerEvents Client](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#c-serverevents-client)
  - Redis
    - [Redis Pub/Sub Server](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#redis-pubsub-server)
  - AppSettings
    - [First-class AppSettings](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#first-class-appsettings)
    - [Writable AppSettings API](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#appsettings-are-now-writable)
  - [Metadata Pages](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#metadata-pages)
  - Authentication
    - [WebSudo](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#web-sudo)
    - [Auth Events](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#auth-events)
  - [OrmLite](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#ormlite)
  - [Text](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#text)
  - Community
    - [ServiceStack MiniProfiler Toolkit](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md#servicestack-miniprofiler-toolkit)

## Redis ServerEvents

One limitation the default `MemoryServerEvents` implementation has is being limited for use within a single App Server where all client connections are maintained. This is no longer a limitation with the new **Redis ServerEvents back-end** which utilizes a distributed redis-server back-end to provide a scale-out option capable of serving fan-out/load-balanced App Servers. If you're familiar with SignalR, this is akin to [SignalR's scaleout with Redis back-end](http://www.asp.net/signalr/overview/signalr-20/performance-and-scaling/scaleout-with-redis).

`RedisServerEvents` is a drop-in replacement for the built-in `MemoryServerEvents` that's effectively a transparent implementation detail, invisible to Server or Client API's where both implementations even [share the same integration Tests](https://github.com/ServiceStack/ServiceStack/blob/b9eb34eb80ff64fa1171d2f7f29ef359c3580eed/tests/ServiceStack.WebHost.Endpoints.Tests/ServerEventTests.cs#L169-L189).

![Redis ServerEvents Scale Out](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/gap/Chat/redis-scaleout.png)

### Enabling RedisServer Events

As a drop-in replacement it can easily be configured with just a few lines of code, as seen in the updated Chat App which can run on either [Memory or Redis ServerEvents providers](https://github.com/ServiceStackApps/Chat/blob/326617e88272d7cc0a8b7513272cf055378957e2/src/Chat/Global.asax.cs#L46-L54):

```csharp
var redisHost = AppSettings.GetString("RedisHost");
if (redisHost != null)
{
    container.Register<IRedisClientsManager>(new PooledRedisClientManager(redisHost));

    container.Register<IServerEvents>(c => 
        new RedisServerEvents(c.Resolve<IRedisClientsManager>()));
    
    container.Resolve<IServerEvents>().Start();
}
```

The above configuration will use Redis ServerEvents if there's a `RedisHost` **appSetting** in Chat's [Web.config](https://github.com/ServiceStackApps/Chat/blob/326617e88272d7cc0a8b7513272cf055378957e2/src/Chat/Web.config#L21):

```xml
<add key="RedisHost" value="localhost:6379" />
```

### Cross-platform Memory and Redis ServerEvent Enabled Chat.exe

To showcase Redis ServerEvents in action, we've prepared a stand-alone [ServiceStack.Gap](https://github.com/ServiceStack/ServiceStack.Gap) version of [Chat](http://chat.servicestack.net) compiled down into a single **Chat.exe** that can run on either Windows and OSX with Mono which can be downloaded from: 

### [Chat.zip](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/Chat.zip) (1.2MB)

[![Redis ServerEvents Preview](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/redis-server-events.gif)](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/Chat.zip)

> As Chat only runs on **2 back-end Services**, it fits well within [ServiceStack's Free Quota's](https://servicestack.net/download#free-quotas) which can be further customized and enhanced without a commercial license.

Running **Chat.exe** without any arguments will run Chat using the default **Memory ServerEvents**. This can be changed to use **Redis ServerEvents** by [un-commenting this line in appsettings.txt](https://github.com/ServiceStack/ServiceStack.Gap/blob/master/src/Chat/Chat/appsettings.txt#L5):

```
#redis localhost
```

This will require a **redis-server** running on `localhost`. If you don't have redis yet, [download redis-server for Windows](https://github.com/ServiceStack/redis-windows).

Alternatively you can specify which **port** to run Chat on and change it to use Redis ServerEvents by specifying the **redis** instance it should connect to on the command-line with:

```
Chat.exe /port=1337 /redis=localhost
```

Also included in `Chat.zip` are [test-fanout-redis-events.bat](https://github.com/ServiceStack/ServiceStack.Gap/blob/master/src/Chat/build/test-fanout-redis-events.bat) and equivalent [test-fanout-redis-events.sh](https://github.com/ServiceStack/ServiceStack.Gap/blob/master/src/Chat/build/test-fanout-redis-events.sh) helper scripts for **spawning multiple versions of Chat.exe** on different ports (and backgrounds) for **Windows or OSX**, showing how multiple clients are able to send messages to each other via Redis whilst being subscribed to different HTTP Servers:

```
START Chat.exe /port=1337 /redis=localhost
START Chat.exe /port=2337 /redis=localhost /background=http://bit.ly/1oQqhtm
START Chat.exe /port=3337 /redis=localhost /background=http://bit.ly/1yIJOBH
```

This script was used to create the animated gif above to launch **3 self-hosting instances of Chat.exe** running on **different ports**, all connected to each other via Redis. This enables some interesting peer-to-peer scenarios where users are able to run a network of (CPU/resource isolated) decentralized stand-alone HTTP Servers on their local machines, but can still communicate with each other via redis.

## C# ServerEvents Client

Like ServiceStack's other [C# Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client), the new `ServerEventsClient` is a [portable library](https://github.com/ServiceStackApps/HelloMobile) contained in the `ServiceStack.Client` NuGet package:

```
PM> Install-Package ServiceStack.Client
```

And like the Service Clients it requires the `BaseUri` of your ServiceStack instance as well as an optional `channel` for the client to subscribe to:

```csharp
var client = new ServerEventsClient("http://chat.servicestack.net", channel:"home");
```

### Managed Connection

The **C# ServerEvent Client** is a managed .NET client with feature parity with the [ServiceStack's JavaScript client](https://github.com/ServiceStackApps/Chat#client-bindings---ss-utilsjs) that **auto-reconnects** when a connection is lost, **sends periodic heartbeats** to maintain an active subscription as well as **auto-unregistering** once the client stops listening for messages, or gets disposed.

### Handling Server Events

Unlike other C# clients, the ServerEvents Client is mainly reactive in that it's primarily waiting for Server Events to be initiated from a remote server instead of the typical scenario in which requests are initiated by clients. To maximize utility, there are a number of different API's to receive and process messages:

### Assigning Callback Handlers

One way to receive messages (useful in long-running clients) is to assign handlers for each of the different events that are fired. This example shows how to capture all the different events a Client can receive:

```csharp
ServerEventConnect connectMsg = null;
var msgs = new List<ServerEventMessage>();
var commands = new List<ServerEventMessage>();
var errors = new List<Exception>();

var client = new ServerEventsClient(baseUri) {
    OnConnect = e => connectMsg = e,
    OnCommand = commands.Add,
    OnMessage = msgs.Add,
    OnException = errors.Add,
}.Start();
```

Once the Client is configured, calling `Start()` will start listening for messages and calling `Stop()` or `Dispose()` will cancel the background HTTP connection and stop it listening for server events.

### Customizing Metadata sent to clients

As ServerEvents have deep integration with the rest of ServiceStack we're able to offer [Typed Messages](https://github.com/ServiceStack/ServiceStack/blob/71b51d231d1ddb2ba7da39613e216ab75fd181c0/src/ServiceStack.Client/ServerEventsClient.cs#L14-L44) containing the users `UserAuthId`, `DisplayName` and `ProfileUrl` of the users avatar when it's available. The typed messages also offer an extensible `Dictionary<string,string> Meta` collection for maintaining custom metadata that can be sent to clients by appending to them in the ServerEventsFeature hooks, which can be defined when registering `ServerEventsFeature`:

```csharp
Plugins.Add(new ServerEventsFeature { 
    // private Connect args
    OnConnect = (subscription,httpReq) => AppendTo(subscription.Meta),

    // public Join/Leave args
    OnCreated = (subscription,httpReq) => AppendTo(subscription.Meta), 
})
``` 

### Using C# Async/Await friendly API's

Depending on your use-case, if you only want to use the ServerEvent Client for a short-time to listen for predictable responses (i.e. waiting for a Server callback on a pending request) you can alternatively use the Task-based API's letting you to participate in C# async/await workflows:

```csharp
var client = new ServerEventsClient(baseUri, channel="Home");

// Wait to receive onConnect event
ServerEventConnect connectMsg = await client.Connect();

// Wait to receive onJoin command event
ServerEventCommand joinMsg = await client.WaitForNextCommand();

// Hold a future task to get notified once a msg has been received
Task<ServerEventMessage> msgTask = client1.WaitForNextMessage();

// Send a Web Service Request using the built-in JsonServiceClient
client.ServiceClient.Post(new PostChatToChannel {
    Channel = client.Channel,     // The channel we're listening on
    From = client.SubscriptionId, // SubscriptionId Populated after Connect() 
    Message = "Hello, World!",
});

// Wait till we receive the chat Msg event we sent earlier
ServerEventMessage msg = await msgTask;
```

The above example showcases the **3 Task-based API's** available:

  1. `Connect()` wait till receiving confirmation of a successful event subscription
  2. `WaitForNextCommand()` wait for the next `onJoin` or `onLeave` subscription events
  3. `WaitForNextMessage()` wait for the next message published to the channel

The `ServiceClient` property lets you access a `JsonServiceClient` that's pre-configured with the clients `BaseUri` so that is primed for Sending Web Service Requests with.

After the ServerEvent Client has connected, the `ConnectionInfo` property is populated with the typed `ServerEventConnect` response. 

### Message Event Handlers

The above examples show generic API's for receiving any type of message, but just like in the JavaScript client, more fine-grained API's are available for handling specific message types.

The `Handlers` dictionary is akin to the JavaScript Client's [Global Event Handlers](https://github.com/ServiceStackApps/Chat#global-event-handlers) which specify lambda's to be executed when messages are sent with the `cmd.*` selector:

```csharp
client.Handlers["chat"] = (client, msg) => {
    var chatMsg = msg.Json.FromJson<ChatMessage>(); //Deserialize JSON string to typed DTO
    "Received '{0}' from '{1}'".Print(chatMsg.Message, chatMsg.FromName);
};
```

Roughly translates to the equivalent JavaScript below:

```javascript
$(source).handleServerEvents({
    handlers: {
        chat: function (msg, event) {
            console.log("Received " + msg.message + " from " + msg.fromName);
        }
    }
});
```

Where both methods handle the `ChatMessage` sent with the `cmd.chat` selector.

### Named Receivers

Whilst handlers provide a light way to handle loose-typed messages, there's a more structured and typed option that works similar to ServiceStack's `IService` classes but are used to instead handle typed Server Event Messages. 

To be able to handle messages with your own classes, get them to implement the `IReceiver` empty marker interface:

```csharp
public interface IReceiver
{
    void NoSuchMethod(string selector, object message);
}
```

Whilst primarily a marker interface, `IReceiver` does include a `NoSuchMethod` API to be able to handle messages sent with a unknown selector **target** that doesn't match any defined method or property.

**Named Receivers** are equivalent to [Receivers](https://github.com/ServiceStackApps/Chat#receivers) in the JavaScript client which can be assigned to handle all messages sent to a receiver with the selector format:

```
{receiver}.{target}
```

A Named Receiver can be registered with the API below:

```csharp
client.RegisterNamedReceiver<TestNamedReceiver>("test");
```

Which will forward all messages with a `test.*` selector to an instance of the `TestNamedReceiver` Type

```csharp
public class TestNamedReceiver : ServerEventReceiver
{
    public void FooMethod(CustomType request) {} // void return type

    public CustomType BarMethod(CustomType request)
    {        
        return request; // works with any return type, which are ignored
    }

    public CustomType BazSetter { get; set; } // Auto populate properties

    public override void NoSuchMethod(string selector, object message)
    {
        var msg = (ServerEventMessage)message;
        var nonExistentMethodCustomType = msg.Json.FromJson<CustomType>();
    }
}
```

This is roughly equivalent to the following JavaScript code:

```javascript
$(source).handleServerEvents({
    receivers: {
        test: {
            FooMethod: function (msg, event) { ... },
            BarMethod: function (msg, event) { ... },
            BazSetter: null,            
        }
    }
});
```

> The [ServerEventReceiver](https://github.com/ServiceStack/ServiceStack/blob/68c7159037e7cf2a519d482b7dae524ca073da20/src/ServiceStack.Client/ServerEventsClient.Receiver.cs#L16-L28) is a convenient base class that in addition to implementing `IReceiver` interface, gets injected with the `Client` as well as additional context about the raw message available in `base.Request`.

#### Unknown Message Handling

One difference in the JavaScript client is that messages with **unknown** targets are assigned as properties on the `test` receiver, e.g `test.QuxTarget = {..}`.

### Sending messages to Named Receivers

Once registered, an instance of `TestNamedReceiver` will process messages sent with a `test.*` selector. The example below shows how to send a DTO to each of `TestNamedReceiver` defined methods and properties:

```csharp
public class MyEventServices : Service
{
    public IServerEvents ServerEvents { get; set; }

    public void Any(CustomType request)
    {
        ServerEvents.NotifyChannel("home", "test.FooMethod", request);
        ServerEvents.NotifyChannel("home", "test.BarMethod", request);
        ServerEvents.NotifyChannel("home", "test.BazSetter", request);

        ServerEvents.NotifyChannel("home", "test.QuxTarget", request);
    }
}
```

### Life-cycle of Receivers

Similar to **Services** in ServiceStack, each message is processed with an instance of the Receiver that's resolved from `ServerEventsClient.Resolver` which by default uses the [NewInstanceResolver](https://github.com/ServiceStack/ServiceStack/blob/ec0226b97227048c3bd7c24667a71e7af7e1ff31/src/ServiceStack.Client/ServerEventsClient.Receiver.cs#L30-L36) to execute messages using a new instance of the Receiver Type: 

```csharp
public class NewInstanceResolver : IResolver
{
    public T TryResolve<T>()
    {
        return typeof(T).CreateInstance<T>();
    }
}
```

This can be changed to re-use the same instance by assigning a [SingletonInstanceResolver](https://github.com/ServiceStack/ServiceStack/blob/ec0226b97227048c3bd7c24667a71e7af7e1ff31/src/ServiceStack.Client/ServerEventsClient.Receiver.cs#L38-L46) instead:

```csharp
public class SingletonInstanceResolver : IResolver
{
    ConcurrentDictionary<Type, object> Cache = new ConcurrentDictionary<Type, object>();

    public T TryResolve<T>()
    {
        return (T)Cache.GetOrAdd(typeof(T), type => type.CreateInstance<T>());
    }
}

client.Resolver = new SingletonInstanceResolver();
```

We can also have it resolve instances from your preferred IOC. Here's an example showing how to register all Receiver Types, auto-wire them with any custom dependencies, and instruct the client to resolve instances from our IOC:

```csharp
// Register all Receivers:
client.RegisterNamedReceiver<TestNamedReceiver>("test");
...

// Register all dependencies used in a new Funq.Container:
var container = new Container();
container.RegisterAs<Dependency, IDependency>();

// Go through an auto-wire all Registered Receiver Types with Funq:
container.RegisterAutoWiredTypes(client.ReceiverTypes);

// Change the client to resolve receivers from the new Funq Container:
client.Resolver = container;
```

We can assign `Funq.Container` directly as it already implements the [IResolver](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Configuration/IResolver.cs) interface, whilst you can re-use the existing IOC **Container Adapters** to [enable support for other IOCs](https://github.com/ServiceStack/ServiceStack/wiki/The-IoC-container#use-another-ioc-container). 

### The Global Receiver

Whilst Named Receivers are used to handle messages sent to a specific namespaced selector, the client also supports registering a **Global Receiver** for handling messages sent with the special `cmd.*` selector.

#### Handling Messages with the Default Selector

All `IServerEvents` Notify API's inlcudes [overloads for sending messages without a selector](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/ServerEventsFeature.cs#L743-L771) that by convention will take the format `cmd.{TypeName}`. 

These events can be handled with a Global Receiver **based on Message type**, e.g:

```csharp
public class GlobalReceiver : ServerEventReceiver
{
    public SetterType AnyNamedProperty { get; set; }

    public void AnyNamedMethod(CustomType request)
    {
        ...
    }
}

client.RegisterReceiver<GlobalReceiver>();
```

Which will be called when messages are sent without a selector, e.g:

```csharp
public class MyServices : Service
{
    public IServerEvents ServerEvents { get; set; }

    public void Any(Request request)
    {
        ServerEvents.NotifyChannel("home", new CustomType { ... });
        ServerEvents.NotifyChannel("home", new SetterType { ... });
    }
}
```

As Global Receivers handle other messages sent with the `cmd.*` selector and can be re-used as a named receiver, we can define a single class to handle all the different custom messages sent in [chat.servicestack.net](http://chat.servicestack.net) App, E.g:

```
cmd.chat Hi
cmd.announce This is your captain speaking...
cmd.toggle#channels
css.background-image url(https://servicestack.net/img/bg.jpg)
...
```

The above messages can all be handled with the Receiver below:

```csharp
public class JavaScriptReceiver : ServerEventReceiver
{
    public void Chat(ChatMessage message) { ... }
    public void Announce(string message) { ... }
    public void Toggle(string message) { ... }
    public void BackgroundImage(string cssRule) { ... }
}

client.RegisterNamedReceiver<JavaScriptReceiver>();
client.RegisterNamedReceiver<JavaScriptReceiver>("css");
```

As seen above the **target** names are **case-insensitive** and `-` are collapsed to cater for JavaScript/CSS naming conventions.

## ServiceStack.Redis

### Redis Pub/Sub Server

To power RedisServerEvents we've extracted the managed Pub/Sub long-running message-loop originally built for [Redis MQ](https://github.com/ServiceStack/ServiceStack/wiki/Messaging-and-Redis) and encapsulated it into a re-usable class that can be used independently for handling messages published to specific [Redis Pub/Sub](http://redis.io/commands#pubsub) channels. 

`RedisPubSubServer` processes messages in a managed background thread that **automatically reconnects** when the redis-server connection fails and works like an independent background Service that can be stopped and started on command. 

The public API is captured in the [IRedisPubSubServer](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Redis/IRedisPubSubServer.cs) interface:

```csharp
public interface IRedisPubSubServer : IDisposable
{
    IRedisClientsManager ClientsManager { get; }
    // What Channels it's subscribed to
    string[] Channels { get; }

    // Run once on initial StartUp
    Action OnInit { get; set; }
    // Called each time a new Connection is Started
    Action OnStart { get; set; }
    // Invoked when Connection is broken or Stopped
    Action OnStop { get; set; }
    // Invoked after Dispose()
    Action OnDispose { get; set; }

    // Fired when each message is received
    Action<string, string> OnMessage { get; set; }
    // Fired after successfully subscribing to the specified channels
    Action<string> OnUnSubscribe { get; set; }
    // Called when an exception occurs 
    Action<Exception> OnError { get; set; }
    // Called before attempting to Failover to a new redis master
    Action<IRedisPubSubServer> OnFailover { get; set; }

    int? KeepAliveRetryAfterMs { get; set; }
    // The Current Time for RedisServer
    DateTime CurrentServerTime { get; }

    // Current Status: Starting, Started, Stopping, Stopped, Disposed
    string GetStatus();
    // Different life-cycle stats
    string GetStatsDescription();
    
    // Subscribe to specified Channels and listening for new messages
    IRedisPubSubServer Start();
    // Close active Connection and stop running background thread
    void Stop();
    // Stop than Start
    void Restart();
}
```

To use `RedisPubSubServer`, initialize it with the channels you want to subscribe to and assign handlers for each of the events you want to handle. At a minimum you'll want to handle `OnMessage`:

```csharp
var clientsManager = new PooledRedisClientManager();
var redisPubSub = new RedisPubSubServer(clientsManager, "channel-1", "channel-2") {
        OnMessage = (channel, msg) => "Received '{0}' from '{1}'".Print(msg, channel)
    }.Start();
```

Calling `Start()` after it's initialized will get it to start listening and processing any messages published to the subscribed channels.

## [App Settings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings)

For many years our solution against using .NET's complex XML configuration for App configuration is to store structured configuration in the **Web.config** appSettings which thanks to the [JSV Format](https://github.com/ServiceStack/ServiceStack.Text/wiki/JSV-Format) makes it easy to read and write structured data from a single string value, e.g:

```xml
<appSettings>
    <add key="String" value="Foo"/>
    <add key="Int" value="42"/>
    <add key="List" value="A,B,C,D,E"/>
    <add key="Dict" value="A:1,B:2,C:3"/>
    <add key="Poco" value="{Foo:Bar}"/>
</appSettings>
```

This can be easily parsed into C# types with the [IAppSettings](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Configuration/IAppSettings.cs) API:

```csharp
IAppSettings settings = new AppSettings();

string value = settings.Get("String");
int value = settings.Get("Int", defaultValue:1);
List<string> values = settings.GetList("List");
Dictionary<string,string> valuesMap = settings.GetDictionary("Dict");
MyConfig config = settings.Get("Poco", new MyConfig { Foo = "Baz" });
```

Like other ServiceStack providers, `IAppSettings` is a clean interface with multiple providers letting you easily change or override where you want to source your App configuration from:

  - **DictionarySettings** - Maintain settings in an in-memory Dictionary
  - **TextFileSettings** - Maintain settings in a plain-text file
  - **OrmLiteAppSettings** - Maintain settings in any RDBMS `Config` table

We take advantage of this in our [public OSS projects](https://github.com/ServiceStackApps/Chat) when we want to override [public appSettings with production settings](https://github.com/ServiceStackApps/HttpBenchmarks/blob/master/src/BenchmarksAnalyzer/Global.asax.cs#L29-L32) or in our [stand-alone Applications](https://github.com/ServiceStack/ServiceStack.Gap) by allowing us to ship our applications with more end-user friendly **plain-text config file** whose defaults are embedded in the stand-alone **.exe**, exporting it if it doesn't exist - letting us achieve a single, portable **.exe** that can be xcopy'ed and run as-is.

### First class AppSettings

After proving its value over the years we've decided to make it a first-class property on `IAppHost.AppSettings` which defaults to looking at .NET's App/Web.config's. 

The new [Chat.zip](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/Chat.zip) App explores different ways AppSettings can be used: 

If there's an existing `appsettings.txt` file where the **.exe** is run it will use that, otherwise it falls back to **Web.config** appSettings:

```csharp
public AppHost() : base("Chat", typeof (ServerEventsServices).Assembly)
{
    var customSettings = new FileInfo("appsettings.txt");
    AppSettings = customSettings.Exists
        ? (IAppSettings)new TextFileSettings(customSettings.FullName)
        : new AppSettings();
}
```

As a normal property in your AppHost, AppSettings can be accessed directly in `AppHost.Configure()`:

```csharp
public void Configure(Container container)
{
    ...
    var redisHost = AppSettings.GetString("RedisHost");
    if (redisHost != null)
    {
        container.Register<IServerEvents>(c => 
            new RedisServerEvents(new PooledRedisClientManager(redisHost)));
        
        container.Resolve<IServerEvents>().Start();
    }
}
```

Inside your services or IOC dependencies, like any other auto-wired dependency:

```csharp
public class ServerEventsServices : Service
{
    public IAppSettings AppSettings { get; set; }

    public void Any(PostRawToChannel request)
    {
        if (!IsAuthenticated && AppSettings.Get("LimitRemoteControlToAuthenticatedUsers", false))
            throw new HttpError(HttpStatusCode.Forbidden, "You must be authenticated to use remote control.");
        ...
    }   
}
```

Directly within Razor views:

```html
<style>
    body {
        background-image: url(@AppSettings.Get("background","/img/bg.jpg")) 
    }
</style>
```

As well as outside ServiceStack, via the `HostContext` static class:

```csharp
var redisHost = HostContext.AppSettings.GetString("redis");
```

### AppSettings are now writable

A new `Set()` API was added to [IAppSettings](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Configuration/IAppSettings.cs) letting you save any serializable property that works for all providers:

```csharp
public interface IAppSettings
{
    void Set<T>(string key, T value);
    ...
}

AppSettings.Set("Poco", new MyConfig { Foo = "Baz" });
```

In providers that support writable configuration natively like `OrmLiteAppSettings` and `DictionarySettings`, the settings get written through to the underlying provider. For read-only providers like Web.config's `AppSettings` or `TextFileSettings` a **shadowed** cache is kept that works similar to prototypal shadowing in JavaScript where if a property doesn't exist, setting a property will be stored on the top-level object instance which also takes precedence on subsequent property access.

## [Metadata Pages](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-Page)

The metadata pages have been expanded to include some of [Swagger API Attribute annotations](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API#swagger-attributes) which now shows the parameters for the Request and Response DTO's as well as any other DTO's used in each metadata operation page:

![Metadata Type Info](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/metadata-swagger-api.png)

When annotated the Description also shows any **allowable Enum values** or **range limits** when provided.

#### HtmlFormat

The humanize feature in [Auto HtmlFormat](https://github.com/ServiceStack/ServiceStack/wiki/HTML5ReportFormat) for splitting JoinedCase words with spaces can be disabled for all pages with:

```csharp
HtmlFormat.Humanize = false;
``` 

Or on adhoc pages by adding `#dehumanize` hash param.

## [Authentication](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization)

### Web Sudo

A common UX in some websites is to add an extra layer of protection for **super protected** functionality by getting users to re-confirm their password verifying it's still them using the website, common in places like confirming a financial transaction. 

**WebSudo** (by [@tvjames](https://github.com/tvjames)) is a new feature similar in spirit requiring users to re-authenticate when accessing Services annotated with the `[WebSudoRequired]` attribute. To make use of WebSudo, first register the plugin:

```csharp
Plugins.Add(new WebSudoFeature());
```

You can then apply WebSudo behavior to existing services by annotating them with `[WebSudoRequired]`:

```csharp
[WebSudoRequired]
public class RequiresWebSudoService : Service
{
    public object Any(RequiresWebSudo request)
    {
        return request;
    }
}
```

Once enabled this will throw a **402 Web Sudo Required** HTTP Error the first time the service is called:

```csharp
var requiresWebSudo = new RequiresWebSudo { Name = "test" };
try
{
    client.Send<RequiresWebSudoResponse>(requiresWebSudo); //throws
}
catch (WebServiceException)
{
    client.Send(authRequest); //re-authenticate
    var response = client.Send(requiresWebSudo); //success!
}
```

Re-authenticating afterwards will allow access to the WebSudo service.

### Auth Events

In order to enable functionality like **WebSudo** we've added additional hooks into the Authentication process with `IAuthEvents`:

```csharp
public interface IAuthEvents
{
    void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService);

    void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
        IAuthTokens tokens, Dictionary<string, string> authInfo);

    void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService);

    void OnCreated(IRequest httpReq, IAuthSession session);
}
```

These are the same authentication hooks that were previously only available when creating a **Custom UserSession** by inheriting [AuthUserSession](https://github.com/ServiceStack/ServiceStack/wiki/Sessions#session-events). The new AuthEvents API provide a loose-typed way where plugins can tap into the same hooks by registering it with `AuthFeature.AuthEvents`, e.g:

```csharp
public class WebSudoFeature : IPlugin, IAuthEvents
{
    public void Register(IAppHost appHost)
    {
        ...
        var authFeature = appHost.GetPlugin<AuthFeature>();
        authFeature.AuthEvents.Add(this);
    }

    // Add implementations on `IAuthEvents` handlers
    public void OnCreated(IRequest httpReq, IAuthSession session)
    {
        ...
    }
    ...
}
```

An alternative way for accessing `IAuthEvents` is to register it like a normal dependency, e.g:

```csharp
container.RegisterAs<LogAuthEvents,IAuthEvents>();
```

To simplify custom implementations you can inherit from the empty concrete [AuthEvents](https://github.com/ServiceStack/ServiceStack/blob/7eb3a34a2e545a54c2591665328c16c5d398d37a/src/ServiceStack/Auth/AuthEvents.cs#L18-L25) and choose to only implement the callbacks you're interested in, e.g:

```csharp
public class LogAuthEvents : AuthEvents
{
    public static ILog Log = LogManager.GetLogger(typeof(LogAuthEvents));

    public override void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService) 
    {
        Log.DebugFormat("User #{0} {1} has logged out", session.UserAuthId, session.UserName);
    }
}
```

## OrmLite

  - Added new `db.ColumnLazy` API for lazily fetching a column of data
  - Added `db.TableExists<T>` for a typed API to detect whether a table exists
  - Added `INamingStrategy.GetSequenceName()` to [override how sequence names in Oracle are generated](http://stackoverflow.com/a/25611452/85785)
  - Upgraded PostgreSql Provider to **Npgsql 2.2.0** and Sqlite to **Sqlite.Core 1.0.93.0**

## Text

  - Added `JsConfig.ParsePrimitiveIntegerTypes` and `JsConfig.ParsePrimitiveFloatingPointTypes` to [change preferences on what primitive numeric types should be converted to](https://github.com/ServiceStack/ServiceStack.Text/pull/428).
  - Added `JsConfig.IgnoreAttributesNamed` to [change what attributes are used to ignore properties](https://github.com/ServiceStack/ServiceStack.Text/commit/3b9972fbd61ce000f9af72d74d79b30eb0d2f45b)
  - Added [string.CountOccurancesOf() extension method](https://github.com/ServiceStack/ServiceStack.Text/commit/d41d7fb879d68e5a4ccb529e3cc6ed7d3ce937a2)
  - Added [Image MimeTypes](https://github.com/ServiceStack/ServiceStack.Text/commit/b07cf033d7b4735b0249850769b119be0c857b9d)

## Community

## [ServiceStack MiniProfiler Toolkit](https://bitbucket.org/migajek/miniprofilingtoolkit)

From the wider ServiceStack Community, [Michał Gajek](https://plus.google.com/u/0/+Micha%C5%82Gajek/auto) has developed an alternative analyzer of ServiceStack's MiniProfiler results in a comprehensive UI that allows deep introspection of your running Services. From the Project's description:

### Description

This project intends to provide tools for collecting & analyzing profiling results of ServiceStack-based apps.
Not only this makes profiling possible in the scenario when no built-in web-frontend is available (like Single Page Applications), but also has several advantages over it:

 - collects & persists the results
 - allows the "background" profiling (example: production environment)
 - it's better to analyze large amounts of collected profiling results, not just focusing on single execution timings
 - helps finding time-consuming queries

### Screenshots

![](http://i.imgur.com/Ybpx4xg.png)

![](http://i.imgur.com/BA4Mc68.png)

![](http://i.imgur.com/US4Gk6s.png)

![](http://i.imgur.com/bILdbQc.png)


### Install

```
PM> Install-Package Migajek.MiniProfiling.ServiceStack.RemoteStorage
```

### Register the Plugin:

```csharp
Plugins.Add(new Migajek.Profiling.ServiceStackProfiler.MiniProfilingToolkit("http://url/", "ProjectName"));
```

# v4.0.30 Release Notes

## [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference)

We have an exciting feature in this release showcasing our initial support for generating Native Types from client VS.NET projects using [ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project#step-1-download-and-install-servicestackvs) new **Add ServiceStack Reference** feature. It provides a simpler, cleaner and more versatile alternative to WCF's **Add Service Reference** in VS.NET. 

Our goal with Native Types is to provide an alternative for sharing DTO dlls, that can enable a better dev workflow for external clients who are now able to generate (and update) Typed APIs for your Services from a remote url - reducing the burden and effort required to consume ServiceStack Services whilst benefiting from clients native language strong-typing feedback.

This is just the beginning, whilst C# is the first language supported it lays the groundwork and signals our approach on adding support for typed API's in other languages in future. Add a [feature request for your favorite language](http://servicestack.uservoice.com/forums/176786-feature-requests) to prioritize support for it sooner!

### Example Usage

The easiest way to Add a ServiceStack reference to your project is to right-click on your project to bring up [ServiceStackVS's](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project) `Add ServiceStack Reference` context-menu item. This opens a dialog where you can add the url of the ServiceStack instance you want to typed DTO's for, as well as the name of the T4 template that's added to your project.

![Add ServiceStack Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/add-service-ref-flow.png)

After clicking OK, the servers DTO's and [ServiceStack.Client](https://www.nuget.org/packages/ServiceStack.Client) NuGet package are added to the project, providing an instant typed API:

![Calling ServiceStack Service](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/call-service.png)

### Consuming Services from Mobile Clients now Easier than Ever!

In addition with our improved PCL Support in this release, it's never been easier to create an instant Typed API for a remote Service consumable from any Xamarin.Android, Xamarin.iOS, Silverlgiht 5, Windows Store or .full NET4.0+ platforms - Here's a quick demo of it working in Android:

![Android Add ServiceStack Reference](https://raw.githubusercontent.com/ServiceStack/ServiceStackVS/master/Images/android-add-ref-demo.gif)

### Advantages over WCF

 - **Simple** Uses a small T4 template to save generated POCO Types. Updating as easy as re-running T4 template
 - **Versatile** Clean DTOs works in all JSON, XML, JSV, MsgPack and ProtoBuf [generic service clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client#built-in-clients)
 - **Reusable** Generated DTO's are not coupled to any endpoint or format. Defaults are both partial and virtual for maximum re-use 
 - **Resilient** Messaging-based services offer a number of [advantages over RPC Services](https://github.com/ServiceStack/ServiceStack/wiki/Advantages-of-message-based-web-services)
 - **Flexible** DTO generation is customizable, Server and Clients can override built-in defaults
 - **Integrated** Rich Service metadata annotated on DTO's, [Internal Services](https://github.com/ServiceStack/ServiceStack/wiki/Restricting-Services) are excluded when accessed externally

### Available from v4.0.30+ ServiceStack Projects

Native Types is now available by default on all **v4.0.30+** ServiceStack projects. It can be disabled by removing the `NativeTypesFeature` plugin with:

```csharp
Plugins.RemoveAll(x => x is NativeTypesFeature);
```

For detailed info on how NativeTypesFeature works, its different customization options and improvements over WCF, checkout the [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) docs.

### [Upgrade ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project)

To take advantage of this feature [Upgrade or Install ServiceStackVS](https://github.com/ServiceStack/ServiceStack/wiki/Creating-your-first-project) VS.NET Extension. If you already have **ServiceStackVS** installed, uninstall it first from `Tools -> Extensions and Updates... -> ServiceStackVS -> Uninstall`.

## Improved PCL Story

Our [PCL Story](https://github.com/ServiceStackApps/HelloMobile) has been greatly improved in this release now that `ServiceStack.Interfaces` has been converted into a pure PCL dll. This now lets you maintain your server DTO's in a pure PCL DLL that can be shared as-is on most supported platforms (Profile136):

 - Xamarin.iOS
 - Xamarin.Android
 - Windows Store
 - WPF app using .NET 4.0 PCL support
 - Silverlight 5

Whilst our impl-free `ServiceStack.Interfaces.dll` was able to be converted into a pure PCL dll, our Client libraries have instead resorted to using [PCL's Bait and Switch technique](http://log.paulbetts.org/the-bait-and-switch-pcl-trick/) to provide platform-specific extensions and optimizations. The one outlier is Silverlight5 which remains a custom (non-PCL) SL5 build, that whilst can now share DTO's, still can't support projects with dependencies on the PCL-compatible version of **ServiceStack.Client**. 

As of this release all PCL, platform and Silverlight dlls are now merged into the main [ServiceStack.Client](https://www.nuget.org/packages/ServiceStack.Client) NuGet packages so now any clients need only reference the main Client NuGet package:

```
Install-Package ServiceStack.Client
``` 

The [Hello PCL](https://github.com/ServiceStackApps/HelloMobile) project now contains examples of reusing a Server DTO project with all supported client platforms as well as showing re-use of a high-level `SharedGateway` which referenes `ServiceStack.Client` that's shared between all PCL-compatible platforms. 

### New ServiceStack + AngularJS Example - [StackApis](http://stackapis.servicestack.net)

[![StackApis Home](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/stackapis-home.png)](http://stackapis.servicestack.net/)

[StackApis](http://stackapis.servicestack.net/) is a simple new ServiceStack + AngularJS example project created with [ServiceStackVS AngularJS Template](https://github.com/ServiceStack/ServiceStackVS#servicestackvs) showcasing how quick and easy it is to create responsive feature-rich Single Page Apps with AngularJS and [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query). StackApis is powered by a Sqlite database containing [snapshot of ServiceStack questions from StackOverflow APIs](https://github.com/ServiceStackApps/StackApis/blob/master/src/StackApis.Tests/UnitTests.cs#L67) that's [persisted in an sqlite database](https://github.com/ServiceStackApps/StackApis/blob/master/src/StackApis.Tests/UnitTests.cs#L119-L124) using [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite/).

### StackApis AutoQuery Service

The [Home Page](https://github.com/ServiceStackApps/StackApis/blob/master/src/StackApis/default.cshtml) is built with less than **<50 Lines** of JavaScript which thanks to [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) routes all requests to the single AutoQuery Service below:

```csharp
[Route("/questions")]
public class StackOverflowQuery : QueryBase<Question>
{
    public int? ScoreGreaterThan { get; set; }
}
```

> Not even `ScoreGreaterThan` is a required property, it's just an example of a [formalized convention](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#advantages-of-well-defined-service-contracts) enabling queries from Typed Service Clients.

Feel free to play around with a deployed version of StackApis at [stackapis.servicestack.net](http://stackapis.servicestack.net/).

You can also use the public `http://stackapis.servicestack.net/` url to test out ServiceStack's new **Add ServiceStack Reference** feature :)

## [Swagger Support](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API)

### All static resources are now embedded 

ServiceStack's [Swagger Support](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API) received some welcomed enhancements thanks to [@tvjames](https://github.com/tvjames) and [@tyst](https://github.com/tyst)'s efforts which now sees all of Swagger's static resources embedded into a single `ServiceStack.Api.Swagger.dll`, taking advantage of the Virtual File Systems [transparent support for Embedded Resources](https://github.com/ServiceStack/ServiceStack.Gap#creating-an-embedded-servicestack-app), making it easier to manage and upgrade Swagger as a self-contained unit.

### New Bootstrap theme for Swagger

A new attractive Bootstrap Theme was also added to Swagger, available from [/swagger-ui-bootstrap/](http://stackapis.servicestack.net/swagger-ui-bootstrap/):

[![Swagger Bootstrap](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/StackApis/stackapis-swagger-bootstrap.png)](http://stackapis.servicestack.net/swagger-ui-bootstrap/)

You can change the [metadata page plugin link](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-page#adding-links-to-metadata-page) to point to this new theme with:

```csharp
Plugins.Add(new SwaggerFeature {
    UseBootstrapTheme = true, 
    LogoUrl = "your-logo.png" //optional use your own logo
});
```

Swagger was also been updated to the latest version.

## Authentication

### Unique Emails

ServiceStack now verifies emails returned by OAuth providers are now unique where if there's already another UserAuth with an existing email, authentication will fail and redirect (for HTML/Web Browser requests) with the Error token: 

    /#f=EmailAlreadyExists

This behavior is in-line with ServiceStack's other AuthProviders. If this change causes any issues, it can be disabled with:

```csharp
AuthProvider.ValidateUniqueEmails = false;
```

> This doesn't apply to Users who login with multiple OAuth Providers as there accounts automatically get merged into a single UserAuth entity.

### CustomValidationFilter

A new `CustomValidationFilter` was added to all AuthProviders which can be used to return a `IHttpResult` to control what error response is returned, e.g: 

```csharp
Plugins.Add(new AuthFeature(
    () => new CustomUserSession(), 
    new IAuthProvider[] {
        new FacebookAuthProvider(appSettings) { 
            CustomValidationFilter = authCtx => 
                CustomIsValid(authCtx) 
                    ? authCtx.Service.Redirect(authCtx.Session.ReferrerUrl
                        .AddHashParam("f","CustomErrorCode"))
                    : null,
        },
    }));
```

## Breaking Changes

### Upgrade all ServiceStack NuGet packages

The primary breaking change was converting ServiceStack's core `ServiceStack.Interfaces.dll` into a pure portable class library which as it's incompatible with the previous non-PCL ServiceStack.Interfaces.dll requires that all NuGet dependenices (inc. transitive dependencies) be upgraded to **v4.0.30**. The version number was bumped to **v4.0.30** specifically to stress that it's incompatible with any **<v4.0.2x** before it. The only other issue we ran into after upgrading most of ServiceStack projects is on projects that reference or mock Interfaces that reference a `System.Net.*` Type like `HttpWebResponse` in `IServiceClient` will now require an explicit reference to `System.Net` for the C# compiler to consider them to be of the same type.

In summary if you have a build error when upgrading v4.0.30 then:
  - Delete any older v4.0.2x SS packages from NuGet /packages
  - Reference `System.Net` on projects that still have build errors

More details about these issues is available on the [announcement post](https://plus.google.com/+DemisBellot/posts/SyVJR419sdE).

### TypeDescriptor support removed 

In order to convert ServiceStack.Interfaces into a portable class library we've had to remove support for an undocumented feature allowing adding of Attributes via .NET's TypeDescriptor. If you were using TypeDescriptor, you can switch to adding attributes dynamically using [ServiceStack's Reflection APIs](https://github.com/ServiceStack/ServiceStack.Text/blob/master/tests/ServiceStack.Text.Tests/AttributeTests.cs).

# v4.0.24 Release Notes

## [Server Events](https://github.com/ServiceStackApps/Chat)

In keeping with our quest to provide a simple, lean and deep integrated technology stack for all your web framework needs we've added support in this release for Server push communications with our initial support for [Server Sent Events](http://www.html5rocks.com/en/tutorials/eventsource/basics/). 

[Server Sent Events](http://www.html5rocks.com/en/tutorials/eventsource/basics/) (SSE) is an elegant [web technology](http://dev.w3.org/html5/eventsource/) for efficiently receiving push notifications from any HTTP Server. It can be thought of as a mix between long polling and one-way WebSockets and contains many benefits over each:

  - **Simple** - Server Sent Events is just a single long-lived HTTP Request that any HTTP Server and Web Framework can support
  - **Efficient** - Each client uses a single TCP connection and each message avoids the overhead of HTTP Connections and Headers that's [often faster than Web Sockets](http://matthiasnehlsen.com/blog/2013/05/01/server-sent-events-vs-websockets/).
  - **Resilient** - Browsers automatically detect when a connection is broken and automatically reconnects
  - **Interoperable** - As it's just plain-old HTTP, it's introspectable with your favorite HTTP Tools and even works through HTTP proxies (with buffering and checked-encoding off).
  - **Well Supported** - As a Web Standard it's supported in all major browsers except for IE which [can be enabled with polyfills](http://html5doctor.com/server-sent-events/#yaffle).

Server Events provides a number of API's that allow sending messages to:

  - All Users
  - All Users subscribed to a channel
  - A Single Users Subscription

It also includes deep integration with ServiceStack's [Sessions](https://github.com/ServiceStack/ServiceStack/wiki/Sessions) and [Authentication Providers](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization) which also sending messages to uses using either:

  - UserAuthId
  - UserName
  - Permanent Session Id (ss-pid)

### Registering

List most other [modular functionality](https://github.com/ServiceStack/ServiceStack/wiki/Plugins) in ServiceStack, Server Sent Events is encapsulated in a single Plugin that can be registered in your AppHost with:

```csharp
Plugins.Add(new ServerEventsFeature());
```

### [ServiceStack Chat (beta)](https://github.com/ServiceStackApps/Chat)

[![Chat Overview](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/Chat/chat-overview.gif)](https://github.com/ServiceStackApps/Chat)

To demonstrate how to make use Server Events we've created a cursory Chat web app for showcasing server push notifications packed with a number of features including:

  - Anonymous or Authenticated access with Twitter, Facebook or GitHub OAuth
  - Joining any arbitrary user-defined channel
  - Private messaging
  - Command history
  - Autocomplete of user names
  - Highlighting of mentions
  - Grouping messages by user
  - Active list of users, kept live with:
    - Periodic Heartbeats
    - Automatic unregistration on page unload
  - Remote Control
    - Send a global announcement to all users
    - Toggle on/off channel controls
    - Change the CSS style of any element
    - Change the HTML document's title
    - Redirect users to any url
    - Play a youtube video
    - Display an image url
    - Raise DOM events

<img src="https://github.com/ServiceStack/Assets/blob/master/img/apps/Chat/vs-sln.png" width="257" align="right" hspace="10">

Chat is another ServiceStack Single Page App Special showing how you can get a lot done with minimal effort and dependencies which delivers all these features in a tiny footprint built with vanilla jQuery and weighing just:

  - [1 default.cshtml page](https://github.com/ServiceStackApps/Chat/blob/master/src/Chat/default.cshtml) with under **170 lines of JavaScript** and **70 lines** of HTML
  - [2 ServiceStack Services](https://github.com/ServiceStackApps/Chat/blob/master/src/Chat/Global.asax.cs) entire backend in 1 `.cs` file
  - 1 ASP.NET Web Application project requiring only a sane **9 .NET dll** references

### Remote control

Chat features the ability to remotely control other users chat window with the client bindings in `/js/ss-utils.js`, providing a number of different ways to interact and modify a live webapp by either:

  - Invoking Global Event Handlers
  - Modifying CSS via jQuery
  - Sending messages to Receivers
  - Raising jQuery Events

All options above are designed to integrate with an apps existing functionality by providing the ability to invoke predefined handlers and exported object instances as well as modify jQuery CSS and raising DOM events.

The [complete documentation](https://github.com/ServiceStackApps/Chat) in Chat is the recommended way to learn more about Server Events which goes through and explains how to use its Server and Client features.

## [ServiceStackVS](https://github.com/ServiceStack/ServiceStackVS) - ServiceStack's VS.NET Extension

Another exciting announcement is the initial release of [ServiceStackVS](https://github.com/ServiceStack/ServiceStackVS) - our VS.NET ServiceStack Extension containing the most popular starting templates for ServiceStack powered solutions:

![Visual Studio Templates](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/vs-templates.png)

Each project template supports our [recommended multi-project structure](https://github.com/ServiceStack/ServiceStack/wiki/Physical-project-structure) promoting a clean architecture and Web Services best-practices, previously [documented in Email Contacts](https://github.com/ServiceStack/EmailContacts/#creating-emailcontacts-solution-from-scratch).

This is now the fastest way to get up and running with ServiceStack. With these new templates you can now create a new ServiceStack Razor, AngularJS and Bootstrap enabled WebApp, pre-wired end-to-end in seconds:

![AngularJS WalkThrough](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/angularjs-overview.gif)

<a href="http://www.packtpub.com/learning-angularjs-for-net-developers/book"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/community/learning-angularjs.jpg" width="175" align="right" hspace="10"></a>

### Get the [Learning AngularJS for .NET Developers](http://www.packtpub.com/learning-angularjs-for-net-developers/book) Book!

On ServiceStack and AngularJS front, we also have great content coming from the ServiceStack community as 
**[Learning AngularJS for .NET Developers](http://www.packtpub.com/learning-angularjs-for-net-developers/book)**, 
a new book by [Alex Pop](https://twitter.com/AlexandruVPop) has just been made available. 

More details about the book as well as downloadable code-samples is available on 
[Alex's announcement blog post](http://alexvpop.blogspot.co.uk/2014/06/announcing-learning-angularjs-dotnet.html).

### Download ServiceStackVS

ServiceStackVS supports both VS.NET 2013 and 2012 and can be [downloaded from the Visual Studio Gallery](http://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

[![VS.NET Gallery Download](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/vsgallery-download.png)](http://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

### VS.NET 2012 Prerequisites

  - VS.NET 2012 Users must install the [Microsoft Visual Studio Shell Redistributable](http://www.microsoft.com/en-au/download/details.aspx?id=40764)
  - It's also highly recommended to [Update to the latest NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). 

> Alternatively if continuing to use an older version of the **NuGet Package Manager** you will need to click on **Enable NuGet Package Restore** after creating a new project to ensure its NuGet dependencies are installed.

### Feedback

We hope **ServiceStackVS** helps make ServiceStack developers more productive than ever and we'll look at continue improving it with new features in future. [Suggestions and feedback are welcome](http://servicestack.uservoice.com/forums/176786-feature-requests).  

## [Authentication](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization)

### Saving User Profile Images

To make it easier to build Social Apps like [Chat](https://github.com/ServiceStackApps/Chat) with ServiceStack we've started saving profile image urls (aka avatars) for the following popular OAuth providers:

 - Twitter
 - Facebook
 - GitHub
 - Google OAuth2
 - LinkedIn OAuth2

The users profile url can be accessed in your services using the `IAuthSession.GetProfileUrl()` extension method which goes through the new `IAuthMetadataProvider` which by default looks in `UserAuthDetails.Items["profileUrl"]`.

### New IAuthMetadataProvider

A new [IAuthMetadataProvider](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Auth/AuthMetadataProvider.cs) has been added that provides a way to customize the `authInfo` in all AuthProviders. It also allows overriding of how extended Auth metadata like `profileUrl` is returned.

```csharp
public interface IAuthMetadataProvider
{
    void AddMetadata(IAuthTokens tokens, Dictionary<string, string> authInfo);

    string GetProfileUrl(IAuthSession authSession, string defaultUrl = null);
}
```

> To override with a custom implementation, register `IAuthMetadataProvider` in the IOC

### Saving OAuth Metadata


The new `SaveExtendedUserInfo` property (enabled by default) on all OAuth providers let you control whether to save the extended OAuth metadata available (into `UserAuthDetails.Items`) when logging in via OAuth.

## [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite/)

### Loading of References in Multi-Select Queries

Previous support of pre-loading of references were limited to a single entity using `LoadSingleById` to automatically fetch all child references, e.g:

```csharp
public class Customer
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }

    [Reference] // Save in CustomerAddress table
    public CustomerAddress PrimaryAddress { get; set; }

    [Reference] // Save in Order table
    public List<Order> Orders { get; set; }
}

var customer = db.LoadSingleById<Customer>(request.Id);
customer.PrimaryAddress   // Loads 1:1 CustomerAddress record 
customer.Orders           // Loads 1:M Order records 
```

We've now also added support for pre-loading of references for multiple resultsets as well with `LoadSelect` which loads references for all results, e.g:

```csharp
var customers = db.LoadSelect<Customer>(q => q.Name.StartsWith("A"));
```

This is implemented efficiently behind the scenes where only 1 additional SQL Query is performed for each defined reference.

> As a design goal none of OrmLite Query API's perform N+1 queries.

### Self References

We've extended OrmLite [References support](https://github.com/ServiceStack/ServiceStack.OrmLite/#reference-support-poco-style) to support Self References for **1:1** relations where the foreign key property can be on the parent table, e.g: 

```csharp
public class Customer
{
    ...
    public int CustomerAddressId { get; set; }

    [Reference]
    public CustomerAddress PrimaryAddress { get; set; }
}
```

Which maintains the same relationship as having the Foreign Key column on the child table instead, i,e:

```csharp
public class CustomerAddress
{
    public int CustomerId { get; set; }
}
```

### Support Foreign Key Attributes to specify Reference Fields

Previously definitions of references relied on [Reference Conventions](https://github.com/ServiceStack/ServiceStack.OrmLite/#reference-conventions) using either the C# Property Name or Property Aliases. You can now also use the [References and ForeignKey attributes](https://github.com/ServiceStack/ServiceStack.OrmLite/#new-foreign-key-attribute-for-referential-actions-on-updatedeletes) to specify Reference Properties, e.g:

```csharp
public class Customer
{
    [Reference(typeof(CustomerAddress))]
    public int PrimaryAddressId { get; set; }

    [Reference]
    public CustomerAddress PrimaryAddress { get; set; }
}
```

> Reference Attributes take precedence over naming conventions

### Support for Stored Procedures with out params

A new `SqlProc` API was added returning an `IDbCommand` which can be used to customize the Stored Procedure call letting you add custom out parameters. The example below shows 

```csharp
string spSql = @"DROP PROCEDURE IF EXISTS spSearchLetters;
    CREATE PROCEDURE spSearchLetters (IN pLetter varchar(10), OUT pTotal int)
    BEGIN
        SELECT COUNT(*) FROM LetterFrequency WHERE Letter = pLetter INTO pTotal;
        SELECT * FROM LetterFrequency WHERE Letter = pLetter;
    END";

db.ExecuteSql(spSql);

var cmd = db.SqlProc("spSearchLetters", new { pLetter = "C" });
var pTotal = cmd.AddParam("pTotal", direction: ParameterDirection.Output);

var results = cmd.ConvertToList<LetterFrequency>();
var total = pTotal.Value;
```

An alternative approach is to use the new overload added to the raw SQL API `SqlList` that lets you customize the Stored Procedure using a filter, e.g:

```csharp
IDbDataParameter pTotal = null;
var results = db.SqlList<LetterFrequency>("spSearchLetters", cmd => {
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.AddParam("pLetter", "C");
        pTotal = cmd.AddParam("pTotal", direction: ParameterDirection.Output);
    });
var total = pTotal.Value;
```

### Minor OrmLite Features

 - Use `OrmLiteConfig.DisableColumnGuessFallback=false` to disable fallback matching heuristics
 - Added [GenericTableExpressions](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/Expression/GenericTableExpressions.cs) example showing how to extend OrmLite to support different runtime table names on a single schema type.

## [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query)

### Support for loading References

AutoQuery now takes advantage of OrmLite's new support for loading child references where marking your Query DTO with `[Reference]` will automatically load its related data, e.g:

```csharp
public class Rockstar
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int? Age { get; set; }

    [Reference]
    public List<RockstarAlbum> Albums { get; set; } 
}
```

### Improved OrderBy

Add support for inverting sort direction of individual orderBy fields using '-' prefix e.g: 

```csharp
// ?orderBy=Rating,-ImdbId
var movies = client.Get(new SearchMovies { OrderBy = "Rating,-ImdbId" });

// ?orderByDesc=-Rating,ImdbId
var movies = client.Get(new SearchMovies { OrderByDesc = "-Rating,ImdbId" });
```

## ServiceStack.Text

 - Added support for `OrderedDictionary` and other uncommon `IDictionary` types
 - WCF-style `JsConfig.OnSerializedFn` custom hook has been added
 - `JsConfig.ReuseStringBuffer` is enabled by default for faster JSON/JSV text serialization
 - Properties can also be ignored with `[JsonIgnore]` attribute

## Other Features

  - New `[Exclude(Feature.Soap)]` attribute can be used to exclude types from XSD/WSDL's
  - XSD/WSDL's no longer including open generic types
  - Added `$.ss.getSelection()`, `$.ss.queryString()`, `$.ss.splitOnFirst()`, `$.ss.splitOnLast()` to /ss-utils.js
  - `TwitterAuthProvider` now makes authenticated v1.1 API requests to fetch user metadata


# v4.0.23 Release Notes

## [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query)

The big ticket feature in this release is the new [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) feature - with our approach of enabling Queryable Data Services, that's designed to avoid [OData's anti-patterns and pitfalls](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#why-not-odata).

 - Simple, intuitive and easy to use!
 - Works with all OrmLite's [supported RDBMS providers](https://github.com/ServiceStack/ServiceStack.OrmLite/#download)
 - Supports multiple table JOINs and custom responses
 - Code-first, declarative programming model
 - Promotes clean, intent-based self-describing API's
 - Highly extensible, implementations are completely overridable
 - Configurable Adhoc, Explicit and Implicit conventions
 - Allows preemptive client queries
 - New `GetLazy()` API in Service Clients allow transparently streaming of paged queries
 - Raw SqlFilters available if required

#### AutoQuery Services are normal ServiceStack Services

AutoQuery also benefits from just being normal ServiceStack Services where you can re-use existing knowledge in implementing, customizing, introspecting and consuming ServiceStack services, i.e:

 - Utilizes the same customizable [Request Pipeline](https://github.com/ServiceStack/ServiceStack/wiki/Order-of-Operations)
 - AutoQuery services can be mapped to any [user-defined route](https://github.com/ServiceStack/ServiceStack/wiki/Routing)
 - Is available in all [registered formats](https://github.com/ServiceStack/ServiceStack/wiki/Formats)
   - The [CSV Format](https://github.com/ServiceStack/ServiceStack/wiki/ServiceStack-CSV-Format) especially shines in AutoQuery who's tabular result-set are perfect for CSV
 - Can be [consumed from typed Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/Clients-overview) allowing an end-to-end API without code-gen in [PCL client platforms as well](https://github.com/ServiceStackApps/HelloMobile)

### Getting Started

AutoQuery uses your Services existing OrmLite DB registration, the example below registers an InMemory Sqlite Provider:

```csharp
container.Register<IDbConnectionFactory>(
    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
```

There are no additional dependencies, enabling AutoQuery is as easy as registering the AutoQueryFeature Plugin:

```csharp
Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
```

The configuration above limits all queries to a maximum of **100** results.

The minimum code to expose a Query Service for the `Rockstar` table under a user-defined Route is just:

```csharp
[Route("/rockstars")]
public class FindRockstars : QueryBase<Rockstar> {}
```

With no additional code, this allows you to use any of the registered built-in conventions, e.g:

    /rockstars?Ids=1,2,3
    /rockstars?AgeOlderThan=42
    /rockstars?AgeGreaterThanOrEqualTo=42
    /rockstars?FirstNameIn=Jim,Kurt
    /rockstars?FirstNameBetween=A,F
    /rockstars?FirstNameStartsWith=Jim
    /rockstars?LastNameEndsWith=son
    /rockstars?IdAbove=1000

You're also able to formalize your API by adding concrete properties to your Request DTO:

```csharp
public class QueryRockstars : QueryBase<Rockstar>
{
    public int? AgeOlderThan { get; set; }
}
```

Which now lets you access AutoQuery Services from the ServiceStack's [Typed Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client):

```csharp
client.Get(new QueryRockstars { AgeOlderThan = 42 });
```

You can also take advantage of the new `GetLazy()` API to transparently stream large result-sets in managable-sized chunks:

```csharp
var results = client.GetLazy(new QueryMovies { Ratings = new[]{"G","PG-13"}}).ToList();
```

As GetLazy returns a lazy `IEnumerable<T>` sequence it can be used within LINQ expressions:

```csharp
var top250 = client.GetLazy(new QueryMovies { Ratings = new[]{ "G", "PG-13" } })
    .Take(250)
    .ConvertTo(x => x.Title);
```

This is just a sampler, for a more complete guide to AutoQuery checkout the [AutoQuery wiki](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query).

## New VistaDB OrmLite Provider!

Also in this release is a preview release of OrmLite's new support for [VistaDB](http://www.gibraltarsoftware.com/) thanks to the efforts of [Ilya Lukyanov](https://github.com/ilyalukyanov).

[VistaDB](http://www.gibraltarsoftware.com/) is a commercial easy-to-deploy SQL Server-compatible embedded database for .NET that provides a good alternative to Sqlite for embedded scenarios.

To use first download and install [VistaDB](http://www.gibraltarsoftware.com/) itself, then grab OrmLite's VistaDB provider from NuGet:

    PM> Install-Package ServiceStack.OrmLite.VistaDb

Then register the VistaDB Provider and the filename of what embedded database to use with:

```csharp
VistaDbDialect.Provider.UseLibraryFromGac = true;

container.Register<IDbConnectionFactory>(
    new OrmLiteConnectionFactory("Data Source=db.vb5;", VistaDbDialect.Provider));
```

The VistaDB provider is almost a complete OrmLite provider, the one major missing feature is OrmLite's new support for [Optimistic Concurrency](https://github.com/ServiceStack/ServiceStack.OrmLite/#optimistic-concurrency) which is missing in VistaDB which doesn't support normal Database triggers but we're still researching the most optimal way to implement this in VistaDB.

## Improved AspNetWindowsAuthProvider

A new `LoadUserAuthFilter` was added to allow `AspNetWindowsAuthProvider` to retrieve more detailed information about Windows Authenticated users by using the .NET's ActiveDirectory services, e.g:

```csharp
public void LoadUserAuthInfo(AuthUserSession userSession, 
    IAuthTokens tokens, Dictionary<string, string> authInfo)
{
    if (userSession == null) return;
    using (PrincipalContext pc = new PrincipalContext(ContextType.Domain))
    {
        var user = UserPrincipal.FindByIdentity(pc, userSession.UserAuthName);
        tokens.DisplayName = user.DisplayName;
        tokens.Email = user.EmailAddress;
        tokens.FirstName = user.GivenName;
        tokens.LastName = user.Surname;
        tokens.FullName = (String.IsNullOrWhiteSpace(user.MiddleName))
            ? "{0} {1}".Fmt(user.GivenName, user.Surname)
            : "{0} {1} {2}".Fmt(user.GivenName, user.MiddleName, user.Surname);
        tokens.PhoneNumber = user.VoiceTelephoneNumber;
    }
}
```

Then to use the above custom filter register it in AspNetWindowsAuthProvider with:

```csharp
Plugins.Add(new AuthFeature(
    () => new CustomUserSession(),
    new IAuthProvider[] {
        new AspNetWindowsAuthProvider(this) {
            LoadUserAuthFilter = LoadUserAuthInfo
        }
    ));
```

Above example kindly provided by [Kevin Howard](https://github.com/KevinHoward).

## Other features

 - [OrmLite's T4 Templates](https://github.com/ServiceStack/ServiceStack.OrmLite/#t4-template-support) were improved by [Darren Reid](https://github.com/Layoric)
 - ApiVersion added to Swaggers ResourcesResponse DTO
 - `Uri` in RedisClient allows passwords

# v4.0.22 Release Notes

## OrmLite

This was primarily an OrmLite-focused release with the introduction of major new features:

### Typed SQL Expressions now support Joins!

Another [highly requested feature](http://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/4459040-enhance-ormlite-with-common-data-usage-patterns) has been realized in this release with OrmLite's typed SqlExpressions extended to add support for Joins. 

The new JOIN support follows OrmLite's traditional approach of a providing a DRY, typed RDBMS-agnostic wrapper that retains a high affinity with SQL, providing an intuitive API that generates predictable SQL and a light-weight mapping to clean POCO's.

### Basic Example

Starting with the most basic example you can simply specify the table you want to join with:

```csharp
var dbCustomers = db.Select<Customer>(q => q.Join<CustomerAddress>());
```

This query rougly maps to the following SQL:

```sql
SELECT Customer.* 
  FROM Customer 
       INNER JOIN 
       CustomerAddress ON (Customer.Id == CustomerAddress.Id)
```

Just like before `q` is an instance of `SqlExpression<Customer>` which is bounded to the base `Customer` type (and what any subsequent implicit API's apply to). 

To better illustrate the above query, lets expand it to the equivalent explicit query:

```csharp
SqlExpression<Customer> q = db.From<Customer>();
q.Join<Customer,CustomerAddress>();

List<Customer> dbCustomers = db.Select(q);
```

### Reference Conventions

The above query joins together the `Customer` and `CustomerAddress` POCO's using the same relationship convention used in [OrmLite's support for References](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/LoadReferencesTests.cs), i.e. using the referenced table `{ParentType}Id` property convention.

An example of what this looks like can be seen the POCO's below:

```csharp
class Customer {
    public Id { get; set; }
    ...
}
class CustomerAddress {
    public Id { get; set; }
    public CustomerId { get; set; }  // Reference based on Property name convention
}
```

References based on matching alias names is also supported, e.g:

```csharp
[Alias("LegacyCustomer")]
class Customer {
    public Id { get; set; }
    ...
}
class CustomerAddress {
    public Id { get; set; }

    [Alias("LegacyCustomerId")]             // Matches `LegacyCustomer` Alias
    public RenamedCustomerId { get; set; }  // Reference based on Alias Convention
}
```

Either convention lets you save a POCO and all its entity references with `db.Save()`, e.g:

```csharp
var customer =  new Customer {
    Name = "Customer 1",
    PrimaryAddress = new CustomerAddress {
        AddressLine1 = "1 Australia Street",
        Country = "Australia"
    },
};
db.Save(customer, references:true);
```

Going back to the above example: 

```csharp
q.Join<CustomerAddress>();
```

Uses the implicit join in the above reference convention to expand into the equivalent explicit API: 

```csharp
q.Join<Customer,CustomerAddress>((customer,address) => customer.Id == address.CustomerId);
```

### Selecting multiple columns across joined tables

Another behaviour implicit when selecting from a typed SqlExpression is that results are mapped to the `Customer` POCO. To change this default we just need to explicitly specify what POCO it should map to instead:

```csharp
List<FullCustomerInfo> customers = db.Select<FullCustomerInfo>(
    db.From<Customer>().Join<CustomerAddress>());
```

Where `FullCustomerInfo` is any POCO that contains a combination of properties matching any of the joined tables in the query. 

The above example is also equivalent to the shorthand `db.Select<Into,From>()` API:

```csharp
var customers = db.Select<FullCustomerInfo,Customer>(q => q.Join<CustomerAddress>());
```

Rules for how results are mapped is simply each property on `FullCustomerInfo` is mapped to the first matching property in any of the tables in the order they were added to the SqlExpression.

As most OrmLite tables have a primary key property named `Id`, the auto-mapping includes a fallback for mapping to a full namespaced Id property in the same `{Type}Id` format. This allows you to auto-populate `CustomerId`, `CustomerAddressId` and `OrderId` columns even though they aren't a match to any of the fields in any of the joined tables.

### Advanced Example

Seeing how the SqlExpression is constructed, joined and mapped, we can take a look at a more advanced example to showcase more of the new API's available:

```csharp
List<FullCustomerInfo> rows = db.Select<FullCustomerInfo>( // Map results to FullCustomerInfo POCO
  db.From<Customer>()                                      // Create typed Customer SqlExpression
    .LeftJoin<CustomerAddress>()                           // Implict left join with base table
    .Join<Customer, Order>((c,o) => c.Id == o.CustomerId)  // Explicit join and condition
    .Where(c => c.Name == "Customer 1")                    // Implicit condition on base table
    .And<Order>(o => o.Cost < 2)                           // Explicit condition on joined Table
    .Or<Customer,Order>((c,o) => c.Name == o.LineItem));   // Explicit condition with joined Tables
```

The comments next to each line document each Type of API used. Some of the new API's introduced in this example include:

  - Usage of `LeftJoin` for LEFT JOIN'S, `RightJoin` and `FullJoin` also available
  - Usage of `And<Table>()`, to specify a condition on a Joined table 
  - Usage of `Or<Table1,Table2>`, to specify a condition against 2 joined tables

More code examples of References and Joined tables are available in:

  - [LoadReferencesTests.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/LoadReferencesTests.cs)
  - [LoadReferencesJoinTests.cs](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/LoadReferencesJoinTests.cs)

## Optimistic Concurrency

Another major feature added to OrmLite is support for optimistic concurrency which can be added to any table by adding a `ulong RowVersion { get; set; }` property, e.g:

```csharp
public class Poco
{
    ...
    public ulong RowVersion { get; set; }
}
```

RowVersion is implemented efficiently in all major RDBMS's, i.e:

 - Uses `rowversion` datatype in SqlServer 
 - Uses PostgreSql's `xmin` system column (no column on table required)
 - Uses UPDATE triggers on MySql, Sqlite and Oracle whose lifetime is attached to Create/Drop tables APIs

Despite their differing implementations each provider works the same way where the `RowVersion` property is populated when the record is selected and only updates the record if the RowVersion matches with what's in the database, e.g:

```csharp
var rowId = db.Insert(new Poco { Text = "Text" }, selectIdentity:true);

var row = db.SingleById<Poco>(rowId);
row.Text += " Updated";
db.Update(row); //success!

row.Text += "Attempting to update stale record";

//Can't update stale record
Assert.Throws<OptimisticConcurrencyException>(() =>
    db.Update(row));

//Can update latest version
var updatedRow = db.SingleById<Poco>(rowId);  // fresh version
updatedRow.Text += "Update Success!";
db.Update(updatedRow);

updatedRow = db.SingleById<Poco>(rowId);
db.Delete(updatedRow);                        // can delete fresh version
```

Optimistic concurrency is only verified on API's that update or delete an entire entity, i.e. it's not enforced in partial updates. There's also an Alternative API available for DELETE's:

```csharp
db.DeleteById<Poco>(id:updatedRow.Id, rowversion:updatedRow.RowVersion)
```

### Other OrmLite features

  - New [Limit API's added to JoinSqlBuilder](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/Expression/SqlExpressionTests.cs#L126-L168)
  - SqlExpression's are now tied to the dialect provider at time of creation

## ServiceStack.Text

A new `JsConfig.ReuseStringBuffer` performance config option is available to JSON and JSV Text Serializers which lets you re-use ThreadStatic StringBuilder when serializing to a string. In initial benchmarks (both synchronous and parallel) it shows around a **~%30 increase in performance** for small POCO's. It can be enabled with:

```csharp
JsConfig.ReuseStringBuffer = true;
```

Default enum values can be excluded from being serialized with:

```csharp
JsConfig.IncludeDefaultEnums = false;
```

## ServiceStack

### [Messaging](https://github.com/ServiceStack/ServiceStack/wiki/Messaging)

Improved support for the MQ Request/Reply pattern with the new `GetTempQueueName()` API now available in all MQ Clients which returns a temporary queue (prefixed with `mq:tmp:`) suitable for use as the ReplyTo queue in Request/Reply scenarios:

```csharp
mqServer.RegisterHandler<Hello>(m =>
    new HelloResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) });
mqServer.Start();

using (var mqClient = mqServer.CreateMessageQueueClient())
{
    var replyToMq = mqClient.GetTempQueueName();
    mqClient.Publish(new Message<Hello>(new Hello { Name = "World" }) {
        ReplyTo = replyToMq
    });

    IMessage<HelloResponse> responseMsg = mqClient.Get<HelloResponse>(replyToMq);
    mqClient.Ack(responseMsg);
    var responseDto = responseMsg.GetBody(); 
}
```

On [Rabbit MQ](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ) it creates an exclusive non-durable queue. 

In [Redis MQ](https://github.com/ServiceStack/ServiceStack/wiki/Messaging-and-Redis) there's a new `RedisMqServer.ExpireTemporaryQueues()` API which can be used on StartUp to expire temporary queues after a given period.

Synchronous and Parallel tests for this feature is available in [MqRequestReplyTests.cs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Server.Tests/Messaging/MqRequestReplyTests.cs).

## New NuGet packages

  - [ServiceStack.Authentication.LightSpeed](https://www.nuget.org/packages/ServiceStack.Authentication.LightSpeed/) is a new User Auth Repository created by [Herdy Handoko](https://plus.google.com/u/0/+HerdyHandoko/posts) providing a new persistence option for User Authentication backed by [Mindscape's LightSpeed ORM](http://www.mindscapehq.com/products/lightspeed). Checkout the [GitHub Project](https://github.com/hhandoko/ServiceStack.Authentication.LightSpeed) for more info.

### Other Framework Features

 - Added support for locking users in all AuthProviders by populating `UserAuth.LockedDate`, effective from next login attempt
 - Reduced dependencies on all Logging providers, now only depends on `ServiceStack.Interfaces`
 - ContentLength is written where possible allowing [Async Progress callbacks on new payloads](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/AsyncProgressTests.cs)
 - Non authenticated requests to `/auth` throw a 401 (otherwise returns basic session info)
 - Metadata filter now supports IE8/IE9
 - `CopyTo` and `WriteTo` Stream extensions now return bytes transferred 

# v4.0.21 Release Notes

## Authentication

### Windows Auth Provider for ASP.NET

An ASP.NET WindowsAuth Provider preview is available. This essentially wraps the existing Windows Auth support baked into ASP.NET and adds an adapter for [ServiceStack's Multi-Provider Authentication model](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization).

It can be registered just like any other Auth Provider, i.e. in the AuthFeature plugin:

```csharp
Plugins.Add(new AuthFeature(
    () => new CustomUserSession(), 
    new IAuthProvider[] {
        new AspNetWindowsAuthProvider(this) { AllowAllWindowsAuthUsers = true }, 
    }
));
```

By default it only allows access to users in `AspNetWindowsAuthProvider.LimitAccessToRoles`, but can be overridden with `AllowAllWindowsAuthUsers=true` to allow access to all Windows Auth users as seen in the example above. 

Credentials can be attached to ServiceStack's Service Clients the same way [as .NET WebRequest's](http://stackoverflow.com/a/3563033/85785) by assingning the `Credentials` property, e.g:

```csharp
var client = new JsonServiceClient(BaseUri) {
    Credentials = CredentialCache.DefaultCredentials,
};

var response = client.Get(new RequiresAuth { Name = "Haz Access!" });
```

To help with debugging, [?debug=requestinfo](https://github.com/ServiceStack/ServiceStack/wiki/Debugging#request-info) has been extended to include the Request's current Logon User info:

![WindowsAuth DebugInfo](https://github.com/ServiceStack/Assets/raw/master/img/release-notes/debuginfo-windowsauth.png)

> We're interested in hearing future use-cases this can support, feedback on this and future integration with Windows Auth are welcomed on [the Active Directory Integration feature request](http://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/4725924-built-in-active-directory-authentication-suport).

### New GitHub and other OAuth Providers available

Thanks to [Rouslan Grabar](https://github.com/iamruss) we now have a number of new OAuth providers built into ServiceStack, including authentication with GitHub, Russia's most popular search engine [Yandex](http://www.yandex.ru/) and Europe's largest Social Networks after Facebook, [VK](http://vk.com) and [Odnoklassniki](http://odnoklassniki.ru/):

```csharp
Plugins.Add(new AuthFeature(
    () => new CustomUserSession(), 
    new IAuthProvider[] {
        new GithubAuthProvider(appSettings), 
        new YandexAuthProvider(appSettings), 
        new VkAuthProvider(appSettings), 
        new OdnoklassnikiAuthProvider(appSettings), 
    }
));
```

### Extended Auth DTO's

You can now test whether a user is authenticated by calling the Auth Service without any parameters, e.g. `/auth` which will return summary auth info of the currently authenticated user or a `401` if the user is not authenticated. A `DisplayName` property was added to `AuthenticateResponse` to return a friendly name of the currently authenticated user.

## [Portable ServiceStack](https://github.com/ServiceStack/ServiceStack.Gap)

A new [ServiceStack.Gap](https://github.com/ServiceStack/ServiceStack.Gap) Repository and NuGet package was added to help with creating ServiceStack-powered Desktop applications.

ServiceStack has a number of features that's particularly well-suited for these kind of apps:

 - It allows your services to be self-hosted using .NET's HTTP Listener
 - It supports pre-compiled Razor Views
 - It supports Embedded resources
 - It supports an embedded database in Sqlite and OrmLite
 - It can be ILMerged into a single .exe

Combined together this allows you to encapsulate your ServiceStack application into a single cross-platform .exe that can run on Windows or OSX.

To illustrate the potential of embedded ServiceStack solutions, a portable version [httpbenchmarks.servicestack.net](https://httpbenchmarks.servicestack.net) was created targetting a number of platforms below:

> **[BenchmarksAnalyzer.zip](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/BenchmarksAnalyzer.zip)** - Single .exe that opens the BenchmarksAnalyzer app in the users browser

[![Partial Console Screenshot](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/gap/partial-exe.png)](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/BenchmarksAnalyzer.zip)

> **[BenchmarksAnalyzer.Mac.zip](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/BenchmarksAnalyzer.Mac.zip)** - Self-hosted app running inside a OSX Cocoa App Web Browser

[![Partial OSX Screenshot](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/gap/partial-osx.png)](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/BenchmarksAnalyzer.Mac.zip)

> **[BenchmarksAnalyzer.Windows.zip](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/BenchmarksAnalyzer.Windows.zip)** - Self-hosted app running inside a Native WinForms app inside [CEF](https://code.google.com/p/chromiumembedded/)

[![Partial Windows Screenshot](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/gap/partial-win.png)](https://github.com/ServiceStack/ServiceStack.Gap/raw/master/deploy/BenchmarksAnalyzer.Windows.zip)

### Usage

By default `BenchmarksAnalyzer.exe` will scan the directory where it's run from, it also supports being called with the path to `.txt` or `.zip` files to view or even a directory where output files are located. Given this there are a few popular ways to use Benchmarks Analyzer:

 - Drop `BenchmarksAnalyzer.exe` into a directory of benchmark outputs before running it
 - Drop a `.zip` or folder onto the `BenchmarksAnalyzer.exe` to view those results

> Note: It can also be specified as a command-line argument, e.g: "BenchmarksAnalyzer.exe path\to\outputs"

![Benchmarks Analyzer Usage](https://github.com/ServiceStack/Assets/raw/master/img/gap/benchmarksanalyzer-usage.gif)

### ServiceStack.Gap Developer Guide

The guides on how each application was created is on [ServiceStack.Gap](https://github.com/ServiceStack/ServiceStack.Gap) site, i.e:

 - [Self-Hosting Console App](https://github.com/ServiceStack/ServiceStack.Gap#self-hosting-console-app)
 - [Windows Forms App with Chromium Embedded Framework and CefSharp](https://github.com/ServiceStack/ServiceStack.Gap#winforms-with-chromium-embedded-framework)
 - [Mac OSX Cocoa App with Xmarain.Mac](https://github.com/ServiceStack/ServiceStack.Gap#mac-osx-cocoa-app-with-xmarainmac)

## Other Framework Features

### Filtering support added to Metadata pages

You can now filter services on ServiceStack's `/metadata` page:

![Metadata Filter](https://github.com/ServiceStack/Assets/raw/master/img/release-notes/metadata-filter.png)

### Typed Request Filters

A more typed API to register Global Request and Response filters per Request DTO Type are available under the `RegisterTyped*` API's in AppHost. These can be used to provide more flexibility in multi-tenant solutions by attaching custom data on incoming requests, e.g:

```csharp
public override void Configure(Container container)
{
    RegisterTypedRequestFilter<Resource>((req, res, dto) =>
    {
        var route = req.GetRoute();
        if (route != null && route.Path == "/tenant/{TenantName}/resource")
        {
            dto.SubResourceName = "CustomResource";
        }
    });
}
```

Typed Filters can also be used to apply custom behavior on Request DTO's sharing a common interface, e.g:

```csharp
public override void Configure(Container container)
{
    RegisterTypedRequestFilter<IHasSharedProperty>((req, res, dtoInterface) => {
        dtoInterface.SharedProperty = "Is Shared";    
    });
}
```

### Buffered Stream option has now added to Response 

Response streams can be buffered in the same way as you can buffer Request streams by setting `UseBufferedStream=true`, e.g:

```csharp
appHost.PreRequestFilters.Add((httpReq, httpRes) => {
    httpReq.UseBufferedStream = true;
    httpRes.UseBufferedStream = true;    
});

```

### AfterInitCallbacks added to AppHost

You can register callbacks to add custom logic straight after the AppHost has finished initializing. E.g. you can find all Roles specified in `[RequiredRole]` attributes with:

```csharp
appHost.AfterInitCallbacks.Add(host =>
{
    var allRoleNames = host.Metadata.OperationsMap
        .SelectMany(x => x.Key.AllAttributes<RequiredRoleAttribute>()
            .Concat(x.Value.ServiceType.AllAttributes<RequiredRoleAttribute>()))
        .SelectMany(x => x.RequiredRoles);
});
```

### Request Scopes can be configured to use ThreadStatic

Request Scoped dependencies are stored in `HttpRequest.Items` for ASP.NET hosts and uses Remoting's `CallContext.LogicalData` API's in self-hosts. Using the Remoting API's can be problematic in old versions of Mono or when executed in test runners.

If this is an issue the RequestContext can be configured to use ThreadStatic with:

```csharp
RequestContext.UseThreadStatic = true;
```

### Logging

Updated Logging providers to allow `debugEnabled` in their LogFactory constructor, e.g:

```csharp
LogFactory.LogManager = new NullLogFactory(debugEnabled:false);
LogFactory.LogManager = new ConsoleLogFactory(debugEnabled:true);
LogFactory.LogManager = new DebugLogFactory(debugEnabled:true);
```

Detailed command logging is now enabled in OrmLite and Redis when `debugEnabled=true`. The external Logging provider NuGet packages have also been updated to use their latest version.

### Razor

 - Enabled support for Razor `@helpers` and `@functions` in Razor Views
 - Direct access to Razor Views in `/Views` is now denied by default

### Service Clients

 - Change Silverlight to auto emulate HTTP Verbs for non GET or POST requests
 - Shorter aliases added on `PostFileWithRequest` which uses the Request DTO's auto-generated url
 - The [PCL version of ServiceStack.Interfaces](https://github.com/ServiceStackApps/HelloMobile) now supports a min version of .NET 4.0

## OrmLite

### Exec and Result Filters

A new `CaptureSqlFilter` Results Filter has been added which shows some of the power of OrmLite's Result filters by being able to capture SQL Statements without running them, e.g:

```csharp
public class CaptureSqlFilter : OrmLiteResultsFilter
{
    public CaptureSqlFilter()
    {
        SqlFilter = CaptureSql;
        SqlStatements = new List<string>();
    }

    private void CaptureSql(string sql)
    {
        SqlStatements.Add(sql);
    }

    public List<string> SqlStatements { get; set; }
}
```

This can then be wrapped around existing database calls to capture and print the generated SQL, e.g:

```csharp
using (var captured = new CaptureSqlFilter())
using (var db = OpenDbConnection())
{
    db.CreateTable<Person>();
    db.Count<Person>(x => x.Age < 50);
    db.Insert(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix" });
    db.Delete<Person>(new { FirstName = "Jimi", Age = 27 });

    var sql = string.Join(";\n", captured.SqlStatements.ToArray());
    sql.Print();
}
```

#### Exec filters can be limited to specific Dialect Providers

```csharp
OrmLiteConfig.DialectProvider.ExecFilter = execFilter;
```

### OrmLite's custom SqlBuilders now implement ISqlExpression

OrmLite provides good support in integrating with external or custom SQL builders that implement OrmLite's simple `ISqlExpression` interface which can be passed directly to `db.Select()` API. This has now been added to OrmLite's other built-in SQL Builders, e.g:

#### Using JoinSqlBuilder

```csharp
var joinQuery = new JoinSqlBuilder<User, User>()
    .LeftJoin<User, Address>(x => x.Id, x => x.UserId, 
        sourceWhere: x => x.Age > 18, 
        destinationWhere: x => x.Country == "Italy");

var results = db.Select<User>(joinQuery);
```

#### Using SqlBuilder

```csharp
var tmpl = sb.AddTemplate(
    "SELECT * FROM User u INNER JOIN Address a on a.UserId = u.Id /**where**/");
sb.Where("Age > @age", new { age = 18 });
sb.Where("Countryalias = @country", new { country = "Italy" });

var results = db.Select<User>(tmpl, tmpl.Parameters);
```

### Other Changes

 - OrmLite can create tables with any numeric type in all providers. Fallbacks were added on ADO.NET providers that don't support the numeric type natively
 - Load/Save Reference property conventions can be [inferred on either aliases or C# property names](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/LoadReferencesTests.cs#L207)
 - OrmLite can create tables from types with Indexers
 - Can use `OrmLiteConfig.StripUpperInLike=true` to [remove use of upper() in Sql Expressions](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/Expression/SelectExpressionTests.cs#L205)

## Redis

A new `TrackingRedisClientsManager` client manager has been added by [Thomas James](https://github.com/tvjames) to help diagnose Apps that are leaking redis connections. 

# v4.0.19 Release Notes

## Embedded ServiceStack

This release has put all the final touches together to open up interesting new use-cases for deploying ServiceStack solutions into a single self-contained, cross-platform, xcopy-able executable.

By leveraging ServiceStack's support for [self-hosting](https://github.com/ServiceStack/ServiceStack/wiki/Self-hosting), the [Virtual File System](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system) support for Embedded Resources and the new support for [Compiled Razor Views](#compiled-razor-views), we can embed all images/js/css Razor views and Markdown Razor assets into a single dll that can be ILMerged with the preferred ServiceStack dependencies (inc. OrmLite.Sqlite) into a single cross-platform .NET exe:

### Razor Rockstars - Embedded Edition

To showcase its potential we've compiled the entire [Razor Rockstars](http://razor.servicestack.net/) website into a [single dll](https://github.com/ServiceStack/RazorRockstars/tree/master/src/RazorRockstars.CompiledViews) that's referenced them in the multiple use-case scenarios below:

> Note: all demo apps are unsigned so will require ignoring security warnings to run.

### As a Single Self-Hosted .exe

The examples below merges Razor Rockstars and ServiceStack into a Single, cross-platform, self-hosting Console App, that opens up Razor Rockstars homepage in the users default web browser when launched:

> [RazorRockstars.exe](https://github.com/ServiceStack/RazorRockstars/raw/master/build/RazorRockstars.exe) - Self-Host running in a Console App

> [WindowlessRockstars.exe](https://github.com/ServiceStack/RazorRockstars/raw/master/build/WindowlessRockstars.exe) - Headless Self-Hosted Console App running in the background

[![SelfHost](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/self-host.png)](https://github.com/ServiceStack/RazorRockstars/raw/master/build/RazorRockstars.exe)

> The total size for the entire  uncompressed **RazorRockstars.exe** ServiceStack website comes down to just **4.8MB** (lighter than the 5MB footprint of EntityFramework.dll) that includes **1.5MB** for RazorRockstars html/img/js/css website assets and **630kb** for native Windows sqlite3.dll.

### Running inside Windows and OSX Native Desktop Apps

You can also achieve a [PhoneGap-like experience](http://phonegap.com/) by hosting ServiceStack inside native .NET Desktop App shells for OSX and Windows:

> [RazorRockstars.MacHost.app](https://github.com/ServiceStack/RazorRockstars/raw/master/build/RazorRockstars.MacHost.app.zip) - Running inside a Desktop Cocoa OSX app using [Xamarin.Mac](https://xamarin.com/mac)

[![OSX Cocoa App](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/osx-host.png)](https://github.com/ServiceStack/RazorRockstars/raw/master/build/RazorRockstars.MacHost.app.zip)

> [WpfHost.zip](https://github.com/ServiceStack/RazorRockstars/raw/master/build/WpfHost.zip) - Running inside a WPF Desktop app

[![WPF App](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/wpf-host.png)](https://github.com/ServiceStack/RazorRockstars/raw/master/build/WpfHost.zip)

Surprisingly .NET Desktop apps built with [Xamarin.Mac on OSX](https://xamarin.com/mac) using Cocoa's WebKit-based WebView widget provides a superior experience over WPF's built-in WebBrowser widget which renders in an old behind-the-times version of IE. To improve the experience on Windows we're exploring better experiences on Windows by researching options around the [Chromium Embedded Framework](https://code.google.com/p/chromiumembedded/) and the existing managed .NET wrappers: [CefGlue](http://xilium.bitbucket.org/cefglue/) and [CefSharp](https://github.com/cefsharp/CefSharp).

**Xamarin.Mac** can deliver an even better end-user experience by bundling the Mono runtime with the app avoiding the need for users to have Mono runtime installed. Incidentally this is the same approach used to deploy .NET OSX apps to the [Mac AppStore](http://www.apple.com/osx/apps/app-store.html).

### Standard Web Hosts

As the only differences when using the embedded .dll is that it embeds all img/js/css/etc assets as embedded resources and makes use of compiled razor views, it can also be used in standard web hosts configurations which are effectively just lightweight wrappers containing the App configuration and references to external dependencies:

  - [CompiledViews in SelfHost](https://github.com/ServiceStack/RazorRockstars/tree/master/src/RazorRockstars.CompiledViews.SelfHost)
  - [CompiledViews in ASP.NET Web Host](https://github.com/ServiceStack/RazorRockstars/tree/master/src/RazorRockstars.CompiledViews.WebHost)

Benefits of Web Hosts referencing embedded dlls include easier updates by being able to update a websites core functionality by copying over a single **.dll** as well as improved performance for Razor views by eliminating Razor compile times.

### ILMerging

Creating the single **RazorRockstars.exe** is simply a matter of [ILMerging all the self-host project dlls](https://github.com/ServiceStack/RazorRockstars/blob/master/build/ilmerge.bat) into a single executable. 

There are only a couple of issues that need to be addressed when running in a single ILMerged .exe:

Assembly names are merged together so all registration of assemblies in `Config.EmbeddedResourceSources` end up referencing the same assembly which results in only serving embedded resources in the host assembly namespace. To workaround this behavior we've added a more specific way to reference assemblies in `Config.EmbeddedResourceBaseTypes`, e.g:

 ```csharp
 SetConfig(new HostConfig {
    DebugMode = true,
    EmbeddedResourceBaseTypes = { GetType(), typeof(BaseTypeMarker) },
});
```

Where `BaseTypeMarker` is just a dummy class that sits on the base namespace of the class library that's used to preserve the Assembly namespace.

The other limitation is not being able to merge unmanaged .dll's, which is what's needed for RazorRockstars as it makes use of the native `sqlite3.dll`. An easy workaround for this is to make `sqlite3.dll` an embedded resource then simply write it out to the current directory where OrmLite.Sqlite can find it when it first makes an sqlite connection, e.g:

 ```csharp
public static void ExportWindowsSqliteDll()
{
    if (Env.IsMono)
        return; //Uses system sqlite3.so or sqlite3.dylib on Linux/OSX

    var resPath = "{0}.sqlite3.dll".Fmt(typeof(AppHost).Namespace);

    var resInfo = typeof(AppHost).Assembly.GetManifestResourceInfo(resPath);
    if (resInfo == null)
        throw new Exception("Couldn't load sqlite3.dll");

    var dllBytes = typeof(AppHost).Assembly.GetManifestResourceStream(resPath).ReadFully();
    var dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    var filePath = Path.Combine(dirPath, "sqlite3.dll");

    File.WriteAllBytes(filePath, dllBytes);
}
```

This isn't required for Mono as it's able to make use of the preinstalled version of sqlite on OSX and Linux platforms.

### Compiled Razor Views

Support for Compiled Razor Views has landed in ServiceStack thanks to the efforts of [Carl Healy](https://github.com/Tyst).

The primary benefits of compiled views is improved performance by eliminating compile times of Razor views. They also provide static compilation benefits by highlighting compile errors during development and also save you from deploying multiple `*.cshtml` files with your app since they all end up pre-compiled in your Assembly. 

Enabling compiled views is fairly transparent where you only need to install the new [Razor.BuildTask NuGet Package](https://www.nuget.org/packages/ServiceStack.Razor.BuildTask/) to the project containing your `.cshtml` Razor Views you want to compile:

    PM> Install-Package ServiceStack.Razor.BuildTask

This doesn't add any additional dlls to your project, instead it just sets the **BuildAction** to all `*.cshtml` pages to `Content` and adds an MSBuild task to your project file to pre-compile razor views on every build.

Then to register assemblies containing compiled razor views with Razor Format you just need to add it to `RazorFormat.LoadFromAssemblies`, e.g:

```csharp
Plugins.Add(new RazorFormat {
    LoadFromAssemblies = { typeof(RockstarsService).Assembly }
});
```

The Compiled Views support continues to keep a great development experience in [DebugMode](https://github.com/ServiceStack/ServiceStack/wiki/Debugging#debugmode) where all Razor Views are initially loaded from the Assembly but then continues to monitor the file system for modified views, automatically compiling and loading them on the fly.

## [Postman Support](http://www.getpostman.com/)

We've added great support for the very popular [Postman Rest Client](http://www.getpostman.com/) in this release which is easily enabled by just registering the plugins below:

```csharp
Plugins.Add(new PostmanFeature());
Plugins.Add(new CorsFeature());
```

> As it makes cross-site requests, Postman also requires CORS support. 

Once enabled, a link with appear in your metadata page:

![Postman Metadata link](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/postman-metadata.png)

Which by default is a link to `/postman` route that returns a JSON postman collection that can be imported into postman by clicking on **import collections** icon at the top:

![Postman Screenshot](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/postman.png)

Once imported it will populate a list of available routes which you can select and easily call from the Postman UI. Just like the [Swagger Support](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API) the list of operations returned respects the [Restriction Attributes](https://github.com/ServiceStack/ServiceStack/wiki/Restricting-Services) and only shows the operations each user is allowed to see.

The above screenshot shows how to call the `SearchRockstars` Route `/rockstars/{Id}` which returns the rockstar with the matching id.

The screenshot above also illustrates some of the customization that's available with the [Email Contacts](https://github.com/ServiceStack/EmailContacts/) metadata imported with the default settings and the Razor Rockstars metadata imported with a customized label: 

    /postman?label=type,+,route

The `label` param accepts a collection of string tokens that controls how the label is formatted.The `type` and `route` are special tokens that get replaced by the **Request DTO name** and **Route** respectively. Everything else are just added string literals including the `+` character which is just a url-encoded version of ` ` space character.

Here are some examples using the example definition below:

```csharp
[Route("/contacts/{Id}")]
public class GetContact { ... }
```

<table>
<tr>
    <td><b>/postman?label=type</b></td>
    <td>GetContact</td>
</tr>
<tr>
    <td><b>/postman?label=route</b></td>
    <td>/contacts/{Id}</td>
</tr>
<tr>
    <td><b>/postman?label=type:english</b></td>
    <td>Get contact</td>
</tr>
<tr>
    <td><b>/postman?label=type:english,+(,route,)</b></td>
    <td>Get contact (/contacts/{Id})</td>
</tr>
</table>

The default label format can also be configured when registering the Postman plugin, e.g:

```csharp
Plugins.Add(new PostmanFeature { 
    DefaultLabelFmt = new List<string> { "type:english", " ", "route" }
});
```

### Support for authenticated requests

We've also made it easy to call authentication-only services with the `/postman?exportSession=true` parameter which will redirect to a url that captures your session cookies into a deep-linkable url like `/postman?ssopt=temp&ssid={key}&sspid={key}` that can be copied into Postman.

This lets you replace your session cookies with the session ids on the url, effectively allowing you to take over someone elses session, in this case telling Postman to make requests on your behalf using your authenticated session cookies. 

As this functionality is potentially dangerous it's only enabled by default in **DebugMode** but can be overridden with:

```csharp
Plugins.Add(new PostmanFeature { 
    EnableSessionExport = true
});
```

### Other Customizations

Other options include hosting postman on an alternate path, adding custom HTTP Headers for each Postman request and providing friendly aliases for Request DTO Property Types that you want to appear to external users, in this case we can show `DateTime` types as `Date` in Postmans UI:

```csharp
Plugins.Add(new PostmanFeature { 
    AtRestPath = "/alt-postman-link",
    Headers = "X-Custom-Header: Value\nXCustom2: Value2",
    FriendlyTypeNames = { {"DateTime", "Date"} },
});
```

## [Cascading layout templates](http://razor.servicestack.net/#no-ceremony)

Support for [Cascading layout templates](http://razor.servicestack.net/#no-ceremony) for Razor ViewPages inside `/Views` were added in this release by [@Its-Tyson](https://github.com/Its-Tyson). 

This works the same intuitive way it does for external Razor Content pages where the `_Layout.cshtml` nearest to the selected View will be used by default, e.g: 

    /Views/_Layout.cshtml
    /Views/Public.cshtml
    /Views/Admin/_Layout.cshtml
    /Views/Admin/Dashboard.cshtml

Where `/Views/Admin/Dashboard.cshtml` by default uses the `/Views/Admin/_Layout.cshtml` template.

## Async APIs added to HTTP Utils 

The following Async versions of [HTTP Utils](https://github.com/ServiceStack/ServiceStack/wiki/Http-Utils) have been added to ServiceStack.Text by [Kyle Gobel](https://github.com/KyleGobel):

```csharp
Task<string> GetStringFromUrlAsync(...)
Task<string> PostStringToUrlAsync(...)
Task<string> PostToUrlAsync(...)
Task<string> PostJsonToUrlAsync(...)
Task<string> PostXmlToUrlAsync(...)
Task<string> PutStringToUrlAsync(...)
Task<string> PutToUrlAsync(...)
Task<string> PutJsonToUrlAsync(...)
Task<string> PutXmlToUrlAsync(...)
Task<string> DeleteFromUrlAsync(...)
Task<string> OptionsFromUrlAsync(...)
Task<string> HeadFromUrlAsync(...)
Task<string> SendStringToUrlAsync(...)
```

## Redis

The latest [stable release of redis-server](http://download.redis.io/redis-stable/00-RELEASENOTES) includes support for the new [ZRANGEBYLEX](http://redis.io/commands/zrangebylex) sorted set operations allowing you to query a sorted set lexically. A good showcase for this is available on [autocomplete.redis.io](http://autocomplete.redis.io/) that shows a demo querying all 8 millions of unique lines of the Linux kernel source code in a fraction of a second.

These new operations are available as a 1:1 mapping with redis-server on IRedisNativeClient:

```csharp
public interface IRedisNativeClient
{
    ...
    byte[][] ZRangeByLex(string setId, string min, string max, int? skip = null, int? take = null);
    long ZLexCount(string setId, string min, string max);
    long ZRemRangeByLex(string setId, string min, string max);
}
```

As well as under more user-friendly APIs under IRedisClient:

```csharp
public interface IRedisClient
{
    ...
    List<string> SearchSortedSet(string setId, string start=null, string end=null, int? skip=null, int? take=null);
    long SearchSortedSetCount(string setId, string start=null, string end=null);
    long RemoveRangeFromSortedSetBySearch(string setId, string start=null, string end=null);
}
```

Just like NuGet version matchers, Redis uses `[` char to express inclusiveness and `(` char for exclusiveness.
Since the `IRedisClient` APIs defaults to inclusive searches, these two APIs are the same:

```csharp
Redis.SearchSortedSetCount("zset", "a", "c")
Redis.SearchSortedSetCount("zset", "[a", "[c")
```

Alternatively you can specify one or both bounds to be exclusive by using the `(` prefix, e.g:

```csharp
Redis.SearchSortedSetCount("zset", "a", "(c")
Redis.SearchSortedSetCount("zset", "(a", "(c")
```

More API examples are available in [LexTests.cs](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/tests/ServiceStack.Redis.Tests/LexTests.cs).

### Twemproxy support

This release also includes better support for [twemproxy](https://github.com/twitter/twemproxy), working around missing server commands sent upon connection.

## OrmLite

New support for StringFilter allowing you apply custom filter on string values, e.g [remove trailing whitespace](http://stackoverflow.com/a/23261868/85785):

```csharp
OrmLiteConfig.StringFilter = s => s.TrimEnd();

db.Insert(new Poco { Name = "Value with trailing   " });
Assert.That(db.Select<Poco>().First().Name, Is.EqualTo("Value with trailing"));
```

Added implicit support for [escaping wildcards in typed expressions](http://stackoverflow.com/a/23435975/85785) that make use of LIKE, namely `StartsWith`, `EndsWith` and `Contains`, e.g:

```csharp
db.Insert(new Poco { Name = "ab" });
db.Insert(new Poco { Name = "a%" });
db.Insert(new Poco { Name = "a%b" });

db.Count<Poco>(q => q.Name.StartsWith("a_")); //0
db.Count<Poco>(q => q.Name.StartsWith("a%")); //2
```

OrmLite also underwent some internal refactoring to remove duplicate code and re-use existing code-paths.

### Other Features

 - Allow overriding of `HttpListenerBase.CreateRequest()` for controlling creation of Self-Hosting requests allowing you to force a [Character encoding to override the built-in heuristics](http://stackoverflow.com/a/23381383/85785) for detecting non UTF-8 character encodings
 - Support for retrieving untyped `base.UserSession` when inheriting from an untyped MVC `ServiceStackController` 
 - Added `@Html.RenderErrorIfAny()` to render a pretty bootstrap-styled exception response in a razor view
 - The generated WSDL output now replaces all occurances of `http://schemas.servicestack.net/types` with `Config.WsdlServiceNamespace` 
 - Initialize the CompressedResult Status code with the current HTTP ResponseStatus code
 - Plugins implementing `IPreInitPlugin` are now configured immediately after `AppHost.Configure()`
 - HttpListeners now unwrap async Aggregate exceptions containing only a Single Exception for better error reporting
 - HttpListeners now shares the same behavior as IIS for [redirecting requests for directories without a trailing slash](https://github.com/ServiceStack/ServiceStack/commit/a0a2857721656c7161fcd83eb07609ae4239ea2a)
 - [Debug Request Info](https://github.com/ServiceStack/ServiceStack/wiki/Debugging#request-info) now shows file listing of the configured VirtualPathProvider
 - Resource Virtual Directories are no longer case-sensitive 
 - Added new `Config.ExcludeAutoRegisteringServiceTypes` option to exclude services from being implicitly auto registered from assembly scanning. All built-in services in ServiceStack.dll now excluded by default which removes unintentional registration of services from ILMerging.

# New HTTP Benchmarks example project

[![HTTP Benchmarks](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/benchmarks-admin-ui.png)](https://httpbenchmarks.servicestack.net/)

Following the release of the [Email Contacts](https://github.com/ServiceStack/EmailContacts/) solution, a new documented ServiceStack example project allowing you to uploaded Apache HTTP Benchmarks to visualize and analyze their results has been released at: [github.com/ServiceStack/HttpBenchmarks](https://github.com/ServiceStack/HttpBenchmarks) and is hosted at [httpbenchmarks.servicestack.net](https://httpbenchmarks.servicestack.net/).

### Example Results

  - [Performance of different RDBMS in an ASP.NET Host](https://httpbenchmarks.servicestack.net/databases-in-asp-net)
  - [Performance of different ServiceStack Hosts](https://httpbenchmarks.servicestack.net/servicestack-hosts)

The documentation includes a development guide that walks through the projects different features:

 - Integration with `Glimpse` with support for `DotNetOpenAuth`
 - Allow authentication with Twitter, Facebook, Google and LinkedIn OAuth providers
 - Enables registration of new user accounts
 - Use of `[FallbackRoute]` attribute to allow users to create top-level routes (e.g. twitter.com/name) 
 - Explains why you want to aim for minimal JS dependencies
 - Introduction of **Really Simple MV Pattern** using plain JavaScript
 - Integration with multi-file Uploader `FineUploader`
 - Processes multiple file uploads including files in **.zip** packages using `DotNetZip`
 - Integration with `Highcharts.js`
 - Hosting differences of ASP.NET with AWS
 - Deploying to AWS and creating customized deployment packages with MSDeploy
 - Configuring SSL
 - Forcing SSL Redirects

The repository also includes benchmark scripts and host projects of [all ServiceStack HTTP Hosts](https://github.com/ServiceStack/HttpBenchmarks/tree/master/servers
), which all support runtime configuration of different RDBMS's: 

# v4.0.18 Release Notes

## New, much faster Self-Host!

Prior to this release ServiceStack had 2 self-hosting options with different [Concurrency Models](https://github.com/ServiceStack/ServiceStack/wiki/Concurrency-model):

- `AppHostHttpListenerBase` - Executes requests on the IO callback thread
- `AppHostHttpListenerPoolBase` - Executes requests on .NET's built-in ThreadPool

Where in typical scenarios (i.e. CPU intensive or blocking IO), executing on .NET's Thread Pool provides better performance.

This [Self-hosting performance analysis](http://en.rdebug.com/2013/05/06/servicestack-selfhosted-performance-boost/) from the ServiceStack community shows we're able to achieve even better performance by utilizing the excellent [Smart Thread Pool](http://www.codeproject.com/Articles/7933/Smart-Thread-Pool) instead, which is now available in the `AppHostHttpListenerSmartPoolBase` base class.

The new Smart Pool self-host routinely outperforms all other self hosting options, and does especially well in heavy IO scenarios as seen in the benchmarks below: 

<table>
    <thead>
        <tr>
            <th></th>
            <th>Self Host</th>
            <th>ASP.NET/IIS Express</th>
            <th>HttpListener Pool</th>
            <th>HttpListener</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <th>Database updates</th>
            <td>1x</td>
            <td>1.9x</td>
            <td>2x</td>
            <td>4.1x</td>
        </tr>
        <tr>
            <th>Single database query</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.5x</td>
            <td>2.6x</td>
        </tr>
        <tr>
            <th>Multiple database queries</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.4x</td>
            <td>2.6x</td>
        </tr>
        <tr>
            <th>Plaintext</th>
            <td>1x</td>
            <td>2.3x</td>
            <td>2.4x</td>
            <td>1.6x</td>
        </tr>
        <tr>
            <th>Fortunes Razor View</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.5x</td>
            <td>1.8x</td>
        </tr>
        <tr>
            <th>JSON serialization</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.4x</td>
            <td>1x</td>
        </tr>
    </tbody>
</table>

### Using different Self Host options

You can easily switch between the different self-hosting options by simply changing your AppHost's base class, e.g: 

```csharp
public class AppHost : AppHostHttpListenerBase { ... }
public class AppHost : AppHostHttpListenerPoolBase { ... }
public class AppHost : AppHostHttpListenerSmartPoolBase { ... }
```

Both the HttpListener Pool and SmartPool hosts have configurable pool sizes that can be tweaked to perform better under different scenarios.

### Optimal Self Hosted option

As the number of self-hosts grow, we've added a new `AppSelfHostBase` base class that represents an alias for the highest performing self-hosting option with an optimal configuration that we'll continue to tune for performance against typical scenarios. Unless you've identified specific configurations that performs better for your use-case, the recommendation is for new self-hosts to inherit this configuration:

```csharp
public class AppHost : AppSelfHostBase { ... }
```

## OrmLite 

OrmLite received a lot more attention this release with a number of value-added additions:

### Improved Oracle RDBMS provider

The OrmLite Oracle Provider has been significantly improved thanks to [Bruce Cowen](https://github.com/BruceCowan-AI) efforts who's brought the quality in-line with other RDBMS providers which now passes OrmLite's test suite. As part of this change, the Oracle Provider now depends on [Oracle's Data Provider for .NET](http://www.oracle.com/technetwork/topics/dotnet/index-085163.html) and can be installed with: 

    PM> Install-Package ServiceStack.OrmLite.Oracle
    PM> Install-Package ServiceStack.OrmLite.Oracle.Signed

More notes about the Oracle provider are maintained in the [OrmLite Release Notes](https://github.com/ServiceStack/ServiceStack.OrmLite/#oracle-provider-notes).

### Improved Typed SqlExpressions

The existing `db.SqlExpression<T>()` API has a more readable alias in:

```csharp
db.From<Table>();
```

Which now supports an optional SQL **FROM** fragment that can be used to specify table joins, e.g:

    var results = db.Select(db.From<Person>("Person INNER JOIN Band ON Person.Id = Band.PersonId"));

#### New ISqlExpression API

OrmLite API's have overloads to execute any SQL builders that implement the simple `ISqlExpression` API, i.e:

```csharp
public interface ISqlExpression
{
    string ToSelectStatement();
}
```

This allows for more readable code when using a decoupled Sql Builder, e.g:

```csharp
int over40s = db.Scalar<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age > 40));

List<string> lastNames = db.Column<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age == 27));

HashSet<int> uniqueAges = db.ColumnDistinct<int>(db.From<Person>().Select(x => x.Age).Where(q => q.Age < 50));

Dictionary<int,string> map = db.Dictionary<int,string>(db.From<Person>().Select(x => new {x.Id, x.LastName}));
```

#### Partial Selects

This also improves the APIs for partial SELECT queries, which originally required the use of custom SQL:

```csharp
var partialColumns = db.SelectFmt<SubsetOfShipper>(typeof(Shipper), "ShipperTypeId = {0}", 2);
```
    
But can now be expressed in any of the more typed examples below: 

```csharp
var partialColumns = db.Select<SubsetOfShipper>(db.From<Shipper>().Where(q => q.ShipperTypeId == 2));
```

Or partially populating the same POCO with only the columns specified:

```csharp
var partialColumns = db.Select<Shipper>(q => q.Select(x => new { x.Phone, x.CompanyName })
                                              .Where(x => x.ShipperTypeId == 2));

var partialColumns = db.Select<Shipper>(q => q.Select("Phone, CompanyName")
                                              .Where(x => x.ShipperTypeId == 2));
```

#### Nullable Limit APIs

The Limit API's now accept `int?` making it easier to apply paging in your ServiceStack services, e.g:

```csharp
public Request 
{
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

public List<Table> Any(Request request)
{
    return Db.Select(db.From<Table>.Limit(request.Skip, request.Take));
}
```

Which will only filter the results for the values provided. Aliases for `Skip()` and `Take()` are also available if LINQ naming is preferred.

#### New AliasNamingStrategy

A new alias naming strategy was added (in addition to `[Alias]` attribute) that lets you specify a dictionary of Table and Column aliases OrmLite should used instead, e.g:

```csharp
OrmLiteConfig.DialectProvider.NamingStrategy = new AliasNamingStrategy {
    TableAliases  = { { "MyTable", "TableAlias" } },
    ColumnAliases = { { "MyField", "ColumnAlias" } },
};
```

Which OrmLite will use instead, e.g when creating a table:

```csharp
db.CreateTable<MyTable>();
```

Aliases can also be referenced when creating custom SQL using the `SqlTable()` and `SqlColumn()` extension methods, e.g:

```csharp
var result = db.SqlList<MyTable>(
    "SELECT * FROM {0} WHERE {1} = {2}".Fmt(
        "MyTable".SqlTable(),
        "MyField".SqlColumn(), "foo".SqlValue()));
```

#### New Exists APIs

Nicer if you just need to check for existence, instead of retrieving a full result-set e.g:

```csharp
bool hasUnder50s = db.Exists<Person>(x => x.Age < 50);
bool hasUnder50s = db.Exists(db.From<Person>().Where(x => x.Age < 50));
```

## Redis

### New Scan APIs Added

Redis v2.8 introduced a beautiful new [SCAN](http://redis.io/commands/scan) operation that provides an optimal strategy for traversing a redis instance entire keyset in managable-size chunks utilizing only a client-side cursor and without introducing any server state. It's a higher performance alternative and should be used instead of [KEYS](http://redis.io/commands/keys) in application code. SCAN and its related operations for traversing members of Sets, Sorted Sets and Hashes are now available in the Redis Client in the following API's:

```csharp
public interface IRedisClient
{
    ...
    IEnumerable<string> ScanAllKeys(string pattern = null, int pageSize = 1000);
    IEnumerable<string> ScanAllSetItems(string setId, string pattern = null, int pageSize = 1000);
    IEnumerable<KeyValuePair<string, double>> ScanAllSortedSetItems(string setId, string pattern = null, int pageSize = 1000);
    IEnumerable<KeyValuePair<string, string>> ScanAllHashEntries(string hashId, string pattern = null, int pageSize = 1000);    
}

//Low-level API
public interface IRedisNativeClient
{
    ...
    ScanResult Scan(ulong cursor, int count = 10, string match = null);
    ScanResult SScan(string setId, ulong cursor, int count = 10, string match = null);
    ScanResult ZScan(string setId, ulong cursor, int count = 10, string match = null);
    ScanResult HScan(string hashId, ulong cursor, int count = 10, string match = null);
}
```

The `IRedisClient` provides a higher-level API that abstracts away the client cursor to expose a lazy Enumerable sequence to provide an optimal way to stream scanned results that integrates nicely with LINQ, e.g:

```csharp
var scanUsers = Redis.ScanAllKeys("urn:User:*");
var sampleUsers = scanUsers.Take(10000).ToList(); //Stop after retrieving 10000 user keys 
```

### New HyperLog API

The development branch of Redis server (available when v3.0 is released) includes an ingenious algorithm to approximate the unique elements in a set with maximum space and time efficiency. For details about how it works see Redis's creator Salvatore's blog who [explains it in great detail](http://antirez.com/news/75). Essentially it lets you maintain an efficient way to count and merge unique elements in a set without having to store its elements. 
A Simple example of it in action:

```csharp
redis.AddToHyperLog("set1", "a", "b", "c");
redis.AddToHyperLog("set1", "c", "d");
var count = redis.CountHyperLog("set1"); //4

redis.AddToHyperLog("set2", "c", "d", "e", "f");

redis.MergeHyperLogs("mergedset", "set1", "set2");

var mergeCount = redis.CountHyperLog("mergedset"); //6
```

## HTTP and MQ Service Clients

### Substitutable OneWay MQ and HTTP Service Clients

Service Clients and MQ Clients have become a lot more interoperable where all MQ Clients now implement the Service Clients `IOneWayClient` API which enables writing code that works with both HTTP and MQ Clients:

```csharp
IOneWayClient client = GetClient();
client.SendOneWay(new RequestDto { ... });
```

Likewise the HTTP Service Clients implement the Messaging API `IMessageProducer`:

```csharp
void Publish<T>(T requestDto);
void Publish<T>(IMessage<T> message);
```

When publishing a `IMessage<T>` the message metadata are sent as HTTP Headers with an `X-` prefix.

### UploadProgress added on Service Clients

Which works similar to [OnDownloadProgress](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/AsyncProgressTests.cs) where you can specify a callback to provide UX Progress updates, e.g:

```csharp
client.OnUploadProgress = (bytesWritten, total) => "Written {0}/{1} bytes...".Print(bytesWritten, total);

client.PostFileWithRequest<UploadResponse>(url, new FileInfo(path), new Upload { CreatedBy = "Me" });
```

## Razor Support

Our support for [No Ceremony Razor pages](https://github.com/ServiceStack/EmailContacts/#the-no-ceremony-option---dynamic-pages-without-controllers) has been very well received which has all but alleviated the need of requiring services / controllers for dynamic html pages. One of the areas where a Service may be required is for execution any custom request filters, which we've now added support for by letting you choose to execute all request filters for a specific Request with: 

```csharp
@{
    ApplyRequestFilters(new RequestDto());
}
```

This will execute all the Request Filters applied to the specified Request DTO. Any one of the filters ends the request (e.g. with a redirect) and the rest of the Razor page will stop execution.

Likewise it's possible to redirect from within Razor with:

```csharp
@{
    if (!IsAuthenticated) {
        Response.RedirectToUrl("/login");
        throw new StopExecutionException();
    }
}
```
An alternative to `StopExecutionException` is to have an explicit `return;`, the difference being that it will continue to execute the remainder of the page, although neither approach will emit any Razor output to the response.

As redirecting non-authenticated users is a common use-case it's also available as a one-liner:

```csharp
@{
    RedirectIfNotAuthenticated();
}
```

Which if no url is specified it will redirect to the path configured on `AuthFeature.HtmlRedirect`.

### ss-utils.js

A few enhancements were added to ServiceStack's **/js/ss-utils.js** is ServiceStack's built-in JS library, first demonstrated in [Email Contacts solution](https://github.com/ServiceStack/EmailContacts/#servicestack-javascript-utils---jsss-utilsjs):

Declarative event handlers can send multiple arguments:

```html
<ul>
    <li data-click="single">Foo</li>
    <li data-click="multiple:arg1,arg2">Bar</li>
</ul>
```

```javascript
$(document).bindHandlers({
    single: function(){
        var li = this;
    },
    multiple: function(arg1, arg2) {
        var li = this;
    }
});
```

Trigger client-side validation errors with `setFieldError()`:

```javascript
$("form").bindForm({
    validate: function(){
        var params = $(this).serializeMap();
        if (params.Password != params.Confirm){
            $(this).setFieldError('Password', 'Passwords to not match');
            return false;
        }
    }
});
```

Model binding now also populates `data-href` and `data-src` attributes e.g:

```html
<a data-href="FieldName"><img data-src="FieldName" /></a>
```

```javascript
$("form").applyValues({ FieldName: imgUrl });
```
## Other Changes

### Restriction attributes allowed on Services
    
Restriction attributes can be added on Service classes in addition to Request DTOs (which still take precedence).

```csharp
[Restrict(LocalhostOnly = true)]
public class LocalHostOnlyServices : Service { ... }
```

## AppSettings

### New OrmLiteAppSettings

Added new read/write AppSettings config option utilizing OrmLite as the back-end. 
This now lets you maintain your applications configuration in any [RDBMS back-end OrmLite supports](https://github.com/ServiceStack/ServiceStack.OrmLite/#download). It basically works like a mini Key/Value database in which can store any serializable value against any key which is maintained into the simple Id/Value `ConfigSettings` table.

#### Usage

Registration just uses an OrmLite DB Factory, e.g:

```csharp
container.Register(c => new OrmLiteAppSettings(c.Resolve<IDbConnectionFactory>()));
var appSettings = container.Resolve<OrmLiteAppSettings>();
appSettings.InitSchema(); //Create the ConfigSettings table if it doesn't exist
```

It then can be accessed like any [AppSetting APIs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Common.Tests/Configuration/AppSettingsTests.cs):

```csharp
//Read the `MyConfig` POCO stored at `config` otherwise use default value if it doesn't exist
MyConfig config = appSettings.Get("config", new MyConfig { Key = "DefaultValue" });
```

It also supports writing config values in addition to the AppSettings read-only API's, e.g:

```csharp
var latestStats = appSettings.GetOrCreate("stats", () => statsProvider.GetLatest());
```

### Extract key / value settings from text file

The new ParseKeyValueText extension method lets you extract key / value data from text, e.g: 

```csharp
var configText = @"
StringKey string value
IntKey 42
ListKey A,B,C,D,E
DictionaryKey A:1,B:2,C:3,D:4,E:5
PocoKey {Foo:Bar,Key:Value}";

Dictionary<string, string> configMap = configText.ParseKeyValueText(delimiter:" ");
```

When combined with the existing `DictionarySettings`, enables a rich, simple and clean alternative to .NET's App.config config section for reading structured configuration into clean data structures, e.g:

```csharp
IAppSettings appSettings = new DictionarySettings(configMap);

string value = appSettings.Get("StringKey");

int value = appSettings.Get("IntKey", defaultValue:1);

List<string> values = appSettings.GetList("ListKey");

Dictionary<string,string> valuesMap = appSettings.GetList("DictionaryKey");

MyConfig config = appSettings.Get("PocoKey", new MyConfig { Key = "DefaultValue"});
```

As we expect this to be a popular combination we've combined them into a single class that accepts a filePath, providing a simple alternative to custom Web.config configurations:

```csharp
var appSettings = new TextFileSettings("~/app.settings".MapHostAbsolutePath());
```

### PerfUtils

We've included the [C# Benchmark Utils](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Common/PerfUtils.cs) previously used in [Sudoku Benchmarks](https://github.com/dartist/sudoku_solver#benchmarks) originally inspired from [Dart's benchmark_harness](https://github.com/dart-lang/benchmark_harness). Unlike other benchmark utils, it runs for a specified period of time (2000ms by default) then returns the avg iteration time in microseconds. Here's an example usage comparing performance of maintaining a unique int collection between HashSet vs List:

```csharp
var rand = new Random();
var set = new HashSet<int>();
var avgMicroSecs = PerfUtils.Measure(
    () => set.Add(rand.Next(0, 1000)), runForMs:2000);

"HashSet: {0}us".Print(avgMicroSecs);

var list = new List<int>();
avgMicroSecs = PerfUtils.Measure(() => {
        int i = rand.Next(0, 1000);
        if (!list.Contains(i))
            list.Add(i);
    }, runForMs: 2000);

"List: {0}us".Print(avgMicroSecs);
```

### Minor Changes

- Numeric type mismatches between POCOs used in OrmLite and underlying RDBMS Tables are transparently coerced
- `Vary: Accept` is included in Global HTTP Headers to resolve browsers caching different Content-Type for the same url
- Razor configuration removes references to a specific version of ASP.NET Web Pages and adds `System` to default namespaces
- Swagger API emits an ApiVersion, configurable with `Config.ApiVersion` that defaults to "1.0"    
- Partials now render inside user-defined Razor sections
- Added `email.ToGravatarUrl()` extension method to retrieve avatar url from an email
- Replaced self-hosts use of ThreadStatics with CallContext to preserve Request scope in async requests
- Avoid runtime razor exceptions in Mono by not registering duplicate assemblies (i.e. from GAC) in RazorHost
- AppHostHttpListenerPoolBase self-host has a default pool size of `16 x Environment.ProcessorCount`
- ServiceStack's `IAppHost.CustomErrorHttpHandlers` can now override built-in HTTP Error handlers and fallback to generic error responses

### New Signed Projects

- [ServiceStack.ProtoBuf.Signed](https://www.nuget.org/packages/ServiceStack.ProtoBuf.Signed)

### Breaking Changes

- Moved `Config.GlobalHtmlErrorHttpHandler` to `IAppHost.GlobalHtmlErrorHttpHandler`


# v4.0.15 Release Notes

### Individual Products now available

In this release we've added the most requested "non-technical feature" by creating new licenses for [individual ServiceStack products](https://servicestack.net/#products) which provide
much better value when only using one of ServiceStack's stand-alone libraries on their own. 

New products available:

  - [servicestack.net/text](https://servicestack.net/text)
  - [servicestack.net/redis](https://servicestack.net/redis)
  - [servicestack.net/ormlite](https://servicestack.net/ormlite)

> Both OrmLite and Redis includes an implicit license for ServiceStack.Text

### ServiceStack

  - Upgraded ServiceStack's external dependencies to use latest version on NuGet
  - Modified [ServiceStack.RabbitMq](http://www.nuget.org/packages/ServiceStack.RabbitMq) to only depend on **ServiceStack** instead of **ServiceStack.Server**
  - Added optional `fieldName` property to ServiceClient [PostFileWithRequest](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/IRestClient.cs#L52-L55)
  - Changed exceptions in FileSystem scanning to be logged as warnings, fixes issues with NTFS symbolic links
  - Pass through Thread CurrentCulture when executing a sync request in a new Task
  - Added Evaluator.NamespaceAssemblies to specify alternate default namespace for Assemblies 
  - Changed to use OrdinalIgnoreCase instead of InvariantCultureIgnoreCase when possible

### OrmLite

#### OrmLite's core Exec functions are now overridable as a Filter

Continuing in efforts to make OrmLite more introspectable and configurable, OrmLite's core Exec functions 
[have been re-factored out into a substitutable Exec Filter](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/fa55404200f4a319eae3a298b648462dadafce5e).

This now makes it possible to inject a custom managed exec function where you can inject your own behavior, tracing, profiling, etc.

It comes in useful for situations when you want to use SqlServer in production but use an `in-memory` Sqlite database in tests and you want to emulate any missing SQL Server Stored Procedures in code:

```csharp
public class MockStoredProcExecFilter : OrmLiteExecFilter
{
    public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        try
        {
            return base.Exec(dbConn, filter);
        }
        catch (Exception ex)
        {
            if (dbConn.GetLastSql() == "exec sp_name @firstName, @age")
                return (T)(object)new Person { FirstName = "Mocked" };
            throw;
        }
    }
}

OrmLiteConfig.ExecFilter = new MockStoredProcExecFilter();

using (var db = OpenDbConnection())
{
    var person = db.SqlScalar<Person>("exec sp_name @firstName, @age",
        new { firstName = "aName", age = 1 });

    person.FirstName.Print(); //Mocked
}
```
Or if you want to do things like executing each operation multiple times, e.g:

```csharp
public class ReplayOrmLiteExecFilter : OrmLiteExecFilter
{
    public int ReplayTimes { get; set; }

    public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        var holdProvider = OrmLiteConfig.DialectProvider;
        var dbCmd = CreateCommand(dbConn);
        try
        {
            var ret = default(T);
            for (var i = 0; i < ReplayTimes; i++)
            {
                ret = filter(dbCmd);
            }
            return ret;
        }
        finally
        {
            DisposeCommand(dbCmd);
            OrmLiteConfig.DialectProvider = holdProvider;
        }
    }
}

OrmLiteConfig.ExecFilter = new ReplayOrmLiteExecFilter { ReplayTimes = 3 };

using (var db = OpenDbConnection())
{
    db.DropAndCreateTable<PocoTable>();
    db.Insert(new PocoTable { Name = "Multiplicity" });

    var rowsInserted = db.Count<PocoTable>(q => q.Name == "Multiplicity"); //3
}
```

#### Other improvements

  - Added [SqlVerifyFragment string extension](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/7f0711aa3368087037d8b7b84cf9f70f1ea2b191) to verify sql fragments where free-text is allowed in SqlExpression APIs  
  - Change MySql to create TimeSpan's column as INT to store ticks

### Redis

  - Add new Increment by double and long methods to Redis Client

### Text

  - Added [T.PopulateFromPropertiesWithoutAttribute](https://github.com/ServiceStack/ServiceStack.Text/commit/9bd0cc35c0a4e3ddcb7e6b6b88e760f45496145b) Auto Mapping method

### New Signed NuGet Packages

  - [ServiceStack.OrmLite.Sqlite.Windows.Signed](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite.Windows.Signed) 

# v4.0.12 Release Notes

## New [Email Contact Services](https://github.com/ServiceStack/EmailContacts/)

A new ServiceStack guidance is available detailing the recommended setup and physical layout structure of typical medium-sized ServiceStack projects.
It includes the complete documentation going through how to create the solution from scratch, and explains all the ServiceStack hidden features it makes use of along the way.

[![EmailContacts Screenshot](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/email-contacts.png)](https://github.com/ServiceStack/EmailContacts/)

[EmailContacts](https://github.com/ServiceStack/EmailContacts/) is a Single Page App built using just ServiceStack, 
jQuery and Bootstrap that showcases some of ServiceStack's built-in features, useful in the reducing the effort for 
developing medium-sized Web Applications.

The purpose of EmailContacts is to manage contacts (in [any RDBMS](https://github.com/ServiceStack/ServiceStack.OrmLite/#download)), 
provide a form to be able to send them messages and maintain a rolling history of any emails sent. 
The application also provides an option to have emails instead sent and processed via [Rabbit MQ](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ).

#### Functional Single Page App in under 130 Lines of HTML and 70 Lines JS

The entire EmailContacts UI is maintained in a single 
[default.cshtml](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/default.cshtml) 
requiring just 70 lines of JavaScript to render the dynamic UI, 
bind server validation errors and provide real-time UX feedback. 
The Application also follows an API-First development style where the Ajax UI calls only published APIs allowing 
all services to be immediately available, naturally, via an end-to-end typed API to Mobile and Desktop .NET clients.

### Example Projects

During this release all Example projects, Demos, Starter Templates, etc in the 
[ServiceStack.Example](https://github.com/ServiceStack/ServiceStack.Examples) and 
[ServiceStack.UseCases](https://github.com/ServiceStack/ServiceStack.UseCases/) 
master repositories were upgraded to ServiceStack v4. A new [ServiceStack + MVC5 project](https://github.com/ServiceStack/ServiceStack.UseCases/tree/master/Mvc5) 
was also added to UseCases, it just follows the instructions at [MVC Integration](https://github.com/ServiceStack/ServiceStack/wiki/Mvc-integration) wiki, but starts with an empty MVC5 project.

### Added new OrmLiteCacheClient

A new `OrmLiteCacheClient` [Caching Provider](https://github.com/ServiceStack/ServiceStack/wiki/Caching) 
was added to the **ServiceStack.Server** NuGet pacakge. 
This provides a lot of utility by supporting 
[OrmLite's RDBMS providers](https://github.com/ServiceStack/ServiceStack.OrmLite/#download) 
allowing utilization of existing RDBMS's as a distributed cache, potentially saving an infrastructure dependency.

Registration is simply:

```csharp 
//Register OrmLite Db Factory if not already
container.Register<IDbConnectionFactory>(c => 
    new OrmLiteConnectionFactory(connString, SqlServerDialect.Provider)); 

container.RegisterAs<OrmLiteCacheClient, ICacheClient>();

//Create 'CacheEntry' RDBMS table if it doesn't exist already
container.Resolve<ICacheClient>().InitSchema(); 
``` 

### Service Clients

  - Added `CaptureSynchronizationContext` option to get Async Service Clients to execute responses on the same SynchronizationContext as their call-site
  - Added `UserAgent` option, now defaults with the ServiceStack .NET client version

### Minor features

  - Allow unrestricted access for Redis MQ and Rabbit MQ clients within free-quotas
  - SessionIds are no longer created with Url Unfriendly chars `+`, `/`
  - Add typed `ToOneWayUrl()` and `ToReplyUrl()` extension method for generating predefined urls
  - Add Test showing how to use `ExecAllAndWait` extension method to [easily run synch operations in parallel](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Common.Tests/ActionExecTests.cs)
  - Added configurable BufferSize in StaticFileHandler
  - All CacheClients can now store AuthUserSessions when `JsConfig.ExcludeTypeInfo=true`
  - Allow RegistrationService to be used for PUT requests to updates User Registration info
  - Elmah Logger now takes in a `HttpApplication` so it can use `ErrorSignal.Get(application).Raise(<exception>)` allowing modules such as ErrorMail and ErrorPost (ElmahR) to be notified

## OrmLite

  - Add support for [cloning SqlExpressions](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/Expression/ExpressionChainingUseCase.cs#L192-L207)
  - Add example of [migrating SqlServer TIME column to BigInteger](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/AdoNetDataAccessTests.cs)
  - Add example of [calling Stored Procedures with OrmLite vs ADO.NET](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/TypeWithByteArrayFieldTests.cs#L55-L147)
  - Add support for [MaxText in all DB providers](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/TypeDescriptorMetadataTests.cs#L57-L96) with `[StringLength(StringLengthAttribute.MaxText)]`
  - Capture the LastSql Run even for queries with exceptions

## Redis

  - Use enhanced functionality for when newer versions of redis-server exists
    - i.e. Use more precise EXPIRE operations when server supports it
  - Add `GetServerTime()` 

## ServiceStack.Text

  - Moved `JsConfig.RegisterForAot()` to `PclExport.RegisterForAot()`
    - Fine-grained AOT hints available on `IosPclExport` static methods in PCL builds

## Breaking Changes

The [ServiceStack.Stripe](https://www.nuget.org/packages/ServiceStack.Stripe/) NuGet package is now a normal .NET 4.0 release. A new portable NuGet package was created for PCL clients at [ServiceStack.Stripe.Pcl](https://www.nuget.org/packages/ServiceStack.Stripe.Pcl/).

# v4.0.11 Release Notes

## OrmLite

This release saw a lot of effort towards adding new features to OrmLite:

### Pluggable Complex Type Serializers

One of the [most requested features](http://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/4738945-allow-ormlite-to-store-complex-blobs-as-json)
to enable pluggable serialization for complex types in OrmLite is now supported. This can be used to specify different serialization strategies for each 
available RDBMS provider, e.g:

```csharp
//ServiceStack's JSON and JSV Format
SqliteDialect.Provider.StringSerializer = new JsvStringSerializer();       
PostgreSqlDialect.Provider.StringSerializer = new JsonStringSerializer();
//.NET's XML and JSON DataContract serializers
SqlServerDialect.Provider.StringSerializer = new DataContractSerializer();
MySqlDialect.Provider.StringSerializer = new JsonDataContractSerializer();
//.NET XmlSerializer
OracleDialect.Provider.StringSerializer = new XmlSerializableSerializer();
```
You can also provide a custom serialization strategy by implementing 
[IStringSerializer](https://github.com/ServiceStack/ServiceStack.Text/blob/master/src/ServiceStack.Text/IStringSerializer.cs).

By default all dialects use the existing JsvStringSerializer, except for PostgreSQL which due to its built-in support for JSON, now uses the JSON format by default.  

#### Breaking Change

Using JSON as a default for PostgreSQL may cause issues if you already have complex types blobbed with the previous JSV Format.
You can revert back to the old behavior by resetting it back to the JSV format with:

```csharp
PostgreSqlDialect.Provider.StringSerializer = new JsvStringSerializer();
```

### New Global Insert / Update Filters

Similar to interceptors in some heavy ORM's, new Insert and Update filters were added which get fired just before any **insert** or **update** operation using OrmLite's typed API's (i.e. not dynamic SQL or partial updates using anon types).
This functionality can be used for easily auto-maintaining Audit information for your POCO data models, e.g:

```csharp
public interface IAudit 
{
    DateTime CreatedDate { get; set; }
    DateTime ModifiedDate { get; set; }
    string ModifiedBy { get; set; }
}

OrmLiteConfig.InsertFilter = (dbCmd, row) => {
    var auditRow = row as IAudit;
    if (auditRow != null)
        auditRow.CreatedDate = auditRow.ModifiedDate = DateTime.UtcNow;
};

OrmLiteConfig.UpdateFilter = (dbCmd, row) => {
    var auditRow = row as IAudit;
    if (auditRow != null)
        auditRow.ModifiedDate = DateTime.UtcNow;
};
```

Which will ensure that the `CreatedDate` and `ModifiedDate` fields are populated on every insert and update.

### Validation

The filters can also be used for validation where throwing an exception will prevent the operation and bubble the exception, e.g:

```csharp
OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = (dbCmd, row) => {
    var auditRow = row as IAudit;
    if (auditRow != null && auditRow.ModifiedBy == null)
        throw new ArgumentNullException("ModifiedBy");
};

try
{
    db.Insert(new AuditTable());
}
catch (ArgumentNullException) {
   //throws ArgumentNullException
}

db.Insert(new AuditTable { ModifiedBy = "Me!" }); //succeeds
```

### Custom SQL Customizations

A number of new hooks were added to provide more flexibility when creating and dropping your RDBMS tables.

#### Custom Field Declarations

The new `[CustomField]` can be used for specifying custom field declarations in the generated Create table DDL statements, e.g:

```csharp
public class PocoTable
{
    public int Id { get; set; }

    [CustomField("CHAR(20)")]
    public string CharColumn { get; set; }

    [CustomField("DECIMAL(18,4)")]
    public decimal? DecimalColumn { get; set; }
}

db.CreateTable<PocoTable>(); 
```

Generates and executes the following SQL:

```sql
CREATE TABLE "PocoTable" 
(
  "Id" INTEGER PRIMARY KEY, 
  "CharColumn" CHAR(20) NULL, 
  "DecimalColumn" DECIMAL(18,4) NULL 
);  
```

#### Pre / Post Custom SQL Hooks when Creating and Dropping tables 

A number of custom SQL hooks were added that allow you to inject custom SQL before and after tables are created or dropped, e.g:

```csharp
[PostCreateTable("INSERT INTO TableWithSeedData (Name) VALUES ('Foo');" +
                 "INSERT INTO TableWithSeedData (Name) VALUES ('Bar');")]
public class TableWithSeedData
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
}
```

And just like other ServiceStack attributes, they can also be added dynamically, e.g:

```csharp
typeof(TableWithSeedData)
    .AddAttributes(new PostCreateTableAttribute(
        "INSERT INTO TableWithSeedData (Name) VALUES ('Foo');" +
        "INSERT INTO TableWithSeedData (Name) VALUES ('Bar');"));
```

Custom SQL Hooks are now available to execute custom SQL before and after a table has been created or dropped, i.e:

```csharp
[PreCreateTable(runSqlBeforeTableCreated)]
[PostCreateTable(runSqlAfterTableCreated)]
[PreDropTable(runSqlBeforeTableDropped)]
[PostDropTable(runSqlAfterTableDropped)]
public class Table {}
```

### Re-factoring OrmLite's SQLite NuGet Packages

In their latest release, the SQLite dev team maintaining the [core SQLite NuGet packages](https://www.nuget.org/profiles/mistachkin/) 
have added a dependency to Entity Framework on their existing Sqlite NuGet packages forcing the installation of Entity Framework for users of OrmLite Sqlite. 
This change also caused some users to see invalid web.config sections after applying the new web.config.transforms.
After speaking to the maintainers they've created a new 
[System.Data.SQLite.Core](http://www.nuget.org/packages/System.Data.SQLite.Core) 
NuGet package without the entity framework dependency and the problematic web.config.transforms.

Unfortunately this was only added for their bundled x86/x64 NuGet package and not their other 
[System.Data.SQLite.x86](http://www.nuget.org/packages/System.Data.SQLite.x86/) and
[System.Data.SQLite.x64](http://www.nuget.org/packages/System.Data.SQLite.x64/) which the team have indicated should be deprecated
in favor of the x86/x64 bundled **System.Data.SQLite.Core** package. 

As a result of this we're removing the dependency to the Sqlite NuGet packages in both architecture specific
[ServiceStack.OrmLite.Sqlite32](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite32/) and 
[ServiceStack.OrmLite.Sqlite64](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite64/) packages and have
instead embedded the Sqlite binaries directly, which will solve the current issues and shield them from any future changes/updates 
from the upstream Sqlite packages.

#### New ServiceStack.OrmLite.Sqlite.Windows NuGet package

Both these arch-specific packages should now be deprecated in favour of a new Sqlite NuGet package supporting both x86/x64 architectures on Windows:

    PM> Install-Package ServiceStack.OrmLite.Sqlite.Windows

Which should now be used for future (or existing) projects previously using the old 
[OrmLite.Sqlite32](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite32/) and 
[OrmLite.Sqlite64](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite64/) packages.

The Windows-specific package was added in addition to our existing Mono and Windows compatible release:

    PM> Install-Package ServiceStack.OrmLite.Sqlite.Mono

Which works cross-platform on Windows and Linux/OSX with Mono should you need cross-platform support.  

## .NET Service Clients

New async API's were added for requests marked with returning `IReturnVoid`.
This provides a typed API for executing services with no response that was previously missing, e.g:

```csharp
public class Request : IReturnVoid {}

await client.PostAsync(new Request());
```

The API's for all sync and async REST operations have been changed to return `HttpWebResponse` which now lets you query the returned HTTP Response, e.g:
```csharp
HttpWebResponse response = await client.PostAsync(new Request());
var api = response.Headers["X-Api"];
```

## Authentication

### New IManageRoles API

A new [IManageRoles API](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Auth/IAuthRepository.cs#L26) 
was added that IAuthRepository's can implement in order to provide an alternative strategy for querying and managing Users' 
Roles and permissions. 

This new API is being used in the `OrmLiteAuthRepository` to provide an alternative way to store 
Roles and Permission in their own distinct table rather than being blobbed with the rest of the User Auth data. 
You can enable this new behavior by specifying `UseDistinctRoleTables=true` when registering the OrmLiteAuthRepository, e.g:

```csharp
container.Register<IAuthRepository>(c =>
new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
    UseDistinctRoleTables = true,
});
```

When enabled, roles and permissions are persisted in the distinct **UserAuthRole** table. 
This behavior is integrated with the rest of ServiceStack including the Users Session, RequiredRole/RequiredPermission attributes and the AssignRoles/UnAssignRoles authentication services.
Examples of this can be seen in [ManageRolesTests.cs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Common.Tests/ManageRolesTests.cs).

## [Messaging](https://github.com/ServiceStack/ServiceStack/wiki/Messaging)

### Flexible Queue Name strategies

There are now more flexible options for specifying the Queue Names used in [ServiceStack's MQ Servers](https://github.com/ServiceStack/ServiceStack/wiki/Messaging).
You can categorize queue names or avoid conflicts with other MQ services by specifying a global prefix to be used for all Queue Names, e.g:

```csharp
QueueNames.SetQueuePrefix("site1.");

QueueNames<Hello>.In //= site1.mq:Hello.inq
```

Or to gain complete control of each queue name used, provide a custom QueueName strategy, e.g:

```csharp
QueueNames.ResolveQueueNameFn = (typeName, suffix) =>
    "SITE.{0}{1}".Fmt(typeName.ToLower(), suffix.ToUpper());

QueueNames<Hello>.In  //= SITE.hello.INQ
```

> Note: Custom QueueNames need to be declared on both MQ Client in addition to ServiceStack Hosts.  

# v4.10 Release Notes

## Debug Links

To provide better visibility to the hidden functionality in ServiceStack we've added **Debug Info** links section to the `/metadata` page which add links to any Plugins with Web UI's, e.g:

![Debug Info Links](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/debug-links.png)

The Debug Links section is only available in **DebugMode** (recap: set by default in Debug builds or explicitly with `Config.DebugMode = true`). In addition, users with the **Admin** role (or if `Config.AdminAuthSecret` is enabled) can also view the debug Plugins UI's in production.

You can add links to your own [Plugins](https://github.com/ServiceStack/ServiceStack/wiki/Plugins) in the metadata pages with:

```csharp
appHost.GetPlugin<MetadataFeature>().AddPluginLink("swagger-ui/", "Swagger UI");
appHost.GetPlugin<MetadataFeature>().AddDebugLink("?debug=requestinfo", "Request Info");
```

`AddPluginLink` adds links under the **Plugin Links** section and should be used if your plugin is publicly visible, otherwise use `AddDebugLink` for plugins only available during debugging or development.

## [Auto Mapping](https://github.com/ServiceStack/ServiceStack/wiki/Auto-mapping)

#### Improved Support for non-POCO types
Previously you could only map between top-level POCO models, now you can map between scalars and collections directly, e.g:

```csharp
var intVal = 2L.ConvertTo<int>();
var decimalVal = 4.4d.ConvertTo<decimal>();
var usersSet = new[] { new User(1), new User(2) }.ConvertTo<HashSet<User>>();
```

#### Improved Auto-Mapping Performance

A better caching strategy is used for conversions paths and now mapping fields utilize cached Delegate expressions so POCO's with fields Map much faster. 

## Async Support

#### Consistent handling of Async Responses

Previously Response Filters were called with the Task response returned from async services for the Response DTO, e.g. `Task<TResponse>`. The response filters are now chained to the task so Response filters see the same native `TResponse` DTO that are passed in from Sync services.

#### Async services can now be used in MQ Servers

Async responses now block for results which is in-line with sync Services behavior where Message Queue Handlers only process one message at a time for each worker thread assigned to the Request type.

## NuGet packages specify min versions

To ensure NuGet pulls the latest dependencies when installing any ServiceStack package, a minimum version is now specified for all NuGet package dependencies. This [should alleviate dependency issues](http://stackoverflow.com/a/21670294/85785) people are seeing from NuGet's default behavior of pulling down old packages. 

# v4.09 Release Notes

## Rabbit MQ Support

The biggest feature in this release is ServiceStack's new support for 
[hosting Services via a Rabbit MQ Server](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ), 
expanding on our existing [Redis MQ and In Memory messaging](https://github.com/ServiceStack/ServiceStack/wiki/Messaging) options
with a new durable MQ option in the robust and popular [Rabbit MQ](http://www.rabbitmq.com). 
ServiceStack's Rabbit MQ support is available on NuGet with:

    PM> Install-Package ServiceStack.RabbitMq

A new [Rabbit MQ on Windows installation and setup guide](https://github.com/mythz/rabbitmq-windows) was published containing
code samples for working with Rabbit MQ from C#/.NET.

### Configurable Metadata Pages 

New customizable filters were added to the `MetadataFeature` plugin to allow customization of the Master and detail metadata pages before they're rendered.
E.g. you can reverse the order of operation names with:

```csharp
var metadata = (MetadataFeature)Plugins.First(x => x is MetadataFeature);
metadata.IndexPageFilter = page => {
    page.OperationNames.Sort((x,y) => y.CompareTo(x));
};
```

### OrmLite new runtime typed API 

The [IUntypedApi](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite/IUntypedApi.cs) interface is useful for when you only have access to a late-bound object runtime type which is accessible via `db.CreateTypedApi`, e.g:

```csharp
public class BaseClass
{
    public int Id { get; set; }
}

public class Target : BaseClass
{
    public string Name { get; set; }
}

var row = (BaseClass)new Target { Id = 1, Name = "Foo" };

var useType = row.GetType();
var typedApi = db.CreateTypedApi(useType);

db.DropAndCreateTables(useType);

typedApi.Save(row);

var typedRow = db.SingleById<Target>(1);
typedRow.Name //= Foo

var updateRow = (BaseClass)new Target { Id = 1, Name = "Bar" };

typedApi.Update(updateRow);

typedRow = db.SingleById<Target>(1);
typedRow.Name //= Bar

typedApi.Delete(typedRow, new { Id = 1 });

typedRow = db.SingleById<Target>(1); //= null
```

#### OrmLite Create Table Support

  - Added NonClustered and Clustered options to `[Index]` attribute

## Breaking changes

### Messaging

In order to support Rabbit MQ Server some changes were made to 
[ServiceStack's Messaging API](https://github.com/ServiceStack/ServiceStack/wiki/Messaging) to support all MQ options, namely:

  - `IMessageQueueClient` now exposes high-level `IMessage` API's instead of raw `byte[]`
  - The `IMessage.Error` property is now a `ResponseStatus` type (same used in Web Services)
  - **Ack** / **Nak** APIs were also added to `IMessageQueueClient`
  - All MQ Brokers now have a default `RetryCount=1`

### ServiceStack.Text

  - UrlEncode extension method now encodes spaces with `+` instead of `%20` to match default `HttpUtility.UrlEncode` behavior

### OrmLite

  - MySql and Sqlite providers now treat GUID's as `char(36)`

# v4.08 Release Notes

Added new [ServiceStack/Stripe](https://github.com/ServiceStack/Stripe) GitHub repository containing a PCL typed, message-based API client gateway for [Stripe's REST API](https://stripe.com/docs/api/). Install from NuGet with:

    Install-Package ServiceStack.Stripe

New in this release:

  - .NET 4.0 build of **ServiceStack.Razor** now available (in addition to .NET 4.5)
  - New **Signed** NuGet packages published for
    - [ServiceStack.Api.Swagger.Signed](https://www.nuget.org/packages/ServiceStack.Api.Swagger.Signed/)
    - [ServiceStack.OrmLite.Oracle.Signed](https://www.nuget.org/packages/ServiceStack.OrmLite.Oracle.Signed/)
  - Updated Swagger UI content files
  - Added MiniProfiler SqlServerStorage adapter to **ServiceStack.Server**
  - The [Razor Rockstars](https://github.com/ServiceStack/RazorRockstars/) and [Social Bootstrap Api](https://github.com/ServiceStack/SocialBootstrapApi/) projects have both been upgraded to v4

### OrmLite

  - Enums with `[Flag]` attribute (aka Enum flags) now stored as ints
  - `TimeSpan` now stores ticks as longs for all DB providers (Breaking change for Sqlite)

# v4.06 Release Notes

## Portable Class Library Clients!

The biggest feature of this release is the release of the new Portable Client NuGet packages:

[![Portable Class Library Support](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/hello-pcl.png)](https://github.com/ServiceStackApps/HelloMobile)

  - ServiceStack.Interfaces.Pcl
    - PCL Profiles: iOS, Android, Windows8, .NET 4.5, Silverlight5, WP8
  - ServiceStack.Client.Pcl
    - PCL Profiles: iOS, Android, Windows8, .NET 4.5
    - Custom builds: Silverlight 5
  - ServiceStack.Text.Pcl
    - PCL Profiles: iOS, Android, Windows8, .NET 4.5
    - Custom builds: Silverlight 5

This now allows sharing binaries between the above platforms. To illustrate this a new [Hello Repository](https://github.com/ServiceStackApps/HelloMobile) was created to show how to use the same portable class libraries and DTO's across the different client platforms above.

#### Breaking Changes

Adding PCL support to the client libraries involved a lot of internal re-factoring which caused a few external user-facing changes:

  - The `IDbConnectionFactory` and `IHasDbConnection` interfaces referencing System.Data was moved to ServiceStack.Common
  - Properties exposing the concrete `NameValueCollection` are now behind an `INameValueCollection` interface
  - Dynamic classes like `DynamicJson` have been moved under the `ServiceStack` namespace

### Improved SOAP Support 

For maximum compatibility with different SOAP clients, SOAP Exceptions are now treated as "Soft HTTP Errors" where exceptions
are automatically converted to a **200 OK** but returns the original Status Code in the `X-Status` HTTP Response header or `X-Status` SOAP Header.

Errors can be detected by looking at the X-Status headers or by checking the **ResponseStatus.ErrorCode** property on the Response DTO. 
This is transparently handled in ServiceStack's built-in SoapClients which automatically converts Response Errors into populated 
C# WebServiceExceptions, retaining the same behavior of ServiceStack's other typed clients, as seen in 
[WebServicesTests](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/AlwaysThrowsService.cs#L162).

IHttpRequest.OperationName now reports the Request DTO name for SOAP requests as well, which it gets from the SOAPAction HTTP Header in SOAP 1.1 requests or the **Action** SOAP Header for SOAP 1.2 Requests.

# ServiceStack V4 Release Notes

We're happy to announce that after months of intense development, v4-beta of ServiceStack has finally been released to NuGet! 

As [announced in August](https://plus.google.com/+DemisBellot/posts/g8TcZaE7bv9) to ensure it's continued development, ServiceStack has moved to a self-sustaining commercial model for commercial usage of ServiceStack from **v4+ onwards**. It's the first time we've been able to commit full-time resources to the project and is what has ensured continued investment and enabled v4 to be possible, with even more exciting features in the pipeline and roadmap for 2014.

## [Introductory Offer](https://servicestack.net/pricing)

For our early supporters we're launching the new [servicestack.net](https://servicestack.net) website with [attractive introductory pricing](https://servicestack.net/pricing) available during the beta between **33-40% off** royalty-free/per-developer perpetual licensing and **20% off** our unlimited-developers/per-core subscriptions. There's also an additional **60 days free** maintenance and updates covering the beta period, available in 2013. These discounts are intended to be grandfathered-in and carried over for any future renewals, making the v4-beta the best time to get ServiceStack. For US Customers we also have free ServiceStack T-Shirts and stickers whilst stocks last - If you'd like them, add your preferred T-Shirt sizes in the Order notes.

#### Free Usage for Small and OSS Projects

We're also happy to announce that v4 includes [free quotas](https://servicestack.net/download#free-quotas) allowing the free usage of all of ServiceStack for small projects and evaluation purposes. Whilst OSS projects are able to use the source code on GitHub under the [AGPL/FOSS License Exception](https://github.com/ServiceStack/ServiceStack/blob/master/license.txt), and the older [v3 of ServiceStack](https://github.com/ServiceStackV3/ServiceStackV3) continues to be available under the [BSD license](https://github.com/ServiceStack/ServiceStack/blob/v3/LICENSE).

#### Upgrading from v3

Whilst we recommend starting with **v4** for greenfield projects, v4 has seen significant changes since v3 that will require some development effort to upgrade. During the upgrade we recommend using a tool like [ReSharper](http://www.jetbrains.com/resharper/) to be able to easily find and update reference of any types that have moved.

# What's new in v4

The major version upgrade of ServiceStack to v4 has provided us a long sought **breaking window** opportunity allowing us to re-factor, simplify, clean-up and fix all the warts and cruft that has been lingering in the ServiceStack code-base since its beginning - that due to backwards compatibility we were reluctant to remove. Whilst v4 has seen significant changes to the code-base, all existing tests are passing again with additional tests added for new functionality. 

We managed to retain a lot of the user-facing API's (E.g New API, AppHost, Config) which were already considered ideal so ideally upgrading shouldn't be too disruptive in the normal use-cases. 

v4 provides us a great foundation to build on that will be further improved during the beta by focusing on stability and fixing any reported issues as well as updating existing documentation to match v4's implementation and publish new examples showcasing v4's new features.

## The big refactor of v4

This was the biggest re-factor in ServiceStack's history which at the end resulted in a much leaner, simplified, consistent and internal logically-structured code-base that's much easier to reason about, where even before adding any features the main ServiceStack repository saw:

    1,192 changed files with 18,325 additions and 29,505 deletions. 

The number of deletions is indicative of how much legacy code was able to be removed, with much of the internals having been heavily restructured. Some of the highlights during the re-factor include: 

  - All projects have been upgraded to .NET 4.0, except ServiceStack.Razor which is .NET 4.5 to use the latest version of Razor
  - All obsolete/unused/shims/duplicate functionality and built-up cruft has now been removed (inc. the Old Api). 
  - State and configuration are now cohesively organized where now all AppHost's share the same `ServiceStackHost` base class which now maintains all state in ServiceStack, inc. the empty `BasicAppHost` that's used for unit testing which now shares much of the same state/context as Integration tests.
  - Many namespaces and some concepts have been collapsed (e.g 'Endpoint'), resulting in ServiceStack projects requiring fewer namespaces
  - All DTO's and extension methods and common user-facing classes have been moved to the base `ServiceStack` namespace - allowing them to be much easier to find. 
  - Re-organization of projects, **NuGet packages now map 1:1 with ServiceStack projects** for finer-grained control of dependencies:
    + **ServiceStack.Interfaces** NuGet project created and ServiceInterface has been merged into **ServiceStack**
    + **ServiceStack** NuGet package now only depends **ServiceStack.Common** and **ServiceStack.Client**
    + A new **ServiceStack.Server** project exists for functionality requiring dependencies on OrmLite or Redis, inc. RedisMqServer and OrmLiteAuthRepository. 
    + **ServiceStack.Client** contains all the HTTP, SOAP and MQ Service Clients that have been split from **ServiceStack.Common** and only depends on ServiceStack.Interfaces and ServiceStack.Text (making it easier to maintain custom builds in future).
  - EndpointHostConfig is now `HostConfig` and is limited to just Configuration, e.g. handlers like `CustomErrorHttpHandlers`, `RawHttpHandlers`, `GlobalHtmlErrorHttpHandler` have been moved to ServiceStackHost. 
  - EndpointHost is gone and replaced by the static `HostContext` class which doesn't contain any state itself, it's just a static convenience wrapper around `ServiceStackHost.Instance` (where all state is maintained). 
  - Removed all 'where T:' constraints where possible
  - Removed `ConfigurationResourceManager`, use `AppSettings` instead
  - The `ServiceStack.WebHost.Endpoints.ServiceStackHttpHandlerFactory` used in Web.config's handler mapping has been renamed to just `ServiceStack.HttpHandlerFactory`
  - `Config.ServiceStackHandlerFactoryPath` has been renamed to `Config.HandlerFactoryPath`.
  - Predefined routes have been renamed from `/syncreply`, `/asynconeway` to just `/reply`, `/oneway`
  - ServiceManager has been merged into `ServiceController`. 
  - The **ServiceStack.Logging** and **ServiceStack.Contrib** v4 projects have been merged into the major ServiceStack repo.
  - The dynamic session `base.Session` has been renamed to `base.SessionBag` to better reflect its semantics.
  - The [Auto Mapping](https://github.com/ServiceStack/ServiceStack/wiki/Auto-mapping) Utils extension methods were renamed from `TFrom.TranslateTo<T>()` to `TFrom.ConvertTo<T>()`.
  - The `RequestFilters` and `ResponseFilters` were renamed to `GlobalRequestFilters` and `GlobalResponseFilters` which matches naming in the client `ServiceClientBase.GlobalRequestFilter`.
  - New `GlobalMessageRequestFilters` and `GlobalMessageResponseFilters` have been added which are instead used by non-HTTP endpoints use, e.g. MQ. 
  - `CustomHttpHandlers` has been renamed to `CustomErrorHttpHandlers`
  - The **LocalHttpWebRequestFilter** and **LocalHttpWebResponseFilter** in the Service Clients were renamed to just `RequestFilter` and `ResponseFilter`
  - The Global **HttpWebRequestFilter** and **HttpWebResponseFilter** filters were also renamed to `GlobalRequestFilter` and `GlobalResponseFilter` respectively.
  
### RequestContext now merged into new IRequest / IResponse classes:

An annoyance remaining in the ServiceStack code-base was RequestContext and its relationship with its IHttpRequest and IHttpResponse classes. This was originally modeled after ASP.NET's relationship with HttpContext and its child HttpRequest/HttpResponse classes. Pragmatically speaking this model isn't ideal, as there was functionality spread across all 3 classes, many times duplicated. It was also not obvious how to retrieve IHttpRequest/IHttpResponse classes from a RequestContext and creating a RequestContext from outside of ServiceStack required more knowledge and effort than it should have. 

The new model adopts a flattened structure similar to Dart's server HttpRequest (http://bit.ly/19WUxLJ) which sees the `IRequestContext` eliminated in favour of a single `IRequest` class that also makes available direct access to the Response.

This now becomes much easier to create a Request from outside of ServiceStack with an ASP.NET or HttpListener HttpContext e.g:

```csharp
var service = new MyService {
    Request = HttpContext.Current.ToRequest()
}

var service = new MyService {
    Request = httpListenerContext.ToRequest()
}
```

There's also direct access to the Response from a Request with:

```csharp
IResponse response = Request.Response;
```

#### ASP.NET wrappers now only depends on HttpContextBase

Also the ASP.NET `IHttpRequest` wrappers bind to the newer and mockable HttpContextBase / HttpRequestBase / HttpResponseBase classes which now makes it easier to call services from newer web frameworks like MVC with:

```csharp
var service = new MyService {
    Request = base.HttpContext.ToRequest()
}
```

The biggest user-facing change was renaming the IHttpRequest/IHttpResponse classes to IRequest/IResponse which is more indicative to what they represent, i.e. the Request and Response classes for all endpoints including MQ and future TCP endpoints. Now only HTTP Requests implement IHttpRequest/IHttpResponse which lets you add logic targeting only HTTP Services with a simple type check:

```csharp
var httpReq = request as IHttpRequest;
if (httpReq != null) {
    //Add logic for HTTP Requests...
}
```

Accessing the IHttpResponse works the same way, e.g:

```csharp
var httpRes = Request.Response as IHttpResponse;
if (httpRes != null) {
    //...
}
```

We're still going to add extension methods on IRequest/IResponse to make it easier to discover new functionality, but for HTTP functionality on non-HTTP requests these would just be a NO-OP rather than throw an exception.

### Community v4 migration notes

  - [Upgrading Servicestack to 4.0 – Notes](http://www.binoot.com/2014/02/23/upgrading-servicestack-to-4-0-notes/) by [@binu_thayamkery](https://twitter.com/binu_thayamkery)
  - [Upgrading OrmLite and ServiceStack to v4](http://camtucker.blogspot.ca/2014/01/updating-to-servicestack-v40.html?view=classic) by [@camtucker](http://camtucker.blogspot.ca/)

----  

# New Features in v4


## Server-side Async Support

The [most requested feature](http://bit.ly/16qCiy1), Server-side async support has now been implemented! This was surprisingly easy to do where now all HttpHandlers in ServiceStack inherit from a common `HttpAsyncTaskHandler` base class that now implements `IHttpAsyncHandler`. This lets you return an async Task from your Service in any number of ways as shown in http://bit.ly/1cOJ3hR 

E.g. Services can now have either an object, Task or async Task return types that can return a started or non-started task (which we'll start ourselves). This transition went as smooth as it could where all existing services continuing to work as before and all tests passing.

## [ServiceStack Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) Task-based Async

In matching the new server-side async story and now that all projects have been upgraded to .NET 4.0, all Service Clients have been changed to return .NET 4.0 Task's for all async operations so they can be used in C#'s async/await methods. Some examples of Async in action: http://bit.ly/17ps94C

The Async API's also provide a **OnDownloadProgress** callback which you can tap into to provide a progress indicator in your UI, E.g: http://bit.ly/19ALXUW

#### Use any Request DTO in Client API's

ServiceClient API's that used to only accept Request DTO's with a `IReturn` marker, now have `object` overloads so they can be used for unmarked Request DTO's as well.

### Custom Silverlight and Android builds

We've added custom **Silverlight** and **Android** automated builds for ServiceStack.Client allowing the client libraries to be available in even more environments - with more to follow.

## Signed NuGet Packages

The following Signed NuGet packages are available for core ServiceStack projects in separate NuGet packages using the .Signed suffix:

  - ServiceStack.Client.Signed
  - ServiceStack.Text.Signed
  - ServiceStack.Redis.Signed
  - ServiceStack.OrmLite.Signed
  - ServiceStack.OrmLite.SqlServer.Signed
  - ServiceStack.ServiceStack.Signed
  - ServiceStack.ServiceStack.Razor.Signed
  - ServiceStack.ServiceStack.Server.Signed
  - ServiceStack.Common.Signed

### ServiceStack.Interfaces is now strong-named

In order to be able to have signed clients sharing types with non-signed ServiceStack instances, the DTO models and ServiceStack.Interfaces need to be signed. It was added in the most defensive way possible where **ServiceStack.Interfaces.dll** is the only dll that's strong-named by default. This should cause minimal friction as it is an impl-free assembly that rarely sees any changes. We're also keeping the AssemblyVersion which makes up the strong-name at a constant `4.0` whilst the benign AssemblyFileVersion will report the true version number. 

### Add Code-first Attributes at runtime, de-coupled from POCO's 

Inspection of all Metadata attributes in ServiceStack now uses ServiceStack.Text's attribute reflection API's which support adding of type and property metadata attributes dynamically. This now lets you add the same behavior normally only available via attributes, dynamically at StartUp. Some benefits of this include: being able to keep [unattributed data model POCOs in OrmLite](http://bit.ly/1e5IQqS) or to [extend built-in and external Request DTOs and Services](https://github.com/ServiceStack/ServiceStack/blob/d93ad805c8c8ffce8e32365e4217c65c19069cf0/tests/ServiceStack.WebHost.Endpoints.Tests/RuntimeAttributeTests.cs) with enhanced functionality that was previously only available using attributes.

#### Fluent route configuration available in [Reverse Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing)

Leveraging the dynamic attribute support, we now include fluent Route definitions when retrieving relative or absolute urls in [Reverse Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing), which can be used in Services when returning urls in responses and is also used in Service Clients to determine which routes to use. Note: as Fluent Routes are defined in the AppHost, they aren't registered and therefore not available in disconnected .NET client applications - so using `[Route]` attributes on Request DTO's remains the best way to share route definitions on both client and server.

Priority was added to `[Route]` attributes so auto-generated routes are given less precedence than explicit user-defined custom routes when selecting the best matching route to use.

### The Virtual FileSystem

The Virtual FileSystem is now fully integrated into the rest of ServiceStack, this enables a few interesting things:

  - The `Config.WebHostPhysicalPath` sets where you want physical files in ServiceStack to be served from
  - You can now access static files when ServiceStack is mounted at a custom path, e.g. /api/default.html will serve the static file at ~/default.html
  - By Default, ServiceStack falls back (i.e when no physical file exists) to looking for Embedded Resource Files inside dlls. 
  - You can specify the number and precedence of which Assemblies it looks at with `Config.EmbeddedResourceSources` which by default looks at:
    - The assembly that contains your AppHost
    - **ServiceStack.dll**

The VFS now elegantly lets you replace built-in ServiceStack templates with your own by simply copying the metadata or [HtmlFormat Template files](http://bit.ly/164YbrQ) you want to customize and placing them in your folder at:

    /Templates/HtmlFormat.html        // The auto HtmlFormat template
    /Templates/IndexOperations.html   // The /metadata template
    /Templates/OperationControl.html  // Individual operation template

This works because the ServiceStack.dll is the last assembly in `Config.EmbeddedResourceSources`.

## API-first development

We're starting to optimize ServiceStack's HTML story around an **API-first** style of web development (particularly well suited to ServiceStack) in which services are developed so they naturally support both web and native clients from the start. Effectively this means that the HTML views are just another client that escapes C# earlier and leverages JS+Ajax to provide its dynamic functionality, and any HTML-specific functionality is encouraged to be kept in Razor views rather than using post backs to generate different server-side HTML representations. 

Having developed the new [servicestack.net website](https://servicestack.net) in this way, we've found it to be a lot more productive and responsive than standard server-side MVC development that we we're accustomed to in .NET as JavaScript ends up being more mallable and flexible language with a smaller and reflective surface type area making it better suited in string manipulation, generating HTML views, consuming ajax services, event handling, DOM binding and manipulation, etc. 

We've begun taking advantage of the Virtual FileSystem to ship embedded resources enhancing ServiceStack's JS integration with client-side libraries like [ss-utils.js](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/js/ss-utils.js) that we maintain and update alongside the rest of ServiceStack's dlls. Whilst we intend to create more examples in the near future showcasing this functionality, here's an overview of what's been added:

  - ss-utils.js available in your local ServiceStack webhost at `/js/ss-utils.js`
  - Inspired by AngularJS we've added **declarative** support over jQuery, letting you declaratively register and trigger events, bind values to HTML elements, register document handlers, etc, saving a lot of boilerplate than normal jQuery (more on this soon)
  - Enhanced HTML forms with integration with ServiceStack validation, adds responsive UX/behavior, follows soft redirects
  - Server-side responses can be decorated with Soft redirects with `HttpResult.SoftRedirect` or client events with `HttpResult.TriggerEvent`
  - Use `("a").setActiveLinks()` to automatically set the active link and containing menu items for the current page
  - Use `$("input").change($.ss.clearAdjacentError)` to clear highlighted errors as users correct their inputs
  - Use `T.AsRawJson()` extension method to serialize C# models into JSON literals that are natively accessible in JS
  - Use `T.ToGetUrl()`, `T.ToPostUrl()` to resolve service urls from typed Request DTOs (no code-gen required)

## Improved Razor Support

#### Improved Server-side validation

The server-side validation story has also been improved with MVC's HTML INPUT and Validation Helpers rewritten to look at ServiceStack error responses (earlier lost in the upgrade to Razor 2.0) and making use of the same bootstrap conventional classes that the client-side Ajax validation uses letting you maintain a single style of error feedback for both validation styles. It also now looks at state contained in the POST'ed data when rendering the HTML INPUT controls.
  
#### Fallback Routes

The default Razor views are now also processed by the `FallbackRoute` if one exists, enhancing the story for Single Page Apps who want requests to un-specified routes to be handled by client-side routing instead of returning 404's.

#### Pre-Request filters

Direct (i.e. No Controller) Razor views and static file handlers now have pre-request filters applied to them, so they can be used for adding global behavior across all ServiceStack service and page requests.

#### Precompilation option for Razor Views

New options have been added to RazorFormat `PrecompilePages` and `WaitForPrecompilationOnStartup` that allow you to precompile razor views on startup and specify whether or not you want to wait for compilation to complete are now options available when registering the `RazorFormat`. As these can slow down dev iteration times they are not done when `Config.DebugMode` (aka development mode), but are otherwise enabled by default for production.

#### Other Razor Improvements

  - More functionality was added to Razor Views matching the same API's available in ServiceStack's base `Service` class
  - RenderSection/IsSectionDefined now looks in all connected views.
  - `GetAbsoluteUrl`, `IsPostBack`, `GetErrorStatus()`, `GetErrorMessage()` convience methods added 

### CORS Feature

CorsFeature now by default automatically handles all HTTP `OPTIONS` requests so you no longer have to explicitly allow for OPTION requests in your routes: http://bit.ly/19HbMVf

Can be disabled with: 

```csharp
Plugins.Add(new CorsFeature { AutoHandleOptionsRequests = false })
```

## Authentication

The Auth Tables are now called **UserAuth** and **UserAuthDetails** and implements the IUserAuth and IUserAuthDetails interfaces. For advanced customization, these tables can now be extended using custom models inheriting these interfaces by using the generic AuthRepository types, e.g:

  - OrmLiteAuthRepository<TUserAuth, TUserAuthDetails>
  - RedisAuthRepository<TUserAuth, TUserAuthDetails>

Where the common non-generic **OrmLiteAuthRepository** is just a concrete impl inheriting from `OrmLiteAuthRepository<UserAuth, UserAuthDetails>`. Use `InitSchema()` to ensure missing Auth Tables are created at registration.
  
#### New optional UserAuthRole table added

A new `UserAuthRole` class was created for users who would prefer roles to be managed in separate tables rather than blobbed with the UserAuth table and session. E.g. You can change your custom session to check the database for asserting required users and permissions with:

```csharp
public class CustomUserSession : AuthUserSession
{
    public override bool HasRole(string role)
    {
        using (var db = HostContext.TryResolve<IDbConnectionFactory>().Open())
        {
            return db.Count<UserAuthRole>(q => 
                q.UserAuthId == int.Parse(UserAuthId) && q.Role == role) > 0;
        }
    }

    public override bool HasPermission(string permission)
    {
        using (var db = HostContext.TryResolve<IDbConnectionFactory>().Open())
        {
            return db.Count<UserAuthRole>(q => 
                q.UserAuthId == int.Parse(UserAuthId) && q.Permission == permission) > 0;
        }
    }
}
```

#### Support for Max Login Attempts 

The `OrmLiteAuthRepository` now supports automatically locking out user accounts after reaching the maximum number of Login attempts which can be specified at registration, e.g:

```csharp
container.Register<IAuthRepository>(c =>
    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
        MaxLoginAttempts = appSettings.Get("MaxLoginAttempts", 5)
    });
```

To opt-in to use the new locking behavior provide a value for `MaxLoginAttempts` as shown above. The above registration first uses the value overridable in appSettings if it exists, otherwise it defaults to a Maximum of 5 login attempts. 

#### Adhoc locking of User Accounts

The `CredentialsAuthProvider` also supports locking user accounts by populating the `UserAuth.LockedDate` column with a non-null value. Set it back to null to unlock the account.

#### Initializing Auth Repository Schemas
    
Some Auth Repositories like OrmLite require an existing schema before they can be used, this can be done in the AppHost with:

```csharp
//Create missing Auth Tables in any Auth Repositories that need them
container.Resolve<IAuthRepository>().InitSchema(); 
```
This was previously named `CreateMissingTables()` and is safe to always run as it's a NO-OP for Auth repositories that don't require a schema and only creates missing tables, so is idempotent/non-destructive on subsequent runs.

#### New AuthWeb Test project

A new test project testing all Authentication providers within the same ServiceStack ASP.NET Web Application is in [ServiceStack.AuthWeb.Tests](https://github.com/ServiceStack/ServiceStack/tree/master/tests/ServiceStack.AuthWeb.Tests).

### AppSettings

AppSettings can now be passed a tier in the constructor, e.g. `new AppSettings(tier: "Live")` which it uses as a prefix to reference Tier-specific appSettings first, e.g:

    <add key="Live.AppDb" value="..." />
    
Before falling back to the common key without the prefix, i.e:

    <add key="AppDb" value="..." />

AppSettings now allows a Parsing Strategy, e.g. You can collapse new lines when reading a complex configuration object in Web.Config `<appSettings/>` with:

```csharp
var appSettings = new AppSettings { 
    ParsingStrategy = AppSettingsStrategy.CollapseNewLines 
};
```

### Nested Request DTOs

Using [nested types as Request DTO's](https://github.com/ServiceStack/ServiceStack/commit/376ca38f604214f4d12e2f7803d8e7cfc271b725) are now supported.
Nested Request DTO types include the names of their containing class to form their unique name, allowing the use of multiple nested types with the same name, which is potentially interesting to be used as a versioning strategy.

### Localized symbols

I've added `IAppHost.ResolveLocalizedString` support in [this commit](http://bit.ly/181q0eP) which lets you override the built-in English symbols used in ServiceStack, e.g. this lets you change built-in ServiceStack routes, e.g: `/auth`, `/assignroles`, `?redirect=`, etc. into something more appropriate for your language. 

## Other New Web Framework Features

  - Added convenient [Repository and Logic base classes](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/ILogic.cs) to reduce boilerplate when extracting logic from services into custom classes
  - Added `IAppHost.OnExceptionTypeFilter` to be able to customize ResponseStatus based on Exception types. Used to change the [default behavior of ArgumentExceptions](https://github.com/ServiceStack/ServiceStack/commit/17985239ed6f84b3126c651dbacd0c760a4d2951) so that they're converted to field errors
  - Added `IAppHost.OnServiceException` so service exceptions can be intercepted and converted to different responses
  - Add `ConvertHtmlCodes` extension method converting HTML entities to hex-encoded entities
  - Add `Config.ScanSkipPaths` option to skip any plugins using the VFS to scan the filesystem (e.g. Razor/Markdown feature) from scanning specified directories, `/bin/` and `/obj/` are added by default.
  - Added a pre-defined `/swagger-ui/` route that loads the Swagger UI and auto configures it to look at ServiceStack services. A link to this is on the metadata page under **Plugin Links** heading.
  - Added `ModelFilter` and `ModelPropertyFilter` to allow fine-grained custom control on what's displayed in the Swagger API
  - Wrappers around .NET's JSON and XML DataContract Serializers now share the same `IStringSerializer` interface
  - Added ToMsgPack/FromMsgPack and ToProtoBuf/FromProtoBuf extension methods
  - Improved support for stripping App Virtual Paths when Resolving Absolute Urls useful when applications are hosted with virtual app paths as done in Amazon Web Services. This behavior can be enabled with `Config.StripApplicationVirtualPath = true`.
  - Support for explicitly referencing ignored DTO properties in Route PathInfo definitions, but not QueryStrings
  - Add support for getting Id property from runtime object type
  - Added support for registering a singleton instance as a runtime type
  - Added new [IRestGateway](https://github.com/ServiceStack/ServiceStack/commit/29d60dfa22424fe20ba35c8603686c05f88a6c25) interface that typed 3rd Party gateways can use to retain a consistent and mockable interface. Specialized MockRestGateway added to stub or mock out gateways to 3rd Party services
  - `__requestinfo` is now available on any request with `?debug=requestinfo` and is accessible to administrators or when in **DebugMode**, and provides in-depth diagnostics about the details of the current request and the configured AppHost including Startup errors (if any).
  - Plugins can register startup exceptions with `IAppHost.NotifyStartupException`
  - Added new HTTP Headers on IHttpRequest for `XForwardedPort` and `XForwardedProtocol`
  - Added `[EnsureHttps]` Request Filter to automatically redirect request if service was not requested under a Secure Connection with options to **SkipIfDebugMode** or **SkipIfXForwardedFor** to allow local development and requests via proxies / load-balancers in HTTP.
  - Users in the **Admin** role have super-user access giving them access to all protected resources. You can also use `Config.AdminAuthSecret` to specify a special string to give you admin access without having to login by adding `?authsecret=xxx` to the query string.

-----

## OrmLite

### Improved Consistency

As the API surface of OrmLite expands it became a lot more important to focus on better consistency which now sees all alternative aliases having been removed in favor of standardized naming around SQL equivalents (e.g Select,Insert,etc). Also the parameterized APIs and the C#-like string.Format API's have now been merged with the parameterized APIs now being the default and the string.Format API having a 'Fmt' suffix. 

Most of these APIs now have XML docs and Examples for a better Intelli-sense experience. We've also provided them in a list along side it's generated SQL in [these API tests](http://bit.ly/1gmrnwe)

Some notes:

  - `Select` returns a List
  - `Single` returns 1 row (or null) if it doesn't exist
  - `Scalar` returns a single a scalar value (e.g. int, long)
  - `Where` is a short-hand for 'Select' that takes a single filter
  - `Count` is a convenience that performs an aggregate SQL Count
  - `Exists` returns true if there were any results
  - `Lazy` suffix indicates the results are lazily streamed
  - `Column` returns the first column results in a List
  - `ColumnDistinct` returns the first column unique results in a HashSet
  - `Dictionary` returns a Dictionary made up from the first 2 columns
  - `Lookup` returns a LINQ-like grouping in a Dictionary<K, List<V>>
  - `NonDefaults` suffix indicates only non-null values are used in qry
  - `Only` suffix allows you to specify fields used on the call-site
  - `Sql` prefix are helpers for reading and querying arbitrary raw SQL
  - `Save` is a convenience that inserts or updates depending if it exists or not. It also now populates AutoIncrementing Id's on Inserts.
  - All batch operations like `InsertAll`, `UpdateAll`, `DeleteAll`, `SaveAll` participate in an existing transaction if 1 exists, otherwise a new one
  - Removed all 'where T:' constraints where possible
  - `OrDefault` APIs removed, All APIs now return null instead of throwing
  - autoDisposeConnection removed. false for ":memory:" otherwise true
  - Now that all OrmLite's parameterized `Query*` APIs have been merged (above), any `Query` APIs are from Dapper's extension method, which is also included in OrmLite under ServiceStack.OrmLite.Dapper namespace.
  - All remaining OrmLIte Attributes have been moved to ServiceStack.Interfaces, which in future will be the only dependency needed by your data models and DTOs.

### OrmLite extension methods are now mockable

OrmLite API's can now be mocked by injecting a ResultsFilter letting you mock the results return by OrmLite which it will use instead of hitting the database. You can also mock with a filter function and it also supports nesting, see examples at: http://bit.ly/1aldecK

This will be useful in Unit Testing Services that access OrmLite directly instead of using a repository.﻿

### Support for references, POCO style

We've added a cool new feature to Store and Load related entities that works great on POCO which are enabled when you use the `[Reference]` attribute, e.g: http://bit.ly/1gmvtV6

Unlike normal complex properties in OrmLite:

  - Doesn't persist as complex type blob
  - Doesn't impact normal querying
  - Saves and loads references independently from itself
  - Populated references get serialized in Text serializers (only populated are visible).
  - Data is only loaded 1-reference-level deep
  - Reference Fields require consistent `(T)Id` naming
 
Basically it provides a better story when dealing with referential data that doesn't impact the POCO's ability to be used as DTO's. At the moment it's limited to loading and saving on a Single instance. We'll look at optimizations for batches on this in future.﻿ 

We're going to be giving OrmLite a lot more attention from now on given that we're working full-time on ServiceStack and are using it exclusively for our .NET RDBMS peristence. We also intend on adding specialized support to take advantage of PostgreSQL's new features like their HStore and native JSON support. PostgreSQL has been offering the best features of both RDBMS and NoSQL worlds lately and has recently become a particularly attractive option now that AWS is offering first-class support for PostgreSQL in both their RDS and Redshift services.

-----
## ServiceStack.Text

  - Allow adding metadata attributes to types or attributes at runtime
  - Add JsConfig.ExcludeTypes option to skip serialization of non-serializable properties like Streams
  - Change QueryString's to also adopt the configured `JsConfig.PropertyConvention` and `JsConfig.EmitLowercaseUnderscoreNames` behavior
  - Added an injectable ComplexTypeStrategy to the QueryStringSerializer that allows customizing the generation of complex  types, e.g. can use a hash literal notation strategy with `QueryStringStrategy.FormUrlEncoded`.
  - Added `typeof(T).New()` extension method providing a fast way of creating new instances of static or runtime types that will use factory functions registered in the centralized `JsConfig.ModelFactory` (if configured).
  - The string "on" (i.e. the default value for HTML checkbox) is considered a **true** value for booleans (same with '1')
  - The JSON serializers can be configured to support UnixTime and UnixTimeMs for DateTime's
  - Renamed JsonDateHandler to `DateHandler` and JsonPropertyConvention to `PropertyConvention`

### HTTP Utils are now mockable

Following in the steps of now being able to Mock OrmLite, the [HTTP Utils](https://github.com/ServiceStack/ServiceStack/wiki/Http-Utils) extension methods (http://bit.ly/WyV2tn) are now mockable, e.g:

    using (new HttpResultsFilter {
        StringResult = "mocked"
    })
    {
        //All return "mocked"
        "http://google.com".GetJsonFromUrl();
        "http://google.com".GetXmlFromUrl();
        "http://google.com".GetStringFromUrl(accept: "text/csv");
        "http://google.com".PostJsonToUrl(json: "{\"postdata\":1}");
    }

More examples showing how all HTTP Apis can be mocked are at: http://bit.ly/HdWmgm

-----

## New pre-release MyGet Feeds

Instead of publishing pre-release packages on NuGet, we're instead going to release our interim packages to [MyGet](https://www.myget.org/) first which provides greater control and allows better management of packages.

The Instructions to add ServiceStack's MyGet feed to VS.NET are:

  1. Go to Tools -> Options -> Package Manager -> Package Sources
  2. Add the Source **https://www.myget.org/F/servicestack** with the name of your choice, e.g. _ServiceStack MyGet feed_

-----

## [Older v3 Release Notes](https://github.com/ServiceStack/ServiceStack/wiki/Release-Notes-v3)
