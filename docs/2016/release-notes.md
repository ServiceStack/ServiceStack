# v4.0.54 Release Notes

We have another massive release in store with big updates to AutoQuery, Server Events and TypeScript support
as well as greater flexibility for Multitenancy scenarios and around Service Clients.

## [AutoQuery Viewer](https://github.com/ServiceStack/Admin)

If you've yet to try
[Auto Query](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) 
we encourage you to check it out, it lets you effortlessly create high-performance, fully-queryable, 
self-descriptive services with just a single, typed Request DTO definition. As they're just normal ServiceStack
Services they also benefit from ServiceStack's surrounding feature ecosystem, including native support in 
.NET PCL Service Clients and multi-language 
[Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) 
clients. We're excited to announce even more new features for AutoQuery in this release - making it more 
capable and productive than ever!

AutoQuery Viewer is an exciting new feature providing an automatic UI to quickly browse and query all 
your AutoQuery Services!

[![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/query-default-values.png)](http://github.servicestack.net/ss_admin/autoquery)

> [YouTube Demo](https://youtu.be/YejYkCvKsuQ)

AutoQuery Viewer is a React App that's bundled within a single `ServiceStack.Admin.dll` that's available from NuGet at:

### Install ServiceStack.Admin

    PM> Install-Package ServiceStack.Admin

Signed Version also available from NuGet at [ServiceStack.Admin.Signed](http://nuget.org/packages/ServiceStack.Admin.Signed)
    
Then to add it to your project, just register the Plugin:

```csharp
Plugins.Add(new AdminFeature());
```

Which requires [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query), if not already registered:

```csharp
Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
```

Once enabled a link to the AutoQuery Viewer will appear under **Plugin Links** in your 
[Metadata Page](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-page):

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/metadata-plugin-link.png)

> Or you can navigate to it directly at `/ss_admin/`

As it's quick to add, we've already enabled it in a number of existing Live Demo's containing AutoQuery Services:

### Live Examples

- http://github.servicestack.net/ss_admin/
- http://northwind.servicestack.net/ss_admin/
- http://stackapis.servicestack.net/ss_admin/
- http://techstacks.io/ss_admin/

### Default Minimal UI

By default AutoQuery Services start with a minimal UI that uses the Request DTO name to identify the Query.
An example of this can be seen with the 
[Northwind AutoQuery Services](http://northwind.servicestack.net/ss_admin/autoquery/QueryCustomers) below:

```csharp
[Route("/query/customers")]
public class QueryCustomers : QueryBase<Customer> {}

[Route("/query/orders")]
public class QueryOrders : QueryBase<Order> {}
```

Which renders a UI with the default query and initial fields unpopulated:

[![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/unannotated-autoquery-services.png)](http://northwind.servicestack.net/ss_admin/autoquery/QueryCustomers)

### Marking up AutoQuery Services

To provide a more useful experience to end users you can also markup your AutoQuery Services by annotating them
with the `[AutoQueryViewer]` attribute, as seen in 
[GitHub QueryRepos](http://github.servicestack.net/ss_admin/autoquery/QueryRepos):

```csharp
[Route("/repos")]
[AutoQueryViewer(IconUrl = "octicon:repo",    
    Title = "ServiceStack Repositories", 
    Description = "Browse different ServiceStack repos",
    DefaultSearchField = "Language", DefaultSearchType = "=", DefaultSearchText = "C#",
    DefaultFields = "Id,Name,Language,Description:500,Homepage,Has_Wiki")]
public class QueryRepos : QueryBase<GithubRepo> {}
```

The additional metadata is then used to customize the UI at the following locations:

[![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/query-default-values-markup.png)](http://github.servicestack.net/ss_admin/autoquery/QueryRepos)

Where `Title`, `Description`, `DefaultSearchField`, `DefaultSearchType` and `DefaultSearchText` is a 
straight forward placeholder replacement.

#### IconUrl

Can either be an url to a **24x24** icon or preferably to avoid relying on any external resources, 
Admin UI embeds both
[Google's Material Design Icons](https://design.google.com/icons/) and 
[GitHub's Octicon](https://octicons.github.com/) fonts which can be referenced using the custom
`octicon:` and `material-icons:` schemes, e.g:

 - octicon:icon
 - material-icons:cast

#### DefaultFields

Can hold a subset list of fields from the AutoQuery **Response Type** in the order you want them displayed.
By default fields have a max-width of **300px** but we can override this default with a `:` suffix as seen
with `Description:500` which changes the Description column width to **500px**. Any text longer than its width
is automatically clipped, but you can still see the full-text by hovering over the field or by clicking the 
AutoQuery generated link, calling the AutoQuery Service and viewing the entire results.

> For more, see [Advanced Customizations](https://github.com/ServiceStack/Admin#advanced-customizations)

### Filter AutoQuery Services

The filter textbox can be used to quickly find and browse to AutoQuery Services:

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/filter-autoquery-services.png)

### Authorized Only Queries

Users only see Queries they have access to, this lets you further tailor the UI for users by using the 
`[Authenticate]`, Required Role or Permission attributes to ensure different users only see relevant queries,
e.g. 

```csharp
[RequiredRole("Sales")]
public class QueryOrders : QueryBase<Order> {}
```

Since the Auth attributes are Request Filter Attributes with a server dependency to **ServiceStack.dll**, 
in order to maintain and share a dependency-free **ServiceModel.dll** you should instead define a custom 
AutoQuery in your Service implementations which will inherit any Service or Action filter attributes as normal:

```csharp
public class QueryOrders : QueryBase<Order> {}

[RequiredRole("Sales")]
public class SalesServices : Service
{
    public IAutoQuery AutoQuery { get; set; }

    public object Any(QueryOrders query)
    {
        return AutoQuery.Execute(query, AutoQuery.CreateQuery(query, Request));
    }
}
```

### Updated in Real-time

To enable a fast and productive UX, the generated AutoQuery link and query results are refreshed as-you-type, 
in addition any change to a any query immediately saves the App's state to **localStorage** so users queries 
are kept across page refreshes and browser restarts.

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/search-as-type.png)

The generated AutoQuery Url is kept in-sync and captures the state of the current query and serves as a 
good source for learning how to construct AutoQuery requests that can be used as-is in client applications. 

### Multiple Conditions

Queries can be constructed with multiple conditions by hitting **Enter** or clicking on the **green (+)** button 
(activated when a condition is valid), adding it to the conditions list and clearing the search text:  

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/multiple-conditions.png)

Clicking the **red** remove icon removes the condition.

### Change Content-Type

You can force a query to return a specific Content-Type response by clicking on one of the format links. E.g
clicking on **json** link will add the **.json** extension to the generated url, overriding the browser's
default Content-Type to specify a JSON response: 

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/custom-content-types.png)

### Customize Columns

Results can further customized to show only the columns you're interested in by clicking on the 
**show/hide columns** icon and selecting the columns you want to see in the order you want them added:

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/customize-columns.png)

### Sorting Columns and Paging Results

Results can be sorted in descending or ascending order by clicking on the column headers:

![](https://raw.githubusercontent.com/ServiceStack/Admin/master/img/paging-queries.png)

Clicking the back/forward navigation icons on the left will page through the results in the order specified.

## AutoQuery Enhancements

We've also added a number of new features to AutoQuery that improves performance and enables greater 
flexibility for your AutoQuery Services:

### Parameterized AutoQuery

AutoQuery now generates parameterized sql for all queries where the `{Value}` placeholder in the AutoQuery 
Templates have been changed to use db parameters.

### Customizable Fields

You can now customize which fields you want returned using the new `Fields` property available on all 
AutoQuery Services, e.g:

    ?Fields=Id,Name,Description,JoinTableId

The Fields still need to be defined on the Response DTO as this feature doesn't change the Response
DTO Schema, only which fields are populated. This does change the underlying RDBMS SELECT that's executed, 
also benefiting from reduced bandwidth between your RDBMS and App Server.

### Multiple Conditions

Previously unsupported, AutoQuery now allows specifying multiple conditions with the same name, e.g:

    ?DescriptionContains=Service&DescriptionContains=Stack

### Named Connection

Related to our improved support for multi-tenancy applications, AutoQuery can easily be used to query 
any number of different databases registered in your AppHost. 

In the example below we configure our main RDBMS to use SQL Server and register a **Named Connection** 
to point to a **Reporting** PostgreSQL RDBMS:

```csharp
var dbFactory = new OrmLiteConnectionFactory(connString, SqlServer2012Dialect.Provider);
container.Register<IDbConnectionFactory>(dbFactory);

dbFactory.RegisterConnection("Reporting", pgConnString, PostgreSqlDialect.Provider);
```

Any normal AutoQuery Services like `QueryOrders` will use the default SQL Server connection whilst 
`QuerySales` will execute its query on the PostgreSQL `Reporting` Database instead:

```csharp
public class QueryOrders : QueryBase<Order> {}

[NamedConnection("Reporting")]
public class QuerySales : QueryBase<Sales> {}
```

### Generate AutoQuery Services from OrmLite T4 Templates

[Richard Safier](https://forums.servicestack.net/t/t4-template-autoquery-feature/1911) 
from the ServiceStack community has extended OrmLite's T4 Templates to include support for generating 
AutoQuery Services for each Table POCO model using the new opt-in 
[CreateAutoQueryTypes](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/0e5c42b84a55e446c39ff3c4e01f361916e591b1/src/T4/OrmLite.Poco.tt#L13)
option whilst the new 
[AddNamedConnection](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/0e5c42b84a55e446c39ff3c4e01f361916e591b1/src/T4/OrmLite.Poco.tt#L14)
option can be used to generate `[NamedConnection]` annotations.

With this feature Richard was able to generate thousands of fully-queryable AutoQuery Services spanning
multiple databases in a single ServiceStack instance with just the T4 templates and configuration.

### AdminFeature Rich UI Implementation

We'd like to make a special mention of how `AdminFeature` was built and deployed as ServiceStack makes it
really easy to package and deploy rich plugins with complex UI and behavior encapsulated within a single plugin - 
which we hope spurs the creation of even richer community [Plugins](github.com/ServiceStack/ServiceStack/wiki/Plugins)!

Development of `AdminFeature` is maintained in a TypeScript 1.8 + JSPM + React
[ServiceStack.Admin.WebHost](https://github.com/ServiceStack/Admin/tree/master/src/ServiceStack.Admin.WebHost) 
project where it's structured to provide an optimal iterative development experience. 
To re-package the App we just call on JSPM to create our app.js bundle by pointing it to the React App's 
`main` entry point:
 
    jspm bundle -m src\main ..\ServiceStack.Admin\ss_admin\app.js

Then each of the static resources are copied into the Plugins 
[ServiceStack.Admin](https://github.com/ServiceStack/Admin/tree/master/src/ServiceStack.Admin) 
project with their **Build Action** set to **Embedded Resource** so they're embedded in the 
**ServiceStack.Admin.dll**.

To add the Embedded Resources to the 
[Virtual File System](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system)
the `AdminFeature` just adds it to `Config.EmbeddedResourceBaseTypes` (also making it safe to ILMerge).

The entire server implementation for the `AdminFeature` is contained below, most of which is dedicated to 
supporting when ServiceStack is mounted at both root `/` or a custom path (e.g. `/api`) - which it supports 
by rewriting the embedded `index.html` with the `HandlerFactoryPath` before returning it:

```csharp
public class AdminFeature : IPlugin, IPreInitPlugin
{
    public void Configure(IAppHost appHost)
    {
        //Register ServiceStack.Admin.dll as an Embedded Resource to VirtualFiles
        appHost.Config.EmbeddedResourceBaseTypes.Add(typeof(AdminFeature));
    }

    public void Register(IAppHost appHost)
    {
        var indexHtml = appHost.VirtualFileSources.GetFile("ss_admin/index.html").ReadAllText();
        if (appHost.Config.HandlerFactoryPath != null) //Inject HandlerFactoryPath if mounted at /custom path
            indexHtml = indexHtml.Replace("/ss_admin", "/{0}/ss_admin".Fmt(appHost.Config.HandlerFactoryPath));

        appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) => 
            pathInfo.StartsWith("/ss_admin") 
                ? (pathInfo == "/ss_admin/index.html" || !appHost.VirtualFileSources.FileExists(pathInfo)
                    ? new StaticContentHandler(indexHtml, MimeTypes.Html) as IHttpHandler
                    : new StaticFileHandler(appHost.VirtualFileSources.GetFile(pathInfo)))
                : null);

        appHost.GetPlugin<MetadataFeature>()
            .AddPluginLink("/ss_admin/autoquery/", "AutoQuery Viewer"); //Add link to /metadata page
    }
}
```

To power most of its UI, AutoQuery Viewer makes use of the 
[existing Metadata service in AutoQuery](https://github.com/ServiceStack/Admin#advanced-customizations).

#### Code-first POCO Simplicity

Other classes worth reviewing is the 
[GitHubTasks.cs](https://github.com/ServiceStack/Admin/blob/master/tests/Admin.Tasks/GitHubTasks.cs) and
[StackOverflowTasks.cs](https://github.com/ServiceStack/Admin/blob/master/tests/Admin.Tasks/StackOverflowTasks.cs)
containing the NUnit tests used to create the test sqlite database on-the-fly, directly from the GitHub and 
StackOverflow JSON APIs, the ease of which speaks to the simplicity of 
[ServiceStack's code-first POCO approach](http://stackoverflow.com/a/32940275/85785).

## [Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)

We've published a couple of new examples projects showing how easy it is to create rich, interactive native 
mobile and web apps using Server Events. 

### [Xamarin.Android Chat](https://github.com/ServiceStackApps/AndroidXamarinChat)

Xamarin.Android Chat utilizes the 
[.NET PCL Server Events Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-Server-Events-Client)
to create an Android Chat App connecting to the existing 
[chat.servicestack.net](http://chat.servicestack.net/) back-end where it's able to communicate with existing 
Ajax clients and other connected Android Chat Apps. The example shows how to enable a native integrated 
experience by translating the existing `cmd.announce` message into an Android notification as well shows how to 
use Xamarin.Auth to authenticate with ServiceStack using Twitter Auth. 

Click the video below to see a quick demo of it in action:

> [YouTube Video](https://www.youtube.com/watch?v=tImAm2LURu0)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/xamarin-android-server-events.png)](https://www.youtube.com/watch?v=tImAm2LURu0)

For a deeper dive, checkout the feature list and source code from the 
[AndroidXamarinChat](https://github.com/ServiceStackApps/AndroidXamarinChat) 
GitHub repo.

### [Networked Time Traveller Shape Creator](https://github.com/ServiceStackApps/typescript-redux#example-9---real-time-networked-time-traveller)

We've also added Server Events to convert a 
[stand-alone Time Traveller Shape Creator](https://github.com/ServiceStackApps/typescript-redux#example-8---time-travelling-using-state-snapshots)
into a networked one where users can **connect to** and **watch** other users using the App in real-time similar 
to how users can use Remote Desktop to watch another computer's screen: 

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redux-chrome-safari.png)](http://redux.servicestack.net)

> Live demo at: http://redux.servicestack.net

Surprisingly most of the client code required to enable this is encapsulated within a single 
[React Connect component](https://github.com/ServiceStackApps/typescript-redux/blob/master/src/TypeScriptRedux/src/example09/Connect.tsx).

The networked Shape Creator makes use of 2 back-end Services that lets users publish their actions to a channel 
and another Service to send a direct message to a User. The 
[implementation for both services](https://github.com/ServiceStackApps/typescript-redux/blob/master/src/TypeScriptRedux/Global.asax.cs) 
is contained below:

```csharp
//Services Contract
[Route("/publish-channel/{Channel}")]
public class PublishToChannel : IReturnVoid, IRequiresRequestStream
{
    public string Channel { get; set; }
    public string Selector { get; set; }
    public Stream RequestStream { get; set; }
}

[Route("/send-user/{To}")]
public class SendUser : IReturnVoid, IRequiresRequestStream
{
    public string To { get; set; }
    public string Selector { get; set; }
    public Stream RequestStream { get; set; }
}

//Services Implementation
public class ReduxServices : Service
{
    public IServerEvents ServerEvents { get; set; }

    public void Any(PublishToChannel request)
    {
        var msg = request.RequestStream.ReadFully().FromUtf8Bytes();
        ServerEvents.NotifyChannel(request.Channel, request.Selector, msg);
    }

    public void Any(SendUser request)
    {
        var msg = request.RequestStream.ReadFully().FromUtf8Bytes();
        ServerEvents.NotifyUserId(request.To, request.Selector, msg);
    }
}
```

Essentially just calling `IServerEvents` to forward the raw JSON Request Body to the specified channel or user.

### Updating Channels on Live Subscriptions

Previously to change Server Event channel subscriptions you would need to create a new connection with the 
channels you wanted to join. You can now update a live Server Events connection with Channels you want to 
Join or Leave using the new built-in ServerEvents `UpdateEventSubscriber` Service:

```csharp
[Route("/event-subscribers/{Id}", "POST")]
public class UpdateEventSubscriber : IReturn<UpdateEventSubscriberResponse>
{
    public string Id { get; set; }
    public string[] SubscribeChannels { get; set; }
    public string[] UnsubscribeChannels { get; set; }
}
```

This lets you modify your active subscription with channels you want to join  or leave with a HTTP POST Request, e.g:

    POST /event-subscribers/{subId}
    SubscribeChannels=chan1,chan2&UnsubscribeChannels=chan3,chan4

### New onUpdate Notification

As this modifies the active subscription it also publishes a new **onUpdate** notification to all channel 
subscribers so they're able to maintain up-to-date info on each subscriber. 

In C# `ServerEventsClient` this can be handled together with **onJoin** and **onLeave** events using `OnCommand`:

```csharp
client.OnCommand = msg => ...; //= ServerEventJoin, ServerEventLeave or ServerEventUpdate
```

In the ss-utils JavaScript Client this can be handled with a Global Event Handler, e.g:

```javascript
$(source).handleServerEvents({
    handlers: {
        onConnect: connectedUserInfo => { ... },
        onJoin: userInfo => { ... },
        onLeave: userInfo => { ... },
        onUpdate: userInfo => { ... }
    }
});
```

### .NET UpdateSubscriber APIs

Typed versions of this API is built into the C# `ServerEventsClient` in both sync/async versions:

```csharp
client.UpdateSubscriber(new UpdateEventSubscriber { 
    SubscribeChannels = new[]{ "chan1", "chan2" },
    UnsubscribeChannels = new[]{ "chan3", "chan4" },
});

client.SubscribeToChannels("chan1", "chan2");
client.UnsubscribeFromChannels("chan3", "chan4");

await client.SubscribeToChannelsAsync("chan1", "chan2");
await client.UnsubscribeFromChannelsAsync("chan3", "chan4");
```

### JavaScript UpdateSubscriber APIs

As well as in ServiceStack's ss-utils JavaScript library:

```javascript
$.ss.updateSubscriber({ 
    SubscribeChannels: "chan1,chan2",
    UnsubscribeChannels: "chan3,chan4"
});

$.ss.subscribeToChannels(["chan1","chan2"], response => ..., error => ...);
$.ss.unsubscribeFromChannels(["chan3","chan4"], response => ..., error => ...);
```

### ServerEvents Update Channel APIs

Whilst internally, from within ServiceStack you can update a channel's subscription using the new
[IServerEvents](https://github.com/ServiceStack/ServiceStack/blob/b9a33c34d0b0eedbcc6b3483257f1dc37bbf713f/src/ServiceStack/ServerEventsFeature.cs#L1004)
APIs:

```csharp
public interface IServerEvents 
{
    ...
    void SubscribeToChannels(string subscriptionId, string[] channels);
    void UnsubscribeFromChannels(string subscriptionId, string[] channels);
}
```

## [TypeScript React App (Beta)](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/typescript-react-jspm-banner.png)](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

We've spent a fair amount of time researching the JavaScript ecosystem to discover what we believe offers VS.NET 
developers the most optimal balance of power, simplicity and tooling to build and maintain large JavaScript Apps. 
[ES6](https://github.com/lukehoban/es6features) 
offers a number of language improvements to ES5-compatible JavaScript making it much more enjoyable to 
develop modern applications with, which we believe justifies the additional tooling needed to transpile it to 
support down-level ES5 browsers. Given the lack of support for Babel/ES6 in VS.NET, the best option to access 
ES6 features is to use 
[TypeScript](http://www.typescriptlang.org/) which also offers its own benefits over and beyond ES6.

The decision to use TypeScript also meant revisiting other tools used in our Single Page App templates. One of
the most productive features in ES6/TypeScript is being able to easily use modules to modularize your code which 
provides an optimal development experience for maintaining large and complex code-bases. For this to work 
seamlessly we needed to integrate TypeScript modules with our front-end JavaScript package manager which is 
why we've replaced [bower](http://bower.io/) with [JSPM](http://jspm.io/) and configured TypeScript to use the 
Universal [SystemJS module format](https://github.com/systemjs/systemjs). 

Finally to minimize JavaScript fatigue, we've removed as much complexity and moving parts as we could and 
have removed [Grunt](http://gruntjs.com/) in favor of leaving only a [Gulp](http://gulpjs.com/) 
JS build system without any
[loss of functionality](https://github.com/ServiceStackApps/ReactDesktopApps#defaultapp-project).

With these changes we've hand picked what we believe is the current **Gold Standard** for developing modern 
JavaScript Apps in VS.NET with the just released 
[TypeScript 1.8](http://www.typescriptlang.org/), 
[React](https://facebook.github.io/react/), 
[JSPM](http://jspm.io/), 
[Gulp](http://gulpjs.com/) and 
[typings](https://github.com/typings/typings) (the successor to [TSD](https://github.com/DefinitelyTyped/tsd)). 
We're also greatly benefiting from this Technology Stack ourselves with the development our latest 
[AutoQuery Viewer](https://github.com/ServiceStack/Admin) TypeScript App.

We've integrated these powerful combinations of technologies and packaged it in the new 
**TypeScript React App (Beta)** VS.NET template that's now available in the updated 
[ServiceStackVS VS.NET Extension](https://github.com/ServiceStack/ServiceStackVS):

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/typescript-react-jspm-template.png)

## [TypeScript Redux](https://github.com/ServiceStackApps/typescript-redux)

To help developers familiarize themselves with these technologies we've also published an in-depth step-by-step 
guide for beginners that starts off building the simplest HelloWorld TypeScript React App from scratch then 
slowly growing with each example explaining how TypeScript, React and Redux can be used to easily create a 
more complex networked Time Travelling Shape Creator as seen in the final Example:

[![](https://raw.githubusercontent.com/ServiceStackApps/typescript-redux/master/img/preview-09.png)](https://github.com/ServiceStackApps/typescript-redux)

> Live Demo: [http://redux.servicestack.net](http://redux.servicestack.net)

Except for the final demo above, all other examples are pure client-side only demos, i.e. without any 
server dependencies and can be previewed directly from the static GitHub website below:

 - [Example 1 - HelloWorld](http://servicestackapps.github.io/typescript-redux/example01/)
 - [Example 2 - Modularizing HelloWorld](http://servicestackapps.github.io/typescript-redux/example02/)
 - [Example 3 - Creating a stateful Component](http://servicestackapps.github.io/typescript-redux/example03/)
 - [Example 4 - Change Counter to use Redux](http://servicestackapps.github.io/typescript-redux/example04/)
 - [Example 5 - Use Provider to inject store in child Context](http://servicestackapps.github.io/typescript-redux/example05/)
 - [Example 6 - Use connect() to make Components stateless](http://servicestackapps.github.io/typescript-redux/example06/)
 - [Example 7 - Shape Creator](http://servicestackapps.github.io/typescript-redux/example07/)
 - [Example 8 - Time Travelling using State Snapshots](http://servicestackapps.github.io/typescript-redux/example08/)

## [ss-utils](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library)

### ss-utils now available on npm and DefinitelyTyped

To make it easier to develop with **ss-utils** in any of the npm-based Single Page Apps templates we're 
maintaining a copy of [ss-utils in npm](https://www.npmjs.com/package/ss-utils) and have also added it to JSPM 
and DefinitelyTyped registry so you can now add it to your project like any other external dependency using JSPM:

    C:\> jspm install ss-utils

If you're using TypeScript, you can also download the accompanying TypeScript definition from:

    C:\>typings install ss-utils --ambient --save
    
Or if you're using the older tsd package manager: `tsd install ss-utils --save`.

### New ss-utils API's

We've added new core utils to make it easier to create paths, urls, normalize JSON responses and send POST
JSON Requests:

#### combinePaths and createUrl

The new `combinePaths` and `createUrl` API's help with constructing urls, e.g:

```javascript
$.ss.combinePaths("path","to","..","join")   //= path/join
$.ss.createPath("path/{foo}", {foo:1,bar:2}) //= path/1

$.ss.createUrl("http://host/path/{foo}",{foo:1,bar:2}) //= http://host/path/1?bar=2
```

This is a change from previous release where `createUrl()` behaved like `createPath()`.

#### normalize and normalizeKey

The new `normalizeKey` and `normalize` APIs helps with normalizing JSON responses with different naming 
conventions by converting each property into lowercase with any `_` separators removed - `normalizeKey()` 
converts a single string whilst `normalize()` converts an entire object graph, e.g:

```javascript
$.ss.normalizeKey("THE_KEY") //= thekey

JSON.stringify(
    $.ss.normalize({THE_KEY:"key",Foo:"foo",bar:{A:1}})
)   //= {"thekey":"key","foo":"foo","bar":{"A":1}}

const deep = true;
JSON.stringify(
    $.ss.normalize({THE_KEY:"key",Foo:"foo",bar:{A:1}}, deep) 
)   //= {"thekey":"key","foo":"foo","bar":{"a":1}}
```

#### postJSON

Finally `postJSON` is jQuery's missing equivalent to `$.getJSON`, but for POST's, eg:

```javascript
$.ss.postJSON(url, {data:1}, response => ..., error => ...);
```

## Customize JSON Responses on-the-fly

The JSON and JSV Responses for all Services (inc. AutoQuery Services) can now be further customized with the 
new `?jsconfig` QueryString param which lets your Service consumers customize the returned JSON Response to 
their preference. This works similar to having wrapped your Service response in a `HttpResult` with a Custom 
`ResultScope` in the Service implementation to enable non-default customization of a Services response, e.g:

    /service?jsconfig=EmitLowercaseUnderscoreNames,ExcludeDefaultValues
    
Works similarly to:

```csharp
return new HttpResult(new { TheKey = "value", Foo=0 }) {
    ResultScope = () => JsConfig.With(
        emitLowercaseUnderscoreNames:true, excludeDefaultValues:true)
};
```

Which results in **lowercase_underscore** key names with any properties with **default values removed**:

    {"the_key":"value"}

It also supports cascading server and client ResultScopes, with the client `?jsconfig` taking precedence.

Nearly all `JsConfig` scope options are supported other than delegates and complex type configuration properties.

### Camel Humps Notation

JsConfig also supports Camel Humps notation letting you target a configuration by just using the 
**Uppercase Letters** in the property name which is also case-insensitive so an equivalent shorter version 
of the above config can be:

    ?jsconfig=ELUN,edv
    
Camel Humps also works with Enum Values so both these two configurations are the same:

    ?jsconfig=DateHandler:UnixTime
    ?jsconfig=dh:ut

### Custom JSON Live Example

AutoQuery Viewer makes use of this feature in order to return human readable dates using the new 
`ISO8601DateOnly` DateHandler Enum Value as well as appending `ExcludeDefaultValues` when specifying custom 
fields so that any unpopulated value type properties with default values are excluded from the JSON Response. 
Here's a live example of this comparing the default Response with the customized JSON Response:

 - http://github.servicestack.net/repos.json?fields=Name,Homepage,Language,Updated_At
 - http://github.servicestack.net/repos.json?fields=Name,Homepage,Language,Updated_At&jsconfig=edv,dh:iso8601do

### Custom JSON Settings

The presence of a **bool** configuration property will be set to `true` unless they have a `false` or `0` 
value in which case they will be set to `false`, e.g:

    ?jsconfig=ExcludeDefaultValues:false

For a quick reference the following **bool** customizations are supported:

<table>
    <thead>
        <tr><th>Name</th><th>Alias</th></tr>
    </thead>
    <tr><td>EmitCamelCaseNames</td><td>eccn</td></tr>
    <tr><td>EmitLowercaseUnderscoreNames</td><td>elun</td></tr>
    <tr><td>IncludeNullValues</td><td>inv</td></tr>
    <tr><td>IncludeNullValuesInDictionaries</td><td>invid</td></tr>
    <tr><td>IncludeDefaultEnums</td><td>ide</td></tr>
    <tr><td>IncludePublicFields</td><td>ipf</td></tr>
    <tr><td>IncludeTypeInfo</td><td>iti</td></tr>
    <tr><td>ExcludeTypeInfo</td><td>eti</td></tr>
    <tr><td>ConvertObjectTypesIntoStringDictionary</td><td>cotisd</td></tr>
    <tr><td>TreatEnumAsInteger</td><td>teai</td></tr>
    <tr><td>TryToParsePrimitiveTypeValues</td><td>ttpptv</td></tr>
    <tr><td>TryToParseNumericType</td><td>ttpnt</td></tr>
    <tr><td>ThrowOnDeserializationError</td><td>tode</td></tr>
    <tr><td>EscapeUnicode</td><td>eu</td></tr>
    <tr><td>PreferInterfaces</td><td>pi</td></tr>
    <tr><td>SkipDateTimeConversion</td><td>sdtc</td></tr>
    <tr><td>AlwaysUseUtc</td><td>auu</td></tr>
    <tr><td>AssumeUtc</td><td>au</td></tr>
    <tr><td>AppendUtcOffset</td><td>auo</td></tr>
    <tr><th colspan=2>DateHandler (dh)</th></tr>
    <tr><td>TimestampOffset</td><td>to</td></tr>
    <tr><td>DCJSCompatible</td><td>dcjsc</td></tr>
    <tr><td>ISO8601</td><td>iso8601</td></tr>
    <tr><td>ISO8601DateOnly</td><td>iso8601do</td></tr>
    <tr><td>ISO8601DateTime</td><td>iso8601dt</td></tr>
    <tr><td>RFC1123</td><td>rfc1123</td></tr>
    <tr><td>UnixTime</td><td>ut</td></tr>
    <tr><td>UnixTimeMs</td><td>utm</td></tr>
    <tr><th colspan=2>TimeSpanHandler (tsh)</th></tr>
    <tr><td>DurationFormat</td><td>df</td></tr>
    <tr><td>StandardFormat</td><td>sf</td></tr>
    <tr><th colspan=2>PropertyConvention (pc)</th></tr>
    <tr><td>Strict</td><td>s</td></tr>
    <tr><td>Lenient</td><td>l</td></tr>
</table>

You can also create a scope from a string manually using the new `JsConfig.CreateScope()`, e.g:

```csharp
using (JsConfig.CreateScope("EmitLowercaseUnderscoreNames,ExcludeDefaultValues,dh:ut")) 
{
    var json = dto.ToJson();
}
```

If you don't wish for consumers to be able to customize JSON responses this feature can be disabled with 
`Config.AllowJsConfig=false`.

## Improved support for Multitenancy

All built-in dependencies available from `Service` base class, AutoQuery, Razor View pages, etc are now 
resolved in a central overridable location in your AppHost. 

This now lets you control which dependency is used based on the incoming Request for each Service by overriding 
any of the AppHost methods below, e.g. to change the DB Connection your Service uses you can override 
`GetDbConnection(IRequest)` in your AppHost.

```csharp
public virtual IDbConnection Db
{
    get { return db ?? (db = HostContext.AppHost.GetDbConnection(Request)); }
}

public virtual ICacheClient Cache
{
    get { return cache ?? (cache = HostContext.AppHost.GetCacheClient(Request)); }
}

public virtual MemoryCacheClient LocalCache //New
{
    get { return localCache ?? (localCache = HostContext.AppHost.GetMemoryCacheClient(Request)); }
}

public virtual IRedisClient Redis
{
    get { return redis ?? (redis = HostContext.AppHost.GetRedisClient(Request)); }
}

public virtual IMessageProducer MessageProducer
{
    get { return messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request)); }
}
```

### Change Database Connection at Runtime

The default implementation of `GetDbConnection(IRequest)` includes an easy way to change the DB Connection 
that can be done by populating the 
[ConnectionInfo](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/ConnectionInfo.cs) 
POCO in any
[Request Filter in the Request Pipeline](https://github.com/ServiceStack/ServiceStack/wiki/Order-of-Operations):

```csharp
req.Items[Keywords.DbInfo] = new ConnectionInfo {
    NamedConnection  = ... //Use a registered NamedConnection for this Request
    ConnectionString = ... //Use a different DB connection for this Request
    ProviderName     = ... //Use a different Dialect Provider for this Request
};
```

To illustrate how this works we'll go through a simple example showing how to create an AutoQuery Service 
that lets the user change which DB the Query is run on. We'll control which of the Services we want to allow 
the user to change the DB it's run on by having them implement the interface below:

```csharp
public interface IChangeDb
{
    string NamedConnection { get; set; }
    string ConnectionString { get; set; }
    string ProviderName { get; set; }
}
```

We'll create one such AutoQuery Service, implementing the above interface:

```csharp
[Route("/rockstars")]
public class QueryRockstars : QueryBase<Rockstar>, IChangeDb
{
    public string NamedConnection { get; set; }
    public string ConnectionString { get; set; }
    public string ProviderName { get; set; }
}
``` 

For this example we'll configure our Database to use a default **SQL Server 2012** database, 
register an optional named connection looking at a "Reporting" **PostgreSQL** database and 
register an alternative **Sqlite** RDBMS Dialect that we also want the user to be able to use:

#### ChangeDB AppHost Registration

```csharp
container.Register<IDbConnectionFactory>(c => 
    new OrmLiteConnectionFactory(defaultDbConn, SqlServer2012Dialect.Provider));

var dbFactory = container.Resolve<IDbConnectionFactory>();

//Register NamedConnection
dbFactory.RegisterConnection("Reporting", ReportingConnString, PostgreSqlDialect.Provider);

//Register DialectProvider
dbFactory.RegisterDialectProvider("Sqlite", SqliteDialect.Provider);
```

#### ChangeDB Request Filter

To enable this feature we just need to add a Request Filter that populates the `ConnectionInfo` with properties
from the Request DTO:

```csharp
GlobalRequestFilters.Add((req, res, dto) => {
   var changeDb = dto as IChangeDb;
   if (changeDb == null) return;

   req.Items[Keywords.DbInfo] = new ConnectionInfo {
       NamedConnection = changeDb.NamedConnection,
       ConnectionString = changeDb.ConnectionString,
       ProviderName = changeDb.ProviderName,
   };
});
```

Since our `IChangeDb` interface shares the same property names as `ConnectionInfo`, the above code can be 
further condensed using a 
[Typed Request Filter](https://github.com/ServiceStack/ServiceStack/wiki/Request-and-response-filters#typed-request-filters)
and ServiceStack's built-in [AutoMapping](https://github.com/ServiceStack/ServiceStack/wiki/Auto-mapping)
down to just:

```csharp
RegisterTypedRequestFilter<IChangeDb>((req, res, dto) =>
    req.Items[Keywords.DbInfo] = dto.ConvertTo<ConnectionInfo>());
```

#### Change Databases via QueryString

With the above configuration the user can now change which database they want to execute the query on, e.g:

```csharp
var response = client.Get(new QueryRockstars()); //SQL Server

var response = client.Get(new QueryRockstars {   //Reporting PostgreSQL DB
    NamedConnection = "Reporting"
}); 

var response = client.Get(new QueryRockstars {   //Alternative SQL Server Database
    ConnectionString = "Server=alt-host;Database=Rockstars;User Id=test;Password=test;"
}); 

var response = client.Get(new QueryRockstars {   //Alternative SQLite Database
    ConnectionString = "C:\backups\2016-01-01.sqlite",
    ProviderName = "Sqlite"
}); 
```

### ConnectionInfo Attribute

To make it even easier to use we've also wrapped this feature in a simple
[ConnectionInfoAttribute.cs](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/ConnectionInfoAttribute.cs)
which allows you to declaratively specify which database a Service should be configured to use, e.g we can
configure the `Db` connection in the Service below to use the PostgreSQL **Reporting** database with:

```csharp
[ConnectionInfo(NamedConnection = "Reporting")]
public class ReportingServices : Service
{
    public object Any(Sales request)
    {
        return new SalesResponse { Results = Db.Select<Sales>() };
    }
}
```

### [Multi Tenancy Example](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/MultiTennantAppHostTests.cs)

To show how much easier it is to implement a Multi Tenancy Service with this feature we've updated the 
[Multi Tenancy AppHost Example](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/MultiTennantAppHostTests.cs) 
comparing it with the previous approach of implementing a Custom `IDbConnectionFactory`.

### New CreateQuery overloads for Custom AutoQuery

In order for AutoQuery to pass the current `IRequest` into the new `AppHost.GetDbConnection(IRequest)` method 
it needs to be passed when calling `CreateQuery`. 2 new API's have been added that now does this:

```csharp
public class MyServices : Service
{
    public IAutoQuery AutoQuery { get; set; }

    public object Any(Request dto)
    {
        var q = AutoQuery.CreateQuery(dto, base.Request);
        //Calls:
        //var q = AutoQuery.CreateQuery(dto, base.Request.GetRequestParams(), base.Request);
        return AutoQuery.Execute(request, q);
    }
}
```

## ServiceClient URL Resolvers

The urls used in all .NET Service Clients are now customizable with the new `UrlResolver` and `TypedUrlResolver` 
delegates. 

E.g. you can use this feature to rewrite the URL used with the Request DTO Type Name used as the subdomain by:

```csharp
[Route("/test")] 
class Request {}

var client = JsonServiceClient("http://example.org/api") {
    TypedUrlResolver =  (meta, httpMethod, dto) => 
        meta.BaseUri.Replace("example.org", dto.GetType().Name + ".example.org")
            .CombineWith(dto.ToUrl(httpMethod, meta.Format)));
};

var res = client.Get(new Request());  //= http://Request.example.org/api/test
var res = client.Post(new Request()); //= http://Request.example.org/api/test
```

This feature is also implemented in `JsonHttpClient`, examples below shows rewriting APIs that use custom urls:

```csharp
var client = JsonHttpClient("http://example.org/api") {
    UrlResolver = (meta, httpMethod, url) => 
        meta.BaseUri.Replace("example.org", "111.111.111.111").CombineWith(url))
};

await client.DeleteAsync<MockResponse>("/dummy"); 
//=http://111.111.111.111/api/dummy

await client.PutAsync<MockResponse>("/dummy", new Request()); 
//=http://111.111.111.111/api/dummy
```

## [ServiceStack.Discovery.Consul](https://github.com/wwwlicious/servicestack-discovery-consul)

This feature was added to make it easier to support the new 
[ServiceStack.Discovery.Consul](https://github.com/wwwlicious/servicestack-discovery-consul)
plugin by [Scott Mackay](https://twitter.com/wwwlicious) which enables external RequestDTO endpoint discovery 
by integrating with [Consul.io](http://consul.io) to provide automatic service registration and health checking.

![RequestDTO Service Discovery](https://raw.githubusercontent.com/wwwlicious/servicestack-discovery-consul/master/assets/RequestDTOServiceDiscovery.png)

To use the plugin install it from NuGet:

    Install-Package ServiceStack.Discovery.Consul

Then configure your AppHost specifying the external `WebHostUrl` for this Service as well as registering the
`ConsulFeature` plugin: 

```csharp
public class AppHost : AppSelfHostBase
{
    public AppHost() : base("MyService", typeof(MyService).Assembly) {}

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
            // the url:port that other services will use to access this one
            WebHostUrl = "http://api.acme.com:1234"
            ApiVersion = "2.0" // optional
        });

        // Pass in any ServiceClient and it will be autowired with Func
        Plugins.Add(new ConsulFeature(new JsonServiceClient()));
    }
}
```

You'll also need to 
[install and start a Consul agent](https://github.com/wwwlicious/servicestack-discovery-consul/blob/master/README.md#running-your-services)
after which once the AppHost is initialized you should see it automatically appear in the 
[Consul UI](https://www.consul.io/intro/getting-started/ui.html) and disappear after the AppHost is shutdown:

![Automatic Service Registration](https://raw.githubusercontent.com/wwwlicious/servicestack-discovery-consul/master/assets/ServiceRegistration.png)

In your Services you'll then be able to use the Consul-injected `IServiceClient` to call any external services
and it will automatically send the request to an active Service endpoint that handles each Request DTO, e.g:

```csharp
public class MyService : Service
{
    public IServiceClient Client { get; set; }

    public void Any(RequestDTO dto)
    {
        // the client will resolve the correct uri for the external dto using consul
        var response = Client.Post(new ExternalDTO { Custom = "bob" });
    }
}
```

#### Health checks

![Default Health Checks](https://raw.githubusercontent.com/wwwlicious/servicestack-discovery-consul/master/assets/HealthChecks.png)

By default the plugin creates 2 health checks used to filter out failing instances of your services:

1. Heartbeat: Creates an endpoint in your service [http://locahost:1234/reply/json/heartbeat](http://locahost:1234/reply/json/heartbeat) that expects a 200 response
2. If Redis has been configured in the AppHost, it will check if Redis is responding

For more info checkout [servicestack-discovery-consul](https://github.com/wwwlicious/servicestack-discovery-consul/) GitHub Repo.

## Multiple File Uploads

New .NET APIs have been added to all .NET Service Clients that allow you to easily upload multiple streams
within a single HTTP request. It supports populating Request DTO with any combination of QueryString and 
POST'ed FormData in addition to multiple file upload data streams:

```csharp
using (var stream1 = uploadFile1.OpenRead())
using (var stream2 = uploadFile2.OpenRead())
{
    var client = new JsonServiceClient(baseUrl);
    var response = client.PostFilesWithRequest<MultipleFileUploadResponse>(
        "/multi-fileuploads?CustomerId=123",
        new MultipleFileUpload { CustomerName = "Foo,Bar" },
        new[] {
            new UploadFile("upload1.png", stream1),
            new UploadFile("upload2.png", stream2),
        });
}
```

Or using only a Typed Request DTO. The `JsonHttpClient` also includes async equivalents for each of the new 
`PostFilesWithRequest` APIs:

```csharp
using (var stream1 = uploadFile1.OpenRead())
using (var stream2 = uploadFile2.OpenRead())
{
    var client = new JsonHttpClient(baseUrl);
    var response = await client.PostFilesWithRequestAsync<MultipleFileUploadResponse>(
        new MultipleFileUpload { CustomerId = 123, CustomerName = "Foo,Bar" },
        new[] {
            new UploadFile("upload1.png", stream1),
            new UploadFile("upload2.png", stream2),
        });
}
```

Special thanks to [@rsafier](https://github.com/rsafier) for contributing support for Multiple File Uploads.

### PCL WinStore Client retargeted to 8.1

Following a VS.NET Update, we've upgraded the **WinStore** PCL ServiceStack.Client to target **8.1**. 

### Local MemoryCacheClient

As it sometimes beneficial to have access to a local in-memory Cache in addition to your registered `ICacheClient` 
[Caching Provider](https://github.com/ServiceStack/ServiceStack/wiki/Caching)
we've pre-registered a `MemoryCacheClient` that all your Services now have access to from the `LocalCache` 
property, i.e:

```csharp
    MemoryCacheClient LocalCache { get; }
```

This doesn't affect any existing functionality that utilizes a cache like Sessions which continue to use
your registered `ICacheClient`, but it does let you change which cache you want different responses to use, e.g: 

```csharp
var cacheKey = "unique_key_for_this_request";
return base.Request.ToOptimizedResultUsingCache(LocalCache, cacheKey, () => {
    //Delegate is executed if item doesn't exist in cache 
});
```

If you don't register a `ICacheClient` ServiceStack automatically registers a `MemoryCacheClient` for you 
which will also refer to the same instance registered for `LocalCache`.

### Cookies

If you're using a Custom `AuthProvider` that doesn't rely on Session Cookies you can disable them from being
created with `Config.AllowSessionCookies=false`. The Cookie behavior can be further customized by overriding
`AllowSetCookie()` in your AppHost, E.g. you can disable all cookies with:

```csharp
public override bool AllowSetCookie(IRequest req, string cookieName)
{
    return false;
}
```

## Redis

 - [Added support for HashSet API's in Redis Transactions](https://github.com/ServiceStack/ServiceStack.Redis/commit/d6ed687a1f25cec0f43fb04e3c906905b5c99085)
 
## OrmLite

New `[EnumAsInt]` attribute as an alternative to `[Flags]` for storing Enums as ints in OrmLite but still 
have them serialized as strings in Service responses.

Free-text SQL Expressions are now converted to Parameterized Statements, e.g:

```csharp
var q = db.From<Rockstar>()
    .Where("Id < {0} AND Age = {1}", 3, 27);

var results = db.Select(q);
```

### Select Fields

The new Select API on SqlExpression enables a resilient way to select custom fields matching the first column it 
finds (from primary table then joined tables). It also fully qualifies field names to avoid ambiguous columns, 
allows matching of joined tables with `{Table}{Column}` convention and ignores any non-matching fields, e.g:

```csharp
var q = db.From<Rockstar>()
    .Join<RockstarAlbum>((r,a) => r.Id == a.RockstarId)
    .Select(new[] { "Id", "FirstName", "Age", "RockstarAlbumName", "_unknown_" });
```

### Other OrmLite Changes

 - All `db.Exists()` APIs have been optimized to only query a single column and row.
 - New `db.SelectLazy()` API added that accepts an SqlExpression
 - Max String column definition for MySQL now uses **LONGTEXT**

### ServiceStack.Text

 - New `JsConfig.SkipDateTimeConversion` to skip built-in Conversion of DateTime's.
 - New `ISO8601DateOnly` and `ISO8601DateTime` DateHandler formats to emit only the Date or Date and Time 

## Stripe Gateway

Added support for serializing nested complex entities using Stripe's unconventional object notation and 
the new `CreateStripeAccount` requiring it, e.g:

```csharp
var response = gateway.Post(new CreateStripeAccount
{
    Country = "US",
    Email = "test@email.com",
    Managed = true,
    LegalEntity = new StripeLegalEntity
    {
        Address = new StripeAddress
        {
            Line1 = "1 Highway Rd",
            City = "Brooklyn",
            State = "NY",
            Country = "US",
            PostalCode = "90210",
        },
        Dob = new StripeDate(1980, 1, 1),
        BusinessName = "Business Name",
        FirstName = "First",
        LastName = "Last",
    }
});
```

Which sends a POST Form Data request that serializes the nested Dob into the object notation Stripe expects, e.g:

    &legal_entity[dob][year]=1970&legal_entity[dob][month]=1&legal_entity[dob][day]=1 
 
## Minor ServiceStack Features

 - Old Session removed and invalided when generating new session ids for a new AuthRequest
 - New ResourcesResponseFilter, ApiDeclarationFilter and OperationFilter added to SwaggerFeature to modify response
 - `Name` property added to `IHttpFiles` in Response.Files collection
 - `HostType`, `RootDirectoryPath`, `RequestAttributes`, `Ipv4Addresses` and `Ipv6Addresses` added to [?debug=requestinfo](https://github.com/ServiceStack/ServiceStack/wiki/Debugging#request-info)
 - `StaticFileHandler` now has `IVirtualFile` and `IVirtualDirectory` constructor overloads
 - New `StaticContentHandler` for returning custom text or binary responses in `RawHttpHandlers`
 
## Changes

`IRequest.GetRequestParams()` Dictionary now returns any duplicate fields with a hash + number suffix, e.g: `#1`.
To retain existing behavior where duplicate values are merged into a `,` delimited string use 
`IRequest.GetFlattenedRequestParams()`

HttpListener now returns the `RemoteEndPoint` as the `UserHostAddress` matching the `UserHostAddress` returned 
in ASP.NET Web Applications.

## WARNING .NET 4.0 builds will cease after August 1, 2016

Microsoft has 
[discontinued supporting .NET 4.0, 4.5 and 4.5.1](https://blogs.msdn.microsoft.com/dotnet/2015/12/09/support-ending-for-the-net-framework-4-4-5-and-4-5-1/)
as of January 12th, 2016. We've already started seeing a number of 3rd Party NuGet packages already drop
support for .NET 4.0 builds which has kept us referencing old versions. As a result we intend to follow and 
stop providing .NET 4.0 builds ourselves after August 1st, 2016. If you absolutely need access to .NET 4.0
builds after this date please 
[leave a comment on this UserVoice entry](https://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/12528912-continue-supporting-net-4-0-projects).


# v4.0.52 Release Notes

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


